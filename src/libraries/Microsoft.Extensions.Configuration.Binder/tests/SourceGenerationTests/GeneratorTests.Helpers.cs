// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests
    {
        /// <summary>
        /// Keep in sync with variants, e.g. <see cref="BindCallSampleCodeVariant_ReorderedInvocations"/>.
        /// </summary>
        private const string BindCallSampleCode = """
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;

            public class Program
            {
        	    public static void Main()
        	    {
        		    ConfigurationBuilder configurationBuilder = new();
        		    IConfigurationRoot config = configurationBuilder.Build();

        		    MyClass configObj = new();
        		    config.Bind(configObj);
                    config.Bind(configObj, options => { });
                    config.Bind("key", configObj);
        	    }

        	    public class MyClass
        	    {
        		    public string MyString { get; set; }
        		    public int MyInt { get; set; }
        		    public List<int> MyList { get; set; }
        		    public Dictionary<string, string> MyDictionary { get; set; }
                    public Dictionary<string, MyClass2> MyComplexDictionary { get; set; }
        	    }

                public class MyClass2 { }
            }
        """;

        private static class Diagnostics
        {
            public static (string Id, string Title) TypeNotSupported = ("SYSLIB1100", "Did not generate binding logic for a type");
            public static (string Id, string Title) PropertyNotSupported = ("SYSLIB1101", "Did not generate binding logic for a property on a type");
            public static (string Id, string Title) ValueTypesInvalidForBind = ("SYSLIB1103", "Value types are invalid inputs to configuration 'Bind' methods");
            public static (string Id, string Title) CouldNotDetermineTypeInfo = ("SYSLIB1104", "The target type for a binder call could not be determined");
        }

        private static readonly Assembly[] s_compilationAssemblyRefs = new[] {
            typeof(ConfigurationBinder).Assembly,
            typeof(ConfigurationBuilder).Assembly,
            typeof(CultureInfo).Assembly,
            typeof(Dictionary<,>).Assembly,
            typeof(Enumerable).Assembly,
            typeof(IConfiguration).Assembly,
            typeof(IServiceCollection).Assembly,
            typeof(IServiceProvider).Assembly,
            typeof(IDictionary).Assembly,
            typeof(OptionsBuilder<>).Assembly,
            typeof(OptionsConfigurationServiceCollectionExtensions).Assembly,
            typeof(Uri).Assembly,
        };

        private enum ExtensionClassType
        {
            None,
            ConfigurationBinder,
            OptionsBuilder,
            ServiceCollection,
        }

        private static async Task VerifyThatSourceIsGenerated(string testSourceCode)
        {
            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(testSourceCode);

            GeneratedSourceResult? source = result.GeneratedSource;
            Assert.NotNull(source);
            Assert.Empty(result.Diagnostics);
            Assert.True(source.Value.SourceText.Lines.Count > 10);
        }

        private static async Task VerifyAgainstBaselineUsingFile(
            string filename,
            string testSourceCode,
            Action<ImmutableArray<Diagnostic>>? assessDiagnostics = null,
            ExtensionClassType extType = ExtensionClassType.None,
            bool validateOutputDiags = true)
        {
            string path = extType is ExtensionClassType.None
                ? Path.Combine("Baselines", filename)
                : Path.Combine("Baselines", extType.ToString(), filename);
            string baseline = LineEndingsHelper.Normalize(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(ConfigurationBindingGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(Environment.NewLine);

            ConfigBindingGenTestDriver genDriver = new();
            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(testSourceCode);
            Assert.NotNull(result.GeneratedSource);
            GeneratedSourceResult generatedSource = result.GeneratedSource.Value;

            SourceText resultSourceText = generatedSource.SourceText;
            bool resultEqualsBaseline = RoslynTestUtils.CompareLines(expectedLines, resultSourceText, out string errorMessage);

#if !UPDATE_BASELINES
            if (!resultEqualsBaseline)
            {
                const string envVarName = "RepoRootDir";
                string errMessage = $"To update baselines, specify a '{envVarName}' environment variable. See this assembly's README.md doc for more details.";

                string? repoRootDir = Environment.GetEnvironmentVariable(envVarName);
                Assert.True(repoRootDir is not null, errMessage);

                IEnumerable<string> lines = resultSourceText.Lines.Select(l => l.ToString());
                string source = string.Join(Environment.NewLine, lines).TrimEnd(Environment.NewLine.ToCharArray()) + Environment.NewLine;
                path = Path.Combine($"{repoRootDir}\\src\\libraries\\Microsoft.Extensions.Configuration.Binder\\tests\\SourceGenerationTests\\", path);

                await File.WriteAllTextAsync(path, source).ConfigureAwait(false);
                resultEqualsBaseline = true;
            }
#endif

            assessDiagnostics ??= static (diagnostics) => Assert.Empty(diagnostics);
            assessDiagnostics(result.Diagnostics);
            Assert.True(resultEqualsBaseline, errorMessage);
        }

        private static async Task<ConfigBindingGenRunResult> RunGeneratorAndUpdateCompilation(
            string source,
            LanguageVersion langVersion = LanguageVersion.CSharp12,
            IEnumerable<Assembly>? assemblyReferences = null,
            bool validateCompilationDiagnostics = false)
        {
            ConfigBindingGenTestDriver driver = new ConfigBindingGenTestDriver(langVersion, assemblyReferences);
            ConfigBindingGenRunResult result = await driver.RunGeneratorAndUpdateCompilation(source);

            if (validateCompilationDiagnostics)
            {
                ImmutableArray<Diagnostic> compilationDiags = result.OutputCompilation.GetDiagnostics();
                Assert.False(compilationDiags.Any(d => d.Severity > DiagnosticSeverity.Info));
            }

            return result;
        }

        private static List<Assembly> GetAssemblyRefsWithAdditional(params Type[] additional)
        {
            List<Assembly> assemblies = new(s_compilationAssemblyRefs);
            assemblies.AddRange(additional.Select(t => t.Assembly));
            return assemblies;
        }

        private static HashSet<Assembly> GetFilteredAssemblyRefs(IEnumerable<Type> exclusions)
        {
            HashSet<Assembly> assemblies = new(s_compilationAssemblyRefs);
            foreach (Type exclusion in exclusions)
            {
                assemblies.Remove(exclusion.Assembly);
            }
            return assemblies;
        }
    }
}
