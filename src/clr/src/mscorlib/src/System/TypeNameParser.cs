// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System
{
    [SecurityCritical]
    internal class SafeTypeNameParserHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region QCalls
        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _ReleaseTypeNameParser(IntPtr pTypeNameParser);
        #endregion

        public SafeTypeNameParserHandle()
            : base(true)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            _ReleaseTypeNameParser(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

    internal sealed class TypeNameParser : IDisposable
    {
        #region QCalls
        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _CreateTypeNameParser(string typeName, ObjectHandleOnStack retHandle, bool throwOnError);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _GetNames(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _GetTypeArguments(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _GetModifiers(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void _GetAssemblyName(SafeTypeNameParserHandle pTypeNameParser, StringHandleOnStack retString);
        #endregion

        #region Static Members
        [SecuritySafeCritical]
        internal static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            if (typeName.Length > 0 && typeName[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
            Contract.EndContractBlock();

            Type ret = null;

            SafeTypeNameParserHandle handle = CreateTypeNameParser(typeName, throwOnError);

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
        [SecurityCritical]
        private SafeTypeNameParserHandle m_NativeParser;
        private static readonly char[] SPECIAL_CHARS = {',', '[', ']', '&', '*', '+', '\\'}; /* see typeparse.h */
        #endregion

        #region Constructor and Disposer
        [SecuritySafeCritical]
        private TypeNameParser(SafeTypeNameParserHandle handle)
        {
            m_NativeParser = handle;
        }

        [SecuritySafeCritical]
        public void Dispose()
        {
            m_NativeParser.Dispose();
        }
        #endregion

        #region private Members
        [SecuritySafeCritical]
        private unsafe Type ConstructType(
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            // assembly name
            Assembly assembly = null;
            string asmName = GetAssemblyName();

            // GetAssemblyName never returns null
            Contract.Assert(asmName != null);

            if (asmName.Length > 0)
            {
                assembly = ResolveAssembly(asmName, assemblyResolver, throwOnError, ref stackMark);

                if (assembly == null)
                {
                    // Cannot resolve the assembly. If throwOnError is true we should have already thrown.
                    return null;
                }
            }

            string[] names = GetNames();
            if (names == null)
            {
                // This can only happen if the type name is an empty string or if the first char is '\0'
                if (throwOnError)
                    throw new TypeLoadException(Environment.GetResourceString("Arg_TypeLoadNullStr"));

                return null;
            }

            Type baseType = ResolveType(assembly, names, typeResolver, throwOnError, ignoreCase, ref stackMark);

            if (baseType == null)
            {
                // Cannot resolve the type. If throwOnError is true we should have already thrown.
                Contract.Assert(throwOnError == false);
                return null;
            }

            SafeTypeNameParserHandle[] typeArguments = GetTypeArguments();

            Type[] types = null;
            if (typeArguments != null)
            {
                types = new Type[typeArguments.Length];
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    Contract.Assert(typeArguments[i] != null);

                    using (TypeNameParser argParser = new TypeNameParser(typeArguments[i]))
                    {
                        types[i] = argParser.ConstructType(assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
                    }

                    if (types[i] == null)
                    {
                        // If throwOnError is true argParser.ConstructType should have already thrown.
                        Contract.Assert(throwOnError == false);
                        return null;
                    }
                }
            }

            int[] modifiers = GetModifiers();

            fixed (int* ptr = modifiers)
            {
                IntPtr intPtr = new IntPtr(ptr);
                return RuntimeTypeHandle.GetTypeHelper(baseType, types, intPtr, modifiers == null ? 0 : modifiers.Length);
            }
        }

        [SecuritySafeCritical]
        private static Assembly ResolveAssembly(string asmName, Func<AssemblyName, Assembly> assemblyResolver, bool throwOnError, ref StackCrawlMark stackMark)
        {
            Contract.Requires(asmName != null && asmName.Length > 0);

            Assembly assembly = null;

            if (assemblyResolver == null)
            {
                if (throwOnError)
                {
                    assembly = RuntimeAssembly.InternalLoad(asmName, null, ref stackMark, false /*forIntrospection*/);
                }
                else
                {
                    // When throwOnError is false we should only catch FileNotFoundException.
                    // Other exceptions like BadImangeFormatException should still fly.
                    try
                    {
                        assembly = RuntimeAssembly.InternalLoad(asmName, null,  ref stackMark, false /*forIntrospection*/);
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
                    throw new FileNotFoundException(Environment.GetResourceString("FileNotFound_ResolveAssembly", asmName));
                }
            }

            return assembly;
        }

        private static Type ResolveType(Assembly assembly, string[] names, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
        {
            Contract.Requires(names != null && names.Length > 0);

            Type type = null;

            // both the customer provided and the default type resolvers accept escaped type names
            string OuterMostTypeName = EscapeTypeName(names[0]);

            // Resolve the top level type.
            if (typeResolver != null)
            {
                type = typeResolver(assembly, OuterMostTypeName, ignoreCase);

                if (type == null && throwOnError)
                {
                    string errorString = assembly == null ?
                        Environment.GetResourceString("TypeLoad_ResolveType", OuterMostTypeName) :
                        Environment.GetResourceString("TypeLoad_ResolveTypeFromAssembly", OuterMostTypeName, assembly.FullName);

                    throw new TypeLoadException(errorString);
                }
            }
            else
            {
                if (assembly == null)
                {
                    type = RuntimeType.GetType(OuterMostTypeName, throwOnError, ignoreCase, false, ref stackMark);
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
                            throw new TypeLoadException(Environment.GetResourceString("TypeLoad_ResolveNestedType", names[i], names[i-1]));
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

            StringBuilder sb = StringBuilderCache.Acquire();
            foreach (char c in name)
            {
                if (Array.IndexOf<char>(SPECIAL_CHARS, c) >= 0)
                    sb.Append('\\');

                sb.Append(c);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        [SecuritySafeCritical]
        private static SafeTypeNameParserHandle CreateTypeNameParser(string typeName, bool throwOnError)
        {
            SafeTypeNameParserHandle retHandle = null;
            _CreateTypeNameParser(typeName, JitHelpers.GetObjectHandleOnStack(ref retHandle), throwOnError);

            return retHandle;
        }

        [SecuritySafeCritical]
        private string[] GetNames()
        {
            string[] names = null;
            _GetNames(m_NativeParser, JitHelpers.GetObjectHandleOnStack(ref names));

            return names;
        }

        [SecuritySafeCritical]
        private SafeTypeNameParserHandle[] GetTypeArguments()
        {
            SafeTypeNameParserHandle[] arguments = null;
            _GetTypeArguments(m_NativeParser, JitHelpers.GetObjectHandleOnStack(ref arguments));

            return arguments;
        }

        [SecuritySafeCritical]
        private int[] GetModifiers()
        {
            int[] modifiers = null;
            _GetModifiers(m_NativeParser, JitHelpers.GetObjectHandleOnStack(ref modifiers));

            return modifiers;
        }

        [SecuritySafeCritical]
        private string GetAssemblyName()
        {
            string assemblyName = null;
            _GetAssemblyName(m_NativeParser, JitHelpers.GetStringHandleOnStack(ref assemblyName));
            
            return assemblyName;
        }
        #endregion
    }
}
