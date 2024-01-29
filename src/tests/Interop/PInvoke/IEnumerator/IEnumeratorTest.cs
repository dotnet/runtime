// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace PInvokeTests
{
    static class IEnumeratorNative
    {
        [DllImport(nameof(IEnumeratorNative), PreserveSig = false)]
        public static extern IEnumerator GetIntegerEnumerator(
            int start,
            int count);

        [DllImport(nameof(IEnumeratorNative), PreserveSig = false)]
        public static extern IEnumerable GetIntegerEnumeration(
            int start,
            int count);

        [DllImport(nameof(IEnumeratorNative), PreserveSig = false)]
        public static extern void VerifyIntegerEnumerator(
            IEnumerator enumerator,
            int start,
            int count);

        [DllImport(nameof(IEnumeratorNative), PreserveSig = false)]
        public static extern void VerifyIntegerEnumeration(
            IEnumerable enumerable,
            int start,
            int count);

        [DllImport(nameof(IEnumeratorNative), PreserveSig = false)]
        public static extern IEnumerator PassThroughEnumerator(IEnumerator enumerator);
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
    [SkipOnMono("PInvoke IEnumerator/IEnumerable marshalling not supported on Mono")]
    public static class IEnumeratorTests
    {
        [Fact]
        public static void TestNativeToManaged()
        {
            AssertExtensions.CollectionEqual(Enumerable.Range(1, 10), EnumeratorAsEnumerable(IEnumeratorNative.GetIntegerEnumerator(1, 10)));
            AssertExtensions.CollectionEqual(Enumerable.Range(1, 10), IEnumeratorNative.GetIntegerEnumeration(1, 10).OfType<int>());
        }

        [Fact]
        public static void TestManagedToNative()
        {
            IEnumeratorNative.VerifyIntegerEnumerator(Enumerable.Range(1, 10).GetEnumerator(), 1, 10);
            IEnumeratorNative.VerifyIntegerEnumeration(Enumerable.Range(1, 10), 1, 10);
        }

        [Fact]
        public static void TestNativeRoundTrip()
        {
            IEnumerator nativeEnumerator = IEnumeratorNative.GetIntegerEnumerator(1, 10);
            Assert.Equal(nativeEnumerator, IEnumeratorNative.PassThroughEnumerator(nativeEnumerator));
        }

        [Fact]
        public static void TestManagedRoundTrip()
        {
            IEnumerator managedEnumerator = Enumerable.Range(1, 10).GetEnumerator();
            Assert.Equal(managedEnumerator, IEnumeratorNative.PassThroughEnumerator(managedEnumerator));
        }

        private static IEnumerable<int> EnumeratorAsEnumerable(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return (int)enumerator.Current;
            }
        }
    }
}
