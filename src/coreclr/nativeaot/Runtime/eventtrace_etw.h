// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header provides Redhawk-specific ETW code and macros, to allow sharing of common
// ETW code between Redhawk and desktop CLR.
//
#ifndef EVENTTRACE_ETW_H
#define EVENTTRACE_ETW_H

#ifdef FEATURE_ETW

#include <evntprov.h>
extern "C" {
    VOID EtwCallback(
        _In_ const GUID * SourceId,
        _In_ uint32_t ControlCode,
        _In_ uint8_t Level,
        _In_ uint64_t MatchAnyKeyword,
        _In_ uint64_t MatchAllKeyword,
        _In_opt_ EVENT_FILTER_DESCRIPTOR * FilterData,
        _Inout_opt_ void * CallbackContext);
}

//
// Python script generated code will call this function when MCGEN_PRIVATE_ENABLE_CALLBACK_V2 is defined
// to enable runtime events
#define MCGEN_PRIVATE_ENABLE_CALLBACK_V2(SourceId, ControlCode, Level, MatchAnyKeyword, MatchAllKeyword, FilterData, CallbackContext) \
        EtwCallback(SourceId, ControlCode, Level, MatchAnyKeyword, MatchAllKeyword, FilterData, CallbackContext)

#include "ClrEtwAll.h"

#undef ETW_TRACING_INITIALIZED
#define ETW_TRACING_INITIALIZED(RegHandle) (RegHandle != NULL)

#undef ETW_CATEGORY_ENABLED
#define ETW_CATEGORY_ENABLED(Context, LevelParam, Keyword) \
    (Context.IsEnabled &&                                                               \
    (                                                                                   \
        (LevelParam <= ((Context).Level)) ||                                                    \
        ((Context.Level) == 0)                                                           \
    ) &&                                                                                \
    (   \
        (Keyword == (ULONGLONG)0) ||    \
        (   \
            (Keyword & (Context.MatchAnyKeyword)) && \
            (   \
                (Keyword & (Context.MatchAllKeyword)) == (Context.MatchAllKeyword)    \
            )   \
        )   \
    )   \
    )

#endif // FEATURE_ETW

#endif // EVENTTRACE_ETW_H
