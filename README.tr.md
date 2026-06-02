# FlowSharp

![FlowSharp workflow automation hero](docs/assets/flowsharp-hero.png)

FlowSharp, **C#**, **.NET 10** ve **Blazor** ile geliştirilen node tabanlı bir workflow otomasyon platformudur. Görsel workflow tasarımcısı, çalıştırılabilir otomasyon node'ları, AI ajan desteği, webhook ve zamanlanmış tetikleyiciler, arka plan worker'ları ve çalışma zamanında yüklenebilen C# plugin sistemi içerir.

![FlowSharp workflow designer](docs/assets/flowsharp-designer-mockup.png)

## Öne Çıkanlar

- Node paleti, bağlantılar, parametreler ve çalışma durumları olan görsel workflow tasarımcısı.
- HTTP, e-posta, PostgreSQL, mantık, veri dönüşümü, JavaScript, iletişim servisleri ve AI için çalıştırılabilir node'lar.
- Semantic Kernel tabanlı, model ve araç alt-node'ları destekleyen AI ajanları.
- Webhook, manuel, zamanlanmış, chat, IMAP, workflow ve hata tetikleyicileri.
- Runtime plugin sistemi: C# kaynak dosyalarını `plugins/` klasörüne bırakıp uygulamayı yeniden derlemeden yeni node yükleme.
- ASP.NET Core Identity, rol/permission policy altyapısı, şifreli credential saklama, SignalR canlı olaylar ve Serilog logları.

## Hızlı Başlangıç

Docker Compose ile stack'i çalıştırın:

```bash
docker compose up -d --build
```

Tarayıcıdan açın:

```text
http://localhost:8080
```

Varsayılan admin hesabı:

```text
admin@flowsharp.local
Admin!2345
```

Varsayılan Docker Compose kurulumu uygulama veritabanı için SQLite, process'ler arası workflow olayları için Redis kullanır.

## Lokal Geliştirme

Gereksinimler:

- .NET 10 SDK
- Docker, Redis ve veritabanı servisleri için opsiyonel ama kullanışlıdır

Derleme:

```powershell
dotnet restore
dotnet build
```

Web uygulamasını çalıştırma:

```powershell
dotnet run --project src/FlowSharp.Web
```

Worker'ı ayrı terminalde çalıştırma:

```powershell
dotnet run --project src/FlowSharp.Worker
```

Tek process geliştirme modu için:

```json
{
  "Worker": {
    "RunInWebProcess": true
  }
}
```


## Veritabanı ve Migration'lar

FlowSharp, Entity Framework Core üzerine kuruludur ve kutudan çıktığı haliyle üç veritabanı sağlayıcısını destekler:

| Sağlayıcı | `Database:Provider` | Önerilen kullanım |
|---|---|---|
| SQLite | `Sqlite` | Lokal geliştirme ve tek düğümlü / küçük ekip self-hosting |
| PostgreSQL | `Postgres` | Üretim ve çok kullanıcılı dağıtımlar |
| SQL Server | `SqlServer` | Microsoft SQL Server standardına sahip ortamlar |

### Sağlayıcı seçimi

Aktif sağlayıcı tamamen yapılandırma ile belirlenir — kod değişikliği gerekmez. Sağlayıcı anahtarını ve uygun bağlantı dizesini ayarlamanız yeterlidir:

```json
{
  "Database": { "Provider": "Postgres", "ApplyMigrationsOnStartup": true },
  "ConnectionStrings": { "DefaultConnection": "Host=...;Database=...;Username=...;Password=..." }
}
```

`ApplyMigrationsOnStartup` `true` olduğunda, uygulama seçilen sağlayıcının migration'larını açılışta otomatik uygular. Yeni bir veritabanı tüm şemayı, mevcut bir veritabanı yalnızca bekleyen migration'ları alır; böylece güvenli ve veri kaybına yol açmayan şema yükseltmesi sağlanır. Eşzamanlı çalışan instance'lar EF Core'un migration kilidiyle koordine edilir.

### Sağlayıcıya özel migration setleri

Her sağlayıcı kendi native migration assembly'sini barındırır; böylece kolon tipleri hedef motor için her zaman doğrudur (örneğin PostgreSQL'de `jsonb`, SQLite'ta `TEXT`, SQL Server'da `nvarchar(max)`):

```text
src/FlowSharp.Migrations.Sqlite
src/FlowSharp.Migrations.Postgres
src/FlowSharp.Migrations.SqlServer
```

Çalışma zamanında uygun set, yapılandırılan sağlayıcıya göre otomatik seçilir.

### Operatörler

Hiçbir migration komutu gerekmez. Bir sağlayıcı seçin, bağlantı dizesini verin ve uygulamayı başlatın — şema otomatik olarak oluşturulur ve güncel tutulur.

### Katkıda bulunanlar

Veri modeli değiştiğinde (yeni veya değiştirilmiş bir entity), setlerin senkron kalması için **üç sağlayıcı için de** migration üretin. Komutların tamamı [Veritabanı ve Migration'lar](docs/guide/database-migrations.md) belgesinde yer alır.


## Dokümantasyon

- [Getting Started](docs/guide/getting-started.md)
- [Architecture](docs/guide/architecture.md)
- [Configuration](docs/guide/configuration.md)
- [Roles And Permissions](docs/guide/roles-and-permissions.md)
- [Built-in Nodes](docs/guide/built-in-nodes.md)
- [AI Agents](docs/guide/ai-agents.md)
- [Webhooks](docs/guide/webhooks.md)
- [Plugin Development](docs/guide/plugin-development.md)
- [Marketplace](docs/guide/marketplace.md)
- [Database & Migrations](docs/guide/database-migrations.md)

## Proje Yapısı

```text
src/
|-- FlowSharp.Web            Blazor UI, Identity, designer, webhooks, marketplace
|-- FlowSharp.Worker         Kuyruktaki ve zamanlanmış işleri çalışan arka plan worker'ı
|-- FlowSharp.Domain         Workflow, execution, queue, credential ve node modelleri
|-- FlowSharp.Application    Interface'ler ve application contract'ları
|-- FlowSharp.Infrastructure EF Core, workflow engine, queue, plugins, scheduler
|-- FlowSharp.Nodes          Yerleşik workflow node'ları
```

## Lisans

FlowSharp **Elastic License 2.0 (ELv2)** ile lisanslanmıştır. Detaylar için [LICENSE.md](LICENSE.md) dosyasına bakın.

## Katkı

Pull request açmadan önce lütfen [CONTRIBUTING.tr.md](CONTRIBUTING.tr.md) dosyasını okuyun.
