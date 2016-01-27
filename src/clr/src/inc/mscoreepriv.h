// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#ifndef __MSCOREEPRIV_H__
#define __MSCOREEPRIV_H__


typedef enum
{
    RUNTIME_INFO_CONSIDER_POST_2_0      = 0x80, // consider v4.0+ versions
    RUNTIME_INFO_EMULATE_EXE_LAUNCH     = 0x100, // Binds as if the provided information were being use in a new process
    RUNTIME_INFO_APPEND_FORCE_PERFORMANCE_COUNTER_UNIQUE_SHARED_MEMORY_READS_SETTING_TO_VERSION // appends either !0 (false), !1 (true) or !2 (unset) depending on the value of forcePerformanceCounterUniqueSharedMemoryReads in the runtime section of the config
                                        = 0x200,
} RUNTIME_INFO_FLAGS_FOR_SHARED_COMPONENTS;



#endif //__MSCOREEPRIV_H__

