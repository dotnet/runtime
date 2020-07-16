// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Logging.Console
{
    internal class AnsiSystemConsole : IAnsiSystemConsole
    {
        private readonly TextWriter _textWriter;

        public AnsiSystemConsole(bool stdErr)
        {
            _textWriter = stdErr ? System.Console.Error : System.Console.Out;
        }

        public void Write(string message)
        {
            _textWriter.Write(message);
        }
    }
}
