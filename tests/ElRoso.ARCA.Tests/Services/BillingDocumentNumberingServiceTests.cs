// EN: Tests for BillingDocumentNumberingService — covers validation gate and routing branches
// that don't depend on real SOAP clients. Deep SOAP-touching coverage requires extracting a
// factory for the WCF clients (deferred to a future refactor).
// ES: Tests para BillingDocumentNumberingService — cubren el gate de validación y las ramas de
// routing que no dependen de los SOAP clients reales. La cobertura profunda requiere extraer
// un factory para los clientes WCF (diferido a refactor futuro).
using ElRoso.ARCA.Caching;
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Services;
using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;
using ElRoso.ARCA.Services;
using ElRoso.ARCA.Services.Soap;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElRoso.ARCA.Tests.Services;

public class BillingDocumentNumberingServiceTests
{
    private readonly Mock<ILoginTicketService> loginTicketServiceMock = new();
    private readonly Mock<ITokenCache> tokenCacheMock = new();
    private readonly Mock<IValidator<BillingDocumentNumberingRequest>> validatorMock = new();
    private readonly Mock<IPadronOperations> padronMock = new();
    private readonly Mock<IWsfeOperations> wsfeMock = new();
    private readonly Mock<IWsfexOperations> wsfexMock = new();
    private readonly ARCAOptions options = new()
    {
        IsProduction = false,
        CertificatePath = "/dev/null",
        CertificatePassword = null,
        TokenCacheDirectory = Path.GetTempPath(),
        SoapTimeoutSeconds = 5,
    };

    private BillingDocumentNumberingService CreateService() => new(
        NullLogger<BillingDocumentNumberingService>.Instance,
        loginTicketServiceMock.Object,
        tokenCacheMock.Object,
        options,
        validatorMock.Object,
        padronMock.Object,
        wsfeMock.Object,
        wsfexMock.Object);

    private static BillingDocumentNumberingRequest MinimalRequest(BillingDocumentTypeARCAEnum type) => new()
    {
        BillingDocumentType       = type,
        BillingDocumentBookPrefix = 1,
        BillingDocumentDate       = new DateTime(2026, 5, 13),
        Currency                  = "Pesos",
        ExchangeRate              = 1,
        ConceptType               = ConceptTypeARCAEnum.Products,
        AmountTax                 = 100,
        IssuingCompany = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 20123456789 },
        Client         = new() { DocumentType = DocumentTypeARCAEnum.SIN_IDENTIFICAR, DocumentNumber = 0 },
    };

    [Fact]
    public void Constructor_should_accept_all_dependencies_without_throwing()
    {
        var act = CreateService;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AuthorizeAsync_should_throw_ARCAValidationException_when_validator_fails()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("Currency", "Currency is not supported") { ErrorCode = "CUR001" },
                new ValidationFailure("AmountTax", "AmountTax must be positive") { ErrorCode = "AMT002" },
            }));

        var service = CreateService();

        var act = () => service.AuthorizeAsync(MinimalRequest(BillingDocumentTypeARCAEnum.FA));

        var exception = await act.Should().ThrowAsync<ARCAValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
        exception.Which.Errors.Should().Contain(e => e.Contains("CUR001") && e.Contains("Currency is not supported"));
        exception.Which.Errors.Should().Contain(e => e.Contains("AMT002") && e.Contains("AmountTax must be positive"));
    }

    [Fact]
    public async Task AuthorizeAsync_should_skip_SOAP_when_validation_fails()
    {
        // EN: When validation fails the service must not consult the token cache or hit WSAA.
        // ES: Cuando la validación falla, el service no debe consultar el caché de tokens ni WSAA.
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult([new ValidationFailure("Field", "bad")]));

        var service = CreateService();

        await Assert.ThrowsAsync<ARCAValidationException>(
            () => service.AuthorizeAsync(MinimalRequest(BillingDocumentTypeARCAEnum.FA)));

        tokenCacheMock.Verify(t => t.GetAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        loginTicketServiceMock.Verify(
            l => l.GetLoginTicketAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthorizeAsync_should_throw_ARCAServiceException_for_unsupported_document_type()
    {
        // EN: Remittances (91) is a valid enum but not routed to WSFEv1 or WSFEXv1.
        // ES: Remittances (91) es un enum válido pero no se rutea a WSFEv1 ni WSFEXv1.
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult());

        var service = CreateService();

        var act = () => service.AuthorizeAsync(MinimalRequest(BillingDocumentTypeARCAEnum.Remittances));

        var exception = await act.Should().ThrowAsync<ARCAServiceException>();
        exception.Which.Message.Should().Contain("Unsupported document type");
        exception.Which.Message.Should().Contain(nameof(BillingDocumentTypeARCAEnum.Remittances));
    }

    [Fact]
    public async Task AuthorizeAsync_should_call_validator_before_any_external_call()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult([new ValidationFailure("X", "y")]));

        var service = CreateService();

        await Assert.ThrowsAsync<ARCAValidationException>(
            () => service.AuthorizeAsync(MinimalRequest(BillingDocumentTypeARCAEnum.FA)));

        validatorMock.Verify(
            v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task Unsupported_type_message_should_include_the_enum_name()
    {
        // EN: Sanity that the error message helps diagnose which type fell through routing.
        // ES: Asegura que el mensaje de error indique cuál tipo cayó al default del routing.
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult());

        var service = CreateService();
        var request = MinimalRequest(BillingDocumentTypeARCAEnum.Remittances);

        try
        {
            await service.AuthorizeAsync(request);
            Assert.Fail("Expected ARCAServiceException");
        }
        catch (ARCAServiceException ex)
        {
            ex.Message.Should().Contain("Unsupported");
            ex.ErrorCode.Should().BeNull("the unsupported-type guard does not have a numeric ARCA error code");
        }
    }

    // ====================================================================
    // EN: Happy path + error branches exercised through the SOAP wrappers.
    // ES: Happy path + ramas de error ejercitadas vía los wrappers SOAP.
    // ====================================================================

    private static ElRoso.ARCA.Domains.Responses.LoginTicketResponse FreshTicket() => new()
    {
        Token = "fake-token",
        Sign = "fake-sign",
        ExpirationTime = DateTime.UtcNow.AddHours(-3).AddHours(6),
    };

    private void SetupValid()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<BillingDocumentNumberingRequest>()))
            .Returns(new ValidationResult());
    }

    private void SetupTokenCacheHit()
    {
        tokenCacheMock
            .Setup(t => t.GetAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());
    }

    [Fact]
    public async Task Domestic_with_CUIT_should_consult_padron_and_return_CAE()
    {
        SetupValid();
        SetupTokenCacheHit();

        padronMock
            .Setup(p => p.GetPersonaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PadronPersonaResult { IsMonotributo = false, ClientName = "Cliente SA" });

        wsfeMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(41);

        wsfeMock
            .Setup(w => w.SolicitarCaeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                            It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfeCaeResult
            {
                IsApproved = true,
                Cae = "75123456789012",
                CaeExpiration = new DateTime(2026, 5, 23),
            });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.FA);
        request.Client = new ClientRequest
        {
            DocumentType = DocumentTypeARCAEnum.CUIT,
            DocumentNumber = 30987654321,
        };

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeTrue();
        response.CAE.Should().Be("75123456789012");
        response.BillingDocumentNumber.Should().Be(42); // lastNumber + 1
        response.BillingDocumentBookExpirationDate.Should().Be(new DateTime(2026, 5, 23));

        // EN: Verify padron WAS called (CUIT client) and condition propagated.
        // ES: Verifica que padron fue llamado (cliente CUIT) y la condición propagó.
        padronMock.Verify(p => p.GetPersonaAsync(
            It.IsAny<string>(), It.IsAny<string>(), 20123456789L, 30987654321L, It.IsAny<CancellationToken>()),
            Times.Once);
        request.Client.ClientName.Should().Be("Cliente SA");
        request.Client.Condition.Should().Be(VATConditionARCAEnum.RESPONSABLE_INSCRIPTO);
    }

    [Fact]
    public async Task Domestic_with_SIN_IDENTIFICAR_should_skip_padron_and_set_consumidor_final()
    {
        SetupValid();
        SetupTokenCacheHit();

        wsfeMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        wsfeMock
            .Setup(w => w.SolicitarCaeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                            It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfeCaeResult
            {
                IsApproved = true,
                Cae = "75100000000000",
                CaeExpiration = new DateTime(2026, 6, 1),
            });

        // EN: Default client in MinimalRequest is SIN_IDENTIFICAR.
        // ES: El cliente default de MinimalRequest es SIN_IDENTIFICAR.
        var request = MinimalRequest(BillingDocumentTypeARCAEnum.FB);

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeTrue();
        response.BillingDocumentNumber.Should().Be(1);

        padronMock.Verify(p => p.GetPersonaAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never, "padron must NOT be consulted for non-CUIT clients");
        request.Client.Condition.Should().Be(VATConditionARCAEnum.CONSUMIDOR_FINAL);
    }

    [Fact]
    public async Task Domestic_with_CUIT_Monotributo_should_set_MONOTRIBUTO_condition()
    {
        SetupValid();
        SetupTokenCacheHit();

        padronMock
            .Setup(p => p.GetPersonaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PadronPersonaResult { IsMonotributo = true, ClientName = "Juan Pérez" });

        wsfeMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        wsfeMock
            .Setup(w => w.SolicitarCaeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                            It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfeCaeResult { IsApproved = true, Cae = "75", CaeExpiration = DateTime.Today });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.FB);
        request.Client = new ClientRequest { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 20111111111 };

        var service = CreateService();
        await service.AuthorizeAsync(request);

        request.Client.Condition.Should().Be(VATConditionARCAEnum.MONOTRIBUTO);
        request.Client.ClientName.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task Domestic_with_CUIT_should_return_errors_when_padron_fails()
    {
        SetupValid();
        SetupTokenCacheHit();

        padronMock
            .Setup(p => p.GetPersonaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PadronPersonaResult
            {
                Errors = ["No existe persona con ese ID - CUIT: 30987654321"],
            });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.FA);
        request.Client = new ClientRequest { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 30987654321 };

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeFalse();
        response.Errors.Should().ContainSingle(e => e.Contains("No existe persona"));
        // EN: WSFE must NOT be called when padron fails.
        // ES: WSFE NO debe ser llamado cuando padron falla.
        wsfeMock.Verify(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                                  It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                        Times.Never);
    }

    [Fact]
    public async Task Domestic_should_return_errors_when_WSFE_rejects()
    {
        SetupValid();
        SetupTokenCacheHit();

        wsfeMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        wsfeMock
            .Setup(w => w.SolicitarCaeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                            It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfeCaeResult
            {
                IsApproved = false,
                Errors = ["10063: CAE ya autorizado para este comprobante"],
            });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.FC);

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeFalse();
        response.CAE.Should().BeNull();
        response.Errors.Should().ContainSingle(e => e.Contains("10063"));
    }

    [Fact]
    public async Task Export_should_call_wsfex_and_return_CAE()
    {
        SetupValid();
        SetupTokenCacheHit();

        wsfexMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<short>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);

        wsfexMock
            .Setup(w => w.AuthorizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                         It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfexCaeResult
            {
                IsApproved = true,
                Cae = "75999999999999",
                DocumentNumber = 101,
                CaeExpiration = new DateTime(2026, 7, 1),
            });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.InvoiceExport);
        request.BillingDocumentId = 1;
        request.Items = [new ItemRequest { ItemDescription = "Software", Amount = 500 }];
        request.Currency = "Dolares";
        request.Client.ClientLanguage = "Inglés";

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeTrue();
        response.CAE.Should().Be("75999999999999");
        response.BillingDocumentNumber.Should().Be(101);
        response.BillingDocumentBookExpirationDate.Should().Be(new DateTime(2026, 7, 1));

        // EN: WSFE must NOT be called for export documents.
        // ES: WSFE NO debe ser llamado para comprobantes de exportación.
        wsfeMock.VerifyNoOtherCalls();
        padronMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Export_should_return_errors_when_wsfex_rejects()
    {
        SetupValid();
        SetupTokenCacheHit();

        wsfexMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<short>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        wsfexMock
            .Setup(w => w.AuthorizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                         It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfexCaeResult
            {
                IsApproved = false,
                Errors = ["1500: Permission denied"],
            });

        var request = MinimalRequest(BillingDocumentTypeARCAEnum.CreditNoteExport);
        request.BillingDocumentId = 1;
        request.Items = [new ItemRequest { ItemDescription = "Refund", Amount = 100 }];

        var service = CreateService();
        var response = await service.AuthorizeAsync(request);

        response.Result.Should().BeFalse();
        response.Errors.Should().ContainSingle(e => e.Contains("1500"));
    }

    [Fact]
    public async Task Token_cache_miss_should_request_fresh_ticket_from_WSAA()
    {
        SetupValid();

        // EN: Cache returns null → service must call ILoginTicketService to get a fresh ticket.
        // ES: Caché devuelve null → service debe llamar a ILoginTicketService por uno fresco.
        tokenCacheMock
            .Setup(t => t.GetAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElRoso.ARCA.Domains.Responses.LoginTicketResponse?)null);

        loginTicketServiceMock
            .Setup(l => l.GetLoginTicketAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        wsfeMock
            .Setup(w => w.GetLastNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                             It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        wsfeMock
            .Setup(w => w.SolicitarCaeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                            It.IsAny<BillingDocumentNumberingRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WsfeCaeResult { IsApproved = true, Cae = "75", CaeExpiration = DateTime.Today });

        var service = CreateService();
        await service.AuthorizeAsync(MinimalRequest(BillingDocumentTypeARCAEnum.FB));

        loginTicketServiceMock.Verify(l => l.GetLoginTicketAsync(
            "wsfe", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // EN: Fresh ticket must be persisted into the cache.
        // ES: Ticket fresco debe persistirse en el caché.
        tokenCacheMock.Verify(t => t.SetAsync(
            "wsfe", It.IsAny<long>(), It.IsAny<ElRoso.ARCA.Domains.Responses.LoginTicketResponse>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
