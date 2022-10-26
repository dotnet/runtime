// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class PeVerifier
	{
		private readonly string _peExecutable;

		public PeVerifier ()
		{
		}

		public PeVerifier (string peExecutable)
		{
			_peExecutable = peExecutable;
		}

		public virtual void Check (LinkedTestCaseResult linkResult, AssemblyDefinition original)
		{
			ProcessSkipAttributes (linkResult, original, out bool skipCheckEntirely, out HashSet<string> assembliesToSkip);

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
					var skipToolchain = (SkipPeVerifyForToolchian) ctorArg.Value;

					if (skipToolchain == SkipPeVerifyForToolchian.Pedump) {
						if (Environment.OSVersion.Platform != PlatformID.Win32NT)
							skipCheckEntirely = true;
					} else
						throw new ArgumentException ($"Unhandled platform and toolchain values of {Environment.OSVersion.Platform} and {skipToolchain}");
				} else if (ctorArg.Type.Name == nameof (String)) {
					assembliesToSkip.Add ((string) ctorArg.Value);
				} else {
					throw new ArgumentException ($"Unhandled constructor argument type of {ctorArg.Type} on {nameof (SkipPeVerifyAttribute)}");
				}
			}
		}

		protected virtual void CheckAssembly (NPath assemblyPath)
		{
#if NETCOREAPP
			// the PE Verifier does not know how to resolve .NET Core assemblies.
#else
			var capturedOutput = new List<string> ();
			var process = new Process ();
			SetupProcess (process, assemblyPath);
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0) {
				Assert.Fail ($"Invalid IL detected in {assemblyPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");
			}
#endif
		}

		protected virtual void SetupProcess (Process process, NPath assemblyPath)
		{
			var exeArgs = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/nologo {assemblyPath.InQuotes ()}" : $"--verify metadata,code {assemblyPath.InQuotes ()}";
			process.StartInfo.FileName = _peExecutable;
			process.StartInfo.Arguments = exeArgs;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
				process.StartInfo.Environment["MONO_PATH"] = assemblyPath.Parent.ToString ();
			}
		}
	}
}