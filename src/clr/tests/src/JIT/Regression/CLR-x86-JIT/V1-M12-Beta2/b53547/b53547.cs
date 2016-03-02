// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
