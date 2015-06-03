// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public class Program
{
    public static bool IsGuid(object item)
    {
        return item is Guid;
    }
    public static int Main()
    {
        if (IsGuid(Guid.NewGuid()))
            return 100;
        else
            return 99;
    }
}
