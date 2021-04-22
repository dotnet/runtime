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
		AssemblyDefinition[] assemblies;
		readonly bool sweepSymbols;
		readonly HashSet<AssemblyDefinition> BypassNGenToSave = new HashSet<AssemblyDefinition> ();

		public SweepStep (bool sweepSymbols = true)
		{
			this.sweepSymbols = sweepSymbols;
		}

		protected override void Process ()
		{
			// To keep facades, scan all references so that even unused facades are kept
			assemblies = Context.KeepTypeForwarderOnlyAssemblies ?
				Context.GetReferencedAssemblies ().ToArray () : Annotations.GetAssemblies ().ToArray ();

			// Ensure that any assemblies which need to be removed are marked for deletion,
			// including assemblies which are not referenced by any others.
			foreach (var assembly in assemblies)
				RemoveUnmarkedAssembly (assembly);

			// Look for references (included to previously unresolved assemblies) marked for deletion
			foreach (var assembly in assemblies)
				UpdateAssemblyReferencesToRemovedAssemblies (assembly);

			// Update scopes before removing any type forwarder.
			foreach (var assembly in assemblies) {
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.CopyUsed:
				case AssemblyAction.Link:
				case AssemblyAction.Save:
					SweepAssemblyReferences (assembly);
					break;
				}
			}

			foreach (var assembly in assemblies)
				ProcessAssemblyAction (assembly);

			if (Context.KeepTypeForwarderOnlyAssemblies)
				return;

			// Ensure that we remove any assemblies which were resolved while sweeping references
			foreach (var assembly in Annotations.GetAssemblies ().ToArray ()) {
				if (!assemblies.Any (processedAssembly => processedAssembly == assembly)) {
					Debug.Assert (!IsUsedAssembly (assembly));
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
				if (!IsUsedAssembly (assembly))
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
					AssemblyDefinition ad = Context.Resolver.Resolve (reference);
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
						BypassNGenToSave.Add (assembly);
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

			case AssemblyAction.AddBypassNGen:
				// FIXME: AddBypassNGen is just wrong, it should not be action as we need to
				// turn it to Action.Save here to e.g. correctly update debug symbols
				if (!Context.KeepTypeForwarderOnlyAssemblies || BypassNGenToSave.Contains (assembly)) {
					goto case AssemblyAction.Save;
				}

				break;

			case AssemblyAction.CopyUsed:
				AssemblyAction assemblyAction = AssemblyAction.Copy;
				if (!Context.KeepTypeForwarderOnlyAssemblies && SweepTypeForwarders (assembly))
					assemblyAction = AssemblyAction.Save;

				Annotations.SetAction (assembly, assemblyAction);
				break;

			case AssemblyAction.Copy:
				break;

			case AssemblyAction.Link:
				SweepAssembly (assembly);
				break;

			case AssemblyAction.Save:
				SweepTypeForwarders (assembly);
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
				SweepAssemblyReferences (assembly);
		}

		static void SweepAssemblyReferences (AssemblyDefinition assembly)
		{
			//
			// We used to run over list returned by GetTypeReferences but
			// that returns typeref(s) of original assembly and we don't track
			// which types are needed for which assembly which left us
			// with dangling assembly references
			//
			assembly.MainModule.AssemblyReferences.Clear ();

			var ars = new AssemblyReferencesCorrector (assembly);
			ars.Process ();
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
				if (!provider.HasCustomAttributes || provider.CustomAttributes.All (attr => !IsSecurityAttributeType (Context.TryResolveTypeDefinition (attr.AttributeType))))
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

			definition = Context.TryResolveTypeDefinition (definition.BaseType);
			if (definition == null)
				return false;

			return IsSecurityAttributeType (definition);
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

		struct AssemblyReferencesCorrector
		{
			readonly AssemblyDefinition assembly;
			readonly DefaultMetadataImporter importer;

			HashSet<TypeReference> updated;

			public AssemblyReferencesCorrector (AssemblyDefinition assembly)
			{
				this.assembly = assembly;
				this.importer = new DefaultMetadataImporter (assembly.MainModule);

				updated = null;
			}

			public void Process ()
			{
				updated = new HashSet<TypeReference> ();

				UpdateCustomAttributesTypesScopes (assembly);
				UpdateSecurityAttributesTypesScopes (assembly);

				foreach (var module in assembly.Modules)
					UpdateCustomAttributesTypesScopes (module);

				var mmodule = assembly.MainModule;
				if (mmodule.HasTypes) {
					foreach (var type in mmodule.Types) {
						UpdateScopes (type);
					}
				}

				if (mmodule.HasExportedTypes)
					UpdateTypeScope (mmodule.ExportedTypes);

				updated = null;
			}

			void UpdateScopes (TypeDefinition typeDefinition)
			{
				UpdateCustomAttributesTypesScopes (typeDefinition);
				UpdateSecurityAttributesTypesScopes (typeDefinition);

				if (typeDefinition.BaseType != null)
					UpdateScopeOfTypeReference (typeDefinition.BaseType);

				if (typeDefinition.HasInterfaces) {
					foreach (var iface in typeDefinition.Interfaces) {
						UpdateCustomAttributesTypesScopes (iface);
						UpdateScopeOfTypeReference (iface.InterfaceType);
					}
				}

				if (typeDefinition.HasGenericParameters)
					UpdateTypeScope (typeDefinition.GenericParameters);

				if (typeDefinition.HasEvents) {
					foreach (var e in typeDefinition.Events) {
						UpdateCustomAttributesTypesScopes (e);
						// e.EventType is not saved
					}
				}

				if (typeDefinition.HasFields) {
					foreach (var f in typeDefinition.Fields) {
						UpdateCustomAttributesTypesScopes (f);
						UpdateScopeOfTypeReference (f.FieldType);
						UpdateMarshalInfoTypeScope (f);
					}
				}

				if (typeDefinition.HasMethods) {
					foreach (var m in typeDefinition.Methods) {
						UpdateCustomAttributesTypesScopes (m);
						UpdateSecurityAttributesTypesScopes (m);
						if (m.HasGenericParameters)
							UpdateTypeScope (m.GenericParameters);

						UpdateCustomAttributesTypesScopes (m.MethodReturnType);
						UpdateScopeOfTypeReference (m.MethodReturnType.ReturnType);
						UpdateMarshalInfoTypeScope (m.MethodReturnType);
						if (m.HasOverrides) {
							foreach (var mo in m.Overrides)
								UpdateMethodReference (mo);
						}

						if (m.HasParameters)
							UpdateTypeScope (m.Parameters);

						if (m.HasBody)
							UpdateTypeScope (m.Body);
					}
				}

				if (typeDefinition.HasProperties) {
					foreach (var p in typeDefinition.Properties) {
						UpdateCustomAttributesTypesScopes (p);
						// p.PropertyType is not saved
					}
				}

				if (typeDefinition.HasNestedTypes) {
					foreach (var nestedType in typeDefinition.NestedTypes) {
						UpdateScopes (nestedType);
					}
				}
			}

			void UpdateTypeScope (Collection<GenericParameter> genericParameters)
			{
				foreach (var gp in genericParameters) {
					UpdateCustomAttributesTypesScopes (gp);
					if (gp.HasConstraints)
						UpdateTypeScope (gp.Constraints);
				}
			}

			void UpdateTypeScope (Collection<GenericParameterConstraint> constraints)
			{
				foreach (var gc in constraints) {
					UpdateCustomAttributesTypesScopes (gc);
					UpdateScopeOfTypeReference (gc.ConstraintType);
				}
			}

			void UpdateTypeScope (Collection<ParameterDefinition> parameters)
			{
				foreach (var p in parameters) {
					UpdateCustomAttributesTypesScopes (p);
					UpdateScopeOfTypeReference (p.ParameterType);
					UpdateMarshalInfoTypeScope (p);
				}
			}

			void UpdateTypeScope (Collection<ExportedType> forwarders)
			{
				foreach (var f in forwarders) {
					TypeDefinition td = f.Resolve ();
					if (td == null) {
						// Forwarded type cannot be resolved but it was marked
						// linker is running in --skip-unresolved true mode
						continue;
					}

					var tr = assembly.MainModule.ImportReference (td);
					if (f.Scope != tr.Scope)
						f.Scope = tr.Scope;
				}
			}

			void UpdateTypeScope (MethodBody body)
			{
				if (body.HasVariables) {
					foreach (var v in body.Variables) {
						UpdateScopeOfTypeReference (v.VariableType);
					}
				}

				if (body.HasExceptionHandlers) {
					foreach (var eh in body.ExceptionHandlers) {
						if (eh.CatchType != null)
							UpdateScopeOfTypeReference (eh.CatchType);
					}
				}

				foreach (var instr in body.Instructions) {
					switch (instr.OpCode.OperandType) {

					case OperandType.InlineMethod: {
							var mr = (MethodReference) instr.Operand;
							UpdateMethodReference (mr);
							break;
						}

					case OperandType.InlineField: {
							var fr = (FieldReference) instr.Operand;
							UpdateFieldReference (fr);
							break;
						}

					case OperandType.InlineTok: {
							switch (instr.Operand) {
							case TypeReference tr:
								UpdateScopeOfTypeReference (tr);
								break;
							case FieldReference fr:
								UpdateFieldReference (fr);
								break;
							case MethodReference mr:
								UpdateMethodReference (mr);
								break;
							}

							break;
						}

					case OperandType.InlineType: {
							var tr = (TypeReference) instr.Operand;
							UpdateScopeOfTypeReference (tr);
							break;
						}
					}
				}
			}

			void UpdateMethodReference (MethodReference mr)
			{
				UpdateScopeOfTypeReference (mr.ReturnType);
				UpdateScopeOfTypeReference (mr.DeclaringType);

				if (mr is GenericInstanceMethod gim) {
					foreach (var tr in gim.GenericArguments)
						UpdateScopeOfTypeReference (tr);
				}

				if (mr.HasParameters) {
					UpdateTypeScope (mr.Parameters);
				}
			}

			void UpdateFieldReference (FieldReference fr)
			{
				UpdateScopeOfTypeReference (fr.FieldType);
				UpdateScopeOfTypeReference (fr.DeclaringType);
			}

			void UpdateMarshalInfoTypeScope (IMarshalInfoProvider provider)
			{
				if (!provider.HasMarshalInfo)
					return;

				if (provider.MarshalInfo is CustomMarshalInfo cmi)
					UpdateScopeOfTypeReference (cmi.ManagedType);
			}

			void UpdateCustomAttributesTypesScopes (ICustomAttributeProvider customAttributeProvider)
			{
				if (!customAttributeProvider.HasCustomAttributes)
					return;

				foreach (var ca in customAttributeProvider.CustomAttributes)
					UpdateForwardedTypesScope (ca);
			}

			void UpdateSecurityAttributesTypesScopes (ISecurityDeclarationProvider securityAttributeProvider)
			{
				if (!securityAttributeProvider.HasSecurityDeclarations)
					return;

				foreach (var ca in securityAttributeProvider.SecurityDeclarations) {
					if (!ca.HasSecurityAttributes)
						continue;

					foreach (var securityAttribute in ca.SecurityAttributes)
						UpdateForwardedTypesScope (securityAttribute);
				}
			}

			void UpdateForwardedTypesScope (CustomAttribute attribute)
			{
				UpdateMethodReference (attribute.Constructor);

				if (attribute.HasConstructorArguments) {
					foreach (var ca in attribute.ConstructorArguments)
						UpdateForwardedTypesScope (ca);
				}

				if (attribute.HasFields) {
					foreach (var field in attribute.Fields)
						UpdateForwardedTypesScope (field.Argument);
				}

				if (attribute.HasProperties) {
					foreach (var property in attribute.Properties)
						UpdateForwardedTypesScope (property.Argument);
				}
			}

			void UpdateForwardedTypesScope (SecurityAttribute attribute)
			{
				if (attribute.HasFields) {
					foreach (var field in attribute.Fields)
						UpdateForwardedTypesScope (field.Argument);
				}

				if (attribute.HasProperties) {
					foreach (var property in attribute.Properties)
						UpdateForwardedTypesScope (property.Argument);
				}
			}

			void UpdateForwardedTypesScope (CustomAttributeArgument attributeArgument)
			{
				UpdateScopeOfTypeReference (attributeArgument.Type);

				switch (attributeArgument.Value) {
				case TypeReference tr:
					UpdateScopeOfTypeReference (tr);
					break;
				case CustomAttributeArgument caa:
					UpdateForwardedTypesScope (caa);
					break;
				case CustomAttributeArgument[] array:
					foreach (var item in array)
						UpdateForwardedTypesScope (item);
					break;
				}
			}

			void UpdateScopeOfTypeReference (TypeReference type)
			{
				if (type == null)
					return;

				if (updated.Contains (type))
					return;

				updated.Add (type);

				// Can't update the scope of windows runtime projections
				if (type.IsWindowsRuntimeProjection)
					return;

				switch (type) {
				case GenericInstanceType git:
					UpdateScopeOfTypeReference (git.ElementType);
					foreach (var ga in git.GenericArguments)
						UpdateScopeOfTypeReference (ga);
					return;
				case FunctionPointerType fpt:
					UpdateScopeOfTypeReference (fpt.ReturnType);
					if (fpt.HasParameters)
						UpdateTypeScope (fpt.Parameters);
					return;
				case IModifierType imt:
					UpdateScopeOfTypeReference (imt.ModifierType);
					UpdateScopeOfTypeReference (imt.ElementType);
					return;
				case TypeSpecification ts:
					UpdateScopeOfTypeReference (ts.ElementType);
					return;
				case TypeDefinition:
				case GenericParameter:
					// Nothing to update
					return;
				}

				//
				// Resolve to type definition to remove any type forwarding imports
				//
				TypeDefinition td = type.Resolve ();
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
				if (type.Scope != tr.Scope)
					type.Scope = tr.Scope;
			}
		}
	}
}
