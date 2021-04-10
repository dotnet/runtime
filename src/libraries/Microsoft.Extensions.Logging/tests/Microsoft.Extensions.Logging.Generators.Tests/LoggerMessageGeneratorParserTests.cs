// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Test
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test")]
    public class LoggerMessageGeneratorParserTests
    {
        [Fact]
        public async Task InvalidMethodName()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void __M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.InvalidLoggingMethodName.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingLogLevel()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingLogLevel.Id, d[0].Id);
        }

        [Fact]
        public async Task InvalidMethodBody()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    static partial void M1(ILogger logger);

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger)
                    {
                    }
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.LoggingMethodHasBody.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingTemplate()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""This is a message without foo"")]
                    static partial void M1(ILogger logger, string foo);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.ArgumentHasNoCorrespondingTemplate.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingArgument()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""{foo}"")]
                    static partial void M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.TemplateHasNoCorrespondingArgument.Id, d[0].Id);
        }

        [Fact]
        public async Task NeedlessQualifierInMessage()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, ""INFO: this is an informative message"")]
                    static partial void M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.RedundantQualifierInMessage.Id, d[0].Id);
        }

        [Fact]
        public async Task NeedlessExceptionInMessage()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {ex} {ex2}"")]
                    static partial void M1(ILogger logger, System.Exception ex, System.Exception ex2);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.ShouldntMentionExceptionInMessage.Id, d[0].Id);
        }

        [Fact]
        public async Task NeedlessLogLevelInMessage()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, ""M1 {l1} {l2}"")]
                    static partial void M1(ILogger logger, LogLevel l1, LogLevel l2);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.ShouldntMentionLogLevelInMessage.Id, d[0].Id);
        }

        [Fact]
        public async Task NeedlessLoggerInMessage()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {logger}"")]
                    static partial void M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.DontMentionLoggerInMessage.Id, d[0].Id);
        }

        [Fact]
        public async Task InvalidParameterName()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {__foo}"")]
                    static partial void M1(ILogger logger, string __foo);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.InvalidLoggingMethodParameterName.Id, d[0].Id);
        }

        [Fact]
        public async Task DateTimeAsParameterType()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {timeStamp}"")]
                    static partial void M1(ILogger logger, System.DateTime timeStamp);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.PassingDateTime.Id, d[0].Id);
        }

        [Fact]
        public async Task NestedType()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    public partial class Nested
                    {
                        [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.LoggingMethodInNestedType.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingExceptionType()
        {
            var d = await RunGenerator(@"
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

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingRequiredType.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingDateTimeType()
        {
            var d = await RunGenerator(@"
                namespace System
                {
                    public class Object {}
                    public class Void {}
                    public class Exception {}
                    public class String {}
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

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingRequiredType.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingStringType()
        {
            var d = await RunGenerator(@"
                namespace System
                {
                    public class Object {}
                    public class Void {}
                    public class Exception {}
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

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingRequiredType.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingEnumerableType()
        {
            var d = await RunGenerator(@"
                namespace System
                {
                    public class Object {}
                    public class Void {}
                    public class Exception {}
                    public struct DateTime {}
                    public class String {}
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

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingRequiredType.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingLoggerMessageAttributeType()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                }
            ", false, includeLoggingReferences: false);

            Assert.Empty(d);
        }

        [Fact]
        public async Task MissingILoggerType()
        {
            var d = await RunGenerator(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                }
            ", false, includeLoggingReferences: false);

            Assert.Empty(d);
        }

        [Fact]
        public async Task MissingLogLevelType()
        {
            var d = await RunGenerator(@"
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

            Assert.Empty(d);
        }

        [Fact]
        public async Task EventIdReuse()
        {
            var d = await RunGenerator(@"
                partial class MyClass
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M2(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.ShouldntReuseEventIds.Id, d[0].Id);
            Assert.Contains("in class MyClass", d[0].GetMessage(), StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task MethodReturnType()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial int M1(ILogger logger);

                    public static partial int M1(ILogger logger) { return 0; }
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.LoggingMethodMustReturnVoid.Id, d[0].Id);
        }

        [Fact]
        public async Task MissingILogger()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {p1}"")]
                    static partial void M1(int p1);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingLoggerArgument.Id, d[0].Id);
        }

        [Fact]
        public async Task NotStatic()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    partial void M1(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.LoggingMethodShouldBeStatic.Id, d[0].Id);
        }

        [Fact]
        public async Task NoILoggerField()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MissingLoggerField.Id, d[0].Id);
        }

        [Fact]
        public async Task MultipleILoggerFields()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    public ILogger _logger1;
                    public ILogger _logger2;

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.MultipleLoggerFields.Id, d[0].Id);
        }

        [Fact]
        public async Task NotPartial()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static void M1(ILogger logger) {}
                }
            ");

            Assert.Equal(2, d.Count);
            Assert.Equal(DiagDescriptors.LoggingMethodMustBePartial.Id, d[0].Id);
            Assert.Equal(DiagDescriptors.LoggingMethodHasBody.Id, d[1].Id);
        }

        [Fact]
        public async Task MethodGeneric()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1<T>(ILogger logger);
                }
            ");

            _ = Assert.Single(d);
            Assert.Equal(DiagDescriptors.LoggingMethodIsGeneric.Id, d[0].Id);
        }

        [Fact]
        public async Task Templates()
        {
            var d = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(1, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(2, LogLevel.Debug, ""M2 {arg1} {arg2}"")]
                    static partial void M2(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(3, LogLevel.Debug, ""M3 {arg1"")]
                    static partial void M3(ILogger logger);

                    [LoggerMessage(4, LogLevel.Debug, ""M4 arg1}"")]
                    static partial void M4(ILogger logger);

                    [LoggerMessage(5, LogLevel.Debug, ""M5 {"")]
                    static partial void M5(ILogger logger);

                    [LoggerMessage(6, LogLevel.Debug, ""}M6 "")]
                    static partial void M6(ILogger logger);

                    [LoggerMessage(7, LogLevel.Debug, ""M7 {{arg1}}"")]
                    static partial void M7(ILogger logger);
                }
            ");

            Assert.Empty(d);
        }

        [Fact]
        public async Task Cancellation()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                _ = await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ", cancellationToken: new CancellationToken(true)));
        }

        [Fact]
        public async Task SourceErrors()
        {
            var d = await RunGenerator(@"
                static partial class C
                {
                    // bogus argument type
                    [LoggerMessage(0, "", ""Hello"")]
                    static partial void M1(ILogger logger);

                    // missing parameter name
                    [LoggerMessage(1, LogLevel.Debug, ""Hello"")]
                    static partial void M2(ILogger);

                    // bogus parameter type
                    [LoggerMessage(2, LogLevel.Debug, ""Hello"")]
                    static partial void M3(XILogger logger);

                    // bogus enum value
                    [LoggerMessage(3, LogLevel.Foo, ""Hello"")]
                    static partial void M4(ILogger logger);

                    // attribute applied to something other than a method
                    [LoggerMessage(4, "", ""Hello"")]
                    int M5;
                }
            ");

            Assert.Empty(d);    // should fail quietly on broken code
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

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
            Assembly[]? refs = null;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
            if (includeLoggingReferences)
            {
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
                refs = new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! };
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
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
