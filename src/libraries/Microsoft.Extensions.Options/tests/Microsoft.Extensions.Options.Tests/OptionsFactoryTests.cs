// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsFactoryTest
    {
        [Fact]
        public void CreateSupportsNames()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>("1", options => options.Message = "one");
            services.Configure<FakeOptions>("2", options => options.Message = "two");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("one", factory.Create("1").Message);
            Assert.Equal("two", factory.Create("2").Message);
        }

        [Fact]
        public void NamesAreCaseSensitive()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>("UP", options => options.Message += "UP");
            services.Configure<FakeOptions>("up", options => options.Message += "up");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("UP", factory.Create("UP").Message);
            Assert.Equal("up", factory.Create("up").Message);
        }        
        
        [Fact]
        public void CanConfigureAllOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions>(o => o.Message = "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void PostConfiguresInOrderAfterConfigures()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>("-", o => o.Message += "-");
            services.ConfigureAll<FakeOptions>(o => o.Message += "[");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigure<FakeOptions>("-", o => o.Message += "-");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "A");
            services.PostConfigure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "B");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "C");
            services.PostConfigure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigure<FakeOptions>("-", o => o.Message += "-");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.ConfigureAll<FakeOptions>(o => o.Message += "]");
            services.Configure<FakeOptions>("-", o => o.Message += "-");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("[]ABC", factory.Create("1").Message);
            Assert.Equal("[++]A+BC+", factory.Create("+").Message);
            Assert.Equal("-[]--ABC-", factory.Create("-").Message);
        }

        [Fact]
        public void CanConfigureAndPostConfigureAllOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions>(o => o.Message = "D");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "f");
            services.ConfigureAll<FakeOptions>(o => o.Message += "e");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "ault");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void NamedSnapshotsConfiguresInRegistrationOrder()
        {
            var services = new ServiceCollection();
            services.Configure<FakeOptions>("-", o => o.Message += "-");
            services.ConfigureAll<FakeOptions>(o => o.Message += "A");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.ConfigureAll<FakeOptions>(o => o.Message += "B");
            services.ConfigureAll<FakeOptions>(o => o.Message += "C");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.Configure<FakeOptions>("-", o => o.Message += "-");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABC", factory.Create("1").Message);
            Assert.Equal("A+BC+", factory.Create("+").Message);
            Assert.Equal("-ABC-", factory.Create("-").Message);
        }

        [Fact]
        public void CanConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.Configure<FakeOptions>(o => o.Message += "0");
            services.Configure<FakeOptions>("1", o => o.Message += "1");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("Default").Message);
            Assert.Equal("Default0", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1", factory.Create("1").Message);
        }

        [Fact]
        public void CanConfigureAndPostConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.Configure<FakeOptions>(o => o.Message += "0");
            services.Configure<FakeOptions>("1", o => o.Message += "1");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "PostConfigure");
            services.PostConfigure<FakeOptions>(o => o.Message += "2");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "3");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("DefaultPostConfigure", factory.Create("Default").Message);
            Assert.Equal("Default0PostConfigure2", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1PostConfigure3", factory.Create("1").Message);
        }

        [Fact]
        public void CanPostConfigureAllOptions()
        {
            var services = new ServiceCollection();
            services.PostConfigureAll<FakeOptions>(o => o.Message = "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void CanPostConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection();
            services.PostConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.PostConfigure<FakeOptions>(o => o.Message += "0");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "1");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("Default").Message);
            Assert.Equal("Default0", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1", factory.Create("1").Message);
        }

        public class FakeOptionsSetupA : ConfigureOptions<FakeOptions>
        {
            public FakeOptionsSetupA() : base(o => o.Message += "A") { }
        }

        public class FakeOptionsSetupB : ConfigureOptions<FakeOptions>
        {
            public FakeOptionsSetupB() : base(o => o.Message += "B") { }
        }

        [Fact]
        public void CanConfigureOptionsOnlyDefault()
        {
            var services = new ServiceCollection();
            services.ConfigureOptions<FakeOptionsSetupA>();
            services.ConfigureOptions(typeof(FakeOptionsSetupB));
            services.ConfigureOptions(new ConfigureOptions<FakeOptions>(o => o.Message += "hi!"));

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABhi!", factory.Create(Options.DefaultName).Message);
            Assert.Equal("", factory.Create("anything").Message);
        }

        public class FakeOptionsValidation : IValidateOptions<FakeOptions>
        {
            public ValidateOptionsResult Validate(string name, FakeOptions options)
            {
                return ValidateOptionsResult.Fail("Hello world");
            }
        }

        [Fact]
        public void CanValidateOptionsWithConfigureOptions()
        {
            var factory = new ServiceCollection()
                .ConfigureOptions<FakeOptionsValidation>()
                .BuildServiceProvider()
                .GetRequiredService<IOptionsFactory<FakeOptions>>();

            var ex = Assert.Throws<OptionsValidationException>(() => factory.Create(Options.DefaultName));
            var message = Assert.Single(ex.Failures);
            Assert.Equal("Hello world", message);
        }

        public class UberSetup
            : IConfigureNamedOptions<FakeOptions>
            , IConfigureNamedOptions<FakeOptions2>
            , IPostConfigureOptions<FakeOptions>
            , IPostConfigureOptions<FakeOptions2>
            , IValidateOptions<FakeOptions>
            , IValidateOptions<FakeOptions2>
        {
            public void Configure(string name, FakeOptions options)
                => options.Message += "["+name;

            public void Configure(FakeOptions options) => Configure(Options.DefaultName, options);

            public void Configure(string name, FakeOptions2 options)
                => options.Message += "[["+name;

            public void Configure(FakeOptions2 options) => Configure(Options.DefaultName, options);

            public void PostConfigure(string name, FakeOptions options)
                => options.Message += "]";

            public void PostConfigure(string name, FakeOptions2 options)
                => options.Message += "]]";

            public ValidateOptionsResult Validate(string name, FakeOptions options)
            {
                if (options.Message == "[foo]")
                {
                    return ValidateOptionsResult.Fail("Invalid message '[foo]'");
                }

                return ValidateOptionsResult.Success;
            }

            public ValidateOptionsResult Validate(string name, FakeOptions2 options)
            {
                if (options.Message.Contains("[[bar]]"))
                {
                    return ValidateOptionsResult.Fail($"Invalid message '{options.Message}'");
                }

                return ValidateOptionsResult.Success;
            }
        }

        [Fact]
        public void CanConfigureTwoOptionsWithConfigureOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureOptions<UberSetup>();

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            var factory2 = sp.GetRequiredService<IOptionsFactory<FakeOptions2>>();

            Assert.Equal("[]", factory.Create(Options.DefaultName).Message);
            Assert.Equal("[hao]", factory.Create("hao").Message);

            var ex1 = Assert.Throws<OptionsValidationException>(() => factory.Create("foo"));
            var failure1 = Assert.Single(ex1.Failures);
            Assert.Equal("Invalid message '[foo]'", failure1);

            Assert.Equal("[[]]", factory2.Create(Options.DefaultName).Message);
            Assert.Equal("[[hao]]", factory2.Create("hao").Message);

            var ex2 = Assert.Throws<OptionsValidationException>(() => factory2.Create("bar"));
            var failure2 = Assert.Single(ex2.Failures);
            Assert.Equal("Invalid message '[[bar]]'", failure2);
        }

        [Fact]
        public void CanMixConfigureEverything()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions2>(o => o.Message = "!");
            services.ConfigureOptions<UberSetup>();
            services.Configure<FakeOptions>("#1", o => o.Message += "#");
            services.PostConfigureAll<FakeOptions2>(o => o.Message += "|");
            services.ConfigureOptions(new PostConfigureOptions<FakeOptions>("override", o => o.Message = "override"));
            services.PostConfigure<FakeOptions>("end", o => o.Message += "_");
            services.ConfigureOptions(new ValidateOptions<FakeOptions>("fail", o => false, "fail"));

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            var factory2 = sp.GetRequiredService<IOptionsFactory<FakeOptions2>>();

            Assert.Equal("[]", factory.Create(Options.DefaultName).Message);
            Assert.Equal("[hao]", factory.Create("hao").Message);
            Assert.Equal("[#1#]", factory.Create("#1").Message);
            Assert.Equal("![[#1]]|", factory2.Create("#1").Message);
            Assert.Equal("![[]]|", factory2.Create(Options.DefaultName).Message);
            Assert.Equal("![[hao]]|", factory2.Create("hao").Message);
            Assert.Equal("override", factory.Create("override").Message);
            Assert.Equal("![[override]]|", factory2.Create("override").Message);
            Assert.Equal("[end]_", factory.Create("end").Message);
            Assert.Equal("![[end]]|", factory2.Create("end").Message);

            var ex1 = Assert.Throws<OptionsValidationException>(() => factory.Create("foo"));
            var failure1 = Assert.Single(ex1.Failures);
            Assert.Equal("Invalid message '[foo]'", failure1);

            var ex2 = Assert.Throws<OptionsValidationException>(() => factory2.Create("bar"));
            var failure2 = Assert.Single(ex2.Failures);
            Assert.Equal("Invalid message '![[bar]]|'", failure2);

            var ex3 = Assert.Throws<OptionsValidationException>(() => factory.Create("fail"));
            var failure3 = Assert.Single(ex3.Failures);
            Assert.Equal("fail", failure3);
        }

        [Fact]
        public void ConfigureOptionsThrowsWithAction()
        {
            var services = new ServiceCollection();
            Action<FakeOptions> act = o => o.Message = "whatev";
            var error = Assert.Throws<InvalidOperationException>(() => services.ConfigureOptions(act));
            Assert.Equal("No IConfigureOptions<>, IPostConfigureOptions<>, or IValidateOptions<> implementations were found, did you mean to call Configure<> or PostConfigure<>?", error.Message);
        }

        [Fact]
        public void ConfigureOptionsThrowsIfNothingFound()
        {
            var services = new ServiceCollection();
            var error = Assert.Throws<InvalidOperationException>(() => services.ConfigureOptions(new object()));
            Assert.Equal("No IConfigureOptions<>, IPostConfigureOptions<>, or IValidateOptions<> implementations were found.", error.Message);
        }
    }
}
