// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        [Intrinsic]
        public static unsafe Type? GetTypeFromHandle(RuntimeTypeHandle handle) => handle.IsNull ? null : GetTypeFromMethodTable(handle.ToMethodTable());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe RuntimeType GetTypeFromMethodTable(MethodTable* pMT)
        {
            ref RuntimeType? type = ref Unsafe.AsRef<RuntimeType?>(pMT->WritableData);
            return type ?? GetTypeFromMethodTableSlow(pMT);
        }

        private static class AllocationLockHolder
        {
            public static readonly Lock AllocationLock = new Lock(useTrivialWaits: true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe RuntimeType GetTypeFromMethodTableSlow(MethodTable* pMT)
        {
            // Allocate and set the RuntimeType under a lock - there's no way to free it if there is a race.
            using (AllocationLockHolder.AllocationLock.EnterScope())
            {
                ref RuntimeType? runtimeTypeCache = ref Unsafe.AsRef<RuntimeType?>(pMT->WritableData);
                if (runtimeTypeCache != null)
                    return runtimeTypeCache;

                RuntimeType? type = FrozenObjectHeapManager.Instance.TryAllocateObject<RuntimeType>();
                if (type == null)
                    throw new OutOfMemoryException();

                type.DangerousSetUnderlyingEEType(pMT);

                runtimeTypeCache = type;

                return type;
            }
        }

        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName) => GetType(typeName, throwOnError: false, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, bool throwOnError) => GetType(typeName, throwOnError: throwOnError, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, bool throwOnError, bool ignoreCase)
        {
            return TypeNameResolver.GetType(typeName, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }

        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver) => GetType(typeName, assemblyResolver, typeResolver, throwOnError: false, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError) => GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase)
        {
            return TypeNameResolver.GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }
    }
}
