// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	public class SweepStep : BaseStep
	{
		readonly bool sweepSymbols;

		public SweepStep (bool sweepSymbols = true)
		{
			this.sweepSymbols = sweepSymbols;
		}

		protected override void Process ()
		{
			var assemblies = Annotations.GetAssemblies ().ToArray ();

			// Ensure that any assemblies which need to be removed are marked for deletion,
			// including assemblies which are not referenced by any others.
			foreach (var assembly in assemblies)
				RemoveUnmarkedAssembly (assembly);

			// Look for references (included to previously unresolved assemblies) marked for deletion
			foreach (var assembly in assemblies)
				UpdateAssemblyReferencesToRemovedAssemblies (assembly);

			// Update scopes before removing any type forwarder.
			foreach (var assembly in assemblies) {
				var action = Annotations.GetAction (assembly);
				switch (action) {
				case AssemblyAction.CopyUsed:
				case AssemblyAction.Link:
				case AssemblyAction.Save:
					bool changed = AssemblyReferencesCorrector.SweepAssemblyReferences (assembly);
					if (changed && action == AssemblyAction.CopyUsed)
						Annotations.SetAction (assembly, AssemblyAction.Save);
					break;
				}
			}

			foreach (var assembly in assemblies)
				ProcessAssemblyAction (assembly);

			// Ensure that we remove any assemblies which were resolved while sweeping references
			foreach (var assembly in Annotations.GetAssemblies ().ToArray ()) {
				if (!assemblies.Any (processedAssembly => processedAssembly == assembly)) {
					Debug.Assert (!IsMarkedAssembly (assembly));
					Annotations.SetAction (assembly, AssemblyAction.Delete);
				}
			}
		}

		void RemoveUnmarkedAssembly (AssemblyDefinition assembly)
		{
			// Check if unmarked whole assembly can be turned into full
			// assembly removal (AssemblyAction.Delete)
			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.AddBypassNGenUsed:
			case AssemblyAction.CopyUsed:
			case AssemblyAction.Link:
				if (!IsMarkedAssembly (assembly))
					RemoveAssembly (assembly);

				break;
			}
		}

		void UpdateAssemblyReferencesToRemovedAssemblies (AssemblyDefinition assembly)
		{
			var action = Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.Copy:
			case AssemblyAction.Delete:
			case AssemblyAction.Link:
			case AssemblyAction.Save:
			case AssemblyAction.Skip:
				return;

			case AssemblyAction.CopyUsed:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				foreach (var reference in assembly.MainModule.AssemblyReferences) {
					AssemblyDefinition? ad = Context.Resolver.Resolve (reference);
					if (ad == null)
						continue;

					RemoveUnmarkedAssembly (ad);
					if (Annotations.GetAction (ad) != AssemblyAction.Delete)
						continue;

					// Assembly was removed in the output but it's referenced by
					// other assembly with action which does not update references

					switch (action) {
					case AssemblyAction.CopyUsed:
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
						continue;

					case AssemblyAction.AddBypassNGenUsed:
						Annotations.SetAction (assembly, AssemblyAction.AddBypassNGen);
						goto case AssemblyAction.AddBypassNGen;

					case AssemblyAction.AddBypassNGen:
						continue;
					}
				}
				break;
			default:
				throw new ArgumentOutOfRangeException (action.ToString ());
			}
		}

		protected void ProcessAssemblyAction (AssemblyDefinition assembly)
		{
			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.AddBypassNGenUsed:
				Annotations.SetAction (assembly, AssemblyAction.AddBypassNGen);
				goto case AssemblyAction.AddBypassNGen;

			case AssemblyAction.CopyUsed:
				AssemblyAction assemblyAction = AssemblyAction.Copy;
				if (SweepTypeForwarders (assembly)) {
					// Need to sweep references, in case sweeping type forwarders removed any
					AssemblyReferencesCorrector.SweepAssemblyReferences (assembly);
					assemblyAction = AssemblyAction.Save;
				}

				Annotations.SetAction (assembly, assemblyAction);
				break;

			case AssemblyAction.Copy:
				break;

			case AssemblyAction.Link:
				SweepAssembly (assembly);
				break;

			case AssemblyAction.AddBypassNGen:
			// FIXME: AddBypassNGen is just wrong, it should not be action as we need to
			// turn it to Action.Save here to e.g. correctly update debug symbols
			case AssemblyAction.Save:
				if (SweepTypeForwarders (assembly)) {
					// Need to sweep references, in case sweeping type forwarders removed any
					AssemblyReferencesCorrector.SweepAssemblyReferences (assembly);
				}
				break;
			}
		}

		protected virtual void SweepAssembly (AssemblyDefinition assembly)
		{
			var types = new List<TypeDefinition> ();
			ModuleDefinition main = assembly.MainModule;
			bool updateScopes = false;

			foreach (TypeDefinition type in main.Types) {
				if (!ShouldRemove (type)) {
					SweepType (type);
					types.Add (type);
					updateScopes = true;
					continue;
				}

				// Is <Module> type.
				if (type.MetadataToken.RID == 1)
					types.Add (type);
				else
					ElementRemoved (type);
			}

			main.Types.Clear ();
			foreach (TypeDefinition type in types)
				main.Types.Add (type);

			SweepResources (assembly);
			updateScopes |= SweepCustomAttributes (assembly);

			foreach (var module in assembly.Modules)
				updateScopes |= SweepCustomAttributes (module);

			//
			// MainModule module references are used by pinvoke
			//
			if (main.HasModuleReferences)
				updateScopes |= SweepCollectionMetadata (main.ModuleReferences);

			if (main.EntryPoint != null && !Annotations.IsMarked (main.EntryPoint)) {
				main.EntryPoint = null;
			}

			if (SweepTypeForwarders (assembly) || updateScopes)
				AssemblyReferencesCorrector.SweepAssemblyReferences (assembly);
		}

		bool IsMarkedAssembly (AssemblyDefinition assembly)
		{
			return Annotations.IsMarked (assembly.MainModule);
		}

		bool CanSweepNamesForMember (IMemberDefinition member, MetadataTrimming metadataTrimming)
		{
			return (Context.MetadataTrimming & metadataTrimming) != 0 && !Annotations.IsReflectionUsed (member);
		}

		protected virtual void RemoveAssembly (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Delete);
		}

		void SweepResources (AssemblyDefinition assembly)
		{
			var resourcesToRemove = Annotations.GetResourcesToRemove (assembly);
			if (resourcesToRemove == null)
				return;

			var resources = assembly.MainModule.Resources;
			foreach (var resource in resourcesToRemove)
				resources.Remove (resource);
		}

		bool SweepTypeForwarders (AssemblyDefinition assembly)
		{
			return assembly.MainModule.HasExportedTypes &&
				SweepCollectionMetadata (assembly.MainModule.ExportedTypes);
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
				var nested = type.NestedTypes[i];
				if (ShouldRemove (nested)) {
					ElementRemoved (type.NestedTypes[i]);
					type.NestedTypes.RemoveAt (i--);
				} else {
					SweepType (nested);
				}
			}
		}

		protected void SweepInterfaces (TypeDefinition type)
		{
			for (int i = type.Interfaces.Count - 1; i >= 0; i--) {
				var iface = type.Interfaces[i];
				if (ShouldRemove (iface)) {
					InterfaceRemoved (type, iface);
					type.Interfaces.RemoveAt (i);
				} else {
					SweepCustomAttributes (iface);
				}
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
			bool removed = SweepCustomAttributes (type as ICustomAttributeProvider);

			if (removed && type.HasSecurity && ShouldSetHasSecurityToFalse (type, type))
				type.HasSecurity = false;
		}

		protected void SweepCustomAttributes (MethodDefinition method)
		{
			bool removed = SweepCustomAttributes (method as ICustomAttributeProvider);

			if (removed && method.HasSecurity && ShouldSetHasSecurityToFalse (method, method))
				method.HasSecurity = false;
		}

		bool ShouldSetHasSecurityToFalse (ISecurityDeclarationProvider providerAsSecurity, ICustomAttributeProvider provider)
		{
			if (!providerAsSecurity.HasSecurityDeclarations) {
				// If the method or type had security before and all attributes were removed, or no remaining attributes are security attributes,
				// then we need to set HasSecurity to false
				if (!provider.HasCustomAttributes || provider.CustomAttributes.All (attr => {
					TypeDefinition? attributeType = Context.TryResolve (attr.AttributeType);
					return attributeType == null || !IsSecurityAttributeType (attributeType);
				}))
					return true;
			}

			return false;
		}

		bool IsSecurityAttributeType (TypeDefinition definition)
		{
			if (definition == null)
				return false;

			if (definition.Namespace == "System.Security") {
				return definition.FullName switch {
					// This seems to be one attribute in the System.Security namespace that doesn't count
					// as an attribute that requires HasSecurity to be true
					"System.Security.SecurityCriticalAttribute" => false,
					_ => true,
				};
			}

			var baseDefinition = Context.TryResolve (definition.BaseType);
			if (baseDefinition == null)
				return false;

			return IsSecurityAttributeType (baseDefinition);
		}

		protected bool SweepCustomAttributes (ICustomAttributeProvider provider)
		{
			bool removed = false;

			for (int i = provider.CustomAttributes.Count - 1; i >= 0; i--) {
				var attribute = provider.CustomAttributes[i];
				if (!Annotations.IsMarked (attribute)) {
					CustomAttributeUsageRemoved (provider, attribute);
					provider.CustomAttributes.RemoveAt (i);
					removed = true;
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

				SweepOverrides (method);

				if (!method.HasMetadataParameters ())
					continue;

				bool sweepNames = CanSweepNamesForMember (method, MetadataTrimming.ParameterName);

#pragma warning disable RS0030 // MethodReference.Parameters is banned. It makes sense to use when directly working with the Cecil type system though.
				foreach (var parameter in method.Parameters) {
					if (sweepNames)
						parameter.Name = null;

					SweepCustomAttributes (parameter);
				}
#pragma warning restore RS0030
			}
		}
		void SweepOverrides (MethodDefinition method)
		{
			for (int i = 0; i < method.Overrides.Count;) {
				// We can't rely on the context resolution cache anymore, since it may remember methods which are already removed
				// So call the direct Resolve here and avoid the cache.
				// We want to remove a method from the list of Overrides if:
				//	Resolve() is null
				//		This can happen for a couple of reasons, but it indicates the method isn't in the final assembly.
				//		Resolve also may return a removed value if method.Overrides[i] is a MethodDefinition. In this case, Resolve short circuits and returns `this`.
				//	OR
				//	ov.DeclaringType is null
				//		ov.DeclaringType may be null if Resolve short circuited and returned a removed method. In this case, we want to remove the override.
				//	OR
				//	ov is in a `link` scope and is unmarked
				//		ShouldRemove returns true if the method is unmarked, but we also We need to make sure the override is in a link scope.
				//		Only things in a link scope are marked, so ShouldRemove is only valid for items in a `link` scope.
#pragma warning disable RS0030 // Cecil's Resolve is banned - it's necessary when the metadata graph isn't stable
				if (method.Overrides[i].Resolve () is not MethodDefinition ov || ov.DeclaringType is null || (IsLinkScope (ov.DeclaringType.Scope) && ShouldRemove (ov)))
					method.Overrides.RemoveAt (i);
				else
					i++;
#pragma warning restore RS0030
			}
		}

		/// <summary>
		/// Returns true if the assembly of the <paramref name="scope"></paramref> is set to link
		/// </summary>
		private bool IsLinkScope (IMetadataScope scope)
		{
			AssemblyDefinition? assembly = Context.Resolve (scope);
			return assembly != null && Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		void SweepDebugInfo (Collection<MethodDefinition> methods)
		{
			List<ScopeDebugInformation>? sweptScopes = null;
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
						if (!Annotations.IsMarked (constants[i].ConstantType))
							constants.RemoveAt (i--);
					}
				}

				var import = scope.Import;
				while (import != null) {
					if (import.HasTargets) {
						var targets = import.Targets;
						for (int i = 0; i < targets.Count; ++i) {
							var ttype = targets[i].Type;
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
				if (ShouldRemove (list[i])) {
					ElementRemoved (list[i]);
					list.RemoveAt (i--);
				} else {
					SweepCustomAttributes (list[i]);
				}
		}

		protected bool SweepCollectionMetadata<T> (IList<T> list) where T : IMetadataTokenProvider
		{
			bool removed = false;

			for (int i = 0; i < list.Count; i++) {
				if (ShouldRemove (list[i])) {
					ElementRemoved (list[i]);
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

		protected virtual void ElementRemoved (IMetadataTokenProvider element)
		{
		}

		protected virtual void InterfaceRemoved (TypeDefinition type, InterfaceImplementation iface)
		{
		}

		protected virtual void CustomAttributeUsageRemoved (ICustomAttributeProvider provider, CustomAttribute attribute)
		{
		}

		sealed class AssemblyReferencesCorrector : TypeReferenceWalker
		{
			readonly DefaultMetadataImporter importer;

			bool changedAnyScopes;

			AssemblyReferencesCorrector (AssemblyDefinition assembly) : base (assembly)
			{
				this.importer = new DefaultMetadataImporter (assembly.MainModule);
				changedAnyScopes = false;
			}

			public static bool SweepAssemblyReferences (AssemblyDefinition assembly)
			{
				//
				// We used to run over list returned by GetTypeReferences but
				// that returns typeref(s) of original assembly and we don't track
				// which types are needed for which assembly which left us
				// with dangling assembly references
				//
				assembly.MainModule.AssemblyReferences.Clear ();

				var arc = new AssemblyReferencesCorrector (assembly);
				arc.Process ();

				return arc.changedAnyScopes;
			}

			protected override void ProcessTypeReference (TypeReference type)
			{
				//
				// Resolve to type definition to remove any type forwarding imports
				//
				// Workaround for https://github.com/dotnet/linker/issues/2260
				// Context has a cache which stores ref->def mapping. This code runs during sweeping
				// which can remove the type-def from its assembly, effectively making the ref unresolvable.
				// But the cache doesn't know that, it would still "resolve" the type-ref to now defunct type-def.
				// For this reason we can't use the context resolution here, and must force Cecil to perform
				// real type resolution again (since it can fail, and that's OK).
#pragma warning disable RS0030 // Cecil's Resolve is banned -- it's necessary when the metadata graph isn't stable
				TypeDefinition td = type.Resolve ();
#pragma warning restore RS0030
				if (td == null) {
					//
					// This can happen when not all assembly refences were provided and we
					// run in `--skip-unresolved` mode. We cannot fully sweep and keep the
					// original assembly reference
					//
					var anr = (AssemblyNameReference) type.Scope;
					type.Scope = importer.ImportReference (anr);
					return;
				}

				var tr = assembly.MainModule.ImportReference (td);
				if (type.Scope == tr.Scope)
					return;

				type.Scope = tr.Scope;
				changedAnyScopes = true;
			}

			protected override void ProcessExportedType (ExportedType exportedType)
			{
#pragma warning disable RS0030 // Cecil's Resolve is banned -- it's necessary when the metadata graph is unstable
				TypeDefinition? td = exportedType.Resolve ();
#pragma warning restore RS0030
				if (td == null) {
					// Forwarded type cannot be resolved but it was marked
					// linker is running in --skip-unresolved true mode
					var anr = (AssemblyNameReference) exportedType.Scope;
					exportedType.Scope = importer.ImportReference (anr);
					return;
				}

				var tr = assembly.MainModule.ImportReference (td);
				if (exportedType.Scope == tr.Scope)
					return;

				exportedType.Scope = tr.Scope;
				changedAnyScopes = true;
			}
		}
	}
}
