// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
