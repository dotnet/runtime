// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace VectorMathTests
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            {
                var a = new System.Numerics.Vector<int>(1);
                object b = a;
                if (b is System.Numerics.Vector<int>)
                {
                    var c = (System.Numerics.Vector<int>)b;
                    if (a != c)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            {
                var a = new System.Numerics.Vector4(1);
                object b = a;
                if (b is System.Numerics.Vector4)
                {
                    var c = (System.Numerics.Vector4)b;
                    if (a != c)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            return 100;
        }
    }
}
