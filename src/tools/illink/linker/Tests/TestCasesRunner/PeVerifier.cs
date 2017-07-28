using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class PeVerifier {
		private readonly string _peExecutable;

		public PeVerifier ()
		{
			_peExecutable = Environment.OSVersion.Platform == PlatformID.Win32NT ? FindPeExecutableFromRegistry ().ToString () : "pedump";
		}

		public PeVerifier (string peExecutable)
		{
			_peExecutable = peExecutable;
		}

		public virtual void Check (LinkedTestCaseResult linkResult, AssemblyDefinition original)
		{
			bool skipCheckEntirely;
			HashSet<string> assembliesToSkip;
			ProcessSkipAttributes (linkResult, original, out skipCheckEntirely, out assembliesToSkip);

			if (skipCheckEntirely)
				return;

			foreach (var file in linkResult.OutputAssemblyPath.Parent.Files ()) {
				if (file.ExtensionWithDot != ".exe" && file.ExtensionWithDot != ".dll")
					continue;

				// Always skip the I18N assemblies, for some reason they end up in the output directory on OSX.
				// verification of these fails due to native pointers
				if (file.FileName.StartsWith ("I18N"))
					continue;

				if (assembliesToSkip.Contains (file.FileName))
					continue;

				CheckAssembly (file);
			}
		}

		private void ProcessSkipAttributes (LinkedTestCaseResult linkResult, AssemblyDefinition original, out bool skipCheckEntirely, out HashSet<string> assembliesToSkip)
		{
			var peVerifyAttrs = original.MainModule.GetType (linkResult.TestCase.ReconstructedFullTypeName).CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SkipPeVerifyAttribute));
			skipCheckEntirely = false;
			assembliesToSkip = new HashSet<string> ();
			foreach (var attr in peVerifyAttrs) {
				var ctorArg = attr.ConstructorArguments.FirstOrDefault ();

				if (!attr.HasConstructorArguments) {
					skipCheckEntirely = true;
				} else if (ctorArg.Type.Name == nameof (SkipPeVerifyForToolchian)) {
					var skipToolchain = (SkipPeVerifyForToolchian)ctorArg.Value;

					if (skipToolchain == SkipPeVerifyForToolchian.Pedump) {
						if (Environment.OSVersion.Platform != PlatformID.Win32NT)
							skipCheckEntirely = true;
					}
					else
						throw new ArgumentException ($"Unhandled platform and toolchain values of {Environment.OSVersion.Platform} and {skipToolchain}");
				} else if (ctorArg.Type.Name == nameof (String)) {
					assembliesToSkip.Add ((string)ctorArg.Value);
				} else {
					throw new ArgumentException ($"Unhandled constructor argument type of {ctorArg.Type} on {nameof (SkipPeVerifyAttribute)}");
				}
			}
		}

		private void CheckAssembly (NPath assemblyPath)
		{
			var capturedOutput = new List<string> ();
			var exeArgs = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/nologo {assemblyPath.InQuotes ()}" : $"--verify metadata,code {assemblyPath.InQuotes ()}";
			var process = new Process ();
			process.StartInfo.FileName = _peExecutable;
			process.StartInfo.Arguments = exeArgs;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0) {
				Assert.Fail ($"Invalid IL detected in {assemblyPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");
			}
		}

		public static NPath FindPeExecutableFromRegistry ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new InvalidOperationException ("This method should only be called on windows");

			NPath result;
			if (TryFindPeExecutableFromRegustrySubfolder ("NETFXSDK", out result))
				return result;
			if (TryFindPeExecutableFromRegustrySubfolder ("Windows", out result))
				return result;

			throw new InvalidOperationException ("Could not locate a peverify.exe executable");
		}

		private static bool TryFindPeExecutableFromRegustrySubfolder (string subfolder, out NPath peVerifyPath)
		{
			var keyPath = $"SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\{subfolder}";
			var key = Registry.LocalMachine.OpenSubKey (keyPath);

			foreach (var sdkKeyName in key.GetSubKeyNames ().OrderBy (name => new Version (name.TrimStart ('v').TrimEnd ('A'))).Reverse ()) {
				var sdkKey = Registry.LocalMachine.OpenSubKey ($"{keyPath}\\{sdkKeyName}");

				var sdkDir = (string)sdkKey.GetValue ("InstallationFolder");
				if (string.IsNullOrEmpty (sdkDir))
					continue;

				var binDir = sdkDir.ToNPath ().Combine ("bin");
				if (!binDir.Exists ())
					continue;

				foreach (var netSdkDirs in binDir.Directories ().OrderBy (dir => dir.FileName)) {
					peVerifyPath = netSdkDirs.Combine ("PEVerify.exe");
					if (peVerifyPath.FileExists ())
						return true;
				}
			}
			peVerifyPath = null;
			return false;
		}
	}
}
