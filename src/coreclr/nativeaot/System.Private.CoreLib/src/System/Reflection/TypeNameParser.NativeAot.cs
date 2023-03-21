// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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
        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _extensibleParser;
        private Assembly _topLevelAssembly;
        private IList<string> _defaultAssemblyNames;

        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver = null,
            Func<Assembly?, string, bool, Type?>? typeResolver = null,
            bool throwOnError = false,
            bool ignoreCase = false,
            bool extensibleParser = false,
            Assembly topLevelAssembly = null,
            IList<string> defaultAssemblyNames = null)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            // Compat: Empty name throws TypeLoadException instead of
            // the natural ArgumentException
            if (typeName.Length == 0)
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
                return null;
            }

            return new TypeNameParser(typeName)
            {
                _assemblyResolver = assemblyResolver,
                _typeResolver = typeResolver,
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _extensibleParser = extensibleParser,
                _topLevelAssembly = topLevelAssembly,
                _defaultAssemblyNames = defaultAssemblyNames
            }.Parse();
        }

        private bool CheckTopLevelAssemblyQualifiedName()
        {
            if (_topLevelAssembly is not null)
            {
                if (_throwOnError)
                    throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly);
                return false;
            }
            return true;
        }

        private Assembly? ResolveAssembly(string assemblyName)
        {
            Assembly? assembly;
            if (_assemblyResolver is not null)
            {
                assembly = _assemblyResolver(new AssemblyName(assemblyName));
            }
            else
            {
                assembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(RuntimeAssemblyName.Parse(assemblyName));
            }

            if (assembly is null && _throwOnError)
            {
                throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, assemblyName));
            }

            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "GetType APIs are marked as RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "GetType APIs are marked as RequiresUnreferencedCode.")]
        private Type? GetType(string typeName, ReadOnlySpan<string> nestedTypeNames, string? assemblyNameIfAny)
        {
            Assembly? assembly;

            if (assemblyNameIfAny is not null)
            {
                assembly = ResolveAssembly(assemblyNameIfAny);
                if (assembly is null)
                    return null;
            }
            else
            {
                assembly = _topLevelAssembly;
            }

            Type? type = null;

            // Resolve the top level type.
            if (_typeResolver is not null)
            {
                string escapedTypeName = EscapeTypeName(typeName);

                type = _typeResolver(assembly, escapedTypeName, _ignoreCase);

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(assembly is null ?
                            SR.Format(SR.TypeLoad_ResolveType, escapedTypeName) :
                            SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, escapedTypeName, assembly.FullName));
                    }
                    return null;
                }
            }
            else
            {
                if (assembly is not null)
                {
                    if (assembly is RuntimeAssemblyInfo runtimeAssembly)
                    {
                        type = runtimeAssembly.GetTypeCore(typeName, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                    }
                    else
                    {
                        // This is a third-party Assembly object. We can emulate GetTypeCore() by calling the public GetType()
                        // method. This is wasteful because it'll probably reparse a type string that we've already parsed
                        // but it can't be helped.
                        type = assembly.GetType(EscapeTypeName(typeName), throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                    }

                    if (type is null)
                        return null;
                }
                else
                {
                    Debug.Assert(_defaultAssemblyNames is not null);

                    foreach (string defaultAssemblyName in _defaultAssemblyNames)
                    {
                        RuntimeAssemblyName runtimeAssemblyName = RuntimeAssemblyName.Parse(defaultAssemblyName);
                        RuntimeAssemblyInfo defaultAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(runtimeAssemblyName);
                        if (defaultAssembly is null)
                            continue;
                        type = defaultAssembly.GetTypeCore(typeName, throwOnError: false, ignoreCase: _ignoreCase);
                        if (type is not null)
                            break;
                    }

                    if (type is null)
                    {
                        if (_throwOnError)
                        {
                            if (_defaultAssemblyNames.Count > 0)
                                throw Helpers.CreateTypeLoadException(typeName, _defaultAssemblyNames[0]);
                            else
                                throw new TypeLoadException(SR.Format(SR.TypeLoad_TypeNotFound, typeName));
                        }
                        return null;
                    }
                }
            }

            for (int i = 0; i < nestedTypeNames.Length; i++)
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
                if (_ignoreCase && _extensibleParser)
                    bindingFlags |= BindingFlags.IgnoreCase;

                Type declaringType = type;

                type = type.GetNestedType(nestedTypeNames[i], bindingFlags);

                // Compat: Non-extensible parser allows ambiguous matches with ignore case lookup
                if (type is null && _ignoreCase && !_extensibleParser)
                {
                    // Return the first name that matches. Which one gets returned on a multiple match is an implementation detail.
                    string lowerName = nestedTypeNames[i].ToLowerInvariant();
                    foreach (Type nt in declaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        if (nt.Name.ToLowerInvariant() == lowerName)
                        {
                            type = nt;
                            break;
                        }
                    }
                }

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType,
                            nestedTypeNames[i], (i > 0) ? nestedTypeNames[i - 1] : typeName));
                    }
                    return null;
                }
            }

            return type;
        }
    }
}
