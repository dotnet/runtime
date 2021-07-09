// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    internal class Log : ILog
    {
        private readonly ITestOutputHelper _output;

        public Log(ITestOutputHelper output)
        {
            _output = output;
            Reset();
        }

        public int ErrorsLogged { get; set; }
        public int WarningsLogged { get; set; }

        public void LogError(string message, params object[] messageArgs)
        {
            ErrorsLogged++;
            _output.WriteLine("Error: " + message, messageArgs);
        }

        public void LogMessage(string message, params object[] messageArgs)
        {
            _output.WriteLine(message, messageArgs);
        }

        public void LogMessage(LogImportance importance, string message, params object[] messageArgs)
        {
            _output.WriteLine(message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            WarningsLogged++;
            _output.WriteLine("Warning: " + message, messageArgs);
        }

        public void Reset()
        {
            ErrorsLogged = 0;
            WarningsLogged = 0;
        }

        public void AssertNoErrorsOrWarnings()
        {
            Assert.Equal(0, ErrorsLogged);
            Assert.Equal(0, WarningsLogged);
        }
    }
}
