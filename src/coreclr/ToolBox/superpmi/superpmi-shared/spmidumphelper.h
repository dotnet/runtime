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
        const Agnostic_CORINFO_RESOLVED_TOKENin& tokenIn);
    static std::string DumpAgnostic_CORINFO_RESOLVED_TOKENout(
        const Agnostic_CORINFO_RESOLVED_TOKENout& tokenOut);
    static std::string DumpAgnostic_CORINFO_RESOLVED_TOKEN(const Agnostic_CORINFO_RESOLVED_TOKEN& token);
    static std::string DumpAgnostic_CORINFO_LOOKUP_KIND(const Agnostic_CORINFO_LOOKUP_KIND& lookupKind);
    static std::string DumpAgnostic_CORINFO_CONST_LOOKUP(
        const Agnostic_CORINFO_CONST_LOOKUP& constLookup);
    static std::string DumpAgnostic_CORINFO_RUNTIME_LOOKUP(
        const Agnostic_CORINFO_RUNTIME_LOOKUP& lookup);
    static std::string DumpAgnostic_CORINFO_LOOKUP(const Agnostic_CORINFO_LOOKUP& lookup);

    template <typename key, typename value>
    static std::string DumpAgnostic_CORINFO_SIG_INFO(
        const Agnostic_CORINFO_SIG_INFO& sigInfo,
        LightWeightMap<key, value>* buffers,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    static std::string DumpAgnostic_CORINFO_SIG_INST_Element(
        const char* prefixStr,
        const char* instCountPrefixStr,
        const char* instIndexPrefixStr,
        unsigned handleInstCount,
        unsigned handleInstIndex,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    static std::string DumpCorInfoFlag(CorInfoFlag flags);

private:

    static void FormatAgnostic_CORINFO_SIG_INST_Element(
        char*& pbuf,
        int& sizeOfBuffer,
        const char* prefixStr,
        const char* instCountPrefixStr,
        const char* instIndexPrefixStr,
        unsigned handleInstCount,
        unsigned handleInstIndex,
        const DenseLightWeightMap<DWORDLONG>* handleMap);

    static void FormatHandleArray(char*& pbuf, int& sizeOfBuffer, const DenseLightWeightMap<DWORDLONG>* map, DWORD count, DWORD startIndex);

    static const int MAX_BUFFER_SIZE = 1000;
};

template <typename key, typename value>
inline std::string SpmiDumpHelper::DumpAgnostic_CORINFO_SIG_INFO(
    const Agnostic_CORINFO_SIG_INFO& sigInfo,
    LightWeightMap<key, value>* buffers,
    const DenseLightWeightMap<DWORDLONG>* handleMap)
{
    char buffer[MAX_BUFFER_SIZE];
    char* pbuf = buffer;
    int sizeOfBuffer = sizeof(buffer);
    int cch;

    cch = sprintf_s(pbuf, sizeOfBuffer, "{callConv-%08X retTypeClass-%016llX retTypeSigClass-%016llX retType-%08X(%s) flg-%08X na-%u",
        sigInfo.callConv, sigInfo.retTypeClass, sigInfo.retTypeSigClass, sigInfo.retType, toString((CorInfoType)sigInfo.retType), sigInfo.flags, sigInfo.numArgs);
    pbuf += cch;
    sizeOfBuffer -= cch;

    FormatAgnostic_CORINFO_SIG_INST_Element(pbuf, sizeOfBuffer, " ", "cc", "ci", sigInfo.sigInst_classInstCount, sigInfo.sigInst_classInst_Index, handleMap);
    FormatAgnostic_CORINFO_SIG_INST_Element(pbuf, sizeOfBuffer, " ", "mc", "mi", sigInfo.sigInst_methInstCount, sigInfo.sigInst_methInst_Index, handleMap);

    cch = sprintf_s(pbuf, sizeOfBuffer, " args-%016llX si-%08X, cbSig-%08X",
        sigInfo.args, sigInfo.pSig_Index, sigInfo.cbSig);
    pbuf += cch;
    sizeOfBuffer -= cch;

    // Add the signature bytes to the output.
    // Normally, pSig_Index will be -1 if there is no signature. However, in some error cases
    // it will be 0 if there are other reasons why it will never be consulted (like a "return value"
    // from an API of `false`). So check that cbSig > 0 before calling GetBuffer(), just to be sure
    // there is some data to find.

    if (sigInfo.cbSig == 0)
    {
        cch = sprintf_s(pbuf, sizeOfBuffer, " (SIG SIZE ZERO)");
        pbuf += cch;
        sizeOfBuffer -= cch;
    }
    else
    {
        PCCOR_SIGNATURE pSig = buffers->GetBuffer(sigInfo.pSig_Index);
        if (pSig == nullptr)
        {
            cch = sprintf_s(pbuf, sizeOfBuffer, " (NO SIG FOUND)");
            pbuf += cch;
            sizeOfBuffer -= cch;
        }
        else
        {
            cch = sprintf_s(pbuf, sizeOfBuffer, " sig-{");
            pbuf += cch;
            sizeOfBuffer -= cch;

            const unsigned int maxSigDisplayBytes = 25; // Don't display more than this.
            const unsigned int sigDisplayBytes = min(maxSigDisplayBytes, sigInfo.cbSig);

            for (DWORD i = 0; i < sigDisplayBytes; i++)
            {
                cch = sprintf_s(pbuf, sizeOfBuffer, "%02X", pSig[i]);
                pbuf += cch;
                sizeOfBuffer -= cch;
            }

            if (sigDisplayBytes < sigInfo.cbSig)
            {
                cch = sprintf_s(pbuf, sizeOfBuffer, "...");
                pbuf += cch;
                sizeOfBuffer -= cch;
            }

            cch = sprintf_s(pbuf, sizeOfBuffer, "}");
            pbuf += cch;
            sizeOfBuffer -= cch;
        }
    }

    cch = sprintf_s(pbuf, sizeOfBuffer, " scp-%016llX tok-%08X}",
        sigInfo.scope, sigInfo.token);

    return std::string(buffer);
}

#endif
