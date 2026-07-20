// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tracing.Tests
{
    public class TracingConfigureOptionsTests
    {
        [Fact]
        public void LoadActivityRulesAddsOneRulePerOperationAndCollapsesDefault()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["SourceName:Op1"] = "true";
            configuration["SourceName:Op2"] = "false";
            configuration["SourceName:Default"] = "true";

            TracingConfigureOptions.LoadActivityRules(options, configuration.GetSection("SourceName"), ActivitySourceScopes.Local, "Listener");

            Assert.Equal(3, options.Rules.Count);
            AssertRule(options.Rules.Single(r => r.OperationName == "Op1"), "SourceName", "Op1", "Listener", ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.OperationName == "Op2"), "SourceName", "Op2", "Listener", ActivitySourceScopes.Local, false);
            AssertRule(options.Rules.Single(r => r.OperationName is null), "SourceName", null, "Listener", ActivitySourceScopes.Local, true);
        }

        [Fact]
        public void LoadActivityRulesIgnoresNonBooleanEntries()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["SourceName:Op1"] = "true";
            configuration["SourceName:Op2"] = "not-a-bool";

            TracingConfigureOptions.LoadActivityRules(options, configuration.GetSection("SourceName"), ActivitySourceScopes.Local, listenerName: null);

            var rule = Assert.Single(options.Rules);
            AssertRule(rule, "SourceName", "Op1", null, ActivitySourceScopes.Local, true);
        }

        [Fact]
        public void LoadActivitySourceRulesAddsLeafBoolEntries()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Section:SourceA"] = "true";
            configuration["Section:SourceB"] = "false";
            configuration["Section:Default"] = "true";

            TracingConfigureOptions.LoadActivitySourceRules(options, configuration.GetSection("Section"), ActivitySourceScopes.Local, "Listener");

            Assert.Equal(3, options.Rules.Count);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceA"), "SourceA", null, "Listener", ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceB"), "SourceB", null, "Listener", ActivitySourceScopes.Local, false);
            AssertRule(options.Rules.Single(r => r.SourceName is null), null, null, "Listener", ActivitySourceScopes.Local, true);
        }

        [Fact]
        public void LoadActivitySourceRulesDescendsIntoSectionsWithChildren()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Section:SourceA:Op1"] = "true";
            configuration["Section:SourceA:Op2"] = "false";
            configuration["Section:SourceA:Default"] = "true";
            configuration["Section:SourceB:Op1"] = "true";
            configuration["Section:SourceB:Default"] = "false";
            configuration["Section:Default"] = "true";

            TracingConfigureOptions.LoadActivitySourceRules(options, configuration.GetSection("Section"), ActivitySourceScopes.Local, "Listener");

            Assert.Equal(6, options.Rules.Count);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceA" && r.OperationName == "Op1"), "SourceA", "Op1", "Listener", ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceA" && r.OperationName == "Op2"), "SourceA", "Op2", "Listener", ActivitySourceScopes.Local, false);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceA" && r.OperationName is null), "SourceA", null, "Listener", ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceB" && r.OperationName == "Op1"), "SourceB", "Op1", "Listener", ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.SourceName == "SourceB" && r.OperationName is null), "SourceB", null, "Listener", ActivitySourceScopes.Local, false);
            AssertRule(options.Rules.Single(r => r.SourceName is null && r.OperationName is null), null, null, "Listener", ActivitySourceScopes.Local, true);
        }

        [Fact]
        public void LoadActivitySourceRulesKeepsLiteralDefaultSourceWhenItHasChildren()
        {
            // A source literally named "Default" with nested operations is treated as the source "Default",
            // not as the all-sources catch-all. The Default -> null collapse only fires for the leaf-bool form.
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Section:Default:Op"] = "true";

            TracingConfigureOptions.LoadActivitySourceRules(options, configuration.GetSection("Section"), ActivitySourceScopes.Local, "Listener");

            var rule = Assert.Single(options.Rules);
            AssertRule(rule, "Default", "Op", "Listener", ActivitySourceScopes.Local, true);
        }

        [Theory]
        [InlineData("EnabledTracing:Default", "true", null, null, null, ActivitySourceScopes.Global | ActivitySourceScopes.Local, true)]
        [InlineData("EnabledTracing:Source", "false", "Source", null, null, ActivitySourceScopes.Global | ActivitySourceScopes.Local, false)]
        [InlineData("EnabledTracing:Source:Default", "true", "Source", null, null, ActivitySourceScopes.Global | ActivitySourceScopes.Local, true)]
        [InlineData("EnabledTracing:Source:Op", "false", "Source", "Op", null, ActivitySourceScopes.Global | ActivitySourceScopes.Local, false)]
        [InlineData("EnabledGlobalTracing:Default", "true", null, null, null, ActivitySourceScopes.Global, true)]
        [InlineData("EnabledGlobalTracing:Source", "false", "Source", null, null, ActivitySourceScopes.Global, false)]
        [InlineData("EnabledGlobalTracing:Source:Default", "true", "Source", null, null, ActivitySourceScopes.Global, true)]
        [InlineData("EnabledGlobalTracing:Source:Op", "false", "Source", "Op", null, ActivitySourceScopes.Global, false)]
        [InlineData("EnabledLocalTracing:Default", "true", null, null, null, ActivitySourceScopes.Local, true)]
        [InlineData("EnabledLocalTracing:Source", "false", "Source", null, null, ActivitySourceScopes.Local, false)]
        [InlineData("EnabledLocalTracing:Source:Default", "true", "Source", null, null, ActivitySourceScopes.Local, true)]
        [InlineData("EnabledLocalTracing:Source:Op", "false", "Source", "Op", null, ActivitySourceScopes.Local, false)]
        [InlineData("Listener:EnabledTracing:Default", "true", null, null, "Listener", ActivitySourceScopes.Global | ActivitySourceScopes.Local, true)]
        [InlineData("Listener:EnabledTracing:Source", "false", "Source", null, "Listener", ActivitySourceScopes.Global | ActivitySourceScopes.Local, false)]
        [InlineData("Listener:EnabledTracing:Source:Default", "true", "Source", null, "Listener", ActivitySourceScopes.Global | ActivitySourceScopes.Local, true)]
        [InlineData("Listener:EnabledTracing:Source:Op", "false", "Source", "Op", "Listener", ActivitySourceScopes.Global | ActivitySourceScopes.Local, false)]
        [InlineData("Listener:EnabledGlobalTracing:Default", "true", null, null, "Listener", ActivitySourceScopes.Global, true)]
        [InlineData("Listener:EnabledGlobalTracing:Source", "false", "Source", null, "Listener", ActivitySourceScopes.Global, false)]
        [InlineData("Listener:EnabledGlobalTracing:Source:Default", "true", "Source", null, "Listener", ActivitySourceScopes.Global, true)]
        [InlineData("Listener:EnabledGlobalTracing:Source:Op", "false", "Source", "Op", "Listener", ActivitySourceScopes.Global, false)]
        [InlineData("Listener:EnabledLocalTracing:Default", "true", null, null, "Listener", ActivitySourceScopes.Local, true)]
        [InlineData("Listener:EnabledLocalTracing:Source", "false", "Source", null, "Listener", ActivitySourceScopes.Local, false)]
        [InlineData("Listener:EnabledLocalTracing:Source:Default", "true", "Source", null, "Listener", ActivitySourceScopes.Local, true)]
        [InlineData("Listener:EnabledLocalTracing:Source:Op", "false", "Source", "Op", "Listener", ActivitySourceScopes.Local, false)]
        public void TopLevelKeyMapsToExpectedRule(string key, string value, string? sourceName, string? operationName, string? listenerName, ActivitySourceScopes scopes, bool enabled)
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[key] = value;

            new TracingConfigureOptions(configuration).Configure(options);

            var rule = Assert.Single(options.Rules);
            AssertRule(rule, sourceName, operationName, listenerName, scopes, enabled);
        }

        [Fact]
        public void TopLevelSectionKeysAreCaseInsensitive()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["enabledtracing:source"] = "true";
            configuration["ENABLEDGLOBALTRACING:source"] = "false";

            new TracingConfigureOptions(configuration).Configure(options);

            Assert.Equal(2, options.Rules.Count);
            AssertRule(options.Rules.Single(r => r.Scopes == (ActivitySourceScopes.Global | ActivitySourceScopes.Local)),
                "source", null, null, ActivitySourceScopes.Global | ActivitySourceScopes.Local, true);
            AssertRule(options.Rules.Single(r => r.Scopes == ActivitySourceScopes.Global),
                "source", null, null, ActivitySourceScopes.Global, false);
        }

        [Fact]
        public void NonBooleanLeafValueIsIgnored()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["EnabledTracing:Source"] = "not-a-bool";

            new TracingConfigureOptions(configuration).Configure(options);

            Assert.Empty(options.Rules);
        }

        [Fact]
        public void ListenerSectionWithoutKnownSubSectionAddsNoRules()
        {
            var options = new TracingOptions();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["Listener:Unrelated:Source"] = "true";

            new TracingConfigureOptions(configuration).Configure(options);

            Assert.Empty(options.Rules);
        }

        [Fact]
        public void ConstructorThrowsOnNullConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => new TracingConfigureOptions(null!));
        }

        private static void AssertRule(TracingRule rule, string? sourceName, string? operationName, string? listenerName, ActivitySourceScopes scopes, bool enable)
        {
            Assert.Equal(sourceName, rule.SourceName);
            Assert.Equal(operationName, rule.OperationName);
            Assert.Equal(listenerName, rule.ListenerName);
            Assert.Equal(scopes, rule.Scopes);
            Assert.Equal(enable, rule.Enable);
        }
    }
}
