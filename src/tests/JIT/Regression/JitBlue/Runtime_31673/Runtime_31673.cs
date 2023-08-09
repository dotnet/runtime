// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_31673
{

    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Vector4 Test(Vector4 v)
        {
            return Vector4.Clamp(v, Vector4.Zero, Vector4.One);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int returnVal = 100;

            Vector4 v1 = new Vector4(1,2,3,4);
            Vector4 v2 = Test(v1);
            if (!v2.Equals(Vector4.One))
            {
                Console.WriteLine(v2);
                returnVal = -1;
            }
            return returnVal;
        }
    }
}
