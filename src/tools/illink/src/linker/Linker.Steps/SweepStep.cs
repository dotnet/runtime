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

	public class SweepStep : BaseStep
	{
		AssemblyDefinition [] assemblies;
		readonly bool sweepSymbols;
		readonly HashSet<AssemblyDefinition> BypassNGenToSave = new HashSet<AssemblyDefinition> ();

		public SweepStep (bool sweepSymbols = true)
		{
			this.sweepSymbols = sweepSymbols;
		}

		protected override void Process ()
		{
			assemblies = Context.Annotations.GetAssemblies ().ToArray ();

			foreach (var assembly in assemblies) {
				RemoveUnusedAssembly (assembly);
			}

			foreach (var assembly in assemblies) {
				ProcessAssemblyAction (assembly);
			}
		}

		void RemoveUnusedAssembly (AssemblyDefinition assembly)
		{
			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.AddBypassNGenUsed:
			case AssemblyAction.CopyUsed:
			case AssemblyAction.Link:
				if (!IsUsedAssembly (assembly))
					RemoveAssembly (assembly);

				break;
			}
		}

		protected void ProcessAssemblyAction (AssemblyDefinition assembly)
		{
			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.AddBypassNGenUsed:
				Annotations.SetAction (assembly, AssemblyAction.AddBypassNGen);
				goto case AssemblyAction.AddBypassNGen;

			case AssemblyAction.AddBypassNGen:
				// FIXME: AddBypassNGen is just wrong, it should not be action as we need to
				// turn it to Action.Save here to e.g. correctly update debug symbols
				if (!Context.KeepTypeForwarderOnlyAssemblies || BypassNGenToSave.Contains (assembly)) {
					goto case AssemblyAction.Save;
				}

				break;

			case AssemblyAction.CopyUsed:
				Annotations.SetAction (assembly, AssemblyAction.Copy);
				goto case AssemblyAction.Copy;

			case AssemblyAction.Copy:
				//
				// Facade assemblies can have unused forwarders pointing to
				// removed type (when facades are kept)
				//
				//		main.exe -> facade.dll -> lib.dll
				//		link     |  copy       |  link
				//
				// when main.exe has unused reference to type in lib.dll
				//
				if (SweepTypeForwarders (assembly))
					Annotations.SetAction (assembly, AssemblyAction.Save);

				break;

			case AssemblyAction.Link:
				SweepAssembly (assembly);
				break;

			case AssemblyAction.Save:
				//
				// Save means we need to rewrite the assembly due to removed assembly
				// reference. We do any additional removed assembly reference clean up here
				//
				UpdateForwardedTypesScope (assembly);
				UpdateCustomAttributesTypesScopes (assembly);
				SweepTypeForwarders (assembly);
				break;
			}
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

			SweepTypeForwarders (assembly);

			UpdateForwardedTypesScope (assembly);
		}

		bool IsUsedAssembly (AssemblyDefinition assembly)
		{
			if (IsMarkedAssembly (assembly))
				return true;

			if (assembly.MainModule.HasExportedTypes && Context.KeepTypeForwarderOnlyAssemblies)
				return true;

			return false;
		}

		bool IsMarkedAssembly (AssemblyDefinition assembly)
		{
			return Annotations.IsMarked (assembly.MainModule);
		}

		protected virtual void RemoveAssembly (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Delete);

			foreach (var a in assemblies) {
				switch (Annotations.GetAction (a)) {
				case AssemblyAction.Skip:
				case AssemblyAction.Delete:
					continue;
				}

				SweepReferences (a, assembly);
			}
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

		void SweepReferences (AssemblyDefinition assembly, AssemblyDefinition referenceToRemove)
		{
			if (assembly == referenceToRemove)
				return;

			bool reference_removed = false;

			var references = assembly.MainModule.AssemblyReferences;
			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];

				AssemblyDefinition ad = Context.Resolver.Resolve (reference);
				if (ad == null || !AreSameReference (ad.Name, referenceToRemove.Name))
					continue;

				ReferenceRemoved (assembly, reference);
				references.RemoveAt (i--);
				reference_removed = true;
			}

			if (reference_removed) {
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.CopyUsed:
					if (IsUsedAssembly (assembly)) {
						goto case AssemblyAction.Copy;
					}
					break;

				case AssemblyAction.Copy:
					//
					// Assembly has a reference to another assembly which has been fully removed. This can
					// happen when for example the reference assembly is 'copy-used' and it's not needed.
					//
					// or
					//
					// Assembly can contain type references with
					// type forwarders to deleted assembly (facade) when
					// facade assemblies are not kept. For that reason we need to
					// rewrite the copy to save to update the scopes not to point
					// forwarding assembly (facade).
					//
					//		foo.dll -> facade.dll    -> lib.dll
					//		copy    |  copy (delete) |  link
					//
					Annotations.SetAction (assembly, AssemblyAction.Save);
					break;

				case AssemblyAction.AddBypassNGenUsed:
					if (IsUsedAssembly (assembly)) {
						Annotations.SetAction (assembly, AssemblyAction.AddBypassNGen);
						goto case AssemblyAction.AddBypassNGen;
					}
					break;

				case AssemblyAction.AddBypassNGen:
					BypassNGenToSave.Add (assembly);
					break;
				}
			}
		}

		bool SweepTypeForwarders (AssemblyDefinition assembly)
		{
			if (assembly.MainModule.HasExportedTypes) {
				return SweepCollectionMetadata (assembly.MainModule.ExportedTypes);
			}

			return false;
		}

		void UpdateForwardedTypesScope (AssemblyDefinition assembly)
		{
			var changed_types = new Dictionary<TypeReference, IMetadataScope> ();

			foreach (TypeReference tr in assembly.MainModule.GetTypeReferences ()) {
				if (tr.IsWindowsRuntimeProjection)
					continue;

				TypeDefinition td;
				try {
					td = tr.Resolve ();
				} catch (AssemblyResolutionException) {
					// Don't crash on unresolved assembly
					continue;
				}

				// at this stage reference might include things that can't be resolved
				// and if it is (resolved) it needs to be kept only if marked (#16213)
				if (td == null || !Annotations.IsMarked (td))
					continue;

				IMetadataScope scope = assembly.MainModule.ImportReference (td).Scope;
				if (tr.Scope != scope)
					changed_types.Add (tr, scope);
			}

			//
			// Resolved everything first before updating scopes.
			// If we set the scope to null, then calling Resolve() on any of its
			// nested types would crash.
			//
			foreach (var e in changed_types) {
				e.Key.Scope = e.Value;
			}

			if (assembly.MainModule.HasExportedTypes) {
				foreach (var et in assembly.MainModule.ExportedTypes) {
					var td = et.Resolve ();
					if (td == null)
						continue;

					et.Scope = assembly.MainModule.ImportReference (td).Scope;
				}
			}
		}

		static void UpdateCustomAttributesTypesScopes (AssemblyDefinition assembly)
		{
			UpdateCustomAttributesTypesScopes ((ICustomAttributeProvider) assembly);

			foreach (var module in assembly.Modules)
				UpdateCustomAttributesTypesScopes (module);

			foreach (var type in assembly.MainModule.Types)
				UpdateCustomAttributesTypesScopes (type);
		}

		static void UpdateCustomAttributesTypesScopes (TypeDefinition typeDefinition)
		{
			UpdateCustomAttributesTypesScopes ((ICustomAttributeProvider)typeDefinition);

			if (typeDefinition.HasEvents)
				UpdateCustomAttributesTypesScopes (typeDefinition.Events);

			if (typeDefinition.HasFields)
				UpdateCustomAttributesTypesScopes (typeDefinition.Fields);

			if (typeDefinition.HasMethods)
				UpdateCustomAttributesTypesScopes (typeDefinition.Methods);

			if (typeDefinition.HasProperties)
				UpdateCustomAttributesTypesScopes (typeDefinition.Properties);

			if (typeDefinition.HasGenericParameters)
				UpdateCustomAttributesTypesScopes (typeDefinition.GenericParameters);

			if (typeDefinition.HasNestedTypes) {
				foreach (var nestedType in typeDefinition.NestedTypes) {
					UpdateCustomAttributesTypesScopes (nestedType);
				}
			}
		}

		static void UpdateCustomAttributesTypesScopes<T> (Collection<T> providers) where T : ICustomAttributeProvider
		{
			foreach (var provider in providers)
				UpdateCustomAttributesTypesScopes (provider);
		}

		static void UpdateCustomAttributesTypesScopes (Collection<GenericParameter> genericParameters)
		{
			foreach (var gp in genericParameters) {
				UpdateCustomAttributesTypesScopes (gp);

				if (gp.HasConstraints)
					UpdateCustomAttributesTypesScopes (gp.Constraints);
			}
		}

		static void UpdateCustomAttributesTypesScopes (ICustomAttributeProvider customAttributeProvider)
		{
			if (!customAttributeProvider.HasCustomAttributes)
				return;

			foreach (var ca in customAttributeProvider.CustomAttributes)
				UpdateForwardedTypesScope (ca);
		}

		static void UpdateForwardedTypesScope (CustomAttribute attribute)
		{
			AssemblyDefinition assembly = attribute.Constructor.Module.Assembly;

			if (attribute.HasConstructorArguments) {
				foreach (var ca in attribute.ConstructorArguments)
					UpdateForwardedTypesScope (ca, assembly);
			}

			if (attribute.HasFields) {
				foreach (var field in attribute.Fields)
					UpdateForwardedTypesScope (field.Argument, assembly);
			}

			if (attribute.HasProperties) {
				foreach (var property in attribute.Properties)
					UpdateForwardedTypesScope (property.Argument, assembly);
			}
		}

		static void UpdateForwardedTypesScope (CustomAttributeArgument attributeArgument, AssemblyDefinition assembly)
		{
			UpdateTypeScope (attributeArgument.Type, assembly);

			switch (attributeArgument.Value) {
			case TypeReference tr:
				UpdateTypeScope (tr, assembly);
				break;
			case CustomAttributeArgument caa:
				UpdateForwardedTypesScope (caa, assembly);
				break;
			case CustomAttributeArgument[] array:
				foreach (var item in array)
					UpdateForwardedTypesScope (item, assembly);
				break;
			}
		}

		static void UpdateTypeScope (TypeReference type, AssemblyDefinition assembly)
		{
			if (type is GenericInstanceType git && git.HasGenericArguments) {
				UpdateTypeScope (git.ElementType, assembly);
				foreach (var ga in git.GenericArguments)
					UpdateTypeScope (ga, assembly);
				return;
			}

			if (type is ArrayType at) {
				UpdateTypeScope (at.ElementType, assembly);
				return;
			}

			TypeDefinition td = type.Resolve ();
			if (td == null)
				return;

			IMetadataScope scope = assembly.MainModule.ImportReference (td).Scope;
			if (type.Scope != scope)
				type.Scope = td.Scope;
		}

		protected virtual void SweepType (TypeDefinition type)
		{
			if (type.HasFields)
				SweepCollectionWithCustomAttributes (type.Fields);

			if (type.HasMethods)
				SweepMethods (type.Methods);

			if (type.HasNestedTypes)
				SweepNestedTypes (type);

			if (type.HasInterfaces)
				SweepInterfaces (type);

			if (type.HasCustomAttributes)
				SweepCustomAttributes (type);

			if (type.HasGenericParameters)
				SweepGenericParameters (type.GenericParameters);

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

		protected void SweepGenericParameters (Collection<GenericParameter> genericParameters)
		{
			foreach (var gp in genericParameters) {
				SweepCustomAttributes (gp);

				if (gp.HasConstraints)
					SweepCustomAttributeCollection (gp.Constraints);
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
				if (Annotations.IsMarked (attribute)) {
					UpdateForwardedTypesScope (attribute);
				} else {
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
			SweepCollectionWithCustomAttributes (methods);
			if (sweepSymbols)
				SweepDebugInfo (methods);

			foreach (var method in methods) {
				if (method.HasGenericParameters)
					SweepGenericParameters (method.GenericParameters);

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

		protected void SweepCollectionWithCustomAttributes<T> (IList<T> list) where T : ICustomAttributeProvider
		{
			for (int i = 0; i < list.Count; i++)
				if (ShouldRemove (list [i])) {
					ElementRemoved (list [i]);
					list.RemoveAt (i--);
				} else {
					SweepCustomAttributes (list [i]);
				}
		}

		protected bool SweepCollectionMetadata<T> (IList<T> list) where T : IMetadataTokenProvider
		{
			bool removed = false;

			for (int i = 0; i < list.Count; i++) {
				if (ShouldRemove (list [i])) {
					ElementRemoved (list [i]);
					list.RemoveAt (i--);
					removed = true;
				}
			}

			return removed;
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
