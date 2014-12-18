// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class CastOfTypeTests
    {
        //
        // Cast and OfType
        //

        [Fact]
        public static void RunCastAndOfTypeTests()
        {
            RunCastTest1(0, true);
            RunCastTest1(1024 * 32, true);
            RunCastTest1(0, false);
            RunCastTest1(1024 * 32, false);
        }

        [Fact]
        public static void RunOfTypeTests()
        {
            RunOfTypeTest1(0, true);
            RunOfTypeTest1(1024 * 32, true);
            RunOfTypeTest1(0, false);
            RunOfTypeTest1(1024 * 32, false);
        }

        private static void RunCastTest1(int count, bool useTheRightType)
        {
            string methodFailed = string.Format("RunCastTest1(count={0}, useTheRightType={1})  --  used in a query:  FAILED.  ", count, useTheRightType);

            // Fill with ints or strings, depending on whether the test is using the correct type.
            List<object> data = new List<object>();
            for (int i = 0; i < count; i++)
            {
                data.Add(useTheRightType ? (object)i : "boom");
            }

            // Now check the output.
            int cnt = 0;
            ParallelQuery<int> q = data.AsParallel().Cast<int>();
            try
            {
                foreach (int e in q)
                {
                    cnt++;
                }

                if (!(useTheRightType || count == 0))
                    Assert.True(false, string.Format(methodFailed + "  > no exception thrown"));
            }
            catch (AggregateException aex)
            {
                bool passed = !useTheRightType;
                if (!passed)
                {
                    Assert.True(false, string.Format("  > caught exceptions - but were not expected? useTheRightType:{0}", !useTheRightType));
                }
                if (!aex.InnerExceptions.All(e => e is InvalidCastException))
                {
                    Assert.True(false, string.Format(methodFailed + "  > some exceptions are not InvalidCastException: {0}", aex.ToString()));
                }
            }

            if (useTheRightType)
            {
                if (cnt != count)
                    Assert.True(false, string.Format(methodFailed + "  > Total should be {0} -- real total is {1}", count, cnt));
            }
        }

        private static void RunOfTypeTest1(int count, bool useTheRightType)
        {
            string methodFailed = string.Format("RunOfTypeTest1(count={0}, useTheRightType={1})  --  used in a query: FAILED.", count, useTheRightType);

            // Fill with ints or strings, depending on whether the test is using the correct type.
            List<object> data = new List<object>();
            for (int i = 0; i < count; i++)
            {
                data.Add(useTheRightType ? (object)i : "boom");
            }

            // Now check the output.
            int cnt = 0;
            ParallelQuery<int> q = data.AsParallel().OfType<int>();
            foreach (int e in q)
            {
                cnt++;
            }

            if (useTheRightType)
            {
                if (cnt != count)
                    Assert.True(false, string.Format(methodFailed + "  > Total should be {0} -- real total is {1}", count, cnt));
            }
            else
            {
                if (cnt != 0)
                    Assert.True(false, string.Format(methodFailed + "  > Total should be {0} -- real total is {1}", 0, cnt));
            }
        }
    }
}
