// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class EventIdTest
    {
        [Fact]
        public void Equality_operations()
        {
            Assert.True(new EventId(1).Equals(new EventId(1)));
            Assert.True(new EventId(1).Equals((object)new EventId(1)));
            Assert.True(new EventId(1).Equals(new EventId(1, "Foo")));
            Assert.True(new EventId(1, "Bar").Equals(new EventId(1, "Foo")));

            Assert.False(new EventId(1).Equals(new EventId(2)));
            Assert.False(new EventId(1).Equals(null));
            Assert.False(new EventId(1, "Foo").Equals(new EventId(2, "Foo")));

            Assert.True(new EventId(1) == new EventId(1));
            Assert.True(new EventId(1) == new EventId(1, "Foo"));
            Assert.True(new EventId(1, "Bar") == new EventId(1, "Foo"));

            Assert.True(new EventId(1) != new EventId(2));
            Assert.True(new EventId(1, "Foo") != new EventId(2, "Foo"));

            Assert.True(new EventId(1).GetHashCode() == new EventId(1).GetHashCode());
            Assert.True(new EventId(1).GetHashCode() == new EventId(1, "Foo").GetHashCode());
            Assert.True(new EventId(1, "Bar").GetHashCode() == new EventId(1, "Foo").GetHashCode());

            Assert.True(new EventId(1).GetHashCode() != new EventId(2).GetHashCode());
            Assert.True(new EventId(1, "Foo").GetHashCode() != new EventId(2, "Foo").GetHashCode());
        }
    }
}
