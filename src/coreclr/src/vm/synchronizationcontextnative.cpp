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
#include "winrtdispatcherqueue.h"
#include "synchronizationcontextnative.h"

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

    // If we didn't find a CoreDispatcher for the thread, let's see if we can get a DispatcherQueue.
    if (result == NULL)
    {
        SafeComHolderPreemp<Windows::System::IDispatcherQueueStatics> pDispatcherQueueStatics;
        {
            HRESULT hr = clr::winrt::GetActivationFactory(RuntimeClass_Windows_System_DispatcherQueue,
                                                         (Windows::System::IDispatcherQueueStatics**)pDispatcherQueueStatics.GetAddr());

            // This interface was added in RS3 along with the public DispatcherQueue support. Older
            // Windows builds don't support it and will return one of two HRESULTs from the call
            // to GetActivationFactory above:
            //    - Pre-RS2 will return REGDB_E_CLASSNOTREG since Windows.System.DispatcherQueue
            //      does not exist at all.
            //    - RS2 will return E_NOINTERFACE since Windows.System.DispatcherQueue does exist
            //      in a limited fashion, but does not support the interface ID that we want.
            //
            // We should just return null if we see these two HRESULTs rather than throwing.
            if (hr != REGDB_E_CLASSNOTREG && hr != E_NOINTERFACE)
            {
                IfFailThrow(hr);
            }
        }

        if (pDispatcherQueueStatics != NULL)
        {
            //
            // Get the current IDispatcherQueue
            //
            SafeComHolderPreemp<Windows::System::IDispatcherQueue> pDispatcherQueue;

            pDispatcherQueueStatics->GetForCurrentThread(&pDispatcherQueue);

            if (pDispatcherQueue != NULL)
            {
                pDispatcherQueue.SuppressRelease();
                result = (void*)pDispatcherQueue;
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
