// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

#include "common.h"
#include "gcenv.h"
#include "gchandletableimpl.h"
#include "objecthandle.h"

IGCHandleTable* CreateGCHandleTable()
{
    return new(nothrow) GCHandleTable();
}

bool GCHandleTable::Initialize()
{
    return Ref_Initialize();
}

void GCHandleTable::Shutdown()
{
    Ref_Shutdown();
}
