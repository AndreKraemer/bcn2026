# Neuigkeiten bei der Cross-Platform-Entwicklung mit .NET MAUI 11.0

Demo-Code zum gleichnamigen Vortrag von **André Krämer** (Microsoft MVP) auf der
[**betterCode() .NET 11.0**](https://net.bettercode.eu/), der Online-Konferenz von heise und
IT-Visions.de zum neuen .NET-Release, am **17. November 2026, 15:45–16:30 Uhr**
([Vortragsseite](https://net.bettercode.eu/veranstaltung-89627-se-0-neuigkeiten-bei-der-cross-platform-entwicklung-mit-.net-maui-11.0.html)).

> Mit .NET MAUI 11 stehen Verbesserungen im Vordergrund, die man im Alltag sofort merkt:
> XAML wird beim Build zu C#, die .NET CLI kann jetzt Geräte, CoreCLR ersetzt Mono als
> Runtime, und mit Microsoft.Maui.Essentials.AI und Microsoft.Extensions.AI laufen
> KI-Funktionen direkt auf dem Gerät.
>
> **Mobile ist keine Sonderwelt mehr. Es ist einfach .NET.**

---

## 🗺️ Agenda und Demos

| # | Thema | Inhalt | Demo |
| --- | --- | --- | --- |
| 01 | **XAML** | Source Generator, implizite und globale Namespaces, C#-Ausdrücke, `x:Code` | `MauiAppDemoNet10*`, `MauiAppDemoNet11`, `MauiAppDemoNet11Xaml` |
| 02 | **Tooling** | `dotnet run/watch/test`, MAUI CLI, MAUI Sherpa, MAUI DevFlow | `MauiDevFlowDemoApp` |
| 03 | **Runtime** | CoreCLR als Standard, Performance, Migration | alle .NET 11 Demos |
| 04 | **Plattformen** | neue Backends aus MAUI Labs, Material 3 und weitere Neuerungen | `MauiAppMaterial3Demo` |
| 05 | **On-Device-KI** | Microsoft.Maui.Essentials.AI und Microsoft.Extensions.AI | `DontLetMeExpire` |

---

## 📦 Die Beispielprojekte

Jedes Projekt ist eine eigenständige Solution (`.slnx`) und lässt sich unabhängig öffnen.

### 01 · XAML: wird beim Build zu C#

In .NET 11 ist der **XAML Source Generator** Standard, in Debug **und** Release. Zum Vergleich
liegt dieselbe App mehrfach auf .NET 10 bei, wo Source Generation noch ein Opt-in ist.

| Projekt | Beschreibung |
| --- | --- |
| [`MauiAppDemoNet10`](MauiAppDemoNet10/) | .NET 10 mit aktivierter XAML Source Generation (`<MauiXamlInflator>SourceGen</MauiXamlInflator>`). |
| [`MauiAppDemoNet10SourceGenDebug`](MauiAppDemoNet10SourceGenDebug/) | Dieselbe App **mit** Source Generation, Vergleichsbasis für das Verhalten im Debugger. |
| [`MauiAppDemoNet10NoSourceGenDebug`](MauiAppDemoNet10NoSourceGenDebug/) | Dieselbe App **ohne** Source Generation (Runtime Inflation im Debug). |
| [`MauiAppDemoNet10NoSourceGenRelease`](MauiAppDemoNet10NoSourceGenRelease/) | Vergleichsprojekt ohne Source Generation für den Release-Pfad (XamlC). |
| [`MauiAppDemoNet10Xaml`](MauiAppDemoNet10Xaml/) | Implizite und globale XML-Namespaces sowie C#-Ausdrücke in XAML, in .NET 10 nur per Opt-in (`MauiAllowImplicitXmlnsDeclaration` + `EnablePreviewFeatures`). |
| [`MauiAppDemoNet11`](MauiAppDemoNet11/) | Dieselbe App auf .NET 11. Source Generation ist Standard, im Projekt ist dafür nichts zu konfigurieren. |
| [`MauiAppDemoNet11Xaml`](MauiAppDemoNet11Xaml/) | **XAML in .NET MAUI 11**: implizite Namespaces, globale Namespaces (`GlobalXmlNs.cs`) und C#-Ausdrücke wie `{!IsBusy}`, `{Tasks.Count == 1 ? '1 Task' : $'{Tasks.Count} Tasks'}` oder Event-Lambdas `{(s, e) => …}`. Braucht `EnablePreviewFeatures`. |

> 💡 Den Effekt von Source Generation zeigt man am besten, indem man dieselbe App mit und
> ohne Source Generation baut und debuggt. Mit `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`
> landet der generierte C#-Code unter `obj/` und lässt sich ansehen.

> ⚠️ **`x:Code`** (C#-Codeblöcke direkt in XAML) ist in `MauiAppDemoNet11Xaml` auskommentiert.
> Mit MAUI 11.0.0 und 11.0.10 bricht der Build mit `MAUIG1001` ab
> ([dotnet/maui#40328](https://github.com/dotnet/maui/issues/40328), minimales Repro:
> [AndreKraemer/MauiXCodeSourceGenRepro](https://github.com/AndreKraemer/MauiXCodeSourceGenRepro)).
> Workaround: Event-Handler in die `.xaml.cs`.

### 02 · Tooling: die .NET CLI kann jetzt Geräte

| Projekt | Beschreibung |
| --- | --- |
| [`MauiDevFlowDemoApp`](MauiDevFlowDemoApp/) | Minimale App mit dem **MAUI DevFlow Agent** (`Microsoft.Maui.DevFlow.Agent`, nur im Debug). Damit kann ein KI-Agent (Copilot, Claude, Codex, …) über `maui devflow mcp` den Visual Tree lesen, Controls antippen, Logs streamen und mehr. |

Gezeigt werden außerdem:

- **`dotnet run` / `dotnet watch` / `dotnet test`** direkt auf Gerät, Emulator und Simulator
- die **MAUI CLI** (experimentell, [dotnet/maui-labs](https://github.com/dotnet/maui-labs)):
  ```bash
  dotnet tool install -g Microsoft.Maui.Cli --prerelease
  maui doctor              # Umgebung prüfen und reparieren
  maui device list         # alle Geräte, alle Plattformen
  maui devflow mcp         # MCP-Server für KI-Agenten
  ```
- **[MAUI Sherpa](https://redth.github.io/MAUI.Sherpa)**: Community-Tool von Jonathan Dick
  für SDKs, Emulatoren, Zertifikate und Provisioning

### 03 · Runtime: CoreCLR statt Mono

Ein eigenes Demo-Projekt gibt es dafür nicht, denn **alle .NET 11 Demos laufen auf CoreCLR**.
Mono ist auf Android, iOS und Mac Catalyst Geschichte. `<UseMonoRuntime>true</UseMonoRuntime>`
wirkt in .NET 11 nicht mehr. Eigene Apps sollte man deshalb früh im Release-Build auf echten
Geräten prüfen (Startzeit, Speicher, App-Größe).

### 04 · Plattformen

| Projekt | Beschreibung |
| --- | --- |
| [`MauiAppMaterial3Demo`](MauiAppMaterial3Demo/) | **Material 3** auf Android per `<UseMaterial3>true</UseMaterial3>`. |

Die neuen experimentellen Backends (Linux/GTK 4, WPF, macOS/AppKit) liegen in
[dotnet/maui-labs](https://github.com/dotnet/maui-labs/tree/main/platforms).

### 05 · On-Device-KI

| Projekt | Beschreibung |
| --- | --- |
| [`DontLetMeExpire`](DontLetMeExpire/) | Lebensmittel-Verwaltung gegen Verschwendung, mit KI als App-Feature statt als Chatfenster. |

Was die App zeigt:

- **Erfassen**: Foto → fertiger Eintrag. Structured Output per `GetResponseAsync<ItemDraft>()`
- **Korrigieren**: Spracheingabe („Das sind zwei, ab in den Gefrierschrank“) über die
  Spracherkennung des Betriebssystems (CommunityToolkit.Maui)
- **Handeln**: Vorrats-Chat mit **Tool Calling**. Die vorhandenen Services der App werden
  zu Tools, Austragen nur nach Rückfrage
- **Erzeugen**: Rezept aus Resten, die ablaufen
- **Bewerten**: „Kann ich das noch essen?“ Das Modell liefert Daten, die Regeln stehen im Code
- **Ein Stack, egal wo das Modell läuft**: `IChatClient` aus Microsoft.Extensions.AI, wahlweise
  **Azure OpenAI / Foundry** (Cloud) oder **Apple Intelligence** über
  Microsoft.Maui.Essentials.AI (lokal, iOS/macOS 26+). `AiClientProvider` prüft Verfügbarkeit
  und Fähigkeiten des lokalen Modells und weicht sonst auf die Cloud aus.
- **.NET Aspire** (`DontLetMeExpire.AppHost`): startet die App pro Plattform aus dem Dashboard
  und zeigt Telemetrie (OpenTelemetry) der KI-Aufrufe
- Barcode-Scan (ZXing.Net.MAUI) mit OpenFoodFacts, Datenhaltung mit SQLite
- `ReplayChatClient`: nimmt KI-Antworten auf und spielt sie ab, falls auf der Bühne das Netz fehlt

---

## 🛠️ Voraussetzungen

- **.NET 11 SDK** (11.0.100 oder neuer) mit **.NET MAUI Workload** für alle .NET 11 Demos
  ```bash
  dotnet workload install maui
  ```
- **.NET 10 SDK** für die `MauiAppDemoNet10*`-Vergleichsprojekte und den Aspire AppHost
- **Visual Studio 2026** mit .NET MAUI Workload, optional **VS Code** mit der .NET MAUI Extension
- Für iOS/macOS-Targets: ein **Mac** mit aktueller Xcode-Version
- Für Apple Intelligence: ein unterstütztes Gerät mit **iOS/iPadOS/macOS 26+** und aktiviertem Modell
- Optional: **MAUI CLI** (siehe oben) und die **devtunnel-CLI** für Telemetrie von echten Geräten

> 📌 Die .NET 11 Demos verwenden `<MauiVersion>11.*</MauiVersion>`. Damit zieht jeder Restore
> automatisch die neueste stabile MAUI-11-Version von NuGet, unabhängig vom installierten Workload.

---

## 🚀 Erste Schritte

### MAUI-App bauen und starten

```bash
cd MauiAppDemoNet11Xaml/MauiAppDemoNet11Xaml
dotnet run -f net11.0-android                    # wählt Gerät/Emulator, streamt die Logs
# oder -f net11.0-windows10.0.19041.0 / net11.0-ios / net11.0-maccatalyst
dotnet watch -f net11.0-android                  # mit Hot Reload
```

In Visual Studio: Solution öffnen, Zielplattform wählen und mit **F5** starten.

### DontLetMeExpire mit Aspire starten

```bash
dotnet run --project DontLetMeExpire/DontLetMeExpire.AppHost
```

Im Aspire-Dashboard ist jede Plattform eine eigene Ressource und wird dort per Klick gestartet.

---

## 🔑 Konfiguration der KI-Demo (`DontLetMeExpire`)

Es werden **keine Schlüssel im Repository abgelegt**. Endpoint und Key für Azure AI Foundry /
Azure OpenAI kommen aus einer von zwei Quellen:

**1. Über Aspire (empfohlen)**: einmalig in den User Secrets des AppHost setzen:

```bash
cd DontLetMeExpire/DontLetMeExpire.AppHost
dotnet user-secrets set "Parameters:ai-endpoint" "https://<ressource>.openai.azure.com/"
dotnet user-secrets set "Parameters:ai-apikey"   "<key>"
```

Fehlen sie, fragt das Dashboard beim Start danach. Das Modell (`ai-model`, Standard `gpt-4o`)
lässt sich im Dashboard umstellen.

**2. Ohne Aspire**: über Umgebungsvariablen beim Build:

| Variable | Beschreibung |
| --- | --- |
| `AI_Foundry_Endpoint` | Endpoint deiner Azure-AI-/OpenAI-Ressource |
| `AI_Foundry_ApiKey` | Zugehöriger API-Key |
| `AI_Foundry_Model` | Name des Modell-Deployments (optional, Standard `gpt-4o`) |

> ⚠️ Ohne Aspire werden die Werte beim Build in eine generierte Klasse unter `obj/` geschrieben
> (`AiSecrets.g.cs`) und stecken damit in der gebauten App. Das ist nur für lokale
> Entwicklung gedacht. So gebaute Apps nicht weitergeben.

> ⚠️ Niemals echte API-Keys committen.

---

## 🧱 Stolpersteine beim Umstieg auf .NET MAUI 11

Aus der Praxis, beides ist mir beim Umstieg selbst passiert:

- **`StyleSheet` + XAML Source Generator**: `<StyleSheet Source="…css" />` kompiliert mit
  SourceGen nicht (CS0117, [dotnet/maui#40232](https://github.com/dotnet/maui/issues/40232)).
  Workaround: diese Datei mit XamlC übersetzen: `<MauiXaml Update="MainPage.xaml" Inflator="XamlC" />`
- **SQLite und `decimal`**: SQLite speichert Beträge als `REAL`. Der Cast nach `decimal` liefert
  unter CoreCLR `19.989999999999998` statt `19.99` wie unter Mono. Explizit runden oder Cent als
  `INTEGER` speichern.
- Weitere Breaking Changes: `Microsoft.Maui.Controls.Compatibility` ist entfallen, Android
  braucht mindestens API 24 (kein x86 mehr), mit Xcode 27 ist der UIScene-Lebenszyklus mit
  `SceneDelegate` Pflicht.

---

## 👤 Über den Sprecher

**André Krämer**, Geschäftsführer der [Quality Bytes GmbH](https://qualitybytes.de), Microsoft MVP seit 2013 und Partner
im Expertennetzwerk IT-Visions.de. Schwerpunkte: Legacy-Modernisierung, Business-Apps mit
Blazor und .NET MAUI, Dokumentenautomatisierung und KI im Entwicklungsprozess.
Autor des Buchs zu .NET MAUI im Hanser Verlag (2. Auflage).

🔗 [qualitybytes.de](https://qualitybytes.de) · [andrekraemer.de](https://andrekraemer.de) · [github.com/AndreKraemer](https://github.com/AndreKraemer)

---

## 📄 Lizenz

Die Beispiele dienen Demonstrationszwecken im Rahmen der betterCode() .NET 11.0 und dürfen frei
zum Lernen verwendet werden.
