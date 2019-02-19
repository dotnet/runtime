using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ILCompiler {
		private readonly string _ilasmExecutable;

		public ILCompiler ()
		{
			_ilasmExecutable = Environment.OSVersion.Platform == PlatformID.Win32NT ? LocateIlasmOnWindows ().ToString () : "ilasm";
		}

		public ILCompiler (string ilasmExecutable)
		{
			_ilasmExecutable = ilasmExecutable;
		}

		public NPath Compile (CompilerOptions options)
		{
			var capturedOutput = new List<string> ();
			var process = new Process ();
			SetupProcess (process, options);
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0)
			{
				Assert.Fail($"Failed to compile IL assembly : {options.OutputPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");
			}

			return options.OutputPath;
		}

		protected virtual void SetupProcess (Process process, CompilerOptions options)
		{
			process.StartInfo.FileName = _ilasmExecutable;
			process.StartInfo.Arguments = BuildArguments (options);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		}

		private string BuildArguments (CompilerOptions options)
		{
			var args = new StringBuilder();
			args.Append(options.OutputPath.ExtensionWithDot == ".dll" ? "/dll" : "/exe");
			args.Append($" /out:{options.OutputPath.InQuotes ()}");
			args.Append($" {options.SourceFiles.Aggregate (string.Empty, (buff, file) => $"{buff} {file.InQuotes ()}")}");
			return args.ToString ();
		}

		public static NPath LocateIlasmOnWindows ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new InvalidOperationException ("This method should only be called on windows");

			var possiblePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory ().ToNPath ().Combine ("ilasm.exe");
			if (possiblePath.FileExists ())
				return possiblePath;

			throw new InvalidOperationException ("Could not locate a ilasm.exe executable");
		}
	}
}
