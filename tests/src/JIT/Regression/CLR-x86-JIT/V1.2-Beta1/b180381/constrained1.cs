// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class Test
{
    public static void M<T>(T t)
    {
        System.Type type = t.GetType();
        Console.WriteLine(type);
    }

    public static int Main()
    {
        M("Hello"); // Works fine
        M(3); // CLR crashes
        return 100;
    }
}
