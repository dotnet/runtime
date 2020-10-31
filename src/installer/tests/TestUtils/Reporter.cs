// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Cli.Build.Framework
{
    // Stupid-simple console manager
    internal class Reporter
    {
        private static readonly Reporter Null = new Reporter(console: null);
        private static object _lock = new object();

        private readonly AnsiConsole _console;

        private Reporter(AnsiConsole console)
        {
            _console = console;
        }

        public static Reporter Output { get; } = new Reporter(AnsiConsole.GetOutput());
        public static Reporter Error { get; } = new Reporter(AnsiConsole.GetOutput());
        public static Reporter Verbose { get; } = new Reporter(AnsiConsole.GetOutput());

        public void WriteLine(string message)
        {
            lock (_lock)
            {
                _console?.WriteLine(message);
            }
        }

        public void WriteLine()
        {
            lock (_lock)
            {
                _console?.Writer?.WriteLine();
            }
        }

        public void Write(string message)
        {
            lock (_lock)
            {
                _console?.Writer?.Write(message);
            }
        }
        
        public void WriteBanner(string content)
        {
            string border = new string('*', content.Length + 6);
            WriteLine($@"{border}
*  {content}  *
{border}".Green());
        }
    }
}
