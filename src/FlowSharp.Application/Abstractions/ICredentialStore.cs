namespace FlowSharp.Application.Abstractions;

/// <summary>Sifreli credential'larin yonetimi (CRUD) ve node'lar icin cozumlemesi.
/// Sahiplik (owner) ile izole edilir: <paramref name="ownerId"/> null ise kisitsiz (Admin),
/// degilse yalniz o sahibin kayitlari.</summary>
public interface ICredentialStore
{
    /// <summary>ownerId null ise tum credential'lar (Admin), degilse yalniz o sahibinkiler.</summary>
    Task<IReadOnlyList<CredentialSummary>> ListAsync(string? ownerId, CancellationToken cancellationToken = default);

    /// <summary>ownerId null ise kisit yok (Admin); degilse yalniz o sahibe ait kayit dondurulur.</summary>
    Task<CredentialDetail?> GetAsync(Guid id, string? ownerId, CancellationToken cancellationToken = default);

    /// <summary>Olusturur veya gunceller (Id null ise yeni). Yeni kayitta <see cref="CredentialInput.OwnerId"/>
    /// sahip olarak atanir; guncellemede sahip degistirilemez. Hassas alanlar sifrelenir.</summary>
    Task<Guid> SaveAsync(CredentialInput input, CancellationToken cancellationToken = default);

    /// <summary>ownerId null ise kisit yok (Admin); degilse yalniz o sahibe ait kayit silinir.</summary>
    Task DeleteAsync(Guid id, string? ownerId, CancellationToken cancellationToken = default);

    /// <summary>Bir node icin credential'i Id ile cozer. Yalniz <paramref name="expectedOwnerId"/> ile
    /// ayni sahibe ait kayit cozulur (cross-tenant secret sizmasini engeller). Bulunamaz/eslesmezse null.</summary>
    Task<IReadOnlyDictionary<string, string>?> ResolveAsync(Guid id, string? expectedOwnerId, CancellationToken cancellationToken = default);

    /// <summary>Eski (isim tabanli) referanslar icin: tip+ad ile, sahiplik dogrulamasiyla cozer.</summary>
    Task<IReadOnlyDictionary<string, string>?> ResolveAsync(string type, string name, string? expectedOwnerId, CancellationToken cancellationToken = default);
}

public sealed record CredentialSummary(Guid Id, string Name, string Type, DateTimeOffset CreatedAt, string? OwnerId = null);

public sealed record CredentialDetail(Guid Id, string Name, string Type, IReadOnlyDictionary<string, string> Data);

public sealed record CredentialInput(Guid? Id, string Name, string Type, IReadOnlyDictionary<string, string> Data, string? OwnerId = null);
