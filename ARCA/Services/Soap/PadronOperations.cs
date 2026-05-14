// <copyright file="PadronOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services.Soap;

using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;

internal sealed class PadronOperations : IPadronOperations
{
    private readonly ARCAOptions options;

    public PadronOperations(ARCAOptions options)
    {
        this.options = options;
    }

    public async Task<PadronPersonaResult> GetPersonaAsync(
        string sign,
        string token,
        long issuingCuit,
        long clientCuit,
        CancellationToken ct)
    {
        try
        {
            var client = new Padron.PersonaServiceA5Client(
                Padron.PersonaServiceA5Client.EndpointConfiguration.PersonaServiceA5Port,
                options.PadronUrl);
            client.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);

            var resp = await client.getPersonaAsync(new Padron.getPersona
            {
                sign = sign,
                token = token,
                cuitRepresentada = issuingCuit,
                idPersona = clientCuit,
            });

            if (resp.personaReturn.errorConstancia != null)
            {
                var errors = resp.personaReturn.errorConstancia.error
                    .Select(e => $"{e} - CUIT: {clientCuit}")
                    .ToList();
                return new PadronPersonaResult { Errors = errors };
            }

            var datos = resp.personaReturn.datosGenerales;
            var isMonotributo = resp.personaReturn.datosMonotributo != null;
            var name = datos.tipoPersona == "FISICA"
                ? $"{datos.apellido} {datos.nombre}"
                : datos.razonSocial;

            return new PadronPersonaResult
            {
                IsMonotributo = isMonotributo,
                ClientName = name,
            };
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA Padron A5 getPersona failed.", ex);
        }
    }
}
