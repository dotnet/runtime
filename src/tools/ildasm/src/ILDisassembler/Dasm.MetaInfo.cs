// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ILDisassembler;

// /METADATA[=<specifier>]: the MetaInfo dump (MDInfo::DisplayMD in src/coreclr/tools/metainfo). MetaInfo reads the metadata through the public
// IMetaDataImport/IMetaDataTables interfaces of the read-write metadata engine, so the lookups below mirror what that
// engine does on the raw tables (binary searches on sorted tables, linear scans, table pointer indirections) rather
// than what System.Reflection.Metadata offers.
internal sealed partial class Dasm
{
    private void DumpMetaInfo()
    {
        uint filter = 0;
        foreach (string spec in _options.MetadataFilters)
        {
            // The specifier is recognized by its first 3 characters.
            string s = spec.Length >= 3 ? spec.Substring(0, 3).ToLowerInvariant() : spec.ToLowerInvariant();
            filter |= s switch
            {
                "hex" => MetaInfo.DumpMoreHex,
                "csv" => MetaInfo.DumpCSV,
                "mdh" => MetaInfo.DumpHeader,
                "raw" => MetaInfo.DumpRaw,
                "hea" => MetaInfo.DumpRawHeaps,
                "sch" => MetaInfo.DumpSchema,
                "unr" => MetaInfo.DumpUnsat,
                "val" => MetaInfo.DumpValidate,
                "sta" => MetaInfo.DumpStats,
                _ => 0u,
            };
        }

        MetaInfo mi = new(this, filter);
        PrintLine("// ================================= M E T A I N F O ================================================\n");
        mi.DisplayMD();
        PrintLine("// ================================= END OF METAINFO ================================================\n");
    }

    private sealed class MetaInfo
    {
        public const uint DumpSchema = 0x00000002;
        public const uint DumpRaw = 0x00000004;
        public const uint DumpHeader = 0x00000008;
        public const uint DumpCSV = 0x00000010;
        public const uint DumpUnsat = 0x00000020;
        public const uint DumpStats = 0x00000080;
        public const uint DumpMoreHex = 0x00000100;
        public const uint DumpValidate = 0x00000200;
        public const uint DumpRawHeaps = 0x00000400;

        private const string Separator = "===========================================================";
        private const string Dashes = "-------------------------------------------------------";

        // Name buffer sizes, in UTF-16 characters (STRING_BUFFER_LEN and MAX_CLASS_NAME).
        private const int StringBufferLen = 4096;
        private const int MaxClassName = 1024;

        private const uint TypeDefModule = 0x02000001;

        private readonly Dasm _dasm;
        private readonly uint _filter;
        private readonly byte[] _d;
        private readonly uint _entryPoint;
        private ulong _coffNames;

        // DumpMI keeps the text of the line being built in a buffer that starts with "// ".
        private readonly StringBuilder _line = new("// ");

        public MetaInfo(Dasm dasm, uint filter)
        {
            _dasm = dasm;
            _filter = filter;
            _d = dasm._metadataBlock.GetContent().AsSpan().ToArray();
            _entryPoint = (uint)(dasm._pe.PEHeaders.CorHeader?.EntryPointTokenOrRelativeVirtualAddress ?? 0);
            LoadMetadata();
        }

        // ================================================================ output (DumpMI)

        // DumpMI appends the text and prints the buffer up to the first newline, keeping the rest for later: only one
        // line goes out per call, so text with several newlines lags behind, and what is left at the end is lost.
        private void Write(string s)
        {
            int nul = s.IndexOf('\0');
            if (nul >= 0)
            {
                s = s.Substring(0, nul);
            }

            _line.Append(s);
            for (int i = 0; i < _line.Length; i++)
            {
                if (_line[i] == '\n')
                {
                    _dasm.PrintLine(_line.ToString(0, i));
                    string rest = _line.ToString(i + 1, _line.Length - i - 1);
                    _line.Clear().Append("// ").Append(rest);
                    break;
                }
            }
        }

        private void WriteLine(string s)
        {
            Write(s);
            Write("\n");
        }

        // ================================================================ formatting helpers

        private static string D(uint v) => ((int)v).ToString(CultureInfo.InvariantCulture);

        private static string D(int v) => v.ToString(CultureInfo.InvariantCulture);

        private static string U(uint v) => v.ToString(CultureInfo.InvariantCulture);

        private static string Hx(uint v) => v.ToString("x", CultureInfo.InvariantCulture);

        // printf("%#x")
        private static string AltHex(uint v) => v == 0 ? "0" : "0x" + Hx(v);

        // printf("%#08x")
        private static string AltHex08(uint v) => v == 0 ? "00000000" : "0x" + Hx(v).PadLeft(6, '0');

        private static string PadL(string s, int w) => s.Length >= w ? s : s.PadLeft(w);

        private static string PadR(string s, int w) => s.Length >= w ? s : s.PadRight(w);

        // Names are converted to UTF-16 into fixed size buffers and back to UTF-8.
        private static string Fit(string s, int cch) => s.Length < cch ? s : s.Substring(0, cch - 1);

        private static string Utf8(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetString(bytes);

        // ================================================================ raw metadata

        private sealed class Stream
        {
            public string Name = "";
            public int Offset;
            public int Size;
        }

        private readonly List<Stream> _streams = new();
        private int _tablesStream = -1;
        private int _tablesStreamSize;

        // Heaps: Base is the offset in _d (or in the private zero buffer when the heap is missing), Size the size the
        // read-write engine reports.
        private struct Heap
        {
            public byte[] Data;
            public int Base;
            public int Size;
        }

        private Heap _strings;
        private Heap _blobs;
        private Heap _guids;
        private Heap _userStrings;

        private int _tableCount;
        private byte _heapSizes;
        private readonly uint[] _rows = new uint[64];
        private readonly int[] _tableOffset = new int[64];
        private readonly int[] _cbRec = new int[64];
        private readonly int[][] _colOffset = new int[TblCount][];
        private readonly int[][] _colSize = new int[TblCount][];

        private const int TblCount = 54;
        private const int TblCountV1 = 42;

        // Column types (see metamodelpub.h): 0..63 a RID into that table, 64..95 a coded token, then fixed types.
        private const byte CT = 64;
        private const byte iSHORT = 96;
        private const byte iUSHORT = 97;
        private const byte iLONG = 98;
        private const byte iULONG = 99;
        private const byte iBYTE = 100;
        private const byte iSTRING = 101;
        private const byte iGUID = 102;
        private const byte iBLOB = 103;

        private const int TblModule = 0, TblTypeRef = 1, TblTypeDef = 2, TblFieldPtr = 3, TblField = 4, TblMethodPtr = 5, TblMethod = 6,
            TblParamPtr = 7, TblParam = 8, TblInterfaceImpl = 9, TblMemberRef = 10, TblConstant = 11, TblCustomAttribute = 12,
            TblFieldMarshal = 13, TblDeclSecurity = 14, TblClassLayout = 15, TblFieldLayout = 16, TblStandAloneSig = 17,
            TblEventMap = 18, TblEventPtr = 19, TblEvent = 20, TblPropertyMap = 21, TblPropertyPtr = 22, TblProperty = 23,
            TblMethodSemantics = 24, TblMethodImpl = 25, TblModuleRef = 26, TblTypeSpec = 27, TblImplMap = 28, TblFieldRVA = 29,
            TblAssembly = 32, TblAssemblyRef = 35, TblFile = 38, TblExportedType = 39, TblManifestResource = 40,
            TblNestedClass = 41, TblGenericParam = 42, TblMethodSpec = 43, TblGenericParamConstraint = 44;

        private const int CdTypeDefOrRef = 0, CdHasConstant = 1, CdHasCustomAttribute = 2, CdHasFieldMarshal = 3, CdHasDeclSecurity = 4,
            CdMemberRefParent = 5, CdHasSemantic = 6, CdMethodDefOrRef = 7, CdMemberForwarded = 8, CdImplementation = 9,
            CdCustomAttributeType = 10, CdResolutionScope = 11, CdTypeOrMethodDef = 12;

        private sealed record TableDef(string Name, string[] ColNames, byte[] ColTypes, int Key);

        private static TableDef T(string name, int key, params (string Name, byte Type)[] cols)
        {
            string[] names = new string[cols.Length];
            byte[] types = new byte[cols.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                names[i] = cols[i].Name;
                types[i] = cols[i].Type;
            }

            return new TableDef(name, names, types, key);
        }

        private static readonly TableDef[] s_tables =
        [
            T("Module", -1, ("Generation", iUSHORT), ("Name", iSTRING), ("Mvid", iGUID), ("EncId", iGUID), ("EncBaseId", iGUID)),
            T("TypeRef", -1, ("ResolutionScope", CT + CdResolutionScope), ("Name", iSTRING), ("Namespace", iSTRING)),
            T("TypeDef", -1, ("Flags", iULONG), ("Name", iSTRING), ("Namespace", iSTRING), ("Extends", CT + CdTypeDefOrRef), ("FieldList", TblField), ("MethodList", TblMethod)),
            T("FieldPtr", -1, ("Field", TblField)),
            T("Field", -1, ("Flags", iUSHORT), ("Name", iSTRING), ("Signature", iBLOB)),
            T("MethodPtr", -1, ("Method", TblMethod)),
            T("Method", -1, ("RVA", iULONG), ("ImplFlags", iUSHORT), ("Flags", iUSHORT), ("Name", iSTRING), ("Signature", iBLOB), ("ParamList", TblParam)),
            T("ParamPtr", -1, ("Param", TblParam)),
            T("Param", -1, ("Flags", iUSHORT), ("Sequence", iUSHORT), ("Name", iSTRING)),
            T("InterfaceImpl", 0, ("Class", TblTypeDef), ("Interface", CT + CdTypeDefOrRef)),
            T("MemberRef", -1, ("Class", CT + CdMemberRefParent), ("Name", iSTRING), ("Signature", iBLOB)),
            T("Constant", 1, ("Type", iBYTE), ("Parent", CT + CdHasConstant), ("Value", iBLOB)),
            T("CustomAttribute", 0, ("Parent", CT + CdHasCustomAttribute), ("Type", CT + CdCustomAttributeType), ("Value", iBLOB)),
            T("FieldMarshal", 0, ("Parent", CT + CdHasFieldMarshal), ("NativeType", iBLOB)),
            T("DeclSecurity", 1, ("Action", iSHORT), ("Parent", CT + CdHasDeclSecurity), ("PermissionSet", iBLOB)),
            T("ClassLayout", 2, ("PackingSize", iUSHORT), ("ClassSize", iULONG), ("Parent", TblTypeDef)),
            T("FieldLayout", 1, ("OffSet", iULONG), ("Field", TblField)),
            T("StandAloneSig", -1, ("Signature", iBLOB)),
            T("EventMap", -1, ("Parent", TblTypeDef), ("EventList", TblEvent)),
            T("EventPtr", -1, ("Event", TblEvent)),
            T("Event", -1, ("EventFlags", iUSHORT), ("Name", iSTRING), ("EventType", CT + CdTypeDefOrRef)),
            T("PropertyMap", -1, ("Parent", TblTypeDef), ("PropertyList", TblProperty)),
            T("PropertyPtr", -1, ("Property", TblProperty)),
            T("Property", -1, ("PropFlags", iUSHORT), ("Name", iSTRING), ("Type", iBLOB)),
            T("MethodSemantics", 2, ("Semantic", iUSHORT), ("Method", TblMethod), ("Association", CT + CdHasSemantic)),
            T("MethodImpl", 0, ("Class", TblTypeDef), ("MethodBody", CT + CdMethodDefOrRef), ("MethodDeclaration", CT + CdMethodDefOrRef)),
            T("ModuleRef", -1, ("Name", iSTRING)),
            T("TypeSpec", -1, ("Signature", iBLOB)),
            T("ImplMap", 1, ("MappingFlags", iUSHORT), ("MemberForwarded", CT + CdMemberForwarded), ("ImportName", iSTRING), ("ImportScope", TblModuleRef)),
            T("FieldRVA", 1, ("RVA", iULONG), ("Field", TblField)),
            T("ENCLog", -1, ("Token", iULONG), ("FuncCode", iULONG)),
            T("ENCMap", -1, ("Token", iULONG)),
            T("Assembly", -1, ("HashAlgId", iULONG), ("MajorVersion", iUSHORT), ("MinorVersion", iUSHORT), ("BuildNumber", iUSHORT), ("RevisionNumber", iUSHORT), ("Flags", iULONG), ("PublicKey", iBLOB), ("Name", iSTRING), ("Locale", iSTRING)),
            T("AssemblyProcessor", -1, ("Processor", iULONG)),
            T("AssemblyOS", -1, ("OSPlatformId", iULONG), ("OSMajorVersion", iULONG), ("OSMinorVersion", iULONG)),
            T("AssemblyRef", -1, ("MajorVersion", iUSHORT), ("MinorVersion", iUSHORT), ("BuildNumber", iUSHORT), ("RevisionNumber", iUSHORT), ("Flags", iULONG), ("PublicKeyOrToken", iBLOB), ("Name", iSTRING), ("Locale", iSTRING), ("HashValue", iBLOB)),
            T("AssemblyRefProcessor", -1, ("Processor", iULONG), ("AssemblyRef", TblAssemblyRef)),
            T("AssemblyRefOS", -1, ("OSPlatformId", iULONG), ("OSMajorVersion", iULONG), ("OSMinorVersion", iULONG), ("AssemblyRef", TblAssemblyRef)),
            T("File", -1, ("Flags", iULONG), ("Name", iSTRING), ("HashValue", iBLOB)),
            T("ExportedType", -1, ("Flags", iULONG), ("TypeDefId", iULONG), ("TypeName", iSTRING), ("TypeNamespace", iSTRING), ("Implementation", CT + CdImplementation)),
            T("ManifestResource", -1, ("Offset", iULONG), ("Flags", iULONG), ("Name", iSTRING), ("Implementation", CT + CdImplementation)),
            T("NestedClass", 0, ("NestedClass", TblTypeDef), ("EnclosingClass", TblTypeDef)),
            T("GenericParam", 2, ("Number", iUSHORT), ("Flags", iUSHORT), ("Owner", CT + CdTypeOrMethodDef), ("Name", iSTRING)),
            T("MethodSpec", -1, ("Method", CT + CdMethodDefOrRef), ("Instantiation", iBLOB)),
            T("GenericParamConstraint", 0, ("Owner", TblGenericParam), ("Constraint", CT + CdTypeDefOrRef)),
            T("Dummy1", -1, ("", TblModule)),
            T("Dummy2", -1, ("", TblModule)),
            T("Dummy3", -1, ("", TblModule)),
            T("Document", -1, ("Name", iBLOB), ("HashAlgorithm", iGUID), ("Hash", iBLOB), ("Language", iGUID)),
            T("MethodDebugInformation", -1, ("Document", 48), ("SequencePoints", iBLOB)),
            T("LocalScope", -1, ("Method", TblMethod), ("ImportScope", 53), ("VariableList", 51), ("ConstantList", 52), ("StartOffset", iULONG), ("Length", iULONG)),
            T("LocalVariable", -1, ("Attributes", iUSHORT), ("Index", iUSHORT), ("Name", iSTRING)),
            T("LocalConstant", -1, ("Name", iSTRING), ("Signature", iBLOB)),
            T("ImportScope", -1, ("Parent", 53), ("Imports", iBLOB)),
        ];

        private static readonly string[] s_codedTokenNames =
        [
            "TypeDefOrRef", "HasConstant", "HasCustomAttribute", "HasFieldMarshal", "HasDeclSecurity", "MemberRefParent",
            "HasSemantic", "MethodDefOrRef", "MemberForwarded", "Implementation", "CustomAttributeType", "ResolutionScope",
            "TypeOrMethodDef",
        ];

        private static readonly uint[][] s_codedTokens =
        [
            [mdtTypeDef, mdtTypeRef, mdtTypeSpec],
            [mdtFieldDef, mdtParamDef, mdtProperty],
            [
                mdtMethodDef, mdtFieldDef, mdtTypeRef, mdtTypeDef, mdtParamDef, mdtInterfaceImpl, mdtMemberRef, mdtModule,
                mdtPermission, mdtProperty, mdtEvent, mdtSignature, mdtModuleRef, mdtTypeSpec, mdtAssembly, mdtAssemblyRef,
                mdtFile, mdtExportedType, mdtManifestResource, mdtGenericParam, mdtGenericParamConstraint, mdtMethodSpec, 0, 0,
            ],
            [mdtFieldDef, mdtParamDef],
            [mdtTypeDef, mdtMethodDef, mdtAssembly],
            [mdtTypeDef, mdtTypeRef, mdtModuleRef, mdtMethodDef, mdtTypeSpec],
            [mdtEvent, mdtProperty],
            [mdtMethodDef, mdtMemberRef],
            [mdtFieldDef, mdtMethodDef],
            [mdtFile, mdtAssemblyRef, mdtExportedType],
            [0, 0, mdtMethodDef, mdtMemberRef, 0],
            [mdtModule, mdtModuleRef, mdtAssemblyRef, mdtTypeRef],
            [mdtTypeDef, mdtMethodDef],
        ];

        private static readonly int[] s_codedBits = [0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5];

        private const uint mdtModule = 0x00000000;
        private const uint mdtTypeRef = 0x01000000;
        private const uint mdtTypeDef = 0x02000000;
        private const uint mdtFieldDef = 0x04000000;
        private const uint mdtMethodDef = 0x06000000;
        private const uint mdtParamDef = 0x08000000;
        private const uint mdtInterfaceImpl = 0x09000000;
        private const uint mdtMemberRef = 0x0a000000;
        private const uint mdtCustomAttribute = 0x0c000000;
        private const uint mdtPermission = 0x0e000000;
        private const uint mdtSignature = 0x11000000;
        private const uint mdtEvent = 0x14000000;
        private const uint mdtProperty = 0x17000000;
        private const uint mdtModuleRef = 0x1a000000;
        private const uint mdtTypeSpec = 0x1b000000;
        private const uint mdtAssembly = 0x20000000;
        private const uint mdtAssemblyRef = 0x23000000;
        private const uint mdtFile = 0x26000000;
        private const uint mdtExportedType = 0x27000000;
        private const uint mdtManifestResource = 0x28000000;
        private const uint mdtGenericParam = 0x2a000000;
        private const uint mdtMethodSpec = 0x2b000000;
        private const uint mdtGenericParamConstraint = 0x2c000000;
        private const uint mdtString = 0x70000000;
        private const uint mdtBaseType = 0x72000000;

        private static uint TypeOf(uint tk) => tk & 0xFF000000;

        private static uint RidOf(uint tk) => tk & 0x00FFFFFF;

        private uint U8At(long off) => off >= 0 && off < _d.Length ? _d[off] : 0u;

        private uint U16At(long off) => U8At(off) | U8At(off + 1) << 8;

        private uint U32At(long off) => U16At(off) | U16At(off + 2) << 16;

        private ulong U64At(long off) => U32At(off) | (ulong)U32At(off + 4) << 32;

        private void LoadMetadata()
        {
            int verLen = (int)U32At(12);
            int p = 16 + verLen;
            int count = (int)U16At(p + 2);
            p += 4;
            for (int i = 0; i < count; i++)
            {
                Stream s = new() { Offset = (int)U32At(p), Size = (int)U32At(p + 4) };
                int n = p + 8;
                int e = n;
                while (e < _d.Length && _d[e] != 0)
                {
                    e++;
                }

                s.Name = Encoding.Latin1.GetString(_d, n, e - n);
                p = (e + 4) & ~3;
                _streams.Add(s);
            }

            Heap Find(string name, out bool found)
            {
                foreach (Stream s in _streams)
                {
                    if (s.Name == name)
                    {
                        found = true;
                        return new Heap { Data = _d, Base = s.Offset, Size = s.Size };
                    }
                }

                found = false;
                return default;
            }

            _strings = Find("#Strings", out bool hasStrings);
            if (hasStrings && _strings.Size > 0)
            {
                // StgStringPool::InitOnMem trims the padding, keeping one terminator.
                int size = _strings.Size;
                while (size > 1 && HeapByte(_strings, size - 1) == 0 && HeapByte(_strings, size - 2) == 0)
                {
                    size--;
                }

                _strings.Size = size;
            }
            else
            {
                _strings = new Heap { Data = new byte[8], Base = 0, Size = 1 };
            }

            _blobs = Find("#Blob", out bool hasBlobs);
            if (!hasBlobs || _blobs.Size == 0)
            {
                _blobs = new Heap { Data = new byte[8], Base = 0, Size = 1 };
            }

            _userStrings = Find("#US", out bool hasUserStrings);
            if (!hasUserStrings || _userStrings.Size == 0)
            {
                _userStrings = new Heap { Data = new byte[8], Base = 0, Size = 1 };
            }

            _guids = Find("#GUID", out bool hasGuids);
            if (!hasGuids)
            {
                _guids = new Heap { Data = new byte[8], Base = 0, Size = 0 };
            }

            foreach (Stream s in _streams)
            {
                if (s.Name is "#~" or "#-")
                {
                    _tablesStream = s.Offset;
                    _tablesStreamSize = s.Size;
                    break;
                }
            }

            if (_tablesStream < 0)
            {
                return;
            }

            int t = _tablesStream;
            uint major = U8At(t + 4);
            uint minor = U8At(t + 5);
            _heapSizes = (byte)U8At(t + 6);
            ulong valid = U64At(t + 8);
            _tableCount = major == 1 && minor == 0 ? TblCountV1 : TblCount;
            int q = t + 24;
            for (int i = 0; i < 64; i++)
            {
                if ((valid & (1UL << i)) != 0)
                {
                    if (i < TblCount)
                    {
                        _rows[i] = U32At(q);
                    }

                    q += 4;
                }
            }

            if ((_heapSizes & 0x40) != 0)
            {
                q += 4;
            }

            for (int i = 0; i < TblCount; i++)
            {
                TableDef def = s_tables[i];
                int cols = def.ColTypes.Length;
                _colOffset[i] = new int[cols];
                _colSize[i] = new int[cols];
                int offset = 0;
                for (int c = 0; c < cols; c++)
                {
                    byte type = def.ColTypes[c];
                    int size;
                    if (type < CT)
                    {
                        size = _rows[type] > ushort.MaxValue ? 4 : 2;
                    }
                    else if (type < iSHORT)
                    {
                        uint[] tokens = s_codedTokens[type - CT];
                        uint max = 0;
                        foreach (uint tk in tokens)
                        {
                            if (tk != mdtString)
                            {
                                max = Math.Max(max, _rows[tk >> 24]);
                            }
                        }

                        size = ((ulong)max << s_codedBits[tokens.Length]) > ushort.MaxValue ? 4 : 2;
                    }
                    else
                    {
                        size = type switch
                        {
                            iBYTE => 1,
                            iSHORT or iUSHORT => 2,
                            iLONG or iULONG => 4,
                            iSTRING => (_heapSizes & 0x01) != 0 ? 4 : 2,
                            iGUID => (_heapSizes & 0x02) != 0 ? 4 : 2,
                            _ => (_heapSizes & 0x04) != 0 ? 4 : 2,
                        };
                    }

                    _colOffset[i][c] = offset;
                    _colSize[i][c] = size;
                    offset += size + (size & 1);
                }

                _cbRec[i] = offset;
                if (i < _tableCount)
                {
                    _tableOffset[i] = q;
                    q += offset * (int)_rows[i];
                }
            }
        }

        private static uint HeapByte(Heap h, int index)
            => index >= 0 && h.Base + index < h.Data.Length ? h.Data[h.Base + index] : 0u;

        private uint Rows(int table) => _rows[table];

        // GetCol: the raw value of a column; signed types are sign-extended.
        private uint Col(int table, int col, uint rid)
        {
            long off = _tableOffset[table] + (long)(rid - 1) * _cbRec[table] + _colOffset[table][col];
            uint v = _colSize[table][col] switch
            {
                1 => U8At(off),
                2 => U16At(off),
                _ => U32At(off),
            };
            if (s_tables[table].ColTypes[col] == iSHORT)
            {
                v = (uint)(int)(short)v;
            }

            return v;
        }

        private static uint DecodeToken(uint val, int coded)
        {
            uint[] tokens = s_codedTokens[coded];
            int bits = s_codedBits[tokens.Length];
            uint ix = val & ~(0xFFFFFFFFu << bits);
            if (ix >= tokens.Length)
            {
                return tokens[0];
            }

            return (val >> bits) | tokens[ix];
        }

        private static uint EncodeToken(uint rid, uint type, int coded)
        {
            uint[] tokens = s_codedTokens[coded];
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                if (tokens[ix] == TypeOf(type))
                {
                    return (rid << s_codedBits[tokens.Length]) | (uint)ix;
                }
            }

            return 0;
        }

        // GetToken: a RID column as a token of its table, or a decoded coded token.
        private uint ColToken(int table, int col, uint rid)
        {
            byte type = s_tables[table].ColTypes[col];
            uint v = Col(table, col, rid);
            if (type < CT)
            {
                return v | (uint)type << 24;
            }

            return DecodeToken(v, type - CT);
        }

        private bool IsSorted(int table) => _rows[table] > 0;

        // vSearchTable: a binary search on a key column; the row found is any one of the matching rows.
        private uint SearchTable(int table, int col, uint target)
        {
            int lo = 1;
            int hi = (int)_rows[table];
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                uint val = Col(table, col, (uint)mid);
                if (val == target)
                {
                    return (uint)mid;
                }

                if (val < target || val == 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (table == TblGenericParam && !IsSorted(table))
            {
                for (uint i = 1; i <= _rows[table]; i++)
                {
                    if (Col(table, col, i) == target)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        // SearchTableForMultipleRows: the range [start, end) of the rows with the key, or (0, 0).
        private (uint Start, uint End) SearchRange(int table, int col, uint target)
        {
            uint begin = SearchTable(table, col, target);
            if (begin == 0)
            {
                return (0, 0);
            }

            uint end = begin + 1;
            while (begin > 1 && Col(table, col, begin - 1) == target)
            {
                begin--;
            }

            while (end <= _rows[table] && Col(table, col, end) == target)
            {
                end++;
            }

            return (begin, end);
        }

        // Find<Table>Helper for the tables with one row per parent: a binary search if the table is sorted (any
        // non-empty table is), a linear search otherwise.
        private uint FindByKey(int table, int col, uint target)
        {
            if (IsSorted(table))
            {
                return SearchTable(table, col, target);
            }

            for (uint i = 1; i <= _rows[table]; i++)
            {
                if (Col(table, col, i) == target)
                {
                    return i;
                }
            }

            return 0;
        }

        // Impl_GetEndRidForColumn
        private uint EndRid(int table, uint row, int col, int target)
        {
            if (row < _rows[table])
            {
                uint end = Col(table, col, row + 1);
                if (end != 0)
                {
                    return end;
                }
            }

            return Col(table, col, row) == 0 ? 0 : _rows[target] + 1;
        }

        // GetMethodRid/GetFieldRid/...: the pointer tables, when present, map list indexes to rows.
        private uint Indirect(int ptrTable, uint index) => _rows[ptrTable] > 0 ? Col(ptrTable, 0, index) : index;

        private static List<uint> Range(uint start, uint end, uint type)
        {
            List<uint> list = new();
            for (uint i = start; i < end; i++)
            {
                list.Add(i | type);
            }

            return list;
        }

        // ---------------------------------------------------------------- heaps

        private sealed class MetaInfoFailure : Exception
        {
            public MetaInfoFailure(string message) : base(message)
            {
            }
        }

        // The UTF-8 bytes of a string heap entry, or null if the index is out of the heap.
        private byte[]? StringBytes(uint index)
        {
            if (index >= (uint)_strings.Size)
            {
                return null;
            }

            int start = _strings.Base + (int)index;
            int end = start;
            byte[] data = _strings.Data;
            while (end < data.Length && data[end] != 0)
            {
                end++;
            }

            return data.AsSpan(start, end - start).ToArray();
        }

        private string Str(uint index)
        {
            byte[]? b = StringBytes(index);
            if (b is null)
            {
                throw new MetaInfoFailure("string");
            }

            return Utf8(b);
        }

        private string StrCol(int table, int col, uint rid) => Str(Col(table, col, rid));

        // ns::MakePath
        private static string MakePath(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;

        // A blob: the offset of its data in Data and its size, or false if it cannot be read.
        private bool TryBlob(Heap heap, uint index, out int offset, out int size, out int prefix)
        {
            offset = 0;
            size = 0;
            prefix = 0;
            if (index == 0)
            {
                return true;
            }

            if (index >= (uint)heap.Size)
            {
                return false;
            }

            int avail = heap.Size - (int)index;
            uint b0 = HeapByte(heap, (int)index);
            uint len;
            if ((b0 & 0x80) == 0)
            {
                prefix = 1;
                len = b0;
            }
            else if ((b0 & 0xC0) == 0x80)
            {
                prefix = 2;
                len = (b0 & 0x3F) << 8 | HeapByte(heap, (int)index + 1);
            }
            else if ((b0 & 0xE0) == 0xC0)
            {
                prefix = 4;
                len = (b0 & 0x1F) << 24 | HeapByte(heap, (int)index + 1) << 16 | HeapByte(heap, (int)index + 2) << 8 | HeapByte(heap, (int)index + 3);
            }
            else
            {
                return false;
            }

            if (prefix > avail || len > (uint)(avail - prefix))
            {
                return false;
            }

            offset = heap.Base + (int)index + prefix;
            size = (int)len;
            return true;
        }

        // A signature or blob from the blob heap: (offset in _d, size).
        private (int Offset, int Size) Blob(uint index)
        {
            if (!TryBlob(_blobs, index, out int offset, out int size, out _))
            {
                throw new MetaInfoFailure("blob");
            }

            if (index == 0 || _blobs.Data != _d)
            {
                // An empty blob points at zeros.
                return (-1, size);
            }

            return (offset, size);
        }

        private (int Offset, int Size) BlobCol(int table, int col, uint rid) => Blob(Col(table, col, rid));

        // Reads a byte of a blob, past its end too.
        private uint B(int offset, long i) => offset < 0 ? 0u : U8At(offset + i);

        private byte[] BlobBytes(int offset, int size)
        {
            byte[] r = new byte[size];
            for (int i = 0; i < size; i++)
            {
                r[i] = (byte)B(offset, i);
            }

            return r;
        }

        // printf("%s") of a pointer into the metadata: the bytes up to the next NUL.
        private string CStringAt(long offset)
        {
            List<byte> bytes = new();
            while (offset >= 0 && offset < _d.Length && _d[offset] != 0)
            {
                bytes.Add(_d[offset]);
                offset++;
            }

            return Utf8(bytes.ToArray());
        }

        private static string CString(byte[] bytes)
        {
            int n = Array.IndexOf(bytes, (byte)0);
            return Utf8(n < 0 ? bytes : bytes.AsSpan(0, n));
        }

        private Guid GetGuid(uint index)
        {
            if (index == 0)
            {
                return Guid.Empty;
            }

            if ((ulong)index * 16 > (ulong)_guids.Size)
            {
                throw new MetaInfoFailure("guid");
            }

            byte[] b = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                b[i] = (byte)HeapByte(_guids, (int)(index - 1) * 16 + i);
            }

            return new Guid(b);
        }

        // ================================================================ DisplayMD

        public void DisplayMD()
        {
            try
            {
                DisplayMDWorker();
            }
            catch (MetaInfoFailure)
            {
                // The failure has been reported; there is nothing more to show here.
            }
        }

        private void DisplayMDWorker()
        {
            WriteLine(Separator);
            if ((_filter & (DumpSchema | DumpHeader | DumpCSV | DumpRaw | DumpStats | DumpRawHeaps)) != 0)
            {
                DisplayRaw();
            }
            else
            {
                DisplayVersionInfo();
                DisplayScopeInfo();
                WriteLine(Separator);
                DisplayGlobalFunctions();
                DisplayGlobalFields();
                DisplayGlobalMemberRefs();
                DisplayTypeDefs();
                DisplayTypeRefs();
                DisplayTypeSpecs();
                DisplayMethodSpecs();
                DisplayModuleRefs();
                DisplaySignatures();
                DisplayAssembly();
                DisplayUserStrings();
                Write("\n\nCoff symbol name overhead:  " + _coffNames.ToString(CultureInfo.InvariantCulture) + "\n");
            }

            WriteLine(Separator);
            if ((_filter & DumpUnsat) != 0)
            {
                DisplayUnsatInfo();
            }

            WriteLine(Separator);
        }

        private void DisplayVersionInfo()
        {
            byte[]? b = StringBytes(1);
            if (b is null)
            {
                throw new MetaInfoFailure("GetString() failed.");
            }

            string s = Utf8(b);
            if (s.StartsWith("Version of runtime against which the binary is built : ", StringComparison.Ordinal))
            {
                WriteLine(s);
            }
        }

        private void DisplayScopeInfo()
        {
            if (Rows(TblModule) == 0)
            {
                throw new MetaInfoFailure("GetScopeProps failed.");
            }

            string name = Fit(StrCol(TblModule, 1, 1), StringBufferLen);
            Guid mvid = GetGuid(Col(TblModule, 2, 1));
            WriteLine("ScopeName : " + name);
            WriteLine("MVID      : " + mvid.ToString("B"));
            DisplayPermissions(0x00000001, "");
            DisplayCustomAttributes(0x00000001, "\t");
        }

        // ---------------------------------------------------------------- names

        private static string TokenTypeName(uint tk) => TypeOf(tk) switch
        {
            mdtTypeDef => "TypeDef",
            mdtInterfaceImpl => "InterfaceImpl",
            mdtMethodDef => "MethodDef",
            mdtFieldDef => "FieldDef",
            mdtTypeRef => "TypeRef",
            mdtMemberRef => "MemberRef",
            mdtCustomAttribute => "CustomAttribute",
            mdtParamDef => "ParamDef",
            mdtProperty => "Property",
            mdtEvent => "Event",
            mdtTypeSpec => "TypeSpec",
            _ => "[UnknownTokenType]",
        };

        private bool ValidRid(int table, uint tk) => RidOf(tk) != 0 && RidOf(tk) <= Rows(table);

        // GetTypeDefProps name: "Namespace.Name", or null if the token is not valid.
        private string? TypeDefFullName(uint td)
        {
            if (td == mdtTypeDef)
            {
                return "";
            }

            if (!ValidRid(TblTypeDef, td))
            {
                return null;
            }

            return MakePath(StrCol(TblTypeDef, 2, RidOf(td)), StrCol(TblTypeDef, 1, RidOf(td)));
        }

        private string? TypeRefFullName(uint tr)
        {
            if (!ValidRid(TblTypeRef, tr))
            {
                return null;
            }

            return MakePath(StrCol(TblTypeRef, 2, RidOf(tr)), StrCol(TblTypeRef, 1, RidOf(tr)));
        }

        private string TypeDefName(uint td) => TypeDefFullName(td) is string s ? Fit(s, StringBufferLen) : "[Invalid TypeDef]";

        private string TypeRefName(uint tr) => TypeRefFullName(tr) is string s ? Fit(s, StringBufferLen) : "[Invalid TypeRef]";

        private string TypeDeforRefName(uint tk)
        {
            if (RidOf(tk) == 0)
            {
                return "";
            }

            return TypeOf(tk) switch
            {
                mdtTypeDef => TypeDefName(tk),
                mdtTypeRef => TypeRefName(tk),
                mdtTypeSpec => "[TypeSpec]",
                _ => "[InvalidReference]",
            };
        }

        // GetMemberProps / GetMemberRefProps names.
        private string MemberName(uint tk)
        {
            int table = TypeOf(tk) == mdtMethodDef ? TblMethod : TblField;
            if (!ValidRid(table, tk))
            {
                throw new MetaInfoFailure("GetMemberProps failed.");
            }

            return Fit(StrCol(table, table == TblMethod ? 3 : 1, RidOf(tk)), StringBufferLen);
        }

        private string MemberRefName(uint tk)
        {
            if (!ValidRid(TblMemberRef, tk))
            {
                throw new MetaInfoFailure("GetMemberRefProps failed.");
            }

            return Fit(StrCol(TblMemberRef, 1, RidOf(tk)), StringBufferLen);
        }

        private string MemberDeforRefName(uint tk)
        {
            if (RidOf(tk) == 0)
            {
                return "";
            }

            return TypeOf(tk) switch
            {
                mdtMethodDef or mdtFieldDef => MemberName(tk),
                mdtMemberRef => MemberRefName(tk),
                _ => "[InvalidReference]",
            };
        }

        // ---------------------------------------------------------------- flags

        private static void Flag(StringBuilder sb, bool set, string name)
        {
            if (set)
            {
                sb.Append('[').Append(name).Append("] ");
            }
        }

        private static string NoneIfEmpty(StringBuilder sb) => sb.Length == 0 ? "[none]" : sb.ToString();

        private static string ClassFlags(uint f)
        {
            StringBuilder sb = new();
            uint vis = f & 7;
            Flag(sb, vis == 0, "NotPublic");
            Flag(sb, vis == 1, "Public");
            Flag(sb, vis == 2, "NestedPublic");
            Flag(sb, vis == 3, "NestedPrivate");
            Flag(sb, vis == 4, "NestedFamily");
            Flag(sb, vis == 5, "NestedAssembly");
            Flag(sb, vis == 6, "NestedFamANDAssem");
            Flag(sb, vis == 7, "NestedFamORAssem");
            uint layout = f & 0x18;
            Flag(sb, layout == 0, "AutoLayout");
            Flag(sb, layout == 0x08, "SequentialLayout");
            Flag(sb, layout == 0x10, "ExplicitLayout");
            Flag(sb, (f & 0x20) == 0, "Class");
            Flag(sb, (f & 0x20) != 0, "Interface");
            Flag(sb, (f & 0x80) != 0, "Abstract");
            Flag(sb, (f & 0x100) != 0, "Sealed");
            Flag(sb, (f & 0x400) != 0, "SpecialName");
            Flag(sb, (f & 0x1000) != 0, "Import");
            Flag(sb, (f & 0x2000) != 0, "Serializable");
            uint fmt = f & 0x30000;
            Flag(sb, fmt == 0, "AnsiClass");
            Flag(sb, fmt == 0x10000, "UnicodeClass");
            Flag(sb, fmt == 0x20000, "AutoClass");
            Flag(sb, (f & 0x100000) != 0, "BeforeFieldInit");
            Flag(sb, (f & 0x200000) != 0, "Forwarder");
            Flag(sb, (f & 0x800) != 0, "RTSpecialName");
            Flag(sb, (f & 0x40000) != 0, "HasSecurity");
            Flag(sb, (f & 0x4000) != 0, "WindowsRuntime");
            return NoneIfEmpty(sb);
        }

        // ---------------------------------------------------------------- methods

        private List<uint> EnumMethods(uint td)
        {
            if (td == mdtTypeDef || td == 0)
            {
                td = TypeDefModule;
            }

            uint rid = RidOf(td);
            if (rid == 0 || rid > Rows(TblTypeDef))
            {
                return new List<uint>();
            }

            uint start = Col(TblTypeDef, 5, rid);
            uint end = EndRid(TblTypeDef, rid, 5, TblMethod);
            List<uint> list = new();
            for (uint i = start; i < end; i++)
            {
                list.Add(Indirect(TblMethodPtr, i) | mdtMethodDef);
            }

            return list;
        }

        private List<uint> EnumFields(uint td)
        {
            if (td == mdtTypeDef || td == 0)
            {
                td = TypeDefModule;
            }

            uint rid = RidOf(td);
            if (rid == 0 || rid > Rows(TblTypeDef))
            {
                return new List<uint>();
            }

            uint start = Col(TblTypeDef, 4, rid);
            uint end = EndRid(TblTypeDef, rid, 4, TblField);
            List<uint> list = new();
            for (uint i = start; i < end; i++)
            {
                list.Add(Indirect(TblFieldPtr, i) | mdtFieldDef);
            }

            return list;
        }

        private void DisplayGlobalFunctions()
        {
            WriteLine("Global functions");
            WriteLine(Dashes);
            DisplayMethods(0);
            WriteLine("");
        }

        private void DisplayGlobalFields()
        {
            WriteLine("Global fields");
            WriteLine(Dashes);
            DisplayFields(0, null);
            WriteLine("");
        }

        private void DisplayGlobalMemberRefs()
        {
            WriteLine("Global MemberRefs");
            WriteLine(Dashes);
            DisplayMemberRefs(0, "");
            WriteLine("");
        }

        private void DisplayMethods(uint td)
        {
            int n = 1;
            foreach (uint md in EnumMethods(td))
            {
                WriteLine("\tMethod #" + D(n++) + " (" + x8(md) + ") " + (md == _entryPoint ? "[ENTRYPOINT]" : ""));
                WriteLine("\t" + Dashes);
                uint flags = DisplayMethodInfo(md);
                DisplayParams(md);
                DisplayCustomAttributes(md, "\t\t");
                DisplayPermissions(md, "\t");
                DisplayMemberRefs(md, "\t");
                if ((flags & 0x2000) != 0)
                {
                    DisplayPinvokeInfo(md);
                }

                WriteLine("");
            }
        }

        private uint DisplayMethodInfo(uint md)
        {
            if (!ValidRid(TblMethod, md))
            {
                throw new MetaInfoFailure("GetMethodProps failed.");
            }

            uint rid = RidOf(md);
            string name = Fit(StrCol(TblMethod, 3, rid), StringBufferLen);
            uint flags = Col(TblMethod, 2, rid);
            uint rva = Col(TblMethod, 0, rid);
            uint implFlags = Col(TblMethod, 1, rid);
            (int sig, int cbSig) = BlobCol(TblMethod, 4, rid);

            WriteLine("\t\tMethodName: " + name + " (" + X8(md) + ")");

            StringBuilder sb = new();
            uint access = flags & 7;
            Flag(sb, access == 6, "Public");
            Flag(sb, access == 1, "Private");
            Flag(sb, access == 4, "Family");
            Flag(sb, access == 3, "Assem");
            Flag(sb, access == 2, "FamANDAssem");
            Flag(sb, access == 5, "FamORAssem");
            Flag(sb, access == 0, "PrivateScope");
            Flag(sb, (flags & 0x10) != 0, "Static");
            Flag(sb, (flags & 0x20) != 0, "Final");
            Flag(sb, (flags & 0x40) != 0, "Virtual");
            Flag(sb, (flags & 0x80) != 0, "HideBySig");
            Flag(sb, (flags & 0x100) == 0, "ReuseSlot");
            Flag(sb, (flags & 0x100) != 0, "NewSlot");
            Flag(sb, (flags & 0x400) != 0, "Abstract");
            Flag(sb, (flags & 0x800) != 0, "SpecialName");
            Flag(sb, (flags & 0x1000) != 0, "RTSpecialName");
            Flag(sb, (flags & 0x2000) != 0, "PinvokeImpl");
            Flag(sb, (flags & 0x8) != 0, "UnmanagedExport");
            if (sb.Length == 0)
            {
                sb.Append("[none]");
            }

            if ((flags & 0x1000) != 0 && name == ".ctor")
            {
                sb.Append("[.ctor] ");
            }

            if ((flags & 0x1000) != 0 && name == ".cctor")
            {
                sb.Append("[.cctor] ");
            }

            Flag(sb, (flags & 0x4000) != 0, "HasSecurity");
            Flag(sb, (flags & 0x8000) != 0, "RequireSecObject");
            WriteLine("\t\tFlags     : " + sb + " (" + x8(flags) + ")");
            WriteLine("\t\tRVA       : 0x" + x8(rva));

            sb.Clear();
            uint codeType = implFlags & 3;
            Flag(sb, codeType == 1, "Native");
            Flag(sb, codeType == 0, "IL");
            Flag(sb, codeType == 2, "OPTIL");
            Flag(sb, codeType == 3, "Runtime");
            Flag(sb, (implFlags & 4) != 0, "Unmanaged");
            Flag(sb, (implFlags & 4) == 0, "Managed");
            Flag(sb, (implFlags & 0x10) != 0, "ForwardRef");
            Flag(sb, (implFlags & 0x80) != 0, "PreserveSig");
            Flag(sb, (implFlags & 0x1000) != 0, "InternalCall");
            Flag(sb, (implFlags & 0x20) != 0, "Synchronized");
            Flag(sb, (implFlags & 0x8) != 0, "NoInlining");
            WriteLine("\t\tImplFlags : " + NoneIfEmpty(sb) + " (" + x8(implFlags) + ")");

            if (cbSig != 0)
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }
            else
            {
                WriteLine("\t\tERROR: no valid signature ");
            }

            DisplayGenericParams(md, "\t\t");
            return flags;
        }

        private void DisplayParams(uint md)
        {
            uint rid = RidOf(md);
            if (rid == 0 || rid > Rows(TblMethod))
            {
                return;
            }

            uint start = Col(TblMethod, 5, rid);
            uint end = EndRid(TblMethod, rid, 5, TblParam);
            if (start >= end)
            {
                return;
            }

            WriteLine("\t\t" + D(end - start) + " Parameters");
            for (uint i = start; i < end; i++)
            {
                uint pd = Indirect(TblParamPtr, i) | mdtParamDef;
                DisplayParamInfo(pd);
                DisplayFieldMarshal(pd);
            }
        }

        private void DisplayParamInfo(uint pd)
        {
            if (!ValidRid(TblParam, pd))
            {
                throw new MetaInfoFailure("GetParamProps failed.");
            }

            uint rid = RidOf(pd);
            uint flags = Col(TblParam, 0, rid);
            uint seq = Col(TblParam, 1, rid);
            string name = Fit(StrCol(TblParam, 2, rid), StringBufferLen);
            StringBuilder sb = new();
            Flag(sb, (flags & 1) != 0, "In");
            Flag(sb, (flags & 2) != 0, "Out");
            Flag(sb, (flags & 0x10) != 0, "Optional");
            Flag(sb, (flags & 0x1000) != 0, "HasDefault");
            Flag(sb, (flags & 0x2000) != 0, "HasFieldMarshal");
            Write("\t\t\t(" + U(seq) + ") ParamToken : (" + x8(pd) + ") Name : " + name + " flags: " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
            WriteLine("");
            DisplayCustomAttributes(pd, "\t\t\t");
        }

        private void DisplayGenericParams(uint tk, string prefix)
        {
            List<uint> list = new();
            if (IsSorted(TblGenericParam))
            {
                uint encoded = EncodeToken(RidOf(tk), TypeOf(tk), CdTypeOrMethodDef);
                (uint start, uint end) = SearchRange(TblGenericParam, 2, encoded);
                list = Range(start, end, mdtGenericParam);
            }

            if (list.Count == 0)
            {
                return;
            }

            WriteLine(prefix + D(list.Count) + " Generic Parameters");
            foreach (uint gp in list)
            {
                DisplayGenericParamInfo(gp, prefix);
            }
        }

        private void DisplayGenericParamInfo(uint gp, string prefix)
        {
            if (!ValidRid(TblGenericParam, gp))
            {
                throw new MetaInfoFailure("GetGenericParamProps failed.");
            }

            uint rid = RidOf(gp);
            uint seq = Col(TblGenericParam, 0, rid);
            uint flags = Col(TblGenericParam, 1, rid);
            uint owner = ColToken(TblGenericParam, 2, rid);
            string name = Fit(StrCol(TblGenericParam, 3, rid), StringBufferLen);
            WriteLine(prefix + "\t(" + U(seq) + ") GenericParamToken : (" + x8(gp) + ") Name : " + name + " flags: " + x8(flags) + " Owner: " + x8(owner));

            List<uint> constraints = new();
            if (IsSorted(TblGenericParamConstraint))
            {
                (uint start, uint end) = SearchRange(TblGenericParamConstraint, 0, rid);
                constraints = Range(start, end, mdtGenericParamConstraint);
            }

            // The constraints are enumerated 4 at a time, one line per batch.
            for (int i = 0; i < constraints.Count; i += 4)
            {
                if (i == 0)
                {
                    WriteLine(prefix + "\t\t" + D(constraints.Count) + " Constraint(s)");
                }

                Write(prefix + "\t\t");
                for (int j = i; j < constraints.Count && j < i + 4; j++)
                {
                    uint c = RidOf(constraints[j]);
                    uint cOwner = ValidRid(TblGenericParamConstraint, constraints[j]) ? ColToken(TblGenericParamConstraint, 0, c) : 0;
                    uint constraint = ValidRid(TblGenericParamConstraint, constraints[j]) ? ColToken(TblGenericParamConstraint, 1, c) : 0;
                    if (cOwner != gp)
                    {
                        Write(x8(constraint) + " (owner: " + x8(cOwner) + ")  ");
                    }
                    else
                    {
                        Write(x8(constraint) + "  ");
                    }
                }

                WriteLine("");
            }

            string newPrefix = prefix + "\t";
            DisplayCustomAttributes(gp, newPrefix.Length < 30 ? newPrefix : newPrefix.Substring(0, 29));
        }

        // ---------------------------------------------------------------- fields

        private void DisplayFields(uint td, Dictionary<uint, uint>? offsets)
        {
            int n = 1;
            foreach (uint fd in EnumFields(td))
            {
                WriteLine("\tField #" + D(n++) + " (" + x8(fd) + ")");
                WriteLine("\t" + Dashes);
                uint flags = DisplayFieldInfo(fd);
                DisplayCustomAttributes(fd, "\t\t");
                DisplayPermissions(fd, "\t");
                DisplayFieldMarshal(fd);
                if (td == 0)
                {
                    DisplayFieldRVA(fd);
                }

                if ((flags & 0x2000) != 0)
                {
                    DisplayPinvokeInfo(fd);
                }

                if (offsets is not null && offsets.TryGetValue(RidOf(fd), out uint offset))
                {
                    WriteLine("\t\tOffset : 0x" + x8(offset));
                }

                WriteLine("");
            }
        }

        private uint DisplayFieldInfo(uint fd)
        {
            if (!ValidRid(TblField, fd))
            {
                throw new MetaInfoFailure("GetFieldProps failed.");
            }

            uint rid = RidOf(fd);
            uint flags = Col(TblField, 0, rid);
            string name = Fit(StrCol(TblField, 1, rid), StringBufferLen);
            (int sig, int cbSig) = BlobCol(TblField, 2, rid);
            StringBuilder sb = new();
            uint access = flags & 7;
            Flag(sb, access == 6, "Public");
            Flag(sb, access == 1, "Private");
            Flag(sb, access == 4, "Family");
            Flag(sb, access == 3, "Assembly");
            Flag(sb, access == 2, "FamANDAssem");
            Flag(sb, access == 5, "FamORAssem");
            Flag(sb, access == 0, "PrivateScope");
            Flag(sb, (flags & 0x10) != 0, "Static");
            Flag(sb, (flags & 0x20) != 0, "InitOnly");
            Flag(sb, (flags & 0x40) != 0, "Literal");
            Flag(sb, (flags & 0x80) != 0, "NotSerialized");
            Flag(sb, (flags & 0x200) != 0, "SpecialName");
            Flag(sb, (flags & 0x400) != 0, "RTSpecialName");
            Flag(sb, (flags & 0x2000) != 0, "PinvokeImpl");
            Flag(sb, (flags & 0x8000) != 0, "HasDefault");
            WriteLine("\t\tField Name: " + name + " (" + X8(fd) + ")");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
            if (cbSig == 0)
            {
                WriteLine("\t\tERROR: no valid signature ");
            }
            else
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }

            return flags;
        }

        private void DisplayFieldRVA(uint fd)
        {
            uint row = FindByKey(TblFieldRVA, 1, RidOf(fd));
            uint rva = row == 0 ? 0 : Col(TblFieldRVA, 0, row);
            WriteLine("\t\tRVA       : 0x" + x8(rva));
        }

        // ---------------------------------------------------------------- member refs

        private void DisplayMemberRefs(uint parent, string prefix)
        {
            if (parent == mdtTypeDef || parent == 0)
            {
                parent = TypeDefModule;
            }

            int n = 1;
            for (uint i = 1; i <= Rows(TblMemberRef); i++)
            {
                if (ColToken(TblMemberRef, 0, i) != parent)
                {
                    continue;
                }

                uint mr = i | mdtMemberRef;
                WriteLine(prefix + "\tMemberRef #" + D(n++) + " (" + x8(mr) + ")");
                WriteLine(prefix + "\t" + Dashes);
                DisplayMemberRefInfo(mr, prefix);
            }
        }

        private void DisplayMemberRefInfo(uint mr, string prefix)
        {
            string name = MemberRefName(mr);
            (int sig, int cbSig) = BlobCol(TblMemberRef, 2, RidOf(mr));
            WriteLine(prefix + "\t\tMember: (" + x8(mr) + ") " + name + ": ");
            if (cbSig != 0)
            {
                DisplaySignature(sig, (uint)cbSig, prefix);
            }
            else
            {
                WriteLine(prefix + "\t\tERROR: no valid signature ");
            }

            DisplayCustomAttributes(mr, "\t\t" + prefix);
        }

        // ---------------------------------------------------------------- type defs

        private void DisplayTypeDefs()
        {
            int n = 1;
            for (uint rid = 2; rid <= Rows(TblTypeDef); rid++)
            {
                uint td = rid | mdtTypeDef;
                WriteLine("TypeDef #" + D(n++) + " (" + x8(td) + ")");
                WriteLine(Dashes);
                DisplayTypeDefInfo(td);
                WriteLine("");
            }
        }

        private void DisplayTypeDefProps(uint td)
        {
            uint rid = RidOf(td);
            string name = Fit(TypeDefFullName(td) ?? throw new MetaInfoFailure("GetTypeDefProps failed."), StringBufferLen);
            uint flags = Col(TblTypeDef, 0, rid);
            uint extends = ColToken(TblTypeDef, 3, rid);
            if (RidOf(extends) == 0)
            {
                extends = mdtTypeRef;
            }

            WriteLine("\tTypDefName: " + name + "  (" + X8(td) + ")");
            WriteLine("\tFlags     : " + ClassFlags(flags) + " (" + x8(flags) + ")");
            WriteLine("\tExtends   : " + X8(extends) + " [" + TokenTypeName(extends) + "] " + TypeDeforRefName(extends));

            uint layout = FindByKey(TblClassLayout, 2, rid);
            if (layout != 0)
            {
                WriteLine("\tLayout    : Packing:" + D(Col(TblClassLayout, 0, layout)) + ", Size:" + D(Col(TblClassLayout, 1, layout)));
            }

            if ((flags & 7) >= 2)
            {
                uint nested = FindByKey(TblNestedClass, 0, rid);
                if (nested != 0)
                {
                    uint enclosing = ColToken(TblNestedClass, 1, nested);
                    WriteLine("\tEnclosingClass : " + TypeDeforRefName(enclosing) + " (" + X8(enclosing) + ")");
                }
                else
                {
                    WriteLine("ERROR: EnclosingClass not found for NestedClass");
                }
            }
        }

        private void DisplayTypeDefInfo(uint td)
        {
            DisplayTypeDefProps(td);

            // GetClassLayout: the field offsets, if the type has a layout or any of its fields has an offset.
            uint rid = RidOf(td);
            bool hasLayout = FindByKey(TblClassLayout, 2, rid) != 0;
            Dictionary<uint, uint> offsets = new();
            uint start = Col(TblTypeDef, 4, rid);
            uint end = EndRid(TblTypeDef, rid, 4, TblField);
            for (uint i = start; i < end; i++)
            {
                uint fieldRid = Indirect(TblFieldPtr, i);
                uint row = FindByKey(TblFieldLayout, 1, fieldRid);
                uint offset = 0xFFFFFFFF;
                if (row != 0)
                {
                    offset = Col(TblFieldLayout, 0, row);
                    hasLayout = true;
                }

                offsets.TryAdd(fieldRid, offset);
            }

            DisplayGenericParams(td, "\t");
            DisplayFields(td, hasLayout && offsets.Count > 0 ? offsets : null);
            DisplayMethods(td);
            DisplayProperties(td);
            DisplayEvents(td);
            DisplayMethodImpls(td);
            DisplayPermissions(td, "");
            DisplayInterfaceImpls(td);
            DisplayCustomAttributes(td, "\t");
        }

        private void DisplayInterfaceImpls(uint td)
        {
            List<uint> list = new();
            if (IsSorted(TblInterfaceImpl))
            {
                (uint start, uint end) = SearchRange(TblInterfaceImpl, 0, RidOf(td));
                list = Range(start, end, mdtInterfaceImpl);
            }

            int n = 1;
            foreach (uint ii in list)
            {
                WriteLine("\tInterfaceImpl #" + D(n++) + " (" + x8(ii) + ")");
                WriteLine("\t" + Dashes);
                uint cls = ColToken(TblInterfaceImpl, 0, RidOf(ii));
                uint itf = ColToken(TblInterfaceImpl, 1, RidOf(ii));
                WriteLine("\t\tClass     : " + TypeDeforRefName(cls));
                WriteLine("\t\tToken     : " + X8(itf) + " [" + TokenTypeName(itf) + "] " + TypeDeforRefName(itf));
                DisplayCustomAttributes(ii, "\t\t");
                DisplayPermissions(ii, "\t");
                WriteLine("");
            }
        }

        private void DisplayMethodImpls(uint td)
        {
            List<uint> rows = new();
            if (IsSorted(TblMethodImpl))
            {
                (uint start, uint end) = SearchRange(TblMethodImpl, 0, RidOf(td));
                rows = Range(start, end, 0);
            }

            int n = 1;
            foreach (uint row in rows)
            {
                WriteLine("\n\tMethodImpl #" + D(n) + " (" + x8((uint)n) + ")");
                n++;
                WriteLine("\t" + Dashes);
                WriteLine("\t\tMethod Body Token : 0x" + x8(ColToken(TblMethodImpl, 1, row)));
                WriteLine("\t\tMethod Declaration Token : 0x" + x8(ColToken(TblMethodImpl, 2, row)));
                WriteLine("");
            }
        }

        // ---------------------------------------------------------------- properties and events

        // EnumProperties/EnumEvents: the map row is found by a linear search, the list is clamped to the table.
        private List<uint> EnumMapList(uint td, int mapTable, int ptrTable, int table, uint tokenType)
        {
            List<uint> list = new();
            if (RidOf(td) == 0)
            {
                return list;
            }

            uint map = 0;
            for (uint i = 1; i <= Rows(mapTable); i++)
            {
                if (Col(mapTable, 0, i) == RidOf(td))
                {
                    map = i;
                    break;
                }
            }

            uint start = 0, end = 0;
            if (map != 0)
            {
                start = Col(mapTable, 1, map);
                end = EndRid(mapTable, map, 1, table);
                uint max = Rows(table) + 1;
                if (start == 0)
                {
                    start = 1;
                }

                if (end > max)
                {
                    end = max;
                }

                if (start > end)
                {
                    start = end;
                }
            }

            for (uint i = start; i < end; i++)
            {
                list.Add(Indirect(ptrTable, i) | tokenType);
            }

            return list;
        }

        // The MethodSemantics rows of an event or property, in table order.
        private List<uint> MethodSemanticsOf(uint tk)
        {
            List<uint> rows = new();
            if (IsSorted(TblMethodSemantics))
            {
                (uint start, uint end) = SearchRange(TblMethodSemantics, 2, EncodeToken(RidOf(tk), TypeOf(tk), CdHasSemantic));
                rows = Range(start, end, 0);
            }

            return rows;
        }

        private void DisplayProperties(uint td)
        {
            int n = 1;
            foreach (uint prop in EnumMapList(td, TblPropertyMap, TblPropertyPtr, TblProperty, mdtProperty))
            {
                WriteLine("\tProperty #" + D(n++) + " (" + x8(prop) + ")");
                WriteLine("\t" + Dashes);
                DisplayPropertyInfo(prop);
                DisplayPermissions(prop, "\t");
                WriteLine("");
            }
        }

        private void DisplayPropertyInfo(uint prop)
        {
            if (!ValidRid(TblProperty, prop))
            {
                throw new MetaInfoFailure("GetPropertyProps failed.");
            }

            uint rid = RidOf(prop);
            uint flags = Col(TblProperty, 0, rid);
            (int sig, int cbSig) = BlobCol(TblProperty, 2, rid);
            uint setter = mdtMethodDef, getter = mdtMethodDef, others = 0;
            foreach (uint row in MethodSemanticsOf(prop))
            {
                uint method = Col(TblMethodSemantics, 1, row) | mdtMethodDef;
                switch (Col(TblMethodSemantics, 0, row))
                {
                    case 0x1: setter = method; break;
                    case 0x2: getter = method; break;
                    case 0x4: others++; break;
                }
            }

            string name = Fit(StrCol(TblProperty, 1, rid), StringBufferLen);
            WriteLine("\t\tProp.Name : " + name + " (" + X8(prop) + ")");
            StringBuilder sb = new();
            Flag(sb, (flags & 0x200) != 0, "SpecialName");
            Flag(sb, (flags & 0x400) != 0, "RTSpecialName");
            Flag(sb, (flags & 0x1000) != 0, "HasDefault");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
            if (cbSig != 0)
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }
            else
            {
                WriteLine("\t\tERROR: no valid signature ");
            }

            WriteLine("\t\tSetter    : (" + x8(setter) + ") " + MemberDeforRefName(setter));
            WriteLine("\t\tGetter    : (" + x8(getter) + ") " + MemberDeforRefName(getter));
            WriteLine("\t\t" + U(others) + " Others");
            DisplayCustomAttributes(prop, "\t\t");
        }

        private void DisplayEvents(uint td)
        {
            int n = 1;
            foreach (uint ev in EnumMapList(td, TblEventMap, TblEventPtr, TblEvent, mdtEvent))
            {
                WriteLine("\tEvent #" + D(n++) + " (" + x8(ev) + ")");
                WriteLine("\t" + Dashes);
                DisplayEventInfo(ev);
                DisplayPermissions(ev, "\t");
                WriteLine("");
            }
        }

        private void DisplayEventInfo(uint ev)
        {
            if (!ValidRid(TblEvent, ev))
            {
                throw new MetaInfoFailure("GetEventProps failed.");
            }

            uint rid = RidOf(ev);
            uint flags = Col(TblEvent, 0, rid);
            uint eventType = ColToken(TblEvent, 2, rid);
            uint addOn = mdtMethodDef, removeOn = mdtMethodDef, fire = mdtMethodDef, others = 0;
            foreach (uint row in MethodSemanticsOf(ev))
            {
                uint method = Col(TblMethodSemantics, 1, row) | mdtMethodDef;
                switch (Col(TblMethodSemantics, 0, row))
                {
                    case 0x08: addOn = method; break;
                    case 0x10: removeOn = method; break;
                    case 0x20: fire = method; break;
                    case 0x04: others++; break;
                }
            }

            string name = Fit(StrCol(TblEvent, 1, rid), StringBufferLen);
            WriteLine("\t\tName      : " + name + " (" + X8(ev) + ")");
            StringBuilder sb = new();
            Flag(sb, (flags & 0x200) != 0, "SpecialName");
            Flag(sb, (flags & 0x400) != 0, "RTSpecialName");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
            WriteLine("\t\tEventType : " + X8(eventType) + " [" + TokenTypeName(eventType) + "]");
            WriteLine("\t\tAddOnMethd: (" + x8(addOn) + ") " + MemberDeforRefName(addOn));
            WriteLine("\t\tRmvOnMethd: (" + x8(removeOn) + ") " + MemberDeforRefName(removeOn));
            WriteLine("\t\tFireMethod: (" + x8(fire) + ") " + MemberDeforRefName(fire));
            WriteLine("\t\t" + U(others) + " OtherMethods");
            DisplayCustomAttributes(ev, "\t\t");
        }

        // ---------------------------------------------------------------- custom attributes

        private List<uint> EnumCustomAttributes(uint tk)
        {
            if (RidOf(tk) == 0)
            {
                return Range(1, Rows(TblCustomAttribute) + 1, mdtCustomAttribute);
            }

            if (IsSorted(TblCustomAttribute))
            {
                (uint start, uint end) = SearchRange(TblCustomAttribute, 0, EncodeToken(RidOf(tk), TypeOf(tk), CdHasCustomAttribute));
                return Range(start, end, mdtCustomAttribute);
            }

            return new List<uint>();
        }

        private void DisplayCustomAttributes(uint tk, string prefix)
        {
            int n = 1;
            foreach (uint ca in EnumCustomAttributes(tk))
            {
                WriteLine(prefix + "CustomAttribute #" + D(n++) + " (" + x8(ca) + ")");
                WriteLine(prefix + Dashes);
                DisplayCustomAttributeInfo(ca, prefix);
            }
        }

        private void DisplayCustomAttributeInfo(uint ca, string prefix)
        {
            uint rid = RidOf(ca);
            uint type = ColToken(TblCustomAttribute, 1, rid);
            (int value, int cbValue) = BlobCol(TblCustomAttribute, 2, rid);
            WriteLine(prefix + "\tCustomAttribute Type: " + x8(type));

            uint tk = type;
            string? methName = null;
            int sig = -1;
            int cbSig = 0;
            bool hasSig = false;
            switch (TypeOf(tk))
            {
                case mdtMemberRef:
                    if (!ValidRid(TblMemberRef, tk))
                    {
                        throw new MetaInfoFailure("GetNameFromToken failed.");
                    }

                    methName = StrCol(TblMemberRef, 1, RidOf(tk));
                    (sig, cbSig) = BlobCol(TblMemberRef, 2, RidOf(tk));
                    hasSig = true;
                    tk = ColToken(TblMemberRef, 0, RidOf(tk));
                    break;
                case mdtMethodDef:
                    if (!ValidRid(TblMethod, tk))
                    {
                        throw new MetaInfoFailure("GetNameFromToken failed.");
                    }

                    methName = StrCol(TblMethod, 3, RidOf(tk));
                    (sig, cbSig) = BlobCol(TblMethod, 4, RidOf(tk));
                    hasSig = true;
                    tk = ParentOfMethod(tk);
                    break;
            }

            string rcName = "";
            switch (TypeOf(tk))
            {
                case mdtTypeDef:
                    rcName = Fit(TypeDefFullName(tk) ?? throw new MetaInfoFailure("GetTypeDefProps failed."), MaxClassName);
                    break;
                case mdtTypeRef:
                    rcName = Fit(TypeRefFullName(tk) ?? throw new MetaInfoFailure("GetTypeRefProps failed."), MaxClassName);
                    break;
            }

            Write(prefix + "\tCustomAttributeName: " + rcName);
            if (hasSig && methName is not null)
            {
                string pretty = PrettyPrintSig(sig, cbSig, methName);
                // The UTF-16 text is converted into a buffer of 2 bytes per character plus 3.
                int limit = 2 * (pretty.Length + 1) + 1;
                Write(" :: " + (Encoding.UTF8.GetByteCount(pretty) + 1 <= limit ? pretty : ""));
            }

            bool coff = rcName == "__DecoratedName";
            if (coff)
            {
                _coffNames += (ulong)cbValue + 6;
            }

            WriteLine("");
            WriteLine(prefix + "\tLength: " + D(cbValue));
            string newPrefix = prefix + "\tValue ";
            DumpHex(newPrefix, value, cbValue, true, 16);
            if (coff)
            {
                WriteLine(prefix + "\t            " + CStringAt(value));
            }

            if (hasSig)
            {
                DecodeCustomAttributeArgs(sig, value, cbValue, prefix);
            }

            WriteLine("");
        }

        // FindParentOfMethodHelper: the type whose method list holds the method.
        private uint ParentOfMethod(uint md)
        {
            uint rid = RidOf(md);
            if (Rows(TblMethodPtr) > 0)
            {
                uint parent = 0;
                for (uint td = 1; td <= Rows(TblTypeDef); td++)
                {
                    uint end = EndRid(TblTypeDef, td, 5, TblMethod);
                    for (uint i = Col(TblTypeDef, 5, td); i < end; i++)
                    {
                        if (Col(TblMethodPtr, 0, i) == rid)
                        {
                            parent = td;
                        }
                    }
                }

                return parent | mdtTypeDef;
            }

            return SearchNotGreater(TblTypeDef, 5, rid) | mdtTypeDef;
        }

        // vSearchTableNotGreater: the last row whose value is not greater than the target.
        private uint SearchNotGreater(int table, int col, uint target)
        {
            uint cRecs = Rows(table);
            uint lo = 1, hi = cRecs, mid = 0, val = 0;
            if (lo > hi)
            {
                return 0;
            }

            while (lo <= hi)
            {
                mid = (lo + hi) / 2;
                val = Col(table, col, mid);
                if (val == target)
                {
                    break;
                }

                if (val < target && val != 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (val > target || val == 0)
            {
                while (val > target || val == 0)
                {
                    if (mid == 1)
                    {
                        return 0;
                    }

                    --mid;
                    val = Col(table, col, mid);
                }
            }
            else
            {
                while (mid < cRecs)
                {
                    val = Col(table, col, mid + 1);
                    if (val > target || val == 0)
                    {
                        break;
                    }

                    mid++;
                }
            }

            return mid;
        }

        // Decodes the constructor arguments of a custom attribute blob (the common cases only).
        private void DecodeCustomAttributeArgs(int sig, int value, int cbValue, string prefix)
        {
            long ps = 0;
            uint data = SigData(sig, ref ps);
            uint cParams = SigData(sig, ref ps);
            data = SigData(sig, ref ps);
            if (data != 0x01)
            {
                return;
            }

            // CustomAttributeParser, after ValidateProlog (which leaves the cursor past the prolog when the blob has one).
            int cur = cbValue >= 2 ? 2 : 0;
            uint u1 = 0, u2 = 0, u4 = 0;
            ulong u8 = 0;
            string? str = null;

            Write(prefix + "\tctor args: (");
            for (uint i = 0; i < cParams; ++i)
            {
                data = SigData(sig, ref ps);
                if (i != 0)
                {
                    Write(", ");
                }

                bool again;
                do
                {
                    again = false;
                    switch (data)
                    {
                        case 0x1c: // ELEMENT_TYPE_OBJECT
                            if (cbValue - cur >= 1)
                            {
                                u1 = B(value, cur);
                                cur += 1;
                            }

                            data = u1;
                            again = true;
                            break;
                        case 0x04: // I1
                        case 0x05: // U1
                            if (cbValue - cur >= 1)
                            {
                                u1 = B(value, cur);
                                cur += 1;
                            }

                            Write(D(u1));
                            break;
                        case 0x06: // I2
                        case 0x07: // U2
                            if (cbValue - cur >= 2)
                            {
                                u2 = B(value, cur) | B(value, cur + 1) << 8;
                                cur += 2;
                            }

                            Write(D(u2));
                            break;
                        case 0x08: // I4
                        case 0x09: // U4
                            if (cbValue - cur >= 4)
                            {
                                u4 = ReadU4(value, cur);
                                cur += 4;
                            }

                            Write(D(u4));
                            break;
                        case 0x0e: // STRING
                            str = CaString(value, cbValue, ref cur, str);
                            Write("\"" + (str ?? "(null)") + "\"");
                            break;
                        case 0x12: // CLASS: only System.Type, stored as a string
                            SigData(sig, ref ps);
                            str = CaString(value, cbValue, ref cur, str);
                            Write("typeof(" + (str ?? "(null)") + ")");
                            break;
                        case 0x50: // SERIALIZATION_TYPE_TYPE
                            str = CaString(value, cbValue, ref cur, str);
                            Write("typeof(" + (str ?? "(null)") + ")");
                            break;
                        case 0x0a: // I8
                        case 0x0b: // U8
                            if (cbValue - cur >= 8)
                            {
                                u8 = ReadU4(value, cur) | (ulong)ReadU4(value, cur + 4) << 32;
                                cur += 8;
                            }

                            Write(u8.ToString("x", CultureInfo.InvariantCulture).Insert(0, "0x"));
                            break;
                        case 0x0c: // R4, read without a bounds check
                            Write(FormatF(BitConverter.Int32BitsToSingle((int)ReadU4(value, cur))));
                            cur += 4;
                            break;
                        case 0x0d: // R8, read without a bounds check
                            Write(FormatF(BitConverter.Int64BitsToDouble((long)(ReadU4(value, cur) | (ulong)ReadU4(value, cur + 4) << 32))));
                            cur += 8;
                            break;
                        default:
                            i = cParams;
                            Write(" <can not decode> ");
                            break;
                    }
                }
                while (again);
            }

            WriteLine(")");
        }

        private uint ReadU4(int offset, int i) => B(offset, i) | B(offset, i + 1) << 8 | B(offset, i + 2) << 16 | B(offset, i + 3) << 24;

        // CustomAttributeParser::GetString, printed with "%s": the text runs to the next NUL in memory. On failure the
        // previous string pointer is printed again.
        private string? CaString(int value, int cbValue, ref int cur, string? previous)
        {
            if (cbValue - cur <= 0)
            {
                return previous;
            }

            uint b0 = B(value, cur);
            if (b0 == 0xFF)
            {
                cur++;
                return null;
            }

            int prefix;
            uint len;
            if ((b0 & 0x80) == 0)
            {
                prefix = 1;
                len = b0;
            }
            else if ((b0 & 0xC0) == 0x80)
            {
                prefix = 2;
                len = (b0 & 0x3F) << 8 | B(value, cur + 1);
            }
            else if ((b0 & 0xE0) == 0xC0)
            {
                prefix = 4;
                len = (b0 & 0x1F) << 24 | B(value, cur + 1) << 16 | B(value, cur + 2) << 8 | B(value, cur + 3);
            }
            else
            {
                return previous;
            }

            if (cur + prefix > cbValue || len > (uint)(cbValue - cur - prefix))
            {
                return previous;
            }

            int start = cur + prefix;
            cur = start + (int)len;
            return value < 0 ? "" : CStringAt(value + start);
        }

        // printf("%f")
        private static string FormatF(double v)
        {
            if (double.IsNaN(v))
            {
                return BitConverter.DoubleToInt64Bits(v) < 0 ? "-nan" : "nan";
            }

            if (double.IsInfinity(v))
            {
                return v < 0 ? "-inf" : "inf";
            }

            return v.ToString("F6", CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- permissions

        private static readonly string?[] s_actions =
        [
            "ActionNil", "Request", "Demand", "Assert", "Deny", "PermitOnly", "LinktimeCheck", "InheritanceCheck",
            "RequestMinimum", "RequestOptional", "RequestRefuse", "PrejitGrant", "PrejitDenied", "NonCasDemand",
            "NonCasLinkDemand", "NonCasInheritance",
        ];

        private void DisplayPermissions(uint tk, string prefix)
        {
            uint type = TypeOf(tk);
            if (tk != 0 && type != mdtTypeDef && type != mdtMethodDef && type != mdtAssembly)
            {
                return;
            }

            List<uint> list;
            if (RidOf(tk) != 0)
            {
                if (IsSorted(TblDeclSecurity))
                {
                    (uint start, uint end) = SearchRange(TblDeclSecurity, 1, EncodeToken(RidOf(tk), type, CdHasDeclSecurity));
                    list = Range(start, end, mdtPermission);
                }
                else
                {
                    list = new List<uint>();
                }
            }
            else
            {
                list = Range(1, Rows(TblDeclSecurity) + 1, mdtPermission);
            }

            int n = 1;
            foreach (uint perm in list)
            {
                WriteLine(prefix + "\tPermission #" + D(n++) + " (" + x8(perm) + ")");
                WriteLine(prefix + "\t" + Dashes);
                DisplayPermissionInfo(perm, prefix);
                WriteLine("");
            }
        }

        private void DisplayPermissionInfo(uint perm, string prefix)
        {
            uint rid = RidOf(perm);
            uint action = Col(TblDeclSecurity, 0, rid);
            (int blob, int cb) = BlobCol(TblDeclSecurity, 2, rid);
            string desc = action < s_actions.Length ? s_actions[action]! : "(null)";
            WriteLine(prefix + "\t\tAction    : " + desc);
            WriteLine(prefix + "\t\tBlobLen   : " + D(cb));
            if (cb != 0)
            {
                DumpHex(prefix + "\tBlob", blob, cb, false, 24);
            }

            DisplayCustomAttributes(perm, "\t\t" + prefix);
        }

        // ---------------------------------------------------------------- marshaling, pinvoke

        private static readonly string[] s_nativeTypes =
        [
            "NATIVE_TYPE_END(DEPRECATED!)", "NATIVE_TYPE_VOID(DEPRECATED!)", "NATIVE_TYPE_BOOLEAN", "NATIVE_TYPE_I1",
            "NATIVE_TYPE_U1", "NATIVE_TYPE_I2", "NATIVE_TYPE_U2", "NATIVE_TYPE_I4", "NATIVE_TYPE_U4", "NATIVE_TYPE_I8",
            "NATIVE_TYPE_U8", "NATIVE_TYPE_R4", "NATIVE_TYPE_R8", "NATIVE_TYPE_SYSCHAR(DEPRECATED!)",
            "NATIVE_TYPE_VARIANT(DEPRECATED!)", "NATIVE_TYPE_CURRENCY", "NATIVE_TYPE_PTR(DEPRECATED!)",
            "NATIVE_TYPE_DECIMAL(DEPRECATED!)", "NATIVE_TYPE_DATE(DEPRECATED!)", "NATIVE_TYPE_BSTR", "NATIVE_TYPE_LPSTR",
            "NATIVE_TYPE_LPWSTR", "NATIVE_TYPE_LPTSTR", "NATIVE_TYPE_FIXEDSYSSTRING", "NATIVE_TYPE_OBJECTREF(DEPRECATED!)",
            "NATIVE_TYPE_IUNKNOWN", "NATIVE_TYPE_IDISPATCH", "NATIVE_TYPE_STRUCT", "NATIVE_TYPE_INTF", "NATIVE_TYPE_SAFEARRAY",
            "NATIVE_TYPE_FIXEDARRAY", "NATIVE_TYPE_INT", "NATIVE_TYPE_UINT", "NATIVE_TYPE_NESTEDSTRUCT(DEPRECATED!)",
            "NATIVE_TYPE_BYVALSTR", "NATIVE_TYPE_ANSIBSTR", "NATIVE_TYPE_TBSTR", "NATIVE_TYPE_VARIANTBOOL", "NATIVE_TYPE_FUNC",
            "NATIVE_TYPE_LPVOID", "NATIVE_TYPE_ASANY", "<UNDEFINED NATIVE TYPE 0x29>", "NATIVE_TYPE_ARRAY",
            "NATIVE_TYPE_LPSTRUCT", "NATIVE_TYPE_CUSTOMMARSHALER", "NATIVE_TYPE_ERROR",
        ];

        private const uint NativeTypeMax = 0x50;

        // TrySigUncompress: false (and cbCur = -1) on an invalid encoding.
        private bool TrySigUncompress(int blob, ref uint cbCur, out uint data)
        {
            uint b0 = B(blob, cbCur);
            uint size;
            if ((b0 & 0x80) == 0)
            {
                data = b0;
                size = 1;
            }
            else if ((b0 & 0xC0) == 0x80)
            {
                data = (b0 & 0x3F) << 8 | B(blob, cbCur + 1);
                size = 2;
            }
            else if ((b0 & 0xE0) == 0xC0)
            {
                data = (b0 & 0x1F) << 24 | B(blob, cbCur + 1) << 16 | B(blob, cbCur + 2) << 8 | B(blob, cbCur + 3);
                size = 4;
            }
            else
            {
                data = 0;
                cbCur = 0xFFFFFFFF;
                return false;
            }

            cbCur += size;
            return true;
        }

        // CPackedLen::GetLength
        private uint PackedLen(int blob, long at, out int byteCount)
        {
            uint b0 = B(blob, at);
            if ((b0 & 0x80) == 0)
            {
                byteCount = 1;
                return b0;
            }

            if ((b0 & 0xC0) == 0x80)
            {
                byteCount = 2;
                return (b0 & 0x3F) << 8 | B(blob, at + 1);
            }

            if ((b0 & 0xE0) == 0xC0)
            {
                byteCount = 4;
                return (b0 & 0x1F) << 24 | B(blob, at + 1) << 16 | B(blob, at + 2) << 8 | B(blob, at + 3);
            }

            byteCount = 1;
            return 0;
        }

        private byte[] PackedString(int blob, ref uint cbCur)
        {
            uint len = PackedLen(blob, cbCur, out int byteCount);
            cbCur += (uint)byteCount;
            byte[] s = new byte[len];
            for (uint i = 0; i < len; i++)
            {
                s[i] = (byte)B(blob, cbCur + i);
            }

            cbCur += len;
            return s;
        }

        private void DisplayFieldMarshal(uint tk)
        {
            uint row = FindByKey(TblFieldMarshal, 0, EncodeToken(RidOf(tk), TypeOf(tk), CdHasFieldMarshal));
            if (row == 0)
            {
                return;
            }

            (int nt, int cbNative) = BlobCol(TblFieldMarshal, 1, row);
            uint cbNativeType = (uint)cbNative;
            uint cbCur = 0;
            uint data = 0;
            while (cbCur < cbNativeType)
            {
                StringBuilder desc = new();
                data = NativeTypeMax;
                if (!TrySigUncompress(nt, ref cbCur, out data))
                {
                    continue;
                }

                if (data >= s_nativeTypes.Length)
                {
                    cbCur = 0xFFFFFFFF;
                    continue;
                }

                desc.Append(s_nativeTypes[data]).Append(' ');
                bool failed = false;
                switch (data)
                {
                    case 0x17: // FIXEDSYSSTRING
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(nt, ref cbCur, out data))
                            {
                                failed = true;
                                break;
                            }

                            desc.Append("{StringElementCount: ").Append(D(data)).Append("} ");
                        }

                        break;
                    case 0x1e: // FIXEDARRAY
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(nt, ref cbCur, out data))
                            {
                                failed = true;
                                break;
                            }

                            desc.Append("{ArrayElementCount: ").Append(D(data));
                            if (cbCur < cbNativeType)
                            {
                                if (!TrySigUncompress(nt, ref cbCur, out data))
                                {
                                    failed = true;
                                    break;
                                }

                                desc.Append(", ArrayElementType(NT): ").Append(D(data));
                            }

                            desc.Append('}');
                        }

                        break;
                    case 0x2a: // ARRAY
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(nt, ref cbCur, out data))
                            {
                                failed = true;
                                break;
                            }

                            bool elemTypeSpecified = data != NativeTypeMax;
                            desc.Append(elemTypeSpecified ? "{ArrayElementType(NT): " + D(data) : "{");
                            if (cbCur < cbNativeType)
                            {
                                if (elemTypeSpecified)
                                {
                                    desc.Append(", ");
                                }

                                if (!TrySigUncompress(nt, ref cbCur, out data))
                                {
                                    failed = true;
                                    break;
                                }

                                desc.Append("SizeParamIndex: ").Append(D(data));
                                if (cbCur < cbNativeType)
                                {
                                    if (!TrySigUncompress(nt, ref cbCur, out data))
                                    {
                                        failed = true;
                                        break;
                                    }

                                    desc.Append(", SizeParamMultiplier: ").Append(D(data));
                                    if (cbCur < cbNativeType)
                                    {
                                        if (!TrySigUncompress(nt, ref cbCur, out data))
                                        {
                                            failed = true;
                                            break;
                                        }

                                        desc.Append(", SizeConst: ").Append(D(data));
                                    }
                                }
                            }

                            desc.Append('}');
                        }

                        break;
                    case 0x1d: // SAFEARRAY
                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(nt, ref cbCur, out data))
                            {
                                failed = true;
                                break;
                            }

                            desc.Append("{SafeArraySubType(VT): ").Append(D(data)).Append(", ");
                            if (cbCur < cbNativeType)
                            {
                                desc.Append("ElementTypeName: ").Append(CString(PackedString(nt, ref cbCur))).Append('}');
                            }
                            else
                            {
                                desc.Append("ElementTypeName: }");
                            }
                        }

                        break;
                    case 0x2c: // CUSTOMMARSHALER
                        desc.Append("{Typelib: ").Append(CString(PackedString(nt, ref cbCur))).Append(", ");
                        desc.Append("Native: ").Append(CString(PackedString(nt, ref cbCur))).Append(", ");
                        desc.Append("Marshaler: ").Append(CString(PackedString(nt, ref cbCur))).Append(", ");
                        {
                            uint len = PackedLen(nt, cbCur, out int byteCount);
                            cbCur += (uint)byteCount;
                            desc.Append("Cookie: ");
                            if (len > 0)
                            {
                                List<byte> bytes = new();
                                for (uint i = 0; i < len - 1; i++, cbCur++)
                                {
                                    uint c = B(nt, cbCur);
                                    if (c == 0)
                                    {
                                        bytes.Add((byte)'\\');
                                        bytes.Add((byte)'0');
                                    }
                                    else
                                    {
                                        bytes.Add((byte)c);
                                    }
                                }

                                bytes.Add((byte)B(nt, cbCur));
                                cbCur++;
                                desc.Append(Encoding.Latin1.GetString(bytes.ToArray()));
                            }
                        }

                        desc.Append('}');
                        break;
                }

                if (failed)
                {
                    continue;
                }

                WriteLine("\t\t\t\t" + desc);
                if (data >= NativeTypeMax)
                {
                    break;
                }
            }

            if (cbCur == 0xFFFFFFFF)
            {
                StringBuilder sb = new("\t\t\t\t{");
                for (int i = 0; i < cbNative; i++)
                {
                    sb.Append(' ').Append(((byte)B(nt, i)).ToString("X2", CultureInfo.InvariantCulture));
                }

                Write(sb.ToString());
                WriteLine(" }");
            }
        }

        private void DisplayPinvokeInfo(uint tk)
        {
            uint row = FindByKey(TblImplMap, 1, EncodeToken(RidOf(tk), TypeOf(tk), CdMemberForwarded));
            if (row == 0)
            {
                return;
            }

            uint flags = Col(TblImplMap, 0, row);
            string name = Fit(StrCol(TblImplMap, 2, row), 512);
            uint moduleRef = ColToken(TblImplMap, 3, row);
            WriteLine("\t\tPinvoke Map Data:");
            WriteLine("\t\tEntry point:      " + name);
            WriteLine("\t\tModule ref:       " + x8(moduleRef));
            StringBuilder sb = new();
            Flag(sb, (flags & 0x1) != 0, "NoMangle");
            uint charSet = flags & 0x6;
            Flag(sb, charSet == 0, "CharSetNotSpec");
            Flag(sb, charSet == 2, "CharSetAnsi");
            Flag(sb, charSet == 4, "CharSetUnicode");
            Flag(sb, charSet == 6, "CharSetAuto");
            Flag(sb, (flags & 0x40) != 0, "SupportsLastError");
            uint callConv = flags & 0x700;
            Flag(sb, callConv == 0x100, "CallConvWinapi");
            Flag(sb, callConv == 0x200, "CallConvCdecl");
            Flag(sb, callConv == 0x300, "CallConvStdcall");
            Flag(sb, callConv == 0x400, "CallConvThiscall");
            Flag(sb, callConv == 0x500, "CallConvFastcall");
            uint bestFit = flags & 0x30;
            Flag(sb, bestFit == 0x10, "BestFitEnabled");
            Flag(sb, bestFit == 0x20, "BestFitDisabled");
            Flag(sb, bestFit == 0, "BestFitUseAssem");
            uint throwOn = flags & 0x3000;
            Flag(sb, throwOn == 0x1000, "ThrowOnUnmappableCharEnabled");
            Flag(sb, throwOn == 0x2000, "ThrowOnUnmappableCharDisabled");
            Flag(sb, throwOn == 0, "ThrowOnUnmappableCharUseAssem");
            WriteLine("\t\tMapping flags:    " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
        }

        // ---------------------------------------------------------------- signatures

        private static readonly string[] s_elementTypes =
        [
            "End", "Void", "Boolean", "Char", "I1", "UI1", "I2", "UI2", "I4", "UI4", "I8", "UI8", "R4", "R8", "String", "Ptr",
            "ByRef", "ValueClass", "Class", "Var", "MDArray", "GenericInst", "TypedByRef", "VALUEARRAY", "I", "U", "R", "FNPTR",
            "Object", "SZArray", "MVar", "CMOD_REQD", "CMOD_OPT", "INTERNAL", "CMOD_INTERNAL",
        ];

        private static readonly string[] s_callingConventions =
        [
            "[DEFAULT]", "[C]", "[STDCALL]", "[THISCALL]", "[FASTCALL]", "[VARARG]", "[FIELD]", "[LOCALSIG]", "[PROPERTY]",
            "[UNMANAGED]", "[GENERICINST]", "[NATIVEVARARG]", "[INVALID]", "[INVALID]", "[INVALID]", "[INVALID]",
        ];

        private const uint ElementTypeMax = 0x23;

        // CorSigUncompressData(PCCOR_SIGNATURE, ULONG*): the size read, or -1 (with 0) on an invalid encoding.
        private uint Uncompress(int sig, long at, out uint data)
        {
            uint b0 = B(sig, at);
            if ((b0 & 0x80) == 0)
            {
                data = b0;
                return 1;
            }

            if ((b0 & 0xC0) == 0x80)
            {
                data = (b0 & 0x3F) << 8 | B(sig, at + 1);
                return 2;
            }

            if ((b0 & 0xE0) == 0xC0)
            {
                data = (b0 & 0x1F) << 24 | B(sig, at + 1) << 16 | B(sig, at + 2) << 8 | B(sig, at + 3);
                return 4;
            }

            data = 0;
            return 0xFFFFFFFF;
        }

        // CorSigUncompressData(PCCOR_SIGNATURE&): no validation, advances the pointer.
        private uint SigData(int sig, ref long p)
        {
            uint b0 = B(sig, p);
            if ((b0 & 0x80) == 0)
            {
                p++;
                return b0;
            }

            if ((b0 & 0xC0) == 0x80)
            {
                uint r = (b0 & 0x3F) << 8 | B(sig, p + 1);
                p += 2;
                return r;
            }

            uint v = (b0 & 0x1F) << 24 | B(sig, p + 1) << 16 | B(sig, p + 2) << 8 | B(sig, p + 3);
            p += 4;
            return v;
        }

        private static readonly uint[] s_sigTokenTypes = [mdtTypeDef, mdtTypeRef, mdtTypeSpec, mdtBaseType];

        private uint UncompressToken(int sig, long at, out uint tk)
        {
            uint cb = Uncompress(sig, at, out uint v);
            tk = (v >> 2) | s_sigTokenTypes[v & 3];
            return cb;
        }

        private uint UncompressSignedInt(int sig, long at, out int value)
        {
            uint cb = Uncompress(sig, at, out uint data);
            if (cb == 0xFFFFFFFF)
            {
                value = 0;
                return cb;
            }

            bool negative = (data & 1) != 0;
            data >>= 1;
            if (negative)
            {
                data |= cb == 1 ? 0xFFFFFFC0 : cb == 2 ? 0xFFFFE000 : 0xF0000000;
            }

            value = (int)data;
            return cb;
        }

        private readonly StringBuilder _sigBuf = new();

        private void DisplaySignature(int sig, uint ulSigBlob, string prefix)
        {
            uint cbCur = 0;
            uint cb;
            uint ulArgs;
            bool failed = false;
            uint ulSigBlobStart = ulSigBlob;

            _sigBuf.Clear();
            cb = Uncompress(sig, 0, out uint ulData);
            WriteLine(prefix + "\t\tCallCnvntn: " + s_callingConventions[ulData & 0x0f]);
            if (cb > ulSigBlob)
            {
                goto ErrExit;
            }

            cbCur += cb;
            ulSigBlob -= cb;

            if ((ulData & 0x20) != 0)
            {
                WriteLine(prefix + "\t\thasThis ");
            }

            if ((ulData & 0x40) != 0)
            {
                WriteLine(prefix + "\t\texplicit ");
            }

            if ((ulData & 0x10) != 0)
            {
                WriteLine(prefix + "\t\tgeneric ");
            }

            _sigBuf.Clear();
            if ((ulData & 0x0f) == 0x06)
            {
                if (!GetOneElementType(sig, cbCur, ulSigBlob, ref cb))
                {
                    failed = true;
                    goto ErrExit;
                }

                WriteLine(prefix + "\t\tField type: " + _sigBuf);
                if (cb > ulSigBlob)
                {
                    goto ErrExit;
                }

                cbCur += cb;
                ulSigBlob -= cb;
            }
            else
            {
                if ((ulData & 0x10) != 0)
                {
                    cb = Uncompress(sig, cbCur, out uint tyArgs);
                    if (cb > ulSigBlob)
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                    ulSigBlob -= cb;
                    WriteLine(prefix + "\t\tType Arity:" + D(tyArgs) + " ");
                }

                cb = Uncompress(sig, cbCur, out ulArgs);
                if (cb > ulSigBlob)
                {
                    goto ErrExit;
                }

                cbCur += cb;
                ulSigBlob -= cb;

                if (ulData != 0x07 && ulData != 0x0a)
                {
                    if (!GetOneElementType(sig, cbCur, ulSigBlob, ref cb))
                    {
                        failed = true;
                        goto ErrExit;
                    }

                    WriteLine(prefix + "\t\tReturnType:" + _sigBuf);
                    if (cb > ulSigBlob)
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                    ulSigBlob -= cb;
                }

                if (ulSigBlob != 0)
                {
                    WriteLine(prefix + "\t\t" + U(ulArgs) + " Arguments");
                }
                else
                {
                    WriteLine(prefix + "\t\tNo arguments.");
                }

                uint i = 0;
                while (i < ulArgs && ulSigBlob > 0)
                {
                    ++i;
                    _sigBuf.Clear();
                    if (!GetOneElementType(sig, cbCur, ulSigBlob, ref cb))
                    {
                        failed = true;
                        goto ErrExit;
                    }

                    WriteLine(prefix + "\t\t\tArgument #" + U(i) + ": " + _sigBuf);
                    if (cb > ulSigBlob)
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                    ulSigBlob -= cb;
                }
            }

            cb = 0;

        ErrExit:
            if ((_filter & DumpMoreHex) != 0 || ulSigBlob != 0)
            {
                if (cb > ulSigBlob)
                {
                    WriteLine("\tERROR IN SIGNATURE:  Signature should be larger.");
                }
                else if (cb < ulSigBlob)
                {
                    Write("\tERROR IN SIGNATURE:  Not all of signature blob was consumed.  " + D(ulSigBlob) + " byte(s) remain");
                    if (ulSigBlob < 4)
                    {
                        Write(": ");
                        StringBuilder sb = new();
                        for (; ulSigBlob != 0; ++cbCur, --ulSigBlob)
                        {
                            sb.Append(((byte)B(sig, cbCur)).ToString("x2", CultureInfo.InvariantCulture)).Append(' ');
                        }

                        Write(sb.ToString());
                        WriteLine("");
                        goto ErrExit2;
                    }

                    WriteLine("");
                }

                DumpHex(prefix + "\t\tSignature ", sig, (int)ulSigBlobStart, false, 24);
            }

        ErrExit2:
            if (failed)
            {
                throw new MetaInfoFailure("ERROR!! Bad signature blob value!");
            }
        }

        // GetOneElementType: appends the element type at sig+start to _sigBuf; false on E_FAIL. Like the metadata engine,
        // the size is not set when the type is not recognized at all.
        private bool GetOneElementType(int sig, uint start, uint ulSigBlob, ref uint pcb)
        {
            bool ok = true;
            uint cbCur = 0;
            uint cb;
            uint ulData;
            uint tk;

            cb = Uncompress(sig, start, out ulData);
            cbCur += cb;

            if ((ulData & 0x40) != 0)
            {
                if (ulData == 0x41)
                {
                    _sigBuf.Append("<ELEMENT_TYPE_SENTINEL>");
                }
                else if (ulData == 0x45)
                {
                    _sigBuf.Append("PINNED");
                }
                else
                {
                    ok = false;
                    goto ErrExit;
                }

                if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                {
                    goto ErrExit;
                }

                cbCur += cb;
                goto ErrExit;
            }

            if (ulData >= ElementTypeMax)
            {
                ok = false;
                goto ErrExit;
            }

            while (ulData == 0x0f || ulData == 0x10)
            {
                _sigBuf.Append(' ').Append(s_elementTypes[ulData]);
                cb = Uncompress(sig, start + cbCur, out ulData);
                cbCur += cb;
            }

            _sigBuf.Append(' ');
            if (ulData >= ElementTypeMax)
            {
                // Indexes past the table here; it never happens with valid signatures.
                ok = false;
                goto ErrExit;
            }

            _sigBuf.Append(s_elementTypes[ulData]);
            if (ulData < 0x0f || ulData == 0x16 || ulData == 0x1c || ulData == 0x18 || ulData == 0x19)
            {
                goto ErrExit;
            }

            if (ulData == 0x11 || ulData == 0x12 || ulData == 0x1f || ulData == 0x20)
            {
                cb = UncompressToken(sig, start + cbCur, out tk);
                cbCur += cb;
                if (TypeOf(tk) == mdtTypeDef || TypeOf(tk) == mdtTypeRef)
                {
                    _sigBuf.Append(' ').Append(TypeDeforRefName(tk));
                }
                else
                {
                    _sigBuf.Append(' ').Append(PadL(Hx(tk), 8));
                }

                if (ulData == 0x1f || ulData == 0x20)
                {
                    if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                }

                goto ErrExit;
            }

            if (ulData == 0x1d)
            {
                if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                {
                    goto ErrExit;
                }

                cbCur += cb;
                goto ErrExit;
            }

            if (ulData == 0x15)
            {
                if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                {
                    goto ErrExit;
                }

                cbCur += cb;
                cb = Uncompress(sig, start + cbCur, out uint numArgs);
                cbCur += cb;
                _sigBuf.Append('<');
                while (numArgs > 0)
                {
                    if (cbCur > ulSigBlob)
                    {
                        goto ErrExit;
                    }

                    if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                    --numArgs;
                    if (numArgs > 0)
                    {
                        _sigBuf.Append(',');
                    }
                }

                _sigBuf.Append('>');
                goto ErrExit;
            }

            if (ulData == 0x13 || ulData == 0x1e)
            {
                cb = Uncompress(sig, start + cbCur, out uint index);
                cbCur += cb;
                _sigBuf.Append(ulData == 0x13 ? "!" : "!!").Append(D(index));
                goto ErrExit;
            }

            if (ulData == 0x1b)
            {
                cb = Uncompress(sig, start + cbCur, out ulData);
                cbCur += cb;
                if ((ulData & 0x40) != 0)
                {
                    _sigBuf.Append(" explicit");
                }

                if ((ulData & 0x20) != 0)
                {
                    _sigBuf.Append(" hasThis");
                }

                _sigBuf.Append(' ').Append(s_callingConventions[ulData & 0x0f]);
                cb = Uncompress(sig, start + cbCur, out uint numArgs);
                cbCur += cb;
                if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                {
                    goto ErrExit;
                }

                cbCur += cb;
                _sigBuf.Append('(');
                while (numArgs > 0)
                {
                    if (cbCur > ulSigBlob)
                    {
                        goto ErrExit;
                    }

                    if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
                    {
                        goto ErrExit;
                    }

                    cbCur += cb;
                    --numArgs;
                    if (numArgs > 0)
                    {
                        _sigBuf.Append(',');
                    }
                }

                _sigBuf.Append(" )");
                goto ErrExit;
            }

            if (ulData != 0x14)
            {
                return false;
            }

            if (!GetOneElementType(sig, start + cbCur, ulSigBlob - cbCur, ref cb))
            {
                goto ErrExit;
            }

            cbCur += cb;
            cb = Uncompress(sig, start + cbCur, out ulData);
            cbCur += cb;
            _sigBuf.Append(' ').Append(D(ulData));
            if (ulData == 0)
            {
                goto ErrExit;
            }

            cb = Uncompress(sig, start + cbCur, out ulData);
            cbCur += cb;
            _sigBuf.Append(' ').Append(D(ulData));
            while (ulData != 0)
            {
                cb = Uncompress(sig, start + cbCur, out uint temp);
                _sigBuf.Append(' ').Append(D(temp));
                cbCur += cb;
                ulData--;
            }

            cb = Uncompress(sig, start + cbCur, out ulData);
            cbCur += cb;
            _sigBuf.Append(' ').Append(D(ulData));
            while (ulData != 0)
            {
                cb = UncompressSignedInt(sig, start + cbCur, out int iTemp);
                _sigBuf.Append(' ').Append(D(iTemp));
                cbCur += cb;
                ulData--;
            }

        ErrExit:
            if (cbCur > ulSigBlob)
            {
                ok = false;
            }

            pcb = cbCur;
            return ok;
        }

        // ---------------------------------------------------------------- PrettyPrintSigLegacy

        private static readonly string?[] s_callConvNames =
        [
            "", "unmanaged cdecl ", "unmanaged stdcall ", "unmanaged thiscall ", "unmanaged fastcall ", "vararg ",
            "<error> ", "<error> ", "", "", "", "native vararg ", null,
        ];

        private string PrettyPrintSig(int sig, int cbSig, string name)
        {
            StringBuilder o = new();
            long p = 0;
            long end = cbSig;
            uint callConv = SigData(sig, ref p);
            if ((callConv & 0x0f) == 0x06)
            {
                PrettyPrintType(sig, ref p, end, o);
                if (name.Length != 0)
                {
                    o.Append(' ').Append(name);
                }

                return o.ToString();
            }

            if ((callConv & 0x20) != 0)
            {
                o.Append("instance ");
            }

            if ((callConv & 0x10) != 0)
            {
                o.Append("generic ");
                SigData(sig, ref p);
            }

            if ((callConv & 0x0f) < s_callConvNames.Length)
            {
                o.Append(s_callConvNames[callConv & 0x0f] ?? throw new MetaInfoFailure("PrettyPrintSig"));
            }

            uint numArgs = SigData(sig, ref p);
            PrettyPrintType(sig, ref p, end, o);
            if (name.Length != 0)
            {
                o.Append(' ').Append(name);
            }

            o.Append('(');
            bool needComma = false;
            while (numArgs != 0)
            {
                if (p >= end)
                {
                    break;
                }

                if (B(sig, p) == 0x41)
                {
                    if (needComma)
                    {
                        o.Append(',');
                    }

                    o.Append("...");
                    p++;
                }
                else
                {
                    if (needComma)
                    {
                        o.Append(',');
                    }

                    PrettyPrintType(sig, ref p, end, o);
                    --numArgs;
                }

                needComma = true;
            }

            o.Append(')');
            return o.ToString();
        }

        private void PrettyPrintType(int sig, ref long p, long end, StringBuilder o)
        {
            uint elt = B(sig, p++);
            switch (elt)
            {
                case 0x01: o.Append("void"); break;
                case 0x02: o.Append("bool"); break;
                case 0x03: o.Append("wchar"); break;
                case 0x04: o.Append("int8"); break;
                case 0x05: o.Append("unsigned int8"); break;
                case 0x06: o.Append("int16"); break;
                case 0x07: o.Append("unsigned int16"); break;
                case 0x08: o.Append("int32"); break;
                case 0x09: o.Append("unsigned int32"); break;
                case 0x0a: o.Append("int64"); break;
                case 0x0b: o.Append("unsigned int64"); break;
                case 0x0c: o.Append("float32"); break;
                case 0x0d: o.Append("float64"); break;
                case 0x19: o.Append("unsigned int"); break;
                case 0x18: o.Append("int"); break;
                case 0x1c: o.Append("class System.Object"); break;
                case 0x0e: o.Append("class System.String"); break;
                case 0x3e: o.Append("class System.__Canon"); break;
                case 0x16: o.Append("refany"); break;
                case 0x11:
                case 0x12:
                {
                    uint v = SigData(sig, ref p);
                    uint tk = (v >> 2) | s_sigTokenTypes[v & 3];
                    o.Append(elt == 0x11 ? "value class " : "class ");
                    if (TypeOf(tk) == mdtTypeRef)
                    {
                        o.Append(TypeRefFullName(tk) is string s ? Fit(s, MaxClassName) : "");
                    }
                    else if (TypeOf(tk) == mdtTypeDef)
                    {
                        o.Append(TypeDefFullName(tk) is string s ? Fit(s, MaxClassName) : "");
                    }
                    else
                    {
                        o.Append("<UNKNOWN>");
                    }

                    break;
                }

                case 0x1d:
                    PrettyPrintType(sig, ref p, end, o);
                    o.Append("[]");
                    break;
                case 0x14:
                {
                    PrettyPrintType(sig, ref p, end, o);
                    uint rank = SigData(sig, ref p);
                    if (rank == 0)
                    {
                        o.Append("[??]");
                    }
                    else
                    {
                        int[] lowerBounds = new int[rank];
                        int[] sizes = new int[rank];
                        uint numSizes = SigData(sig, ref p);
                        for (uint i = 0; i < numSizes; i++)
                        {
                            uint v = SigData(sig, ref p);
                            if (i < rank)
                            {
                                sizes[i] = (int)v;
                            }
                        }

                        uint numLowBounds = SigData(sig, ref p);
                        for (uint i = 0; i < numLowBounds; i++)
                        {
                            uint v = SigData(sig, ref p);
                            if (i < rank)
                            {
                                lowerBounds[i] = (int)v;
                            }
                        }

                        o.Append('[');
                        for (uint i = 0; i < rank; i++)
                        {
                            if (sizes[i] != 0 && lowerBounds[i] != 0)
                            {
                                o.Append(D(lowerBounds[i])).Append("...").Append(D(lowerBounds[i] + sizes[i] + 1));
                            }

                            if (i < rank - 1)
                            {
                                o.Append(',');
                            }
                        }

                        o.Append(']');
                    }

                    break;
                }

                case 0x1e:
                    o.Append("!!").Append(D(SigData(sig, ref p)));
                    break;
                case 0x13:
                    o.Append('!').Append(D(SigData(sig, ref p)));
                    break;
                case 0x15:
                {
                    PrettyPrintType(sig, ref p, end, o);
                    uint n = SigData(sig, ref p);
                    o.Append('<');
                    for (uint i = 0; i < n; i++)
                    {
                        if (i > 0)
                        {
                            o.Append(',');
                        }

                        PrettyPrintType(sig, ref p, end, o);
                    }

                    o.Append('>');
                    break;
                }

                case 0x3f:
                {
                    o.Append("[module#").Append(D(SigData(sig, ref p))).Append(", token#");
                    uint v = SigData(sig, ref p);
                    uint tk = (v >> 2) | s_sigTokenTypes[v & 3];
                    o.Append(X8(tk)).Append(']');
                    break;
                }

                case 0x1b:
                {
                    o.Append("fnptr ");
                    // PrettyPrintSigWorker restarts the output, then continues with the method signature in place.
                    string inner = PrettyPrintSigInPlace(sig, ref p, end - p);
                    o.Clear().Append(inner);
                    break;
                }

                case 0x3d:
                    o.Append("native ");
                    PrettyPrintType(sig, ref p, end, o);
                    break;
                case 0x45:
                    PrettyPrintType(sig, ref p, end, o);
                    o.Append(" pinned");
                    break;
                case 0x0f:
                    PrettyPrintType(sig, ref p, end, o);
                    o.Append('*');
                    break;
                case 0x10:
                    PrettyPrintType(sig, ref p, end, o);
                    o.Append('&');
                    break;
            }
        }

        // The nested PrettyPrintSigWorker of a function pointer: it shrinks the shared output buffer to nothing first,
        // and advances the caller's pointer (passed by reference) past the method signature.
        private string PrettyPrintSigInPlace(int sig, ref long p, long len)
        {
            StringBuilder o = new();
            long end = p + len;
            uint callConv = SigData(sig, ref p);
            if ((callConv & 0x0f) == 0x06)
            {
                PrettyPrintType(sig, ref p, end, o);
                return o.ToString();
            }

            if ((callConv & 0x20) != 0)
            {
                o.Append("instance ");
            }

            if ((callConv & 0x10) != 0)
            {
                o.Append("generic ");
                SigData(sig, ref p);
            }

            if ((callConv & 0x0f) < s_callConvNames.Length)
            {
                o.Append(s_callConvNames[callConv & 0x0f] ?? throw new MetaInfoFailure("PrettyPrintSig"));
            }

            uint numArgs = SigData(sig, ref p);
            PrettyPrintType(sig, ref p, end, o);
            o.Append('(');
            bool needComma = false;
            while (numArgs != 0)
            {
                if (p >= end)
                {
                    break;
                }

                if (needComma)
                {
                    o.Append(',');
                }

                if (B(sig, p) == 0x41)
                {
                    o.Append("...");
                    p++;
                }
                else
                {
                    PrettyPrintType(sig, ref p, end, o);
                    --numArgs;
                }

                needComma = true;
            }

            o.Append(')');
            return o.ToString();
        }

        // ---------------------------------------------------------------- the other tables

        private void DisplayTypeRefs()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblTypeRef); rid++)
            {
                uint tr = rid | mdtTypeRef;
                WriteLine("TypeRef #" + D(n++) + " (" + x8(tr) + ")");
                WriteLine(Dashes);
                uint scope = ColToken(TblTypeRef, 0, rid);
                string name = Fit(TypeRefFullName(tr)!, StringBufferLen);
                WriteLine("Token:             0x" + x8(tr));
                WriteLine("ResolutionScope:   0x" + x8(scope));
                WriteLine("TypeRefName:       " + name);
                DisplayCustomAttributes(tr, "\t");
                DisplayMemberRefs(tr, "");
                WriteLine("");
            }
        }

        private void DisplayTypeSpecs()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblTypeSpec); rid++)
            {
                uint ts = rid | mdtTypeSpec;
                WriteLine("TypeSpec #" + D(n++) + " (" + x8(ts) + ")");
                WriteLine(Dashes);
                DisplayTypeSpecInfo(ts, "");
                DisplayMemberRefs(ts, "");
                WriteLine("");
            }
        }

        private void DisplayTypeSpecInfo(uint ts, string prefix)
        {
            _sigBuf.Clear();
            (int sig, int cbSig) = BlobCol(TblTypeSpec, 0, RidOf(ts));
            uint cb = 0;
            if (!GetOneElementType(sig, 0, (uint)cbSig, ref cb))
            {
                return;
            }

            WriteLine(prefix + "\tTypeSpec :" + _sigBuf);
            if ((_filter & DumpMoreHex) != 0)
            {
                DumpHex(prefix + "\tSignature", sig, cbSig, false, 24);
            }
        }

        private void DisplayMethodSpecs()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblMethodSpec); rid++)
            {
                uint ms = rid | mdtMethodSpec;
                WriteLine("MethodSpec #" + D(n++) + " (" + x8(ms) + ")");
                _sigBuf.Clear();
                uint parent = ColToken(TblMethodSpec, 0, rid);
                (int sig, int cbSig) = BlobCol(TblMethodSpec, 1, rid);
                WriteLine("\tParent   : 0x" + x8(parent));
                DisplaySignature(sig, (uint)cbSig, "");
                WriteLine("");
            }
        }

        private void DisplayModuleRefs()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblModuleRef); rid++)
            {
                uint mr = rid | mdtModuleRef;
                WriteLine("ModuleRef #" + D(n++) + " (" + x8(mr) + ")");
                WriteLine(Dashes);
                WriteLine("\t\tModuleRef: (" + x8(mr) + ") " + Fit(StrCol(TblModuleRef, 0, rid), StringBufferLen) + ": ");
                DisplayCustomAttributes(mr, "\t\t");
                DisplayMemberRefs(mr, "");
                WriteLine("");
            }
        }

        private void DisplaySignatures()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblStandAloneSig); rid++)
            {
                uint sg = rid | mdtSignature;
                WriteLine("Signature #" + D(n++) + " (" + AltHex08(sg) + ")");
                WriteLine(Dashes);
                (int sig, int cbSig) = BlobCol(TblStandAloneSig, 0, rid);
                if (cbSig != 0)
                {
                    DisplaySignature(sig, (uint)cbSig, "");
                }
                else
                {
                    WriteLine("\t\tERROR: no valid signature ");
                }

                WriteLine("");
            }
        }

        // ---------------------------------------------------------------- assembly

        private void DisplayAssembly()
        {
            DisplayAssemblyInfo();
            DisplayAssemblyRefs();
            DisplayFiles();
            DisplayExportedTypes();
            DisplayManifestResources();
        }

        private void DisplayAssemblyMetadata(uint major, uint minor, uint build, uint revision, string locale)
        {
            WriteLine("\tVersion: " + D(major) + "." + D(minor) + "." + D(build) + "." + D(revision));
            WriteLine("\tMajor Version: 0x" + x8(major));
            WriteLine("\tMinor Version: 0x" + x8(minor));
            WriteLine("\tBuild Number: 0x" + x8(build));
            WriteLine("\tRevision Number: 0x" + x8(revision));
            WriteLine("\tLocale: " + (locale.Length != 0 ? locale : "<null>"));
        }

        private static string AssemblyFlags(uint flags)
        {
            StringBuilder sb = new();
            Flag(sb, (flags & 0x1) != 0, "PublicKey");
            Flag(sb, (flags & 0x100) != 0, "Retargetable");
            Flag(sb, (flags & 0xE00) == 0x200, "WindowsRuntime");
            return NoneIfEmpty(sb);
        }

        private void DisplayAssemblyInfo()
        {
            if (Rows(TblAssembly) == 0)
            {
                return;
            }

            uint mda = 1 | mdtAssembly;
            uint hashAlg = Col(TblAssembly, 0, 1);
            uint flags = Col(TblAssembly, 5, 1);
            (int pk, int cbPk) = BlobCol(TblAssembly, 6, 1);
            string name = Fit(StrCol(TblAssembly, 7, 1), StringBufferLen);
            string locale = Fit(StrCol(TblAssembly, 8, 1), StringBufferLen);
            WriteLine("Assembly");
            WriteLine(Dashes);
            WriteLine("\tToken: 0x" + x8(mda));
            WriteLine("\tName : " + name);
            DumpHex("\tPublic Key    ", pk, cbPk, false, 24);
            WriteLine("\tHash Algorithm : 0x" + x8(hashAlg));
            DisplayAssemblyMetadata(Col(TblAssembly, 1, 1), Col(TblAssembly, 2, 1), Col(TblAssembly, 3, 1), Col(TblAssembly, 4, 1), locale);
            WriteLine("\tFlags : " + AssemblyFlags(flags) + " (" + x8(flags) + ")");
            DisplayCustomAttributes(mda, "\t");
            DisplayPermissions(mda, "\t");
            WriteLine("");
        }

        private void DisplayAssemblyRefs()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblAssemblyRef); rid++)
            {
                uint ar = rid | mdtAssemblyRef;
                WriteLine("AssemblyRef #" + D(n++) + " (" + x8(ar) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + x8(ar));
                uint flags = Col(TblAssemblyRef, 4, rid);
                (int pk, int cbPk) = BlobCol(TblAssemblyRef, 5, rid);
                string name = Fit(StrCol(TblAssemblyRef, 6, rid), StringBufferLen);
                string locale = Fit(StrCol(TblAssemblyRef, 7, rid), StringBufferLen);
                (int hash, int cbHash) = BlobCol(TblAssemblyRef, 8, rid);
                DumpHex("\tPublic Key or Token", pk, cbPk, false, 24);
                WriteLine("\tName: " + name);
                DisplayAssemblyMetadata(Col(TblAssemblyRef, 0, rid), Col(TblAssemblyRef, 1, rid), Col(TblAssemblyRef, 2, rid), Col(TblAssemblyRef, 3, rid), locale);
                DumpHex("\tHashValue Blob", hash, cbHash, false, 24);
                WriteLine("\tFlags: " + AssemblyFlags(flags) + " (" + x8(flags) + ")");
                DisplayCustomAttributes(ar, "\t");
                WriteLine("");
                WriteLine("");
            }
        }

        private void DisplayFiles()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblFile); rid++)
            {
                uint file = rid | mdtFile;
                WriteLine("File #" + D(n++) + " (" + x8(file) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + x8(file));
                uint flags = Col(TblFile, 0, rid);
                string name = Fit(StrCol(TblFile, 1, rid), StringBufferLen);
                (int hash, int cbHash) = BlobCol(TblFile, 2, rid);
                WriteLine("\tName : " + name);
                DumpHex("\tHashValue Blob ", hash, cbHash, false, 24);
                StringBuilder sb = new();
                Flag(sb, (flags & 1) == 0, "ContainsMetaData");
                Flag(sb, (flags & 1) != 0, "ContainsNoMetaData");
                WriteLine("\tFlags : " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
                DisplayCustomAttributes(file, "\t");
                WriteLine("");
                WriteLine("");
            }
        }

        private void DisplayExportedTypes()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblExportedType); rid++)
            {
                uint et = rid | mdtExportedType;
                WriteLine("ExportedType #" + D(n++) + " (" + x8(et) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + x8(et));
                uint flags = Col(TblExportedType, 0, rid);
                uint typeDef = Col(TblExportedType, 1, rid);
                string name = Fit(MakePath(StrCol(TblExportedType, 3, rid), StrCol(TblExportedType, 2, rid)), StringBufferLen);
                uint impl = ColToken(TblExportedType, 4, rid);
                WriteLine("\tName: " + name);
                WriteLine("\tImplementation token: 0x" + x8(impl));
                WriteLine("\tTypeDef token: 0x" + x8(typeDef));
                WriteLine("\tFlags     : " + ClassFlags(flags) + " (" + x8(flags) + ")");
                DisplayCustomAttributes(et, "\t");
                WriteLine("");
                WriteLine("");
            }
        }

        private void DisplayManifestResources()
        {
            int n = 1;
            for (uint rid = 1; rid <= Rows(TblManifestResource); rid++)
            {
                uint mr = rid | mdtManifestResource;
                WriteLine("ManifestResource #" + D(n++) + " (" + x8(mr) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + x8(mr));
                uint offset = Col(TblManifestResource, 0, rid);
                uint flags = Col(TblManifestResource, 1, rid);
                string name = Fit(StrCol(TblManifestResource, 2, rid), StringBufferLen);
                uint impl = ColToken(TblManifestResource, 3, rid);
                WriteLine("Name: " + name);
                WriteLine("Implementation token: 0x" + x8(impl));
                WriteLine("Offset: 0x" + x8(offset));
                StringBuilder sb = new();
                Flag(sb, (flags & 7) == 1, "Public");
                Flag(sb, (flags & 7) == 2, "Private");
                WriteLine("\tFlags: " + NoneIfEmpty(sb) + " (" + x8(flags) + ")");
                DisplayCustomAttributes(mr, "\t");
                WriteLine("");
                WriteLine("");
            }
        }

        // ---------------------------------------------------------------- user strings

        private void DisplayUserStrings()
        {
            // EnumUserStrings collects every non-empty entry first; a bad entry fails the whole enumeration.
            List<(uint Index, int Offset, int Size)> strings = new();
            for (uint index = 0; index < (uint)_userStrings.Size;)
            {
                if (!TryBlob(_userStrings, index, out int offset, out int size, out int prefix))
                {
                    return;
                }

                if (index == 0)
                {
                    prefix = 1;
                }

                if (size != 0)
                {
                    strings.Add((index, offset, size));
                }

                index += (uint)(prefix + size);
            }

            bool first = true;
            bool unprint = false;
            foreach ((uint index, int offset, int size) in strings)
            {
                if (first)
                {
                    WriteLine("User Strings");
                    WriteLine(Dashes);
                    first = false;
                }

                if (size % 2 == 0)
                {
                    throw new MetaInfoFailure("GetUserString failed.");
                }

                int cch = size / 2;
                char[] chars = new char[cch];
                for (int j = 0; j < cch; j++)
                {
                    chars[j] = (char)(HeapByte(_userStrings, offset - _userStrings.Base + 2 * j) | HeapByte(_userStrings, offset - _userStrings.Base + 2 * j + 1) << 8);
                }

                Write((index | mdtString).ToString("x8", CultureInfo.InvariantCulture) + " : (" + PadL(D(cch), 2) + ") L\"");
                for (int j = 0; j < cch; j++)
                {
                    char c = chars[j];
                    switch (c)
                    {
                        case '\0': Write("\\0"); break;
                        case '\r': Write("\\r"); break;
                        case '\n': Write("\\n"); break;
                        case '\t': Write("\\t"); break;
                        default:
                            if (c >= 0x20 && c < 0x7F)
                            {
                                Write(c.ToString());
                            }
                            else
                            {
                                unprint = true;
                                Write(".");
                            }

                            break;
                    }

                    if (j > 0 && (j & 0x7F) == 0)
                    {
                        WriteLine("");
                    }
                }

                WriteLine("\"");
                if (unprint)
                {
                    unprint = false;
                    WriteLine("\t\tUser string has unprintables, hex format below:");
                    StringBuilder sb = new();
                    int k = 0;
                    for (int j = 0; j < cch; j++)
                    {
                        sb.Append(((int)chars[j]).ToString("x4", CultureInfo.InvariantCulture)).Append(' ');
                        k++;
                        if (k == 16 || j == cch - 1)
                        {
                            WriteLine("\t\t" + sb);
                            sb.Clear();
                            k = 0;
                        }
                    }
                }
            }
        }

        // ---------------------------------------------------------------- unresolved externals

        private void DisplayUnsatInfo()
        {
            Write("\nUnresolved Externals\n");
            Write("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

            List<uint> tokens = new();
            for (uint td = 2; td <= Rows(TblTypeDef); td++)
            {
                bool isInterface = (Col(TblTypeDef, 0, td) & 0x20) != 0;
                uint start = Col(TblTypeDef, 5, td);
                uint end = EndRid(TblTypeDef, td, 5, TblMethod);
                for (uint i = start; i < end; i++)
                {
                    uint rid = Indirect(TblMethodPtr, i);
                    uint flags = Col(TblMethod, 2, rid);
                    uint implFlags = Col(TblMethod, 1, rid);
                    if (isInterface && (flags & 0x10) == 0)
                    {
                        continue;
                    }

                    if ((implFlags & 0x10) != 0)
                    {
                        if ((flags & 0x2000) != 0 || (implFlags & 3) == 3 || (implFlags & 0x1000) != 0 || (flags & 0x400) != 0)
                        {
                            continue;
                        }

                        tokens.Add(rid | mdtMethodDef);
                    }
                }
            }

            // RefToDefOptimization resolves (and reparents) the global MemberRefs that match a member of <Module>.
            for (uint mr = 1; mr <= Rows(TblMemberRef); mr++)
            {
                if (ColToken(TblMemberRef, 0, mr) == TypeDefModule && !ResolvesToModuleMember(mr))
                {
                    tokens.Add(mr | mdtMemberRef);
                }
            }

            foreach (uint tk in tokens)
            {
                if (TypeOf(tk) == mdtMethodDef)
                {
                    DisplayMethodInfo(tk);
                }
                else
                {
                    DisplayMemberRefInfo(tk, "");
                }
            }
        }

        private bool ResolvesToModuleMember(uint mr)
        {
            string name = StrCol(TblMemberRef, 1, mr);
            (int sig, int cbSig) = BlobCol(TblMemberRef, 2, mr);
            byte[] refSig = BlobBytes(sig, cbSig);
            foreach (uint md in EnumMethods(TypeDefModule))
            {
                uint rid = RidOf(md);
                if (StrCol(TblMethod, 3, rid) == name)
                {
                    (int s, int cb) = BlobCol(TblMethod, 4, rid);
                    if (BlobBytes(s, cb).AsSpan().SequenceEqual(refSig))
                    {
                        return true;
                    }
                }
            }

            foreach (uint fd in EnumFields(TypeDefModule))
            {
                uint rid = RidOf(fd);
                if (StrCol(TblField, 1, rid) == name)
                {
                    (int s, int cb) = BlobCol(TblField, 2, rid);
                    if (BlobBytes(s, cb).AsSpan().SequenceEqual(refSig))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- DumpHex

        private void DumpHex(string prefix, int data, int cbData, bool text, int nLine)
        {
            nLine &= ~7;
            int nPrefix = 0;
            foreach (char c in prefix)
            {
                nPrefix = c == '\t' ? (nPrefix + 8) & ~7 : nPrefix + 1;
            }

            string? linePrefix = prefix;
            int pos = 0;
            uint remaining = (uint)cbData;
            do
            {
                StringBuilder sb = new();
                sb.Append(linePrefix ?? new string(' ', nPrefix)).Append(':');
                linePrefix = null;
                int nPrint = (int)Math.Min(remaining, (uint)nLine);
                int nSpace = nLine - nPrint;
                for (int i = 0; i < nPrint; i++)
                {
                    if ((i & 7) == 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(((byte)B(data, pos + i)).ToString("x2", CultureInfo.InvariantCulture)).Append(' ');
                }

                if (text)
                {
                    if (nSpace != 0)
                    {
                        sb.Append(' ', nSpace * 3 + nSpace / 8);
                    }

                    sb.Append('>');
                    for (int i = 0; i < nPrint; i++)
                    {
                        uint b = B(data, pos + i);
                        sb.Append(b >= 0x20 && b < 0x7F ? (char)b : ' ');
                    }

                    sb.Append(' ', nSpace).Append('<');
                }

                Write(sb.ToString());
                WriteLine("");
                remaining -= (uint)nPrint;
                pos += nPrint;
            }
            while (remaining > 0);
        }

        // ================================================================ raw dumps

        private void DisplayRaw()
        {
            if ((_filter & DumpCSV) != 0)
            {
                DumpRawCSV();
            }

            if ((_filter & (DumpSchema | DumpHeader | DumpRaw | DumpStats)) != 0)
            {
                DumpRawTables();
            }

            if ((_filter & DumpRawHeaps) != 0)
            {
                DumpHeaps();
            }
        }

        private string RawNameOfType(byte type)
        {
            if (type < CT)
            {
                return type < s_tables.Length ? s_tables[type].Name : "";
            }

            if (type < iSHORT)
            {
                return s_codedTokenNames[type - CT];
            }

            return type switch
            {
                iBYTE => "BYTE",
                iSHORT => "short",
                iUSHORT => "USHORT",
                iLONG => "long",
                iULONG => "ULONG",
                iSTRING => "string",
                iGUID => "GUID",
                iBLOB => "blob",
                _ => "unknown type 0x" + type.ToString("x2", CultureInfo.InvariantCulture),
            };
        }

        private uint BlobSizeOrZero(uint index) => TryBlob(_blobs, index, out _, out int size, out _) ? (uint)size : 0;

        private string RawCol(int table, int col, uint rid, bool stats)
        {
            byte type = s_tables[table].ColTypes[col];
            if (type < CT)
            {
                return RawNameOfType(type) + "[" + Hx(ColToken(table, col, rid)) + "]";
            }

            if (type < iSHORT)
            {
                return s_codedTokenNames[type - CT] + "[" + x8(ColToken(table, col, rid)) + "]";
            }

            uint val = Col(table, col, rid);
            switch (type)
            {
                case iBYTE:
                    return val.ToString("x2", CultureInfo.InvariantCulture);
                case iSHORT:
                case iUSHORT:
                    return val.ToString("x4", CultureInfo.InvariantCulture);
                case iLONG:
                case iULONG:
                    return x8(val);
                case iSTRING:
                {
                    string s = "string#" + Hx(val);
                    if (stats && val != 0)
                    {
                        s += "(" + D((StringBytes(val)?.Length ?? 0) + 1) + ")";
                    }

                    return s;
                }

                case iGUID:
                    return "guid#" + Hx(val) + (stats && val != 0 ? "(16)" : "");
                case iBLOB:
                {
                    string s = "blob#" + Hx(val);
                    if (stats && val != 0)
                    {
                        uint cb = BlobSizeOrZero(val) + 1;
                        if (cb > 128)
                        {
                            cb++;
                        }

                        if (cb > 16535)
                        {
                            cb++;
                        }

                        s += "(" + D(cb) + ")";
                    }

                    return s;
                }

                default:
                    return "unknown type 0x" + val.ToString("x4", CultureInfo.InvariantCulture);
            }
        }

        private uint RawColStats(int table, int col)
        {
            byte type = s_tables[table].ColTypes[col];
            uint result = 0;
            if (type < iSTRING)
            {
                return 0;
            }

            for (uint rid = 1; rid <= Rows(table); rid++)
            {
                uint val = Col(table, col, rid);
                if (val == 0)
                {
                    continue;
                }

                switch (type)
                {
                    case iSTRING:
                        result += (uint)(StringBytes(val)?.Length ?? 0) + 1;
                        break;
                    case iGUID:
                        result += 16;
                        break;
                    case iBLOB:
                    {
                        uint cb = BlobSizeOrZero(val);
                        result += cb + 1;
                        if (cb > 128)
                        {
                            result++;
                        }

                        if (cb > 16535)
                        {
                            result++;
                        }

                        break;
                    }
                }
            }

            return result;
        }

        private void DumpRawTables()
        {
            bool stats = (_filter & DumpStats) != 0;
            bool rows = (_filter & (DumpSchema | DumpHeader)) == 0;
            bool schema = rows || (_filter & DumpSchema) != 0;

            uint verLen = U32At(12);
            int header = 16 + (int)verLen;
            WriteLine("Metadata section: 0x" + x8(U32At(0)) + ", version: " + D(U16At(4)) + "." + D(U16At(6)) + ", extra: " + D(U32At(8))
                + ", version len: " + D(verLen) + ", version: " + CStringAt(16));
            WriteLine("           flags: 0x" + U8At(header).ToString("x2", CultureInfo.InvariantCulture) + ", streams: " + D(U16At(header + 2)));
            if ((_filter & DumpMoreHex) != 0)
            {
                int cb = header + 4;
                if (_streams.Count > 0)
                {
                    cb = _streams[0].Offset;
                }

                DumpHex("        ", 0, cb, true, 16);
            }

            int ix = 0;
            foreach (Stream s in _streams)
            {
                WriteLine("Stream " + D(ix++) + ": name: " + s.Name + ", size " + D(s.Size));
            }

            if (_tablesStream >= 0)
            {
                int t = _tablesStream;
                WriteLine("Metadata header: " + D(U8At(t + 4)) + "." + D(U8At(t + 5)) + ", heaps: 0x" + U8At(t + 6).ToString("x2", CultureInfo.InvariantCulture)
                    + ", rid: 0x" + U8At(t + 7).ToString("x2", CultureInfo.InvariantCulture) + ", valid: 0x" + U64At(t + 8).ToString("x16", CultureInfo.InvariantCulture)
                    + ", sorted: 0x" + U64At(t + 16).ToString("x16", CultureInfo.InvariantCulture));
                if ((_filter & DumpMoreHex) != 0)
                {
                    DumpHex("        ", t, 24, true, 16);
                }
            }

            WriteLine("");

            Write("Strings: " + D((uint)_strings.Size) + "(" + AltHex((uint)_strings.Size) + ")");
            Write(", Blobs: " + D((uint)_blobs.Size) + "(" + AltHex((uint)_blobs.Size) + ")");
            Write(", Guids: " + D((uint)_guids.Size) + "(" + AltHex((uint)_guids.Size) + ")");
            WriteLine(", User strings: " + D((uint)_userStrings.Size) + "(" + AltHex((uint)_userStrings.Size) + ")");

            for (int tbl = 0; tbl < _tableCount; tbl++)
            {
                TableDef def = s_tables[tbl];
                uint cRows = Rows(tbl);
                uint cbRow = (uint)_cbRec[tbl];
                if (rows)
                {
                    WriteLine("=================================================");
                }

                WriteLine(PadL(D(tbl), 2) + "(" + AltHex((uint)tbl) + "): " + PadR(def.Name, 20) + " cRecs:" + PadL(D(cRows), 5) + "(" + AltHex(cRows) + "), cbRec:"
                    + PadL(D(cbRow), 3) + "(" + AltHex(cbRow) + "), cbTable:" + PadL(D(cbRow * cRows), 6) + "(" + AltHex(cbRow * cRows) + ")");
                if (!schema && !rows)
                {
                    continue;
                }

                for (int col = 0; col < def.ColTypes.Length; col++)
                {
                    string line = "  col " + PadL(Hx((uint)col), 2) + ":" + (col == def.Key ? '*' : ' ') + " " + PadR(def.ColNames[col], 12)
                        + " oCol:" + PadL(Hx((uint)_colOffset[tbl][col]), 2) + ", cbCol:" + Hx((uint)_colSize[tbl][col]) + ", "
                        + PadR(RawNameOfType(def.ColTypes[col]), 7);
                    if (stats)
                    {
                        uint size = RawColStats(tbl, col);
                        if (size != 0)
                        {
                            line += "(" + D(size) + ")";
                        }
                    }

                    Write(line);
                    WriteLine("");
                }

                if (!rows)
                {
                    continue;
                }

                for (uint rid = 1; rid <= cRows; rid++)
                {
                    if (rid == 1)
                    {
                        WriteLine("-------------------------------------------------");
                    }

                    StringBuilder sb = new();
                    sb.Append(' ').Append(PadL(Hx(rid), 3)).Append(" == ");
                    for (int col = 0; col < def.ColTypes.Length; col++)
                    {
                        if (col != 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(D(col)).Append(':').Append(RawCol(tbl, col, rid, stats));
                    }

                    Write(sb.ToString());
                    WriteLine("");
                }
            }
        }

        private void DumpRawCSV()
        {
            WriteLine("Name,Size,cRecs,cbRec");
            WriteLine("Strings," + D((uint)_strings.Size));
            WriteLine("Blobs," + D((uint)_blobs.Size));
            WriteLine("Guids," + D((uint)_guids.Size));
            for (int tbl = 0; tbl < _tableCount; tbl++)
            {
                uint cRows = Rows(tbl);
                uint cbRow = (uint)_cbRec[tbl];
                WriteLine(s_tables[tbl].Name + "," + D(cbRow * cRows) + "," + D(cRows) + "," + D(cbRow));
            }
        }

        private void DumpHeaps()
        {
            uint size = (uint)_blobs.Size;
            WriteLine("");
            WriteLine("Blob Heap:  " + D(size) + "(" + AltHex(size) + ") bytes");
            uint offset = 0;
            while (true)
            {
                int data = -1;
                int cbData = 0;
                int prefix = 1;
                bool ok = TryBlob(_blobs, offset, out int dataOffset, out int dataSize, out int prefixSize);
                if (ok && offset != 0)
                {
                    data = _blobs.Data == _d ? dataOffset : -1;
                    cbData = dataSize;
                    prefix = prefixSize;
                }

                DumpHex(PadL(Hx(offset), 5) + "," + PadR(Hx((uint)cbData), 2), data, cbData, true, 16);

                // GetNextBlob
                if (!ok)
                {
                    break;
                }

                uint next = offset + (uint)(offset == 0 ? 1 : prefix + cbData);
                if (next >= size)
                {
                    break;
                }

                offset = next;
            }

            size = (uint)_strings.Size;
            WriteLine("");
            WriteLine("String Heap:  " + D(size) + "(" + AltHex(size) + ") bytes");
            offset = 0;
            while (true)
            {
                byte[] s = StringBytes(offset) ?? Array.Empty<byte>();
                if ((_filter & DumpMoreHex) != 0)
                {
                    DumpHex(x8(offset), _strings.Data == _d ? _strings.Base + (int)offset : -1, s.Length + 1, true, 16);
                }
                else if (s.Length != 0)
                {
                    Write(x8(offset) + ": " + Utf8(s) + "\n");
                }

                uint next = offset + (uint)s.Length + 1;
                if (next >= size)
                {
                    break;
                }

                offset = next;
            }

            WriteLine("");
            DisplayUserStrings();
        }
    }
}
