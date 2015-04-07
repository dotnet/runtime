// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class CancelKeyPress
{
    [Fact]
    public static void CanAddAndRemoveHandler()
    {
        ConsoleCancelEventHandler handler = new ConsoleCancelEventHandler(Console_CancelKeyPress);

        Console.CancelKeyPress += handler;
        Console.CancelKeyPress -= handler;
    }

    static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        // We don't actually want to do anything here.  This will only get called on the off chance
        // that someone CTRL+C's the test run while the handler is hooked up.  This is just used to 
        // validate that we can add and remove a handler, we don't care about exercising it.
    }
}
