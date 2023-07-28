// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Internal.TypeSystem;

#nullable disable

namespace Internal.TypeSystem
{
    public static class CustomAttributeTypeNameParser
    {
        /// <summary>
        /// Parses the string '<paramref name="name"/>' and returns the type corresponding to the parsed type name.
        /// The type name string should be in the 'SerString' format as defined by the ECMA-335 standard.
        /// This is the inverse of what <see cref="CustomAttributeTypeNameFormatter"/> does.
        /// </summary>
        public static TypeDesc GetTypeByCustomAttributeTypeName(this ModuleDesc module, string name, bool throwIfNotFound = true,
            Func<ModuleDesc, string, MetadataType> canonResolver = null)
        {
            return System.Reflection.TypeNameParser.ResolveType(module, name, throwIfNotFound, canonResolver);
        }
    }
}

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
        private ModuleDesc _module;
        private bool _throwIfNotFound;
        private Func<ModuleDesc, string, MetadataType> _canonResolver;

        public static TypeDesc ResolveType(ModuleDesc module, string name, bool throwIfNotFound,
            Func<ModuleDesc, string, MetadataType> canonResolver)
        {
            return new TypeNameParser(name.AsSpan())
            {
                _module = module,
                _throwIfNotFound = throwIfNotFound,
                _canonResolver = canonResolver
            }.Parse()?.Value;
        }

        private sealed class Type
        {
            public Type(TypeDesc type) => Value = type;
            public TypeDesc Value { get; }

            public Type MakeArrayType() => new Type(Value.MakeArrayType());
            public Type MakeArrayType(int rank) => new Type(Value.MakeArrayType(rank));
            public Type MakePointerType() => new Type(Value.MakePointerType());
            public Type MakeByRefType() => new Type(Value.MakeByRefType());

            public Type MakeGenericType(Type[] typeArguments)
            {
                TypeDesc[] instantiation = new TypeDesc[typeArguments.Length];
                for (int i = 0; i < typeArguments.Length; i++)
                    instantiation[i] = typeArguments[i].Value;
                return new Type(((MetadataType)Value).MakeInstantiatedType(instantiation));
            }
        }

        private static bool CheckTopLevelAssemblyQualifiedName() => true;

        private Type GetType(string typeName, ReadOnlySpan<string> nestedTypeNames, string assemblyNameIfAny)
        {
            ModuleDesc module = (assemblyNameIfAny == null) ? _module :
                _module.Context.ResolveAssembly(new AssemblyName(assemblyNameIfAny), throwIfNotFound: _throwIfNotFound);

            if (_canonResolver != null && nestedTypeNames.IsEmpty)
            {
                MetadataType canonType = _canonResolver(module, typeName);
                if (canonType != null)
                    return new Type(canonType);
            }

            if (module != null)
            {
                Type type = GetTypeCore(module, typeName, nestedTypeNames);
                if (type != null)
                    return type;
            }

            // If it didn't resolve and wasn't assembly-qualified, we also try core library
            if (assemblyNameIfAny == null)
            {
                Type type = GetTypeCore(module.Context.SystemModule, typeName, nestedTypeNames);
                if (type != null)
                    return type;
            }

            if (_throwIfNotFound)
                ThrowHelper.ThrowTypeLoadException(EscapeTypeName(typeName, nestedTypeNames), module);
            return null;
        }

        private static Type GetTypeCore(ModuleDesc module, string typeName, ReadOnlySpan<string> nestedTypeNames)
        {
            (string typeNamespace, string name) = SplitFullTypeName(typeName);

            MetadataType type = module.GetType(typeNamespace, name, throwIfNotFound: false);
            if (type == null)
                return null;

            for (int i = 0; i < nestedTypeNames.Length; i++)
            {
                type = type.GetNestedType(nestedTypeNames[i]);
                if (type == null)
                    return null;
            }

            return new Type(type);
        }

        private void ParseError()
        {
            ThrowHelper.ThrowTypeLoadException(_input.ToString(), _module);
        }
    }
}
