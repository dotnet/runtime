// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriterOptions_Tests : TarTestsBase
    {
        [Fact]
        public void DefaultValues()
        {
            TarWriterOptions options = new TarWriterOptions();
            Assert.Equal(TarEntryFormat.Pax, options.Format);
            Assert.Equal(TarLinkStrategy.PreserveLink, options.HardLinkStrategy);
        }

        [Theory]
        [MemberData(nameof(GetTarEntryFormats))]
        public void Format_AcceptsValidValues(TarEntryFormat format)
        {
            TarWriterOptions options = new TarWriterOptions() { Format = format };
            Assert.Equal(format, options.Format);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTarEntryFormats))]
        public void Format_RejectsInvalidValues(TarEntryFormat format)
        {
            TarWriterOptions options = new TarWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Format = format);
        }

        [Theory]
        [MemberData(nameof(GetLinkStrategies))]
        public void HardLinkStrategy_AcceptsValidValues(TarLinkStrategy strategy)
        {
            TarWriterOptions options = new TarWriterOptions() { HardLinkStrategy = strategy };
            Assert.Equal(strategy, options.HardLinkStrategy);
        }

        [Theory]
        [MemberData(nameof(GetInvalidLinkStrategies))]
        public void HardLinkStrategy_RejectsInvalidValues(TarLinkStrategy strategy)
        {
            TarWriterOptions options = new TarWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.HardLinkStrategy = strategy);
        }
    }
}
