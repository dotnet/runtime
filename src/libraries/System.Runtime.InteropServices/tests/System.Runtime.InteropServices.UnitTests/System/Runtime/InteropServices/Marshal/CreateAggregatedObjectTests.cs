// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class CreateAggregatedObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateAggregatedObject_Generic_ReturnsExpected()
        {
            var original = new object();
            IntPtr ptr = Marshal.GetIUnknownForObject(original);
            try
            {
                int toAggregate = 1;
                IntPtr aggregatedObject1 = Marshal.CreateAggregatedObject(ptr, toAggregate);
                try
                {
                    Assert.NotEqual(IntPtr.Zero, aggregatedObject1);

                    IntPtr aggregatedObject2 = Marshal.CreateAggregatedObject(ptr, toAggregate);
                    Assert.NotEqual(IntPtr.Zero, aggregatedObject2);
                    Assert.NotEqual(aggregatedObject1, aggregatedObject2);
                }
                finally
                {
                    Marshal.Release(aggregatedObject1);
                }
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateAggregatedObject_NonGeneric_ReturnsExpected()
        {
            var original = new object();
            IntPtr ptr = Marshal.GetIUnknownForObject(original);
            try
            {
                int toAggregate = 1;
                IntPtr aggregatedObject1 = Marshal.CreateAggregatedObject(ptr, (object)toAggregate);
                try
                {
                    Assert.NotEqual(IntPtr.Zero, aggregatedObject1);

                    IntPtr aggregatedObject2 = Marshal.CreateAggregatedObject(ptr, (object)toAggregate);
                    Assert.NotEqual(IntPtr.Zero, aggregatedObject2);
                    Assert.NotEqual(aggregatedObject1, aggregatedObject2);
                }
                finally
                {
                    Marshal.Release(aggregatedObject1);
                }
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void CreateAggregatedObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateAggregatedObject(IntPtr.Zero, 1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateAggregateObject_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pOuter", () => Marshal.CreateAggregatedObject(IntPtr.Zero, 1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateAggregateObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.CreateAggregatedObject((IntPtr)1, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateAggregatedObject_AlreadyHasContainer_ThrowsArgumentException()
        {
            var o1 = new object();
            IntPtr ptr1 = Marshal.GetIUnknownForObject(o1);
            try
            {
                var o2 = new object();
                IntPtr ptr2 = Marshal.GetIUnknownForObject(o2);
                try
                {
                    AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateAggregatedObject(ptr1, o1));
                    AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateAggregatedObject(ptr1, o2));
                }
                finally
                {
                    Marshal.Release(ptr2);
                }
            }
            finally
            {
                Marshal.Release(ptr1);
            }
        }
    }
}
