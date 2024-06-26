// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

using TypeName = System.Reflection.Metadata.TypeName;
using TypeNameParseOptions = System.Reflection.Metadata.TypeNameParseOptions;
using AssemblyNameInfo = System.Reflection.Metadata.AssemblyNameInfo;
using TypeNameHelpers = System.Reflection.Metadata.TypeNameHelpers;

#nullable enable

namespace Mono.Linker
{
	internal sealed partial class TypeNameResolver
	{
		readonly ITryResolveMetadata _metadataResolver;
		readonly ITryResolveAssemblyName _assemblyResolver;

		private static readonly TypeNameParseOptions s_typeNameParseOptions = new () { MaxNodes = int.MaxValue };

		public readonly record struct TypeResolutionRecord (AssemblyDefinition ReferringAssembly, TypeDefinition ResolvedType);

		public TypeNameResolver (ITryResolveMetadata metadataResolver, ITryResolveAssemblyName assemblyNameResolver)
		{
			_metadataResolver = metadataResolver;
			_assemblyResolver = assemblyNameResolver;
		}

		public bool TryResolveTypeName (
			AssemblyDefinition assembly,
			string typeNameString,
			[NotNullWhen (true)] out TypeReference? typeReference,
			[NotNullWhen (true)] out List<TypeResolutionRecord>? typeResolutionRecords)
		{
			typeResolutionRecords = new List<TypeResolutionRecord> ();
			if (!TypeName.TryParse (typeNameString, out TypeName? parsedTypeName, s_typeNameParseOptions)) {
				typeReference = null;
				return false;
			}
			typeReference = ResolveTypeName (assembly, parsedTypeName, typeResolutionRecords);

			if (typeReference == null)
				typeResolutionRecords = null;

			return typeReference != null;
		}

		TypeReference? ResolveTypeName (AssemblyDefinition originalAssembly, TypeName? typeName, List<TypeResolutionRecord> typeResolutionRecords)
		{
			if (typeName == null)
				return null;

			AssemblyDefinition? assembly = originalAssembly;
			if (typeName.AssemblyName is AssemblyNameInfo assemblyName)
				// In this case we ignore the assembly parameter since the type name has assembly in it
				assembly = _assemblyResolver.TryResolve (assemblyName.Name);

			if (assembly == null)
				return null;

			if (typeName.IsConstructedGenericType) {
				var genericTypeRef = ResolveTypeName (assembly, typeName.GetGenericTypeDefinition (), typeResolutionRecords);
				if (genericTypeRef == null)
					return null;

				Debug.Assert (genericTypeRef is TypeDefinition);
				var genericInstanceType = new GenericInstanceType (genericTypeRef);
				foreach (var arg in typeName.GetGenericArguments()) {
					var genericArgument = ResolveTypeName (assembly, arg, typeResolutionRecords);
					if (genericArgument == null)
						return null;

					genericInstanceType.GenericArguments.Add (genericArgument);
				}

				return genericInstanceType;
			} else if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef) {
				var elementType = ResolveTypeName (assembly, typeName.GetElementType (), typeResolutionRecords);
				if (elementType == null)
					return null;

				if (typeName.IsArray)
					return typeName.IsSZArray ? new ArrayType (elementType) : new ArrayType (elementType, typeName.GetArrayRank ());
				if (typeName.IsByRef)
					return new ByReferenceType (elementType);
				if (typeName.IsPointer)
					return new PointerType (elementType);
				Debug.Fail("Unreachable");
				return null;
			}

			Debug.Assert (typeName.IsSimple);
			TypeName topLevelTypeName = typeName;
			while (topLevelTypeName.IsNested)
				topLevelTypeName = topLevelTypeName.DeclaringType!;
			Debug.Assert (topLevelTypeName.AssemblyName == typeName.AssemblyName);
			TypeDefinition? resolvedType = GetSimpleTypeFromModule (typeName, assembly.MainModule);

			// True type references (like generics and arrays) don't count as actually resolved types, they're just wrappers
			// so only record type resolutions for types which are actually resolved.
			if (resolvedType != null) {
				typeResolutionRecords.Add (new (assembly, resolvedType));
				return resolvedType;
			}

			// If it didn't resolve and wasn't assembly-qualified, we also try core library
			if (topLevelTypeName.AssemblyName == null && assembly.Name.Name != PlatformAssemblies.CoreLib) {
				if (_assemblyResolver.TryResolve (PlatformAssemblies.CoreLib) is AssemblyDefinition coreLib) {
					resolvedType = GetSimpleTypeFromModule (typeName, coreLib.MainModule);
					if (resolvedType != null) {
						typeResolutionRecords.Add (new (coreLib, resolvedType));
						return resolvedType;
					}
				}
			}

			return null;

			TypeDefinition? GetSimpleTypeFromModule (TypeName typeName, ModuleDefinition module)
			{
				if (typeName.IsNested)
				{
					TypeDefinition? type = GetSimpleTypeFromModule (typeName.DeclaringType!, module);
					if (type == null)
						return null;
					return GetNestedType (type, TypeNameHelpers.Unescape (typeName.Name));
				}

				return module.ResolveType (TypeNameHelpers.Unescape (typeName.FullName), _metadataResolver);
			}

			TypeDefinition? GetNestedType (TypeDefinition type, string nestedTypeName)
			{
				if (!type.HasNestedTypes)
					return null;

				foreach (var nestedType in type.NestedTypes) {
					if (nestedType.Name == nestedTypeName)
						return nestedType;
				}

				return null;
			}
		}
	}
}
