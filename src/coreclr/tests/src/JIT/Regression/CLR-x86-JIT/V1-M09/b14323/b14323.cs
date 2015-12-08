

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DefaultNamespace
{
    internal class AppStarter
    {
        public static int Main(String[] args)
        {
            int[] foo = new int[1];
            long j = 0;
            foo[(int)j] = 1;
            return 100;
        }
    };
}

