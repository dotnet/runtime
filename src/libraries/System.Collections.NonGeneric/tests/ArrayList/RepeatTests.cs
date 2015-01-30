// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class RepeatTests
    {
        [Fact]
        public void TestRepeatBasic()
        {
            ArrayList alst1;

            //[]Vanila test case - Repeat returns an ArrayList with the repeated object n times. 
            alst1 = ArrayList.Repeat(5, 1000);

            for (int i = 0; i < alst1.Count; i++)
            {
                Assert.Equal(5, (int)alst1[i]);
            }

            alst1 = ArrayList.Repeat(null, 10);

            for (int i = 0; i < alst1.Count; i++)
            {
                Assert.Null(alst1[i]);
            }

            alst1 = ArrayList.Repeat(5, 0);

            Assert.Equal(0, alst1.Count);

            //[]parm value
            //
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    alst1 = ArrayList.Repeat(5, -1);
                });
        }
    }
}
