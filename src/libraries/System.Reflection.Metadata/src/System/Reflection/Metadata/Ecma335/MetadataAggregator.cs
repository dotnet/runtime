// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed class MetadataAggregator
    {
        // For each heap handle and each delta contains aggregate heap lengths.
        // heapSizes[heap kind][reader index] == Sum { 0..index | reader[i].XxxHeap.Block.Length }
        private readonly ImmutableArray<ImmutableArray<int>> _heapSizes;

        private readonly ImmutableArray<ImmutableArray<RowCounts>> _rowCounts;

        // internal for testing
        internal struct RowCounts : IComparable<RowCounts>
        {
            public int AggregateInserts;
            public int Updates;

            public int CompareTo(RowCounts other)
            {
                return AggregateInserts - other.AggregateInserts;
            }

            public override string ToString()
            {
                return $"+0x{AggregateInserts:x} ~0x{Updates:x}";
            }
        }

        public MetadataAggregator(MetadataReader baseReader, IReadOnlyList<MetadataReader> deltaReaders)
            : this(baseReader, null, null, deltaReaders)
        {
        }

        public MetadataAggregator(
            IReadOnlyList<int>? baseTableRowCounts,
            IReadOnlyList<int>? baseHeapSizes,
            IReadOnlyList<MetadataReader>? deltaReaders)
            : this(null, baseTableRowCounts, baseHeapSizes, deltaReaders)
        {
        }

        private MetadataAggregator(
            MetadataReader? baseReader,
            IReadOnlyList<int>? baseTableRowCounts,
            IReadOnlyList<int>? baseHeapSizes,
            IReadOnlyList<MetadataReader>? deltaReaders)
        {
            if (baseTableRowCounts == null)
            {
                if (baseReader == null)
                {
                    Throw.ArgumentNull(nameof(baseReader));
                }

                if (baseReader.GetTableRowCount(TableIndex.EncMap) != 0)
                {
                    throw new ArgumentException(SR.BaseReaderMustBeFullMetadataReader, nameof(baseReader));
                }

                CalculateBaseCounts(baseReader, out baseTableRowCounts, out baseHeapSizes);
                Debug.Assert(baseTableRowCounts != null);
            }
            else
            {
                if (baseTableRowCounts.Count != MetadataTokens.TableCount)
                {
                    throw new ArgumentException(SR.Format(SR.ExpectedListOfSize, MetadataTokens.TableCount), nameof(baseTableRowCounts));
                }

                if (baseHeapSizes == null)
                {
                    Throw.ArgumentNull(nameof(baseHeapSizes));
                }

                if (baseHeapSizes.Count != MetadataTokens.HeapCount)
                {
                    throw new ArgumentException(SR.Format(SR.ExpectedListOfSize, MetadataTokens.HeapCount), nameof(baseTableRowCounts));
                }
            }

            if (deltaReaders == null || deltaReaders.Count == 0)
            {
                throw new ArgumentException(SR.ExpectedNonEmptyList, nameof(deltaReaders));
            }

            for (int i = 0; i < deltaReaders.Count; i++)
            {
                if (deltaReaders[i].GetTableRowCount(TableIndex.EncMap) == 0 || !deltaReaders[i].IsMinimalDelta)
                {
                    throw new ArgumentException(SR.ReadersMustBeDeltaReaders, nameof(deltaReaders));
                }
            }

            _heapSizes = CalculateHeapSizes(baseHeapSizes, deltaReaders);
            _rowCounts = CalculateRowCounts(baseTableRowCounts, deltaReaders);
        }

        // for testing only
        internal MetadataAggregator(RowCounts[][] rowCounts, int[][] heapSizes)
        {
            _rowCounts = ToImmutable(rowCounts);
            _heapSizes = ToImmutable(heapSizes);
        }

        private static void CalculateBaseCounts(
            MetadataReader baseReader,
            out IReadOnlyList<int> baseTableRowCounts,
            out IReadOnlyList<int> baseHeapSizes)
        {
            int[] rowCounts = new int[MetadataTokens.TableCount];
            int[] heapSizes = new int[MetadataTokens.HeapCount];

            for (int i = 0; i < rowCounts.Length; i++)
            {
                rowCounts[i] = baseReader.GetTableRowCount((TableIndex)i);
            }

            for (int i = 0; i < heapSizes.Length; i++)
            {
                heapSizes[i] = baseReader.GetHeapSize((HeapIndex)i);
            }

            baseTableRowCounts = rowCounts;
            baseHeapSizes = heapSizes;
        }

        private static ImmutableArray<ImmutableArray<int>> CalculateHeapSizes(
            IReadOnlyList<int> baseSizes,
            IReadOnlyList<MetadataReader> deltaReaders)
        {
            // GUID heap index is multiple of sizeof(Guid) == 16
            const int guidSize = 16;
            int generationCount = 1 + deltaReaders.Count;

            var userStringSizes = new int[generationCount];
            var stringSizes = new int[generationCount];
            var blobSizes = new int[generationCount];
            var guidSizes = new int[generationCount];

            userStringSizes[0] = baseSizes[(int)HeapIndex.UserString];
            stringSizes[0] = baseSizes[(int)HeapIndex.String];
            blobSizes[0] = baseSizes[(int)HeapIndex.Blob];
            guidSizes[0] = baseSizes[(int)HeapIndex.Guid] / guidSize;

            for (int r = 0; r < deltaReaders.Count; r++)
            {
                userStringSizes[r + 1] = userStringSizes[r] + deltaReaders[r].GetHeapSize(HeapIndex.UserString);
                stringSizes[r + 1] = stringSizes[r] + deltaReaders[r].GetHeapSize(HeapIndex.String);
                blobSizes[r + 1] = blobSizes[r] + deltaReaders[r].GetHeapSize(HeapIndex.Blob);
                guidSizes[r + 1] = guidSizes[r] + deltaReaders[r].GetHeapSize(HeapIndex.Guid) / guidSize;
            }

            return ImmutableArray.Create(
                userStringSizes.ToImmutableArray(),
                stringSizes.ToImmutableArray(),
                blobSizes.ToImmutableArray(),
                guidSizes.ToImmutableArray());
        }

        private static ImmutableArray<ImmutableArray<RowCounts>> CalculateRowCounts(
            IReadOnlyList<int> baseRowCounts,
            IReadOnlyList<MetadataReader> deltaReaders)
        {
            // TODO: optimize - we don't need to allocate all these arrays
            var rowCounts = GetBaseRowCounts(baseRowCounts, generations: 1 + deltaReaders.Count);

            for (int generation = 1; generation <= deltaReaders.Count; generation++)
            {
                CalculateDeltaRowCountsForGeneration(rowCounts, generation, ref deltaReaders[generation - 1].EncMapTable);
            }

            return ToImmutable(rowCounts);
        }

        private static ImmutableArray<ImmutableArray<T>> ToImmutable<T>(T[][] array)
        {
            var immutable = new ImmutableArray<T>[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                immutable[i] = array[i].ToImmutableArray();
            }

            return immutable.ToImmutableArray();
        }

        // internal for testing
        internal static RowCounts[][] GetBaseRowCounts(IReadOnlyList<int> baseRowCounts, int generations)
        {
            var rowCounts = new RowCounts[MetadataTokens.TableCount][];

            for (int t = 0; t < rowCounts.Length; t++)
            {
                rowCounts[t] = new RowCounts[generations];
                rowCounts[t][0].AggregateInserts = baseRowCounts[t];
            }

            return rowCounts;
        }

        // internal for testing
        internal static void CalculateDeltaRowCountsForGeneration(RowCounts[][] rowCounts, int generation, ref EnCMapTableReader encMapTable)
        {
            foreach (var tableRowCounts in rowCounts)
            {
                tableRowCounts[generation].AggregateInserts = tableRowCounts[generation - 1].AggregateInserts;
            }

            int mapRowCount = encMapTable.NumberOfRows;
            for (int mapRid = 1; mapRid <= mapRowCount; mapRid++)
            {
                uint token = encMapTable.GetToken(mapRid);
                int rid = (int)(token & TokenTypeIds.RIDMask);

                var tableRowCounts = rowCounts[token >> TokenTypeIds.RowIdBitCount];

                if (rid > tableRowCounts[generation].AggregateInserts)
                {
                    if (rid != tableRowCounts[generation].AggregateInserts + 1)
                    {
                        throw new BadImageFormatException(SR.EnCMapNotSorted);
                    }

                    // insert:
                    tableRowCounts[generation].AggregateInserts = rid;
                }
                else
                {
                    // update:
                    tableRowCounts[generation].Updates++;
                }
            }
        }

        /// <summary>
        /// Given a handle of an entity in an aggregate metadata calculates
        /// a handle of the entity within the metadata generation it is defined in.
        /// </summary>
        /// <param name="handle">Handle of an entity in an aggregate metadata.</param>
        /// <param name="generation">The generation the entity is defined in.</param>
        /// <returns>Handle of the entity within the metadata generation <paramref name="generation"/>.</returns>
        public Handle GetGenerationHandle(Handle handle, out int generation)
        {
            if (handle.IsVirtual)
            {
                // TODO: if a virtual handle is connected to real handle then translate the rid,
                // otherwise return vhandle and base.
                throw new NotSupportedException();
            }

            if (handle.IsHeapHandle)
            {
                int heapOffset = handle.Offset;

                HeapIndex heapIndex;
                MetadataTokens.TryGetHeapIndex(handle.Kind, out heapIndex);

                var sizes = _heapSizes[(int)heapIndex];

                // #Guid heap offset is 1-based, other heaps have 0-based offset:
                var size = (handle.Type == HandleType.Guid) ? heapOffset - 1 : heapOffset;

                generation = sizes.BinarySearch(size);
                if (generation >= 0)
                {
                    Debug.Assert(sizes[generation] == size);

                    // the index points to the start of the next generation that added data to the heap:
                    do
                    {
                        generation++;
                    }
                    while (generation < sizes.Length && sizes[generation] == size);
                }
                else
                {
                    generation = ~generation;
                }

                if (generation >= sizes.Length)
                {
                    throw new ArgumentException(SR.HandleBelongsToFutureGeneration, nameof(handle));
                }

                // GUID heap accumulates - previous heap is copied to the next generation
                int relativeHeapOffset = (handle.Type == HandleType.Guid || generation == 0) ? heapOffset : heapOffset - sizes[generation - 1];

                return new Handle((byte)handle.Type, relativeHeapOffset);
            }
            else
            {
                int rowId = handle.RowId;

                var sizes = _rowCounts[(int)handle.Type];

                generation = sizes.BinarySearch(new RowCounts { AggregateInserts = rowId });
                if (generation >= 0)
                {
                    Debug.Assert(sizes[generation].AggregateInserts == rowId);

                    // the row is in a generation that inserted exactly one row -- the one that we are looking for;
                    // or it's in a preceding generation if the current one didn't insert any rows of the kind:
                    while (generation > 0 && sizes[generation - 1].AggregateInserts == rowId)
                    {
                        generation--;
                    }
                }
                else
                {
                    // the row is in a generation that inserted multiple new rows:
                    generation = ~generation;

                    if (generation >= sizes.Length)
                    {
                        throw new ArgumentException(SR.HandleBelongsToFutureGeneration, nameof(handle));
                    }
                }

                // In each delta table updates always precede inserts.
                int relativeRowId = (generation == 0) ? rowId :
                    rowId -
                    sizes[generation - 1].AggregateInserts +
                    sizes[generation].Updates;

                return new Handle((byte)handle.Type, relativeRowId);
            }
        }
    }
}
