// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal class BaseException : Exception { }
internal class DerivedException : BaseException { }

internal class Test
{
    private static int Main()
    {
        BaseException ex = new DerivedException();
        try
        {
            if (ex is DerivedException)
                throw (DerivedException)ex;
        }
        catch (DerivedException)
        {
            goto continue_1;
        }
        Console.WriteLine("failed(1)");
        return 101;

    continue_1:
        try
        {
            if (ex is DerivedException)
                throw (DerivedException)ex;
        }
        catch (DerivedException)
        {
            goto continue_2;
        }
        Console.WriteLine("failed(2)");
        return 102;

    continue_2:
        Console.WriteLine("Good");
        return 100;
    }
}
