// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
