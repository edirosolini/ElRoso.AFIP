// EN: Tests for BillingDocumentNumberingResponse — FromRequest factory and QRCode generation.
// ES: Tests para BillingDocumentNumberingResponse — factory FromRequest y generación del QR.
using System.Text;
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;
using Newtonsoft.Json.Linq;

namespace ElRoso.ARCA.Tests.Domains.Responses;

public class BillingDocumentNumberingResponseTests
{
    private static BillingDocumentNumberingRequest SampleRequest() => new()
    {
        BillingDocumentType       = BillingDocumentTypeARCAEnum.FA,
        BillingDocumentBookPrefix = 3,
        BillingDocumentDate       = new DateTime(2026, 5, 13),
        Currency                  = "Pesos",
        ExchangeRate              = 1,
        ConceptType               = ConceptTypeARCAEnum.Products,
        AmountTax                 = 1210.00,
        Version                   = 1,
        IssuingCompany = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 20123456789 },
        Client         = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 30987654321 },
    };

    [Fact]
    public void Default_Errors_should_be_empty_list()
    {
        var response = new BillingDocumentNumberingResponse();

        response.Errors.Should().BeEmpty();
        response.Result.Should().BeFalse();
        response.Version.Should().Be(1);
    }

    [Fact]
    public void FromRequest_should_copy_echo_fields()
    {
        var request = SampleRequest();

        var response = BillingDocumentNumberingResponse.FromRequest(request);

        response.BillingDocumentType.Should().Be(request.BillingDocumentType);
        response.BillingDocumentDate.Should().Be(request.BillingDocumentDate);
        response.BillingDocumentBookPrefix.Should().Be(request.BillingDocumentBookPrefix);
        response.Currency.Should().Be(request.Currency);
        response.ExchangeRate.Should().Be(request.ExchangeRate);
        response.Total.Should().Be(request.Total);
        response.Version.Should().Be(request.Version);
        response.IssuingCompany.Should().BeSameAs(request.IssuingCompany);
        response.Client.Should().BeSameAs(request.Client);
    }

    [Fact]
    public void QRCode_should_return_null_when_CAE_is_null()
    {
        var response = BillingDocumentNumberingResponse.FromRequest(SampleRequest());
        response.CAE = null;

        response.QRCode().Should().BeNull();
    }

    [Fact]
    public void QRCode_should_return_null_when_CAE_is_empty()
    {
        var response = BillingDocumentNumberingResponse.FromRequest(SampleRequest());
        response.CAE = string.Empty;

        response.QRCode().Should().BeNull();
    }

    [Fact]
    public void QRCode_should_return_null_when_IssuingCompany_is_null()
    {
        var response = BillingDocumentNumberingResponse.FromRequest(SampleRequest());
        response.CAE = "75123456789012";
        response.IssuingCompany = null;

        response.QRCode().Should().BeNull();
    }

    [Fact]
    public void QRCode_should_return_null_when_Client_is_null()
    {
        var response = BillingDocumentNumberingResponse.FromRequest(SampleRequest());
        response.CAE = "75123456789012";
        response.Client = null;

        response.QRCode().Should().BeNull();
    }

    [Fact]
    public void QRCode_should_return_url_starting_with_AFIP_base()
    {
        var response = BillingDocumentNumberingResponse.FromRequest(SampleRequest());
        response.CAE = "75123456789012";
        response.BillingDocumentNumber = 42;

        var url = response.QRCode();

        url.Should().StartWith("https://www.afip.gob.ar/fe/qr/?p=");
    }

    [Fact]
    public void QRCode_payload_should_decode_to_expected_AFIP_schema()
    {
        // EN: Decode the base64 payload and verify every field in the AFIP schema.
        // ES: Decodifica el base64 y verifica cada campo del schema AFIP.
        var request = SampleRequest();
        var response = BillingDocumentNumberingResponse.FromRequest(request);
        response.CAE = "75123456789012";
        response.BillingDocumentNumber = 42;

        var url = response.QRCode()!;
        var base64 = url["https://www.afip.gob.ar/fe/qr/?p=".Length..];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        var payload = JObject.Parse(json);

        payload["ver"]!.Value<int>().Should().Be(1);
        payload["fecha"]!.Value<string>().Should().Be("2026-05-13");
        payload["cuit"]!.Value<long>().Should().Be(20123456789);
        payload["ptoVta"]!.Value<int>().Should().Be(3);
        payload["tipoCmp"]!.Value<int>().Should().Be((int)BillingDocumentTypeARCAEnum.FA);
        payload["nroCmp"]!.Value<int>().Should().Be(42);
        payload["importe"]!.Value<double>().Should().Be(request.Total);
        payload["moneda"]!.Value<string>().Should().Be("Pesos");
        payload["ctz"]!.Value<double>().Should().Be(1);
        payload["tipoDocRec"]!.Value<int>().Should().Be((int)DocumentTypeARCAEnum.CUIT);
        payload["nroDocRec"]!.Value<long>().Should().Be(30987654321);
        payload["tipoCodAut"]!.Value<string>().Should().Be("E");
        payload["codAut"]!.Value<long>().Should().Be(75123456789012);
    }

    [Fact]
    public void QRCode_should_format_date_as_ISO_yyyy_MM_dd()
    {
        var request = SampleRequest();
        request.BillingDocumentDate = new DateTime(2026, 3, 5);
        var response = BillingDocumentNumberingResponse.FromRequest(request);
        response.CAE = "75123456789012";

        var url = response.QRCode()!;
        var base64 = url["https://www.afip.gob.ar/fe/qr/?p=".Length..];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        json.Should().Contain("\"fecha\":\"2026-03-05\"");
    }

    [Fact]
    public void QRCode_should_use_export_currency_and_exchange_rate()
    {
        // EN: Export invoices use foreign currency and an exchange rate.
        // ES: Las facturas de exportación usan moneda extranjera y cotización.
        var request = SampleRequest();
        request.BillingDocumentType = BillingDocumentTypeARCAEnum.InvoiceExport;
        request.BillingDocumentId = 1;
        request.Items = [new() { ItemDescription = "Software", Amount = 1000 }];
        request.Currency = "Dolares";
        request.ExchangeRate = 1050.50;
        request.AmountTax = 1000;

        var response = BillingDocumentNumberingResponse.FromRequest(request);
        response.CAE = "75999999999999";

        var url = response.QRCode()!;
        var base64 = url["https://www.afip.gob.ar/fe/qr/?p=".Length..];
        var payload = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));

        payload["moneda"]!.Value<string>().Should().Be("Dolares");
        payload["ctz"]!.Value<double>().Should().Be(1050.50);
        payload["tipoCmp"]!.Value<int>().Should().Be((int)BillingDocumentTypeARCAEnum.InvoiceExport);
    }
}
