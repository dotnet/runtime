// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace VectorMathTests
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int Count = System.Numerics.Vector<int>.Count;
            return 100;
        }
    }
}
