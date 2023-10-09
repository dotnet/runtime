// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    IConfigurationSection section = config.GetSection("MySection");

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
        [Fact]
        public async Task Configure_T() =>
            await VerifyAgainstBaselineUsingFile("Configure_T.generated.txt", GetConfigureSource("section"), extType: ExtensionClassType.ServiceCollection);

        [Fact]
        public async Task Configure_T_name() =>
            await VerifyAgainstBaselineUsingFile("Configure_T_name.generated.txt", GetConfigureSource(@""""", section"), extType: ExtensionClassType.ServiceCollection);

        [Fact]
        public async Task Configure_T_BinderOptions() =>
            await VerifyAgainstBaselineUsingFile("Configure_T_BinderOptions.generated.txt", GetConfigureSource("section, _ => { }"), extType: ExtensionClassType.ServiceCollection);

        [Fact]
        public async Task Configure_T_name_BinderOptions() =>
            await VerifyAgainstBaselineUsingFile("Configure_T_name_BinderOptions.generated.txt", GetConfigureSource(@""""", section, _ => { }"), extType: ExtensionClassType.ServiceCollection);

        [Theory]
        [InlineData("OptionsConfigurationServiceCollectionExtensions.Configure<MyClass>(config: section, services: services);")]
        [InlineData("""OptionsConfigurationServiceCollectionExtensions.Configure<MyClass>(name: "", config: section, services: services);""")]
        [InlineData("OptionsConfigurationServiceCollectionExtensions.Configure<MyClass>(configureBinder: _ => { }, config: section, services: services);")]
        [InlineData("""OptionsConfigurationServiceCollectionExtensions.Configure<MyClass>(configureBinder: _ => { }, config: section, name: "", services: services);""")]
        [InlineData("""OptionsConfigurationServiceCollectionExtensions.Configure<MyClass>(name: "", services: services, configureBinder: _ => { }, config: section);""")]
        public async Task Configure_T_NamedParameters_OutOfOrder(string row)
        {
            string source = $$"""
                    using System.Collections.Generic;
                    using Microsoft.Extensions.Configuration;
                    using Microsoft.Extensions.DependencyInjection;
                    
                    public class Program
                    {
                        public static void Main()
                        {
                            ConfigurationBuilder configurationBuilder = new();
                            IConfiguration config = configurationBuilder.Build();
                            IConfigurationSection section = config.GetSection("MySection");
                            ServiceCollection services = new();

                            {{row}}
                        }
                    
                        public class MyClass
                        {
                            public string MyString { get; set; }
                            public int MyInt { get; set; }
                            public List<int> MyList { get; set; }
                            public Dictionary<string, string> MyDictionary { get; set; }
                        }
                    }
                    """;

            await VerifyThatSourceIsGenerated(source);
        }

        [Theory]
        [InlineData("OptionsBuilderConfigurationExtensions.Bind(config: config, optionsBuilder: optionsBuilder);")]
        [InlineData("OptionsBuilderConfigurationExtensions.Bind(configureBinder: _ => { }, config: config, optionsBuilder: optionsBuilder);")]
        [InlineData("OptionsBuilderConfigurationExtensions.Bind(config: config, configureBinder: _ => { }, optionsBuilder: optionsBuilder);")]
        public async Task Bind_T_NamedParameters_OutOfOrder(string row)
        {
            string source = $$"""
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

                            {{row}}
                        }
                    
                        public class MyClass
                        {
                            public string MyString { get; set; }
                            public int MyInt { get; set; }
                            public List<int> MyList { get; set; }
                            public Dictionary<string, string> MyDictionary { get; set; }
                        }
                    }
                    """;

            await VerifyThatSourceIsGenerated(source);
        }

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

        [Fact]
        public async Task Bind_T()
        {
            await VerifyAgainstBaselineUsingFile("Bind_T.generated.txt", GetBindSource(), extType: ExtensionClassType.OptionsBuilder);
        }

        [Fact]
        public async Task Bind_T_BinderOptions()
        {
            await VerifyAgainstBaselineUsingFile("Bind_T_BinderOptions.generated.txt", GetBindSource(", _ => { }"), extType: ExtensionClassType.OptionsBuilder);
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

            await VerifyAgainstBaselineUsingFile("BindConfiguration.generated.txt", GetSource(), extType: ExtensionClassType.OptionsBuilder);
            await VerifyAgainstBaselineUsingFile("BindConfiguration.generated.txt", GetSource(@", _ => { }"), extType: ExtensionClassType.OptionsBuilder);
        }
        #endregion OptionsBuilder<T> extensions.
    }
}
