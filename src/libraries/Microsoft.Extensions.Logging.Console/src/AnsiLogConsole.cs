// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// For consoles which understand the ANSI escape code sequences to represent color
    /// </summary>
    internal class AnsiLogConsole : IConsole
    {
        private readonly IAnsiSystemConsole _systemConsole;

        public AnsiLogConsole(bool stdErr = false)
        {
            _systemConsole = new AnsiSystemConsole(stdErr);
        }

        public void Write(string message)
        {
            _systemConsole.Write(message);
        }
    }
}
