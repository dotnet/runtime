//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: DacDbiImplLocks.cpp
//

//
// Implement DAC/DBI interface for testing our ability to detect when the LS
// holds a lock that we encounter while executing in the DAC.
//
//*****************************************************************************

#include "stdafx.h"
#include "dacdbiinterface.h"
#include "holder.h"
#include "switches.h"
#include "dacdbiimpl.h"

// ============================================================================
// Functions to test data safety. In these functions we determine whether a lock
// is held in a code path we need to execute for inspection. If so, we throw an 
// exception. 
// ============================================================================

#ifdef TEST_DATA_CONSISTENCY
#include "crst.h"

void DacDbiInterfaceImpl::TestCrst(VMPTR_Crst vmCrst)
{
    DD_ENTER_MAY_THROW;

    DebugTryCrst(vmCrst.GetDacPtr());
}

void DacDbiInterfaceImpl::TestRWLock(VMPTR_SimpleRWLock vmRWLock)
{
    DD_ENTER_MAY_THROW;

    DebugTryRWLock(vmRWLock.GetDacPtr());
}
#endif

