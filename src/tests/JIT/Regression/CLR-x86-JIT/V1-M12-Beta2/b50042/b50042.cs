// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/*
set DOTNET_JitNoRegLoc=1
set DOTNET_JitSched=2
*/
namespace Test
{
    using System;

    class AA { }

    public class BB
    {
        static object Method1(String[] param3)
        {
            object L = null;
            return L;
        }

        [Fact]
        public static int TestEntryPoint()
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
