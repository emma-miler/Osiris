using System.Net;
using Osiris;

class Program
{

    public static void Main()
    {
        Log.Initialize("logs", maxLogFiles: 10);
        Log.Trace("test");
        Log.Debug("test");
        Log.Info("test");
        Log.Warning("test");
        Log.Error("test");
        Log.Critical("test");
        Log.Custom(Microsoft.Extensions.Logging.LogLevel.Information, "test", null, ConsoleColor.Cyan, ConsoleColor.DarkYellow);
        throw new WebException("test exception");
    }
}

