// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;

namespace System.Runtime.Tests
{
    public static class LazyOfTMetadataTests
    {
        [Fact]
        public static void TestCtor()
        {
            new Lazy<int, string>(null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lazy<int, string>("test", (LazyThreadSafetyMode)12345));
            Assert.Throws<ArgumentNullException>(() => new Lazy<int, string>(null, "test"));
            Assert.Throws<ArgumentNullException>(() => new Lazy<int, string>(null, "test", false));
            Assert.Throws<ArgumentNullException>(() => new Lazy<int, string>(null, "test", LazyThreadSafetyMode.PublicationOnly));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lazy<int, string>(() => 42, "test", (LazyThreadSafetyMode)12345));
        }

        [Fact]
        public static void TestMetadataProperty()
        {
            Assert.Equal("metadata1", new Lazy<int, string>("metadata1").Metadata);
            Assert.Equal("metadata2", new Lazy<int, string>("metadata2", false).Metadata);
            Assert.Equal("metadata3", new Lazy<int, string>("metadata3", LazyThreadSafetyMode.PublicationOnly).Metadata);
            Assert.Equal(4, new Lazy<object, int>(() => new object(), 4).Metadata);
            Assert.Equal(5, new Lazy<object, int>(() => new object(), 5, false).Metadata);
            Assert.Equal(6, new Lazy<object, int>(() => new object(), 6, LazyThreadSafetyMode.None).Metadata);
        }

        [Fact]
        public static void TestBasicInitialization()
        {
            Lazy<int, string> lazy;

            lazy = new Lazy<int, string>("test1");
            Assert.False(lazy.IsValueCreated);
            Assert.Equal("test1", lazy.Metadata);
            Assert.False(lazy.IsValueCreated);
            Assert.Equal(0, lazy.Value);
            Assert.True(lazy.IsValueCreated);

            lazy = new Lazy<int, string>(() => 42, "test2");
            Assert.False(lazy.IsValueCreated);
            Assert.Equal("test2", lazy.Metadata);
            Assert.False(lazy.IsValueCreated);
            Assert.Equal(42, lazy.Value);
            Assert.True(lazy.IsValueCreated);
        }
    }
}
