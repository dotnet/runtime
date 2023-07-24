// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;


// The jit should correctly import get struct address as a first statement during the importation phase.

namespace GitHub_24114
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Test();
            return 100;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Test()
        {
            var options = SimpleStruct.Default;
        }
    }

    public struct SimpleStruct
    {
        public static readonly SimpleStruct Default = new SimpleStruct()
        {
        };
    }
}
