// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.General;

namespace System.Reflection
{
    //
    // Parser for type names passed to GetType() apis.
    //
    internal ref partial struct TypeNameParser
    {
        private ReadOnlySpan<char> _input;
        private int _index;

        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _prohibitAssemblyQualifiedName;
        private IList<string> _defaultAssemblyNames;

        // [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver = null,
            Func<Assembly?, string, bool, Type?>? typeResolver = null,
            bool throwOnError = false,
            bool ignoreCase = false,
            bool prohibitAssemblyQualifiedName = false,
            IList<string> defaultAssemblyNames = null)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            // Compat: Empty name throws TypeLoadException instead of
            // the natural ArgumentException
            if (typeName.Length == 0)
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
                else
                    return null;
            }

            return new TypeNameParser(typeName)
            {
                _assemblyResolver = assemblyResolver,
                _typeResolver = typeResolver,
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _prohibitAssemblyQualifiedName = prohibitAssemblyQualifiedName,
                _defaultAssemblyNames = defaultAssemblyNames
            }.Parse();
        }

        private Assembly? ResolveAssembly(string assemblyName)
        {
            // TODO: Check behavior for invalid assembly names w/ throwOnError
            Assembly? assembly;
            if (_assemblyResolver != null)
            {
                assembly = _assemblyResolver(new AssemblyName(assemblyName));
            }
            else
            {
                // !!! TODO: Check behavior for empty names
                assembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(RuntimeAssemblyName.Parse(assemblyName));
            }

            if (assembly == null)
            {
                if (_throwOnError)
                {
                    throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, assemblyName));
                }
            }

            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetType(string name, string? assemblyNameIfAny)
        {
            Assembly? assembly = (assemblyNameIfAny != null) ? ResolveAssembly(assemblyNameIfAny) : null;

            Type? type;

            // Resolve the top level type.
            if (_typeResolver != null)
            {
                // The external type resolver expects escaped type names
                string escapedTypeName = EscapeTypeName(name);

                type = _typeResolver(assembly, escapedTypeName, _ignoreCase);

                if (type == null && _throwOnError)
                {
                    string errorString = assembly == null ?
                        SR.Format(SR.TypeLoad_ResolveType, escapedTypeName) :
                        SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, escapedTypeName, assembly.FullName);

                    throw new TypeLoadException(errorString);
                }
            }
            else
            {
                if (assembly != null)
                {
                    return assembly.GetTypeCore(name, ignoreCase: _ignoreCase);
                }
                else
                {
                    // TODO: Default assembly names for _prohibitAssemblyQualifiedName?

                    foreach (string defaultAssemblyName in _defaultAssemblyNames)
                    {
                        RuntimeAssemblyName runtimeAssemblyName = RuntimeAssemblyName.Parse(defaultAssemblyName);
                        RuntimeAssemblyInfo defaultAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(runtimeAssemblyName);
                        if (defaultAssembly == null)
                            continue;
                        Type resolvedType = defaultAssembly.GetTypeCore(name, ignoreCase: _ignoreCase);
                        if (resolvedType != null)
                            return resolvedType;
                    }

                    if (_throwOnError && _defaultAssemblyNames.Count > 0)
                    {
                        // Though we don't have to throw a TypeLoadException exception (that's our caller's job), we can throw a more specific exception than he would so just do it.
                        throw Helpers.CreateTypeLoadException(name, _defaultAssemblyNames[0]);
                    }
                    return null;
                }
            }

            return type;
       }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetNestedType(Type declaringType, string name)
        {
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
            if (_ignoreCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            Type? type = declaringType.GetNestedType(name, bindingFlags);

            if (type == null && _throwOnError)
                throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, name, declaringType.Name));

            return type;
        }
    }
}
