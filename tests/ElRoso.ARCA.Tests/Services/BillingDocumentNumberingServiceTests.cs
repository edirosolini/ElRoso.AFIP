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
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElRoso.ARCA.Tests.Services;

public class BillingDocumentNumberingServiceTests
{
    private readonly Mock<ILoginTicketService> loginTicketServiceMock = new();
    private readonly Mock<ITokenCache> tokenCacheMock = new();
    private readonly Mock<IValidator<BillingDocumentNumberingRequest>> validatorMock = new();
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
        validatorMock.Object);

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
}
