// EN: Tests for ARCAAuthException - thrown on WSAA / certificate / signing failures.
// ES: Tests para ARCAAuthException - falla de WSAA / certificado / firma.
using ElRoso.ARCA.Exceptions;

namespace ElRoso.ARCA.Tests.Exceptions;

public class ARCAAuthExceptionTests
{
    [Fact]
    public void Constructor_with_message_should_set_message()
    {
        var exception = new ARCAAuthException("Certificate not found");

        exception.Message.Should().Be("Certificate not found");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_with_inner_exception_should_preserve_chain()
    {
        var inner = new InvalidOperationException("PKCS7 signing failed");

        var exception = new ARCAAuthException("WSAA login failed", inner);

        exception.Message.Should().Be("WSAA login failed");
        exception.InnerException.Should().BeSameAs(inner);
    }
}
