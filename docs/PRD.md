# DevARI Manager - Product Requirements Document (PRD)
# Gelistirici Arac Yoneticisi - Urun Gereksinimleri Dokumani

**Versiyon:** 1.0
**Tarih:** 2026-02-25
**Platform:** Windows 10 / Windows 11
**Teknoloji:** C# / .NET 8 / WPF
**Kod Adi:** DevARI Manager Setup

---

## 1. VIZYON VE AMAC

### 1.1 Problem Tanimi
Yazilim gelistirme ogrencileri, gelistirme ortamlarini kurarken ciddi zaman ve motivasyon kaybi yasiyor. "Kurulum cehennemi" olarak bilinen bu surecte:
- Yanlis versiyon yukleme
- PATH/Environment degiskenlerini unutma
- Servislerin baslatilamamasi
- Bagimliliklarin eksik kalmasi
- Farkli araclarin birbiriyle uyumsuzlugu

Bu sorunlar nedeniyle ogrenciler gercek gelistirme islerine baslamadan saatler/gunler kaybediyor.

### 1.2 Cozum
**DevARI Manager**, tek bir modern arayuzden tum gelistirici araclarini yukleme, kaldirma, servis yonetimi, ortam degiskeni kontrolu ve saglik denetimi yapabilen bir Windows masaustu uygulamasidir.

### 1.3 Hedef Kitle
- Yazilim muhendisligi ogrencileri
- Bootcamp katilimcilari
- Junior gelistiriciler
- Egitmenler (sinif ortami icin toplu kurulum)
- Freelance gelistiriciler

---

## 2. DESTEKLENEN TEKNOLOJI YIGINI (TECH STACK)

### 2.1 Runtime & Dil Ortamlari
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **Node.js** | Runtime | LTS + Current surum destegi |
| **Python** | Runtime | 3.x serileri |
| **Java JDK** | Runtime | OpenJDK 17/21 |
| **.NET SDK** | Runtime | .NET 6/7/8 |
| **Go** | Runtime | Golang |
| **Rust** | Runtime | rustup + cargo |
| **Ruby** | Runtime | Ruby + gem |

### 2.2 Veritabanlari & Veri Depolama
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **MongoDB** | NoSQL DB | Community Server + Compass |
| **Redis** | Cache/DB | Redis for Windows (Memurai veya resmi WSL) |
| **Apache Cassandra** | NoSQL DB | Distributed NoSQL |
| **PostgreSQL** | SQL DB | Iliskisel veritabani |
| **MySQL** | SQL DB | Iliskisel veritabani |
| **SQLite** | Embedded DB | Dosya tabanli DB |

### 2.3 Web Framework Araclari
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **React** (via npm/npx) | Frontend | create-react-app / Vite |
| **NestJS** (via npm) | Backend | NestJS CLI |
| **Next.js** (via npm) | Fullstack | Next.js CLI |
| **Angular CLI** | Frontend | Angular CLI |
| **Vue CLI** | Frontend | Vue.js CLI |
| **Express Generator** | Backend | Express.js scaffolding |

### 2.4 Mobil Gelistirme
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **Flutter SDK** | Cross-platform | Flutter + Dart SDK |
| **Android Studio** | IDE | Android IDE + SDK Manager |
| **Android SDK** | SDK | Command-line tools |
| **React Native CLI** | Cross-platform | RN CLI + bağımlılıklar |
| **Gradle** | Build | Android build sistemi |

### 2.5 Konteyner & DevOps
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **Docker Desktop** | Container | Container runtime |
| **Docker Compose** | Orchestration | Multi-container |
| **Git** | VCS | Versiyon kontrol |
| **GitHub CLI** | VCS | gh komutu |
| **WSL 2** | Subsystem | Windows Subsystem for Linux |

### 2.6 Paket Yoneticileri
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **npm** | Node | Node.js ile gelir |
| **yarn** | Node | Alternatif paket yoneticisi |
| **pnpm** | Node | Hizli paket yoneticisi |
| **pip** | Python | Python paket yoneticisi |
| **Chocolatey** | Windows | Windows paket yoneticisi |
| **winget** | Windows | Microsoft paket yoneticisi |
| **Scoop** | Windows | Portable paket yoneticisi |

### 2.7 IDE & Editorler
| Arac | Kategori | Aciklama |
|------|----------|----------|
| **VS Code** | Editor | Lightweight editor |
| **Visual Studio** | IDE | Community edition |
| **IntelliJ IDEA** | IDE | Community edition |
| **Postman** | API | API test araci |

---

## 3. OZELLIK DETAYLARI

### 3.1 Ana Dashboard
```
+------------------------------------------------------------------+
|  DevARI Manager v1.0                              [_] [□] [X]    |
+------------------------------------------------------------------+
|  [Dashboard] [Araclar] [Servisler] [Ortam] [Saglik] [Ayarlar]   |
+------------------------------------------------------------------+
|                                                                    |
|  SISTEM OZETI                          HIZLI ISLEMLER             |
|  +----------------------------+       +------------------------+  |
|  | Kurulu Arac:    24/42      |       | [▶ Tum DB Baslat]     |  |
|  | Calisan Servis:  5         |       | [⏹ Tum DB Durdur]     |  |
|  | PATH Sorunu:     2         |       | [🔄 Ortam Tazele]     |  |
|  | Guncelleme:      3         |       | [🩺 Saglik Taramasi]  |  |
|  +----------------------------+       +------------------------+  |
|                                                                    |
|  SON ISLEMLER                                                     |
|  +------------------------------------------------------------+  |
|  | [✓] Node.js v20.11 basariyla yuklendi      - 5 dk once     |  |
|  | [✓] MongoDB servisi baslatildi             - 12 dk once    |  |
|  | [!] Redis PATH eksik - Duzelt              - 1 saat once   |  |
|  +------------------------------------------------------------+  |
|                                                                    |
+------------------------------------------------------------------+
```

### 3.2 Arac Yonetimi Modulu

#### 3.2.1 Kurulum (Install)
- Secilen aracin resmi kaynaktan indirilmesi
- Sessiz kurulum (silent install) parametreleri ile otomatik kurulum
- Kurulum sırasında ilerleme cubugu
- Versiyon secimi (dropdown ile LTS/Current/Specific)
- Kurulum oncesi disk alani ve bagimlılık kontrolu
- Arka planda PowerShell/winget/Chocolatey kullanimi

#### 3.2.2 Kaldirma (Uninstall)
- Temiz kaldirma islemi
- Iliskili PATH ve ortam degiskeni temizligi
- Kalinti dosya temizleme secenegi
- Kaldirma oncesi onay diyalogu

#### 3.2.3 Guncelleme (Update)
- Mevcut versiyon vs guncel versiyon karsilastirmasi
- Tek tikla guncelleme
- Toplu guncelleme secenegi
- Guncelleme oncesi yedekleme secenegi

#### 3.2.4 Kurulum Durumu Kontrolu (Health Check)
- Aracin kurulu olup olmadigi kontrolu (`where` / `which` komutlari)
- Versiyon bilgisi alma (`--version` ciktilari)
- Beklenen PATH'te olup olmadigi kontrolu
- Bagimlilik zinciri kontrolu (ornegin: Flutter → Dart, Android SDK, JDK)

### 3.3 Servis Yonetimi Modulu

#### 3.3.1 Desteklenen Servisler
| Servis | Varsayilan Port | Baslat/Durdur |
|--------|----------------|---------------|
| MongoDB | 27017 | mongod process / Windows Service |
| Redis | 6379 | redis-server process |
| Cassandra | 9042 | cassandra process |
| PostgreSQL | 5432 | Windows Service |
| MySQL | 3306 | Windows Service |
| Docker | - | Docker Desktop Service |

#### 3.3.2 Servis Kontrol Paneli
```
+----------------------------------------------------------+
|  SERVIS YONETIMI                                          |
+----------------------------------------------------------+
|  Servis        | Durum    | Port  | PID  | Islem         |
|  ------------- | -------- | ----- | ---- | ------------- |
|  MongoDB       | ● Aktif  | 27017 | 4521 | [⏹] [🔄]     |
|  Redis         | ○ Kapali | 6379  | -    | [▶] [🔄]     |
|  PostgreSQL    | ● Aktif  | 5432  | 2103 | [⏹] [🔄]     |
|  Cassandra     | ○ Kapali | 9042  | -    | [▶] [🔄]     |
|  MySQL         | ○ Kapali | 3306  | -    | [▶] [🔄]     |
|  Docker Engine | ● Aktif  | -     | 1890 | [⏹] [🔄]     |
+----------------------------------------------------------+
|  [▶ Hepsini Baslat]  [⏹ Hepsini Durdur]                  |
+----------------------------------------------------------+
```

#### 3.3.3 Servis Ozellikleri
- Baslat / Durdur / Yeniden Baslat
- Port kullanim durumu kontrolu (port carpisma tespiti)
- PID izleme
- Log goruntuleme (son N satir)
- Otomatik baslama ayari (Windows ile birlikte)
- Ozel konfigurasyon dosyasi duzenleme

### 3.4 Ortam Degiskeni Yonetimi (Environment & PATH)

#### 3.4.1 PATH Yoneticisi
```
+----------------------------------------------------------+
|  PATH YONETIMI                                            |
+----------------------------------------------------------+
|  Degisken              | Deger           | Durum          |
|  --------------------- | --------------- | -------------- |
|  JAVA_HOME             | C:\jdk-21       | ✅ Dogru       |
|  ANDROID_HOME          | (tanimlanmamis) | ❌ Eksik       |
|  FLUTTER_HOME          | C:\flutter      | ✅ Dogru       |
|  GOPATH                | (tanimlanmamis) | ⚠️ Opsiyonel   |
|  PYTHON_HOME           | C:\Python312    | ✅ Dogru       |
|  NODE_PATH              | (tanimlanmamis) | ⚠️ Opsiyonel   |
+----------------------------------------------------------+
|  System PATH Girisleri:                                    |
|  [✅] C:\Program Files\nodejs\                             |
|  [✅] C:\Python312\                                        |
|  [❌] C:\flutter\bin\ (EKSIK - [Ekle] dugmesi)            |
|  [✅] C:\Program Files\Git\cmd\                            |
|  [❌] C:\Android\Sdk\platform-tools\ (EKSIK - [Ekle])     |
+----------------------------------------------------------+
|  [🔧 Eksik PATH'leri Otomatik Ekle]  [🔄 Tazele]         |
+----------------------------------------------------------+
```

#### 3.4.2 Otomatik PATH Duzeltme
- Eksik PATH girisi tespiti
- Tek tikla ekleme
- Toplu duzeltme secenegi
- PATH degisikliklerinin aninda uygulanmasi (broadcast WM_SETTINGCHANGE)
- Degisiklik oncesi yedekleme

#### 3.4.3 Ortam Degiskeni Islemleri
- JAVA_HOME, ANDROID_HOME, FLUTTER_HOME, GOPATH, PYTHON_HOME vb. kontrolu
- Eksik degiskenlerin otomatik tanimlanmasi
- Yanlis deger tespiti ve duzeltme onerisi
- User vs System degisken ayrimi

### 3.5 Saglik Taramasi Modulu (Health Check)

#### 3.5.1 Genel Sistem Taramasi
```
+----------------------------------------------------------+
|  SAGLIK TARAMASI RAPORU                                   |
+----------------------------------------------------------+
|                                                           |
|  [===========================] %100 Tarama Tamamlandi     |
|                                                           |
|  ✅ BASARILI (18)                                         |
|  ├── Node.js v20.11.0 - PATH dogru                       |
|  ├── npm v10.2.0 - Calisiyor                             |
|  ├── Git v2.43.0 - PATH dogru                            |
|  ├── Python v3.12.1 - PATH dogru                         |
|  ├── MongoDB v7.0 - Servis aktif, port 27017 acik        |
|  └── ... (daha fazla goster)                              |
|                                                           |
|  ⚠️ UYARI (3)                                            |
|  ├── Redis - Guncel degil (7.0 → 7.2 mevcut)            |
|  ├── Docker - 2GB+ disk kullanimi                         |
|  └── npm - Global paketlerde uyumsuzluk                  |
|                                                           |
|  ❌ HATA (2)                                              |
|  ├── Flutter - ANDROID_HOME tanimli degil                 |
|  │   [Otomatik Duzelt] [Manuel Bilgi]                     |
|  ├── Android SDK - platform-tools PATH'te yok             |
|  │   [Otomatik Duzelt] [Manuel Bilgi]                     |
|  └──                                                      |
|                                                           |
|  [📋 Raporu Kaydet]  [🔧 Tum Hatalari Duzelt]           |
+----------------------------------------------------------+
```

#### 3.5.2 Araca Ozel Saglik Kontrolleri

**Flutter Doctor Entegrasyonu:**
- `flutter doctor` ciktisini parse etme
- Android SDK, Android Studio, VS Code, Chrome kontrolleri
- Lisans kabul durumu kontrolu
- Emulator kurulum kontrolu

**Android SDK Kontrolu:**
- SDK yolu dogrulama
- Platform-tools, build-tools, emulator kontrolleri
- SDK Manager uzerinden eksik bilesen tespiti
- ANDROID_HOME ve PATH kontrolu

**Node.js Ekosistem Kontrolu:**
- Node + npm versiyon uyumu
- Global paket listesi
- node_modules cakisma kontrolu
- npx erisim kontrolu

**Veritabani Baglanti Testi:**
- Her DB icin baglanti string ile test
- Port acik mi kontrolu
- Veri dizini erisilebilirlik kontrolu
- Temel CRUD test (opsiyonel)

### 3.6 Profil ve Preset Sistemi

#### 3.6.1 Hazir Profiller
| Profil | Icerik |
|--------|--------|
| **Web Frontend** | Node.js, npm/yarn, Git, VS Code, Chrome |
| **Web Backend (Node)** | Node.js, npm, NestJS CLI, MongoDB, Redis, Git, Postman |
| **Web Fullstack** | Node.js, npm, React, NestJS, MongoDB, Redis, PostgreSQL, Git, VS Code, Docker |
| **Mobil (Flutter)** | Flutter, Dart, Android Studio, Android SDK, JDK, Git, VS Code |
| **Mobil (React Native)** | Node.js, React Native CLI, Android Studio, Android SDK, JDK, Git, VS Code |
| **Data Science** | Python, pip, Jupyter, PostgreSQL, MongoDB, Git, VS Code |
| **DevOps** | Docker, Docker Compose, Git, GitHub CLI, WSL 2, Node.js |
| **Ozel** | Kullanicinin kendi sectigi araclar |

#### 3.6.2 Preset Islemleri
- Profil secince ilgili tum araclari siraya alip yukleme
- Profil disari aktarma (JSON/YAML)
- Profil iceri aktarma
- Egitmen profili paylasimi (sinif ortami icin)

### 3.7 Terminal / Log Paneli

#### 3.7.1 Canli Terminal Ciktisi
- Her islem icin canli PowerShell cikti gorunumu
- Renkli log (basari=yesil, hata=kirmizi, uyari=sari)
- Log filtreleme ve arama
- Log dosyasina kaydetme
- Kopyala/Yapistir destegi

### 3.8 Guncelleme ve Bildirim Sistemi
- Arac versiyonu karsilastirma (kurulu vs son surum)
- Guncelleme mevcut bildirimi
- Toplu guncelleme secenegi
- DevARI Manager kendini guncelleme (auto-update)

---

## 4. TEKNIK MIMARI

### 4.1 Teknoloji Secimi: C# / .NET 8 / WPF

**Neden C# + WPF?**
- Windows-native uygulama performansi
- System.Diagnostics.Process ile tam PowerShell/CMD entegrasyonu
- Windows Registry, Environment, Service API'lerine dogrudan erisim
- WPF ile modern, ozellestirilabilir UI (Material Design)
- MVVM pattern ile temiz mimari
- Tek exe dagitim imkani (self-contained)
- Admin yetkileri yonetimi (UAC)

### 4.2 Proje Yapisi
```
DevARIManager/
├── src/
│   ├── DevARIManager.App/                 # WPF Uygulama (Entry Point)
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml
│   │   ├── Views/
│   │   │   ├── DashboardView.xaml
│   │   │   ├── ToolsView.xaml
│   │   │   ├── ServicesView.xaml
│   │   │   ├── EnvironmentView.xaml
│   │   │   ├── HealthCheckView.xaml
│   │   │   ├── ProfilesView.xaml
│   │   │   ├── SettingsView.xaml
│   │   │   └── TerminalView.xaml
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   ├── DashboardViewModel.cs
│   │   │   ├── ToolsViewModel.cs
│   │   │   ├── ServicesViewModel.cs
│   │   │   ├── EnvironmentViewModel.cs
│   │   │   ├── HealthCheckViewModel.cs
│   │   │   ├── ProfilesViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Controls/
│   │   │   ├── ToolCard.xaml
│   │   │   ├── ServiceRow.xaml
│   │   │   ├── PathEntry.xaml
│   │   │   └── HealthItem.xaml
│   │   ├── Converters/
│   │   ├── Themes/
│   │   │   ├── LightTheme.xaml
│   │   │   └── DarkTheme.xaml
│   │   └── Resources/
│   │       └── Icons/
│   │
│   ├── DevARIManager.Core/               # Is Mantigi Katmani
│   │   ├── Models/
│   │   │   ├── ToolDefinition.cs
│   │   │   ├── ToolStatus.cs
│   │   │   ├── ServiceInfo.cs
│   │   │   ├── EnvironmentVariable.cs
│   │   │   ├── HealthCheckResult.cs
│   │   │   └── Profile.cs
│   │   ├── Services/
│   │   │   ├── IToolManager.cs
│   │   │   ├── ToolManager.cs
│   │   │   ├── IServiceManager.cs
│   │   │   ├── ServiceManager.cs
│   │   │   ├── IEnvironmentManager.cs
│   │   │   ├── EnvironmentManager.cs
│   │   │   ├── IHealthChecker.cs
│   │   │   ├── HealthChecker.cs
│   │   │   ├── IProcessRunner.cs
│   │   │   ├── ProcessRunner.cs
│   │   │   ├── IProfileManager.cs
│   │   │   └── ProfileManager.cs
│   │   ├── ToolDefinitions/
│   │   │   ├── NodeJsDefinition.cs
│   │   │   ├── PythonDefinition.cs
│   │   │   ├── MongoDbDefinition.cs
│   │   │   ├── RedisDefinition.cs
│   │   │   ├── FlutterDefinition.cs
│   │   │   ├── AndroidSdkDefinition.cs
│   │   │   ├── DockerDefinition.cs
│   │   │   ├── GitDefinition.cs
│   │   │   └── ... (her arac icin)
│   │   └── Helpers/
│   │       ├── RegistryHelper.cs
│   │       ├── PathHelper.cs
│   │       ├── DownloadHelper.cs
│   │       ├── VersionHelper.cs
│   │       └── AdminHelper.cs
│   │
│   └── DevARIManager.Tests/              # Unit & Integration Testler
│       ├── ToolManagerTests.cs
│       ├── ServiceManagerTests.cs
│       ├── EnvironmentManagerTests.cs
│       └── HealthCheckerTests.cs
│
├── assets/
│   ├── icons/
│   ├── images/
│   └── installer/
│
├── profiles/                              # Hazir Profil Dosyalari
│   ├── web-frontend.json
│   ├── web-backend-node.json
│   ├── web-fullstack.json
│   ├── mobile-flutter.json
│   ├── mobile-reactnative.json
│   ├── data-science.json
│   └── devops.json
│
├── scripts/                               # PowerShell Yardimci Scriptleri
│   ├── install/
│   │   ├── Install-NodeJs.ps1
│   │   ├── Install-Python.ps1
│   │   ├── Install-MongoDB.ps1
│   │   └── ...
│   ├── uninstall/
│   ├── health/
│   └── utils/
│
├── docs/
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   └── USER_GUIDE.md
│
├── DevARIManager.sln
├── README.md
└── .gitignore
```

### 4.3 Katmanli Mimari
```
┌─────────────────────────────────────────────┐
│              WPF UI (XAML + Views)           │  Sunum Katmani
├─────────────────────────────────────────────┤
│           ViewModels (MVVM Pattern)          │  ViewModel Katmani
├─────────────────────────────────────────────┤
│         Core Services (Is Mantigi)           │  Servis Katmani
│   ToolManager | ServiceManager | HealthCheck │
├─────────────────────────────────────────────┤
│            Process Runner Layer              │  Altyapi Katmani
│   PowerShell | CMD | winget | Chocolatey     │
├─────────────────────────────────────────────┤
│          Windows OS APIs                     │  Isletim Sistemi
│   Registry | Services | Environment | WMI    │
└─────────────────────────────────────────────┘
```

### 4.4 Temel Sinif Yapisi

**ToolDefinition (Her Arac Icin Tanim):**
```csharp
public class ToolDefinition
{
    public string Id { get; set; }                    // "nodejs"
    public string DisplayName { get; set; }           // "Node.js"
    public string Category { get; set; }              // "Runtime"
    public string IconPath { get; set; }              // Ikon dosya yolu
    public string Description { get; set; }           // Arac aciklamasi
    public string[] AvailableVersions { get; set; }   // Yuklenebilir versiyonlar
    public string VersionCheckCommand { get; set; }   // "node --version"
    public string[] RequiredPaths { get; set; }       // PATH'te olmasi gereken dizinler
    public string[] EnvironmentVariables { get; set; } // Gerekli env vars
    public string[] Dependencies { get; set; }         // Bagimliliklari ["npm"]
    public InstallMethod InstallMethod { get; set; }  // winget/choco/direct/custom
    public string WingetId { get; set; }              // "OpenJS.NodeJS.LTS"
    public string ChocoId { get; set; }               // "nodejs-lts"
    public string DirectDownloadUrl { get; set; }     // Dogrudan indirme URL
    public string SilentInstallArgs { get; set; }     // Sessiz kurulum parametreleri
    public bool IsService { get; set; }               // Servis olarak calisabilir mi
    public ServiceDefinition ServiceDef { get; set; } // Servis detaylari
}
```

### 4.5 Kurulum Stratejisi (Install Strategy)
```
Oncelik Sirasi:
1. winget (Microsoft resmi, en guvenilir)
2. Chocolatey (genis paket deposu)
3. Scoop (portable uygulamalar)
4. Dogrudan indirme + sessiz kurulum (fallback)
5. Ozel PowerShell script (karmasik kurulumlar)
```

### 4.6 Admin Yetki Yonetimi
- Uygulama baslangicta admin yetkisi ister (UAC prompt)
- PATH ve ortam degiskeni islemleri admin gerektirir
- Servis yonetimi admin gerektirir
- Kurulum/kaldirma islemleri admin gerektirir
- Manifest dosyasinda `requireAdministrator` ayari

---

## 5. KULLANICI DENEYIMI (UX) AKISLARI

### 5.1 Ilk Kullanim Akisi
```
1. Uygulama ilk kez acilir
2. Karsilama ekrani → Profil secimi (ornegin "Web Fullstack")
3. Secilen profilin araclar listesi gosterilir
4. Kullanici onaylar → Toplu kurulum baslar
5. Ilerleme cubugu ile tum araclar sirayla yuklenir
6. Kurulum tamamlaninca saglik taramasi otomatik calisir
7. Sonuc raporu gosterilir
8. Dashboard'a yonlendirilir
```

### 5.2 Gunluk Kullanim Akisi
```
1. Uygulama acilir → Dashboard
2. Calismak istedigi teknoloji icin servisleri baslatir
   (ornegin: MongoDB + Redis icin tek tik)
3. Gelistirme yapar
4. Isleri bitince servisleri durdurur
```

### 5.3 Sorun Giderme Akisi
```
1. Bir sey calismiyor → Saglik Taramasi calistir
2. Sorunlar listesi gosterilir
3. Her sorun icin "Otomatik Duzelt" secenegi
4. Tek tikla duzeltme veya adim adim kilavuz
```

---

## 6. GUVENLIK GEREKSINIMLERI

- UAC yetki yukseltme (admin islemleri icin)
- Sadece resmi kaynaklardan indirme (checksum dogrulama)
- PATH degisikliklerinden once yedekleme
- Hassas bilgi loglanmamali
- Windows Defender uyumlulugu (false positive onleme)
- Dijital imzali installer (code signing)

---

## 7. PERFORMANS GEREKSINIMLERI

- Uygulama acilis suresi: < 3 saniye
- Saglik taramasi: < 30 saniye (tum araclar)
- Servis baslat/durdur: < 5 saniye
- Arayuz donmamali (async/await pattern zorunlu)
- Bellek kullanimi: < 150 MB idle durumda
- Arkaplan islemleri UI'i bloklamamali

---

## 8. DAGITIM VE KURULUM

### 8.1 Installer
- **Inno Setup** veya **WiX Toolset** ile MSI/EXE installer
- Sessiz kurulum destegi (`/silent /verysilent`)
- Masaustu kisayolu olusturma
- Baslat menusu entegrasyonu
- Program ekle/kaldir desteği

### 8.2 Sistem Gereksinimleri
| Gereksinim | Minimum |
|------------|---------|
| OS | Windows 10 64-bit (1809+) |
| RAM | 4 GB |
| Disk | 500 MB (uygulama) + araclar icin ek alan |
| .NET | .NET 8 Runtime (self-contained secenegi) |
| Internet | Kurulum islemleri icin gerekli |
| Yetki | Administrator |

---

## 9. GELISTIRME FAZI VE YONETIMI

### Faz 1 - Temel Altyapi (Hafta 1-2)
- [ ] Proje yapisini olustur (.NET 8 + WPF)
- [ ] MVVM altyapisini kur (CommunityToolkit.Mvvm)
- [ ] ProcessRunner servisi (PowerShell calistirma)
- [ ] Temel UI iskelet (Navigation, Dashboard)
- [ ] ToolDefinition modeli ve JSON tabanli konfigurasyon
- [ ] Admin yetki yonetimi

### Faz 2 - Arac Yonetimi (Hafta 3-4)
- [ ] winget/Chocolatey entegrasyonu
- [ ] ToolManager servisi (Install/Uninstall/Update)
- [ ] Versiyon kontrol mekanizmasi
- [ ] Arac kartlari UI (ToolCard)
- [ ] Indirme ve kurulum ilerleme cubugu
- [ ] Ilk 10 arac tanimi (Node, Python, Git, MongoDB, Redis, Flutter, Docker, VS Code, JDK, PostgreSQL)

### Faz 3 - Servis Yonetimi (Hafta 5)
- [ ] ServiceManager servisi
- [ ] Windows Service entegrasyonu
- [ ] Process yonetimi (start/stop/restart)
- [ ] Port kontrol mekanizmasi
- [ ] Servis UI paneli
- [ ] Log goruntuleme

### Faz 4 - Ortam ve PATH (Hafta 6)
- [ ] EnvironmentManager servisi
- [ ] PATH okuma/yazma/duzeltme
- [ ] Ortam degiskeni yonetimi
- [ ] Otomatik duzeltme mekanizmasi
- [ ] WM_SETTINGCHANGE broadcast
- [ ] Ortam UI paneli

### Faz 5 - Saglik Taramasi (Hafta 7)
- [ ] HealthChecker servisi
- [ ] Her arac icin saglik kontrol kurallari
- [ ] Flutter doctor entegrasyonu
- [ ] Android SDK kontrolleri
- [ ] Baglanti testleri (DB'ler icin)
- [ ] Saglik raporu UI

### Faz 6 - Profil ve Preset (Hafta 8)
- [ ] ProfileManager servisi
- [ ] Hazir profil JSON dosyalari
- [ ] Profil secim UI
- [ ] Toplu kurulum akisi
- [ ] Profil disari/iceri aktarma

### Faz 7 - Cilalama ve Dagitim (Hafta 9-10)
- [ ] Dark/Light tema
- [ ] Turkce/Ingilizce dil destegi
- [ ] Hata yonetimi ve loglama
- [ ] Installer olusturma (Inno Setup)
- [ ] Performans optimizasyonu
- [ ] Beta test ve hata duzeltme
- [ ] Dokumantasyon
- [ ] v1.0 Release

---

## 10. BASARI METRIKLERI

| Metrik | Hedef |
|--------|-------|
| Ilk kurulum suresi (fullstack profil) | < 15 dakika |
| Saglik taramasi suresi | < 30 saniye |
| PATH duzeltme suresi | < 5 saniye |
| Servis baslat suresi | < 5 saniye |
| Ogrenci memnuniyeti | > %90 |
| Kurulum basari orani | > %95 |
| Desteklenen arac sayisi (v1.0) | 30+ |

---

## 11. GELECEK VIZYONU (v2.0+)

- **Egitmen Paneli:** Sinif bazli kurulum takibi
- **Uzaktan Profil Paylasimi:** Cloud uzerinden profil sync
- **Plugin Sistemi:** Topluluk tarafindan yeni arac tanimi ekleme
- **WSL 2 Entegrasyonu:** Linux araclarini da yonetme
- **CI/CD Pipeline Kurulumu:** GitHub Actions / GitLab CI sablonlari
- **Proje Sablonlari:** Boilerplate proje olusturma (create-app)
- **Performans Izleme:** RAM/CPU/Disk kullanimi izleme
- **macOS Destegi:** Cross-platform genisleme (.NET MAUI)

---

## 12. RISKLER VE AZALTMA STRATEJILERI

| Risk | Etki | Azaltma |
|------|------|---------|
| Antivirus false positive | Yuksek | Code signing sertifikasi |
| winget/choco versiyon uyumsuzlugu | Orta | Fallback mekanizmasi (alternatif kurulum yolu) |
| Admin yetkisi reddedilmesi | Yuksek | Kullaniciya net aciklama, non-admin mod (kisitli) |
| Arac URL degisikligi | Orta | JSON config ile dinamik URL yonetimi |
| Windows Update sonrasi sorun | Orta | Saglik taramasi ile erken tespit |
| Buyuk indirme boyutlari | Dusuk | Paralel indirme, ozet gosterim |

---

*Bu dokuman DevARI Manager projesinin temel gereksinimlerini tanimlar. Gelistirme sureci boyunca guncellenecektir.*
