// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
struct AA
{
    static bool Static3(ulong param2)
    {
        bool b = false;
        return (bool)(object)(long)(byte)(b ? Convert.ToInt64(param2) : (long)param2);
    }
    static int Main()
    {
        try
        {
            Static3(0);
            return 101;
        }
        catch (InvalidCastException)
        {
            return 100;
        }
    }
}
