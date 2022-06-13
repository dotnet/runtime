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
    public class V7TarEntry_Conversion_Tests : TarTestsConversionBase
    {
        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Ustar()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Pax()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Gnu()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedUstar = new V7TarEntry(other: ustar);

            Assert.Equal(TarEntryType.V7RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }

        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedPax = new V7TarEntry(other: pax);

            Assert.Equal(TarEntryType.V7RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }

        [Fact]
        public void Constructor_ConversionFromGnu()
        {
            GnuTarEntry gnu = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedGnu = new V7TarEntry(other: gnu);

            Assert.Equal(TarEntryType.V7RegularFile, convertedGnu.EntryType);
            Assert.Equal(InitialEntryName, convertedGnu.Name);
        }

        [Fact]
        public void Constructor_ConversionFromUstar_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            UstarTarEntry ustarEntry = sourceReader.GetNextEntry(copyData: false) as UstarTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
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
            V7TarEntry v7Entry = new V7TarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionFromGnu_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.gnu, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            GnuTarEntry gnuEntry = sourceReader.GetNextEntry(copyData: false) as GnuTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: gnuEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_RegularFile() =>
            TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Directory() =>
            TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_SymbolicLink() =>
            TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_HardLink() =>
            TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Gnu);
    }
}
