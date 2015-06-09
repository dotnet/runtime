// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
struct AA
{
    static int Main()
    {
        try
        {
            Main1();
            return 101;
        }
        catch (InvalidCastException)
        {
            return 100;
        }
    }
    static void Main1()
    {
        try
        {
            bool b = false;
            b = ((bool)((
                b ? b :
                    (b ?
                        (b ? (object)new AA() : (object)new CC())
                        : (object)new CC())
            )));
        }
        finally { }
    }
}
struct BB { }
class CC { }
