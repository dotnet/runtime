// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
set COMPLUS_JitNoRegLoc=1
set COMPLUS_JitSched=2
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
