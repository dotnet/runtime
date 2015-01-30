//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// ngencommon.h - cross-compilation enablement structures.
//



#ifndef _NGENCOMMON_H_
#define _NGENCOMMON_H_

#define NGENWORKER_FLAGS_TUNING                  0x0001
#define NGENWORKER_FLAGS_APPCOMPATWP8            0x0002
#define NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK   0x0004
#define NGENWORKER_FLAGS_FULLTRUSTDOMAIN         0x0008
#ifdef MDIL
#define NGENWORKER_FLAGS_CREATEMDIL 0x0010
#define NGENWORKER_FLAGS_MINIMAL_MDIL 0x0040
#define NGENWORKER_FLAGS_EMBEDMDIL 0x0080
#define NGENWORKER_FLAGS_NOMDIL 0x0100
#endif
#define NGENWORKER_FLAGS_WINMD_RESILIENT         0x1000
#define NGENWORKER_FLAGS_READYTORUN              0x2000
#define NGENWORKER_FLAGS_NO_METADATA             0x4000

#endif // _NGENCOMMON_H_
