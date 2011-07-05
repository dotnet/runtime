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
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Steps {

	public class OutputStep : BaseStep {

		protected override void Process ()
		{
			CheckOutputDirectory ();
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

		void OutputAssembly (AssemblyDefinition assembly)
		{
			string directory = Context.OutputDirectory;

			CopyConfigFileIfNeeded (assembly, directory);

			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
				assembly.Write (GetAssemblyFileName (assembly, directory), SaveSymbols (assembly));
				break;
			case AssemblyAction.Copy:
				CloseSymbols (assembly);
				CopyAssembly (GetOriginalAssemblyFileInfo (assembly), directory, Context.LinkSymbols);
				break;
			case AssemblyAction.Delete:
				CloseSymbols (assembly);
				var target = GetAssemblyFileName (assembly, directory);
				if (File.Exists (target))
					File.Delete (target);
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
			return new FileInfo (assembly.MainModule.FullyQualifiedName);
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

			source += ".mdb";
			if (!File.Exists (source))
				return;
			File.Copy (source, target + ".mdb", true);
		}

		static string GetAssemblyFileName (AssemblyDefinition assembly, string directory)
		{
			string file = assembly.Name.Name + (assembly.MainModule.Kind == ModuleKind.Dll ? ".dll" : ".exe");
			return Path.Combine (directory, file);
		}
	}
}
