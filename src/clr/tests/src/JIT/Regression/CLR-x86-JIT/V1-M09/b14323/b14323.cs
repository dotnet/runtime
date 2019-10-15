// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;


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

