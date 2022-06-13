// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            TarEntryFormat firstAndLastFormat,
            TarEntryFormat formatToConvert)
        {
            using MemoryStream dataStream = new MemoryStream();

            TarEntryType firstAndLastEntryType = GetTarEntryTypeForTarEntryFormat(originalEntryType, firstAndLastFormat);
            TarEntry firstEntry = InvokeTarEntryCreationConstructor(firstAndLastFormat, firstAndLastEntryType, "file.txt");

            firstEntry.Gid = TestGid;
            firstEntry.Uid = TestUid;
            firstEntry.Mode = TestMode;
            firstEntry.ModificationTime = TestModificationTime;

            if (firstAndLastEntryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
            {
                firstEntry.DataStream = dataStream;
                Assert.Same(dataStream, firstEntry.DataStream);
            }

            if (firstAndLastEntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                firstEntry.LinkName = TestLinkName;
                Assert.Equal(TestLinkName, firstEntry.LinkName);
            }

            if (firstAndLastEntryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice)
            {
                PosixTarEntry posixTarEntry = firstEntry as PosixTarEntry;
                posixTarEntry.DeviceMajor = TestBlockDeviceMajor;
                posixTarEntry.DeviceMinor = TestBlockDeviceMinor;
            }

            TarEntry otherEntry = InvokeTarEntryConversionConstructor(formatToConvert, firstEntry);

            CheckConversionType(otherEntry, formatToConvert);
            Assert.Equal(formatToConvert, otherEntry.Format);

            TarEntryType otherEntryType = GetTarEntryTypeForTarEntryFormat(originalEntryType, formatToConvert);
            Assert.Equal(otherEntryType, otherEntry.EntryType);

            Assert.Equal(firstEntry.Gid, otherEntry.Gid);
            Assert.Equal(firstEntry.Uid, otherEntry.Uid);
            Assert.Equal(firstEntry.Mode, otherEntry.Mode);
            Assert.Equal(firstEntry.ModificationTime, otherEntry.ModificationTime);

            if (firstAndLastEntryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
            {
                Assert.Same(dataStream, otherEntry.DataStream);
            }

            if (firstAndLastEntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                Assert.Equal(TestLinkName, otherEntry.LinkName);
            }

            if (firstAndLastEntryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice)
            {
                PosixTarEntry posixTarEntry = firstEntry as PosixTarEntry;
                Assert.Equal(TestBlockDeviceMajor, posixTarEntry.DeviceMajor);
                Assert.Equal(TestBlockDeviceMinor, posixTarEntry.DeviceMinor);
            }

            TarEntry secondEntry = InvokeTarEntryConversionConstructor(firstAndLastFormat, otherEntry);
            Assert.Equal(firstAndLastEntryType, secondEntry.EntryType);

            Assert.Equal(firstEntry.Gid, secondEntry.Gid);
            Assert.Equal(firstEntry.Uid, secondEntry.Uid);
            Assert.Equal(firstEntry.Mode, otherEntry.Mode);
            Assert.Equal(firstEntry.ModificationTime, otherEntry.ModificationTime);

            if (firstAndLastEntryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
            {
                Assert.Same(dataStream, secondEntry.DataStream);
            }

            if (firstAndLastEntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                Assert.Equal(TestLinkName, secondEntry.LinkName);
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
