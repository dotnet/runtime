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
        private static int s_inlineTlsLength;
        private static unsafe int GetInlineTlsLength(TypeManagerHandle typeManager)
        {
            int length = s_inlineTlsLength;
            if (length == 0)
            {
                RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.ThreadStaticRegion, out int sectionLength);
                s_inlineTlsLength = length = sectionLength / IntPtr.Size;
            }

            return length;
        }

        private static unsafe bool IsInlineThreadStatic(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            if (pModuleData->ModuleIndex != 0)
                return false;

            return typeTlsIndex < GetInlineTlsLength(pModuleData->TypeManager);
        }

        /// <summary>
        /// This method is called from a ReadyToRun helper to get base address of thread
        /// static storage for the given type.
        /// </summary>
        internal static unsafe object GetThreadStaticBaseForType(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            if (!IsInlineThreadStatic(pModuleData, typeTlsIndex))
                return GetUninlinedThreadStaticBaseForType(pModuleData, typeTlsIndex);

            ref object[] threadStorage = ref RuntimeImports.RhGetInlineThreadStaticStorage();
            if (threadStorage == null)
                return GetInlinedThreadStaticBaseSlow(pModuleData, typeTlsIndex);

            return threadStorage[typeTlsIndex];
        }

        [RuntimeExport("RhpGetInlinedThreadStaticBaseSlow")]
        internal static unsafe object GetInlinedThreadStaticBaseSlow(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            Debug.Assert(pModuleData->ModuleIndex == 0);

            // Get the array that holds thread statics for the current thread, if none present
            // allocate a new one big enough to hold the current module data
            ref object[] threadStorage = ref RuntimeImports.RhGetInlineThreadStaticStorage();
            Debug.Assert(threadStorage == null);

            TypeManagerHandle typeManager = pModuleData->TypeManager;
            int inlineThreaStaticsLen = GetInlineTlsLength(typeManager);
            object[] newStorage = new object[inlineThreaStaticsLen];

            for(int i = 0; i < inlineThreaStaticsLen; i++)
            {
                // Allocate an object that will represent a memory block for all thread static fields of the type
                object threadStaticBase = AllocateThreadStaticStorageForType(typeManager, i);
                newStorage[i] = threadStaticBase;
            }

            threadStorage = newStorage;
            return newStorage[typeTlsIndex];
        }

        internal static unsafe object GetUninlinedThreadStaticBaseForType(TypeManagerSlot* pModuleData, int typeTlsIndex)
        {
            Debug.Assert(typeTlsIndex >= 0);
            int moduleIndex = pModuleData->ModuleIndex;
            Debug.Assert(moduleIndex >= 0);

            object[][] threadStorage = RuntimeImports.RhGetThreadStaticStorage();
            if (threadStorage != null && threadStorage.Length > moduleIndex)
            {
                object[] moduleStorage = threadStorage[moduleIndex];
                if (moduleStorage != null)
                {
                    int storeIndex = typeTlsIndex;
                    // threadstatics up to InlineTlsLength for module0 are stored separately.
                    // we can reduce the storage index by that much
                    if (moduleIndex == 0)
                        storeIndex -= GetInlineTlsLength(pModuleData->TypeManager);

                    if (moduleStorage.Length > storeIndex)
                    {
                        object threadStaticBase = moduleStorage[storeIndex];
                        if (threadStaticBase != null)
                        {
                            return threadStaticBase;
                        }
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

            int storeIndex = typeTlsIndex;
            // threadstatics up to InlineTlsLength for module0 are stored separately.
            // we can reduce the storage index by that much
            if (moduleIndex == 0)
                storeIndex -= GetInlineTlsLength(pModuleData->TypeManager);

            // Get the array that holds thread static memory blocks for each type in the given module
            ref object[] moduleStorage = ref threadStorage[moduleIndex];
            if (moduleStorage == null)
            {
                moduleStorage = new object[storeIndex + 1];
            }
            else if (storeIndex >= moduleStorage.Length)
            {
                // storeIndex could have a big range, we do not want to reallocate every time we see +1 index
                // so we double up from previous size to guarantee a worst case linear complexity
                int newSize = Math.Max(storeIndex + 1, moduleStorage.Length * 2);
                Array.Resize(ref moduleStorage, newSize);
            }

            // Allocate an object that will represent a memory block for all thread static fields of the type
            object threadStaticBase = AllocateThreadStaticStorageForType(pModuleData->TypeManager, typeTlsIndex);

            Debug.Assert(moduleStorage[storeIndex] == null);
            moduleStorage[storeIndex] = threadStaticBase;
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

            return RuntimeImports.RhNewObject(new EETypePtr(gcDesc));
        }
    }
}
