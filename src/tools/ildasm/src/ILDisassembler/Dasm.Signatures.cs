// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

namespace ILDisassembler;

// Port of the shared signature formatter (src/coreclr/inc/formattype.cpp) as compiled into ildasm.
internal sealed partial class Dasm
{
    private const byte ELEMENT_TYPE_END = 0x00;
    private const byte ELEMENT_TYPE_VOID = 0x01;
    private const byte ELEMENT_TYPE_BOOLEAN = 0x02;
    private const byte ELEMENT_TYPE_CHAR = 0x03;
    private const byte ELEMENT_TYPE_I1 = 0x04;
    private const byte ELEMENT_TYPE_U1 = 0x05;
    private const byte ELEMENT_TYPE_I2 = 0x06;
    private const byte ELEMENT_TYPE_U2 = 0x07;
    private const byte ELEMENT_TYPE_I4 = 0x08;
    private const byte ELEMENT_TYPE_U4 = 0x09;
    private const byte ELEMENT_TYPE_I8 = 0x0a;
    private const byte ELEMENT_TYPE_U8 = 0x0b;
    private const byte ELEMENT_TYPE_R4 = 0x0c;
    private const byte ELEMENT_TYPE_R8 = 0x0d;
    private const byte ELEMENT_TYPE_STRING = 0x0e;
    private const byte ELEMENT_TYPE_PTR = 0x0f;
    private const byte ELEMENT_TYPE_BYREF = 0x10;
    private const byte ELEMENT_TYPE_VALUETYPE = 0x11;
    private const byte ELEMENT_TYPE_CLASS = 0x12;
    private const byte ELEMENT_TYPE_VAR = 0x13;
    private const byte ELEMENT_TYPE_ARRAY = 0x14;
    private const byte ELEMENT_TYPE_GENERICINST = 0x15;
    private const byte ELEMENT_TYPE_TYPEDBYREF = 0x16;
    private const byte ELEMENT_TYPE_I = 0x18;
    private const byte ELEMENT_TYPE_U = 0x19;
    private const byte ELEMENT_TYPE_FNPTR = 0x1b;
    private const byte ELEMENT_TYPE_OBJECT = 0x1c;
    private const byte ELEMENT_TYPE_SZARRAY = 0x1d;
    private const byte ELEMENT_TYPE_MVAR = 0x1e;
    private const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
    private const byte ELEMENT_TYPE_CMOD_OPT = 0x20;
    private const byte ELEMENT_TYPE_MAX = 0x23;
    private const byte ELEMENT_TYPE_SENTINEL = 0x41;
    private const byte ELEMENT_TYPE_PINNED = 0x45;
    private const byte ELEMENT_TYPE_TYPEDEF = ELEMENT_TYPE_MAX + 1;

    private const uint IMAGE_CEE_CS_CALLCONV_MASK = 0x0f;
    private const uint IMAGE_CEE_CS_CALLCONV_FIELD = 0x6;
    private const uint IMAGE_CEE_CS_CALLCONV_LOCAL_SIG = 0x7;
    private const uint IMAGE_CEE_CS_CALLCONV_GENERICINST = 0xa;
    private const uint IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
    private const uint IMAGE_CEE_CS_CALLCONV_HASTHIS = 0x20;
    private const uint IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 0x40;

    private static bool IsCallConv(uint callConv, uint kind) => (callConv & IMAGE_CEE_CS_CALLCONV_MASK) == kind;

    // The owners used to resolve !n and !!n to generic parameter names.
    private int _tkVarOwner;
    private int _tkMVarOwner;

    // Names and attributes of the parameters (or locals) printed with a signature.
    internal struct ParamDescriptor
    {
        public string? Name;
        public uint Attr;
        public int Tok;
    }

    // Replaces the native "inlabel" string: either a "<prefix>_<n>" name generator or an explicit name table.
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
            uint callConv = sig[p++];

            if (IsCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
            {
                PrettyPrintTypeOrDef(sig, ref p, out_);
                if (name.Length != 0)
                {
                    out_.Append(' ').Append(name);
                }

                return;
            }

            if ((callConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS) != 0)
            {
                out_.Append(KEYWORD("explicit "));
            }

            if ((callConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) != 0)
            {
                out_.Append(KEYWORD("instance "));
            }

            if (IsCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
            {
                openpar = LTN();
                closepar = GTN();
            }
            else
            {
                uint idx = callConv & IMAGE_CEE_CS_CALLCONV_MASK;
                string? cc = idx switch
                {
                    0 => "",
                    1 => "unmanaged cdecl ",
                    2 => "unmanaged stdcall ",
                    3 => "unmanaged thiscall ",
                    4 => "unmanaged fastcall ",
                    5 => "vararg ",
                    9 => "unmanaged ",
                    _ => null,
                };
                out_.Append(KEYWORD(cc ?? $"callconv({idx}) "));
            }

            if ((callConv & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
            {
                numTyArgs = UncompressData(sig, ref p);
            }

            numArgs = UncompressData(sig, ref p);
            if (!IsCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
            {
                // return type
                if (argNames is not null)
                {
                    out_.Append(DumpParamAttr(argNames[ixArg + (int)numArgs].Attr));
                }

                PrettyPrintTypeOrDef(sig, ref p, out_);
                if (argNames is not null)
                {
                    out_.Append(' ').Append(DumpMarshaling(argNames[ixArg + (int)numArgs].Tok));
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
            p++; // IMAGE_CEE_CS_CALLCONV_LOCAL_SIG
            numArgs = UncompressData(sig, ref p);
        }

        out_.Append(openpar);

        bool needComma = false;
        while (p < typeEnd)
        {
            if (name is not null)
            {
                if (sig[p] == ELEMENT_TYPE_SENTINEL)
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

                    PrettyPrintTypeOrDef(sig, ref p, out_);
                    if (label is not null)
                    {
                        if (argNames is not null)
                        {
                            out_.Append(' ').Append(DumpMarshaling(argNames[ixArg].Tok)).Append(ProperName(argNames[ixArg].Name ?? ""));
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

                if (argNames is not null && argNames[ixArg].Attr == 0xFFFFFFFF)
                {
                    PrettyPrintTypeOrDef(sig, ref p, new StringBuilder());
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

                PrettyPrintTypeOrDef(sig, ref p, out_);
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

    // PrettyPrintTypeOrDef: prints the type, or the name of a .typedef aliasing exactly these bytes.
    private void PrettyPrintTypeOrDef(byte[] sig, ref int p, StringBuilder out_)
    {
        int begin = p;
        int len = out_.Length;
        PrettyPrintType(sig, ref p, out_);
        if (p > begin && _typedefs.Count > 0)
        {
            ReadOnlySpan<byte> bytes = sig.AsSpan(begin, p - begin);
            foreach (TypeDefDescr td in _typedefs)
            {
                if (td.Sig is not null && td.Sig.AsSpan().SequenceEqual(bytes))
                {
                    out_.Length = len;
                    out_.Append(JUMPPT(ProperName(td.Name), td.TkSelf));
                    break;
                }
            }
        }
    }

    private void PrettyPrintType(byte[] sig, ref int p, StringBuilder out_)
    {
        StringBuilder appendix = new();
        bool reiterate;
        do
        {
            reiterate = false;
            byte typ = sig[p++];
            string? str = typ switch
            {
                ELEMENT_TYPE_VOID => "void",
                ELEMENT_TYPE_BOOLEAN => "bool",
                ELEMENT_TYPE_CHAR => "char",
                ELEMENT_TYPE_I1 => "int8",
                ELEMENT_TYPE_U1 => "uint8",
                ELEMENT_TYPE_I2 => "int16",
                ELEMENT_TYPE_U2 => "uint16",
                ELEMENT_TYPE_I4 => "int32",
                ELEMENT_TYPE_U4 => "uint32",
                ELEMENT_TYPE_I8 => "int64",
                ELEMENT_TYPE_U8 => "uint64",
                ELEMENT_TYPE_R4 => "float32",
                ELEMENT_TYPE_R8 => "float64",
                ELEMENT_TYPE_U => "native uint",
                ELEMENT_TYPE_I => "native int",
                ELEMENT_TYPE_OBJECT => "object",
                ELEMENT_TYPE_STRING => "string",
                ELEMENT_TYPE_TYPEDBYREF => "typedref",
                _ => null,
            };
            if (str is not null)
            {
                out_.Append(KEYWORD(str));
                continue;
            }

            switch (typ)
            {
                case ELEMENT_TYPE_VALUETYPE:
                case ELEMENT_TYPE_CLASS:
                {
                    out_.Append(KEYWORD(typ == ELEMENT_TYPE_VALUETYPE ? "valuetype " : "class "));
                    int tk = UncompressToken(sig, ref p);
                    if (IsNilToken(tk))
                    {
                        out_.Append("[ERROR! NIL TOKEN]");
                    }
                    else
                    {
                        PrettyPrintClass(out_, tk);
                    }

                    break;
                }

                case ELEMENT_TYPE_SZARRAY:
                    appendix.Insert(0, "[]");
                    reiterate = true;
                    break;

                case ELEMENT_TYPE_ARRAY:
                {
                    PrettyPrintTypeOrDef(sig, ref p, out_);
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

                case ELEMENT_TYPE_VAR:
                {
                    out_.Append('!');
                    int n = (int)UncompressData(sig, ref p);
                    if (!PrettyPrintGP(_tkVarOwner, out_, n))
                    {
                        out_.Append(n.ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                }

                case ELEMENT_TYPE_MVAR:
                {
                    out_.Append("!!");
                    int n = (int)UncompressData(sig, ref p);
                    if (!PrettyPrintGP(_tkMVarOwner, out_, n))
                    {
                        out_.Append(n.ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                }

                case ELEMENT_TYPE_FNPTR:
                    out_.Append(KEYWORD("method "));
                    PrettyPrintSignature(sig, ref p, 0x7FFF, "*", out_, null, false);
                    break;

                case ELEMENT_TYPE_GENERICINST:
                {
                    PrettyPrintTypeOrDef(sig, ref p, out_);
                    out_.Append(LTN());
                    uint numArgs = UncompressData(sig, ref p);
                    bool needComma = false;
                    while (numArgs-- > 0)
                    {
                        if (needComma)
                        {
                            out_.Append(',');
                        }

                        PrettyPrintTypeOrDef(sig, ref p, out_);
                        needComma = true;
                    }

                    out_.Append(GTN());
                    break;
                }

                case ELEMENT_TYPE_CMOD_OPT:
                case ELEMENT_TYPE_CMOD_REQD:
                {
                    int tk = UncompressToken(sig, ref p);
                    StringBuilder tmp = new();
                    tmp.Append(KEYWORD(typ == ELEMENT_TYPE_CMOD_OPT ? " modopt(" : " modreq("));
                    PrettyPrintClass(tmp, tk);
                    tmp.Append(')');
                    appendix.Insert(0, tmp.ToString());
                    reiterate = true;
                    break;
                }

                case ELEMENT_TYPE_PINNED:
                    appendix.Insert(0, " pinned");
                    reiterate = true;
                    break;

                case ELEMENT_TYPE_PTR:
                    appendix.Insert(0, "*");
                    reiterate = true;
                    break;

                case ELEMENT_TYPE_BYREF:
                    appendix.Insert(0, AMP());
                    reiterate = true;
                    break;

                default:
                    if (typ != 0)
                    {
                        out_.Append(ERRORMSG($"/* UNKNOWN TYPE (0x{typ:X})*/"));
                    }

                    break;
            }
        }
        while (reiterate);

        out_.Append(appendix);
    }

    private string PrettyPrintClass(int tk)
    {
        StringBuilder sb = new();
        PrettyPrintClass(sb, tk);
        return sb.ToString();
    }

    private void PrettyPrintClass(StringBuilder out_, int tk)
    {
        if (tk == mdTokenNil)
        {
            out_.Append("[*]");
            return;
        }

        if (!IsValidToken(tk) && TypeFromToken(tk) != mdtModule)
        {
            out_.Append(ERRORMSG($" [ERROR: INVALID TOKEN 0x{tk:X8}] "));
            return;
        }

        switch (TypeFromToken(tk))
        {
            case mdtTypeRef:
            case mdtTypeDef:
            {
                TypeDefDescr? alias = FindTypedefFor(tk);
                if (alias is not null)
                {
                    out_.Append(JUMPPT(ProperName(alias.Name), alias.TkSelf));
                    break;
                }

                string name, ns;
                int tkEncloser;
                if (TypeFromToken(tk) == mdtTypeRef)
                {
                    tkEncloser = GetResolutionScopeOfTypeRef(tk);
                    (name, ns) = GetNameOfTypeRef(tk);
                }
                else
                {
                    tkEncloser = GetEnclosingClass(tk);
                    (name, ns) = GetNameOfTypeDef(tk);
                }

                name = MakeNameIfNone(name, tk);
                if (tkEncloser == mdTokenNil || RidFromToken(tkEncloser) != 0)
                {
                    PrettyPrintClass(out_, tkEncloser);
                    if (TypeFromToken(tkEncloser) == mdtTypeRef || TypeFromToken(tkEncloser) == mdtTypeDef)
                    {
                        out_.Append('/');
                    }
                }

                if (TypeFromToken(tk) == mdtTypeDef)
                {
                    string fn = ns.Length != 0 ? ProperName(ns) + "." + ProperName(name) : ProperName(name);
                    out_.Append(JUMPPT(fn, tk));
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
                    out_.Append(COMMENT($"/*{tk:X8}*/"));
                }

                break;
            }

            case mdtAssemblyRef:
            {
                string? name = RidFromToken(tk) <= _asmRefNames.Length ? _asmRefNames[RidFromToken(tk) - 1] : null;
                name ??= Str(_md.GetAssemblyReference((AssemblyReferenceHandle)Handle(tk)).Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(JUMPPT(ProperName(name), tk));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/*{tk:X8}*/"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case mdtAssembly:
            {
                string name = Str(_md.GetAssemblyDefinition().Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(JUMPPT(ProperName(name), tk));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/* {tk:X8} */"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case mdtModuleRef:
            {
                string name = Str(_md.GetModuleReference((ModuleReferenceHandle)Handle(tk)).Name);
                if (name.Length != 0)
                {
                    out_.Append('[').Append(KEYWORD(".module ")).Append(JUMPPT(ProperName(name), tk));
                    if (_options.ShowTokens)
                    {
                        out_.Append(COMMENT($"/*{tk:X8}*/"));
                    }

                    out_.Append(']');
                }

                break;
            }

            case mdtTypeSpec:
            {
                TypeDefDescr? alias = FindTypedefFor(tk);
                if (alias is not null)
                {
                    out_.Append(JUMPPT(ProperName(alias.Name), alias.TkSelf));
                }
                else
                {
                    byte[] sig = GetTypeSpecSig(tk);
                    int p = 0;
                    PrettyPrintType(sig, ref p, out_);
                }

                if (_options.ShowTokens)
                {
                    out_.Append(COMMENT($"/*{tk:X8}*/"));
                }

                break;
            }

            case mdtModule:
                break;

            default:
                out_.Append(ERRORMSG($" [ERROR: INVALID TOKEN TYPE 0x{tk:X8}] "));
                break;
        }
    }

    // PrettyPrintGP: prints the name of generic parameter n of owner, unless the name is missing or ambiguous.
    private bool PrettyPrintGP(int tkOwner, StringBuilder out_, int n)
    {
        if (tkOwner == 0 || (TypeFromToken(tkOwner) != mdtTypeDef && TypeFromToken(tkOwner) != mdtMethodDef) || !IsValidToken(tkOwner))
        {
            return false;
        }

        ImmutableArray<GenericParameterHandle> gps = GetGenericParams(tkOwner);
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

    private string DumpParamAttr(uint attr)
    {
        StringBuilder sb = new();
        if ((attr & 0x1) != 0)
        {
            sb.Append(KEYWORD("[in]"));
        }

        if ((attr & 0x2) != 0)
        {
            sb.Append(KEYWORD("[out]"));
        }

        if ((attr & 0x10) != 0)
        {
            sb.Append(KEYWORD("[opt]"));
        }

        if (sb.Length != 0)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }

    private const uint NATIVE_TYPE_MAX = 0x50;

    private static string? SimpleNativeType(uint t) => t switch
    {
        0x01 => " void",
        0x02 => " bool",
        0x03 => " int8",
        0x04 => " unsigned int8",
        0x05 => " int16",
        0x06 => " unsigned int16",
        0x07 => " int32",
        0x08 => " unsigned int32",
        0x09 => " int64",
        0x0a => " unsigned int64",
        0x0b => " float32",
        0x0c => " float64",
        0x0d => " syschar",
        0x0e => " variant",
        0x0f => " currency",
        0x11 => " decimal",
        0x12 => " date",
        0x13 => " bstr",
        0x14 => " lpstr",
        0x15 => " lpwstr",
        0x16 => " lptstr",
        0x18 => " objectref",
        0x1b => " struct",
        0x2d => " error",
        0x1f => " int",
        0x20 => " uint",
        0x21 => " nested struct",
        0x22 => " byvalstr",
        0x23 => " ansi bstr",
        0x24 => " tbstr",
        0x25 => " variant bool",
        0x26 => " method",
        0x28 => " as any",
        0x2b => " lpstruct",
        0x10 or 0x1d or 0x2a or 0x17 or 0x1e or 0x1c or 0x19 or 0x1a or 0x2c or 0x00 or NATIVE_TYPE_MAX => "",
        _ => null,
    };

    private static string? VariantTypeName(uint vt) => vt switch
    {
        0 => "",
        1 => " null",
        12 => " variant",
        6 => " currency",
        24 => " void",
        11 => " bool",
        16 => " int8",
        2 => " int16",
        3 => " int32",
        20 => " int64",
        4 => " float32",
        5 => " float64",
        17 => " unsigned int8",
        18 => " unsigned int16",
        19 => " unsigned int32",
        21 => " unsigned int64",
        26 => " *",
        14 => " decimal",
        7 => " date",
        8 => " bstr",
        30 => " lpstr",
        31 => " lpwstr",
        13 => " iunknown",
        9 => " idispatch",
        27 => " safearray",
        22 => " int",
        23 => " unsigned int",
        10 => " error",
        25 => " hresult",
        28 => " carray",
        29 => " userdefined",
        36 => " record",
        64 => " filetime",
        65 => " blob",
        66 => " stream",
        67 => " storage",
        68 => " streamed_object",
        69 => " stored_object",
        70 => " blob_object",
        71 => " cf",
        72 => " clsid",
        _ => null,
    };

    private sealed class MarshalFormatException : Exception
    {
    }

    // DumpMarshaling: " marshal(...) " for the field or parameter, or "" if it has no marshaling info.
    private string DumpMarshaling(int tok)
    {
        byte[]? blob = GetMarshalBlob(tok);
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
            return SimpleNativeType(data) ?? throw new MarshalFormatException();
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
            ulData = NATIVE_TYPE_MAX;
            string sz = SimpleType(out ulData);
            if (sz.Length == 0)
            {
                switch (ulData)
                {
                    case 0x10: // NATIVE_TYPE_PTR
                        addAsterisk = true;
                        break;

                    case 0x1d: // NATIVE_TYPE_SAFEARRAY
                    {
                        buf.Append(KEYWORD(" safearray"));
                        ulData = 0; // VT_EMPTY
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                        }

                        string vt = VariantTypeName(ulData & 0x0FFF) ?? throw new MarshalFormatException();
                        buf.Append(KEYWORD(vt));
                        string suffix = (ulData & 0xF000) switch
                        {
                            0x2000 => "[]",
                            0x1000 => " vector",
                            0x4000 => "&",
                            0x6000 => "&[]",
                            0x5000 => "& vector",
                            0x3000 => "[] vector",
                            0x7000 => "&[] vector",
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

                    case 0x2a: // NATIVE_TYPE_ARRAY
                        addBrackets = true;
                        break;

                    case 0x17: // NATIVE_TYPE_FIXEDSYSSTRING
                        buf.Append(KEYWORD(" fixed sysstring")).Append(" [");
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                            buf.Append(((int)ulData).ToString(CultureInfo.InvariantCulture));
                        }

                        buf.Append(']');
                        break;

                    case 0x1e: // NATIVE_TYPE_FIXEDARRAY
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

                    case 0x1c: // NATIVE_TYPE_INTF
                    case 0x19: // NATIVE_TYPE_IUNKNOWN
                    case 0x1a: // NATIVE_TYPE_IDISPATCH
                        buf.Append(KEYWORD(ulData == 0x1c ? " interface" : ulData == 0x19 ? " iunknown" : " idispatch"));
                        sz = " ";
                        if (cur < cb)
                        {
                            ulData = Uncompress();
                            buf.Append('(').Append(KEYWORD("iidparam")).Append(" = ").Append(((int)ulData).ToString(CultureInfo.InvariantCulture)).Append(')');
                        }

                        break;

                    case 0x2c: // NATIVE_TYPE_CUSTOMMARSHALER
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

            if (ulData >= NATIVE_TYPE_MAX)
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
