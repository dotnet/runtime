// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/32743", TestRuntimes.Mono)]
    public class LoggerMessageGeneratorParserTests
    {
        [Fact]
        public async Task InvalidMethodName()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void __M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.InvalidLoggingMethodName.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task MissingLogLevel()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Message = ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MissingLogLevel.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task InvalidMethodBody()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger)
                    {
                    }
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodHasBody.Id, diagnostics[0].Id);
        }

        [Theory]
        [InlineData("EventId = 0, Level = null, Message = \"This is a message with {foo}\"")]
        [InlineData("eventId: 0, level: null, message: \"This is a message with {foo}\"")]
        [InlineData("0, null, \"This is a message with {foo}\"")]
        public async Task WithNullLevel_GeneratorWontFail(string argumentList)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator($@"
                partial class C
                {{
                    [LoggerMessage({argumentList})]
                    static partial void M1(ILogger logger, string foo);
                    
                    [LoggerMessage({argumentList})]
                    static partial void M2(ILogger logger, LogLevel level, string foo);
                }}
            ");

            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("EventId = null, Level = LogLevel.Debug, Message = \"This is a message with {foo}\"")]
        [InlineData("eventId: null, level: LogLevel.Debug, message: \"This is a message with {foo}\"")]
        [InlineData("null, LogLevel.Debug, \"This is a message with {foo}\"")]
        public async Task WithNullEventId_GeneratorWontFail(string argumentList)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator($@"
                partial class C
                {{
                    [LoggerMessage({argumentList})]
                    static partial void M1(ILogger logger, string foo);
                }}
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task WithNullMessage_GeneratorWontFail()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = null)]
                    static partial void M1(ILogger logger, string foo);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate.Id, diagnostics[0].Id);
            Assert.Contains("foo", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task WithNullSkipEnabledCheck_GeneratorWontFail()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""This is a message with {foo}"", SkipEnabledCheck = null)]
                    static partial void M1(ILogger logger, string foo);
                }
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task WithBadMisconfiguredInput_GeneratorWontFail()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                public static partial class C
                {
                    [LoggerMessage(SkipEnabledCheck = 6)]
                    public static partial void M0(ILogger logger, LogLevel level);

                    [LoggerMessage(eventId: true, level: LogLevel.Debug, message: ""misconfigured eventId as bool"")]
                    public static partial void M1(ILogger logger);
                }
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task MissingTemplate()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""This is a message without foo"")]
                    static partial void M1(ILogger logger, string foo);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate.Id, diagnostics[0].Id);
            Assert.Contains("foo", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task MissingArgument()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""{foo}"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.TemplateHasNoCorrespondingArgument.Id, diagnostics[0].Id);
            Assert.Contains("foo", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task NeedlessQualifierInMessage()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = ""INFO: this is an informative message"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.RedundantQualifierInMessage.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NeedlessExceptionInMessage()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {ex} {ex2}"")]
                    static partial void M1(ILogger logger, System.Exception ex, System.Exception ex2);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionExceptionInMessage.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NeedlessLogLevelInMessage()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Message = ""M1 {l1} {l2}"")]
                    static partial void M1(ILogger logger, LogLevel l1, LogLevel l2);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionLogLevelInMessage.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NeedlessLoggerInMessage()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {logger}"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionLoggerInMessage.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task DoubleLogLevel_InAttributeAndAsParameterButMissingInTemplate_ProducesDiagnostic()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger, LogLevel levelParam);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task LogLevelDoublySet_AndInMessageTemplate_ProducesDiagnostic()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {level2}"")]
                    static partial void M1(ILogger logger, LogLevel level1, LogLevel level2);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task DoubleLogLevel_FirstOneSetAsMethodParameter_SecondOneInMessageTemplate_Supported()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Message = ""M1 {level2}"")]
                    static partial void M1(ILogger logger, LogLevel level1, LogLevel level2);
                }
            ");

            Assert.Empty(diagnostics);
        }

#if false
        // TODO: can't have the same template with different casing
        [Fact]
        public async Task InconsistentTemplateCasing()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {p1} {P1}"")]
                    static partial void M1(ILogger logger, int p1, int P1);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.InconsistentTemplateCasing.Id, diagnostics[0].Id);
        }

        // TODO: can't have malformed format strings (like dangling {, etc)
        [Fact]
        public async Task MalformedFormatString()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {p1} {P1}"")]
                    static partial void M1(ILogger logger, int p1, int P1);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MalformedFormatStrings.Id, diagnostics[0].Id);
        }
#endif

        [Fact]
        public async Task InvalidParameterName()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {__foo}"")]
                    static partial void M1(ILogger logger, string __foo);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.InvalidLoggingMethodParameterName.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NestedTypeOK()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    public partial class Nested
                    {
                        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }
            ");

            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("true")]
        [InlineData("null")]
        public async Task UsingSkipEnabledCheck(string skipEnabledCheckValue)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator($@"
                partial class C
                {{
                    public partial class WithLoggerMethodUsingSkipEnabledCheck
                    {{
                        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"", SkipEnabledCheck = {skipEnabledCheckValue})]
                        static partial void M1(ILogger logger);
                    }}
                }}
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task MissingExceptionType()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                namespace System
                {
                    public class Object {}
                    public class Void {}
                    public class String {}
                    public struct DateTime {}
                }
                namespace System.Collections
                {
                    public interface IEnumerable {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public enum LogLevel {}
                    public interface ILogger {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public class LoggerMessageAttribute {}
                }
                partial class C
                {
                }
            ", false, includeBaseReferences: false, includeLoggingReferences: false);

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MissingRequiredType.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task MissingLoggerMessageAttributeType()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                }
            ", false, includeLoggingReferences: false);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task MissingILoggerType()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                }
            ", false, includeLoggingReferences: false);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task MissingLogLevelType()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public interface ILogger {}
                }
                partial class C
                {
                }
            ", false, includeLoggingReferences: false);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task EventIdReuse()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class MyClass
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M2(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ShouldntReuseEventIds.Id, diagnostics[0].Id);
            Assert.Contains("MyClass", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task MethodReturnType()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    public static partial int M1(ILogger logger);

                    public static partial int M1(ILogger logger) { return 0; }
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodMustReturnVoid.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task MissingILogger()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1 {p1}"")]
                    static partial void M1(int p1);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MissingLoggerArgument.Id, diagnostics[0].Id);
            string message = diagnostics[0].GetMessage();
            Assert.Contains("M1", message, StringComparison.InvariantCulture);
            Assert.Contains("Microsoft.Extensions.Logging.ILogger", message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task NotStatic()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    partial void M1(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodShouldBeStatic.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NoILoggerField()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    public partial void M1();
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MissingLoggerField.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task MultipleILoggerFields()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    public ILogger _logger1;
                    public ILogger _logger2;

                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    public partial void M1();
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.MultipleLoggerFields.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task NotPartial()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static void M1(ILogger logger) {}
                }
            ");

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodMustBePartial.Id, diagnostics[0].Id);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodHasBody.Id, diagnostics[1].Id);
        }

        [Fact]
        public async Task MethodGeneric()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1<T>(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodIsGeneric.Id, diagnostics[0].Id);
        }

        [Fact]
        public async Task Templates()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2 {arg1} {arg2}"")]
                    static partial void M2(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = ""M3 {arg1"")]
                    static partial void M3(ILogger logger);

                    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""M4 arg1}"")]
                    static partial void M4(ILogger logger);

                    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = ""M5 {"")]
                    static partial void M5(ILogger logger);

                    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = ""}M6 "")]
                    static partial void M6(ILogger logger);

                    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = ""M7 {{arg1}}"")]
                    static partial void M7(ILogger logger);
                }
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Cancellation()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ", cancellationToken: new CancellationToken(true)));
        }

        [Fact]
        public async Task SourceErrors()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                static partial class C
                {
                    // bogus argument type
                    [LoggerMessage(EventId = 0, Level = "", Message = ""Hello"")]
                    static partial void M1(ILogger logger);

                    // missing parameter name
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""Hello"")]
                    static partial void M2(ILogger);

                    // bogus parameter type
                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""Hello"")]
                    static partial void M3(XILogger logger);

                    // attribute applied to something other than a method
                    [LoggerMessage(EventId = 4, Message = ""Hello"")]
                    int M5;
                }
            ");

            Assert.Empty(diagnostics);    // should fail quietly on broken code
        }

        private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
            string code,
            bool wrap = true,
            bool inNamespace = true,
            bool includeBaseReferences = true,
            bool includeLoggingReferences = true,
            CancellationToken cancellationToken = default)
        {
            var text = code;
            if (wrap)
            {
                var nspaceStart = "namespace Test {";
                var nspaceEnd = "}";
                if (!inNamespace)
                {
                    nspaceStart = "";
                    nspaceEnd = "";
                }

                text = $@"
                    {nspaceStart}
                    using Microsoft.Extensions.Logging;
                    {code}
                    {nspaceEnd}
                ";
            }

            Assembly[]? refs = null;
            if (includeLoggingReferences)
            {
                refs = new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly };
            }

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                refs,
                new[] { text },
                includeBaseReferences: includeBaseReferences,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return d;
        }
    }
}
