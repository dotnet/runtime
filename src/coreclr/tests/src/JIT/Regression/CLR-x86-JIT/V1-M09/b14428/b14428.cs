// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class DateTimeCompare
    {

        public static int Main(String[] args)
        {
            Object v1 = new DateTime(1952, 2, 19);
            Object v2 = new DateTime(1968, 12, 8);
            Console.WriteLine(DateTime.Compare((DateTime)v1, (DateTime)v2));
            return 100;
        }
    }
}
