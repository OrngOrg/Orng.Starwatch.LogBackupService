using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Orng.Starwatch.LogBackupService;
public class ServiceConfig
{
    public string StarwatchUrl { get; set; } = "https://localhost:8000";

    public string StarwatchUsername { get; set; } = string.Empty;

    public string StarwatchPassword { get; set; } = string.Empty;

    public int LogCheckInterval { get; set; } = 300;

    public string LogOutputPath { get; set; } = string.Empty;

    public int LogBufferSize { get; set; } = Environment.SystemPageSize;

    public string DateFormat { get; set; } = "MMMM dd yyyy @ HH mm";

    public string DateFormatCulture { get; set; } = "en-US";

    [JsonIgnore]
    private CultureInfo? _DateFormatCultureParsed { get; set; } = null;

    [JsonIgnore]
    public CultureInfo DateFormatCultureParsed
    {
        get
        {
            if (_DateFormatCultureParsed is null)
                _DateFormatCultureParsed = CultureInfo.GetCultureInfo(DateFormatCulture);
            
            return _DateFormatCultureParsed;
        }
    }

    public string LogFilenameFormat { get; set; } = "{0}.log.{1}";

    public bool EnableDebugOutput { get; set; } = false;

    public static ServiceConfig? ReadFromFile(string path)
    => JsonConvert.DeserializeObject<ServiceConfig>(File.ReadAllText(path));

    public void WriteToFile(string path) => File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
}
