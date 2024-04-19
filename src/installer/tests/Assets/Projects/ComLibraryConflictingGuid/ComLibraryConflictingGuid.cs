// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace ComLibrary
{
    // These two types must have the same GUID for testing.

    [ComVisible(true)]
    [Guid("cc6e9910-18d5-484a-a2d2-fa8910fd0261")]
    public class Server
    {
        public Server()
        {
            Console.WriteLine($"New instance of {nameof(Server)} created");
        }
    }
    
    [ComVisible(true)]
    [Guid("cc6e9910-18d5-484a-a2d2-fa8910fd0261")]
    public class Server2
    {
        public Server2()
        {
            Console.WriteLine($"New instance of {nameof(Server2)} created");
        }
    }
}
