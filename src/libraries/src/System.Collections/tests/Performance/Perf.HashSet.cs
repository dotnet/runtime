// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Performs quick perf test of HashSet operations. Tests with T = int
    /// 
    /// This is intended for the regular perf runs to catch perf regressions. 
    /// Use HashSetDetailedTests for detailed perf analysis.
    /// </summary>
    public class Perf_HashSet
    {
        private const int InitialSetSize = 8000000;
        private const int InitialSetSize_small = 320000;
        private const int MaxStartSize = 32000;

        [Benchmark]
        [InlineData(InitialSetSize, 1)]
        [InlineData(InitialSetSize, 100)]
        [InlineData(InitialSetSize, 10000)]
        public static void Add(int initialSetSize, int countToAdd)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            // n is varying start size of set
            int[] startingElements = intGenerator.MakeNewTs(initialSetSize);
            int[] stuffToAdd = intGenerator.MakeNewTs(countToAdd);

            foreach (var iteration in Benchmark.Iterations)
            {
                HashSet<int> theSet = new HashSet<int>(startingElements);

                using (iteration.StartMeasurement())
                {
                    foreach (int thing in stuffToAdd)
                    {
                        theSet.Add(thing);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(InitialSetSize, 1)]
        [InlineData(InitialSetSize, 100)]
        [InlineData(InitialSetSize, 10000)]
        public static void Contains_True(int initialSetSize, int countToCheck)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(initialSetSize);
            int[] subsetToCheck = intGenerator.GenerateSelectionSubset(startingElements, countToCheck);
            bool present;

            foreach (var iteration in Benchmark.Iterations)
            {
                HashSet<int> theSet = new HashSet<int>(startingElements);

                using (iteration.StartMeasurement())
                {
                    foreach (int thing in subsetToCheck)
                    {
                        present = theSet.Contains(thing);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(InitialSetSize, 1)]
        [InlineData(InitialSetSize, 100)]
        [InlineData(InitialSetSize, 10000)]
        public static void Contains_False(int initialSetSize, int countToCheck)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(initialSetSize);
            int missingValue = InstanceCreators.IntGenerator_MaxValue + 1;
            bool present;

            foreach (var iteration in Benchmark.Iterations)
            {
                HashSet<int> theSet = new HashSet<int>(startingElements);

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < countToCheck; i++)
                    {
                        present = theSet.Contains(missingValue);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(InitialSetSize, 1)]
        [InlineData(InitialSetSize, 100)]
        [InlineData(InitialSetSize, 10000)]
        public static void Remove(int initialSetSize, int countToRemove)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(initialSetSize);
            int[] stuffToRemove = intGenerator.GenerateSelectionSubset(startingElements, countToRemove);

            foreach (var iteration in Benchmark.Iterations)
            {
                HashSet<int> theSet = new HashSet<int>(startingElements);

                using (iteration.StartMeasurement())
                {
                    foreach (int thing in stuffToRemove)
                    {
                        theSet.Remove(thing);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData(InitialSetSize_small)]
        public static void Clear(int initialSetSize)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingSet = intGenerator.MakeNewTs(initialSetSize);
            HashSet<int> theSet = new HashSet<int>();

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingSet);

                using (iteration.StartMeasurement())
                    theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        public static void Union(int startSize, int countToUnion)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToUnion = intGenerator.GenerateMixedSelection(startingElements, countToUnion);
            HashSet<int> theSet = new HashSet<int>();

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.UnionWith(stuffToUnion);

                theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        public static void Union_NoOp(int startSize, int countToUnion)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToUnion = intGenerator.GenerateMixedSelection(startingElements, countToUnion);
            HashSet<int> theSet = new HashSet<int>(startingElements);
            theSet.UnionWith(stuffToUnion);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                    theSet.UnionWith(stuffToUnion);
            }
        }

        [Benchmark]
        [InlineData(InitialSetSize_small, MaxStartSize)]
        public static void IntersectHashSet(int startSize, int countToIntersect)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(startSize);
            int[] stuffToIntersect = intGenerator.GenerateMixedSelection(startingElements, countToIntersect);
            HashSet<int> theSet = new HashSet<int>();

            HashSet<int> hashSetToIntersect = new HashSet<int>(stuffToIntersect);

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.IntersectWith(hashSetToIntersect);

                theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        private static void TestIntersectEnum(int startSize, int countToIntersect)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(startSize);
            int[] stuffToIntersect = intGenerator.GenerateMixedSelection(startingElements, countToIntersect);
            HashSet<int> theSet = new HashSet<int>();

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.IntersectWith(stuffToIntersect);

                theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        public static void Except(int startSize, int countToExcept)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(startSize);
            int[] stuffToExcept = intGenerator.GenerateMixedSelection(startingElements, countToExcept);
            HashSet<int> theSet = new HashSet<int>();

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.ExceptWith(stuffToExcept);

                theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        public static void SymmetricExcept(int startSize, int countToExcept)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(startSize);
            int[] stuffToExcept = intGenerator.GenerateMixedSelection(startingElements, countToExcept);
            HashSet<int> theSet = new HashSet<int>();
            HashSet<int> otherSet = new HashSet<int>(stuffToExcept);

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.SymmetricExceptWith(otherSet);

                theSet.Clear();
            }
        }

        [Benchmark]
        [InlineData(MaxStartSize, InitialSetSize_small)]
        public static void SymmetricExceptEnum(int startSize, int countToExcept)
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(startSize);
            int[] stuffToExcept = intGenerator.GenerateMixedSelection(startingElements, countToExcept);
            HashSet<int> theSet = new HashSet<int>();

            foreach (var iteration in Benchmark.Iterations)
            {
                theSet.UnionWith(startingElements);

                using (iteration.StartMeasurement())
                    theSet.SymmetricExceptWith(stuffToExcept);

                theSet.Clear();
            }
        }

        [Benchmark]
        public static void TestIsSubsetHashSet()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            // this makes perf as bad as possible; has fallout case for count check
            int[] additionalStuffToAdd = intGenerator.MakeNewTs((Math.Max(0, InitialSetSize_small - theSet.Count)));
            HashSet<int> setToCheckSubset = new HashSet<int>(startingElements);
            setToCheckSubset.UnionWith(additionalStuffToAdd);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsSubsetOf(setToCheckSubset);
        }

        [Benchmark]
        public static void TestIsSubsetEnum()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToCheckSubset = intGenerator.GenerateMixedSelection(startingElements, InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsSubsetOf(stuffToCheckSubset);
        }

        [Benchmark]
        public static void TestIsProperSubsetHashSet()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            // this makes perf as bad as possible; avoids fallout case based on count
            int[] additionalStuffToAdd = intGenerator.MakeNewTs(Math.Max(0, InitialSetSize_small - theSet.Count));
            HashSet<int> setToCheckSubset = new HashSet<int>(startingElements);
            setToCheckSubset.UnionWith(additionalStuffToAdd);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsProperSubsetOf(setToCheckSubset);
        }

        [Benchmark]
        public static void TestIsProperSubsetEnum()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToCheckSubset = intGenerator.GenerateMixedSelection(startingElements, InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsProperSubsetOf(stuffToCheckSubset);
        }

        [Benchmark]
        public static void TestIsSuperset()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToCheckSuperset = intGenerator.GenerateSelectionSubset(startingElements, InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsSupersetOf(stuffToCheckSuperset);
        }

        [Benchmark]
        public static void TestIsProperSupersetHashSet()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToCheckProperSuperset = intGenerator.GenerateSelectionSubset(startingElements, InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsProperSupersetOf(stuffToCheckProperSuperset);
        }

        [Benchmark]
        public static void TestIsProperSupersetEnum()
        {
            RandomTGenerator<int> intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            int[] startingElements = intGenerator.MakeNewTs(MaxStartSize);
            int[] stuffToCheckProperSuperset = intGenerator.GenerateSelectionSubset(startingElements, InitialSetSize_small);
            HashSet<int> theSet = new HashSet<int>();
            theSet.UnionWith(startingElements);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    theSet.IsProperSupersetOf(stuffToCheckProperSuperset);
        }
    }

    // Wrapper class for delegates capable of creating new instances of different types
    public class InstanceCreators
    {
        public static int IntGenerator_MinValue
        {
            get { return -50000; }
        }

        public static int IntGenerator_MaxValue
        {
            get { return 50000; }
        }

        public static int IntGenerator(Random generator, int i)
        {
            return generator.Next(IntGenerator_MinValue, IntGenerator_MaxValue);
        }

        public static DummyClass DummyGenerator(Random generator, int i)
        {
            return new DummyClass(string.Format("Dummy{0}", i));
        }
    }

    // Generates T instances for use with HashSet perf tests
    public class RandomTGenerator<T>
    {
        private static Random s_sequenceGenerator;
        private static Random s_elementGenerator;

        private Instantiate _instantiate;

        public delegate T Instantiate(Random elementGenerator, int i);

        static RandomTGenerator()
        {
            s_sequenceGenerator = new Random();
            s_elementGenerator = new Random();
        }

        public RandomTGenerator(Instantiate i)
        {
            _instantiate = i;
        }

        public T[] MakeNewTs(int count)
        {
            T[] results = new T[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = _instantiate(s_elementGenerator, i);
            }
            return results;
        }

        public T[] GenerateSelectionSubset(T[] allObjects, int numToSelect)
        {
            T[] sequence = new T[numToSelect];
            int max = allObjects.Length - 1;
            for (int i = 0; i < numToSelect; i++)
            {
                int nextInt = s_sequenceGenerator.Next(0, max);
                sequence[i] = allObjects[nextInt];
            }
            return sequence;
        }

        public T[] GenerateMixedSelection(T[] allObjects, int numElements)
        {
            T[] sequence = new T[numElements];
            int max = allObjects.Length - 1;
            for (int i = 0; i < numElements; i++)
            {
                if (i % 2 == 0)
                {
                    int nextInt = s_sequenceGenerator.Next(0, max);
                    sequence[i] = allObjects[nextInt];
                }
                else
                {
                    sequence[i] = _instantiate(s_elementGenerator, i);
                }
            }
            return sequence;
        }
    }

    #region DummyClass / helpers
    // DummyClass is used for HashSet of object tests
    public class DummyClass
    {
        private static Random s_nameRand;
        private static Random s_percentRand;

        private string _id;
        private string _name;
        private double _percent;

        // initialize random generators
        static DummyClass()
        {
            s_nameRand = new Random();
            s_percentRand = new Random();
        }

        public DummyClass(string id)
        {
            _id = id;
            _name = RandomString();
            _percent = s_percentRand.NextDouble();
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public double Percent
        {
            get { return _percent; }
            set { _percent = value; }
        }

        public override string ToString()
        {
            return _id;
        }

        // Generates a random-ish string 
        private static string RandomString()
        {
            int size = s_nameRand.Next(0, 100);

            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * s_nameRand.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }

    // Dummy equality comparer for some DummyClass test cases where we want
    // DummyClass value equality instead of reference
    public class DummyClassEqualityComparer : IEqualityComparer<DummyClass>
    {
        // Don't worry about boundary cases like null, etc; this is just for DummyClass test cases below
        public bool Equals(DummyClass x, DummyClass y)
        {
            return x.Id.Equals(y.Id) && x.Name.Equals(y.Name) && x.Percent.Equals(y.Percent);
        }

        public int GetHashCode(DummyClass obj)
        {
            return obj.Id.GetHashCode() | obj.Name.GetHashCode() | obj.Percent.GetHashCode();
        }
    }
    #endregion
}
