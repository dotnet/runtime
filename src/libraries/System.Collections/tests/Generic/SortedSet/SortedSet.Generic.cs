// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class SortedSet_Generic_Tests_string : SortedSet_Generic_Tests<string>
    {
        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class SortedSet_Generic_Tests_int : SortedSet_Generic_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override bool DefaultValueAllowed => true;

        [Fact]
        public void SortedSet_Generic_GetViewBetween_MinMax()
        {
            var set = (SortedSet<int>)CreateSortedSet(new[] { 1, 3, 5, 7, 9 }, 5, 5);
            SortedSet<int> view = set.GetViewBetween(4, 8);

            Assert.True(set.Contains(1));
            Assert.True(set.Contains(3));
            Assert.True(set.Contains(5));
            Assert.True(set.Contains(7));
            Assert.True(set.Contains(9));

            Assert.False(view.Contains(1));
            Assert.False(view.Contains(3));
            Assert.True(view.Contains(5));
            Assert.True(view.Contains(7));
            Assert.False(view.Contains(9));

            Assert.Equal(1, set.Min);
            Assert.Equal(9, set.Max);

            Assert.Equal(5, view.Min);
            Assert.Equal(7, view.Max);
        }

        [Fact]
        public void SortedSet_Generic_IntersectWith_SupersetEnumerableWithDups()
        {
            var set = (SortedSet<int>)CreateSortedSet(new[] { 1, 3, 5, 7, 9 }, 5, 5);
            set.IntersectWith(new[] { 5, 7, 3, 7, 11, 7, 5, 2 });

            Assert.Equal(new[] { 3, 5, 7 }, set);
        }

        [Fact]
        public void SortedSet_Generic_GetViewBetween_MinMax_Exhaustive()
        {
            var set = (SortedSet<int>)CreateSortedSet(new[] { 7, 11, 3, 1, 5, 9, 13 }, 7, 7);
            for (int i = 0; i < 14; i++)
            {
                for (int j = i; j < 14; j ++)
                {
                    SortedSet<int> view = set.GetViewBetween(i, j);

                    if (j < i || (j == i && i % 2 == 0) )
                    {
                        Assert.Equal(default(int), view.Min);
                        Assert.Equal(default(int), view.Max);
                    }
                    else
                    {
                        Assert.Equal(i + ((i+1) % 2), view.Min);
                        Assert.Equal(j - ((j+1) % 2), view.Max);
                    }
                }
            }
        }
    }

    public class SortedSet_Generic_Tests_int_With_NullComparer : SortedSet_Generic_Tests_int
    {
        protected override IComparer<int> GetIComparer() => null;
    }

    [OuterLoop]
    public class SortedSet_Generic_Tests_EquatableBackwardsOrder : SortedSet_Generic_Tests<EquatableBackwardsOrder>
    {
        protected override EquatableBackwardsOrder CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new EquatableBackwardsOrder(rand.Next());
        }

        protected override ISet<EquatableBackwardsOrder> GenericISetFactory()
        {
            return new SortedSet<EquatableBackwardsOrder>();
        }
    }

    [OuterLoop]
    public class SortedSet_Generic_Tests_int_With_Comparer_SameAsDefaultComparer : SortedSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_SameAsDefaultComparer();
        }

        protected override IComparer<int> GetIComparer()
        {
            return new Comparer_SameAsDefaultComparer();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SortedSet<int>(new Comparer_SameAsDefaultComparer());
        }

        [Fact]
        public void SortedSet_Generic_GetViewBetween_MinMax_WithCustomComparer()
        {
            var set = (SortedSet<int>)CreateSortedSet(new[] { 5, 15, 25, 35, 45 }, 5, 5);

            for (int i = 0; i <= 40; i += 10)
            {
                for (int j = i + 10; j <= 50; j += 10)
                {
                    SortedSet<int> view = set.GetViewBetween(i, j);

                    Assert.Equal(i + 5, view.Min);
                    Assert.Equal(j - 5, view.Max);
                }
            }
        }
    }

    [OuterLoop]
    public class SortedSet_Generic_Tests_int_With_Comparer_HashCodeAlwaysReturnsZero : SortedSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_HashCodeAlwaysReturnsZero();
        }

        protected override IComparer<int> GetIComparer()
        {
            return new Comparer_HashCodeAlwaysReturnsZero();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SortedSet<int>(new Comparer_HashCodeAlwaysReturnsZero());
        }
    }

    [OuterLoop]
    public class SortedSet_Generic_Tests_int_With_Comparer_ModOfInt : SortedSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_ModOfInt(15000);
        }

        protected override IComparer<int> GetIComparer()
        {
            return new Comparer_ModOfInt(15000);
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SortedSet<int>(new Comparer_ModOfInt(15000));
        }
    }

    [OuterLoop]
    public class SortedSet_Generic_Tests_int_With_Comparer_AbsOfInt : SortedSet_Generic_Tests<int>
    {
        protected override IEqualityComparer<int> GetIEqualityComparer()
        {
            return new Comparer_AbsOfInt();
        }

        protected override IComparer<int> GetIComparer()
        {
            return new Comparer_AbsOfInt();
        }

        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override ISet<int> GenericISetFactory()
        {
            return new SortedSet<int>(new Comparer_AbsOfInt());
        }
    }
}
