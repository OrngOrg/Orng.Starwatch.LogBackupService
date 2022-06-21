using Orng.Starwatch.LogBackupService;
using Serilog;

Console.Title = "Starwatch Log Backup Service";

Log.Logger = new LoggerConfiguration()
#if DEBUG
    .MinimumLevel.Debug()
#else
    .MinimumLevel.Information()
#endif
    .WriteTo.Console()
    .CreateLogger();

Log.Debug("Application started");
Log.Debug("Reading service config");
Helper.ReadConfigFile();

if (Helper.Config is null)
    throw new Exception("Failed to load config.");

var lm = new LogMonitor(Helper.Config);
lm.Start();
