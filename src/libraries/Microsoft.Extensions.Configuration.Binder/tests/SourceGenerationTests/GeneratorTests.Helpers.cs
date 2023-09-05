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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        private static async Task VerifyAgainstBaselineUsingFile(
            string filename,
            string testSourceCode,
            Action<ImmutableArray<Diagnostic>>? assessDiagnostics = null,
            ExtensionClassType extType = ExtensionClassType.None,
            bool validateOutputCompDiags = true)
        {
            string path = extType is ExtensionClassType.None
                ? Path.Combine("Baselines", filename)
                : Path.Combine("Baselines", extType.ToString(), filename);
            string baseline = LineEndingsHelper.Normalize(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(ConfigurationBindingGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(Environment.NewLine);

            var (d, r) = await RunGenerator(testSourceCode, validateOutputCompDiags);
            bool success = RoslynTestUtils.CompareLines(expectedLines, r[0].SourceText, out string errorMessage);

#if UPDATE_BASELINES
            if (!success)
            {
                string? repoRootDir = Environment.GetEnvironmentVariable("RepoRootDir");
                Assert.True(repoRootDir is not null, "To update baselines, specifiy the root runtime repo dir");

                IEnumerable<string> lines = r[0].SourceText.Lines.Select(l => l.ToString());
                string source = string.Join(Environment.NewLine, lines).TrimEnd(Environment.NewLine.ToCharArray()) + Environment.NewLine;
                path = Path.Combine($"{repoRootDir}\\src\\libraries\\Microsoft.Extensions.Configuration.Binder\\tests\\SourceGenerationTests\\", path);

                await File.WriteAllTextAsync(path, source).ConfigureAwait(false);
                success = true;
            }
#endif

            Assert.Single(r);
            (assessDiagnostics ?? ((d) => Assert.Empty(d))).Invoke(d);
            Assert.True(success, errorMessage);
        }

        private static async Task<(ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGenerator(
            string testSourceCode,
            bool validateOutputCompDiags = false,
            LanguageVersion langVersion = LanguageVersion.CSharp12,
            IEnumerable<Assembly>? references = null)
        {
            using var workspace = RoslynTestUtils.CreateTestWorkspace();
            CSharpParseOptions parseOptions = new CSharpParseOptions(langVersion).WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsPreview", "") });

            Project proj = RoslynTestUtils.CreateTestProject(workspace, references ?? s_compilationAssemblyRefs, langVersion: langVersion)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Annotations))
                .WithDocuments(new string[] { testSourceCode })
                .WithParseOptions(parseOptions);

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            Compilation comp = await proj.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { new ConfigurationBindingGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
            GeneratorDriver gd = cgd.RunGeneratorsAndUpdateCompilation(comp, out Compilation outputCompilation, out _, CancellationToken.None);
            GeneratorDriverRunResult runResult = gd.GetRunResult();

            if (validateOutputCompDiags)
            {
                Assert.False(outputCompilation.GetDiagnostics().Any(d => d.Severity > DiagnosticSeverity.Info));
            }

            return (runResult.Results[0].Diagnostics, runResult.Results[0].GeneratedSources);
        }

        public static List<Assembly> GetAssemblyRefsWithAdditional(params Type[] additional)
        {
            List<Assembly> assemblies = new(s_compilationAssemblyRefs);
            assemblies.AddRange(additional.Select(t => t.Assembly));
            return assemblies;
        }

        public static HashSet<Assembly> GetFilteredAssemblyRefs(IEnumerable<Type> exclusions)
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
