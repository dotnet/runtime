// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace RuntimeLibrariesTest
{
    public class Logger
    {
        public static void LogInformation(string message, params object[] args)
        {
            if (args.Length == 0)
                Console.WriteLine(message);
            else
                Console.WriteLine(message, args);
        }
    }
}
