// EN: Expose internals to the test assembly so we can unit-test internal sealed services.
// ES: Exponer internals al ensamblado de tests para poder testear los services internal sealed.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ElRoso.ARCA.Tests")]
