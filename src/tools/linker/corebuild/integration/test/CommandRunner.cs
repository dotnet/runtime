using System;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class CommandRunner
	{
		protected readonly ITestOutputHelper outputHelper;

		private string command;
		private string args;
		private string workingDir;
		private string additionalPath;
		private int timeout = Int32.MaxValue;
		private string terminatingOutput;

		public CommandRunner(string command, ITestOutputHelper outputHelper) {
			this.command = command;
			this.outputHelper = outputHelper;
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
			if (outputHelper == null) {
				throw new Exception("No output helper present.");
			}
			var psi = new ProcessStartInfo
			{
				FileName = command,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			outputHelper.WriteLine($"caller working directory: {Environment.CurrentDirectory}");
			if (!String.IsNullOrEmpty(args)) {
				psi.Arguments = args;
				outputHelper.WriteLine($"{command} {args}");
			} else {
				outputHelper.WriteLine($"{command}");
			}
			if (!String.IsNullOrEmpty(workingDir)) {
				outputHelper.WriteLine($"working directory: {workingDir}");
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

			outputHelper.WriteLine("environment:");
			foreach (var item in psi.Environment) {
				outputHelper.WriteLine($"\t{item.Key}={item.Value}");
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
				outputHelper.WriteLine($"killing process after {timeout} ms");
				process.Kill();
			}
			// WaitForExit with timeout doesn't guarantee
			// that the async output handlers have been
			// called, so WaitForExit needs to be called
			// afterwards.
			process.WaitForExit();
			string processOutputStr = processOutput.ToString();
			string processErrorStr = processError.ToString();
			outputHelper.WriteLine(processOutputStr);
			outputHelper.WriteLine(processErrorStr);
			commandOutput = processOutputStr;
			return process.ExitCode;
		}
	}
}
