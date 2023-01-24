// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        [JSExport]
        public static int Main()
        {
            return 42;
        }
    }
}
