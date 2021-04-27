// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class OptionsTest
    {
        [Fact]
        public void UsesFactory()
        {
            var services = new ServiceCollection()
                .AddSingleton<IOptionsFactory<FakeOptions>, FakeOptionsFactory>()
                .Configure<FakeOptions>(o => o.Message = "Ignored")
                .BuildServiceProvider();

            var snap = services.GetRequiredService<IOptions<FakeOptions>>();
            Assert.Equal(FakeOptionsFactory.Options, snap.Value);
        }

        [Fact]
        public void CanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var services = new ServiceCollection();
            services.Configure<ComplexOptions>(new ConfigurationBuilder().AddInMemoryCollection(dic).Build());
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<ComplexOptions>>().Value;
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Fact]
        public void CanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual","Sup"}
            };
            var services = new ServiceCollection();
            services.Configure<DerivedOptions>(new ConfigurationBuilder().AddInMemoryCollection(dic).Build());
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<DerivedOptions>>().Value;
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void CanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "stuff"},
            };
            var services = new ServiceCollection();
            services.Configure<ComplexOptions>(new ConfigurationBuilder().AddInMemoryCollection(dic).Build());
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<ComplexOptions>>().Value;
            Assert.Equal("stuff", ComplexOptions.StaticProperty);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void ShouldBeIgnoredTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var services = new ServiceCollection();
            services.Configure<ComplexOptions>(new ConfigurationBuilder().AddInMemoryCollection(dic).Build());
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<ComplexOptions>>().Value;
            Assert.Null(options.GetType().GetProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void CanBindToNonPublicProperties(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var services = new ServiceCollection();
            services.Configure<ComplexOptions>(new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<ComplexOptions>>().Value;
            Assert.Equal("stuff", options.GetType().GetProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void CanNamedBindToNonPublicProperties(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var services = new ServiceCollection();
            services.Configure<ComplexOptions>("named", new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("named");
            Assert.Equal("stuff", options.GetType().GetProperty(property).GetValue(options));
        }

        [Fact]
        public void SetupCallsInOrder()
        {
            var services = new ServiceCollection().AddOptions();
            var dic = new Dictionary<string, string>
            {
                {"Message", "!"},
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(dic);
            var config = builder.Build();
            services.Configure<FakeOptions>(o => o.Message += "Igetstomped");
            services.Configure<FakeOptions>(config);
            services.Configure<FakeOptions>(o => o.Message += "a");
            services.Configure<FakeOptions>(o => o.Message += "z");

            var service = services.BuildServiceProvider().GetService<IOptions<FakeOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            Assert.Equal("!az", options.Message);
        }

        [Fact]
        public void PostConfiguresInRegistrationOrderAfterConfigures()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>(o => o.Message += "_");
            services.PostConfigure<FakeOptions>(o => o.Message += "A");
            services.PostConfigure<FakeOptions>(o => o.Message += "B");
            services.PostConfigure<FakeOptions>(o => o.Message += "C");
            services.Configure<FakeOptions>(o => o.Message += "-");

            var sp = services.BuildServiceProvider();
            var option = sp.GetRequiredService<IOptions<FakeOptions>>().Value;
            Assert.Equal("_-ABC", option.Message);
        }

        public static TheoryData Configure_GetsNullableOptionsFromConfiguration_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, string>, IDictionary<string, object>>
                {
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(NullableOptions.MyNullableBool), "true" },
                            { nameof(NullableOptions.MyNullableInt), "1" },
                            { nameof(NullableOptions.MyNullableDateTime), new DateTime(2015, 1, 1).ToString(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern) }
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(NullableOptions.MyNullableBool), true },
                            { nameof(NullableOptions.MyNullableInt), 1 },
                            { nameof(NullableOptions.MyNullableDateTime), new DateTime(2015, 1, 1) }
                        }
                    },
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(NullableOptions.MyNullableBool), "false" },
                            { nameof(NullableOptions.MyNullableInt), "-1" },
                            { nameof(NullableOptions.MyNullableDateTime), new DateTime(1995, 12, 31).ToString(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern) }
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(NullableOptions.MyNullableBool), false },
                            { nameof(NullableOptions.MyNullableInt), -1 },
                            { nameof(NullableOptions.MyNullableDateTime), new DateTime(1995, 12, 31) }
                        }
                    },
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(NullableOptions.MyNullableBool), null },
                            { nameof(NullableOptions.MyNullableInt), null },
                            { nameof(NullableOptions.MyNullableDateTime), null }
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(NullableOptions.MyNullableBool), null },
                            { nameof(NullableOptions.MyNullableInt), null },
                            { nameof(NullableOptions.MyNullableDateTime), null }
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(Configure_GetsNullableOptionsFromConfiguration_Data))]
        public void Configure_GetsNullableOptionsFromConfiguration(
            IDictionary<string, string> configValues,
            IDictionary<string, object> expectedValues)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new ConfigurationBuilder().AddInMemoryCollection(configValues);
            var config = builder.Build();
            services.Configure<NullableOptions>(config);

            // Act
            var options = services.BuildServiceProvider().GetService<IOptions<NullableOptions>>().Value;

            // Assert
            var optionsProps = options.GetType().GetProperties().ToDictionary(p => p.Name);
            var assertions = expectedValues
                .Select(_ => new Action<KeyValuePair<string, object>>(kvp =>
                    Assert.Equal(kvp.Value, optionsProps[kvp.Key].GetValue(options))));
            Assert.Collection(expectedValues, assertions.ToArray());
        }

        public static TheoryData Configure_GetsEnumOptionsFromConfiguration_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, string>, IDictionary<string, object>>
                {
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(EnumOptions.UriKind), (UriKind.Absolute).ToString() },
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(EnumOptions.UriKind), UriKind.Absolute },
                        }
                    },
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(EnumOptions.UriKind), ((int)UriKind.Absolute).ToString() },
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(EnumOptions.UriKind), UriKind.Absolute },
                        }
                    },
                    {
                        new Dictionary<string, string>
                        {
                            { nameof(EnumOptions.UriKind), null },
                        },
                        new Dictionary<string, object>
                        {
                            { nameof(EnumOptions.UriKind), UriKind.RelativeOrAbsolute },  //default enum, since not overridden by configuration
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(Configure_GetsEnumOptionsFromConfiguration_Data))]
        public void Configure_GetsEnumOptionsFromConfiguration(
            IDictionary<string, string> configValues,
            IDictionary<string, object> expectedValues)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new ConfigurationBuilder().AddInMemoryCollection(configValues);
            var config = builder.Build();
            services.Configure<EnumOptions>(config);

            // Act
            var options = services.BuildServiceProvider().GetService<IOptions<EnumOptions>>().Value;

            // Assert
            var optionsProps = options.GetType().GetProperties().ToDictionary(p => p.Name);
            var assertions = expectedValues
                .Select(_ => new Action<KeyValuePair<string, object>>(kvp =>
                    Assert.Equal(kvp.Value, optionsProps[kvp.Key].GetValue(options))));
            Assert.Collection(expectedValues, assertions.ToArray());
        }

        [Fact]
        public void Options_StaticCreateCreateMakesOptions()
        {
            var options = Options.Create(new FakeOptions
            {
                Message = "This is a message"
            });

            Assert.Equal("This is a message", options.Value.Message);
        }

        [Fact]
        public void OptionsWrapper_MakesOptions()
        {
            var options = new OptionsWrapper<FakeOptions>(new FakeOptions
            {
                Message = "This is a message"
            });

            Assert.Equal("This is a message", options.Value.Message);
        }

        [Fact]
        public void Options_CanOverrideForSpecificTOptions()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>(options =>
            {
                options.Message = "Initial value";
            });

            services.AddSingleton(Options.Create(new FakeOptions
            {
                Message = "Override"
            }));

            var sp = services.BuildServiceProvider();
            Assert.Equal("Override", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);
        }

        [Fact]
        public void Options_CanCreateInstancesWithoutDefaultCtor()
        {
            var services = new ServiceCollection();
            services.Configure<OptionsWithoutDefaultCtor>("Named", options =>
            {
                options.Message = "Initial value";
            });

            services.AddSingleton<IOptionsFactory<OptionsWithoutDefaultCtor>, CustomOptionsFactory>();

            var sp = services.BuildServiceProvider();
            var optionsWithoutDefaultCtor = sp.GetRequiredService<IOptionsMonitor<OptionsWithoutDefaultCtor>>().Get("Named");
            Assert.Equal("Initial value", optionsWithoutDefaultCtor.Message);
            Assert.Equal("Named", optionsWithoutDefaultCtor.Name);
        }

        [Fact]
        public void Options_WithoutDefaultCtor_ThrowDuringResolution()
        {
            var services = new ServiceCollection();
            services.Configure<OptionsWithoutDefaultCtor>("Named", options =>
            {
                options.Message = "Initial value";
            });

            var sp = services.BuildServiceProvider();
            Assert.Throws<MissingMethodException>(() => sp.GetRequiredService<IOptionsMonitor<OptionsWithoutDefaultCtor>>().Get("Named"));
        }

        private class OptionsWithoutDefaultCtor
        {
            public string Name { get; }
            public string Message { get; set; }

            public OptionsWithoutDefaultCtor(string name)
            {
                Name = name;
            }
        }

        private class CustomOptionsFactory: OptionsFactory<OptionsWithoutDefaultCtor>
        {
            public CustomOptionsFactory(IEnumerable<IConfigureOptions<OptionsWithoutDefaultCtor>> setups, IEnumerable<IPostConfigureOptions<OptionsWithoutDefaultCtor>> postConfigures, IEnumerable<IValidateOptions<OptionsWithoutDefaultCtor>> validations) : base(setups, postConfigures, validations)
            {
            }

            protected override OptionsWithoutDefaultCtor CreateInstance(string name)
            {
                return new OptionsWithoutDefaultCtor(name);
            }
        }
    }
}
