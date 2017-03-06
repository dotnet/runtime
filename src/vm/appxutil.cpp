// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//
// Provides VM-specific AppX utility code.

#include "common.h"

#include "utilcode.h"
#include "holder.h"
#include "volatile.h"
#include "appxutil.h"
#include "ex.h"

#include "Windows.ApplicationModel.h"
#include "Windows.ApplicationModel.Core.h"

namespace AppX
{
    //-----------------------------------------------------------------------------------
    // This is a small helper class designed to ensure that the current thread is
    // RoInitialized for the lifetime of the holder. Use this holder only if code does
    // not store any WinRT interfaces in locations that will out-live the holder
    // itself.

    class RoInitializeHolder
    {
    public:
        enum ThreadingModel
        {
            MultiThreaded,              // Require multi-threaded model
            SingleThreaded,             // Require single-threaded model
            AnyThreadedMultiPreferred   // Any threading model is ok;
                                        // prefer multi-threaded model
        };

        RoInitializeHolder(
            ThreadingModel threadingModel)  // desired/preferred apartment model
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END

            HRESULT hr = S_OK;

            {
                GCX_PREEMP();

                // Prefer MultiThreaded when AnyThreadedMultiPreferred is specified.
                hr = ::RoInitialize((threadingModel == SingleThreaded) ? RO_INIT_SINGLETHREADED
                                                                       : RO_INIT_MULTITHREADED);
            }

            // Success means that the thread's RoInitialize ref count has been incremented,
            // and must be paired with a call to RoUnintialize.
            _uninitRequired = SUCCEEDED(hr);

            if (FAILED(hr))
            {
                // Throw if:
                //    1. RoInitialize failed for any reason other than RPC_E_CHANGED_MODE
                //    2. RoInitialize failed with RPC_E_CHANGED_MODE and caller will not
                //       accept a different apartment model.
                if (hr != RPC_E_CHANGED_MODE || threadingModel != AnyThreadedMultiPreferred)
                {
                    // Note: throwing here will cause us to skip the dtor, but will only
                    // do so when SUCCEEDED(hr) is FALSE, which means that _uninitRequired
                    // is also FALSE so there is no RoInitialize refcount leak here.
                    _ASSERTE(!_uninitRequired);

                    ThrowHR(hr);
                }
            }
        }

        // Ensures RoUninitialize is called (if needed) before holder falls out of scope.
        ~RoInitializeHolder()
        {
            LIMITED_METHOD_CONTRACT;
            if (_uninitRequired)
            {
                _uninitRequired = false;
                ::RoUninitialize();
            }
        }

    private:
        bool _uninitRequired; // Is a call to RoUnitialize required?
    };

    //-----------------------------------------------------------------------------------

    HRESULT IsAppXDesignModeWorker(bool * pfResult)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END

        HRESULT hr = S_OK;

        boolean fDesignModeEnabled = false;

        // Delayloaded entrypoint may throw.
        EX_TRY
        {
            // Ensure that thread is initialized for WinRT; either apt model will work for this API.
            RoInitializeHolder hRoInit(RoInitializeHolder::AnyThreadedMultiPreferred);

            ReleaseHolder<ABI::Windows::ApplicationModel::IDesignModeStatics> pIDesignMode;
            IfFailThrow(clr::winrt::GetActivationFactory(
                RuntimeClass_Windows_ApplicationModel_DesignMode, pIDesignMode));

            IfFailThrow(pIDesignMode->get_DesignModeEnabled(&fDesignModeEnabled));
        }
        EX_CATCH_HRESULT(hr)
        IfFailRet(hr);

        if (!!fDesignModeEnabled)
        {
            *pfResult = true;
            return S_OK;
        }

        *pfResult = false;
        return S_OK;
    }

    //-----------------------------------------------------------------------------------
    // Returns true if running in an AppX process with DevMode enabled.

    bool IsAppXDesignMode()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END

        // CoreCLR does not have proper support for AppX design mode. Once/if it has one, it should not need
        // any special casing like desktop. Avoid the expensive check completely.
        return false;
    }

    HRESULT GetApplicationId(LPCWSTR& rString)
    {
        LIMITED_METHOD_CONTRACT;

        // the PRAID is a static value for the life of the process.  the reason for caching is
        // because the watson bucketing code requires this value during unhandled exception
        // processing and due to the contracts in that code it cannot tolerate the switch to
        // preemptive mode when calling out to WinRT.
        static LPCWSTR s_wzPraid = nullptr;

        HRESULT hr = S_OK;
        
        if (s_wzPraid == nullptr)
        {
            ReleaseHolder<ABI::Windows::ApplicationModel::Core::ICoreApplication> coreApp;

            hr = clr::winrt::GetActivationFactory(RuntimeClass_Windows_ApplicationModel_Core_CoreApplication, coreApp);

            if (SUCCEEDED(hr))
            {
                WinRtString winrtAppId;
                hr = coreApp->get_Id(winrtAppId.Address());
                
                if (SUCCEEDED(hr))
                {
                    LPCWSTR wzPraid = DuplicateString(winrtAppId.GetRawBuffer(), winrtAppId.size());
                    if (wzPraid)
                    {
                        if (InterlockedCompareExchangeT(&s_wzPraid, wzPraid, nullptr) != nullptr)
                            delete[] wzPraid;
                    }
                    else
                    {
                        hr = E_OUTOFMEMORY;
                    }
                }
            }
        }
        
        rString = s_wzPraid;

        return hr;
    }
    

}


