// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Reflection.Augments;
using Internal.Reflection.Core.NonPortable;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        public bool IsInterface => (GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;

        [Intrinsic]
        public static Type? GetTypeFromHandle(RuntimeTypeHandle handle) => handle.IsNull ? null : GetTypeFromEETypePtr(handle.ToEETypePtr());

        internal static Type GetTypeFromEETypePtr(EETypePtr eeType)
        {
            // If we support the writable data section on EETypes, the runtime type associated with the MethodTable
            // is cached there. If writable data is not supported, we need to do a lookup in the runtime type
            // unifier's hash table.
            if (Internal.Runtime.MethodTable.SupportsWritableData)
            {
                ref GCHandle handle = ref eeType.GetWritableData<GCHandle>();
                if (handle.IsAllocated)
                {
                    return Unsafe.As<Type>(handle.Target);
                }
                else
                {
                    return GetTypeFromEETypePtrSlow(eeType, ref handle);
                }
            }
            else
            {
                return RuntimeTypeUnifier.GetRuntimeTypeForEEType(eeType);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Type GetTypeFromEETypePtrSlow(EETypePtr eeType, ref GCHandle handle)
        {
            // Note: this is bypassing the "fast" unifier cache (based on a simple IntPtr
            // identity of MethodTable pointers). There is another unifier behind that cache
            // that ensures this code is race-free.
            Type result = RuntimeTypeUnifier.GetRuntimeTypeBypassCache(eeType);
            GCHandle tempHandle = GCHandle.Alloc(result);

            // We don't want to leak a handle if there's a race
            if (Interlocked.CompareExchange(ref Unsafe.As<GCHandle, IntPtr>(ref handle), (IntPtr)tempHandle, default) != default)
            {
                tempHandle.Free();
            }

            return result;
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
