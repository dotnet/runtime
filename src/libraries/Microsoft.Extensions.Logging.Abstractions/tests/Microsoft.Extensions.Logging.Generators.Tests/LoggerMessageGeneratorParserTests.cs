// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public class LoggerMessageGeneratorParserTests
    {
        [Fact]
        public async Task Valid_AdditionalAttributes()
        {
            Assert.Empty(await RunGenerator($@"
                using System.Diagnostics.CodeAnalysis;
                partial class C
                {{
                    [SuppressMessage(""CATEGORY1"", ""SOMEID1"")]
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    [SuppressMessage(""CATEGORY2"", ""SOMEID2"")]
                    static partial void M1(ILogger logger);
                }}
            "));
        }

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

        [Fact]
        public async Task InvalidMethodExpressionBody()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger) => throw new Exception();
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

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2 {arg1}: {ex}"")]
                    static partial void M2(ILogger logger, string arg1, System.Exception ex);
                }
            ");

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionExceptionInMessage.Id, diagnostics[0].Id);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionExceptionInMessage.Id, diagnostics[1].Id);
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

                    [LoggerMessage(EventId = 2, Message = ""M2 {logger}"")]
                    static partial void M2(ILogger logger, LogLevel level);
                }
            ");

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionLoggerInMessage.Id, diagnostics[0].Id);
            Assert.Equal(DiagnosticDescriptors.ShouldntMentionLoggerInMessage.Id, diagnostics[1].Id);
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

#if ROSLYN4_0_OR_GREATER
        [Fact]
        public async Task FileScopedNamespaceOK()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using Microsoft.Extensions.Logging;

                namespace MyLibrary;

                internal partial class Logger
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = ""Hello {Name}!"")]
                    public static partial void Greeting(ILogger logger, string name);
                }
            ");

            Assert.Empty(diagnostics);
        }
#endif

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
                    public abstract class Attribute {}
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
                    public class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                    [Microsoft.Extensions.Logging.LoggerMessage]
                    public static partial void Log(ILogger logger);
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
        public async Task EventNameReuse()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class MyClass
                {
                    [LoggerMessage(EventId = 0, EventName = ""MyEvent"", Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 1, EventName = ""MyEvent"", Level = LogLevel.Debug, Message = ""M1"")]
                    static partial void M2(ILogger logger);
                }
            ");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.ShouldntReuseEventNames.Id, diagnostics[0].Id);
            Assert.Contains("MyEvent", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
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

        [Theory]
        [InlineData("ref")]
        [InlineData("in")]
        public async Task SupportsRefKindsInAndRef(string modifier)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                partial class C
                {{
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""Parameter {{P1}}"")]
                    static partial void M(ILogger logger, {modifier} int p1);
                }}");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task InvalidRefKindsOut()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                partial class C
                {{
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""Parameter {{P1}}"")]
                    static partial void M(ILogger logger, out int p1);
                }}");

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.InvalidLoggingMethodParameterOut.Id, diagnostics[0].Id);
            Assert.Contains("p1", diagnostics[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task MalformedFormatString()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1 {A} M1 { M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2 {A} M2 } M2"")]
                    static partial void M2(ILogger logger);

                    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = ""M3 {arg1"")]
                    static partial void M3(ILogger logger);

                    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""M4 arg1}"")]
                    static partial void M4(ILogger logger);

                    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = ""M5 {"")]
                    static partial void M5(ILogger logger);

                    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = ""}M6 "")]
                    static partial void M6(ILogger logger);

                    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = ""{M7{"")]
                    static partial void M7(ILogger logger);

                    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = ""{{{arg1 M8"")]
                    static partial void M8(ILogger logger);

                    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = ""arg1}}} M9"")]
                    static partial void M9(ILogger logger);

                    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = ""{} M10"")]
                    static partial void M10(ILogger logger);

                    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = ""{ } M11"")]
                    static partial void M11(ILogger logger);
                }
            ");

            Assert.Equal(11, diagnostics.Count);
            foreach (var diagnostic in diagnostics)
            {
                Assert.Equal(DiagnosticDescriptors.MalformedFormatStrings.Id, diagnostic.Id);
            }
        }

        [Fact]
        public async Task ValidTemplates()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = """")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2"")]
                    static partial void M2(ILogger logger);

                    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = ""{arg1}"")]
                    static partial void M3(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""M4 {arg1}"")]
                    static partial void M4(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = ""{arg1} M5"")]
                    static partial void M5(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = ""M6{arg1}M6{arg2}M6"")]
                    static partial void M6(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = ""M7 {{const}}"")]
                    static partial void M7(ILogger logger);

                    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = ""{{prefix{{{arg1}}}suffix}}"")]
                    static partial void M8(ILogger logger, string arg1);

                    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = ""prefix }}"")]
                    static partial void M9(ILogger logger);

                    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = ""}}suffix"")]
                    static partial void M10(ILogger logger);
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

        [Fact]
        internal void MultipleTypeDefinitions()
        {
            // Adding a dependency to an assembly that has internal definitions of public types
            // should not result in a collision and break generation.
            // Verify usage of the extension GetBestTypeByMetadataName(this Compilation) instead of Compilation.GetTypeByMetadataName().
            var referencedSource = @"
                namespace Microsoft.Extensions.Logging
                {
                    internal class LoggerMessageAttribute { }
                }
                namespace Microsoft.Extensions.Logging
                {
                    internal interface ILogger { }
                    internal enum LogLevel { }
                }";

            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

            // Obtain the image of the referenced assembly.
            byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

            // Generate the code
            string source = @"
                namespace Test
                {
                    using Microsoft.Extensions.Logging;

                    partial class C
                    {
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
            LoggerMessageGenerator generator = new LoggerMessageGenerator();

            (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources) =
                RoslynTestUtils.RunGenerator(compilation, generator);

            // Make sure compilation was successful.
            Assert.Empty(diagnostics);
            Assert.Equal(1, generatedSources.Length);
            Assert.Equal(21, generatedSources[0].SourceText.Lines.Count);
        }
        [Theory]
        [InlineData("{request}", "request")]
        [InlineData("{request}", "@request")]
        [InlineData("{@request}", "request")]
        [InlineData("{@request}", "@request")]
        public async Task AtSymbolArgument(string stringTemplate, string parameterName)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                partial class C
                {{
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""{stringTemplate}"")]
                    static partial void M1(ILogger logger, string {parameterName});
                }}
            ");

            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("{request}", "request")]
        [InlineData("{request}", "@request")]
        [InlineData("{@request}", "request")]
        [InlineData("{@request}", "@request")]
        public async Task AtSymbolArgumentOutOfOrder(string stringTemplate, string parameterName)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                partial class C
                {{
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""{stringTemplate} {{a1}}"")]
                    static partial void M1(ILogger logger,string a1, string {parameterName});
                }}
            ");

            Assert.Empty(diagnostics);
        }

        [Fact]
        public static void SyntaxListWithManyItems()
        {
            const int nItems = 200000;
            var builder = new System.Text.StringBuilder();
            builder.AppendLine(
                """
                using Microsoft.Extensions.Logging;
                class Program
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "M1")]
                    static partial void M1(ILogger logger)
                    {
                """);
            builder.AppendLine("    int[] values = new[] { ");
            for (int i = 0; i < nItems; i++)
            {
                builder.Append("0, ");
            }
            builder.AppendLine("};");
            builder.AppendLine("}");
            builder.AppendLine("}");

            string source = builder.ToString();
            Compilation compilation = CompilationHelper.CreateCompilation(source);
            LoggerMessageGenerator generator = new LoggerMessageGenerator();

            (ImmutableArray<Diagnostic> diagnostics, _) =
                RoslynTestUtils.RunGenerator(compilation, generator);

            Assert.Single(diagnostics);
            Assert.Equal(DiagnosticDescriptors.LoggingMethodHasBody.Id, diagnostics[0].Id);
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
