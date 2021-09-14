// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

namespace StackMemTest
{
    class Program
    {
        struct AwesomeStruct
        {
            public object o;
            public int i;
        }
        unsafe static int Main(string[] args)
        {
            Console.WriteLine("StackMem");

            Span<AwesomeStruct> theSpan = new Span<AwesomeStruct>(RuntimeHelpers.StackAlloc<AwesomeStruct>(200_000), 200_000);

            for (int i = 0; i < 100000; i++)
            {
                theSpan[i].o = i.ToString();
                theSpan[i].i = i;
            }

            for (int i = 0; i < 100000; i++)
            {
                if (!theSpan[i].i.ToString().Equals(theSpan[i].o))
                {
                    return -i;
                }
            }

            

            return 100;
        }
    }
}
