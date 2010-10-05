//
// LoadI18nAssemblies.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
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

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class LoadI18nAssemblies : BaseStep {

		static readonly byte [] _pktoken = new byte [] {0x07, 0x38, 0xeb, 0x9f, 0x13, 0x2e, 0xd7, 0x56};

		I18nAssemblies _assemblies;

		public LoadI18nAssemblies (I18nAssemblies assemblies)
		{
			_assemblies = assemblies;
		}

		protected override bool ConditionToProcess ()
		{
			return _assemblies != I18nAssemblies.None &&
				Type.GetType ("System.MonoType") != null;
		}

		protected override void Process()
		{
			LoadAssembly (GetAssemblyName (I18nAssemblies.Base));

			LoadI18nAssembly (I18nAssemblies.CJK);
			LoadI18nAssembly (I18nAssemblies.MidEast);
			LoadI18nAssembly (I18nAssemblies.Other);
			LoadI18nAssembly (I18nAssemblies.Rare);
			LoadI18nAssembly (I18nAssemblies.West);
		}

		bool ShouldCopyAssembly (I18nAssemblies current)
		{
			return (current & _assemblies) != 0;
		}

		void LoadI18nAssembly (I18nAssemblies asm)
		{
			if (!ShouldCopyAssembly (asm))
				return;

			AssemblyNameReference name = GetAssemblyName (asm);
			LoadAssembly (name);
		}

		void LoadAssembly (AssemblyNameReference name)
		{
			AssemblyDefinition assembly = Context.Resolve (name);
			ResolveFromAssemblyStep.ProcessLibrary (Context, assembly);
		}

		AssemblyNameReference GetAssemblyName (I18nAssemblies assembly)
		{
			AssemblyNameReference name = new AssemblyNameReference ("I18N", GetCorlibVersion ());
			if (assembly != I18nAssemblies.Base)
				name.Name += "." + assembly;

			name.PublicKeyToken = _pktoken;
			return name;
		}

		Version GetCorlibVersion ()
		{
			foreach (AssemblyDefinition assembly in Context.GetAssemblies ())
				if (assembly.Name.Name == "mscorlib")
					return assembly.Name.Version;

			return new Version ();
		}
	}
}
