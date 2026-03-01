<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?style=for-the-badge&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WPF%20%2B%20Material%20Design-blueviolet?style=for-the-badge" alt="WPF">
  <img src="https://img.shields.io/github/license/davutari/DevARISetup?style=for-the-badge" alt="License">
  <img src="https://img.shields.io/github/v/release/davutari/DevARISetup?style=for-the-badge&color=green" alt="Release">
</p>

<h1 align="center">DevARI Setup</h1>
<p align="center">
  <strong>Windows 10/11 için Gelishtirici Araclari Yoneticisi</strong><br>
  <em>20+ araci tek tikla yukle, yonet ve yapilandir</em>
</p>

<p align="center">
  <a href="https://github.com/davutari/DevARISetup/releases/latest"><strong>Indir</strong></a> ·
  <a href="#ozellikler">Ozellikler</a> ·
  <a href="#desteklenen-araclar">Araclar</a> ·
  <a href="#kurulum">Kurulum</a> ·
  <a href="#ekran-goruntuleri">Ekran Goruntuleri</a>
</p>

---

## Nedir?

DevARI Setup, Windows uzerinde gelistirici ortamini hizla kurmak icin tasarlanmis bir masaustu uygulamasidir. **Winget** ve **Chocolatey** paket yoneticilerini kullanarak Node.js, Python, Docker, Git ve daha bircok araci tek bir arayuzden yonetmenizi saglar.

Yeni bir bilgisayar mi aldiniz? Formati mi attiniz? DevARI Setup ile tum gelistirme ortaminizi dakikalar icinde yeniden kurun.

## Ozellikler

| Ozellik | Aciklama |
|---------|----------|
| **Tek Tikla Kurulum** | Winget & Chocolatey ile 20+ araci otomatik yukleyin |
| **Servis Yonetimi** | PostgreSQL, MySQL, MongoDB, Redis, Docker, Nginx, Cassandra servislerini baslat/durdur |
| **Ortam Denetimi** | PATH, JAVA_HOME, GOPATH gibi ortam degiskenlerini otomatik kontrol ve duzeltme |
| **Saglik Kontrolu** | Tum araclarin ve servislerin durumunu tek tikla kontrol edin |
| **Profil Yonetimi** | Farkli projeler icin farkli arac setleri tanimlayip gecis yapin |
| **Canli Log Akisi** | Kurulum ve yapilandirma islemlerini terminal gorunumunde canli takip |
| **Modern Arayuz** | Material Design temali koyu tema, gradient kartlar, animasyonlar |
| **Yonetici Destegi** | UAC ile gerektiginde admin yetkisi otomatik ister |

## Desteklenen Araclar

### Runtime & Diller
| Arac | Paket Yoneticisi | ID |
|------|------------------|----|
| Node.js LTS | winget | `OpenJS.NodeJS.LTS` |
| Python 3.13 | winget | `Python.Python.3.13` |
| Java JDK 17 | winget | `Microsoft.OpenJDK.17` |
| Go | winget | `GoLang.Go` |
| Rust | winget | `Rustlang.Rustup` |

### Veritabani
| Arac | Paket Yoneticisi | ID |
|------|------------------|----|
| PostgreSQL 17 | winget | `PostgreSQL.PostgreSQL.17` |
| MySQL | winget | `Oracle.MySQL` |
| MongoDB | winget | `MongoDB.Server` |
| Redis | winget | `Redis.Redis` |
| Apache Cassandra | choco | `apache-cassandra` |
| MongoDB Compass | winget | `MongoDB.Compass.Full` |

### DevOps & Araclar
| Arac | Paket Yoneticisi | ID |
|------|------------------|----|
| Docker Desktop | winget | `Docker.DockerDesktop` |
| Git | winget | `Git.Git` |
| VS Code | winget | `Microsoft.VisualStudioCode` |
| GitHub CLI | winget | `GitHub.cli` |
| Nginx | choco | `nginx` |
| Laragon | choco | `laragon` |

### Mobil Gelistirme
| Arac | Paket Yoneticisi | ID |
|------|------------------|----|
| Flutter SDK | choco | `flutter` |
| Dart SDK | choco | `dart-sdk` |
| Android Studio | winget | `Google.AndroidStudio` |

## Kurulum

### Hazir Calistirma (Onerilen)

1. [**Son surumu indirin**](https://github.com/davutari/DevARISetup/releases/latest) (~66 MB)
2. ZIP dosyasini acin
3. `DevARIManager.App.exe` dosyasini calistirin

> **Not:** .NET runtime gerektirmez. Tek dosya, self-contained uygulama.

### Kaynak Koddan Derleme

```bash
# Repoyu klonlayın
git clone https://github.com/davutari/DevARISetup.git
cd DevARISetup

# Derleyin
dotnet build DevARIManager.sln

# Calistirin
dotnet run --project src/DevARIManager.App

# Self-contained publish
dotnet publish src/DevARIManager.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Gereksinimler (derleme icin):**
- .NET 8.0 SDK
- Windows 10/11

## Mimari

```
DevARISetup/
├── src/
│   ├── DevARIManager.App/          # WPF UI katmani
│   │   ├── Views/                  # XAML sayfalari
│   │   ├── ViewModels/             # MVVM ViewModel'ler
│   │   ├── Converters/             # WPF degistiriciler
│   │   ├── Themes/                 # Material Design tema
│   │   └── MainWindow.xaml         # Ana pencere (sidebar navigasyon)
│   │
│   └── DevARIManager.Core/         # Is mantigi katmani
│       ├── Models/                 # Veri modelleri
│       ├── Services/               # Servis katmani
│       │   ├── ToolManager.cs      # Arac yukle/kaldir/kontrol
│       │   ├── ServiceManager.cs   # Servis baslat/durdur
│       │   ├── HealthChecker.cs    # Saglik kontrolu
│       │   ├── EnvironmentManager.cs # PATH & ortam degiskenleri
│       │   └── ProfileManager.cs   # Profil yonetimi
│       └── Helpers/
│           └── ProcessRunner.cs    # PowerShell/CMD calistirma
│
├── website/                        # Tanitim web sitesi
│   └── index.html                  # Single-page landing page
│
└── docs/
    └── PRD.md                      # Proje gereksinimleri
```

### Teknoloji Yigini

- **UI Framework:** WPF (.NET 8)
- **MVVM:** CommunityToolkit.Mvvm 8.4
- **Tema:** MaterialDesignThemes 5.1.0
- **DI:** Microsoft.Extensions.DependencyInjection
- **Paket Yoneticileri:** Winget, Chocolatey
- **Hedef:** `net8.0-windows` (Windows 10/11 x64)

## Nasil Calisir?

```
Kullanici → Arac Sec → [Winget/Choco] → Otomatik Yukle → PATH Ayarla → Hazir!
```

1. **Arac Tarama:** Uygulama baslatildiginda tum araclarin yuklu olup olmadigini kontrol eder
2. **Tek Tikla Kurulum:** Secilen araci Winget veya Chocolatey ile yukler
3. **Bagimlilk Yonetimi:** Ornegin Flutter icin otomatik olarak Git ve JDK kontrol eder
4. **Servis Yonetimi:** Veritabani ve sunucu servislerini Windows Service veya process olarak yonetir
5. **Ortam Ayarlari:** PATH, JAVA_HOME gibi ortam degiskenlerini otomatik yapilandirir

## Katkida Bulunma

1. Bu repoyu fork edin
2. Feature branch olusturun (`git checkout -b feature/yeni-ozellik`)
3. Degisikliklerinizi commit edin (`git commit -m 'Yeni ozellik ekle'`)
4. Branch'i push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request acin

## Lisans

Bu proje MIT lisansi ile lisanslanmistir. Detaylar icin [LICENSE](LICENSE) dosyasina bakin.

---

<p align="center">
  <strong>Developed by Davut ARI && Claude</strong><br>
  <a href="https://github.com/davutari/DevARISetup">GitHub</a> ·
  <a href="https://github.com/davutari/DevARISetup/releases">Releases</a> ·
  <a href="https://github.com/davutari/DevARISetup/issues">Issues</a>
</p>
