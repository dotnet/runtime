// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public class LoggerMessageGeneratorEmitterTests
    {
        [Fact]
        public async Task TestEmitter()
        {
            // The functionality of the resulting code is tested via LoggerMessageGeneratedCodeTests.cs
            string[] sources = Directory.GetFiles("TestClasses");
            foreach (var src in sources)
            {
                var testSourceCode = await File.ReadAllTextAsync(src).ConfigureAwait(false);

                var (d, r) = await RoslynTestUtils.RunGenerator(
                    new LoggerMessageGenerator(),
                    new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                    new[] { testSourceCode }).ConfigureAwait(false);

                Assert.True(src.Contains("WithDiagnostics") ? !d.IsEmpty : d.IsEmpty);
                Assert.Single(r);
            }
        }

        [Fact]
        public async Task TestBaseline_TestWithSkipEnabledCheck_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithSkipEnabledCheck
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = ""Message: When using SkipEnabledCheck, the generated code skips logger.IsEnabled(logLevel) check before calling log. To be used when consumer has already guarded logger method in an IsEnabled check."", SkipEnabledCheck = true)]
        public static partial void M0(ILogger logger);
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithSkipEnabledCheck.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestWithDefaultValues_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithDefaultValues
    {
        [LoggerMessage]
        public static partial void M0(ILogger logger, LogLevel level);
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithDefaultValues.generated.txt", testSourceCode);
        }

        [Theory]
        [InlineData("EventId = 0, Level = LogLevel.Error, Message = \"M0 {a1} {a2}\"")]
        [InlineData("eventId: 0, level: LogLevel.Error, message: \"M0 {a1} {a2}\"")]
        [InlineData("0, LogLevel.Error, \"M0 {a1} {a2}\"")]
        [InlineData("0, LogLevel.Error, \"M0 {a1} {a2}\", SkipEnabledCheck = false")]
        public async Task TestBaseline_TestWithTwoParams_Success(string argumentList)
        {
            string testSourceCode = $@"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{{
    internal static partial class TestWithTwoParams
    {{
        [LoggerMessage({argumentList})]
        public static partial void M0(ILogger logger, int a1, System.Collections.Generic.IEnumerable<int> a2);
    }}
}}";
            await VerifyAgainstBaselineUsingFile("TestWithTwoParams.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestWithMoreThan6Params_Success()
        {
            // TODO: Remove support for more than 6 arguments
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithMoreThan6Params
    {
        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = ""M9 {p1} {p2} {p3} {p4} {p5} {p6} {p7}"")]
        public static partial void Method9(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, System.Collections.Generic.IEnumerable<int> p7);
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithMoreThan6Params.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestWithDynamicLogLevel_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithDynamicLogLevel
    {
        [LoggerMessage(EventId = 9, Message = ""M9"")]
        public static partial void M9(LogLevel level, ILogger logger);
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithDynamicLogLevel.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestWithNestedClass_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    namespace NestedNamespace
    {
        public static partial class MultiLevelNestedClass
        {
            public partial struct NestedStruct
            {
                internal partial record NestedRecord(string Name, string Address)
                { 
                    internal static partial class NestedClassTestsExtensions<T1> where T1 : Class1
                    {
                        internal static partial class NestedMiddleParentClass
                        {
                            internal static partial class Nested<T2> where T2 : Class2
                            {
                                [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = ""M9"")]
                                public static partial void M9(ILogger logger);
                            }
                        }
                    }
                }
            }
        }
        internal class Class1 { }
        internal class Class2 { }
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithNestedClass.generated.txt", testSourceCode);
        }

#if ROSLYN4_0_OR_GREATER
        [Fact]
        public async Task TestBaseline_TestWithFileScopedNamespace_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses;

internal static partial class TestWithDefaultValues
{
    [LoggerMessage]
    public static partial void M0(ILogger logger, LogLevel level);
}";
            await VerifyAgainstBaselineUsingFile("TestWithDefaultValues.generated.txt", testSourceCode);
        }
#endif

        [Fact]
        public async Task TestBaseline_TestWithNestedClassWithGenericTypesWithAttributes_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    public partial class GenericTypeWithAttribute<[Foo] A, [Bar] B, C>
    {
        public void M0<D>(A a, B b, C c, ILogger logger) => Log<D>.M0(logger, a, b, c);
        private static partial class Log<[Foo] D>
        {
            [LoggerMessage(EventId = 42, Level = LogLevel.Debug, Message = ""a = {a}; b = {b}; c = {c}"")]
            public static partial void M0(ILogger logger, A a, B b, C c);
        }
    }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class FooAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class BarAttribute : Attribute { }
}";
            await VerifyAgainstBaselineUsingFile("TestWithNestedClassWithGenericTypesWithAttributes.generated.txt", testSourceCode);
        }

#if ROSLYN4_8_OR_GREATER
        [Fact]
        public async Task TestBaseline_TestWithLoggerFromPrimaryConstructor_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal partial class TestWithLoggerFromPrimaryConstructor(ILogger logger)
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M0"")]
        public partial void M0();
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithLoggerFromPrimaryConstructor.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestWithLoggerInFieldAndFromPrimaryConstructor_UsesField()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal partial class TestWithLoggerFromPrimaryConstructor(ILogger logger)
    {
        private readonly ILogger _logger = logger;

        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M0"")]
        public partial void M0();
    }
}";
            await VerifyAgainstBaselineUsingFile("TestWithLoggerInFieldAndFromPrimaryConstructor.generated.txt", testSourceCode);
        }
#endif

        [Fact]
        public void GenericTypeParameterAttributesAreRetained()
        {
            var type = typeof(TestClasses.NestedClassWithGenericTypesWithAttributesTestsExtensions<,,>).GetTypeInfo();

            Assert.Equal(3, type.GenericTypeParameters.Length);
            Assert.NotNull(type.GenericTypeParameters[0].GetCustomAttribute<TestClasses.FooAttribute>());
            Assert.NotNull(type.GenericTypeParameters[1].GetCustomAttribute<TestClasses.BarAttribute>());
        }

        private async Task VerifyAgainstBaselineUsingFile(string filename, string testSourceCode)
        {
            string baseline = LineEndingsHelper.Normalize(await File.ReadAllTextAsync(Path.Combine("Baselines", filename)).ConfigureAwait(false));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(LoggerMessageGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(Environment.NewLine);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);

            Assert.True(RoslynTestUtils.CompareLines(expectedLines, r[0].SourceText,
                out string errorMessage), errorMessage);
        }
    }
}
