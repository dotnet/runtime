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
				ProcessAssemblyAction (assembly);
				if ((Annotations.GetAction (assembly) == AssemblyAction.Copy) &&
					!Context.KeepTypeForwarderOnlyAssemblies) {
						// Copy assemblies can still contain Type references with
						// type forwarders from Delete assemblies
						// thus try to resolve all the type references and see
						// if some changed the scope. if yes change the action to Save
						if (ResolveAllTypeReferences (assembly))
							Annotations.SetAction (assembly, AssemblyAction.Save);
				}

				AssemblyAction currentAction = Annotations.GetAction (assembly);

				if ((currentAction == AssemblyAction.Link) || (currentAction == AssemblyAction.Save)) {
					// if we save (only or by linking) then unmarked exports (e.g. forwarders) must be cleaned
					// or they can point to nothing which will break later (e.g. when re-loading for stripping IL)
					// reference: https://bugzilla.xamarin.com/show_bug.cgi?id=36577
					if (assembly.MainModule.HasExportedTypes)
						SweepCollectionNonAttributable (assembly.MainModule.ExportedTypes);
				}
			}
		}

		protected void ProcessAssemblyAction (AssemblyDefinition assembly)
		{
			switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.Link:
					if (!IsMarkedAssembly (assembly)) {
						RemoveAssembly (assembly);
						return;
					}
					break;

				case AssemblyAction.AddBypassNGenUsed:
					if (!IsMarkedAssembly (assembly)) {
						RemoveAssembly (assembly);
					} else {
						Annotations.SetAction (assembly, AssemblyAction.AddBypassNGen);
					}
					return;

				case AssemblyAction.CopyUsed:
					if (!IsMarkedAssembly (assembly)) {
						RemoveAssembly (assembly);
					} else {
						Annotations.SetAction (assembly, AssemblyAction.Copy);
					}
					return;

				default:
					return;
			}
			
			SweepAssembly (assembly);
		}

		protected virtual void SweepAssembly (AssemblyDefinition assembly)
		{
			var types = new List<TypeDefinition> ();

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				if (Annotations.IsMarked (type)) {
					SweepType (type);
					types.Add (type);
					continue;
				}

				if (type.Name == "<Module>")
					types.Add (type);
				else
					ElementRemoved (type);
			}

			assembly.MainModule.Types.Clear ();
			foreach (TypeDefinition type in types)
				assembly.MainModule.Types.Add (type);

			SweepResources (assembly);
			SweepCustomAttributes (assembly);

			foreach (var module in assembly.Modules)
				SweepCustomAttributes (module);
		}

		bool IsMarkedAssembly (AssemblyDefinition assembly)
		{
			return Annotations.IsMarked (assembly.MainModule);
		}

		protected virtual void RemoveAssembly (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Delete);

			SweepReferences (assembly);
		}

		void SweepResources (AssemblyDefinition assembly)
		{
			var resourcesToRemove = Annotations.GetResourcesToRemove (assembly);
			if (resourcesToRemove != null) {
				var resources = assembly.MainModule.Resources;

				for (int i = 0; i < resources.Count; i++) {
					var resource = resources [i] as EmbeddedResource;
					if (resource == null)
						continue;

					if (resourcesToRemove.Contains (resource.Name))
						resources.RemoveAt (i--);
				}
			}
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
				AssemblyDefinition r = Context.Resolver.Resolve (reference);
				if (r == null)
					continue;
				if (!AreSameReference (r.Name, target.Name))
					continue;

				ReferenceRemoved (assembly, reference);
				// removal from `references` requires an adjustment to `i`
				references.RemoveAt (i--);
				// Removing the reference does not mean it will be saved back to disk!
				// That depends on the AssemblyAction set for the `assembly`
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.Copy:
					// We need to save the assembly if a reference was removed, otherwise we can end up
					// with an assembly that references an assembly that no longer exists
					Annotations.SetAction (assembly, AssemblyAction.Save);
					// Copy means even if "unlinked" we still want that assembly to be saved back 
					// to disk (OutputStep) without the (removed) reference
					if (!Context.KeepTypeForwarderOnlyAssemblies) {
						ResolveAllTypeReferences (assembly);
					}
					break;

				case AssemblyAction.CopyUsed:
					if (IsMarkedAssembly (assembly) && !Context.KeepTypeForwarderOnlyAssemblies) {
						Annotations.SetAction (assembly, AssemblyAction.Save);
						ResolveAllTypeReferences (assembly);
					}
					break;

				case AssemblyAction.Save:
				case AssemblyAction.Link:
				case AssemblyAction.AddBypassNGen:
				case AssemblyAction.AddBypassNGenUsed:
					if (!Context.KeepTypeForwarderOnlyAssemblies) {
						ResolveAllTypeReferences (assembly);
					}
					break;
				}
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

			if (type.HasInterfaces)
				SweepInterfaces (type);

			if (type.HasCustomAttributes)
				SweepCustomAttributes (type);

			if (type.HasGenericParameters)
				SweepCustomAttributeCollection (type.GenericParameters);

			if (type.HasProperties)
				SweepCustomAttributeCollection (type.Properties);

			if (type.HasEvents)
				SweepCustomAttributeCollection (type.Events);

			if (type.HasFields && !type.IsBeforeFieldInit && !Annotations.HasPreservedStaticCtor (type) && !type.IsEnum)
				type.IsBeforeFieldInit = true;
		}

		protected void SweepNestedTypes (TypeDefinition type)
		{
			for (int i = 0; i < type.NestedTypes.Count; i++) {
				var nested = type.NestedTypes [i];
				if (Annotations.IsMarked (nested)) {
					SweepType (nested);
				} else {
					ElementRemoved (type.NestedTypes [i]);
					type.NestedTypes.RemoveAt (i--);
				}
			}
		}

		protected void SweepInterfaces (TypeDefinition type)
		{
			for (int i = type.Interfaces.Count - 1; i >= 0; i--) {
				var iface = type.Interfaces [i];
				if (Annotations.IsMarked (iface)) {
					SweepCustomAttributes (iface);
					continue;
				}
				InterfaceRemoved (type, iface);
				type.Interfaces.RemoveAt (i);
			}
		}

		protected void SweepCustomAttributes (TypeDefinition type)
		{
			var removed = SweepCustomAttributes (type as ICustomAttributeProvider);

			if (ShouldSetHasSecurityToFalse (type, type, type.HasSecurity, removed))
				type.HasSecurity = false;
		}

		protected void SweepCustomAttributes (MethodDefinition method)
		{
			var removed = SweepCustomAttributes (method as ICustomAttributeProvider);

			if (ShouldSetHasSecurityToFalse (method, method, method.HasSecurity, removed))
				method.HasSecurity = false;
		}

		bool ShouldSetHasSecurityToFalse (ISecurityDeclarationProvider providerAsSecurity, ICustomAttributeProvider provider, bool existingHasSecurity, IList<CustomAttribute> removedAttributes)
		{
			if (existingHasSecurity && removedAttributes.Count > 0 && !providerAsSecurity.HasSecurityDeclarations) {
				// If the method or type had security before and all attributes were removed, or no remaining attributes are security attributes,
				// then we need to set HasSecurity to false
				if (provider.CustomAttributes.Count == 0 || provider.CustomAttributes.All (attr => !IsSecurityAttributeType (attr.AttributeType.Resolve ())))
					return true;
			}

			return false;
		}

		static bool IsSecurityAttributeType (TypeDefinition definition)
		{
			if (definition == null)
				return false;

			if (definition.Namespace == "System.Security") {
				switch (definition.FullName) {
					// This seems to be one attribute in the System.Security namespace that doesn't count
					// as an attribute that requires HasSecurity to be true
					case "System.Security.SecurityCriticalAttribute":
						return false;
				}

				return true;
			}

			if (definition.BaseType == null)
				return false;

			return IsSecurityAttributeType (definition.BaseType.Resolve ());
		}

		protected IList<CustomAttribute> SweepCustomAttributes (ICustomAttributeProvider provider)
		{
			var removed = new List<CustomAttribute>();

			for (int i = provider.CustomAttributes.Count - 1; i >= 0; i--) {
				var attribute = provider.CustomAttributes [i];
				if (!Annotations.IsMarked (attribute)) {
					CustomAttributeUsageRemoved (provider, attribute);
					removed.Add (provider.CustomAttributes [i]);
					provider.CustomAttributes.RemoveAt (i);
				}
			}

			return removed;
		}

		protected void SweepCustomAttributeCollection<T> (Collection<T> providers) where T : ICustomAttributeProvider
		{
			foreach (var provider in providers)
				SweepCustomAttributes (provider);
		}

		protected virtual void SweepMethods (Collection<MethodDefinition> methods)
		{
			SweepCollection (methods);
			if (sweepSymbols)
				SweepDebugInfo (methods);

			foreach (var method in methods) {
				if (method.HasGenericParameters)
					SweepCustomAttributeCollection (method.GenericParameters);

				SweepCustomAttributes (method.MethodReturnType);

				if (!method.HasParameters)
					continue;

				foreach (var parameter in method.Parameters)
					SweepCustomAttributes (parameter);
			}
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

		protected void SweepCollection (IList<MethodDefinition> list)
		{
			for (int i = 0; i < list.Count; i++)
				if (ShouldRemove (list [i])) {
					ElementRemoved (list [i]);
					list.RemoveAt (i--);
				} else {
					SweepCustomAttributes (list [i]);
				}
		}

		protected void SweepCollection<T> (IList<T> list) where T : ICustomAttributeProvider
		{
			for (int i = 0; i < list.Count; i++)
				if (ShouldRemove (list [i])) {
					ElementRemoved (list [i]);
					list.RemoveAt (i--);
				} else {
					SweepCustomAttributes (list [i]);
				}
		}

		protected void SweepCollectionNonAttributable<T> (IList<T> list) where T : IMetadataTokenProvider
		{
			for (int i = 0; i < list.Count; i++)
				if (ShouldRemove (list [i])) {
					ElementRemoved (list [i]);
					list.RemoveAt (i--);
				}
		}

		protected virtual bool ShouldRemove<T> (T element) where T : IMetadataTokenProvider
		{
			return !Annotations.IsMarked (element);
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

		protected virtual void ElementRemoved (IMetadataTokenProvider element)
		{
		}

		protected virtual void ReferenceRemoved (AssemblyDefinition assembly, AssemblyNameReference reference)
		{
		}

		protected virtual void InterfaceRemoved (TypeDefinition type, InterfaceImplementation iface)
		{
		}

		protected virtual void CustomAttributeUsageRemoved (ICustomAttributeProvider provider, CustomAttribute attribute)
		{
		}
	}
}
