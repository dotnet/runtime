// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected override bool DefaultValueAllowed
        {
            get
            {
                return true;
            }
        }
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
