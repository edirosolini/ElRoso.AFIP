// EN: Tests for ARCAServiceException - thrown when ARCA web services return errors.
// ES: Tests para ARCAServiceException - error devuelto por los WS de ARCA.
using ElRoso.ARCA.Core;

namespace ElRoso.ARCA.Tests.Exceptions;

public class ARCAServiceExceptionTests
{
    [Fact]
    public void Constructor_with_message_only_should_have_null_error_code()
    {
        var exception = new ARCAServiceException("Service unavailable");

        exception.Message.Should().Be("Service unavailable");
        exception.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Constructor_with_inner_exception_should_preserve_chain()
    {
        var inner = new TimeoutException();

        var exception = new ARCAServiceException("SOAP timeout", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Constructor_with_error_code_and_message_should_format_message_and_keep_code()
    {
        var exception = new ARCAServiceException(1503, "Invalid CUIT");

        exception.ErrorCode.Should().Be(1503);
        exception.Message.Should().Contain("1503");
        exception.Message.Should().Contain("Invalid CUIT");
    }
}
