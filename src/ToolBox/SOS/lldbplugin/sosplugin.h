// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <lldb/API/LLDB.h>
#include "mstypes.h"
#define DEFINE_EXCEPTION_RECORD
#include <dbgeng.h>
#include <dbgtargetcontext.h>
#include "debugclient.h"

typedef HRESULT (*CommandFunc)(PDEBUG_CLIENT client, const char *args);

extern char *g_coreclrDirectory;
extern ULONG g_currentThreadIndex;
extern ULONG g_currentThreadSystemId;

bool 
sosCommandInitialize(lldb::SBDebugger debugger);

bool
setsostidCommandInitialize(lldb::SBDebugger debugger);

bool
setclrpathCommandInitialize(lldb::SBDebugger debugger);

bool
corerunCommandInitialize(lldb::SBDebugger debugger);
