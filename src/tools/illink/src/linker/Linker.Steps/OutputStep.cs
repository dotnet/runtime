// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// OutputStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public class OutputStep : BaseStep
	{
		readonly List<string> assembliesWritten;

		public OutputStep ()
		{
			assembliesWritten = new List<string> ();
		}

		protected override bool ConditionToProcess ()
		{
			return Context.ErrorsCount == 0;
		}

		protected override void Process ()
		{
			CheckOutputDirectory ();
			OutputPInvokes ();
			Tracer.Finish ();
		}

		protected override void EndProcess ()
		{
			if (Context.AssemblyListFile != null) {
				using (var w = File.CreateText (Context.AssemblyListFile)) {
					w.WriteLine ("[" + string.Join (", ", assembliesWritten.Select (a => "\"" + a + "\"").ToArray ()) + "]");
				}
			}
		}

		void CheckOutputDirectory ()
		{
			if (Directory.Exists (Context.OutputDirectory))
				return;

			Directory.CreateDirectory (Context.OutputDirectory);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			OutputAssembly (assembly);
		}

		protected void WriteAssembly (AssemblyDefinition assembly, string directory)
		{
			WriteAssembly (assembly, directory, SaveSymbols (assembly));
		}

		protected virtual void WriteAssembly (AssemblyDefinition assembly, string directory, WriterParameters writerParameters)
		{
			foreach (var module in assembly.Modules) {
				// Write back pure IL even for crossgen-ed assemblies
				if (module.IsCrossgened ()) {
					module.Attributes |= ModuleAttributes.ILOnly;
					module.Attributes ^= ModuleAttributes.ILLibrary;
					module.Architecture = TargetArchitecture.I386; // I386+ILOnly which ultimately translates to AnyCPU
				}
			}

			string outputName = GetAssemblyFileName (assembly, directory);
			try {
				assembly.Write (outputName, writerParameters);
			} catch (Exception e) {
				// We should be okay catching everything here, assembly.Write is all in Cecil and most of the state necessary to debug will be captured in assembly
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (null, DiagnosticId.FailedToWriteOutput, outputName), e);
			}
		}

		void OutputAssembly (AssemblyDefinition assembly)
		{
			string directory = Context.OutputDirectory;

			CopyConfigFileIfNeeded (assembly, directory);

			var action = Annotations.GetAction (assembly);
			Context.LogMessage ($"Output action: '{action,8}' assembly: '{assembly}'.");

			switch (action) {
			case AssemblyAction.Save:
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
				WriteAssembly (assembly, directory);
				CopySatelliteAssembliesIfNeeded (assembly, directory);
				assembliesWritten.Add (GetOriginalAssemblyFileInfo (assembly).Name);
				break;
			case AssemblyAction.Copy:
				CloseSymbols (assembly);
				CopyAssembly (assembly, directory);
				CopySatelliteAssembliesIfNeeded (assembly, directory);
				assembliesWritten.Add (GetOriginalAssemblyFileInfo (assembly).Name);
				break;
			case AssemblyAction.Delete:
				CloseSymbols (assembly);
				DeleteAssembly (assembly, directory);
				break;
			default:
				CloseSymbols (assembly);
				break;
			}
		}

		private void OutputPInvokes ()
		{
			if (Context.PInvokesListFile == null)
				return;

			using (var fs = File.Open (Path.Combine (Context.OutputDirectory, Context.PInvokesListFile), FileMode.Create)) {
				var values = Context.PInvokes.Distinct ().OrderBy (l => l);
				// Ignore warning, since we're just enabling analyzer for dogfooding
#pragma warning disable IL2026
				var jsonSerializer = new DataContractJsonSerializer (typeof (List<PInvokeInfo>));
				jsonSerializer.WriteObject (fs, values);
#pragma warning restore IL2026
			}
		}

		protected virtual void DeleteAssembly (AssemblyDefinition assembly, string directory)
		{
			var target = GetAssemblyFileName (assembly, directory);
			if (File.Exists (target)) {
				File.Delete (target);
				File.Delete (target + ".mdb");
				File.Delete (Path.ChangeExtension (target, "pdb"));
				File.Delete (GetConfigFile (target));
			}
		}

		void CloseSymbols (AssemblyDefinition assembly)
		{
			Annotations.CloseSymbolReader (assembly);
		}

		WriterParameters SaveSymbols (AssemblyDefinition assembly)
		{
			var parameters = new WriterParameters {
				DeterministicMvid = Context.DeterministicOutput
			};

			if (!Context.LinkSymbols)
				return parameters;

			if (!assembly.MainModule.HasSymbols)
				return parameters;

			// Use a string check to avoid a hard dependency on Mono.Cecil.Pdb
			if (Environment.OSVersion.Platform != PlatformID.Win32NT && assembly.MainModule.SymbolReader.GetType ().FullName == "Mono.Cecil.Pdb.NativePdbReader")
				return parameters;

			parameters.WriteSymbols = true;
			parameters.SymbolWriterProvider = new CustomSymbolWriterProvider (Context.PreserveSymbolPaths);
			return parameters;
		}


		void CopySatelliteAssembliesIfNeeded (AssemblyDefinition assembly, string directory)
		{
			if (!Annotations.ProcessSatelliteAssemblies)
				return;

			FileInfo original = GetOriginalAssemblyFileInfo (assembly);
			string resourceFile = GetAssemblyResourceFileName (original.FullName);

			foreach (var subDirectory in Directory.EnumerateDirectories (original.DirectoryName!)) {
				var satelliteAssembly = Path.Combine (subDirectory, resourceFile);
				if (!File.Exists (satelliteAssembly))
					continue;

				string cultureName = subDirectory.Substring (subDirectory.LastIndexOf (Path.DirectorySeparatorChar) + 1);
				string culturePath = Path.Combine (directory, cultureName);

				Directory.CreateDirectory (culturePath);
				File.Copy (satelliteAssembly, Path.Combine (culturePath, resourceFile), true);
			}
		}

		void CopyConfigFileIfNeeded (AssemblyDefinition assembly, string directory)
		{
			string config = GetConfigFile (GetOriginalAssemblyFileInfo (assembly).FullName);
			if (!File.Exists (config))
				return;

			string target = Path.GetFullPath (GetConfigFile (GetAssemblyFileName (assembly, directory)));

			if (config == target)
				return;

			File.Copy (config, GetConfigFile (GetAssemblyFileName (assembly, directory)), true);
		}

		static string GetAssemblyResourceFileName (string assembly)
		{
			return Path.GetFileNameWithoutExtension (assembly) + ".resources.dll";
		}

		static string GetConfigFile (string assembly)
		{
			return assembly + ".config";
		}

		FileInfo GetOriginalAssemblyFileInfo (AssemblyDefinition assembly)
		{
			return new FileInfo (Context.GetAssemblyLocation (assembly));
		}

		protected virtual void CopyAssembly (AssemblyDefinition assembly, string directory)
		{
			FileInfo fi = GetOriginalAssemblyFileInfo (assembly);
			string target = Path.GetFullPath (Path.Combine (directory, fi.Name));
			string source = fi.FullName;

			if (source == target)
				return;

			File.Copy (source, target, true);
			if (!Context.LinkSymbols)
				return;

			var mdb = source + ".mdb";
			if (File.Exists (mdb))
				File.Copy (mdb, target + ".mdb", true);

			var pdb = Path.ChangeExtension (source, "pdb");
			if (File.Exists (pdb))
				File.Copy (pdb, Path.ChangeExtension (target, "pdb"), true);
		}

		protected virtual string GetAssemblyFileName (AssemblyDefinition assembly, string directory)
		{
			string file = GetOriginalAssemblyFileInfo (assembly).Name;
			return Path.Combine (directory, file);
		}
	}
}
