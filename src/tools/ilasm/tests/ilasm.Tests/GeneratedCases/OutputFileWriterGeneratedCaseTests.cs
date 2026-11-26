// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.ILAssembler.Tests.GeneratedCases
{
    public class OutputFileWriterGeneratedCaseTests
    {
        private static readonly ImmutableArray<byte> s_newPdb = [0x42, 0x53, 0x4A, 0x42];

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
