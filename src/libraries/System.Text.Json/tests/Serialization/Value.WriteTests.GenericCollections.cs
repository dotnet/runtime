// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        [Fact]
        public static void WriteListOfList()
        {
            var input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteListOfArray()
        {
            var input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfList()
        {
            var input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveList()
        {
            var input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIEnumerableTOfIEnumerableT()
        {
            IEnumerable<IEnumerable<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIEnumerableTOfArray()
        {
            IEnumerable<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIEnumerableT()
        {
            IEnumerable<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIEnumerableT()
        {
            IEnumerable<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIListTOfIListT()
        {
            IList<IList<int>> input = new List<IList<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIListTOfArray()
        {
            IList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIListT()
        {
            IList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIListT()
        {
            IList<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteICollectionTOfICollectionT()
        {
            ICollection<ICollection<int>> input = new List<ICollection<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteICollectionTOfArray()
        {
            ICollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfICollectionT()
        {
            ICollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveICollectionT()
        {
            ICollection<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIReadOnlyCollectionTOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<IReadOnlyCollection<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIReadOnlyCollectionTOfArray()
        {
            IReadOnlyCollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteIReadOnlyListTOfIReadOnlyListT()
        {
            IReadOnlyList<IReadOnlyList<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteIReadOnlyListTOfArray()
        {
            IReadOnlyList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIReadOnlyListT()
        {
            IReadOnlyList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIReadOnlyListT()
        {
            IReadOnlyList<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteISetTOfISetT()
        {
            ISet<ISet<int>> input = new HashSet<ISet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<ISet<ISet<int>>>(json);

            if (input.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, input.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, input.Last());
            }
        }

        [Fact]
        public static void WriteISetTOfHashSetT()
        {
            ISet<HashSet<int>> input = new HashSet<HashSet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<ISet<HashSet<int>>>(json);

            if (input.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, input.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, input.Last());
            }
        }

        [Fact]
        public static void WriteHashSetTOfISetT()
        {
            HashSet<ISet<int>> input = new HashSet<ISet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<HashSet<ISet<int>>>(json);

            if (input.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, input.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, input.Last());
            }
        }

        [Fact]
        public static void WriteISetTOfArray()
        {
            ISet<int[]> input = new HashSet<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteArrayOfISetT()
        {
            ISet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>() { 1, 2 };
            input[1] = new HashSet<int>() { 3, 4 };

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<ISet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public static void WritePrimitiveISetT()
        {
            ISet<int> input = new HashSet<int> { 1, 2 };

            string json = JsonSerializer.ToString(input);
            Assert.True(json == "[1,2]" || json == "[2,1]");
        }

        [Fact]
        public static void WriteStackTOfStackT()
        {
            Stack<Stack<int>> input = new Stack<Stack<int>>(new List<Stack<int>>
            {
                new Stack<int>(new List<int>() { 1, 2 }),
                new Stack<int>(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[4,3],[2,1]]", json);
        }

        [Fact]
        public static void WriteStackTOfArray()
        {
            Stack<int[]> input = new Stack<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfStackT()
        {
            Stack<int>[] input = new Stack<int>[2];
            input[0] = new Stack<int>(new List<int> { 1, 2 });
            input[1] = new Stack<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveStackT()
        {
            Stack<int> input = new Stack<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public static void WriteQueueTOfQueueT()
        {
            Queue<Queue<int>> input = new Queue<Queue<int>>(new List<Queue<int>>
            {
                new Queue<int>(new List<int>() { 1, 2 }),
                new Queue<int>(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteQueueTOfArray()
        {
            Queue<int[]> input = new Queue<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfQueueT()
        {
            Queue<int>[] input = new Queue<int>[2];
            input[0] = new Queue<int>(new List<int> { 1, 2 });
            input[1] = new Queue<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveQueueT()
        {
            Queue<int> input = new Queue<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteHashSetTOfHashSetT()
        {
            HashSet<HashSet<int>> input = new HashSet<HashSet<int>>(new List<HashSet<int>>
            {
                new HashSet<int>(new List<int>() { 1, 2 }),
                new HashSet<int>(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<HashSet<HashSet<int>>>(json);

            if (input.First().Contains(1))
            {
                Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
                Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
            }
            else
            {
                Assert.Equal(new HashSet<int> { 3, 4 }, input.First());
                Assert.Equal(new HashSet<int> { 1, 2 }, input.Last());
            }
        }

        [Fact]
        public static void WriteHashSetTOfArray()
        {
            HashSet<int[]> input = new HashSet<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.True(json.Contains("[1,2]"));
            Assert.True(json.Contains("[3,4]"));
        }

        [Fact]
        public static void WriteArrayOfHashSetT()
        {
            HashSet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>(new List<int> { 1, 2 });
            input[1] = new HashSet<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.ToString(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Parse<HashSet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public static void WritePrimitiveHashSetT()
        {
            HashSet<int> input = new HashSet<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.True(json == "[1,2]" || json == "[2,1]");
        }

        [Fact]
        public static void WriteLinkedListTOfLinkedListT()
        {
            LinkedList<LinkedList<int>> input = new LinkedList<LinkedList<int>>(new List<LinkedList<int>>
            {
                new LinkedList<int>(new List<int>() { 1, 2 }),
                new LinkedList<int>(new List<int>() { 3, 4 })
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteLinkedListTOfArray()
        {
            LinkedList<int[]> input = new LinkedList<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfLinkedListT()
        {
            LinkedList<int>[] input = new LinkedList<int>[2];
            input[0] = new LinkedList<int>(new List<int> { 1, 2 });
            input[1] = new LinkedList<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveLinkedListT()
        {
            LinkedList<int> input = new LinkedList<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteArrayOfSortedSetT()
        {
            SortedSet<int>[] input = new SortedSet<int>[2];
            input[0] = new SortedSet<int>(new List<int> { 1, 2 });
            input[1] = new SortedSet<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveSortedSetT()
        {
            SortedSet<int> input = new SortedSet<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WritePrimitiveKeyValuePair()
        {
            KeyValuePair<string, int> input = new KeyValuePair<string, int>("Key", 123) ;

            string json = JsonSerializer.ToString(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":123}", json);
        }

        [Fact]
        public static void WriteListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("123", 123),
                new KeyValuePair<string, int>("456", 456)
            };

            string json = JsonSerializer.ToString(input);
            Assert.Equal(@"[{""Key"":""123"",""Value"":123},{""Key"":""456"",""Value"":456}]", json);
        }

        [Fact]
        public static void WriteKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = new KeyValuePair<string, List<int>>("Key", new List<int> { 1, 2, 3 });

            string json = JsonSerializer.ToString(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":[1,2,3]}", json);
        }

        [Fact]
        public static void WriteKeyValuePairOfKeyValuePair()
        {
            KeyValuePair<string, KeyValuePair<string, int>> input = new KeyValuePair<string, KeyValuePair<string, int>>(
                "Key", new KeyValuePair<string, int>("Key", 1));

            string json = JsonSerializer.ToString(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":{""Key"":""Key"",""Value"":1}}", json);
        }
    }
}
