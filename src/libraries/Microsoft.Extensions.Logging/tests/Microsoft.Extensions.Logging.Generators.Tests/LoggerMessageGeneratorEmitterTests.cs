// © Microsoft Corporation. All rights reserved.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Test
{
    public class LoggerMessageGeneratorEmitterTests
    {
        [Fact]
        public async Task TestEmitter()
        {
            // This test exists strictly to calculate the code coverage
            // attained by processing Definitions.cs. The functionality of the
            // resulting code is tested via LoggerMessageGeneratedCodeTests.cs

            var sources = new[]
            {
                @"..\..\..\TestClasses\MiscTestExtensions.cs",
                @"..\..\..\TestClasses\LevelTestExtensions.cs",
                @"..\..\..\TestClasses\ArgTestExtensions.cs",
                @"..\..\..\TestClasses\EventNameTestExtensions.cs",
                @"..\..\..\TestClasses\SignatureTestExtensions.cs",
                @"..\..\..\TestClasses\MessageTestExtensions.cs",
                @"..\..\..\TestClasses\EnumerableTestExtensions.cs",
                @"..\..\..\TestClasses\TestInstances.cs",
                @"..\..\..\TestClasses\CollectionTestExtensions.cs",
                @"..\..\..\TestClasses\TemplateTestExtensions.cs",
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
