using FlowSharp.Domain.Common;

namespace FlowSharp.Domain.Credentials;

/// <summary>
/// Bir node'un dis servise baglanmak icin kullandigi sifreli kimlik bilgisi.
/// Hassas alanlar (apiKey, token, password...) <see cref="EncryptedData"/> icinde
/// sifrelenmis JSON olarak saklanir; veritabaninda duz metin tutulmaz.
/// </summary>
public sealed class Credential : AuditableEntity
{
    public required string Name { get; set; }

    /// <summary>Bu credential'i olusturan kullanicinin Identity Id'si (sahiplik/izolasyon).
    /// null ise sistem/eski kayit; yalniz sahibi (ya da Admin) gorur ve kullanabilir.</summary>
    public string? OwnerId { get; set; }

    /// <summary>Credential tip anahtari (orn. "httpHeaderAuth", "openAiApi", "postgres").</summary>
    public required string Type { get; set; }

    /// <summary>AES ile sifrelenmis JSON govdesi (base64).</summary>
    public required string EncryptedData { get; set; }
}
