// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Test
{
    public class LoggerMessageGeneratorEmitterTests
    {
        private class Options : AnalyzerConfigOptions
        {
            private readonly string _response;

            public Options(string response)
            {
                _response = response;
            }

            public override bool TryGetValue(string key, out string value)
            {
                value = _response;
                return _response.Length > 0;
            }
        }

        private class OptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly string _response;

            public OptionsProvider(string response)
            {
                _response = response;
            }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();
            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotImplementedException();
            public override AnalyzerConfigOptions GlobalOptions => new Options(_response);
        }

        [Theory]
        [InlineData("")]
        [InlineData("TRUE")]
        [InlineData("FALSE")]
        public async Task TestEmitter(string response)
        {
            // This test exists strictly to calculate the code coverage
            // attained by processing Definitions.cs. The functionality of the
            // resulting code is tested via LoggerMessageGeneratedCodeTests.cs

            var testSourceCode = await File.ReadAllTextAsync(@"..\..\..\..\src\libraries\Microsoft.Extensions.Logging\tests\Microsoft.Extensions.Logging.Generator.Tests\Definitions.cs");

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new LoggerMessageGenerator(),
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
                new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
                new[] { testSourceCode },
                optionsProvider: new OptionsProvider(response)).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);
        }
    }
}
