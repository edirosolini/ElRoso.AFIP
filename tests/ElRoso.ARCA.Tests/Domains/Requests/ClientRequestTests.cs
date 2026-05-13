// EN: Tests for ClientRequest - read-only Condition/ClientName mutated via internal setters.
// ES: Tests para ClientRequest - Condition/ClientName son read-only y mutan via setters internos.
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;

namespace ElRoso.ARCA.Tests.Domains.Requests;

public class ClientRequestTests
{
    [Fact]
    public void Defaults_should_have_empty_strings_and_zero_values()
    {
        var client = new ClientRequest();

        client.ClientName.Should().BeEmpty();
        client.Address.Should().BeEmpty();
        client.ClientLanguage.Should().BeEmpty();
        client.DocumentNumber.Should().Be(0);
        client.CountryId.Should().Be(0);
    }

    [Fact]
    public void SetCondition_should_update_readonly_property()
    {
        var client = new ClientRequest();

        client.SetCondition(VATConditionARCAEnum.CONSUMIDOR_FINAL);

        client.Condition.Should().Be(VATConditionARCAEnum.CONSUMIDOR_FINAL);
    }

    [Fact]
    public void SetClientName_should_update_readonly_property()
    {
        var client = new ClientRequest();

        client.SetClientName("Juan Pérez SA");

        client.ClientName.Should().Be("Juan Pérez SA");
    }
}
