// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Threading;

using Internal.Runtime.Augments;

namespace Internal.Runtime
{
    /// <summary>
    /// Maps unboxing stubs to their targets on platforms where we can't decode the stub instructions (WASM).
    /// The data comes from the <see cref="ReflectionMapBlob.UnboxingStubMap"/> blob emitted by the compiler.
    /// </summary>
    internal static unsafe class UnboxingStubMap
    {
        private sealed class Map
        {
            public IntPtr[] SimpleKeys = Array.Empty<IntPtr>();
            public IntPtr[] SimpleValues = Array.Empty<IntPtr>();
            public IntPtr[] InstantiatingKeys = Array.Empty<IntPtr>();
            public IntPtr[] InstantiatingValues = Array.Empty<IntPtr>();
        }

        private static Map? s_map;

        public static bool TryGetSimpleTarget(IntPtr stub, out IntPtr target)
        {
            Map map = GetMap();
            return TryLookup(map.SimpleKeys, map.SimpleValues, stub, out target);
        }

        public static bool TryGetInstantiatingTarget(IntPtr stub, out IntPtr target)
        {
            Map map = GetMap();
            return TryLookup(map.InstantiatingKeys, map.InstantiatingValues, stub, out target);
        }

        private static bool TryLookup(IntPtr[] keys, IntPtr[] values, IntPtr stub, out IntPtr target)
        {
            int index = Array.BinarySearch(keys, stub);
            if (index >= 0)
            {
                target = values[index];
                return true;
            }

            target = IntPtr.Zero;
            return false;
        }

        private static Map GetMap()
        {
            return s_map ?? BuildMap();
        }

        private static Map BuildMap()
        {
            int simpleCount = 0;
            int instantiatingCount = 0;

            ReadOnlySpan<TypeManagerHandle> modules = RuntimeAugments.GetLoadedModules();
            foreach (TypeManagerHandle module in modules)
            {
                if (TryGetBlob(module, out uint* pBlob))
                {
                    simpleCount += (int)pBlob[0];
                    instantiatingCount += (int)pBlob[1];
                }
            }

            var map = new Map();
            if (simpleCount + instantiatingCount > 0)
            {
                map.SimpleKeys = new IntPtr[simpleCount];
                map.SimpleValues = new IntPtr[simpleCount];
                map.InstantiatingKeys = new IntPtr[instantiatingCount];
                map.InstantiatingValues = new IntPtr[instantiatingCount];

                int simpleIndex = 0;
                int instantiatingIndex = 0;
                foreach (TypeManagerHandle module in modules)
                {
                    if (!TryGetBlob(module, out uint* pBlob))
                        continue;

                    uint moduleSimpleCount = pBlob[0];
                    uint moduleInstantiatingCount = pBlob[1];
                    int* pPairs = (int*)(pBlob + 2);

                    for (uint i = 0; i < moduleSimpleCount; i++, pPairs += 2)
                    {
                        map.SimpleKeys[simpleIndex] = (IntPtr)pPairs[0];
                        map.SimpleValues[simpleIndex] = (IntPtr)pPairs[1];
                        simpleIndex++;
                    }

                    for (uint i = 0; i < moduleInstantiatingCount; i++, pPairs += 2)
                    {
                        map.InstantiatingKeys[instantiatingIndex] = (IntPtr)pPairs[0];
                        map.InstantiatingValues[instantiatingIndex] = (IntPtr)pPairs[1];
                        instantiatingIndex++;
                    }
                }

                Array.Sort(map.SimpleKeys, map.SimpleValues);
                Array.Sort(map.InstantiatingKeys, map.InstantiatingValues);
            }

            return Interlocked.CompareExchange(ref s_map, map, null) ?? map;
        }

        private static bool TryGetBlob(TypeManagerHandle module, out uint* pBlob)
        {
            byte* pbBlob;
            uint cbBlob;
            if (RuntimeImports.RhFindBlob(module, (uint)ReflectionMapBlob.UnboxingStubMap, &pbBlob, &cbBlob)
                && cbBlob >= 2 * sizeof(uint))
            {
                pBlob = (uint*)pbBlob;
                return true;
            }

            pBlob = null;
            return false;
        }
    }
}
