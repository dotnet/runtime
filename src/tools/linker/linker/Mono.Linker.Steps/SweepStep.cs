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

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;
using Mono.Cecil.Cil;

namespace Mono.Linker.Steps {

	public class SweepStep : BaseStep {

		AssemblyDefinition [] assemblies;
		HashSet<AssemblyDefinition> resolvedTypeReferences;
		readonly bool sweepSymbols;

		public SweepStep (bool sweepSymbols = true)
		{
			this.sweepSymbols = sweepSymbols;
		}

		protected override void Process ()
		{
			assemblies = Context.Annotations.GetAssemblies ().ToArray ();
			foreach (var assembly in assemblies) {
				SweepAssembly (assembly);
				if ((Annotations.GetAction (assembly) == AssemblyAction.Copy) &&
					!Context.KeepTypeForwarderOnlyAssemblies) {
						// Copy assemblies can still contain Type references with
						// type forwarders from Delete assemblies
						// thus try to resolve all the type references and see
						// if some changed the scope. if yes change the action to Save
						if (ResolveAllTypeReferences (assembly))
							Annotations.SetAction (assembly, AssemblyAction.Save);
				}

				AssemblyAction currentAction = Annotations.GetAction(assembly);

				if ((currentAction == AssemblyAction.Link) || (currentAction == AssemblyAction.Save)) {
					// if we save (only or by linking) then unmarked exports (e.g. forwarders) must be cleaned
					// or they can point to nothing which will break later (e.g. when re-loading for stripping IL)
					// reference: https://bugzilla.xamarin.com/show_bug.cgi?id=36577
					if (assembly.MainModule.HasExportedTypes)
						SweepCollection(assembly.MainModule.ExportedTypes);
				}
			}
		}

		void SweepAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			if (!IsMarkedAssembly (assembly)) {
				RemoveAssembly (assembly);
				return;
			}

			var types = new List<TypeDefinition> ();

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				if (Annotations.IsMarked (type)) {
					SweepType (type);
					types.Add (type);
					continue;
				}

				if (type.Name == "<Module>")
					types.Add (type);
			}

			assembly.MainModule.Types.Clear ();
			foreach (TypeDefinition type in types)
				assembly.MainModule.Types.Add (type);
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
			if (assembly == target)
				return;

			var references = assembly.MainModule.AssemblyReferences;
			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				AssemblyDefinition r = null;
				try {
					r = Context.Resolver.Resolve (reference);
				}
				catch (AssemblyResolutionException) {
					continue;
				}
				if (!AreSameReference (r.Name, target.Name))
					continue;

				references.RemoveAt (i);
				// Removing the reference does not mean it will be saved back to disk!
				// That depends on the AssemblyAction set for the `assembly`
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.Copy:
					// Copy means even if "unlinked" we still want that assembly to be saved back 
					// to disk (OutputStep) without the (removed) reference
					Annotations.SetAction (assembly, AssemblyAction.Save);
					if (!Context.KeepTypeForwarderOnlyAssemblies) {
						ResolveAllTypeReferences (assembly);
					}
					break;

				case AssemblyAction.Save:
				case AssemblyAction.Link:
					if (!Context.KeepTypeForwarderOnlyAssemblies) {
						ResolveAllTypeReferences (assembly);
					}
					break;
				}
				return;
			}
		}

		bool ResolveAllTypeReferences (AssemblyDefinition assembly)
		{
			if (resolvedTypeReferences == null)
				resolvedTypeReferences = new HashSet<AssemblyDefinition> ();
			if (resolvedTypeReferences.Contains (assembly))
				return false;
			resolvedTypeReferences.Add (assembly);

			var hash = new Dictionary<TypeReference,IMetadataScope> ();
			bool changes = false;

			foreach (TypeReference tr in assembly.MainModule.GetTypeReferences ()) {
				if (hash.ContainsKey (tr))
					continue;
				if (tr.IsWindowsRuntimeProjection)
					continue;
				var td = tr.Resolve ();
				IMetadataScope scope = tr.Scope;
				// at this stage reference might include things that can't be resolved
				// and if it is (resolved) it needs to be kept only if marked (#16213)
				if ((td != null) && Annotations.IsMarked (td)) {
					scope = assembly.MainModule.ImportReference (td).Scope;
					if (tr.Scope != scope)
						changes = true;
					hash.Add (tr, scope);
				}
			}
			if (assembly.MainModule.HasExportedTypes) {
				foreach (var et in assembly.MainModule.ExportedTypes) {
					var td = et.Resolve ();
					IMetadataScope scope = et.Scope;
					if ((td != null) && Annotations.IsMarked (td)) {
						scope = assembly.MainModule.ImportReference (td).Scope;
						hash.Add (td, scope);
						et.Scope = scope;
					}
				}
			}

			// Resolve everything first before updating scopes.
			// If we set the scope to null, then calling Resolve() on any of its
			// nested types would crash.

			foreach (var e in hash) {
				e.Key.Scope = e.Value;
			}

			return changes;
		}

		protected virtual void SweepType (TypeDefinition type)
		{
			if (type.HasFields)
				SweepCollection (type.Fields);

			if (type.HasMethods)
				SweepMethods (type.Methods);

			if (type.HasNestedTypes)
				SweepNestedTypes (type);
		}

		protected void SweepNestedTypes (TypeDefinition type)
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

		void SweepMethods (Collection<MethodDefinition> methods)
		{
			SweepCollection (methods);
			if (sweepSymbols)
				SweepDebugInfo (methods);
		}

		void SweepDebugInfo (Collection<MethodDefinition> methods)
		{
			List<ScopeDebugInformation> sweptScopes = null;
			foreach (var m in methods) {
				if (m.DebugInformation == null)
					continue;

				var scope = m.DebugInformation.Scope;
				if (scope == null)
					continue;

				if (sweptScopes == null) {
					sweptScopes = new List<ScopeDebugInformation> ();
				} else if (sweptScopes.Contains (scope)) {
					continue;
				}

				sweptScopes.Add (scope);

				if (scope.HasConstants) {
					var constants = scope.Constants;
					for (int i = 0; i < constants.Count; ++i) {
						if (!Annotations.IsMarked (constants [i].ConstantType))
							constants.RemoveAt (i--);
					}
				}

				var import = scope.Import;
				while (import != null) {
					if (import.HasTargets) {
						var targets = import.Targets;
						for (int i = 0; i < targets.Count; ++i) {
							var ttype = targets [i].Type;
							if (ttype != null && !Annotations.IsMarked (ttype))
								targets.RemoveAt (i--);

							// TODO: Clear also AssemblyReference and Namespace when not marked
						}
					}

					import = import.Parent;
				}
			}
		}

		protected void SweepCollection<T> (IList<T> list) where T : IMetadataTokenProvider
		{
			for (int i = 0; i < list.Count; i++)
				if (!Annotations.IsMarked (list [i]))
					list.RemoveAt (i--);
		}

		static bool AreSameReference (AssemblyNameReference a, AssemblyNameReference b)
		{
			if (a == b)
				return true;

			if (a.Name != b.Name)
				return false;

			if (a.Version > b.Version)
				return false;

			return true;
		}
	}
}
