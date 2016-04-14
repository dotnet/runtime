// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class GroupCollectionTests
    {
        [Fact]
        public static void Groups_Get_NeverReturnsEmptyCollection()
        {
            string inputWithoutGroup = "Today is a great day for coding.";
            string inputWithGroup = "I had had an accident.";
            Regex regex = new Regex(@"(\w+)\s(\1)");
            Match matchPos = regex.Match(inputWithGroup);
            Match matchNeg = regex.Match(inputWithoutGroup);

            //test if Success returns true with group(s) present
            Assert.True(matchPos.Groups[0].Success);

            //public [] operator calling internal GetGroup()
            //should return internal _emptygroup thus making
            //call to Success() false
            Assert.False(matchNeg.Groups[0].Success);
        }

        [Fact]
        public static void GetEnumerator()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            GroupCollection groups = match.Groups;
            IEnumerator enumerator = groups.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(groups[counter], enumerator.Current);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(groups.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");
            IEnumerator enumerator = match.Groups.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public static void Item_Invalid(int groupNumber)
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;

            Group group = groups[groupNumber];
            Assert.Same(string.Empty, group.Value);
            Assert.Equal(0, group.Index);
            Assert.Equal(0, group.Length);
            Assert.Equal(0, group.Captures.Count);
        }

        [Fact]
        public void ICollection_Properties()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;
            ICollection collection = groups;

            Assert.False(collection.IsSynchronized);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void ICollection_CopyTo(int index)
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;
            ICollection collection = groups;

            Group[] copy = new Group[collection.Count + index];
            collection.CopyTo(copy, index);
            for (int i = 0; i < index; i++)
            {
                Assert.Null(copy[i]);
            }
            for (int i = index; i < copy.Length; i++)
            {
                Assert.Same(groups[i - index], copy[i]);
            }
        }

        [Fact]
        public void ICollection_CopyTo_NullArray_ThrowsArgumentNullException()
        {
            Regex regex = new Regex("e");
            ICollection collection = regex.Match("aaabbccccccccccaaaabc").Groups;

            Assert.Throws<ArgumentNullException>("array", () => collection.CopyTo(null, 0));
        }
    }
}
