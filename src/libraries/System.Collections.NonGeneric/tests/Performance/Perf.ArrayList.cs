// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class Perf_ArrayList
    {
        private static volatile int s_temp;

        private ArrayList CreateArrayListOfInts(int size = 100000)
        {
            var arrayListOfInts = new ArrayList(size);
            for (int element = 0; element < size; element++)
            {
                arrayListOfInts.Add(element);
            }

            return arrayListOfInts;
        }

        private ArrayList CreateArrayList(int size = 100000)
        {
            var arrayListOfInts = new ArrayList(size);
            var obj = new object();
            for (int i = 0; i < size; i++)
            {
                arrayListOfInts.Add(obj);
            }

            return arrayListOfInts;
        }

        [Benchmark(InnerIterationCount = 10000000)]
        public void Add()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            var element = new object();
            foreach (var iteration in Benchmark.Iterations)
            {
                var elements = new ArrayList();
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.Add(element);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 6000)]
        public void AddRange()
        {
            int size = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(1000);
            foreach (var iteration in Benchmark.Iterations)
            {
                ArrayList[] elementsCollection = new ArrayList[size];
                for (int index = 0; index < size; index++)
                {
                    elementsCollection[index] = new ArrayList();
                }

                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < size; index++)
                    {
                        elementsCollection[index].AddRange(elements);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10)]
        public void BinarySearch()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayListOfInts(100000);
            Comparer<int> comparer = Comparer<int>.Default;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < innerIterationCount; index++)
                    {
                        for (int i = 0; i < elements.Count; i++)
                        {
                            elements.BinarySearch(i, comparer);
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 5000)]
        public void Clear()
        {
            int size = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(10000);
            foreach (var iteration in Benchmark.Iterations)
            {
                ArrayList[] elementsCollection = new ArrayList[size];
                for (int index = 0; index < size; index++)
                {
                    elementsCollection[index] = new ArrayList(elements);
                }

                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < size; index++)
                    {
                        elementsCollection[index].Clear();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Contains()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int size = 100000;
            ArrayList elements = CreateArrayListOfInts(size);
            object contained = elements[size / 2];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.Contains(contained);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100000000)]
        public void Ctor_Default()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        new ArrayList();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public void Ctor_ICollection()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(10000);
            object[] array = elements.ToArray();
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        new ArrayList(array);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100000000)]
        public void Count()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(10000);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        s_temp = elements.Count;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100000000)]
        public void Indexer()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList((int)Benchmark.InnerIterationCount);
            object temp;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < innerIterationCount; index++)
                    {
                        temp = elements[index];
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public void Enumerator()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(10000);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        foreach (var element in elements)
                        { }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public void SetCapacity()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int size = 10000;
            ArrayList elements = CreateArrayList(size);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        // Capacity set back and forth between "size + 1" and "size + 2"
                        elements.Capacity = size + (i % 2) + 1;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 50000)]
        public void ToArray()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            foreach (var iteration in Benchmark.Iterations)
            {
                ArrayList elements = CreateArrayList(10000);

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.ToArray();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 500)]
        public void IndexOf()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayListOfInts();

            int nonexistentElemetnt = -1;
            int firstElemetnt = 0;
            int middleElemetnt = elements.Count / 2;
            int lastElemetnt = elements.Count - 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.IndexOf(nonexistentElemetnt);
                        elements.IndexOf(firstElemetnt);
                        elements.IndexOf(middleElemetnt);
                        elements.IndexOf(lastElemetnt);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10)]
        [InlineData(0, 100000)]
        [InlineData(99999, 100000)]
        public void InsertRange(int index, int length)
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(length);
            var arrayList = new ArrayList(elements);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        arrayList.InsertRange(index, elements);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 50000)]
        public void CopyTo()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int size = 10000;
            ArrayList elements = CreateArrayList(size);
            object[] destination = new object[size];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.CopyTo(destination, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public void LastIndexOf()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int size = 10000;
            ArrayList elements = CreateArrayListOfInts(size);
            int element = size / 2;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.LastIndexOf(element);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Remove()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayListOfInts();
            int element = elements.Count / 2;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.Remove(element);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 7000)]
        public void RemoveRange()
        {
            int size = (int)Benchmark.InnerIterationCount;
            ArrayList elements = CreateArrayList(size);

            foreach (var iteration in Benchmark.Iterations)
            {
                ArrayList[] elementsCollection = new ArrayList[size];
                for (int index = 0; index < size; index++)
                {
                    elementsCollection[index] = new ArrayList(elements);
                }

                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < size; index++)
                    {
                        elementsCollection[index].RemoveRange(1, size - 2);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Reverse()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int startIndex = 0;
            int count = 100000;
            ArrayList elements = CreateArrayList(count);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.Reverse(startIndex, count);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000000)]
        public void SetRange()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int startIndex = 0;
            int size = 100000;
            ArrayList elements = CreateArrayList(size);
            var newElemnts = new ArrayList(size);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.SetRange(startIndex, elements);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 20000000)]
        public void GetRange()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            int startIndex = 0;
            int size = 100000;
            ArrayList elements = CreateArrayList(size);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        elements.GetRange(startIndex, size);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000000)]
        public void Sort()
        {
            int size = (int)Benchmark.InnerIterationCount;
            var random = new Random(32829);
            ArrayList elements = new ArrayList(size);
            for (int index = 0; index < elements.Count; index++)
            {
                elements.Add(random.Next());
            }

            foreach (var iteration in Benchmark.Iterations)
            {
                ArrayList[] elementsCollection = new ArrayList[size];
                for (int index = 0; index < size; index++)
                {
                    elementsCollection[index] = new ArrayList(elements);
                }

                using (iteration.StartMeasurement())
                {
                    for (int index = 0; index < size; index++)
                    {
                        elementsCollection[index].Sort();
                    }
                }
            }
        }
    }
}