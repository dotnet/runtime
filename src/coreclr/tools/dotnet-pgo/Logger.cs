// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class Logger
    {
        private bool _hideMessages;

        public void PrintWarning(string warning)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: " + warning);
            Console.ForegroundColor = oldColor;
        }

        public void PrintError(string error)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: " + error);
            Console.ForegroundColor = oldColor;
        }

        public void HideMessages()
        {
            _hideMessages = true;
        }

        public void PrintMessage(string message)
        {
            if (!_hideMessages)
                Console.WriteLine(message);
        }

        public void PrintOutput(string message)
        {
            Console.WriteLine(message);
        }
    }
}
