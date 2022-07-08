// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime.ICorDebug;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarTestsConversionBase : TarTestsBase
    {
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
                Assert.Equal(firstEntry.ModificationTime, gnuEntry.AccessTime);
                Assert.Equal(firstEntry.ModificationTime, gnuEntry.ChangeTime);
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
                DateTimeOffset actualAccessTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaATime);
                DateTimeOffset actualChangeTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, PaxEaCTime);
                if (originalEntry.Format is TarEntryFormat.Pax or TarEntryFormat.Gnu)
                {
                    GetExpectedTimestampsFromOriginalPaxOrGnu(originalEntry, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime);
                    Assert.Equal(expectedATime, actualAccessTime);
                    Assert.Equal(expectedCTime, actualChangeTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    AssertExtensions.GreaterThanOrEqualTo(actualAccessTime, initialNow);
                    AssertExtensions.GreaterThanOrEqualTo(actualChangeTime, initialNow);
                }
            }

            if (formatToConvert is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = convertedEntry as GnuTarEntry;
                if (originalEntry.Format is TarEntryFormat.Pax or TarEntryFormat.Gnu)
                {
                    GetExpectedTimestampsFromOriginalPaxOrGnu(originalEntry, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime);
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.AccessTime, expectedATime);
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.ChangeTime, expectedCTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.AccessTime, initialNow);
                    AssertExtensions.GreaterThanOrEqualTo(gnuEntry.ChangeTime, initialNow);
                }
            }

            return convertedEntry;
        }

        private void GetExpectedTimestampsFromOriginalPaxOrGnu(TarEntry originalEntry, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime)
        {
            Assert.True(originalEntry.Format is TarEntryFormat.Gnu or TarEntryFormat.Pax);
            if (originalEntry.Format is TarEntryFormat.Pax)
            {
                PaxTarEntry originalPaxEntry = originalEntry as PaxTarEntry;
                Assert.Contains("atime", originalPaxEntry.ExtendedAttributes);
                Assert.Contains("ctime", originalPaxEntry.ExtendedAttributes);
                expectedATime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "atime");
                expectedCTime = GetDateTimeOffsetFromTimestampString(originalPaxEntry.ExtendedAttributes, "ctime");
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
                _ => throw new FormatException($"Unexpected format: {targetFormat}")
            };
    }
}
