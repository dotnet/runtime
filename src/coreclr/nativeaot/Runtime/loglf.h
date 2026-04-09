// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The code in sos.DumpStressLog depends on the facility codes
// being bit flags sorted in increasing order.
// See code:EEStartup#TableOfContents for EE overview
DEFINE_LOG_FACILITY(LF_GC           ,0x00000001)
DEFINE_LOG_FACILITY(LF_GCINFO       ,0x00000002)
DEFINE_LOG_FACILITY(LF_GCALLOC      ,0x00000004)
DEFINE_LOG_FACILITY(LF_GCROOTS      ,0x00000008)
DEFINE_LOG_FACILITY(LF_STARTUP      ,0x00000010)  // Log startup and shutdown failures
DEFINE_LOG_FACILITY(LF_STACKWALK    ,0x00000020)
//                  LF_ALWAYS        0x80000000   // make certain you don't try to use this bit for a real facility
//                  LF_ALL           0xFFFFFFFF
//
#undef DEFINE_LOG_FACILITY

