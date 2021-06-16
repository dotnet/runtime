// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Tests
{
    // NOTE: DependentHandle is already heavily tested indirectly through ConditionalWeakTable<,>.
    // This class contains some specific tests for APIs that are only relevant when used directly.
    public class DependentHandleTests
    {
        [Fact]
        public void GetTarget_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(DependentHandle).Target);
        }

        [Fact]
        public void SetTarget_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DependentHandle handle = default;
                handle.Target = new();
            });
        }

        [Fact]
        public void GetDependent_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(DependentHandle).Dependent);
        }

        [Fact]
        public void SetDependent_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DependentHandle handle = default;
                handle.Dependent = new();
            });
        }

        [Fact]
        public void GetTargetAndDependent_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(DependentHandle).GetTargetAndDependent());
        }

        [Fact]
        public void Dispose_RepeatedCallsAreFine()
        {
            object key = new(), value = new();
            DependentHandle handle = new(key, value);

            Assert.True(handle.IsAllocated);

            handle.Dispose();

            Assert.False(handle.IsAllocated);

            handle.Dispose();

            Assert.False(handle.IsAllocated);

            handle.Dispose();
            handle.Dispose();
            handle.Dispose();

            Assert.False(handle.IsAllocated);
        }

        [Fact]
        public void Dispose_ValidOnDefault()
        {
            DependentHandle handle = default;
            Assert.False(handle.IsAllocated);
            handle.Dispose();
        }
    }
}
