//
// SweepStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class SweepStep : BaseStep {

		AssemblyDefinition [] assemblies;

		protected override void Process ()
		{
			assemblies = Context.GetAssemblies ();
			foreach (var assembly in assemblies)
				SweepAssembly (assembly);
		}

		void SweepAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			if (!IsMarkedAssembly (assembly)) {
				RemoveAssembly (assembly);
				return;
			}

			var types = assembly.MainModule.Types;
			var cloned_types = new List<TypeDefinition> (types);

			types.Clear ();

			foreach (TypeDefinition type in cloned_types) {
				if (Annotations.IsMarked (type)) {
					SweepType (type);
					types.Add (type);
					continue;
				}

				if (type.Name == "<Module>")
					types.Add (type);
			}
		}

		bool IsMarkedAssembly (AssemblyDefinition assembly)
		{
			return Annotations.IsMarked (assembly.MainModule);
		}

		void RemoveAssembly (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Delete);

			SweepReferences (assembly);
		}

		void SweepReferences (AssemblyDefinition target)
		{
			foreach (var assembly in assemblies)
				SweepReferences (assembly, target);
		}

		void SweepReferences (AssemblyDefinition assembly, AssemblyDefinition target)
		{
			var references = assembly.MainModule.AssemblyReferences;
			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				if (!AreSameReference (reference, target.Name))
					continue;

				references.RemoveAt (i);
				return;
			}
		}

		static ICollection Clone (ICollection collection)
		{
			return new ArrayList (collection);
		}

		void SweepType (TypeDefinition type)
		{
			if (type.HasFields)
				SweepCollection (type.Fields);

			if (type.HasMethods)
				SweepCollection (type.Methods);

			if (type.HasNestedTypes)
				SweepNestedTypes (type);
		}

		void SweepNestedTypes (TypeDefinition type)
		{
			for (int i = 0; i < type.NestedTypes.Count; i++) {
				var nested = type.NestedTypes [i];
				if (Annotations.IsMarked (nested)) {
					SweepType (nested);
				} else {
					type.NestedTypes.RemoveAt (i--);
				}
			}
		}

		void SweepCollection (IList list)
		{
			for (int i = 0; i < list.Count; i++)
				if (!Annotations.IsMarked ((IMetadataTokenProvider) list [i]))
					list.RemoveAt (i--);
		}

		static bool AreSameReference (AssemblyNameReference a, AssemblyNameReference b)
		{
			if (a == b)
				return true;

			if (a.Name != b.Name)
				return false;

			if (a.Version != b.Version)
				return false;

			return true;
		}
	}
}
