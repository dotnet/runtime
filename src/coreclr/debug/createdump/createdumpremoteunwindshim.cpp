// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"

#ifdef _DEBUG

// Standalone createdump does not initialize the PAL debug channel infrastructure.
DWORD dbg_channel_flags[DCI_LAST] = {};
BOOL g_Dbg_asserts_enabled = TRUE;
FILE* output_file = nullptr;
Volatile<BOOL> dbg_master_switch = FALSE;

int DBG_printf(
    DBG_CHANNEL_ID,
    DBG_LEVEL_ID,
    BOOL,
    LPCSTR,
    LPCSTR,
    INT,
    LPCSTR,
    ...)
{
    return 0;
}

#endif // _DEBUG
