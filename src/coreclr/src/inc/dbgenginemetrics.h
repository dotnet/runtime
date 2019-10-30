// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// DbgEngineMetrics.h
//
// This file contains the defintion of CLR_ENGINE_METRICS.  This struct is used for Silverlight debugging.
// 
// ======================================================================================



#ifndef __DbgEngineMetrics_h__
#define __DbgEngineMetrics_h__

//---------------------------------------------------------------------------------------
//
// This struct contains information necessary for Silverlight debugging.  coreclr.dll has a static struct
// of this type.  It is read by dbgshim.dll to help synchronize the debugger and coreclr.dll in launch 
// and early attach scenarios.
//

typedef struct tagCLR_ENGINE_METRICS
{
    DWORD   cbSize;                 // the size of the struct; also identifies the format of the struct
    DWORD   dwDbiVersion;           // the version of the debugging interface expected by this CoreCLR
    LPVOID  phContinueStartupEvent; // pointer to the continue startup event handle
} CLR_ENGINE_METRICS;

#endif // __DbgEngineMetrics_h__
