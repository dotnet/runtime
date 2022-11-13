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
        private ReadOnlySpan<char> _input;
        private int _index;
        private int _errorIndex; // Position for error reporting

        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _extensibleParser;
        private Assembly _topLevelAssembly;
        private IList<string> _defaultAssemblyNames;

        // [RequiresUnreferencedCode("The type might be removed")]
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
                else
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
            if (_topLevelAssembly != null)
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
            if (_assemblyResolver != null)
            {
                assembly = _assemblyResolver(new AssemblyName(assemblyName));
            }
            else
            {
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
                assembly ??= _topLevelAssembly;

                if (assembly != null)
                {
                    return assembly.GetTypeCore(name, throwOnError: true, ignoreCase: _ignoreCase);
                }
                else
                {
                    Debug.Assert(_defaultAssemblyNames != null);

                    foreach (string defaultAssemblyName in _defaultAssemblyNames)
                    {
                        RuntimeAssemblyName runtimeAssemblyName = RuntimeAssemblyName.Parse(defaultAssemblyName);
                        RuntimeAssemblyInfo defaultAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(runtimeAssemblyName);
                        if (defaultAssembly == null)
                            continue;
                        Type resolvedType = defaultAssembly.GetTypeCore(name, throwOnError: false, ignoreCase: _ignoreCase);
                        if (resolvedType != null)
                            return resolvedType;
                    }

                    if (_throwOnError)
                    {
                        if (_defaultAssemblyNames.Count > 0)
                            throw Helpers.CreateTypeLoadException(name, _defaultAssemblyNames[0]);
                        else
                            throw new TypeLoadException(SR.Format(SR.TypeLoad_TypeNotFound, name));
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
            if (_ignoreCase && _extensibleParser)
                bindingFlags |= BindingFlags.IgnoreCase;

            Type? type = declaringType.GetNestedType(name, bindingFlags);

            // Compat: Non-extensible parser allows ambiguous matches with ignore case lookup
            if (type == null && _ignoreCase && !_extensibleParser)
            {
                // Return the first name that matches. Which one gets returned on a multiple match is an implementation detail.
                string lowerName = name.ToLowerInvariant();
                foreach (Type nt in declaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (nt.Name.ToLowerInvariant() == lowerName)
                    {
                        type = nt;
                        break;
                    }
                }
            }

            if (type == null && _throwOnError)
                throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, name, declaringType.Name));

            return type;
        }
    }
}
