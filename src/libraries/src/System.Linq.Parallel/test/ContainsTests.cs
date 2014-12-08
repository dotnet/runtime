// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;

namespace Test
{
    public class ContainsTests
    {
        //
        // Contains
        //
        [Fact]
        public static void RunContainsTests()
        {
            RunContainsTest_NoMatching(1024);
            RunContainsTest_AllMatching(1024);
            RunContainsTest_OneMatching(1024, 512);
            RunContainsTest_OneMatching(1024, 0);
            RunContainsTest_OneMatching(1024, 1023);
            RunContainsTest_OneMatching(1024 * 1024, 1024 * 512);
        }

        private static void RunContainsTest_NoMatching(int size)
        {
            int toFind = 103372;
            //Random r = new Random(33);
            int[] data = new int[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = i;
                if (data[i] == toFind)
                    data[i] += 1;
            }

            bool expect = false;
            bool result = data.AsParallel().Contains(toFind);

            if (result != expect)
                Assert.True(false, string.Format("RunContainsTest_NoMatching(size={2}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunContainsTest_AllMatching(int size)
        {
            int toFind = 103372;
            int[] data = new int[size];
            for (int i = 0; i < size; i++)
                data[i] = 103372;

            bool expect = true;
            bool result = data.AsParallel().Contains(toFind);
            if (result != expect)
                Assert.True(false, string.Format("RunContainsTest_AllMatching(size={2}):  FAILED.  > Expect: {0}, real: {1}", expect, result, size));
        }

        private static void RunContainsTest_OneMatching(int size, int matchPosition)
        {
            int toFind = 103372;
            //Random r = new Random(33);
            int[] data = new int[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = i;
                if (data[i] == toFind)
                    data[i] += 1;
            }
            data[matchPosition] = toFind;

            bool expect = true;
            bool result = data.AsParallel().Contains(toFind);

            if (result != expect)
                Assert.True(false,
                    string.Format("RunContainsTest_OneMatching(size={2}, matchPosition={3}):  FAILED.  > Expect: {0}, real: {1}",
                    expect, result, size, matchPosition));
        }
    }
}
