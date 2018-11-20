// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*

 */


#ifndef ZAPPER_STATS_H_0170123740208327
#define ZAPPER_STATS_H_0170123740208327

#include "sarray.h"
#include "sstring.h"
#include "corcompile.h"
#include "arraylist.h"
#include "log.h"
#include "shash.h"
#include "utilcode.h"


//  forward declarations
class ZapperOptions;

class ZapperStats
{
 public:

    unsigned m_methods;                 // Total number of methods
    unsigned m_failedMethods;           // Methods which failed to compile correctly
    unsigned m_failedILStubs;           // ILSTUB methods which failed to compile correctly

    ULONG    m_ilCodeSize;
    ULONG    m_nativeCodeSize;          // Really just the Hot Code Size + Unprofiled size
    ULONG    m_nativeColdCodeSize;
    ULONG    m_nativeRODataSize;
    ULONG    m_gcInfoSize;
#ifdef WIN64EXCEPTIONS
    ULONG    m_unwindInfoSize;
#endif // WIN64EXCEPTIONS

    ULONG    m_NumHotAllocations;
    ULONG    m_NumHotColdAllocations;
    ULONG    m_NumMediumHeaders;

    ULONG    m_nativeCodeSizeInSplitMethods;
    ULONG    m_nativeColdCodeSizeInSplitMethods;
    ULONG    m_nativeCodeSizeInSplitProfiledMethods;
    ULONG    m_nativeColdCodeSizeInSplitProfiledMethods;
    ULONG    m_nativeCodeSizeInProfiledMethods;
    ULONG    m_nativeColdCodeSizeInProfiledMethods;
    ULONG    m_totalHotCodeSize;
    ULONG    m_totalUnprofiledCodeSize;
    ULONG    m_totalColdCodeSize;
    ULONG    m_totalCodeSizeInProfiledMethods;
    ULONG    m_totalColdCodeSizeInProfiledMethods;

    unsigned m_inputFileSize;
    unsigned m_outputFileSize;
    unsigned m_metadataSize;
    unsigned m_preloadImageSize;
    unsigned m_hotCodeMgrSize;
    unsigned m_unprofiledCodeMgrSize;
    unsigned m_coldCodeMgrSize;
    unsigned m_eeInfoTableSize;
    unsigned m_helperTableSize;
    unsigned m_dynamicInfoTableSize;
    unsigned m_dynamicInfoDelayListSize;
    unsigned m_debuggingTableSize;
    unsigned m_headerSectionSize;
    unsigned m_codeSectionSize;
    unsigned m_coldCodeSectionSize;
    unsigned m_exceptionSectionSize;
    unsigned m_readOnlyDataSectionSize;
    unsigned m_relocSectionSize;
    unsigned m_ILMetadataSize;
    unsigned m_virtualImportThunkSize;
    unsigned m_externalMethodThunkSize;
    unsigned m_externalMethodDataSize;

    unsigned m_prestubMethods;
    unsigned m_directMethods;
    unsigned m_indirectMethodReasons[CORINFO_INDIRECT_CALL_COUNT];

    ZapperStats();
    void PrintStats();
};

char const * GetCallReasonString( CorInfoIndirectCallReason reason );

#endif  //  ZAPPER_STATS_H_0170123740208327

