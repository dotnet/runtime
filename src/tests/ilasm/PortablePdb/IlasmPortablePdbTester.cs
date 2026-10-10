using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace IlasmPortablePdbTests
{
    public class IlasmPortablePdbTester : IDisposable
    {
        private const string CoreRoot = "CORE_ROOT";
        private const string IlasmFileName = "ilasm";
        private const string TestDir = "TestFiles";
        public string CoreRootVar { get; private set; }
        public bool IsUnix { get; private set; }
        public string NativeExtension { get; private set; }
        public string IlasmFile { get; private set; }

        public IlasmPortablePdbTester()
        {
            CoreRootVar = Environment.GetEnvironmentVariable(CoreRoot);
            IsUnix = !OperatingSystem.IsWindows();
            NativeExtension = IsUnix ? string.Empty : ".exe";
            IlasmFile = IlasmFileName + NativeExtension;
        }

        // Tests whether pe file includes portable pdb codeview and pdb checksum debug directory entries
        // and their contents against the generated portable pdb file, with and without deterministic output
        [Theory]
        [InlineData("TestPdbDebugDirectory1.il", false)]
        [InlineData("TestPdbDebugDirectory1.il", true)]
        [InlineData("TestPdbDebugDirectory2.il", false)]
        [InlineData("TestPdbDebugDirectory2.il", true)]
        public void TestPortablePdbDebugDirectory(string ilSource, bool deterministic)
        {
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb, deterministic);

            using (var peStream = new FileStream(dll, FileMode.Open, FileAccess.Read))
            {
                using (var peReader = new PEReader(peStream))
                {
                    var dbgDirEntries = peReader.ReadDebugDirectory();
                    Assert.False(dbgDirEntries.IsEmpty);

                    var dbgEntry = dbgDirEntries.FirstOrDefault(dbgEntry => dbgEntry.IsPortableCodeView);
                    Assert.True(dbgEntry.DataSize > 0);

                    var portablePdbDbgEntry = peReader.ReadCodeViewDebugDirectoryData(dbgEntry);
                    Assert.Equal(1, portablePdbDbgEntry.Age);
                    Assert.Equal(pdb, portablePdbDbgEntry.Path);

                    var pdbChecksumEntry = Assert.Single(dbgDirEntries, entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
                    var pdbChecksum = peReader.ReadPdbChecksumDebugDirectoryData(pdbChecksumEntry);
                    Assert.Equal("SHA256", pdbChecksum.AlgorithmName);

                    var pdbImage = File.ReadAllBytes(pdb);
                    using (var pdbImageReaderProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbImage)))
                    {
                        var pdbHeader = pdbImageReaderProvider.GetMetadataReader().DebugMetadataHeader;
                        Assert.NotNull(pdbHeader);

                        // check pdb id (guid and stamp) against the codeview entry
                        var pdbId = new BlobContentId(pdbHeader.Id);
                        Assert.Equal(portablePdbDbgEntry.Guid, pdbId.Guid);
                        Assert.Equal(dbgEntry.Stamp, pdbId.Stamp);

                        // check pdb checksum: the hash of the entire pdb file with its 20-byte pdb id zeroed
                        Array.Clear(pdbImage, pdbHeader.IdStartOffset, pdbHeader.Id.Length);
                        Assert.Equal(SHA256.HashData(pdbImage), pdbChecksum.Checksum.ToArray());
                    }

                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);
                        // check pdb stream
                        Assert.NotNull(portablePdbMdReader.DebugMetadataHeader);
                        var peMdReader = peReader.GetMetadataReader();
                        Assert.NotNull(peMdReader);

                        // check entry point if exists
                        if (!portablePdbMdReader.DebugMetadataHeader.EntryPoint.IsNil)
                        {
                            var method = peMdReader.GetMethodDefinition(portablePdbMdReader.DebugMetadataHeader.EntryPoint);
                            var methodName = peMdReader.GetString(method.Name);
                            Assert.Equal("Main", methodName);
                        }
                    }
                }
            }
        }

        // Tests that deterministic output derives the MVID, the PE timestamp and the PDB ID from the content:
        // two inputs that differ only in a method body, in metadata of the same size, or in sequence points
        // get different identities, and the same input gives the same bytes.
        // Both inputs are assembled from the same source path to the same output path, so that they differ
        // only in the replaced text. A change that reaches only the PDB still changes the PE image, through
        // the PDB ID in its CodeView entry.
        [Theory]
        [InlineData("MethodBody", "ldc.i4.1", "ldc.i4.2", false, true)]
        [InlineData("MethodBody", "ldc.i4.1", "ldc.i4.2", false, false)]
        [InlineData("MetadataOfSameSize", "int32 F()", "int32 G()", false, true)]
        [InlineData("MetadataOfSameSize", "int32 F()", "int32 G()", false, false)]
        [InlineData("SequencePoints", ".line 10,10", ".line 20,20", true, true)]
        [InlineData("SequencePoints", ".line 10,10", ".line 20,20", true, false)]
        public void TestDeterministicIdentity(string change, string original, string replacement, bool onlyPdbChanges, bool debug)
        {
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            var template = File.ReadAllText(Path.Combine(TestDir, "TestDeterministicIdentity.il"));
            Assert.Equal(1, template.Split(original).Length - 1);

            var ilSource = $"TestDeterministicIdentity{change}{(debug ? "Debug" : "NoDebug")}.il";
            var ilPath = Path.Combine(TestDir, ilSource);

            File.WriteAllText(ilPath, template);
            var first = AssembleDeterministic(ilasm, ilSource, debug);
            var again = AssembleDeterministic(ilasm, ilSource, debug);
            File.WriteAllText(ilPath, template.Replace(original, replacement));
            var second = AssembleDeterministic(ilasm, ilSource, debug);

            Assert.Equal(first.Dll, again.Dll);
            Assert.Equal(first.Pdb, again.Pdb);

            bool imageChanges = !onlyPdbChanges || debug;
            Assert.Equal(imageChanges, first.Mvid != second.Mvid);
            Assert.Equal(imageChanges, first.Stamp != second.Stamp);
            if (debug)
            {
                Assert.Equal(onlyPdbChanges, !first.PdbId.SequenceEqual(second.PdbId));
            }

            AssertIdentityIsContentHash(first.Dll, first.Pdb);
            AssertIdentityIsContentHash(second.Dll, second.Pdb);
        }

        // Tests that deterministic output is repeatable and its identity is the hash of the image as written,
        // for image layouts that differ from the default: stripped relocations, PE32+, and an export directory,
        // whose timestamp must not be the current time.
        [Theory]
        [InlineData("TestDeterministicIdentity.il", "-stripreloc", false)]
        [InlineData("TestDeterministicIdentity.il", "-pe64 -x64", false)]
        [InlineData("TestDeterministicExport.il", "", true)]
        public void TestDeterministicImageLayout(string ilSource, string options, bool hasExports)
        {
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            var first = AssembleDeterministic(ilasm, ilSource, debug: true, options: options);
            var again = AssembleDeterministic(ilasm, ilSource, debug: true, options: options);

            Assert.Equal(first.Dll, again.Dll);
            Assert.Equal(first.Pdb, again.Pdb);
            AssertIdentityIsContentHash(first.Dll, first.Pdb);

            using (var peReader = new PEReader(ImmutableArray.Create(first.Dll)))
            {
                Assert.Equal(options.Contains("-stripreloc"), peReader.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.RelocsStripped));
                Assert.Equal(options.Contains("-pe64"), peReader.PEHeaders.PEHeader.Magic == PEMagic.PE32Plus);

                var exportTable = peReader.PEHeaders.PEHeader.ExportTableDirectory;
                Assert.Equal(hasExports, exportTable.Size != 0);
                if (hasExports)
                {
                    Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(exportTable, out int exportTableOffset));
                    Assert.Equal(0u, BitConverter.ToUInt32(first.Dll, exportTableOffset + 4));
                }
            }
        }

        private static (byte[] Dll, byte[] Pdb, Guid Mvid, uint Stamp, byte[] PdbId) AssembleDeterministic(string ilasm, string ilSource, bool debug, string options = "")
        {
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb, deterministic: true, debug: debug, options: options);

            var peImage = File.ReadAllBytes(dll);
            Guid mvid;
            uint stamp;
            using (var peReader = new PEReader(ImmutableArray.Create(peImage)))
            {
                var mdReader = peReader.GetMetadataReader();
                mvid = mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid);
                stamp = (uint)peReader.PEHeaders.CoffHeader.TimeDateStamp;
            }

            byte[] pdbImage = null;
            byte[] pdbId = null;
            if (debug)
            {
                pdbImage = File.ReadAllBytes(pdb);
                using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbImage)))
                {
                    pdbId = pdbReaderProvider.GetMetadataReader().DebugMetadataHeader.Id.ToArray();
                }
            }

            return (peImage, pdbImage, mvid, stamp, pdbId);
        }

        // Checks that the MVID and the PE timestamp are the start of the SHA-256 hash of the PE file with both
        // zeroed, and that the PDB ID is the start of the SHA-256 hash of the PDB file with its ID zeroed.
        private static void AssertIdentityIsContentHash(byte[] peImage, byte[] pdbImage)
        {
            using (var peReader = new PEReader(ImmutableArray.Create(peImage)))
            {
                var mdReader = peReader.GetMetadataReader();
                int mvidOffset = peReader.PEHeaders.MetadataStartOffset
                    + mdReader.GetHeapMetadataOffset(HeapIndex.Guid)
                    + (MetadataTokens.GetHeapOffset(mdReader.GetModuleDefinition().Mvid) - 1) * 16;
                int stampOffset = peReader.PEHeaders.CoffHeaderStartOffset + 4;
                var identity = peImage.AsSpan(mvidOffset, 16).ToArray().Concat(peImage.AsSpan(stampOffset, 4).ToArray()).ToArray();

                var zeroed = (byte[])peImage.Clone();
                Array.Clear(zeroed, mvidOffset, 16);
                Array.Clear(zeroed, stampOffset, 4);
                Assert.Equal(identity, SHA256.HashData(zeroed).Take(identity.Length).ToArray());
            }

            if (pdbImage is not null)
            {
                using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbImage)))
                {
                    var pdbHeader = pdbReaderProvider.GetMetadataReader().DebugMetadataHeader;
                    var zeroed = (byte[])pdbImage.Clone();
                    Array.Clear(zeroed, pdbHeader.IdStartOffset, pdbHeader.Id.Length);
                    Assert.Equal(pdbHeader.Id.ToArray(), SHA256.HashData(zeroed).Take(pdbHeader.Id.Length).ToArray());
                }
            }
        }

        // Tests whether the portable PDB has all document name properly defined
        // The test source file includes external source reference and thus has 2 variants depending on OS type
        [Fact]
        public void TestPortablePdbDocuments()
        {
            var ilSource = IsUnix ? "TestDocuments1_unix.il" : "TestDocuments1_win.il";

            var expected = IlasmPortablePdbTesterCommon.GetExpectedDocuments(ilSource, TestDir);
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb);

            using (var peStream = new FileStream(dll, FileMode.Open, FileAccess.Read))
            {
                using (var peReader = new PEReader(peStream))
                {
                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);
                        Assert.Equal(expected.Count, portablePdbMdReader.Documents.Count);

                        int i = 0;
                        foreach (var documentHandle in portablePdbMdReader.Documents)
                        {
                            Assert.True(i < expected.Count);
                            var document = portablePdbMdReader.GetDocument(documentHandle);
                            var name = portablePdbMdReader.GetString(document.Name);
                            Assert.Equal(expected[i].Name, name);
                            i++;
                        }
                        Assert.Equal(expected.Count, i);
                    }
                }
            }
        }

        // Tests whether the portable PDB MethodDebugInformation table has all the entries as MethoDef table
        [Fact]
        public void TestPortablePdbMethodDebugInformation1()
        {
            var ilSource = "TestMethodDebugInformation.il";

            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb);

            using (var peStream = new FileStream(dll, FileMode.Open, FileAccess.Read))
            {
                using (var peReader = new PEReader(peStream))
                {
                    var peMdReader = peReader.GetMetadataReader();
                    Assert.NotNull(peMdReader);
                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);
                        Assert.Equal(peMdReader.MethodDefinitions.Count, portablePdbMdReader.MethodDebugInformation.Count);
                    }
                }
            }
        }

        // Tests whether the portable PDB has appropriate sequence points defined
        // The test source file includes external source reference and thus has 2 variants depending on OS type
        [Theory]
        [InlineData("TestMethodDebugInformation")]
        [InlineData("TestDocuments1")]
        public void TestPortablePdbMethodDebugInformation2(string testName)
        {
            var ilSource = testName + (IsUnix ? "_unix.il" : "_win.il");

            var expected = IlasmPortablePdbTesterCommon.GetExpectedForTestMethodDebugInformation(testName, IsUnix);
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb);

            using (var peStream = new FileStream(dll, FileMode.Open, FileAccess.Read))
            {
                using (var peReader = new PEReader(peStream))
                {
                    var peMdReader = peReader.GetMetadataReader();
                    Assert.NotNull(peMdReader);
                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);

                        foreach (var methodDefinitionHandle in peMdReader.MethodDefinitions)
                        {
                            // get method definition from pe file metadata
                            var methodDefinition = peMdReader.GetMethodDefinition(methodDefinitionHandle);
                            var methodName = peMdReader.GetString(methodDefinition.Name);
                            Assert.True(expected.TryGetValue(methodName, out var expectedMethodDbgInfo));

                            // verify method debug information from portable pdb metadata
                            var methodDebugInformation = portablePdbMdReader.GetMethodDebugInformation(methodDefinitionHandle);

                            if (expectedMethodDbgInfo.Document == null)
                            {
                                Assert.True(methodDebugInformation.Document.IsNil);
                            }
                            else
                            {
                                var methodDocument = portablePdbMdReader.GetDocument(methodDebugInformation.Document);
                                var methodDocumentName = portablePdbMdReader.GetString(methodDocument.Name);
                                Assert.Equal(expectedMethodDbgInfo.Document.Name, methodDocumentName);
                            }
                            int i = 0;
                            foreach (var sequencePoint in methodDebugInformation.GetSequencePoints())
                            {
                                var sequencePointDocument = portablePdbMdReader.GetDocument(sequencePoint.Document);
                                var sequencePointDocumentName = portablePdbMdReader.GetString(sequencePointDocument.Name);

                                Assert.True(i < expectedMethodDbgInfo.SequencePoints.Count);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].Document.Name, sequencePointDocumentName);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].IsHidden, sequencePoint.IsHidden);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].Offset, sequencePoint.Offset);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].StartLine, sequencePoint.StartLine);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].EndLine, sequencePoint.EndLine);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].StartColumn, sequencePoint.StartColumn);
                                Assert.Equal(expectedMethodDbgInfo.SequencePoints[i].EndColumn, sequencePoint.EndColumn);
                                i++;
                            }
                            Assert.Equal(expectedMethodDbgInfo.SequencePoints.Count, i);
                        }

                    }
                }
            }
        }

        // Tests whether the portable PDB has appropriate local scopes defined
        [Theory]
        [InlineData("TestLocalScopes1.il")]
        [InlineData("TestLocalScopes2.il")]
        [InlineData("TestLocalScopes3.il")]
        [InlineData("TestLocalScopes4.il")]
        public void TestPortablePdbLocalScope(string ilSource)
        {
            var expected = IlasmPortablePdbTesterCommon.GetExpectedForTestLocalScopes(ilSource);
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb);

            using (var peStream = new FileStream(dll, FileMode.Open, FileAccess.Read))
            {
                using (var peReader = new PEReader(peStream))
                {
                    var peMdReader = peReader.GetMetadataReader();
                    Assert.NotNull(peMdReader);
                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);

                        foreach (var methodDefinitionHandle in peMdReader.MethodDefinitions)
                        {
                            // get method definition from pe file metadata
                            var methodDefinition = peMdReader.GetMethodDefinition(methodDefinitionHandle);
                            var methodName = peMdReader.GetString(methodDefinition.Name);

                            // verify local scopes from portable pdb metadata
                            var localScopeHandles = portablePdbMdReader.GetLocalScopes(methodDefinitionHandle);

                            int i = 0;
                            foreach (var localScopeHandle in localScopeHandles)
                            {
                                Assert.True(i < expected.Count);
                                Assert.Equal(expected[i].MethodName, methodName);

                                var localScope = portablePdbMdReader.GetLocalScope(localScopeHandle);
                                Assert.Equal(expected[i].StartOffset, localScope.StartOffset);
                                Assert.Equal(expected[i].EndOffset, localScope.EndOffset);
                                Assert.Equal(expected[i].Length, localScope.Length);
                                var variableHandles = localScope.GetLocalVariables();
                                Assert.Equal(expected[i].Variables.Count, variableHandles.Count);

                                int j = 0;
                                foreach (var variableHandle in localScope.GetLocalVariables())
                                {
                                    Assert.True(j < expected[i].Variables.Count);
                                    var variable = portablePdbMdReader.GetLocalVariable(variableHandle);
                                    var variableName = portablePdbMdReader.GetString(variable.Name);
                                    Assert.Equal(expected[i].Variables[j].Name, variableName);
                                    Assert.Equal(expected[i].Variables[j].Index, variable.Index);
                                    Assert.Equal(expected[i].Variables[j].IsDebuggerHidden,
                                        variable.Attributes == LocalVariableAttributes.DebuggerHidden);
                                    j++;
                                }
                                Assert.Equal(expected[i].Variables.Count, j);
                                i++;
                            }
                            Assert.Equal(expected.Count, i);
                        }
                    }
                }
            }
        }

        public void Dispose() {}
    }
}
