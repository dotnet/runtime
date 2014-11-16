// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Xunit;
using RowCounts = System.Reflection.Metadata.Ecma335.MetadataAggregator.RowCounts;

namespace System.Reflection.Metadata.Tests
{
    public class MetadataAggregatorTests
    {
        private unsafe static EnCMapTableReader CreateEncMapTable(int[] tokens)
        {
            GCHandle handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
            var block = new MemoryBlock((byte*)handle.AddrOfPinnedObject(), tokens.Length * sizeof(uint));
            return new EnCMapTableReader((uint)tokens.Length, block, containingBlockOffset: 0);
        }

        private static EnCMapTableReader[] CreateEncMapTables(int[][] tables)
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

        private static void TestGenerationHandle(MetadataAggregator aggregator, int token, int expectedToken, int expectedGeneration)
        {
            int actualGeneration;
            var actualHandle = aggregator.GetGenerationHandle(new Handle((uint)token), out actualGeneration);
            Assert.Equal(expectedGeneration, actualGeneration);
            Assert.Equal(expectedToken, (int)actualHandle.value);
        }

        [Fact]
        public void RowCounts()
        {
            var encMaps = new[]
            {
                new[] // Gen1
                {
                    0x0100009c,
                    0x0200002e,
                    0x0600009e,
                    0x0600009f,
                    0x23000011,
                },
                new[] // Gen2
                {
                    0x0100009d,
                    0x06000075,
                    0x1700001a,
                    0x18000037,
                    0x18000038,
                    0x23000012,
                    0x23000013,
                },
                new[] // Gen3
                {
                    0x0100009e,
                    0x0100009f,
                    0x06000075,
                    0x11000031,
                    0x1700001a,
                    0x18000039,
                    0x1800003a,
                    0x23000014,
                    0x23000015,
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

            TestGenerationHandle(aggregator, 0x11000031, expectedToken: 0x11000001, expectedGeneration: 3);
            TestGenerationHandle(aggregator, 0x11000030, expectedToken: 0x11000030, expectedGeneration: 0);
            TestGenerationHandle(aggregator, 0x11000001, expectedToken: 0x11000001, expectedGeneration: 0);
            TestGenerationHandle(aggregator, 0x11000015, expectedToken: 0x11000015, expectedGeneration: 0);
            TestGenerationHandle(aggregator, 0x11000000, expectedToken: 0x11000000, expectedGeneration: 0);

            TestGenerationHandle(aggregator, 0x06000075, expectedToken: 0x06000075, expectedGeneration: 0);
            TestGenerationHandle(aggregator, 0x0600009e, expectedToken: 0x06000001, expectedGeneration: 1);
            TestGenerationHandle(aggregator, 0x0600009f, expectedToken: 0x06000002, expectedGeneration: 1);

            TestGenerationHandle(aggregator, 0x1800003a, expectedToken: 0x18000002, expectedGeneration: 3);

            Assert.Throws<ArgumentException>(() => TestGenerationHandle(aggregator, 0x11000032, expectedToken: 0x00000000, expectedGeneration: 0));
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
                new int[] // #Guid
                {
                    2,      // Gen0
                    4,      // Gen1
                    8,      // Gen2
                    16,     // Gen3
                    32,     // Gen4
                }
            };

            var aggregator = new MetadataAggregator(new RowCounts[0][], heapSizes);

            TestGenerationHandle(aggregator, (int)TokenTypeIds.Blob | 99, expectedToken: (int)TokenTypeIds.Blob | 99, expectedGeneration: 0);
            TestGenerationHandle(aggregator, (int)TokenTypeIds.Blob | 100, expectedToken: (int)TokenTypeIds.Blob | 0, expectedGeneration: 3);
            TestGenerationHandle(aggregator, (int)TokenTypeIds.Blob | 200, expectedToken: (int)TokenTypeIds.Blob | 0, expectedGeneration: 4);
            TestGenerationHandle(aggregator, (int)TokenTypeIds.UserString | 12, expectedToken: (int)TokenTypeIds.UserString | 2, expectedGeneration: 2);
            TestGenerationHandle(aggregator, (int)TokenTypeIds.String | 0, expectedToken: (int)TokenTypeIds.String | 0, expectedGeneration: 2);

            Assert.Throws<ArgumentException>(() => TestGenerationHandle(aggregator, (int)TokenTypeIds.String | 22, expectedToken: 0x00000000, expectedGeneration: 0));
        }
    }
}
