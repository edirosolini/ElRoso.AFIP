// EN: Tests for ARCAValidationException - thrown when FluentValidation fails locally.
// ES: Tests para ARCAValidationException - se lanza cuando FluentValidation falla local.
using ElRoso.ARCA.Exceptions;

namespace ElRoso.ARCA.Tests.Exceptions;

public class ARCAValidationExceptionTests
{
    [Fact]
    public void Constructor_with_errors_should_expose_them_as_readonly_list()
    {
        var errors = new[] { "CUIT is required", "Amount must be > 0" };

        var exception = new ARCAValidationException(errors);

        exception.Errors.Should().BeEquivalentTo(errors);
        exception.Errors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void Constructor_should_set_default_message()
    {
        var exception = new ARCAValidationException(["any error"]);

        exception.Message.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public void Constructor_with_empty_errors_should_succeed()
    {
        var exception = new ARCAValidationException([]);

        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_should_materialize_lazy_enumerable()
    {
        // EN: Verifies that internal IEnumerable is materialized (no deferred execution surprises).
        // ES: Verifica que el IEnumerable interno se materialice (no hay sorpresas de ejecución diferida).
        var callCount = 0;
        IEnumerable<string> LazyErrors()
        {
            callCount++;
            yield return "lazy error";
        }

        var exception = new ARCAValidationException(LazyErrors());

        _ = exception.Errors.Count; // first read
        _ = exception.Errors.Count; // second read
        callCount.Should().Be(1, "enumerable must be materialized once at construction time");
    }
}
