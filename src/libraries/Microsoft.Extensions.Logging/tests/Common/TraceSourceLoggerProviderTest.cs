// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging.TraceSource;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceLoggerProviderTest
    {
        [Fact]
        public void Dispose_TraceListenerIsFlushedOnce()
        {
            // Arrange
            var testSwitch = new SourceSwitch("TestSwitch", "Level will be set to warning for this test");
            testSwitch.Level = SourceLevels.Warning;
            var listener = new BufferedConsoleTraceListener();

            TraceSourceLoggerProvider provider = new TraceSourceLoggerProvider(testSwitch, listener);
            var logger1 = provider.CreateLogger("FirstLogger");
            var logger2 = provider.CreateLogger("SecondLogger");
            logger1.LogError("message1");
            logger2.LogError("message2");

            // Act
            provider.Dispose();

            // Assert
            Assert.Equal(1, listener.FlushCount);
            Assert.Equal(new []
            {
                "FirstLogger Error: 0 : ",
                "message1" + Environment.NewLine,
                "SecondLogger Error: 0 : ",
                "message2" + Environment.NewLine
            }, listener.Messages);
        }

        private class BufferedConsoleTraceListener : TraceListener
        {
            public int FlushCount { get; set; }
            public List<string> Messages { get; } = new List<string>();

            public override void Flush()
            {
                FlushCount++;
            }

            public override void Write(string message)
            {
                Messages.Add(message);
            }

            public override void WriteLine(string message)
            {
                Messages.Add(message + Environment.NewLine);
            }
        }
    }
}
#elif NETCOREAPP2_2
#else
#error Target framework needs to be updated
#endif
