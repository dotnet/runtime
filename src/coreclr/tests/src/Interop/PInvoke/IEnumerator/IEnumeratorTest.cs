// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TestLibrary;

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

    public static class IEnumeratorTests
    {
        private static void TestNativeToManaged()
        {
            Assert.AreAllEqual(Enumerable.Range(1, 10), EnumeratorAsEnumerable(IEnumeratorNative.GetIntegerEnumerator(1, 10)));
            Assert.AreAllEqual(Enumerable.Range(1, 10), IEnumeratorNative.GetIntegerEnumeration(1, 10).OfType<int>());
        }

        private static void TestManagedToNative()
        {
            IEnumeratorNative.VerifyIntegerEnumerator(Enumerable.Range(1, 10).GetEnumerator(), 1, 10);
            IEnumeratorNative.VerifyIntegerEnumeration(Enumerable.Range(1, 10), 1, 10);
        }

        private static void TestNativeRoundTrip()
        {
            IEnumerator nativeEnumerator = IEnumeratorNative.GetIntegerEnumerator(1, 10);
            Assert.AreEqual(nativeEnumerator, IEnumeratorNative.PassThroughEnumerator(nativeEnumerator));
        }

        private static void TestManagedRoundTrip()
        {
            IEnumerator managedEnumerator = Enumerable.Range(1, 10).GetEnumerator();
            Assert.AreEqual(managedEnumerator, IEnumeratorNative.PassThroughEnumerator(managedEnumerator));
        }

        public static int Main()
        {
            try
            {
                TestNativeToManaged();
                TestManagedToNative();
                TestNativeRoundTrip();
                TestManagedRoundTrip();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
                return 101;
            }

            return 100;
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
