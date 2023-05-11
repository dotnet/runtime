// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        [Fact]
        public static int TestEntryPoint()
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
