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
            //Console.WriteLine (DateTime.GetNow().ToString());
            Console.WriteLine(DateTime.Now.ToString());
            return 100;
        }
    }

}
