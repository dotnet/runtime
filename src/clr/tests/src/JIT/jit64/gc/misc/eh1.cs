// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class T
{
    public static int Main()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception E)
        {
            Console.WriteLine("Caught expected exception " + E.GetType());
            return 100;
        }
#pragma warning disable 0162
        return -1;
#pragma warning restore 0252
    }
}
