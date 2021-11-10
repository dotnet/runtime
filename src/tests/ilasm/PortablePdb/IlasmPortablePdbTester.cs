using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace IlasmPortablePdbTests
{
    public class IlasmPortablePdbTester : XunitBase
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

        // Tests whether pe file includes portable pdb codeview debug directory
        // and its contents against the generated portable pdb metadata file
        [Theory]
        [InlineData("TestPdbDebugDirectory1.il")]
        [InlineData("TestPdbDebugDirectory2.il")]
        public void TestPortablePdbDebugDirectory(string ilSource)
        {
            var ilasm = IlasmPortablePdbTesterCommon.GetIlasmFullPath(CoreRootVar, IlasmFile);
            IlasmPortablePdbTesterCommon.Assemble(ilasm, ilSource, TestDir, out string dll, out string pdb);
            
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

                    using (var pdbReaderProvider = IlasmPortablePdbTesterCommon.GetMetadataReaderProvider(dll, pdb, peReader, false))
                    {
                        var portablePdbMdReader = pdbReaderProvider.GetMetadataReader();
                        Assert.NotNull(portablePdbMdReader);
                        // check pdb stream
                        Assert.NotNull(portablePdbMdReader.DebugMetadataHeader);
                        // check pdb guid
                        var pdbGuid = portablePdbDbgEntry.Guid.ToByteArray();
                        var pdbId = portablePdbMdReader.DebugMetadataHeader.Id.ToArray();
                        int i = 0;
                        foreach (var pdbGuidByte in pdbGuid)
                        {
                            Assert.True(i < pdbId.Length);
                            Assert.Equal(pdbGuidByte, pdbId[i++]);
                        }
                        Assert.Equal(i, pdbGuid.Length);
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
        [Fact]
        public void TestPortablePdbMethodDebugInformation2()
        {
            var ilSource = IsUnix ? "TestMethodDebugInformation_unix.il" : "TestMethodDebugInformation_win.il";

            var expected = IlasmPortablePdbTesterCommon.GetExpectedForTestMethodDebugInformation(ilSource);
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
                            var methodDocument = portablePdbMdReader.GetDocument(methodDebugInformation.Document);
                            var methodDocumentName = portablePdbMdReader.GetString(methodDocument.Name);
                            Assert.Equal(expectedMethodDbgInfo.Document.Name, methodDocumentName);

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
        
        public static int Main(string[] args)
        {
            return new IlasmPortablePdbTester().RunTests();
        }
    }
}
