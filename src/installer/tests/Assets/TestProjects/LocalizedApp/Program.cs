// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Globalization;
using System.Resources;

namespace LocalizedApp
{
    class Program
    {
        static void Main()
        {
            string [] cultures = { "kn-IN", "ta-IN", "en-US" };
            string greeting = "";
            foreach (var culture in cultures)
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
                greeting += $"{Hello.Greet}! ";
            }

            Console.WriteLine(greeting);
        }
    }
}
