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

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;

namespace Mono.Linker.Steps {

	public class OutputStep : BaseStep {

		private static Dictionary<UInt16, TargetArchitecture> architectureMap;

		private enum NativeOSOverride {
			Apple = 0x4644,
			FreeBSD = 0xadc4,
			Linux = 0x7b79,
			NetBSD = 0x1993,
			Default = 0
		}

		static TargetArchitecture CalculateArchitecture (TargetArchitecture readyToRunArch)
		{
			if (architectureMap == null) {
				architectureMap = new Dictionary<UInt16, TargetArchitecture> ();
				foreach (var os in Enum.GetValues (typeof (NativeOSOverride))) {
					ushort osVal = (ushort) (NativeOSOverride) os;
					foreach (var arch in Enum.GetValues (typeof (TargetArchitecture))) {
						ushort archVal = (ushort) (TargetArchitecture)arch;
						architectureMap.Add ((ushort) (archVal ^ osVal), (TargetArchitecture) arch);
					}
				}
			}

			TargetArchitecture pureILArch;
			if (architectureMap.TryGetValue ((ushort) readyToRunArch, out pureILArch)) {
				return pureILArch;
			}
			throw new BadImageFormatException ("unrecognized module attributes");
		}

		protected override void Process ()
		{
			CheckOutputDirectory ();
			Annotations.SaveDependencies ();
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

		static bool IsReadyToRun (ModuleDefinition module)
		{
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & (ModuleAttributes) 0x04) != 0;
		}

		void WriteAssembly (AssemblyDefinition assembly, string directory)
		{
			foreach (var module in assembly.Modules) {
				// Write back pure IL even for R2R assemblies
				if (IsReadyToRun (module)) {
					module.Attributes |= ModuleAttributes.ILOnly;
					module.Attributes ^= (ModuleAttributes) (uint) 0x04;
					module.Architecture = CalculateArchitecture (module.Architecture);
				}
			}

			assembly.Write (GetAssemblyFileName (assembly, directory), SaveSymbols (assembly));
		}

		void OutputAssembly (AssemblyDefinition assembly)
		{
			string directory = Context.OutputDirectory;

			CopyConfigFileIfNeeded (assembly, directory);

			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.Save:
			case AssemblyAction.Link:
				Context.Annotations.AddDependency (assembly);
				WriteAssembly (assembly, directory);
				break;
			case AssemblyAction.Copy:
				Context.Annotations.AddDependency (assembly);
				CloseSymbols (assembly);
				CopyAssembly (GetOriginalAssemblyFileInfo (assembly), directory, Context.LinkSymbols);
				break;
			case AssemblyAction.Delete:
				CloseSymbols (assembly);
				var target = GetAssemblyFileName (assembly, directory);
				if (File.Exists (target)) {
					File.Delete (target);
					File.Delete (target + ".mdb");
					File.Delete (Path.ChangeExtension (target, "pdb"));
					File.Delete (GetConfigFile (target));
				}
				break;
			default:
				CloseSymbols (assembly);
				break;
			}
		}

		void CloseSymbols (AssemblyDefinition assembly)
		{
			Annotations.CloseSymbolReader (assembly);
		}

		WriterParameters SaveSymbols (AssemblyDefinition assembly)
		{
			var parameters = new WriterParameters ();
			if (!Context.LinkSymbols)
				return parameters;

			if (!assembly.MainModule.HasSymbols)
				return parameters;

			if (Context.SymbolWriterProvider != null)
				parameters.SymbolWriterProvider = Context.SymbolWriterProvider;
			else
				parameters.WriteSymbols = true;
			return parameters;
		}

		static void CopyConfigFileIfNeeded (AssemblyDefinition assembly, string directory)
		{
			string config = GetConfigFile (GetOriginalAssemblyFileInfo (assembly).FullName);
			if (!File.Exists (config))
				return;

			string target = Path.GetFullPath (GetConfigFile (GetAssemblyFileName (assembly, directory)));

			if (config == target)
				return;

			File.Copy (config, GetConfigFile (GetAssemblyFileName (assembly, directory)), true);
		}

		static string GetConfigFile (string assembly)
		{
			return assembly + ".config";
		}

		static FileInfo GetOriginalAssemblyFileInfo (AssemblyDefinition assembly)
		{
			return new FileInfo (assembly.MainModule.FileName);
		}

		static void CopyAssembly (FileInfo fi, string directory, bool symbols)
		{
			string target = Path.GetFullPath (Path.Combine (directory, fi.Name));
			string source = fi.FullName;
			if (source == target)
				return;

			File.Copy (source, target, true);

			if (!symbols)
				return;

			var mdb = source + ".mdb";
			if (File.Exists (mdb))
				File.Copy (mdb, target + ".mdb", true);

			var pdb = Path.ChangeExtension (source, "pdb");
			if (File.Exists (pdb))
				File.Copy (pdb, Path.ChangeExtension (target, "pdb"), true);
		}

		static string GetAssemblyFileName (AssemblyDefinition assembly, string directory)
		{
			string file = GetOriginalAssemblyFileInfo (assembly).Name;
			return Path.Combine (directory, file);
		}
	}
}
