// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System
{
    internal sealed partial class SafeTypeNameParserHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region QCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_ReleaseTypeNameParser")]
        private static partial void Release(IntPtr pTypeNameParser);
        #endregion

        public SafeTypeNameParserHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Release(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

    internal sealed partial class TypeNameParser : IDisposable
    {
        #region QCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_CreateTypeNameParser", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void _CreateTypeNameParser(string typeName, ObjectHandleOnStack retHandle, [MarshalAs(UnmanagedType.Bool)] bool throwOnError);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_GetNames")]
        private static partial void _GetNames(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_GetTypeArguments")]
        private static partial void _GetTypeArguments(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_GetModifiers")]
        private static partial void _GetModifiers(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeName_GetAssemblyName")]
        private static partial void _GetAssemblyName(SafeTypeNameParserHandle pTypeNameParser, StringHandleOnStack retString);
        #endregion

        #region Static Members
        [RequiresUnreferencedCode("The type might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            if (typeName.Length > 0 && typeName[0] == '\0')
                throw new ArgumentException(SR.Format_StringZeroLength);

            Type? ret = null;

            SafeTypeNameParserHandle? handle = CreateTypeNameParser(typeName, throwOnError);

            if (handle != null)
            {
                // If we get here the typeName must have been successfully parsed.
                // Let's construct the Type object.
                using (TypeNameParser parser = new TypeNameParser(handle))
                {
                    ret = parser.ConstructType(assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
                }
            }

            return ret;
        }
        #endregion

        #region Private Data Members
        private readonly SafeTypeNameParserHandle m_NativeParser;
        private static readonly char[] SPECIAL_CHARS = { ',', '[', ']', '&', '*', '+', '\\' }; /* see typeparse.h */
        #endregion

        #region Constructor and Disposer
        private TypeNameParser(SafeTypeNameParserHandle handle)
        {
            m_NativeParser = handle;
        }

        public void Dispose()
        {
            m_NativeParser.Dispose();
        }
        #endregion

        #region private Members
        [RequiresUnreferencedCode("The type might be removed")]
        private unsafe Type? ConstructType(
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            // assembly name
            Assembly? assembly = null;
            string asmName = GetAssemblyName();

            // GetAssemblyName never returns null
            Debug.Assert(asmName != null);

            if (asmName.Length > 0)
            {
                assembly = ResolveAssembly(asmName, assemblyResolver, throwOnError, ref stackMark);

                if (assembly == null)
                {
                    // Cannot resolve the assembly. If throwOnError is true we should have already thrown.
                    return null;
                }
            }

            string[]? names = GetNames();
            if (names == null)
            {
                // This can only happen if the type name is an empty string or if the first char is '\0'
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);

                return null;
            }

            Type? baseType = ResolveType(assembly, names, typeResolver, throwOnError, ignoreCase, ref stackMark);

            if (baseType == null)
            {
                // Cannot resolve the type. If throwOnError is true we should have already thrown.
                Debug.Assert(!throwOnError);
                return null;
            }

            SafeTypeNameParserHandle[]? typeArguments = GetTypeArguments();

            Type?[]? types = null;
            if (typeArguments != null)
            {
                types = new Type[typeArguments.Length];
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    Debug.Assert(typeArguments[i] != null);

                    using (TypeNameParser argParser = new TypeNameParser(typeArguments[i]))
                    {
                        types[i] = argParser.ConstructType(assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
                    }

                    if (types[i] == null)
                    {
                        // If throwOnError is true argParser.ConstructType should have already thrown.
                        Debug.Assert(!throwOnError);
                        return null;
                    }
                }
            }

            int[]? modifiers = GetModifiers();
            return RuntimeTypeHandle.GetTypeHelper(baseType, types!, modifiers);
        }

        private static Assembly? ResolveAssembly(string asmName, Func<AssemblyName, Assembly?>? assemblyResolver, bool throwOnError, ref StackCrawlMark stackMark)
        {
            Debug.Assert(!string.IsNullOrEmpty(asmName));

            Assembly? assembly;
            if (assemblyResolver == null)
            {
                if (throwOnError)
                {
                    assembly = RuntimeAssembly.InternalLoad(asmName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                else
                {
                    // When throwOnError is false we should only catch FileNotFoundException.
                    // Other exceptions like BadImangeFormatException should still fly.
                    try
                    {
                        assembly = RuntimeAssembly.InternalLoad(asmName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
                    }
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }
            }
            else
            {
                assembly = assemblyResolver(new AssemblyName(asmName));
                if (assembly == null && throwOnError)
                {
                    throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, asmName));
                }
            }

            return assembly;
        }

        [RequiresUnreferencedCode("The type might be removed")]
        private static Type? ResolveType(Assembly? assembly, string[] names, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
        {
            Debug.Assert(names != null && names.Length > 0);

            Type? type;

            // both the customer provided and the default type resolvers accept escaped type names
            string OuterMostTypeName = EscapeTypeName(names[0]);

            // Resolve the top level type.
            if (typeResolver != null)
            {
                type = typeResolver(assembly, OuterMostTypeName, ignoreCase);

                if (type == null && throwOnError)
                {
                    string errorString = assembly == null ?
                        SR.Format(SR.TypeLoad_ResolveType, OuterMostTypeName) :
                        SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, OuterMostTypeName, assembly.FullName);

                    throw new TypeLoadException(errorString);
                }
            }
            else
            {
                if (assembly == null)
                {
                    type = RuntimeType.GetType(OuterMostTypeName, throwOnError, ignoreCase, ref stackMark);
                }
                else
                {
                    type = assembly.GetType(OuterMostTypeName, throwOnError, ignoreCase);
                }
            }

            // Resolve nested types.
            if (type != null)
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
                if (ignoreCase)
                    bindingFlags |= BindingFlags.IgnoreCase;

                for (int i = 1; i < names.Length; i++)
                {
                    type = type.GetNestedType(names[i], bindingFlags);

                    if (type == null)
                    {
                        if (throwOnError)
                            throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, names[i], names[i - 1]));
                        else
                            break;
                    }
                }
            }

            return type;
        }

        private static string EscapeTypeName(string name)
        {
            if (name.IndexOfAny(SPECIAL_CHARS) < 0)
                return name;

            var sb = new ValueStringBuilder(stackalloc char[64]);
            foreach (char c in name)
            {
                if (Array.IndexOf<char>(SPECIAL_CHARS, c) >= 0)
                    sb.Append('\\');

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static SafeTypeNameParserHandle? CreateTypeNameParser(string typeName, bool throwOnError)
        {
            SafeTypeNameParserHandle? retHandle = null;
            _CreateTypeNameParser(typeName, ObjectHandleOnStack.Create(ref retHandle), throwOnError);

            return retHandle;
        }

        private string[]? GetNames()
        {
            string[]? names = null;
            _GetNames(m_NativeParser, ObjectHandleOnStack.Create(ref names));

            return names;
        }

        private SafeTypeNameParserHandle[]? GetTypeArguments()
        {
            SafeTypeNameParserHandle[]? arguments = null;
            _GetTypeArguments(m_NativeParser, ObjectHandleOnStack.Create(ref arguments));

            return arguments;
        }

        private int[]? GetModifiers()
        {
            int[]? modifiers = null;
            _GetModifiers(m_NativeParser, ObjectHandleOnStack.Create(ref modifiers));

            return modifiers;
        }

        private string GetAssemblyName()
        {
            string? assemblyName = null;
            _GetAssemblyName(m_NativeParser, new StringHandleOnStack(ref assemblyName));

            return assemblyName!;
        }
        #endregion
    }
}
