// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using SharedLibrary;

namespace PortableApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Returns 1 if using the reference assembly, and 2 if
            // using the assembly injected by the startup hook. This
            // should never actually use the reference assembly, which
            // is not published with the app.
            return SharedType.Value;
        }
    }
}
