// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class MovableTests
    {
        [Fact]
        public static void TestMovable()
        {
            MockDisposable mockDisposable = new MockDisposable();
            AssertExtensions.Throws<ArgumentNullException>("nullableType", () => new Movable<MockDisposable>(null));
            using (Movable<MockDisposable> movableMock = mockDisposable)
            {
                Assert.True(movableMock.HasValue);
                Assert.Equal(mockDisposable, movableMock.Value);
                Assert.Equal(mockDisposable, (MockDisposable)movableMock);
                Assert.True(mockDisposable.timesOfDisposing == 0);
                Assert.Equal(mockDisposable, movableMock.Move());
                Assert.False(movableMock.HasValue);
                Assert.True(mockDisposable.timesOfDisposing == 0);
                AssertExtensions.Throws<InvalidOperationException>("invalidResource", () => movableMock.Move());
                AssertExtensions.Throws<InvalidOperationException>("invalidResource", () => _ = movableMock.Value);
            }

            Assert.True(mockDisposable.timesOfDisposing == 0);
            new Movable<MockDisposable>(mockDisposable).Dispose();
            Assert.True(mockDisposable.timesOfDisposing == 1);
            Assert.True(false);
        }

        private class MockDisposable : IDisposable
        {
            internal int timesOfDisposing = 0;

            public void Dispose()
            {
                ++timesOfDisposing;
            }
        }
    }
}
