// <copyright file="ElectronicMailboxOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services.Soap;

using System.Globalization;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;
using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;

internal sealed class ElectronicMailboxOperations : IElectronicMailboxOperations
{
    private readonly ARCAOptions options;

    public ElectronicMailboxOperations(ARCAOptions options)
    {
        this.options = options;
    }

    public async Task<MailboxQueryResponse> ListAsync(
        string sign,
        string token,
        long cuitRepresentada,
        MailboxQueryRequest request,
        CancellationToken ct)
    {
        try
        {
            var client = CreateClient();

            var auth = new WSCComu.AuthRequest
            {
                token = token,
                sign = sign,
                cuitRepresentada = cuitRepresentada,
            };

            var filter = BuildFilter(request);
            var result = await client.consultarComunicacionesAsync(auth, filter);
            var paginada = result.RespuestaPaginada;

            if (paginada is null)
                return new MailboxQueryResponse();

            return new MailboxQueryResponse
            {
                Page = paginada.pagina,
                TotalPages = paginada.totalPaginas,
                ItemsPerPage = paginada.itemsPorPagina,
                TotalItems = paginada.totalItems,
                Messages = [.. (paginada.items ?? []).Select(MapSummary)],
            };
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSCComu consultarComunicaciones failed.", ex);
        }
    }

    public async Task<MailboxConsumeResponse> ConsumeAsync(
        string sign,
        string token,
        long cuitRepresentada,
        long messageId,
        CancellationToken ct)
    {
        try
        {
            var client = CreateClient();

            var auth = new WSCComu.AuthRequest
            {
                token = token,
                sign = sign,
                cuitRepresentada = cuitRepresentada,
            };

            // EN: incluirAdjuntos=true so the consumer can render the full message in one call.
            // ES: incluirAdjuntos=true para que el consumidor pueda renderizar todo en una llamada.
            var result = await client.consumirComunicacionAsync(auth, messageId, true);
            var comunicacion = result.Comunicacion;

            if (comunicacion is null)
            {
                return new MailboxConsumeResponse
                {
                    Success = false,
                    Errors = ["WSCComu returned no message for the given id."],
                };
            }

            return new MailboxConsumeResponse
            {
                Success = true,
                Message = MapMessage(comunicacion),
            };
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSCComu consumirComunicacion failed.", ex);
        }
    }

    private WSCComu.VEConsumerClient CreateClient()
    {
        var client = new WSCComu.VEConsumerClient(
            WSCComu.VEConsumerClient.EndpointConfiguration.VEConsumerPort,
            options.WsccomuUrl);
        client.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);
        return client;
    }

    private static WSCComu.Filter BuildFilter(MailboxQueryRequest req)
    {
        var f = new WSCComu.Filter
        {
            pagina = req.Page,
            fechaDesde = req.FromDate?.ToString("yyyy-MM-dd"),
            fechaHasta = req.ToDate?.ToString("yyyy-MM-dd"),
            referencia1 = req.Reference1,
            referencia2 = req.Reference2,
        };

        if (req.StateId.HasValue) { f.estado = req.StateId.Value; f.estadoSpecified = true; }
        if (req.HasAttachment.HasValue) { f.tieneAdjunto = req.HasAttachment.Value; f.tieneAdjuntoSpecified = true; }
        if (req.PublishingSystemId.HasValue) { f.sistemaPublicadorId = req.PublishingSystemId.Value; f.sistemaPublicadorIdSpecified = true; }
        if (req.PageSize.HasValue) { f.resultadosPorPagina = req.PageSize.Value; f.resultadosPorPaginaSpecified = true; }
        return f;
    }

    private static MailboxMessageSummary MapSummary(WSCComu.ComunicacionSimplificada s) => new()
    {
        Id = s.idComunicacion,
        RecipientCuit = s.cuitDestinatario,
        PublishedDate = TryParseDate(s.fechaPublicacion),
        ExpirationDate = TryParseDate(s.fechaVencimiento),
        PublishingSystemId = s.sistemaPublicador,
        PublishingSystem = s.sistemaPublicadorDesc ?? string.Empty,
        StateId = s.estado,
        StateName = s.estadoDesc ?? string.Empty,
        Subject = s.asunto ?? string.Empty,
        Priority = s.prioridad,
        HasAttachment = s.tieneAdjunto,
        Reference1 = s.referencia1,
        Reference2 = s.referencia2,
    };

    private static MailboxMessage MapMessage(WSCComu.Comunicacion c) => new()
    {
        Id = c.idComunicacion,
        RecipientCuit = c.cuitDestinatario,
        PublishedDate = TryParseDate(c.fechaPublicacion),
        ExpirationDate = TryParseDate(c.fechaVencimiento),
        PublishingSystem = c.sistemaPublicadorDesc ?? string.Empty,
        StateId = c.estado,
        StateName = c.estadoDesc ?? string.Empty,
        Subject = c.asunto ?? string.Empty,
        Body = c.mensaje ?? string.Empty,
        Priority = c.prioridad,
        HasAttachment = c.tieneAdjunto,
        Attachments = [.. (c.adjuntos ?? []).Select(MapAttachment)],
    };

    private static MailboxAttachment MapAttachment(WSCComu.adjunto a) => new()
    {
        FileName = a.filename ?? string.Empty,
        // EN: WSCComu doesn't return MIME type — caller can infer from filename extension.
        // ES: WSCComu no devuelve MIME type — el caller lo puede inferir de la extensión.
        MimeType = string.Empty,
        Content = a.content ?? [],
    };

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // EN: Try several common ARCA date formats.
        // ES: Probar varios formatos comunes de fecha de ARCA.
        string[] formats = ["yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy"];
        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2) ? dt2 : null;
    }
}
