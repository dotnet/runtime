// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ngencommon.h - cross-compilation enablement structures.
//



#ifndef _NGENCOMMON_H_
#define _NGENCOMMON_H_

#define NGENWORKER_FLAGS_TUNING                  0x0001

#define NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK   0x0004
#define NGENWORKER_FLAGS_LARGEVERSIONBUBBLE      0x0008

#define NGENWORKER_FLAGS_READYTORUN              0x2000
#define NGENWORKER_FLAGS_SILENT                  0x8000
#define NGENWORKER_FLAGS_VERBOSE                0x10000
#define NGENWORKER_FLAGS_SUPPRESS_WARNINGS      0x20000

#endif // _NGENCOMMON_H_
