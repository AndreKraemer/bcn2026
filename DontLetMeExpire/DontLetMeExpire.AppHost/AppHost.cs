var builder = DistributedApplication.CreateBuilder(args);

// Foundry-Endpoint und -Key fuer die KI-Funktionen der App.
// Einmalig in den User Secrets dieses Projekts setzen (auf dem Rechner, auf dem der AppHost laeuft):
//   dotnet user-secrets set "Parameters:ai-endpoint" "https://<ressource>.openai.azure.com/"
//   dotnet user-secrets set "Parameters:ai-apikey"   "<key>"
// Fehlen sie, fragt das Aspire-Dashboard beim Start danach.
var aiEndpoint = builder.AddParameter("ai-endpoint");
var aiApiKey = builder.AddParameter("ai-apikey", secret: true);
// Name des Modell-Deployments, im Dashboard umstellbar (z. B. für den Vergleich zweier Modelle)
var aiModel = builder.AddParameter("ai-model", "gpt-4o");

var app = builder.AddMauiProject("app", "../DontLetMeExpire/DontLetMeExpire.csproj");

// Jede Plattform ist eine eigene Ressource im Dashboard und wird dort per Klick gestartet.
// Der AppHost läuft unter Windows oder macOS. Was auf dem jeweiligen Rechner nicht geht,
// zeigt das Dashboard als "Unsupported" an: unter Windows iOS und Mac Catalyst, auf dem Mac Windows.
// Android geht auf beiden. Desktop-Köpfe und der Simulator erreichen das Dashboard direkt über localhost.
WithAi(app.AddWindowsDevice());
WithAi(app.AddMacCatalystDevice());
WithAi(app.AddiOSSimulator());

// Echte Geräte erreichen localhost des Rechners nicht. Ihre Telemetrie läuft über einen Dev Tunnel.
// Das braucht die devtunnel-CLI (Windows: winget install Microsoft.devtunnel, macOS: brew install --cask devtunnel)
// und einmalig "devtunnel user login". Ohne CLI laufen die Geräte trotzdem, nur ihre Telemetrie fehlt im Dashboard.
// Abschalten lässt sich der Tunnel auch bewusst: dotnet user-secrets set "Demo:UseDevTunnel" "false"
var useDevTunnel = !string.Equals(builder.Configuration["Demo:UseDevTunnel"], "false", StringComparison.OrdinalIgnoreCase)
                   && IsOnPath("devtunnel");
if (!useDevTunnel)
{
    Console.WriteLine("Hinweis: Kein Dev Tunnel (devtunnel-CLI fehlt oder Demo:UseDevTunnel=false). " +
                      "Echte Geräte laufen, senden aber keine Telemetrie ans Dashboard.");
}

var androidDevice = WithAi(app.AddAndroidDevice());
var iosDevice = WithAi(app.AddiOSDevice());
if (useDevTunnel)
{
    androidDevice.WithOtlpDevTunnel();
    iosDevice.WithOtlpDevTunnel();
}

builder.Build().Run();

// Prüft, ob ein Programm über PATH erreichbar ist (devtunnel bzw. devtunnel.exe).
static bool IsOnPath(string program)
{
    var names = OperatingSystem.IsWindows() ? new[] { program + ".exe", program + ".cmd" } : new[] { program };
    var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);

    // Homebrew legt Programme auf Apple Silicon unter /opt/homebrew/bin ab. Das fehlt im PATH,
    // wenn Rider oder Visual Studio Code nicht aus dem Terminal gestartet wurden.
    var extra = OperatingSystem.IsMacOS() ? new[] { "/opt/homebrew/bin", "/usr/local/bin" } : [];

    return paths.Concat(extra)
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Any(p => names.Any(n => File.Exists(Path.Combine(p, n))));
}

IResourceBuilder<T> WithAi<T>(IResourceBuilder<T> head) where T : IResourceWithEnvironment =>
    head.WithEnvironment("AI_Foundry_Endpoint", aiEndpoint)
        .WithEnvironment("AI_Foundry_ApiKey", aiApiKey)
        .WithEnvironment("AI_Foundry_Model", aiModel);
