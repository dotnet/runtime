// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract class TarTestsConversionBase : TarTestsBase
    {
        protected abstract TarEntryFormat FormatUnderTest { get; }

        private readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

        protected void TestConstructionConversion(
            TarEntryType originalEntryType,
            TarEntryFormat firstFormat,
            TarEntryFormat formatToConvert,
            bool setATimeCTime = false)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using MemoryStream dataStream = new MemoryStream();

            TarEntryType actualEntryType = GetTarEntryTypeForTarEntryFormat(originalEntryType, firstFormat);

            TarEntry firstEntry = GetFirstEntry(dataStream, actualEntryType, firstFormat, setATimeCTime);
            TarEntry otherEntry = ConvertAndVerifyEntry(firstEntry, originalEntryType, formatToConvert, now);
        }

        protected void TestConstructionConversionBackAndForth(
            TarEntryType originalEntryType,
            TarEntryFormat firstAndLastFormat,
            TarEntryFormat formatToConvert)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using MemoryStream dataStream = new MemoryStream();

            TarEntryType firstAndLastEntryType = GetTarEntryTypeForTarEntryFormat(originalEntryType, firstAndLastFormat);

            TarEntry firstEntry = GetFirstEntry(dataStream, firstAndLastEntryType, firstAndLastFormat);
            TarEntry otherEntry = ConvertAndVerifyEntry(firstEntry, originalEntryType, formatToConvert, now); // First conversion
            DateTimeOffset secondNow = DateTimeOffset.UtcNow;
            ConvertAndVerifyEntry(otherEntry, firstAndLastEntryType, firstAndLastFormat, secondNow); // Convert back to original format
        }

        private TarEntry GetFirstEntry(MemoryStream dataStream, TarEntryType entryType, TarEntryFormat format, bool setATimeCTime = false)
        {
            TarEntry firstEntry = InvokeTarEntryCreationConstructor(format, entryType, "file.txt", setATimeCTime);

            firstEntry.Gid = TestGid;
            firstEntry.Uid = TestUid;
            firstEntry.Mode = TestMode;
            // Modification Time is set to 'DateTimeOffset.UtcNow' in the constructor

            if (entryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
            {
                firstEntry.DataStream = dataStream;
            }
            else if (entryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                firstEntry.LinkName = TestLinkName;
            }
            else if (entryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice)
            {
                PosixTarEntry posixTarEntry = firstEntry as PosixTarEntry;
                posixTarEntry.DeviceMajor = entryType is TarEntryType.BlockDevice ? TestBlockDeviceMajor : TestCharacterDeviceMajor;
                posixTarEntry.DeviceMinor = entryType is TarEntryType.BlockDevice ? TestBlockDeviceMinor : TestCharacterDeviceMinor;
            }

            return firstEntry;
        }

        private TarEntry ConvertAndVerifyEntry(TarEntry originalEntry, TarEntryType entryType, TarEntryFormat formatToConvert, DateTimeOffset initialNow)
        {
            TarEntry convertedEntry = InvokeTarEntryConversionConstructor(formatToConvert, originalEntry);

            CheckConversionType(convertedEntry, formatToConvert);
            Assert.Equal(formatToConvert, convertedEntry.Format);

            TarEntryType convertedEntryType = GetTarEntryTypeForTarEntryFormat(entryType, formatToConvert);
            Assert.Equal(convertedEntryType, convertedEntry.EntryType);

            Assert.Equal(originalEntry.Gid, convertedEntry.Gid);
            Assert.Equal(originalEntry.Uid, convertedEntry.Uid);
            Assert.Equal(originalEntry.Mode, convertedEntry.Mode);
            Assert.Equal(originalEntry.ModificationTime, convertedEntry.ModificationTime);

            if (originalEntry.EntryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
            {
                Assert.Same(originalEntry.DataStream, convertedEntry.DataStream);
            }
            else if (originalEntry.EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                Assert.Equal(originalEntry.LinkName, convertedEntry.LinkName);
            }
            else if (originalEntry.EntryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice)
            {
                PosixTarEntry originalPosixTarEntry = originalEntry as PosixTarEntry;
                PosixTarEntry convertedPosixTarEntry = convertedEntry as PosixTarEntry;
                Assert.Equal(originalPosixTarEntry.DeviceMajor, convertedPosixTarEntry.DeviceMajor);
                Assert.Equal(originalPosixTarEntry.DeviceMinor, convertedPosixTarEntry.DeviceMinor);
            }

            // 'null' indicates the originalEntry did not include a timestamp.
            GetTimestampsFromEntry(originalEntry, out DateTimeOffset? expectedATime, out DateTimeOffset? expectedCTime);
            // Converting to Gnu only preserves timestamps when the original is also Gnu.
            if (formatToConvert == TarEntryFormat.Gnu && originalEntry.Format != TarEntryFormat.Gnu)
            {
                expectedATime = null;
                expectedCTime = null;
            }

            if (formatToConvert is TarEntryFormat.Pax)
            {
                PaxTarEntry paxEntry = convertedEntry as PaxTarEntry;
                DateTimeOffset? actualAccessTime = TryGetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaATime);
                DateTimeOffset? actualChangeTime = TryGetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaCTime);
                Assert.Equal(expectedATime, actualAccessTime);
                Assert.Equal(expectedCTime, actualChangeTime);
            }
            else if (formatToConvert is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = convertedEntry as GnuTarEntry;
                Assert.Equal(expectedATime ?? default, gnuEntry.AccessTime);
                Assert.Equal(expectedCTime ?? default, gnuEntry.ChangeTime);
            }

            return convertedEntry;
        }

        private void GetTimestampsFromEntry(TarEntry originalEntry, out DateTimeOffset? expectedATime, out DateTimeOffset? expectedCTime)
        {
            if (originalEntry.Format is TarEntryFormat.Pax)
            {
                PaxTarEntry originalPaxEntry = originalEntry as PaxTarEntry;

                expectedATime = null;
                if (originalPaxEntry.ExtendedAttributes.ContainsKey("atime"))
                {
                    expectedATime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "atime");
                }

                expectedCTime = null;
                if (originalPaxEntry.ExtendedAttributes.ContainsKey("ctime"))
                {
                    expectedCTime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "ctime");
                }
            }
            else if (originalEntry.Format is TarEntryFormat.Gnu)
            {
                GnuTarEntry originalGnuEntry = originalEntry as GnuTarEntry;
                expectedATime = originalGnuEntry.AccessTime;
                // default means: no timestamp.
                if (expectedATime == default(DateTimeOffset))
                {
                    expectedATime = null;
                }
                expectedCTime = originalGnuEntry.ChangeTime;
                if (expectedCTime == default(DateTimeOffset))
                {
                    expectedCTime = null;
                }
            }
            else
            {
                // Format has no timestamps.
                expectedATime = null;
                expectedCTime = null;
            }
        }

        protected TarEntry InvokeTarEntryConversionConstructor(TarEntryFormat targetFormat, TarEntry other)
            => targetFormat switch
            {
                TarEntryFormat.V7 => new V7TarEntry(other),
                TarEntryFormat.Ustar => new UstarTarEntry(other),
                TarEntryFormat.Pax => new PaxTarEntry(other),
                TarEntryFormat.Gnu => new GnuTarEntry(other),
                _ => throw new InvalidDataException($"Unexpected format: {targetFormat}")
            };

        [Fact]
        public void Constructor_ConversionFromGnu_ATimeCTime() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, FormatUnderTest, setATimeCTime: true);

        [Fact]
        public void Constructor_ConversionFromPax_ATimeCTime() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Pax, FormatUnderTest, setATimeCTime: true);
    }
}
