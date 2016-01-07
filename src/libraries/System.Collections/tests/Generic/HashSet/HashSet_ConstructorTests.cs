// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Tests.HashSet_HashSetTestSupport;

namespace Tests
{
    public class HashSet_Constructor
    {
        // Use parity only as a hashcode so as to have many collisions.
        private class BadIntEqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                return obj % 2;
            }

            public override bool Equals(object obj)
            {
                return obj is BadIntEqualityComparer; // Equal to all other instances of this type, not to anything else.
            }

            public override int GetHashCode()
            {
                return unchecked((int)0xC001CAFE); // Doesn't matter as long as its constant.
            }
        }

        private static int s_nextInt;
        private static Func<int> s_intGenerator = () =>
        {
            int current = s_nextInt;
            s_nextInt++;
            return current;
        };

        [Fact]
        public static void Ctor_NegativeTests()
        {
            HashSet<int> hashSet;
            List<int> collection = null;
            Assert.Throws<ArgumentNullException>(() => hashSet = new HashSet<int>(collection)); //"Expected ArgumentNullException"

            Assert.Throws<ArgumentNullException>(() => hashSet = new HashSet<int>(null, null)); //"Expected ArgumentNullException."

            Assert.Throws<ArgumentNullException>(() => hashSet = new HashSet<int>(null, EqualityComparer<int>.Default)); //"Expected ArgumentNullException."
        }

        #region Default Constructor

        //Test 1: Check that EqualityComparer<T>.Default is used
        [Fact]
        public static void Test1()
        {
            HashSet<int> hashSet = new HashSet<int>();
            int[] items = new int[0];
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void Test1_Negative()
        {
            HashSet<int> hashSet = new HashSet<int>();
            int[] items = new int[0];
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 2: T implements IEquatable<T>
        [Fact]
        public static void Test2()
        {
            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            ValueItem[] items = new ValueItem[0];
            HashSetTestSupport<ValueItem> driver = new HashSetTestSupport<ValueItem>(hashSet, ValueItem.GenerateNext, items);
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void Test2_Negative()
        {
            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            ValueItem[] items = new ValueItem[0];
            HashSetTestSupport<ValueItem> driver = new HashSetTestSupport<ValueItem>(hashSet, ValueItem.GenerateNext, items);
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 3: T doesn't implement IEquatable<T>
        [Fact]
        public static void Test3()
        {
            HashSet<Item> hashSet = new HashSet<Item>();
            Item[] items = new Item[0];
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items);
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void Test3_Negative()
        {
            HashSet<Item> hashSet = new HashSet<Item>();
            Item[] items = new Item[0];
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items);
            driver.VerifyHashSet_NegativeTests();
        }

        #endregion

        #region Constructor_IEnumerable

        //Test 2: Collection is empty
        [Fact]
        public static void Ctor_IEnumerable_empty()
        {
            int[] items = new int[0];
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void Ctor_IEnumerable_empty_Neg()
        {
            int[] items = new int[0];
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 3: Collection has one element
        [Fact]
        public static void Ctor_IEnumerable_Single()
        {
            int[] items = new int[] { -23 };
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_Single_Neg()
        {
            int[] items = new int[] { -23 };
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 4: Collection has multiple unique elements
        [Fact]
        public static void Ctor_IEnumerable_Multiple()
        {
            int[] items = new int[] { -23, -2, 4, 6, 0, 123, -4 };
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void Ctor_IEnumerable_Multiple_Neg()
        {
            int[] items = new int[] { -23, -2, 4, 6, 0, 123, -4 };
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 5: Collection has duplicate elements
        [Fact]
        public static void Ctor_IEnumerable_Duplicate()
        {
            int[] items = new int[] { 4, -23, -23, -2, 4, 6, 0, 4, 123, -4, 123 };
            HashSet<int> hashSet = new HashSet<int>(items);
            int[] expected = new int[] { -23, -2, 4, 6, 0, 123, -4 };
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, expected);
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_ManyDuplicates()
        {
            int[] items = new int[] { 4, -23, -23, -2, 4, 6, 0, 4, 123, -4, 123 };
            HashSet<int> hashSet = new HashSet<int>(Enumerable.Range(0, 40).SelectMany(i => items).ToArray());
            int[] expected = new int[] { -23, -2, 4, 6, 0, 123, -4 };
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, expected);
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_Duplicate_Neg()
        {
            int[] items = new int[] { 4, -23, -23, -2, 4, 6, 0, 4, 123, -4, 123 };
            HashSet<int> hashSet = new HashSet<int>(items);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        #endregion

        #region Constructor_IEqualityComparer

        //Test 2: Comparer is custom comparer
        [Fact]
        public static void HashSetConstructor_IEqualityComparer_Custom()
        {
            IEqualityComparer<Item> comparer = new ItemEqualityComparer();
            HashSet<Item> hashSet = new HashSet<Item>(comparer);
            Item[] items = new Item[0];
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items, comparer);
            driver.VerifyHashSetTests();
        }

        //Test 4: Comparer is EqualityComparer<T>.Default and T does not implement IEquatable<T>
        [Fact]
        public static void HashSetConstructor_IEqualityComparer_NotIEquatable()
        {
            IEqualityComparer<Item> defaultComparer = EqualityComparer<Item>.Default;
            HashSet<Item> hashSet = new HashSet<Item>(defaultComparer);
            Item[] items = new Item[0];
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items, defaultComparer);
        }

        #endregion

        #region Constructor_IEnumerable_IEqualityComparer

        //Test 2: Collection is empty
        [Fact]
        public static void HashSetConstructor_IEn_IEq_IEnumerable_Empty()
        {
            int[] items = new int[0];
            HashSet<int> hashSet = new HashSet<int>(items, EqualityComparer<int>.Default);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }

        //Test 3: Collection has one element
        [Fact]
        public static void HashSetConstructor_IEn_IEq_IEnumerable_Single()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { -23 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[] { -23 });
            driver.VerifyHashSetTests();
        }

        //Test 4: Collection has multiple unique elements
        [Fact]
        public static void HashSetConstructor_IEn_IEq_IEnumerable_Multiple()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { -23, -2, 4, 6, 0, 123, -4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[] { -23, -2, 4, 6, 0, 123, -4 });
            driver.VerifyHashSetTests();
        }
        [Fact]
        public static void HashSetConstructor_IEn_IEq_IEnumerable_Multiple_Negative()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { -23, -2, 4, 6, 0, 123, -4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[] { -23, -2, 4, 6, 0, 123, -4 });
            driver.VerifyHashSet_NegativeTests();
        }

        //Test 5: Collection has duplicate elements
        [Fact]
        public static void HashSetConstructor_IEn_IEq_IEnumerable_Duplicate()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 4, -23, -23, -2, 4, 6, 0, 4, 123, -4, 123 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[] { -23, -2, 4, 6, 0, 123, -4 });
            driver.VerifyHashSetTests();
        }

        //Test 6: Comparer is null
        [Fact]
        public static void HashSetConstructor_IEn_IEq_Comparer_Null()
        {
            IEqualityComparer<int> nullComparer = null;
            HashSet<int> hashSet = new HashSet<int>(new int[] { -2, -3, -4, 0, 5 }, nullComparer);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[] { -2, -3, -4, 0, 5 });
            driver.VerifyHashSetTests();
        }

        //Test 7: Comparer is custom comparer
        [Fact]
        public static void HashSetConstructor_IEn_IEq_Comparer_Custom()
        {
            Item[] items = new Item[] { new Item(1), new Item(2), new Item(3), new Item(4) };
            IEqualityComparer<Item> comparer = new ItemEqualityComparer();
            HashSet<Item> hashSet = new HashSet<Item>(items, comparer);
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items, comparer);
            driver.VerifyHashSetTests();
        }

        //Test 8: Comparer is EqualityComparer<T>.Default
        [Fact]
        public static void HashSetConstructor_IEn_IEq_Comparer_Default()
        {
            IEqualityComparer<ValueItem> defaultComparer = EqualityComparer<ValueItem>.Default;
            ValueItem[] items = new ValueItem[] { new ValueItem(1, -1), new ValueItem(2, -2), new ValueItem(3, -3), new ValueItem(4, -4) };
            HashSet<ValueItem> hashSet = new HashSet<ValueItem>(items, defaultComparer);
            HashSetTestSupport<ValueItem> driver = new HashSetTestSupport<ValueItem>(hashSet, ValueItem.GenerateNext, items, defaultComparer);
            driver.VerifyHashSetTests();
        }

        //Test 9: Comparer is EqualityComparer<T>.Default and T does not implement IEquatable<T>
        [Fact]
        public static void HashSetConstructor_IEn_IEq_Comparer_NotIEquatable()
        {
            IEqualityComparer<Item> defaultComparer = EqualityComparer<Item>.Default;
            Item[] items = new Item[] { new Item(1), new Item(2), new Item(3), new Item(4) };
            HashSet<Item> hashSet = new HashSet<Item>(items, defaultComparer);
            HashSetTestSupport<Item> driver = new HashSetTestSupport<Item>(hashSet, Item.GenerateNext, items, defaultComparer);
            driver.VerifyHashSetTests();
        }

        #endregion

        #region Constructor_IEnumerable / HashSet

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Empty()
        {
            var items = new int[0];
            HashSet<int> source = new HashSet<int>(items);

            HashSet<int> hashSet = new HashSet<int>(source);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Empty_Neg()
        {
            var items = new int[0];
            HashSet<int> source = new HashSet<int>(items);

            HashSet<int> hashSet = new HashSet<int>(source);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Multiple()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            HashSet<int> source = new HashSet<int>(items);

            HashSet<int> hashSet = new HashSet<int>(source);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Multiple_Neg()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            HashSet<int> source = new HashSet<int>(items);

            HashSet<int> hashSet = new HashSet<int>(source);
            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, items);
            driver.VerifyHashSet_NegativeTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Fresh_HashSet()
        {
            HashSet<int> source = new HashSet<int>();
            HashSet<int> hashSet = new HashSet<int>(source);

            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new int[0]);
            driver.VerifyHashSetTests();
        }

        private static IEnumerable<int> NonSquares(int limit)
        {
            for (int i = 0; i != limit; ++i)
            {
                int root = (int)Math.Sqrt(i);
                if (i != root * root)
                    yield return i;
            }
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_Sparse()
        {
            HashSet<int> source = new HashSet<int>();
            for (int i = 0; i != 1000; ++i)
            {
                source.Add(i);
            }

            foreach (int i in NonSquares(1000)) // Unevenly spaced survivors increases chance of catching any spacing-related bugs.
            {
                source.Remove(i);
            }

            HashSet<int> hashSet = new HashSet<int>(source);

            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new List<int>(source).ToArray());
            driver.VerifyHashSetTests();
        }

        [Fact]
        public static void Ctor_IEnumerable_HashSet_SparseManyCollisions()
        {
            HashSet<int> source = new HashSet<int>(new BadIntEqualityComparer());
            for (int i = 0; i != 1000; ++i)
            {
                source.Add(i);
            }

            foreach (int i in NonSquares(1000)) // Unevenly spaced survivors increases chance of catching any spacing-related bugs.
            {
                source.Remove(i);
            }

            HashSet<int> hashSet = new HashSet<int>(source, new BadIntEqualityComparer());

            HashSetTestSupport<int> driver = new HashSetTestSupport<int>(hashSet, s_intGenerator, new List<int>(source).ToArray(), new BadIntEqualityComparer());
            driver.VerifyHashSetTests();
        }
        #endregion
    }
}
