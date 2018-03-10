// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ngencommon.h - cross-compilation enablement structures.
//



#ifndef _NGENCOMMON_H_
#define _NGENCOMMON_H_

#define NGENWORKER_FLAGS_TUNING                  0x0001

#define NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK   0x0004

#define NGENWORKER_FLAGS_WINMD_RESILIENT         0x1000
#define NGENWORKER_FLAGS_READYTORUN              0x2000
#define NGENWORKER_FLAGS_NO_METADATA             0x4000
#define NGENWORKER_FLAGS_SILENT                  0x8000
#define NGENWORKER_FLAGS_VERBOSE                0x10000

#endif // _NGENCOMMON_H_
