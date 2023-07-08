// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace Internal.Reflection.Core.NonPortable
{
    //
    // ! If you change this policy to not unify all instances, you must change the implementation of Equals/GetHashCode in the runtime type classes.
    //
    // The RuntimeTypeUnifier maintains a record of all System.Type objects created by the runtime. The split into two classes is an artifact of
    // reflection being implemented partly in System.Private.CoreLib and partly in S.R.R.
    //
    // Though the present incarnation enforces the "one instance per semantic identity rule", its surface area is also designed
    // to be able to switch to a non-unified model if desired.
    //
    // ! If you do switch away from a "one instance per semantic identity rule", you must also change the implementation
    // ! of RuntimeType.Equals() and RuntimeType.GetHashCode().
    //
    //
    // Internal details:
    //
    //  The RuntimeType is not a single class but a family of classes that can be categorized along two dimensions:
    //
    //    - Type structure (named vs. array vs. generic instance, etc.)
    //
    //    - Is invokable (i.e. has a RuntimeTypeHandle.)
    //
    //  Taking advantage of this, RuntimeTypeUnifier splits the unification across several type tables, each with its own separate lock.
    //  Each type table owns a specific group of RuntimeTypes. These groups can overlap. In particular, types with EETypes can and do
    //  appear in both TypeTableForTypesWithEETypes and the "inspection" type table for the type's specific flavor. This allows
    //  fast lookups for both the Object.GetType() calls and the metadata initiated lookups.
    //
    internal static partial class RuntimeTypeUnifier
    {
        //
        // Retrieves the unified Type object for given RuntimeTypeHandle (this is basically the Type.GetTypeFromHandle() api without the input validation.)
        //
        internal static Type GetRuntimeTypeForEEType(EETypePtr eeType)
        {
            // If writable data is supported, we shouldn't be using the hashtable - the runtime type
            // is accessible through a couple indirections from the EETypePtr which is much faster.
            Debug.Assert(!Internal.Runtime.MethodTable.SupportsWritableData);
            return RuntimeTypeHandleToTypeCache.Table.GetOrAdd(eeType.RawValue);
        }

        //
        // TypeTable mapping raw RuntimeTypeHandles (normalized or otherwise) to Types.
        //
        // Unlike most unifier tables, RuntimeTypeHandleToRuntimeTypeCache exists for fast lookup, not unification. It hashes and compares
        // on the raw IntPtr value of the RuntimeTypeHandle. Because Redhawk can and does create multiple EETypes for the same
        // semantically identical type, the same RuntimeType can legitimately appear twice in this table. The factory, however,
        // does a second lookup in the true unifying tables rather than creating the Type itself.
        // Thus, the one-to-one relationship between Type reference identity and Type semantic identity is preserved.
        //
        private sealed class RuntimeTypeHandleToTypeCache : ConcurrentUnifierW<IntPtr, Type>
        {
            private RuntimeTypeHandleToTypeCache() { }

            protected sealed override Type Factory(IntPtr rawRuntimeTypeHandleKey)
            {
                EETypePtr eeType = new EETypePtr(rawRuntimeTypeHandleKey);
                return GetRuntimeTypeBypassCache(eeType);
            }

            public static readonly RuntimeTypeHandleToTypeCache Table = new RuntimeTypeHandleToTypeCache();
        }

        // This bypasses the CoreLib's unifier, but there's another unifier deeper within the reflection stack:
        // this code is safe to call without locking. See comment above.
        public static Type GetRuntimeTypeBypassCache(EETypePtr eeType)
        {
            RuntimeTypeHandle runtimeTypeHandle = new RuntimeTypeHandle(eeType);

            ReflectionExecutionDomainCallbacks callbacks = RuntimeAugments.Callbacks;

            if (eeType.IsDefType)
            {
                if (eeType.IsGeneric)
                {
                    return callbacks.GetConstructedGenericTypeForHandle(runtimeTypeHandle);
                }
                else
                {
                    return callbacks.GetNamedTypeForHandle(runtimeTypeHandle);
                }
            }
            else if (eeType.IsArray)
            {
                if (!eeType.IsSzArray)
                    return callbacks.GetMdArrayTypeForHandle(runtimeTypeHandle, eeType.ArrayRank);
                else
                    return callbacks.GetArrayTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsPointer)
            {
                return callbacks.GetPointerTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsFunctionPointer)
            {
                return callbacks.GetFunctionPointerTypeForHandle(runtimeTypeHandle);
            }
            else if (eeType.IsByRef)
            {
                return callbacks.GetByRefTypeForHandle(runtimeTypeHandle);
            }
            else
            {
                Debug.Fail("Invalid RuntimeTypeHandle");
                throw new ArgumentException(SR.Arg_InvalidHandle);
            }
        }
    }
}
