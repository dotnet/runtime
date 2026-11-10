// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    public partial class Test
    {
        public static Task<int> Main(string[] args)
        {
            DisplayMeaning(42);
            return Task.FromResult(0);
        }

        [JSImport("Sample.Test.displayMeaning", "main.js")]
        internal static partial void DisplayMeaning(int meaning);

        [JSExport]
        internal static async Task PrintMeaning(Task<int> meaningPromise)
        {
            Console.WriteLine("Meaning of life is " + await meaningPromise);
        }
    }
}
