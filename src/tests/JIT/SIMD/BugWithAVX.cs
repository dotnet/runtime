// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace SIMDTests.BugWithAVXTests
{
    public class Program
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            int Count = System.Numerics.Vector<int>.Count;
        }
    }
}
