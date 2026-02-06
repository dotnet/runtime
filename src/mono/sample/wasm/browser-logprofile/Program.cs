// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [JSExport]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TakeHeapshot() { }

        [JSExport]
        public static int TestMeaning()
        {
            for (int i=0; i<100; i++){
                var r = new int[1000];
            }
 
            return 42;
        }
    }
}
