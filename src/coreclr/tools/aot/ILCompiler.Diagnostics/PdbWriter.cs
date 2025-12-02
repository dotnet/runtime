// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Internal.TypeSystem;
using Microsoft.DiaSymReader;

namespace ILCompiler.Diagnostics
{
    // NGEN always generates PDBs with public symbols lists (so tools can map IP ranges to
    // methods).  This bitmask indicates what extra info should be added to the PDB
    [Flags]
    public enum PDBExtraData
    {
        None = 0,
        // Add string table subsection, files checksum subsection, and lines subsection to
        // allow tools to map IP ranges to source lines.
        kPDBLines  = 0x00000001,
    };

    public enum SymChecksumType : byte
    {
        None = 0,        // indicates no checksum is available
        MD5,
        SHA1,
        SHA_256,
    };

    class SymDocument : IEquatable<SymDocument>
    {
        public string Name;
        public SymChecksumType ChecksumType;
        public byte[] Checksum;


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is SymDocument documentOther)
            {
                return Equals(documentOther);
            }

            return false;
        }

        public bool Equals(SymDocument other)
        {
            if (Name != other.Name)
                return false;
            if (ChecksumType != other.ChecksumType)
                return false;
            if (Checksum.Length != other.Checksum.Length)
                return false;
            for (int i = 0; i < Checksum.Length; i++)
            {
                if (Checksum[i] != other.Checksum[i])
                    return false;
            }

            return true;
        }
    }

    public partial class PdbWriter
    {
        private const string DiaSymReaderLibrary = "Microsoft.DiaSymReader.Native";

        string _pdbPath;
        PDBExtraData _pdbExtraData;
        readonly TargetDetails _target;

        string _pdbFilePath;
        string _tempSourceDllName;

        List<SymDocument> _symDocuments = new List<SymDocument>();
        Dictionary<string,int> _stringTableToOffsetMapping;
        Dictionary<SymDocument,int> _documentToChecksumOffsetMapping;

        UIntPtr _pdbMod;
        ISymNGenWriter2 _ngenWriter;

        static PdbWriter()
        {
            NativeLibrary.SetDllImportResolver(typeof(PdbWriter).Assembly, DllImportResolver);
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            IntPtr libraryHandle = IntPtr.Zero;
            if (libraryName == DiaSymReaderLibrary)
            {
                string archSuffix = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
                if (archSuffix == "x64")
                {
                    archSuffix = "amd64";
                }
                libraryHandle = NativeLibrary.Load(DiaSymReaderLibrary + "." + archSuffix + ".dll", assembly, searchPath);
            }
            return libraryHandle;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [LibraryImport(DiaSymReaderLibrary, StringMarshalling = StringMarshalling.Utf16)]
        private static partial void CreateNGenPdbWriter(
            string ngenImagePath,
            string pdbPath,
            out IntPtr ngenPdbWriterPtr);

        public PdbWriter(string pdbPath, PDBExtraData pdbExtraData, TargetDetails target)
        {
            SymDocument unknownDocument = new SymDocument();
            unknownDocument.Name = "unknown";
            unknownDocument.ChecksumType = SymChecksumType.None;
            unknownDocument.Checksum = Array.Empty<byte>();

            _symDocuments.Add(unknownDocument);
            _pdbPath = pdbPath;
            _pdbExtraData = pdbExtraData;
            _target = target;
        }

        public void WritePDBData(string dllPath, IEnumerable<MethodInfo> methods)
        {
            bool failed = true;
            try
            {
                try
                {
                    WritePDBDataHelper(dllPath, methods);
                }
                finally
                {
                    if (_ngenWriter != null)
                    {
                        if (_pdbMod != UIntPtr.Zero)
                        {
                            _ngenWriter.CloseMod(_pdbMod);
                        }
                        ComObject ngenWriterComObject = (ComObject)(object)_ngenWriter;
                        ngenWriterComObject.FinalRelease();
                    }
                }

                failed = false;
            }
            finally
            {
                if (_tempSourceDllName != null)
                {
                    try
                    {
                        File.Delete(_tempSourceDllName);
                    }
                    catch {}
                }

                if (failed && (_pdbFilePath != null))
                {
                    try
                    {
                        // If anything fails, do not create a partial pdb file
                        File.Delete(_pdbFilePath);
                    }
                    catch {}
                }
            }
        }

        private void WritePDBDataHelper(string dllPath, IEnumerable<MethodInfo> methods)
        {
            // This will try to open the managed PDB if lines info was requested.  This is a
            // likely failure point, so intentionally do this before creating the NGEN PDB file
            // on disk.
            bool isILPDBProvided = false;
            if (_pdbExtraData.HasFlag(PDBExtraData.kPDBLines))
            {
                // line mapping not ported from crossgen yet.
                throw new NotImplementedException();
            }

            string dllNameWithoutExtension = Path.GetFileNameWithoutExtension(dllPath);
            _pdbFilePath = Path.Combine(_pdbPath, dllNameWithoutExtension + ".pdb");

            string originalDllPath = dllPath;

            // Currently DiaSymReader does not work properly generating NGEN PDBS unless
            // the DLL whose PDB is being generated ends in .ni.*.   Unfortunately, readyToRun
            // images do not follow this convention and end up producing bad PDBS.  To fix
            // this (without changing diasymreader.dll which ships indepdendently of .NET Core)
            // we copy the file to something with this convention before generating the PDB
            // and delete it when we are done.
            if (!dllPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase) && !dllPath.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase))
            {
                _tempSourceDllName = Path.Combine(Path.GetDirectoryName(dllPath), dllNameWithoutExtension + ".ni" + Path.GetExtension(dllPath));
                File.Copy(dllPath, _tempSourceDllName, overwrite: true);
                dllPath = _tempSourceDllName;
                _pdbFilePath = Path.Combine(_pdbPath, dllNameWithoutExtension + ".ni.pdb");
            }

            // Delete any preexisting PDB file upfront, otherwise CreateNGenPdbWriter silently opens it
            File.Delete(_pdbFilePath);

            var comWrapper = new StrategyBasedComWrappers();
            CreateNGenPdbWriter(dllPath, _pdbFilePath, out var pdbWriterInst);
            _ngenWriter = (ISymNGenWriter2)comWrapper.GetOrCreateObjectForComInstance(pdbWriterInst, CreateObjectFlags.UniqueInstance);
            Marshal.Release(pdbWriterInst);

            {
                // PDB file is now created. Get its path and update _pdbFilePath so the PDB file
                // can be deleted if we don't make it successfully to the end.
                const int capacity = 1024;
                var pdbFilePathBuilder = new char[capacity];
                _ngenWriter.QueryPDBNameExW(pdbFilePathBuilder, new IntPtr(capacity - 1) /* remove 1 byte for null */);
                int length = 0;
                while (length < pdbFilePathBuilder.Length && pdbFilePathBuilder[length] != '\0')
                {
                    length++;
                }
                _pdbFilePath = new string(pdbFilePathBuilder, 0, length);
            }

            _ngenWriter.OpenModW(originalDllPath, Path.GetFileName(originalDllPath), out _pdbMod);

            WriteCompilerVersion();
            WriteStringTable();
            WriteFileChecksums();

            ushort? iCodeSection = null;
            uint rvaOfTextSection = 0;
            using (var peReader = new PEReader(new FileStream(dllPath, FileMode.Open), PEStreamOptions.Default))
            {
                var sections = peReader.PEHeaders.SectionHeaders;

                for (int i = 0; i < sections.Length; i++)
                {
                    ushort pdbSectionNumber = checked((ushort)(i+1));

                    _ngenWriter.AddSection(pdbSectionNumber, OMF.StandardText, 0, sections[i].SizeOfRawData);
                    if (sections[i].Name == ".text")
                    {
                        iCodeSection = pdbSectionNumber;
                        rvaOfTextSection = (uint)sections[i].VirtualAddress;
                    }
                    _ngenWriter.ModAddSecContribEx(_pdbMod, pdbSectionNumber, 0, sections[i].SizeOfRawData, (uint)sections[i].SectionCharacteristics, 0, 0);
                }
            }

            // To support lines info, we need a "dummy" section, indexed as 0, for use as a
            // sentinel when MSPDB sets up its section contribution table
            _ngenWriter.AddSection(0,           // Dummy section 0
                OMF.SentinelType,
                0,
                unchecked((int)0xFFFFFFFF));

            foreach (var method in methods)
            {
                WriteMethodPDBData(iCodeSection.Value, method, Path.GetFileNameWithoutExtension(originalDllPath), rvaOfTextSection, isILPDBProvided);
            }
        }

        void WriteMethodPDBData(ushort iCodeSection, MethodInfo method, string assemblyName, uint textSectionOffset, bool isILPDBProvided)
        {
            string nameSuffix = $"{method.Name}$#{(assemblyName != method.AssemblyName ? method.AssemblyName : String.Empty)}#{method.MethodToken.ToString("X")}";

            _ngenWriter.AddSymbol(nameSuffix, iCodeSection, method.HotRVA - textSectionOffset);
            if (method.ColdRVA != 0)
            {
                _ngenWriter.AddSymbol($"[COLD] {nameSuffix}", iCodeSection, method.ColdRVA);
            }

            if (isILPDBProvided)
            {
                // line mapping not ported from crossgen yet.
                throw new NotImplementedException();
            }
        }

        private const int CV_SIGNATURE_C13 = 4;
        private enum DEBUG_S_SUBSECTION_TYPE {
            DEBUG_S_IGNORE = unchecked((int)0x80000000),    // if this bit is set in a subsection type then ignore the subsection contents

            DEBUG_S_SYMBOLS = 0xf1,
            DEBUG_S_LINES,
            DEBUG_S_STRINGTABLE,
            DEBUG_S_FILECHKSMS,
            DEBUG_S_FRAMEDATA,
            DEBUG_S_INLINEELINES,
            DEBUG_S_CROSSSCOPEIMPORTS,
            DEBUG_S_CROSSSCOPEEXPORTS,

            DEBUG_S_IL_LINES,
            DEBUG_S_FUNC_MDTOKEN_MAP,
            DEBUG_S_TYPE_MDTOKEN_MAP,
            DEBUG_S_MERGED_ASSEMBLYINPUT,

            DEBUG_S_COFF_SYMBOL_RVA,
        }

        private enum SYM_ENUM : ushort
        {
            S_COMPILE3      =  0x113c,  // Replacement for S_COMPILE2
        }

        private enum CV_CPU_TYPE
        {
            CV_CFL_PENTIUMIII   = 0x07,
            CV_CFL_X64          = 0xD0,
            CV_CFL_ARMNT        = 0xF4,
            CV_CFL_ARM64        = 0xF6,
        }

        private enum CV_CFL_LANG
        {
            CV_CFL_MSIL     = 0x0F,  // Unknown MSIL (LTCG of .NETMODULE)
        }

        private void WriteStringTable()
        {
            _stringTableToOffsetMapping = new Dictionary<string,int>();

            MemoryStream stringTableStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stringTableStream, Encoding.UTF8);
            writer.Write(CV_SIGNATURE_C13);
            writer.Write((uint)DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE);
            long sizeOfStringTablePosition = writer.BaseStream.Position;
            writer.Write((uint)0); // Size of actual string table. To be filled in later
            long startOfStringTableOffset = writer.BaseStream.Position;
            foreach (var document in _symDocuments)
            {
                string str = document.Name;
                if (_stringTableToOffsetMapping.ContainsKey(str))
                    continue;

                long offset = writer.BaseStream.Position;
                _stringTableToOffsetMapping.Add(str, checked((int)(offset - startOfStringTableOffset)));
                writer.Write(str.AsSpan());
                writer.Write((byte)0); // Null terminate all strings
            }

            // Update string table size
            long stringTableSize = writer.BaseStream.Position - startOfStringTableOffset;
            writer.BaseStream.Position = sizeOfStringTablePosition;
            writer.Write(checked((uint)stringTableSize));
            writer.Flush();

            // Write string table into pdb file
            byte[] stringTableArray = stringTableStream.ToArray();
            _ngenWriter.ModAddSymbols(_pdbMod, stringTableArray, stringTableArray.Length);
        }

        private void WriteFileChecksums()
        {
            _documentToChecksumOffsetMapping = new Dictionary<SymDocument,int>();

            MemoryStream checksumStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(checksumStream, Encoding.UTF8);
            writer.Write(CV_SIGNATURE_C13);
            writer.Write((uint)DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS);

            long sizeOfChecksumTablePosition = writer.BaseStream.Position;
            writer.Write((uint)0); // Size of actual checksum table. To be filled in later
            long startOfChecksumTableOffset = writer.BaseStream.Position;
            foreach (var document in _symDocuments)
            {
                long offset = writer.BaseStream.Position;
                _documentToChecksumOffsetMapping.Add(document, checked((int)(offset - startOfChecksumTableOffset)));

                SymChecksumType checksumType = document.ChecksumType;
                byte[] checksum = document.Checksum;

                if (document.Checksum.Length > 255)
                {
                    // Should never happen, but just in case checksum data is invalid, just put
                    // no checksum into the NGEN PDB
                    checksumType = SymChecksumType.None;
                    checksum = Array.Empty<byte>();
                }
                writer.Write(_stringTableToOffsetMapping[document.Name]);
                writer.Write((byte)checksum.Length);
                writer.Write((byte)checksumType);
                writer.Write(checksum);

                // Must align to the next 4-byte boundary
                while ((writer.BaseStream.Position % 4) != 0)
                {
                    writer.Write((byte)0);
                }
            }

            // Update checksum table size
            long checksumTableSize = writer.BaseStream.Position - startOfChecksumTableOffset;
            writer.BaseStream.Position = sizeOfChecksumTablePosition;
            writer.Write(checked((uint)checksumTableSize));
            writer.Flush();

            // Write string table into pdb file
            byte[] checksumTableArray = checksumStream.ToArray();
            _ngenWriter.ModAddSymbols(_pdbMod, checksumTableArray, checksumTableArray.Length);
        }

        private void WriteCompilerVersion()
        {
            // The only symbol we write into the DEBUG_S_SYMBOLS stream is the compiler version.
            // Other symbols are represented as "public" symbols which are something else entirely.

            MemoryStream symbolStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(symbolStream, Encoding.UTF8);
            writer.Write(CV_SIGNATURE_C13);
            writer.Write((uint)DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS);
            long startOfSymbolTablePosition = writer.BaseStream.Position;
            writer.Write((uint)0); // Size of actual symbol table. To be filled in later
            long startOfSymbolTableOffset = writer.BaseStream.Position;

            {
                long startOfCompile3RecordLength = writer.BaseStream.Position;
                writer.Write((ushort)0); // Write record length. Fill in later
                long startOfCompile3Record = writer.BaseStream.Position;
                writer.Write((ushort)SYM_ENUM.S_COMPILE3);
                byte iLanguage = (byte)CV_CFL_LANG.CV_CFL_MSIL;
                writer.Write(iLanguage);
                // Write rest of flags
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);

                switch (_target.Architecture)
                {
                    case TargetArchitecture.ARM:
                        writer.Write((ushort)CV_CPU_TYPE.CV_CFL_ARMNT);
                        break;
                    case TargetArchitecture.ARM64:
                        writer.Write((ushort)CV_CPU_TYPE.CV_CFL_ARM64);
                        break;
                    case TargetArchitecture.X64:
                        writer.Write((ushort)CV_CPU_TYPE.CV_CFL_X64);
                        break;
                    case TargetArchitecture.X86:
                        writer.Write((ushort)CV_CPU_TYPE.CV_CFL_PENTIUMIII);
                        break;
                    default:
                        throw new Exception("Unknown target architecture");
                }

                writer.Write((ushort)0); // Front end Major Version
                writer.Write((ushort)0); // Front end Minor Version
                writer.Write((ushort)0); // Front end Build Version
                writer.Write((ushort)0); // Front end QFE Version

                Version compilerVersion = null;
                foreach (AssemblyFileVersionAttribute versionAttribute in typeof(PdbWriter).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true))
                {
                    string versionString = versionAttribute.Version;
                    compilerVersion = new Version(versionString);
                }
                if (compilerVersion == null)
                {
                    throw new Exception("No AssemblyFileVersionAttribute present");
                }

                writer.Write((ushort)compilerVersion.Major); // Front end Major Version
                writer.Write((ushort)compilerVersion.Minor); // Front end Minor Version
                writer.Write((ushort)compilerVersion.Build); // Front end Build Version
                writer.Write((ushort)compilerVersion.Revision); // Front end QFE Version

                // compiler version string
                string informationalVersion = null;
                foreach (AssemblyInformationalVersionAttribute versionAttribute in typeof(PdbWriter).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true))
                {
                    informationalVersion = versionAttribute.InformationalVersion;
                }

                if (informationalVersion == null)
                {
                    throw new Exception("No AssemblyInformationalVersionAttribute present");
                }

                string CompilerVersionString = $"Crossgen2 - {informationalVersion}";
                writer.Write(CompilerVersionString.AsSpan());
                writer.Write((byte)0); // Null terminate all strings

                // Must align to the next 4-byte boundary
                while ((writer.BaseStream.Position % 4) != 0)
                {
                    writer.Write((byte)0);
                }

                // Update Compile3 record size
                long currentPosition = writer.BaseStream.Position;
                long compile3RecordSize = writer.BaseStream.Position - startOfCompile3Record;
                writer.BaseStream.Position = startOfCompile3RecordLength;
                writer.Write(checked((ushort)compile3RecordSize));
                writer.Flush();
                writer.BaseStream.Position = currentPosition;
            }

            // Update symbol table size
            long symbolTableSize = writer.BaseStream.Position - startOfSymbolTableOffset;
            writer.BaseStream.Position = startOfSymbolTablePosition;
            writer.Write(checked((uint)symbolTableSize));
            writer.Flush();

            // Write symbol table into pdb file
            byte[] symbolTableArray = symbolStream.ToArray();
            _ngenWriter.ModAddSymbols(_pdbMod, symbolTableArray, symbolTableArray.Length);
        }
    }
}
