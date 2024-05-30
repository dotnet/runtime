// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Packaging.Tests
{
    public class PartPieceTests : FileCleanupTestBase
    {
        private delegate byte[] FileContentsGenerator(PartConstructionParameters pcp, int totalLength);
        private record class PartConstructionParameters (string FullPath, bool CreateAsAtomic, bool CreateAsValidPieceSequence, bool UppercaseFileName, bool ShufflePieces, int[] PieceLengths, FileContentsGenerator PieceGenerator)
        { }

        private string m_contentTypesXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
    <Default Extension=""bin"" ContentType=""application/octet-stream"" />
    <Default Extension=""xml"" ContentType=""application/xml"" />
</Types>";
        private readonly byte[] _partPieceSampleZipPackage;
        private readonly byte[] _atomicPartContentTypesSampleZipPackage;
        private readonly byte[] _partPieceContentTypesSampleZipPackage;
        private readonly byte[] _uppercasePartPieceContentTypesSampleZipPackage;

        public PartPieceTests()
            : base()
        {
            _partPieceSampleZipPackage = CreatePackage(
                new PartConstructionParameters("AtomicPartEntry.bin", true, false, false, false, [200], GenerateRandomBytes),
                new PartConstructionParameters("FixedDocumentSequence.bin", false, true, false, false, [100, 10, 88, 2], GenerateRandomBytes),
                new PartConstructionParameters("child/childEntry.bin", false, true, false, false, [1024, 10, 1024, 2], GenerateRandomBytes),
                new PartConstructionParameters("FixedDocumentSequenceOnePiece.bin", false, true, false, false, [100], GenerateRandomBytes),
                new PartConstructionParameters("UppercaseFixedDocumentSequenceOnePiece.bin", false, true, true, false, [100, 10, 88, 2], GenerateRandomBytes),
                new PartConstructionParameters("child/UppercaseChildEntry.bin", false, true, true, false, [1024, 10, 1024, 2], GenerateRandomBytes),
                new PartConstructionParameters(null, false, true, false, false, [100], GenerateRandomBytes),
                new PartConstructionParameters(null, false, true, false, false, [100, 100], GenerateRandomBytes),
                new PartConstructionParameters("LongFixedDocumentSequence.bin", false, true, false, false, [100, 10, 88, 2, 5, 6, 7, 8, 9, 10, 11, 12], GenerateRandomBytes),
                new PartConstructionParameters("OutOfOrder.bin", false, true, false, true, [100, 10, 88, 2], GenerateRandomBytes),
                new PartConstructionParameters("child/OutOfOrder.bin", false, true, false, true, [100, 10, 88, 2, 150], GenerateRandomBytes),
                new PartConstructionParameters("[Content_Types].xml", false, true, false, false, [0, 10, 20], GenerateContentTypesFile),
                new PartConstructionParameters("invalidPieces/missingLast.bin/[0].piece", true, false, false, false, [20], GenerateRandomBytes),
                new PartConstructionParameters("invalidPieces/missingLast.bin/[1].piece", true, false, false, false, [20], GenerateRandomBytes),
                new PartConstructionParameters("invalidPieces/missingFirst.bin/[1].last.piece", true, false, false, false, [20], GenerateRandomBytes),
                new PartConstructionParameters("invalidPieces/tenthPart.bin/[10].last.piece", true, false, false, false, [20], GenerateRandomBytes),
                new PartConstructionParameters("SkippingPartSequence.bin", false, false, false, false, [100, 10, 88, 2, 150], GenerateRandomBytes),
                new PartConstructionParameters("ReadableAtomicPartEntry.bin", true, false, false, false, [1024], GenerateSequentialBytes),
                new PartConstructionParameters("ReadablePartPieceEntry.bin", false, true, false, false, [16, 16, 16, 16], GenerateSequentialBytes)
            );

            _atomicPartContentTypesSampleZipPackage = CreatePackage(
                new PartConstructionParameters("AtomicPartEntry.bin", true, false, false, false, [200], GenerateRandomBytes),
                new PartConstructionParameters("[Content_Types].xml", true, false, false, false, [0], GenerateContentTypesFile)
            );

            _partPieceContentTypesSampleZipPackage = CreatePackage(
                new PartConstructionParameters("AtomicPartEntry.bin", true, false, false, false, [200], GenerateRandomBytes),
                new PartConstructionParameters("[Content_Types].xml", false, true, false, false, [0, 10, 20], GenerateContentTypesFile)
            );

            _uppercasePartPieceContentTypesSampleZipPackage = CreatePackage(
                new PartConstructionParameters("AtomicPartEntry.bin", true, false, false, false, [200], GenerateRandomBytes),
                new PartConstructionParameters("[Content_Types].xml", false, true, true, false, [0, 10, 20], GenerateContentTypesFile)
            );
        }

        private byte[] GenerateRandomBytes(PartConstructionParameters pcp, int totalLength)
        {
            var bytes = new byte[totalLength];

            Random.Shared.NextBytes(bytes);
            return bytes;
        }

        private byte[] GenerateSequentialBytes(PartConstructionParameters pcp, int totalLength)
        {
            var bytes = new byte[totalLength];

            for(int i = 0; i < totalLength; i++)
            {
                bytes[i] = (byte)(i % 255);
            }
            return bytes;
        }

        private byte[] GenerateContentTypesFile(PartConstructionParameters pcp, int totalLength)
        {
            return Encoding.UTF8.GetBytes(m_contentTypesXml);
        }

        // Some of these tests rely on a correct ZIP file but a corrupt OPC Package. This means that we need to build the ZIP file manually
        private byte[] CreatePackage(params PartConstructionParameters[] constructionParameters)
        {
            using var ms = new MemoryStream();
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);

            foreach (var part in constructionParameters)
            {
                var fileName = part.UppercaseFileName ? part.FullPath.ToUpper() : part.FullPath;
                byte[] fileContents = part.PieceGenerator(part, part.PieceLengths.Sum());

                // Simple case first - just write a set of random bytes
                if (part.CreateAsAtomic)
                {
                    var entry = zipArchive.CreateEntry(fileName);
                    using var entryStream = entry.Open();

                    entryStream.Write(fileContents);

                    entryStream.Flush();
                }
                else
                {
                    var partPieces = Enumerable.Range(0, part.PieceLengths.Length).ToArray();
                    int currOffset = 0;

                    // If ShufflePieces is set, create the part pieces out of order
                    if (part.ShufflePieces)
                    {
                        Random.Shared.Shuffle(partPieces);
                    }

                    foreach (var i in partPieces)
                    {
                        // If CreateAsValidPieceSequence is set, make sure the last piece in the part skips an index
                        var isLastPiece = i == part.PieceLengths.Length - 1;
                        var extension = i == part.PieceLengths.Length - 1 ? ".last.piece" : ".piece";
                        var partPieceIndex = !part.CreateAsValidPieceSequence && i == part.PieceLengths.Length - 1 ? i + 1 : i;
                        var fileNameSeparator = string.IsNullOrEmpty(fileName) ? string.Empty : "/";
                        var piecePartPath = $"{fileName}{fileNameSeparator}[{partPieceIndex}]{extension}";

                        if (part.UppercaseFileName)
                        {
                            piecePartPath = piecePartPath.ToUpper();
                        }

                        var entry = zipArchive.CreateEntry(piecePartPath);
                        using var entryStream = entry.Open();

                        // If we're in the last piece, just write whatever's left of the byte buffer
                        entryStream.Write(fileContents, currOffset, isLastPiece ? fileContents.Length - currOffset : part.PieceLengths[i]);
                        currOffset += part.PieceLengths[i];

                        entryStream.Flush();
                    }
                }
            }
            zipArchive.Dispose();
            ms.Flush();

            return ms.ToArray();
        }

        // The piece names below are valid part pieces. They represent every combination of
        // [root / child directory, last / non-last piece, lowercase / uppercase .piece extension]
        [Theory]
        // * First piece of a part in the root directory
        [InlineData("FixedDocumentSequence.bin/[0].piece")]
        // * First piece of a part in a subdirectory
        [InlineData("child/childEntry.bin/[0].piece")]
        // * Last piece of a part in the root directory
        [InlineData("FixedDocumentSequence.bin/[3].last.piece")]
        // * Last piece of a part in a subdirectory
        [InlineData("child/childEntry.bin/[3].last.piece")]
        // * Part of a piece with an uppercase file extension in the root directory
        [InlineData("UPPERCASEFIXEDDOCUMENTSEQUENCEONEPIECE.BIN/[2].PIECE")]
        // * Part of a piece with an uppercase file extension in a subdirectory
        [InlineData("CHILD/UPPERCASECHILDENTRY.BIN/[2].PIECE")]
        // * Last piece of a part with an uppercase file extension in the root directory
        [InlineData("UPPERCASEFIXEDDOCUMENTSEQUENCEONEPIECE.BIN/[3].LAST.PIECE")]
        // * Last piece of a part with an uppercase file extension in a subdirectory
        [InlineData("CHILD/UPPERCASECHILDENTRY.BIN/[3].LAST.PIECE")]
        public void ValidPartPiecesAreParsable(string partPieceName)
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            var partPieceEntry = zipArchive.GetEntry(partPieceName)!;

            Assert.True(ZipPackagePartPiece.TryParse(partPieceEntry, out var processedPart));
        }

        // Piece names below might look like a valid part piece at first, but are invalid.
        // This is essentially a part piece with an empty name (and a corresponding .last.piece.)
        // I also include a bona fide atomic part name, to make sure we don't interpret
        // those as part pieces.
        // NB: Technically, a part piece with a double-digit number breaches the OPC spec,
        // but this functionality is in place because some software (the XPS Document Writer, etc.)
        // generates packages with part numbers in this range.
        [Theory]
        [InlineData("[0].piece")]
        [InlineData("[0].last.piece")]
        [InlineData("AtomicPartEntry.bin")]
        public void InvalidPartPiecesAreNotParsable(string partPieceName)
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            var partPieceEntry = zipArchive.GetEntry(partPieceName)!;

            Assert.False(ZipPackagePartPiece.TryParse(partPieceEntry, out var processedPart));
        }

        [Theory]
        [InlineData("/AtomicPartEntry.bin")]
        [InlineData("/child/childEntry.bin")]
        [InlineData("/child/OutOfOrder.bin")]
        [InlineData("/CHILD/UPPERCASECHILDENTRY.BIN")]
        [InlineData("/FixedDocumentSequence.bin")]
        [InlineData("/FixedDocumentSequenceOnePiece.bin")]
        [InlineData("/OutOfOrder.bin")]
        [InlineData("/ReadableAtomicPartEntry.bin")]
        [InlineData("/ReadablePartPieceEntry.bin")]
        [InlineData("/UPPERCASEFIXEDDOCUMENTSEQUENCEONEPIECE.BIN")]
        public void ValidPartsAppearInSamplePackage(string partName)
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms);

            Assert.NotNull(zipPackage.GetPart(new Uri(partName, UriKind.Relative)));
        }

        // Three reasons why an interleaved part may be invalid are:
        // 1. There's a missing .last.piece entry
        // 2. There's a .last.piece entry without any previous .piece entries
        // 3. One or more of the .piece entries from the middle of the sequence are missing
        [Theory]
        [InlineData("/invalidPieces/missingLast.bin")]
        [InlineData("/invalidPieces/missingFirst.bin")]
        [InlineData("/SkippingPartSequence.bin")]
        public void InvalidPartsDoNotAppearInSamplePackage(string partName)
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms);

            Assert.Throws<InvalidOperationException>(() => zipPackage.GetPart(new Uri(partName, UriKind.Relative)));
        }

        // Verify that we can parse both atomic and interleaved [Content_Types].xml
        // files (which have separate part piece handling in ZipPackage.)
        [Fact]
        public void CanParseAtomicContentTypesFile()
        {
            using var ms = new MemoryStream(_atomicPartContentTypesSampleZipPackage);
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms);

            Assert.NotEmpty(zipPackage.GetParts());
        }

        [Fact]
        public void CanParseInterleavedContentTypesFile()
        {
            using var ms = new MemoryStream(_partPieceContentTypesSampleZipPackage);
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms);

            Assert.NotEmpty(zipPackage.GetParts());
        }

        // Verify that the IComparable<T> implementation on ZipPackagePartPiece works properly.
        // If it is, we should see the list reordered by piece number
        [Theory]
        [InlineData("OutOfOrder.bin/[2].piece,OutOfOrder.bin/[0].piece,OutOfOrder.bin/[1].piece,OutOfOrder.bin/[3].last.piece")]
        [InlineData("child/OutOfOrder.bin/[3].piece,child/OutOfOrder.bin/[2].piece,child/OutOfOrder.bin/[0].piece,child/OutOfOrder.bin/[1].piece,child/OutOfOrder.bin/[4].last.piece")]
        public void OutOfOrderPartPiecesAreParsable(string partPieceLists)
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            string[] archiveNames = partPieceLists.Split(',');
            var partPieces = new SortedSet<ZipPackagePartPiece>();

            foreach (var aN in archiveNames)
            {
                var zipEntry = zipArchive.GetEntry(aN);

                if (ZipPackagePartPiece.TryParse(zipEntry, out var partPiece))
                {
                    partPieces.Add(partPiece);
                }
            }

            foreach (var partPieceIndexMetadata in partPieces.Index())
            {
                Assert.Equal(partPieceIndexMetadata.Item.PieceNumber, partPieceIndexMetadata.Index);
            }
        }

        [Fact]
        public void UppercaseContentTypePartPieceSequenceIsFound()
        {
            using var ms = new MemoryStream(_uppercasePartPieceContentTypesSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms);

            Assert.NotEmpty(zipPackage.GetParts());
        }

        [Fact]
        public void CanCreateAtomicPart()
        {
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage, 0, _partPieceSampleZipPackage.Length);

            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            var newPart = zipPackage.CreatePart(new Uri("/NewAtomicPartEntry.bin", UriKind.Relative), "application/octet-stream");

            Assert.NotNull(newPart);
        }

        [Fact]
        public void CannotCreatePartWithDuplicateName()
        {
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage, 0, _partPieceSampleZipPackage.Length);

            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            Assert.Throws<InvalidOperationException>(() => zipPackage.CreatePart(new Uri("/AtomicPartEntry.bin", UriKind.Relative), "application/octet-stream"));
        }

        [Theory]
        [InlineData("/InvalidPartEntry.bin/[0].piece")]
        [InlineData("/InvalidPartEntry.bin/[10].piece")]
        public void CannotNameAtomicPartAsValidPartPiece(string partName)
        {
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage, 0, _partPieceSampleZipPackage.Length);

            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            Assert.Throws<ArgumentException>(() => zipPackage.CreatePart(new Uri(partName, UriKind.Relative), "application/octet-stream"));
        }

        [Fact]
        public void CannotNameAtomicPartAsInvalidPartPiece()
        {
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage, 0, _partPieceSampleZipPackage.Length);

            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            Assert.Throws<ArgumentException>(() => zipPackage.CreatePart(new Uri("/InvalidPartEntry.bin/[8].piece", UriKind.Relative), "application/octet-stream"));
        }

        // This confirms that both atomic and interleaved parts can be deleted. It deletes the part,
        // then checks the underlying ZIP file to ensure that any underlying files are gone.
        [Theory]
        [InlineData("/AtomicPartEntry.bin", "AtomicPartEntry.bin")]
        [InlineData("/FixedDocumentSequence.bin", "FixedDocumentSequence.bin/[0].piece,FixedDocumentSequence.bin/[1].piece,FixedDocumentSequence.bin/[2].piece,FixedDocumentSequence.bin/[3].last.piece")]
        public void CanDeleteParts(string partToDelete, string zipEntriesToCheck)
        {
            var atomicPartEntryUri = new Uri(partToDelete, UriKind.Relative);
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage, 0, _partPieceSampleZipPackage.Length);

            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            var zipPackage = Package.Open(ms, FileMode.Open);

            zipPackage.DeletePart(atomicPartEntryUri);

            Assert.Throws<InvalidOperationException>(() => zipPackage.GetPart(atomicPartEntryUri));

            zipPackage.Flush();
            (zipPackage as IDisposable).Dispose();

            ms.Seek(0, SeekOrigin.Begin);
            zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);

            foreach (var zipEntryName in zipEntriesToCheck.Split(','))
            {
                Assert.Null(zipArchive.GetEntry(zipEntryName));
            }
        }

        // This confirms that data can be read from both atomic and interleaved parts.
        // When reading data from interleaved parts, reads from different positions in
        // the stream (to ensure that we're reading data from different part pieces,
        // or reading multiple part pieces at once.)
        [Theory]
        [InlineData("/ReadableAtomicPartEntry.bin", 100, 10)]
        [InlineData("/ReadablePartPieceEntry.bin", 11, 10)]
        [InlineData("/ReadablePartPieceEntry.bin", 11, 25)]
        public void CanReadPartData(string partName, int position, int dataLength)
        {
            byte[] expectedValues = Enumerable.Range(position, dataLength).Select(x => (byte)x).ToArray();
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            var atomicPart = zipPackage.GetPart(new Uri(partName, UriKind.Relative));
            using var readStream = atomicPart.GetStream(FileMode.Open);
            var byteBuffer = new byte[dataLength];

            readStream.Seek(position, SeekOrigin.Begin);

            readStream.Read(byteBuffer);
            Assert.Equal(byteBuffer, expectedValues);
        }

        // An interleaved part requires a separate type of stream. This test
        // confirms that we can seek within it.
        [Fact]
        public void CanSeekAndReadDataFromTwoPartPieces()
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            var atomicPart = zipPackage.GetPart(new Uri("/ReadablePartPieceEntry.bin", UriKind.Relative));
            using var readStream = atomicPart.GetStream(FileMode.Open);
            var byteBuffer = new byte[10];

            readStream.Read(byteBuffer);
            Assert.Equal(byteBuffer, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

            readStream.Seek(16, SeekOrigin.Begin);

            readStream.Read(byteBuffer);
            Assert.Equal(byteBuffer, [16, 17, 18, 19, 20, 21, 22, 23, 24, 25]);
        }

        [Fact]
        public void CannotReadPastEndOfPartPieceSequence()
        {
            using var ms = new MemoryStream(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            using var zipPackage = Package.Open(ms, FileMode.Open);

            var atomicPart = zipPackage.GetPart(new Uri("/ReadablePartPieceEntry.bin", UriKind.Relative));
            using var readStream = atomicPart.GetStream(FileMode.Open);
            var byteBuffer = new byte[readStream.Length + 1];

            Assert.NotEqual(readStream.Read(byteBuffer), byteBuffer.Length);
        }

        // This confirms that data can be written to both atomic and interleaved parts.
        // When writing data to interleaved parts, writes to different positions in
        // the stream (to ensure that we're writing data to different part pieces,
        // or writing multiple part pieces at once.) It also flushes and closes the package,
        // then reopens it and re-reads the data to ensure that this data has been flushed
        // to the base stream.
        [Theory]
        [InlineData("/ReadableAtomicPartEntry.bin", 5, 10)]
        [InlineData("/ReadablePartPieceEntry.bin", 10, 10)]
        [InlineData("/ReadablePartPieceEntry.bin", 60, 8)]
        public void CanWritePartData(string partName, int startIndex, int count)
        {
            var bytesToWrite = Enumerable.Repeat<byte>(1, count).ToArray();
            var atomicPartEntryUri = new Uri(partName, UriKind.Relative);
            long expectedStreamLength = startIndex + count;

            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            var zipPackage = Package.Open(ms, FileMode.Open);

            var atomicPart = zipPackage.GetPart(atomicPartEntryUri);
            var writeStream = atomicPart.GetStream(FileMode.OpenOrCreate);

            // If we're about to write beyond the end of the stream, record the length
            // and verify before we read. This enables verification that the piece of an
            // interleaved part is growing in the ZIP file when the stream expands.
            if (writeStream.Length > expectedStreamLength)
            {
                expectedStreamLength = writeStream.Length;
            }
            writeStream.Seek(startIndex, SeekOrigin.Begin);
            writeStream.Write(bytesToWrite);

            writeStream.Flush();
            zipPackage.Flush();
            (zipPackage as IDisposable).Dispose();

            ms.Seek(0, SeekOrigin.Begin);

            zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            zipPackage = Package.Open(ms, FileMode.Open);
            atomicPart = zipPackage.GetPart(atomicPartEntryUri);
            using var readStream = atomicPart.GetStream(FileMode.OpenOrCreate);
            var buffer = new byte[count];

            Assert.Equal(expectedStreamLength, readStream.Length);

            readStream.Seek(startIndex, SeekOrigin.Begin);
            readStream.Read(buffer);

            Assert.Equal(bytesToWrite, buffer);
        }

        [Fact]
        public void CanSeekAndWriteDataToTwoPartPieces()
        {
            var atomicPartEntryUri = new Uri("/ReadablePartPieceEntry.bin", UriKind.Relative);

            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            var zipPackage = Package.Open(ms, FileMode.Open);

            var atomicPart = zipPackage.GetPart(atomicPartEntryUri);
            var writeStream = atomicPart.GetStream(FileMode.OpenOrCreate);

            writeStream.Write([0, 0]);
            writeStream.Seek(16, SeekOrigin.Begin);
            writeStream.Write([0, 0]);

            writeStream.Flush();

            zipPackage.Flush();
            (zipPackage as IDisposable).Dispose();

            ms.Seek(0, SeekOrigin.Begin);

            zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
            zipPackage = Package.Open(ms, FileMode.Open);
            atomicPart = zipPackage.GetPart(atomicPartEntryUri);
            using var readStream = atomicPart.GetStream(FileMode.OpenOrCreate);
            var buffer = new byte[24];

            readStream.Read(buffer);

            Assert.Equal(buffer, [0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0, 0, 18, 19, 20, 21, 22, 23]);
        }

        [Theory]
        [InlineData("ReadablePartPieceEntry.bin", 30, "[0].piece,[1].piece,[2].last.piece", "[2].piece,[3].last.piece")]
        [InlineData("ReadablePartPieceEntry.bin", 0, "[0].last.piece", "[0].piece,[1].piece,[2].piece,[3].last.piece")]
        public void SetLengthRemovesPartPieces(string partName, int newLength, string remainingPieces, string removedPieces)
        {
            using var ms = new MemoryStream();

            ms.Write(_partPieceSampleZipPackage);
            var zipArchive = new ZipArchive(ms, ZipArchiveMode.Update);
            var zipPackage = Package.Open(ms, FileMode.Open);

            var partEntry = zipPackage.GetPart(new Uri("/" + partName, UriKind.Relative));
            var writeStream = partEntry.GetStream(FileMode.OpenOrCreate);

            // We perform a write to force the InterleavedPartStream to lazily initialise some of the underlying streams.
            // Doing so forces ZipPackage to open the ZipArchiveEntry (which in turn would normally prevent it from being deleted.)
            writeStream.Write([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
            writeStream.SetLength(newLength);

            writeStream.Flush();

            zipPackage.Flush();
            (zipPackage as IDisposable).Dispose();

            ms.Seek(0, SeekOrigin.Begin);

            zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);

            foreach (var pieceName in remainingPieces.Split(','))
            {
                Assert.NotNull(zipArchive.GetEntry(partName + "/" + pieceName));
            }
            foreach (var pieceName in removedPieces.Split(','))
            {
                Assert.Null(zipArchive.GetEntry(partName + "/" + pieceName));
            }

            zipArchive.Dispose();
        }
    }
}
