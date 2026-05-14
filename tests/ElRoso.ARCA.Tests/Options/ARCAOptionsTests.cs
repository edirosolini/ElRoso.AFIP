// EN: Tests for ARCAOptions - defaults and environment URL resolution.
// ES: Tests para ARCAOptions - defaults y resolución de URLs por ambiente.
using ElRoso.ARCA.Core;

namespace ElRoso.ARCA.Tests.Options;

public class ARCAOptionsTests
{
    [Fact]
    public void Defaults_should_target_homologation_and_safe_values()
    {
        var options = new ARCAOptions();

        options.IsProduction.Should().BeFalse("homologation must be the safe default — production requires explicit opt-in");
        options.SoapTimeoutSeconds.Should().Be(30);
        options.CertificatePath.Should().BeEmpty();
        options.CertificatePassword.Should().BeNull();
        options.TokenCacheDirectory.Should().Be(Path.GetTempPath());
    }

    [Theory]
    [InlineData(false, "wsaahomo.afip.gov.ar")]
    [InlineData(true, "wsaa.afip.gov.ar")]
    public void WsaaUrl_should_switch_by_environment(bool isProduction, string expectedHost)
    {
        var options = new ARCAOptions { IsProduction = isProduction };

        var url = typeof(ARCAOptions)
            .GetProperty("WsaaUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(options) as string;

        url.Should().Contain(expectedHost);
    }

    [Theory]
    [InlineData(false, "wswhomo.afip.gov.ar")]
    [InlineData(true, "servicios1.afip.gov.ar")]
    public void WsfeUrl_should_switch_by_environment(bool isProduction, string expectedHost)
    {
        var options = new ARCAOptions { IsProduction = isProduction };

        var url = typeof(ARCAOptions)
            .GetProperty("WsfeUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(options) as string;

        url.Should().Contain(expectedHost);
    }

    [Theory]
    [InlineData(false, "wswhomo.afip.gov.ar")]
    [InlineData(true, "servicios1.afip.gov.ar")]
    public void WsfexUrl_should_switch_by_environment(bool isProduction, string expectedHost)
    {
        var options = new ARCAOptions { IsProduction = isProduction };

        var url = typeof(ARCAOptions)
            .GetProperty("WsfexUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(options) as string;

        url.Should().Contain(expectedHost);
    }

    [Theory]
    [InlineData(false, "awshomo.afip.gov.ar")]
    [InlineData(true, "aws.afip.gov.ar")]
    public void PadronUrl_should_switch_by_environment(bool isProduction, string expectedHost)
    {
        var options = new ARCAOptions { IsProduction = isProduction };

        var url = typeof(ARCAOptions)
            .GetProperty("PadronUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(options) as string;

        url.Should().Contain(expectedHost);
    }
}
