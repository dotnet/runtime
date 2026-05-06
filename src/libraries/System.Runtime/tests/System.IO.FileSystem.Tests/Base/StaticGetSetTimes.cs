// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public abstract class StaticGetSetTimes : BaseGetSetTimes<string>
    {
        protected override string GetMissingItem() => GetTestFilePath();

        protected override string GetItemPath(string item) => item;

        [Fact]
        public void NullPath_ThrowsArgumentNullException()
        {
            Assert.All(TimeFunctions(), (item) =>
            {
                Assert.Throws<ArgumentNullException>(() => item.Setter(null, DateTime.Today));
                Assert.Throws<ArgumentNullException>(() => item.Getter(null));
            });
        }

        [Fact]
        public void EmptyPath_ThrowsArgumentException()
        {
            Assert.All(TimeFunctions(), (item) =>
            {
                Assert.Throws<ArgumentException>(() => item.Setter(string.Empty, DateTime.Today));
                Assert.Throws<ArgumentException>(() => item.Getter(string.Empty));
            });
        }
    }
}
