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
            public IntPtr[] Keys = Array.Empty<IntPtr>();
            public IntPtr[] Values = Array.Empty<IntPtr>();
            public int SimpleCount;
        }

        private static Map? s_map;

        public static IntPtr GetTarget(IntPtr stub, bool includeInstantiatingStubs, out bool isInstantiatingStub)
        {
            Map map = GetMap();
            isInstantiatingStub = false;
            int index = Array.BinarySearch(map.Keys, 0, map.SimpleCount, stub);
            if (index < 0 && includeInstantiatingStubs)
            {
                index = Array.BinarySearch(map.Keys, map.SimpleCount, map.Keys.Length - map.SimpleCount, stub);
                isInstantiatingStub = index >= 0;
            }

            return index >= 0 ? map.Values[index] : stub;
        }

        private static Map GetMap()
        {
            return Volatile.Read(ref s_map) ?? BuildMap();
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

            var map = new Map { SimpleCount = simpleCount };
            if (simpleCount + instantiatingCount > 0)
            {
                map.Keys = new IntPtr[simpleCount + instantiatingCount];
                map.Values = new IntPtr[simpleCount + instantiatingCount];

                int simpleIndex = 0;
                int instantiatingIndex = simpleCount;
                foreach (TypeManagerHandle module in modules)
                {
                    if (!TryGetBlob(module, out uint* pBlob))
                        continue;

                    uint moduleSimpleCount = pBlob[0];
                    uint moduleInstantiatingCount = pBlob[1];
                    int* pPairs = (int*)(pBlob + 2);

                    for (uint i = 0; i < moduleSimpleCount; i++, pPairs += 2)
                    {
                        map.Keys[simpleIndex] = (IntPtr)pPairs[0];
                        map.Values[simpleIndex] = (IntPtr)pPairs[1];
                        simpleIndex++;
                    }

                    for (uint i = 0; i < moduleInstantiatingCount; i++, pPairs += 2)
                    {
                        map.Keys[instantiatingIndex] = (IntPtr)pPairs[0];
                        map.Values[instantiatingIndex] = (IntPtr)pPairs[1];
                        instantiatingIndex++;
                    }
                }

                Array.Sort(map.Keys, map.Values, 0, simpleCount);
                Array.Sort(map.Keys, map.Values, simpleCount, instantiatingCount);
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
