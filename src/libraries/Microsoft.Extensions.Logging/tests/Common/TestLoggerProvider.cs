// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Logging.Test
{
#pragma warning disable CS0436 // Type conflicts with imported type
    [ProviderAlias("TestLogger")]
#pragma warning restore CS0436 // Type conflicts with imported type
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly Func<LogLevel, bool> _filter;

        public TestLoggerProvider(TestSink testSink, bool isEnabled) :
            this(testSink, _ => isEnabled)
        {
        }

        public TestLoggerProvider(TestSink testSink, Func<LogLevel, bool> filter)
        {
            Sink = testSink;
            _filter = filter;
        }

        public TestSink Sink { get; }

        public bool DisposeCalled { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, Sink, _filter);
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    public class TestLoggerProvider2 : TestLoggerProvider
    {
        public TestLoggerProvider2(TestSink testSink) : base(testSink, true)
        {
        }
    }
}
