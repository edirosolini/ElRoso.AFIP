// EN: Tests for InvoiceVerificationService — validation + token cache + WSCDC operations orchestration.
// ES: Tests para InvoiceVerificationService — validación + caché de tokens + orquestación de WSCDC.
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

public class InvoiceVerificationServiceTests
{
    private readonly Mock<ILoginTicketService> loginTicketServiceMock = new();
    private readonly Mock<ITokenCache> tokenCacheMock = new();
    private readonly Mock<IValidator<InvoiceVerificationRequest>> validatorMock = new();
    private readonly Mock<IInvoiceVerificationOperations> operationsMock = new();
    private readonly ARCAOptions options = new()
    {
        IsProduction = false,
        CertificatePath = "/dev/null",
        CertificatePassword = null,
        TokenCacheDirectory = Path.GetTempPath(),
        SoapTimeoutSeconds = 5,
    };

    private InvoiceVerificationService CreateService() => new(
        NullLogger<InvoiceVerificationService>.Instance,
        loginTicketServiceMock.Object,
        tokenCacheMock.Object,
        options,
        validatorMock.Object,
        operationsMock.Object);

    private static InvoiceVerificationRequest ValidRequest() => new()
    {
        BillingDocumentType = BillingDocumentTypeARCAEnum.FA,
        BillingDocumentBookPrefix = 1,
        BillingDocumentNumber = 42,
        BillingDocumentDate = new DateTime(2026, 5, 1),
        TotalAmount = 1210.00,
        AuthorizationCode = "75123456789012",
        IssuingCompany = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 20123456789 },
    };

    private static ElRoso.ARCA.Domains.Responses.LoginTicketResponse FreshTicket() => new()
    {
        Token = "fake-token",
        Sign = "fake-sign",
        ExpirationTime = DateTime.UtcNow.AddHours(-3).AddHours(6),
    };

    [Fact]
    public async Task VerifyAsync_should_throw_ARCAValidationException_when_validator_fails()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult([new ValidationFailure("AuthorizationCode", "required") { ErrorCode = "AUTH001" }]));

        var service = CreateService();

        var act = () => service.VerifyAsync(ValidRequest());

        await act.Should().ThrowAsync<ARCAValidationException>();
        operationsMock.VerifyNoOtherCalls();
        tokenCacheMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyAsync_with_authorized_voucher_returns_IsAuthorized_true()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult());

        tokenCacheMock
            .Setup(t => t.GetAsync("wscdc", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        operationsMock
            .Setup(o => o.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                      It.IsAny<InvoiceVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVerificationOperationResult
            {
                IsAuthorized = true,
                Resultado = "A",
                ProcessedDate = new DateTime(2026, 5, 14),
            });

        var service = CreateService();
        var response = await service.VerifyAsync(ValidRequest());

        response.IsAuthorized.Should().BeTrue();
        response.Resultado.Should().Be("A");
        response.ProcessedDate.Should().Be(new DateTime(2026, 5, 14));
        response.Observations.Should().BeEmpty();
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_with_rejected_voucher_returns_observations()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult());

        tokenCacheMock
            .Setup(t => t.GetAsync("wscdc", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        operationsMock
            .Setup(o => o.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                      It.IsAny<InvoiceVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVerificationOperationResult
            {
                IsAuthorized = false,
                Resultado = "R",
                Observations = ["10048: CUIT del emisor no corresponde al CAE"],
            });

        var service = CreateService();
        var response = await service.VerifyAsync(ValidRequest());

        response.IsAuthorized.Should().BeFalse();
        response.Resultado.Should().Be("R");
        response.Observations.Should().ContainSingle(o => o.Contains("10048"));
    }

    [Fact]
    public async Task VerifyAsync_with_WSCDC_errors_returns_errors_list()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult());

        tokenCacheMock
            .Setup(t => t.GetAsync("wscdc", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        operationsMock
            .Setup(o => o.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                      It.IsAny<InvoiceVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVerificationOperationResult
            {
                Errors = ["600: Token no válido"],
            });

        var service = CreateService();
        var response = await service.VerifyAsync(ValidRequest());

        response.IsAuthorized.Should().BeFalse();
        response.Errors.Should().ContainSingle(e => e.Contains("600"));
    }

    [Fact]
    public async Task VerifyAsync_should_use_token_cache_when_hit()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult());

        tokenCacheMock
            .Setup(t => t.GetAsync("wscdc", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        operationsMock
            .Setup(o => o.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                      It.IsAny<InvoiceVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVerificationOperationResult { IsAuthorized = true, Resultado = "A" });

        var service = CreateService();
        await service.VerifyAsync(ValidRequest());

        // EN: With cache hit, ILoginTicketService must NOT be called.
        // ES: Con cache hit, ILoginTicketService NO debe ser llamado.
        loginTicketServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyAsync_on_cache_miss_should_refresh_via_WSAA()
    {
        validatorMock
            .Setup(v => v.Validate(It.IsAny<InvoiceVerificationRequest>()))
            .Returns(new ValidationResult());

        tokenCacheMock
            .Setup(t => t.GetAsync("wscdc", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElRoso.ARCA.Domains.Responses.LoginTicketResponse?)null);

        loginTicketServiceMock
            .Setup(l => l.GetLoginTicketAsync("wscdc", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FreshTicket());

        operationsMock
            .Setup(o => o.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                                      It.IsAny<InvoiceVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceVerificationOperationResult { IsAuthorized = true, Resultado = "A" });

        var service = CreateService();
        await service.VerifyAsync(ValidRequest());

        loginTicketServiceMock.Verify(l => l.GetLoginTicketAsync(
            "wscdc", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        tokenCacheMock.Verify(t => t.SetAsync(
            "wscdc", It.IsAny<long>(), It.IsAny<ElRoso.ARCA.Domains.Responses.LoginTicketResponse>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
