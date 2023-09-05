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
            var (d, r) = await RunGenerator(BindCallSampleCode, langVersion: langVersion);
            Assert.Empty(r);

            Diagnostic diagnostic = Assert.Single(d);
            Assert.True(diagnostic.Id == "SYSLIB1102");
            Assert.Contains("C# 12", diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
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

        [Fact]
        public async Task BindCanParseMethodParam()
        {
            string source = """
                using System;
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                    public static void Main()
                    {
                        ConfigurationBuilder configurationBuilder = new();
                        IConfiguration config = configurationBuilder.Build();

                        BindOptions(config, new MyClass0());
                        BindOptions(config, new MyClass1(), (_) => { });
                        BindOptions(config, "", new MyClass2());
                    }

                    private void BindOptions(IConfiguration config, MyClass0 instance)
                    {
                        config.Bind(instance);
                    }

                    private void BindOptions(IConfiguration config, MyClass1 instance, Action<BinderOptions>? configureOptions)
                    {
                        config.Bind(instance, configureOptions);
                    }

                    private void BindOptions(IConfiguration config, string path, MyClass2 instance)
                    {
                        config.Bind(path, instance);
                    }

                    public class MyClass0 { }
                    public class MyClass1 { }
                    public class MyClass2 { }
                }
                """;

            var (d, r) = await RunGenerator(source);
            Assert.Single(r);

            string generatedSource = string.Join('\n', r[0].SourceText.Lines.Select(x => x.ToString()));
            Assert.Contains("public static void Bind_ProgramMyClass0(this IConfiguration configuration, object? instance)", generatedSource);
            Assert.Contains("public static void Bind_ProgramMyClass1(this IConfiguration configuration, object? instance, Action<BinderOptions>? configureOptions)", generatedSource);
            Assert.Contains("public static void Bind_ProgramMyClass2(this IConfiguration configuration, string key, object? instance)", generatedSource);

            Assert.Empty(d);
        }

        [Fact]
        public async Task SucceedForMinimalInput()
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
                var (d, r) = await RunGenerator(source, references: GetFilteredAssemblyRefs(exclusions));

                Assert.Empty(d);

                if (expectOutput)
                {
                    Assert.Single(r);
                }
                else
                {
                    Assert.Empty(r);
                }
            }
        }

        [Fact]
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

            var (d, r) = await RunGenerator(source, references: GetAssemblyRefsWithAdditional(typeof(ImmutableArray<>), typeof(Encoding), typeof(JsonSerializer)));
            Assert.Single(r);
            Assert.Equal(12, d.Where(diag => diag.Id == Diagnostics.TypeNotSupported.Id).Count());
            Assert.Equal(10, d.Where(diag => diag.Id == Diagnostics.PropertyNotSupported.Id).Count());
        }
    }
}
