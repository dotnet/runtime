// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
** Header: winrtdispatcherqueue.h
**
===========================================================*/

#ifndef _WINRTDISPATCHERQUEUE_H
#define _WINRTDISPATCHERQUEUE_H

#include <inspectable.h>

// The following definitions were taken from windows.system.h.
// Use windows.system.h from the RS3 SDK instead of this when that SDK is available.
namespace Windows {
    namespace System {
        /* [v1_enum] */
        enum DispatcherQueuePriority
        {
            DispatcherQueuePriority_Low = -10,
            DispatcherQueuePriority_Normal  = 0,
            DispatcherQueuePriority_High    = 10
        } ;

        MIDL_INTERFACE("DFA2DC9C-1A2D-4917-98F2-939AF1D6E0C8")
        IDispatcherQueueHandler : public IUnknown
        {
        public:
            virtual HRESULT STDMETHODCALLTYPE Invoke( void) = 0;
        };

        MIDL_INTERFACE("5FEABB1D-A31C-4727-B1AC-37454649D56A")
        IDispatcherQueueTimer : public IInspectable
        {
        public:
            virtual HRESULT STDMETHODCALLTYPE Start( void) = 0;

            virtual HRESULT STDMETHODCALLTYPE Stop( void) = 0;

            virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Interval(
                /* [out][retval] */ __RPC__out ABI::Windows::Foundation::TimeSpan *value) = 0;

            virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Interval(
                /* [in] */ ABI::Windows::Foundation::TimeSpan value) = 0;

            virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsStarted(
                /* [out][retval] */ __RPC__out boolean *value) = 0;

            virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsRepeating(
                /* [out][retval] */ __RPC__out boolean *value) = 0;

            virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IsRepeating(
                /* [in] */ boolean value) = 0;

#if 0 // We don't use these functions
            virtual HRESULT STDMETHODCALLTYPE add_Tick(
                /* [in] */ __RPC__in_opt __FITypedEventHandler_2_Windows__CSystem__CDispatcherQueueTimer_IInspectable *handler,
                /* [out][retval] */ __RPC__out EventRegistrationToken *token) = 0;

            virtual HRESULT STDMETHODCALLTYPE remove_Tick(
                /* [in] */ EventRegistrationToken token) = 0;
#endif
        };

        MIDL_INTERFACE("603E88E4-A338-4FFE-A457-A5CFB9CEB899")
        IDispatcherQueue : public IInspectable
        {
        public:
            virtual HRESULT STDMETHODCALLTYPE CreateTimer(
                /* [out][retval] */ __RPC__deref_out_opt Windows::System::IDispatcherQueueTimer **result) = 0;

            virtual HRESULT STDMETHODCALLTYPE TryEnqueue(
                /* [in] */ __RPC__in_opt Windows::System::IDispatcherQueueHandler *callback,
                /* [out][retval] */ __RPC__out boolean *result) = 0;

            virtual HRESULT STDMETHODCALLTYPE TryEnqueueWithPriority(
                /* [in] */ Windows::System::DispatcherQueuePriority priority,
                /* [in] */ __RPC__in_opt Windows::System::IDispatcherQueueHandler *callback,
                /* [out][retval] */ __RPC__out boolean *result) = 0;
        };

        MIDL_INTERFACE("A96D83D7-9371-4517-9245-D0824AC12C74")
        IDispatcherQueueStatics : public IInspectable
        {
        public:
            virtual HRESULT STDMETHODCALLTYPE GetForCurrentThread(
                /* [out][retval] */ __RPC__deref_out_opt Windows::System::IDispatcherQueue **result) = 0;
        };

        extern const __declspec(selectany) IID & IID_IDispatcherQueueStatics = __uuidof(IDispatcherQueueStatics);
    }
}

#ifndef RUNTIMECLASS_Windows_System_DispatcherQueue_DEFINED
#define RUNTIMECLASS_Windows_System_DispatcherQueue_DEFINED
    extern const __declspec(selectany) _Null_terminated_ WCHAR RuntimeClass_Windows_System_DispatcherQueue[] = L"Windows.System.DispatcherQueue";
#endif

#endif // _WINRTDISPATCHERQUEUE_H
