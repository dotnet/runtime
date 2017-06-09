//
// AssemblyResolver.cs
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
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Mono.Linker {

#if FEATURE_ILLINK
	public class AssemblyResolver : DirectoryAssemblyResolver {
#else
	public class AssemblyResolver : BaseAssemblyResolver {
#endif

		readonly Dictionary<string, AssemblyDefinition> _assemblies;

		public IDictionary<string, AssemblyDefinition> AssemblyCache {
			get { return _assemblies; }
		}

		public AssemblyResolver ()
			: this (new Dictionary<string, AssemblyDefinition> (StringComparer.OrdinalIgnoreCase))
		{
		}

		public AssemblyResolver (Dictionary<string, AssemblyDefinition> assembly_cache)
		{
			_assemblies = assembly_cache;
		}

		public override AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			AssemblyDefinition asm;
			if (!_assemblies.TryGetValue (name.Name, out asm)) {
				asm = base.Resolve (name, parameters);
				_assemblies [asm.Name.Name] = asm;
			}

			return asm;
		}

		public virtual AssemblyDefinition CacheAssembly (AssemblyDefinition assembly)
		{
			_assemblies [assembly.Name.Name] = assembly;
			base.AddSearchDirectory (Path.GetDirectoryName (assembly.MainModule.FileName));
			return assembly;
		}

		protected override void Dispose (bool disposing)
		{
			foreach (var asm in _assemblies.Values) {
				asm.Dispose ();
			}

			_assemblies.Clear ();
		}
	}
}
