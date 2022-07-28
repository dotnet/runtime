// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task WriteListOfList()
        {
            var input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            var input2 = new GenericListWrapper<StringListWrapper>
            {
                new StringListWrapper() { "1", "2" },
                new StringListWrapper() { "3", "4" }
            };

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteListOfArray()
        {
            var input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfList()
        {
            var input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveList()
        {
            var input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteGenericIEnumerableOfGenericIEnumerable()
        {
            IEnumerable<IEnumerable<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIEnumerableWrapper<StringIEnumerableWrapper> input2 = new GenericIEnumerableWrapper<StringIEnumerableWrapper>(new List<StringIEnumerableWrapper>
            {
                new StringIEnumerableWrapper(new List<string> { "1", "2" }),
                new StringIEnumerableWrapper(new List<string> { "3", "4" }),
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteIEnumerableTOfArray()
        {
            IEnumerable<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfIEnumerableT()
        {
            IEnumerable<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveIEnumerableT()
        {
            IEnumerable<int> input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteGenericIListOfGenericIList()
        {
            IList<IList<int>> input = new List<IList<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIListWrapper<StringIListWrapper> input2 = new GenericIListWrapper<StringIListWrapper>
            {
                new StringIListWrapper() { "1", "2" },
                new StringIListWrapper() { "3", "4" }
            };

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteIListTOfArray()
        {
            IList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfIListT()
        {
            IList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveIListT()
        {
            IList<int> input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteGenericStructIListWrapperT()
        {
            {
                GenericStructIListWrapper<int> obj = new GenericStructIListWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", await Serializer.SerializeWrapper(obj));
            }

            {
                GenericStructIListWrapper<int> obj = default;
                Assert.Equal("[]", await Serializer.SerializeWrapper(obj));
            }
        }

        [Fact]
        public async Task WriteGenericStructICollectionWrapperT()
        {
            {
                GenericStructICollectionWrapper<int> obj = new GenericStructICollectionWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", await Serializer.SerializeWrapper(obj));
            }

            {
                GenericStructICollectionWrapper<int> obj = default;
                Assert.Equal("[]", await Serializer.SerializeWrapper(obj));
            }
        }

        [Fact]
        public async Task WriteGenericICollectionOfGenericICollection()
        {
            ICollection<ICollection<int>> input = new List<ICollection<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericICollectionWrapper<GenericICollectionWrapper<string>> input2 = new GenericICollectionWrapper<GenericICollectionWrapper<string>>
            {
                new GenericICollectionWrapper<string>() { "1", "2" },
                new GenericICollectionWrapper<string>() { "3", "4" }
            };

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteICollectionTOfArray()
        {
            ICollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfICollectionT()
        {
            ICollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveICollectionT()
        {
            ICollection<int> input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteGenericIReadOnlyCollectionOfGenericIReadOnlyCollection()
        {
            IReadOnlyCollection<IReadOnlyCollection<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>> input2 =
                new GenericIReadOnlyCollectionWrapper<WrapperForIReadOnlyCollectionOfT<string>>(new List<WrapperForIReadOnlyCollectionOfT<string>>
            {
                new WrapperForIReadOnlyCollectionOfT<string>(new List<string> { "1", "2" }),
                new WrapperForIReadOnlyCollectionOfT<string>(new List<string> { "3", "4" })
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteIReadOnlyCollectionTOfArray()
        {
            IReadOnlyCollection<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveIReadOnlyCollectionT()
        {
            IReadOnlyCollection<int> input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteIReadOnlyListTOfIReadOnlyListT()
        {
            IReadOnlyList<IReadOnlyList<int>> input = new List<List<int>>
            {
                new List<int>() { 1, 2 },
                new List<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper> input2 = new GenericIReadOnlyListWrapper<StringIReadOnlyListWrapper>(new List<StringIReadOnlyListWrapper>
            {
                new StringIReadOnlyListWrapper(new List<string> { "1", "2" }),
                new StringIReadOnlyListWrapper(new List<string> { "3", "4" })
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteIReadOnlyListTOfArray()
        {
            IReadOnlyList<int[]> input = new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfIReadOnlyListT()
        {
            IReadOnlyList<int>[] input = new List<int>[2];
            input[0] = new List<int>() { 1, 2 };
            input[1] = new List<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveIReadOnlyListT()
        {
            IReadOnlyList<int> input = new List<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task GenericStructISetWrapperT()
        {
            {
                GenericStructISetWrapper<int> obj = new GenericStructISetWrapper<int>() { 10, 20 };
                Assert.Equal("[10,20]", await Serializer.SerializeWrapper(obj));
            }

            {
                GenericStructISetWrapper<int> obj = default;
                Assert.Equal("[]", await Serializer.SerializeWrapper(obj));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public async Task WriteISetTOfISetT()
        {
            ISet<ISet<int>> input = new HashSet<ISet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<ISet<ISet<int>>>(json);

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

            json = await Serializer.SerializeWrapper(input2);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input2 = await Serializer.DeserializeWrapper<GenericISetWrapper<StringISetWrapper>>(json);

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
        public async Task WriteISetTOfHashSetT()
        {
            ISet<HashSet<int>> input = new HashSet<HashSet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<ISet<HashSet<int>>>(json);

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
        public async Task WriteHashSetTOfISetT()
        {
            HashSet<ISet<int>> input = new HashSet<ISet<int>>
            {
                new HashSet<int>() { 1, 2 },
                new HashSet<int>() { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<HashSet<ISet<int>>>(json);

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
        public async Task WriteISetTOfArray()
        {
            ISet<int[]> input = new HashSet<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        public async Task WriteArrayOfISetT()
        {
            ISet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>() { 1, 2 };
            input[1] = new HashSet<int>() { 3, 4 };

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<ISet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public async Task WritePrimitiveISetT()
        {
            ISet<int> input = new HashSet<int> { 1, 2 };

            string json = await Serializer.SerializeWrapper(input);
            Assert.True(json == "[1,2]" || json == "[2,1]");
        }

        [Fact]
        public async Task WriteStackTOfStackT()
        {
            Stack<Stack<int>> input = new Stack<Stack<int>>(new List<Stack<int>>
            {
                new Stack<int>(new List<int>() { 1, 2 }),
                new Stack<int>(new List<int>() { 3, 4 })
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[4,3],[2,1]]", json);

            GenericStackWrapper<StringStackWrapper> input2 = new GenericStackWrapper<StringStackWrapper>(new List<StringStackWrapper>
            {
                new StringStackWrapper(new List<string>() { "1", "2" }),
                new StringStackWrapper(new List<string>() { "3", "4" })
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""4"",""3""],[""2"",""1""]]", json);
        }

        [Fact]
        public async Task WriteStackTOfArray()
        {
            Stack<int[]> input = new Stack<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[3,4],[1,2]]", json);
        }

        [Fact]
        public async Task WriteArrayOfStackT()
        {
            Stack<int>[] input = new Stack<int>[2];
            input[0] = new Stack<int>(new List<int> { 1, 2 });
            input[1] = new Stack<int>(new List<int> { 3, 4 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[2,1],[4,3]]", json);
        }

        [Fact]
        public async Task WritePrimitiveStackT()
        {
            Stack<int> input = new Stack<int>(new List<int> { 1, 2 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[2,1]", json);
        }

        [Fact]
        public async Task WriteQueueTOfQueueT()
        {
            Queue<Queue<int>> input = new Queue<Queue<int>>(new List<Queue<int>>
            {
                new Queue<int>(new List<int>() { 1, 2 }),
                new Queue<int>(new List<int>() { 3, 4 })
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericQueueWrapper<StringQueueWrapper> input2 = new GenericQueueWrapper<StringQueueWrapper>(new List<StringQueueWrapper>
            {
                new StringQueueWrapper(new List<string>() { "1", "2" }),
                new StringQueueWrapper(new List<string>() { "3", "4" })
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteQueueTOfArray()
        {
            Queue<int[]> input = new Queue<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfQueueT()
        {
            Queue<int>[] input = new Queue<int>[2];
            input[0] = new Queue<int>(new List<int> { 1, 2 });
            input[1] = new Queue<int>(new List<int> { 3, 4 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveQueueT()
        {
            Queue<int> input = new Queue<int>(new List<int> { 1, 2 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteHashSetTOfHashSetT()
        {
            HashSet<HashSet<int>> input = new HashSet<HashSet<int>>(new List<HashSet<int>>
            {
                new HashSet<int>(new List<int>() { 1, 2 }),
                new HashSet<int>(new List<int>() { 3, 4 })
            });

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<HashSet<HashSet<int>>>(json);

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

            json = await Serializer.SerializeWrapper(input2);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input2 = await Serializer.DeserializeWrapper<GenericHashSetWrapper<StringHashSetWrapper>>(json);

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
        public async Task WriteHashSetTOfArray()
        {
            HashSet<int[]> input = new HashSet<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Contains("[1,2]", json);
            Assert.Contains("[3,4]", json);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50721", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public async Task WriteArrayOfHashSetT()
        {
            HashSet<int>[] input = new HashSet<int>[2];
            input[0] = new HashSet<int>(new List<int> { 1, 2 });
            input[1] = new HashSet<int>(new List<int> { 3, 4 });

            string json = await Serializer.SerializeWrapper(input);

            // Because order isn't guaranteed, roundtrip data to ensure write was accurate.
            input = await Serializer.DeserializeWrapper<HashSet<int>[]>(json);
            Assert.Equal(new HashSet<int> { 1, 2 }, input.First());
            Assert.Equal(new HashSet<int> { 3, 4 }, input.Last());
        }

        [Fact]
        public async Task WritePrimitiveHashSetT()
        {
            HashSet<int> input = new HashSet<int>(new List<int> { 1, 2 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.True(json == "[1,2]" || json == "[2,1]");
        }

        [Fact]
        public async Task WriteLinkedListTOfLinkedListT()
        {
            LinkedList<LinkedList<int>> input = new LinkedList<LinkedList<int>>(new List<LinkedList<int>>
            {
                new LinkedList<int>(new List<int>() { 1, 2 }),
                new LinkedList<int>(new List<int>() { 3, 4 })
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);

            GenericLinkedListWrapper<StringLinkedListWrapper> input2 = new GenericLinkedListWrapper<StringLinkedListWrapper>(new List<StringLinkedListWrapper>
            {
                new StringLinkedListWrapper(new List<string>() { "1", "2" }),
                new StringLinkedListWrapper(new List<string>() { "3", "4" }),
            });

            json = await Serializer.SerializeWrapper(input2);
            Assert.Equal(@"[[""1"",""2""],[""3"",""4""]]", json);
        }

        [Fact]
        public async Task WriteLinkedListTOfArray()
        {
            LinkedList<int[]> input = new LinkedList<int[]>(new List<int[]>
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WriteArrayOfLinkedListT()
        {
            LinkedList<int>[] input = new LinkedList<int>[2];
            input[0] = new LinkedList<int>(new List<int> { 1, 2 });
            input[1] = new LinkedList<int>(new List<int> { 3, 4 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveLinkedListT()
        {
            LinkedList<int> input = new LinkedList<int>(new List<int> { 1, 2 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteArrayOfSortedSetT()
        {
            SortedSet<int>[] input = new SortedSet<int>[2];
            input[0] = new SortedSet<int>(new List<int> { 1, 2 });
            input[1] = new SortedSet<int>(new List<int> { 3, 4 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[[1,2],[3,4]]", json);
        }

        [Fact]
        public async Task WritePrimitiveSortedSetT()
        {
            SortedSet<int> input = new SortedSet<int>(new List<int> { 1, 2 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal("[1,2]", json);
        }

        [Fact]
        public async Task WriteGenericCollectionWrappers()
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

            Assert.Equal(SimpleTestClassWithGenericCollectionWrappers.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj1));
            Assert.Equal(SimpleTestClassWithGenericCollectionWrappers.s_json.StripWhitespace(), await Serializer.SerializeWrapper<object>(obj1));

            Assert.Equal(SimpleTestClassWithStringIEnumerableWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj2));
            Assert.Equal(SimpleTestClassWithStringIEnumerableWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper<object>(obj2));

            Assert.Equal(SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj3));
            Assert.Equal(SimpleTestClassWithStringIReadOnlyCollectionWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper<object>(obj3));

            Assert.Equal(SimpleTestClassWithStringIReadOnlyListWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj4));
            Assert.Equal(SimpleTestClassWithStringIReadOnlyListWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper<object>(obj4));

            Assert.Equal(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj5));
            Assert.Equal(SimpleTestClassWithStringToStringIReadOnlyDictionaryWrapper.s_json.StripWhitespace(), await Serializer.SerializeWrapper<object>(obj5));
        }

        [Fact]
        public async Task WriteSimpleTestClassWithGenericStructCollectionWrappers()
        {
            {
                SimpleTestClassWithGenericStructCollectionWrappers obj = new SimpleTestClassWithGenericStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestClassWithGenericStructCollectionWrappers.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj));
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
                Assert.Equal(json.StripWhitespace(), await Serializer.SerializeWrapper(obj));
            }
        }

        [Fact]
        public async Task WriteSimpleTestStructWithNullableGenericStructCollectionWrappers()
        {
            {
                SimpleTestStructWithNullableGenericStructCollectionWrappers obj = new SimpleTestStructWithNullableGenericStructCollectionWrappers();
                obj.Initialize();
                Assert.Equal(SimpleTestStructWithNullableGenericStructCollectionWrappers.s_json.StripWhitespace(), await Serializer.SerializeWrapper(obj));
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
                Assert.Equal(json.StripWhitespace(), await Serializer.SerializeWrapper(obj));
            }
        }

        [Fact]
        public async Task ConvertIEnumerableValueTypesThenSerialize()
        {
            IEnumerable<ValueA> valueAs = Enumerable.Range(0, 5).Select(x => new ValueA { Value = x }).ToList();
            IEnumerable<ValueB> valueBs = valueAs.Select(x => new ValueB { Value = x.Value });

            string expectedJson = @"[{""Value"":0},{""Value"":1},{""Value"":2},{""Value"":3},{""Value"":4}]";
            Assert.Equal(expectedJson, await Serializer.SerializeWrapper<IEnumerable<ValueB>>(valueBs));
        }

        [Fact]
        public async Task WriteIEnumerableT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = await Serializer.SerializeWrapper(items.AsEnumerable());

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public async Task WriteICollectionT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = await Serializer.SerializeWrapper((ICollection<int>)items);

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public async Task WriteIListT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedList<int>(Enumerable.Range(1, count));
                _ = await Serializer.SerializeWrapper((IList<int>)items);

                Assert.Equal(0, items.RefCount);
            }
        }

        [Fact]
        public async Task WriteIDictionaryT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var pairs = new RefCountedDictionary<int, int>(Enumerable.Range(1, count).Select(x => new KeyValuePair<int, int>(x, x)));
                _ = await Serializer.SerializeWrapper((IDictionary<int, int>)pairs);

                Assert.Equal(0, pairs.RefCount);
            }
        }

        [Fact]
        public async Task WriteIReadOnlyDictionaryT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var pairs = new RefCountedDictionary<int, int>(Enumerable.Range(1, count).Select(x => new KeyValuePair<int, int>(x, x)));
                _ = await Serializer.SerializeWrapper((IReadOnlyDictionary<int, int>)pairs);

                Assert.Equal(0, pairs.RefCount);
            }
        }

        [Fact]
        public async Task WriteISetT_DisposesEnumerators()
        {
            for (int count = 0; count < 5; count++)
            {
                var items = new RefCountedSet<int>(Enumerable.Range(1, count));
                _ = await Serializer.SerializeWrapper((ISet<int>)items);

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
