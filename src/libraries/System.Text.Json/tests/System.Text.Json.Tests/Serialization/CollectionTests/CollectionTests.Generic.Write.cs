// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void WriteListOfList()
        {
            var input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            var input2 = new GenericListWrapper<StringListWrapper>
            {
                new StringListWrapper() { "1", "2" },
                new StringListWrapper() { "3", "4" }
            };

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteListOfArray()
        {
            var input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfList()
        {
            var input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveList()
        {
            var input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteGenericIEnumerableOfGenericIEnumerable()
        {
            IEnumerable<IEnumerable<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIEnumerableWrapper<StringIEnumerableWrapper> input2 = new GenericIEnumerableWrapper<StringIEnumerableWrapper>(new List<StringIEnumerableWrapper>
            {
                new StringIEnumerableWrapper(new List<string> { "1", "2" }),
                new StringIEnumerableWrapper(new List<string> { "3", "4" }),
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteIEnumerableTOfArray()
        {
            IEnumerable<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIEnumerableT()
        {
            IEnumerable<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIEnumerableT()
        {
            IEnumerable<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteGenericIListOfGenericIList()
        {
            IList<IList<int>> input = new List<IList<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIListWrapper<StringIListWrapper> input2 = new GenericIListWrapper<StringIListWrapper>
            {
                new StringIListWrapper() { "1", "2" },
                new StringIListWrapper() { "3", "4" }
            };

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteIListTOfArray()
        {
            IList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIListT()
        {
            IList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIListT()
        {
            IList<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteGenericStructIListWrapperT()
        {
            {
                GenericStructIListWrapper<int> obj = new GenericStructIListWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", JsonSerializer.Serialize(obj));
            }

            {
                GenericStructIListWrapper<int> obj = default;
                Assert.Equal("[]", JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteGenericStructICollectionWrapperT()
        {
            {
                GenericStructICollectionWrapper<int> obj = new GenericStructICollectionWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", JsonSerializer.Serialize(obj));
            }

            {
                GenericStructICollectionWrapper<int> obj = default;
                Assert.Equal("[]", JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteGenericICollectionOfGenericICollection()
        {
            ICollection<ICollection<int>> input = new List<ICollection<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericICollectionWrapper<GenericICollectionWrapper<string>> input2 = new GenericICollectionWrapper<GenericICollectionWrapper<string>>
            {
                new GenericICollectionWrapper<string>() { "1", "2" },
                new GenericICollectionWrapper<string>() { "3", "4" }
            };

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteICollectionTOfArray()
        {
            ICollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfICollectionT()
        {
            ICollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveICollectionT()
        {
            ICollection<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteGenericIReadOnlyCollectionOfGenericIReadOnlyCollection()
        {
            IReadOnlyCollection<IReadOnlyCollection<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>> input2 =
                new GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>>(new List<WrapperForIReadOnlyCollectionOfT<string>>
            {
                new WrapperForIReadOnlyCollectionOfT<string>(new List<string> { "1", "2" }),
                new WrapperForIReadOnlyCollectionOfT<string>(new List<string> { "3", "4" })
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteIReadOnlyCollectionTOfArray()
        {
            IReadOnlyCollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper> input2 = new GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper>(new List<StringIReadOnlyListWrapper>
            {
                new StringIReadOnlyListWrapper(new List<string> { "1", "2" }),
                new StringIReadOnlyListWrapper(new List<string> { "3", "4" })
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteIReadOnlyListTOfArray()
        {
            IReadOnlyList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfIReadOnlyListT()
        {
            IReadOnlyList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveIReadOnlyListT()
        {
            IReadOnlyList<int> input = new List<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void GenericStructISetWrapperT()
        {
            {
                GenericStructISetWrapper<int> obj = new GenericStructISetWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", JsonSerializer.Serialize(obj));
            }

            {
                GenericStructISetWrapper<int> obj = default;
                Assert.Equal("[]", JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void WriteISetTOfISetT()
        {
            ISet<ISet<int>> input = new HashSet<ISet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<ISet<ISet<int>>>(json);

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

            GenericISetWrapper<StringISetWrapper> input2 = new GenericISetWrapper<StringISetWrapper>
            {
                new StringISetWrapper() { "1", "2" },
                new StringISetWrapper() { "3", "4" },
            };

            json = JsonSerializer.Serialize(input2);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input2 = JsonSerializer.Deserialize<GenericISetWrapper<StringISetWrapper>>(json);

            if (input2.First().Contains("1"))
            {
                Assert.Equal(new StringISetWrapper() { "1", "2" }, input2.First());
                Assert.Equal(new StringISetWrapper() { "3", "4" }, input2.Last());
            }
            else
            {
                Assert.Equal(new StringISetWrapper() { "3", "4" }, input2.First());
                Assert.Equal(new StringISetWrapper() { "1", "2" }, input2.Last());
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void WriteISetTOfHashSetT()
        {
            ISet<HashSet<int>> input = new HashSet<HashSet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<ISet<HashSet<int>>>(json);

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

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<HashSet<ISet<int>>>(json);

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

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public static void WriteArrayOfISetT()
        {
            ISet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>() { 1, 2 };
            input[1] = new HashSet<int>() { 3, 4 };

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<ISet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public static void WritePrimitiveISetT()
        {
            ISet<int> input = new HashSet<int> { 1, 2 };

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[4,3],[2,1]]", json);

            GenericStackWrapper<StringStackWrapper> input2 = new GenericStackWrapper<StringStackWrapper>(new List<StringStackWrapper>
            {
                new StringStackWrapper(new List<string>() { "1", "2" }),
                new StringStackWrapper(new List<string>() { "3", "4" })
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""4"",""3""],[""2"",""1""]]", json);
        }

        [Fact]
        public static void WriteStackTOfArray()
        {
            Stack<int[]> input = new Stack<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public static void WriteArrayOfStackT()
        {
            Stack<int>[] input = new Stack<int>[2];
            input[0] = new Stack<int>(new List<int> { 1, 2 });
            input[1] = new Stack<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public static void WritePrimitiveStackT()
        {
            Stack<int> input = new Stack<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericQueueWrapper<StringQueueWrapper> input2 = new GenericQueueWrapper<StringQueueWrapper>(new List<StringQueueWrapper>
            {
                new StringQueueWrapper(new List<string>() { "1", "2" }),
                new StringQueueWrapper(new List<string>() { "3", "4" })
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteQueueTOfArray()
        {
            Queue<int[]> input = new Queue<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfQueueT()
        {
            Queue<int>[] input = new Queue<int>[2];
            input[0] = new Queue<int>(new List<int> { 1, 2 });
            input[1] = new Queue<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveQueueT()
        {
            Queue<int> input = new Queue<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<HashSet<HashSet<int>>>(json);

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

            GenericHashSetWrapper<StringHashSetWrapper> input2 = new GenericHashSetWrapper<StringHashSetWrapper>(new List<StringHashSetWrapper>
            {
                new StringHashSetWrapper(new List<string>() { "1", "2" }),
                new StringHashSetWrapper(new List<string>() { "3", "4" })
            });

            json = JsonSerializer.Serialize(input2);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input2 = JsonSerializer.Deserialize<GenericHashSetWrapper<StringHashSetWrapper>>(json);

            if (input2.First().Contains("1"))
            {
                Assert.Equal(new StringHashSetWrapper(new List<string> { "1", "2" }), input2.First());
                Assert.Equal(new StringHashSetWrapper(new List<string> { "3", "4" }), input2.Last());
            }
            else
            {
                Assert.Equal(new StringHashSetWrapper(new List<string> { "3", "4" }), input2.First());
                Assert.Equal(new StringHashSetWrapper(new List<string> { "1", "2" }), input2.Last());
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

            string json = JsonSerializer.Serialize(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void WriteArrayOfHashSetT()
        {
            HashSet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>(new List<int> { 1, 2 });
            input[1] = new HashSet<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.Serialize(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = JsonSerializer.Deserialize<HashSet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public static void WritePrimitiveHashSetT()
        {
            HashSet<int> input = new HashSet<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
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

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericLinkedListWrapper<StringLinkedListWrapper> input2 = new GenericLinkedListWrapper<StringLinkedListWrapper>(new List<StringLinkedListWrapper>
            {
                new StringLinkedListWrapper(new List<string>() { "1", "2" }),
                new StringLinkedListWrapper(new List<string>() { "3", "4" }),
            });

            json = JsonSerializer.Serialize(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public static void WriteLinkedListTOfArray()
        {
            LinkedList<int[]> input = new LinkedList<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WriteArrayOfLinkedListT()
        {
            LinkedList<int>[] input = new LinkedList<int>[2];
            input[0] = new LinkedList<int>(new List<int> { 1, 2 });
            input[1] = new LinkedList<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveLinkedListT()
        {
            LinkedList<int> input = new LinkedList<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteArrayOfSortedSetT()
        {
            SortedSet<int>[] input = new SortedSet<int>[2];
            input[0] = new SortedSet<int>(new List<int> { 1, 2 });
            input[1] = new SortedSet<int>(new List<int> { 3, 4 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public static void WritePrimitiveSortedSetT()
        {
            SortedSet<int> input = new SortedSet<int>(new List<int> { 1, 2 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public static void WriteGenericCollectionWrappers()
        {
            SimpleTestClassWithGenericCollectionWrappers obj1 = new SimpleTestClassWithGenericCollectionWrappers();
            SimpleTestClassWithStringIEnumerableWrapper obj2 = new SimpleTestClassWithStringIEnumerableWrapper();
            SimpleTestClassWithStringIReadOnlyCollectionWrapper obj3 = new SimpleTestClassWithStringIReadOnlyCollectionWrapper();
            SimpleTestClassWithStringIReadOnlyListWrapper obj4 = new SimpleTestClassWithStringIReadOnlyListWrapper();
            SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper obj5 = new SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper();

            obj1.Initialize();
            obj2.Initialize();
            obj3.Initialize();
            obj4.Initialize();
            obj5.Initialize();

            Assert.Equal(SimpleTestClassWithGenericCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj1));
            Assert.Equal(SimpleTestClassWithGenericCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj1));

            Assert.Equal(SimpleTestClassWithStringIEnumerableWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj2));
            Assert.Equal(SimpleTestClassWithStringIEnumerableWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj2));

            Assert.Equal(SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj3));
            Assert.Equal(SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj3));

            Assert.Equal(SimpleTestClassWithStringIReadOnlyListWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj4));
            Assert.Equal(SimpleTestClassWithStringIReadOnlyListWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj4));

            Assert.Equal(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize(obj5));
            Assert.Equal(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json.StripWhitespace(), JsonSerializer.Serialize<object>(obj5));
        }

        [Fact]
        public static void WriteSimpleTestClassWithGenericStructCollectionWrappers()
        {
            {
                SimpleTestClassWithGenericStructCollectionWrappers obj = new SimpleTestClassWithGenericStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestClassWithGenericStructCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }

            {
                SimpleTestClassWithGenericStructCollectionWrappers obj = new SimpleTestClassWithGenericStructCollectionWrappers()
                {
                    List = default,
                    Dictionary = default,
                    Collection = default,
                    Set = default
                };
                string json =
                    @"{" +
                    @"""List"" : []," +
                    @"""Collection"" : []," +
                    @"""Set"" : []," +
                    @"""Dictionary"" : {}" +
                    @"}";
                Assert.Equal(json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void WriteSimpleTestStructWithNullableGenericStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = new SimpleTestStructWithNullableGenericStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestStructWithNullableGenericStructCollectionWrappers.s_json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }

            {
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = new SimpleTestStructWithNullableGenericStructCollectionWrappers();
                string json =
                    @"{" +
                    @"""List"" : null," +
                    @"""Collection"" : null," +
                    @"""Set"" : null," +
                    @"""Dictionary"" : null" +
                    @"}";
                Assert.Equal(json.StripWhitespace(), JsonSerializer.Serialize(obj));
            }
        }

        [Fact]
        public static void ConvertIEnumerableValueTypesThenSerialize()
        {
            IEnumerable<ValueA> valueAs = Enumerable.Range(0, 5).Select(x => new ValueA { Value = x }).ToList();
            IEnumerable<ValueB> valueBs = valueAs.Select(x => new ValueB { Value = x.Value });

            string expectedJson = @"[{""Value"":0},{""Value"":1},{""Value"":2},{""Value"":3},{""Value"":4}]";
            Assert.Equal(expectedJson, JsonSerializer.Serialize<IEnumerable<ValueB>>(valueBs));
        }

        [Fact]
        public static void WriteIEnumerableT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = JsonSerializer.Serialize(items.AsEnumerable());

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public static void WriteICollectionT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = JsonSerializer.Serialize((ICollection<int>)items);

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public static void WriteIListT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = JsonSerializer.Serialize((IList<int>)items);

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public static void WriteIDictionaryT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var pairs = new RefCountedDictionary<int, int>(Enumerable.Range(1, count).Select(x => new KeyValuePair<int, int>(x, x)));
                _ = JsonSerializer.Serialize((IDictionary<int, int>)pairs);

                Assert.Equal(0, pairs.RefCount);
            }
        }

        [Fact]
        public static void WriteIReadOnlyDictionaryT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var pairs = new RefCountedDictionary<int, int>(Enumerable.Range(1, count).Select(x => new KeyValuePair<int, int>(x, x)));
                _ = JsonSerializer.Serialize((IReadOnlyDictionary<int, int>)pairs);

                Assert.Equal(0, pairs.RefCount);
            }
        }

        [Fact]
        public static void WriteISetT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedSet<int>(Enumerable.Range(1, count));
                _ = JsonSerializer.Serialize((ISet<int>)items);

                Assert.Equal(0, items.RefCount);
            }
        }

        public class SimpleClassWithKeyValuePairs
        {
            public KeyValuePair<string, string> KvpWStrVal { get; set; }
            public KeyValuePair<string, object> KvpWObjVal { get; set; }
            public KeyValuePair<string, SimpleClassWithKeyValuePairs> KvpWClassVal { get; set; }
            public KeyValuePair<string, KeyValuePair<string, string>> KvpWStrKvpVal { get; set; }
            public KeyValuePair<string, KeyValuePair<string, object>> KvpWObjKvpVal { get; set; }
            public KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> KvpWClassKvpVal { get; set; }
        }

        public class ValueA
        {
            public int Value { get; set; }
        }

        public class ValueB
        {
            public int Value { get; set; }
        }

        private class RefCountedList<T> : List<T>, IEnumerable<T>   //  Reimplement interface.
        {
            public RefCountedList() : base() { }
            public RefCountedList(IEnumerable<T> collection) : base(collection) { }

            public int RefCount { get; private set; }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                RefCount++;
                return new DisposableEnumerator<T>(GetEnumerator(), () => RefCount--);
            }

            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();
        }

        private class RefCountedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>    //  Reimplement interface.
        {
            public RefCountedDictionary() : base() { }
            public RefCountedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            {
                foreach (var kvp in collection)
                    Add(kvp.Key, kvp.Value);
            }

            public int RefCount { get; private set; }

            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                RefCount++;
                return new DisposableEnumerator<KeyValuePair<TKey, TValue>>(GetEnumerator(), () => RefCount--);
            }

            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();
        }

        private class RefCountedSet<T> : HashSet<T>, IEnumerable<T>     // Reimplement interface.
        {
            public RefCountedSet() : base() { }
            public RefCountedSet(IEnumerable<T> collection) : base(collection) { }

            public int RefCount { get; private set; }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                RefCount++;
                return new DisposableEnumerator<T>(GetEnumerator(), () => RefCount--);
            }

            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();
        }
    }
}
