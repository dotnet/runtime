// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            DisplayMeaning(2*Fibonacci(8));
            return 0;
        }

        [DllImport("fibonacci")]
        public static extern int Fibonacci(int n);

        [JSImport("Sample.Test.displayMeaning", "main.js")]
        internal static partial void DisplayMeaning(int meaning);
    }
}
