﻿// Licensed to the .NET Foundation under one or more agreements.
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
                CompareDateTimeOffsets(firstEntry.ModificationTime, GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "atime"));
                CompareDateTimeOffsets(firstEntry.ModificationTime, GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "ctime"));
            }
            else if (format is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = firstEntry as GnuTarEntry;
                CompareDateTimeOffsets(firstEntry.ModificationTime, gnuEntry.AccessTime);
                CompareDateTimeOffsets(firstEntry.ModificationTime, gnuEntry.ChangeTime);
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
                DateTimeOffset actualAccessTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "atime");
                DateTimeOffset actualChangeTime = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, "atime");
                if (originalEntry.Format is TarEntryFormat.Pax or TarEntryFormat.Gnu)
                {
                    GetExpectedTimestampsFromOriginalPaxOrGnu(originalEntry, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime);
                    CompareDateTimeOffsets(expectedATime, actualAccessTime);
                    CompareDateTimeOffsets(expectedCTime, actualChangeTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    CompareDateTimeOffsets(initialNow, actualAccessTime);
                    CompareDateTimeOffsets(initialNow, actualChangeTime);
                }
            }

            if (formatToConvert is TarEntryFormat.Gnu)
            {
                GnuTarEntry gnuEntry = convertedEntry as GnuTarEntry;
                if (originalEntry.Format is TarEntryFormat.Pax or TarEntryFormat.Gnu)
                {
                    GetExpectedTimestampsFromOriginalPaxOrGnu(originalEntry, out DateTimeOffset expectedATime, out DateTimeOffset expectedCTime);
                    CompareDateTimeOffsets(expectedATime, gnuEntry.AccessTime);
                    CompareDateTimeOffsets(expectedCTime, gnuEntry.ChangeTime);
                }
                else if (originalEntry.Format is TarEntryFormat.Ustar or TarEntryFormat.V7)
                {
                    CompareDateTimeOffsets(initialNow, gnuEntry.AccessTime);
                    CompareDateTimeOffsets(initialNow, gnuEntry.ChangeTime);
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

        protected TarEntry InvokeTarEntryCreationConstructor(TarEntryFormat targetFormat, TarEntryType entryType, string entryName)
            => targetFormat switch
            {
                TarEntryFormat.V7 => new V7TarEntry(entryType, entryName),
                TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new FormatException($"Unexpected format: {targetFormat}")
            };

        protected TarEntry InvokeTarEntryConversionConstructor(TarEntryFormat targetFormat, TarEntry other)
            => targetFormat switch
            {
                TarEntryFormat.V7 => new V7TarEntry(other),
                TarEntryFormat.Ustar => new UstarTarEntry(other),
                TarEntryFormat.Pax => new PaxTarEntry(other),
                TarEntryFormat.Gnu => new GnuTarEntry(other),
                _ => throw new FormatException($"Unexpected format: {targetFormat}")
            };

        protected TarEntryType GetTarEntryTypeForTarEntryFormat(TarEntryType entryType, TarEntryFormat format)
        {
            if (format is TarEntryFormat.V7)
            {
                if (entryType is TarEntryType.RegularFile)
                {
                    return TarEntryType.V7RegularFile;
                }
            }
            else
            {
                if (entryType is TarEntryType.V7RegularFile)
                {
                    return TarEntryType.RegularFile;
                }
            }
            return entryType;
        }

        protected void CheckConversionType(TarEntry entry, TarEntryFormat expectedFormat)
        {
            Type expectedType = expectedFormat switch
            {
                TarEntryFormat.V7 => typeof(V7TarEntry),
                TarEntryFormat.Ustar => typeof(UstarTarEntry),
                TarEntryFormat.Pax => typeof(PaxTarEntry),
                TarEntryFormat.Gnu => typeof(GnuTarEntry),
                _ => throw new FormatException($"Unexpected format {expectedFormat}")
            };

            Assert.Equal(expectedType, entry.GetType());
        }
    }
}
