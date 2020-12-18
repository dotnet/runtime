// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

 */


#include "common.h"

namespace
{
//  some helper functions
//

template < typename T >
void init_array( T * arr, size_t size )
{
    ::ZeroMemory( arr, size * sizeof( T ) );
}


//  locals
//

const char * const fixupNames[] =
{
    "Class Handle",
    "Field Handle",
    "Method Handle",
    "Function Call/Pointer",
    "Sync",
    "Pinvoke",
    "Profiling Handle",
    "Interface Table",
    "Module Handle",
    "Module Domain ID",
    "Class Domain ID",
    "Field Address",
    "Varargs Handle",
    "String",
    "Init Class",
    "Load Class",
    "Stub Dispatch",
    "Active Dependency"
};

const char * const callReasons[] =
{
    "Unknown",
    "Exotic (Not IL or ECall)",
    "PInvoke",
    "Has Generic Instantiation",
    "No code generated yet",
    "Method has fixups",
    "Prestub may produce stub",
    "Remoting interception",
    "Contains Cer root",
    "Restore method (Generics)",
    "Restore first call",
    "Restore value type",
    "Restore",
    "Can't patch",
    "Profiling",
    "Other loader module",
};

static_assert_no_msg((sizeof(callReasons)/sizeof(callReasons[0])) == CORINFO_INDIRECT_CALL_COUNT);

}

ZapperStats::ZapperStats()
    : m_methods( 0 )
    , m_failedMethods( 0 )
    , m_failedILStubs( 0 )
    , m_ilCodeSize( 0 )
    , m_nativeCodeSize( 0 )
    , m_nativeColdCodeSize( 0 )
    , m_nativeRODataSize( 0 )
    , m_gcInfoSize( 0 )
#ifdef FEATURE_EH_FUNCLETS
    , m_unwindInfoSize( 0 )
#endif // FEATURE_EH_FUNCLETS
    , m_NumHotAllocations( 0 )
    , m_NumHotColdAllocations( 0 )
    , m_NumMediumHeaders( 0 )
    , m_nativeCodeSizeInSplitMethods( 0 )
    , m_nativeColdCodeSizeInSplitMethods( 0 )
    , m_nativeCodeSizeInSplitProfiledMethods( 0 )
    , m_nativeColdCodeSizeInSplitProfiledMethods( 0 )
    , m_nativeCodeSizeInProfiledMethods( 0 )
    , m_nativeColdCodeSizeInProfiledMethods( 0 )
    , m_totalHotCodeSize( 0 )
    , m_totalUnprofiledCodeSize( 0 )
    , m_totalColdCodeSize( 0 )
    , m_totalCodeSizeInProfiledMethods( 0 )
    , m_totalColdCodeSizeInProfiledMethods( 0 )
    , m_inputFileSize( 0 )
    , m_outputFileSize( 0 )
    , m_metadataSize( 0 )
    , m_preloadImageSize( 0 )
    , m_hotCodeMgrSize( 0 )
    , m_unprofiledCodeMgrSize( 0 )
    , m_coldCodeMgrSize( 0 )
    , m_eeInfoTableSize( 0 )
    , m_helperTableSize( 0 )
    , m_dynamicInfoTableSize( 0 )
    , m_dynamicInfoDelayListSize( 0 )
    , m_debuggingTableSize( 0 )
    , m_headerSectionSize( 0 )
    , m_codeSectionSize( 0 )
    , m_coldCodeSectionSize( 0 )
    , m_exceptionSectionSize( 0 )
    , m_readOnlyDataSectionSize( 0 )
    , m_relocSectionSize( 0 )
    , m_ILMetadataSize( 0 )
    , m_externalMethodThunkSize( 0 )
    , m_externalMethodDataSize( 0 )
    , m_prestubMethods( 0 )
    , m_directMethods( 0 )
{
    init_array( m_indirectMethodReasons, CORINFO_INDIRECT_CALL_COUNT );
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void ZapperStats::PrintStats()
{
    if (m_outputFileSize > 0) {

        GetSvcLogger()->Printf( "-------------------------------------------------------\n");
        GetSvcLogger()->Printf( "Input file size:            %8d\n",         m_inputFileSize);
        GetSvcLogger()->Printf( "Output file size:           %8d\n", m_outputFileSize);
        GetSvcLogger()->Printf( "Input file size/Output file size ratio:\t%8.2fx\n", double( m_outputFileSize ) / m_inputFileSize);
        GetSvcLogger()->Printf( "\n");
        GetSvcLogger()->Printf( "Metadata:                   %8d\t%8.2f%%\n", m_metadataSize, (double)m_metadataSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Debugging maps:             %8d\t%8.2f%%\n", m_debuggingTableSize, (double)m_debuggingTableSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Hot Code manager:           %8d\t%8.2f%%\n", m_hotCodeMgrSize, (double)m_hotCodeMgrSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Unprofiled Code manager:    %8d\t%8.2f%%\n", m_unprofiledCodeMgrSize, (double)m_unprofiledCodeMgrSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Cold Code manager:          %8d\t%8.2f%%\n", m_coldCodeMgrSize, (double)m_coldCodeMgrSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "GC info:                    %8d\t%8.2f%%\n", m_headerSectionSize, (double)m_headerSectionSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Native code & r/o data:     %8d\t%8.2f%%\n", m_codeSectionSize, (double)m_codeSectionSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Cold code:                  %8d\t%8.2f%%\n", m_coldCodeSectionSize, (double)m_coldCodeSectionSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Exception tables:           %8d\t%8.2f%%\n", m_exceptionSectionSize, (double)m_exceptionSectionSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Relocs:                     %8d\t%8.2f%%\n", m_relocSectionSize, (double)m_relocSectionSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "IL metadata:                %8d\t%8.2f%%\n", m_ILMetadataSize, (double)m_ILMetadataSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "External Method Thunks:     %8d\t%8.2f%%\n", m_externalMethodThunkSize, (double)m_externalMethodThunkSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "External Method Data:       %8d\t%8.2f%%\n", m_externalMethodDataSize, (double)m_externalMethodDataSize/m_outputFileSize*100);
        GetSvcLogger()->Printf( "Image of EE structures:     %8d\t%8.2f%%\n", m_preloadImageSize, (double)m_preloadImageSize/m_outputFileSize*100);

        unsigned totalIndirections =
          m_dynamicInfoDelayListSize +
          m_eeInfoTableSize +
          m_helperTableSize +
          m_dynamicInfoTableSize;

        GetSvcLogger()->Printf( "Indirections:               %8d\t%8.2f%%\n",
                totalIndirections, (double)totalIndirections/m_outputFileSize*100);
        GetSvcLogger()->Printf( "       ----- Breakdown of Indirections ----\n");

        GetSvcLogger()->Printf( "       Delay load lists:           %8d\t%8.2f%%\n",
                m_dynamicInfoDelayListSize, (double)m_dynamicInfoDelayListSize/totalIndirections*100);
        GetSvcLogger()->Printf( "       Tables:                     %8d\t%8.2f%%\n",
                m_dynamicInfoTableSize, (double)m_dynamicInfoTableSize/totalIndirections*100);
        GetSvcLogger()->Printf( "       EE Values:                  %8d\t%8.2f%%\n",
                m_eeInfoTableSize, (double)m_eeInfoTableSize/totalIndirections*100);
        GetSvcLogger()->Printf( "       Helper functions:           %8d\t%8.2f%%\n",
                m_helperTableSize, (double)m_helperTableSize/totalIndirections*100);
    }

    GetSvcLogger()->Printf( "       Direct method descs:\t%5d/%5d %8.2f%%\n",
            m_directMethods, m_prestubMethods+m_directMethods,
            (double)m_directMethods/(m_directMethods+m_prestubMethods)*100);
    GetSvcLogger()->Printf( "       Indirect method descs:\t%5d/%5d %8.2f%%\n",
            m_prestubMethods, m_prestubMethods+m_directMethods,
            (double)m_prestubMethods/(m_directMethods+m_prestubMethods)*100);

    for (int i=0; i < CORINFO_INDIRECT_CALL_COUNT; i++)
        GetSvcLogger()->Printf( "               %-30s  %5d %8.2f%%\n",
                callReasons[i],
                m_indirectMethodReasons[i],
                double(m_indirectMethodReasons[i])/(m_directMethods+m_prestubMethods)*100);

    GetSvcLogger()->Printf( "-------------------------------------------------------\n");
    GetSvcLogger()->Printf( "Total Methods: \t\t\t\t%8d\n", m_methods);
    GetSvcLogger()->Printf( "Total Hot Only: \t\t\t%8d\n", m_NumHotAllocations);
    GetSvcLogger()->Printf( "Total Split (Hot/Cold): \t\t%8d\n", m_NumHotColdAllocations);
    GetSvcLogger()->Printf( "Total Medium Headers: \t\t\t%8d\n", m_NumMediumHeaders);
    GetSvcLogger()->Printf( "Split Methods: Hot Code \t\t%8d\n", m_nativeCodeSizeInSplitMethods);
    GetSvcLogger()->Printf( "Split Methods: Cold Code \t\t%8d\n", m_nativeColdCodeSizeInSplitMethods);
    GetSvcLogger()->Printf( "Split Profiled Methods: Hot Code \t%8d\n", m_nativeCodeSizeInSplitProfiledMethods);
    GetSvcLogger()->Printf( "Split Profiled Methods: Cold Code \t%8d\n", m_nativeColdCodeSizeInSplitProfiledMethods);
    GetSvcLogger()->Printf( "Profiled Methods: Hot Code \t\t%8d\n", m_nativeCodeSizeInProfiledMethods);
    GetSvcLogger()->Printf( "Profiled Methods: Cold Code \t\t%8d\n", m_nativeColdCodeSizeInProfiledMethods);
    GetSvcLogger()->Printf( "Profiled Methods: Total Hot Code+Headers %7d\n", m_totalCodeSizeInProfiledMethods);
    GetSvcLogger()->Printf( "Profiled Methods: Total Cold Code+Headers %6d\n", m_totalColdCodeSizeInProfiledMethods);
    GetSvcLogger()->Printf( "All Methods: Total Hot+Unprofiled Code \t\t%8d\n",  m_nativeCodeSize);
    GetSvcLogger()->Printf( "All Methods: Total Cold Code \t\t%8d\n", m_nativeColdCodeSize);
    GetSvcLogger()->Printf( "All Methods: Total Hot Code+Headers \t%8d\n",  m_totalHotCodeSize);
    GetSvcLogger()->Printf( "All Methods: Total Unprofiled Code+Headers \t%8d\n", m_totalUnprofiledCodeSize);
    GetSvcLogger()->Printf( "All Methods: Total Cold Code+Headers \t%8d\n", m_totalColdCodeSize);

    GetSvcLogger()->Printf( "-------------------------------------------------------\n");
    GetSvcLogger()->Printf( "Total IL Code:          %8d\n", m_ilCodeSize);
    GetSvcLogger()->Printf( "Total Native Code:      %8d\n", m_nativeCodeSize + m_nativeColdCodeSize);
    GetSvcLogger()->Printf( "Total Code+Headers:     %8d\n", m_totalHotCodeSize + m_totalUnprofiledCodeSize + m_totalColdCodeSize);

    GetSvcLogger()->Printf( "Total Native RO Data:   %8d\n", m_nativeRODataSize);
    GetSvcLogger()->Printf( "Total Native GC Info:   %8d\n", m_gcInfoSize);
    size_t nativeTotal = m_nativeCodeSize + m_nativeRODataSize + m_gcInfoSize;
#ifdef FEATURE_EH_FUNCLETS
    GetSvcLogger()->Printf( "Total Native UnwindInfo:%8d\n", m_unwindInfoSize);
    nativeTotal += m_unwindInfoSize;
#endif // FEATURE_EH_FUNCLETS
    GetSvcLogger()->Printf( "Total Native Total :    %8d\n", nativeTotal);

    if (m_methods > 0) {
        GetSvcLogger()->Printf( "\n");
        GetSvcLogger()->Printf( "Average IL Code:            %8.2f\n", double(m_ilCodeSize) / m_methods);
        GetSvcLogger()->Printf( "Average NativeCode:         %8.2f\n", double(m_nativeCodeSize) / m_methods);
        GetSvcLogger()->Printf( "Average Native RO Data:     %8.2f\n", double(m_nativeRODataSize) / m_methods);
        GetSvcLogger()->Printf( "Average Native GC Info:     %8.2f\n", double(m_gcInfoSize) / m_methods);
#ifdef FEATURE_EH_FUNCLETS
        GetSvcLogger()->Printf( "Average Native UnwindInfo:  %8.2f\n", double(m_unwindInfoSize) / m_methods);
#endif // FEATURE_EH_FUNCLETS
        GetSvcLogger()->Printf( "Average Native:             %8.2f\n", double(nativeTotal) / m_methods);
        GetSvcLogger()->Printf( "\n");
        GetSvcLogger()->Printf( "NativeGC / Native:      %8.2f\n", double(m_gcInfoSize) / nativeTotal);
        GetSvcLogger()->Printf( "Native / IL:            %8.2f\n", double(nativeTotal) / m_ilCodeSize);
    }

    if (m_failedMethods > 0)
        GetSvcLogger()->Printf( "Methods which did not compile: %d\n", m_failedMethods);
    if (m_failedILStubs > 0)
        GetSvcLogger()->Printf( "IL STUBS which did not compile: %d\n", m_failedILStubs);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

char const * GetCallReasonString( CorInfoIndirectCallReason reason )
{
    return callReasons[ reason ];
}
