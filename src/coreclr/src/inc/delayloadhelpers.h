// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains convenience functionality for lazily loading modules
// and getting entrypoints within them.
// 

#ifndef DelayLoadHelpers_h
#define DelayLoadHelpers_h

#include "volatile.h"

namespace DelayLoad
{
    //=================================================================================================================
    // Contains information needed to load and cache a module. Use through
    // the DELAY_LOADED_MODULE macro defined below.
    struct Module
    {
        LPCWSTR const   m_wzDllName;
        HMODULE         m_hMod;
        HRESULT         m_hr;
        Volatile<bool>  m_fInitialized;

        // Returns a non-ref-counted HMODULE; will load the module if necessary.
        // Do not FreeLibrary the returned value.
        HRESULT GetValue(HMODULE *pHMODULE);
    };
}

//=====================================================================================================================
// Use at global scope to declare a delay loaded module represented as a
// DelayLoad::Module instance. The module may then be accessed as
// 'DelayLoad::Modules::DLL_NAME'.
//
// Parameters:
//      DLL_NAME - the simple name (without extension) of the DLL.
//
// Example:
//      DELAY_LOADED_MODULE(Kernel32);
//      void Foo() {
//          HMODULE hModKernel32 = nullptr;
//          IfFailThrow(DelayLoad::Modules::Kernel32.GetValue(&hModKernel32));
//          // Use hModKernel32 as needed. Do not FreeLibrary the value!
//      }

#define DELAY_LOADED_MODULE(DLL_NAME) \
    namespace DelayLoad { \
        namespace Modules { \
            SELECTANY Module DLL_NAME = { L#DLL_NAME W(".dll"), nullptr, S_OK, false }; \
        } \
    }

namespace DelayLoad
{
    //=================================================================================================================
    // Contains information needed to load a function pointer from a DLL. Builds
    // on the DelayLoad::Module functionality, and should be used through
    // the DELAY_LOADED_FUNCTION macro defined below.
    struct Function
    {
        Module * const  m_pModule;
        LPCSTR const    m_szFunctionName;
        PVOID           m_pvFunction;
        HRESULT         m_hr;
        Volatile<bool>  m_fInitialized;

        // On success, ppvFunc is set to point to the entrypoint corresponding to
        // m_szFunctionName as exported from m_pModule.
        HRESULT GetValue(LPVOID * ppvFunc);

        // Convenience function that does the necessary casting for you.
        template <typename FnT> inline
        HRESULT GetValue(FnT ** ppFunc)
        {
            return GetValue(reinterpret_cast<LPVOID*>(ppFunc));
        }
    };
}

//=====================================================================================================================
// Use at global scope to declare a delay loaded function and its associated module,
// represented as DelayLoad::Function and DelayLoad::Module instances, respectively.
// The function may then be accessed as 'DelayLoad::DLL_NAME::FUNC_NAME', and the
// module may be access as described in DELAY_LOADED_MODULE's comment.
//
// Parameters:
//      DLL_NAME  - unquoted simple name (without extension) of the DLL containing
//                  the function.
//      FUNC_NAME - unquoted entrypoint name exported from the DLL.
//
// Example:
//      DELAY_LOADED_FUNCTION(MyDll, MyFunction);
//      HRESULT Foo(...) {
//          typedef HRESULT MyFunction_t(<args>);
//          MyFunction_t * pFunc = nullptr;
//          IfFailRet(DelayLoad::WinTypes::RoResolveNamespace.GetValue(&pFunc));
//          return (*pFunc)(...);
//      }

#define DELAY_LOADED_FUNCTION(DLL_NAME, FUNC_NAME) \
    DELAY_LOADED_MODULE(DLL_NAME) \
    namespace DelayLoad { \
        namespace DLL_NAME { \
            SELECTANY Function FUNC_NAME = { &Modules::##DLL_NAME, #FUNC_NAME, nullptr, S_OK, false }; \
        } \
    }

#endif // DelayLoadHelpers_h

