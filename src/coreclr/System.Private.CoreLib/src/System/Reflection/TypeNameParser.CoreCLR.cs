// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _extensibleParser;
        private bool _requireAssemblyQualifiedName;
        private bool _suppressContextualReflectionContext;
        private Assembly? _requestingAssembly;
        private Assembly? _topLevelAssembly;

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Assembly requestingAssembly,
            bool throwOnError = false,
            bool ignoreCase = false)
        {
            return GetType(typeName, assemblyResolver: null, typeResolver: null, requestingAssembly: requestingAssembly,
                throwOnError: throwOnError, ignoreCase: ignoreCase, extensibleParser: false);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            Assembly? requestingAssembly,
            bool throwOnError = false,
            bool ignoreCase = false,
            bool extensibleParser = true)
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

            Metadata.TypeName? parsed = Metadata.TypeNameParser.Parse(typeName, throwOnError: throwOnError);
            if (parsed is null)
            {
                return null;
            }

            return new TypeNameParser()
            {
                _assemblyResolver = assemblyResolver,
                _typeResolver = typeResolver,
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _extensibleParser = extensibleParser,
                _requestingAssembly = requestingAssembly
            }.Resolve(parsed);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            bool throwOnError,
            bool ignoreCase,
            Assembly topLevelAssembly)
        {
            Metadata.TypeName? parsed = Metadata.TypeNameParser.Parse(typeName, throwOnError);

            if (parsed is null)
            {
                return null;
            }
            else if (topLevelAssembly is not null && parsed.AssemblyName is not null)
            {
                return throwOnError ? throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly) : null;
            }

            return new TypeNameParser()
            {
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _topLevelAssembly = topLevelAssembly,
                _requestingAssembly = topLevelAssembly
            }.Resolve(parsed);
        }

        // Resolve type name referenced by a custom attribute metadata.
        // It uses the standard Type.GetType(typeName, throwOnError: true) algorithm with the following modifications:
        // - ContextualReflectionContext is not taken into account
        // - The dependency between the returned type and the requesting assembly is recorded for the purpose of
        // lifetime tracking of collectible types.
        internal static RuntimeType GetTypeReferencedByCustomAttribute(string typeName, RuntimeModule scope)
        {
            ArgumentException.ThrowIfNullOrEmpty(typeName);

            RuntimeAssembly requestingAssembly = scope.GetRuntimeAssembly();

            Metadata.TypeName parsed = Metadata.TypeName.Parse(typeName);
            RuntimeType? type = (RuntimeType?)new TypeNameParser()
            {
                _throwOnError = true,
                _suppressContextualReflectionContext = true,
                _requestingAssembly = requestingAssembly
            }.Resolve(parsed);

            Debug.Assert(type != null);

            RuntimeTypeHandle.RegisterCollectibleTypeDependency(type, requestingAssembly);

            return type;
        }

        // Used by VM
        internal static unsafe RuntimeType? GetTypeHelper(char* pTypeName, RuntimeAssembly? requestingAssembly,
            bool throwOnError, bool requireAssemblyQualifiedName)
        {
            ReadOnlySpan<char> typeName = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pTypeName);

            // Compat: Empty name throws TypeLoadException instead of
            // the natural ArgumentException
            if (typeName.Length == 0)
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
                return null;
            }

            Metadata.TypeName? parsed = Metadata.TypeNameParser.Parse(typeName, throwOnError);
            if (parsed is null)
            {
                return null;
            }

            RuntimeType? type = (RuntimeType?)new TypeNameParser()
            {
                _requestingAssembly = requestingAssembly,
                _throwOnError = throwOnError,
                _suppressContextualReflectionContext = true,
                _requireAssemblyQualifiedName = requireAssemblyQualifiedName,
            }.Resolve(parsed);

            if (type != null)
                RuntimeTypeHandle.RegisterCollectibleTypeDependency(type, requestingAssembly);

            return type;
        }

        private Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            Assembly? assembly;
            if (_assemblyResolver is not null)
            {
                assembly = _assemblyResolver(assemblyName);
                if (assembly is null && _throwOnError)
                {
                    throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, assemblyName));
                }
            }
            else
            {
                assembly = RuntimeAssembly.InternalLoad(assemblyName, ref Unsafe.NullRef<StackCrawlMark>(),
                    _suppressContextualReflectionContext ? null : AssemblyLoadContext.CurrentContextualReflectionContext,
                    requestingAssembly: (RuntimeAssembly?)_requestingAssembly, throwOnFileNotFound: _throwOnError);
            }
            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetType(string escapedTypeName, // For nested types, it's Name. For other types it's FullName
            ReadOnlySpan<string> nestedTypeNames, Metadata.TypeName parsedName)
        {
            Assembly? assembly;

            if (parsedName.AssemblyName is not null)
            {
                assembly = ResolveAssembly(parsedName.AssemblyName.ToAssemblyName());
                if (assembly is null)
                    return null;
            }
            else
            {
                assembly = _topLevelAssembly;
            }

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
                            SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, escapedTypeName, assembly.FullName));
                    }
                    return null;
                }
            }
            else
            {
                if (assembly is null)
                {
                    if (_requireAssemblyQualifiedName)
                    {
                        if (_throwOnError)
                        {
                            throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveType, escapedTypeName));
                        }
                        return null;
                    }
                    return GetTypeFromDefaultAssemblies(UnescapeTypeName(escapedTypeName), nestedTypeNames, parsedName);
                }

                if (assembly is RuntimeAssembly runtimeAssembly)
                {
                    string unescapedTypeName = UnescapeTypeName(escapedTypeName);
                    // Compat: Non-extensible parser allows ambiguous matches with ignore case lookup
                    if (!_extensibleParser || !_ignoreCase)
                    {
                        return runtimeAssembly.GetTypeCore(unescapedTypeName, nestedTypeNames, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                    }
                    type = runtimeAssembly.GetTypeCore(unescapedTypeName, default, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                }
                else
                {
                    // This is a third-party Assembly object. Emulate GetTypeCore() by calling the public GetType()
                    // method. This is wasteful because it'll probably reparse a type string that we've already parsed
                    // but it can't be helped.
                    type = assembly.GetType(escapedTypeName, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                }

                if (type is null)
                    return null;
            }

            for (int i = 0; i < nestedTypeNames.Length; i++)
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
                if (_ignoreCase)
                    bindingFlags |= BindingFlags.IgnoreCase;

                type = type.GetNestedType(nestedTypeNames[i], bindingFlags);

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType,
                            nestedTypeNames[i], (i > 0) ? nestedTypeNames[i - 1] : escapedTypeName));
                    }
                    return null;
                }
            }

            return type;
        }

        private Type? GetTypeFromDefaultAssemblies(string typeName, ReadOnlySpan<string> nestedTypeNames, Metadata.TypeName parsedName)
        {
            RuntimeAssembly? requestingAssembly = (RuntimeAssembly?)_requestingAssembly;
            if (requestingAssembly is not null)
            {
                Type? type = requestingAssembly.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly coreLib = (RuntimeAssembly)typeof(object).Assembly;
            if (requestingAssembly != coreLib)
            {
                Type? type = coreLib.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly? resolvedAssembly = AssemblyLoadContext.OnTypeResolve(requestingAssembly, parsedName.FullName);
            if (resolvedAssembly is not null)
            {
                Type? type = resolvedAssembly.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            if (_throwOnError)
                throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, parsedName.FullName, (requestingAssembly ?? coreLib).FullName));

            return null;
        }
    }
}
