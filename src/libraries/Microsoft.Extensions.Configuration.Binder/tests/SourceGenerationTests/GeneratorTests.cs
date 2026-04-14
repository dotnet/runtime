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
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
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

            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsWithSuppressor(result.OutputCompilation);

            Assert.Contains(diagnostics, d => d.Id == "IL2026" && d.IsSuppressed);
            Assert.Contains(diagnostics, d => d.Id == "IL3050" && d.IsSuppressed);
            Assert.DoesNotContain(diagnostics, d => (d.Id is "IL2026" or "IL3050") && !d.IsSuppressed);

            await VerifySuppressedCallsMatchInterceptedCalls(result);
        }

        /// <summary>
        /// Verifies that the suppressor also works for the straightforward assignment case,
        /// ensuring no regression in existing behavior.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
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

            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsWithSuppressor(result.OutputCompilation);

            Assert.Contains(diagnostics, d => d.Id == "IL2026" && d.IsSuppressed);
            Assert.Contains(diagnostics, d => d.Id == "IL3050" && d.IsSuppressed);
            Assert.DoesNotContain(diagnostics, d => (d.Id is "IL2026" or "IL3050") && !d.IsSuppressed);

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

            // Collect all intercepted line numbers from the generator spec.
            HashSet<int> interceptedLines = GetInterceptedLines(result.GenerationSpec);
            Assert.NotEmpty(interceptedLines);

            // Run the ILLink analyzer + suppressor on the output compilation (which includes generated InterceptsLocation attributes).
            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsWithSuppressor(result.OutputCompilation);

            // Every suppressed IL2026/IL3050 diagnostic should be on an intercepted line.
            foreach (Diagnostic d in diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && d.IsSuppressed))
            {
                int line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
                Assert.True(interceptedLines.Contains(line),
                    $"Suppressed {d.Id} at line {line} but no interceptor was generated for that call site.");
            }

            // Every intercepted line should have its IL2026/IL3050 diagnostics suppressed.
            var unsuppressed = diagnostics.Where(d => (d.Id is "IL2026" or "IL3050") && !d.IsSuppressed).ToList();
            foreach (Diagnostic d in unsuppressed)
            {
                int line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
                Assert.False(interceptedLines.Contains(line),
                    $"Unsuppressed {d.Id} at line {line} but an interceptor was generated for that call site.");
            }
        }

        private static HashSet<int> GetInterceptedLines(SourceGenerationSpec spec)
        {
            var lines = new HashSet<int>();
            InterceptorInfo info = spec.InterceptorInfo;

            AddLocations(info.ConfigBinder);
            AddTypedLocations(info.ConfigBinder_Bind_instance);
            AddTypedLocations(info.ConfigBinder_Bind_instance_BinderOptions);
            AddTypedLocations(info.ConfigBinder_Bind_key_instance);

            return lines;

            void AddLocations(IEnumerable<InvocationLocationInfo>? locationInfos)
            {
                if (locationInfos is null)
                    return;

                foreach (InvocationLocationInfo loc in locationInfos)
                {
                    lines.Add(GetLineNumber(loc));
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

        private static int GetLineNumber(InvocationLocationInfo loc)
        {
            if (loc.LineNumber != 0)
            {
                return loc.LineNumber;
            }

            // v1 interceptor: parse line from display location, e.g. "path(line,col)"
            string display = loc.InterceptableLocationGetDisplayLocation();
            int parenIndex = display.LastIndexOf('(');
            int commaIndex = display.IndexOf(',', parenIndex);
            return int.Parse(display.Substring(parenIndex + 1, commaIndex - parenIndex - 1));
        }

        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithSuppressor(Compilation compilation)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new DynamicallyAccessedMembersAnalyzer(),
                new ConfigurationBindingGenerator.Suppressor());

            var globalOptions = ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                new[]
                {
                    new KeyValuePair<string, string>("build_property.EnableTrimAnalyzer", "true"),
                    new KeyValuePair<string, string>("build_property.EnableAotAnalyzer", "true"),
                });
            var analyzerOptions = new AnalyzerOptions(
                ImmutableArray<AdditionalText>.Empty,
                new SimpleAnalyzerConfigOptionsProvider(globalOptions));
            var options = new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: true);

            return await new CompilationWithAnalyzers(compilation, analyzers, options)
                .GetAllDiagnosticsAsync();
        }

        private sealed class SimpleAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly SimpleOptions _globalOptions;

            public SimpleAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> globalOptions)
            {
                _globalOptions = new SimpleOptions(globalOptions);
            }

            public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => SimpleOptions.Empty;
            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => SimpleOptions.Empty;

            private sealed class SimpleOptions : AnalyzerConfigOptions
            {
                public static readonly SimpleOptions Empty = new(ImmutableDictionary<string, string>.Empty);

                private readonly ImmutableDictionary<string, string> _dict;
                public SimpleOptions(ImmutableDictionary<string, string> dict) => _dict = dict;

#pragma warning disable 8765 // Nullability of parameter doesn't match overridden member
                public override bool TryGetValue(string key, out string? value) => _dict.TryGetValue(key, out value);
#pragma warning restore 8765
            }
        }
    }
}
