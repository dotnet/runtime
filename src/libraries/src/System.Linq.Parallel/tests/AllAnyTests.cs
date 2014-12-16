// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class AllAnyTests
    {
        //
        // Any and All
        //

        [Fact]
        public static void RunAnyTests()
        {
            RunAnyTest_AllFalse(1024);
            RunAnyTest_AllTrue(1024);
            RunAnyTest_OneTrue(1024, 512);
            RunAnyTest_OneTrue(1024, 0);
            RunAnyTest_OneTrue(1024, 1023);
            RunAnyTest_OneTrue(1024 * 1024, 1024 * 512);
        }

        [Fact]
        public static void RunAllTests()
        {
            RunAllTest_AllFalse(1024);
            RunAllTest_AllTrue(1024);
            RunAllTest_OneFalse(1024, 512);
            RunAllTest_OneFalse(1024, 0);
            RunAllTest_OneFalse(1024, 1023);
            RunAllTest_OneFalse(1024 * 1024, 1024 * 512);
        }

        private static void RunAnyTest_AllFalse(int size)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;

            bool expect = false;
            bool result = bools.AsParallel().Any(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAnyTest_AllFalse(size={2}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunAnyTest_AllTrue(int size)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;

            bool expect = true;
            bool result = bools.AsParallel().Any(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAnyTest_AllTrue(size={2}  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunAnyTest_OneTrue(int size, int truePosition)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;
            bools[truePosition] = true;

            bool expect = true;
            bool result = bools.AsParallel().Any(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAnyTest_OneTrue(size={2}, truePosition={3}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size, truePosition));
        }

        //
        // Tests the Any() operator applied to infinite enumerables
        //
        [Fact]
        public static void RunAnyTest_Infinite()
        {
            if (!InfiniteEnumerable().AsParallel().Select(x => -x).Any())
            {
                Assert.True(false, string.Format("RunAnyTest_Infinite:  FAILED.  > Expected true return value."));
            }

            if (!InfiniteEnumerable().AsParallel().Select(x => -x).Any(x => true))
            {
                Assert.True(false, string.Format("RunAnyTest_Infinite:  FAILED.  > Expected true return value."));
            }
        }

        private static void RunAllTest_AllFalse(int size)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;

            bool expect = false;
            bool result = bools.AsParallel().All(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAllTest_AllFalse(size={2}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunAllTest_AllTrue(int size)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;

            bool expect = true;
            bool result = bools.AsParallel().All(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAllTest_AllTrue(size={2}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunAllTest_OneFalse(int size, int falsePosition)
        {
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;
            bools[falsePosition] = false;

            bool expect = false;
            bool result = bools.AsParallel().All(delegate (bool b) { return b; });

            if (result != expect)
                Assert.True(false, string.Format("RunAllTest_OneFalse(size={2}, falsePosition={3}): FAILED.  > Expect: {0}, real: {1}", expect, result, size, falsePosition));
        }

        private static IEnumerable<int> InfiniteEnumerable()
        {
            while (true) yield return 0;
        }
    }
}
