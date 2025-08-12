# Key Management Web

Bu proje, ASP.NET Core 8.0 kullanılarak geliştirilmiş web tabanlı bir Anahtar Yönetim Sistemi (Key Management System) uygulamasıdır. Uygulama, kullanıcıların kriptografik anahtarları (AES, DES vb.) güvenli bir şekilde oluşturmasını, yönetmesini, güncellemesini ve silmesini sağlar. Ayrıca, kullanıcı yetkilendirme, e-posta bildirimleri ve tüm işlemlerin kaydını tutma gibi gelişmiş özelliklere sahiptir.

## Özellikler

- **Kullanıcı Yönetimi:**
  - Güvenli kullanıcı kaydı ve girişi (BCrypt.Net ile parola hashleme).
  - Yönetici (Admin) ve standart kullanıcı rolleri.
- **Yetkilendirme Sistemi:**
  - Kullanıcılara özel izinler atama (Anahtar **Oluşturma**, **Güncelleme**, **Silme**).
  - Yönetici, tüm yetkilere sahip özel bir roldür.
- **Anahtar Yönetimi:**
  - AES ve DES türünde kriptografik anahtarlar oluşturma.
  - Anahtarların değerlerini manuel veya otomatik olarak güncelleme.
  - Anahtarlar için otomatik güncelleme periyodu (gün sayısı) belirleme.
  - Anahtar geçmişini (`KeyHistory`) takip etme. Her anahtar güncellemesinde eski değer saklanır.
- **Şifreleme ve Şifre Çözme Modülleri:**
  - Uygulama içinde metinleri veya dosyaları seçilen anahtarlarla şifreleme.
  - Şifrelenmiş verileri yine anahtarları kullanarak çözme.
- **Bildirimler ve Kayıt Tutma:**
  - Anahtar oluşturma, güncelleme ve silme gibi kritik işlemlerde yöneticiye e-posta ile bildirim gönderme.
  - Tüm kullanıcı işlemlerini `App_Data/log.txt` dosyasına zaman damgasıyla kaydetme (logging).
- **Güvenlik:**
  - Yetkisiz erişimi engellemek için tüm sayfalarda `[Authorize]` attribute'u kullanımı.
  - Kullanıcı bazlı yetkilendirme ile güvenli operasyonlar.

## Kullanılan Teknolojiler

- **Backend:** C#, ASP.NET Core 8.0 MVC
- **Veritabanı:** Entity Framework Core 8, Microsoft SQL Server
- **Kimlik Doğrulama:** ASP.NET Core Identity, BCrypt.Net-Next (Parola Hashleme)
- **E-posta Gönderimi:** MailKit
- **Frontend:** Razor Pages, HTML, CSS, JavaScript

## Kurulum ve Çalıştırma

Projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin:

1.  **Ön Gereksinimler:**
    - [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
    - [Microsoft SQL Server](https://www.microsoft.com/tr-tr/sql-server/sql-server-downloads)

2.  **Projeyi Klonlayın:**
    ```bash
    git clone <repository-url>
    cd KeyManagementWeb-main/KeyManagementWeb
    ```

3.  **Veritabanı Ayarları:**
    - `appsettings.json` dosyasını açın.
    - `ConnectionStrings` bölümündeki `DefaultConnection` değerini kendi SQL Server bağlantı bilgilerinizle güncelleyin.

4.  **E-posta Ayarları:**
    - `appsettings.json` dosyasındaki `EmailSettings` bölümünü kendi SMTP sunucu bilgilerinizle doldurun. Bu, bildirimlerin çalışması için gereklidir.

5.  **Veritabanını Oluşturun:**
    - Proje ana dizininde bir terminal açın ve Entity Framework Core migration'larını çalıştırarak veritabanını oluşturun.
    ```bash
    dotnet ef database update
    ```

6.  **Uygulamayı Çalıştırın:**
    ```bash
    dotnet run
    ```

7.  Uygulama varsayılan olarak `https://localhost:PORT` ve `http://localhost:PORT` adreslerinde çalışmaya başlayacaktır.

## Yapılandırma

Uygulamanın temel ayarları `KeyManagementWeb/appsettings.json` dosyasında bulunur.

- **`ConnectionStrings`**: Veritabanı bağlantı dizesini içerir.
- **`EmailSettings`**: E-posta bildirimleri için SMTP ayarlarını içerir:
  - `FromEmail`: Gönderici e-posta adresi.
  - `ToEmail`: Bildirimlerin gönderileceği e-posta adresi (genellikle yönetici).
  - `SmtpServer`: SMTP sunucu adresi.
  - `Port`: SMTP port numarası.
  - `Username`: SMTP kullanıcı adı.
  - `Password`: SMTP parolası.

## Kullanım

1.  **Admin Girişi:** Uygulamayı ilk çalıştırdığınızda bir admin hesabı oluşturmanız gerekebilir. `AccountController`'daki `Register` metodunu kullanarak bir kullanıcı oluşturun ve veritabanından bu kullanıcıya `admin` rolü veya `DUC` yetkilerini verin.
2.  **Kullanıcı Yönetimi (Admin):** Admin olarak giriş yaptıktan sonra, `Admin` panelinden yeni kullanıcıları görüntüleyebilir ve onlara anahtar yönetimi için izinler (`D`elete, `U`pdate, `C`reate) atayabilirsiniz.
3.  **Anahtar Yönetimi:** Ana sayfada mevcut anahtarları listeleyebilir, yeni anahtarlar (AES/DES) oluşturabilir, mevcut anahtarların değerini veya güncelleme periyodunu değiştirebilir ve artık ihtiyaç duyulmayan anahtarları silebilirsiniz.

## Proje Yapısı

```
/KeyManagementWeb
├── Controllers/      # Uygulama mantığını ve yönlendirmeyi yönetir.
├── Data/             # Entity Framework DbContext ve migration'ları içerir.
├── Models/           # Veritabanı varlıklarını (Key, User, KeyHistory) tanımlar.
├── Views/            # Kullanıcı arayüzünü oluşturan Razor dosyalarını içerir.
├── Services/         # İş mantığı servislerini içerir (varsa).
├── wwwroot/          # Statik dosyaları (CSS, JS, resimler) barındırır.
├── appsettings.json  # Uygulama yapılandırma ayarları.
└── Program.cs        # Uygulamanın başlangıç noktası, servislerin ve middleware'lerin yapılandırıldığı yer.
