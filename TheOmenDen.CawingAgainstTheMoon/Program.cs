using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace TheOmenDen.CawingAgainstTheMoon;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ConfigureSerilog();
        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            Application.Run(new frmMain());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "WordPress Extractor")
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
                    a.Console(new RenderedCompactJsonFormatter());
                    a.Debug(new CompactJsonFormatter());
                }
            ).CreateLogger();
    }
}