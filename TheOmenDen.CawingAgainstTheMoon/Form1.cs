using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using Blazored.LocalStorage;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.Bootstrap;
using Blazorise.LoadingIndicator;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Form = System.Windows.Forms.Form;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.IO;

namespace TheOmenDen.CawingAgainstTheMoon;

public partial class frmMain : Form
{
    public frmMain()
    {
        InitializeComponent();
        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();

        services.AddSingleton<RecyclableMemoryStreamManager>();
        services.AddSingleton<ICsvExtractionService, CsvExtractionService>();
        services.AddSingleton<IJsonExtractionService, JsonExtractionService>();

        services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Cawing Against the Moon")
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithMemoryUsage()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.WithDemystifiedStackTraces()
            .Enrich.WithExceptionDetails()
            .WriteTo.Async(a =>
            {
                a.File(new CompactJsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day);
                a.Console(new RenderedCompactJsonFormatter());
                a.Debug(new CompactJsonFormatter());
            })
            .CreateLogger(), dispose: true));

        services.AddBlazorise(options => options.Immediate = true)
            .AddBootstrap5Components()
            .AddBootstrap5Providers()
            .AddBootstrapIcons()
            .AddLoadingIndicator();

        services.AddBlazoredLocalStorage();

        blzView1.HostPage = @"wwwroot/index.html";
        blzView1.Services = services.BuildServiceProvider();
        blzView1.UrlLoading += OnUrlRoutingEvent;
        blzView1.RootComponents.Add<App>("#app");
    }

    private static void OnUrlRoutingEvent(Object? sender, UrlLoadingEventArgs urlLoadingEventArgs)
    {
        if (urlLoadingEventArgs.Url.Host == "0.0.0.0")
        {
            return;
        }
        urlLoadingEventArgs.UrlLoadingStrategy =
            UrlLoadingStrategy.OpenInWebView;
    }
}
