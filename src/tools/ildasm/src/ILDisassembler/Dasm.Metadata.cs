// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILDisassembler;

// Token level access to the metadata, shaped after the IMDInternalImport calls made by the native ildasm.
internal sealed partial class Dasm
{
    internal const int mdtModule = 0x00000000;
    internal const int mdtTypeRef = 0x01000000;
    internal const int mdtTypeDef = 0x02000000;
    internal const int mdtFieldDef = 0x04000000;
    internal const int mdtMethodDef = 0x06000000;
    internal const int mdtParamDef = 0x08000000;
    internal const int mdtInterfaceImpl = 0x09000000;
    internal const int mdtMemberRef = 0x0a000000;
    internal const int mdtCustomAttribute = 0x0c000000;
    internal const int mdtPermission = 0x0e000000;
    internal const int mdtSignature = 0x11000000;
    internal const int mdtEvent = 0x14000000;
    internal const int mdtProperty = 0x17000000;
    internal const int mdtModuleRef = 0x1a000000;
    internal const int mdtTypeSpec = 0x1b000000;
    internal const int mdtAssembly = 0x20000000;
    internal const int mdtAssemblyRef = 0x23000000;
    internal const int mdtFile = 0x26000000;
    internal const int mdtExportedType = 0x27000000;
    internal const int mdtManifestResource = 0x28000000;
    internal const int mdtGenericParam = 0x2a000000;
    internal const int mdtMethodSpec = 0x2b000000;
    internal const int mdtGenericParamConstraint = 0x2c000000;
    internal const int mdtString = 0x70000000;
    internal const int mdtBaseType = 0x72000000;

    internal const int mdTokenNil = 0;
    internal const int mdTypeDefNil = mdtTypeDef;
    internal const int mdTypeRefNil = mdtTypeRef;

    internal static int TypeFromToken(int tk) => (int)((uint)tk & 0xFF000000);

    internal static int RidFromToken(int tk) => tk & 0x00FFFFFF;

    internal static bool IsNilToken(int tk) => RidFromToken(tk) == 0;

    private static int Token(EntityHandle h) => h.IsNil ? 0 : MetadataTokens.GetToken(h);

    private static int Token(Handle h) => h.IsNil ? 0 : MetadataTokens.GetToken(h);

    // A nil coded index keeps its table: a nil TypeDefOrRef is a nil TypeDef token, as the native reader reports it.
    private static int TokenOrNil(EntityHandle h, int nilToken) => h.IsNil ? nilToken : MetadataTokens.GetToken(h);

    private static EntityHandle Handle(int tk) => MetadataTokens.EntityHandle(tk);

    internal bool IsValidToken(int tk)
    {
        int type = TypeFromToken(tk);
        int rid = RidFromToken(tk);
        if (type == mdtString)
        {
            return rid < _md.GetHeapSize(HeapIndex.UserString);
        }

        int table = type >> 24;
        if (table > (int)TableIndex.CustomDebugInformation || table >= 0x2D)
        {
            return false;
        }

        return rid != 0 && rid <= _md.GetTableRowCount((TableIndex)table);
    }

    private int RowCount(TableIndex t) => _md.GetTableRowCount(t);

    private byte[] Blob(BlobHandle h) => h.IsNil ? Array.Empty<byte>() : _md.GetBlobBytes(h);

    private string Str(StringHandle h) => h.IsNil ? "" : _md.GetString(h);

    // ---------------------------------------------------------------- types

    private (string Name, string Namespace) GetNameOfTypeDef(int tk)
    {
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk));
        return (Str(td.Name), Str(td.Namespace));
    }

    private (string Name, string Namespace) GetNameOfTypeRef(int tk)
    {
        TypeReference tr = _md.GetTypeReference((TypeReferenceHandle)Handle(tk));
        return (Str(tr.Name), Str(tr.Namespace));
    }

    private int GetResolutionScopeOfTypeRef(int tk)
    {
        TypeReference tr = _md.GetTypeReference((TypeReferenceHandle)Handle(tk));
        return Token(tr.ResolutionScope);
    }

    // GetNestedClassProps: the enclosing class, or mdTypeDefNil.
    private int GetEnclosingClass(int tk)
    {
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk));
        TypeDefinitionHandle h = td.GetDeclaringType();
        return h.IsNil ? mdTypeDefNil : Token(h);
    }

    private (TypeAttributes Attrs, int Extends) GetTypeDefProps(int tk)
    {
        TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)Handle(tk));
        return (td.Attributes, TokenOrNil(td.BaseType, mdTypeDefNil));
    }

    private byte[] GetTypeSpecSig(int tk)
        => Blob(_md.GetTypeSpecification((TypeSpecificationHandle)Handle(tk)).Signature);

    private byte[] GetStandaloneSig(int tk)
        => Blob(_md.GetStandaloneSignature((StandaloneSignatureHandle)Handle(tk)).Signature);

    private bool FindTypeDefByName(string ns, string name, out int tk)
    {
        foreach (TypeDefinitionHandle h in _md.TypeDefinitions)
        {
            TypeDefinition td = _md.GetTypeDefinition(h);
            if (td.GetDeclaringType().IsNil && _md.StringComparer.Equals(td.Name, name) && _md.StringComparer.Equals(td.Namespace, ns))
            {
                tk = Token(h);
                return true;
            }
        }

        tk = 0;
        return false;
    }

    // ---------------------------------------------------------------- members

    private string GetNameOfMethodDef(int tk) => Str(_md.GetMethodDefinition((MethodDefinitionHandle)Handle(tk)).Name);

    private MethodAttributes GetMethodDefAttrs(int tk) => _md.GetMethodDefinition((MethodDefinitionHandle)Handle(tk)).Attributes;

    private byte[] GetSigOfMethodDef(int tk) => Blob(_md.GetMethodDefinition((MethodDefinitionHandle)Handle(tk)).Signature);

    private int GetParentOfMethodDef(int tk) => Token(_md.GetMethodDefinition((MethodDefinitionHandle)Handle(tk)).GetDeclaringType());

    private string GetNameOfFieldDef(int tk) => Str(_md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).Name);

    private FieldAttributes GetFieldDefAttrs(int tk) => _md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).Attributes;

    private byte[] GetSigOfFieldDef(int tk) => Blob(_md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).Signature);

    private int GetParentOfFieldDef(int tk) => Token(_md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).GetDeclaringType());

    private (string Name, byte[] Sig, int Parent) GetMemberRefProps(int tk)
    {
        MemberReference mr = _md.GetMemberReference((MemberReferenceHandle)Handle(tk));
        return (Str(mr.Name), Blob(mr.Signature), TokenOrNil(mr.Parent, mdTypeRefNil));
    }

    private (int Method, byte[] Instantiation) GetMethodSpecProps(int tk)
    {
        MethodSpecification ms = _md.GetMethodSpecification((MethodSpecificationHandle)Handle(tk));
        return (Token(ms.Method), Blob(ms.Signature));
    }

    // GetParentToken for the member kinds the native tool asks about.
    private int GetParentToken(int tk)
    {
        switch (TypeFromToken(tk))
        {
            case mdtMethodDef: return GetParentOfMethodDef(tk);
            case mdtFieldDef: return GetParentOfFieldDef(tk);
            case mdtMemberRef: return GetMemberRefProps(tk).Parent;
            case mdtMethodSpec: return GetMethodSpecProps(tk).Method;
            case mdtParamDef:
                foreach (MethodDefinitionHandle mh in _md.MethodDefinitions)
                {
                    foreach (ParameterHandle ph in _md.GetMethodDefinition(mh).GetParameters())
                    {
                        if (Token(ph) == tk)
                        {
                            return Token(mh);
                        }
                    }
                }

                return 0;
            case mdtEvent:
            case mdtProperty:
                return 0;
            default:
                return 0;
        }
    }

    private bool IsDup(int tk) => _dups.Contains(tk);

    // MAKE_NAME_IF_NONE: unnamed and duplicate items get synthetic names.
    private static readonly string[] s_stdNamePrefix =
    [
        "MO", "TR", "TD", "", "FD", "", "MD", "", "PA", "II", "MR", "", "CA", "", "PE", "", "", "SG", "", "", "EV",
        "", "", "PR", "", "", "MOR", "TS", "", "", "", "", "AS", "", "", "AR", "", "", "FL", "ET", "MAR",
    ];

    private string MakeNameIfNone(string? name, int tk)
    {
        if (string.IsNullOrEmpty(name) || IsDup(tk))
        {
            int table = (int)((uint)tk >> 24);
            string prefix = table < s_stdNamePrefix.Length ? s_stdNamePrefix[table] : "";
            return "$" + prefix + "$" + RidFromToken(tk).ToString("X");
        }

        return name;
    }

    // ---------------------------------------------------------------- generic parameters

    // Note: default(...HandleCollection) values are not safe to enumerate, hence the arrays. The owner map is built
    // from the raw table because GetGenericParameters() truncates the count of more than 65535 parameters.
    private Dictionary<int, List<GenericParameterHandle>>? _genericParamsByOwner;

    private ImmutableArray<GenericParameterHandle> GetGenericParams(int owner)
    {
        if (_genericParamsByOwner is null)
        {
            _genericParamsByOwner = new Dictionary<int, List<GenericParameterHandle>>();
            int n = RowCount(TableIndex.GenericParam);
            for (int i = 1; i <= n; i++)
            {
                GenericParameterHandle h = MetadataTokens.GenericParameterHandle(i);
                int o = Token(_md.GetGenericParameter(h).Parent);
                if (!_genericParamsByOwner.TryGetValue(o, out List<GenericParameterHandle>? list))
                {
                    _genericParamsByOwner[o] = list = new List<GenericParameterHandle>();
                }

                list.Add(h);
            }
        }

        return _genericParamsByOwner.TryGetValue(owner, out List<GenericParameterHandle>? l)
            ? ImmutableArray.CreateRange(l)
            : ImmutableArray<GenericParameterHandle>.Empty;
    }

    // ---------------------------------------------------------------- custom attributes, constants, marshaling

    private ImmutableArray<CustomAttributeHandle> GetCustomAttributes(int owner)
    {
        if (!IsValidToken(owner))
        {
            return ImmutableArray<CustomAttributeHandle>.Empty;
        }

        return Arr(_md.GetCustomAttributes(Handle(owner)));
    }

    private byte[]? GetMarshalBlob(int tk)
    {
        if (RidFromToken(tk) == 0)
        {
            return null;
        }

        BlobHandle h = TypeFromToken(tk) switch
        {
            mdtFieldDef => _md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).GetMarshallingDescriptor(),
            mdtParamDef => _md.GetParameter((ParameterHandle)Handle(tk)).GetMarshallingDescriptor(),
            _ => default,
        };
        return h.IsNil ? null : _md.GetBlobBytes(h);
    }

    private ConstantHandle GetConstant(int tk)
    {
        return TypeFromToken(tk) switch
        {
            mdtFieldDef => _md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).GetDefaultValue(),
            mdtParamDef => _md.GetParameter((ParameterHandle)Handle(tk)).GetDefaultValue(),
            mdtProperty => _md.GetPropertyDefinition((PropertyDefinitionHandle)Handle(tk)).GetDefaultValue(),
            _ => default,
        };
    }

    private ImmutableArray<DeclarativeSecurityAttributeHandle> GetDeclSecurity(int owner)
    {
        if (!IsValidToken(owner))
        {
            return ImmutableArray<DeclarativeSecurityAttributeHandle>.Empty;
        }

        return TypeFromToken(owner) switch
        {
            mdtTypeDef => Arr(_md.GetTypeDefinition((TypeDefinitionHandle)Handle(owner)).GetDeclarativeSecurityAttributes()),
            mdtMethodDef => Arr(_md.GetMethodDefinition((MethodDefinitionHandle)Handle(owner)).GetDeclarativeSecurityAttributes()),
            mdtAssembly => Arr(_md.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()),
            _ => ImmutableArray<DeclarativeSecurityAttributeHandle>.Empty,
        };
    }

    private (MethodImportAttributes Flags, string Name, int ModuleRef)? GetPinvokeMap(int tk)
    {
        MethodImport mi;
        if (TypeFromToken(tk) == mdtMethodDef)
        {
            mi = _md.GetMethodDefinition((MethodDefinitionHandle)Handle(tk)).GetImport();
        }
        else
        {
            // SRM exposes no ImplMap for fields; find the row by hand.
            return GetFieldPinvokeMap(tk);
        }

        if (mi.Module.IsNil && mi.Name.IsNil && mi.Attributes == 0)
        {
            return null;
        }

        return (mi.Attributes, Str(mi.Name), Token(mi.Module));
    }

    private (MethodImportAttributes Flags, string Name, int ModuleRef)? GetFieldPinvokeMap(int tk)
    {
        int rows = RowCount(TableIndex.ImplMap);
        if (rows == 0)
        {
            return null;
        }

        // ImplMap: MappingFlags (2), MemberForwarded (MemberForwarded coded index), ImportName (string), ImportScope (ModuleRef index)
        int rowSize = _md.GetTableRowSize(TableIndex.ImplMap);
        int memberForwardedSize = CodedIndexSize(1, TableIndex.Field, TableIndex.MethodDef);
        int stringSize = _md.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        int moduleRefSize = RowCount(TableIndex.ModuleRef) > ushort.MaxValue ? 4 : 2;
        int baseOffset = _md.GetTableMetadataOffset(TableIndex.ImplMap);
        BlobReader r = _metadataBlock.GetReader();
        for (int i = 0; i < rows; i++)
        {
            r.Offset = baseOffset + i * rowSize;
            ushort flags = r.ReadUInt16();
            uint member = memberForwardedSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            uint nameOffset = stringSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            uint scope = moduleRefSize == 2 ? r.ReadUInt16() : r.ReadUInt32();
            if ((member & 1) == 0 && (int)(member >> 1) == RidFromToken(tk))
            {
                string name = _md.GetString(MetadataTokens.StringHandle((int)nameOffset));
                return ((MethodImportAttributes)flags, name, (int)scope == 0 ? 0 : mdtModuleRef | (int)scope);
            }
        }

        return null;
    }

    private int CodedIndexSize(int tagBits, params TableIndex[] tables)
    {
        int max = 0;
        foreach (TableIndex t in tables)
        {
            max = Math.Max(max, RowCount(t));
        }

        return max < (1 << (16 - tagBits)) ? 2 : 4;
    }

    private uint GetFieldRva(int tk)
        => (uint)_md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).GetRelativeVirtualAddress();

    private int GetFieldOffset(int tk)
        => _md.GetFieldDefinition((FieldDefinitionHandle)Handle(tk)).GetOffset();

    // ---------------------------------------------------------------- signature decoding primitives

    // CorSigUncompressData
    private static uint UncompressData(byte[] sig, ref int p)
    {
        byte b = sig[p];
        if ((b & 0x80) == 0)
        {
            p++;
            return b;
        }

        if ((b & 0xC0) == 0x80)
        {
            uint v = (uint)((b & 0x3F) << 8 | sig[p + 1]);
            p += 2;
            return v;
        }

        if ((b & 0xE0) == 0xC0)
        {
            uint v = (uint)((b & 0x1F) << 24 | sig[p + 1] << 16 | sig[p + 2] << 8 | sig[p + 3]);
            p += 4;
            return v;
        }

        // Invalid compressed integer: consume one byte like the native decoder.
        p++;
        return 0xFFFFFFFF;
    }

    private static readonly int[] s_tokenTypes = [mdtTypeDef, mdtTypeRef, mdtTypeSpec, mdtBaseType];

    // CorSigUncompressToken
    private static int UncompressToken(byte[] sig, ref int p)
    {
        uint v = UncompressData(sig, ref p);
        return (int)(v >> 2) | s_tokenTypes[v & 3];
    }

    // CorSigUncompressSignedInt
    private static int UncompressSignedInt(byte[] sig, ref int p)
    {
        int start = p;
        uint v = UncompressData(sig, ref p);
        int len = p - start;
        bool negative = (v & 1) != 0;
        v >>= 1;
        if (negative)
        {
            v |= len switch
            {
                1 => 0xFFFFFFC0,
                2 => 0xFFFFE000,
                _ => 0xF0000000,
            };
        }

        return (int)v;
    }

    private static uint ReadUInt32(byte[] b, int p) => (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);

    private static uint ReadUInt32(ReadOnlySpan<byte> b, int p) => (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);

    private static bool SigEquals(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b);

    private static ImmutableArray<T> Arr<T>(IEnumerable<T> e) => ImmutableArray.CreateRange(e);
}
