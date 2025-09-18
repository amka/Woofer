global using static Woofer.Constants;

using Gio;
using GObject;
using Gtk;
using Microsoft.Extensions.Logging;
using Woofer.UI;
using File = System.IO.File;

namespace Woofer;

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly Adw.Application _app;
    private MainWindow? _mainWindow;
    
    public Application(ILogger<Application> logger)
    {
        _logger = logger;
        _app = Adw.Application.New(APP_ID, ApplicationFlags.DefaultFlags);

        LoadResources();
        InitializeStyles();

        _app.OnStartup += OnStartup;
        _app.OnActivate += OnActivate;
    }

    public void Run(string[] args)
    {
        // Run the application
        _app.RunWithSynchronizationContext(args);
    }
    
    /// <summary>
    /// Loads the necessary resources for the application.
    /// This function attempts to load resources from different locations based on the environment.
    /// If the application is running as a Flatpak, it loads resources from the Flatpak environment.
    /// Otherwise, it tries to load the resources from the program directory or standard system paths.
    /// </summary>
    private static void LoadResources()
    {
        var resourcePath = Environment.GetEnvironmentVariable("FLATPAK_ID") switch
        {
            null => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, $"{APP_ID}.gresource")),
            _ => RESOURCES_PATH,
        };

        if (!File.Exists(resourcePath)) return;

        var resource = Resource.Load(resourcePath);
        resource.Register();
    }

    private void InitializeStyles()
    {
        var cssProvider = CssProvider.New();
        cssProvider.LoadFromResource("/com/tenderowl/woofer/styles.css");
        var display = Gdk.Display.GetDefault();
        if (display == null) return;
        StyleContext.AddProviderForDisplay(display, cssProvider, Gtk.Constants.STYLE_PROVIDER_PRIORITY_APPLICATION);
    }

    private void OnActivate(Gio.Application application, EventArgs eventArgs)
    {
        // Create a new MainWindow and show it.
        // The application is passed to the MainWindow so that it can be used
        _mainWindow ??= new MainWindow((Adw.Application)application);
        _mainWindow.Present();
    }

    private void OnStartup(Gio.Application application, EventArgs eventArgs)
    {
        CreateAction("Quit", (_, _) => { _app.Quit(); }, ["<Ctrl>Q"]);
        CreateAction("About", (_, _) => { OnAboutAction(); });
        CreateAction("Preferences", (_, _) => { OnPreferencesAction(); }, ["<Ctrl>comma"]);
    }
    
    private void OnAboutAction()
    {
        var about = Adw.AboutWindow.New();
        about.TransientFor = _app.ActiveWindow;
        about.ApplicationName = "Woofer";
        about.ApplicationIcon = "com.tenderowl.woofer";
        about.DeveloperName = "TenderOwl";
        about.Version = "0.1.0";
        about.Developers = ["TenderOwl"];
        about.Copyright = "© 2025 TenderOwl";
        about.Present();
    }

    private void OnPreferencesAction() {
        _logger.LogInformation("app.preferences action activated");
    }

    private void CreateAction(string name, SignalHandler<SimpleAction, SimpleAction.ActivateSignalArgs> callback,
        string[]? shortcuts = null)
    {
        var lowerName = name.ToLowerInvariant();
        var actionItem = SimpleAction.New(lowerName, null);
        actionItem.OnActivate += callback;
        _app.AddAction(actionItem);
        
        if (shortcuts is { Length: > 0 })
        {
            _app.SetAccelsForAction($"app.{lowerName}", shortcuts);
        }
    }
}