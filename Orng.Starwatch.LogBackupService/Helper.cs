using Serilog;
using System.Runtime.InteropServices;

namespace Orng.Starwatch.LogBackupService;
public static class Helper
{
    public static int ErrorExitCode => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? 1
        : -1;

    public const string PleaseConfig = "Missing {0}, please configure the config file.";

    public static void Shutdown (string reason, params string[] formatParams)
    {
        Log.Error($"{string.Format(reason, formatParams)} Shutting down...");
        Environment.Exit(ErrorExitCode);
    }

    public static void Shutdown (Exception ex, string reason, params string[] formatParams)
    {
        Log.Debug(ex.ToString());
        Shutdown(reason, formatParams);
    }

    public static ServiceConfig? Config { get; set; } = null;

    public static void ReadConfigFile ()
    {
        if (File.Exists("config"))
        {
            try
            {
                Config = ServiceConfig.ReadFromFile("config");
            }
            catch (Exception ex)
            {
                Shutdown(ex, $"Ran into {ex.Message} while reading log file.");
            }

            if (Config is null)
            {
                Shutdown("Config was read, but is null.");
                return;
            }

            if (Config.StarwatchUsername.Length == 0)
            {
                Shutdown(PleaseConfig, "StarwatchUsername");
                return;
            }

            if (Config.StarwatchPassword.Length == 0)
            {
                Shutdown(PleaseConfig, "StarwatchPassword");
                return;
            }

            if (Config.StarwatchUrl.Length == 0)
            {
                Shutdown(PleaseConfig, "StarwatchUrl");
                return;
            }

            if (Config.StarwatchUsername.Length == 0)
            {
                Shutdown(PleaseConfig, "StarwatchUsername");
                return;
            }

            return;
        }

        Config = new ServiceConfig();
        Config.WriteToFile("config");

        Shutdown("Please configure the file located @ 'config' before continuing.");
    }
}
