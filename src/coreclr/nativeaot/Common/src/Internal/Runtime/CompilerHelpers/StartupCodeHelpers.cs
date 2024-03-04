// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Debug = Internal.Runtime.CompilerHelpers.StartupDebug;

namespace Internal.Runtime.CompilerHelpers
{
    internal static partial class StartupCodeHelpers
    {
        /// <summary>
        /// Table of logical modules. Only the first s_moduleCount elements of the array are in use.
        /// </summary>
        private static TypeManagerHandle[] s_modules;

        /// <summary>
        /// Number of valid elements in the logical module table.
        /// </summary>
        private static int s_moduleCount;

        /// <summary>
        /// GC handle of an array with s_moduleCount elements, each representing and array of GC static bases of the types in the module.
        /// </summary>
        private static IntPtr s_moduleGCStaticsSpines;

        [UnmanagedCallersOnly(EntryPoint = "InitializeModules", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static unsafe void InitializeModules(IntPtr osModule, IntPtr* pModuleHeaders, int count, IntPtr* pClasslibFunctions, int nClasslibFunctions)
        {
            RuntimeImports.RhpRegisterOsModule(osModule);
            TypeManagerHandle[] modules = CreateTypeManagers(osModule, pModuleHeaders, count, pClasslibFunctions, nClasslibFunctions);
            object[] gcStaticBaseSpines = new object[count];

            for (int i = 0; i < modules.Length; i++)
            {
                InitializeGlobalTablesForModule(modules[i], i, gcStaticBaseSpines);
            }

            s_moduleGCStaticsSpines = RuntimeImports.RhHandleAlloc(gcStaticBaseSpines, GCHandleType.Normal);

            // We are now at a stage where we can use GC statics - publish the list of modules
            // so that the eager constructors can access it.
            s_modules = modules;
            s_moduleCount = modules.Length;

            // These two loops look funny but it's important to initialize the global tables before running
            // the first class constructor to prevent them calling into another uninitialized module
            for (int i = 0; i < modules.Length; i++)
            {
                RunInitializers(modules[i], ReadyToRunSectionType.EagerCctor);
            }
        }

        /// <summary>
        /// Return the number of registered logical modules; optionally copy them into an array.
        /// </summary>
        /// <param name="outputModules">Array to copy logical modules to, null = only return logical module count</param>
        internal static int GetLoadedModules(TypeManagerHandle[] outputModules)
        {
            if (outputModules != null)
            {
                int copyLimit = (s_moduleCount < outputModules.Length ? s_moduleCount : outputModules.Length);
                for (int copyIndex = 0; copyIndex < copyLimit; copyIndex++)
                {
                    outputModules[copyIndex] = s_modules[copyIndex];
                }
            }
            return s_moduleCount;
        }

        private static unsafe TypeManagerHandle[] CreateTypeManagers(IntPtr osModule, IntPtr* pModuleHeaders, int count, IntPtr* pClasslibFunctions, int nClasslibFunctions)
        {
            // Count the number of modules so we can allocate an array to hold the TypeManager objects.
            // At this stage of startup, complex collection classes will not work.
            int moduleCount = 0;
            for (int i = 0; i < count; i++)
            {
                // The null pointers are sentinel values and padding inserted as side-effect of
                // the section merging. (The global static constructors section used by C++ has
                // them too.)
                if (pModuleHeaders[i] != IntPtr.Zero)
                    moduleCount++;
            }

            // We cannot use the new keyword just yet, so stackalloc the array first
            TypeManagerHandle* pHandles = stackalloc TypeManagerHandle[moduleCount];
            int moduleIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (pModuleHeaders[i] != IntPtr.Zero)
                {
                    TypeManagerHandle handle = RuntimeImports.RhpCreateTypeManager(osModule, pModuleHeaders[i], pClasslibFunctions, nClasslibFunctions);

                    // Rehydrate any dehydrated data structures
                    IntPtr dehydratedDataSection = RuntimeImports.RhGetModuleSection(
                        handle, ReadyToRunSectionType.DehydratedData, out int dehydratedDataLength);
                    if (dehydratedDataSection != IntPtr.Zero)
                    {
                        RehydrateData(dehydratedDataSection, dehydratedDataLength);
                    }

                    pHandles[moduleIndex++] = handle;
                }
            }

            // Any potentially dehydrated MethodTables got rehydrated, we can safely use `new` now.
            TypeManagerHandle[] modules = new TypeManagerHandle[moduleCount];
            for (int i = 0; i < moduleCount; i++)
                modules[i] = pHandles[i];

            return modules;
        }

        /// <summary>
        /// Each managed module linked into the final binary may have its own global tables for strings,
        /// statics, etc that need initializing. InitializeGlobalTables walks through the modules
        /// and offers each a chance to initialize its global tables.
        /// </summary>
        private static unsafe void InitializeGlobalTablesForModule(TypeManagerHandle typeManager, int moduleIndex, object[] gcStaticBaseSpines)
        {
            // Configure the module indirection cell with the newly created TypeManager. This allows EETypes to find
            // their interface dispatch map tables.
            int length;
            TypeManagerSlot* section = (TypeManagerSlot*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.TypeManagerIndirection, out length);
            section->TypeManager = typeManager;
            section->ModuleIndex = moduleIndex;

            // Initialize statics if any are present
            IntPtr staticsSection = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.GCStaticRegion, out length);
            if (staticsSection != IntPtr.Zero)
            {
                Debug.Assert(length % (MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint)) == 0);

                object[] spine = InitializeStatics(staticsSection, length);

                // Call write barrier directly. Assigning object reference does a type check.
                Debug.Assert((uint)moduleIndex < (uint)gcStaticBaseSpines.Length);
                ref object rawSpineIndexData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(gcStaticBaseSpines).Data);
                Unsafe.Add(ref rawSpineIndexData, moduleIndex) = spine;
            }

            // Initialize frozen object segment for the module with GC present
            IntPtr frozenObjectSection = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.FrozenObjectRegion, out length);
            if (frozenObjectSection != IntPtr.Zero)
            {
                Debug.Assert(length % IntPtr.Size == 0);
                InitializeModuleFrozenObjectSegment(frozenObjectSection, length);
            }
        }

        private static unsafe void InitializeModuleFrozenObjectSegment(IntPtr segmentStart, int length)
        {
            if (RuntimeImports.RhRegisterFrozenSegment((void*)segmentStart, (nuint)length, (nuint)length, (nuint)length) == IntPtr.Zero)
            {
                // This should only happen if we ran out of memory.
                RuntimeExceptionHelpers.FailFast("Failed to register frozen object segment for the module.");
            }
        }

        internal static void RunModuleInitializers()
        {
            for (int i = 0; i < s_moduleCount; i++)
            {
                RunInitializers(s_modules[i], ReadyToRunSectionType.ModuleInitializerList);
            }
        }

        private static unsafe void RunInitializers(TypeManagerHandle typeManager, ReadyToRunSectionType section)
        {
            var pInitializers = (byte*)RuntimeImports.RhGetModuleSection(typeManager, section, out int length);
            Debug.Assert(length % (MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint)) == 0);

            for (byte* pCurrent = pInitializers;
                pCurrent < (pInitializers + length);
                pCurrent += MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint))
            {
                var initializer = MethodTable.SupportsRelativePointers ? (delegate*<void>)ReadRelPtr32(pCurrent) : *(delegate*<void>*)pCurrent;
                initializer();
            }

            static void* ReadRelPtr32(void* address)
                => (byte*)address + *(int*)address;
        }

        private static unsafe object[] InitializeStatics(IntPtr gcStaticRegionStart, int length)
        {
            byte* gcStaticRegionEnd = (byte*)gcStaticRegionStart + length;

            object[] spine = new object[length / (MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint))];

            ref object rawSpineData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(spine).Data);

            int currentBase = 0;
            for (byte* block = (byte*)gcStaticRegionStart;
                block < gcStaticRegionEnd;
                block += MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint))
            {
                // Gc Static regions can be shared by modules linked together during compilation. To ensure each
                // is initialized once, the static region pointer is stored with lowest bit set in the image.
                // The first time we initialize the static region its pointer is replaced with an object reference
                // whose lowest bit is no longer set.
                IntPtr* pBlock = MethodTable.SupportsRelativePointers ? (IntPtr*)ReadRelPtr32(block) : *(IntPtr**)block;
                nint blockAddr = MethodTable.SupportsRelativePointers ? (nint)ReadRelPtr32(pBlock) : *pBlock;
                if ((blockAddr & GCStaticRegionConstants.Uninitialized) == GCStaticRegionConstants.Uninitialized)
                {
#pragma warning disable CS8500
                    object? obj = null;
                    RuntimeImports.RhAllocateNewObject(
                        new IntPtr(blockAddr & ~GCStaticRegionConstants.Mask),
                        (uint)GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP,
                        &obj);
                    if (obj == null)
                    {
                        RuntimeExceptionHelpers.FailFast("Failed allocating GC static bases");
                    }


                    if ((blockAddr & GCStaticRegionConstants.HasPreInitializedData) == GCStaticRegionConstants.HasPreInitializedData)
                    {
                        // The next pointer is preinitialized data blob that contains preinitialized static GC fields,
                        // which are pointer relocs to GC objects in frozen segment.
                        // It actually has all GC fields including non-preinitialized fields and we simply copy over the
                        // entire blob to this object, overwriting everything.
                        void* pPreInitDataAddr = MethodTable.SupportsRelativePointers ? ReadRelPtr32((int*)pBlock + 1) : (void*)*(pBlock + 1);
                        RuntimeImports.RhBulkMoveWithWriteBarrier(ref obj.GetRawData(), ref *(byte*)pPreInitDataAddr, obj.GetRawObjectDataSize());
                    }

                    // Call write barrier directly. Assigning object reference does a type check.
                    Debug.Assert(currentBase < spine.Length);
                    Unsafe.Add(ref rawSpineData, currentBase) = obj;

                    // Update the base pointer to point to the pinned object
                    *pBlock = *(IntPtr*)&obj;
#pragma warning restore CS8500
                }

                currentBase++;
            }

            return spine;

            static void* ReadRelPtr32(void* address)
                => (byte*)address + *(int*)address;
        }

        private static unsafe void RehydrateData(IntPtr dehydratedData, int length)
        {
            // Destination for the hydrated data is in the first 32-bit relative pointer
            byte* pDest = (byte*)ReadRelPtr32((void*)dehydratedData);

            // The dehydrated data follows
            byte* pCurrent = (byte*)dehydratedData + sizeof(int);
            byte* pEnd = (byte*)dehydratedData + length;

            // Fixup table immediately follows the command stream
            int* pFixups = (int*)pEnd;

            while (pCurrent < pEnd)
            {
                pCurrent = DehydratedDataCommand.Decode(pCurrent, out int command, out int payload);
                switch (command)
                {
                    case DehydratedDataCommand.Copy:
                        Debug.Assert(payload != 0);
                        if (payload < 4)
                        {
                            *pDest = *pCurrent;
                            if (payload > 1)
                                *(short*)(pDest + payload - 2) = *(short*)(pCurrent + payload - 2);
                        }
                        else if (payload < 8)
                        {
                            *(int*)pDest = *(int*)pCurrent;
                            *(int*)(pDest + payload - 4) = *(int*)(pCurrent + payload - 4);
                        }
                        else if (payload <= 16)
                        {
#if TARGET_64BIT
                            *(long*)pDest = *(long*)pCurrent;
                            *(long*)(pDest + payload - 8) = *(long*)(pCurrent + payload - 8);
#else
                            *(int*)pDest = *(int*)pCurrent;
                            *(int*)(pDest + 4) = *(int*)(pCurrent + 4);
                            *(int*)(pDest + payload - 8) = *(int*)(pCurrent + payload - 8);
                            *(int*)(pDest + payload - 4) = *(int*)(pCurrent + payload - 4);
#endif
                        }
                        else
                        {
                            // At the time of writing this, 90% of DehydratedDataCommand.Copy cases
                            // would fall into the above specialized cases. 10% fall back to memmove.
                            Unsafe.CopyBlock(pDest, pCurrent, (uint)payload);
                        }

                        pDest += payload;
                        pCurrent += payload;
                        break;
                    case DehydratedDataCommand.ZeroFill:
                        pDest += payload;
                        break;
                    case DehydratedDataCommand.PtrReloc:
                        *(void**)pDest = ReadRelPtr32(pFixups + payload);
                        pDest += sizeof(void*);
                        break;
                    case DehydratedDataCommand.RelPtr32Reloc:
                        WriteRelPtr32(pDest, ReadRelPtr32(pFixups + payload));
                        pDest += sizeof(int);
                        break;
                    case DehydratedDataCommand.InlinePtrReloc:
                        while (payload-- > 0)
                        {
                            *(void**)pDest = ReadRelPtr32(pCurrent);
                            pDest += sizeof(void*);
                            pCurrent += sizeof(int);
                        }
                        break;
                    case DehydratedDataCommand.InlineRelPtr32Reloc:
                        while (payload-- > 0)
                        {
                            WriteRelPtr32(pDest, ReadRelPtr32(pCurrent));
                            pDest += sizeof(int);
                            pCurrent += sizeof(int);
                        }
                        break;
                }
            }

            static void* ReadRelPtr32(void* address)
                => (byte*)address + *(int*)address;

            static void WriteRelPtr32(void* dest, void* value)
                => *(int*)dest = (int)((byte*)value - (byte*)dest);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TypeManagerSlot
    {
        public TypeManagerHandle TypeManager;
        public int ModuleIndex;
    }
}
