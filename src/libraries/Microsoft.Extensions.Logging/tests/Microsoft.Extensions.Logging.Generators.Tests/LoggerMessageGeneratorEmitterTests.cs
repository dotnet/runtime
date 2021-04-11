// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Test
{
    public class LoggerMessageGeneratorEmitterTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34091", TestRuntimes.Mono)]
        public async Task TestEmitter()
        {
            // This test exists strictly to calculate the code coverage
            // attained by processing Definitions.cs. The functionality of the
            // resulting code is tested via LoggerMessageGeneratedCodeTests.cs
            string testProjectFolder = Path.Combine("..", "..", "..", "..", "src", "libraries", "Microsoft.Extensions.Logging", "tests", "Microsoft.Extensions.Logging.Generators.Tests", "TestClasses");

            var sources = new[]
            {
                Path.Combine(testProjectFolder, "MiscTestExtensions.cs"),
                Path.Combine(testProjectFolder, "LevelTestExtensions.cs"),
                Path.Combine(testProjectFolder, "ArgTestExtensions.cs"),
                Path.Combine(testProjectFolder, "EventNameTestExtensions.cs"),
                Path.Combine(testProjectFolder, "SignatureTestExtensions.cs"),
                Path.Combine(testProjectFolder, "MessageTestExtensions.cs"),
                Path.Combine(testProjectFolder, "EnumerableTestExtensions.cs"),
                Path.Combine(testProjectFolder, "TestInstances.cs"),
                Path.Combine(testProjectFolder, "CollectionTestExtensions.cs"),
                Path.Combine(testProjectFolder, "TemplateTestExtensions.cs"),
            };

            foreach (var src in sources)
            {
                var testSourceCode = await File.ReadAllTextAsync(src).ConfigureAwait(false);

                var (d, r) = await RoslynTestUtils.RunGenerator(
                    new LoggerMessageGenerator(),
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
                    new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
                    new[] { testSourceCode }).ConfigureAwait(false);

                Assert.Empty(d);
                _ = Assert.Single(r);
            }
        }
    }
}
