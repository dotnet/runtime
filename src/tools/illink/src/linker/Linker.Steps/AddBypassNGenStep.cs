// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{

	public class AddBypassNGenStep : BaseStep
	{

		AssemblyDefinition? coreLibAssembly;
		CustomAttribute? bypassNGenAttribute;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) == AssemblyAction.AddBypassNGen) {
				coreLibAssembly = Context.Resolve (assembly.MainModule.TypeSystem.CoreLibrary);
				if (coreLibAssembly == null)
					return;
				bypassNGenAttribute = null;
				if (assembly == coreLibAssembly) {
					EnsureBypassNGenAttribute (assembly.MainModule);
				}

				foreach (var type in assembly.MainModule.Types)
					ProcessType (type);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			if (type.HasMethods)
				ProcessMethods (type.Methods, type.Module);

			if (type.HasNestedTypes)
				ProcessNestedTypes (type);
		}

		void ProcessMethods (Collection<MethodDefinition> methods, ModuleDefinition module)
		{
			foreach (var method in methods)
				if (!Annotations.IsMarked (method)) {
					EnsureBypassNGenAttribute (module);
					method.CustomAttributes.Add (bypassNGenAttribute);
				}
		}

		void ProcessNestedTypes (TypeDefinition type)
		{
			for (int i = 0; i < type.NestedTypes.Count; i++) {
				var nested = type.NestedTypes[i];
				ProcessType (nested);
			}
		}

		private void EnsureBypassNGenAttribute (ModuleDefinition targetModule)
		{
			Debug.Assert (coreLibAssembly != null);
			if (bypassNGenAttribute != null) {
				return;
			}
			ModuleDefinition corelibMainModule = coreLibAssembly.MainModule;
			TypeReference bypassNGenAttributeRef = new TypeReference ("System.Runtime", "BypassNGenAttribute", corelibMainModule, targetModule.TypeSystem.CoreLibrary);
			TypeDefinition bypassNGenAttributeDef = corelibMainModule.MetadataResolver.Resolve (bypassNGenAttributeRef);
			MethodDefinition? bypassNGenAttributeDefaultConstructor = null;

			if (bypassNGenAttributeDef == null) {
				// System.Runtime.BypassNGenAttribute is not found in corelib. Add it.
				TypeReference systemAttributeRef = new TypeReference ("System", "Attribute", corelibMainModule, targetModule.TypeSystem.CoreLibrary);
				TypeReference systemAttribute = corelibMainModule.MetadataResolver.Resolve (systemAttributeRef);
				systemAttribute = corelibMainModule.ImportReference (systemAttribute);

				if (systemAttribute == null)
					throw new System.ApplicationException ("System.Attribute is not found in " + targetModule.TypeSystem.CoreLibrary.Name);

				MethodReference systemAttributeDefaultConstructorRef = new MethodReference (".ctor", corelibMainModule.TypeSystem.Void, systemAttributeRef);
				MethodReference systemAttributeDefaultConstructor = corelibMainModule.MetadataResolver.Resolve (systemAttributeDefaultConstructorRef);
				systemAttributeDefaultConstructor = corelibMainModule.ImportReference (systemAttributeDefaultConstructor);

				if (systemAttributeDefaultConstructor == null)
					throw new System.ApplicationException ("System.Attribute has no default constructor");

				bypassNGenAttributeDef = new TypeDefinition ("System.Runtime", "BypassNGenAttribute", TypeAttributes.NotPublic | TypeAttributes.Sealed, systemAttribute);

				coreLibAssembly.MainModule.Types.Add (bypassNGenAttributeDef);

				if (Annotations.GetAction (coreLibAssembly) == AssemblyAction.Copy) {
					Annotations.SetAction (coreLibAssembly, AssemblyAction.Save);
				}

				const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
				bypassNGenAttributeDefaultConstructor = new MethodDefinition (".ctor", ctorAttributes, coreLibAssembly.MainModule.TypeSystem.Void);
#pragma warning disable RS0030 // Anything after MarkStep should use Cecil directly as all method bodies should be processed by this point
				var instructions = bypassNGenAttributeDefaultConstructor.Body.Instructions;
#pragma warning restore RS0030
				instructions.Add (Instruction.Create (OpCodes.Ldarg_0));
				instructions.Add (Instruction.Create (OpCodes.Call, systemAttributeDefaultConstructor));
				instructions.Add (Instruction.Create (OpCodes.Ret));

				bypassNGenAttributeDef.Methods.Add (bypassNGenAttributeDefaultConstructor);
			} else {
				foreach (MethodDefinition method in bypassNGenAttributeDef.Methods) {
					if (method.IsConstructor && !method.IsStatic && !method.HasMetadataParameters ()) {
						bypassNGenAttributeDefaultConstructor = method;
						break;
					}
				}

				if (bypassNGenAttributeDefaultConstructor == null) {
					throw new System.ApplicationException ("System.Runtime.BypassNGenAttribute has no default constructor");
				}
			}

			MethodReference defaultConstructorReference = targetModule.ImportReference (bypassNGenAttributeDefaultConstructor);
			bypassNGenAttribute = new CustomAttribute (defaultConstructorReference);
		}
	}
}
