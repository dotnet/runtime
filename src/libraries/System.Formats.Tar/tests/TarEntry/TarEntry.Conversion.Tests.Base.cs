// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarTestsConversionBase : TarTestsBase
    {
        private readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

        protected void TestConstructionConversion(
            TarEntryType originalEntryType,
            TarEntryFormat firstFormat,
            TarEntryFormat formatToConvert)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using MemoryStream dataStream = new MemoryStream();

            TarEntryType actualEntryType = GetTarEntryTypeForTarEntryFormat(originalEntryType, firstFormat);

            TarEntry firstEntry = GetFirstEntry(dataStream, actualEntryType, firstFormat);
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

        private TarEntry GetFirstEntry(MemoryStream dataStream, TarEntryType entryType, TarEntryFormat format)
        {
            TarEntry firstEntry = InvokeTarEntryCreationConstructor(format, entryType, "file.txt");

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

            if (format is TarEntryFormat.Pax)
            {
                PaxTarEntry paxEntry = firstEntry as PaxTarEntry;
                Assert.Contains("atime", paxEntry.ExtendedAttributes);
                Assert.Contains("ctime", paxEntry.ExtendedAttributes);
                Assert.Equal(firstEntry.ModificationTime, GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "atime"));
                Assert.Equal(firstEntry.ModificationTime, GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "ctime"));
            }
            else if (format is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = firstEntry as GnuTarEntry;
                Assert.Equal(default, gnuEntry.AccessTime);
                Assert.Equal(default, gnuEntry.ChangeTime);
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

            if (formatToConvert is TarEntryFormat.Pax)
            {
                PaxTarEntry paxEntry = convertedEntry as PaxTarEntry;
                if (originalEntry.Format is TarEntryFormat.Gnu)
                {
                    GnuTarEntry gnuEntry = originalEntry as GnuTarEntry;

                    DateTimeOffset expectedATime = gnuEntry.AccessTime;
                    DateTimeOffset expectedCTime = gnuEntry.ChangeTime;

                    DateTimeOffset actualAccessTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaATime);
                    DateTimeOffset actualChangeTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaCTime);

                    if (expectedATime == default)
                    {
                        AssertExtensions.GreaterThanOrEqualTo(actualAccessTime, paxEntry.ModificationTime);
                    }
                    else
                    {
                        expectedATime = expectedATime - _oneSecond;
                        AssertExtensions.GreaterThanOrEqualTo(expectedATime, actualAccessTime);
                    }

                    if (expectedCTime == default)
                    {
                        AssertExtensions.GreaterThanOrEqualTo(actualChangeTime, paxEntry.ModificationTime);
                    }
                    else
                    {
                        expectedCTime = expectedCTime - _oneSecond;
                        AssertExtensions.GreaterThanOrEqualTo(expectedCTime, actualChangeTime);
                    }
                }
                else if (originalEntry.Format is TarEntryFormat.Pax)
                {
                    PaxTarEntry originalPaxEntry = originalEntry as PaxTarEntry;

                    DateTimeOffset expectedATime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, PaxEaATime) - _oneSecond;
                    DateTimeOffset expectedCTime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, PaxEaCTime) - _oneSecond;

                    DateTimeOffset actualAccessTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaATime);
                    DateTimeOffset actualChangeTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaCTime);

                    AssertExtensions.GreaterThanOrEqualTo(actualAccessTime, expectedATime);
                    AssertExtensions.GreaterThanOrEqualTo(actualChangeTime, expectedCTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    DateTimeOffset actualAccessTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaATime);
                    DateTimeOffset actualChangeTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaCTime);

                    AssertExtensions.GreaterThanOrEqualTo(actualAccessTime, initialNow);
                    AssertExtensions.GreaterThanOrEqualTo(actualChangeTime, initialNow);
                }
            }

            if (formatToConvert is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = convertedEntry as GnuTarEntry;
                if (originalEntry.Format is TarEntryFormat.Pax or TarEntryFormat.Gnu)
                {
                    GetExpectedTimestampsFromOriginalPaxOrGnu(originalEntry, formatToConvert, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime);
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.AccessTime, expectedATime);
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.ChangeTime, expectedCTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    Assert.Equal(default, gnuEntry.AccessTime);
                    Assert.Equal(default, gnuEntry.ChangeTime);
                }
            }

            return convertedEntry;
        }

        private void GetExpectedTimestampsFromOriginalPaxOrGnu(TarEntry originalEntry, TarEntryFormat formatToConvert, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime)
        {
            Assert.True(originalEntry.Format is TarEntryFormat.Gnu or TarEntryFormat.Pax);
            if (originalEntry.Format is TarEntryFormat.Pax)
            {
                PaxTarEntry originalPaxEntry = originalEntry as PaxTarEntry;
                Assert.Contains("atime", originalPaxEntry.ExtendedAttributes); //  We are verifying that the original had an atime and ctime set
                Assert.Contains("ctime", originalPaxEntry.ExtendedAttributes); //  and that when converting to GNU we are _not_ preserving them
                // And that instead, we are setting them to MinValue
                expectedATime = formatToConvert is TarEntryFormat.Gnu ? default : GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "atime");
                expectedCTime = formatToConvert is TarEntryFormat.Gnu ? default : GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "ctime");
            }
            else
            {
                GnuTarEntry originalGnuEntry = originalEntry as GnuTarEntry;
                expectedATime = originalGnuEntry.AccessTime;
                expectedCTime = originalGnuEntry.ChangeTime;
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
    }
}
