// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Tests
{
    public class PriorityQueue_Generic_Tests_string_string : PriorityQueue_Generic_Tests<string, string>
    {
        protected override (string, string) CreateT(int seed)
        {
            var element = this.CreateString(seed);
            var priority = this.CreateString(seed);
            return (element, priority);
        }

        protected string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class PriorityQueue_Generic_Tests_int_int : PriorityQueue_Generic_Tests<int, int>
    {
        protected override (int, int) CreateT(int seed)
        {
            var element = this.CreateInt(seed);
            var priority = this.CreateInt(seed);
            return (element, priority);
        }

        protected int CreateInt(int seed) => new Random(seed).Next();
    }
}
