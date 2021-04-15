// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerMessageTest
    {
        [Fact]
        public void LogMessage()
        {
            // Arrange
            var controller = "home";
            var action = "index";
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            testLogger.ActionMatched(controller, action);

            // Assert
            Assert.Single(testSink.Writes);
            var writeContext = testSink.Writes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(writeContext.State);
            AssertLogValues(
                new[] {
                    new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ActionMatchedInfo.NamedStringFormat),
                    new KeyValuePair<string, object>("controller", controller),
                    new KeyValuePair<string, object>("action", action)
                },
                actualLogValues.ToArray());
            Assert.Equal(LogLevel.Information, writeContext.LogLevel);
            Assert.Equal(1, writeContext.EventId);
            Assert.Null(writeContext.Exception);
            Assert.Equal(
                string.Format(
                    TestLoggerExtensions.ActionMatchedInfo.FormatString,
                    controller,
                    action),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithoutAnyParameters()
        {
            // Arrange
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithoutAnyParams();

            // Assert
            Assert.NotNull(disposable);
            Assert.Empty(testSink.Writes);
            Assert.Single(testSink.Scopes);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeWithoutAnyParameters.Message)
            },
            actualLogValues.ToArray());
            Assert.Equal(
                TestLoggerExtensions.ScopeWithoutAnyParameters.Message,
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithOneParameter()
        {
            // Arrange
            var param1 = Guid.NewGuid().ToString();
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithOneParam(param1);

            // Assert
            Assert.NotNull(disposable);
            Assert.Empty(testSink.Writes);
            Assert.Single(testSink.Scopes);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("RequestId", param1),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeWithOneParameter.NamedStringFormat)
            },
            actualLogValues.ToArray());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeWithOneParameter.FormatString, param1),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithTwoParameters()
        {
            // Arrange
            var param1 = "foo";
            var param2 = "bar";
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithTwoParams(param1, param2);

            // Assert
            Assert.NotNull(disposable);
            Assert.Empty(testSink.Writes);
            Assert.Single(testSink.Scopes);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("param1", param1),
                new KeyValuePair<string, object>("param2", param2),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeInfoWithTwoParameters.NamedStringFormat)
            },
            actualLogValues.ToArray());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeInfoWithTwoParameters.FormatString, param1, param2),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithThreeParameters()
        {
            // Arrange
            var param1 = "foo";
            var param2 = "bar";
            int param3 = 10;
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithThreeParams(param1, param2, param3);

            // Assert
            Assert.NotNull(disposable);
            Assert.Empty(testSink.Writes);
            Assert.Single(testSink.Scopes);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("param1", param1),
                new KeyValuePair<string, object>("param2", param2),
                new KeyValuePair<string, object>("param3", param3),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeInfoWithThreeParameters.NamedStringFormat)
            },
            actualLogValues.ToArray());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeInfoWithThreeParameters.FormatString, param1, param2, param3),
                actualLogValues.ToString());
        }

        [Theory]
        [MemberData(nameof(LogMessagesData))]
        public void LogMessages(Delegate messageDelegate, int argumentCount)
        {
            // Arrange
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);
            var exception = new Exception("TestException");
            var parameterNames = Enumerable.Range(0, argumentCount).Select(i => "P" + i).ToArray();
            var parameters = new List<object>();
            parameters.Add(testLogger);
            parameters.AddRange(parameterNames);
            parameters.Add(exception);

            var expectedFormat = "Log " + string.Join(" ", parameterNames.Select(p => "{" + p + "}"));
            var expectedToString = "Log " + string.Join(" ", parameterNames);
            var expectedValues = parameterNames.Select(p => new KeyValuePair<string, object>(p, p)).ToList();
            expectedValues.Add(new KeyValuePair<string, object>("{OriginalFormat}", expectedFormat));

            // Act
            messageDelegate.DynamicInvoke(parameters.ToArray());

            // Assert
            Assert.Single(testSink.Writes);
            var write = testSink.Writes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(write.State);
            AssertLogValues(expectedValues, actualLogValues.ToList());
            Assert.Equal(expectedToString, actualLogValues.ToString());
        }

        [Fact]
        public void LogMessage_WithNullParameter_DoesNotMutateArgument()
        {
            // Arrange
            string format = "TestMessage {param1} {param2} {param3}";
            string param1 = "foo";
            string param2 = null;
            int param3 = 10;
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            testLogger.LogInformation(format, param1, param2, param3);

            // Assert
            Assert.Single(testSink.Writes);
            var write = testSink.Writes.First();
            var actualLogValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(write.State);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("param1", param1),
                new KeyValuePair<string, object>("param2", param2),
                new KeyValuePair<string, object>("param3", param3),
                new KeyValuePair<string, object>("{OriginalFormat}", format)
            },
            actualLogValues.ToArray());
        }

        [Fact]
        public void DefineMessage_WithNoParameters_ThrowsException_WhenFormatString_HasNamedParameters()
        {
            // Arrange
            var formatString = "Action with name {ActionName} not found.";
            var expectedMessage = $"The format string '{formatString}' does not have the expected number " +
                    $"of named parameters. Expected 0 parameter(s) but found 1 parameter(s).";

            // Act
            var exception = Assert.Throws<ArgumentException>(() => LoggerMessage.Define(LogLevel.Error, 0, formatString));

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void DefineMessage_ThrowsException_WhenExpectedFormatStringParameterCount_NotFound(
            int expectedNamedParameterCount)
        {
            // Arrange
            var formatString = "Action with name ActionName not found.";
            var expectedMessage = $"The format string '{formatString}' does not have the expected number " +
                    $"of named parameters. Expected {expectedNamedParameterCount} parameter(s) but found 0 parameter(s).";

            // Act
            Exception exception = null;
            switch (expectedNamedParameterCount)
            {
                case 1:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string>(LogLevel.Error, 0, formatString));
                    break;
                case 2:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string, string>(LogLevel.Error, 0, formatString));
                    break;
                case 3:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string, string, string>(LogLevel.Error, 0, formatString));
                    break;
                case 4:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string, string, string, string>(LogLevel.Error, 0, formatString));
                    break;
                case 5:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string, string, string, string, string>(LogLevel.Error, 0, formatString));
                    break;
                case 6:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.Define<string, string, string, string, string, string>(LogLevel.Error, 0, formatString));
                    break;
                default:
                    throw new ArgumentException($"Invalid value for '{nameof(expectedNamedParameterCount)}'");
            }

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void DefineScope_WithNoParameters_ThrowsException_WhenFormatString_HasNamedParameters()
        {
            // Arrange
            var formatString = "Starting request scope for request id {RequestId}";
            var expectedMessage = $"The format string '{formatString}' does not have the expected number " +
                    $"of named parameters. Expected 0 parameter(s) but found 1 parameter(s).";

            // Act
            var exception = Assert.Throws<ArgumentException>(() => LoggerMessage.DefineScope(formatString));

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void DefineScope_ThrowsException_WhenExpectedFormatStringParameterCount_NotFound(
            int expectedNamedParameterCount)
        {
            // Arrange
            var formatString = "Starting request scope for request id RequestId";
            var expectedMessage = $"The format string '{formatString}' does not have the expected number " +
                    $"of named parameters. Expected {expectedNamedParameterCount} parameter(s) but found 0 parameter(s).";

            // Act
            Exception exception = null;
            switch (expectedNamedParameterCount)
            {
                case 1:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string>(formatString));
                    break;
                case 2:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string, string>(formatString));
                    break;
                case 3:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string, string, string>(formatString));
                    break;
                case 4:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string, string, string, string>(formatString));
                    break;
                case 5:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string, string, string, string, string>(formatString));
                    break;
                case 6:
                    exception = Assert.Throws<ArgumentException>(
                        () => LoggerMessage.DefineScope<string, string, string, string, string, string>(formatString));
                    break;
                default:
                    throw new ArgumentException($"Invalid value for '{nameof(expectedNamedParameterCount)}'");
            }

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [MemberData(nameof(DefineMethodsWithInvalidParametersData))]
        public void DefineAndDefineScope_ThrowsException_WhenFormatString_IsNull(Delegate method, object[] parameters)
        {
            // Act
            var exception = Assert.Throws<TargetInvocationException>(
                () => method.DynamicInvoke(parameters));

            // Assert
            Assert.IsType<ArgumentNullException>(exception.InnerException);
        }

        public static IEnumerable<object[]> DefineMethodsWithInvalidParametersData => new[]
        {
            new object[] { (Define)LoggerMessage.Define, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string>, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string, string>, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string, string, string>, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string, string, string, string>, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string, string, string, string, string>, DefineInvalidParameters },
            new object[] { (Define)LoggerMessage.Define<string, string, string, string, string, string>, DefineInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string>, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string, string>, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string, string, string>, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string, string, string, string>, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string, string, string, string, string>, DefineScopeInvalidParameters },
            new object[] { (DefineScope)LoggerMessage.DefineScope<string, string, string, string, string, string>, DefineScopeInvalidParameters }
        };

        public static IEnumerable<object[]> LogMessagesData => new[]
        {
            new object[] { LoggerMessage.Define(LogLevel.Error, 0, "Log "), 0 },
            new object[] { LoggerMessage.Define<string>(LogLevel.Error, 1, "Log {P0}"), 1 },
            new object[] { LoggerMessage.Define<string, string>(LogLevel.Error, 2, "Log {P0} {P1}"), 2 },
            new object[] { LoggerMessage.Define<string, string, string>(LogLevel.Error, 3, "Log {P0} {P1} {P2}"), 3 },
            new object[] { LoggerMessage.Define<string, string, string, string>(LogLevel.Error, 4, "Log {P0} {P1} {P2} {P3}"), 4 },
            new object[] { LoggerMessage.Define<string, string, string, string, string>(LogLevel.Error, 5, "Log {P0} {P1} {P2} {P3} {P4}"), 5 },
            new object[] { LoggerMessage.Define<string, string, string, string, string, string>(LogLevel.Error, 6, "Log {P0} {P1} {P2} {P3} {P4} {P5}"), 6 },
        };

        private delegate Delegate Define(LogLevel logLevel, EventId eventId, string formatString);
        private delegate Delegate DefineScope(string formatString);
        private static object[] DefineInvalidParameters => new object[] { LogLevel.Error, new EventId(0), null };
        private static object[] DefineScopeInvalidParameters => new object[] { null };

        private void AssertLogValues(
            IEnumerable<KeyValuePair<string, object>> expected,
            IEnumerable<KeyValuePair<string, object>> actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                throw new EqualException(expected, actual);
            }

            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            Assert.Equal(expected.Count(), actual.Count());

            // we do not care about the order of the log values
            expected = expected.OrderBy(kvp => kvp.Key);
            actual = actual.OrderBy(kvp => kvp.Key);

            Assert.Equal(expected, actual);
        }
    }
}
