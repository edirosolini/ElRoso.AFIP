// EN: Tests for FileTokenCache — encrypted file-based ticket cache.
// ES: Tests para FileTokenCache — caché de tickets en archivos cifrados.
using ElRoso.ARCA.Caching;
using ElRoso.ARCA.Domains.Responses;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElRoso.ARCA.Tests.Caching;

public class FileTokenCacheTests : IDisposable
{
    private readonly string tempDir;
    private readonly FileTokenCache cache;

    public FileTokenCacheTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "ElRoso.ARCA.Tests", Guid.NewGuid().ToString("N"));
        cache = new FileTokenCache(tempDir, NullLogger<FileTokenCache>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    private static LoginTicketResponse FreshTicket(DateTime? expirationArLocal = null) => new()
    {
        // EN: Default expiration = 6h ahead in Argentina local time (UTC-3).
        // ES: Expiración default = 6h adelante en hora local Argentina (UTC-3).
        ExpirationTime = expirationArLocal ?? DateTime.UtcNow.AddHours(-3).AddHours(6),
        Sign = "fake-sign-value",
        Token = "fake-token-value",
    };

    [Fact]
    public void Constructor_should_create_cache_directory_if_missing()
    {
        var newDir = Path.Combine(Path.GetTempPath(), "ElRoso.ARCA.Tests", Guid.NewGuid().ToString("N"));
        Directory.Exists(newDir).Should().BeFalse();

        _ = new FileTokenCache(newDir, NullLogger<FileTokenCache>.Instance);

        Directory.Exists(newDir).Should().BeTrue();
        Directory.Delete(newDir, recursive: true);
    }

    [Fact]
    public async Task GetAsync_should_return_null_when_no_file_exists()
    {
        var result = await cache.GetAsync("wsfe", 20123456789);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_then_GetAsync_should_roundtrip_fresh_ticket()
    {
        var ticket = FreshTicket();

        await cache.SetAsync("wsfe", 20123456789, ticket);
        var retrieved = await cache.GetAsync("wsfe", 20123456789);

        retrieved.Should().NotBeNull();
        retrieved!.Token.Should().Be(ticket.Token);
        retrieved.Sign.Should().Be(ticket.Sign);
        retrieved.ExpirationTime.Should().BeCloseTo(ticket.ExpirationTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetAsync_should_write_file_with_expected_name()
    {
        await cache.SetAsync("wsfe", 20123456789, FreshTicket());

        var expected = Path.Combine(tempDir, "ARCA_token_wsfe_20123456789.bin");
        File.Exists(expected).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_should_return_null_for_expired_token()
    {
        // EN: ExpirationTime in the past — must be treated as miss (refresh required).
        // ES: ExpirationTime en el pasado — debe tratarse como miss (hace falta refresh).
        var pastAr = DateTime.UtcNow.AddHours(-3).AddHours(-1); // 1h ago in AR time
        var expired = FreshTicket(pastAr);

        await cache.SetAsync("wsfe", 20123456789, expired);
        var result = await cache.GetAsync("wsfe", 20123456789);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_should_apply_expiration_buffer()
    {
        // EN: ExpirationBuffer = 2 min. Tickets expiring within that window must be treated as expired.
        // ES: ExpirationBuffer = 2 min. Tickets que expiran dentro de esa ventana son tratados como expirados.
        var almostExpiredAr = DateTime.UtcNow.AddHours(-3).AddSeconds(30); // expires in 30s — within buffer
        var ticket = FreshTicket(almostExpiredAr);

        await cache.SetAsync("wsfe", 20123456789, ticket);
        var result = await cache.GetAsync("wsfe", 20123456789);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Different_service_and_company_should_have_isolated_caches()
    {
        var wsfeTicket = FreshTicket();
        wsfeTicket.Token = "wsfe-token";
        var padronTicket = FreshTicket();
        padronTicket.Token = "padron-token";

        await cache.SetAsync("wsfe", 20111111111, wsfeTicket);
        await cache.SetAsync("ws_sr_constancia_inscripcion", 20111111111, padronTicket);
        await cache.SetAsync("wsfe", 30222222222, wsfeTicket);  // different company

        var wsfe1 = await cache.GetAsync("wsfe", 20111111111);
        var padron1 = await cache.GetAsync("ws_sr_constancia_inscripcion", 20111111111);
        var wsfe2 = await cache.GetAsync("wsfe", 30222222222);

        wsfe1!.Token.Should().Be("wsfe-token");
        padron1!.Token.Should().Be("padron-token");
        wsfe2.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_should_overwrite_existing_ticket()
    {
        var first = FreshTicket();
        first.Token = "first-token";
        var second = FreshTicket();
        second.Token = "second-token";

        await cache.SetAsync("wsfe", 20123456789, first);
        await cache.SetAsync("wsfe", 20123456789, second);
        var result = await cache.GetAsync("wsfe", 20123456789);

        result!.Token.Should().Be("second-token");
    }

    [Fact]
    public async Task Concurrent_reads_on_same_key_should_not_throw()
    {
        // EN: SemaphoreSlim per-key must serialize without deadlock / corruption.
        // ES: SemaphoreSlim por clave debe serializar sin deadlock / corrupción.
        await cache.SetAsync("wsfe", 20123456789, FreshTicket());

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => cache.GetAsync("wsfe", 20123456789))
            .ToArray();

        await Task.WhenAll(tasks);

        tasks.All(t => t.Result is not null).Should().BeTrue();
    }
}
