// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class A
{
    public static int Main()
    {
        Main1();
        return 100;
    }
    public static void Main1()
    {
        bool b = false;
        while (b)
            break;
        try
        {
            do
            {
                continue;
            } while (new object[] { }[0] != null);
        }
        catch (Exception) { }
    }
}