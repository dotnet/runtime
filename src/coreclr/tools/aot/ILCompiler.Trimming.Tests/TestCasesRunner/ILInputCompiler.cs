// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Extensions;
using Xunit;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILInputCompiler
	{
		public NPath Compile (CompilerOptions options)
		{
			var capturedOutput = new List<string> ();
			var process = new Process ();
			SetupProcess (process, options);
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data!);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0) {
				Assert.Fail($"Failed to compile IL assembly : {options.OutputPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");
			}

			return options.OutputPath;
		}

		protected virtual void SetupProcess (Process process, CompilerOptions options)
		{
			process.StartInfo.FileName = LocateIlasm ().ToString ();
			process.StartInfo.Arguments = BuildArguments (options);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		}

		private static string BuildArguments (CompilerOptions options)
		{
			var args = new StringBuilder ();
#if NETCOREAPP
			args.Append (options.OutputPath.ExtensionWithDot == ".dll" ? "-dll" : "-exe");
			args.Append ($" -out:{options.OutputPath.InQuotes ()}");
#else
			args.Append (options.OutputPath.ExtensionWithDot == ".dll" ? "/dll" : "/exe");
			args.Append ($" /out:{options.OutputPath.InQuotes ()}");
#endif
			args.Append ($" {options.SourceFiles.Aggregate (string.Empty, (buff, file) => $"{buff} {file.InQuotes ()}")}");
			return args.ToString ();
		}

		protected virtual NPath LocateIlasm ()
		{
#if NETCOREAPP
			var extension = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? ".exe" : "";

			var toolsDir = (string) AppContext.GetData ("Mono.Linker.Tests.ILToolsDir")!;

			var ilasmPath = Path.GetFullPath (Path.Combine (toolsDir, $"ilasm{extension}")).ToNPath ();
			if (ilasmPath.FileExists ())
				return ilasmPath;

			throw new InvalidOperationException ("ilasm not found at " + ilasmPath);
#else
			return Environment.OSVersion.Platform == PlatformID.Win32NT ? LocateIlasmOnWindows () : "ilasm".ToNPath ();
#endif
		}

		public static NPath LocateIlasmOnWindows ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new InvalidOperationException ("This method should only be called on windows");

			var possiblePath = RuntimeEnvironment.GetRuntimeDirectory ().ToNPath ().Combine ("ilasm.exe");
			if (possiblePath.FileExists ())
				return possiblePath;

			possiblePath = Environment.CurrentDirectory.ToNPath ().Combine ("ilasm.exe");
			if (possiblePath.FileExists ())
				return possiblePath;

			throw new InvalidOperationException ("Could not locate a ilasm.exe executable");
		}
	}
}
