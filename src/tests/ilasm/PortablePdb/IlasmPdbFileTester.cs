// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Xunit;

namespace IlasmPortablePdbTests
{
    // Runs ilasm end to end and checks the PDB file it writes and the image's debug directory.
    // "Native" cases run CORE_ROOT/ilasm. "Managed" cases run CORE_ROOT/managed-ilasm/ilasm, which is built only
    // where the SDK tools are (SdkToolsSupported in eng/Subsets.props), so they run only when CORE_ROOT has it.
    // Cases that native ilasm does not satisfy run only against the managed ilasm and say why.
    // The tests are static: the merged test runner creates an instance of a test class only to dispose it.
    public class IlasmPdbFileTester
    {
        private const string TestDir = "TestFiles";
        private const string Native = "native";
        private const string Managed = "managed";

        private static readonly string s_ilasmFile = "ilasm" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty);

        // Whether CORE_ROOT has the managed ilasm; the condition of every managed case.
        public static bool HasManagedIlasm =>
            Environment.GetEnvironmentVariable("CORE_ROOT") is { Length: > 0 } coreRoot &&
            File.Exists(Path.Combine(coreRoot, "managed-ilasm", s_ilasmFile));

        private static string GetIlasm(string kind) =>
            IlasmPortablePdbTesterCommon.GetIlasmFullPath(
                Environment.GetEnvironmentVariable("CORE_ROOT"),
                kind == Managed ? Path.Combine("managed-ilasm", s_ilasmFile) : s_ilasmFile);

        // A fresh directory per test and ilasm, so tests do not see each other's outputs.
        private static string CreateOutputDirectory(string testName, string kind)
        {
            string directory = Path.Combine(Environment.CurrentDirectory, "IlasmPdbFileTester", $"{testName}-{kind}");
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            Directory.CreateDirectory(directory);
            return directory;
        }

        private static int RunIlasm(string kind, string ilSource, string outputPath, string switches)
        {
            string ilSourcePath = Path.Combine(Environment.CurrentDirectory, TestDir, ilSource);
            Assert.True(File.Exists(ilSourcePath));

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = GetIlasm(kind),
                Arguments = $"-nologo -quiet -dll -output={outputPath} {switches} {ilSourcePath}",
            };

            using Process ilasm = Process.Start(startInfo);
            ilasm.WaitForExit();
            return ilasm.ExitCode;
        }

        private static PEReader ReadImage(string path) => new PEReader(ImmutableArray.Create(File.ReadAllBytes(path)));

        private static DebugDirectoryEntryType[] GetDebugDirectoryTypes(string imagePath)
        {
            using PEReader pe = ReadImage(imagePath);
            return pe.ReadDebugDirectory().Select(entry => entry.Type).ToArray();
        }

        private static bool HasDebuggableAttribute(string imagePath)
        {
            using PEReader pe = ReadImage(imagePath);
            MetadataReader reader = pe.GetMetadataReader();
            foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(handle);
                if (attribute.Constructor.Kind == HandleKind.MemberReference)
                {
                    MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (constructor.Parent.Kind == HandleKind.TypeReference &&
                        reader.GetString(reader.GetTypeReference((TypeReferenceHandle)constructor.Parent).Name) == "DebuggableAttribute")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // With -DEBUG or -PDB the PDB is a file beside the output, the CodeView entry names its full path and
        // carries its id, and nothing is embedded in the image.
        [Theory]
        [InlineData("-debug")]
        [InlineData("-pdb")]
        public static void Native_PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(string pdbSwitch) =>
            PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(Native, pdbSwitch);

        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData("-debug")]
        [InlineData("-pdb")]
        public static void Managed_PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(string pdbSwitch) =>
            PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(Managed, pdbSwitch);

        private static void PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(string kind, string pdbSwitch)
        {
            string directory = CreateOutputDirectory(nameof(PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt) + pdbSwitch, kind);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");
            string pdb = Path.Combine(directory, "TestPdbFile1.pdb");

            Assert.Equal(0, RunIlasm(kind, "TestPdbFile1.il", dll, pdbSwitch));

            Assert.True(File.Exists(pdb));
            using PEReader pe = ReadImage(dll);
            ImmutableArray<DebugDirectoryEntry> entries = pe.ReadDebugDirectory();
            Assert.DoesNotContain(entries, entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            DebugDirectoryEntry codeViewEntry = Assert.Single(entries, entry => entry.Type == DebugDirectoryEntryType.CodeView);
            CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(codeViewEntry);
            Assert.Equal(pdb, codeView.Path);

            using MetadataReaderProvider pdbProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(File.ReadAllBytes(pdb)));
            BlobContentId pdbId = new BlobContentId(pdbProvider.GetMetadataReader().DebugMetadataHeader.Id);
            Assert.Equal(pdbId.Guid, codeView.Guid);
            Assert.Equal(pdbId.Stamp, codeViewEntry.Stamp);
        }

        // The debug directory is CodeView then PdbChecksum, and under -DET a Reproducible entry follows.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Native_PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(bool deterministic) =>
            PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(Native, deterministic);

        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData(false)]
        [InlineData(true)]
        public static void Managed_PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(bool deterministic) =>
            PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(Managed, deterministic);

        private static void PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(string kind, bool deterministic)
        {
            string directory = CreateOutputDirectory(nameof(PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic) + deterministic, kind);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");

            Assert.Equal(0, RunIlasm(kind, "TestPdbFile1.il", dll, deterministic ? "-debug -det" : "-debug"));

            DebugDirectoryEntryType[] expected = deterministic
                ? new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible }
                : new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum };
            Assert.Equal(expected, GetDebugDirectoryTypes(dll));
        }

        // The PdbChecksum entry is SHA-256 of the PDB file with its 20-byte id zeroed (PE-COFF.md).
        // Managed only: native ilasm hashes the PDB before its content is final (#135211).
        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData("-debug")]
        [InlineData("-debug -det")]
        public static void Managed_PdbSwitch_PdbChecksumIsSha256OfThePdbWithItsIdZeroed(string switches)
        {
            string directory = CreateOutputDirectory(nameof(Managed_PdbSwitch_PdbChecksumIsSha256OfThePdbWithItsIdZeroed) + switches.Replace(" ", string.Empty), Managed);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");
            string pdb = Path.Combine(directory, "TestPdbFile1.pdb");

            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", dll, switches));

            using PEReader pe = ReadImage(dll);
            DebugDirectoryEntry checksumEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
            PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
            byte[] pdbBytes = File.ReadAllBytes(pdb);
            using (MetadataReaderProvider pdbProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbBytes)))
            {
                DebugMetadataHeader header = pdbProvider.GetMetadataReader().DebugMetadataHeader;
                Array.Clear(pdbBytes, header.IdStartOffset, header.Id.Length);
            }

            Assert.Equal("SHA256", checksum.AlgorithmName);
            Assert.Equal(SHA256.HashData(pdbBytes), checksum.Checksum.ToArray());
        }

        // -PDB produces the PDB without adding a DebuggableAttribute; -DEBUG adds one.
        [Theory]
        [InlineData("-pdb", false)]
        [InlineData("-debug", true)]
        public static void Native_PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(string pdbSwitch, bool expectAttribute) =>
            PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(Native, pdbSwitch, expectAttribute);

        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData("-pdb", false)]
        [InlineData("-debug", true)]
        public static void Managed_PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(string pdbSwitch, bool expectAttribute) =>
            PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(Managed, pdbSwitch, expectAttribute);

        private static void PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(string kind, string pdbSwitch, bool expectAttribute)
        {
            string directory = CreateOutputDirectory(nameof(PdbSwitch_AddsADebuggableAttributeOnlyWithDebug) + pdbSwitch, kind);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");

            Assert.Equal(0, RunIlasm(kind, "TestPdbFile1.il", dll, pdbSwitch));

            Assert.Equal(expectAttribute, HasDebuggableAttribute(dll));
        }

        // A PDB left by an earlier build is replaced when ilasm writes a new one.
        [Fact]
        public static void Native_StalePdb_IsReplacedWhenAPdbIsWritten() => StalePdb_IsReplacedWhenAPdbIsWritten(Native);

        [ConditionalFact(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        public static void Managed_StalePdb_IsReplacedWhenAPdbIsWritten() => StalePdb_IsReplacedWhenAPdbIsWritten(Managed);

        private static void StalePdb_IsReplacedWhenAPdbIsWritten(string kind)
        {
            string directory = CreateOutputDirectory(nameof(StalePdb_IsReplacedWhenAPdbIsWritten), kind);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");
            string pdb = Path.Combine(directory, "TestPdbFile1.pdb");
            File.WriteAllBytes(pdb, new byte[] { 0xDE, 0xAD });

            Assert.Equal(0, RunIlasm(kind, "TestPdbFile1.il", dll, "-debug"));

            using PEReader pe = ReadImage(dll);
            Assert.True(pe.TryOpenAssociatedPortablePdb(dll, path => File.OpenRead(path), out MetadataReaderProvider pdbProvider, out string pdbPath));
            pdbProvider.Dispose();
            Assert.Equal(pdb, pdbPath);
        }

        // Without a PDB switch, a successful build deletes the PDB of the image it replaces.
        // Managed only: native ilasm deletes <output>.PDB, which on a case-sensitive file system is not this file.
        [ConditionalFact(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        public static void Managed_StalePdb_IsDeletedWhenItsImageIsReplacedWithoutAPdb()
        {
            string directory = CreateOutputDirectory(nameof(Managed_StalePdb_IsDeletedWhenItsImageIsReplacedWithoutAPdb), Managed);
            string dll = Path.Combine(directory, "TestPdbFile1.dll");
            string pdb = Path.Combine(directory, "TestPdbFile1.pdb");
            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", dll, "-debug"));
            Assert.True(File.Exists(pdb));

            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", dll, string.Empty));

            Assert.True(File.Exists(dll));
            Assert.False(File.Exists(pdb));
        }

        // Without a PDB switch, a PDB beside an output that did not exist before belongs to no image ilasm replaced,
        // so it is kept.
        // Managed only: native ilasm deletes <output>.PDB whether or not it belongs to the replaced image.
        [ConditionalFact(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        public static void Managed_UnrelatedPdb_IsKeptWhenNoImageIsReplaced()
        {
            string directory = CreateOutputDirectory(nameof(Managed_UnrelatedPdb_IsKeptWhenNoImageIsReplaced), Managed);
            string otherDll = Path.Combine(directory, "Other.dll");
            string dll = Path.Combine(directory, "TestPdbFile1.dll");
            string pdb = Path.Combine(directory, "TestPdbFile1.pdb");
            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", otherDll, "-debug"));
            File.Move(Path.Combine(directory, "Other.pdb"), pdb);
            byte[] unrelatedPdb = File.ReadAllBytes(pdb);

            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", dll, string.Empty));

            Assert.Equal(unrelatedPdb, File.ReadAllBytes(pdb));
        }

        // When assembly fails, the existing output and PDB are left as they were: after an error in the source
        // (TestPdbFileError.il) and after an exception (TestPdbFileMissingInclude.il, whose include is missing).
        // Managed only: native ilasm deletes <output>.PDB when assembly fails.
        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData("TestPdbFileError.il", "")]
        [InlineData("TestPdbFileError.il", "-debug")]
        [InlineData("TestPdbFileMissingInclude.il", "-debug")]
        public static void Managed_FailedBuild_LeavesTheExistingOutputAndPdbUnchanged(string ilSource, string switches)
        {
            string name = Path.GetFileNameWithoutExtension(ilSource);
            string directory = CreateOutputDirectory($"{nameof(Managed_FailedBuild_LeavesTheExistingOutputAndPdbUnchanged)}-{name}{switches}", Managed);
            string dll = Path.Combine(directory, name + ".dll");
            string pdb = Path.Combine(directory, name + ".pdb");
            byte[] existingDll = { 0x4D, 0x5A };
            byte[] existingPdb = { 0xDE, 0xAD };
            File.WriteAllBytes(dll, existingDll);
            File.WriteAllBytes(pdb, existingPdb);

            Assert.NotEqual(0, RunIlasm(Managed, ilSource, dll, switches));

            Assert.Equal(existingDll, File.ReadAllBytes(dll));
            Assert.Equal(existingPdb, File.ReadAllBytes(pdb));
        }

        // With -ERR, an image written despite errors is handled as after a successful build: with -DEBUG its PDB is
        // written beside it, and without a PDB switch the PDB of the image it replaces is deleted.
        // Managed only: native ilasm deletes <output>.PDB, which on a case-sensitive file system is not this file.
        [ConditionalTheory(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        [InlineData("-err -debug")]
        [InlineData("-err")]
        public static void Managed_ErrorTolerantBuildWithErrors_HandlesThePdbAsASuccessfulBuildDoes(string switches)
        {
            string directory = CreateOutputDirectory(nameof(Managed_ErrorTolerantBuildWithErrors_HandlesThePdbAsASuccessfulBuildDoes) + switches.Replace(" ", string.Empty), Managed);
            string dll = Path.Combine(directory, "TestPdbFileRecoverableError.dll");
            string pdb = Path.Combine(directory, "TestPdbFileRecoverableError.pdb");
            Assert.Equal(0, RunIlasm(Managed, "TestPdbFile1.il", dll, "-debug"));
            Assert.NotEqual(0, RunIlasm(Managed, "TestPdbFileRecoverableError.il", dll, "-debug"));

            Assert.Equal(0, RunIlasm(Managed, "TestPdbFileRecoverableError.il", dll, switches));

            using PEReader pe = ReadImage(dll);
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal("TestPdbFileRecoverableError", reader.GetString(reader.GetAssemblyDefinition().Name));
            if (switches.Contains("-debug"))
            {
                Assert.True(pe.TryOpenAssociatedPortablePdb(dll, path => File.OpenRead(path), out MetadataReaderProvider pdbProvider, out string pdbPath));
                pdbProvider.Dispose();
                Assert.Equal(pdb, pdbPath);
            }
            else
            {
                Assert.False(File.Exists(pdb));
            }
        }

        // An output named like its PDB is refused rather than overwritten by the PDB.
        // Managed only: native ilasm writes the image and then overwrites it with the PDB.
        [ConditionalFact(typeof(IlasmPdbFileTester), nameof(HasManagedIlasm))]
        public static void Managed_OutputNamedLikeItsPdb_IsRefused()
        {
            string directory = CreateOutputDirectory(nameof(Managed_OutputNamedLikeItsPdb_IsRefused), Managed);
            string output = Path.Combine(directory, "TestPdbFile1.pdb");

            Assert.NotEqual(0, RunIlasm(Managed, "TestPdbFile1.il", output, "-debug"));

            Assert.False(File.Exists(output));
        }
    }
}
