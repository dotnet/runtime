﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Test;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Tests.Redaction
{
    public class RedactionTests
    {
        [Fact]
        public void Redact_Message()
        {
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);
            ILoggerFactory factory = LoggerFactory.Create(b =>
            {
                b.AddProvider(provider);
                b.Services.AddSingleton<IRedactorProvider, TestRedactorProvider>();
                b.AddRedactionProcessor();
            });

            ILogger foo = factory.CreateLogger("Foo");
            LogMessages.LogName(foo, "Frank", 76);
            LogMessages.LogNameRedacted(foo, "Frank", 76);

            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal("User Frank has now 76 status", sink.Writes.ElementAt(0).Message);
            Assert.Equal("User [Redacted - 2] has now 76 status", sink.Writes.ElementAt(1).Message);
        }

        [Fact]
        public void Redact_StateValues()
        {
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);
            ILoggerFactory factory = LoggerFactory.Create(b =>
            {
                b.AddProvider(provider);
                b.Services.AddSingleton<IRedactorProvider, TestRedactorProvider>();
                b.AddRedactionProcessor();
            });

            ILogger foo = factory.CreateLogger("Foo");
            LogMessages.LogNameRedacted(foo, "Frank", 76);

            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes.ElementAt(0);
            Assert.Equal("User [Redacted - 2] has now 76 status", write.Message);

            var values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(write.State);
            Assert.Collection(values,
                kvp =>
                {
                    Assert.Equal("username", kvp.Key);
                    Assert.Equal("[Redacted - 2]", kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("status", kvp.Key);
                    Assert.Equal(76, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("{OriginalFormat}", kvp.Key);
                    Assert.Equal("User {username} has now {status} status", kvp.Value);
                });
        }
    }

    public static partial class LogMessages
    {
        //[LoggerMessage(1, LogLevel.Information, "User {username} has now {status} status")]
        public static void LogName(this ILogger logger, string username, int status)
        {
            // manually writing the code the source generator is proposed to create
            __LogNameCallback(logger, username, status, null);
        }

        private static readonly Action<ILogger, String, int, Exception?> __LogNameCallback =
            LoggerMessage.Define<String, int>(
                LogLevel.Information,
                new EventId(1, nameof(LogName)),
                "User {username} has now {status} status");


        //[LoggerMessage(2, LogLevel.Information, "User {username} has now {status} status")]
        public static void LogNameRedacted(this ILogger logger, [PrivateData] string username, int status)
        {
            // manually writing the code the source generator is proposed to create
            __LogNameRedactedCallback(logger, username, status, null);
        }

        private static readonly Action<ILogger, String, int, Exception?> __LogNameRedactedCallback =
            LoggerMessage.Define<String, int>(
                LogLevel.Information,
                new EventId(1, nameof(LogNameRedacted)),
                "User {username} has now {status} status",
                new LogDefineOptions() { ParameterMetadata = new Attribute[]?[] { new Attribute[] { new PrivateDataAttribute() }, null } });
    }
}

#endif
