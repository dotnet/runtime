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
			Tracer.Finish ();
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
					module.Architecture = CalculateArchitecture (module.Architecture);
				}
			}

			assembly.Write (GetAssemblyFileName (assembly, directory), writerParameters);
		}

		void OutputAssembly (AssemblyDefinition assembly)
		{
			string directory = Context.OutputDirectory;

			CopyConfigFileIfNeeded (assembly, directory);

			var action = Annotations.GetAction (assembly);
			Context.LogMessage (MessageImportance.Low, $"Output action: {action,8} assembly: {assembly}");

			switch (action) {
			case AssemblyAction.Save:
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
				Context.Tracer.AddDependency (assembly);
				WriteAssembly (assembly, directory);
				break;
			case AssemblyAction.Copy:
				Context.Tracer.AddDependency (assembly);
				CloseSymbols (assembly);
				CopyAssembly (assembly, directory);
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

		protected virtual void DeleteAssembly(AssemblyDefinition assembly, string directory)
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

			if (Context.SymbolWriterProvider != null)
				parameters.SymbolWriterProvider = Context.SymbolWriterProvider;
			else
				parameters.WriteSymbols = true;
			return parameters;
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

		static string GetConfigFile (string assembly)
		{
			return assembly + ".config";
		}

		static FileInfo GetOriginalAssemblyFileInfo (AssemblyDefinition assembly)
		{
			return new FileInfo (assembly.MainModule.FileName);
		}

		protected virtual void CopyAssembly (AssemblyDefinition assembly, string directory)
		{
			// Special case.  When an assembly has embedded pdbs, link symbols is not enabled, and the assembly's action is copy,
			// we want to match the behavior of assemblies with the other symbol types and end up with an assembly that does not have symbols.
			// In order to do that, we can't simply copy files.  We need to write the assembly without symbols
			if (assembly.MainModule.HasSymbols && !Context.LinkSymbols && assembly.MainModule.SymbolReader is EmbeddedPortablePdbReader) {
				WriteAssembly (assembly, directory, new WriterParameters ());
				return;
			}

			FileInfo fi = GetOriginalAssemblyFileInfo (assembly);
			string target = Path.GetFullPath (Path.Combine (directory, fi.Name));
			string source = fi.FullName;
			if (source == target)
				return;

			CopyFileAndRemoveReadOnly (source, target);

			if (!Context.LinkSymbols)
				return;

			var mdb = source + ".mdb";
			if (File.Exists (mdb))
				CopyFileAndRemoveReadOnly (mdb, target + ".mdb");

			var pdb = Path.ChangeExtension (source, "pdb");
			if (File.Exists (pdb))
				CopyFileAndRemoveReadOnly (pdb, Path.ChangeExtension (target, "pdb"));
		}

		static void CopyFileAndRemoveReadOnly (string src, string dest) {
			File.Copy (src, dest, true);

			System.IO.FileAttributes attrs = File.GetAttributes (dest);

			if ((attrs & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
				File.SetAttributes (dest, attrs & ~System.IO.FileAttributes.ReadOnly);
		}

		protected virtual string GetAssemblyFileName (AssemblyDefinition assembly, string directory)
		{
			string file = GetOriginalAssemblyFileInfo (assembly).Name;
			return Path.Combine (directory, file);
		}
	}
}
