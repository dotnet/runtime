// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static void Main()
        {
        }

        private static int counter;

        [JSExport]
        public static int Increment() => ++counter;
    }
}
