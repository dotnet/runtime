// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.IsolatedStorage
{
    // Test default IsolatedStorage base class behaviors
    public class IsolatedStorageBaseClassTests : IsoStorageTest
    {
        private class TestStorage : IsolatedStorage
        {
            public override void Remove()
            {
                throw new NotImplementedException();
            }

            public char TestSeparatorExternal { get { return SeparatorExternal; } }
            public char TestSeparatorInternal { get { return SeparatorInternal; } }
        }

        [Fact]
        public void CurrentSizeThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
#pragma warning disable CS0618
            Assert.Throws<InvalidOperationException>(() => storage.CurrentSize);
#pragma warning restore CS0618
        }

        [Fact]
        public void UsedSizeThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
            Assert.Throws<InvalidOperationException>(() => storage.UsedSize);
        }

        [Fact]
        public void AvailableFreeSpaceThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
            Assert.Throws<InvalidOperationException>(() => storage.AvailableFreeSpace);
        }

        [Fact]
        public void MaximumSizeThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
#pragma warning disable CS0618
            Assert.Throws<InvalidOperationException>(() => storage.MaximumSize);
#pragma warning restore CS0618
        }

        [Fact]
        public void QuotaThrowsInvalidOperation()
        {
            TestStorage storage = new TestStorage();
            Assert.Throws<InvalidOperationException>(() => storage.Quota);
        }

        [Fact]
        public void SeparatorExternalIsDirectorySeparator()
        {
            TestStorage storage = new TestStorage();
            Assert.Equal(Path.DirectorySeparatorChar, storage.TestSeparatorExternal);
        }

        [Fact]
        public void SeparatorInternalIsPeriod()
        {
            TestStorage storage = new TestStorage();
            Assert.Equal('.', storage.TestSeparatorInternal);
        }

        [Fact]
        public void IncreaseQuotaToReturnsFalse()
        {
            TestStorage storage = new TestStorage();
            Assert.False(storage.IncreaseQuotaTo(10));
        }
    }
}
