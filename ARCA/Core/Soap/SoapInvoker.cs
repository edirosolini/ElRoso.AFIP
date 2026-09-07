// <copyright file="SoapInvoker.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

using System.ServiceModel;

/// <summary>
/// EN: Runs one call against a generated SOAP proxy. The proxies produced by
///     <c>dotnet-svcutil</c> take no <see cref="CancellationToken"/>, so it is bridged here, and
///     the WCF channel is always closed (or aborted) instead of being left to the GC.
/// ES: Ejecuta una llamada contra un proxy SOAP generado. Los proxies que produce
///     <c>dotnet-svcutil</c> no reciben <see cref="CancellationToken"/>, así que se puentea acá, y
///     el canal WCF siempre se cierra (o se aborta) en vez de quedar esperando al GC.
/// </summary>
/// <remarks>
/// EN: ⚠️ Cancelling stops the local wait, not the remote work. ARCA may still authorize a
///     voucher whose caller already walked away — the caller is expected to log the abandoned
///     request so reconciliation knows where to look.
/// ES: ⚠️ Cancelar corta la espera local, no el trabajo remoto. ARCA puede autorizar igual un
///     comprobante cuyo llamador ya se fue — el llamador tiene que dejar traza del pedido
///     abandonado para que la reconciliación sepa dónde mirar.
/// </remarks>
internal static class SoapInvoker
{
    internal static async Task<TResult> InvokeAsync<TResult>(
        ICommunicationObject client,
        Func<Task<TResult>> call,
        CancellationToken ct)
    {
        TResult result;
        Task<TResult>? pending = null;

        try
        {
            pending = call();

            if (ct.CanBeCanceled && !pending.IsCompleted)
            {
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Registration disposed on the way out — no timer is left behind for a call that
                // completes normally.
                // El registro se libera al salir — no queda ningún timer colgado para una llamada
                // que termina bien.
                using (ct.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancelled))
                {
                    if (await Task.WhenAny(pending, cancelled.Task).ConfigureAwait(false) != pending)
                        ct.ThrowIfCancellationRequested();
                }
            }

            result = await pending.ConfigureAwait(false);
        }
        catch
        {
            Abort(client);
            Observe(pending);
            throw;
        }

        Close(client);
        return result;
    }

    /// <summary>
    /// EN: Closing a faulted channel throws. The result is already in hand, so fall back to
    ///     Abort rather than turning a successful call into a failure.
    /// ES: Cerrar un canal en falla tira excepción. El resultado ya está, así que se cae a Abort
    ///     en vez de convertir una llamada exitosa en un error.
    /// </summary>
    private static void Close(ICommunicationObject client)
    {
        try
        {
            client.Close();
        }
        catch
        {
            Abort(client);
        }
    }

    private static void Abort(ICommunicationObject client)
    {
        try
        {
            client.Abort();
        }
        catch
        {
            // Nothing left to do — the channel is being discarded either way.
            // No queda nada por hacer — el canal se descarta igual.
        }
    }

    /// <summary>
    /// EN: The abandoned call still finishes somewhere. Observing it keeps a later fault from
    ///     surfacing as an unobserved task exception.
    /// ES: La llamada abandonada igual termina en algún lado. Observarla evita que una falla
    ///     posterior aparezca como excepción de tarea no observada.
    /// </summary>
    private static void Observe<TResult>(Task<TResult>? pending) =>
        pending?.ContinueWith(
            static t => _ = t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}
