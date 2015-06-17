//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include <lldb/API/LLDB.h>
#include "mstypes.h"
#define DEFINE_EXCEPTION_RECORD
#include <dbgeng.h>
#include "debugclient.h"

typedef HRESULT (*CommandFunc)(PDEBUG_CLIENT client, const char *args);

bool 
sosCommandInitialize(lldb::SBDebugger debugger);

bool
corerunCommandInitialize(lldb::SBDebugger debugger);