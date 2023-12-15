// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class Type
    {
        #region keep in sync with object-internals.h
        internal RuntimeTypeHandle _impl;
        internal LoaderAllocator? m_keepalive;
        #endregion

        internal IntPtr GetUnderlyingNativeHandle()
        {
            return _impl.Value;
        }

        internal virtual bool IsTypeBuilder() => false;

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName, bool throwOnError, bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, ignoreCase, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName, bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, false, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetType(typeName, assemblyResolver, typeResolver, false, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetType(typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        private static Type? GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
        {
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
        }

        public static Type? GetTypeFromHandle(RuntimeTypeHandle handle)
        {
            if (handle.Value == IntPtr.Zero)
                return null;

            return internal_from_handle(handle.Value);
        }

        internal virtual Type InternalResolve()
        {
            return UnderlyingSystemType;
        }

        // Called from the runtime to return the corresponding finished Type object
        internal virtual Type RuntimeResolve()
        {
            throw new NotImplementedException();
        }

        internal virtual bool IsUserType
        {
            get
            {
                return true;
            }
        }

        internal virtual MethodInfo GetMethod(MethodInfo fromNoninstanciated)
        {
            throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        }

        internal virtual ConstructorInfo GetConstructor(ConstructorInfo fromNoninstanciated)
        {
            throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        }

        internal virtual FieldInfo GetField(FieldInfo fromNoninstanciated)
        {
            throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Type internal_from_handle(IntPtr handle);
    }
}
