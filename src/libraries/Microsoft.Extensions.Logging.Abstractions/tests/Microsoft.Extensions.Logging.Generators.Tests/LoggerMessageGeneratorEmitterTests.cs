// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public async Task TestBaseline_TestWithTwoParams_Success()
        {
            string testSourceCode = @"
namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TestWithTwoParams
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = ""M0 {a1} {a2}"")]
        public static partial void M0(ILogger logger, int a1, System.Collections.Generic.IEnumerable<int> a2);
    }
}";
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

        private async Task VerifyAgainstBaselineUsingFile(string filename, string testSourceCode)
        {
            string[] expectedLines = await File.ReadAllLinesAsync(Path.Combine("Baselines", filename)).ConfigureAwait(false);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
                new[] { typeof(ILogger).Assembly, typeof(LoggerMessageAttribute).Assembly },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);

            Assert.True(CompareLines(expectedLines, r[0].SourceText,
                out string errorMessage), errorMessage);
        }

        private bool CompareLines(string[] expectedLines, SourceText sourceText, out string message)
        {
            if (expectedLines.Length != sourceText.Lines.Count)
            {
                message = string.Format("Line numbers do not match. Expected: {0} lines, but generated {1}",
                    expectedLines.Length, sourceText.Lines.Count);
                return false;
            }
            int index = 0;
            foreach (TextLine textLine in sourceText.Lines)
            {
                string expectedLine = expectedLines[index];
                if (!expectedLine.Equals(textLine.ToString(), StringComparison.Ordinal))
                {
                    message = string.Format("Line {0} does not match.{1}Expected Line:{1}{2}{1}Actual Line:{1}{3}",
                        textLine.LineNumber, Environment.NewLine, expectedLine, textLine);
                    return false;
                }
                index++;
            }
            message = string.Empty;
            return true;
        }
    }
}
