// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILDisassembler;

// /METADATA[=<specifier>]: the MetaInfo dump. The metadata is read with System.Reflection.Metadata; the raw dumps
// (RAW, SCHEMA, MDHEADER, CSV, STATS, HEAPS) also show the stored layout of the tables and heaps. If the metadata cannot
// be read, the dump stops with an error line and the rest of the disassembly goes on.
internal sealed class MetaInfoDisassembler : IMetadataDisassembler
{
    private readonly string[] _filters;

    /// <param name="filters">What to show (MDHEADER, HEX, CSV, UNREX, SCHEMA, RAW, HEAPS, VALIDATE, STATS), recognized by
    /// their first three characters.</param>
    public MetaInfoDisassembler(string[] filters)
    {
        _filters = filters;
    }

    public DisassemblyPhase Phase => DisassemblyPhase.Summary;

    public void Disassemble(Dasm dasm)
    {
        uint filter = 0;
        foreach (string spec in _filters)
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

        MetaInfo mi = new(dasm, filter);
        dasm.PrintLine("// ================================= M E T A I N F O ================================================\n");
        mi.DisplayMD();
        dasm.PrintLine("// ================================= END OF METAINFO ================================================\n");
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

        private const uint TypeDefModule = 0x02000001;

        private readonly Dasm _dasm;
        private readonly uint _filter;
        private readonly MetadataReader _md;
        private readonly uint _entryPoint;
        private ulong _coffNames;

        // DumpMI keeps the text of the line being built in a buffer that starts with "// ".
        private readonly StringBuilder _line = new("// ");

        public MetaInfo(Dasm dasm, uint filter)
        {
            _dasm = dasm;
            _filter = filter;
            _md = dasm.Metadata;
            _entryPoint = (uint)(dasm.PE.PEHeaders.CorHeader?.EntryPointTokenOrRelativeVirtualAddress ?? 0);
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

        private static string Utf8(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetString(bytes);

        // ================================================================ metadata access

        private sealed class MetaInfoFailure : Exception
        {
            public MetaInfoFailure(string message) : base(message)
            {
            }
        }

        private uint Rows(int table) => (uint)_md.GetTableRowCount((TableIndex)table);

        private static uint Token(EntityHandle h) => (uint)MetadataTokens.GetToken(h);

        private string Str(StringHandle h) => _md.GetString(h);

        private byte[] Blob(BlobHandle h) => _md.GetBlobBytes(h);

        // A byte of a blob; past its end, 0.
        private static uint B(byte[] blob, long i) => i >= 0 && i < blob.Length ? blob[i] : 0u;

        // The text of a blob from the given position up to the next NUL or the end of the blob.
        private static string CString(byte[] blob, int start = 0)
        {
            if (start >= blob.Length)
            {
                return "";
            }

            ReadOnlySpan<byte> s = blob.AsSpan(start);
            int n = s.IndexOf((byte)0);
            return Utf8(n < 0 ? s : s.Slice(0, n));
        }

        private unsafe ReadOnlySpan<byte> HeapBytes(HeapIndex heap)
        {
            int size = _md.GetHeapSize(heap);
            return size == 0 ? default : new ReadOnlySpan<byte>(_md.MetadataPointer + _md.GetHeapMetadataOffset(heap), size);
        }

        // The size of a heap as shown by the raw dumps; a missing or empty heap counts as its single empty entry.
        private int HeapSize(HeapIndex heap)
        {
            int size = _md.GetHeapSize(heap);
            return size == 0 && heap != HeapIndex.Guid ? 1 : size;
        }

        // The UTF-8 bytes of the string heap entry at the offset, or null if the offset is out of the heap.
        private byte[]? StringBytes(uint offset)
        {
            ReadOnlySpan<byte> heap = HeapBytes(HeapIndex.String);
            if (offset >= (uint)heap.Length)
            {
                return null;
            }

            ReadOnlySpan<byte> s = heap.Slice((int)offset);
            int n = s.IndexOf((byte)0);
            return (n < 0 ? s : s.Slice(0, n)).ToArray();
        }

        // The size of the blob or user string heap entry at the offset.
        private unsafe int HeapEntrySize(HeapIndex heap, int offset)
        {
            byte* start = _md.MetadataPointer + _md.GetHeapMetadataOffset(heap);
            return new BlobReader(start + offset, _md.GetHeapSize(heap) - offset).ReadCompressedInteger();
        }

        // ns::MakePath
        private static string MakePath(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;

        // ================================================================ raw layout (raw dumps and ClassLayout lookups)

        private sealed class Stream
        {
            public string Name = "";
            public int Offset;
            public int Size;
        }

        // The metadata bytes, with the stream headers and table layout read from them.
        private byte[] _d = [];
        private readonly List<Stream> _streams = new();
        private int _tablesStream = -1;
        private int _tableCount;
        private readonly int[] _cbRec = new int[TblCount];
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

        // The dump works on the raw tokens and coded indexes stored in the tables: the token types of the tables.
        private const uint mdtModule = (uint)TableIndex.Module << 24;
        private const uint mdtTypeRef = (uint)TableIndex.TypeRef << 24;
        private const uint mdtTypeDef = (uint)TableIndex.TypeDef << 24;
        private const uint mdtFieldDef = (uint)TableIndex.Field << 24;
        private const uint mdtMethodDef = (uint)TableIndex.MethodDef << 24;
        private const uint mdtParamDef = (uint)TableIndex.Param << 24;
        private const uint mdtInterfaceImpl = (uint)TableIndex.InterfaceImpl << 24;
        private const uint mdtMemberRef = (uint)TableIndex.MemberRef << 24;
        private const uint mdtCustomAttribute = (uint)TableIndex.CustomAttribute << 24;
        private const uint mdtPermission = (uint)TableIndex.DeclSecurity << 24;
        private const uint mdtSignature = (uint)TableIndex.StandAloneSig << 24;
        private const uint mdtEvent = (uint)TableIndex.Event << 24;
        private const uint mdtProperty = (uint)TableIndex.Property << 24;
        private const uint mdtModuleRef = (uint)TableIndex.ModuleRef << 24;
        private const uint mdtTypeSpec = (uint)TableIndex.TypeSpec << 24;
        private const uint mdtAssembly = (uint)TableIndex.Assembly << 24;
        private const uint mdtAssemblyRef = (uint)TableIndex.AssemblyRef << 24;
        private const uint mdtFile = (uint)TableIndex.File << 24;
        private const uint mdtExportedType = (uint)TableIndex.ExportedType << 24;
        private const uint mdtManifestResource = (uint)TableIndex.ManifestResource << 24;
        private const uint mdtGenericParam = (uint)TableIndex.GenericParam << 24;
        private const uint mdtMethodSpec = (uint)TableIndex.MethodSpec << 24;
        private const uint mdtGenericParamConstraint = (uint)TableIndex.GenericParamConstraint << 24;
        private const uint mdtString = (uint)HandleKind.UserString << 24;
        // The fourth (invalid) TypeDefOrRefOrSpecEncoded tag.
        private const uint mdtBaseType = 0x72000000;

        private static uint TypeOf(uint tk) => tk & 0xFF000000;

        private static uint RidOf(uint tk) => tk & 0x00FFFFFF;

        private uint U8At(long off) => off >= 0 && off < _d.Length ? _d[off] : 0u;

        private uint U16At(long off) => U8At(off) | U8At(off + 1) << 8;

        private uint U32At(long off) => U16At(off) | U16At(off + 2) << 16;

        private ulong U64At(long off) => U32At(off) | (ulong)U32At(off + 4) << 32;

        private unsafe void LoadLayout()
        {
            _d = new ReadOnlySpan<byte>(_md.MetadataPointer, _md.MetadataLength).ToArray();
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
                if (_tablesStream < 0 && s.Name is "#~" or "#-")
                {
                    _tablesStream = s.Offset;
                }
            }

            if (_tablesStream < 0)
            {
                return;
            }

            int t = _tablesStream;
            uint heapSizes = U8At(t + 6);
            _tableCount = U8At(t + 4) == 1 && U8At(t + 5) == 0 ? TblCountV1 : TblCount;
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
                        size = Rows(type) > ushort.MaxValue ? 4 : 2;
                    }
                    else if (type < iSHORT)
                    {
                        uint[] tokens = s_codedTokens[type - CT];
                        uint max = 0;
                        foreach (uint tk in tokens)
                        {
                            max = Math.Max(max, Rows((int)(tk >> 24)));
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
                            iSTRING => (heapSizes & 0x01) != 0 ? 4 : 2,
                            iGUID => (heapSizes & 0x02) != 0 ? 4 : 2,
                            _ => (heapSizes & 0x04) != 0 ? 4 : 2,
                        };
                    }

                    _colOffset[i][c] = offset;
                    _colSize[i][c] = size;
                    offset += size + (size & 1);
                }

                _cbRec[i] = offset;
            }
        }

        // The stored value of a column; signed types are sign-extended.
        private uint Col(int table, int col, uint rid)
        {
            long off = _md.GetTableMetadataOffset((TableIndex)table) + (long)(rid - 1) * _md.GetTableRowSize((TableIndex)table) + _colOffset[table][col];
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

        // A RID column as a token of its table, or a decoded coded token.
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
            catch (Exception e) when (e is BadImageFormatException or ArgumentException)
            {
                // The line being built is dropped along with the rest of the dump.
                _dasm.PrintLine("// Error: the metadata cannot be read: " + e.Message);
            }
        }

        private void DisplayMDWorker()
        {
            WriteLine(Separator);
            LoadLayout();
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
            byte[] b = StringBytes(1) ?? throw new MetaInfoFailure("GetString() failed.");
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

            ModuleDefinition module = _md.GetModuleDefinition();
            string name = Str(module.Name);
            Guid mvid = _md.GetGuid(module.Mvid);
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

            TypeDefinition t = _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)RidOf(td)));
            return MakePath(Str(t.Namespace), Str(t.Name));
        }

        private string? TypeRefFullName(uint tr)
        {
            if (!ValidRid(TblTypeRef, tr))
            {
                return null;
            }

            TypeReference t = _md.GetTypeReference(MetadataTokens.TypeReferenceHandle((int)RidOf(tr)));
            return MakePath(Str(t.Namespace), Str(t.Name));
        }

        private string TypeDefName(uint td) => TypeDefFullName(td) ?? "[Invalid TypeDef]";

        private string TypeRefName(uint tr) => TypeRefFullName(tr) ?? "[Invalid TypeRef]";

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

            return table == TblMethod
                ? Str(_md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(tk))).Name)
                : Str(_md.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle((int)RidOf(tk))).Name);
        }

        private string MemberRefName(uint tk)
        {
            if (!ValidRid(TblMemberRef, tk))
            {
                throw new MetaInfoFailure("GetMemberRefProps failed.");
            }

            return Str(_md.GetMemberReference(MetadataTokens.MemberReferenceHandle((int)RidOf(tk))).Name);
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

        private TypeDefinition? TypeDefOrModule(uint td)
        {
            if (td == mdtTypeDef || td == 0)
            {
                td = TypeDefModule;
            }

            return ValidRid(TblTypeDef, td) ? _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)RidOf(td))) : null;
        }

        private List<uint> EnumMethods(uint td)
        {
            List<uint> list = new();
            if (TypeDefOrModule(td) is TypeDefinition t)
            {
                foreach (MethodDefinitionHandle h in t.GetMethods())
                {
                    list.Add(Token(h));
                }
            }

            return list;
        }

        private List<uint> EnumFields(uint td)
        {
            List<uint> list = new();
            if (TypeDefOrModule(td) is TypeDefinition t)
            {
                foreach (FieldDefinitionHandle h in t.GetFields())
                {
                    list.Add(Token(h));
                }
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
                WriteLine("\tMethod #" + D(n++) + " (" + Dasm.x8(md) + ") " + (md == _entryPoint ? "[ENTRYPOINT]" : ""));
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

            MethodDefinition m = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(md)));
            string name = Str(m.Name);
            uint flags = (uint)m.Attributes;
            uint rva = (uint)m.RelativeVirtualAddress;
            uint implFlags = (uint)m.ImplAttributes;
            byte[] sig = Blob(m.Signature);
            int cbSig = sig.Length;

            WriteLine("\t\tMethodName: " + name + " (" + Dasm.X8(md) + ")");

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
            WriteLine("\t\tFlags     : " + sb + " (" + Dasm.x8(flags) + ")");
            WriteLine("\t\tRVA       : 0x" + Dasm.x8(rva));

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
            WriteLine("\t\tImplFlags : " + NoneIfEmpty(sb) + " (" + Dasm.x8(implFlags) + ")");

            if (cbSig != 0)
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }
            else
            {
                WriteLine("\t\tERROR: no valid signature ");
            }

            DisplayGenericParams(m.GetGenericParameters(), "\t\t");
            return flags;
        }

        private void DisplayParams(uint md)
        {
            if (!ValidRid(TblMethod, md))
            {
                return;
            }

            ParameterHandleCollection parameters = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(md))).GetParameters();
            if (parameters.Count == 0)
            {
                return;
            }

            WriteLine("\t\t" + D(parameters.Count) + " Parameters");
            foreach (ParameterHandle h in parameters)
            {
                DisplayParamInfo(h);
                DisplayFieldMarshal(_md.GetParameter(h).GetMarshallingDescriptor());
            }
        }

        private void DisplayParamInfo(ParameterHandle h)
        {
            uint pd = Token(h);
            Parameter param = _md.GetParameter(h);
            uint flags = (uint)param.Attributes;
            uint seq = (uint)param.SequenceNumber;
            string name = Str(param.Name);
            StringBuilder sb = new();
            Flag(sb, (flags & 1) != 0, "In");
            Flag(sb, (flags & 2) != 0, "Out");
            Flag(sb, (flags & 0x10) != 0, "Optional");
            Flag(sb, (flags & 0x1000) != 0, "HasDefault");
            Flag(sb, (flags & 0x2000) != 0, "HasFieldMarshal");
            Write("\t\t\t(" + U(seq) + ") ParamToken : (" + Dasm.x8(pd) + ") Name : " + name + " flags: " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
            WriteLine("");
            DisplayCustomAttributes(pd, "\t\t\t");
        }

        private void DisplayGenericParams(GenericParameterHandleCollection list, string prefix)
        {
            if (list.Count == 0)
            {
                return;
            }

            WriteLine(prefix + D(list.Count) + " Generic Parameters");
            foreach (GenericParameterHandle gp in list)
            {
                DisplayGenericParamInfo(gp, prefix);
            }
        }

        private void DisplayGenericParamInfo(GenericParameterHandle h, string prefix)
        {
            uint gp = Token(h);
            GenericParameter p = _md.GetGenericParameter(h);
            uint seq = (uint)p.Index;
            uint flags = (uint)p.Attributes;
            uint owner = Token(p.Parent);
            string name = Str(p.Name);
            WriteLine(prefix + "\t(" + U(seq) + ") GenericParamToken : (" + Dasm.x8(gp) + ") Name : " + name + " flags: " + Dasm.x8(flags) + " Owner: " + Dasm.x8(owner));

            List<uint> constraints = new();
            foreach (GenericParameterConstraintHandle c in p.GetConstraints())
            {
                constraints.Add(Token(_md.GetGenericParameterConstraint(c).Type));
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
                    Write(Dasm.x8(constraints[j]) + "  ");
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
                WriteLine("\tField #" + D(n++) + " (" + Dasm.x8(fd) + ")");
                WriteLine("\t" + Dashes);
                FieldDefinition field = _md.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle((int)RidOf(fd)));
                DisplayFieldInfo(fd, field);
                DisplayCustomAttributes(fd, "\t\t");
                DisplayPermissions(fd, "\t");
                DisplayFieldMarshal(field.GetMarshallingDescriptor());
                if (td == 0)
                {
                    WriteLine("\t\tRVA       : 0x" + Dasm.x8((uint)field.GetRelativeVirtualAddress()));
                }

                // A PinvokeImpl field has no P/Invoke data to show: ImplMap rows only map methods.
                if (offsets is not null && offsets.TryGetValue(RidOf(fd), out uint offset))
                {
                    WriteLine("\t\tOffset : 0x" + Dasm.x8(offset));
                }

                WriteLine("");
            }
        }

        private void DisplayFieldInfo(uint fd, FieldDefinition field)
        {
            uint flags = (uint)field.Attributes;
            string name = Str(field.Name);
            byte[] sig = Blob(field.Signature);
            int cbSig = sig.Length;
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
            WriteLine("\t\tField Name: " + name + " (" + Dasm.X8(fd) + ")");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
            if (cbSig == 0)
            {
                WriteLine("\t\tERROR: no valid signature ");
            }
            else
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }
        }

        // ---------------------------------------------------------------- member refs

        private void DisplayMemberRefs(uint parent, string prefix)
        {
            if (parent == mdtTypeDef || parent == 0)
            {
                parent = TypeDefModule;
            }

            int n = 1;
            foreach (MemberReferenceHandle h in _md.MemberReferences)
            {
                if (Token(_md.GetMemberReference(h).Parent) != parent)
                {
                    continue;
                }

                uint mr = Token(h);
                WriteLine(prefix + "\tMemberRef #" + D(n++) + " (" + Dasm.x8(mr) + ")");
                WriteLine(prefix + "\t" + Dashes);
                DisplayMemberRefInfo(mr, prefix);
            }
        }

        private void DisplayMemberRefInfo(uint mr, string prefix)
        {
            string name = MemberRefName(mr);
            byte[] sig = Blob(_md.GetMemberReference(MetadataTokens.MemberReferenceHandle((int)RidOf(mr))).Signature);
            int cbSig = sig.Length;
            WriteLine(prefix + "\t\tMember: (" + Dasm.x8(mr) + ") " + name + ": ");
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
                WriteLine("TypeDef #" + D(n++) + " (" + Dasm.x8(td) + ")");
                WriteLine(Dashes);
                DisplayTypeDefInfo(td);
                WriteLine("");
            }
        }

        private void DisplayTypeDefProps(uint td, TypeDefinition t)
        {
            string name = TypeDefFullName(td) ?? throw new MetaInfoFailure("GetTypeDefProps failed.");
            uint flags = (uint)t.Attributes;
            uint extends = t.BaseType.IsNil ? mdtTypeRef : Token(t.BaseType);

            WriteLine("\tTypDefName: " + name + "  (" + Dasm.X8(td) + ")");
            WriteLine("\tFlags     : " + ClassFlags(flags) + " (" + Dasm.x8(flags) + ")");
            WriteLine("\tExtends   : " + Dasm.X8(extends) + " [" + TokenTypeName(extends) + "] " + TypeDeforRefName(extends));

            TypeLayout layout = t.GetLayout();
            if (HasClassLayout(td, layout))
            {
                WriteLine("\tLayout    : Packing:" + D(layout.PackingSize) + ", Size:" + D(layout.Size));
            }

            if ((flags & 7) >= 2)
            {
                TypeDefinitionHandle enclosing = t.GetDeclaringType();
                if (!enclosing.IsNil)
                {
                    uint tk = Token(enclosing);
                    WriteLine("\tEnclosingClass : " + TypeDeforRefName(tk) + " (" + Dasm.X8(tk) + ")");
                }
                else
                {
                    WriteLine("ERROR: EnclosingClass not found for NestedClass");
                }
            }
        }

        // Whether the type has a ClassLayout row. System.Reflection.Metadata reports a row with packing 0 and size 0 (as
        // emitted for explicit layout without .pack or .size) like a missing row, so that case is looked up in the table.
        private bool HasClassLayout(uint td, TypeLayout layout)
        {
            if (!layout.IsDefault)
            {
                return true;
            }

            for (uint i = 1; i <= Rows(TblClassLayout); i++)
            {
                if (Col(TblClassLayout, 2, i) == RidOf(td))
                {
                    return true;
                }
            }

            return false;
        }

        private void DisplayTypeDefInfo(uint td)
        {
            TypeDefinition t = _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)RidOf(td)));
            DisplayTypeDefProps(td, t);

            // The field offsets, if the type has a layout or any of its fields has an offset.
            bool hasLayout = HasClassLayout(td, t.GetLayout());
            Dictionary<uint, uint> offsets = new();
            foreach (FieldDefinitionHandle h in t.GetFields())
            {
                int offset = _md.GetFieldDefinition(h).GetOffset();
                if (offset != -1)
                {
                    hasLayout = true;
                }

                offsets.TryAdd(RidOf(Token(h)), (uint)offset);
            }

            DisplayGenericParams(t.GetGenericParameters(), "\t");
            DisplayFields(td, hasLayout && offsets.Count > 0 ? offsets : null);
            DisplayMethods(td);
            DisplayProperties(t);
            DisplayEvents(t);
            DisplayMethodImpls(t);
            DisplayPermissions(td, "");
            DisplayInterfaceImpls(td, t);
            DisplayCustomAttributes(td, "\t");
        }

        private void DisplayInterfaceImpls(uint td, TypeDefinition t)
        {
            int n = 1;
            foreach (InterfaceImplementationHandle h in t.GetInterfaceImplementations())
            {
                uint ii = Token(h);
                uint itf = Token(_md.GetInterfaceImplementation(h).Interface);
                WriteLine("\tInterfaceImpl #" + D(n++) + " (" + Dasm.x8(ii) + ")");
                WriteLine("\t" + Dashes);
                WriteLine("\t\tClass     : " + TypeDeforRefName(td));
                WriteLine("\t\tToken     : " + Dasm.X8(itf) + " [" + TokenTypeName(itf) + "] " + TypeDeforRefName(itf));
                DisplayCustomAttributes(ii, "\t\t");
                DisplayPermissions(ii, "\t");
                WriteLine("");
            }
        }

        private void DisplayMethodImpls(TypeDefinition t)
        {
            int n = 1;
            foreach (MethodImplementationHandle h in t.GetMethodImplementations())
            {
                MethodImplementation impl = _md.GetMethodImplementation(h);
                WriteLine("\n\tMethodImpl #" + D(n) + " (" + Dasm.x8((uint)n) + ")");
                n++;
                WriteLine("\t" + Dashes);
                WriteLine("\t\tMethod Body Token : 0x" + Dasm.x8(Token(impl.MethodBody)));
                WriteLine("\t\tMethod Declaration Token : 0x" + Dasm.x8(Token(impl.MethodDeclaration)));
                WriteLine("");
            }
        }

        // ---------------------------------------------------------------- properties and events

        private void DisplayProperties(TypeDefinition t)
        {
            int n = 1;
            foreach (PropertyDefinitionHandle h in t.GetProperties())
            {
                uint prop = Token(h);
                WriteLine("\tProperty #" + D(n++) + " (" + Dasm.x8(prop) + ")");
                WriteLine("\t" + Dashes);
                DisplayPropertyInfo(prop, _md.GetPropertyDefinition(h));
                DisplayPermissions(prop, "\t");
                WriteLine("");
            }
        }

        private void DisplayPropertyInfo(uint prop, PropertyDefinition p)
        {
            uint flags = (uint)p.Attributes;
            byte[] sig = Blob(p.Signature);
            int cbSig = sig.Length;
            PropertyAccessors accessors = p.GetAccessors();
            uint setter = Token(accessors.Setter), getter = Token(accessors.Getter);
            uint others = (uint)accessors.Others.Length;
            string name = Str(p.Name);
            WriteLine("\t\tProp.Name : " + name + " (" + Dasm.X8(prop) + ")");
            StringBuilder sb = new();
            Flag(sb, (flags & 0x200) != 0, "SpecialName");
            Flag(sb, (flags & 0x400) != 0, "RTSpecialName");
            Flag(sb, (flags & 0x1000) != 0, "HasDefault");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
            if (cbSig != 0)
            {
                DisplaySignature(sig, (uint)cbSig, "");
            }
            else
            {
                WriteLine("\t\tERROR: no valid signature ");
            }

            WriteLine("\t\tSetter    : (" + Dasm.x8(setter) + ") " + MemberDeforRefName(setter));
            WriteLine("\t\tGetter    : (" + Dasm.x8(getter) + ") " + MemberDeforRefName(getter));
            WriteLine("\t\t" + U(others) + " Others");
            DisplayCustomAttributes(prop, "\t\t");
        }

        private void DisplayEvents(TypeDefinition t)
        {
            int n = 1;
            foreach (EventDefinitionHandle h in t.GetEvents())
            {
                uint ev = Token(h);
                WriteLine("\tEvent #" + D(n++) + " (" + Dasm.x8(ev) + ")");
                WriteLine("\t" + Dashes);
                DisplayEventInfo(ev, _md.GetEventDefinition(h));
                DisplayPermissions(ev, "\t");
                WriteLine("");
            }
        }

        private void DisplayEventInfo(uint ev, EventDefinition e)
        {
            uint flags = (uint)e.Attributes;
            uint eventType = Token(e.Type);
            EventAccessors accessors = e.GetAccessors();
            uint addOn = Token(accessors.Adder), removeOn = Token(accessors.Remover), fire = Token(accessors.Raiser);
            uint others = (uint)accessors.Others.Length;
            string name = Str(e.Name);
            WriteLine("\t\tName      : " + name + " (" + Dasm.X8(ev) + ")");
            StringBuilder sb = new();
            Flag(sb, (flags & 0x200) != 0, "SpecialName");
            Flag(sb, (flags & 0x400) != 0, "RTSpecialName");
            WriteLine("\t\tFlags     : " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
            WriteLine("\t\tEventType : " + Dasm.X8(eventType) + " [" + TokenTypeName(eventType) + "]");
            WriteLine("\t\tAddOnMethd: (" + Dasm.x8(addOn) + ") " + MemberDeforRefName(addOn));
            WriteLine("\t\tRmvOnMethd: (" + Dasm.x8(removeOn) + ") " + MemberDeforRefName(removeOn));
            WriteLine("\t\tFireMethod: (" + Dasm.x8(fire) + ") " + MemberDeforRefName(fire));
            WriteLine("\t\t" + U(others) + " OtherMethods");
            DisplayCustomAttributes(ev, "\t\t");
        }

        // ---------------------------------------------------------------- custom attributes

        private IEnumerable<CustomAttributeHandle> EnumCustomAttributes(uint tk)
            => RidOf(tk) == 0 ? _md.CustomAttributes : _md.GetCustomAttributes(MetadataTokens.EntityHandle((int)tk));

        private void DisplayCustomAttributes(uint tk, string prefix)
        {
            int n = 1;
            foreach (CustomAttributeHandle h in EnumCustomAttributes(tk))
            {
                WriteLine(prefix + "CustomAttribute #" + D(n++) + " (" + Dasm.x8(Token(h)) + ")");
                WriteLine(prefix + Dashes);
                DisplayCustomAttributeInfo(_md.GetCustomAttribute(h), prefix);
            }
        }

        private void DisplayCustomAttributeInfo(CustomAttribute ca, string prefix)
        {
            uint type = Token(ca.Constructor);
            byte[] value = Blob(ca.Value);
            int cbValue = value.Length;
            WriteLine(prefix + "\tCustomAttribute Type: " + Dasm.x8(type));

            uint tk = type;
            string? methName = null;
            byte[] sig = [];
            int cbSig = 0;
            bool hasSig = false;
            switch (TypeOf(tk))
            {
                case mdtMemberRef:
                    if (!ValidRid(TblMemberRef, tk))
                    {
                        throw new MetaInfoFailure("GetNameFromToken failed.");
                    }

                    MemberReference mr = _md.GetMemberReference(MetadataTokens.MemberReferenceHandle((int)RidOf(tk)));
                    methName = Str(mr.Name);
                    sig = Blob(mr.Signature);
                    cbSig = sig.Length;
                    hasSig = true;
                    tk = Token(mr.Parent);
                    break;
                case mdtMethodDef:
                    if (!ValidRid(TblMethod, tk))
                    {
                        throw new MetaInfoFailure("GetNameFromToken failed.");
                    }

                    MethodDefinition md = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(tk)));
                    methName = Str(md.Name);
                    sig = Blob(md.Signature);
                    cbSig = sig.Length;
                    hasSig = true;
                    tk = Token(md.GetDeclaringType());
                    break;
            }

            string rcName = "";
            switch (TypeOf(tk))
            {
                case mdtTypeDef:
                    rcName = TypeDefFullName(tk) ?? throw new MetaInfoFailure("GetTypeDefProps failed.");
                    break;
                case mdtTypeRef:
                    rcName = TypeRefFullName(tk) ?? throw new MetaInfoFailure("GetTypeRefProps failed.");
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
                WriteLine(prefix + "\t            " + CString(value));
            }

            if (hasSig)
            {
                DecodeCustomAttributeArgs(sig, value, cbValue, prefix);
            }

            WriteLine("");
        }

        // Decodes the constructor arguments of a custom attribute blob (the common cases only).
        private void DecodeCustomAttributeArgs(byte[] sig, byte[] value, int cbValue, string prefix)
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

        private static uint ReadU4(byte[] blob, int i) => B(blob, i) | B(blob, i + 1) << 8 | B(blob, i + 2) << 16 | B(blob, i + 3) << 24;

        // CustomAttributeParser::GetString, printed with "%s": the text runs to the next NUL in the blob. On failure the
        // previous string is printed again.
        private static string? CaString(byte[] value, int cbValue, ref int cur, string? previous)
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
            return CString(value, start);
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

            DeclarativeSecurityAttributeHandleCollection list;
            if (RidOf(tk) == 0)
            {
                list = _md.DeclarativeSecurityAttributes;
            }
            else if (type == mdtTypeDef)
            {
                list = _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)RidOf(tk))).GetDeclarativeSecurityAttributes();
            }
            else if (type == mdtMethodDef)
            {
                list = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(tk))).GetDeclarativeSecurityAttributes();
            }
            else
            {
                list = _md.GetAssemblyDefinition().GetDeclarativeSecurityAttributes();
            }

            int n = 1;
            foreach (DeclarativeSecurityAttributeHandle h in list)
            {
                uint perm = Token(h);
                WriteLine(prefix + "\tPermission #" + D(n++) + " (" + Dasm.x8(perm) + ")");
                WriteLine(prefix + "\t" + Dashes);
                DisplayPermissionInfo(perm, _md.GetDeclarativeSecurityAttribute(h), prefix);
                WriteLine("");
            }
        }

        private void DisplayPermissionInfo(uint perm, DeclarativeSecurityAttribute attr, string prefix)
        {
            uint action = (uint)(short)attr.Action;
            byte[] blob = Blob(attr.PermissionSet);
            int cb = blob.Length;
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
        private static bool TrySigUncompress(byte[] blob, ref uint cbCur, out uint data)
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
        private static uint PackedLen(byte[] blob, long at, out int byteCount)
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

        private static byte[] PackedString(byte[] blob, ref uint cbCur)
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

        private void DisplayFieldMarshal(BlobHandle descriptor)
        {
            if (descriptor.IsNil)
            {
                return;
            }

            byte[] nt = Blob(descriptor);
            int cbNative = nt.Length;
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

        private void DisplayPinvokeInfo(uint md)
        {
            MethodImport import = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(md))).GetImport();
            if (import.Module.IsNil && import.Name.IsNil && import.Attributes == 0)
            {
                // The method has no ImplMap row.
                return;
            }

            uint flags = (uint)import.Attributes;
            string name = Str(import.Name);
            uint moduleRef = Token(import.Module);
            WriteLine("\t\tPinvoke Map Data:");
            WriteLine("\t\tEntry point:      " + name);
            WriteLine("\t\tModule ref:       " + Dasm.x8(moduleRef));
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
            WriteLine("\t\tMapping flags:    " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
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
        private static uint Uncompress(byte[] sig, long at, out uint data)
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

        // CorSigUncompressData(PCCOR_SIGNATURE&): no validation, advances the position.
        private static uint SigData(byte[] sig, ref long p)
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

        private static uint UncompressToken(byte[] sig, long at, out uint tk)
        {
            uint cb = Uncompress(sig, at, out uint v);
            tk = (v >> 2) | s_sigTokenTypes[v & 3];
            return cb;
        }

        private static uint UncompressSignedInt(byte[] sig, long at, out int value)
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

        private void DisplaySignature(byte[] sig, uint ulSigBlob, string prefix)
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

        // GetOneElementType: appends the element type at sig+start to _sigBuf; false on E_FAIL. The
        // size is not set when the type is not recognized at all.
        private bool GetOneElementType(byte[] sig, uint start, uint ulSigBlob, ref uint pcb)
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

        private string PrettyPrintSig(byte[] sig, int cbSig, string name)
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

        private void PrettyPrintType(byte[] sig, ref long p, long end, StringBuilder o)
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
                        o.Append(TypeRefFullName(tk) ?? "");
                    }
                    else if (TypeOf(tk) == mdtTypeDef)
                    {
                        o.Append(TypeDefFullName(tk) ?? "");
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
                    o.Append(Dasm.X8(tk)).Append(']');
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
        private string PrettyPrintSigInPlace(byte[] sig, ref long p, long len)
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
                WriteLine("TypeRef #" + D(n++) + " (" + Dasm.x8(tr) + ")");
                WriteLine(Dashes);
                uint scope = Token(_md.GetTypeReference(MetadataTokens.TypeReferenceHandle((int)rid)).ResolutionScope);
                string name = TypeRefFullName(tr)!;
                WriteLine("Token:             0x" + Dasm.x8(tr));
                WriteLine("ResolutionScope:   0x" + Dasm.x8(scope));
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
                WriteLine("TypeSpec #" + D(n++) + " (" + Dasm.x8(ts) + ")");
                WriteLine(Dashes);
                DisplayTypeSpecInfo(ts, "");
                DisplayMemberRefs(ts, "");
                WriteLine("");
            }
        }

        private void DisplayTypeSpecInfo(uint ts, string prefix)
        {
            _sigBuf.Clear();
            byte[] sig = Blob(_md.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle((int)RidOf(ts))).Signature);
            int cbSig = sig.Length;
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
                WriteLine("MethodSpec #" + D(n++) + " (" + Dasm.x8(ms) + ")");
                _sigBuf.Clear();
                MethodSpecification spec = _md.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle((int)rid));
                uint parent = Token(spec.Method);
                byte[] sig = Blob(spec.Signature);
                int cbSig = sig.Length;
                WriteLine("\tParent   : 0x" + Dasm.x8(parent));
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
                WriteLine("ModuleRef #" + D(n++) + " (" + Dasm.x8(mr) + ")");
                WriteLine(Dashes);
                WriteLine("\t\tModuleRef: (" + Dasm.x8(mr) + ") " + Str(_md.GetModuleReference(MetadataTokens.ModuleReferenceHandle((int)rid)).Name) + ": ");
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
                byte[] sig = Blob(_md.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle((int)rid)).Signature);
                int cbSig = sig.Length;
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

        private void DisplayAssemblyMetadata(Version version, string locale)
        {
            uint major = (uint)version.Major, minor = (uint)version.Minor, build = (uint)version.Build, revision = (uint)version.Revision;
            WriteLine("\tVersion: " + D(major) + "." + D(minor) + "." + D(build) + "." + D(revision));
            WriteLine("\tMajor Version: 0x" + Dasm.x8(major));
            WriteLine("\tMinor Version: 0x" + Dasm.x8(minor));
            WriteLine("\tBuild Number: 0x" + Dasm.x8(build));
            WriteLine("\tRevision Number: 0x" + Dasm.x8(revision));
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
            AssemblyDefinition asm = _md.GetAssemblyDefinition();
            uint hashAlg = (uint)asm.HashAlgorithm;
            uint flags = (uint)asm.Flags;
            byte[] pk = Blob(asm.PublicKey);
            int cbPk = pk.Length;
            string name = Str(asm.Name);
            string locale = Str(asm.Culture);
            WriteLine("Assembly");
            WriteLine(Dashes);
            WriteLine("\tToken: 0x" + Dasm.x8(mda));
            WriteLine("\tName : " + name);
            DumpHex("\tPublic Key    ", pk, cbPk, false, 24);
            WriteLine("\tHash Algorithm : 0x" + Dasm.x8(hashAlg));
            DisplayAssemblyMetadata(asm.Version, locale);
            WriteLine("\tFlags : " + AssemblyFlags(flags) + " (" + Dasm.x8(flags) + ")");
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
                WriteLine("AssemblyRef #" + D(n++) + " (" + Dasm.x8(ar) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + Dasm.x8(ar));
                AssemblyReference asmRef = _md.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle((int)rid));
                uint flags = (uint)asmRef.Flags;
                byte[] pk = Blob(asmRef.PublicKeyOrToken);
                int cbPk = pk.Length;
                string name = Str(asmRef.Name);
                string locale = Str(asmRef.Culture);
                byte[] hash = Blob(asmRef.HashValue);
                int cbHash = hash.Length;
                DumpHex("\tPublic Key or Token", pk, cbPk, false, 24);
                WriteLine("\tName: " + name);
                DisplayAssemblyMetadata(asmRef.Version, locale);
                DumpHex("\tHashValue Blob", hash, cbHash, false, 24);
                WriteLine("\tFlags: " + AssemblyFlags(flags) + " (" + Dasm.x8(flags) + ")");
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
                WriteLine("File #" + D(n++) + " (" + Dasm.x8(file) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + Dasm.x8(file));
                AssemblyFile f = _md.GetAssemblyFile(MetadataTokens.AssemblyFileHandle((int)rid));
                uint flags = f.ContainsMetadata ? 0u : 1u;
                string name = Str(f.Name);
                byte[] hash = Blob(f.HashValue);
                int cbHash = hash.Length;
                WriteLine("\tName : " + name);
                DumpHex("\tHashValue Blob ", hash, cbHash, false, 24);
                StringBuilder sb = new();
                Flag(sb, (flags & 1) == 0, "ContainsMetaData");
                Flag(sb, (flags & 1) != 0, "ContainsNoMetaData");
                WriteLine("\tFlags : " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
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
                WriteLine("ExportedType #" + D(n++) + " (" + Dasm.x8(et) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + Dasm.x8(et));
                ExportedType e = _md.GetExportedType(MetadataTokens.ExportedTypeHandle((int)rid));
                uint flags = (uint)e.Attributes;
                uint typeDef = (uint)e.GetTypeDefinitionId();
                string name = MakePath(Str(e.Namespace), Str(e.Name));
                uint impl = Token(e.Implementation);
                WriteLine("\tName: " + name);
                WriteLine("\tImplementation token: 0x" + Dasm.x8(impl));
                WriteLine("\tTypeDef token: 0x" + Dasm.x8(typeDef));
                WriteLine("\tFlags     : " + ClassFlags(flags) + " (" + Dasm.x8(flags) + ")");
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
                WriteLine("ManifestResource #" + D(n++) + " (" + Dasm.x8(mr) + ")");
                WriteLine(Dashes);
                WriteLine("\tToken: 0x" + Dasm.x8(mr));
                ManifestResource res = _md.GetManifestResource(MetadataTokens.ManifestResourceHandle((int)rid));
                uint offset = (uint)res.Offset;
                uint flags = (uint)res.Attributes;
                string name = Str(res.Name);
                uint impl = Token(res.Implementation);
                WriteLine("Name: " + name);
                WriteLine("Implementation token: 0x" + Dasm.x8(impl));
                WriteLine("Offset: 0x" + Dasm.x8(offset));
                StringBuilder sb = new();
                Flag(sb, (flags & 7) == 1, "Public");
                Flag(sb, (flags & 7) == 2, "Private");
                WriteLine("\tFlags: " + NoneIfEmpty(sb) + " (" + Dasm.x8(flags) + ")");
                DisplayCustomAttributes(mr, "\t");
                WriteLine("");
                WriteLine("");
            }
        }

        // ---------------------------------------------------------------- user strings

        private void DisplayUserStrings()
        {
            // Every non-empty entry is collected first; a bad entry fails the whole enumeration.
            List<(uint Index, int Size, string Value)> strings = new();
            for (UserStringHandle h = _md.GetNextHandle(default(UserStringHandle)); !h.IsNil; h = _md.GetNextHandle(h))
            {
                int index = MetadataTokens.GetHeapOffset(h);
                int size = HeapEntrySize(HeapIndex.UserString, index);
                if (size != 0)
                {
                    strings.Add(((uint)index, size, _md.GetUserString(h)));
                }
            }

            bool first = true;
            bool unprint = false;
            foreach ((uint index, int size, string value) in strings)
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

                char[] chars = value.ToCharArray();
                int cch = chars.Length;

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
            for (int td = 2; td <= Rows(TblTypeDef); td++)
            {
                TypeDefinition t = _md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(td));
                bool isInterface = ((uint)t.Attributes & 0x20) != 0;
                foreach (MethodDefinitionHandle h in t.GetMethods())
                {
                    MethodDefinition m = _md.GetMethodDefinition(h);
                    uint flags = (uint)m.Attributes;
                    uint implFlags = (uint)m.ImplAttributes;
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

                        tokens.Add(Token(h));
                    }
                }
            }

            // RefToDefOptimization resolves (and reparents) the global MemberRefs that match a member of <Module>.
            foreach (MemberReferenceHandle h in _md.MemberReferences)
            {
                MemberReference mr = _md.GetMemberReference(h);
                if (Token(mr.Parent) == TypeDefModule && !ResolvesToModuleMember(mr))
                {
                    tokens.Add(Token(h));
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

        private bool ResolvesToModuleMember(MemberReference mr)
        {
            string name = Str(mr.Name);
            byte[] refSig = Blob(mr.Signature);
            foreach (uint md in EnumMethods(TypeDefModule))
            {
                MethodDefinition m = _md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)RidOf(md)));
                if (Str(m.Name) == name && Blob(m.Signature).AsSpan().SequenceEqual(refSig))
                {
                    return true;
                }
            }

            foreach (uint fd in EnumFields(TypeDefModule))
            {
                FieldDefinition f = _md.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle((int)RidOf(fd)));
                if (Str(f.Name) == name && Blob(f.Signature).AsSpan().SequenceEqual(refSig))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- DumpHex

        // Dumps cbData bytes of data from start; past the end of data, the bytes show as 0.
        private void DumpHex(string prefix, byte[] data, int cbData, bool text, int nLine, int start = 0)
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

                    sb.Append(((byte)B(data, start + pos + i)).ToString("x2", CultureInfo.InvariantCulture)).Append(' ');
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
                        uint b = B(data, start + pos + i);
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

        private uint BlobSize(uint offset) => (uint)_md.GetBlobReader(MetadataTokens.BlobHandle((int)offset)).Length;

        private string RawCol(int table, int col, uint rid, bool stats)
        {
            byte type = s_tables[table].ColTypes[col];
            if (type < CT)
            {
                return RawNameOfType(type) + "[" + Hx(ColToken(table, col, rid)) + "]";
            }

            if (type < iSHORT)
            {
                return s_codedTokenNames[type - CT] + "[" + Dasm.x8(ColToken(table, col, rid)) + "]";
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
                    return Dasm.x8(val);
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
                        uint cb = BlobSize(val) + 1;
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
                        uint cb = BlobSize(val);
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
            WriteLine("Metadata section: 0x" + Dasm.x8(U32At(0)) + ", version: " + D(U16At(4)) + "." + D(U16At(6)) + ", extra: " + D(U32At(8))
                + ", version len: " + D(verLen) + ", version: " + CString(_d, 16));
            WriteLine("           flags: 0x" + U8At(header).ToString("x2", CultureInfo.InvariantCulture) + ", streams: " + D(U16At(header + 2)));
            if ((_filter & DumpMoreHex) != 0)
            {
                int cb = header + 4;
                if (_streams.Count > 0)
                {
                    cb = _streams[0].Offset;
                }

                DumpHex("        ", _d, cb, true, 16);
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
                    DumpHex("        ", _d, 24, true, 16, t);
                }
            }

            WriteLine("");

            uint strings = (uint)HeapSize(HeapIndex.String), blobs = (uint)HeapSize(HeapIndex.Blob), guids = (uint)HeapSize(HeapIndex.Guid);
            uint userStrings = (uint)HeapSize(HeapIndex.UserString);
            Write("Strings: " + D(strings) + "(" + AltHex(strings) + ")");
            Write(", Blobs: " + D(blobs) + "(" + AltHex(blobs) + ")");
            Write(", Guids: " + D(guids) + "(" + AltHex(guids) + ")");
            WriteLine(", User strings: " + D(userStrings) + "(" + AltHex(userStrings) + ")");

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
            WriteLine("Strings," + D(HeapSize(HeapIndex.String)));
            WriteLine("Blobs," + D(HeapSize(HeapIndex.Blob)));
            WriteLine("Guids," + D(HeapSize(HeapIndex.Guid)));
            for (int tbl = 0; tbl < _tableCount; tbl++)
            {
                uint cRows = Rows(tbl);
                uint cbRow = (uint)_cbRec[tbl];
                WriteLine(s_tables[tbl].Name + "," + D(cbRow * cRows) + "," + D(cRows) + "," + D(cbRow));
            }
        }

        private void DumpHeaps()
        {
            uint size = (uint)HeapSize(HeapIndex.Blob);
            WriteLine("");
            WriteLine("Blob Heap:  " + D(size) + "(" + AltHex(size) + ") bytes");
            BlobHandle h = default;
            do
            {
                byte[] data = Blob(h);
                DumpHex(PadL(Hx((uint)MetadataTokens.GetHeapOffset(h)), 5) + "," + PadR(Hx((uint)data.Length), 2), data, data.Length, true, 16);
                h = _md.GetNextHandle(h);
            }
            while (!h.IsNil);

            size = (uint)HeapSize(HeapIndex.String);
            WriteLine("");
            WriteLine("String Heap:  " + D(size) + "(" + AltHex(size) + ") bytes");
            uint offset = 0;
            while (true)
            {
                byte[] s = StringBytes(offset) ?? Array.Empty<byte>();
                if ((_filter & DumpMoreHex) != 0)
                {
                    // The entry with its terminating NUL.
                    DumpHex(Dasm.x8(offset), s, s.Length + 1, true, 16);
                }
                else if (s.Length != 0)
                {
                    Write(Dasm.x8(offset) + ": " + Utf8(s) + "\n");
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
