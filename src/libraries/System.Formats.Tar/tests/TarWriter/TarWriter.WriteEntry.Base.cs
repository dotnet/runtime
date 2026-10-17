// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract class TarWriter_WriteEntry_Base : TarTestsBase
    {
        protected virtual TarEntryFormat TestFormat => TarEntryFormat.Pax;

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteEntry_Null_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TestFormat, leaveOpen: false);
            TarWriter writer = writerHolder;

            await Assert.ThrowsAsync<ArgumentNullException>(() => WriteEntry(writer, null, async));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteRegularFile(bool async) => WriteAndVerifyEntry(GetRegularFileEntryTypeForFormat(TestFormat), async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteHardLink(bool async) => WriteAndVerifyEntry(TarEntryType.HardLink, async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteSymbolicLink(bool async) => WriteAndVerifyEntry(TarEntryType.SymbolicLink, async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteDirectory(bool async) => WriteAndVerifyEntry(TarEntryType.Directory, async);

        [Theory]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        public async Task Write_LinkEntry_EmptyLinkName_Throws(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, leaveOpen: false);
            TarWriter writer = writerHolder;

            TarEntry entry = InvokeTarEntryCreationConstructor(TestFormat, entryType, "link");
            await Assert.ThrowsAsync<ArgumentException>("entry", () => WriteEntry(writer, entry, async));
        }

        // Writes an entry of the given type using this class' TestFormat, verifying its properties
        // both before writing and after reading it back from the archive.
        protected async Task WriteAndVerifyEntry(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TestFormat, leaveOpen: true))
            {
                TarWriter writer = writerHolder;

                TarEntry entryToWrite = InvokeTarEntryCreationConstructor(TestFormat, entryType, InitialEntryName);
                SetEntryProperties(entryToWrite, entryType);
                VerifyEntryProperties(entryToWrite, entryType, isWritable: true);
                await WriteEntry(writer, entryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            TarEntry entry = await GetNextEntry(reader, async: async);
            VerifyEntryProperties(entry, entryType, isWritable: false);
        }

        // Dispatches to the Set* overload matching both the entry's concrete format type and its TarEntryType.
        private void SetEntryProperties(TarEntry entry, TarEntryType entryType)
        {
            switch (entry)
            {
                case V7TarEntry v7:
                    switch (entryType)
                    {
                        case TarEntryType.V7RegularFile: SetRegularFile(v7); break;
                        case TarEntryType.Directory: SetDirectory(v7); break;
                        case TarEntryType.HardLink: SetHardLink(v7); break;
                        case TarEntryType.SymbolicLink: SetSymbolicLink(v7); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for V7: {entryType}");
                    }
                    break;
                case GnuTarEntry gnu:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: SetRegularFile(gnu); break;
                        case TarEntryType.Directory: SetDirectory(gnu); break;
                        case TarEntryType.HardLink: SetHardLink(gnu); break;
                        case TarEntryType.SymbolicLink: SetSymbolicLink(gnu); break;
                        case TarEntryType.CharacterDevice: SetCharacterDevice(gnu); break;
                        case TarEntryType.BlockDevice: SetBlockDevice(gnu); break;
                        case TarEntryType.Fifo: SetFifo(gnu); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Gnu: {entryType}");
                    }
                    break;
                case PaxTarEntry pax:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: SetRegularFile(pax); break;
                        case TarEntryType.Directory: SetDirectory(pax); break;
                        case TarEntryType.HardLink: SetHardLink(pax); break;
                        case TarEntryType.SymbolicLink: SetSymbolicLink(pax); break;
                        case TarEntryType.CharacterDevice: SetCharacterDevice(pax); break;
                        case TarEntryType.BlockDevice: SetBlockDevice(pax); break;
                        case TarEntryType.Fifo: SetFifo(pax); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Pax: {entryType}");
                    }
                    break;
                case UstarTarEntry ustar:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: SetRegularFile(ustar); break;
                        case TarEntryType.Directory: SetDirectory(ustar); break;
                        case TarEntryType.HardLink: SetHardLink(ustar); break;
                        case TarEntryType.SymbolicLink: SetSymbolicLink(ustar); break;
                        case TarEntryType.CharacterDevice: SetCharacterDevice(ustar); break;
                        case TarEntryType.BlockDevice: SetBlockDevice(ustar); break;
                        case TarEntryType.Fifo: SetFifo(ustar); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Ustar: {entryType}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected entry format: {entry.GetType()}");
            }
        }

        // Dispatches to the Verify* overload matching both the entry's concrete format type and its TarEntryType.
        private void VerifyEntryProperties(TarEntry entry, TarEntryType entryType, bool isWritable)
        {
            switch (entry)
            {
                case V7TarEntry v7:
                    switch (entryType)
                    {
                        case TarEntryType.V7RegularFile: VerifyRegularFile(v7, isWritable); break;
                        case TarEntryType.Directory: VerifyDirectory(v7); break;
                        case TarEntryType.HardLink: VerifyHardLink(v7); break;
                        case TarEntryType.SymbolicLink: VerifySymbolicLink(v7); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for V7: {entryType}");
                    }
                    break;
                case GnuTarEntry gnu:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: VerifyRegularFile(gnu, isWritable); break;
                        case TarEntryType.Directory: VerifyDirectory(gnu); break;
                        case TarEntryType.HardLink: VerifyHardLink(gnu); break;
                        case TarEntryType.SymbolicLink: VerifySymbolicLink(gnu); break;
                        case TarEntryType.CharacterDevice: VerifyCharacterDevice(gnu); break;
                        case TarEntryType.BlockDevice: VerifyBlockDevice(gnu); break;
                        case TarEntryType.Fifo: VerifyFifo(gnu); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Gnu: {entryType}");
                    }
                    break;
                case PaxTarEntry pax:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: VerifyRegularFile(pax, isWritable); break;
                        case TarEntryType.Directory: VerifyDirectory(pax); break;
                        case TarEntryType.HardLink: VerifyHardLink(pax); break;
                        case TarEntryType.SymbolicLink: VerifySymbolicLink(pax); break;
                        case TarEntryType.CharacterDevice: VerifyCharacterDevice(pax); break;
                        case TarEntryType.BlockDevice: VerifyBlockDevice(pax); break;
                        case TarEntryType.Fifo: VerifyFifo(pax); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Pax: {entryType}");
                    }
                    break;
                case UstarTarEntry ustar:
                    switch (entryType)
                    {
                        case TarEntryType.RegularFile: VerifyRegularFile(ustar, isWritable); break;
                        case TarEntryType.Directory: VerifyDirectory(ustar); break;
                        case TarEntryType.HardLink: VerifyHardLink(ustar); break;
                        case TarEntryType.SymbolicLink: VerifySymbolicLink(ustar); break;
                        case TarEntryType.CharacterDevice: VerifyCharacterDevice(ustar); break;
                        case TarEntryType.BlockDevice: VerifyBlockDevice(ustar); break;
                        case TarEntryType.Fifo: VerifyFifo(ustar); break;
                        default: throw new InvalidOperationException($"Unexpected entry type for Ustar: {entryType}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected entry format: {entry.GetType()}");
            }
        }

        protected void VerifyDirectory(TarEntry entry, TarEntryFormat format, string name)
        {
            Assert.NotNull(entry);
            Assert.Equal(format, entry.Format);
            Assert.Equal(TarEntryType.Directory, entry.EntryType);
            Assert.Equal(name, entry.Name);
        }

        protected void VerifyGlobalExtendedAttributesEntry(TarEntry entry, Dictionary<string, string> attrs)
        {
            PaxGlobalExtendedAttributesTarEntry gea = entry as PaxGlobalExtendedAttributesTarEntry;
            Assert.NotNull(gea);
            Assert.Equal(attrs.Count, gea.GlobalExtendedAttributes.Count);

            foreach ((string key, string value) in attrs)
            {
                Assert.Contains(key, gea.GlobalExtendedAttributes);
                Assert.Equal(value, gea.GlobalExtendedAttributes[key]);
            }
        }

        public static IEnumerable<object[]> WriteIntField_TheoryData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                // Min value.
                yield return new object[] { format, 0 };

                yield return new object[] { format, 1 };
                yield return new object[] { format, 42 };

                // Max value octal.
                yield return new object[] { format, 0x1FFFFF };

                // These values do not fit the octal representation.
                bool formatIsOctalOnly = format is not TarEntryFormat.Pax and not TarEntryFormat.Gnu;
                if (!formatIsOctalOnly)
                {
                    // Max value property.
                    yield return new object[] { format, int.MaxValue };
                }

            }
        }

        public static IEnumerable<object[]> WriteTimeStampsWithFormats_TheoryData()
        {
            foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Gnu, TarEntryFormat.Pax })
            {
                foreach (DateTimeOffset timestamp in GetWriteTimeStamps(entryFormat))
                {
                    yield return new object[] { entryFormat, timestamp };
                }
            }
        }

        public static IEnumerable<object[]> WriteTimeStamp_Pax_TheoryData()
        {
            foreach (DateTimeOffset timestamp in GetWriteTimeStamps(TarEntryFormat.Pax))
            {
                yield return new object[] { timestamp };
            }
        }

        private static IEnumerable<DateTimeOffset> GetWriteTimeStamps(TarEntryFormat format)
        {
            // One second past Y2K38
            yield return new DateTimeOffset(2038, 1, 19, 3, 14, 8, TimeSpan.Zero);

            // Min value octal
            yield return DateTimeOffset.UnixEpoch;

            // Max value 12-byte octal field.
            yield return DateTimeOffset.UnixEpoch + new TimeSpan(0x1FFFFFFFF * TimeSpan.TicksPerSecond);

            // These values do not fit the octal representation.
            bool formatIsOctalOnly = format is not TarEntryFormat.Pax and not TarEntryFormat.Gnu;
            if (!formatIsOctalOnly)
            {
                // Min value property.
                yield return default; // This is not representable with the octal format.

                // One second past what a 12-byte field can store with octal representation
                yield return DateTimeOffset.UnixEpoch + new TimeSpan((0x1FFFFFFFF + 1) * TimeSpan.TicksPerSecond);

                // Max value property. Everything below seconds is set to zero for test equality comparison.
                yield return new DateTimeOffset(new DateTime(DateTime.MaxValue.Year,
                                                            DateTime.MaxValue.Month,
                                                            DateTime.MaxValue.Day,
                                                            DateTime.MaxValue.Hour,
                                                            DateTime.MaxValue.Minute,
                                                            DateTime.MaxValue.Second,
                                                            DateTime.MaxValue.Kind), TimeSpan.Zero);
            }
        }
    }
}
