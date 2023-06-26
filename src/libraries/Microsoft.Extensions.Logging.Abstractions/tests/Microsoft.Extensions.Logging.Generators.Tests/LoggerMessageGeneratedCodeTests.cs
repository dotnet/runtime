// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Generators.Tests.TestClasses;
using Microsoft.Extensions.Logging.Generators.Tests.TestClasses.UsesConstraintInAnotherNamespace;
using Xunit;
using NamespaceForABC;
using ConstraintInAnotherNamespace;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    public class LoggerMessageGeneratedCodeTests
    {
        [Fact]
        public void FindsLoggerFieldInBaseClass()
        {
            var logger = new MockLogger();

            logger.Reset();

            new DerivedClass(logger).Test();
            Assert.Equal("Test.", logger.LastFormattedString);
        }

        [Fact]
        public void FindsLoggerFieldInAnotherParialClass()
        {
            var logger = new MockLogger();

            logger.Reset();

            new PartialClassWithLoggerField(logger).Test();
            Assert.Equal("Test.", logger.LastFormattedString);
        }

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

            logger.Reset();
            MessageTestExtensions.M5(logger, LogLevel.Trace);
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(400_526_807, logger.LastEventId.Id);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            MessageTestExtensions.M6(logger, LogLevel.Trace);
            Assert.Null(logger.LastException);
            Assert.Equal(string.Empty, logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(6, logger.LastEventId.Id);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            MessageTestExtensions.M7(logger, LogLevel.Trace, "p", "q");
            Assert.Null(logger.LastException);
            Assert.Equal("\"p\" -> \"q\"", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(7, logger.LastEventId.Id);
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

            // [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "M0")]
            logger.Reset();
            o.M0();
            Assert.Null(logger.LastException);
            Assert.Equal("M0", logger.LastFormattedString);
            Assert.Equal(LogLevel.Error, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            // [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "M1 {p1}")]
            logger.Reset();
            o.M1("Foo");
            Assert.Null(logger.LastException);
            Assert.Equal("M1 Foo", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            // [LoggerMessage(LogLevel.Information, "M2 {p1}")]
            logger.Reset();
            o.M2("Bar");
            Assert.Null(logger.LastException);
            Assert.Equal("M2 Bar", logger.LastFormattedString);
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.Equal(350_193_950, logger.LastEventId.Id);

            // [LoggerMessage("M3 {p1}")]
            logger.Reset();
            o.M3(LogLevel.Critical, "Foo Bar");
            Assert.Null(logger.LastException);
            Assert.Equal("M3 Foo Bar", logger.LastFormattedString);
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Equal(366_971_569, logger.LastEventId.Id);

            // [LoggerMessage(LogLevel.Debug)]
            logger.Reset();
            o.M4();
            Assert.Null(logger.LastException);
            Assert.Equal("", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(383_749_188, logger.LastEventId.Id);

            // [LoggerMessage(level: LogLevel.Warning, message: "custom message {v}", eventId: 12341)]
            logger.Reset();
            o.M5("Hello");
            Assert.Null(logger.LastException);
            Assert.Equal("custom message Hello", logger.LastFormattedString);
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.Equal(12341, logger.LastEventId.Id);

            // [LoggerMessage(EventName = "My Event Name", Level = LogLevel.Information, Message = "M6 - {p1}")]
            logger.Reset();
            o.M6("Generate event Id");
            Assert.Null(logger.LastException);
            Assert.Equal("M6 - Generate event Id", logger.LastFormattedString);
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.Equal("My Event Name", logger.LastEventId.Name);
            Assert.Equal(26_601_394, logger.LastEventId.Id);

            // [LoggerMessage(Level = LogLevel.Warning, Message = "M7 - {p1}")]
            logger.Reset();
            o.M7("Generate event Id");
            Assert.Null(logger.LastException);
            Assert.Equal("M7 - Generate event Id", logger.LastFormattedString);
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.Equal("M7", logger.LastEventId.Name);
            Assert.Equal(434_082_045, logger.LastEventId.Id);

            // [LoggerMessage(EventId = 100, Level = LogLevel.Warning, Message = "M8 - {p1}")]
            logger.Reset();
            o.M8("Generate event Id");
            Assert.Null(logger.LastException);
            Assert.Equal("M8 - Generate event Id", logger.LastFormattedString);
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.Equal("M8", logger.LastEventId.Name);
            Assert.Equal(100, logger.LastEventId.Id);
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

            logger.Reset();
            LevelTestExtensions.M10vs11(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("event ID 10 vs. 11", logger.LastFormattedString);
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
            Assert.Equal(11, logger.LastEventId.Id);

            logger.Reset();
            LevelTestExtensions.M12(logger, LogLevel.Trace);
            Assert.Null(logger.LastException);
            Assert.Equal("M12 Trace", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LevelTestExtensions.M13(logger, LogLevel.Trace);
            Assert.Null(logger.LastException);
            Assert.Equal("M13 Microsoft.Extensions.Logging.Generators.Tests.MockLogger", logger.LastFormattedString);
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

            logger.Reset();
            ExceptionTestExtensions.M2(logger, "One", new ArgumentException("Foo"));
            Assert.Equal("Foo", logger.LastException!.Message);
            Assert.Equal("M2 One: System.ArgumentException: Foo", logger.LastFormattedString);
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

            logger.Reset();
            EventNameTestExtensions.CustomEventName(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("CustomEventName", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
            Assert.Equal("CustomEventName", logger.LastEventId.Name);
        }

        [Fact]
        public void SkipEnabledCheckTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            logger.Enabled = false;
            Assert.False(logger.IsEnabled(LogLevel.Information));
            SkipEnabledCheckExtensions.LoggerMethodWithFalseSkipEnabledCheck(logger);
            Assert.Null(logger.LastException);
            Assert.Null(logger.LastFormattedString);
            Assert.Equal((LogLevel)(-1), logger.LastLogLevel);
            Assert.Equal(0, logger.CallCount);
            Assert.Equal(default, logger.LastEventId);

            logger.Reset();
            logger.Enabled = false;
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            SkipEnabledCheckExtensions.LoggerMethodWithTrueSkipEnabledCheck(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("Message: When using SkipEnabledCheck, the generated code skips logger.IsEnabled(logLevel) check before calling log. To be used when consumer has already guarded logger method in an IsEnabled check.", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
            Assert.Equal("LoggerMethodWithTrueSkipEnabledCheck", logger.LastEventId.Name);
        }
        private struct MyStruct { }

        [Fact]
        public void ConstraintsTests()
        {
            var logger = new MockLogger();

            var printer = new MessagePrinter<Message>();
            logger.Reset();
            printer.Print(logger, new Message() { Text = "Hello" });
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("The message is Hello.", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            var printer2 = new MessagePrinterHasConstraintOnLogClassAndLogMethod<Message>();
            logger.Reset();
            printer2.Print(logger, new Message() { Text = "Hello" });
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("The message is `Hello`.", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions<Object>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions1<MyStruct>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions2<MyStruct>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions3<MyStruct>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions4<Attribute>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ConstraintsTestExtensions5<MyStruct>.M0(logger, 12);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("M012", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void NestedClassTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            NestedClassTestsExtensions<ABC>.NestedMiddleParentClass.NestedClass.M8(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M8", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            NonStaticNestedClassTestsExtensions<ABC>.NonStaticNestedMiddleParentClass.NestedClass.M9(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M9", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            NestedStruct.Logger.M10(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M10", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            NestedRecord.Logger.M11(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M11", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            MultiLevelNestedClass.NestedStruct.NestedRecord.Logger.M12(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M12", logger.LastFormattedString);
            Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
            Assert.Equal(1, logger.CallCount);
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

        [Fact]
        public void OverloadTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            OverloadTestExtensions.M0(logger, 1);
            Assert.Null(logger.LastException);
            Assert.Equal($"{nameof(OverloadTestExtensions.M0)}1", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal("M0", logger.LastEventId.Name);

            logger.Reset();
            OverloadTestExtensions.M0(logger, "string");
            Assert.Null(logger.LastException);
            Assert.Equal($"{nameof(OverloadTestExtensions.M0)}string", logger.LastFormattedString);
            Assert.Equal(LogLevel.Trace, logger.LastLogLevel);
            Assert.Equal("M0", logger.LastEventId.Name);
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
