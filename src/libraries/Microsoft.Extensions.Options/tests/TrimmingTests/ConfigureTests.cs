// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

class Program
{
    static int Main(string[] args)
    {
        ServiceCollection services = new ServiceCollection();
        services.Configure<OptionsA>(o =>
        {
            o.OptionValue = 99;
        });
        services.ConfigureOptions<OptionsAPostConfigure>();
        services.AddOptions<OptionsB>()
            .Configure<IOptions<OptionsA>>((b, a) =>
            {
                b.OptionString = a.Value.OptionValue.ToString();
            });

        ServiceProvider provider = services.BuildServiceProvider();

        OptionsA optionsA = provider.GetService<IOptions<OptionsA>>().Value;
        OptionsB optionsB = provider.GetService<IOptionsMonitor<OptionsB>>().CurrentValue;
        OptionsC optionsC = provider.GetService<IOptions<OptionsC>>().Value;
        OptionsD optionsD = provider.GetService<IOptionsFactory<OptionsD>>().Create(string.Empty);

        if (optionsA.OptionValue != 99 ||
            optionsA.PostConfigureOption != 101 ||
            optionsB.OptionString != "99" ||
            optionsC is null ||
            optionsD is null)
        {
            return -1;
        }

        return 100;
    }

    private class OptionsA
    {
        public int OptionValue { get; set; }
        public int PostConfigureOption { get; set; }
    }

    private class OptionsAPostConfigure : IPostConfigureOptions<OptionsA>
    {
        public void PostConfigure(string name, OptionsA options)
        {
            if (name.Length != 0)
            {
                throw new ArgumentException("name must be empty", nameof(name));
            }

            options.PostConfigureOption = 101;
        }
    }

    private class OptionsB
    {
        public string OptionString { get; set; }
    }

    // Note: OptionsC is never configured
    private class OptionsC
    {
        public string OptionString { get; set; }
    }

    // Note: OptionsD is never configured
    private class OptionsD
    {
        public string OptionString { get; set; }
    }
}
