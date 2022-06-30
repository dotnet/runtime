// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.Console
{
    internal interface IConsole
    {
        void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray);
    }
}
