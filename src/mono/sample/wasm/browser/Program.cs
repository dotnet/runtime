// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine($"MF .NET, args: {String.Join(", ", args)}, location: '{GetLocation()}'");
            return 0;
        }

        [JSImport("location.href", "main.js")]
        internal static partial string GetLocation();

        [JSExport]
        internal static string Greet() => "MF JSExport";
    }
}
