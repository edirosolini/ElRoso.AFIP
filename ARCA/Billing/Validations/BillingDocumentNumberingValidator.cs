// <copyright file="BillingDocumentNumberingValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Billing;

using ElRoso.ARCA.Billing;
using ElRoso.ARCA.Core;
using ElRoso.ARCA.Read;
using FluentValidation;

internal class BillingDocumentNumberingValidator : AbstractValidator<BillingDocumentNumberingRequest>
{
    private static readonly BillingDocumentTypeARCAEnum[] NoteTypes =
    [
        BillingDocumentTypeARCAEnum.NDA, BillingDocumentTypeARCAEnum.NCA,
        BillingDocumentTypeARCAEnum.NDB, BillingDocumentTypeARCAEnum.NCB,
        BillingDocumentTypeARCAEnum.NDC, BillingDocumentTypeARCAEnum.NCC,
    ];

    private static readonly BillingDocumentTypeARCAEnum[] ExportTypes =
    [
        BillingDocumentTypeARCAEnum.InvoiceExport,
        BillingDocumentTypeARCAEnum.DebitNoteExport,
        BillingDocumentTypeARCAEnum.CreditNoteExport,
    ];

    private static readonly BillingDocumentTypeARCAEnum[] VatTypes =
    [
        BillingDocumentTypeARCAEnum.FA, BillingDocumentTypeARCAEnum.NDA, BillingDocumentTypeARCAEnum.NCA,
        BillingDocumentTypeARCAEnum.FB, BillingDocumentTypeARCAEnum.NDB, BillingDocumentTypeARCAEnum.NCB,
    ];

    public BillingDocumentNumberingValidator()
    {
        RuleFor(x => x.BillingDocumentType).IsInEnum();
        RuleFor(x => x.ConceptType).IsInEnum();
        RuleFor(x => x.BillingDocumentBookPrefix).NotEmpty();
        RuleFor(x => x.BillingDocumentDate).NotEmpty();
        RuleFor(x => x.ExchangeRate).NotEmpty();
        RuleFor(x => x.AmountTax).NotEmpty();
        RuleFor(x => x.IssuingCompany).NotNull();
        RuleFor(x => x.Client).NotNull().SetValidator(new ClientValidator());
        RuleFor(x => x.Currency).Must(c => DictionariesCommon.Currencies.ContainsKey(c))
            .WithMessage("'{PropertyValue}' is not a supported currency.");

        // Credit/Debit notes must include linked documents.
        // Las notas de crédito/débito deben incluir comprobantes asociados.
        RuleFor(x => x.BillingDocumentNumberingAssociateds)
            .NotNull().NotEmpty()
            .When(x => NoteTypes.Contains(x.BillingDocumentType))
            .WithMessage("Credit and debit notes must include associated receipts.");

        // Export documents require an ID and at least one item.
        // Los documentos de exportación requieren ID e ítems.
        RuleFor(x => x.BillingDocumentId).NotNull().NotEmpty()
            .When(x => ExportTypes.Contains(x.BillingDocumentType));
        RuleFor(x => x.Items).NotNull().NotEmpty()
            .When(x => ExportTypes.Contains(x.BillingDocumentType));

        // Service concept requires date range and payment due.
        // El concepto Servicios requiere rango de fechas y vencimiento.
        RuleFor(x => x)
            .Must(BeValidServiceDates)
            .When(x => x.ConceptType == ConceptTypeARCAEnum.Services)
            .WithMessage("Service invoices require DateOfServicesFrom, DateOfServicesTo, and PaymentDue.");

        RuleForEach(x => x.BillingDocumentNumberingTaxes)
            .SetValidator(new BillingDocumentNumberingTaxValidator())
            .When(x => VatTypes.Contains(x.BillingDocumentType));

        RuleForEach(x => x.BillingDocumentNumberingOtherTaxes)
            .SetValidator(new BillingDocumentNumberingOtherTaxValidator());
    }

    private static bool BeValidServiceDates(BillingDocumentNumberingRequest x)
    {
        // Export services only need PaymentDue.
        // Exportación de servicios solo requiere PaymentDue.
        if (ExportTypes.Contains(x.BillingDocumentType))
            return x.PaymentDue != null;

        return x.DateOfServicesFrom != null && x.DateOfServicesTo != null && x.PaymentDue != null;
    }
}