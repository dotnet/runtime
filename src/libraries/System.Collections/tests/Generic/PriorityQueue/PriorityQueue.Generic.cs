// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class PriorityQueue_Generic_Tests_string_string : PriorityQueue_Generic_Tests<string, string>
    {
        protected override string CreateT(int seed) => CreateString(seed);
        protected override string CreateElement(int seed) => CreateString(seed);

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
        protected override int CreateT(int seed) => CreateInt(seed);
        protected override int CreateElement(int seed) => CreateInt(seed);

        protected int CreateInt(int seed) => new Random(seed).Next();
    }

    public class PriorityQueue_Generic_Tests_string_string_CustomComparer : PriorityQueue_Generic_Tests_string_string
    {
        protected override IComparer<string> GetIComparer() => StringComparer.InvariantCultureIgnoreCase;
    }

    public class PriorityQueue_Generic_Tests_int_int_CustomComparer : PriorityQueue_Generic_Tests_int_int
    {
        protected override IComparer<int> GetIComparer() => Comparer<int>.Create((x, y) => -x.CompareTo(y));
    }
}
