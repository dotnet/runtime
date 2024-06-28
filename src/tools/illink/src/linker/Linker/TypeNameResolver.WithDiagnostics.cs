// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker;

using TypeName = System.Reflection.Metadata.TypeName;
using AssemblyNameInfo = System.Reflection.Metadata.AssemblyNameInfo;

namespace Mono.Linker
{
	internal sealed partial class TypeNameResolver
	{
		public bool TryResolveTypeName (
			string typeNameString,
			in DiagnosticContext diagnosticContext,
			[NotNullWhen (true)] out TypeReference? typeReference,
			[NotNullWhen (true)] out List<TypeResolutionRecord>? typeResolutionRecords,
			bool needsAssemblyName = true)
		{
			typeReference = null;
			typeResolutionRecords = null;
			if (string.IsNullOrEmpty (typeNameString))
				return false;

			if (!TypeName.TryParse (typeNameString, out TypeName? parsedTypeName, s_typeNameParseOptions))
				return false;

			typeResolutionRecords = new List<TypeResolutionRecord> ();
			AssemblyDefinition? typeAssembly;
			if (parsedTypeName.AssemblyName is AssemblyNameInfo assemblyName) {
				typeAssembly = _assemblyResolver.TryResolve (assemblyName.Name);
				if (typeAssembly is null) {
					typeResolutionRecords = null;
					return false;
				}

				typeReference = ResolveTypeName (typeAssembly, parsedTypeName, typeResolutionRecords);
				if (typeReference == null) {
					typeResolutionRecords = null;
				}

				return typeReference != null;
			}

			// If parsedTypeName doesn't have an assembly name in it but it does have a namespace,
			// search for the type in the calling object's assembly. If not found, look in the core
			// assembly.
			ICustomAttributeProvider? provider = diagnosticContext.Origin.Provider;
			typeAssembly = provider switch {
				AssemblyDefinition asm => asm,
				TypeDefinition type => type.Module?.Assembly,
				IMemberDefinition member => member.DeclaringType.Module.Assembly,
				null => null,
				_ => throw new NotSupportedException ()
			};

			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out typeReference, typeResolutionRecords))
				return true;

			// If type is not found in the caller's assembly, try in core assembly.
			typeAssembly = _assemblyResolver.TryResolve (PlatformAssemblies.CoreLib);
			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out typeReference, typeResolutionRecords))
				return true;

			// It is common to use Type.GetType for looking if a type is available.
			// If no type was found only warn and return null.
			if (needsAssemblyName && provider != null)
				diagnosticContext.AddDiagnostic (DiagnosticId.TypeWasNotFoundInAssemblyNorBaseLibrary, typeNameString);

			typeResolutionRecords = null;
			return false;

			bool TryResolveTypeName (AssemblyDefinition assemblyDefinition, TypeName? typeName, [NotNullWhen (true)] out TypeReference? typeReference, List<TypeResolutionRecord> typeResolutionRecords)
			{
				typeReference = null;
				if (assemblyDefinition == null)
					return false;

				typeReference = ResolveTypeName (assemblyDefinition, typeName, typeResolutionRecords);
				return typeReference != null;
			}
		}
	}
}
