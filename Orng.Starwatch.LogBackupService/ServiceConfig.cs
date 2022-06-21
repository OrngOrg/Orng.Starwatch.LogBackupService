using System;
using System.Collections.Generic;
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

    public static ServiceConfig? ReadFromFile(string path)
    => JsonConvert.DeserializeObject<ServiceConfig>(File.ReadAllText(path));

    public void WriteToFile(string path) => File.WriteAllText(path, JsonConvert.SerializeObject(this));
}
