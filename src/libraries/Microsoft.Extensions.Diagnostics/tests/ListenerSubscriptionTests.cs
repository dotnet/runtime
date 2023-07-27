// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Diagnostics.Metrics;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class ListenerSubscriptionTests
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("", "", "")]
        [InlineData("*", "", "")]
        [InlineData("lonG", "", "")]
        [InlineData("lonG.*", "", "")]
        [InlineData("lonG.sillY.meteR", "", "")]
        [InlineData("lonG.sillY.meteR.*", "", "")]
        [InlineData("lonG.sillY.meteR.namE", "", "")]
        [InlineData("lonG.sillY.meteR.namE.*", "", "")]
        [InlineData("lonG.sillY.meteR.namE", "instrumenTnamE", "listeneRnamE")]
        public void RuleMatchesTest(string meterName, string instrumentName, string listenerName)
        {
            RemoteExecutor.Invoke((string m, string i, string l) => {
                var rule = new InstrumentEnableRule(m, i, l, MeterScope.Global, enable: true);
                var meter = new Meter("Long.Silly.Meter.Name");
                var instrument = meter.CreateCounter<int>("InstrumentName");
                Assert.True(ListenerSubscription.RuleMatches(rule, instrument, "ListenerName"));
            }, meterName, instrumentName, listenerName).Dispose();
        }

        [Theory]
        [MemberData(nameof(IsMoreSpecificTestData))]
        public void IsMoreSpecificTest(InstrumentEnableRule rule, InstrumentEnableRule? best)
        {
            Assert.True(ListenerSubscription.IsMoreSpecific(rule, best));

            if (best != null)
            {
                Assert.False(ListenerSubscription.IsMoreSpecific(best, rule));
            }
        }

        public static IEnumerable<object[]> IsMoreSpecificTestData() => new object[][]
        {
            // Anything is better than null
            new object[] { new InstrumentEnableRule(null, null, null, MeterScope.Global, true), null },
            // Any field is better than empty
            new object[] { new InstrumentEnableRule("meterName", null, null, MeterScope.Global, true),
                new InstrumentEnableRule(null, null, null, MeterScope.Global, true) },
            new object[] { new InstrumentEnableRule(null, "instrumentName", null, MeterScope.Global, true),
                new InstrumentEnableRule(null, null, null, MeterScope.Global, true) },
            new object[] { new InstrumentEnableRule(null, null, "listenerName", MeterScope.Global, true),
                new InstrumentEnableRule(null, null, null, MeterScope.Global, true) },
            // Multiple fields are better than one, in order Meter, Instrument, Listener
            // Longer Meter Name is better
            // Scopes
        };
    }
}
