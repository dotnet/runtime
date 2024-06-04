// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Tests
{
    public class InstrumentRuleTests
    {
        [Fact]
        public void ScopeRequired()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentRule(null, null, null, MeterScope.None, true));
            Assert.Equal("scopes", ex.ParamName);
        }
    }
}
