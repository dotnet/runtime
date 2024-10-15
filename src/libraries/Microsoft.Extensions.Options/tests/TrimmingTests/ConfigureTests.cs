// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        LocalOptionsValidator localOptionsValidator = new LocalOptionsValidator();
        OptionsUsingValidationAttributes optionsUsingValidationAttributes = new OptionsUsingValidationAttributes
        {
            P1 = "12345",
            P2 = new List<string> { "1234", "12345" },
            P3 = "123456",
            P4 = "12345",
            P5 = 7,
            P6 = TimeSpan.FromSeconds(5),
        };

        ValidateOptionsResult result = localOptionsValidator.Validate("", optionsUsingValidationAttributes);
        if (result.Failed)
        {
            return -2;
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

public class OptionsUsingValidationAttributes
{
    [Required]
    [MinLength(5)]
    public string P1 { get; set; }

    [Required]
    [MaxLength(5)]
    public List<string> P2 { get; set; }

    [Length(2, 8)]
    public string P3 { get; set; }

    [Compare("P1")]
    public string P4 { get; set; }

    [Range(1, 10, MinimumIsExclusive = true, MaximumIsExclusive = true)]
    public int P5 { get; set; }

    [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
    public TimeSpan P6 { get; set; }

}

[OptionsValidator]
public partial class LocalOptionsValidator : IValidateOptions<OptionsUsingValidationAttributes>
{
}

