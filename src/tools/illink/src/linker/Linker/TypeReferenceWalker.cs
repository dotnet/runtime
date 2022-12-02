// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker
{
	abstract class TypeReferenceWalker
	{
		protected readonly AssemblyDefinition assembly;

		protected HashSet<TypeReference> Visited { get; } = new HashSet<TypeReference> ();

		public TypeReferenceWalker (AssemblyDefinition assembly)
		{
			this.assembly = assembly;
		}

		// Traverse the assembly and mark the scopes of discovered type references (but not exported types).
		// This includes scopes referenced by Cecil TypeReference objects that don't represent rows in the typeref table,
		// such as references to built-in types, or attribute arguments which encode type references as strings.
		protected virtual void Process ()
		{
			if (Visited.Count > 0)
				throw new InvalidOperationException ();

			WalkCustomAttributesTypesScopes (assembly);
			WalkSecurityAttributesTypesScopes (assembly);

			foreach (var module in assembly.Modules)
				WalkCustomAttributesTypesScopes (module);

			var mmodule = assembly.MainModule;
			if (mmodule.HasTypes) {
				foreach (var type in mmodule.Types) {
					WalkScopes (type);
				}
			}

			if (mmodule.HasExportedTypes)
				WalkTypeScope (mmodule.ExportedTypes);

			ProcessExtra ();
		}

		protected virtual void ProcessExtra () { }

		void WalkScopes (TypeDefinition typeDefinition)
		{
			WalkCustomAttributesTypesScopes (typeDefinition);
			WalkSecurityAttributesTypesScopes (typeDefinition);

			if (typeDefinition.BaseType != null)
				WalkScopeOfTypeReference (typeDefinition.BaseType);

			if (typeDefinition.HasInterfaces) {
				foreach (var iface in typeDefinition.Interfaces) {
					WalkCustomAttributesTypesScopes (iface);
					WalkScopeOfTypeReference (iface.InterfaceType);
				}
			}

			if (typeDefinition.HasGenericParameters)
				WalkTypeScope (typeDefinition.GenericParameters);

			if (typeDefinition.HasEvents) {
				foreach (var e in typeDefinition.Events) {
					WalkCustomAttributesTypesScopes (e);
					// e.EventType is not saved
				}
			}

			if (typeDefinition.HasFields) {
				foreach (var f in typeDefinition.Fields) {
					WalkCustomAttributesTypesScopes (f);
					WalkScopeOfTypeReference (f.FieldType);
					WalkMarshalInfoTypeScope (f);
				}
			}

			if (typeDefinition.HasMethods) {
				foreach (var m in typeDefinition.Methods) {
					WalkCustomAttributesTypesScopes (m);
					WalkSecurityAttributesTypesScopes (m);
					if (m.HasGenericParameters)
						WalkTypeScope (m.GenericParameters);

					WalkCustomAttributesTypesScopes (m.MethodReturnType);
					WalkScopeOfTypeReference (m.MethodReturnType.ReturnType);
					WalkMarshalInfoTypeScope (m.MethodReturnType);
					if (m.HasOverrides) {
						foreach (var mo in m.Overrides)
							WalkMethodReference (mo);
					}
#pragma warning disable RS0030 // MethodReference.Parameters is banned - It's best to leave this as is
					if (m.HasMetadataParameters ())
						WalkTypeScope (m.Parameters);
#pragma warning restore RS0030

					if (m.HasBody)
						WalkTypeScope (m.Body);
				}
			}

			if (typeDefinition.HasProperties) {
				foreach (var p in typeDefinition.Properties) {
					WalkCustomAttributesTypesScopes (p);
					// p.PropertyType is not saved
				}
			}

			if (typeDefinition.HasNestedTypes) {
				foreach (var nestedType in typeDefinition.NestedTypes) {
					WalkScopes (nestedType);
				}
			}
		}

		void WalkTypeScope (Collection<GenericParameter> genericParameters)
		{
			foreach (var gp in genericParameters) {
				WalkCustomAttributesTypesScopes (gp);
				if (gp.HasConstraints)
					WalkTypeScope (gp.Constraints);
			}
		}

		void WalkTypeScope (Collection<GenericParameterConstraint> constraints)
		{
			foreach (var gc in constraints) {
				WalkCustomAttributesTypesScopes (gc);
				WalkScopeOfTypeReference (gc.ConstraintType);
			}
		}

		void WalkTypeScope (Collection<ParameterDefinition> parameters)
		{
			foreach (var p in parameters) {
				WalkCustomAttributesTypesScopes (p);
				WalkScopeOfTypeReference (p.ParameterType);
				WalkMarshalInfoTypeScope (p);
			}
		}

		void WalkTypeScope (Collection<ExportedType> forwarders)
		{
			foreach (var f in forwarders)
				ProcessExportedType (f);
		}

		void WalkTypeScope (MethodBody body)
		{
#pragma warning disable RS0030 // Processing type references should not trigger method marking/processing, so access Cecil directly
			if (body.HasVariables) {
				foreach (var v in body.Variables) {
					WalkScopeOfTypeReference (v.VariableType);
				}
			}

			if (body.HasExceptionHandlers) {
				foreach (var eh in body.ExceptionHandlers) {
					if (eh.CatchType != null)
						WalkScopeOfTypeReference (eh.CatchType);
				}
			}

			foreach (var instr in body.Instructions) {
				switch (instr.OpCode.OperandType) {

				case OperandType.InlineMethod: {
						var mr = (MethodReference) instr.Operand;
						WalkMethodReference (mr);
						break;
					}

				case OperandType.InlineField: {
						var fr = (FieldReference) instr.Operand;
						WalkFieldReference (fr);
						break;
					}

				case OperandType.InlineTok: {
						switch (instr.Operand) {
						case TypeReference tr:
							WalkScopeOfTypeReference (tr);
							break;
						case FieldReference fr:
							WalkFieldReference (fr);
							break;
						case MethodReference mr:
							WalkMethodReference (mr);
							break;
						}

						break;
					}

				case OperandType.InlineType: {
						var tr = (TypeReference) instr.Operand;
						WalkScopeOfTypeReference (tr);
						break;
					}
				}
			}
#pragma warning restore RS0030 // Do not used banned APIs
		}

		void WalkMethodReference (MethodReference mr)
		{
			WalkScopeOfTypeReference (mr.ReturnType);
			WalkScopeOfTypeReference (mr.DeclaringType);

			if (mr is GenericInstanceMethod gim) {
				foreach (var tr in gim.GenericArguments)
					WalkScopeOfTypeReference (tr);
			}

			if (mr.HasMetadataParameters ()) {
#pragma warning disable RS0030 // MethedReference.Parameters is banned. Best to leave working code as is.
				WalkTypeScope (mr.Parameters);
#pragma warning restore RS0030 // Do not used banned APIs
			}
		}

		void WalkFieldReference (FieldReference fr)
		{
			WalkScopeOfTypeReference (fr.FieldType);
			WalkScopeOfTypeReference (fr.DeclaringType);
		}

		void WalkMarshalInfoTypeScope (IMarshalInfoProvider provider)
		{
			if (!provider.HasMarshalInfo)
				return;

			if (provider.MarshalInfo is CustomMarshalInfo cmi)
				WalkScopeOfTypeReference (cmi.ManagedType);
		}

		void WalkCustomAttributesTypesScopes (ICustomAttributeProvider customAttributeProvider)
		{
			if (!customAttributeProvider.HasCustomAttributes)
				return;

			foreach (var ca in customAttributeProvider.CustomAttributes)
				WalkForwardedTypesScope (ca);
		}

		void WalkSecurityAttributesTypesScopes (ISecurityDeclarationProvider securityAttributeProvider)
		{
			if (!securityAttributeProvider.HasSecurityDeclarations)
				return;

			foreach (var ca in securityAttributeProvider.SecurityDeclarations) {
				if (!ca.HasSecurityAttributes)
					continue;

				foreach (var securityAttribute in ca.SecurityAttributes)
					WalkForwardedTypesScope (securityAttribute);
			}
		}

		void WalkForwardedTypesScope (CustomAttribute attribute)
		{
			WalkMethodReference (attribute.Constructor);

			if (attribute.HasConstructorArguments) {
				foreach (var ca in attribute.ConstructorArguments)
					WalkForwardedTypesScope (ca);
			}

			if (attribute.HasFields) {
				foreach (var field in attribute.Fields)
					WalkForwardedTypesScope (field.Argument);
			}

			if (attribute.HasProperties) {
				foreach (var property in attribute.Properties)
					WalkForwardedTypesScope (property.Argument);
			}
		}

		void WalkForwardedTypesScope (SecurityAttribute attribute)
		{
			if (attribute.HasFields) {
				foreach (var field in attribute.Fields)
					WalkForwardedTypesScope (field.Argument);
			}

			if (attribute.HasProperties) {
				foreach (var property in attribute.Properties)
					WalkForwardedTypesScope (property.Argument);
			}
		}

		void WalkForwardedTypesScope (CustomAttributeArgument attributeArgument)
		{
			WalkScopeOfTypeReference (attributeArgument.Type);

			switch (attributeArgument.Value) {
			case TypeReference tr:
				WalkScopeOfTypeReference (tr);
				break;
			case CustomAttributeArgument caa:
				WalkForwardedTypesScope (caa);
				break;
			case CustomAttributeArgument[] array:
				foreach (var item in array)
					WalkForwardedTypesScope (item);
				break;
			}
		}

		void WalkScopeOfTypeReference (TypeReference type)
		{
			if (type == null)
				return;

			if (!Visited.Add (type))
				return;

			// Don't walk the scope of windows runtime projections
			if (type.IsWindowsRuntimeProjection)
				return;

			switch (type) {
			case GenericInstanceType git:
				WalkScopeOfTypeReference (git.ElementType);
				foreach (var ga in git.GenericArguments)
					WalkScopeOfTypeReference (ga);
				return;
			case FunctionPointerType fpt:
				WalkScopeOfTypeReference (fpt.ReturnType);
				if (fpt.HasParameters)
					WalkTypeScope (fpt.Parameters);
				return;
			case IModifierType imt:
				WalkScopeOfTypeReference (imt.ModifierType);
				WalkScopeOfTypeReference (imt.ElementType);
				return;
			case TypeSpecification ts:
				WalkScopeOfTypeReference (ts.ElementType);
				return;
			case TypeDefinition:
			case GenericParameter:
				// Nothing to walk
				return;
			}

			ProcessTypeReference (type);
		}

		protected abstract void ProcessTypeReference (TypeReference type);

		protected abstract void ProcessExportedType (ExportedType exportedType);
	}

}