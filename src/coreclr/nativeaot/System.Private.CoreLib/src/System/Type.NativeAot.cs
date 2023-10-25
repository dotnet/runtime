// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Reflection.Augments;
using Internal.Reflection.Core.NonPortable;
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
            // If we support the writable data section on MethodTables, the runtime type associated with the MethodTable
            // is cached there. If writable data is not supported, we need to do a lookup in the runtime type
            // unifier's hash table.
            if (MethodTable.SupportsWritableData)
            {
                ref UnsafeGCHandle handle = ref Unsafe.AsRef<UnsafeGCHandle>(pMT->WritableData);
                if (handle.IsAllocated)
                {
                    return Unsafe.As<RuntimeType>(handle.Target);
                }
                else
                {
                    return GetTypeFromMethodTableSlow(pMT, ref handle);
                }
            }
            else
            {
                return RuntimeTypeUnifier.GetRuntimeTypeForMethodTable(pMT);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe RuntimeType GetTypeFromMethodTableSlow(MethodTable* pMT, ref UnsafeGCHandle handle)
        {
            // Note: this is bypassing the "fast" unifier cache (based on a simple IntPtr
            // identity of MethodTable pointers). There is another unifier behind that cache
            // that ensures this code is race-free.
            Type result = RuntimeTypeUnifier.GetRuntimeTypeBypassCache(new EETypePtr(pMT));
            UnsafeGCHandle tempHandle = UnsafeGCHandle.Alloc(result);

            // We don't want to leak a handle if there's a race
            if (Interlocked.CompareExchange(ref Unsafe.As<UnsafeGCHandle, IntPtr>(ref handle), Unsafe.As<UnsafeGCHandle, IntPtr>(ref tempHandle), default) != default)
            {
                tempHandle.Free();
            }

            return Unsafe.As<RuntimeType>(handle.Target);
        }

        internal EETypePtr GetEEType()
        {
            RuntimeTypeHandle typeHandle = RuntimeAugments.Callbacks.GetTypeHandleIfAvailable(this);
            Debug.Assert(!typeHandle.IsNull);
            return typeHandle.ToEETypePtr();
        }

        internal bool TryGetEEType(out EETypePtr eeType)
        {
            RuntimeTypeHandle typeHandle = RuntimeAugments.Callbacks.GetTypeHandleIfAvailable(this);
            if (typeHandle.IsNull)
            {
                eeType = default(EETypePtr);
                return false;
            }
            eeType = typeHandle.ToEETypePtr();
            return true;
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
