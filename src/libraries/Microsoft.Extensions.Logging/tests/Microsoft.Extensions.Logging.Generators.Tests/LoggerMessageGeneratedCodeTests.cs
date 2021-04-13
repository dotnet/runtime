// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Generators.Tests.TestClasses;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/32743", TestRuntimes.Mono)]
    public class LoggerMessageGeneratedCodeTests
    {
        [Fact]
        public void BasicTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            NoNamespace.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            Level1.OneLevelNamespace.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            Level1.Level2.TwoLevelNamespace.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void EnableTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            logger.Enabled = false;
            NoNamespace.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(0, logger.CallCount);          // ensure the logger doesn't get called when it is disabled
        }

        [Fact]
        public void ArgTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            ArgTestExtensions.Method1(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M1", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method2(logger, "arg1");
            Assert.Null(logger.LastException);
            Assert.Equal("M2 arg1", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method3(logger, "arg1", 2);
            Assert.Null(logger.LastException);
            Assert.Equal("M3 arg1 2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method4(logger, new InvalidOperationException("A"));
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M4", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method5(logger, new InvalidOperationException("A"), new InvalidOperationException("B"));
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M5 System.InvalidOperationException: B", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method6(logger, new InvalidOperationException("A"), 2);
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M6 2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method7(logger, 1, new InvalidOperationException("B"));
            Assert.Equal("B", logger.LastException!.Message);
            Assert.Equal("M7 1", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method8(logger, 1, 2, 3, 4, 5, 6, 7);
            Assert.Equal("M81234567", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method9(logger, 1, 2, 3, 4, 5, 6, 7);
            Assert.Equal("M9 1 2 3 4 5 6 7", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method10(logger, 1);
            Assert.Equal("M101", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void CollectionTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            CollectionTestExtensions.M0(logger);
            TestCollection(1, logger);

            logger.Reset();
            CollectionTestExtensions.M1(logger, 0);
            TestCollection(2, logger);

            logger.Reset();
            CollectionTestExtensions.M2(logger, 0, 1);
            TestCollection(3, logger);

            logger.Reset();
            CollectionTestExtensions.M3(logger, 0, 1, 2);
            TestCollection(4, logger);

            logger.Reset();
            CollectionTestExtensions.M4(logger, 0, 1, 2, 3);
            TestCollection(5, logger);

            logger.Reset();
            CollectionTestExtensions.M5(logger, 0, 1, 2, 3, 4);
            TestCollection(6, logger);

            logger.Reset();
            CollectionTestExtensions.M6(logger, 0, 1, 2, 3, 4, 5);
            TestCollection(7, logger);

            logger.Reset();
            CollectionTestExtensions.M7(logger, 0, 1, 2, 3, 4, 5, 6);
            TestCollection(8, logger);

            logger.Reset();
            CollectionTestExtensions.M8(logger, 0, 1, 2, 3, 4, 5, 6, 7);
            TestCollection(9, logger);

            logger.Reset();
            CollectionTestExtensions.M9(logger, LogLevel.Critical, 0, new ArgumentException("Foo"), 1);
            TestCollection(3, logger);

            Assert.True(true);
        }

        [Fact]
        public void MessageTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            MessageTestExtensions.M0(logger);
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            MessageTestExtensions.M1(logger);
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/52527")]
        public void MessageTests_SuppressWarning_WarnAsError_NoError()
        {
            // Diagnostics produced by source generators do not respect the /warnAsError or /noWarn compiler flags.
            // These are handled fine by the logger generator and generate warnings. Unfortunately, the warning suppression is
            // not being observed by the C# compiler at the moment, so having these here causes build warnings.
#if false
            var logger = new MockLogger();

            logger.Reset();
            MessageTestExtensions.M2(logger, "Foo", "Bar");
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("p1", "Foo"),
                new KeyValuePair<string, object?>("p2", "Bar"),
                new KeyValuePair<string, object?>("{OriginalFormat}", string.Empty));
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            MessageTestExtensions.M3(logger, "Foo", 42);
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("p1", "Foo"),
                new KeyValuePair<string, object?>("p2", 42),
                new KeyValuePair<string, object?>("{OriginalFormat}", string.Empty));
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
#endif
        }

        [Fact]
        public void InstanceTests()
        {
            var logger = new MockLogger();
            var o = new TestInstances(logger);

            logger.Reset();
            o.M0();
            Assert.Null(logger.LastException);
            Assert.Equal("M0", logger.LastFormattedString);
            Assert.Equal(LogLevel.Error, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            o.M1("Foo");
            Assert.Null(logger.LastException);
            Assert.Equal("M1 Foo", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void LevelTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            LevelTestExtensions.M0(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M0", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M1(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M1", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M2(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M2", logger.LastFormattedString);
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M3(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M3", logger.LastFormattedString);
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M4(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M4", logger.LastFormattedString);
            Assert.Equal(LogLevel.Error, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M5(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M5", logger.LastFormattedString);
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M6(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M6", logger.LastFormattedString);
            Assert.Equal(LogLevel.None, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M7(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M7", logger.LastFormattedString);
            Assert.Equal((LogLevel)42, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M8(logger, LogLevel.Critical);
            Assert.Null(logger.LastException);
            Assert.Equal("M8", logger.LastFormattedString);
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M9(LogLevel.Trace, logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M9", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void ExceptionTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            ExceptionTestExtensions.M0(logger, new ArgumentException("Foo"), new ArgumentException("Bar"));
            Assert.Equal("Foo", logger.LastException!.Message);
            Assert.Equal("M0 System.ArgumentException: Bar", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ExceptionTestExtensions.M1(new ArgumentException("Foo"), logger, new ArgumentException("Bar"));
            Assert.Equal("Foo", logger.LastException!.Message);
            Assert.Equal("M1 System.ArgumentException: Bar", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void EventNameTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            EventNameTestExtensions.M0(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M0", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
            Assert.Equal("CustomEventName", logger.LastEventId.Name);
        }

        [Fact]
        public void TemplateTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            TemplateTestExtensions.M0(logger, 0);
            Assert.Null(logger.LastException);
            Assert.Equal("M0 0", logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("A1", 0),
                new KeyValuePair<string, object?>("{OriginalFormat}", "M0 {A1}"));

            logger.Reset();
            TemplateTestExtensions.M1(logger, 42);
            Assert.Null(logger.LastException);
            Assert.Equal("M1 42 42", logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("A1", 42),
                new KeyValuePair<string, object?>("{OriginalFormat}", "M1 {A1} {A1}"));

            logger.Reset();
            TemplateTestExtensions.M2(logger, 42, 43, 44, 45, 46, 47, 48);
            Assert.Null(logger.LastException);
            Assert.Equal("M2 42 43 44 45 46 47 48", logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("A1", 42),
                new KeyValuePair<string, object?>("a2", 43),
                new KeyValuePair<string, object?>("A3", 44),
                new KeyValuePair<string, object?>("a4", 45),
                new KeyValuePair<string, object?>("A5", 46),
                new KeyValuePair<string, object?>("a6", 47),
                new KeyValuePair<string, object?>("A7", 48),
                new KeyValuePair<string, object?>("{OriginalFormat}", "M2 {A1} {a2} {A3} {a4} {A5} {a6} {A7}"));

            logger.Reset();
            TemplateTestExtensions.M3(logger, 42, 43);
            Assert.Null(logger.LastException);
            Assert.Equal("M3 43 42", logger.LastFormattedString);
            AssertLastState(logger,
                new KeyValuePair<string, object?>("A1", 42),
                new KeyValuePair<string, object?>("a2", 43),
                new KeyValuePair<string, object?>("{OriginalFormat}", "M3 {a2} {A1}"));

        }

        private static void AssertLastState(MockLogger logger, params KeyValuePair<string, object?>[] expected)
        {
            var rol = (IReadOnlyList<KeyValuePair<string, object?>>)logger.LastState!;
            int count = 0;
            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Key, rol[count].Key);
                Assert.Equal(kvp.Value, rol[count].Value);
                count++;
            }
        }

        private static void TestCollection(int expected, MockLogger logger)
        {
            var rol = (logger.LastState as IReadOnlyList<KeyValuePair<string, object?>>)!;
            Assert.NotNull(rol);

            Assert.Equal(expected, rol.Count);
            for (int i = 0; i < expected; i++)
            {
                if (i != expected - 1)
                {
                    var kvp = new KeyValuePair<string, object?>($"p{i}", i);
                    Assert.Equal(kvp, rol[i]);
                }
            }

            int count = 0;
            foreach (var actual in rol)
            {
                if (count != expected - 1)
                {
                    var kvp = new KeyValuePair<string, object?>($"p{count}", count);
                    Assert.Equal(kvp, actual);
                }

                count++;
            }

            Assert.Equal(expected, count);
            Assert.Throws<IndexOutOfRangeException>(() => rol[expected]);
        }
    }
}
