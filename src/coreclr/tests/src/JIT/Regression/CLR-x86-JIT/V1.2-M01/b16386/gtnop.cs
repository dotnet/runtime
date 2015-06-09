// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public class gtnop
{
    public static int Main()
    {
        byte[] arr = new byte[1];
        short i = 3;
        try { arr[(byte)(20u) * i] = 0; }
        catch (IndexOutOfRangeException) { return 100; }
        return 1;
    }
}
