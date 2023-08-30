// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

// Test includes an intentional unreachable return 
#pragma warning disable 162

namespace PInvokeTest
{
    public class Test
    {
        [DllImport("__Internal", EntryPoint = "sin", CallingConvention = CallingConvention.Cdecl)]
        static extern void create (GameControllerButtonBind button);

        [Fact]
        public static int TestNestedStruct()
        {
            GameControllerButtonBind button = new GameControllerButtonBind(GameControllerBindType.ControllerBindtypeAxis, null);
            // This is a good test, as long as the following function call is successfull without assertion or other error.
            create(button);

            return (100);
        }
    }
}

