// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace GitHub_23545
{
    public struct TestStruct
    {
        public int value1;

        public override string ToString()
        {
            return this.value1.ToString();
        }
    }

    public class Test
    {
        public static Dictionary<TestStruct, TestStruct> StructKeyValue
        {
            get
            {
                return new Dictionary<TestStruct, TestStruct>()
                {
                    {
                        new TestStruct(){value1 = 12}, new TestStruct(){value1 = 15}
                    }
                };
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int value = 0;
            foreach (var e in StructKeyValue)
            {
                value += e.Key.value1 + e.Value.value1;
                Console.WriteLine(e.Key.ToString() + " " + e.Value.ToString());
            }
            if (value != 27)
            {
                return -1;
            }
            return 100;
        }
    }
}
