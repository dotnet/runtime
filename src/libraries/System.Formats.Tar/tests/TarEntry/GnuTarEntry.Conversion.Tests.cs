// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class GnuTarEntry_Conversion_Tests : TarTestsConversionBase
    {
        [Fact]
        public void Constructor_ConversionFromV7_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromV7_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromV7_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromV7_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_BlockDevice() => TestConstructionConversion(TarEntryType.BlockDevice, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromUstar_CharacterDevice() => TestConstructionConversion(TarEntryType.CharacterDevice, TarEntryFormat.Ustar, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_BlockDevice() => TestConstructionConversion(TarEntryType.BlockDevice, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionFromPax_CharacterDevice() => TestConstructionConversion(TarEntryType.CharacterDevice, TarEntryFormat.Pax, TarEntryFormat.Gnu);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_ConversionFromV7_From_UnseekableTarReader(TarEntryFormat writerFormat)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.v7, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            V7TarEntry v7Entry = sourceReader.GetNextEntry(copyData: false) as V7TarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: v7Entry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_ConversionFromUstar_From_UnseekableTarReader(TarEntryFormat writerFormat)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            UstarTarEntry ustarEntry = sourceReader.GetNextEntry(copyData: false) as UstarTarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_ConversionFromPax_From_UnseekableTarReader(TarEntryFormat writerFormat)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            PaxTarEntry paxEntry = sourceReader.GetNextEntry(copyData: false) as PaxTarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        // BlockDevice, CharacterDevice and Fifo are not supported by V7

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_BlockDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_BlockDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_BlockDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_CharacterDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_CharacterDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_CharacterDevice() =>
            TestConstructionConversionBackAndForth(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Fifo() =>
            TestConstructionConversionBackAndForth(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Fifo() =>
            TestConstructionConversionBackAndForth(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Fifo() =>
            TestConstructionConversionBackAndForth(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Gnu);
    }
}
