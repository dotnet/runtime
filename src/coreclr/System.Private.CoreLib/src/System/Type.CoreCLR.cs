// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        public bool IsInterface
        {
            get
            {
                if (this is RuntimeType rt)
                    return RuntimeTypeHandle.IsInterface(rt);
                return (GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
            }
        }

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
        public static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, false, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackMark);
        }

        [RequiresUnreferencedCode("The type might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly?, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
        }

        internal virtual RuntimeTypeHandle GetTypeHandleInternal()
        {
            return TypeHandle;
        }

        // Given a class handle, this will return the class for that handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Type? GetTypeFromHandle(RuntimeTypeHandle handle);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool operator ==(Type? left, Type? right);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool operator !=(Type? left, Type? right);

        // Exists to faciliate code sharing between CoreCLR and CoreRT.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRuntimeImplemented() => this is RuntimeType;
    }
}
