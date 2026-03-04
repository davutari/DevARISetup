using System.Text;

namespace DevARIManager.Linux.Shared;

public static class LinuxToolRegistry
{
    public static IReadOnlyList<LinuxToolDefinition> All { get; } =
    [
        new("git", "Git", "apt", "command -v git >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y git", "sudo apt-get remove -y git"),
        new("nodejs", "Node.js", "apt", "command -v node >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y nodejs npm", "sudo apt-get remove -y nodejs npm"),
        new("python3", "Python 3", "apt", "command -v python3 >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y python3 python3-pip python3-venv", "sudo apt-get remove -y python3 python3-pip python3-venv"),
        new("docker", "Docker", "apt", "command -v docker >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y docker.io docker-compose-v2", "sudo apt-get remove -y docker.io docker-compose-v2", "docker"),
        new("dotnet8", ".NET SDK 8", "apt", "dotnet --list-sdks 2>/dev/null | grep -q '^8\\.'", "sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0", "sudo apt-get remove -y dotnet-sdk-8.0"),
        new("openjdk17", "OpenJDK 17", "apt", "java -version 2>&1 | grep -q '17\\.'", "sudo apt-get update && sudo apt-get install -y openjdk-17-jdk", "sudo apt-get remove -y openjdk-17-jdk"),
        new("go", "Go", "apt", "command -v go >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y golang", "sudo apt-get remove -y golang"),
        new("rust", "Rust", "rustup", "command -v rustc >/dev/null 2>&1", "curl https://sh.rustup.rs -sSf | sh -s -- -y", "rustup self uninstall -y"),
        new("vscode", "VS Code", "snap", "command -v code >/dev/null 2>&1", "sudo snap install code --classic", "sudo snap remove code"),
        new("postgresql", "PostgreSQL", "apt", "command -v psql >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y postgresql postgresql-contrib", "sudo apt-get remove -y postgresql postgresql-contrib", "postgresql"),
        new("mysql", "MySQL", "apt", "command -v mysql >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y mysql-server", "sudo apt-get remove -y mysql-server", "mysql"),
        new("mongodb", "MongoDB", "apt", "command -v mongod >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y mongodb", "sudo apt-get remove -y mongodb", "mongod"),
        new("redis", "Redis", "apt", "command -v redis-server >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y redis-server", "sudo apt-get remove -y redis-server", "redis-server"),
        new("nginx", "Nginx", "apt", "command -v nginx >/dev/null 2>&1", "sudo apt-get update && sudo apt-get install -y nginx", "sudo apt-get remove -y nginx", "nginx"),
        new("flutter", "Flutter", "snap", "command -v flutter >/dev/null 2>&1", "sudo snap install flutter --classic", "sudo snap remove flutter")
    ];

    public static string EscapeForDoubleQuotedBash(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '\\' or '"' or '$' or '`')
            {
                builder.Append('\\');
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
