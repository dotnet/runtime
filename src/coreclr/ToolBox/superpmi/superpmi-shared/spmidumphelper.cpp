// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SpmiDumpHelper.cpp - a helper to dump structs that are used in JitEEInterface calls and spmi collections.
//----------------------------------------------------------

#include "standardpch.h"
#include "spmidumphelper.h"
#include "spmirecordhelper.h"
#include <assert.h>

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKENin(
    const Agnostic_CORINFO_RESOLVED_TOKENin& tokenIn)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "tc-%016llX ts-%016llX tok-%08X tt-%u", tokenIn.tokenContext, tokenIn.tokenScope,
              tokenIn.token, tokenIn.tokenType);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKENout(
    const Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "cls-%016llX meth-%016llX fld-%016llX ti-%u ts-%u mi-%u ms-%u", tokenOut.hClass,
              tokenOut.hMethod, tokenOut.hField, tokenOut.pTypeSpec_Index, tokenOut.cbTypeSpec,
              tokenOut.pMethodSpec_Index, tokenOut.cbMethodSpec);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKEN(
    const Agnostic_CORINFO_RESOLVED_TOKEN& token)
{
    return DumpAgnostic_CORINFO_RESOLVED_TOKENin(token.inValue) + std::string(" ") +
           DumpAgnostic_CORINFO_RESOLVED_TOKENout(token.outValue);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_LOOKUP_KIND(
    const Agnostic_CORINFO_LOOKUP_KIND& lookupKind)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "nrl-%u rlk-%u", lookupKind.needsRuntimeLookup, lookupKind.runtimeLookupKind);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_CONST_LOOKUP(
    const Agnostic_CORINFO_CONST_LOOKUP& constLookup)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "at-%u handle/address-%016llX", constLookup.accessType, constLookup.handle);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RUNTIME_LOOKUP(
    const Agnostic_CORINFO_RUNTIME_LOOKUP& lookup)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, " sig-%016llX hlp-%u ind-%u tfn-%u tff-%u so-%u { ", lookup.signature, lookup.helper,
              lookup.indirections, lookup.testForNull, lookup.testForFixup, lookup.sizeOffset);
    std::string resultDump(buffer);
    for (int i = 0; i < CORINFO_MAXINDIRECTIONS; i++)
    {
        sprintf_s(buffer, MAX_BUFFER_SIZE, "%016llX ", lookup.offsets[i]);
        resultDump += std::string(buffer);
    }
    resultDump += std::string("}");
    return resultDump;
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_LOOKUP(const Agnostic_CORINFO_LOOKUP& lookup)
{
    std::string kind = DumpAgnostic_CORINFO_LOOKUP_KIND(lookup.lookupKind);
    std::string lookupDescription;
    if (lookup.lookupKind.needsRuntimeLookup)
    {
        lookupDescription = DumpAgnostic_CORINFO_RUNTIME_LOOKUP(lookup.runtimeLookup);
    }
    else
    {
        lookupDescription = DumpAgnostic_CORINFO_CONST_LOOKUP(lookup.constLookup);
    }
    return kind + std::string(" ") + lookupDescription;
}

// Dump the consecutive elements of a DenseLightweightMap, which are DWORDLONG, and assumed to represent an array of handles.
void SpmiDumpHelper::FormatHandleArray(char*& pbuf, int& sizeOfBuffer, const DenseLightWeightMap<DWORDLONG>* map, DWORD count, DWORD startIndex)
{
    int cch;

    cch = sprintf_s(pbuf, sizeOfBuffer, "{");
    pbuf += cch;
    sizeOfBuffer -= cch;

    const unsigned int maxHandleArrayDisplayElems = 5; // Don't display more than this.
    const unsigned int handleArrayDisplayElems = min(maxHandleArrayDisplayElems, count);

    bool first = true;
    for (DWORD i = startIndex; i < startIndex + handleArrayDisplayElems; i++)
    {
        cch = sprintf_s(pbuf, sizeOfBuffer, "%s%016llX", first ? "" : " ", map->Get(i));
        pbuf += cch;
        sizeOfBuffer -= cch;

        first = false;
    }

    if (handleArrayDisplayElems < count)
    {
        cch = sprintf_s(pbuf, sizeOfBuffer, " ...");
        pbuf += cch;
        sizeOfBuffer -= cch;
    }

    cch = sprintf_s(pbuf, sizeOfBuffer, "}");
    pbuf += cch;
    sizeOfBuffer -= cch;
}

void SpmiDumpHelper::FormatAgnostic_CORINFO_SIG_INST_Element(
    char*& pbuf,
    int& sizeOfBuffer,
    const char* prefixStr,
    const char* instCountPrefixStr,
    const char* instIndexPrefixStr,
    unsigned handleInstCount,
    unsigned handleInstIndex,
    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    int cch = sprintf_s(pbuf, sizeOfBuffer, "%s%s-%u %s-%u ", prefixStr, instCountPrefixStr, handleInstCount, instIndexPrefixStr, handleInstIndex);
    pbuf += cch;
    sizeOfBuffer -= cch;

    FormatHandleArray(pbuf, sizeOfBuffer, handleMap, handleInstCount, handleInstIndex);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_SIG_INST_Element(
    const char* prefixStr,
    const char* instCountPrefixStr,
    const char* instIndexPrefixStr,
    unsigned handleInstCount,
    unsigned handleInstIndex,
    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    char buffer[MAX_BUFFER_SIZE];
    char* pbuf = buffer;
    int sizeOfBuffer = sizeof(buffer);

    FormatAgnostic_CORINFO_SIG_INST_Element(pbuf, sizeOfBuffer, prefixStr, instCountPrefixStr, instIndexPrefixStr, handleInstCount, handleInstIndex, handleMap);

    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpCorInfoFlag(CorInfoFlag flags)
{
    std::string s("");

#define AddFlag(__name)\
    if (flags & __name) { s += std::string(" ") + std::string(#__name); flags = (CorInfoFlag)((DWORD)flags & ~(DWORD)__name); }

    AddFlag(CORINFO_FLG_PROTECTED);
    AddFlag(CORINFO_FLG_STATIC);
    AddFlag(CORINFO_FLG_FINAL);
    AddFlag(CORINFO_FLG_SYNCH);
    AddFlag(CORINFO_FLG_VIRTUAL);
    AddFlag(CORINFO_FLG_NATIVE);
    AddFlag(CORINFO_FLG_INTRINSIC_TYPE);
    AddFlag(CORINFO_FLG_ABSTRACT);
    AddFlag(CORINFO_FLG_EnC);
    AddFlag(CORINFO_FLG_FORCEINLINE);
    AddFlag(CORINFO_FLG_SHAREDINST);
    AddFlag(CORINFO_FLG_DELEGATE_INVOKE);
    AddFlag(CORINFO_FLG_PINVOKE);
    AddFlag(CORINFO_FLG_NOGCCHECK);
    AddFlag(CORINFO_FLG_INTRINSIC);
    AddFlag(CORINFO_FLG_CONSTRUCTOR);
    AddFlag(CORINFO_FLG_AGGRESSIVE_OPT);
    AddFlag(CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS);
    AddFlag(CORINFO_FLG_DONT_INLINE);
    AddFlag(CORINFO_FLG_DONT_INLINE_CALLER);
    AddFlag(CORINFO_FLG_JIT_INTRINSIC);
    AddFlag(CORINFO_FLG_VALUECLASS);
    AddFlag(CORINFO_FLG_VAROBJSIZE);
    AddFlag(CORINFO_FLG_ARRAY);
    AddFlag(CORINFO_FLG_OVERLAPPING_FIELDS);
    AddFlag(CORINFO_FLG_INTERFACE);
    AddFlag(CORINFO_FLG_CUSTOMLAYOUT);
    AddFlag(CORINFO_FLG_CONTAINS_GC_PTR);
    AddFlag(CORINFO_FLG_DELEGATE);
    AddFlag(CORINFO_FLG_CONTAINS_STACK_PTR);
    AddFlag(CORINFO_FLG_VARIANCE);
    AddFlag(CORINFO_FLG_BEFOREFIELDINIT);
    AddFlag(CORINFO_FLG_GENERIC_TYPE_VARIABLE);
    AddFlag(CORINFO_FLG_UNSAFE_VALUECLASS);

#undef AddFlag

    if (flags != 0)
    {
        char buffer[MAX_BUFFER_SIZE];
        sprintf_s(buffer, MAX_BUFFER_SIZE, " Unknown flags-%08X", flags);
        s += std::string(buffer);
    }

    return s;
}

std::string SpmiDumpHelper::DumpJitFlags(CORJIT_FLAGS corJitFlags)
{
    return DumpJitFlags(corJitFlags.GetFlagsRaw());
}

std::string SpmiDumpHelper::DumpJitFlags(unsigned long long flags)
{
    std::string s("");

#define AddFlag(__name)\
    if (flags & (1ull << CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_ ## __name)) { \
       s += std::string(" ") + std::string(#__name); \
       flags &= ~(1ull << CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_ ## __name); }

    // Note some flags are target dependent, but we want to
    // be target-agnostic. So we use numbers for the few
    // flags that are not universally defined.

#define AddFlagNumeric(__name, __val)\
    if (flags & (1ull << __val)) { \
       s += std::string(" ") + std::string(#__name); \
       flags &= ~(1ull <<__val); }

    AddFlag(SPEED_OPT);
    AddFlag(SIZE_OPT);
    AddFlag(DEBUG_CODE);
    AddFlag(DEBUG_EnC);
    AddFlag(DEBUG_INFO);
    AddFlag(MIN_OPT);

    AddFlag(MCJIT_BACKGROUND);

    // x86 only
    //
    AddFlagNumeric(PINVOKE_RESTORE_ESP, 8);
    AddFlagNumeric(TARGET_P4, 9);
    AddFlagNumeric(USE_FCOMI, 10);
    AddFlagNumeric(USE_CMOV, 11);

    AddFlag(OSR);
    AddFlag(ALT_JIT);

    AddFlagNumeric(FEATURE_SIMD, 17);

    AddFlag(MAKEFINALCODE);
    AddFlag(READYTORUN);
    AddFlag(PROF_ENTERLEAVE);

    AddFlag(PROF_NO_PINVOKE_INLINE);
    AddFlag(SKIP_VERIFICATION);
    AddFlag(PREJIT);
    AddFlag(RELOC);
    AddFlag(IMPORT_ONLY);
    AddFlag(IL_STUB);
    AddFlag(PROCSPLIT);
    AddFlag(BBINSTR);
    AddFlag(BBOPT);
    AddFlag(FRAMED);

    AddFlag(PUBLISH_SECRET_PARAM);

    AddFlag(SAMPLING_JIT_BACKGROUND);
    AddFlag(USE_PINVOKE_HELPERS);
    AddFlag(REVERSE_PINVOKE);
    AddFlag(TRACK_TRANSITIONS);
    AddFlag(TIER0);
    AddFlag(TIER1);

    // arm32 only
    //
    AddFlagNumeric(RELATIVE_CODE_RELOCS, 41);

    AddFlag(NO_INLINING);

    // "Extra jit flag" support
    //
    AddFlagNumeric(HAS_PGO, EXTRA_JIT_FLAGS::HAS_PGO);
    AddFlagNumeric(HAS_EDGE_PROFILE, EXTRA_JIT_FLAGS::HAS_EDGE_PROFILE);
    AddFlagNumeric(HAS_CLASS_PROFILE, EXTRA_JIT_FLAGS::HAS_CLASS_PROFILE);
    AddFlagNumeric(HAS_LIKELY_CLASS, EXTRA_JIT_FLAGS::HAS_LIKELY_CLASS);
    AddFlagNumeric(HAS_STATIC_PROFILE, EXTRA_JIT_FLAGS::HAS_STATIC_PROFILE);
    AddFlagNumeric(HAS_DYNAMIC_PROFILE, EXTRA_JIT_FLAGS::HAS_DYNAMIC_PROFILE);

#undef AddFlag
#undef AddFlagNumeric

    if (flags != 0)
    {
        char buffer[MAX_BUFFER_SIZE];
        sprintf_s(buffer, MAX_BUFFER_SIZE, " Unknown jit flags-%016llX", flags);
        s += std::string(buffer);
    }

    return s;
}

