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
            Assert.Equal(TarHardLinkMode.PreserveLink, options.HardLinkMode);
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
        public void HardLinkMode_AcceptsValidValues(TarHardLinkMode mode)
        {
            TarWriterOptions options = new TarWriterOptions() { HardLinkMode = mode };
            Assert.Equal(mode, options.HardLinkMode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidLinkStrategies))]
        public void HardLinkMode_RejectsInvalidValues(TarHardLinkMode mode)
        {
            TarWriterOptions options = new TarWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.HardLinkMode = mode);
        }
    }
}
