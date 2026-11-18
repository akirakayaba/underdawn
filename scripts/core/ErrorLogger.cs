//Name:			ErrorLogger.cs
//Description:	Autoload that writes every engine/script error, warning and C#
//				crash to user://logs/error_log.txt so bugs can be reported with
//				the log (on windows: %APPDATA%/Godot/app_userdata/UnderDawn/logs)
//				the old log is kept as error_log.txt.old once it gets over 1 MB

using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class ErrorLogger : Node
{
	public static string LogPath { get; private set; }

	private const long MaxLogSize = 1024 * 1024;
	private static readonly object fileLock = new();
	private ErrorFileLogger logger;

	/*_EnterTree instead of _Ready so it starts logging before the other autoloads*/
	public override void _EnterTree()
	{
		LogPath = ProjectSettings.GlobalizePath("user://logs/error_log.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
		if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogSize)
			File.Move(LogPath, LogPath + ".old", true);

		Write("INFO", $"session started ({OS.GetName()}, Godot {Engine.GetVersionInfo()["string"]})");
		logger = new ErrorFileLogger();
		OS.AddLogger(logger);
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Write("CRASH", e.ExceptionObject.ToString());
		TaskScheduler.UnobservedTaskException += (_, e) => Write("ERROR", "unobserved task exception: " + e.Exception);
	}

	public override void _ExitTree()
	{
		OS.RemoveLogger(logger);
		Write("INFO", "session ended");
	}

	/*can be called from any thread, logging must never crash the game itself*/
	public static void Write(string level, string message)
	{
		if (LogPath == null)
			return;
		lock (fileLock)
		{
			try
			{
				File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}: {message}{System.Environment.NewLine}");
			}
			catch (IOException)
			{
			}
		}
	}
}

/*gets every error / warning godot prints (including C# exceptions in game code)*/
public partial class ErrorFileLogger : Logger
{
	public override void _LogError(string function, string file, int line, string code, string rationale,
		bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
	{
		string type = errorType switch
		{
			1 => "WARNING",
			2 => "SCRIPT ERROR",
			3 => "SHADER ERROR",
			_ => "ERROR"
		};
		string message = string.IsNullOrEmpty(rationale) ? code : rationale;
		ErrorLogger.Write(type, $"{message} ({file}:{line} {function})");
	}

	public override void _LogMessage(string message, bool error)
	{
		if (error)
			ErrorLogger.Write("ERROR", message.TrimEnd());
	}
}
