// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        public static int Main(String[] args)
        {

            byte x = 0xFF;		//	ANDREIS: Added cast operator due compiler error SC0031
            byte tmp = 255;
            if (tmp == (byte)x)
            {
                Console.WriteLine("Pass");
                return 100;
            }
            else
            {
                Console.WriteLine("Fail");
                return 1;
            }
        }
    }
}
