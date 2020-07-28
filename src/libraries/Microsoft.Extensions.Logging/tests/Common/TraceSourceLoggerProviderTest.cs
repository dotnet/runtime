// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddTraceSource(testSwitch, listener))
                .BuildServiceProvider();

            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger1 = factory.CreateLogger("FirstLogger");
            var logger2 = factory.CreateLogger("SecondLogger");
            logger1.LogError("message1");
            logger2.LogError("message2");

            // Act
            serviceProvider.Dispose();

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
#elif NETCOREAPP
#else
#error Target framework needs to be updated
#endif
