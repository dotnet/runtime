// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebuggerTests
    {
        [Fact]
        public void IsAttached()
        {
            bool b = Debugger.IsAttached;
        }

        [Fact]
        public void IsLogging()
        {
            if (Debugger.IsAttached)
                Debugger.IsLogging();
            else
                Assert.False(Debugger.IsLogging());
        }

        [Fact]
        public void Log()
        {
            Debugger.Log(10, "category", "This is a test log message raised in the System.Diagnostics.Debug tests for the .NET Debugger class.");
        }

        [Fact]
        public void NotifyOfCrossThreadDependency()
        {
            Debugger.NotifyOfCrossThreadDependency();
        }
    }
}
