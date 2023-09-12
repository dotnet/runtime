// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class Managed
{
    [DllImport("MarshalStructAsParam")]
    static extern GameControllerBindType getBindType (GameControllerButtonBind button);

    public static int Main()
    {
        GameControllerButtonBind button = new GameControllerButtonBind(GameControllerBindType.ControllerBindtypeAxis, null);
        if (getBindType(button) == GameControllerBindType.ControllerBindtypeAxis)
        {
            Console.WriteLine("\nTEST PASSED!");
            return 100;
        }
        else
        {
            Console.WriteLine("\nTEST FAILED!");
            return 1;
        }
    }
}
