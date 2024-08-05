// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Dynamic.Runtime.Tests
{
    public class CallInfoTests
    {
        [Fact]
        public void NullNames()
        {
            AssertExtensions.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(string[])));
            AssertExtensions.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(IEnumerable<string>)));
        }

        [Fact]
        public void ArgCountTooLow()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CallInfo(1, "a", "b"));
        }

        [Fact]
        public void NullName()
        {
            AssertExtensions.Throws<ArgumentNullException>("argNames[1]", () => new CallInfo(3, "a", null, "c"));
        }

        [Fact]
        public void ArgCountRetained()
        {
            Assert.Equal(0, new CallInfo(0).ArgumentCount);
            Assert.Equal(4, new CallInfo(4).ArgumentCount);
            Assert.Equal(3, new CallInfo(3, "a", "b", "c").ArgumentCount);
        }

        [Fact]
        public void ArgNamesRetained()
        {
            Assert.Equal(new[] { "a", "b", "c" }, new CallInfo(3, "a", "b", "c").ArgumentNames);
            // Having a singleton for empty name lists isn't required by the spec,
            // (any empty ReadOnlyCollection<string> will fulfill that),
            // but consider the impact (possibly breaking, possibly less performant)
            // before changing this.
            Assert.Same(new CallInfo(0).ArgumentNames, new CallInfo(1).ArgumentNames);
        }

        [Fact]
        public void Equality()
        {
            CallInfo a = new CallInfo(5, "a", "b");
            CallInfo b = new CallInfo(5, "a", "b");
            CallInfo c = new CallInfo(5, a.ArgumentNames);
            CallInfo d = new CallInfo(5, "b", "a");
            CallInfo e = new CallInfo(4, "a", "b");
            CallInfo f = new CallInfo(5, "a");
            CallInfo x = new CallInfo(3);
            CallInfo y = new CallInfo(3);
            CallInfo z = new CallInfo(2);

            Assert.Equal(a, a);
            Assert.Equal(a, b);
            Assert.Equal(a, c);
            Assert.NotEqual(a, d);
            Assert.NotEqual(a, e);
            Assert.NotEqual(a, f);
            Assert.Equal(x, y);
            Assert.NotEqual(x, z);

            var dict = new Dictionary<CallInfo, int> { { a, 1 }, { x, 2 } };

            Assert.Equal(1, dict[a]);
            Assert.Equal(1, dict[b]);
            Assert.Equal(1, dict[c]);
            Assert.False(dict.ContainsKey(d));
            Assert.False(dict.ContainsKey(e));
            Assert.False(dict.ContainsKey(f));
            Assert.Equal(2, dict[y]);
            Assert.False(dict.ContainsKey(z));
        }
    }
}
