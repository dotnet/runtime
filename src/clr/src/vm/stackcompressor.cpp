// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 


#include "common.h"

#ifdef FEATURE_COMPRESSEDSTACK

#include "stackcompressor.h"
#include "securitystackwalk.h"
#include "appdomainstack.inl"
#include "comdelegate.h"

//-----------------------------------------------------------
// Stack walk callback data structure for stack compress.
//-----------------------------------------------------------
typedef struct _StackCompressData
{
    void*    compressedStack;
    StackCrawlMark *    stackMark;
    DWORD               dwFlags;
    Assembly *          prevAssembly; // Previously checked assembly.
    AppDomain *         prevAppDomain;
    Frame* pCtxTxFrame;
} StackCompressData;


void TurnSecurityStackWalkProgressOn( Thread* pThread ) 
{ 
    WRAPPER_NO_CONTRACT;
    pThread->SetSecurityStackwalkInProgress( TRUE ); 
}
void TurnSecurityStackWalkProgressOff( Thread* pThread ) 
{ 
    WRAPPER_NO_CONTRACT;
    pThread->SetSecurityStackwalkInProgress( FALSE ); 
}
typedef Holder< Thread*, TurnSecurityStackWalkProgressOn, TurnSecurityStackWalkProgressOff > StackWalkProgressEnableHolder;



DWORD StackCompressor::GetCSInnerAppDomainOverridesCount(COMPRESSEDSTACKREF csRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    //csRef can be NULL - that implies that we set the CS, then crossed an AD. So we would already have counted the overrides when we hit the
    // ctxTxFrame. Nothing to do here
    if (csRef != NULL)
    {
        NewCompressedStack* cs = (NewCompressedStack*)csRef->GetUnmanagedCompressedStack();
        if (cs != NULL)
            return cs->GetInnerAppDomainOverridesCount();
    }
    return 0;
}
DWORD StackCompressor::GetCSInnerAppDomainAssertCount(COMPRESSEDSTACKREF csRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    //csRef can be NULL - that implies that we set the CS, then crossed an AD. So we would already have counted the overrides when we hit the
    // ctxTxFrame. Nothing to do here
    if (csRef != NULL)
    {
        NewCompressedStack* cs = (NewCompressedStack*)csRef->GetUnmanagedCompressedStack();
        if (cs != NULL)
            return cs->GetInnerAppDomainAssertCount();
    }
    return 0;
}

void* StackCompressor::SetAppDomainStack(Thread* pThread, void* curr)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        THROWS;
    } CONTRACTL_END;

    NewCompressedStack* unmanagedCompressedStack = (NewCompressedStack *)curr;

    AppDomainStack* pRetADStack = NULL;
       
    if (unmanagedCompressedStack != NULL)
    {
        pRetADStack = new AppDomainStack(pThread->GetAppDomainStack());
        pThread->SetAppDomainStack(unmanagedCompressedStack->GetAppDomainStack() );
    }
    else
    {
        if (!pThread->IsDefaultSecurityInfo())    /* Do nothing for the single domain/FT/no overrides case */
        {
            pRetADStack = new AppDomainStack(pThread->GetAppDomainStack());            
            pThread->ResetSecurityInfo(); 
        }
    }
    return (void*)pRetADStack;
}

void StackCompressor::RestoreAppDomainStack(Thread* pThread, void* appDomainStack)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(appDomainStack != NULL);
    AppDomainStack* pADStack = (AppDomainStack*)appDomainStack;
    pThread->SetAppDomainStack(*pADStack);
    delete pADStack;
}

void StackCompressor::Destroy(void *stack)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(stack != NULL && "Don't pass NULL");
    NewCompressedStack* ncs = (NewCompressedStack*)stack;
    ncs->Destroy();
}


/* Forward declarations of the new CS stackwalking implementation */
static void NCS_GetCompressedStackWorker(Thread *t, void *pData);
static StackWalkAction NCS_CompressStackCB(CrawlFrame* pCf, void *pData);

OBJECTREF StackCompressor::GetCompressedStack( StackCrawlMark* stackMark, BOOL fWalkStack )
{
   
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;// Not an entry point
    } CONTRACTL_END;

    // Get the current thread that we're to walk.
    Thread * t = GetThread();    

    NewCompressedStackHolder csHolder(new NewCompressedStack());

        
    if (fWalkStack)
    {
        //
        // Initialize the callback data on the stack...        
        //

        StackCompressData walkData;
        
        walkData.dwFlags = 0;
        walkData.prevAssembly = NULL;
        walkData.prevAppDomain = NULL;
        walkData.stackMark = stackMark;
        walkData.pCtxTxFrame = NULL;


        walkData.compressedStack = (void*)csHolder.GetValue();
        NCS_GetCompressedStackWorker(t, &walkData);
    }

    struct _gc {
        SAFEHANDLE pSafeCSHandle;
    } gc;
    gc.pSafeCSHandle = NULL;
    
    GCPROTECT_BEGIN(gc);
    
    gc.pSafeCSHandle = (SAFEHANDLE) AllocateObject(MscorlibBinder::GetClass(CLASS__SAFE_CSHANDLE));
    CallDefaultConstructor(gc.pSafeCSHandle);
    gc.pSafeCSHandle->SetHandle((void*) csHolder.GetValue());
    csHolder.SuppressRelease();
    
    GCPROTECT_END();
    return (OBJECTREF) gc.pSafeCSHandle;
}

    
void NCS_GetCompressedStackWorker(Thread *t, void *pData)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    StackCompressData *pWalkData = (StackCompressData*)pData;
    NewCompressedStack* compressedStack = (NewCompressedStack*) pWalkData->compressedStack;

    _ASSERTE( t != NULL );

    {
        StackWalkProgressEnableHolder holder( t );

        //
        // Begin the stack walk...
        //
        // LIGHTUNWIND flag: allow using stackwalk cache for security stackwalks
        // SKIPFUNCLETS flag: stop processing the stack after completing the current funclet (for instance
        // only process the catch block on x64, not the throw block)
        t->StackWalkFrames(NCS_CompressStackCB, pWalkData, SKIPFUNCLETS | LIGHTUNWIND);


        // Ignore CS (if present) when we hit a FT assert
        if (pWalkData->dwFlags & CORSEC_FT_ASSERT)
            return;
        
        // Check if there is a CS at the top of the thread
        COMPRESSEDSTACKREF csRef = (COMPRESSEDSTACKREF)t->GetCompressedStack();
        AppDomain *pAppDomain = t->GetDomain();
        Frame *pFrame = NULL;
#ifdef FEATURE_REMOTING
        if (csRef == NULL)
        {
            // There may have been an AD transition and we shd look at the CB data to see if this is the case
            if (pWalkData->pCtxTxFrame != NULL)
            {
                pFrame = pWalkData->pCtxTxFrame;
                csRef = Security::GetCSFromContextTransitionFrame(pFrame);
                _ASSERTE(csRef != NULL); //otherwise we would not have saved the frame in the CB data
                pAppDomain = pWalkData->pCtxTxFrame->GetReturnDomain();
            }
        }
#endif // FEATURE_REMOTING

        if (csRef != NULL)
        {
            

            compressedStack->ProcessCS(pAppDomain, csRef, pFrame);
        }
        else
        {
            compressedStack->ProcessAppDomainTransition(); // just to update domain overrides/assert count at the end of stackwalk
        }


    }

    return;      
}

StackWalkAction NCS_CompressStackCB(CrawlFrame* pCf, void *pData)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    StackCompressData *pCBdata = (StackCompressData*)pData;
    NewCompressedStack* compressedStack = (NewCompressedStack*) pCBdata->compressedStack;

    // First check if the walk has skipped the required frames. The check
    // here is between the address of a local variable (the stack mark) and a
    // pointer to the EIP for a frame (which is actually the pointer to the
    // return address to the function from the previous frame). So we'll
    // actually notice which frame the stack mark was in one frame later. This
    // is fine for our purposes since we're always looking for the frame of the
    // caller of the method that actually created the stack mark. 
    _ASSERTE((pCBdata->stackMark == NULL) || (*pCBdata->stackMark == LookForMyCaller));
    if ((pCBdata->stackMark != NULL) &&
        !pCf->IsInCalleesFrames(pCBdata->stackMark))
        return SWA_CONTINUE;

    Frame *pFrame = pCf->GetFrame();

#ifdef FEATURE_REMOTING
    // Save the CtxTxFrame if this is one
    if (pCBdata->pCtxTxFrame == NULL)
    {
        if (Security::IsContextTransitionFrameWithCS(pFrame))
        {
            pCBdata->pCtxTxFrame = pFrame;
        }
    }
#endif // FEATURE_REMOTING

    //  Handle AppDomain transitions:
    AppDomain *pAppDomain = pCf->GetAppDomain();
    if (pCBdata->prevAppDomain != pAppDomain)
    {
#ifndef FEATURE_REMOTING
        BOOL bRealAppDomainTransition = (pCBdata->prevAppDomain != NULL);

        // For a "real" appdomain transition, we can stop the stackwalk since there's no managed AD transitions
        // without remoting. The "real" here denotes that this is not the first appdomain transition (from NULL to current)
        // that happens on the first crawlframe we see on a stackwalk. Also don't do the final ad transition here (that'll happen 
        // outside the callback)
        if (bRealAppDomainTransition)
        {
            return SWA_ABORT;
        }
        else
#endif // !FEATURE_REMOTING
        {
            compressedStack->ProcessAppDomainTransition();    
            pCBdata->prevAppDomain = pAppDomain;
        }
            
    }


    if (pCf->GetFunction() == NULL)
        return SWA_CONTINUE; // not a function frame, so we were just looking for CtxTransitionFrames. Resume the stackwalk...
        
    // Get the security object for this function...
    OBJECTREF* pRefSecDesc = pCf->GetAddrOfSecurityObject();

    MethodDesc * pFunc = pCf->GetFunction();

    _ASSERTE(pFunc != NULL); // we requested methods!

    Assembly * pAssem = pCf->GetAssembly();
    _ASSERTE(pAssem != NULL);
    PREFIX_ASSUME(pAssem != NULL);



    
    if (pRefSecDesc != NULL)
        SecurityDeclarative::DoDeclarativeSecurityAtStackWalk(pFunc, pAppDomain, pRefSecDesc);

    
    
    if (pFunc->GetMethodTable()->IsDelegate())
    {
        DelegateEEClass* delegateCls = (DelegateEEClass*) pFunc->GetMethodTable()->GetClass();
        if (pFunc == delegateCls->m_pBeginInvokeMethod)
        {
            // Async delegate case: we may need to insert the creator frame into the CS 
            DELEGATEREF dRef = (DELEGATEREF) ((FramedMethodFrame *)pFrame)->GetThis();
            _ASSERTE(dRef);
            if (COMDelegate::IsSecureDelegate(dRef))
            {
                if (!dRef->IsWrapperDelegate())
                {
                    MethodDesc* pCreatorMethod = (MethodDesc*) dRef->GetMethodPtrAux();
                    Assembly* pCreatorAssembly = pCreatorMethod->GetAssembly();
                    compressedStack->ProcessFrame(pAppDomain,
                                                  NULL,
                                                  NULL,
                                                  pCreatorAssembly->GetSharedSecurityDescriptor(), 
                                                  NULL) ; // ignore return value - No FSD being passed in.
                }
            }
            
        }
    }
        

    DWORD retFlags = compressedStack->ProcessFrame(pAppDomain, 
                                                   pAssem,
                                                   pFunc,
                                                   pAssem->GetSharedSecurityDescriptor(), 
                                                   (FRAMESECDESCREF *) pRefSecDesc) ;
    
    pCBdata->dwFlags |= (retFlags & CORSEC_FT_ASSERT);
    // ProcessFrame returns TRUE if we should stop stackwalking
   if (retFlags != 0 || Security::IsSpecialRunFrame(pFunc))
        return SWA_ABORT;
   
    return SWA_CONTINUE;

}
#endif // #ifdef FEATURE_COMPRESSEDSTACK
