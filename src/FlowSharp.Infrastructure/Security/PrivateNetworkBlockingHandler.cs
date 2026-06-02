using System.Net;
using Microsoft.Extensions.Options;

namespace FlowSharp.Infrastructure.Security;

public sealed class PrivateNetworkBlockingHandler(IOptionsMonitor<HttpNodeNetworkOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!options.CurrentValue.ShouldBlockPrivateNetworks)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var uri = request.RequestUri ?? throw new InvalidOperationException("HTTP istegi icin URL gerekli.");
        await EnsurePublicTargetAsync(uri, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private static async Task EnsurePublicTargetAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Public modda yalniz HTTP ve HTTPS hedefleri desteklenir.");
        }

        var host = uri.Host.Trim('[', ']');
        if (IPAddress.TryParse(host, out var literalAddress))
        {
            ThrowIfPrivate(uri, literalAddress);
            return;
        }

        var addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, cancellationToken);
        if (addresses.Length == 0)
        {
            throw new InvalidOperationException($"'{uri.Host}' icin DNS kaydi bulunamadi.");
        }

        foreach (var address in addresses)
        {
            ThrowIfPrivate(uri, address);
        }
    }

    private static void ThrowIfPrivate(Uri uri, IPAddress address)
    {
        if (PrivateNetworkGuard.IsBlocked(address))
        {
            throw new InvalidOperationException($"Public modda private/localhost hedeflerine HTTP istegi engellendi: {uri.Host}");
        }
    }
}
