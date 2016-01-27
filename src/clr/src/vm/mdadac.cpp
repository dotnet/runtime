// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "mda.h"
#include "mdaassistants.h"
#include "sstring.h"
#include "daccess.h"

#ifdef MDA_SUPPORTED
MdaStaticHeap g_mdaStaticHeap = 
{ 
    { 0 },  // m_assistants[]
    0,      // m_pMda
    { 0 },  // m_mda[]

#define MDA_ASSISTANT_STATIC_INIT
#include "mdaschema.inl"
#undef MDA_ASSISTANT_STATIC_INIT
};


//
// MdaManagedDebuggingAssistants
//
void ManagedDebuggingAssistants::AllocateManagedDebuggingAssistants()
{
    WRAPPER_NO_CONTRACT;
    g_mdaStaticHeap.m_pMda = new (&g_mdaStaticHeap.m_mda) ManagedDebuggingAssistants();
}

ManagedDebuggingAssistants::ManagedDebuggingAssistants()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    Initialize();
#endif
}
#endif // MDA_SUPPORTED






