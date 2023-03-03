// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    /// <summary>
    /// Utility class for encoding GCDescs. GCDesc is a runtime structure used by the
    /// garbage collector that describes the GC layout of a memory region.
    /// </summary>
    public struct GCDescEncoder
    {
        /// <summary>
        /// Retrieves size of the GCDesc that describes the instance GC layout for the given type.
        /// </summary>
        public static int GetGCDescSize(TypeDesc type)
        {
            if (type.IsArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (elementType.IsGCPointer)
                {
                    // For efficiency this is special cased and encoded as one serie.
                    return 3 * type.Context.Target.PointerSize;
                }
                else if (elementType.IsDefType)
                {
                    var defType = (DefType)elementType;
                    if (defType.ContainsGCPointers)
                    {
                        GCPointerMap pointerMap = GCPointerMap.FromInstanceLayout(defType);
                        if (pointerMap.IsAllGCPointers)
                        {
                            // For efficiency this is special cased and encoded as one serie.
                            return 3 * type.Context.Target.PointerSize;
                        }
                        else
                        {
                            int numSeries = pointerMap.NumSeries;
                            Debug.Assert(numSeries > 0);
                            return (numSeries + 2) * type.Context.Target.PointerSize;
                        }
                    }
                }
            }
            else
            {
                var defType = (DefType)type;
                if (defType.ContainsGCPointers)
                {
                    int numSeries = GCPointerMap.FromInstanceLayout(defType).NumSeries;
                    Debug.Assert(numSeries > 0);
                    return (numSeries * 2 + 1) * type.Context.Target.PointerSize;
                }
            }

            return 0;
        }

        public static void EncodeGCDesc<T>(ref T builder, TypeDesc type)
            where T : struct, ITargetBinaryWriter
        {
            int initialBuilderPosition = builder.CountBytes;

            if (type.IsArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;

                // 2 means m_pEEType and _numComponents. Syncblock is sort of appended at the end of the object layout in this case.
                int baseSize = 2 * builder.TargetPointerSize;

                if (type.IsMdArray)
                {
                    // Multi-dim arrays include upper and lower bounds for each rank
                    baseSize += 2 * sizeof(int) * ((ArrayType)type).Rank;
                }

                if (elementType.IsGCPointer)
                {
                    EncodeAllGCPointersArrayGCDesc(ref builder, baseSize);
                }
                else if (elementType.IsDefType)
                {
                    var elementDefType = (DefType)elementType;
                    if (elementDefType.ContainsGCPointers)
                    {
                        GCPointerMap pointerMap = GCPointerMap.FromInstanceLayout(elementDefType);
                        if (pointerMap.IsAllGCPointers)
                        {
                            EncodeAllGCPointersArrayGCDesc(ref builder, baseSize);
                        }
                        else
                        {
                            EncodeArrayGCDesc(ref builder, pointerMap, baseSize);
                        }
                    }
                }
            }
            else
            {
                var defType = (DefType)type;
                if (defType.ContainsGCPointers)
                {
                    // Computing the layout for the boxed version if this is a value type.
                    int offs = defType.IsValueType ? builder.TargetPointerSize : 0;

                    // Include syncblock
                    int objectSize = defType.InstanceByteCount.AsInt + offs + builder.TargetPointerSize;

                    EncodeStandardGCDesc(ref builder, GCPointerMap.FromInstanceLayout(defType), objectSize, offs);
                }
            }

            Debug.Assert(initialBuilderPosition + GetGCDescSize(type) == builder.CountBytes);
        }

        public static void EncodeStandardGCDesc<T>(ref T builder, GCPointerMap map, int size, int delta)
            where T : struct, ITargetBinaryWriter
        {
            Debug.Assert(size >= map.Size);

            int pointerSize = builder.TargetPointerSize;

            int numSeries = 0;

            for (int cellIndex = map.Size - 1; cellIndex >= 0; cellIndex--)
            {
                if (map[cellIndex])
                {
                    numSeries++;

                    int seriesSize = pointerSize;

                    while (cellIndex > 0 && map[cellIndex - 1])
                    {
                        seriesSize += pointerSize;
                        cellIndex--;
                    }

                    builder.EmitNaturalInt(seriesSize - size);
                    builder.EmitNaturalInt(cellIndex * pointerSize + delta);
                }
            }

            Debug.Assert(numSeries > 0);
            builder.EmitNaturalInt(numSeries);
        }

        // Arrays of all GC references are encoded as special kind of GC desc for efficiency
        private static void EncodeAllGCPointersArrayGCDesc<T>(ref T builder, int baseSize)
            where T : struct, ITargetBinaryWriter
        {
            // Construct the gc info as if this array contains exactly one pointer
            // - the encoding trick where the size of the series is measured as a difference from
            // total object size will make this work for arbitrary array lengths

            // Series size
            builder.EmitNaturalInt(-(baseSize + builder.TargetPointerSize));
            // Series offset
            builder.EmitNaturalInt(baseSize);
            // NumSeries
            builder.EmitNaturalInt(1);
        }

        private static void EncodeArrayGCDesc<T>(ref T builder, GCPointerMap map, int baseSize)
            where T : struct, ITargetBinaryWriter
        {
            // NOTE: This format cannot properly represent element types with sizes >= 64k bytes.
            //       We guard it with an assert, but it is the responsibility of the code using this
            //       to cap array component sizes. MethodTable only has a UInt16 field for component sizes too.

            int numSeries = 0;
            int leadingNonPointerCount = 0;

            int pointerSize = builder.TargetPointerSize;

            for (int cellIndex = 0; cellIndex < map.Size && !map[cellIndex]; cellIndex++)
            {
                leadingNonPointerCount++;
            }

            int nonPointerCount = leadingNonPointerCount;

            for (int cellIndex = map.Size - 1; cellIndex >= leadingNonPointerCount; cellIndex--)
            {
                if (map[cellIndex])
                {
                    numSeries++;

                    int pointerCount = 1;
                    while (cellIndex > leadingNonPointerCount && map[cellIndex - 1])
                    {
                        cellIndex--;
                        pointerCount++;
                    }

                    Debug.Assert(pointerCount < 64 * 1024);
                    builder.EmitHalfNaturalInt((short)pointerCount);

                    Debug.Assert(nonPointerCount * pointerSize < 64 * 1024);
                    builder.EmitHalfNaturalInt((short)(nonPointerCount * pointerSize));

                    nonPointerCount = 0;
                }
                else
                {
                    nonPointerCount++;
                }
            }

            Debug.Assert(numSeries > 0);
            builder.EmitNaturalInt(baseSize + leadingNonPointerCount * pointerSize);
            builder.EmitNaturalInt(-numSeries);
        }
    }
}
