// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace System.Reflection
{
    internal unsafe ref partial struct TypeNameParser
    {
        private ReadOnlySpan<char> _input;
        private int _index;

        private Func<AssemblyName, Assembly?>? _assemblyResolver;
        private Func<Assembly?, string, bool, Type?>? _typeResolver;
        private bool _throwOnError;
        private bool _ignoreCase;
        private bool _prohibitAssemblyQualifiedName;
        private void* _stackMark;

        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            bool prohibitAssemblyQualifiedName,
            ref StackCrawlMark stackMark)
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
                _stackMark = Unsafe.AsPointer(ref stackMark)
            }.Parse();
        }

        private Assembly? ResolveAssembly(string assemblyName)
        {
            Assembly? assembly;
            if (_assemblyResolver != null)
            {
                assembly = _assemblyResolver(new AssemblyName(assemblyName));
                if (assembly == null && _throwOnError)
                {
                    throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, assemblyName));
                }
            }
            else
            {
                ref StackCrawlMark stackMark = ref Unsafe.AsRef<StackCrawlMark>(_stackMark);

                if (_throwOnError)
                {
                    assembly = RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                else
                {
                    // When throwOnError is false we should only catch FileNotFoundException.
                    // Other exceptions like BadImangeFormatException should still fly.
                    try
                    {
                        assembly = RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
                    }
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }
            }
            return assembly;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeNameParser.GetType is marked as RequiresUnreferencedCode.")]
        private Type? GetType(string name, string? assemblyNameIfAny)
        {
            Assembly? assembly = (assemblyNameIfAny != null) ? ResolveAssembly(assemblyNameIfAny) : null;

            // Both the external type resolver and the default type resolvers expect escaped type names
            string escapedTypeName = EscapeTypeName(name);

            Type? type;

            // Resolve the top level type.
            if (_typeResolver != null)
            {
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
                if (assembly == null)
                {
                    ref StackCrawlMark stackMark = ref Unsafe.AsRef<StackCrawlMark>(_stackMark);

                    type = RuntimeType.GetType(escapedTypeName, _throwOnError, _ignoreCase, ref stackMark);
                }
                else
                {
                    type = assembly.GetType(escapedTypeName, _throwOnError, _ignoreCase);
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
