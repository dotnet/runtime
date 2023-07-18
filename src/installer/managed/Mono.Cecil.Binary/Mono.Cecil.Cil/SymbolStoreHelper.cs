//
// SymbolStoreHelper.cs
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

namespace Mono.Cecil.Cil {

	using System;
	using SR = System.Reflection;

	sealed class SymbolStoreHelper {

		static ISymbolStoreFactory s_factory;

		SymbolStoreHelper ()
		{
		}

		public static ISymbolReader GetReader (ModuleDefinition module)
		{
			InitFactory ();

			return s_factory.CreateReader (module, module.Image.FileInformation.FullName);
		}

		public static ISymbolWriter GetWriter (ModuleDefinition module, string assemblyFileName)
		{
			InitFactory ();

			return s_factory.CreateWriter (module, assemblyFileName);
		}

		static void InitFactory ()
		{
			if (s_factory != null)
				return;

			string assembly_name;
			string type_name = GetSymbolSupportType (out assembly_name);

			Type factoryType = Type.GetType (type_name + ", " + assembly_name, false);
			if (factoryType == null) {
				try {
					SR.Assembly assembly = SR.Assembly.LoadWithPartialName (assembly_name);
					factoryType = assembly.GetType (type_name);
				} catch {}
			}

			if (factoryType == null)
				throw new NotSupportedException ();

			s_factory = (ISymbolStoreFactory) Activator.CreateInstance (factoryType);
		}

		static string GetSymbolSupportType (out string assembly)
		{
			string kind = GetSymbolKind ();
			assembly = "Mono.Cecil." + kind;
			return string.Format (assembly + "." + kind + "Factory");
		}

		static string GetSymbolKind ()
		{
			return OnMono () ? "Mdb" : "Pdb";
		}

		static bool OnMono ()
		{
			return Type.GetType ("Mono.Runtime") != null;
		}
	}
}
