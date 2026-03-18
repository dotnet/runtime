// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarExtractOptions_Tests : TarTestsBase
    {
        [Fact]
        public void DefaultValues()
        {
            TarExtractOptions options = new TarExtractOptions();
            Assert.False(options.OverwriteFiles);
            Assert.Equal(TarLinkStrategy.PreserveLink, options.HardLinkStrategy);
        }

        [Theory]
        [MemberData(nameof(GetLinkStrategies))]
        public void HardLinkStrategy_AcceptsValidValues(TarLinkStrategy strategy)
        {
            TarExtractOptions options = new TarExtractOptions() { HardLinkStrategy = strategy };
            Assert.Equal(strategy, options.HardLinkStrategy);
        }

        [Theory]
        [MemberData(nameof(GetInvalidLinkStrategies))]
        public void HardLinkStrategy_RejectsInvalidValues(TarLinkStrategy strategy)
        {
            TarExtractOptions options = new TarExtractOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.HardLinkStrategy = strategy);
        }
    }
}
