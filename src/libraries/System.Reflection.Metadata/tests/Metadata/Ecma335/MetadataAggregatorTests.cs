// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Internal;
using System.Runtime.InteropServices;
using Xunit;
using RowCounts = System.Reflection.Metadata.Ecma335.MetadataAggregator.RowCounts;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MetadataAggregatorTests
    {
        private static unsafe EnCMapTableReader CreateEncMapTable(byte[] tokens)
        {
            GCHandle handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
            var block = new MemoryBlock((byte*)handle.AddrOfPinnedObject(), tokens.Length);
            return new EnCMapTableReader(tokens.Length / sizeof(uint), block, containingBlockOffset: 0);
        }

        private static EnCMapTableReader[] CreateEncMapTables(byte[][] tables)
        {
            var result = new EnCMapTableReader[tables.Length];

            for (int i = 0; i < tables.Length; i++)
            {
                result[i] = CreateEncMapTable(tables[i]);
            }

            return result;
        }

        private static void AssertTableRowCounts(string expected, RowCounts[] actual)
        {
            Assert.Equal(expected, string.Join(" | ", actual));
        }

        private static void TestGenerationHandle(MetadataAggregator aggregator, Handle aggregateHandle, Handle expectedHandle, int expectedGeneration)
        {
            int actualGeneration;
            var actualHandle = aggregator.GetGenerationHandle(aggregateHandle, out actualGeneration);
            Assert.Equal(expectedGeneration, actualGeneration);
            Assert.Equal(expectedHandle, actualHandle);
        }

        [Fact]
        public void RowCounts()
        {
            var encMaps = new[]
            {
                new byte[] // Gen1
                {
                    0x9c, 0x00, 0x00, 0x01,
                    0x2e, 0x00, 0x00, 0x02,
                    0x9e, 0x00, 0x00, 0x06,
                    0x9f, 0x00, 0x00, 0x06,
                    0x11, 0x00, 0x00, 0x23,
                },
                new byte[] // Gen2
                {
                    0x9d, 0x00, 0x00, 0x01,
                    0x75, 0x00, 0x00, 0x06,
                    0x1a, 0x00, 0x00, 0x17,
                    0x37, 0x00, 0x00, 0x18,
                    0x38, 0x00, 0x00, 0x18,
                    0x12, 0x00, 0x00, 0x23,
                    0x13, 0x00, 0x00, 0x23,
                },
                new byte[] // Gen3
                {
                    0x9e, 0x00, 0x00, 0x01,
                    0x9f, 0x00, 0x00, 0x01,
                    0x75, 0x00, 0x00, 0x06,
                    0x31, 0x00, 0x00, 0x11,
                    0x1a, 0x00, 0x00, 0x17,
                    0x39, 0x00, 0x00, 0x18,
                    0x3a, 0x00, 0x00, 0x18,
                    0x14, 0x00, 0x00, 0x23,
                    0x15, 0x00, 0x00, 0x23,
                }
            };

            var baseRowCounts = new int[MetadataTokens.TableCount];
            baseRowCounts[0x01] = 0x9b;
            baseRowCounts[0x02] = 0x2d;
            baseRowCounts[0x06] = 0x9d;
            baseRowCounts[0x11] = 0x30;
            baseRowCounts[0x17] = 0x19;
            baseRowCounts[0x18] = 0x36;
            baseRowCounts[0x23] = 0x10;

            var rowCounts = MetadataAggregator.GetBaseRowCounts(baseRowCounts, encMaps.Length + 1);

            var encMapTables = CreateEncMapTables(encMaps);

            for (int i = 0; i < encMapTables.Length; i++)
            {
                MetadataAggregator.CalculateDeltaRowCountsForGeneration(rowCounts, i + 1, ref encMapTables[i]);
            }

            AssertTableRowCounts("+0x9b ~0x0 | +0x9c ~0x0 | +0x9d ~0x0 | +0x9f ~0x0", rowCounts[0x01]);
            AssertTableRowCounts("+0x2d ~0x0 | +0x2e ~0x0 | +0x2e ~0x0 | +0x2e ~0x0", rowCounts[0x02]);
            AssertTableRowCounts("+0x9d ~0x0 | +0x9f ~0x0 | +0x9f ~0x1 | +0x9f ~0x1", rowCounts[0x06]);
            AssertTableRowCounts("+0x30 ~0x0 | +0x30 ~0x0 | +0x30 ~0x0 | +0x31 ~0x0", rowCounts[0x11]);
            AssertTableRowCounts("+0x19 ~0x0 | +0x19 ~0x0 | +0x1a ~0x0 | +0x1a ~0x1", rowCounts[0x17]);
            AssertTableRowCounts("+0x36 ~0x0 | +0x36 ~0x0 | +0x38 ~0x0 | +0x3a ~0x0", rowCounts[0x18]);
            AssertTableRowCounts("+0x10 ~0x0 | +0x11 ~0x0 | +0x13 ~0x0 | +0x15 ~0x0", rowCounts[0x23]);

            var aggregator = new MetadataAggregator(rowCounts, new int[0][]);

            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000031), expectedHandle: MetadataTokens.Handle(0x11000001), expectedGeneration: 3);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000030), expectedHandle: MetadataTokens.Handle(0x11000030), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000001), expectedHandle: MetadataTokens.Handle(0x11000001), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000015), expectedHandle: MetadataTokens.Handle(0x11000015), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000000), expectedHandle: MetadataTokens.Handle(0x11000000), expectedGeneration: 0);

            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x06000075), expectedHandle: MetadataTokens.Handle(0x06000075), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x0600009e), expectedHandle: MetadataTokens.Handle(0x06000001), expectedGeneration: 1);
            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x0600009f), expectedHandle: MetadataTokens.Handle(0x06000002), expectedGeneration: 1);

            TestGenerationHandle(aggregator, MetadataTokens.Handle(0x1800003a), expectedHandle: MetadataTokens.Handle(0x18000002), expectedGeneration: 3);

            AssertExtensions.Throws<ArgumentException>("handle", () => TestGenerationHandle(aggregator, MetadataTokens.Handle(0x11000032), expectedHandle: MetadataTokens.Handle(0x00000000), expectedGeneration: 0));
        }

        [Fact]
        public void HeapSizes()
        {
            var heapSizes = new int[][]
            {
                new int[] // #US
                {
                    0,     // Gen0
                    10,    // Gen1
                    20,    // Gen2
                    30,    // Gen3
                    40,    // Gen4
                },
                new int[] // #String
                {
                    0,     // Gen0
                    0,     // Gen1
                    22,    // Gen2
                    22,    // Gen3
                    22,    // Gen4
                },
                new int[] // #Blob
                {
                    100,    // Gen0
                    100,    // Gen1
                    100,    // Gen2
                    200,    // Gen3
                    400,    // Gen4
                },
                new int[] // #Guid (sizes are numbers of GUIDs on the heap, not bytes; GUIDs on the heap accumulate, previous content is copied to the next gen).
                {
                    1,      // Gen0: Guid #1
                    2,      // Gen1: Guid #1, #2
                    2,      // Gen2: Guid #1, #2
                    2,      // Gen3: Guid #1, #2
                    3,      // Gen4: Guid #1, #2, #3
                }
            };

            var aggregator = new MetadataAggregator(new RowCounts[0][], heapSizes);

            TestGenerationHandle(aggregator, MetadataTokens.BlobHandle(99), expectedHandle: MetadataTokens.BlobHandle(99), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.BlobHandle(100), expectedHandle: MetadataTokens.BlobHandle(0), expectedGeneration: 3);
            TestGenerationHandle(aggregator, MetadataTokens.BlobHandle(200), expectedHandle: MetadataTokens.BlobHandle(0), expectedGeneration: 4);
            TestGenerationHandle(aggregator, MetadataTokens.UserStringHandle(12), expectedHandle: MetadataTokens.UserStringHandle(2), expectedGeneration: 2);
            TestGenerationHandle(aggregator, MetadataTokens.StringHandle(0), expectedHandle: MetadataTokens.StringHandle(0), expectedGeneration: 2);

            // GUIDs on the heap accumulate, previous content is copied to the next gen, so the expected handle is the same as the given handle
            TestGenerationHandle(aggregator, MetadataTokens.GuidHandle(1), expectedHandle: MetadataTokens.GuidHandle(1), expectedGeneration: 0);
            TestGenerationHandle(aggregator, MetadataTokens.GuidHandle(2), expectedHandle: MetadataTokens.GuidHandle(2), expectedGeneration: 1);
            TestGenerationHandle(aggregator, MetadataTokens.GuidHandle(3), expectedHandle: MetadataTokens.GuidHandle(3), expectedGeneration: 4);

            AssertExtensions.Throws<ArgumentException>("handle", () => TestGenerationHandle(aggregator, MetadataTokens.StringHandle(22), expectedHandle: MetadataTokens.StringHandle(0), expectedGeneration: 0));
        }
    }
}
