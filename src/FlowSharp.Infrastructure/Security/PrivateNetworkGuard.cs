using System.Net;

namespace FlowSharp.Infrastructure.Security;

/// <summary>
/// Bir IP adresinin private/localhost/reserved (SSRF acisindan riskli) olup olmadigini
/// belirleyen ortak kurallar. Hem <see cref="PrivateNetworkBlockingHandler"/> (erken kontrol)
/// hem de cikis HTTP istemcisinin <c>ConnectCallback</c>'i (gercek baglanti aninda, IP pinleyerek)
/// bu mantigi kullanir; boylece DNS rebinding ve yonlendirme (redirect) ile atlatma engellenir.
/// </summary>
internal static class PrivateNetworkGuard
{
    public static bool IsBlocked(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork => IsBlockedIPv4(bytes),
            System.Net.Sockets.AddressFamily.InterNetworkV6 => IsBlockedIPv6(bytes),
            _ => true
        };
    }

    private static bool IsBlockedIPv4(byte[] bytes)
    {
        var first = bytes[0];
        var second = bytes[1];

        return first switch
        {
            0 => true,
            10 => true,
            100 when second is >= 64 and <= 127 => true,
            127 => true,
            169 when second == 254 => true,
            172 when second is >= 16 and <= 31 => true,
            192 when second == 168 => true,
            >= 224 => true,
            _ => false
        };
    }

    private static bool IsBlockedIPv6(byte[] bytes)
    {
        return bytes[0] switch
        {
            0xFC or 0xFD => true,
            0xFE when (bytes[1] & 0xC0) == 0x80 => true,
            0xFF => true,
            _ => IsUnspecifiedIPv6(bytes)
        };
    }

    private static bool IsUnspecifiedIPv6(byte[] bytes) => bytes.All(b => b == 0);
}
