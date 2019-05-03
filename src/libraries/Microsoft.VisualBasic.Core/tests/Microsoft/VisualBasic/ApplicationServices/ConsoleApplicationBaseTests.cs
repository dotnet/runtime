// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.VisualBasic.ApplicationServices.Tests
{
    public class ConsoleApplicationBaseTests
    {
        [Fact]
        public void CommandLineArgs()
        {
            var app = new ConsoleApplicationBase();
            var expected = System.Environment.GetCommandLineArgs().Skip(1).ToArray();
            Assert.Equal(expected, app.CommandLineArgs);
        }
    }
}
