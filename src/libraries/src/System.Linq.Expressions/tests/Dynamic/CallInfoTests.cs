// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Dynamic.Tests
{
    public class CallInfoTests
    {
        [Fact]
        public void NullNames()
        {
            Assert.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(IEnumerable<string>)));
            Assert.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(string[])));
        }

        [Fact]
        public void NegativeCount()
        {
            Assert.Throws<ArgumentException>(null, () => new CallInfo(-1));
        }

        [Fact]
        public void CountLessThanNameCount()
        {
            Assert.Throws<ArgumentException>(null, () => new CallInfo(2, "foo", "bar", "baz", "quux", "quuux"));
        }

        [Fact]
        public void NullItem()
        {
            Assert.Throws<ArgumentNullException>("argNames[1]", () => new CallInfo(3, "foo", null, "bar"));
            Assert.Throws<ArgumentNullException>(
                "argNames[0]", () => new CallInfo(3, Enumerable.Repeat(default(string), 2)));
        }

        [Fact]
        public void ArgumentCountMatches()
        {
            for (int i = 0; i != 10; ++i)
            {
                var info = new CallInfo(i);
                Assert.Equal(i, info.ArgumentCount);
            }
        }

        [Fact]
        public void ArgumentNamesMatch()
        {
            var info = new CallInfo(5, "foo", "bar", "baz", "quux", "quuux");
            Assert.Equal(new[] {"foo", "bar", "baz", "quux", "quuux"}, info.ArgumentNames);
        }

        [Fact]
        public void EqualityBasedOnNamesAndCount()
        {
            var names = new[] {"foo", "bar", "baz", "quux", "quuux"};
            for (int i = 0; i <= names.Length; ++i)
            {
                for (int j = 0; j != 3; ++j)
                {
                    for (int x = 0; x <= names.Length; ++x)
                    {
                        for (int y = 0; y != 3; ++y)
                        {
                            var info0 = new CallInfo(i + j, names.Take(i));
                            var info1 = new CallInfo(x + y, names.Take(x));
                            Assert.Equal(i == x & j == y, info0.Equals(info1));
                        }
                    }
                }
            }
        }

        [Fact]
        public void HashCodeBasedOnEquality()
        {
            var names = new[] {"foo", "bar", "baz", "quux", "quuux"};
            for (int i = 0; i <= names.Length; ++i)
            {
                for (int j = 0; j != 3; ++j)
                {
                    for (int x = 0; x <= names.Length; ++x)
                    {
                        for (int y = 0; y != 3; ++y)
                        {
                            var info0 = new CallInfo(i + j, names.Take(i));
                            var info1 = new CallInfo(x + y, names.Take(x));
                            if (info0.Equals(info1))
                            {
                                Assert.Equal(info0.GetHashCode(), info1.GetHashCode());
                            }
                            else
                            {
                                // Failure at this point is not definitely a bug,
                                // but should be considered a concern unless it can be
                                // convincingly ruled a fluke.
                                Assert.NotEqual(info0.GetHashCode(), info1.GetHashCode());
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void NotEqualToNonCallInfo()
        {
            var info = new CallInfo(0);
            Assert.False(info.Equals(null));
            Assert.False(info.Equals("CallInfo"));
            Assert.False(info.Equals(23));
        }

        [Fact]
        public void FreshCopyOfNamesMadeEnumerable()
        {
            List<string> nameList = new List<string> {"foo", "bar"};
            var nameReadOnly = nameList.AsReadOnly();
            var info = new CallInfo(2, nameReadOnly);
            nameList[0] = "baz";
            nameList[1] = "qux";
            Assert.Equal(new[] {"foo", "bar"}, info.ArgumentNames);
        }

        [Fact]
        public void FreshCopyOfNamesMadeArray()
        {
            string[] nameArray = {"foo", "bar"};
            var nameReadOnly = new ReadOnlyCollection<string>(nameArray);
            var info = new CallInfo(2, nameReadOnly);
            nameArray[0] = "baz";
            nameArray[1] = "qux";
            Assert.Equal(new[] {"foo", "bar"}, info.ArgumentNames);
        }
    }
}
