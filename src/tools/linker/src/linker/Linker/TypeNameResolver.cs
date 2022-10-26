// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.TypeParsing;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	internal sealed class TypeNameResolver
	{
		readonly LinkContext _context;

		public readonly record struct TypeResolutionRecord (AssemblyDefinition ReferringAssembly, TypeDefinition ResolvedType);

		public TypeNameResolver (LinkContext context)
		{
			_context = context;
		}

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

			TypeName parsedTypeName;
			try {
				parsedTypeName = TypeParser.ParseTypeName (typeNameString);
			} catch (ArgumentException) {
				return false;
			} catch (System.IO.FileLoadException) {
				return false;
			}

			typeResolutionRecords = new List<TypeResolutionRecord> ();
			AssemblyDefinition? typeAssembly;
			if (parsedTypeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				typeAssembly = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				if (typeAssembly == null) {
					typeResolutionRecords = null;
					return false;
				}

				typeReference = ResolveTypeName (typeAssembly, assemblyQualifiedTypeName.TypeName, typeResolutionRecords);
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
			typeAssembly = _context.TryResolve (PlatformAssemblies.CoreLib);
			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out typeReference, typeResolutionRecords))
				return true;

			// It is common to use Type.GetType for looking if a type is available.
			// If no type was found only warn and return null.
			if (needsAssemblyName && provider != null)
				diagnosticContext.AddDiagnostic (DiagnosticId.TypeWasNotFoundInAssemblyNorBaseLibrary, typeNameString);

			typeResolutionRecords = null;
			return false;

			bool TryResolveTypeName (AssemblyDefinition assemblyDefinition, TypeName typeName, [NotNullWhen (true)] out TypeReference? typeReference, List<TypeResolutionRecord> typeResolutionRecords)
			{
				typeReference = null;
				if (assemblyDefinition == null)
					return false;

				typeReference = ResolveTypeName (assemblyDefinition, typeName, typeResolutionRecords);
				return typeReference != null;
			}
		}

		public bool TryResolveTypeName (
			AssemblyDefinition assembly,
			string typeNameString,
			[NotNullWhen (true)] out TypeReference? typeReference,
			[NotNullWhen (true)] out List<TypeResolutionRecord>? typeResolutionRecords)
		{
			typeResolutionRecords = new List<TypeResolutionRecord> ();
			typeReference = ResolveTypeName (assembly, TypeParser.ParseTypeName (typeNameString), typeResolutionRecords);

			if (typeReference == null)
				typeResolutionRecords = null;

			return typeReference != null;
		}

		TypeReference? ResolveTypeName (AssemblyDefinition assembly, TypeName typeName, List<TypeResolutionRecord> typeResolutionRecords)
		{
			if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				// In this case we ignore the assembly parameter since the type name has assembly in it
				var assemblyFromName = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				return assemblyFromName == null ? null : ResolveTypeName (assemblyFromName, assemblyQualifiedTypeName.TypeName, typeResolutionRecords);
			}

			if (assembly == null || typeName == null)
				return null;

			if (typeName is ConstructedGenericTypeName constructedGenericTypeName) {
				var genericTypeRef = ResolveTypeName (assembly, constructedGenericTypeName.GenericType, typeResolutionRecords);
				if (genericTypeRef == null)
					return null;

				Debug.Assert (genericTypeRef is TypeDefinition);
				var genericInstanceType = new GenericInstanceType (genericTypeRef);
				foreach (var arg in constructedGenericTypeName.GenericArguments) {
					var genericArgument = ResolveTypeName (assembly, arg, typeResolutionRecords);
					if (genericArgument == null)
						return null;

					genericInstanceType.GenericArguments.Add (genericArgument);
				}

				return genericInstanceType;
			} else if (typeName is HasElementTypeName elementTypeName) {
				var elementType = ResolveTypeName (assembly, elementTypeName.ElementTypeName, typeResolutionRecords);
				if (elementType == null)
					return null;

				return typeName switch {
					ArrayTypeName => new ArrayType (elementType),
					MultiDimArrayTypeName multiDimArrayTypeName => new ArrayType (elementType, multiDimArrayTypeName.Rank),
					ByRefTypeName => new ByReferenceType (elementType),
					PointerTypeName => new PointerType (elementType),
					_ => elementType
				};
			}

			TypeDefinition? resolvedType = assembly.MainModule.ResolveType (typeName.ToString (), _context);

			// True type references (like generics and arrays) don't count as actually resolved types, they're just wrappers
			// so only record type resolutions for types which are actually resolved.
			if (resolvedType != null) {
				typeResolutionRecords.Add (new (assembly, resolvedType));
			}

			return resolvedType;
		}
	}
}