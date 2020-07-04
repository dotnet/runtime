// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;


namespace Test
{
    internal class AA
    {
        private static unsafe int Main()
        {
            byte* p = stackalloc byte[new sbyte[] { 10 }[0]];
            return 100;
        }
    }
}
