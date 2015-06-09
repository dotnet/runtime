// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

        public static int Main(String[] args)
        {
            MultiDim_ArgCheck_Fail();
            return 100;
        }
    }
}
