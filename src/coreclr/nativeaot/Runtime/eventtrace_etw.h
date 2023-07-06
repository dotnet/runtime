// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header provides Redhawk-specific ETW code and macros, to allow sharing of common
// ETW code between Redhawk and desktop CLR.
//
#ifndef EVENTTRACE_ETW_H
#define EVENTTRACE_ETW_H

#ifdef FEATURE_ETW

// Map the CLR private provider to our version so we can avoid inserting more #ifdef's in the code.
#define MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_Context
#define MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Context
#define Microsoft_Windows_DotNETRuntimeHandle Microsoft_Windows_Redhawk_GC_PublicHandle

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
