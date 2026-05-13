// EN: Tests for computed properties on BillingDocumentNumberingRequest (Total, sums, rounding).
// ES: Tests para propiedades computadas de BillingDocumentNumberingRequest (Total, sumas, redondeo).
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;

namespace ElRoso.ARCA.Tests.Domains.Requests;

public class BillingDocumentNumberingRequestTests
{
    [Fact]
    public void AmountTax_should_round_to_two_decimals()
    {
        var request = new BillingDocumentNumberingRequest { AmountTax = 100.567 };

        request.AmountTax.Should().Be(100.57);
    }

    [Fact]
    public void AmountNotTax_should_round_to_two_decimals()
    {
        var request = new BillingDocumentNumberingRequest { AmountNotTax = 50.444 };

        request.AmountNotTax.Should().Be(50.44);
    }

    [Fact]
    public void ExchangeRate_should_round_to_two_decimals()
    {
        var request = new BillingDocumentNumberingRequest { ExchangeRate = 1050.999 };

        request.ExchangeRate.Should().Be(1051.00);
    }

    [Fact]
    public void TaxAmount_should_sum_all_tax_lines()
    {
        var request = new BillingDocumentNumberingRequest
        {
            BillingDocumentNumberingTaxes =
            [
                new() { PercentageTax = "21.00", BaseAmount = 100, Amount = 21 },
                new() { PercentageTax = "10.50", BaseAmount = 100, Amount = 10.5 },
            ],
        };

        request.BillingDocumentNumberingTaxAmount.Should().Be(31.50);
    }

    [Fact]
    public void TaxAmount_should_be_zero_when_taxes_are_null()
    {
        var request = new BillingDocumentNumberingRequest();

        request.BillingDocumentNumberingTaxAmount.Should().Be(0);
    }

    [Fact]
    public void OtherTaxAmount_should_sum_all_other_tax_lines()
    {
        var request = new BillingDocumentNumberingRequest
        {
            BillingDocumentNumberingOtherTaxes =
            [
                new() { Description = "Otros", BaseAmount = 100, PercentageTax = 3, Amount = 3 },
                new() { Description = "Impuestos internos", BaseAmount = 100, PercentageTax = 5, Amount = 5 },
            ],
        };

        request.BillingDocumentNumberingOtherTaxAmount.Should().Be(8);
    }

    [Fact]
    public void OtherTaxAmount_should_be_zero_when_other_taxes_are_null()
    {
        var request = new BillingDocumentNumberingRequest();

        request.BillingDocumentNumberingOtherTaxAmount.Should().Be(0);
    }

    [Theory]
    [InlineData(BillingDocumentTypeARCAEnum.InvoiceExport)]
    [InlineData(BillingDocumentTypeARCAEnum.DebitNoteExport)]
    [InlineData(BillingDocumentTypeARCAEnum.CreditNoteExport)]
    public void Total_for_export_documents_should_equal_AmountTax_only(BillingDocumentTypeARCAEnum type)
    {
        // EN: Export documents do not break down VAT — Total equals the gross net (AmountTax field).
        // ES: Documentos de exportación no desglosan IVA — Total = AmountTax.
        var request = new BillingDocumentNumberingRequest
        {
            BillingDocumentType = type,
            AmountTax = 1000,
            AmountNotTax = 500,
            BillingDocumentNumberingTaxes = [new() { PercentageTax = "21.00", BaseAmount = 1000, Amount = 210 }],
            BillingDocumentNumberingOtherTaxes = [new() { Description = "Otros", BaseAmount = 100, PercentageTax = 3, Amount = 3 }],
        };

        request.Total.Should().Be(1000);
    }

    [Theory]
    [InlineData(BillingDocumentTypeARCAEnum.FC)]
    [InlineData(BillingDocumentTypeARCAEnum.NDC)]
    [InlineData(BillingDocumentTypeARCAEnum.NCC)]
    public void Total_for_C_documents_should_be_AmountTax_plus_OtherTaxes_no_VAT(BillingDocumentTypeARCAEnum type)
    {
        // EN: Class C documents (Monotributo) do not discriminate VAT — only AmountTax + Other taxes count.
        // ES: Comprobantes clase C (Monotributo) no discriminan IVA — solo AmountTax + Otros impuestos.
        var request = new BillingDocumentNumberingRequest
        {
            BillingDocumentType = type,
            AmountTax = 1000,
            BillingDocumentNumberingTaxes = [new() { PercentageTax = "21.00", BaseAmount = 1000, Amount = 210 }],
            BillingDocumentNumberingOtherTaxes = [new() { Description = "Otros", BaseAmount = 100, PercentageTax = 3, Amount = 3 }],
        };

        request.Total.Should().Be(1003);
    }

    [Fact]
    public void Total_for_FA_should_sum_all_components()
    {
        var request = new BillingDocumentNumberingRequest
        {
            BillingDocumentType = BillingDocumentTypeARCAEnum.FA,
            AmountTax = 1000,
            AmountNotTax = 100,
            BillingDocumentNumberingTaxes = [new() { PercentageTax = "21.00", BaseAmount = 1000, Amount = 210 }],
            BillingDocumentNumberingOtherTaxes = [new() { Description = "Otros", BaseAmount = 100, PercentageTax = 3, Amount = 3 }],
        };

        request.Total.Should().Be(1313);
    }

    [Fact]
    public void Default_Version_should_be_1()
    {
        var request = new BillingDocumentNumberingRequest();

        request.Version.Should().Be(1);
    }
}
