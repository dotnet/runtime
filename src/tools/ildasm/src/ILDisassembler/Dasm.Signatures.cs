// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;

namespace ILDisassembler;

// Signature formatting.
internal sealed partial class Dasm
{
    // ELEMENT_TYPE_CLASS and ELEMENT_TYPE_VALUETYPE, which SignatureTypeCode folds into TypeHandle.
    private const SignatureTypeCode ClassTypeCode = (SignatureTypeCode)SignatureTypeKind.Class;
    private const SignatureTypeCode ValueTypeTypeCode = (SignatureTypeCode)SignatureTypeKind.ValueType;

    // The kind of a calling convention value read as a compressed integer.
    private static SignatureKind KindOf(uint callConv) => (SignatureKind)(callConv & SignatureHeader.CallingConventionOrKindMask);

    // The owners used to resolve !n and !!n to generic parameter names.
    private EntityHandle _varOwner;
    private EntityHandle _mvarOwner;

    // Names and attributes of the parameters (or locals) printed with a signature.
    internal struct ParamDescriptor
    {
        public string? Name;
        public ParameterAttributes Attr;
        public ParameterHandle Param;
    }

    // Labels of instruction offsets: either a "<prefix>_<n>" name generator or an explicit name table.
    internal sealed class SigLabel
    {
        public string Prefix = "";
        public int StartIndex;
        public ParamDescriptor[]? Names;
    }

    // PrettyPrintSig: returns the formatted signature appended to 'prefix'; name==null means a local var sig.
    private string PrettyPrintSig(byte[] sig, string? name, string prefix = "", SigLabel? label = null, bool printTyArity = false)
    {
        StringBuilder sb = new(prefix);
        try
        {
            int p = 0;
            PrettyPrintSignature(sig, ref p, sig.Length, name, sb, label, printTyArity);
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentOutOfRangeException or BadImageFormatException)
        {
            sb.Clear();
            sb.Append("ERROR PARSING THE SIGNATURE");
        }

        return sb.ToString();
    }

    private void PrettyPrintSignature(byte[] sig, ref int p, int typeLen, string? name, StringBuilder out_, SigLabel? label, bool printTyArity)
    {
        uint numArgs;
        uint numTyArgs = 0;
        int typeEnd = Math.Min(p + typeLen, sig.Length);
        int ixArg = 0;
        string openpar = "(";
        string closepar = ")";
        ParamDescriptor[]? argNames = null;
        string labelPrefix = "";

        if (label is not null)
        {
            ixArg = label.StartIndex;
            labelPrefix = label.Prefix;
            argNames = label.Names;
        }

        if (name is not null)
        {
            SignatureHeader header = new(sig[p++]);

            if (header.Kind == SignatureKind.Field)
            {
                PrettyPrintType(sig, ref p, out_);
                if (name.Length != 0)
                {
                    out_.Append(' ').Append(name);
                }

                return;
            }

            if (header.HasExplicitThis)
            {
                out_.Append(KEYWORD("explicit "));
            }

            if (header.IsInstance)
            {
                out_.Append(KEYWORD("instance "));
            }

            if (header.Kind == SignatureKind.MethodSpecification)
            {
                openpar = LTN();
                closepar = GTN();
            }
            else
            {
                // Not header.CallingConvention, which is Default for the values it does not define.
                SignatureCallingConvention callConv = (SignatureCallingConvention)(header.RawValue & SignatureHeader.CallingConventionOrKindMask);
                string? cc = callConv switch
                {
                    SignatureCallingConvention.Default => "",
                    SignatureCallingConvention.CDecl => "unmanaged cdecl ",
                    SignatureCallingConvention.StdCall => "unmanaged stdcall ",
                    SignatureCallingConvention.ThisCall => "unmanaged thiscall ",
                    SignatureCallingConvention.FastCall => "unmanaged fastcall ",
                    SignatureCallingConvention.VarArgs => "vararg ",
                    SignatureCallingConvention.Unmanaged => "unmanaged ",
                    _ => null,
                };
                out_.Append(KEYWORD(cc ?? $"callconv({(int)callConv}) "));
            }

            if (header.IsGeneric)
            {
                numTyArgs = UncompressData(sig, ref p);
            }

            numArgs = UncompressData(sig, ref p);
            if (header.Kind != SignatureKind.MethodSpecification)
            {
                // return type
                if (argNames is not null)
                {
                    out_.Append(DumpParamAttr(argNames[ixArg + (int)numArgs].Attr));
                }

                PrettyPrintType(sig, ref p, out_);
                if (argNames is not null)
                {
                    out_.Append(' ').Append(DumpMarshaling(argNames[ixArg + (int)numArgs].Param));
                }

                if (name.Length != 0)
                {
                    out_.Append(' ').Append(name);
                }

                if (numTyArgs != 0 && printTyArity)
                {
                    out_.Append(LTN()).Append('[').Append(numTyArgs.ToString(CultureInfo.InvariantCulture)).Append(']').Append(GTN());
                }
            }
        }
        else
        {
            p++; // SignatureKind.LocalVariables
            numArgs = UncompressData(sig, ref p);
        }

        out_.Append(openpar);

        bool needComma = false;
        while (p < typeEnd)
        {
            if (name is not null)
            {
                if ((SignatureTypeCode)sig[p] == SignatureTypeCode.Sentinel)
                {
                    if (needComma)
                    {
                        out_.Append(',');
                    }

                    out_.Append("...");
                    p++;
                }
                else
                {
                    if (numArgs == 0)
                    {
                        break;
                    }

                    if (needComma)
                    {
                        out_.Append(',');
                    }

                    if (argNames is not null)
                    {
                        out_.Append(DumpParamAttr(argNames[ixArg].Attr));
                    }

                    PrettyPrintType(sig, ref p, out_);
                    if (label is not null)
                    {
                        if (argNames is not null)
                        {
                            out_.Append(' ').Append(DumpMarshaling(argNames[ixArg].Param)).Append(ProperName(argNames[ixArg].Name ?? ""));
                            ixArg++;
                        }
                        else
                        {
                            out_.Append(' ').Append(labelPrefix).Append('_').Append(ixArg++.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    --numArgs;
                }
            }
            else
            {
                if (numArgs == 0)
                {
                    break;
                }

                if (argNames is not null && (uint)argNames[ixArg].Attr == 0xFFFFFFFF)
                {
                    PrettyPrintType(sig, ref p, new StringBuilder());
                    ixArg++;
                    numArgs--;
                    continue;
                }

                if (needComma)
                {
                    out_.Append(',');
                }

                if (argNames is not null)
                {
                    out_.Append('[').Append(((int)argNames[ixArg].Attr).ToString(CultureInfo.InvariantCulture)).Append("] ");
                }

                PrettyPrintType(sig, ref p, out_);
                if (label is not null)
                {
                    if (argNames is not null)
                    {
                        out_.Append(' ').Append(ProperLocalName(argNames[ixArg++].Name ?? ""));
                    }
                    else
                    {
                        out_.Append(' ').Append(labelPrefix).Append('_').Append(ixArg++.ToString(CultureInfo.InvariantCulture));
                    }
                }

                --numArgs;
            }

            needComma = true;
        }

        if (numArgs > 0)
        {
            out_.Append(ERRORMSG(" [SIGNATURE ENDED PREMATURELY]"));
        }

        out_.Append(closepar);
    }

    private void PrettyPrintType(byte[] sig, ref int p, StringBuilder out_)
    {
        StringBuilder appendix = new();
        bool reiterate;
        do
        {
            reiterate = false;
            SignatureTypeCode typ = (SignatureTypeCode)sig[p++];
            string? str = typ switch
            {
                SignatureTypeCode.Void => "void",
                SignatureTypeCode.Boolean => "bool",
                SignatureTypeCode.Char => "char",
                SignatureTypeCode.SByte => "int8",
                SignatureTypeCode.Byte => "uint8",
                SignatureTypeCode.Int16 => "int16",
                SignatureTypeCode.UInt16 => "uint16",
                SignatureTypeCode.Int32 => "int32",
                SignatureTypeCode.UInt32 => "uint32",
                SignatureTypeCode.Int64 => "int64",
                SignatureTypeCode.UInt64 => "uint64",
                SignatureTypeCode.Single => "float32",
                SignatureTypeCode.Double => "float64",
                SignatureTypeCode.UIntPtr => "native uint",
                SignatureTypeCode.IntPtr => "native int",
                SignatureTypeCode.Object => "object",
                SignatureTypeCode.String => "string",
                SignatureTypeCode.TypedReference => "typedref",
                _ => null,
            };
            if (str is not null)
            {
                out_.Append(KEYWORD(str));
                continue;
            }

            switch (typ)
            {
                case ValueTypeTypeCode:
                case ClassTypeCode:
                {
                    out_.Append(KEYWORD(typ == ValueTypeTypeCode ? "valuetype " : "class "));
                    int tk = UncompressToken(sig, ref p);
                    // A nil row.
                    if ((tk & 0xFFFFFF) == 0)
                    {
                        out_.Append("[ERROR! NIL TOKEN]");
                    }
                    else
                    {
                        PrettyPrintSigClass(out_, tk);
                    }

                    break;
                }

                case SignatureTypeCode.SZArray:
                    appendix.Insert(0, "[]");
                    reiterate = true;
                    break;

                case SignatureTypeCode.Array:
                {
                    PrettyPrintType(sig, ref p, out_);
                    uint rank = UncompressData(sig, ref p);
                    if (rank == 0)
                    {
                        out_.Append(ERRORMSG("[BAD: RANK == 0!]"));
                    }
                    else
                    {
                        int[] lowerBounds = new int[rank];
                        int[] sizes = new int[rank];
                        uint numSizes = UncompressData(sig, ref p);
                        for (uint i = 0; i < numSizes; i++)
                        {
                            uint s = UncompressData(sig, ref p);
                            if (i < rank)
                            {
                                sizes[i] = (int)s;
                            }
                        }

                        uint numLowBounds = UncompressData(sig, ref p);
                        for (uint i = 0; i < numLowBounds; i++)
                        {
                            int lb = UncompressSignedInt(sig, ref p);
                            if (i < rank)
                            {
                                lowerBounds[i] = lb;
                            }
                        }

                        out_.Append('[');
                        if (rank == 1 && numSizes == 0 && numLowBounds == 0)
                        {
                            out_.Append("...");
                        }
                        else
                        {
                            for (uint i = 0; i < rank; i++)
                            {
                                if (lowerBounds[i] == 0 && i < numSizes)
                                {
                                    out_.Append(sizes[i].ToString(CultureInfo.InvariantCulture));
                                }
                                else if (i < numLowBounds)
                                {
                                    out_.Append(lowerBounds[i].ToString(CultureInfo.InvariantCulture));
                                    out_.Append("...");
                                    if (i < numSizes)
                                    {
                                        out_.Append((lowerBounds[i] + sizes[i] - 1).ToString(CultureInfo.InvariantCulture));
                                    }
                                }

                                if (i < rank - 1)
                                {
                                    out_.Append(',');
                                }
                            }
                        }

                        out_.Append(']');
                    }

                    break;
                }

                case SignatureTypeCode.GenericTypeParameter:
                {
                    out_.Append('!');
                    int n = (int)UncompressData(sig, ref p);
                    if (!PrettyPrintGP(_varOwner, out_, n))
                    {
                        out_.Append(n.ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                }

                case SignatureTypeCode.GenericMethodParameter:
                {
                    out_.Append("!!");
                    int n = (int)UncompressData(sig, ref p);
                    if (!PrettyPrintGP(_mvarOwner, out_, n))
                    {
                        out_.Append(n.ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                }

                case SignatureTypeCode.FunctionPointer:
                    out_.Append(KEYWORD("method "));
                    PrettyPrintSignature(sig, ref p, 0x7FFF, "*", out_, null, false);
                    break;

                case SignatureTypeCode.GenericTypeInstance:
                {
                    PrettyPrintType(sig, ref p, out_);
                    out_.Append(LTN());
                    uint numArgs = UncompressData(sig, ref p);
                    bool needComma = false;
                    while (numArgs-- > 0)
                    {
                        if (needComma)
                        {
                            out_.Append(',');
                        }

                        PrettyPrintType(sig, ref p, out_);
                        needComma = true;
                    }

                    out_.Append(GTN());
                    break;
                }

                case SignatureTypeCode.OptionalModifier:
                case SignatureTypeCode.RequiredModifier:
                {
                    int tk = UncompressToken(sig, ref p);
                    StringBuilder tmp = new();
                    tmp.Append(KEYWORD(typ == SignatureTypeCode.OptionalModifier ? " modopt(" : " modreq("));
                    PrettyPrintSigClass(tmp, tk);
                    tmp.Append(')');
                    appendix.Insert(0, tmp.ToString());
                    reiterate = true;
                    break;
                }

                case SignatureTypeCode.Pinned:
                    appendix.Insert(0, " pinned");
                    reiterate = true;
                    break;

                case SignatureTypeCode.Pointer:
                    appendix.Insert(0, "*");
                    reiterate = true;
                    break;

                case SignatureTypeCode.ByReference:
                    appendix.Insert(0, AMP());
                    reiterate = true;
                    break;

                default:
                    if (typ != SignatureTypeCode.Invalid)
                    {
                        out_.Append(ERRORMSG($"/* UNKNOWN TYPE (0x{(byte)typ:X})*/"));
                    }

                    break;
            }
        }
        while (reiterate);

        out_.Append(appendix);
    }

    internal string PrettyPrintClass(EntityHandle h)
    {
        StringBuilder sb = new();
        PrettyPrintClass(sb, h);
        return sb.ToString();
    }

    // A TypeDefOrRefOrSpecEncoded token read from a signature, which may not refer to an existing row.
    private void PrettyPrintSigClass(StringBuilder out_, int tk)
    {
        if (TryGetHandle(tk, out EntityHandle h))
        {
            PrettyPrintClass(out_, h);
        }
        else
        {
            out_.Append(ERRORMSG($" [ERROR: INVALID TOKEN 0x{tk:X8}] "));
        }
    }

    internal void PrettyPrintClass(StringBuilder out_, EntityHandle h)
    {
        if (h.IsNil && h.Kind == HandleKind.ModuleDefinition)
        {
            out_.Append("[*]");
            return;
        }

        if (!IsValid(h) && h.Kind != HandleKind.ModuleDefinition)
        {
            out_.Append(ERRORMSG($" [ERROR: INVALID TOKEN 0x{MetadataTokens.GetToken(h):X8}] "));
            return;
        }

        switch (h.Kind)
        {
            case HandleKind.TypeReference:
            case HandleKind.TypeDefinition:
            {
                string name, ns;
                EntityHandle encloser;
                if (h.Kind == HandleKind.TypeReference)
                {
                    TypeReference tr = _md.GetTypeReference((TypeReferenceHandle)h);
                    // A nil resolution scope prints as [*].
                    encloser = NilAsDefault(tr.ResolutionScope);
                    (name, ns) = (Str(tr.Name), Str(tr.Namespace));
                }
                else
                {
                    TypeDefinition td = _md.GetTypeDefinition((TypeDefinitionHandle)h);
                    encloser = td.GetDeclaringType();
                    (name, ns) = (Str(td.Name), Str(td.Namespace));
                }

                name = MakeNameIfNone(name, h);
                if (encloser.Kind == HandleKind.ModuleDefinition || !encloser.IsNil)
                {
                    PrettyPrintClass(out_, encloser);
                    if (encloser.Kind is HandleKind.TypeReference or HandleKind.TypeDefinition)
                    {
                        out_.Append('/');
                    }
                }

                if (h.Kind == HandleKind.TypeDefinition)
                {
                    string fn = ns.Length != 0 ? ProperName(ns) + "." + ProperName(name) : ProperName(name);
                    out_.Append(JUMPPT(fn, h));
                }
                else
                {
                    if (ns.Length != 0)
                    {
                        out_.Append(ProperName(ns)).Append('.');
                    }

                    out_.Append(ProperName(name));
                }

                if (_options.ShowTokens)
                {
                    out_.Append(COMMENT($"/*{Token(h):X8}*/"));
                }

                break;
            }

            case HandleKind.AssemblyReference:
            {
                int row = MetadataTokens.GetRowNumber(h);
                string? name = row <= _asmRefNames.Length ? _asmRefNames[row - 1] : null;
                name ??= Str(_md.GetAssemblyReference((AssemblyReferenceHandle)h).Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(JUMPPT(ProperName(name), h));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/*{Token(h):X8}*/"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case HandleKind.AssemblyDefinition:
            {
                string name = Str(_md.GetAssemblyDefinition().Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(JUMPPT(ProperName(name), h));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/* {Token(h):X8} */"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case HandleKind.ModuleReference:
            {
                string name = Str(_md.GetModuleReference((ModuleReferenceHandle)h).Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(KEYWORD(".module ")).Append(JUMPPT(ProperName(name), h));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/*{Token(h):X8}*/"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case HandleKind.TypeSpecification:
            {
                byte[] sig = Blob(_md.GetTypeSpecification((TypeSpecificationHandle)h).Signature);
                int p = 0;
                PrettyPrintType(sig, ref p, out_);
                if (_options.ShowTokens)
                {
                    out_.Append(COMMENT($"/*{Token(h):X8}*/"));
                }

                break;
            }

            case HandleKind.ModuleDefinition:
                break;

            default:
                out_.Append(ERRORMSG($" [ERROR: INVALID TOKEN TYPE 0x{Token(h):X8}] "));
                break;
        }
    }

    // PrettyPrintGP: prints the name of generic parameter n of owner, unless the name is missing or ambiguous.
    private bool PrettyPrintGP(EntityHandle owner, StringBuilder out_, int n)
    {
        if (owner.Kind is not (HandleKind.TypeDefinition or HandleKind.MethodDefinition) || !IsValid(owner))
        {
            return false;
        }

        ImmutableArray<GenericParameterHandle> gps = GetGenericParams(owner);
        if (gps.Length <= n)
        {
            return false;
        }

        string[] names = new string[gps.Length];
        int nx = -1;
        int ix = 0;
        foreach (GenericParameterHandle gph in gps)
        {
            GenericParameter gp = _md.GetGenericParameter(gph);
            names[ix] = Str(gp.Name);
            if (gp.Index == n)
            {
                nx = ix;
            }

            ix++;
        }

        if (nx < 0)
        {
            return false;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (i != nx && names[i] == names[nx])
            {
                return false;
            }
        }

        out_.Append(ProperName(names[nx]));
        return true;
    }

    // ---------------------------------------------------------------- param attributes and marshaling

    private string DumpParamAttr(ParameterAttributes attr)
    {
        StringBuilder sb = new();
        if ((attr & ParameterAttributes.In) != 0)
        {
            sb.Append(KEYWORD("[in]"));
        }

        if ((attr & ParameterAttributes.Out) != 0)
        {
            sb.Append(KEYWORD("[out]"));
        }

        if ((attr & ParameterAttributes.Optional) != 0)
        {
            sb.Append(KEYWORD("[opt]"));
        }

        if (sb.Length != 0)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }

    // The native types of marshaling descriptors (CorNativeType). System.Runtime.InteropServices.UnmanagedType has most
    // of them, but not all (and some are obsolete there).
    private enum NativeType : uint
    {
        End = 0x00,
        Void = 0x01,
        Boolean = 0x02,
        I1 = 0x03,
        U1 = 0x04,
        I2 = 0x05,
        U2 = 0x06,
        I4 = 0x07,
        U4 = 0x08,
        I8 = 0x09,
        U8 = 0x0a,
        R4 = 0x0b,
        R8 = 0x0c,
        SysChar = 0x0d,
        Variant = 0x0e,
        Currency = 0x0f,
        Ptr = 0x10,
        Decimal = 0x11,
        Date = 0x12,
        BStr = 0x13,
        LPStr = 0x14,
        LPWStr = 0x15,
        LPTStr = 0x16,
        FixedSysString = 0x17,
        ObjectRef = 0x18,
        IUnknown = 0x19,
        IDispatch = 0x1a,
        Struct = 0x1b,
        Interface = 0x1c,
        SafeArray = 0x1d,
        FixedArray = 0x1e,
        Int = 0x1f,
        UInt = 0x20,
        NestedStruct = 0x21,
        ByValStr = 0x22,
        AnsiBStr = 0x23,
        TBStr = 0x24,
        VariantBool = 0x25,
        Func = 0x26,
        AsAny = 0x28,
        Array = 0x2a,
        LPStruct = 0x2b,
        CustomMarshaler = 0x2c,
        Error = 0x2d,
        Max = 0x50,
    }

    private static string? SimpleNativeType(NativeType t) => t switch
    {
        NativeType.Void => " void",
        NativeType.Boolean => " bool",
        NativeType.I1 => " int8",
        NativeType.U1 => " unsigned int8",
        NativeType.I2 => " int16",
        NativeType.U2 => " unsigned int16",
        NativeType.I4 => " int32",
        NativeType.U4 => " unsigned int32",
        NativeType.I8 => " int64",
        NativeType.U8 => " unsigned int64",
        NativeType.R4 => " float32",
        NativeType.R8 => " float64",
        NativeType.SysChar => " syschar",
        NativeType.Variant => " variant",
        NativeType.Currency => " currency",
        NativeType.Decimal => " decimal",
        NativeType.Date => " date",
        NativeType.BStr => " bstr",
        NativeType.LPStr => " lpstr",
        NativeType.LPWStr => " lpwstr",
        NativeType.LPTStr => " lptstr",
        NativeType.ObjectRef => " objectref",
        NativeType.Struct => " struct",
        NativeType.Error => " error",
        NativeType.Int => " int",
        NativeType.UInt => " uint",
        NativeType.NestedStruct => " nested struct",
        NativeType.ByValStr => " byvalstr",
        NativeType.AnsiBStr => " ansi bstr",
        NativeType.TBStr => " tbstr",
        NativeType.VariantBool => " variant bool",
        NativeType.Func => " method",
        NativeType.AsAny => " as any",
        NativeType.LPStruct => " lpstruct",
        // The types with arguments, handled by FormatMarshaling.
        NativeType.Ptr or NativeType.SafeArray or NativeType.Array or NativeType.FixedSysString or NativeType.FixedArray
            or NativeType.Interface or NativeType.IUnknown or NativeType.IDispatch or NativeType.CustomMarshaler
            or NativeType.End or NativeType.Max => "",
        _ => null,
    };

    private static string? VariantTypeName(VarEnum vt) => vt switch
    {
        VarEnum.VT_EMPTY => "",
        VarEnum.VT_NULL => " null",
        VarEnum.VT_VARIANT => " variant",
        VarEnum.VT_CY => " currency",
        VarEnum.VT_VOID => " void",
        VarEnum.VT_BOOL => " bool",
        VarEnum.VT_I1 => " int8",
        VarEnum.VT_I2 => " int16",
        VarEnum.VT_I4 => " int32",
        VarEnum.VT_I8 => " int64",
        VarEnum.VT_R4 => " float32",
        VarEnum.VT_R8 => " float64",
        VarEnum.VT_UI1 => " unsigned int8",
        VarEnum.VT_UI2 => " unsigned int16",
        VarEnum.VT_UI4 => " unsigned int32",
        VarEnum.VT_UI8 => " unsigned int64",
        VarEnum.VT_PTR => " *",
        VarEnum.VT_DECIMAL => " decimal",
        VarEnum.VT_DATE => " date",
        VarEnum.VT_BSTR => " bstr",
        VarEnum.VT_LPSTR => " lpstr",
        VarEnum.VT_LPWSTR => " lpwstr",
        VarEnum.VT_UNKNOWN => " iunknown",
        VarEnum.VT_DISPATCH => " idispatch",
        VarEnum.VT_SAFEARRAY => " safearray",
        VarEnum.VT_INT => " int",
        VarEnum.VT_UINT => " unsigned int",
        VarEnum.VT_ERROR => " error",
        VarEnum.VT_HRESULT => " hresult",
        VarEnum.VT_CARRAY => " carray",
        VarEnum.VT_USERDEFINED => " userdefined",
        VarEnum.VT_RECORD => " record",
        VarEnum.VT_FILETIME => " filetime",
        VarEnum.VT_BLOB => " blob",
        VarEnum.VT_STREAM => " stream",
        VarEnum.VT_STORAGE => " storage",
        VarEnum.VT_STREAMED_OBJECT => " streamed_object",
        VarEnum.VT_STORED_OBJECT => " stored_object",
        VarEnum.VT_BLOB_OBJECT => " blob_object",
        VarEnum.VT_CF => " cf",
        VarEnum.VT_CLSID => " clsid",
        _ => null,
    };

    private sealed class MarshalFormatException : Exception
    {
    }

    // DumpMarshaling: " marshal(...) " for the field or parameter, or "" if it has no marshaling info.
    private string DumpMarshaling(EntityHandle h)
    {
        byte[]? blob = GetMarshalBlob(h);
        if (blob is null || blob.Length == 0)
        {
            return "";
        }

        try
        {
            return FormatMarshaling(blob);
        }
        catch (Exception e) when (e is MarshalFormatException or IndexOutOfRangeException)
        {
            StringBuilder sb = new();
            sb.Append(' ').Append(KEYWORD("marshal")).Append("({");
            foreach (byte b in blob)
            {
                sb.Append(' ').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            sb.Append(" }) ");
            return sb.ToString();
        }
    }

    private string FormatMarshaling(byte[] sig)
    {
        int cb = sig.Length;
        int cur = 0;
        uint ulData = 0;
        bool addAsterisk = false;
        bool addBrackets = false;
        StringBuilder buf = new();
        buf.Append(' ').Append(KEYWORD("marshal")).Append('(');

        uint Uncompress()
        {
            uint v = UncompressData(sig, ref cur);
            if (v == 0xFFFFFFFF)
            {
                throw new MarshalFormatException();
            }

            return v;
        }

        string SimpleType(out uint data)
        {
            data = Uncompress();
            return SimpleNativeType((NativeType)data) ?? throw new MarshalFormatException();
        }

        string ReadString()
        {
            int len = (int)GetLength(sig, ref cur);
            string s = len == 0 ? "" : Encoding.UTF8.GetString(sig, cur, Math.Min(len, sig.Length - cur));
            cur += len;
            return s;
        }

        void AppendBrackets(bool readFlags)
        {
            uint sizeParam = 0xFFFFFFFF;
            uint sizeConst = 0xFFFFFFFF;
            buf.Append('[');
            if (cur < cb)
            {
                sizeParam = ulData = Uncompress();
                if (cur < cb)
                {
                    sizeConst = ulData = Uncompress();
                    if (readFlags && cur < cb)
                    {
                        ulData = Uncompress();
                        if ((ulData & 1) == 0)
                        {
                            sizeParam = 0xFFFFFFFF;
                        }
                    }
                }
            }

            if (sizeConst != 0xFFFFFFFF)
            {
                buf.Append(((int)sizeConst).ToString(CultureInfo.InvariantCulture));
            }

            if (sizeParam != 0xFFFFFFFF)
            {
                buf.Append(" + ").Append(((int)sizeParam).ToString(CultureInfo.InvariantCulture));
            }

            buf.Append(']');
        }

        while (cur < cb)
        {
            ulData = (uint)NativeType.Max;
            string sz = SimpleType(out ulData);
            if (sz.Length == 0)
            {
                switch ((NativeType)ulData)
                {
                    case NativeType.Ptr:
                        addAsterisk = true;
                        break;

                    case NativeType.SafeArray:
                    {
                        buf.Append(KEYWORD(" safearray"));
                        ulData = (uint)VarEnum.VT_EMPTY;
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                        }

                        string vt = VariantTypeName((VarEnum)(ulData & 0x0FFF)) ?? throw new MarshalFormatException();
                        buf.Append(KEYWORD(vt));
                        string suffix = (VarEnum)(ulData & 0xF000) switch
                        {
                            VarEnum.VT_ARRAY => "[]",
                            VarEnum.VT_VECTOR => " vector",
                            VarEnum.VT_BYREF => "&",
                            VarEnum.VT_BYREF | VarEnum.VT_ARRAY => "&[]",
                            VarEnum.VT_BYREF | VarEnum.VT_VECTOR => "& vector",
                            VarEnum.VT_ARRAY | VarEnum.VT_VECTOR => "[] vector",
                            VarEnum.VT_BYREF | VarEnum.VT_ARRAY | VarEnum.VT_VECTOR => "&[] vector",
                            _ => "",
                        };
                        buf.Append(KEYWORD(suffix));
                        if (cur < cb)
                        {
                            string udt = ReadString();
                            if (udt.Length != 0)
                            {
                                buf.Append(", \"").Append(UnquotedProperName(udt)).Append('"');
                            }
                        }

                        break;
                    }

                    case NativeType.Array:
                        addBrackets = true;
                        break;

                    case NativeType.FixedSysString:
                        buf.Append(KEYWORD(" fixed sysstring")).Append(" [");
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                            buf.Append(((int)ulData).ToString(CultureInfo.InvariantCulture));
                        }

                        buf.Append(']');
                        break;

                    case NativeType.FixedArray:
                        buf.Append(KEYWORD(" fixed array")).Append(" [");
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                            buf.Append(((int)ulData).ToString(CultureInfo.InvariantCulture));
                        }

                        buf.Append(']');
                        if (cur < cb)
                        {
                            sz = SimpleType(out ulData);
                        }

                        break;

                    case NativeType.Interface:
                    case NativeType.IUnknown:
                    case NativeType.IDispatch:
                        buf.Append(KEYWORD((NativeType)ulData switch
                        {
                            NativeType.Interface => " interface",
                            NativeType.IUnknown => " iunknown",
                            _ => " idispatch",
                        }));
                        sz = " ";
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                            buf.Append('(').Append(KEYWORD("iidparam")).Append(" = ").Append(((int)ulData).ToString(CultureInfo.InvariantCulture)).Append(')');
                        }

                        break;

                    case NativeType.CustomMarshaler:
                    {
                        buf.Append(KEYWORD(" custom")).Append(" (");
                        string guid = ReadString();
                        bool fourStrings = guid.Length != 0;
                        if (fourStrings)
                        {
                            buf.Append('"').Append(UnquotedProperName(guid)).Append("\",");
                        }

                        if (cur >= cb)
                        {
                            throw new MarshalFormatException();
                        }

                        string nativeName = ReadString();
                        if (fourStrings)
                        {
                            buf.Append('"').Append(nativeName.Length != 0 ? UnquotedProperName(nativeName) : "").Append("\",");
                        }

                        if (cur >= cb)
                        {
                            throw new MarshalFormatException();
                        }

                        string marshaler = ReadString();
                        buf.Append('"').Append(marshaler.Length != 0 ? UnquotedProperName(marshaler) : "").Append("\",");
                        if (cur >= cb)
                        {
                            throw new MarshalFormatException();
                        }

                        int cookieLen = (int)GetLength(sig, ref cur);
                        if (cur + cookieLen > cb)
                        {
                            throw new MarshalFormatException();
                        }

                        buf.Append('"');
                        for (int i = 0; i < cookieLen; i++, cur++)
                        {
                            if (sig[cur] == 0 && i < cookieLen - 1)
                            {
                                buf.Append("\\0");
                            }
                            else
                            {
                                buf.Append((char)sig[cur]);
                            }
                        }

                        buf.Append('"');
                        buf.Append(')');
                        break;
                    }
                }
            }

            if (sz.Length != 0)
            {
                buf.Append(KEYWORD(sz));
                if (addAsterisk)
                {
                    buf.Append('*');
                    addAsterisk = false;
                }

                if (addBrackets)
                {
                    addBrackets = false;
                    AppendBrackets(readFlags: true);
                }
            }

            if (ulData >= (uint)NativeType.Max)
            {
                break;
            }
        }

        if (addAsterisk)
        {
            buf.Append('*');
        }

        if (addBrackets)
        {
            AppendBrackets(readFlags: false);
        }

        buf.Append(") ");
        return buf.ToString();
    }

    // GetLength: the compressed length prefix of a marshaling string; 0 bytes consumed on an invalid prefix.
    private static uint GetLength(byte[] b, ref int p)
    {
        if (p >= b.Length)
        {
            return 0;
        }

        byte c = b[p];
        if ((c & 0x80) == 0)
        {
            p += 1;
            return (uint)(c & 0x7f);
        }

        if ((c & 0xC0) == 0x80)
        {
            uint v = (uint)((c & 0x3f) << 8 | b[p + 1]);
            p += 2;
            return v;
        }

        if ((c & 0xE0) == 0xC0)
        {
            uint v = (uint)((c & 0x1f) << 24 | b[p + 1] << 16 | b[p + 2] << 8 | b[p + 3]);
            p += 4;
            return v;
        }

        return 0;
    }
}
