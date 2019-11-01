//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SpmiDumpHelper.cpp - a helper to dump structs that are used in JitEEInterface calls and spmi collections.
//----------------------------------------------------------

#include "standardpch.h"
#include "spmidumphelper.h"
#include "spmirecordhelper.h"
#include <assert.h>

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKENin(
    const MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin& tokenIn)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "tc-%016llX ts-%016llX tok-%08X tt-%u", tokenIn.tokenContext, tokenIn.tokenScope,
              tokenIn.token, tokenIn.tokenType);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKENout(
    const MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "cls-%016llX meth-%016llX fld-%016llX ti-%u ts-%u mi-%u ms-%u", tokenOut.hClass,
              tokenOut.hMethod, tokenOut.hField, tokenOut.pTypeSpec_Index, tokenOut.cbTypeSpec,
              tokenOut.pMethodSpec_Index, tokenOut.cbMethodSpec);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKEN(
    const MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN& token)
{
    return DumpAgnostic_CORINFO_RESOLVED_TOKENin(token.inValue) + std::string(" ") +
           DumpAgnostic_CORINFO_RESOLVED_TOKENout(token.outValue);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_LOOKUP_KIND(
    const MethodContext::Agnostic_CORINFO_LOOKUP_KIND& lookupKind)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "nrl-%u rlk-%u", lookupKind.needsRuntimeLookup, lookupKind.runtimeLookupKind);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_CONST_LOOKUP(
    const MethodContext::Agnostic_CORINFO_CONST_LOOKUP& constLookup)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "at - %u handle/address-%016llX", constLookup.accessType, constLookup.handle);
    return std::string(buffer);
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RUNTIME_LOOKUP(
    const MethodContext::Agnostic_CORINFO_RUNTIME_LOOKUP& lookup)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, " sig-%016llX hlp-%u ind-%u tfn-%u tff-%u { ", lookup.signature, lookup.helper,
              lookup.indirections, lookup.testForNull, lookup.testForFixup);
    std::string resultDump(buffer);
    for (int i = 0; i < CORINFO_MAXINDIRECTIONS; i++)
    {
        sprintf_s(buffer, MAX_BUFFER_SIZE, "%016llX ", lookup.offsets[i]);
        resultDump += std::string(buffer);
    }
    resultDump += std::string("}");
    return resultDump;
}

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_LOOKUP(const MethodContext::Agnostic_CORINFO_LOOKUP& lookup)
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

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_SIG_INFO(const MethodContext::Agnostic_CORINFO_SIG_INFO& sigInfo)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "{flg-%08X na-%u cc-%u ci-%u mc-%u mi-%u args-%016llX scp-%016llX tok-%08X}",
              sigInfo.flags, sigInfo.numArgs, sigInfo.sigInst_classInstCount, sigInfo.sigInst_classInst_Index,
              sigInfo.sigInst_methInstCount, sigInfo.sigInst_methInst_Index, sigInfo.args, sigInfo.scope,
              sigInfo.token);
    return std::string(buffer);
}
