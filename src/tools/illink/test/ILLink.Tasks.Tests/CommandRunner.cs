using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{

	public class CommandHelper
	{
		private ILogger logger;

		public CommandHelper(ILogger logger)
		{
			this.logger = logger;
		}

		public int Dotnet(string args, string workingDir, string additionalPath = null)
		{
			return RunCommand(Path.GetFullPath(TestContext.DotnetToolPath), args,
				workingDir, additionalPath, out string commandOutput);
		}

		public int RunCommand(string command, string args, int timeout = Int32.MaxValue)
		{
			return RunCommand(command, args, null, null, out string commandOutput, timeout);
		}

		public int RunCommand(string command, string args, string workingDir)
		{
			return RunCommand(command, args, workingDir, null, out string commandOutput);
		}

		public int RunCommand(string command, string args, string workingDir, string additionalPath,
			out string commandOutput, int timeout = Int32.MaxValue, string terminatingOutput = null)
		{
			return (new CommandRunner(command, logger))
				.WithArguments(args)
				.WithWorkingDir(workingDir)
				.WithAdditionalPath(additionalPath)
				.WithTimeout(timeout)
				.WithTerminatingOutput(terminatingOutput)
				.Run(out commandOutput);
		}
	}

	public class CommandRunner
	{
		private readonly ILogger logger;

		private string command;
		private string args;
		private string workingDir;
		private string additionalPath;
		private int timeout = Int32.MaxValue;
		private string terminatingOutput;

		private void LogMessage (string message)
		{
			logger.LogMessage (message);
		}

		public CommandRunner(string command, ILogger logger) {
			this.command = command;
			this.logger = logger;
		}

		public CommandRunner WithArguments(string args) {
			this.args = args;
			return this;
		}

		public CommandRunner WithWorkingDir(string workingDir) {
			this.workingDir = workingDir;
			return this;
		}

		public CommandRunner WithAdditionalPath(string additionalPath) {
			this.additionalPath = additionalPath;
			return this;
		}

		public CommandRunner WithTimeout(int timeout) {
			this.timeout = timeout;
			return this;
		}

		public CommandRunner WithTerminatingOutput(string terminatingOutput) {
			this.terminatingOutput = terminatingOutput;
			return this;
		}

		public int Run()
		{
			return Run(out string commandOutputUnused);
		}

		public int Run(out string commandOutput)
		{
			if (String.IsNullOrEmpty(command)) {
				throw new Exception("No command was specified specified.");
			}
			if (logger == null) {
				throw new Exception("No logger present.");
			}
			var psi = new ProcessStartInfo
			{
				FileName = command,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			LogMessage ($"caller working directory: {Environment.CurrentDirectory}");
			if (!String.IsNullOrEmpty(args)) {
				psi.Arguments = args;
				LogMessage ($"{command} {args}");
			} else {
				LogMessage ($"{command}");
			}
			if (!String.IsNullOrEmpty(workingDir)) {
				LogMessage ($"working directory: {workingDir}");
				psi.WorkingDirectory = workingDir;
			}
			if (!String.IsNullOrEmpty(additionalPath)) {
				string path = psi.Environment["PATH"];
				psi.Environment["PATH"] = path + ";" + additionalPath;
			}
			var process = new Process();
			process.StartInfo = psi;

			// dotnet sets some environment variables that
			// may cause problems in the child process.
			psi.Environment.Remove("MSBuildExtensionsPath");
			psi.Environment.Remove("MSBuildLoadMicrosoftTargetsReadOnly");
			psi.Environment.Remove("MSBuildSDKsPath");
			psi.Environment.Remove("VbcToolExe");
			psi.Environment.Remove("CscToolExe");
			psi.Environment.Remove("MSBUILD_EXE_PATH");

			LogMessage ("environment:");
			foreach (var item in psi.Environment) {
				LogMessage ($"\t{item.Key}={item.Value}");
			}

			StringBuilder processOutput = new StringBuilder();
			DataReceivedEventHandler handler = (sender, e) => {
				processOutput.Append(e.Data);
				processOutput.AppendLine();
			};
			StringBuilder processError = new StringBuilder();
			DataReceivedEventHandler ehandler = (sender, e) => {
				processError.Append(e.Data);
				processError.AppendLine();
			};
			process.OutputDataReceived += handler;
			process.ErrorDataReceived += ehandler;

			// terminate process if output contains specified string
			if (!String.IsNullOrEmpty(terminatingOutput)) {
				DataReceivedEventHandler terminatingOutputHandler = (sender, e) => {
					if (!String.IsNullOrEmpty(e.Data) && e.Data.Contains(terminatingOutput)) {
						process.Kill();
					}
				};
				process.OutputDataReceived += terminatingOutputHandler;
			}

			// start the process
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			if (!process.WaitForExit(timeout)) {
				LogMessage ($"killing process after {timeout} ms");
				process.Kill();
			}
			// WaitForExit with timeout doesn't guarantee
			// that the async output handlers have been
			// called, so WaitForExit needs to be called
			// afterwards.
			process.WaitForExit();
			string processOutputStr = processOutput.ToString();
			string processErrorStr = processError.ToString();
			LogMessage(processOutputStr);
			LogMessage(processErrorStr);
			commandOutput = processOutputStr;
			return process.ExitCode;
		}
	}
}
