// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

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
        /// <summary>
        /// This method is called from a ReadyToRun helper to get base address of thread
        /// static storage for the given type.
        /// </summary>
        internal static unsafe object GetThreadStaticBaseForType(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            // Get the array that holds thread static memory blocks for each type in the given module
            object[] storage = RuntimeImports.RhGetThreadStaticStorageForModule(pModuleData->ModuleIndex);

            // Check whether thread static storage has already been allocated for this module and type.
            if ((storage != null) && ((uint)typeTlsIndex < (uint)storage.Length) && (storage[typeTlsIndex] != null))
            {
                return storage[typeTlsIndex];
            }

            return GetThreadStaticBaseForTypeSlow(pModuleData, typeTlsIndex);
        }

        [RuntimeExport("RhpGetThreadStaticBaseForTypeSlow")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe object GetThreadStaticBaseForTypeSlow(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            // Get the array that holds thread static memory blocks for each type in the given module
            object[] storage = RuntimeImports.RhGetThreadStaticStorageForModule(pModuleData->ModuleIndex);

            // This the first access to the thread statics of the type corresponding to typeTlsIndex.
            // Make sure there is enough storage allocated to hold it.
            storage = EnsureThreadStaticStorage(pModuleData->ModuleIndex, storage, requiredSize: typeTlsIndex + 1);

            // Allocate an object that will represent a memory block for all thread static fields of the type
            object threadStaticBase = AllocateThreadStaticStorageForType(pModuleData->TypeManager, typeTlsIndex);

            Debug.Assert(storage[typeTlsIndex] == null);

            storage[typeTlsIndex] = threadStaticBase;

            return threadStaticBase;
        }

        /// <summary>
        /// if it is required, this method extends thread static storage of the given module
        /// to the specified size and then registers the memory with the runtime.
        /// </summary>
        private static object[] EnsureThreadStaticStorage(int moduleIndex, object[] existingStorage, int requiredSize)
        {
            if ((existingStorage != null) && (requiredSize < existingStorage.Length))
            {
                return existingStorage;
            }

            object[] newStorage = new object[requiredSize];
            if (existingStorage != null)
            {
                Array.Copy(existingStorage, newStorage, existingStorage.Length);
            }

            // Install the newly created array as thread static storage for the given module
            // on the current thread. This call can fail due to a failure to allocate/extend required
            // internal thread specific resources.
            if (!RuntimeImports.RhSetThreadStaticStorageForModule(newStorage, moduleIndex))
            {
                throw new OutOfMemoryException();
            }

            return newStorage;
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

            return RuntimeImports.RhNewObject(new EETypePtr(gcDesc));
        }
    }
}
