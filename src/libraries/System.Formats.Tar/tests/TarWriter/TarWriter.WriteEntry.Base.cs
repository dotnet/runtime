// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_WriteEntry_Base : TarTestsBase
    {
        protected void WriteEntry_Null_Throws_Internal(TarEntryFormat format)
        {
            using MemoryStream archiveStream = new MemoryStream();
            using TarWriter writer = new TarWriter(archiveStream, format, leaveOpen: false);
            Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null));
        }

        protected async Task WriteEntry_Null_Throws_Async_Internal(TarEntryFormat format)
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, format, leaveOpen: false))
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null));
                }
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
                yield return DateTimeOffset.MinValue; // This is not representable with the octal format.

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
