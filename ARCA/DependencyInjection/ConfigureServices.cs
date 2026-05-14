// <copyright file="ConfigureServices.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.DependencyInjection;

using ElRoso.ARCA.Caching;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Services;
using ElRoso.ARCA.Options;
using ElRoso.ARCA.Services;
using ElRoso.ARCA.Services.Soap;
using ElRoso.ARCA.Validations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class ARCAServiceCollectionExtensions
{
    /// <summary>
    /// Registers all ARCA (ex-AFIP) services into the DI container.
    /// Registra todos los servicios de ARCA (ex-AFIP) en el contenedor de DI.
    /// </summary>
    /// <example>
    /// builder.Services.AddARCAClient(options =>
    /// {
    ///     options.IsProduction        = false;
    ///     options.CertificatePath     = "/certs/empresa.pfx";
    ///     options.CertificatePassword = "miPassword";
    ///     options.TokenCacheDirectory = "/tmp/ARCA-tokens";
    /// });
    /// </example>
    public static IServiceCollection AddARCAClient(
        this IServiceCollection services,
        Action<ARCAOptions> configure)
    {
        var options = new ARCAOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<ITokenCache, FileTokenCache>(sp =>
            new FileTokenCache(
                options.TokenCacheDirectory,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileTokenCache>>()));

        services.AddSingleton<ILoginTicketService, LoginTicketService>();

        // SOAP operation wrappers — separated so consumer services are unit-testable.
        // Wrappers de operaciones SOAP — separados para que los services consumidores sean testeables.
        services.AddSingleton<IPadronOperations, PadronOperations>();
        services.AddSingleton<IWsfeOperations, WsfeOperations>();
        services.AddSingleton<IWsfexOperations, WsfexOperations>();
        services.AddSingleton<IInvoiceVerificationOperations, InvoiceVerificationOperations>();
        services.AddSingleton<IElectronicMailboxOperations, ElectronicMailboxOperations>();

        services.AddSingleton<IBillingDocumentNumberingService, BillingDocumentNumberingService>();
        services.AddSingleton<IInvoiceVerificationService, InvoiceVerificationService>();
        services.AddSingleton<IElectronicMailboxService, ElectronicMailboxService>();

        // Singleton: validators are stateless and safe to reuse.
        // Singleton: los validadores son stateless y se pueden reusar.
        services.AddSingleton<IValidator<BillingDocumentNumberingRequest>, BillingDocumentNumberingValidator>();
        services.AddSingleton<IValidator<InvoiceVerificationRequest>, InvoiceVerificationValidator>();

        return services;
    }
}
