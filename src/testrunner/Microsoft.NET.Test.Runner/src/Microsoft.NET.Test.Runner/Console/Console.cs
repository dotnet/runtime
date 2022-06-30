// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.Console
{
    internal class SystemConsole : IConsole
    {
        public void WriteLine(string message, ConsoleColor color)
        {
            ConsoleColor currentColor = System.Console.ForegroundColor;
            try
            {
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(message);
            }
            finally
            {
                System.Console.ForegroundColor = currentColor;
            }
        }
    }
}
