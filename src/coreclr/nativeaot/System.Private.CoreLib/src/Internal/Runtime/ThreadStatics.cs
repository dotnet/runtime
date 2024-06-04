// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerHelpers;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    /// <summary>
    /// This class is used by ReadyToRun helpers to get access to thread static fields of a type
    /// and to allocate required TLS memory.
    /// </summary>
    internal static class ThreadStatics
    {
        [ThreadStatic]
        private static object t_inlinedThreadStaticBase;

        /// <summary>
        /// This method is called from a ReadyToRun helper to get base address of thread
        /// static storage for the given type.
        /// </summary>
        internal static unsafe object GetThreadStaticBaseForType(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            if (typeTlsIndex < 0)
                return t_inlinedThreadStaticBase;

            return GetUninlinedThreadStaticBaseForType(pModuleData, typeTlsIndex);
        }

        internal static unsafe object GetInlinedThreadStaticBaseSlow(ref object? threadStorage)
        {
            Debug.Assert(threadStorage == null);
            // Allocate an object that will represent a memory block for all thread static fields
            TypeManagerHandle typeManager = MethodTable.Of<object>()->TypeManager;
            object threadStaticBase = AllocateThreadStaticStorageForType(typeManager, 0);

            // register the storage location with the thread for GC reporting.
            RuntimeImports.RhRegisterInlinedThreadStaticRoot(ref threadStorage, typeManager);

            // assign the storage block to the storage variable and return
            threadStorage = threadStaticBase;
            t_inlinedThreadStaticBase = threadStaticBase;

            return threadStaticBase;
        }

        internal static unsafe object GetUninlinedThreadStaticBaseForType(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            Debug.Assert(typeTlsIndex >= 0);
            int moduleIndex = pModuleData->ModuleIndex;
            Debug.Assert(moduleIndex >= 0);

            object[][] perThreadStorage = RuntimeImports.RhGetThreadStaticStorage();
            if (perThreadStorage != null && perThreadStorage.Length > moduleIndex)
            {
                object[] perModuleStorage = perThreadStorage[moduleIndex];
                if (perModuleStorage != null && perModuleStorage.Length > typeTlsIndex)
                {
                    object threadStaticBase = perModuleStorage[typeTlsIndex];
                    if (threadStaticBase != null)
                    {
                        return threadStaticBase;
                    }
                }
            }

            return GetUninlinedThreadStaticBaseForTypeSlow(pModuleData, typeTlsIndex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe object GetUninlinedThreadStaticBaseForTypeSlow(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            Debug.Assert(typeTlsIndex >= 0);
            int moduleIndex = pModuleData->ModuleIndex;
            Debug.Assert(moduleIndex >= 0);

            // Get the array that holds thread statics for the current thread, if none present
            // allocate a new one big enough to hold the current module data
            ref object[][] threadStorage = ref RuntimeImports.RhGetThreadStaticStorage();
            if (threadStorage == null)
            {
                threadStorage = new object[moduleIndex + 1][];
            }
            else if (moduleIndex >= threadStorage.Length)
            {
                Array.Resize(ref threadStorage, moduleIndex + 1);
            }

            // Get the array that holds thread static memory blocks for each type in the given module
            ref object[] moduleStorage = ref threadStorage[moduleIndex];
            if (moduleStorage == null)
            {
                moduleStorage = new object[typeTlsIndex + 1];
            }
            else if (typeTlsIndex >= moduleStorage.Length)
            {
                // typeTlsIndex could have a big range, we do not want to reallocate every time we see +1 index
                // so we double up from previous size to guarantee a worst case linear complexity
                int newSize = Math.Max(typeTlsIndex + 1, moduleStorage.Length * 2);
                Array.Resize(ref moduleStorage, newSize);
            }

            // Allocate an object that will represent a memory block for all thread static fields of the type
            object threadStaticBase = AllocateThreadStaticStorageForType(pModuleData->TypeManager, typeTlsIndex);

            Debug.Assert(moduleStorage[typeTlsIndex] == null);
            moduleStorage[typeTlsIndex] = threadStaticBase;
            return threadStaticBase;
        }

        /// <summary>
        /// This method allocates an object that represents a memory block for all thread static fields of the type
        /// that corresponds to the specified TLS index.
        /// </summary>
        private static unsafe object AllocateThreadStaticStorageForType(TypeManagerHandle typeManager, int typeTlsIndex)
        {
            int length;
            IntPtr* threadStaticRegion;

            // Get a pointer to the beginning of the module's Thread Static section. Then get a pointer
            // to the MethodTable that represents a memory map for thread statics storage.
            threadStaticRegion = (IntPtr*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.ThreadStaticRegion, out length);

            IntPtr gcDesc;
            if (typeTlsIndex < (length / IntPtr.Size))
            {
                gcDesc = threadStaticRegion[typeTlsIndex];
            }
            else
            {
                gcDesc = Internal.Runtime.Augments.RuntimeAugments.TypeLoaderCallbacks.GetThreadStaticGCDescForDynamicType(typeManager, typeTlsIndex);
            }

            return RuntimeImports.RhNewObject((MethodTable*)gcDesc);
        }
    }
}
