// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __PREDEFTLSSLOT_H__
#define __PREDEFTLSSLOT_H__

// ******************************************************************************
// WARNING!!!: These enums are used by SOS in the diagnostics repo. Values should
// added or removed in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/shared/inc/predeftlsslot.h
// ******************************************************************************

// The historic location of ThreadType slot kept for compatibility with SOS
// TODO: Introduce DAC API to make this hack unnecessary
enum PredefinedTlsSlots
{
    TlsIdx_ThreadType = 11 // bit flags to indicate special thread's type
};

enum TlsThreadTypeFlag // flag used for thread type in Tls data
{
    ThreadType_GC                       = 0x00000001,
    ThreadType_Timer                    = 0x00000002,
    ThreadType_Gate                     = 0x00000004,
    ThreadType_DbgHelper                = 0x00000008,
    ThreadType_Shutdown                 = 0x00000010,
    ThreadType_DynamicSuspendEE         = 0x00000020,
    ThreadType_Finalizer                = 0x00000040,
    ThreadType_ADUnloadHelper           = 0x00000200,
    ThreadType_ShutdownHelper           = 0x00000400,
    ThreadType_Threadpool_IOCompletion  = 0x00000800,
    ThreadType_Threadpool_Worker        = 0x00001000,
    ThreadType_Wait                     = 0x00002000,
    ThreadType_ProfAPI_Attach           = 0x00004000,
    ThreadType_ProfAPI_Detach           = 0x00008000,
    ThreadType_ETWRundownThread         = 0x00010000,
    ThreadType_GenericInstantiationCompare= 0x00020000, // Used to indicate that the thread is determining if a generic instantiation in an ngen image matches a lookup.
};

#endif

