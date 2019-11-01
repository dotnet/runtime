// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
