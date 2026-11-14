// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Internal.JitInterface;
using Internal.Pgo;

namespace SuperPmi;

// Unused high JIT flag bits that the tools co-opt to note what PGO data a method context has.
public static class ExtraJitFlags
{
    public const int HAS_PGO = 63;
    public const int HAS_EDGE_PROFILE = 62;
    public const int HAS_CLASS_PROFILE = 61;
    public const int HAS_LIKELY_CLASS = 60;
    public const int HAS_STATIC_PROFILE = 59;
    public const int HAS_DYNAMIC_PROFILE = 58;
    public const int HAS_METHOD_PROFILE = 57;
    public const int HAS_LIKELY_METHOD = 56;

    // The raw JIT flags plus the PGO pseudo-flags.
    public static ulong GetFlagsWithPgo(MethodContext mc)
    {
        ulong rawFlags = mc.repGetJitFlags().JitFlags;

        if (mc.hasPgoData(out bool hasEdgeProfile, out bool hasClassProfile, out bool hasMethodProfile, out bool hasLikelyClass,
            out bool hasLikelyMethod, out PgoSource pgoSource))
        {
            rawFlags |= 1UL << HAS_PGO;
            if (hasEdgeProfile)
                rawFlags |= 1UL << HAS_EDGE_PROFILE;
            if (hasClassProfile)
                rawFlags |= 1UL << HAS_CLASS_PROFILE;
            if (hasMethodProfile)
                rawFlags |= 1UL << HAS_METHOD_PROFILE;
            if (hasLikelyClass)
                rawFlags |= 1UL << HAS_LIKELY_CLASS;
            if (hasLikelyMethod)
                rawFlags |= 1UL << HAS_LIKELY_METHOD;
            if (pgoSource == PgoSource.Static)
                rawFlags |= 1UL << HAS_STATIC_PROFILE;
            if (pgoSource == PgoSource.Dynamic)
                rawFlags |= 1UL << HAS_DYNAMIC_PROFILE;
        }

        return rawFlags;
    }
}

// Formats the structs used in JIT-EE interface calls and SuperPMI collections.
public static class SpmiDumpHelper
{
    private const int MaxSigDisplayBytes = 25;
    private const int MaxHandleArrayDisplayElems = 5;

    public static string DumpAgnostic_CORINFO_RESOLVED_TOKENin(in Agnostic_CORINFO_RESOLVED_TOKENin tokenIn)
        => $"tc-{tokenIn.tokenContext:X16} ts-{tokenIn.tokenScope:X16} tok-{tokenIn.token:X8} tt-{tokenIn.tokenType}";

    public static string DumpAgnostic_CORINFO_RESOLVED_TOKENout(in Agnostic_CORINFO_RESOLVED_TOKENout tokenOut)
        => $"cls-{tokenOut.hClass:X16} meth-{tokenOut.hMethod:X16} fld-{tokenOut.hField:X16} ti-{tokenOut.pTypeSpec_Index} " +
            $"ts-{tokenOut.cbTypeSpec} mi-{tokenOut.pMethodSpec_Index} ms-{tokenOut.cbMethodSpec}";

    public static string DumpAgnostic_CORINFO_RESOLVED_TOKEN(in Agnostic_CORINFO_RESOLVED_TOKEN token)
        => DumpAgnostic_CORINFO_RESOLVED_TOKENin(token.inValue) + " " + DumpAgnostic_CORINFO_RESOLVED_TOKENout(token.outValue);

    public static string DumpAgnostic_CORINFO_LOOKUP_KIND(in Agnostic_CORINFO_LOOKUP_KIND lookupKind)
        => $"nrl-{lookupKind.needsRuntimeLookup} rlk-{lookupKind.runtimeLookupKind}";

    public static string DumpAgnostic_CORINFO_CONST_LOOKUP(in Agnostic_CORINFO_CONST_LOOKUP constLookup)
        => $"at-{constLookup.accessType} handle/address-{constLookup.handle:X16}";

    public static string DumpAgnostic_CORINFO_RUNTIME_LOOKUP(in Agnostic_CORINFO_RUNTIME_LOOKUP lookup)
    {
        var sb = new StringBuilder($" sig-{lookup.signature:X16} hlp-{lookup.helper} ind-{lookup.indirections} tfn-{lookup.testForNull} " +
            $"so-{lookup.sizeOffset} {{ ");
        foreach (ulong offset in lookup.offsets)
            sb.Append($"{offset:X16} ");
        sb.Append('}');
        return sb.ToString();
    }

    public static string DumpAgnostic_CORINFO_LOOKUP(in Agnostic_CORINFO_LOOKUP lookup)
    {
        string kind = DumpAgnostic_CORINFO_LOOKUP_KIND(lookup.lookupKind);
        string description = lookup.lookupKind.needsRuntimeLookup != 0
            ? DumpAgnostic_CORINFO_RUNTIME_LOOKUP(lookup.runtimeLookup)
            : DumpAgnostic_CORINFO_CONST_LOOKUP(lookup.constLookup);
        return kind + " " + description;
    }

    public static string DumpPSig<TKey, TItem>(uint pSig_Index, uint cbSig, LightWeightMap<TKey, TItem> buffers)
        where TKey : unmanaged
        where TItem : unmanaged
    {
        var sb = new StringBuilder($"cbSig-{cbSig}, si-{pSig_Index:X8}");

        // pSig_Index is usually -1 without a signature, but can be 0 in error cases where it is never
        // consulted, so only look at the buffer when there is a size.
        if (cbSig == 0)
        {
            sb.Append(" (SIG SIZE ZERO)");
        }
        else
        {
            ReadOnlySpan<byte> pSig = buffers.GetBuffer(pSig_Index);
            if (pSig_Index == uint.MaxValue)
            {
                sb.Append(" (NO SIG FOUND)");
            }
            else
            {
                sb.Append(" sig-{");
                int displayBytes = (int)Math.Min(MaxSigDisplayBytes, cbSig);
                for (int i = 0; i < displayBytes; i++)
                {
                    if (i > 0)
                        sb.Append(' ');
                    sb.Append(pSig[i].ToString("X2"));
                }
                if (displayBytes < cbSig)
                    sb.Append("...");
                sb.Append('}');
            }
            buffers.Unlock();
        }

        return sb.ToString();
    }

    public static string DumpAgnostic_CORINFO_SIG_INFO<TKey, TItem>(in Agnostic_CORINFO_SIG_INFO sigInfo, LightWeightMap<TKey, TItem> buffers,
        DenseLightWeightMap<ulong> handleMap)
        where TKey : unmanaged
        where TItem : unmanaged
    {
        var sb = new StringBuilder($"{{callConv-{sigInfo.callConv:X8} retTypeClass-{sigInfo.retTypeClass:X16} " +
            $"retTypeSigClass-{sigInfo.retTypeSigClass:X16} retType-{sigInfo.retType:X8}({DumpStrings.toString_CorInfoType(sigInfo.retType)}) " +
            $"flg-{sigInfo.flags:X8} na-{sigInfo.numArgs}");
        FormatAgnostic_CORINFO_SIG_INST_Element(sb, " ", "cc", "ci", sigInfo.sigInst_classInstCount, sigInfo.sigInst_classInst_Index, handleMap);
        FormatAgnostic_CORINFO_SIG_INST_Element(sb, " ", "mc", "mi", sigInfo.sigInst_methInstCount, sigInfo.sigInst_methInst_Index, handleMap);
        sb.Append($" args-{sigInfo.args:X16} sig-{DumpPSig(sigInfo.pSig_Index, sigInfo.cbSig, buffers)} msig-{sigInfo.methodSignature:X16} " +
            $"scp-{sigInfo.scope:X16} tok-{sigInfo.token:X8}}}");
        return sb.ToString();
    }

    public static string DumpAgnostic_CORINFO_SIG_INST_Element(string prefixStr, string instCountPrefixStr, string instIndexPrefixStr,
        uint handleInstCount, uint handleInstIndex, DenseLightWeightMap<ulong> handleMap)
    {
        var sb = new StringBuilder();
        FormatAgnostic_CORINFO_SIG_INST_Element(sb, prefixStr, instCountPrefixStr, instIndexPrefixStr, handleInstCount, handleInstIndex, handleMap);
        return sb.ToString();
    }

    private static void FormatAgnostic_CORINFO_SIG_INST_Element(StringBuilder sb, string prefixStr, string instCountPrefixStr,
        string instIndexPrefixStr, uint handleInstCount, uint handleInstIndex, DenseLightWeightMap<ulong> handleMap)
    {
        sb.Append($"{prefixStr}{instCountPrefixStr}-{handleInstCount} {instIndexPrefixStr}-{handleInstIndex} ");
        FormatHandleArray(sb, handleMap, handleInstCount, handleInstIndex);
    }

    // The consecutive elements of a handle map, assumed to be an array of handles.
    private static void FormatHandleArray(StringBuilder sb, DenseLightWeightMap<ulong> map, uint count, uint startIndex)
    {
        sb.Append('{');
        uint displayElems = Math.Min(MaxHandleArrayDisplayElems, count);
        for (uint i = startIndex; i < startIndex + displayElems; i++)
            sb.Append($"{(i == startIndex ? "" : " ")}{map.Get(i):X16}");
        if (displayElems < count)
            sb.Append(" ...");
        sb.Append('}');
    }

    private static readonly (string Name, uint Value)[] s_corInfoFlags =
    [
        ("CORINFO_FLG_STATIC", 0x00000008),
        ("CORINFO_FLG_FINAL", 0x00000010),
        ("CORINFO_FLG_SYNCH", 0x00000020),
        ("CORINFO_FLG_VIRTUAL", 0x00000040),
        ("CORINFO_FLG_INTRINSIC_TYPE", 0x00000200),
        ("CORINFO_FLG_ABSTRACT", 0x00000400),
        ("CORINFO_FLG_EnC", 0x00000800),
        ("CORINFO_FLG_FORCEINLINE", 0x00010000),
        ("CORINFO_FLG_SHAREDINST", 0x00020000),
        ("CORINFO_FLG_DELEGATE_INVOKE", 0x00040000),
        ("CORINFO_FLG_PINVOKE", 0x00080000),
        ("CORINFO_FLG_NOGCCHECK", 0x00200000),
        ("CORINFO_FLG_INTRINSIC", 0x00400000),
        ("CORINFO_FLG_CONSTRUCTOR", 0x00800000),
        ("CORINFO_FLG_AGGRESSIVE_OPT", 0x01000000),
        ("CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS", 0x02000000),
        ("CORINFO_FLG_DONT_INLINE", 0x10000000),
        ("CORINFO_FLG_DONT_INLINE_CALLER", 0x20000000),
        ("CORINFO_FLG_VALUECLASS", 0x00010000),
        ("CORINFO_FLG_VAROBJSIZE", 0x00040000),
        ("CORINFO_FLG_ARRAY", 0x00080000),
        ("CORINFO_FLG_OVERLAPPING_FIELDS", 0x00100000),
        ("CORINFO_FLG_INTERFACE", 0x00200000),
        ("CORINFO_FLG_CONTAINS_GC_PTR", 0x01000000),
        ("CORINFO_FLG_DELEGATE", 0x02000000),
        ("CORINFO_FLG_BYREF_LIKE", 0x08000000),
        ("CORINFO_FLG_BEFOREFIELDINIT", 0x20000000),
        ("CORINFO_FLG_GENERIC_TYPE_VARIABLE", 0x40000000),
        ("CORINFO_FLG_UNSAFE_VALUECLASS", 0x80000000),
    ];

    // Method and class flags share bit values, so each set bit is named by its first entry.
    public static string DumpCorInfoFlag(uint flags)
    {
        var s = new StringBuilder();
        foreach ((string name, uint value) in s_corInfoFlags)
        {
            if ((flags & value) != 0)
            {
                s.Append(' ').Append(name);
                flags &= ~value;
            }
        }
        if (flags != 0)
            s.Append($" Unknown flags-{flags:X8}");
        return s.ToString();
    }

    // Flag names in the order they are printed. A few flags are target dependent, so they are named by
    // bit number to stay target-agnostic.
    private static readonly (string Name, int Bit)[] s_jitFlags =
    [
        ("SPEED_OPT", 0),
        ("SIZE_OPT", 1),
        ("DEBUG_CODE", 2),
        ("DEBUG_EnC", 3),
        ("DEBUG_INFO", 4),
        ("MIN_OPT", 5),
        ("ENABLE_CFG", 6),
        ("OSR", 7),
        ("ALT_JIT", 8),
        ("FROZEN_ALLOC_ALLOWED", 9),
        ("PORTABLE_ENTRY_POINTS", 10),
        ("AOT", 11),
        ("PROF_ENTERLEAVE", 12),
        ("PROF_NO_PINVOKE_INLINE", 13),
        ("ASYNC", 14),
        ("RELOC", 15),
        ("IL_STUB", 16),
        ("PROCSPLIT", 17),
        ("BBINSTR", 18),
        ("BBINSTR_IF_LOOPS", 19),
        ("BBOPT", 20),
        ("FRAMED", 21),
        ("USE_PINVOKE_HELPERS", 23),
        ("REVERSE_PINVOKE", 24),
        ("TRACK_TRANSITIONS", 25),
        ("TIER0", 26),
        ("TIER1", 27),
        ("NO_INLINING", 28),
        ("RELATIVE_CODE_RELOCS", 29), // arm32 only
        ("SOFTFP_ABI", 30),           // arm32 only
        ("USE_DISPATCH_HELPERS", 31),
        ("WASM_MANAGED_HELPER", 33),
        ("HAS_PGO", ExtraJitFlags.HAS_PGO),
        ("HAS_EDGE_PROFILE", ExtraJitFlags.HAS_EDGE_PROFILE),
        ("HAS_CLASS_PROFILE", ExtraJitFlags.HAS_CLASS_PROFILE),
        ("HAS_METHOD_PROFILE", ExtraJitFlags.HAS_METHOD_PROFILE),
        ("HAS_LIKELY_CLASS", ExtraJitFlags.HAS_LIKELY_CLASS),
        ("HAS_LIKELY_METHOD", ExtraJitFlags.HAS_LIKELY_METHOD),
        ("HAS_STATIC_PROFILE", ExtraJitFlags.HAS_STATIC_PROFILE),
        ("HAS_DYNAMIC_PROFILE", ExtraJitFlags.HAS_DYNAMIC_PROFILE),
    ];

    public static string DumpJitFlags(ulong flags)
    {
        var s = new StringBuilder();
        foreach ((string name, int bit) in s_jitFlags)
        {
            if ((flags & (1UL << bit)) != 0)
            {
                s.Append(' ').Append(name);
                flags &= ~(1UL << bit);
            }
        }

        if (flags != 0)
            s.Append($" Unknown jit flags-{flags:X16}");

        return s.ToString();
    }
}

public static class DumpStrings
{
    public static string toString_InfoAccessType(uint iat) => iat switch
    {
        0 => "VALUE",
        1 => "PVALUE",
        2 => "PPVALUE",
        3 => "RELPVALUE",
        _ => "UNKNOWN",
    };

    public static string toString_CORINFO_CALL_KIND(uint kind) => kind switch
    {
        0 => "CALL",
        1 => "CALL_CODE_POINTER",
        2 => "VIRTUALCALL_STUB",
        3 => "VIRTUALCALL_LDVIRTFTN",
        4 => "VIRTUALCALL_VTABLE",
        _ => "UNKNOWN",
    };

    public static string toString_CorInfoType(uint type) => (CorInfoType)type switch
    {
        CorInfoType.CORINFO_TYPE_UNDEF => "undef",
        CorInfoType.CORINFO_TYPE_VOID => "void",
        CorInfoType.CORINFO_TYPE_BOOL => "bool",
        CorInfoType.CORINFO_TYPE_CHAR => "char",
        CorInfoType.CORINFO_TYPE_BYTE => "byte",
        CorInfoType.CORINFO_TYPE_UBYTE => "ubyte",
        CorInfoType.CORINFO_TYPE_SHORT => "short",
        CorInfoType.CORINFO_TYPE_USHORT => "ushort",
        CorInfoType.CORINFO_TYPE_INT => "int",
        CorInfoType.CORINFO_TYPE_UINT => "uint",
        CorInfoType.CORINFO_TYPE_LONG => "long",
        CorInfoType.CORINFO_TYPE_ULONG => "ulong",
        CorInfoType.CORINFO_TYPE_NATIVEINT => "nativeint",
        CorInfoType.CORINFO_TYPE_NATIVEUINT => "nativeuint",
        CorInfoType.CORINFO_TYPE_FLOAT => "float",
        CorInfoType.CORINFO_TYPE_DOUBLE => "double",
        CorInfoType.CORINFO_TYPE_CLASS => "class",
        CorInfoType.CORINFO_TYPE_PTR => "ptr",
        CorInfoType.CORINFO_TYPE_BYREF => "byref",
        CorInfoType.CORINFO_TYPE_VALUECLASS => "valueclass",
        _ => "UNKNOWN",
    };

    // Only these leading CorInfoReloc values have names in the dumps.
    private static readonly string[] s_relocationTypes =
    [
        "NONE", "DIRECT", "RELATIVE32", "ARM64_BRANCH26", "ARM64_PAGEBASE_REL21", "ARM64_PAGEOFFSET_12A", "ARM64_PAGEOFFSET_12L",
        "ARM64_LIN_TLSDESC_ADR_PAGE21", "ARM64_LIN_TLSDESC_LD64_LO12", "ARM64_LIN_TLSDESC_ADD_LO12", "ARM64_LIN_TLSDESC_CALL",
        "ARM64_WIN_TLS_SECREL_HIGH12A", "ARM64_WIN_TLS_SECREL_LOW12A", "AMD64_WIN_SECREL", "AMD64_LIN_TLSGD", "ARM32_THUMB_BRANCH24",
        "ARM32_THUMB_MOV32", "ARM32_THUMB_MOV32_PCREL", "LOONGARCH64_PC", "LOONGARCH64_JIR", "RISCV64_CALL_PLT", "RISCV64_PCREL_I",
        "RISCV64_PCREL_S",
    ];

    public static string relocationTypeToString(uint relocType)
        => relocType < s_relocationTypes.Length ? s_relocationTypes[relocType] : "UNKNOWN";

    public const uint CORINFO_EH_CLAUSE_NONE = 0;
    public const uint CORINFO_EH_CLAUSE_FILTER = 1;

    public static string toString_CorInfoTypeWithMod(uint type) => type switch
    {
        (uint)CorInfoType.CORINFO_TYPE_BYREF | (uint)CorInfoTypeWithMod.CORINFO_TYPE_MOD_PINNED => "pinned byref",
        (uint)CorInfoType.CORINFO_TYPE_CLASS | (uint)CorInfoTypeWithMod.CORINFO_TYPE_MOD_PINNED => "pinned class",
        _ => toString_CorInfoType(type & (uint)CorInfoTypeWithMod.CORINFO_TYPE_MASK),
    };
}
