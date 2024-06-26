// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

using Internal.TypeSystem;

namespace Internal.TypeSystem
{
    public static class CustomAttributeTypeNameParser
    {
        private static readonly TypeNameParseOptions s_typeNameParseOptions = new() { MaxNodes = int.MaxValue };

        /// <summary>
        /// Parses the string '<paramref name="name"/>' and returns the type corresponding to the parsed type name.
        /// The type name string should be in the 'SerString' format as defined by the ECMA-335 standard.
        /// This is the inverse of what <see cref="CustomAttributeTypeNameFormatter"/> does.
        /// </summary>
        public static TypeDesc GetTypeByCustomAttributeTypeName(this ModuleDesc module, string name, bool throwIfNotFound = true,
            Func<ModuleDesc, string, MetadataType> canonResolver = null)
        {
            if (!TypeName.TryParse(name.AsSpan(), out TypeName parsed, s_typeNameParseOptions))
                ThrowHelper.ThrowTypeLoadException(name, module);

            return new TypeNameResolver()
            {
                _context = module.Context,
                _module = module,
                _throwIfNotFound = throwIfNotFound,
                _canonResolver = canonResolver
            }.Resolve(parsed);
        }

        public static TypeDesc GetTypeByCustomAttributeTypeNameForDataFlow(string name, ModuleDesc callingModule,
            TypeSystemContext context, List<ModuleDesc> referencedModules, bool needsAssemblyName, out bool failedBecauseNotFullyQualified)
        {
            failedBecauseNotFullyQualified = false;
            if (!TypeName.TryParse(name.AsSpan(), out TypeName parsed, s_typeNameParseOptions))
                return null;

            if (needsAssemblyName && !IsFullyQualified(parsed))
            {
                failedBecauseNotFullyQualified = true;
                return null;
            }

            TypeNameResolver resolver = new()
            {
                _context = context,
                _module = callingModule,
                _referencedModules = referencedModules
            };

            TypeDesc type = resolver.Resolve(parsed);

            return type;

            static bool IsFullyQualified (TypeName typeName)
            {
                if (typeName.AssemblyName is null)
                {
                    return false;
                }

                if (typeName.IsConstructedGenericType)
                {
                    foreach (var typeArgument in typeName.GetGenericArguments())
                    {
                        if (!IsFullyQualified(typeArgument))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        private struct TypeNameResolver
        {
            internal TypeSystemContext _context;
            internal ModuleDesc _module;
            internal bool _throwIfNotFound;
            internal Func<ModuleDesc, string, MetadataType> _canonResolver;

            internal List<ModuleDesc> _referencedModules;

            internal TypeDesc Resolve(TypeName typeName)
            {
                if (typeName.IsSimple)
                {
                    return GetSimpleType(typeName);
                }

                if (typeName.IsConstructedGenericType)
                {
                    return GetGenericType(typeName);
                }

                if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
                {
                    TypeDesc type = Resolve(typeName.GetElementType());
                    if (type == null)
                        return null;

                    if (typeName.IsArray)
                        return typeName.IsSZArray ? type.MakeArrayType() : type.MakeArrayType(rank: typeName.GetArrayRank());

                    if (typeName.IsPointer)
                        return type.MakePointerType();

                    if (typeName.IsByRef)
                        return type.MakeByRefType();
                }

                Debug.Fail("Expected to be unreachable");
                return null;
            }

            private TypeDesc GetSimpleType(TypeName typeName)
            {
                TypeName topLevelTypeName = typeName;
                while (topLevelTypeName.IsNested)
                {
                    topLevelTypeName = topLevelTypeName.DeclaringType;
                }

                ModuleDesc module = _module;
                if (topLevelTypeName.AssemblyName != null)
                {
                    module = _context.ResolveAssembly(typeName.AssemblyName, throwIfNotFound: _throwIfNotFound);
                    if (module == null)
                        return null;
                }

                if (module != null)
                {
                    TypeDesc type = GetSimpleTypeFromModule(typeName, module);
                    if (type != null)
                    {
                        _referencedModules?.Add(module);
                        return type;
                    }
                }

                // If it didn't resolve and wasn't assembly-qualified, we also try core library
                if (topLevelTypeName.AssemblyName == null)
                {
                    if (module != _context.SystemModule)
                    {
                        TypeDesc type = GetSimpleTypeFromModule(typeName, _context.SystemModule);
                        if (type != null)
                        {
                            _referencedModules?.Add(_context.SystemModule);
                            return type;
                        }
                    }
                }

                if (_throwIfNotFound)
                    ThrowHelper.ThrowTypeLoadException(typeName.FullName, module);
                return null;
            }

            private TypeDesc GetSimpleTypeFromModule(TypeName typeName, ModuleDesc module)
            {
                if (typeName.IsNested)
                {
                    TypeDesc type = GetSimpleTypeFromModule(typeName.DeclaringType, module);
                    if (type == null)
                        return null;
                    return ((MetadataType)type).GetNestedType(TypeNameHelpers.Unescape(typeName.Name));
                }

                string fullName = TypeNameHelpers.Unescape(typeName.FullName);

                if (_canonResolver != null)
                {
                    MetadataType canonType = _canonResolver(module, fullName);
                    if (canonType != null)
                        return canonType;
                }

                (string typeNamespace, string name) = TypeNameHelpers.Split(fullName);

                return module.GetType(typeNamespace, name, throwIfNotFound: false);
            }

            private TypeDesc GetGenericType(TypeName typeName)
            {
                TypeDesc typeDefinition = Resolve(typeName.GetGenericTypeDefinition());
                if (typeDefinition == null)
                    return null;

                ImmutableArray<TypeName> typeArguments = typeName.GetGenericArguments();
                TypeDesc[] instantiation = new TypeDesc[typeArguments.Length];
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    TypeDesc type = Resolve(typeArguments[i]);
                    if (type == null)
                        return null;
                    instantiation[i] = type;
                }
                return ((MetadataType)typeDefinition).MakeInstantiatedType(instantiation);
            }
        }
    }
}
