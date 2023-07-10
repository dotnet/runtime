// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration.Tests
{
    public partial class ConfigurationBindingGeneratorTests
    {
        private const string BindCallSampleCode = @"
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
                config.Bind(""key"", configObj);
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

        [Theory]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(LanguageVersion.CSharp11)]
        public async Task Bind(LanguageVersion langVersion) =>
            await VerifyAgainstBaselineUsingFile("Bind.generated.txt", BindCallSampleCode, langVersion, extType: ExtensionClassType.ConfigurationBinder);

        [Fact]
        public async Task Bind_Instance()
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

                                MyClass configObj = new();
                                config.Bind(configObj);
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

            await VerifyAgainstBaselineUsingFile("Bind_Instance.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Bind_Instance_BinderOptions()
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

                                MyClass configObj = new();
                                config.Bind(configObj, options => { });
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

            await VerifyAgainstBaselineUsingFile("Bind_Instance_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Bind_Key_Instance()
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

                                MyClass configObj = new();
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

            await VerifyAgainstBaselineUsingFile("Bind_Key_Instance.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get()
        {
            string source = @"
        using System.Collections.Generic;
        using Microsoft.Extensions.Configuration;

        public class Program
        {
        	public static void Main()
        	{
        		ConfigurationBuilder configurationBuilder = new();
        		IConfigurationRoot config = configurationBuilder.Build();

        		MyClass configObj = config.Get<MyClass>();
                configObj = config.Get(typeof(MyClass2));
                configObj = config.Get<MyClass>(binderOptions => { });
                configObj = config.Get(typeof(MyClass2), binderOptions => { });
        	}

        	public class MyClass
        	{
        		public string MyString { get; set; }
        		public int MyInt { get; set; }
        		public List<int> MyList { get; set; }
                public int[] MyArray { get; set; }
        		public Dictionary<string, string> MyDictionary { get; set; }
        	}

            public class MyClass2
            {
                public int MyInt { get; set; }
            }

            public class MyClass3
            {
                public int MyInt { get; set; }
            }

            public class MyClass4
            {
                public int MyInt { get; set; }
            }
        }";

            await VerifyAgainstBaselineUsingFile("Get.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_T()
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

                                MyClass configObj = config.Get<MyClass>();
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_T.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_T_BinderOptions()
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

                                MyClass configObj = config.Get<MyClass>(binderOptions => { });
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_T_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_TypeOf()
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

                                MyClass configObj = config.Get(typeof(MyClass2));
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_TypeOf.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task Get_TypeOf_BinderOptions()
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

                                config.Get(typeof(MyClass2), binderOptions => { });
                            }

                            public class MyClass
                            {
                                public string MyString { get; set; }
                                public int MyInt { get; set; }
                                public List<int> MyList { get; set; }
                                public int[] MyArray { get; set; }
                                public Dictionary<string, string> MyDictionary { get; set; }
                            }

                            public class MyClass2
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass3
                            {
                                public int MyInt { get; set; }
                            }

                            public class MyClass4
                            {
                                public int MyInt { get; set; }
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("Get_TypeOf_BinderOptions.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue()
        {
            string source = @"
        using System.Collections.Generic;
        using System.Globalization;
        using Microsoft.Extensions.Configuration;

        public class Program
        {
        	public static void Main()
        	{
        		ConfigurationBuilder configurationBuilder = new();
        		IConfigurationRoot config = configurationBuilder.Build();

        		config.GetValue<int>(""key"");
                config.GetValue(typeof(bool?), ""key"");
                config.GetValue<MyClass>(""key"", new MyClass());
                config.GetValue<byte[]>(""key"", new byte[] { });
                config.GetValue(typeof(CultureInfo), ""key"", CultureInfo.InvariantCulture);
        	}

        	public class MyClass
        	{
        		public string MyString { get; set; }
        		public int MyInt { get; set; }
        		public List<int> MyList { get; set; }
                public int[] MyArray { get; set; }
        		public Dictionary<string, string> MyDictionary { get; set; }
        	}
        }";

            await VerifyAgainstBaselineUsingFile("GetValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_T_Key()
        {
            string source = """
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue<int>("key");
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_T_Key.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_T_Key_DefaultValue()
        {
            string source = """
                        using System.Collections.Generic;
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue<int>("key", 5);
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_T_Key_DefaultValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_TypeOf_Key()
        {
            string source = """
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue(typeof(bool?), "key");
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_TypeOf_Key.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task GetValue_TypeOf_Key_DefaultValue()
        {
            string source = """
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                config.GetValue(typeof(CultureInfo), "key", CultureInfo.InvariantCulture);
                            }
                        }
                    """;

            await VerifyAgainstBaselineUsingFile("GetValue_TypeOf_Key_DefaultValue.generated.txt", source, extType: ExtensionClassType.ConfigurationBinder);
        }

        [Fact]
        public async Task None()
        {
            string source = @"
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
        }"
            ;

            var (d, r) = await RunGenerator(source);
            Assert.Empty(r);
            Assert.Empty(d);
        }

        [Fact]
        public async Task Primitives()
        {
            string source = """
                        using System;
                        using System.Globalization;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfigurationRoot config = configurationBuilder.Build();

                                MyClass obj = new();
                                config.Bind(obj);
                            }

                            public class MyClass
                            {
                                public bool Prop0 { get; set; }
                                public byte Prop1 { get; set; }
                                public sbyte Prop2 { get; set; }
                                public char Prop3 { get; set; }
                                public double Prop4 { get; set; }
                                public string Prop5 { get; set; }
                                public int Prop6 { get; set; }
                                public short Prop8 { get; set; }
                                public long Prop9 { get; set; }
                                public float Prop10 { get; set; }
                                public ushort Prop13 { get; set; }
                                public uint Prop14 { get; set; }
                                public ulong Prop15 { get; set; }
                                public object Prop16 { get; set; }
                                public CultureInfo Prop17 { get; set; }
                                public DateTime Prop19 { get; set; }
                                public DateTimeOffset Prop20 { get; set; }
                                public decimal Prop21 { get; set; }
                                public TimeSpan Prop23 { get; set; }
                                public Guid Prop24 { get; set; }
                                public Uri Prop25 { get; set; }
                                public Version Prop26 { get; set; }
                                public DayOfWeek Prop27 { get; set; }
                                public Int128 Prop7 { get; set; }
                                public Half Prop11 { get; set; }
                                public UInt128 Prop12 { get; set; }
                                public DateOnly Prop18 { get; set; }
                                public TimeOnly Prop22 { get; set; }
                                public byte[] Prop22 { get; set; }
                                public int Prop23 { get; set; }
                                public DateTime Prop24 { get; set; }
                            }
                        }
                        """;

            await VerifyAgainstBaselineUsingFile("Primitives.generated.txt", source);
        }

        [Fact]
        public async Task Collections()
        {
            string source = """
                        using System.Collections.Generic;
                        using Microsoft.Extensions.Configuration;

                        public class Program
                        {
                            public static void Main()
                            {
                                ConfigurationBuilder configurationBuilder = new();
                                IConfiguration config = configurationBuilder.Build();
                                IConfigurationSection section = config.GetSection(""MySection"");

                                section.Get<MyClassWithCustomCollections>();
                            }

                            public class MyClassWithCustomCollections
                            {
                                public CustomDictionary<string, int> CustomDictionary { get; set; }
                                public CustomList CustomList { get; set; }
                                public ICustomDictionary<string> ICustomDictionary { get; set; }
                                public ICustomSet<MyClassWithCustomCollections> ICustomCollection { get; set; }
                                public IReadOnlyList<int> IReadOnlyList { get; set; }
                                public IReadOnlyDictionary<MyClassWithCustomCollections, int> UnsupportedIReadOnlyDictionaryUnsupported { get; set; }
                                public IReadOnlyDictionary<string, int> IReadOnlyDictionary { get; set; }
                            }

                            public class CustomDictionary<TKey, TValue> : Dictionary<TKey, TValue>
                            {
                            }

                            public class CustomList : List<string>
                            {
                            }

                            public interface ICustomDictionary<T> : IDictionary<T, string>
                            {
                            }

                            public interface ICustomSet<T> : ISet<T>
                            {
                            }
                        }
                        """;

            await VerifyAgainstBaselineUsingFile("Collections.generated.txt", source, assessDiagnostics: (d) =>
            {
                Assert.Equal(6, d.Length);
                Test(d.Where(diagnostic => diagnostic.Id is "SYSLIB1100"), "Did not generate binding logic for a type");
                Test(d.Where(diagnostic => diagnostic.Id is "SYSLIB1101"), "Did not generate binding logic for a property on a type");

                static void Test(IEnumerable<Diagnostic> d, string expectedTitle)
                {
                    Assert.Equal(3, d.Count());
                    foreach (Diagnostic diagnostic in d)
                    {
                        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                        Assert.Contains(expectedTitle, diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture));
                    }
                }
            });
        }
    }
}
