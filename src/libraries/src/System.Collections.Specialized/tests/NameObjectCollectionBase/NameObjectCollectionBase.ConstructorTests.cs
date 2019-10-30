// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseConstructorTests
    {
        [Fact]
        public void Constructor_Provider_Comparer()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MyNameObjectCollection coll = new MyNameObjectCollection(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
#pragma warning restore CS0618 // Type or member is obsolete
            coll.Add(null, null);
            Assert.Equal(1, coll.Count);
            coll.Remove(null);
            Assert.Equal(0, coll.Count);
        }

        [Fact]
        public void Constructor_Int_Provider_Comparer()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MyNameObjectCollection coll = new MyNameObjectCollection(5, CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
#pragma warning restore CS0618 // Type or member is obsolete
            coll.Add("a", new Foo("1"));
            int i = 0;
            IEnumerator e = coll.GetEnumerator();
            while (e.MoveNext())
            {
                i++;
            }
            Assert.Equal(1, i);
        }

    }
}
