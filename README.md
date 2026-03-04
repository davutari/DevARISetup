<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?style=for-the-badge&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/Platform-Linux%20x64-green?style=for-the-badge&logo=linux" alt="Linux Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WPF%20%2B%20Material%20Design-blueviolet?style=for-the-badge" alt="WPF">
  <img src="https://img.shields.io/github/license/davutari/DevARISetup?style=for-the-badge" alt="License">
  <img src="https://img.shields.io/github/v/release/davutari/DevARISetup?style=for-the-badge&color=green" alt="Release">
</p>

<h1 align="center">DevARI Setup</h1>
<p align="center">
  <strong>Windows 10/11 için Geliştirici Araçları Yöneticisi</strong><br>
  <em>20+ aracı tek tıkla yükle, yönet ve yapılandır</em>
</p>

<p align="center">
  <a href="https://github.com/davutari/DevARISetup/releases/latest"><strong>İndir</strong></a> ·
  <a href="linux/README.md"><strong>Linux Indirme</strong></a> ·
  <a href="#özellikler">Özellikler</a> ·
  <a href="#desteklenen-araçlar">Araçlar</a> ·
  <a href="#kurulum">Kurulum</a> ·
  <a href="#ekran-görüntüleri">Ekran Görüntüleri</a>
</p>

---

## Nedir?

DevARI Setup, Windows üzerinde geliştirici ortamını hızla kurmak için tasarlanmış bir masaüstü uygulamasıdır. **Winget** ve **Chocolatey** paket yöneticilerini kullanarak Node.js, Python, Docker, Git ve daha birçok aracı tek bir arayüzden yönetmenizi sağlar.

Yeni bir bilgisayar mı aldınız? Formatı mı attınız? DevARI Setup ile tüm geliştirme ortamınızı dakikalar içinde yeniden kurun.

## Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Tek Tıkla Kurulum** | Winget & Chocolatey ile 20+ aracı otomatik yükleyin |
| **Servis Yönetimi** | PostgreSQL, MySQL, MongoDB, Redis, Docker, Nginx, Cassandra servislerini başlat/durdur |
| **Ortam Denetimi** | PATH, JAVA_HOME, GOPATH gibi ortam değişkenlerini otomatik kontrol ve düzeltme |
| **Sağlık Kontrolü** | Tüm araçların ve servislerin durumunu tek tıkla kontrol edin |
| **Profil Yönetimi** | Farklı projeler için farklı araç setleri tanımlayıp geçiş yapın |
| **Canlı Log Akışı** | Kurulum ve yapılandırma işlemlerini terminal görünümünde canlı takip |
| **Modern Arayüz** | Material Design temalı koyu tema, gradient kartlar, animasyonlar |
| **Yönetici Desteği** | UAC ile gerektiğinde admin yetkisi otomatik ister |

## Desteklenen Araçlar

### Runtime & Diller
| Araç | Paket Yöneticisi | ID |
|------|------------------|----|
| Node.js LTS | winget | `OpenJS.NodeJS.LTS` |
| Python 3.13 | winget | `Python.Python.3.13` |
| Java JDK 17 | winget | `Microsoft.OpenJDK.17` |
| Go | winget | `GoLang.Go` |
| Rust | winget | `Rustlang.Rustup` |

### Veritabanı
| Araç | Paket Yöneticisi | ID |
|------|------------------|----|
| PostgreSQL 17 | winget | `PostgreSQL.PostgreSQL.17` |
| MySQL | winget | `Oracle.MySQL` |
| MongoDB | winget | `MongoDB.Server` |
| Redis | winget | `Redis.Redis` |
| Apache Cassandra | choco | `apache-cassandra` |
| MongoDB Compass | winget | `MongoDB.Compass.Full` |

### DevOps & Araçlar
| Araç | Paket Yöneticisi | ID |
|------|------------------|----|
| Docker Desktop | winget | `Docker.DockerDesktop` |
| Git | winget | `Git.Git` |
| VS Code | winget | `Microsoft.VisualStudioCode` |
| GitHub CLI | winget | `GitHub.cli` |
| Nginx | choco | `nginx` |
| Laragon | choco | `laragon` |

### Mobil Geliştirme
| Araç | Paket Yöneticisi | ID |
|------|------------------|----|
| Flutter SDK | choco | `flutter` |
| Dart SDK | choco | `dart-sdk` |
| Android Studio | winget | `Google.AndroidStudio` |

## Kurulum

### Hazır Çalıştırma (Önerilen)

1. [**Son sürümü indirin**](https://github.com/davutari/DevARISetup/releases/latest) (~66 MB)
2. ZIP dosyasını açın
3. `DevARIManager.App.exe` dosyasını çalıştırın

> **Not:** .NET runtime gerektirmez. Tek dosya, self-contained uygulama.

### Kaynak Koddan Derleme

```bash
# Repoyu klonlayın
git clone https://github.com/davutari/DevARISetup.git
cd DevARISetup

# Derleyin
dotnet build DevARIManager.sln

# Çalıştırın
dotnet run --project src/DevARIManager.App

# Self-contained publish
dotnet publish src/DevARIManager.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Gereksinimler (derleme için):**
- .NET 8.0 SDK
- Windows 10/11

### Linux Port (CLI)

Linux portu, WPF yerine terminal odakli bir uygulama olarak `src/DevARIManager.Linux` altinda sunulmustur.
Linux indirme ve kurulum kisayolu icin: `linux/README.md`

```bash
# Linux CLI calistirma
dotnet run --project src/DevARIManager.Linux -- list
dotnet run --project src/DevARIManager.Linux -- check all
dotnet run --project src/DevARIManager.Linux -- install git

# .deb ve AppImage uretimi
./packaging/linux/build-linux.sh 0.1.0
```

Uretilen dosyalar:
- `dist/linux/devari-manager_0.1.0_amd64.deb`
- `dist/linux/devari-manager-0.1.0-x86_64.AppImage` (sistemde `appimagetool` varsa)

## Mimari

```
DevARISetup/
├── src/
│   ├── DevARIManager.App/          # WPF UI katmanı
│   │   ├── Views/                  # XAML sayfaları
│   │   ├── ViewModels/             # MVVM ViewModel'ler
│   │   ├── Converters/             # WPF dönüştürücüler
│   │   ├── Themes/                 # Material Design tema
│   │   └── MainWindow.xaml         # Ana pencere (sidebar navigasyon)
│   │
│   └── DevARIManager.Core/         # İş mantığı katmanı
│       ├── Models/                 # Veri modelleri
│       ├── Services/               # Servis katmanı
│       │   ├── ToolManager.cs      # Araç yükle/kaldır/kontrol
│       │   ├── ServiceManager.cs   # Servis başlat/durdur
│       │   ├── HealthChecker.cs    # Sağlık kontrolü
│       │   ├── EnvironmentManager.cs # PATH & ortam değişkenleri
│       │   └── ProfileManager.cs   # Profil yönetimi
│       └── Helpers/
│           └── ProcessRunner.cs    # PowerShell/CMD çalıştırma
│
├── website/                        # Tanıtım web sitesi
│   └── index.html                  # Single-page landing page
│
└── docs/
    └── PRD.md                      # Proje gereksinimleri
```

### Teknoloji Yığını

- **UI Framework:** WPF (.NET 8)
- **Linux Port UI:** CLI (.NET 8)
- **MVVM:** CommunityToolkit.Mvvm 8.4
- **Tema:** MaterialDesignThemes 5.1.0
- **DI:** Microsoft.Extensions.DependencyInjection
- **Paket Yöneticileri:** Winget, Chocolatey
- **Hedef:** `net8.0-windows` (Windows GUI) + `net8.0` (Linux CLI)

## Nasıl Çalışır?

```
Kullanıcı → Araç Seç → [Winget/Choco] → Otomatik Yükle → PATH Ayarla → Hazır!
```

1. **Araç Tarama:** Uygulama başlatıldığında tüm araçların yüklü olup olmadığını kontrol eder
2. **Tek Tıkla Kurulum:** Seçilen aracı Winget veya Chocolatey ile yükler
3. **Bağımlılık Yönetimi:** Örneğin Flutter için otomatik olarak Git ve JDK kontrol eder
4. **Servis Yönetimi:** Veritabanı ve sunucu servislerini Windows Service veya process olarak yönetir
5. **Ortam Ayarları:** PATH, JAVA_HOME gibi ortam değişkenlerini otomatik yapılandırır

## Katkıda Bulunanlar

Bu projeye katkıda bulunan değerli geliştiricilere teşekkürler:

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/davutari">
        <img src="https://github.com/davutari.png" width="80px;" alt="Davut ARI"/><br />
        <sub><b>Davut ARI</b></sub>
      </a><br />
      <sub>Proje Sahibi & Ana Geliştirici</sub>
    </td>
    <td align="center">
      <a href="https://github.com/MustafaKemal0146">
        <img src="https://github.com/MustafaKemal0146.png" width="80px;" alt="Mustafa Kemal"/><br />
        <sub><b>Mustafa Kemal</b></sub>
      </a><br />
      <sub>Linux Desteği (CLI, GUI & Paketleme)</sub>
    </td>
  </tr>
</table>

> 🎉 **Mustafa Kemal** — Linux desteği için harika bir katkı sağladı! Avalonia GUI, CLI aracı ve .deb/AppImage paketleme altyapısıyla projeyi çoklu platform desteğine taşıdı. Teşekkürler ve tebrikler! 👏

## Katkıda Bulunma

Projeye katkıda bulunmak istiyorsanız:

1. Bu repoyu fork edin
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik ekle'`)
4. Branch'i push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request açın

Her türlü katkıya açığız — yeni özellikler, hata düzeltmeleri, dokümantasyon iyileştirmeleri!

## Lisans

Bu proje MIT lisansı ile lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

---

<p align="center">
  <strong>Developed by Davut ARI && Claude</strong><br>
  <a href="https://github.com/davutari/DevARISetup">GitHub</a> ·
  <a href="https://github.com/davutari/DevARISetup/releases">Releases</a> ·
  <a href="https://github.com/davutari/DevARISetup/issues">Issues</a>
</p>
