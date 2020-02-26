// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Extensions.Logging.Console
{
    internal class AnsiSystemConsole : IAnsiSystemConsole
    {
        private readonly TextWriter _textWriter;

        /// <inheritdoc />
        public AnsiSystemConsole(bool stdErr = false)
        {
            _textWriter = stdErr ? System.Console.Error : System.Console.Out;
        }

        public void Write(string message)
        {
            _textWriter.Write(message);
        }
    }
}
