// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.CSharp10)]
        public async Task LangVersionMustBeCharp12OrHigher(LanguageVersion langVersion)
        {
            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(BindCallSampleCode, langVersion: langVersion);
            Assert.False(result.GeneratedSource.HasValue);

            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.True(diagnostic.Id == "SYSLIB1102");
            Assert.Contains("C# 12", diagnostic.Descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));
            Assert.Contains("C# 12", diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
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

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(7, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
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

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(2, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
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

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.False(result.GeneratedSource.HasValue);
            Assert.Equal(6, result.Diagnostics.Count());

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                Assert.True(diagnostic.Id == Diagnostics.CouldNotDetermineTypeInfo.Id);
                Assert.Contains(Diagnostics.CouldNotDetermineTypeInfo.Title, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                Assert.NotNull(diagnostic.Location);
            }
        }

        [Fact]
        public async Task SucceedWhenGivenConflictingTypeNames()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/93498

            string source = """
                using Microsoft.Extensions.Configuration;

                var c = new ConfigurationBuilder().Build();
                c.Get<Foo.Bar.BType>();

                namespace Microsoft.Foo
                {
                    internal class AType {}
                }

                namespace Foo.Bar
                {
                    internal class BType {}
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public async Task SucceedWhenGivenMinimumRequiredReferences()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();
                        config.Bind(new MyClass0());
                    }

                    public class MyClass0 { }
                }
                """;

            HashSet<Type> exclusions = new()
            {
                typeof(CultureInfo),
                typeof(IServiceCollection),
                typeof(IDictionary),
                typeof(ServiceCollection),
                typeof(OptionsBuilder<>),
                typeof(OptionsConfigurationServiceCollectionExtensions),
                typeof(Uri)
            };

            await Test(expectOutput: true);

            exclusions.Add(typeof(ConfigurationBinder));
            await Test(expectOutput: false);

            exclusions.Remove(typeof(ConfigurationBinder));
            exclusions.Add(typeof(IConfiguration));
            await Test(expectOutput: false);

            async Task Test(bool expectOutput)
            {
                ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetFilteredAssemblyRefs(exclusions));
                Assert.Empty(result.Diagnostics);
                Action ValidateSourceResult = expectOutput ? () => Assert.NotNull(result.GeneratedSource) : () => Assert.False(result.GeneratedSource.HasValue);
                ValidateSourceResult();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task ListOfTupleTest()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        var settingsSection = config.GetSection("Settings");

                        Settings options = settingsSection.Get<Settings>()!;
                    }
                }

                public class Settings
                {
                    public List<(string Item1, string? Item2)>? Items { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            // Ensure the generated code can be compiled.
            // If there is any compilation error, exception will be thrown with the list of the errors in the exception message.
            byte[] emittedAssemblyImage = CreateAssemblyImage(result.OutputCompilation);
            Assert.NotNull(emittedAssemblyImage);
        }

        [Fact]
        public async Task BindingToCollectionOnlyTest()
        {
            string source = """
                using Microsoft.Extensions.Configuration;
                using System;
                using System.Collections.Generic;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        var settingsSection = config.GetSection("Settings");

                        IDictionary<string, string> options = settingsSection.Get<IDictionary<string, string>>()!;
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ConfigurationBuilder), typeof(List<>)));
            Assert.NotNull(result.GeneratedSource);
            Assert.Empty(result.Diagnostics);

            // Ensure the generated code can be compiled.
            // If there is any compilation error, exception will be thrown with the list of the errors in the exception message.
            byte[] emittedAssemblyImage = CreateAssemblyImage(result.OutputCompilation);
            Assert.NotNull(emittedAssemblyImage);
        }

        /// <summary>
        /// We binding the type "SslClientAuthenticationOptions" which has a property "CipherSuitesPolicy" of type "CipherSuitesPolicy". We can't bind this type.
        /// This test is to ensure not including the property "CipherSuitesPolicy" in the generated code caused a build break.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task IgnoredUnBindablePropertiesTest()
        {
            string source = """
                 using System;
                 using System.Net.Security;
                 using Microsoft.Extensions.Configuration;
                 using System.Collections.Immutable;
                 using System.Text;
                 using System.Text.Json;

                 public class Program
                 {
                     public static void Main()
                     {
                         ConfigurationBuilder configurationBuilder = new();
                         IConfiguration config = configurationBuilder.Build();

                         var obj = config.Get<SslClientAuthenticationOptions>();
                      }
                 }
                 """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ImmutableArray<>), typeof(Encoding), typeof(JsonSerializer), typeof(System.Net.Security.AuthenticatedStream)));
            Assert.NotNull(result.GeneratedSource);

            Assert.DoesNotContain("CipherSuitesPolicy = ", result.GeneratedSource.Value.SourceText.ToString());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [ActiveIssue("Work out why we aren't getting all the expected diagnostics.")]
        public async Task IssueDiagnosticsForAllOffendingCallsites()
        {
            string source = """
                using System.Collections.Immutable;
                using System.Text;
                using System.Text.Json;
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                	public static void Main()
                	{
                		ConfigurationBuilder configurationBuilder = new();
                		IConfiguration configuration = configurationBuilder.Build();

                        var obj = new TypeGraphWithUnsupportedMember();
                        configuration.Bind(obj);

                        var obj2 = new AnotherGraphWithUnsupportedMembers();
                        var obj4 = Encoding.UTF8;

                        // Must require separate suppression.
                        configuration.Bind(obj2);
                        configuration.Bind(obj2, _ => { });
                        configuration.Bind("", obj2);
                        configuration.Get<TypeGraphWithUnsupportedMember>();
                        configuration.Get<AnotherGraphWithUnsupportedMembers>(_ => { });
                        configuration.Get(typeof(TypeGraphWithUnsupportedMember));
                        configuration.Get(typeof(AnotherGraphWithUnsupportedMembers), _ => { });
                        configuration.Bind(obj4);
                        configuration.Get<Encoding>();
                	}

                    public class TypeGraphWithUnsupportedMember
                    {
                        public JsonWriterOptions WriterOptions { get; set; }
                    }

                    public class AnotherGraphWithUnsupportedMembers
                    {
                        public JsonWriterOptions WriterOptions { get; set; }
                        public ImmutableArray<int> UnsupportedArray { get; set; }
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source, assemblyReferences: GetAssemblyRefsWithAdditional(typeof(ImmutableArray<>), typeof(Encoding), typeof(JsonSerializer)));
            Assert.NotNull(result.GeneratedSource);
            Assert.True(result.Diagnostics.Any(diag => diag.Id == Diagnostics.TypeNotSupported.Id));
            Assert.True(result.Diagnostics.Any(diag => diag.Id == Diagnostics.PropertyNotSupported.Id));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_HasPragmaSuppressibleLocation()
        {
            // SYSLIB1103: ValueTypesInvalidForBind (Warning, configurable).
            string source = """
                #pragma warning disable SYSLIB1103
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.True(diagnostic.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_NoPragma_IsNotSuppressed()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.False(diagnostic.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_MultipleDiagnostics_OnlySomeSuppressed()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                using System.Text;
                using System.Text.Json;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        // SYSLIB1103 suppressed for this call only.
                        #pragma warning disable SYSLIB1103
                        int myInt = 1;
                        config.Bind(myInt);
                        #pragma warning restore SYSLIB1103

                        // SYSLIB1103 NOT suppressed for this call.
                        long myLong = 1;
                        config.Bind(myLong);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation)
                .Where(d => d.Id == "SYSLIB1103")
                .ToList();

            Assert.Equal(2, effective.Count);
            Assert.Single(effective, d => d.IsSuppressed);
            Assert.Single(effective, d => !d.IsSuppressed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Diagnostic_PragmaRestoreOutsideSpan_IsNotSuppressed()
        {
            string source = """
                using System;
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfigurationRoot config = configurationBuilder.Build();

                        // Suppress and restore BEFORE the diagnostic site.
                        #pragma warning disable SYSLIB1103
                        #pragma warning restore SYSLIB1103

                        int myInt = 1;
                        config.Bind(myInt);
                    }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            var effective = CompilationWithAnalyzers.GetEffectiveDiagnostics(result.Diagnostics, result.OutputCompilation);
            Diagnostic diagnostic = Assert.Single(effective, d => d.Id == "SYSLIB1103");
            Assert.False(diagnostic.IsSuppressed);
        }

        /// <summary>
        /// Verifies that the suppressor suppresses IL2026/IL3050 when a ConfigurationBinder call
        /// is passed directly as a method argument (e.g. Some.Method(config.Get&lt;T&gt;())).
        /// Regression test for https://github.com/dotnet/runtime/issues/94544.
        /// </summary>
        [Fact]
        public async Task Suppressor_SuppressesWarnings_WhenBindingCallIsMethodArgument()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                        Some.Method(c.Get<MyOptions>());
                    }
                }

                internal static class Some
                {
                    public static void Method(MyOptions? options) { }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        /// <summary>
        /// Verifies that the suppressor also works for the straightforward assignment case,
        /// ensuring no regression in existing behavior.
        /// </summary>
        [Fact]
        public async Task Suppressor_SuppressesWarnings_ForSimpleBindingCall()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                        var options = c.Get<MyOptions>();
                    }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        [Fact]
        public async Task Suppressor_SuppressesWarnings_WithLineDirective()
        {
            string source = """
                using Microsoft.Extensions.Configuration;

                public class Program
                {
                    public static void Main()
                    {
                        IConfigurationSection c = new ConfigurationBuilder().Build().GetSection("Options");
                #line 100 "Remapped.cs"
                        var options = c.Get<MyOptions>();
                #line default
                    }
                }

                public class MyOptions
                {
                    public int MaxRetries { get; set; }
                }
                """;

            ConfigBindingGenRunResult result = await RunGeneratorAndUpdateCompilation(source);
            Assert.NotNull(result.GeneratedSource);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        /// <summary>
        /// Verifies that the set of IL2026/IL3050 diagnostics suppressed by the suppressor
        /// matches exactly the set of calls intercepted by the source generator.
        /// Catches both under-suppression (https://github.com/dotnet/runtime/issues/94544)
        /// and over-suppression (https://github.com/dotnet/runtime/issues/96643).
        /// </summary>
        private static async Task VerifySuppressedCallsMatchInterceptedCalls(ConfigBindingGenRunResult result)
        {
            Assert.NotNull(result.GenerationSpec);

            // Collect all intercepted (line, column) locations from the generator spec.
            // The interceptor targets MemberAccessExpression.Name (e.g. "Get" in "c.Get<T>()").
            HashSet<(int Line, int Column)> interceptedLocations = GetInterceptedLocations(result.GenerationSpec);
            Assert.NotEmpty(interceptedLocations);

            // Run the ILLink analyzer + suppressor on the output compilation (which includes generated InterceptsLocation attributes).
            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsWithSuppressor(result.OutputCompilation);

            // The ILLink analyzer must have produced at least one IL2026 or IL3050 that was suppressed.
            // Without this, the assertions below would pass vacuously if the analyzer didn't fire.
            Assert.Contains(diagnostics, d => (d.Id is "IL2026" or "IL3050") && d.IsSuppressed);

            // Every suppressed IL2026/IL3050 diagnostic should be at an intercepted location.
            foreach (Diagnostic d in diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && d.IsSuppressed))
            {
                (int line, int column) = GetMethodNameLocation(d);
                Assert.True(interceptedLocations.Contains((line, column)),
                    $"Suppressed {d.Id} at ({line},{column}) but no interceptor was generated for that call site.");
            }

            // Every intercepted location should have its IL2026/IL3050 diagnostics suppressed.
            foreach (Diagnostic d in diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && !d.IsSuppressed))
            {
                (int line, int column) = GetMethodNameLocation(d);
                Assert.False(interceptedLocations.Contains((line, column)),
                    $"Unsuppressed {d.Id} at ({line},{column}) but an interceptor was generated for that call site.");
            }
        }

        /// <summary>
        /// Resolves a diagnostic's location to the method name position that the interceptor targets.
        /// The ILLink analyzer reports on the MemberAccessExpression (e.g. "c.Get&lt;T&gt;"),
        /// but the interceptor targets just the Name part (e.g. "Get"). This method walks from
        /// the diagnostic location to the InvocationExpression's MemberAccessExpression.Name
        /// to get the matching (line, column).
        /// </summary>
        private static (int Line, int Column) GetMethodNameLocation(Diagnostic diagnostic)
        {
            Location location = diagnostic.AdditionalLocations.Count > 0
                ? diagnostic.AdditionalLocations[0]
                : diagnostic.Location;
            SyntaxTree sourceTree = location.SourceTree!;
            SyntaxNode node = sourceTree.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true);

            InvocationExpressionSyntax invocation = (node as InvocationExpressionSyntax
                ?? node.Parent as InvocationExpressionSyntax)!;

            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
            FileLinePositionSpan nameSpan = sourceTree.GetLineSpan(memberAccess.Name.Span);

            return (nameSpan.StartLinePosition.Line + 1, nameSpan.StartLinePosition.Character + 1);
        }

        private static HashSet<(int Line, int Column)> GetInterceptedLocations(SourceGenerationSpec spec)
        {
            var locations = new HashSet<(int, int)>();
            InterceptorInfo info = spec.InterceptorInfo;

            AddLocations(info.ConfigBinder);
            AddLocations(info.OptionsBuilderExt);
            AddLocations(info.ServiceCollectionExt);
            AddTypedLocations(info.ConfigBinder_Bind_instance);
            AddTypedLocations(info.ConfigBinder_Bind_instance_BinderOptions);
            AddTypedLocations(info.ConfigBinder_Bind_key_instance);

            return locations;

            void AddLocations(IEnumerable<InvocationLocationInfo>? locationInfos)
            {
                if (locationInfos is null)
                    return;

                foreach (InvocationLocationInfo loc in locationInfos)
                {
                    locations.Add(GetLocation(loc));
                }
            }

            void AddTypedLocations(IEnumerable<TypedInterceptorInvocationInfo>? typedInfos)
            {
                if (typedInfos is null)
                    return;

                foreach (TypedInterceptorInvocationInfo typed in typedInfos)
                {
                    AddLocations(typed.Locations);
                }
            }
        }

        private static (int Line, int Column) GetLocation(InvocationLocationInfo loc)
        {
            if (loc.LineNumber != 0)
            {
                return (loc.LineNumber, loc.CharacterNumber);
            }

            // v1 interceptor: parse from display location, e.g. "path(line,col)"
            string display = loc.InterceptableLocationGetDisplayLocation();
            Match match = Regex.Match(display, @"\((\d+),(\d+)\)$");
            Assert.True(match.Success, $"Could not parse display location: {display}");

            return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithSuppressor(Compilation compilation)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new DynamicallyAccessedMembersAnalyzer(),
                new ConfigurationBindingGenerator.Suppressor());

            var trimAotAnalyzerOptions = new DictionaryAnalyzerConfigOptions(
                ImmutableDictionary.CreateRange<string, string>(
                    StringComparer.OrdinalIgnoreCase,
                    [
                        new("build_property.EnableTrimAnalyzer", "true"),
                        new("build_property.EnableAotAnalyzer", "true"),
                    ]));
            var analyzerOptions = new AnalyzerOptions(
                ImmutableArray<AdditionalText>.Empty,
                new GlobalOptionsOnlyProvider(trimAotAnalyzerOptions));
            var options = new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: true);

            return await new CompilationWithAnalyzers(compilation, analyzers, options)
                .GetAllDiagnosticsAsync();
        }
    }
}
