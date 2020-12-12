// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Tests
{
    public class PriorityQueue_Generic_Tests_string_string : PriorityQueue_Generic_Tests<string, string>
    {
        protected override (string, string) CreateT(int seed)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityQueue_Generic_Tests_int_int : PriorityQueue_Generic_Tests<int, int>
    {
        protected override (int, int) CreateT(int seed)
        {
            throw new NotImplementedException();
        }
    }
}
