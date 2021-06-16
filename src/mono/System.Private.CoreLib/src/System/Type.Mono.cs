// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System
{
    public partial class Type
    {
        #region keep in sync with object-internals.h
        internal RuntimeTypeHandle _impl;
        #endregion

        internal IntPtr GetUnderlyingNativeHandle()
        {
            return _impl.Value;
        }

        internal bool IsRuntimeImplemented() => this.UnderlyingSystemType is RuntimeType;

        internal virtual bool IsTypeBuilder() => false;

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

        public static Type GetTypeFromHandle(RuntimeTypeHandle handle)
        {
            if (handle.Value == IntPtr.Zero)
                return null!; // FIXME: shouldn't return null

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
            throw new InvalidOperationException("can only be called in generic type");
        }

        internal virtual ConstructorInfo GetConstructor(ConstructorInfo fromNoninstanciated)
        {
            throw new InvalidOperationException("can only be called in generic type");
        }

        internal virtual FieldInfo GetField(FieldInfo fromNoninstanciated)
        {
            throw new InvalidOperationException("can only be called in generic type");
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Type internal_from_handle(IntPtr handle);

        [Intrinsic]
        public static bool operator ==(Type? left, Type? right) => left == right;

        public static bool operator !=(Type? left, Type? right)
        {
            return !(left == right);
        }
    }
}
