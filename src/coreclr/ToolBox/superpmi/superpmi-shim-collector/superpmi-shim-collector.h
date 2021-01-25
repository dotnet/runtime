//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SuperPMI-Shim-Collector.h - Shim that collects and yields .mc (method context) files.
//----------------------------------------------------------
#ifndef _SuperPMIShim
#define _SuperPMIShim

class MethodContext;
extern MethodContext* g_globalContext;

void DebugBreakorAV(int val);

#endif
