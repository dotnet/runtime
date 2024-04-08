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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
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
    }
}
