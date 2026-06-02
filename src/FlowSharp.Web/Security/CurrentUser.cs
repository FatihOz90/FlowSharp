using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using FlowSharp.Infrastructure.Identity;

namespace FlowSharp.Web.Security;

/// <summary>
/// Oturum acmis kullanicinin kimligini ve Admin olup olmadigini cozer. Sahiplik (owner)
/// filtreleri icin kullanilir: Admin tum kayitlari gorur, digerleri yalniz kendi OwnerId'sini.
/// </summary>
internal static class CurrentUser
{
    public static async Task<(string? Id, bool IsAdmin)> ResolveAsync(AuthenticationStateProvider provider)
    {
        var user = (await provider.GetAuthenticationStateAsync()).User;
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return (id, user.IsInRole(IdentitySeeder.AdminRole));
    }
}
