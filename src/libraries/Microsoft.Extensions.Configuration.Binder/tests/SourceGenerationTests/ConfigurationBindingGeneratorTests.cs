// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public partial class ConfigurationBindingGeneratorTests
    {
        [Fact]
        public async Task LangVersionMustBeCharp11OrHigher()
        {
            var (d, r) = await RunGenerator(BindCallSampleCode, LanguageVersion.CSharp10);
            Assert.Empty(r);

            Diagnostic diagnostic = Assert.Single(d);
            Assert.True(diagnostic.Id == "SYSLIB1102");
            Assert.Contains("C# 11", diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        private async Task<(ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGenerator(
            string testSourceCode,
            LanguageVersion langVersion = LanguageVersion.CSharp11) =>
            await RoslynTestUtils.RunGenerator(
                new ConfigurationBindingGenerator(),
                new[] {
                    typeof(ConfigurationBinder).Assembly,
                    typeof(CultureInfo).Assembly,
                    typeof(IConfiguration).Assembly,
                    typeof(IServiceCollection).Assembly,
                    typeof(IDictionary).Assembly,
                    typeof(ServiceCollection).Assembly,
                    typeof(OptionsConfigurationServiceCollectionExtensions).Assembly,
                    typeof(Uri).Assembly,
                },
                new[] { testSourceCode },
                langVersion: langVersion).ConfigureAwait(false);
    }
}
