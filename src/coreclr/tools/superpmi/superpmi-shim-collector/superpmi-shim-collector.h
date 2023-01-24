// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SuperPMI-Shim-Collector.h - Shim that collects and yields .mc (method context) files.
//----------------------------------------------------------
#ifndef _SuperPMIShim
#define _SuperPMIShim

class MethodContext;
extern MethodContext* g_globalContext;
extern char*          g_collectionFilter;

void DebugBreakorAV(int val);

#endif
