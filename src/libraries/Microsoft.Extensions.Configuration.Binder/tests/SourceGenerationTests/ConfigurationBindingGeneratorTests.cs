// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public partial class ConfigurationBindingGeneratorTests
    {
        private static class Diagnostics
        {
            public static (string Id, string Title) TypeNotSupported = ("SYSLIB1100", "Did not generate binding logic for a type");
            public static (string Id, string Title) PropertyNotSupported = ("SYSLIB1101", "Did not generate binding logic for a property on a type");
            public static (string Id, string Title) ValueTypesInvalidForBind = ("SYSLIB1103", "Value types are invalid inputs to configuration 'Bind' methods");
            public static (string Id, string Title) CouldNotDetermineTypeInfo = ("SYSLIB1104", "The target type for a binder call could not be determined");
        }

        private enum ExtensionClassType
        {
            None,
            ConfigurationBinder,
            OptionsBuilder,
            ServiceCollection,
        }

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

        [Fact]
        public async Task ValueTypesAreInvalidAsBindInputs()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;
                
                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1
                		config.Bind(myInt);
                        int? myNInt = 2;
                        config.Bind(myNInt)

                        var myStruct = new MyStruct()
                        config.Bind(myStruct, options => { })
                        MyStruct? myNStruct = new();
                        config.Bind(myNStruct, options => { });

                        var myRecordStruct = new MyRecordStruct();
                        config.Bind("key", myRecordStruct);
                        MyRecordStruct? myNRecordStruct = new();
                        config.Bind("key", myNRecordStruct);

                        Memory<int> memory = new(new int[] {1, 2, 3});
                        config.Bind(memory);
                	}

                    public struct MyStruct { }
                    public record struct MyRecordStruct { }
                }
                """;

            var (d, r) = await RunGenerator(source);
            Assert.Empty(r);
            Assert.Equal(7, d.Count());

            foreach (Diagnostic diagnostic in d)
            {
                Assert.True(diagnostic.Id == Diagnostics.ValueTypesInvalidForBind.Id);
                Assert.Contains(Diagnostics.ValueTypesInvalidForBind.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task InvalidRootMethodInputTypes()
        {
            string source = """
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        config.GetValue(typeof(int*), "");
                        config.Get<Dictionary<string, T>>();
                    }

                    public struct MyStruct { }
                    public record struct MyRecordStruct { }
                }
                """;

            var (d, r) = await RunGenerator(source);
            Assert.Empty(r);
            Assert.Equal(2, d.Count());

            foreach (Diagnostic diagnostic in d)
            {
                Assert.True(diagnostic.Id == Diagnostics.CouldNotDetermineTypeInfo.Id);
                Assert.Contains(Diagnostics.CouldNotDetermineTypeInfo.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task CannotDetermineTypeInfo()
        {
            string source = """
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                
                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfiguration config = configurationBuilder.Build();
                
                		PerformGenericBinderCalls<MyClass>(config);
                	}

                    public static void PerformGenericBinderCalls<T>(IConfiguration config) where T : class
                    {
                        config.Get<T>();
                        config.Get<T>(binderOptions => { });
                        config.GetValue<T>("key");
                        config.GetValue<T>("key", default(T));

                        IConfigurationSection section = config.GetSection("MySection");
                		ServiceCollection services = new();
                        services.Configure<T>(section);
                    }

                    private void BindOptions(IConfiguration config, object? instance)
                    {
                        config.Bind(instance);
                    }

                    public class MyClass { }
                }
                """;

            var (d, r) = await RunGenerator(source);
            Assert.Empty(r);
            Assert.Equal(6, d.Count());

            foreach (Diagnostic diagnostic in d)
            {
                Assert.True(diagnostic.Id == Diagnostics.CouldNotDetermineTypeInfo.Id);
                Assert.Contains(Diagnostics.CouldNotDetermineTypeInfo.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        private static async Task VerifyAgainstBaselineUsingFile(
            string filename,
            string testSourceCode,
            LanguageVersion languageVersion = LanguageVersion.Preview,
            Action<ImmutableArray<Diagnostic>>? assessDiagnostics = null,
            ExtensionClassType extType = ExtensionClassType.None)
        {
            string path = extType is ExtensionClassType.None
                ? Path.Combine("Baselines", filename)
                : Path.Combine("Baselines", extType.ToString(), filename);
            string baseline = LineEndingsHelper.Normalize(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(ConfigurationBindingGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(Environment.NewLine);

            var (d, r) = await RunGenerator(testSourceCode, languageVersion);

            Assert.Single(r);
            (assessDiagnostics ?? ((d) => Assert.Empty(d))).Invoke(d);

            Assert.True(RoslynTestUtils.CompareLines(expectedLines, r[0].SourceText,
                out string errorMessage), errorMessage);
        }

        private static async Task<(ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGenerator(
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
                    typeof(OptionsBuilder<>).Assembly,
                    typeof(OptionsConfigurationServiceCollectionExtensions).Assembly,
                    typeof(Uri).Assembly,
                },
                new[] { testSourceCode },
                langVersion: langVersion).ConfigureAwait(false);
    }
}
