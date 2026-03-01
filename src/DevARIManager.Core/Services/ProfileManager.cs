using DevARIManager.Core.Models;

namespace DevARIManager.Core.Services;

public interface IProfileManager
{
    IReadOnlyList<Profile> GetAllProfiles();
    Profile? GetProfile(string profileId);
}

public class ProfileManager : IProfileManager
{
    private readonly List<Profile> _profiles;

    public ProfileManager()
    {
        _profiles = InitializeProfiles();
    }

    public IReadOnlyList<Profile> GetAllProfiles() => _profiles.AsReadOnly();

    public Profile? GetProfile(string profileId) => _profiles.FirstOrDefault(p => p.Id == profileId);

    private static List<Profile> InitializeProfiles() =>
    [
        new()
        {
            Id = "web-frontend",
            Name = "Web Frontend",
            Description = "React, Angular veya Vue.js ile modern frontend geliştirme",
            IconGlyph = "LanguageHtml5",
            IsBuiltIn = true,
            ToolIds = ["nodejs", "npm", "yarn", "git", "vscode"]
        },
        new()
        {
            Id = "web-backend-node",
            Name = "Web Backend (Node.js)",
            Description = "NestJS veya Express ile backend API geliştirme",
            IconGlyph = "ServerNetwork",
            IsBuiltIn = true,
            ToolIds = ["nodejs", "npm", "nestjs-cli", "mongodb", "redis", "git", "postman", "vscode"]
        },
        new()
        {
            Id = "web-fullstack",
            Name = "Web Fullstack",
            Description = "Frontend + Backend + Database tam yığın geliştirme",
            IconGlyph = "LayersTriple",
            IsBuiltIn = true,
            ToolIds = ["nodejs", "npm", "yarn", "nestjs-cli", "mongodb", "redis", "postgresql", "git", "docker", "postman", "vscode"]
        },
        new()
        {
            Id = "mobile-flutter",
            Name = "Mobil (Flutter)",
            Description = "Flutter ile cross-platform mobil uygulama geliştirme",
            IconGlyph = "CellphoneLink",
            IsBuiltIn = true,
            ToolIds = ["flutter", "dart-sdk", "android-studio", "java-jdk", "git", "vscode"]
        },
        new()
        {
            Id = "mobile-react-native",
            Name = "Mobil (React Native)",
            Description = "React Native ile cross-platform mobil geliştirme",
            IconGlyph = "CellphoneLink",
            IsBuiltIn = true,
            ToolIds = ["nodejs", "npm", "expo-cli", "android-studio", "java-jdk", "git", "vscode"]
        },
        new()
        {
            Id = "devops",
            Name = "DevOps",
            Description = "Docker, Git ve CI/CD araçları ile DevOps workflow",
            IconGlyph = "CloudSync",
            IsBuiltIn = true,
            ToolIds = ["docker", "docker-compose", "git", "github-cli", "nodejs", "python", "vscode"]
        },
        new()
        {
            Id = "data-science",
            Name = "Data Science",
            Description = "Python ve veritabanları ile veri bilimi geliştirme",
            IconGlyph = "ChartBar",
            IsBuiltIn = true,
            ToolIds = ["python", "nodejs", "postgresql", "mongodb", "git", "vscode"]
        },
        new()
        {
            Id = "dotnet-developer",
            Name = ".NET Developer",
            Description = "C# ve .NET ile uygulama geliştirme",
            IconGlyph = "MicrosoftVisualStudio",
            IsBuiltIn = true,
            ToolIds = ["dotnet-sdk", "git", "docker", "postgresql", "redis", "vscode", "postman"]
        },
        new()
        {
            Id = "go-developer",
            Name = "Go Developer",
            Description = "Go ile backend ve sistem programlama",
            IconGlyph = "LanguageGo",
            IsBuiltIn = true,
            ToolIds = ["go", "git", "docker", "postgresql", "redis", "vscode", "postman"]
        },
        new()
        {
            Id = "php-developer",
            Name = "PHP Developer",
            Description = "PHP, Laravel ve Composer ile web backend geliştirme",
            IconGlyph = "LanguagePhp",
            IsBuiltIn = true,
            ToolIds = ["php", "composer", "laravel-cli", "mysql", "redis", "git", "vscode", "postman"]
        },
        new()
        {
            Id = "php-laragon",
            Name = "PHP (Laragon)",
            Description = "Laragon ile hazır PHP geliştirme ortamı (Apache, MySQL, PHP hepsi dahil)",
            IconGlyph = "PackageVariant",
            IsBuiltIn = true,
            ToolIds = ["laragon", "composer", "git", "vscode"]
        },
        new()
        {
            Id = "cpp-developer",
            Name = "C/C++ Developer",
            Description = "MinGW (GCC) ve CMake ile C/C++ geliştirme",
            IconGlyph = "LanguageCpp",
            IsBuiltIn = true,
            ToolIds = ["mingw-gcc", "cmake", "git", "vscode"]
        },
        new()
        {
            Id = "aspnet-developer",
            Name = "ASP.NET Developer",
            Description = "ASP.NET Core, Entity Framework ve SQL veritabanları ile web geliştirme",
            IconGlyph = "MicrosoftVisualStudio",
            IsBuiltIn = true,
            ToolIds = ["dotnet-sdk", "dotnet-ef", "postgresql", "redis", "docker", "git", "postman", "vscode"]
        },
        new()
        {
            Id = "kotlin-android",
            Name = "Kotlin (Android)",
            Description = "Kotlin ile native Android uygulama geliştirme",
            IconGlyph = "LanguageKotlin",
            IsBuiltIn = true,
            ToolIds = ["kotlin", "java-jdk", "android-studio", "git", "vscode"]
        }
    ];
}
