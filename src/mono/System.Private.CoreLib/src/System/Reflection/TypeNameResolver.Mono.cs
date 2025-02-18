// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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
        private Assembly? _topLevelAssembly;
        private RuntimeAssembly? _requestingAssembly;
        private AssemblyLoadContext? _loadContext;

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            RuntimeAssembly requestingAssembly,
            bool throwOnError = false,
            bool ignoreCase = false)
        {
            return GetType(typeName, null, null, requestingAssembly, throwOnError, ignoreCase, false);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            RuntimeAssembly requestingAssembly,
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
                _requestingAssembly = requestingAssembly,
                _loadContext = AssemblyLoadContext.CurrentContextualReflectionContext,
            }.Resolve(parsed);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            bool throwOnError,
            bool ignoreCase,
            Assembly topLevelAssembly)
        {
            // Mono AssemblyBuilder is also a RuntimeAssembly, see AssemblyBuilder.Mono.cs
            Debug.Assert(topLevelAssembly is RuntimeAssembly or Emit.RuntimeAssemblyBuilder);
            TypeName? parsed = TypeNameParser.Parse(typeName, throwOnError);

            if (parsed is null)
            {
                return null;
            }
            else if (topLevelAssembly is not null && parsed.AssemblyName is not null)
            {
                return throwOnError ? throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly) : null;
            }

            return new TypeNameResolver()
            {
                _throwOnError = throwOnError,
                _ignoreCase = ignoreCase,
                _topLevelAssembly = topLevelAssembly,
                _requestingAssembly = Unsafe.As<RuntimeAssembly>(topLevelAssembly),
                _loadContext = AssemblyLoadContext.CurrentContextualReflectionContext,
            }.Resolve(parsed);
        }

        // Used by VM
        internal static unsafe RuntimeType? GetTypeHelper(string typeName, IntPtr gchALC, RuntimeAssembly? requestingAssembly,
            bool ignoreCase, bool useTopLevelAssembly)
        {
            if (typeName.Length == 0)
            {
                return null;
            }

            TypeName? parsed = TypeNameParser.Parse(typeName, throwOnError: false);
            if (parsed is null)
            {
                return null;
            }

            if (useTopLevelAssembly && parsed.AssemblyName is not null)
            {
                throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly);
            }

            return (RuntimeType?)new TypeNameResolver()
            {
                _ignoreCase = ignoreCase,
                _topLevelAssembly = useTopLevelAssembly ? requestingAssembly : null,
                _requestingAssembly = requestingAssembly,
                _loadContext = AssemblyLoadContext.GetAssemblyLoadContext(gchALC),
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
                assembly = RuntimeAssembly.InternalLoad(name, _requestingAssembly!,
                    _loadContext, throwOnFileNotFound: _throwOnError);
            }
            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "TypeNameResolver.GetType is marked as RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeNameResolver.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetType(string escapedTypeName, ReadOnlySpan<string> nestedTypeNames, TypeName parsedName)
        {
            Assembly? assembly;

            if (parsedName.AssemblyName is not null)
            {
                assembly = ResolveAssembly(parsedName.AssemblyName.ToAssemblyName());
                if (assembly is null)
                {
                    return null;
                }
            }
            else
            {
                assembly = _topLevelAssembly;
            }

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
                    type = GetTypeFromDefaultAssemblies(TypeName.Unescape(escapedTypeName), parsedName);
                }
                // We cannot check if it is RuntimeAssembly, because the object might be a RuntimeAssemblyBuilder.
                else if (AssemblyLoadContext.GetRuntimeAssembly(assembly) is { } ra)
                {
                    type = ra.GetTypeCore(TypeName.Unescape(escapedTypeName), ignoreCase: _ignoreCase);
                }
                else
                {
                    // This is a third-party Assembly object. Emulate GetTypeCore() by calling the public GetType()
                    // method. This is wasteful because it'll probably reparse a type string that we've already parsed
                    // but it can't be helped.
                    type = assembly.GetType(escapedTypeName, _throwOnError, _ignoreCase);
                }

                if (type is null)
                {
                    if (_throwOnError)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveType, parsedName.FullName),
                            typeName: escapedTypeName);
                    }
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

        [RequiresUnreferencedCode("Types might be removed by trimming. If the type name is a string literal, consider using Type.GetType instead.")]
        private Type? GetTypeFromDefaultAssemblies(string typeName, TypeName parsedName)
        {
            RuntimeAssembly? requestingAssembly = _requestingAssembly;
            if (requestingAssembly is not null)
            {
                Type? type = requestingAssembly.GetTypeCore(typeName, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly coreLib = (RuntimeAssembly)typeof(object).Assembly;
            if (requestingAssembly != coreLib)
            {
                Type? type = coreLib.GetTypeCore(typeName, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            RuntimeAssembly? resolvedAssembly = AssemblyLoadContext.OnTypeResolve(requestingAssembly, parsedName.FullName);
            if (resolvedAssembly is not null)
            {
                Type? type = resolvedAssembly.GetTypeCore(typeName, ignoreCase: _ignoreCase);
                if (type is not null)
                    return type;
            }

            if (_throwOnError)
            {
                throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, parsedName.FullName, (requestingAssembly ?? coreLib).FullName),
                    typeName: parsedName.FullName);
            }

            return null;
        }
    }
}
