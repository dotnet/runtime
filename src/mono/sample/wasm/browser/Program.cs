// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static readonly DateTime Created = DateTime.Now;
        public static readonly string Location = GetLocation();

        public static int Main(string[] args)
        {
            Console.WriteLine($".NET, args({args.Length}): {String.Join(", ", args)}, static location: '{Location}', location: '{GetLocation()}', created: '{Created.ToString("yyyy-MM-dd HH:mm:ss")}'");
            return 0;
        }

        [JSImport("location.href", "main.js")]
        internal static partial string GetLocation();

        [JSExport]
        internal static string Greet() => "JSExport";
    }
}
