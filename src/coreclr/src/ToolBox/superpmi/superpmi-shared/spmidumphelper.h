//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SpmiDumpHelper.h - a helper to dump structs that are used in JitEEInterface calls and spmi collections.
//----------------------------------------------------------

#ifndef _SpmiDumpHelper
#define _SpmiDumpHelper

#include "methodcontext.h"

class SpmiDumpHelper
{
public:
    static std::string DumpAgnostic_CORINFO_RESOLVED_TOKENin(
        const MethodContext::Agnostic_CORINFO_RESOLVED_TOKENin& tokenIn);
    static std::string DumpAgnostic_CORINFO_RESOLVED_TOKENout(
        const MethodContext::Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut);
    static std::string DumpAgnostic_CORINFO_RESOLVED_TOKEN(const MethodContext::Agnostic_CORINFO_RESOLVED_TOKEN& token);
    static std::string DumpAgnostic_CORINFO_LOOKUP_KIND(const MethodContext::Agnostic_CORINFO_LOOKUP_KIND& lookupKind);
    static std::string DumpAgnostic_CORINFO_CONST_LOOKUP(
        const MethodContext::Agnostic_CORINFO_CONST_LOOKUP& constLookup);
    static std::string DumpAgnostic_CORINFO_RUNTIME_LOOKUP(
        const MethodContext::Agnostic_CORINFO_RUNTIME_LOOKUP& lookup);
    static std::string DumpAgnostic_CORINFO_LOOKUP(const MethodContext::Agnostic_CORINFO_LOOKUP& lookup);
    static std::string DumpAgnostic_CORINFO_SIG_INFO(const MethodContext::Agnostic_CORINFO_SIG_INFO& sigInfo);

private:
    static const int MAX_BUFFER_SIZE = 1000;
};

#endif
