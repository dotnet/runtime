// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// A provider of <see cref="ConsoleLogger"/> instances.
    /// </summary>
    public partial class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private static bool DoesConsoleSupportAnsi()
        {
            // for Windows, check the console mode
            var stdOutHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.STD_OUTPUT_HANDLE);
            if (!Interop.Kernel32.GetConsoleMode(stdOutHandle, out int consoleMode))
            {
                return false;
            }

            return (consoleMode & Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING) == Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        }
    }
}
