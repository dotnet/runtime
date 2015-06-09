// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class CC
{
    static int Main()
    {
        try
        {
            Main1();
            return 101;
        }
        catch (NullReferenceException)
        {
            return 100;
        }
    }
    static void Main1()
    {
        object b = null;
        while ((bool)b)
            return;
        while ((bool)b)
        {
            while (b == null)
            {
                do { } while ((bool)b);
                while ((bool)b) { }
                GC.Collect();
            }
        }
    }
}
