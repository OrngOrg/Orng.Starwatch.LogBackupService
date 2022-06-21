using Orng.Starwatch.API;
using System.IO;
using Serilog;

namespace Orng.Starwatch.LogBackupService;

public class LogMonitor
{
    private readonly ServiceConfig config;

    public LogMonitor(ServiceConfig config) => this.config = config;


    // Worker 
    public bool ShouldStop { get; set; } = false;

    private Thread? workerThread = null;

    public void Start ()
    {
        workerThread = new Thread(Worker);
        workerThread.Start();
    }

    public void Stop() => ShouldStop = true;

    private void Worker ()
    {
        Log.Debug("Starting worker");
        Init();

        while (!ShouldStop)
        {
            try
            {
                Work();
            }
            catch (Exception ex)
            {
                Log.Debug($"Exception checking log: {ex}");
                Log.Information($"Could not check log: {ex.Message}");
            }

            Log.Debug($"Sleeping for {config.LogCheckInterval} seconds.");
            Thread.Sleep(config.LogCheckInterval * 1000);
        }
    }

    // Logic

    private const int SignatureLength = 256;
    private readonly byte[] currentSignature = new byte[SignatureLength];      // Used to trigger downloads
    private readonly byte[] signatureCheckBuffer = new byte[SignatureLength];  // Used for log1
    private readonly byte[] signatureCheckBuffer2 = new byte[SignatureLength]; // Used for log2+
    private byte[] downloadBuffer = new byte[0];
    private const string LastPath = "last.log";
    private const string TmpPath = "log.tmp";
    private const string SignatureList = "signatures";
    private const string DateFormat = "MMMM dd yyyy @ HH mm";
    private const string OutputFilename = "{0}.log.{1}"; // where {0} is DateFormat, {1} is the log number.
    private ApiClient? cli = null;

    private void Init ()
    {
        Log.Debug("Init");
        cli = new ApiClient(config.StarwatchUrl, config.StarwatchUsername, config.StarwatchPassword);

        if (File.Exists(LastPath))
        {
            ReadSignature(LastPath, currentSignature);
        }

        downloadBuffer = new byte[config.LogBufferSize];
    }

    private void ReadSignature (string path, byte[] buffer)
    {
        Log.Debug("ReadSignature");
        var fs = File.OpenRead(path);
        fs.Read(buffer, 0, SignatureLength);
        fs.Close();
    }

    private bool CheckHasSignature (byte[] signature)
    {
        Log.Debug("CheckHasSignature");
        var fs = File.OpenRead(SignatureList);
        bool hasMatch = false;

        for (int sig=0; sig< (int) Math.Floor((double)fs.Length / (double)SignatureLength); sig++)
        {
            fs.Read(signatureCheckBuffer, 0, SignatureLength);
            bool fullMatch = true;

            for (int i = 0; i < SignatureLength; i++)
            {
                if (signature[i] != signatureCheckBuffer[i])
                {
                    fullMatch = false;
                    break;
                }
            }

            if (fullMatch)
            {
                hasMatch = true;
                break;
            }
        }

        fs.Close();
        Log.Debug($"CheckHasSignature rets {hasMatch}");
        return hasMatch;
    }

    private void DocumentSignature (byte[] signature)
    {
        Log.Debug("DocumentSignature");
        var fs = File.Open(SignatureList, FileMode.Append, FileAccess.Write);
        fs.Write(signature, 0, signature.Length);
        fs.Close();
    }

    private void UpdateSignature ()
    {
        Log.Debug("UpdateSignature");
        for (int i=0; i<SignatureLength; i++)
            currentSignature[i] = signatureCheckBuffer[i];
    }

    private bool CheckIfNew ()
    {
        Log.Debug("CheckIfNew");

        if (!File.Exists(LastPath))
            return true;

        ReadSignature(LastPath, signatureCheckBuffer);

        for (int i=0; i<SignatureLength; i++)
        {
            if (signatureCheckBuffer[i] != currentSignature[i])
                return true;
        }

        return false;
    }

    private void CopyTmp(int i = 1, string path = TmpPath)
    {
        
        DateTime now = DateTime.Now;
        string filename = string.Format(OutputFilename, now.ToString(DateFormat), i);

        Log.Debug($"CopyTmp {path} to {config.LogOutputPath}{filename}");

        File.Copy(path, $"{config.LogOutputPath}{filename}");
    }

    private void Work ()
    {
        if (cli is null)
            throw new Exception("No init");

        Log.Debug("Work");
        Log.Information($"Downloading log 1 to {LastPath}");
        try
        {
            bool ok = cli.DownloadLog(LastPath, downloadBuffer, 1);
            if (!ok)
            {
                throw new Exception("Failed to download log 1");
            }
            bool isNew = CheckIfNew();

            // trigger for checking signatures and copying files.
            if (isNew)
            {
                Log.Information("New - Checking older logs for updates");
                
                UpdateSignature();
                DocumentSignature(currentSignature);

                CopyTmp(1, LastPath);

                for (int i = 2; i <= 5; i++)
                {
                    try
                    {
                        Log.Information($"Downloading log {i} to {TmpPath}");
                        cli.DownloadLog(TmpPath, downloadBuffer, i);
                        ReadSignature(TmpPath, signatureCheckBuffer2);
                        bool exists = CheckHasSignature(signatureCheckBuffer2);

                        if (exists)
                            break;

                        DocumentSignature(signatureCheckBuffer2);
                        CopyTmp(i);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Couldn't download log {i}: {ex}");
                        Log.Information($"Couldn't download log {i}: {ex.Message}");
                    }
                }
            }
            else
            {
                Log.Information($"No new updates, waiting {config.LogCheckInterval} seconds.");
            }
        }
        catch (Exception mex)
        {
            Log.Debug($"Couldn't work: {mex}");
            Log.Information($"Couldn't download log 1: {mex.Message}");
        }
        
    }
}
