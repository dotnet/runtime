// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class PriorityQueue_Generic_Tests_string_string : PriorityQueue_Generic_Tests<string, string>
    {
        protected override (string, string) CreateT(int seed)
        {
            var random = new Random(seed);
            return (CreateString(random), CreateString(random));

            static string CreateString(Random random)
            {
                int stringLength = random.Next(5, 15);
                byte[] bytes = new byte[stringLength];
                random.NextBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }

    public class PriorityQueue_Generic_Tests_int_int : PriorityQueue_Generic_Tests<int, int>
    {
        protected override (int, int) CreateT(int seed)
        {
            var random = new Random(seed);
            return (random.Next(),random.Next());
        }
    }

    public class PriorityQueue_Generic_Tests_string_string_CustomComparer : PriorityQueue_Generic_Tests_string_string
    {
        protected override IComparer<string> GetPriorityComparer() => StringComparer.InvariantCultureIgnoreCase;
    }

    public class PriorityQueue_Generic_Tests_int_int_CustomComparer : PriorityQueue_Generic_Tests_int_int
    {
        protected override IComparer<int> GetPriorityComparer() => Comparer<int>.Create((x, y) => -x.CompareTo(y));
    }
}
