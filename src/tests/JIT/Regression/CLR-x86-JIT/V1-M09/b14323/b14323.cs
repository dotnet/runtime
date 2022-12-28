// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;


namespace DefaultNamespace
{
    public class AppStarter
    {
        public static int Main()
        {
            int[] foo = new int[1];
            long j = 0;
            foo[(int)j] = 1;
            return 100;
        }
    };
}

