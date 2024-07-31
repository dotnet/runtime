// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using System.Collections.Immutable;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial struct TypeNameResolver
	{
		readonly Compilation _compilation;

		static readonly TypeNameParseOptions s_typeNameParseOptions = new () { MaxNodes = int.MaxValue };

		public TypeNameResolver (Compilation compilation)
		{
			_compilation = compilation;
		}

		public bool TryResolveTypeName (
			string typeNameString,
			in DiagnosticContext diagnosticContext,
			IAssemblySymbol assembly,
			out ITypeSymbol? type,
			bool needsAssemblyName)
		{
			type = null;
			if (!TypeName.TryParse (typeNameString.AsSpan (), out TypeName? typeName, s_typeNameParseOptions))
				return false;

			if (needsAssemblyName && !IsFullyQualified (typeName)) {
				diagnosticContext.AddDiagnostic (DiagnosticId.TypeNameIsNotAssemblyQualified, typeNameString);
				return false;
			}

			type = ResolveTypeName (assembly, typeName);
			return type != null;

			static bool IsFullyQualified (TypeName typeName)
			{
				if (typeName.AssemblyName is null)
					return false;

				if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
					return IsFullyQualified (typeName.GetElementType ());

				if (typeName.IsConstructedGenericType) {
					foreach (var arg in typeName.GetGenericArguments ()) {
						if (!IsFullyQualified (arg))
							return false;
					}
				}

				return true;
			}
		}

		ITypeSymbol? ResolveTypeName (IAssemblySymbol assembly, TypeName typeName)
		{
			if (typeName.IsSimple)
				return GetSimpleType (assembly, typeName);

			if (typeName.IsConstructedGenericType)
				return GetGenericType (assembly, typeName);

			if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
			{
				if (ResolveTypeName (assembly, typeName.GetElementType ()) is not ITypeSymbol type)
					return null;

				if (typeName.IsArray)
					return typeName.IsSZArray ? _compilation.CreateArrayTypeSymbol (type) : _compilation.CreateArrayTypeSymbol (type, typeName.GetArrayRank ());

				// Roslyn doesn't have a representation for byref types
				// (the byrefness is considered part of the symbol, not its type)
				if (typeName.IsByRef)
					return null;

				if (typeName.IsPointer)
					return _compilation.CreatePointerTypeSymbol (type);

				Debug.Fail ("Unreachable");
				return null;
			}

			return null;
		}

		private ITypeSymbol? GetSimpleType (IAssemblySymbol assembly, TypeName typeName)
		{
			IAssemblySymbol module = assembly;
			if (typeName.AssemblyName is AssemblyNameInfo assemblyName) {
				if (ResolveAssembly (assemblyName) is not IAssemblySymbol resolvedAssembly)
					return null;
				module = resolvedAssembly;
			}

			if (GetSimpleTypeFromModule (typeName, module) is ITypeSymbol type)
				return type;

			// The analyzer doesn't see the core library, so can't fall back to lookup up types in corelib.
			return null;
		}

		private static ITypeSymbol? GetSimpleTypeFromModule (TypeName typeName, IAssemblySymbol module)
		{
			string fullName = TypeNameHelpers.Unescape (typeName.FullName);
			return module.GetTypeByMetadataName (fullName);
		}

		private ITypeSymbol? GetGenericType (IAssemblySymbol assembly, TypeName typeName)
		{
			if (ResolveTypeName (assembly, typeName.GetGenericTypeDefinition ()) is not INamedTypeSymbol typeDefinition)
				return null;

			ImmutableArray<TypeName> typeArguments = typeName.GetGenericArguments ();
			ITypeSymbol[] instantiation = new ITypeSymbol[typeArguments.Length];
			for (int i = 0; i < typeArguments.Length; i++)
			{
				if (ResolveTypeName (assembly, typeArguments[i]) is not ITypeSymbol type)
					return null;
				instantiation[i] = type;
			}
			return typeDefinition.Construct (instantiation);
		}

		IAssemblySymbol? ResolveAssembly (AssemblyNameInfo? assemblyName)
		{
			if (assemblyName is null)
				return null;

			if (_compilation.Assembly.Name == assemblyName.Name)
				return _compilation.Assembly;

			foreach (var metadataReference in _compilation.References) {
				if (_compilation.GetAssemblyOrModuleSymbol (metadataReference) is not IAssemblySymbol asmSym)
					continue;
				if (asmSym.Name == assemblyName.Name)
					return asmSym;
			}
			return null;
		}
	}
}
