using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace IlasmPortablePdbTests
{
    public static class IlasmPortablePdbTesterCommon
    {
        public const string CommonIlasmArguments = "-nologo -dll -debug";

        public static string GetIlasmFullPath(string coreRootVar, string ilasmFile)
        {
            Assert.True(!string.IsNullOrWhiteSpace(coreRootVar));
            Assert.True(Directory.Exists(coreRootVar));
            var ilasmFullPath = Path.Combine(coreRootVar, ilasmFile);
            Assert.True(File.Exists(ilasmFullPath));
            return ilasmFullPath;
        }

        public static void Assemble(string ilasmFullPath, string ilSrc, string testDir, out string dll, out string pdb)
        {
            var currentDirectory = Environment.CurrentDirectory;
            var ilSrcFullPath = Path.Combine(currentDirectory, testDir, ilSrc);
            Assert.True(File.Exists(ilSrcFullPath));

            var dllFileName = $"{Path.GetFileNameWithoutExtension(ilSrc)}.dll";
            var pdbFileName = $"{Path.GetFileNameWithoutExtension(ilSrc)}.pdb";

            var dllFullPath = Path.Combine(currentDirectory, testDir, dllFileName);
            var pdbFullPath = Path.Combine(currentDirectory, testDir, pdbFileName);

            var ilasmArgs = $"{CommonIlasmArguments} -output={dllFullPath} {ilSrcFullPath}";
            var ilasmPsi = new ProcessStartInfo
            {
                UseShellExecute = false,
                WorkingDirectory = currentDirectory,
                FileName = ilasmFullPath,
                Arguments = ilasmArgs
            };

            Process ilasmProcess = Process.Start(ilasmPsi);
            ilasmProcess.WaitForExit();

            Assert.Equal(0, ilasmProcess.ExitCode);
            Assert.True(File.Exists(dllFullPath));
            Assert.True(File.Exists(pdbFullPath));

            dll = dllFullPath;
            pdb = pdbFullPath;
        }

        public static MetadataReaderProvider GetMetadataReaderProvider(string dll, string pdb, PEReader peReader, bool embedded)
        {
            Assert.True(peReader.TryOpenAssociatedPortablePdb(
                dll,
                filePath => File.OpenRead(filePath),
                out var pdbReaderProvider,
                out var foundPdbPath));

            Assert.NotNull(pdbReaderProvider);
            if (!embedded)
                Assert.Equal(pdb, foundPdbPath);
            else
                Assert.Null(foundPdbPath);
            
            return pdbReaderProvider;
        }

        public static List<DocumentStub> GetExpectedDocuments(string testName, string testDir)
        {
            switch (testName)
            {
                case "TestDocuments1_win.il":
                    return new List<DocumentStub>()
                    {
                        new DocumentStub(Path.Combine(Environment.CurrentDirectory, testDir, testName)),
                        new DocumentStub("C:\\tmp\\non_existent_source1.cs"),
                        new DocumentStub("C:\\tmp\\non_existent_source2.cs")
                    };
                case "TestDocuments1_unix.il":
                    return new List<DocumentStub>()
                    {
                        new DocumentStub(Path.Combine(Environment.CurrentDirectory, testDir, testName)),
                        new DocumentStub("/tmp/non_existent_source1.cs"),
                        new DocumentStub("/tmp/non_existent_source2.cs"),
                    };
                default:
                    Assert.Fail();
                    return null;
            }
        }

        public static Dictionary<string, MethodDebugInformationStub> GetExpectedForTestMethodDebugInformation(string testName)
        {
            string method1;
            string method2;
            DocumentStub document1;
            DocumentStub document2;

            switch (testName)
            {
                case "TestMethodDebugInformation_unix.il":
                    method1 = ".ctor";
                    method2 = "Pow";
                    document1 = new DocumentStub("/tmp/TestMethodDebugInformation/SimpleMath.cs");
                    document2 = new DocumentStub("/tmp/TestMethodDebugInformation/SimpleMathMethods.cs");
                    break;
                case "TestMethodDebugInformation_win.il":
                    method1 = ".ctor";
                    method2 = "Pow";
                    document1 = new DocumentStub("C:\\tmp\\TestMethodDebugInformation\\SimpleMath.cs");
                    document2 = new DocumentStub("C:\\tmp\\TestMethodDebugInformation\\SimpleMathMethods.cs");
                    break;
                default:
                    Assert.Fail();
                    return null;
            }

            return new Dictionary<string, MethodDebugInformationStub>()
            {
                {
                    method1,
                    new MethodDebugInformationStub(method1, document1,
                        new List<SequencePointStub>()
                        {
                            new SequencePointStub(document1, false, 0x0, 6, 6, 9, 37),
                            new SequencePointStub(document1, false, 0x7, 7, 7, 9, 10),
                            new SequencePointStub(document1, false, 0x8, 8, 8, 13, 28),
                            new SequencePointStub(document1, false, 0xf, 9, 9, 9, 10),
                        })
                },
                {
                    method2,
                    new MethodDebugInformationStub(method2, document2,
                        new List<SequencePointStub>()
                        {
                            new SequencePointStub(document2, false, 0x0, 6, 6, 9, 10),
                            new SequencePointStub(document2, false, 0x1, 7, 7, 13, 23),
                            new SequencePointStub(document2, false, 0x3, 8, 8, 13, 23),
                            new SequencePointStub(document2, false, 0x5, 9, 9, 13, 25),
                            new SequencePointStub(document2, false, 0x7, 10, 10, 18, 23),
                            new SequencePointStub(document2, true,  0x9),
                            new SequencePointStub(document2, false, 0xb, 11, 11, 13, 14),
                            new SequencePointStub(document2, false, 0xc, 12, 12, 17, 26),
                            new SequencePointStub(document2, false, 0x10, 13, 13, 17, 21),
                            new SequencePointStub(document2, false, 0x14, 14, 14, 13, 14),
                            new SequencePointStub(document2, false, 0x15, 10, 10, 32, 35),
                            new SequencePointStub(document2, false, 0x19, 10, 10, 25, 30),
                            new SequencePointStub(document2, true,  0x1e),
                            new SequencePointStub(document2, false, 0x21, 15, 15, 13, 24),
                            new SequencePointStub(document2, false, 0x26, 16, 16, 9, 10),
                        })
                }
            };
        }

        public static List<LocalScopeStub> GetExpectedForTestLocalScopes(string testName)
        {
            switch (testName)
            {
                case "TestLocalScopes1.il":
                    return new List<LocalScopeStub>()
                    {
                        new LocalScopeStub("Foo", 0x0, 0x19, 0x19,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_0", 0),
                            }),

                        new LocalScopeStub("Foo", 0x6, 0x14, 0xe,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_1", 1),
                                new VariableStub("LOCAL_2", 2)
                            })
                    };
                case "TestLocalScopes2.il":
                    return new List<LocalScopeStub>()
                    {
                        new LocalScopeStub("Foo", 0x0, 0x20, 0x20,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_0", 0),
                            }),

                        new LocalScopeStub("Foo", 0x6, 0x14, 0xe,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_1", 1),
                                new VariableStub("LOCAL_2", 2)
                            })
                    };
                case "TestLocalScopes3.il":
                    return new List<LocalScopeStub>()
                    {
                        new LocalScopeStub("Foo", 0x9, 0x1f, 0x16,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_0", 0),
                                new VariableStub("LOCAL_1", 1)
                            })
                    };
                case "TestLocalScopes4.il":
                    return new List<LocalScopeStub>()
                    {
                        new LocalScopeStub("Foo", 0x0, 0x24, 0x24,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_0", 0),
                            }),

                        new LocalScopeStub("Foo", 0x6, 0x1f, 0x19,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_11", 1),
                                new VariableStub("LOCAL_12", 3)
                            }),

                        new LocalScopeStub("Foo", 0xb, 0x15, 0xa,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_2", 2)
                            }),

                        new LocalScopeStub("Foo", 0x10, 0x15, 0x5,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_3", 3)
                            }),

                        new LocalScopeStub("Foo", 0x16, 0x1b, 0x5,
                            new List<VariableStub>()
                            {
                                new VariableStub("LOCAL_4", 4)
                            })
                    };
                default:
                    Assert.Fail();
                    return null;
            }
        }
    }
}
