// EN: Trivial tests for small DTOs/requests to keep coverage at production threshold.
// ES: Tests triviales para DTOs/requests pequeños para mantener cobertura en threshold de producción.
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;

namespace ElRoso.ARCA.Tests.Domains.Requests;

public class SimpleDtosTests
{
    [Fact]
    public void LoginTicketRequest_should_have_well_formed_XmlTemplate()
    {
        var request = new LoginTicketRequest();

        request.XmlTemplate.Should().Contain("<loginTicketRequest>");
        request.XmlTemplate.Should().Contain("<header>");
        request.XmlTemplate.Should().Contain("<uniqueId>");
        request.XmlTemplate.Should().Contain("<generationTime>");
        request.XmlTemplate.Should().Contain("<expirationTime>");
        request.XmlTemplate.Should().Contain("<service>");
    }

    [Fact]
    public void LoginTicketRequest_Service_should_default_to_empty_and_be_settable()
    {
        var request = new LoginTicketRequest();

        request.Service.Should().BeEmpty();

        request.Service = "wsfe";
        request.Service.Should().Be("wsfe");
    }

    [Fact]
    public void IssuingCompanyRequest_should_default_to_zero_and_be_settable()
    {
        var company = new IssuingCompanyRequest();

        company.DocumentNumber.Should().Be(0);

        company.DocumentType = DocumentTypeARCAEnum.CUIT;
        company.DocumentNumber = 20123456789;
        company.VATCondition = VATConditionARCAEnum.RESPONSABLE_INSCRIPTO;

        company.DocumentType.Should().Be(DocumentTypeARCAEnum.CUIT);
        company.DocumentNumber.Should().Be(20123456789);
        company.VATCondition.Should().Be(VATConditionARCAEnum.RESPONSABLE_INSCRIPTO);
    }

    [Fact]
    public void BillingDocumentNumberingAssociatedRequest_should_hold_all_fields()
    {
        var associated = new BillingDocumentNumberingAssociatedRequest
        {
            BillingDocumentBookPrefix = 1,
            BillingDocumentDate = new DateTime(2026, 1, 15),
            BillingDocumentNumber = 42,
            BillingDocumentType = BillingDocumentTypeARCAEnum.FA,
        };

        associated.BillingDocumentBookPrefix.Should().Be(1);
        associated.BillingDocumentDate.Should().Be(new DateTime(2026, 1, 15));
        associated.BillingDocumentNumber.Should().Be(42);
        associated.BillingDocumentType.Should().Be(BillingDocumentTypeARCAEnum.FA);
    }

    [Fact]
    public void ItemRequest_should_hold_description_and_amount()
    {
        var item = new ItemRequest
        {
            ItemDescription = "Software license",
            Amount = 1000.50,
        };

        item.ItemDescription.Should().Be("Software license");
        item.Amount.Should().Be(1000.50);
    }

    [Fact]
    public void BillingDocumentNumberingOtherTaxRequest_should_hold_all_fields()
    {
        var tax = new BillingDocumentNumberingOtherTaxRequest
        {
            Description = "Impuestos internos",
            BaseAmount = 100,
            PercentageTax = 3,
            Amount = 3,
        };

        tax.Description.Should().Be("Impuestos internos");
        tax.BaseAmount.Should().Be(100);
        tax.PercentageTax.Should().Be(3);
        tax.Amount.Should().Be(3);
    }
}
