// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
