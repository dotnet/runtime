// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsBuilderTest
    {
        [Fact]
        public void CanSupportDefaultName()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder = services.AddOptions<FakeOptions>();
            builder
                .Configure(options => options.Message += "Igetstomped")
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![Default]", factory.Create(Options.DefaultName).Message);
        }

        [Fact]
        public void CanSupportNamedOptions()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder1 = services.AddOptions<FakeOptions>("1");
            var builder2 = services.AddOptions<FakeOptions>("2");
            builder1
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "one");
            builder2
                .Bind(config)
                .PostConfigure(options => options.Message += ">")
                .Configure(options => options.Message += "<")
                .Configure(options => options.Message += "two");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![one]", factory.Create("1").Message);
            Assert.Equal("!<two>", factory.Create("2").Message);
        }

        [Fact]
        public void CanMixConfigureCallsOutsideBuilderInOrder()
        {
            var services = new ServiceCollection();
            var builder = services.AddOptions<FakeOptions>("1");

            services.ConfigureAll<FakeOptions>(o => o.Message += "A");
            builder.PostConfigure(o => o.Message += "D");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "E");
            builder.Configure(o => o.Message += "B");
            services.Configure<FakeOptions>("1", o => o.Message += "C");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABCDE", factory.Create("1").Message);
        }


        public class SomeService
        {
            public SomeService(string stuff) => Stuff = stuff;

            public string Stuff { get; set; }
        }

        public class Counter
        {
            public int Count { get; set; }

            public void Increment() => Count++;
        }

        public class SomeCounterConsumer
        {
            public SomeCounterConsumer(Counter c)
            {
                Current = c.Count;
                c.Increment();
            }

            public int Current { get; }
        }

        [Fact]
        public void ConfigureOptionsWithSingletonDepWillUpdate()
        {
            var someService = new SomeService("Something");
            var services = new ServiceCollection().AddOptions().AddSingleton(someService);
            services.AddOptions<FakeOptions>().Configure<SomeService>((o, s) => o.Message = s.Stuff);
            services.AddOptions<FakeOptions>("named").Configure<SomeService>((o, s) => o.Message = "named " + s.Stuff);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("named Something", factory.Create("named").Message);
            Assert.Equal("Something", factory.Create(Options.DefaultName).Message);

            someService.Stuff = "Else";

            Assert.Equal("named Else", factory.Create("named").Message);
            Assert.Equal("Else", factory.Create(Options.DefaultName).Message);
        }

        [Fact]
        public void ConfigureOptionsWithTransientDep()
        {
            var services = new ServiceCollection()
                .AddSingleton<Counter>()
                .AddTransient<SomeCounterConsumer>();
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "none");
            services.AddOptions<FakeOptions>("1dep").Configure<SomeCounterConsumer>((o, s) => o.Message = s.Current + "");
            services.AddOptions<FakeOptions>("2dep").Configure<SomeCounterConsumer, SomeCounterConsumer>((o, s, s2) => o.Message = s.Current + " " + s2.Current);
            services.AddOptions<FakeOptions>("3dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3) => o.Message = s.Current + " " + s2.Current + " " + s3.Current);
            services.AddOptions<FakeOptions>("4dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current);
            services.AddOptions<FakeOptions>("5dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4, s5) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current + " " + s5.Current);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("none", factory.Create(Options.DefaultName).Message);
            Assert.Equal("0", factory.Create("1dep").Message);
            Assert.Equal("1 2", factory.Create("2dep").Message);
            Assert.Equal("3 4 5", factory.Create("3dep").Message);
            Assert.Equal("6 7 8 9", factory.Create("4dep").Message);
            Assert.Equal("10 11 12 13 14", factory.Create("5dep").Message);

            // Factory caches configures
            Assert.Equal("0", factory.Create("1dep").Message);

            // New factory will reexecute
            Assert.Equal("15", sp.GetRequiredService<IOptionsFactory<FakeOptions>>().Create("1dep").Message);
        }

        [Fact]
        public void PostConfigureOptionsWithTransientDep()
        {
            var services = new ServiceCollection()
                .AddSingleton<Counter>()
                .AddTransient<SomeCounterConsumer>();
            services.ConfigureAll<FakeOptions>(o => o.Message = "Override");
            services.AddOptions<FakeOptions>().PostConfigure(o => o.Message = "none");
            services.AddOptions<FakeOptions>("1dep").PostConfigure<SomeCounterConsumer>((o, s) => o.Message = s.Current + "");
            services.AddOptions<FakeOptions>("2dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer>((o, s, s2) => o.Message = s.Current + " " + s2.Current);
            services.AddOptions<FakeOptions>("3dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3) => o.Message = s.Current + " " + s2.Current + " " + s3.Current);
            services.AddOptions<FakeOptions>("4dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current);
            services.AddOptions<FakeOptions>("5dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4, s5) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current + " " + s5.Current);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("none", factory.Create(Options.DefaultName).Message);
            Assert.Equal("0", factory.Create("1dep").Message);
            Assert.Equal("1 2", factory.Create("2dep").Message);
            Assert.Equal("3 4 5", factory.Create("3dep").Message);
            Assert.Equal("6 7 8 9", factory.Create("4dep").Message);
            Assert.Equal("10 11 12 13 14", factory.Create("5dep").Message);

            // Factory caches configures
            Assert.Equal("0", factory.Create("1dep").Message);

            // New factory will reexecute
            Assert.Equal("15", sp.GetRequiredService<IOptionsFactory<FakeOptions>>().Create("1dep").Message);
        }

        [Fact]
        public void CanConfigureWithServiceProvider()
        {
            var someService = new SomeService("Something");
            var services = new ServiceCollection().AddSingleton(someService);
            services.AddOptions<FakeOptions>().Configure<IServiceProvider>((o, s) => o.Message = s.GetRequiredService<SomeService>().Stuff);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Something", factory.Create(Options.DefaultName).Message);
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
            services.AddOptions<ComplexOptions>().Bind(new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
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
            services.AddOptions<ComplexOptions>("named").Bind(new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("named");
            Assert.Equal("stuff", options.GetType().GetProperty(property).GetValue(options));
        }

    }
}