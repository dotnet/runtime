// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Tests
{
    public class Perf_List
    {
        /// <summary>
        /// Creates a list containing a number of elements equal to the specified size
        /// </summary>
        public static List<object> CreateList(int size)
        {
            Random rand = new Random(24565653);
            List<object> list = new List<object>();
            for (int i = 0; i < size; i++)
                list.Add(rand.Next());
            return list;
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Add(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                List<object> copyList = new List<object>(list);
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        copyList.Add(123555); copyList.Add(123555); copyList.Add(123555); copyList.Add(123555);
                        copyList.Add(123555); copyList.Add(123555); copyList.Add(123555); copyList.Add(123555);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void AddRange(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                    {
                        List<object> emptyList = new List<object>();
                        emptyList.AddRange(list);
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Clear(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup lists to clear
                List<object>[] listlist = new List<object>[5000];
                for (int i = 0; i < 5000; i++)
                    listlist[i] = new List<object>(list);

                // Clear the lists
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                        listlist[i].Clear();
            }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Contains(int size)
        {
            List<object> list = CreateList(size);
            object contained = list[list.Count / 2];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 500; i++)
                    {
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                        list.Contains(contained); list.Contains(contained); list.Contains(contained); list.Contains(contained);
                    }
                }
        }

        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                        new List<object>(); new List<object>(); new List<object>(); new List<object>(); new List<object>();
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ctor_IEnumerable(int size)
        {
            List<object> list = CreateList(size);
            var array = list.ToArray();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        new List<object>(array);
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetCount(int size)
        {
            List<object> list = CreateList(size);
            int temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                        temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count; temp = list.Count;
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void GetItem(int size)
        {
            List<object> list = CreateList(size);
            object temp;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                        temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50]; temp = list[50];
                    }
                }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Enumerator(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        foreach (var element in list) { }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void SetCapacity(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100; i++)
                    {
                        // Capacity set back and forth between size+1 and size+2
                        list.Capacity = size + (i % 2) + 1;
                    }
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ToArray(int size)
        {
            List<object> list = CreateList(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        list.ToArray();
        }

        [Benchmark]
        public static void IndexOf_ValueType()
        {
            List<int> collection = new List<int>();
            int nonexistentItem, firstItem, middleItem, lastItem;

            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i);
            }

            nonexistentItem = -1;
            firstItem = 0;
            middleItem = collection.Count / 2;
            lastItem = collection.Count - 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    collection.IndexOf(nonexistentItem);
                    collection.IndexOf(firstItem);
                    collection.IndexOf(middleItem);
                    collection.IndexOf(lastItem);
                }
            }
        }

        [Benchmark]
        public static void IndexOf_ReferenceType()
        {
            List<string> collection = new List<string>();
            string nonexistentItem, firstItem, middleItem, lastItem;

            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i.ToString());
            }

            nonexistentItem = "foo";
            firstItem = 0.ToString();
            middleItem = (collection.Count / 2).ToString();
            lastItem = (collection.Count - 1).ToString();

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    collection.IndexOf(nonexistentItem);
                    collection.IndexOf(firstItem);
                    collection.IndexOf(middleItem);
                    collection.IndexOf(lastItem);
                }
            }
        }

        private static int getSampleLength(bool largeSets)
        {
            if (largeSets)
                return LARGE_SAMPLE_LENGTH;
            else
                return smallSampleLength;
        }

        [Benchmark]
        [InlineData(true)]
        [InlineData(false)]
        public static void GenericList_AddRange_Int_NoCapacityIncrease(bool largeSets)
        {
            int sampleLength = getSampleLength(largeSets);

            int[] sampleSet = new int[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i;
            }

            int addLoops = LARGE_SAMPLE_LENGTH / sampleLength;

            //Create an ArrayList big enough to hold 17 copies of the sample set
            int startingCapacity = 17 * sampleLength * addLoops;
            List<int> list = new List<int>(startingCapacity);

            //Add the data to the array list.

            for (int j = 0; j < addLoops; j++)
            {
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
                list.AddRange(sampleSet);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                //Clear the ArrayList without changing its capacity, so that when more data is added to the list its
                //capacity will not need to increase.
                list.RemoveRange(0, startingCapacity);

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < addLoops; j++)
                    {
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(true)]
        [InlineData(false)]
        public static void GenericList_AddRange_Int_CapacityIncrease(bool largeSets)
        {
            int sampleLength = getSampleLength(largeSets);

            int[] sampleSet = new int[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i;
            }

            int addLoops = LARGE_SAMPLE_LENGTH / sampleLength;

            foreach (var iteration in Benchmark.Iterations)
            {
                List<int> list = new List<int>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < addLoops; j++)
                    {
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(true)]
        [InlineData(false)]
        public static void GenericList_AddRange_String_NoCapacityIncrease(bool largeSets)
        {
            int sampleLength = getSampleLength(largeSets);

            string[] sampleSet = new string[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i.ToString();
            }

            int addLoops = LARGE_SAMPLE_LENGTH / sampleLength;

            //Create an ArrayList big enough to hold 17 copies of the sample set
            int startingCapacity = 17 * sampleLength * addLoops;
            List<string> list = new List<string>(startingCapacity);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < addLoops; j++)
                    {
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                    }
                }

                list.RemoveRange(0, startingCapacity);
            }
        }

        [Benchmark]
        [InlineData(true)]
        [InlineData(false)]
        public static void GenericList_AddRange_String_CapacityIncrease(bool largeSets)
        {
            int sampleLength = getSampleLength(largeSets);

            string[] sampleSet = new string[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i.ToString();
            }

            int addLoops = LARGE_SAMPLE_LENGTH / sampleLength;

            foreach (var iteration in Benchmark.Iterations)
            {
                List<string> list = new List<string>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < addLoops; j++)
                    {
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                        list.AddRange(sampleSet);
                    }
                }
            }
        }

        [Benchmark]
        public static void Add_ValueType()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                List<int> collection = new List<int>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 256; ++j)
                    {
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                        collection.Add(j);
                    }
                }
            }
        }

        [Benchmark]
        public static void Add_ReferenceType()
        {
            string itemToAdd = "foo";

            foreach (var iteration in Benchmark.Iterations)
            {
                List<string> collection = new List<string>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 256; ++j)
                    {
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                        collection.Add(itemToAdd);
                    }
                }
            }
        }

        [Benchmark]
        public static void GenericList_BinarySearch_Int()
        {
            int sampleLength = 10000;

            int[] sampleSet = new int[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i;
            }

            List<int> list = new List<int>(sampleSet);
            IComparer<int> comparer = Comparer<int>.Default;

            int result = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < sampleLength; j++)
                        result = list.BinarySearch(sampleSet[j], comparer);
                }
            }
        }

        [Benchmark]
        public static void GenericList_BinarySearch_String()
        {
            int sampleLength = 1000;

            string[] sampleSet = new string[sampleLength];
            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i.ToString();
            }

            List<string> list = new List<string>(sampleSet);
            IComparer<string> comparer = Comparer<string>.Default;

            int result = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < sampleLength; j++)
                        result = list.BinarySearch(sampleSet[j], comparer);
                }
            }
        }

        [Benchmark]
        public static void Contains_ValueType()
        {
            List<int> collection = new List<int>();
            int nonexistentItem, firstItem, middleItem, lastItem;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i);
            }

            nonexistentItem = -1;
            firstItem = 0;
            middleItem = collection.Count / 2;
            lastItem = collection.Count - 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    collection.Contains(nonexistentItem);
                    collection.Contains(firstItem);
                    collection.Contains(middleItem);
                    collection.Contains(lastItem);
                }
            }
        }

        [Benchmark]
        public static void Contains_ReferenceType()
        {
            List<string> collection = new List<string>();
            string nonexistentItem, firstItem, middleItem, lastItem;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i.ToString());
            }

            nonexistentItem = "foo";
            firstItem = 0.ToString();
            middleItem = (collection.Count / 2).ToString();
            lastItem = (collection.Count - 1).ToString();


            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    collection.Contains(nonexistentItem);
                    collection.Contains(firstItem);
                    collection.Contains(middleItem);
                    collection.Contains(lastItem);
                }
            }
        }

        [Benchmark]
        public static void ctor_ICollection_ValueType()
        {
            List<int> originalCollection = new List<int>();
            List<int> newCollection;

            //[] Initialize
            for (int i = 0; i < 1024; ++i)
            {
                originalCollection.Add(i);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    newCollection = new List<int>(originalCollection);
                }
            }
        }

        [Benchmark]
        public static void ctor_ICollection_ReferenceType()
        {
            List<string> originalCollection = new List<string>();
            List<string> newCollection;

            //[] Initialize
            for (int i = 0; i < 1024; ++i)
            {
                originalCollection.Add(i.ToString());
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    newCollection = new List<string>(originalCollection);
                }
            }
        }

        [Benchmark]
        public static void Indexer_ValueType()
        {
            List<int> collection = new List<int>();
            int item = 0;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 8192; ++j)
                    {
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                    }
                }
            }
        }

        [Benchmark]
        public static void Indexer_ReferenceType()
        {
            List<string> collection = new List<string>();
            string item = null;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i.ToString());
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 8192; ++j)
                    {
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                        item = collection[j];
                    }
                }
            }
        }

        [Benchmark]
        public static void Sort_ValueType()
        {
            Random random = new Random(32829);
            int size = 3000;
            int[] items;
            List<int> collection;

            //[] Initialize
            items = new int[size];

            for (int i = 0; i < size; ++i)
                items[i] = random.Next();

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<int>(size);

                for (int j = 0; j < size; ++j)
                    collection.Add(items[j]);

                using (iteration.StartMeasurement())
                    collection.Sort();
            }
        }

        [Benchmark]
        public static void Sort_ReferenceType()
        {
            Random random = new Random(32829);
            int size = 3000;
            string[] items;
            List<string> collection;

            //[] Initialize
            items = new string[size];

            for (int i = 0; i < size; ++i)
            {
                items[i] = random.Next().ToString();
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<string>(size);

                for (int j = 0; j < size; ++j)
                {
                    collection.Add(items[j]);
                }

                using (iteration.StartMeasurement())
                    collection.Sort();
            }
        }

        [Benchmark]
        public static void GenericList_Reverse_Int()
        {
            int sampleLength = 100000;

            int[] sampleSet = new int[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i;
            }

            List<int> list = new List<int>(sampleSet);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    list.Reverse();
        }

        [Benchmark]
        public static void GenericList_Reverse_String()
        {
            int sampleLength = 100000;

            string[] sampleSet = new string[sampleLength];
            for (int i = 0; i < sampleLength; i++)
            {
                sampleSet[i] = i.ToString();
            }

            List<string> list = new List<string>(sampleSet);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    list.Reverse();
        }

        [Benchmark]
        public static void Remove_ValueType()
        {
            int size = 3000;
            int[] items;
            List<int> collection;
            int start, middle, end;

            //[] Initialize
            items = new int[size];

            for (int i = 0; i < size; ++i)
            {
                items[i] = i;
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<int>();

                for (int j = 0; j < size; ++j)
                {
                    collection.Add(items[j]);
                }

                start = 0;
                middle = size / 3;
                end = size - 1;

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < size / 3; j++)
                    {
                        collection.Remove(-1);
                        collection.Remove(items[start]);
                        collection.Remove(items[middle]);
                        collection.Remove(items[end]);

                        ++start;
                        ++middle;
                        --end;
                    }
                }
            }
        }

        [Benchmark]
        public static void Remove_ReferenceType()
        {
            int size = 3000;
            string[] items;
            List<string> collection;
            int start, middle, end;

            //[] Initialize
            items = new string[size];

            for (int i = 0; i < size; ++i)
            {
                items[i] = i.ToString();
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<string>();

                for (int j = 0; j < size; ++j)
                {
                    collection.Add(items[j]);
                }

                start = 0;
                middle = size / 3;
                end = size - 1;

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < size / 3; j++)
                    {
                        collection.Remove("-1");
                        collection.Remove(items[start]);
                        collection.Remove(items[middle]);
                        collection.Remove(items[end]);

                        ++start;
                        ++middle;
                        --end;
                    }
                }
            }
        }

        [Benchmark]
        public static void Insert_ValueType()
        {
            List<int> collection;

            collection = new List<int>();
            for (int j = 0; j < 1024; ++j)
            {
                collection.Insert(0, j);//Insert at the begining of the list
                collection.Insert(j / 2, j);//Insert in the middle of the list
                collection.Insert(collection.Count, j);//Insert at the end of the list
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<int>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 1024; ++j)
                    {
                        collection.Insert(0, j);//Insert at the begining of the list
                        collection.Insert(j / 2, j);//Insert in the middle of the list
                        collection.Insert(collection.Count, j);//Insert at the end of the list
                    }
                }
            }
        }

        [Benchmark]
        public static void Insert_ReferenceType()
        {
            List<string> collection;
            string itemToAdd = "foo";

            foreach (var iteration in Benchmark.Iterations)
            {
                collection = new List<string>();

                using (iteration.StartMeasurement())
                {
                    for (int j = 0; j < 1024; ++j)
                    {
                        collection.Insert(0, itemToAdd);//Insert at the begining of the list
                        collection.Insert(j / 2, itemToAdd);//Insert in the middle of the list
                        collection.Insert(collection.Count, itemToAdd);//Insert at the end of the list
                    }
                }
            }
        }

        [Benchmark]
        public static void Enumeration_ValueType()
        {
            List<int> collection = new List<int>();
            int item;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i);
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    foreach (int tempItem in collection)
                    {
                        item = tempItem;
                    }
                }
            }
        }

        [Benchmark]
        public static void Enumeration_ReferenceType()
        {
            List<string> collection = new List<string>();
            string item;

            //[] Initialize
            for (int i = 0; i < 8192; ++i)
            {
                collection.Add(i.ToString());
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    foreach (string tempItem in collection)
                    {
                        item = tempItem;
                    }
                }
            }
        }

        private const int LARGE_SAMPLE_LENGTH = 10000;
        private const int SMALL_LOOPS = 1000;

        private const int smallSampleLength = LARGE_SAMPLE_LENGTH / SMALL_LOOPS;
    }
}
