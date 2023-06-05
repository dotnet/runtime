// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace NarrowStore
{
    public class Program 
    { 
        byte x01;
        byte t01;
    
        int Test()
        {
            x01 = 3;
            t01 = (byte)~x01;
            if (t01 == 252)
            {
                Console.WriteLine("Pass");
                return 100;
            }
            else
            {
                Console.WriteLine("FAIL");
                return -1;
            }
        }

        [Fact]
        public static int TestEntryPoint() 
        { 
            Program prog = new Program();
            
            int result = prog.Test();
            return result;
        }
    }
}

