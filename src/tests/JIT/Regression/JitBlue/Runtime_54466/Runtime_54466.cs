// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Runtime_54466
{
    public class Test
    {
        static int Main()
        {
            return t(1, 1, 1, 1, Vector2.One, Vector2.One, Vector2.One, Vector2.One);
        }
        
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int t(int a1, int a2, int a3, int a4, Vector2 x1, Vector2 x2, Vector2 x3, Vector2 x4)
        {
           if (x1 != Vector2.One) 
           {
               Console.WriteLine("FAIL");
               return 101;
           }
           return 100;
        }
    }
        
}


