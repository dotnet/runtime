// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ---------------------------------------------------------------------------
// EEContract.cpp
//

// ! I am the owner for issues in the contract *infrastructure*, not for every 
// ! CONTRACT_VIOLATION dialog that comes up. If you interrupt my work for a routine
// ! CONTRACT_VIOLATION, you will become the new owner of this file.
// ---------------------------------------------------------------------------


#include "common.h"
#include "dbginterface.h"


#ifdef ENABLE_CONTRACTS

void EEContract::Disable()
{
    BaseContract::Disable();
}

void EEContract::DoChecks(UINT testmask, __in_z const char *szFunction, __in_z const char *szFile, int lineNum)
{
    SCAN_IGNORE_THROW;      // Tell the static contract analyzer to ignore contract violations
    SCAN_IGNORE_FAULT;      // due to the contract checking logic itself.
    SCAN_IGNORE_TRIGGER;
    SCAN_IGNORE_LOCK;
    
    // Many of the checks below result in calls to GetThread()
    // that work just fine if GetThread() returns NULL, so temporarily
    // allow such calls.
    BEGIN_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;
    m_pThread = GetThread();
    if (m_pThread != NULL)
    {
        m_pClrDebugState = m_pThread->GetClrDebugState();
    }

    // Call our base DoChecks.
    BaseContract::DoChecks(testmask, szFunction, szFile, lineNum);

    m_testmask = testmask;
    m_contractStackRecord.m_testmask = testmask;

    // GC mode check
    switch (testmask & MODE_Mask)
    {
        case MODE_Coop:
            if (m_pThread == NULL || !m_pThread->PreemptiveGCDisabled())
            {
                //
                // Check if this is the debugger helper thread and has the runtime
                // stoppped.  If both of these things are true, then we do not care
                // whether we are in COOP mode or not.
                //
                if ((g_pDebugInterface != NULL) && 
                    g_pDebugInterface->ThisIsHelperThread() &&
                    g_pDebugInterface->IsStopped())
                {
                    break;
                }

                // Pretend that the threads doing GC are in cooperative mode so that code with 
                // MODE_COOPERATIVE contract works fine on them. 
                if (IsGCThread()) 
                {
                    break;
                }

                if (!( (ModeViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                {
                    if (m_pThread == NULL)
                    {
                        CONTRACT_ASSERT("You must have called SetupThread in order to be in GC Cooperative mode.",
                                        Contract::MODE_Preempt,
                                        Contract::MODE_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                       );
                    }
                    else
                    {
                        CONTRACT_ASSERT("MODE_COOPERATIVE encountered while thread is in preemptive state.",
                                        Contract::MODE_Preempt,
                                        Contract::MODE_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                       );
                    }
                }
            }
            break;

        case MODE_Preempt:
            // Unmanaged threads are considered permanently preemptive so a NULL thread amounts to a passing case here.
            if (m_pThread != NULL && m_pThread->PreemptiveGCDisabled())
            {
                if (!( (ModeViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                {
                        CONTRACT_ASSERT("MODE_PREEMPTIVE encountered while thread is in cooperative state.",
                                        Contract::MODE_Coop,
                                        Contract::MODE_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                       );
                    }
            }
            break;

        case MODE_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }

    // GC Trigger check
    switch (testmask & GC_Mask)
    {
        case GC_Triggers:
            // We don't want to do a full TRIGGERSGC here as this could corrupt
            // OBJECTREF-typed arguments to the function. 
            {
                if (m_pClrDebugState->GetGCNoTriggerCount())
                {
                    if (!( (GCViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                    {
                        CONTRACT_ASSERT("GC_TRIGGERS encountered in a GC_NOTRIGGER scope",
                                        Contract::GC_NoTrigger,
                                        Contract::GC_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                        );
                    }
                }
            }
            break;

        case GC_NoTrigger:
            m_pClrDebugState->ViolationMaskReset( GCViolation );

                // Inlined BeginNoTriggerGC
            m_pClrDebugState->IncrementGCNoTriggerCount();
            if (m_pThread && m_pThread->m_fPreemptiveGCDisabled)
                {
                m_pClrDebugState->IncrementGCForbidCount();
            }

            break;

        case GC_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }

    // Host Triggers check
    switch (testmask & HOST_Mask)
    {
        case HOST_Calls:
            {
                if (!m_pClrDebugState->IsHostCaller())
                {
                    if (!( (HostViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                    {
                        // Avoid infinite recursion by temporarily allowing HOST_CALLS
                        // violations so that we don't get contract asserts in anything
                        // called downstream of CONTRACT_ASSERT. If we unwind out of
                        // here, our dtor will reset our state to what it was on entry.
                        CONTRACT_VIOLATION(HostViolation);    
                        CONTRACT_ASSERT("HOST_CALLS  encountered in a HOST_NOCALLS scope",
                                        Contract::HOST_NoCalls,
                                        Contract::HOST_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                        );
                    }
                }
            }
            break;

        case HOST_NoCalls:
           //  m_pClrDebugState->ViolationMaskReset( HostViolation );
            m_pClrDebugState->ResetHostCaller();
            break;

        case HOST_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }
    END_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;

    // EE Thread-required check
    // NOTE: The following must NOT be inside BEGIN/END_GETTHREAD_ALLOWED, 
    // as the change to m_pClrDebugState->m_allowGetThread below would be
    // overwritten by END_GETTHREAD_ALLOWED.
    switch (testmask & EE_THREAD_Mask)
    {
        case EE_THREAD_Required:
            if (!((EEThreadViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
            {
                if (m_pThread == NULL)
                {
                    CONTRACT_ASSERT("EE_THREAD_REQUIRED encountered with no current EE Thread object in TLS.",
                                    Contract::EE_THREAD_Required,
                                    Contract::EE_THREAD_Mask,
                                    m_contractStackRecord.m_szFunction,
                                    m_contractStackRecord.m_szFile,
                                    m_contractStackRecord.m_lineNum
                                   );
                }
                else if (!m_pClrDebugState->IsGetThreadAllowed())
                {
                    // In general, it's unsafe for an EE_THREAD_NOT_REQUIRED function to
                    // call an EE_THREAD_REQUIRED function. In cases where it is safe,
                    // you may wrap the call to the EE_THREAD_REQUIRED function inside a
                    // BEGIN/END_GETTHREAD_ALLOWED block, but you may only do so if the
                    // case where GetThread() == NULL is clearly handled in a way that
                    // prevents entry into the BEGIN/END_GETTHREAD_ALLOWED block.
                    CONTRACT_ASSERT("EE_THREAD_REQUIRED encountered in an EE_THREAD_NOT_REQUIRED scope, without an intervening BEGIN/END_GETTHREAD_ALLOWED block.",
                                    Contract::EE_THREAD_Required,
                                    Contract::EE_THREAD_Mask,
                                    m_contractStackRecord.m_szFunction,
                                    m_contractStackRecord.m_szFile,
                                    m_contractStackRecord.m_lineNum
                                   );
                }
            }
            m_pClrDebugState->SetGetThreadAllowed();
            break;

        case EE_THREAD_Not_Required:
            m_pClrDebugState->ResetGetThreadAllowed();
            break;

        case EE_THREAD_Disabled:
            break;

        default:
            UNREACHABLE();
    }
}
#endif // ENABLE_CONTRACTS


BYTE* __stdcall GetAddrOfContractShutoffFlag()
{
    LIMITED_METHOD_CONTRACT;

    // Exposed entrypoint where we cannot probe or do anything TLS
    // related
    static BYTE gContractShutoffFlag = 0;

    return &gContractShutoffFlag;
}

