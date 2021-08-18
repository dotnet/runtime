using System;
using System.Reflection.Runtime.TypeParsing;
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

		public TypeReference ResolveTypeName (string typeNameString, ICustomAttributeProvider origin, out AssemblyDefinition typeAssembly, bool needsAssemblyName = true)
		{
			typeAssembly = null;
			if (string.IsNullOrEmpty (typeNameString))
				return null;

			TypeName parsedTypeName;
			try {
				parsedTypeName = TypeParser.ParseTypeName (typeNameString);
			} catch (ArgumentException) {
				return null;
			} catch (System.IO.FileLoadException) {
				return null;
			}

			if (parsedTypeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				typeAssembly = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				if (typeAssembly == null)
					return null;

				return ResolveTypeName (typeAssembly, assemblyQualifiedTypeName.TypeName);
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

			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out var typeRef))
				return typeRef;

			// If type is not found in the caller's assembly, try in core assembly.
			typeAssembly = _context.TryResolve (PlatformAssemblies.CoreLib);
			if (typeAssembly != null && TryResolveTypeName (typeAssembly, parsedTypeName, out var typeRefFromSPCL))
				return typeRefFromSPCL;

			// It is common to use Type.GetType for looking if a type is available.
			// If no type was found only warn and return null.
			if (needsAssemblyName && origin != null) {
				_context.LogWarning ($"Type '{typeNameString}' was not found in the caller assembly nor in the base library. " +
					$"Type name strings used for dynamically accessing a type should be assembly qualified.",
				2105, new MessageOrigin (origin));
			}

			typeAssembly = null;
			return null;

			bool TryResolveTypeName (AssemblyDefinition assemblyDefinition, TypeName typeName, out TypeReference typeReference)
			{
				typeReference = null;
				if (assemblyDefinition == null)
					return false;

				typeReference = ResolveTypeName (assemblyDefinition, typeName);
				return typeReference != null;
			}
		}

		public TypeReference ResolveTypeName (AssemblyDefinition assembly, string typeNameString)
		{
			return ResolveTypeName (assembly, TypeParser.ParseTypeName (typeNameString));
		}

		TypeReference ResolveTypeName (AssemblyDefinition assembly, TypeName typeName)
		{
			if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				// In this case we ignore the assembly parameter since the type name has assembly in it
				var assemblyFromName = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				return ResolveTypeName (assemblyFromName, assemblyQualifiedTypeName.TypeName);
			}

			if (assembly == null || typeName == null)
				return null;

			if (typeName is ConstructedGenericTypeName constructedGenericTypeName) {
				var genericTypeRef = ResolveTypeName (assembly, constructedGenericTypeName.GenericType);
				if (genericTypeRef == null)
					return null;

				TypeDefinition genericType = genericTypeRef.Resolve ();
				var genericInstanceType = new GenericInstanceType (genericType);
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
					ArrayTypeName _ => new ArrayType (elementType),
					MultiDimArrayTypeName multiDimArrayTypeName => new ArrayType (elementType, multiDimArrayTypeName.Rank),
					ByRefTypeName _ => new ByReferenceType (elementType),
					PointerTypeName _ => new PointerType (elementType),
					_ => elementType
				};
			}

			return assembly.MainModule.ResolveType (typeName.ToString ());
		}
	}
}