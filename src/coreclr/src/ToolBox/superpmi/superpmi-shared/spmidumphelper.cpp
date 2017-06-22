//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SpmiDumpHelper.cpp - a helper to dump structs that are used in JitEEInterface calls and spmi collections.
//----------------------------------------------------------

#include "standardpch.h"
#include "spmidumphelper.h"

std::string SpmiDumpHelper::DumpAgnostic_CORINFO_RESOLVED_TOKENin(
    const MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin& tokenIn)
{
    char buffer[MAX_BUFFER_SIZE];
    sprintf_s(buffer, MAX_BUFFER_SIZE, "tc-%016llX ts-%016llX tok - %08X tt-%u", tokenIn.tokenContext, tokenIn.tokenScope,
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
