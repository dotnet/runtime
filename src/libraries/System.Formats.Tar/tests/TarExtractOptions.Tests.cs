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
            Assert.Equal(TarHardLinkMode.PreserveLink, options.HardLinkMode);
        }

        [Theory]
        [MemberData(nameof(GetLinkStrategies))]
        public void HardLinkMode_AcceptsValidValues(TarHardLinkMode mode)
        {
            TarExtractOptions options = new TarExtractOptions() { HardLinkMode = mode };
            Assert.Equal(mode, options.HardLinkMode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidLinkStrategies))]
        public void HardLinkMode_RejectsInvalidValues(TarHardLinkMode mode)
        {
            TarExtractOptions options = new TarExtractOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.HardLinkMode = mode);
        }
    }
}
