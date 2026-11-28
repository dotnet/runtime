// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILDisassembler;

// Metadata access helpers.
internal sealed partial class Dasm
{
    // The token of a handle, as printed in comments and compared with tokens read from the image; 0 for any nil handle.
    private static int Token(EntityHandle h) => h.IsNil ? 0 : MetadataTokens.GetToken(h);

    private static int Token(Handle h) => h.IsNil ? 0 : MetadataTokens.GetToken(h);

    // Whether the handle refers to an existing row.
    internal bool IsValid(EntityHandle h)
        => !h.IsNil && MetadataTokens.TryGetTableIndex(h.Kind, out TableIndex table) && MetadataTokens.GetRowNumber(h) <= _md.GetTableRowCount(table);

    internal bool IsValid(UserStringHandle h) => MetadataTokens.GetHeapOffset(h) < _md.GetHeapSize(HeapIndex.UserString);

    // A token read from the image (IL, signatures, the CLR header, vtable fixups, exports) as a handle, if it refers to
    // an existing row. Tokens that do not are only printed.
    private bool TryGetHandle(int token, out EntityHandle h)
    {
        // Rejects user string, heap and virtual tokens, which are not entities.
        if ((uint)token >> 24 < (uint)HandleKind.UserString)
        {
            h = MetadataTokens.EntityHandle(token);
            if (IsValid(h))
            {
                return true;
            }
        }

        h = default;
        return false;
    }

    // Like TryGetHandle, accepting user strings too (ldstr).
    private bool TryGetHandle(int token, out Handle h)
    {
        if ((uint)token >> 24 == (uint)HandleKind.UserString)
        {
            UserStringHandle us = MetadataTokens.UserStringHandle(token & 0xFFFFFF);
            h = us;
            return IsValid(us);
        }

        bool valid = TryGetHandle(token, out EntityHandle eh);
        h = eh;
        return valid;
    }

    internal int RowCount(TableIndex t) => _md.GetTableRowCount(t);

    // The global <Module> type: the first TypeDef row. COMPAT: its members are printed without their class.
    private static readonly TypeDefinitionHandle s_moduleType = MetadataTokens.TypeDefinitionHandle(1);

    internal byte[] Blob(BlobHandle h) => h.IsNil ? Array.Empty<byte>() : _md.GetBlobBytes(h);

    internal string Str(StringHandle h) => h.IsNil ? "" : _md.GetString(h);

    // ---------------------------------------------------------------- types

    internal (string Name, string Namespace) GetNameOfTypeDef(TypeDefinitionHandle h)
    {
        TypeDefinition td = _md.GetTypeDefinition(h);
        return (Str(td.Name), Str(td.Namespace));
    }

    private (string Name, string Namespace) GetNameOfTypeRef(TypeReferenceHandle h)
    {
        TypeReference tr = _md.GetTypeReference(h);
        return (Str(tr.Name), Str(tr.Namespace));
    }

    // The name of a TypeDef or TypeRef; empty for anything else.
    internal (string Name, string Namespace) GetNameOfType(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeDefinition => GetNameOfTypeDef((TypeDefinitionHandle)h),
        HandleKind.TypeReference => GetNameOfTypeRef((TypeReferenceHandle)h),
        _ => ("", ""),
    };

    private bool FindTypeDefByName(string ns, string name, out TypeDefinitionHandle found)
    {
        foreach (TypeDefinitionHandle h in _md.TypeDefinitions)
        {
            TypeDefinition td = _md.GetTypeDefinition(h);
            if (td.GetDeclaringType().IsNil && _md.StringComparer.Equals(td.Name, name) && _md.StringComparer.Equals(td.Namespace, ns))
            {
                found = h;
                return true;
            }
        }

        found = default;
        return false;
    }

    // ---------------------------------------------------------------- members

    // The signature of a MethodDef or MemberRef, or null for anything else.
    internal byte[]? GetMethodSig(EntityHandle h) => h.Kind switch
    {
        HandleKind.MethodDefinition => Blob(_md.GetMethodDefinition((MethodDefinitionHandle)h).Signature),
        HandleKind.MemberReference => Blob(_md.GetMemberReference((MemberReferenceHandle)h).Signature),
        _ => null,
    };

    // A nil handle of any table as the nil token (0), which prints as [*] where a type is expected.
    private static EntityHandle NilAsDefault(EntityHandle h) => h.IsNil ? default : h;

    // The parent of a member; a nil MemberRef parent stays a (nil) TypeRef, unlike the other nil parents.
    private EntityHandle GetParent(EntityHandle h)
    {
        if (h.IsNil)
        {
            return default;
        }

        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
                return NilAsDefault(_md.GetMethodDefinition((MethodDefinitionHandle)h).GetDeclaringType());
            case HandleKind.FieldDefinition:
                return NilAsDefault(_md.GetFieldDefinition((FieldDefinitionHandle)h).GetDeclaringType());
            case HandleKind.MemberReference:
            {
                EntityHandle parent = _md.GetMemberReference((MemberReferenceHandle)h).Parent;
                return parent.IsNil ? default(TypeReferenceHandle) : parent;
            }

            case HandleKind.MethodSpecification:
                return NilAsDefault(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);

            case HandleKind.Parameter:
                foreach (MethodDefinitionHandle mh in _md.MethodDefinitions)
                {
                    foreach (ParameterHandle ph in _md.GetMethodDefinition(mh).GetParameters())
                    {
                        if (ph == (ParameterHandle)h)
                        {
                            return mh;
                        }
                    }
                }

                return default;
            default:
                return default;
        }
    }

    private bool IsDup(EntityHandle h) => _dups.Contains(h);

    // MAKE_NAME_IF_NONE: unnamed and duplicate items get synthetic names, from the table and the row number.
    private static readonly string[] s_stdNamePrefix =
    [
        "MO", "TR", "TD", "", "FD", "", "MD", "", "PA", "II", "MR", "", "CA", "", "PE", "", "", "SG", "", "", "EV",
        "", "", "PR", "", "", "MOR", "TS", "", "", "", "", "AS", "", "", "AR", "", "", "FL", "ET", "MAR",
    ];

    internal string MakeNameIfNone(string? name, EntityHandle h)
    {
        if (string.IsNullOrEmpty(name) || IsDup(h))
        {
            int table = (int)h.Kind;
            string prefix = table < s_stdNamePrefix.Length ? s_stdNamePrefix[table] : "";
            return "$" + prefix + "$" + MetadataTokens.GetRowNumber(h).ToString("X");
        }

        return name;
    }

    // ---------------------------------------------------------------- generic parameters

    // Note: default(...HandleCollection) values are not safe to enumerate, hence the arrays. The owner map is built from
    // every row because TypeDefinition/MethodDefinition.GetGenericParameters() truncate the count of more than 65535
    // parameters and need a sorted table.
    private Dictionary<EntityHandle, List<GenericParameterHandle>>? _genericParamsByOwner;

    private ImmutableArray<GenericParameterHandle> GetGenericParams(EntityHandle owner)
    {
        if (_genericParamsByOwner is null)
        {
            _genericParamsByOwner = new Dictionary<EntityHandle, List<GenericParameterHandle>>();
            int n = RowCount(TableIndex.GenericParam);
            for (int i = 1; i <= n; i++)
            {
                GenericParameterHandle h = MetadataTokens.GenericParameterHandle(i);
                EntityHandle o = _md.GetGenericParameter(h).Parent;
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

    private ImmutableArray<CustomAttributeHandle> GetCustomAttributes(EntityHandle owner)
    {
        if (!IsValid(owner))
        {
            return ImmutableArray<CustomAttributeHandle>.Empty;
        }

        return Arr(_md.GetCustomAttributes(owner));
    }

    private byte[]? GetMarshalBlob(EntityHandle h)
    {
        if (h.IsNil)
        {
            return null;
        }

        BlobHandle blob = h.Kind switch
        {
            HandleKind.FieldDefinition => _md.GetFieldDefinition((FieldDefinitionHandle)h).GetMarshallingDescriptor(),
            HandleKind.Parameter => _md.GetParameter((ParameterHandle)h).GetMarshallingDescriptor(),
            _ => default,
        };
        return blob.IsNil ? null : _md.GetBlobBytes(blob);
    }

    private ConstantHandle GetConstant(EntityHandle h) => h.Kind switch
    {
        HandleKind.FieldDefinition => _md.GetFieldDefinition((FieldDefinitionHandle)h).GetDefaultValue(),
        HandleKind.Parameter => _md.GetParameter((ParameterHandle)h).GetDefaultValue(),
        HandleKind.PropertyDefinition => _md.GetPropertyDefinition((PropertyDefinitionHandle)h).GetDefaultValue(),
        _ => default,
    };

    private ImmutableArray<DeclarativeSecurityAttributeHandle> GetDeclSecurity(EntityHandle owner)
    {
        if (!IsValid(owner))
        {
            return ImmutableArray<DeclarativeSecurityAttributeHandle>.Empty;
        }

        return owner.Kind switch
        {
            HandleKind.TypeDefinition => Arr(_md.GetTypeDefinition((TypeDefinitionHandle)owner).GetDeclarativeSecurityAttributes()),
            HandleKind.MethodDefinition => Arr(_md.GetMethodDefinition((MethodDefinitionHandle)owner).GetDeclarativeSecurityAttributes()),
            HandleKind.AssemblyDefinition => Arr(_md.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()),
            _ => ImmutableArray<DeclarativeSecurityAttributeHandle>.Empty,
        };
    }

    private (MethodImportAttributes Flags, string Name, ModuleReferenceHandle ModuleRef)? GetPinvokeMap(EntityHandle h)
    {
        if (h.Kind != HandleKind.MethodDefinition)
        {
            return GetFieldPinvokeMap((FieldDefinitionHandle)h);
        }

        MethodImport mi = _md.GetMethodDefinition((MethodDefinitionHandle)h).GetImport();
        if (mi.Module.IsNil && mi.Name.IsNil && mi.Attributes == 0)
        {
            return null;
        }

        return (mi.Attributes, Str(mi.Name), mi.Module);
    }

    // SRM exposes the ImplMap row of methods only (MethodDefinition.GetImport()): the row of a field is read from the table.
    private (MethodImportAttributes Flags, string Name, ModuleReferenceHandle ModuleRef)? GetFieldPinvokeMap(FieldDefinitionHandle field)
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
            // The MemberForwarded tag of a field is 0.
            if ((member & 1) == 0 && (int)(member >> 1) == MetadataTokens.GetRowNumber(field))
            {
                string name = _md.GetString(MetadataTokens.StringHandle((int)nameOffset));
                return ((MethodImportAttributes)flags, name, MetadataTokens.ModuleReferenceHandle((int)scope));
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

        // Invalid compressed integer: consume one byte.
        p++;
        return 0xFFFFFFFF;
    }

    // The table of the fourth (invalid) TypeDefOrRefOrSpecEncoded tag: it keeps the row number for error messages.
    private const int InvalidTypeDefOrRefTable = 0x72000000;

    // CorSigUncompressToken: the raw token of a TypeDefOrRefOrSpecEncoded value, which may not refer to an existing row.
    private static int UncompressToken(byte[] sig, ref int p)
    {
        uint v = UncompressData(sig, ref p);
        int row = (int)(v >> 2);
        return (v & 3) switch
        {
            0 => (int)TableIndex.TypeDef << 24 | row,
            1 => (int)TableIndex.TypeRef << 24 | row,
            2 => (int)TableIndex.TypeSpec << 24 | row,
            _ => InvalidTypeDefOrRefTable | row,
        };
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

    internal static uint ReadUInt32(byte[] b, int p) => (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);

    internal static uint ReadUInt32(ReadOnlySpan<byte> b, int p) => (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);

    private static ImmutableArray<T> Arr<T>(IEnumerable<T> e) => ImmutableArray.CreateRange(e);
}
