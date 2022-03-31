// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.TypeParsing;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	internal class TypeNameResolver
	{
		readonly LinkContext _context;

		public TypeNameResolver (LinkContext context)
		{
			_context = context;
		}

		public bool TryResolveTypeName (string typeNameString, ICustomAttributeProvider? origin, [NotNullWhen (true)] out TypeReference? typeReference, [NotNullWhen (true)] out AssemblyDefinition? typeAssembly, bool needsAssemblyName = true)
		{
			typeReference = null;
			typeAssembly = null;
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

			if (parsedTypeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				typeAssembly = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				if (typeAssembly == null)
					return false;

				typeReference = ResolveTypeName (typeAssembly, assemblyQualifiedTypeName.TypeName);
				return typeReference != null;
			}

			// If parsedTypeName doesn't have an assembly name in it but it does have a namespace,
			// search for the type in the calling object's assembly. If not found, look in the core
			// assembly.
			typeAssembly = origin switch {
				AssemblyDefinition asm => asm,
				TypeDefinition type => type.Module?.Assembly,
				IMemberDefinition member => member.DeclaringType.Module.Assembly,
				null => null,
				_ => throw new NotSupportedException ()
			};

			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out typeReference))
				return true;

			// If type is not found in the caller's assembly, try in core assembly.
			typeAssembly = _context.TryResolve (PlatformAssemblies.CoreLib);
			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out typeReference))
				return true;

			// It is common to use Type.GetType for looking if a type is available.
			// If no type was found only warn and return null.
			if (needsAssemblyName && origin != null) {
				_context.LogWarning (new MessageOrigin (origin), DiagnosticId.TypeWasNotFoundInAssemblyNorBaseLibrary, typeNameString);
			}

			typeAssembly = null;
			return false;

			bool TryResolveTypeName (AssemblyDefinition assemblyDefinition, TypeName typeName, [NotNullWhen (true)] out TypeReference? typeReference)
			{
				typeReference = null;
				if (assemblyDefinition == null)
					return false;

				typeReference = ResolveTypeName (assemblyDefinition, typeName);
				return typeReference != null;
			}
		}

		public bool TryResolveTypeName (AssemblyDefinition assembly, string typeNameString, [NotNullWhen (true)] out TypeReference? typeReference)
		{
			typeReference = ResolveTypeName (assembly, TypeParser.ParseTypeName (typeNameString));
			return typeReference != null;
		}

		TypeReference? ResolveTypeName (AssemblyDefinition assembly, TypeName typeName)
		{
			if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				// In this case we ignore the assembly parameter since the type name has assembly in it
				var assemblyFromName = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				return assemblyFromName == null ? null : ResolveTypeName (assemblyFromName, assemblyQualifiedTypeName.TypeName);
			}

			if (assembly == null || typeName == null)
				return null;

			if (typeName is ConstructedGenericTypeName constructedGenericTypeName) {
				var genericTypeRef = ResolveTypeName (assembly, constructedGenericTypeName.GenericType);
				if (genericTypeRef == null)
					return null;

				Debug.Assert (genericTypeRef is TypeDefinition);
				var genericInstanceType = new GenericInstanceType (genericTypeRef);
				foreach (var arg in constructedGenericTypeName.GenericArguments) {
					var genericArgument = ResolveTypeName (assembly, arg);
					if (genericArgument == null)
						return null;

					genericInstanceType.GenericArguments.Add (genericArgument);
				}

				return genericInstanceType;
			} else if (typeName is HasElementTypeName elementTypeName) {
				var elementType = ResolveTypeName (assembly, elementTypeName.ElementTypeName);
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

			return assembly.MainModule.ResolveType (typeName.ToString (), _context);
		}
	}
}