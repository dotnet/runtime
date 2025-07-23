// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace System.Reflection
{
    internal unsafe ref partial struct TypeNameResolver
    {
        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _extensibleParser;
        private ref StackCrawlMark _stackMark;

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            bool extensibleParser,
            ref StackCrawlMark stackMark)
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

            TypeName? parsed = TypeNameParser.Parse(typeName, throwOnError);
            if (parsed is null)
            {
                return null;
            }

            return new TypeNameResolver()
            {
                _assemblyResolver = assemblyResolver,
                _typeResolver = typeResolver,
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _extensibleParser = extensibleParser,
                _stackMark = ref stackMark
            }.Resolve(parsed);
        }

        private Assembly? ResolveAssembly(AssemblyName name)
        {
            Assembly? assembly;
            if (_assemblyResolver is not null)
            {
                assembly = _assemblyResolver(name);
                if (assembly is null && _throwOnError)
                {
                    throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, name.FullName));
                }
            }
            else
            {
                if (_throwOnError)
                {
                    assembly = Assembly.Load(name, ref _stackMark, null);
                }
                else
                {
                    // When throwOnError is false we should only catch FileNotFoundException.
                    // Other exceptions like BadImangeFormatException should still fly.
                    try
                    {
                        assembly = Assembly.Load(name, ref _stackMark, null);
                    }
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }
            }
            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "TypeNameResolver.GetType is marked as RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeNameResolver.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetType(string escapedTypeName, ReadOnlySpan<string> nestedTypeNames, TypeName parsedName)
        {
            Assembly? assembly = (parsedName.AssemblyName is not null) ? ResolveAssembly(parsedName.AssemblyName.ToAssemblyName()) : null;

            // Both the external type resolver and the default type resolvers expect escaped type names
            Type? type;

            // Resolve the top level type.
            if (_typeResolver is not null)
            {
                type = _typeResolver(assembly, escapedTypeName, _ignoreCase);

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(assembly is null ?
                            SR.Format(SR.TypeLoad_ResolveType, escapedTypeName) :
                            SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, escapedTypeName, assembly.FullName),
                            typeName: escapedTypeName);
                    }
                    return null;
                }
            }
            else
            {
                if (assembly is null)
                {
                    type = RuntimeType.GetType(escapedTypeName, _throwOnError, _ignoreCase, ref _stackMark);
                }
                else
                {
                    type = assembly.GetType(escapedTypeName, _throwOnError, _ignoreCase);
                }

                if (type is null)
                {
                    return null;
                }
            }

            for (int i = 0; i < nestedTypeNames.Length; i++)
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
                if (_ignoreCase)
                    bindingFlags |= BindingFlags.IgnoreCase;

                if (type is RuntimeType rt)
                {
                    // Compat: Non-extensible parser allows ambiguous matches with ignore case lookup
                    bool ignoreAmbiguousMatch = !_extensibleParser && _ignoreCase;
                    type = rt.GetNestedType(nestedTypeNames[i], bindingFlags, ignoreAmbiguousMatch);
                }
                else
                {
                    type = type.GetNestedType(nestedTypeNames[i], bindingFlags);
                }

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType,
                            nestedTypeNames[i], (i > 0) ? nestedTypeNames[i - 1] : TypeName.Unescape(escapedTypeName)),
                            typeName: escapedTypeName);
                    }
                    return null;
                }
            }

            return type;
        }
    }
}
