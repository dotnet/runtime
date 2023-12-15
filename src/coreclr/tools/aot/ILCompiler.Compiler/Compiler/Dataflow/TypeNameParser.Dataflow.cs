// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Internal.TypeSystem;

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
        private TypeSystemContext _context;
        private ModuleDesc _callingModule;
        private List<ModuleDesc> _referencedModules;
        private bool _typeWasNotFoundInAssemblyNorBaseLibrary;

        public static TypeDesc ResolveType(string name, ModuleDesc callingModule,
            TypeSystemContext context, List<ModuleDesc> referencedModules, out bool typeWasNotFoundInAssemblyNorBaseLibrary)
        {
            var parser = new TypeNameParser(name)
            {
                _context = context,
                _callingModule = callingModule,
                _referencedModules = referencedModules
            };

            TypeDesc result = parser.Parse()?.Value;

            typeWasNotFoundInAssemblyNorBaseLibrary = parser._typeWasNotFoundInAssemblyNorBaseLibrary;
            return result;
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
            ModuleDesc module;

            if (assemblyNameIfAny != null)
            {
                module = (TryParseAssemblyName(assemblyNameIfAny) is AssemblyName an) ?
                    _context.ResolveAssembly(an, throwIfNotFound: false) : null;
            }
            else
            {
                module = _callingModule;
            }

            if (module != null)
            {
                Type type = GetTypeCore(module, typeName, nestedTypeNames);
                if (type != null)
                {
                    _referencedModules?.Add(module);
                    return type;
                }
            }

            // If it didn't resolve and wasn't assembly-qualified, we also try core library
            if (assemblyNameIfAny == null)
            {
                Type type = GetTypeCore(_context.SystemModule, typeName, nestedTypeNames);
                if (type != null)
                {
                    _referencedModules?.Add(_context.SystemModule);
                    return type;
                }

                _typeWasNotFoundInAssemblyNorBaseLibrary = true;
            }

            return null;
        }

        private static AssemblyName TryParseAssemblyName(string assemblyName)
        {
            try
            {
                return new AssemblyName(assemblyName);
            }
            catch (FileLoadException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
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

        private static void ParseError()
        {
        }
    }
}
