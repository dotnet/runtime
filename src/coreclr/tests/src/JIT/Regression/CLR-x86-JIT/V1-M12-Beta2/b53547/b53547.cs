

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
