// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Xunit;

// The description parameter is not read: it names the generated case in the test's display name.
#pragma warning disable xUnit1026

namespace ILAssembler.Tests.GeneratedCases
{
    /// <summary>
    /// The PDB rules, checked over seeded generated programs and option combinations
    /// (<see cref="PdbCaseGenerator"/>), and for the output file writer (<see cref="OutputFileWriter"/>) over every
    /// combination of output name, pre-existing output, pre-existing PDB path and PDB presence that
    /// <see cref="PdbCaseGenerator.OutputWriteStates"/> enumerates.
    /// </summary>
    public class PdbGeneratedCaseTests
    {
        private static readonly ImmutableArray<byte> s_newPdb = [0x42, 0x53, 0x4A, 0x42];

        private static (CompilationResult Result, ImmutableArray<byte> Image) Compile(GeneratedProgram program, GeneratedOptions options)
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(program.ToSource(), options.ToOptions());
            return (result, DocumentCompilerTestHelpers.Serialize(result));
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.AnyCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void Pdb_IsProducedExactlyWhenASwitchRequestsIt(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.AnyCases[index];

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(input.Program.ToSource(), input.Options.ToOptions());

            Assert.Equal(input.Options.RequestsPdb, result.PortablePdb.HasValue);
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.AnyCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void WithoutAPdbSwitch_ImageHasNoDebugDirectory(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.AnyCases[index];
            GeneratedOptions options = input.Options with { Debug = false, DebugMode = null, Pdb = false };

            (_, ImmutableArray<byte> image) = Compile(input.Program, options);
            using var pe = new PEReader(image);
            DirectoryEntry debugTable = pe.PEHeaders.PEHeader!.DebugTableDirectory;

            Assert.Equal((0, 0), (debugTable.RelativeVirtualAddress, debugTable.Size));

            // No debug data either: the Reproducible entry ManagedPEBuilder writes by default into a deterministic
            // image would make its .text larger than a nondeterministic one's.
            Assert.Equal(TextSize(input.Program, options with { Deterministic = false }), TextSize(input.Program, options with { Deterministic = true }));
        }

        private static int TextSize(GeneratedProgram program, GeneratedOptions options)
        {
            (_, ImmutableArray<byte> image) = Compile(program, options);
            using var pe = new PEReader(image);
            return pe.PEHeaders.SectionHeaders.Single(section => section.Name == ".text").VirtualSize;
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.PdbRequestedCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void RequestedPdb_IsReferencedByCodeViewThenPdbChecksumThenReproducibleIffDeterministic(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.PdbRequestedCases[index];
            (_, ImmutableArray<byte> image) = Compile(input.Program, input.Options);
            using var pe = new PEReader(image);

            DebugDirectoryEntryType[] expected = input.Options.Deterministic
                ? [DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible]
                : [DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum];
            Assert.Equal(expected, pe.ReadDebugDirectory().Select(entry => entry.Type));
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.PdbRequestedCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void RequestedPdb_CodeViewEntryCarriesThePdbId(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.PdbRequestedCases[index];
            (CompilationResult result, ImmutableArray<byte> image) = Compile(input.Program, input.Options);
            using var pe = new PEReader(image);
            DebugDirectoryEntry codeViewEntry = pe.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.CodeView);
            using MetadataReaderProvider pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
            BlobContentId pdbId = new(pdbProvider.GetMetadataReader().DebugMetadataHeader!.Id);

            Assert.Equal((pdbId.Guid, pdbId.Stamp), (pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Guid, codeViewEntry.Stamp));
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.PdbRequestedCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void RequestedPdb_CodeViewEntryNamesThePdbPathOrItsFallback(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.PdbRequestedCases[index];
            (_, ImmutableArray<byte> image) = Compile(input.Program, input.Options);
            using var pe = new PEReader(image);
            DebugDirectoryEntry codeViewEntry = pe.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.CodeView);

            Assert.Equal(input.Options.ExpectedCodeViewPath, pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Path);
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.PdbRequestedCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void RequestedPdb_PdbChecksumIsSha256OfThePdbWithItsIdZeroed(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.PdbRequestedCases[index];
            (CompilationResult result, ImmutableArray<byte> image) = Compile(input.Program, input.Options);
            using var pe = new PEReader(image);
            DebugDirectoryEntry checksumEntry = pe.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
            PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);

            byte[] pdb = DocumentCompilerTestHelpers.GetPortablePdb(result).ToArray();
            using (MetadataReaderProvider pdbProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb)))
            {
                DebugMetadataHeader header = pdbProvider.GetMetadataReader().DebugMetadataHeader!;
                Array.Clear(pdb, header.IdStartOffset, header.Id.Length);
            }

            Assert.Equal("SHA256", checksum.AlgorithmName);
            Assert.Equal(SHA256.HashData(pdb), checksum.Checksum.ToArray());
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.AnyCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void Deterministic_CompilingTwice_GivesIdenticalImageAndPdb(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.AnyCases[index];
            GeneratedOptions options = input.Options with { Deterministic = true };

            (CompilationResult firstResult, ImmutableArray<byte> firstImage) = Compile(input.Program, options);
            (CompilationResult secondResult, ImmutableArray<byte> secondImage) = Compile(input.Program, options);

            Assert.Equal<byte>(firstImage, secondImage);
            Assert.Equal(firstResult.PortablePdb.HasValue, secondResult.PortablePdb.HasValue);
            if (firstResult.PortablePdb is { } firstPdb)
            {
                Assert.Equal<byte>(firstPdb, secondResult.PortablePdb!.Value);
            }
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.PdbRequestedCaseData), MemberType = typeof(PdbCaseGenerator))]
        public void Deterministic_DifferentSources_GiveDifferentImagesAndPdbIds(int index, string description)
        {
            CompileCase input = PdbCaseGenerator.PdbRequestedCases[index];
            GeneratedOptions options = input.Options with { Deterministic = true };

            (CompilationResult firstResult, ImmutableArray<byte> firstImage) = Compile(input.Program, options);
            (CompilationResult secondResult, ImmutableArray<byte> secondImage) = Compile(input.Program.WithExtraMethod(), options);

            Assert.NotEqual<byte>(firstImage, secondImage);
            using MetadataReaderProvider firstPdb = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(firstResult);
            using MetadataReaderProvider secondPdb = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(secondResult);
            Assert.NotEqual<byte>(
                firstPdb.GetMetadataReader().DebugMetadataHeader!.Id,
                secondPdb.GetMetadataReader().DebugMetadataHeader!.Id);
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.OutputWriteStates), MemberType = typeof(PdbCaseGenerator))]
        public void OutputFileWriter_NeverLeavesATemporaryFile(string outputFileName, ExistingOutput existingOutput, ExistingPdb existingPdb, bool withPdb)
        {
            foreach (bool imageWriteFails in new[] { false, true })
            {
                RunOutputWrite(outputFileName, existingOutput, existingPdb, withPdb, imageWriteFails, (directory, _, _) =>
                    Assert.DoesNotContain(Directory.EnumerateFileSystemEntries(directory), path => path.EndsWith(".tmp", StringComparison.Ordinal)));
            }
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.OutputWriteStates), MemberType = typeof(PdbCaseGenerator))]
        public void OutputFileWriter_WhenTheImageWriteFails_LeavesThePdbPathUnchanged(string outputFileName, ExistingOutput existingOutput, ExistingPdb existingPdb, bool withPdb)
        {
            RunOutputWrite(outputFileName, existingOutput, existingPdb, withPdb, imageWriteFails: true, (_, pdbPath, exception) =>
            {
                Assert.NotNull(exception);
                switch (existingPdb)
                {
                    case ExistingPdb.Absent:
                        Assert.False(Path.Exists(pdbPath));
                        break;
                    case ExistingPdb.File:
                        Assert.Equal(PdbCaseGenerator.ExistingPair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
                        break;
                    case ExistingPdb.Directory:
                        Assert.True(Directory.Exists(pdbPath));
                        break;
                }
            });
        }

        [Theory]
        [MemberData(nameof(PdbCaseGenerator.OutputWriteStates), MemberType = typeof(PdbCaseGenerator))]
        public void OutputFileWriter_AfterTheImageIsWritten_LeavesTheNewPdbOrDeletesOnlyTheReplacedImagesPdb(string outputFileName, ExistingOutput existingOutput, ExistingPdb existingPdb, bool withPdb)
        {
            RunOutputWrite(outputFileName, existingOutput, existingPdb, withPdb, imageWriteFails: false, (_, pdbPath, exception) =>
            {
                if (existingPdb == ExistingPdb.Directory)
                {
                    // A directory is never deleted; with a PDB, renaming over it fails.
                    Assert.Equal(withPdb, exception is not null);
                    Assert.True(Directory.Exists(pdbPath));
                }
                else if (withPdb)
                {
                    Assert.Null(exception);
                    Assert.Equal(s_newPdb.ToArray(), File.ReadAllBytes(pdbPath));
                }
                else if (existingPdb == ExistingPdb.File && existingOutput != ExistingOutput.ImageOfThePdb)
                {
                    // The PDB does not belong to the image that was replaced, so it stays.
                    Assert.Null(exception);
                    Assert.Equal(PdbCaseGenerator.ExistingPair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
                }
                else
                {
                    Assert.Null(exception);
                    Assert.False(File.Exists(pdbPath));
                }
            });
        }

        private static void RunOutputWrite(
            string outputFileName,
            ExistingOutput existingOutput,
            ExistingPdb existingPdb,
            bool withPdb,
            bool imageWriteFails,
            Action<string, string, Exception?> check)
        {
            string directory = Directory.CreateTempSubdirectory("ilasm-output-").FullName;
            try
            {
                string outputPath = Path.Combine(directory, outputFileName);
                string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
                switch (existingOutput)
                {
                    case ExistingOutput.ImageOfThePdb:
                        File.WriteAllBytes(outputPath, PdbCaseGenerator.ExistingPair.Image.ToArray());
                        break;
                    case ExistingOutput.ImageOfAnotherPdb:
                        File.WriteAllBytes(outputPath, PdbCaseGenerator.ImageOfAnotherPdb.ToArray());
                        break;
                    case ExistingOutput.NotAnImage:
                        File.WriteAllBytes(outputPath, [0x01]);
                        break;
                    case ExistingOutput.CoffObject:
                        File.WriteAllBytes(outputPath, new byte[20]);
                        break;
                }

                switch (existingPdb)
                {
                    case ExistingPdb.File:
                        File.WriteAllBytes(pdbPath, PdbCaseGenerator.ExistingPair.Pdb.ToArray());
                        break;
                    case ExistingPdb.Directory:
                        Directory.CreateDirectory(pdbPath);
                        break;
                }

                Exception? exception = Record.Exception(() => OutputFileWriter.Write(
                    outputPath,
                    pdbPath,
                    stream =>
                    {
                        stream.WriteByte(0x4D);
                        if (imageWriteFails)
                        {
                            throw new IOException("Injected image write failure");
                        }
                    },
                    withPdb ? s_newPdb : null));

                check(directory, pdbPath, exception);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
