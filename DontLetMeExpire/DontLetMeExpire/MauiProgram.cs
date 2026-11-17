using Azure.AI.OpenAI;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using DontLetMeExpire.AI;
using DontLetMeExpire.OpenFoodFacts;
using DontLetMeExpire.Services;
using DontLetMeExpire.ViewModels;
using DontLetMeExpire.Views;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using ZXing.Net.Maui.Controls;

namespace DontLetMeExpire
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
              .UseMauiApp<App>()
              .ConfigureSyncfusionToolkit()
              .UseMauiCommunityToolkit()
              .UseBarcodeReader() // ZXing.Net.MAUI
              .ConfigureFonts(fonts =>
              {
                  fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                  fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                  fonts.AddFont("MaterialSymbols-Rounded.ttf", "MaterialSymbolsRounded");
              });

#if DEBUG
            builder.Logging.AddDebug();
#endif
#if WINDOWS
      // DatePicker unter Windows: Erster Tag der Woche anpassen
      Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("FixFirstDayOfWeek", (handler, view) =>
      {
        handler.PlatformView.FirstDayOfWeek = (Windows.Globalization.DayOfWeek)(int)
            System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
      });
#endif

            //    builder.Services.AddSingleton<IStorageLocationService, DummyStorageLocationService>();
            //    builder.Services.AddSingleton<IItemService, DummyItemService>();
            builder.Services.AddSingleton<IStorageLocationService, SqliteStorageLocationService>();
            builder.Services.AddSingleton<IItemService, SqliteItemService>();
            builder.Services.AddSingleton<IAlertService, AlertService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IPreferences>(Preferences.Default);
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IOpenFoodFactsApiClient, OpenFoodFactsApiClient>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ItemViewModel>();
            builder.Services.AddTransient<ItemPage>();
            builder.Services.AddTransient<ItemsViewModel>();
            builder.Services.AddTransient<ItemsPage>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();

            // Aspire Service Defaults: OpenTelemetry (Logs, Metriken, Traces) an das Aspire-Dashboard.
            // Ohne Aspire ist OTEL_EXPORTER_OTLP_ENDPOINT nicht gesetzt, dann wird nichts exportiert.
            builder.AddServiceDefaults();

            // Endpoint und Key kommen aus Aspire oder aus dem Build (siehe AiSettings).
            // Eigenes Modell-Deployment anlegen unter https://ai.azure.com/
            if (!AiSettings.IsConfigured)
            {
                System.Diagnostics.Debug.WriteLine("KI ist nicht konfiguriert: AI_Foundry_Endpoint/AI_Foundry_ApiKey fehlen.");
            }

            // Cloud: Azure OpenAI in AI Foundry
            builder.Services.AddKeyedChatClient(AiClientProvider.CloudKey, _ =>
                {
                    var endpoint = AiSettings.IsConfigured ? AiSettings.Endpoint : "https://ai-not-configured.invalid/";
                    var foundryClient = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(AiSettings.IsConfigured ? AiSettings.ApiKey : "missing"));
                    return foundryClient.GetChatClient(AiSettings.Model).AsIChatClient();
                })
                // Reihenfolge = von außen nach innen:
                // 1. führt Tool-Aufrufe des Modells aus und schickt die Ergebnisse zurück,
                // 2. meldet jeden Modell- und Tool-Aufruf an OpenTelemetry (Prompts sichtbar: nur für die Demo!),
                // 3. schreibt ins Log,
                // 4. nimmt Antworten auf und spielt sie bei Netzproblemen ab (Einstellungen > KI-Antworten).
                .UseFunctionInvocation()
                .UseOpenTelemetry(sourceName: AiTelemetry.SourceName, configure: o => o.EnableSensitiveData = true)
                .UseLogging()
                .Use((inner, services) => new ReplayChatClient(inner, services.GetRequiredService<ReplayStore>()));

#if IOS || MACCATALYST
            // Lokal: Apple Intelligence auf dem Gerät (iOS/macOS 26+). Gleiche Schnittstelle, gleiche Middleware.
            // Ob das Modell bereit ist und Bilder versteht, prüft der AiClientProvider zur Laufzeit.
            if (AiClientProvider.IsLocalSupportedOnPlatform)
            {
#pragma warning disable MAUIAI0001 // Preview-API
                builder.Services.AddKeyedChatClient(AiClientProvider.LocalKey,
                        sp => AiClientProvider.IsLocalSupportedOnPlatform
                            ? new Microsoft.Maui.Essentials.AI.AppleIntelligenceChatClient(sp.GetRequiredService<ILoggerFactory>(), sp)
                            : throw new PlatformNotSupportedException("Apple Intelligence braucht iOS/macOS 26 oder neuer."))
                    .UseFunctionInvocation()
                    .UseOpenTelemetry(sourceName: AiTelemetry.SourceName, configure: o => o.EnableSensitiveData = true)
                    .UseLogging();
#pragma warning restore MAUIAI0001
            }
#endif
            builder.Services.AddSingleton<AiClientProvider>();
            builder.Services.AddSingleton<ReplayStore>();
            builder.Services.AddSingleton<ToolCallLog>();

            // Akt 1: Erfassung per Foto(s) und Sprache
            builder.Services.AddSingleton<ItemRecognitionTools>();
            builder.Services.AddSingleton<ItemRecognitionService>();
            builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
            builder.Services.AddSingleton<VoiceInputService>();

            // Akt 2: Vorrats-Chat mit Tools (ViewModel als Singleton, damit der Chat beim Seitenwechsel erhalten bleibt)
            builder.Services.AddSingleton<PantryTools>();
            builder.Services.AddSingleton<ChatViewModel>();
            builder.Services.AddTransient<ChatPage>();

            // Akt 3: Rezept mit "Gekocht"
            builder.Services.AddSingleton<RecipeService>();

            // Lebensmittel-Chat ("Kann ich das noch essen?") und "Geöffnet"
            builder.Services.AddSingleton<ItemAdvisorService>();
            builder.Services.AddTransient<ItemChatViewModel>();
            builder.Services.AddTransient<ItemChatPage>();

            return builder.Build();
        }
    }
}
