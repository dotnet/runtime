// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace ComLibrary
{
    [ComVisible(true)]
    public class Server
    {
        public Server()
        {
            Console.WriteLine($"New instance of {nameof(Server)} created");
        }
    }
}
