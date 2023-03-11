// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests
{
#if NETCOREAPP
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public class ConfingurationBindingSourceGeneratorTests
    {
        [Fact]
        public async Task TestBaseline_TestBindCallGen()
        {
            string testSourceCode = @"
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class Program
{
	public static void Main()
	{
		ConfigurationBuilder configurationBuilder = new();
		IConfigurationRoot config = configurationBuilder.Build();

		MyClass options = new();
		config.Bind(options);
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
}";

            await VerifyAgainstBaselineUsingFile("TestBindCallGen.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestGetCallGen()
        {
            string testSourceCode = @"
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class Program
{
	public static void Main()
	{
		ConfigurationBuilder configurationBuilder = new();
		IConfigurationRoot config = configurationBuilder.Build();

		MyClass options = config.Get<MyClass>();
	}
	
	public class MyClass
	{
		public string MyString { get; set; }
		public int MyInt { get; set; }
		public List<int> MyList { get; set; }
		public Dictionary<string, string> MyDictionary { get; set; }
	}
}";

            await VerifyAgainstBaselineUsingFile("TestGetCallGen.generated.txt", testSourceCode);
        }

        [Fact]
        public async Task TestBaseline_TestConfigureCallGen()
        {
            string testSourceCode = @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
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
        services.Configure<MyClass>(section);
	}
	
	public class MyClass
	{
		public string MyString { get; set; }
		public int MyInt { get; set; }
		public List<int> MyList { get; set; }
		public Dictionary<string, string> MyDictionary { get; set; }
	}
}";

            await VerifyAgainstBaselineUsingFile("TestConfigureCallGen.generated.txt", testSourceCode);
        }

        private async Task VerifyAgainstBaselineUsingFile(string filename, string testSourceCode)
        {
            string baseline = LineEndingsHelper.Normalize(await File.ReadAllTextAsync(Path.Combine("Baselines", filename)).ConfigureAwait(false));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(ConfigurationBindingSourceGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(Environment.NewLine);

            var (d, r) = await RoslynTestUtils.RunGenerator(
                new ConfigurationBindingSourceGenerator(),
                new[] {
                    typeof(ConfigurationBinder).Assembly,
                    typeof(IConfiguration).Assembly,
                    typeof(IServiceCollection).Assembly,
                    typeof(IDictionary).Assembly,
                    typeof(ServiceCollection).Assembly,
                    typeof(OptionsConfigurationServiceCollectionExtensions).Assembly,
                },
                new[] { testSourceCode }).ConfigureAwait(false);

            Assert.Empty(d);
            Assert.Single(r);

            Assert.True(RoslynTestUtils.CompareLines(expectedLines, r[0].SourceText,
                out string errorMessage), errorMessage);
        }
    }
#endif
}
