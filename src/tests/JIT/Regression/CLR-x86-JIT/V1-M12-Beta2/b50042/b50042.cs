// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/*
set COMPlus_JitNoRegLoc=1
set COMPlus_JitSched=2
*/
namespace Test
{
    using System;

    class AA { }

    class BB
    {
        static object Method1(String[] param3)
        {
            object L = null;
            return L;
        }

        static int Main()
        {
            try
            {
                AA aa = ((AA)(BB.Method1(new String[4])));
            }
            catch (Exception) { }
            return 100;
        }
    }
}
