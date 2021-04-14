// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/32743", TestRuntimes.Mono)]
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

                Assert.Empty(d);
                Assert.Single(r);
            }
        }

        [Fact]
        public async Task TestBaseline_TestWithOneParam_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithOneParam
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = ""M0 {A1}"")]
        public static partial void M0(ILogger logger, int a1);
    }
}";
            string expectedResult = await File.ReadAllTextAsync(Path.Combine("Baselines", "TestWithOneParam.generated.txt")).ConfigureAwait(false);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);
            Assert.Equal(expectedResult, r[0].SourceText.ToString());
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
        public static partial void Method9(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);
    }
}";
            string expectedResult = await File.ReadAllTextAsync(Path.Combine("Baselines", "TestWithMoreThan6Params.generated.txt")).ConfigureAwait(false);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);
            Assert.Equal(expectedResult, r[0].SourceText.ToString());
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
            string expectedResult = await File.ReadAllTextAsync(Path.Combine("Baselines", "TestWithDynamicLogLevel.generated.txt")).ConfigureAwait(false);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);
            Assert.Equal(expectedResult, r[0].SourceText.ToString());
        }
    }
}
