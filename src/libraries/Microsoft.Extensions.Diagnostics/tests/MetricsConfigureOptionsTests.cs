// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public class MetricsConfigureOptionsTests
    {
        [Fact]
        public void LoadInstrumentRulesTest()
        {
            var options = new MetricsOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["MeterName1:InstrumentName1"] = "true";
            configuration["MeterName1:InstrumentName2"] = "false";
            configuration["MeterName1:Default"] = "true";

            MetricsConfigureOptions.LoadInstrumentRules(options, configuration.GetSection("MeterName1"), MeterScope.Local, "ListenerName");

            Assert.Equal(3, options.Rules.Count);

            var rule1 = options.Rules.Single(rule => rule.InstrumentName == "InstrumentName1");
            AssertRule(rule1, "MeterName1", "InstrumentName1", "ListenerName", MeterScope.Local, true);
            var rule2 = options.Rules.Single(rule => rule.InstrumentName == "InstrumentName2");
            AssertRule(rule2, "MeterName1", "InstrumentName2", "ListenerName", MeterScope.Local, false);
            var rule3 = options.Rules.Single(rule => rule.InstrumentName == null);
            AssertRule(rule3, "MeterName1", null, "ListenerName", MeterScope.Local, true);
        }

        [Fact]
        public void LoadMeterRulesBoolTest()
        {
            var options = new MetricsOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Section:MeterName1"] = "true";
            configuration["Section:MeterName2"] = "false";
            configuration["Section:Default"] = "true";
            MetricsConfigureOptions.LoadMeterRules(options, configuration.GetSection("Section"), MeterScope.Local, listenerName: "ListenerName");

            Assert.Equal(3, options.Rules.Count);

            var rule1 = options.Rules.Single(rule => rule.MeterName == "MeterName1");
            AssertRule(rule1, "MeterName1", null, "ListenerName", MeterScope.Local, true);
            var rule2 = options.Rules.Single(rule => rule.MeterName == "MeterName2");
            AssertRule(rule2, "MeterName2", null, "ListenerName", MeterScope.Local, false);
            var rule3 = options.Rules.Single(rule => rule.MeterName == null);
            AssertRule(rule3, null, null, "ListenerName", MeterScope.Local, true);
        }

        [Fact]
        public void LoadMeterRulesWithInstrumentsTest()
        {
            var options = new MetricsOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Section:MeterName1:InstrumentName1"] = "true";
            configuration["Section:MeterName1:InstrumentName2"] = "false";
            configuration["Section:MeterName1:Default"] = "true";
            configuration["Section:MeterName2:InstrumentName1"] = "true";
            configuration["Section:MeterName2:InstrumentName2"] = "false";
            configuration["Section:MeterName2:Default"] = "true";
            configuration["Section:Default"] = "true";
            MetricsConfigureOptions.LoadMeterRules(options, configuration.GetSection("Section"), MeterScope.Local, listenerName: "ListenerName");

            Assert.Equal(7, options.Rules.Count);

            var rule1 = options.Rules.Single(rule => rule.MeterName == "MeterName1" && rule.InstrumentName == "InstrumentName1");
            AssertRule(rule1, "MeterName1", "InstrumentName1", "ListenerName", MeterScope.Local, true);
            var rule2 = options.Rules.Single(rule => rule.MeterName == "MeterName1" && rule.InstrumentName == "InstrumentName2");
            AssertRule(rule2, "MeterName1", "InstrumentName2", "ListenerName", MeterScope.Local, false);
            var rule3 = options.Rules.Single(rule => rule.MeterName == "MeterName1" && rule.InstrumentName == null);
            AssertRule(rule3, "MeterName1", null, "ListenerName", MeterScope.Local, true);

            var rule4 = options.Rules.Single(rule => rule.MeterName == "MeterName2" && rule.InstrumentName == "InstrumentName1");
            AssertRule(rule4, "MeterName2", "InstrumentName1", "ListenerName", MeterScope.Local, true);
            var rule5 = options.Rules.Single(rule => rule.MeterName == "MeterName2" && rule.InstrumentName == "InstrumentName2");
            AssertRule(rule5, "MeterName2", "InstrumentName2", "ListenerName", MeterScope.Local, false);
            var rule6 = options.Rules.Single(rule => rule.MeterName == "MeterName2" && rule.InstrumentName == null);
            AssertRule(rule6, "MeterName2", null, "ListenerName", MeterScope.Local, true);

            var rule7 = options.Rules.Single(rule => rule.MeterName == null);
            AssertRule(rule7, null, null, "ListenerName", MeterScope.Local, true);
        }

        [Theory]
        [InlineData("EnabledMetrics:Default", "true", null, null, null, MeterScope.Global | MeterScope.Local, true)]
        [InlineData("EnabledMetrics:MeterName", "false", "MeterName", null, null, MeterScope.Global | MeterScope.Local, false)]
        [InlineData("EnabledMetrics:MeterName:Default", "true", "MeterName", null, null, MeterScope.Global | MeterScope.Local, true)]
        [InlineData("EnabledMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", null, MeterScope.Global | MeterScope.Local, false)]
        [InlineData("EnabledGlobalMetrics:Default", "true", null, null, null, MeterScope.Global, true)]
        [InlineData("EnabledGlobalMetrics:MeterName", "false", "MeterName", null, null, MeterScope.Global, false)]
        [InlineData("EnabledGlobalMetrics:MeterName:Default", "true", "MeterName", null, null, MeterScope.Global, true)]
        [InlineData("EnabledGlobalMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", null, MeterScope.Global, false)]
        [InlineData("EnabledLocalMetrics:Default", "true", null, null, null, MeterScope.Local, true)]
        [InlineData("EnabledLocalMetrics:MeterName", "false", "MeterName", null, null, MeterScope.Local, false)]
        [InlineData("EnabledLocalMetrics:MeterName:Default", "true", "MeterName", null, null, MeterScope.Local, true)]
        [InlineData("EnabledLocalMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", null, MeterScope.Local, false)]
        [InlineData("Listener:EnabledMetrics:Default", "true", null, null, "Listener", MeterScope.Global | MeterScope.Local, true)]
        [InlineData("Listener:EnabledMetrics:MeterName", "false", "MeterName", null, "Listener", MeterScope.Global | MeterScope.Local, false)]
        [InlineData("Listener:EnabledMetrics:MeterName:Default", "true", "MeterName", null, "Listener", MeterScope.Global | MeterScope.Local, true)]
        [InlineData("Listener:EnabledMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", "Listener", MeterScope.Global | MeterScope.Local, false)]
        [InlineData("Listener:EnabledGlobalMetrics:Default", "true", null, null, "Listener", MeterScope.Global, true)]
        [InlineData("Listener:EnabledGlobalMetrics:MeterName", "false", "MeterName", null, "Listener", MeterScope.Global, false)]
        [InlineData("Listener:EnabledGlobalMetrics:MeterName:Default", "true", "MeterName", null, "Listener", MeterScope.Global, true)]
        [InlineData("Listener:EnabledGlobalMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", "Listener", MeterScope.Global, false)]
        [InlineData("Listener:EnabledLocalMetrics:Default", "true", null, null, "Listener", MeterScope.Local, true)]
        [InlineData("Listener:EnabledLocalMetrics:MeterName", "false", "MeterName", null, "Listener", MeterScope.Local, false)]
        [InlineData("Listener:EnabledLocalMetrics:MeterName:Default", "true", "MeterName", null, "Listener", MeterScope.Local, true)]
        [InlineData("Listener:EnabledLocalMetrics:MeterName:InstrumentName", "false", "MeterName", "InstrumentName", "Listener", MeterScope.Local, false)]
        public void LoadTopLevelRulesTest(string key, string value, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enabled)
        {
            var options = new MetricsOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[key] = value;

            new MetricsConfigureOptions(configuration).Configure(options);
            var rule = Assert.Single(options.Rules);
            AssertRule(rule, meterName, instrumentName, listenerName, scopes, enabled);
        }

        private static void AssertRule(InstrumentRule rule, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enable)
        {
            Assert.Equal(meterName, rule.MeterName);
            Assert.Equal(instrumentName, rule.InstrumentName);
            Assert.Equal(listenerName, rule.ListenerName);
            Assert.Equal(scopes, rule.Scopes);
            Assert.Equal(enable, rule.Enable);
        }
    }
}
