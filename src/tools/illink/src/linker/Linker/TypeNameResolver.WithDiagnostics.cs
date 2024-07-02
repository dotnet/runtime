// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;

using TypeName = System.Reflection.Metadata.TypeName;

namespace Mono.Linker
{
	internal sealed partial class TypeNameResolver
	{
		public bool TryResolveTypeName (
			string typeNameString,
			in DiagnosticContext diagnosticContext,
			[NotNullWhen (true)] out TypeReference? typeReference,
			[NotNullWhen (true)] out List<TypeResolutionRecord>? typeResolutionRecords,
			bool needsAssemblyName)
		{
			typeReference = null;
			typeResolutionRecords = null;
			if (string.IsNullOrEmpty (typeNameString))
				return false;

			if (!TypeName.TryParse (typeNameString, out TypeName? parsedTypeName, s_typeNameParseOptions))
				return false;

			if (needsAssemblyName && !IsFullyQualified (parsedTypeName)) {
				diagnosticContext.AddDiagnostic (DiagnosticId.TypeNameIsNotAssemblyQualified, typeNameString);
				return false;
			}

			// If parsedTypeName doesn't have an assembly name in it but it does have a namespace,
			// search for the type in the calling object's assembly. If not found, look in the core
			// assembly.
			ICustomAttributeProvider? provider = diagnosticContext.Origin.Provider;
			AssemblyDefinition? referencingAssembly = provider switch {
				AssemblyDefinition asm => asm,
				TypeDefinition type => type.Module?.Assembly,
				IMemberDefinition member => member.DeclaringType.Module.Assembly,
				null => null,
				_ => throw new NotSupportedException ()
			};

			if (referencingAssembly is null)
				return false;

			typeResolutionRecords = new List<TypeResolutionRecord> ();
			typeReference = ResolveTypeName (referencingAssembly, parsedTypeName, typeResolutionRecords);
			return typeReference != null;

			static bool IsFullyQualified (TypeName typeName)
			{
				if (typeName.AssemblyName is null)
					return false;

				if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
					return IsFullyQualified (typeName.GetElementType ());

				if (typeName.IsConstructedGenericType) {
					foreach (var typeArgument in typeName.GetGenericArguments ()) {
						if (!IsFullyQualified (typeArgument))
							return false;
					}
				}

				return true;
			}
		}
	}
}
