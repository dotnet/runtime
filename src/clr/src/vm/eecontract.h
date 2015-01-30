//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// ---------------------------------------------------------------------------
// EEContract.h
//

// ! I am the owner for issues in the contract *infrastructure*, not for every 
// ! CONTRACT_VIOLATION dialog that comes up. If you interrupt my work for a routine
// ! CONTRACT_VIOLATION, you will become the new owner of this file.
// ---------------------------------------------------------------------------


#ifndef EECONTRACT_H_
#define EECONTRACT_H_

#include "contract.h"
#include "stackprobe.h"

// --------------------------------------------------------------------------------
// EECONTRACT is an extension of the lower level CONTRACT macros to include some
// EE specific stuff like GC mode checking.  See check.h for more info on CONTRACT.
// --------------------------------------------------------------------------------

#undef GC_TRIGGERS
#undef GC_NOTRIGGER

#ifdef ENABLE_CONTRACTS_IMPL

class EEContract : public BaseContract
{
  private:
    Thread *m_pThread; // Current thread pointer
    // Have to override this function in any derived class to indicate that a valid destructor is defined for this class
    virtual void DestructorDefinedThatCallsRestore(){}

  public:
    __declspec(nothrow) ~EEContract()
    {
        Restore();
    }

    void Disable();
    void DoChecks(UINT testmask, __in_z const char *szFunction, __in_z char *szFile, int lineNum);
};



#define MODE_COOPERATIVE     do { STATIC_CONTRACT_MODE_COOPERATIVE; REQUEST_TEST(Contract::MODE_Coop,     Contract::MODE_Disabled); } while(0)
#define MODE_PREEMPTIVE      do { STATIC_CONTRACT_MODE_PREEMPTIVE; REQUEST_TEST(Contract::MODE_Preempt,  Contract::MODE_Disabled); } while(0)
#define MODE_ANY             do { STATIC_CONTRACT_MODE_ANY; REQUEST_TEST(Contract::MODE_Disabled, Contract::MODE_Disabled); } while(0)

#define GC_TRIGGERS          do { STATIC_CONTRACT_GC_TRIGGERS; REQUEST_TEST(Contract::GC_Triggers,   Contract::GC_Disabled); } while(0)
#define GC_NOTRIGGER         do { STATIC_CONTRACT_GC_NOTRIGGER; REQUEST_TEST(Contract::GC_NoTrigger,  Contract::GC_Disabled); } while(0)

// Notice there's no static contract component to this.  It's
// perfectly reasonable to find EE_THREAD_REQUIRED inside the scope of
// EE_THREAD_NOT_REQUIRED (e.g., an EE_THREAD_NOT_REQUIRED scope can have two
// possible code paths--one with an EE Thread and one without).  So we can't do
// any meaningful testing statically.  It's all gotta be done at runtime.
#define EE_THREAD_NOT_REQUIRED  \
                             do { REQUEST_TEST(Contract::EE_THREAD_Not_Required, Contract::EE_THREAD_Disabled); } while(0)

#define EE_THREAD_REQUIRED   do { REQUEST_TEST(Contract::EE_THREAD_Required, Contract::EE_THREAD_Disabled); } while(0)

#define HOST_NOCALLS         do { STATIC_CONTRACT_HOST_NOCALLS; REQUEST_TEST(Contract::HOST_NoCalls, Contract::HOST_Disabled); } while(0)
#define HOST_CALLS           do {  STATIC_CONTRACT_HOST_CALLS; REQUEST_TEST(Contract::HOST_Calls, Contract::HOST_Disabled); } while(0)

#else   // ENABLE_CONTRACTS_IMPL

#define MODE_COOPERATIVE
#define MODE_PREEMPTIVE
#define MODE_ANY
#define GC_TRIGGERS
#define GC_NOTRIGGER
#define HOST_NOCALLS
#define HOST_CALLS
#define EE_THREAD_NOT_REQUIRED
#define EE_THREAD_REQUIRED


#endif  // ENABLE_CONTRACTS_IMPL

// Replace the CONTRACT macro with the EE version
#undef CONTRACT
#define CONTRACT(_returntype)  CUSTOM_CONTRACT(EEContract, _returntype)

#undef CONTRACT_VOID
#define CONTRACT_VOID  CUSTOM_CONTRACT_VOID(EEContract)

#undef CONTRACTL
#define CONTRACTL  CUSTOM_CONTRACTL(EEContract)

#undef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_CONTRACT CUSTOM_LIMITED_METHOD_CONTRACT(EEContract)

#undef WRAPPER_NO_CONTRACT
#define WRAPPER_NO_CONTRACT CUSTOM_WRAPPER_NO_CONTRACT(EEContract)

//
// The default contract is the recommended contract for ordinary EE code.
// The ordinary EE code can throw or trigger GC any time, does not operate
// on raw object refs, etc.
//

#undef STANDARD_VM_CHECK
#define STANDARD_VM_CHECK           \
    THROWS;                     \
    GC_TRIGGERS;                \
    MODE_PREEMPTIVE;            \
    SO_INTOLERANT;              \
    INJECT_FAULT(COMPlusThrowOM();); \

#endif  // EECONTRACT_H_
