// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Implementation: SynchronizationContextNative.cpp
**
**
** Purpose: Native methods on System.Threading.SynchronizationContext.
**
**
===========================================================*/

#include "common.h"

#ifdef FEATURE_APPX
#include <roapi.h>
#include <windows.ui.core.h>
#endif
#include "synchronizationcontextnative.h"

FCIMPL3(DWORD, SynchronizationContextNative::WaitHelper, PTRArray *handleArrayUNSAFE, CLR_BOOL waitAll, DWORD millis)
{
    FCALL_CONTRACT;

    DWORD ret = 0;

    PTRARRAYREF handleArrayObj = (PTRARRAYREF) handleArrayUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(handleArrayObj);

    CQuickArray<HANDLE> qbHandles;
    int cHandles = handleArrayObj->GetNumComponents();

    // Since DoAppropriateWait could cause a GC, we need to copy the handles to an unmanaged block
    // of memory to ensure they aren't relocated during the call to DoAppropriateWait.
    qbHandles.AllocThrows(cHandles);
    memcpy(qbHandles.Ptr(), handleArrayObj->GetDataPtr(), cHandles * sizeof(HANDLE));

    Thread * pThread = GetThread();
    ret = pThread->DoAppropriateWait(cHandles, qbHandles.Ptr(), waitAll, millis, 
                                     (WaitMode)(WaitMode_Alertable | WaitMode_IgnoreSyncCtx));
    
    HELPER_METHOD_FRAME_END();
    return ret;
}
FCIMPLEND
    
#ifdef FEATURE_APPX

Volatile<ABI::Windows::UI::Core::ICoreWindowStatic*> g_pICoreWindowStatic;

void* QCALLTYPE SynchronizationContextNative::GetWinRTDispatcherForCurrentThread()
{
    QCALL_CONTRACT;
    void* result = NULL;
    BEGIN_QCALL;

    _ASSERTE(WinRTSupported());

    //
    // Get access to ICoreWindow's statics.  We grab just one ICoreWindowStatic for the whole process.
    //
    ABI::Windows::UI::Core::ICoreWindowStatic* pICoreWindowStatic = g_pICoreWindowStatic;
    if (pICoreWindowStatic == NULL)
    {
        SafeComHolderPreemp<ABI::Windows::UI::Core::ICoreWindowStatic> pNewICoreWindowStatic;
        {
            HRESULT hr = clr::winrt::GetActivationFactory(RuntimeClass_Windows_UI_Core_CoreWindow, (ABI::Windows::UI::Core::ICoreWindowStatic**)pNewICoreWindowStatic.GetAddr());

            //
            // Older Windows builds don't support ICoreWindowStatic.  We should just return a null CoreDispatcher 
            // in that case, rather than throwing.
            //
            if (hr != E_NOTIMPL)
                IfFailThrow(hr);
        }

        if (pNewICoreWindowStatic != NULL)
        {
            ABI::Windows::UI::Core::ICoreWindowStatic* old = InterlockedCompareExchangeT<ABI::Windows::UI::Core::ICoreWindowStatic*>(&g_pICoreWindowStatic, pNewICoreWindowStatic, NULL);
            if (old == NULL)
            {
                pNewICoreWindowStatic.SuppressRelease();
                pICoreWindowStatic = pNewICoreWindowStatic;
            }
            else
            {
                pICoreWindowStatic = old;
            }
        }
    }


    if (pICoreWindowStatic != NULL)
    {
        //
        // Get the current ICoreWindow
        //
        SafeComHolderPreemp<ABI::Windows::UI::Core::ICoreWindow> pCoreWindow;

        //
        // workaround: we're currently ignoring the HRESULT from get_Current, because Windows is returning errors for threads that have no CoreWindow.
        // A better behavior would be to return S_OK, with a NULL CoreWindow.  If/when Windows does the right thing here, we can change this
        // back to checking the HRESULT.
        //
        pICoreWindowStatic->GetForCurrentThread(&pCoreWindow);

        if (pCoreWindow != NULL)
        {
            //
            // Get the ICoreDispatcher for this window
            //
            SafeComHolderPreemp<ABI::Windows::UI::Core::ICoreDispatcher> pCoreDispatcher;
            IfFailThrow(pCoreWindow->get_Dispatcher(&pCoreDispatcher));

            if (pCoreDispatcher != NULL)
            {
                //
                // Does the dispatcher belong to the current thread?
                //
                boolean hasThreadAccess = FALSE;
                IfFailThrow(pCoreDispatcher->get_HasThreadAccess(&hasThreadAccess));
                if (hasThreadAccess)
                {
                    //
                    // This is the dispatcher for the current thread.  Return it.
                    //
                    pCoreDispatcher.SuppressRelease();
                    result = (void*)pCoreDispatcher;
                }
            }
        }
    }

    END_QCALL;
    return result;
}

void SynchronizationContextNative::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;
    
    if (g_pICoreWindowStatic)
    {
        SafeRelease(g_pICoreWindowStatic);
        g_pICoreWindowStatic = NULL;
    }
}



#endif //FEATURE_APPX
