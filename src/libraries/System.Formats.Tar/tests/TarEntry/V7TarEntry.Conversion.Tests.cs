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
        public void Constructor_ConversionFromUstar_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Ustar, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromUstar_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Ustar, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromUstar_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Ustar, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromUstar_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Ustar, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromPax_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Pax, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromPax_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Pax, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromPax_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Pax, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromPax_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Pax, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromGnu_RegularFile() => TestConstructionConversion(TarEntryType.RegularFile, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromGnu_Directory() => TestConstructionConversion(TarEntryType.Directory, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromGnu_SymbolicLink() => TestConstructionConversion(TarEntryType.SymbolicLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionFromGnu_HardLink() => TestConstructionConversion(TarEntryType.HardLink, TarEntryFormat.Gnu, TarEntryFormat.V7);

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
            V7TarEntry v7Entry = new V7TarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
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
            V7TarEntry v7Entry = new V7TarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
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

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_ConversionFromGnu_From_UnseekableTarReader(TarEntryFormat writerFormat)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.gnu, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            GnuTarEntry gnuEntry = sourceReader.GetNextEntry(copyData: false) as GnuTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: gnuEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, writerFormat, leaveOpen: true))
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
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_RegularFile() =>
            TestConstructionConversionBackAndForth(TarEntryType.RegularFile, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_Directory() =>
            TestConstructionConversionBackAndForth(TarEntryType.Directory, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_SymbolicLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.SymbolicLink, TarEntryFormat.V7, TarEntryFormat.Gnu);

        [Fact]
        public void Constructor_ConversionV7_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.V7);

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Ustar);

        [Fact]
        public void Constructor_ConversionPax_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Pax);

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth_HardLink() =>
            TestConstructionConversionBackAndForth(TarEntryType.HardLink, TarEntryFormat.V7, TarEntryFormat.Gnu);
    }
}
