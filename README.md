# Ön Muhasebe API — .NET 8 Minimal API

Küçük işletmeler için cari hesap, fatura ve tahsilat yönetimi sunan REST API.
Türkiye'deki e-dönüşüm kurallarını iş kuralı olarak modeller: alıcıya göre
**e-Fatura / e-Arşiv** seçimi, VKN/TCKN doğrulama, 16 karakterli fatura
numaralandırma, KDV dökümü ve iptal kısıtları.

## Öne çıkanlar

- **Katmanlı mimari:** `Domain` (iş kuralları) → `Application` (servisler, DTO'lar, arayüzler) → `Infrastructure` (depolar) → `Api` (uç noktalar). Domain ve Application katmanları ASP.NET'e bağımlı değildir.
- **Zengin domain modeli:** Geçersiz durum oluşturulamaz. Örneğin `VergiNumarasi` sadece kontrol hanesi doğru olan VKN/TCKN ile oluşur, `FaturaSatiri` sadece geçerli KDV oranlarını (%0, %1, %10, %20) kabul eder.
- **e-Dönüşüm kuralları:**
  - Alıcı kayıtlı e-Fatura kullanıcısıysa **e-Fatura** (etiket ve Temel/Ticari senaryo ile), değilse **e-Arşiv**.
  - Gönderilmiş **e-Arşiv iptal edilebilir**. Gönderilmiş **e-Fatura iptal edilemez**: API, ret veya iade faturası gerektiğini açıklayan bir hata döner.
  - Fatura numarası (`ABC2026000000001`) yalnızca gönderimde alınır; seri+yıl başına atomik sayaç kullanıldığı için eşzamanlı isteklerde bile mükerrer numara oluşmaz (1000 paralel istekle test edildi).
- **Para hesapları:** `decimal` ve kuruş hassasiyetinde "yarım yukarı" yuvarlama. Satır bazında iskonto ve KDV hesaplanır, oran bazında KDV dökümü çıkarılır.
- **Cari ekstre:** Gönderilmiş faturalar borç, tahsilatlar alacak olarak yazılır ve yürüyen bakiye hesaplanır. Taslak ve iptal edilen faturalar bakiyeyi etkilemez.
- **Güvenlik:** .NET 8 yerleşik bearer token kimlik doğrulaması ve rol bazlı yetkilendirme (iptal sadece `Yonetici` rolünde). Şifreler PBKDF2-SHA256 (100.000 iterasyon, rastgele tuz) ile saklanır ve sabit sürede karşılaştırılır.
- **Hata yönetimi:** İş kuralı hataları `IExceptionHandler` ile RFC 7807 **ProblemDetails** cevabına çevrilir (400 / 404). Beklenmeyen hatalarda detay gizlenir.
- **Testler:** xUnit ile 36 test: VKN/TCKN algoritması, fatura tutarları ve yuvarlama, belge tipi seçimi, iptal kuralları, numaralandırma ve eşzamanlılık, servisler, ekstre, şifre hash.
- Docker ve GitHub Actions CI.

## Mimari

```
OnMuhasebe.sln
├── src/
│   ├── OnMuhasebe.Domain          Cari, Fatura, FaturaSatiri, FaturaNumarasi, Tahsilat, VergiNumarasi
│   ├── OnMuhasebe.Application     CariService, FaturaService, TahsilatService, EkstreService, DTO'lar, repository arayüzleri
│   ├── OnMuhasebe.Infrastructure  Bellek içi (thread-safe) depolar, fatura numaratörü, DI kaydı
│   └── OnMuhasebe.Api             Minimal API uç noktaları, kimlik doğrulama, hata işleyici
└── tests/
    └── OnMuhasebe.Tests           xUnit testleri
```

Veri erişimi arayüzler üzerinden yapıldığı için bellek içi depolar, uygulama
katmanına dokunmadan EF Core / SQL Server ile değiştirilebilir.

## Çalıştırma

```bash
dotnet test
dotnet run --project src/OnMuhasebe.Api      # http://localhost:5190
```

Development ortamında iki demo kullanıcı tanımlıdır (`appsettings.Development.json`):

| Kullanıcı | Şifre | Rol |
|---|---|---|
| `yonetici` | `Yonetici123!` | Yonetici |
| `muhasebe` | `Muhasebe123!` | Muhasebe |

Örnek istekler `src/OnMuhasebe.Api/OnMuhasebe.Api.http` dosyasındadır (VS Code REST Client, Visual Studio veya Rider ile çalışır).

Production için kullanıcılar ortam değişkenleriyle verilir:

```bash
dotnet run --project src/OnMuhasebe.Api -- hash-sifre "GucluSifre!"   # hash üretir
Kullanicilar__0__KullaniciAdi=admin
Kullanicilar__0__Rol=Yonetici
Kullanicilar__0__SifreHash=<üretilen hash>
```

Docker:

```bash
docker build -t onmuhasebe-api .
docker run -p 8080:8080 -e Kullanicilar__0__KullaniciAdi=admin -e Kullanicilar__0__Rol=Yonetici -e Kullanicilar__0__SifreHash=... onmuhasebe-api
```

## Uç noktalar

| Metot | Yol | Açıklama |
|---|---|---|
| POST | `/api/auth/giris` | Token alır |
| GET / POST | `/api/cariler` | Cari listele / ekle |
| GET | `/api/cariler/{id}` | Cari detayı |
| GET | `/api/cariler/{id}/ekstre` | Cari hesap ekstresi ve bakiye |
| POST | `/api/faturalar` | Taslak fatura (belge tipi otomatik) |
| GET | `/api/faturalar/{id}` | Fatura detayı, KDV dökümü, toplamlar |
| POST | `/api/faturalar/{id}/gonder` | Numara atar ve gönderir |
| POST | `/api/faturalar/{id}/iptal` | İptal (sadece Yonetici, e-Fatura için kural kontrolü) |
| POST | `/api/tahsilatlar` | Tahsilat kaydı |
| GET | `/saglik` | Sağlık kontrolü |

## Örnek cevap

`POST /api/faturalar` (3 × 33,335 TL vida %10 iskonto %20 KDV + 100 TL kitap %10 KDV), kısaltılmış:

```json
{
  "belgeTipi": "EFatura",
  "senaryo": "Ticari",
  "aliciEtiketi": "urn:mail:defaultpk@efe.com",
  "durum": "Taslak",
  "kdvDokumu": [
    { "oran": 10, "matrah": 100, "kdv": 10 },
    { "oran": 20, "matrah": 90.01, "kdv": 18 }
  ],
  "iskontoToplami": 10,
  "netToplam": 190.01,
  "kdvToplami": 28,
  "genelToplam": 218.01
}
```

Gönderilmiş bir e-Faturayı iptal etmeye çalışınca:

```json
{
  "status": 400,
  "title": "İş kuralı hatası",
  "detail": "Gönderilmiş e-Fatura iptal edilemez. Ticari senaryoda alıcı reddedebilir; aksi halde iade faturası düzenlenmelidir."
}
```

## Yol haritası

- EF Core + SQL Server/PostgreSQL deposu ve migration'lar
- UBL-TR XML çıktısı
- OpenAPI/Swagger dokümantasyonu
- `WebApplicationFactory` ile entegrasyon testleri

## Lisans

MIT
