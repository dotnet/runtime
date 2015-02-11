// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class EqualityComparerTests
    {
        [Fact]
        public void TestEqualityComparerBasic()
        {
            MyHashtable hsh1;
            IEqualityComparer ikc;

            //[] Default ctor
            hsh1 = new MyHashtable();
            Assert.Null(hsh1.EqualityComparer);

            //[] ctor(IKeyComparer)
            System.Globalization.CultureInfo prevCulture = System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("de-DE");
            ikc = StringComparer.CurrentCulture;
            hsh1 = new MyHashtable(0, 1.0f, ikc);

            Assert.Equal(ikc, hsh1.EqualityComparer);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = prevCulture;
        }
    }

    public class MyHashtable : Hashtable
    {
        public MyHashtable() : base() { }
        public MyHashtable(int capacity, float loadFactor, IEqualityComparer ikc) : base(capacity, loadFactor, ikc) { }

        public new IEqualityComparer EqualityComparer
        {
            get
            {
                return base.EqualityComparer;
            }
        }
    }
}
