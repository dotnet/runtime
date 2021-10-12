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
        private const int spanSize = 1000;
        unsafe static int Main(string[] args)
        {
            Console.WriteLine("StackMem");

            Span<AwesomeStruct> theSpan = RuntimeHelpers.StackAlloc<AwesomeStruct>(spanSize);
            Console.WriteLine(theSpan.Length);

            for (int i = 0; i < spanSize; i++)
            {
                theSpan[i].o = i.ToString();
                theSpan[i].i = i;
            }

            for (int i = 0; i < spanSize; i++)
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
