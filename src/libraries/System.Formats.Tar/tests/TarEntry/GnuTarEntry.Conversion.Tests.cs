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
        public void Constructor_ConversionFromV7()
        {
            V7TarEntry v7 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            GnuTarEntry convertedV7 = new GnuTarEntry(other: v7);

            Assert.Equal(TarEntryType.RegularFile, convertedV7.EntryType);
            Assert.Equal(InitialEntryName, convertedV7.Name);
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            GnuTarEntry convertedUstar = new GnuTarEntry(other: ustar);

            Assert.Equal(TarEntryType.RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }

        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            GnuTarEntry convertedPax = new GnuTarEntry(other: pax);

            Assert.Equal(TarEntryType.RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }

        [Fact]
        public void Constructor_ConversionFromV7_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.v7, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            V7TarEntry v7Entry = sourceReader.GetNextEntry(copyData: false) as V7TarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: v7Entry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
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
        public void Constructor_ConversionFromUstar_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            UstarTarEntry ustarEntry = sourceReader.GetNextEntry(copyData: false) as UstarTarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
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
        public void Constructor_ConversionFromPax_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            PaxTarEntry paxEntry = sourceReader.GetNextEntry(copyData: false) as PaxTarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
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
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        // BlockDevice, CharacterDevice and Fifo are not supported by V7

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_BlockDevice() =>
            TestConstructionConversion(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_BlockDevice() =>
            TestConstructionConversion(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_BlockDevice() =>
            TestConstructionConversion(TarEntryType.BlockDevice, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_CharacterDevice() =>
            TestConstructionConversion(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_CharacterDevice() =>
            TestConstructionConversion(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_CharacterDevice() =>
            TestConstructionConversion(TarEntryType.CharacterDevice, TarEntryFormat.Gnu, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Fifo() =>
            TestConstructionConversion(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Fifo() =>
            TestConstructionConversion(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Fifo() =>
            TestConstructionConversion(TarEntryType.Fifo, TarEntryFormat.Gnu, TarEntryFormat.Gnu);
    }
}
