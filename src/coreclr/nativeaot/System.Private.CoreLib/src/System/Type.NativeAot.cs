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
            public static LowLevelLock AllocationLock = new LowLevelLock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe RuntimeType GetTypeFromMethodTableSlow(MethodTable* pMT)
        {
            // Allocate and set the RuntimeType under a lock - there's no way to free it if there is a race.
            AllocationLockHolder.AllocationLock.Acquire();
            try
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
            finally
            {
                AllocationLockHolder.AllocationLock.Release();
            }
        }

        //
        // This is a port of the desktop CLR's RuntimeType.FormatTypeName() routine. This routine is used by various Reflection ToString() methods
        // to display the name of a type. Do not use for any other purpose as it inherits some pretty quirky desktop behavior.
        //
        internal string FormatTypeNameForReflection()
        {
            // Legacy: this doesn't make sense, why use only Name for nested types but otherwise
            // ToString() which contains namespace.
            Type rootElementType = this;
            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType()!;
            if (rootElementType.IsNested)
            {
                return Name!;
            }

            // Legacy: why removing "System"? Is it just because C# has keywords for these types?
            // If so why don't we change it to lower case to match the C# keyword casing?
            string typeName = ToString();
            if (typeName.StartsWith("System."))
            {
                if (rootElementType.IsPrimitive || rootElementType == typeof(void))
                {
                    typeName = typeName.Substring("System.".Length);
                }
            }
            return typeName;
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
            return TypeNameParser.GetType(typeName, throwOnError: throwOnError, ignoreCase: ignoreCase);
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
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }
    }
}
