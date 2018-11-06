// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public class UberBothSetup : IConfigureNamedOptions<FakeOptions>, IConfigureNamedOptions<FakeOptions2>, IPostConfigureOptions<FakeOptions>, IPostConfigureOptions<FakeOptions2>
        {
            public void Configure(string name, FakeOptions options)
                => options.Message += "["+name;

            public void Configure(FakeOptions options) => Configure(Options.DefaultName, options);

            public void Configure(string name, FakeOptions2 options)
                => options.Message += "[["+name;

            public void Configure(FakeOptions2 options) => Configure(Options.DefaultName, options);

            public void PostConfigure(string name, FakeOptions2 options)
                => options.Message += "]]";

            public void PostConfigure(string name, FakeOptions options)
                => options.Message += "]";
        }

        [Fact]
        public void CanConfigureTwoOptionsWithConfigureOptions()
        {
            var services = new ServiceCollection();
            services.ConfigureOptions<UberBothSetup>();

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            var factory2 = sp.GetRequiredService<IOptionsFactory<FakeOptions2>>();

            Assert.Equal("[]", factory.Create(Options.DefaultName).Message);
            Assert.Equal("[hao]", factory.Create("hao").Message);
            Assert.Equal("[[]]", factory2.Create(Options.DefaultName).Message);
            Assert.Equal("[[hao]]", factory2.Create("hao").Message);
        }

        [Fact]
        public void CanMixConfigureEverything()
        {
            var services = new ServiceCollection();
            services.ConfigureAll<FakeOptions2>(o => o.Message = "!");
            services.ConfigureOptions<UberBothSetup>();
            services.Configure<FakeOptions>("#1", o => o.Message += "#");
            services.PostConfigureAll<FakeOptions2>(o => o.Message += "|");
            services.ConfigureOptions(new PostConfigureOptions<FakeOptions>("override", o => o.Message = "override"));
            services.PostConfigure<FakeOptions>("end", o => o.Message += "_");

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
        }

        [Fact]
        public void ConfigureOptionsThrowsWithAction()
        {
            var services = new ServiceCollection();
            Action<FakeOptions> act = o => o.Message = "whatev";
            var error = Assert.Throws<InvalidOperationException>(() => services.ConfigureOptions(act));
            Assert.Equal("No IConfigureOptions<> or IPostConfigureOptions<> implementations were found, did you mean to call Configure<> or PostConfigure<>?", error.Message);
        }

        [Fact]
        public void ConfigureOptionsThrowsIfNothingFound()
        {
            var services = new ServiceCollection();
            var error = Assert.Throws<InvalidOperationException>(() => services.ConfigureOptions(new object()));
            Assert.Equal("No IConfigureOptions<> or IPostConfigureOptions<> implementations were found.", error.Message);
        }
    }
}