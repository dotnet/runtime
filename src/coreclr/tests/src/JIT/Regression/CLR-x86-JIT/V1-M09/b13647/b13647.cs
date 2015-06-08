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
            //Console.WriteLine (DateTime.GetNow().ToString());
            Console.WriteLine(DateTime.Now.ToString());
            return 100;
        }
    }

}
