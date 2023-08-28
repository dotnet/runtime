// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBindingGeneratorTests
    {
        #region IServiceCollection extensions.
        private string GetConfigureSource(string paramList) => $$"""
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            public class Program
            {
                public static void Main()
                {
                    ConfigurationBuilder configurationBuilder = new();
                    IConfiguration config = configurationBuilder.Build();
                    IConfigurationSection section = config.GetSection(""MySection"");

                    ServiceCollection services = new();
                    services.Configure<MyClass>({{paramList}});
                }

                public class MyClass
                {
                    public string MyString { get; set; }
                    public int MyInt { get; set; }
                    public List<int> MyList { get; set; }
                    public List<MyClass2> MyList2 { get; set; }
                    public Dictionary<string, string> MyDictionary { get; set; }
                }

                public class MyClass2
                {
                    public int MyInt { get; set; }
                }
            }
            """;
        #endregion IServiceCollection extensions.

        #region OptionsBuilder<T> extensions.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Configure_T() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Configure_T.generated.txt"), GetConfigureSource("section"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Configure_T_NetFwk() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Configure_T.generated.txt"), GetConfigureSource("section"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Configure_T_name() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Configure_T_name.generated.txt"), GetConfigureSource(@""""", section"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Configure_T_name_NetFwk() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Configure_T_name.generated.txt"), GetConfigureSource(@""""", section"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Configure_T_BinderOptions() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Configure_T_BinderOptions.generated.txt"), GetConfigureSource("section, _ => { }"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Configure_T_BinderOptions_NetFwk() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Configure_T_BinderOptions.generated.txt"), GetConfigureSource("section, _ => { }"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Configure_T_name_BinderOptions() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Configure_T_name_BinderOptions.generated.txt"), GetConfigureSource(@""""", section, _ => { }"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Configure_T_name_BinderOptions_NetFwk() =>
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Configure_T_name_BinderOptions.generated.txt"), GetConfigureSource(@""""", section, _ => { }"));

        private string GetBindSource(string? configureActions = null) => $$"""
            using System.Collections.Generic;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Options;

            public class Program
            {
                public static void Main()
                {
                    ConfigurationBuilder configurationBuilder = new();
                    IConfiguration config = configurationBuilder.Build();

                    var services = new ServiceCollection();
                    OptionsBuilder<MyClass> optionsBuilder = new(services, "");
                    optionsBuilder.Bind(config{{configureActions}});
                }

                public class MyClass
                {
                    public string MyString { get; set; }
                    public int MyInt { get; set; }
                    public List<int> MyList { get; set; }
                }
            }
            """;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Bind_T()
        {
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Bind_T.generated.txt"), GetBindSource());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Bind_T_NetFwk()
        {
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Bind_T.generated.txt"), GetBindSource());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public async Task Bind_T_BinderOptions()
        {
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "Bind_T_BinderOptions.generated.txt"), GetBindSource(", _ => { }"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetFramework))]
        public async Task Bind_T_BinderOptions_NetFwk()
        {
            await VerifyAgainstBaselineUsingFile(Path.Combine("net462", "Bind_T_BinderOptions.generated.txt"), GetBindSource(", _ => { }"));
        }

        [Fact]
        public async Task BindConfiguration()
        {
            string GetSource(string? configureActions = null) => $$"""
                using System.Collections.Generic;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Options;

                public class Program
                {
                    public static void Main()
                    {
                        var services = new ServiceCollection();
                        OptionsBuilder<MyClass> optionsBuilder = new(services, Options.DefaultName);
                        optionsBuilder.BindConfiguration(""{{configureActions}});
                    }

                    public class MyClass
                    {
                        public string MyString { get; set; }
                        public int MyInt { get; set; }
                        public List<int> MyList { get; set; }
                    }
                }
                """;

            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "BindConfiguration.generated.txt"), GetSource());
            await VerifyAgainstBaselineUsingFile(Path.Combine("netcoreapp", "BindConfiguration.generated.txt"), GetSource(@"), _ => { }"));
        }
        #endregion OptionsBuilder<T> extensions.
    }
}
