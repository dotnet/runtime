// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class jitAssert
    {
        public static bool MultiDim_ArgCheck_Fail()
        {
            if (true)
            {
                Console.WriteLine("\n\tMultiDim Array arg check disabled for now - can't create 2D array!\n");
                return true;
            }

            try
            {
            }
            catch (RankException)
            {
            }

            return true;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            MultiDim_ArgCheck_Fail();
            return 100;
        }
    }
}
