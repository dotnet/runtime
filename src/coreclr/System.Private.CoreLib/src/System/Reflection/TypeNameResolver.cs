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
    internal struct TypeNameResolver
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

            var parsed = Metadata.TypeNameParser.Parse(typeName, throwOnError: throwOnError);
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
            var parsed = Metadata.TypeNameParser.Parse(typeName,
               allowFullyQualifiedName: true, // let it get parsed, but throw when topLevelAssembly was specified
               throwOnError: throwOnError);

            if (parsed is null)
            {
                return null;
            }
            else if (parsed.AssemblyName is not null && topLevelAssembly is not null)
            {
                return throwOnError ? throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly) : null;
            }

            return new TypeNameResolver()
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
        [RequiresUnreferencedCode("TODO: introduce dedicated overload that does not use forbidden API")]
        internal static RuntimeType GetTypeReferencedByCustomAttribute(string typeName, RuntimeModule scope)
        {
            ArgumentException.ThrowIfNullOrEmpty(typeName);

            RuntimeAssembly requestingAssembly = scope.GetRuntimeAssembly();

            var parsed = Metadata.TypeNameParser.Parse(typeName, allowFullyQualifiedName: requestingAssembly is null, throwOnError: true)!; // adsitnik allowFullyQualifiedName part might be wrong
            RuntimeType? type = (RuntimeType?)new TypeNameResolver()
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
        [RequiresUnreferencedCode("TODO: introduce dedicated overload that does not use forbidden API")]
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

            var parsed = Metadata.TypeNameParser.Parse(typeName,
               allowFullyQualifiedName: true,
               throwOnError: throwOnError);

            if (parsed is null)
            {
                return null;
            }

            RuntimeType? type = (RuntimeType?)new TypeNameResolver()
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
        private Type? GetType(Metadata.TypeName typeName)
        {
            Assembly? assembly;

            if (typeName.AssemblyName is not null)
            {
                assembly = ResolveAssembly(typeName.AssemblyName);
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
                string escapedTypeName = TypeNameParser.EscapeTypeName(typeName.Name);

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
                            throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveType, TypeNameParser.EscapeTypeName(typeName.Name)));
                        }
                        return null;
                    }
                    return GetTypeFromDefaultAssemblies(typeName.Name, nestedTypeNames: ReadOnlySpan<string>.Empty);
                }

                if (assembly is RuntimeAssembly runtimeAssembly)
                {
                    // Compat: Non-extensible parser allows ambiguous matches with ignore case lookup
                    if (!_extensibleParser || !_ignoreCase)
                    {
                        return runtimeAssembly.GetTypeCore(typeName.Name, nestedTypeNames: ReadOnlySpan<string>.Empty, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                    }
                    type = runtimeAssembly.GetTypeCore(typeName.Name, default, throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                }
                else
                {
                    // This is a third-party Assembly object. Emulate GetTypeCore() by calling the public GetType()
                    // method. This is wasteful because it'll probably reparse a type string that we've already parsed
                    // but it can't be helped.
                    type = assembly.GetType(TypeNameParser.EscapeTypeName(typeName.Name), throwOnError: _throwOnError, ignoreCase: _ignoreCase);
                }

                if (type is null)
                    return null;
            }

            return type;
        }

        private Type? GetTypeFromDefaultAssemblies(string typeName, ReadOnlySpan<string> nestedTypeNames)
        {
            RuntimeAssembly? requestingAssembly = (RuntimeAssembly?)_requestingAssembly;
            if (requestingAssembly is not null)
            {
                Type? type = ((RuntimeAssembly)requestingAssembly).GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly coreLib = (RuntimeAssembly)typeof(object).Assembly;
            if (requestingAssembly != coreLib)
            {
                Type? type = ((RuntimeAssembly)coreLib).GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly? resolvedAssembly = AssemblyLoadContext.OnTypeResolve(requestingAssembly, TypeNameParser.EscapeTypeName(typeName, nestedTypeNames));
            if (resolvedAssembly is not null)
            {
                Type? type = resolvedAssembly.GetTypeCore(typeName, nestedTypeNames, throwOnError: false, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            if (_throwOnError)
                throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, TypeNameParser.EscapeTypeName(typeName), (requestingAssembly ?? coreLib).FullName));

            return null;
        }

        [RequiresUnreferencedCode("The type might be removed")]
        private Type? Resolve(Metadata.TypeName typeName)
        {
            if (typeName.ContainingType is not null) // nested type
            {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
                if (_ignoreCase)
                {
                    flags |= BindingFlags.IgnoreCase;
                }
                Type? containingType = Resolve(typeName.ContainingType);
                if (containingType is null)
                {
                    return null;
                }
                Type? nestedType = containingType.GetNestedType(typeName.Name, flags);
                if (nestedType is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, typeName.Name, typeName.ContainingType.Name));
                    }
                    return null;
                }

                return Make(nestedType, typeName);
            }
            else if (typeName.UnderlyingType is null)
            {
                Type? type = GetType(typeName);

                return Make(type, typeName);
            }

            return Make(Resolve(typeName.UnderlyingType), typeName);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Used to implement resolving types from strings.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "Used to implement resolving types from strings.")]
        [RequiresUnreferencedCode("The type might be removed")]
        private Type? Make(Type? type, Metadata.TypeName typeName)
        {
            if (type is null || typeName.IsElementalType)
            {
                return type;
            }
            else if (typeName.IsConstructedGenericType)
            {
                Metadata.TypeName[] genericArgs = typeName.GetGenericArguments();
                Type[] genericTypes = new Type[genericArgs.Length];
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    Type? genericArg = Resolve(genericArgs[i]);
                    if (genericArg is null)
                    {
                        return null;
                    }
                    genericTypes[i] = genericArg;
                }

                return type.MakeGenericType(genericTypes);
            }
            else if (typeName.IsManagedPointerType)
            {
                return type.MakeByRefType();
            }
            else if (typeName.IsUnmanagedPointerType)
            {
                return type.MakePointerType();
            }
            else if (typeName.IsSzArrayType)
            {
                return type.MakeArrayType();
            }
            else
            {
                return type.MakeArrayType(rank: typeName.GetArrayRank());
            }
        }
    }
}
