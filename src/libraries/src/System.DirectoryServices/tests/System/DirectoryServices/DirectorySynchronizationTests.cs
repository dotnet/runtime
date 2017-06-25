// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class DirectorySynchronizationTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var synchronization = new DirectorySynchronization();
            Assert.Equal(DirectorySynchronizationOptions.None, synchronization.Option);

            Assert.Empty(synchronization.GetDirectorySynchronizationCookie());
        }

        [Theory]
        [InlineData(DirectorySynchronizationOptions.None)]
        [InlineData(DirectorySynchronizationOptions.IncrementalValues | DirectorySynchronizationOptions.ObjectSecurity)]
        public void Ctor_Option(DirectorySynchronizationOptions option)
        {
            var synchronization = new DirectorySynchronization(option);
            Assert.Equal(option, synchronization.Option);

            Assert.Empty(synchronization.GetDirectorySynchronizationCookie());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 1, 2, 3, })]
        public void Ctor_Cookie(byte[] cookie)
        {
            var synchronization = new DirectorySynchronization(cookie);
            Assert.Equal(DirectorySynchronizationOptions.None, synchronization.Option);

            byte[] synchronizationCookie = synchronization.GetDirectorySynchronizationCookie();
            Assert.NotSame(synchronizationCookie, cookie);
            Assert.Equal(cookie ?? Array.Empty<byte>(), synchronizationCookie);
        }

        [Theory]
        [InlineData(DirectorySynchronizationOptions.None, null)]
        [InlineData(DirectorySynchronizationOptions.IncrementalValues, new byte[0])]
        [InlineData(DirectorySynchronizationOptions.IncrementalValues | DirectorySynchronizationOptions.ObjectSecurity, new byte[] { 1, 2, 3 })]
        public void Ctor_Option_Cookie(DirectorySynchronizationOptions option, byte[] cookie)
        {
            var synchronization = new DirectorySynchronization(option, cookie);
            Assert.Equal(option, synchronization.Option);

            byte[] synchronizationCookie = synchronization.GetDirectorySynchronizationCookie();
            Assert.NotSame(synchronizationCookie, cookie);
            Assert.Equal(cookie ?? Array.Empty<byte>(), synchronizationCookie);
        }

        [Theory]
        [InlineData((DirectorySynchronizationOptions)(-1))]
        [InlineData((DirectorySynchronizationOptions)int.MaxValue)]
        public void Ctor_InvalidOption_ThrowsInvalidEnumArgumentException(DirectorySynchronizationOptions options)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new DirectorySynchronization(options));
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new DirectorySynchronization(options, new byte[0]));
        }

        public static IEnumerable<object[]> Ctor_Synchronization_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new DirectorySynchronization(DirectorySynchronizationOptions.ObjectSecurity, new byte[] { 1, 2, 3 }) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Synchronization_TestData))]
        public void Ctor_Synchronization(DirectorySynchronization otherSynchronization)
        {
            var synchronization = new DirectorySynchronization(otherSynchronization);
            Assert.Equal(otherSynchronization?.Option ?? DirectorySynchronizationOptions.None, synchronization.Option);

            Assert.Equal(otherSynchronization?.GetDirectorySynchronizationCookie() ?? Array.Empty<byte>(), synchronization.GetDirectorySynchronizationCookie());
        }

        [Fact]
        public void Copy_Invoke_ReturnsExpected()
        {
            var synchronization = new DirectorySynchronization(DirectorySynchronizationOptions.ObjectSecurity, new byte[] { 1, 2, 3 });
            DirectorySynchronization copy = synchronization.Copy();

            Assert.NotSame(synchronization, copy);
            Assert.Equal(DirectorySynchronizationOptions.ObjectSecurity, synchronization.Option);
            Assert.Equal(new byte[] { 1, 2, 3 }, synchronization.GetDirectorySynchronizationCookie());
        }

        [Fact]
        public void ResetDirectorySynchronizationCookie_Parameterless_SetsToEmpty()
        {
            var synchronization = new DirectorySynchronization(new byte[] { 1, 2, 3 });
            synchronization.ResetDirectorySynchronizationCookie();

            Assert.Empty(synchronization.GetDirectorySynchronizationCookie());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 1, 2, 3 })]
        public void ResetDirectorySynchronizationCookie_Cookie_SetsToEmpty(byte[] cookie)
        {
            var synchronization = new DirectorySynchronization(new byte[] { 255, 255, 255 });
            synchronization.ResetDirectorySynchronizationCookie(cookie);

            Assert.Equal(cookie ?? Array.Empty<byte>(), synchronization.GetDirectorySynchronizationCookie());
        }

        [Fact]
        public void ResetDirectorySynchronizationCookie_Cookie_MakesCopyOfCookie()
        {
            var cookie = new byte[] { 1, 2, 3 };
            var synchronization = new DirectorySynchronization();

            synchronization.ResetDirectorySynchronizationCookie(cookie);
            cookie[0] = 20;

            Assert.Equal(new byte[] { 1, 2, 3 }, synchronization.GetDirectorySynchronizationCookie());
        }
    }
}
