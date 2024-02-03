using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Osiris;

public class Log
{
    public enum DetailLevel
    {
        /// <summary>
        /// No additional information is added to the log output
        /// </summary>
        None,
        /// <summary>
        /// Date, time, log level and tag are added to the log output
        /// </summary>
        Basic,
        /// <summary>
        /// Date, time, caller information, log level, and tag are added to the log output
        /// </summary>
        Detailed
    }

    #region Public Members
    static DetailLevel _logDetailLevel = DetailLevel.Detailed;
    public static DetailLevel LogDetailLevel
    {
        get => _logDetailLevel;
        set {
            lock (_lock)
                _logDetailLevel = value;
        }
    }
    static string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.ffff";
    public static string DateTimeFormat 
    {
        get => _dateTimeFormat;
        set {
            lock(_lock)
                _dateTimeFormat = value;
        }
    }
    public static bool Initialized { get; private set; } = false;

    public static event EventHandler<Exception>? OnUnhandledException;

    #endregion Public Members

    #region Public Methods

    public static void Initialize(string? logFolder,
        int maxLogFiles = -1,
        TimeSpan? maxLogAge = null
    )
    {
        lock (_lock)
        {
            Initialized = true;
            _logFolder = logFolder ?? "";
            // If we log to file, initialize that subsystem
            if (_logToFile)
            {
                _logFileName = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss") + ".txt";
                // Ensure directory exists
                Directory.CreateDirectory(_logFolder);
                File.WriteAllText($"{_logFolder}/{_logFileName}", "");
                File.WriteAllText($"{_logFolder}/latest.txt", "");
                // Copy over the parameters the use passed in
                _maxLogFiles = maxLogFiles;
                _maxLogAge = maxLogAge ?? TimeSpan.Zero;
                // Then start a thread that will run log cleanup every 30 minutes
                Task.Run(LogCleanupThread);
            }
            // Set up exception handling stuff
            // TODO: WinForms and WPF require special handling. will need to add that later
            // as i dont have access to a windows system right now.
            System.AppDomain.CurrentDomain.UnhandledException += BaseExceptionHandler;
        }
    }

    public static void AddLogger(ILogger logger) => _userLoggers.Add(logger);
    public static bool RemoveLogger(ILogger logger) => _userLoggers.Remove(logger);

    #endregion Public Methods

    #region Logging Functions

    public static void LogException(Exception ex,
            LogLevel level = LogLevel.Error,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        Error(ex.Message, null, _name, _path, _line);
        if (ex.StackTrace is null)
            return;
        Error(ex.StackTrace, null, _name, _path, _line);
    }

    public static void Trace(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Trace, message, tag, _name, _path, _line, ConsoleColor.Black, ConsoleColor.Blue);
    }

    public static void Debug(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Debug, message, tag, _name, _path, _line, ConsoleColor.Black, ConsoleColor.Cyan);
    }

    public static void Info(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Information, message, tag, _name, _path, _line, ConsoleColor.Black, ConsoleColor.Green);
    }

    public static void Warning(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Warning, message, tag, _name, _path, _line, ConsoleColor.Black, ConsoleColor.Yellow);
    }

    public static void Error(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Error, message, tag, _name, _path, _line, ConsoleColor.Black, ConsoleColor.Red);
    }

    public static void Critical(string message,
            string? tag = null,
            [CallerMemberName] string _name = "",
            [CallerFilePath] string _path = "",
            [CallerLineNumber] int _line = 0
        )
    {
        _Log(LogLevel.Critical, message, tag, _name, _path, _line, ConsoleColor.DarkRed, ConsoleColor.White);
    }

    #endregion Logging Functions

    #region Private Members

    static readonly object _lock = new();

    static string _logFolder = string.Empty;
    static string _logFileName = string.Empty;
    static bool _logToFile => !string.IsNullOrEmpty(_logFolder);

    static int _maxLogFiles = -1;
    static TimeSpan _maxLogAge = TimeSpan.Zero;

    static List<UnhandledExceptionEventHandler> _exceptionHandlers = new();

    static List<ILogger> _userLoggers = new();

    #endregion Private Members


    #region Private Methods

    static void _Log(LogLevel level, string message, string? tag, string name, string path, int line, ConsoleColor bg, ConsoleColor fg)
    {
        lock (_lock)
        {
            // Info about the method who called us
            string callerInfo = $"<{Path.GetFileName(path)}@{name}:{line}> ";
            // Current time
            string time = $"({DateTime.Now.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}) ";
            // Log level, in short form
            string logLevel = $"[{GetLogLevelShortText(level)}] ";
            // Possible message tag
            string tagString = string.IsNullOrEmpty(tag) ? " " : $"|{tag}| ";

            string output = string.Empty;
            // Build output string
            if (LogDetailLevel >= DetailLevel.Basic)
                output += time;
            if (LogDetailLevel >= DetailLevel.Detailed)
                output += callerInfo;
            
            if (LogDetailLevel >= DetailLevel.Basic)
                output += logLevel;
            output += tag;

            output += message;

            // Write out to console
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
            Console.Write(output);
            Console.ResetColor();
            Console.WriteLine("");

            // Write out to file, if applicable
            if (_logToFile)
            {
                File.AppendAllText($"{_logFolder}/{_logFileName}", output + "\n");
                File.AppendAllText($"{_logFolder}/latest.txt", output + "\n");
            }

            // Trigger each attached logger
            foreach (ILogger logger in _userLoggers)
            {
                logger.Log(level, message);
            }
        }
    }

    /// <summary>
    /// Returns the short-form string for the given log level
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    static string GetLogLevelShortText(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace: return "TRAC";
            case LogLevel.Debug: return "DBUG";
            case LogLevel.Information: return "INFO";
            case LogLevel.Warning: return "WARN";
            case LogLevel.Error: return "ERRR";
            case LogLevel.Critical: return "CRIT";
            case LogLevel.None: return "NONE";
            default: throw new Exception("LogLevel enum invalid");
        }
    }

    static void LogCleanupThread()
    {
        while (true)
        {
            LogCleanup();
            Thread.Sleep(TimeSpan.FromMinutes(30));
        }
    }

    /// <summary>
    /// Runs log cleanup based on `_maxLogAge` and `_maxLogFiles`
    /// </summary>
    static void LogCleanup()
    {
        // First, clean up log files by age
        if (_maxLogAge != TimeSpan.Zero)
        {
            List<FileInfo> files = Directory.GetFiles(_logFolder).Select(x => new FileInfo(x)).ToList();
            foreach (FileInfo file in files)
            {
                if (file.CreationTime > DateTime.Now + _maxLogAge)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        Warning($"Failed to clean up log file '{file.FullName}'");
                        LogException(ex, LogLevel.Warning);
                    }
                }
            }
        }
        // Then, truncate the directory to `_maxLogFiles` amount of files
        if (_maxLogFiles > 0)
        {
            List<FileInfo> files = Directory.GetFiles(_logFolder).Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
            for (int i = 0; i < files.Count - _maxLogFiles; i++)
            {
                try
                {
                    files[i].Delete();
                }
                catch (Exception ex)
                {
                    Warning($"Failed to clean up log file '{files[i].FullName}'");
                    LogException(ex, LogLevel.Warning);
                }
            }
        }
    }

    static void BaseExceptionHandler(object? sender, UnhandledExceptionEventArgs args) => HandleException((Exception)args.ExceptionObject);

    static void HandleException(Exception ex)
    {
        LogException(ex, LogLevel.Error, "ERROR_HANDLER", "OSIRIS", 0);
        OnUnhandledException?.Invoke(typeof(Log), ex);
    }

    #endregion Private Methods

}

