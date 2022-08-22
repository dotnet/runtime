// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Debug = Internal.Runtime.CompilerHelpers.StartupDebug;

namespace Internal.Runtime.CompilerHelpers
{
    public static partial class StartupCodeHelpers
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

            TypeManagerHandle[] modules = new TypeManagerHandle[moduleCount];
            int moduleIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (pModuleHeaders[i] != IntPtr.Zero)
                {
                    modules[moduleIndex] = RuntimeImports.RhpCreateTypeManager(osModule, pModuleHeaders[i], pClasslibFunctions, nClasslibFunctions);
                    moduleIndex++;
                }
            }

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
                Debug.Assert(length % IntPtr.Size == 0);

                object[] spine = InitializeStatics(staticsSection, length);

                // Call write barrier directly. Assigning object reference does a type check.
                Debug.Assert((uint)moduleIndex < (uint)gcStaticBaseSpines.Length);
                ref object rawSpineIndexData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(gcStaticBaseSpines).Data);
                InternalCalls.RhpAssignRef(ref Unsafe.Add(ref rawSpineIndexData, moduleIndex), spine);
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
            if (RuntimeImports.RhpRegisterFrozenSegment(segmentStart, (IntPtr)length) == IntPtr.Zero)
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
            var initializers = (delegate*<void>*)RuntimeImports.RhGetModuleSection(typeManager, section, out int length);
            Debug.Assert(length % IntPtr.Size == 0);
            int count = length / IntPtr.Size;
            for (int i = 0; i < count; i++)
            {
                initializers[i]();
            }
        }

        private static unsafe object[] InitializeStatics(IntPtr gcStaticRegionStart, int length)
        {
            IntPtr gcStaticRegionEnd = (IntPtr)((byte*)gcStaticRegionStart + length);

            object[] spine = new object[length / IntPtr.Size];

            ref object rawSpineData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(spine).Data);

            int currentBase = 0;
            for (IntPtr* block = (IntPtr*)gcStaticRegionStart; block < (IntPtr*)gcStaticRegionEnd; block++)
            {
                // Gc Static regions can be shared by modules linked together during compilation. To ensure each
                // is initialized once, the static region pointer is stored with lowest bit set in the image.
                // The first time we initialize the static region its pointer is replaced with an object reference
                // whose lowest bit is no longer set.
                IntPtr* pBlock = (IntPtr*)*block;
                nint blockAddr = *pBlock;
                if ((blockAddr & GCStaticRegionConstants.Uninitialized) == GCStaticRegionConstants.Uninitialized)
                {
                    object? obj = null;
                    RuntimeImports.RhAllocateNewObject(
                        new IntPtr(blockAddr & ~GCStaticRegionConstants.Mask),
                        (uint)GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP,
                        Unsafe.AsPointer(ref obj));
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
                        IntPtr pPreInitDataAddr = *(pBlock + 1);
                        RuntimeImports.RhBulkMoveWithWriteBarrier(ref obj.GetRawData(), ref *(byte *)pPreInitDataAddr, obj.GetRawObjectDataSize());
                    }

                    // Call write barrier directly. Assigning object reference does a type check.
                    Debug.Assert(currentBase < spine.Length);
                    InternalCalls.RhpAssignRef(ref Unsafe.Add(ref rawSpineData, currentBase), obj);

                    // Update the base pointer to point to the pinned object
                    *pBlock = *(IntPtr*)Unsafe.AsPointer(ref obj);
                }

                currentBase++;
            }

            return spine;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TypeManagerSlot
    {
        public TypeManagerHandle TypeManager;
        public int ModuleIndex;
    }
}
