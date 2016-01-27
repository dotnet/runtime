// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// LegacyActivationShim.h
//
// This file allows simple migration from .NET Runtime v2 Host Activation APIs
// to the .NET Runtime v4 Host Activation APIs through simple shim functions.

#ifndef __LEGACYACTIVATIONSHIMUTIL_H__
#define __LEGACYACTIVATIONSHIMUTIL_H__

// To minimize how much we perturb sources that we are included in, we make sure that
// all macros we define/redefine are restored at the end of the header.
#pragma push_macro("SELECTANY")
#pragma push_macro("_TEXT_ENCODE")
#pragma push_macro("countof")
#pragma push_macro("UNUSED")

#ifndef _MSC_VER
#error "LegacyActivationShim.h cannot be used on non-MS compilers"
#endif

// ---SELECTANY------------------------------------------------------------------------------------
#undef SELECTANY
#define SELECTANY extern __declspec(selectany)

// Allow users of these headers to provide custom 'LoadLibrary' implementation (e.g. WszLoadLibrary).
// Example of usage is in ndp\clr\src\fusion\tools\viewer.
#ifndef LEGACY_ACTIVATION_SHIM_LOAD_LIBRARY
#define LEGACY_ACTIVATION_SHIM_LOAD_LIBRARY ::LoadLibrary
#endif

// _T macro alternative to make strings ASCII/UNICODE
#undef _TEXT_ENCODE
#ifdef UNICODE
#define _TEXT_ENCODE(str) L ## str
#else //!UNICODE
#define _TEXT_ENCODE(str) str
#endif //!UNICODE

// countof ... number of items in an array
#ifndef countof
#define countof(x) (sizeof(x) / sizeof(x[0]))
#endif countof

#ifndef UNUSED
#define UNUSED(var) ((void)(var))
#endif //UNUSED

#ifndef __LEGACYACTIVATIONSHIM_H__
    #error Error: Include LegacyActivationShim.h or LegacyActivationShimDelayLoad.h instead of directly including LegacyActivationShimUtil.h
#endif // __LEGACYACTIVATIONSHIM_H__

// ---PLACEMENT NEW--------------------------------------------------------------------------------
#ifndef __PLACEMENT_NEW_INLINE
#define __PLACEMENT_NEW_INLINE
// Inline placement new
inline void* operator new(size_t, void *_Where)
{	// construct array with placement at _Where
    return (_Where);
}

// delete if placement new fails
inline void operator delete(void *, void *)
{
}
#endif __PLACEMENT_NEW_INLINE

// ---LEGACYACTIVATON NAMESPACE--------------------------------------------------------------------
namespace LegacyActivationShim
{
    // ---UTIL NAMESPACE---------------------------------------------------------------------------
    namespace Util
    {
        // ---INTERLOCKEDCOMPAREEXCHANGEPOINTERT---------------------------------------------------
        // Variation on InterlockedCompareExchangePointer that adds some type safety.
        // Added 'T' to end of name because an identical name to the original function
        // confuses GCC
        template <typename T>
        inline
        T InterlockedCompareExchangePointerT(
            T volatile* destination,
            T exchange,
            T comparand)
        {
#ifdef __UtilCode_h__
            // Utilcode has redefined InterlockedCompareExchangePointer
            return ::InterlockedCompareExchangeT(destination, exchange, comparand);
#else // __UtilCode_h__
            return reinterpret_cast<T>(InterlockedCompareExchangePointer(
                (PVOID volatile *)(destination),
                (PVOID)(exchange),
                (PVOID)(comparand)));
#endif // __UtilCode_h__ else
        }

        // ---PlacementNewDeleteHelper-------------------------------------------------------------
        template <typename TYPE, bool IS_CLASS>
        class PlacementNewDeleteHelper;

        template <typename TYPE>
        class PlacementNewDeleteHelper<TYPE, true>
        {
        public:
            // Some environments #define New and Delete, so name these functions
            // Construct and Destruct to keep them unique.
            static void Construct(TYPE const & value, void *pvWhere)
                { new (pvWhere) TYPE(value); }

            static void Destruct(TYPE & value)
                { value.~TYPE(); }
        };

        template <typename TYPE>
        class PlacementNewDeleteHelper<TYPE, false>
        {
        public:
            static void Construct(TYPE const & value, void *pvWhere)
                { *reinterpret_cast<TYPE *>(pvWhere) = value; }

            static void Destruct(TYPE &)
                { }
        };

        // ---HOLDERBASE---------------------------------------------------------------------------
        template <typename TYPE>
        class HolderBase
        {
        public:
            // Relies on implicit default constructor, which permits zero-init static
            // object declaration. Do not define one.
            // HolderBase() {}

        protected:
            char    m_value[sizeof(TYPE)];

            inline
            TYPE & GetRef()
                { return *reinterpret_cast<TYPE *>(&m_value[0]); }

            inline
            TYPE & GetPtr()
                { return reinterpret_cast<TYPE *>(&m_value[0]); }

            inline
            void Construct(TYPE const & value)
                { PlacementNewDeleteHelper<TYPE, __is_class(TYPE)>::Construct(value, (void *)&m_value[0]); }

            inline
            void Destruct()
                { PlacementNewDeleteHelper<TYPE, __is_class(TYPE)>::Destruct(GetRef()); }
        };

        // ---HOLDER-------------------------------------------------------------------------------
        template <typename TYPE, void (*ASSIGNF)(TYPE &), void (*RELEASEF)(TYPE &)>
        class Holder : public HolderBase<TYPE>
        {
        protected:
            bool    m_assigned;
            bool    m_suppressed;

        public:
            inline
            Holder() : m_assigned(false), m_suppressed(false)
                {}

            inline
            Holder(TYPE const & value) : m_assigned(false), m_suppressed(false)
                { Assign(value); }

            inline
            ~Holder()
                { Release(); }

            inline
            void Assign(TYPE const & value)
            {
                Release();
                Construct(value);
                m_assigned = true;
                (*ASSIGNF)(GetValue());
            }

            inline
            void Release()
            {
                if (m_assigned)
                {
                    if (!m_suppressed)
                    {
                        (*RELEASEF)(GetValue());
                    }
                    m_assigned = false;
                    m_suppressed = false;
                    Destruct();
                }
            }

            inline
            void SuppressRelease()
            {
                m_suppressed = m_assigned;
            }

            inline
            TYPE & GetValue()
            {
                // _ASSERTE(m_assigned);
                return GetRef();
            }

            inline
            bool IsAssigned()
                { return m_assigned; }
        };

        // ---ZEROINITGLOBALHOLDER-----------------------------------------------------------------
        // This class should ONLY be used for global (file scope) variables. It relies on zero
        // initialized data in the image. This will fail miserably for other scenarios, as the
        // memory used for the object may not be zero-initialized, which will result in incorrect
        // behaviour.
        template <typename TYPE, void (*ASSIGNF)(TYPE &), void (*RELEASEF)(TYPE &)>
        class ZeroInitGlobalHolder : public HolderBase<TYPE>
        {
        protected:
            bool    m_assigned;

        public:
            // Relies on implicit default constructor, which permits zero-init static
            // field declaration. Do not define an explicit constructor.
            // ZeroInitGlobalHolder() {}

            inline
            ~ZeroInitGlobalHolder()
                { Release(); }

            inline
            void Assign(TYPE const & value)
            {
                Release();
                Construct(value);
                m_assigned = true;
                (*ASSIGNF)(GetValue());
            }

            inline
            void Release()
            {
                if (m_assigned)
                {
                    (*RELEASEF)(GetValue());
                    m_assigned = false;
                    Destruct();
                }
            }

            inline
            TYPE & GetValue()
            {
                // _ASSERTE(m_assigned);
                return GetRef();
            }

            inline
            bool IsAssigned()
            { return m_assigned; }

            inline
            void ClearUnsafe()
            { m_assigned = false; }
        };

        // ---DONOTHINGHELPER----------------------------------------------------------------------
        template <typename TYPE>
        inline
        void DoNothingHelper(TYPE & value)
            { UNUSED(value); }

        // ---RELEASEHELPER------------------------------------------------------------------------
        template <typename TYPE>
        inline
        void ReleaseHelper(TYPE & value)
            { value->Release(); }

        // ---RELEASEHOLDER------------------------------------------------------------------------
        template <typename TYPE>
        class ReleaseHolder
            : public Holder< TYPE, &DoNothingHelper<TYPE>, &ReleaseHelper<TYPE> >
        {
          public:
            inline
            ReleaseHolder(TYPE & value)
                : Holder< TYPE, &DoNothingHelper<TYPE>, &ReleaseHelper<TYPE> >(value)
                {}

            ReleaseHolder()
                : Holder< TYPE, &DoNothingHelper<TYPE>, &ReleaseHelper<TYPE> >()
            {}
        };

        // ---ZEROINITGLOBALRELEASEHOLDER----------------------------------------------------------
        template <typename TYPE>
        class ZeroInitGlobalReleaseHolder
            : public ZeroInitGlobalHolder< TYPE, &DoNothingHelper<TYPE>, &ReleaseHelper<TYPE> >
        {
        };

        // ---FREELIBRARYHELPER--------------------------------------------------------------------
        inline
        void FreeLibraryHelper(HMODULE & hMod)
        {
            FreeLibrary(hMod);
        }

        // ---HMODULEHOLDER------------------------------------------------------------------------
        class HMODULEHolder
            : public Holder< HMODULE, &DoNothingHelper<HMODULE>, &FreeLibraryHelper >
        {
          public:
            inline
            HMODULEHolder(HMODULE value)
                : Holder< HMODULE, &DoNothingHelper<HMODULE>, &FreeLibraryHelper >(value)
                {}

            HMODULEHolder()
                : Holder< HMODULE, &DoNothingHelper<HMODULE>, &FreeLibraryHelper >()
                {}
        };

        // ---ZEROINITHMODULEHOLDER----------------------------------------------------------------
        class ZeroInitGlobalHMODULEHolder
            : public ZeroInitGlobalHolder< HMODULE, &DoNothingHelper<HMODULE>, &FreeLibraryHelper >
        {
        };

        // ---DELAYLOADFUNCTOR---------------------------------------------------------------------
        // T must be a function typedef.
        // For example, "typedef int X(short i); DelayLoadFunctor<X> pfnX;"
        template <typename T>
        class DelayLoadFunctor
        {
        private:
            HMODULEHolder m_hModHolder;
            T * m_proc;

        public:
            HRESULT Init(LPCTSTR wzDllName, LPCSTR szProcName)
            {
                // Load module
                HMODULE hMod = LEGACY_ACTIVATION_SHIM_LOAD_LIBRARY(wzDllName);
                if (hMod == NULL)
                    return HRESULT_FROM_WIN32(::GetLastError());
                HMODULEHolder hModHolder(hMod);

                // Load proc address
                T * proc = reinterpret_cast<T *>(::GetProcAddress(hMod, szProcName));
                if (proc == NULL)
                    return HRESULT_FROM_WIN32(::GetLastError());

                // Store results
                hModHolder.SuppressRelease();
                m_hModHolder.Assign(hMod);
                m_proc = proc;

                return S_OK;
            }

            HRESULT Init(HMODULE hMod, LPCSTR szProcName)
            {
                // Load proc address
                T * proc = reinterpret_cast<T *>(::GetProcAddress(hMod, szProcName));
                if (proc == NULL)
                    return HRESULT_FROM_WIN32(::GetLastError());

                // Store result
                m_proc = proc;

                // Success
                return S_OK;
            }

            T& operator()()
            {
                return *m_proc;
            }
        };

        // ---ZEROINITGLOBALSPINLOCK----------------------------------------------------------------
        class ZeroInitGlobalSpinLock
        {
        private:
            enum LOCK_STATE
            {
                UNLOCKED = 0,
                LOCKED = 1
            };

            LONG volatile m_lock;

            static inline void Lock(ZeroInitGlobalSpinLock*& lock)
            {
                while (InterlockedExchange(&lock->m_lock, LOCKED) == LOCKED)
                {
                    ::SwitchToThread();
                }
            }

            static inline void Unlock(ZeroInitGlobalSpinLock*& lock)
                { InterlockedExchange(&lock->m_lock, UNLOCKED); }

        public:
            typedef LegacyActivationShim::Util::Holder<ZeroInitGlobalSpinLock*,
                     &ZeroInitGlobalSpinLock::Lock,
                     &ZeroInitGlobalSpinLock::Unlock>
                Holder;
        };

        // ---MSCOREEDATA--------------------------------------------------------------------------
        SELECTANY HMODULE                     g_hModMscoree = NULL;
        SELECTANY ZeroInitGlobalHMODULEHolder g_hModMscoreeHolder;

        // ---GETMSCOREE---------------------------------------------------------------------------
        inline
        HRESULT GetMSCOREE(HMODULE *pMscoree)
        {
            if (g_hModMscoree == NULL)
            {
                HMODULE hModMscoree = LEGACY_ACTIVATION_SHIM_LOAD_LIBRARY(_TEXT_ENCODE("mscoree.dll"));
                if (hModMscoree == NULL)
                    return HRESULT_FROM_WIN32(GetLastError());
                HMODULEHolder hModMscoreeHolder(hModMscoree);

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<HMODULE>(
                    &g_hModMscoree, hModMscoree, NULL) == NULL)
                {
                    g_hModMscoreeHolder.ClearUnsafe();
                    g_hModMscoreeHolder.Assign(g_hModMscoree);
                    hModMscoreeHolder.SuppressRelease();
                }
            }

            *pMscoree = g_hModMscoree;
            return S_OK;
        }

        // ---MSCOREEFUNCTOR-----------------------------------------------------------------------
        template <typename T>
        class MscoreeFunctor : public DelayLoadFunctor<T>
        {
        public:
            HRESULT Init(LPCSTR szProcName)
            {
                HRESULT hr = S_OK;
                HMODULE hModMscoree = NULL;
                IfHrFailRet(GetMSCOREE(&hModMscoree));

                return DelayLoadFunctor<T>::Init(hModMscoree, szProcName);
            }
        };

        // ---CALLCLRCREATEINSTANCE------------------------------------------------------------------
        inline 
        HRESULT CallCLRCreateInstance(
            REFCLSID clsid, 
            REFIID   riid, 
            LPVOID  *ppInterface)
        {
            HRESULT hr = S_OK;
            HMODULE hMscoree = NULL;
            IfHrFailRet(GetMSCOREE(&hMscoree));

            typedef HRESULT (__stdcall *CLRCreateInstance_pfn) (
                REFCLSID clsid, 
                REFIID   riid, 
                LPVOID  *ppInterface);

            CLRCreateInstance_pfn pfnCLRCreateInstance =
               reinterpret_cast<CLRCreateInstance_pfn>(GetProcAddress(hMscoree, "CLRCreateInstance"));

            if (pfnCLRCreateInstance == NULL)
                return HRESULT_FROM_WIN32(GetLastError());

            return (*pfnCLRCreateInstance)(
                clsid, 
                riid, 
                ppInterface);
        }

        // ---CLRMETAHOST INTERFACE DATA-----------------------------------------------------------
        SELECTANY ICLRMetaHost* g_pCLRMetaHost = NULL;
        SELECTANY ZeroInitGlobalReleaseHolder<ICLRMetaHost*> g_hCLRMetaHost;

        // ---GETCLRMETAHOST-----------------------------------------------------------------------
        // NOTE: Does not AddRef returned interface pointer.
        inline
        HRESULT GetCLRMetaHost(
            /*out*/ ICLRMetaHost **ppCLRMetaHost)
        {
            HRESULT hr = S_OK;

            if (g_pCLRMetaHost == NULL)
            {
                ICLRMetaHost *pMetaHost = NULL;
                IfHrFailRet(CallCLRCreateInstance(CLSID_CLRMetaHost,
                                     IID_ICLRMetaHost,
                                     reinterpret_cast<LPVOID *>(&pMetaHost)));
                ReleaseHolder<ICLRMetaHost*> hMetaHost(pMetaHost);

                //
                // Great - we got an ICLRMetaHost. Now publish this to
                // g_pCLRMetaHost in a thread-safe way.
                //

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<ICLRMetaHost *>(
                        &g_pCLRMetaHost, pMetaHost, NULL) == NULL)
                {
                    // Successful publish. In this case, we also assign to the
                    // holder to ensure that the interface is released when the
                    // image is unloaded.
                    g_hCLRMetaHost.ClearUnsafe();
                    g_hCLRMetaHost.Assign(g_pCLRMetaHost);
                    hMetaHost.SuppressRelease(); // Keep it AddRef'ed for the g_hCLRMetaHost
                }
            }

            *ppCLRMetaHost = g_pCLRMetaHost;

            return hr;
        }

        // ---HasNewActivationAPIs-----------------------------------------------------------------
        SELECTANY ULONG g_fHasNewActivationAPIs = ULONG(-1);

        inline
        bool HasNewActivationAPIs()
        {
            if (g_fHasNewActivationAPIs == ULONG(-1))
            {
                ICLRMetaHost *pMetaHost = NULL;
                HRESULT hr = GetCLRMetaHost(&pMetaHost);
                InterlockedCompareExchange((LONG volatile *)&g_fHasNewActivationAPIs, (LONG)(SUCCEEDED(hr)), ULONG(-1));
            }

            return g_fHasNewActivationAPIs != 0;
        }

        // ---CLRMETAHOSTPOLICY INTERFACE DATA-----------------------------------------------------
        SELECTANY ICLRMetaHostPolicy* g_pCLRMetaHostPolicy = NULL;
        SELECTANY ZeroInitGlobalReleaseHolder<ICLRMetaHostPolicy*> g_hCLRMetaHostPolicy;

        // ---GETCLRMETAHOSTPOLICY-----------------------------------------------------------------
        // NOTE: Does not AddRef returned interface pointer.
        inline
        HRESULT GetCLRMetaHostPolicy(
            /*out*/ ICLRMetaHostPolicy **ppICLRMetaHostPolicy)
        {
            HRESULT hr = S_OK;

            if (g_pCLRMetaHostPolicy == NULL)
            {
                ICLRMetaHostPolicy *pMetaHostPolicy = NULL;
                IfHrFailRet(CallCLRCreateInstance(CLSID_CLRMetaHostPolicy,
                                     IID_ICLRMetaHostPolicy,
                                     reinterpret_cast<LPVOID *>(&pMetaHostPolicy)));
                ReleaseHolder<ICLRMetaHostPolicy*> hMetaHostPolicy(pMetaHostPolicy);

                //
                // Great - we got an ICLRMetaHostPolicy. Now publish this to
                // g_pCLRMetaHostPolicy in a thread-safe way.
                //

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<ICLRMetaHostPolicy*>(
                        &g_pCLRMetaHostPolicy, pMetaHostPolicy, NULL) == NULL)
                {
                    // Successful publish. In this case, we also assign to the
                    // holder to ensure that the interface is released when the
                    // image is unloaded.
                    g_hCLRMetaHostPolicy.ClearUnsafe();
                    g_hCLRMetaHostPolicy.Assign(g_pCLRMetaHostPolicy);
                    hMetaHostPolicy.SuppressRelease();
                }
            }

            *ppICLRMetaHostPolicy = g_pCLRMetaHostPolicy;

            return hr;
        }

        // ---RUNTIMEINFO DATA---------------------------------------------------------------------
        struct RuntimeInfo
        {
            ICLRRuntimeInfo *m_pRuntimeInfo;

            DWORD   m_cchImageVersion;
            WCHAR   m_wszImageVersion[512];

            inline
            void Init()
            {
                m_pRuntimeInfo = NULL;
                m_cchImageVersion = countof(m_wszImageVersion);
                m_wszImageVersion[0] = L'\0';
            }

            inline
            void Release()
            {
                if (m_pRuntimeInfo != NULL)
                {
                    m_pRuntimeInfo->Release();
                    m_pRuntimeInfo = NULL;
                }
            }
        };

        SELECTANY LONG                   g_runtimeInfoIsInitialized = FALSE;
        SELECTANY RuntimeInfo            g_runtimeInfo;
        SELECTANY ZeroInitGlobalSpinLock g_runtimeInfoLock;
        SELECTANY ZeroInitGlobalReleaseHolder<RuntimeInfo*> g_hRuntimeInfo;

        // ---GETCLRRUNTIMEINFOHELPER--------------------------------------------------------------
        // Logic:
        //     1. Try to bind using ICLRMetaHostPolicy::GetRequestedRuntime and incoming arguments.
        //     2. Try to bind using ICLRMetaHostPolicy::GetRequestedRuntime and "v4.0.0" and
        //        upgrade policy.
        //     3. Try to bind to latest using GetRequestedRuntimeInfo.

        inline
        HRESULT GetCLRRuntimeInfoHelper(
            /*out*/ ICLRRuntimeInfo **ppCLRRuntimeInfo,
            LPCWSTR  pEXE = NULL,
            IStream *pIStream = NULL,
            __inout_ecount_opt(*pcchVersion) LPWSTR wszVersion = NULL,
            DWORD   *pcchVersion = NULL,
            __out_ecount_opt(*pcchImageVersion) LPWSTR wszImageVersion = NULL,
            DWORD   *pcchImageVersion = NULL)
        {
            HRESULT hr = S_OK;

            //
            // 1. Try policy-based binding first, which will incorporate config files and such.
            //

            ICLRMetaHostPolicy *pMetaHostPolicy = NULL;
            IfHrFailRet(GetCLRMetaHostPolicy(&pMetaHostPolicy));

            DWORD dwConfigFlags = 0;

            hr = pMetaHostPolicy->GetRequestedRuntime(
                METAHOST_POLICY_USE_PROCESS_IMAGE_PATH,
                pEXE,
                pIStream,
                wszVersion,
                pcchVersion,
                wszImageVersion,
                pcchImageVersion,
                &dwConfigFlags,
                IID_ICLRRuntimeInfo,
                reinterpret_cast<LPVOID *>(ppCLRRuntimeInfo));

            if (hr != S_OK &&
                pEXE == NULL &&
                pIStream == NULL &&
                wszVersion == NULL)
            {   //
                // 2. Try to bind using ICLRMetaHostPolicy::GetRequestedRuntime and "v4.0.0" and upgrade policy.
                //

                WCHAR _wszVersion[256];  // We can't use new in this header, so just pick an obscenely long version string length of 256
                DWORD _cchVersion = countof(_wszVersion);
                wcscpy_s(_wszVersion, _cchVersion, L"v4.0.0");
                hr = pMetaHostPolicy->GetRequestedRuntime(
                    static_cast<METAHOST_POLICY_FLAGS>(METAHOST_POLICY_USE_PROCESS_IMAGE_PATH |
                                                       METAHOST_POLICY_APPLY_UPGRADE_POLICY),
                    pEXE,
                    pIStream, // (is NULL)
                    _wszVersion,
                    &_cchVersion,
                    wszImageVersion,
                    pcchImageVersion,
                    &dwConfigFlags,
                    IID_ICLRRuntimeInfo,
                    reinterpret_cast<LPVOID *>(ppCLRRuntimeInfo));
            }

            if (hr != S_OK &&
                pEXE == NULL &&
                pIStream == NULL &&
                wszVersion == NULL)
            {   //
                // 3. Try to bind using GetRequestedRuntimeInfo(NULL)
                //

                typedef HRESULT __stdcall GetRequestedRuntimeInfo_t(
                    LPCWSTR pExe,
                    LPCWSTR pwszVersion,
                    LPCWSTR pConfigurationFile,
                    DWORD startupFlags,
                    DWORD runtimeInfoFlags,
                    LPWSTR pDirectory,
                    DWORD dwDirectory,
                    DWORD *dwDirectoryLength,
                    LPWSTR pVersion,
                    DWORD cchBuffer,
                    DWORD* dwlength);

                HMODULE hMscoree = NULL;
                IfHrFailRet(GetMSCOREE(&hMscoree));

                // We're using GetRequestedRuntimeInfo here because it is the only remaining API
                // that will not be Whidbey-capped and will allow "bind to latest" semantics. This
                // is cheating a bit, but should work for now. The alternative is to use
                // ICLRMetaHost::EnumerateRuntimes to achieve the same result.
                DelayLoadFunctor<GetRequestedRuntimeInfo_t> GetRequestedRuntimeInfoFN;
                IfHrFailRet(GetRequestedRuntimeInfoFN.Init(hMscoree, "GetRequestedRuntimeInfo"));

                WCHAR szDir_[_MAX_PATH];
                DWORD cchDir_ = countof(szDir_);
                WCHAR szVersion_[_MAX_PATH];
                DWORD cchVersion_ = countof(szVersion_);
                DWORD dwInfoFlags_ = RUNTIME_INFO_UPGRADE_VERSION 
                                   | RUNTIME_INFO_DONT_SHOW_ERROR_DIALOG;

                IfHrFailRet(GetRequestedRuntimeInfoFN()(
                    NULL,
                    NULL,
                    NULL,
                    0,
                    dwInfoFlags_,
                    szDir_,
                    cchDir_,
                    &cchDir_,
                    szVersion_,
                    cchVersion_,
                    &cchVersion_));

                // Unable to get a version to try to load.
                if (hr != S_OK)
                {
                    return CLR_E_SHIM_RUNTIMELOAD;
                }

                ICLRMetaHost *pMetaHost = NULL;
                IfHrFailRet(GetCLRMetaHost(&pMetaHost));

                hr = pMetaHost->GetRuntime(szVersion_,
                                           IID_ICLRRuntimeInfo,
                                           reinterpret_cast<LPVOID *>(ppCLRRuntimeInfo));

                if (hr != S_OK)
                {
                    return CLR_E_SHIM_RUNTIMELOAD;
                }

                if (wszImageVersion != NULL)
                {
                    wcsncpy_s(wszImageVersion, *pcchImageVersion, szVersion_, cchVersion_);
                    *pcchImageVersion = cchVersion_;
                }
            }

            if (hr == S_OK &&
                (dwConfigFlags & METAHOST_CONFIG_FLAGS_LEGACY_V2_ACTIVATION_POLICY_MASK) ==
                METAHOST_CONFIG_FLAGS_LEGACY_V2_ACTIVATION_POLICY_TRUE)
            {   // If the config requested that the runtime be bound as the legacy runtime.
                IfHrFailRet((*ppCLRRuntimeInfo)->BindAsLegacyV2Runtime());
            }

            return hr;
        }

        // ---GETRUNTIMEINFO-----------------------------------------------------------------------
        inline
        HRESULT GetRuntimeInfo(
            /*out*/ RuntimeInfo **ppRuntimeInfo,
            LPCWSTR  pEXE = NULL,
            IStream *pIStream = NULL,
            __inout_ecount_opt(*pcchVersion) LPWSTR wszVersion = NULL,
            DWORD   *pcchVersion = NULL)
        {
            HRESULT hr = S_OK;

            if (!g_runtimeInfoIsInitialized)
            {
                ZeroInitGlobalSpinLock::Holder lock(&g_runtimeInfoLock);
                if (!g_runtimeInfoIsInitialized)
                {
                    g_runtimeInfo.Init();

                    IfHrFailRet(GetCLRRuntimeInfoHelper(
                        &g_runtimeInfo.m_pRuntimeInfo,
                        pEXE,
                        pIStream,
                        wszVersion,
                        pcchVersion,
                        g_runtimeInfo.m_wszImageVersion,
                        &g_runtimeInfo.m_cchImageVersion));
                        
                    //
                    // Initialized - now publish.
                    //

                    g_hRuntimeInfo.ClearUnsafe();
                    g_hRuntimeInfo.Assign(&g_runtimeInfo);
                    InterlockedExchange(&g_runtimeInfoIsInitialized, TRUE);
                }
            }

            //
            // Return the struct
            //

            *ppRuntimeInfo = &g_runtimeInfo;
            return hr;
        }

        // --------BINDTOV4------------------------------------------------------------------------
        // Used by hosted DLLs that require the use of v4 for all their
        // LegacyActivationShim calls. Can (and should) be called from DllMain,
        // provided the DLL has a static (non-delayload) dependency on mscoree.dll.
        inline
        HRESULT BindToV4()
        {
            HRESULT hr = E_FAIL;

            if (!g_runtimeInfoIsInitialized)
            {
                ZeroInitGlobalSpinLock::Holder lock(&g_runtimeInfoLock);
                if (!g_runtimeInfoIsInitialized)
                {
                    ICLRMetaHostPolicy *pMetaHostPolicy = NULL;
                    IfHrFailRet(GetCLRMetaHostPolicy(&pMetaHostPolicy));

                    g_runtimeInfo.Init();

                    //
                    // Try to bind using ICLRMetaHostPolicy::GetRequestedRuntime and "v4.0.0" and upgrade policy.
                    //

                    WCHAR _wszVersion[256];  // We can't use new in this header, so just pick an obscenely long version string length of 256
                    DWORD _cchVersion = countof(_wszVersion);
                    wcscpy_s(_wszVersion, _cchVersion, L"v4.0.0");

                    IfHrFailRet(pMetaHostPolicy->GetRequestedRuntime(
                        METAHOST_POLICY_APPLY_UPGRADE_POLICY,
                        NULL, // image path
                        NULL, // config stream
                        _wszVersion,
                        &_cchVersion,
                        g_runtimeInfo.m_wszImageVersion,
                        &g_runtimeInfo.m_cchImageVersion,
                        NULL, // config flags
                        IID_ICLRRuntimeInfo,
                        reinterpret_cast<LPVOID *>(&g_runtimeInfo.m_pRuntimeInfo)));

                    //
                    // Initialized - now publish.
                    //

                    g_hRuntimeInfo.ClearUnsafe();
                    g_hRuntimeInfo.Assign(&g_runtimeInfo);
                    InterlockedExchange(&g_runtimeInfoIsInitialized, TRUE);
                    
                    hr = S_OK;
                }
            }

            return hr;
        }

        // ---GETCLRRUNTIMEINFO--------------------------------------------------------------------
        inline
        HRESULT GetCLRRuntimeInfo(
            /*out*/ ICLRRuntimeInfo **ppCLRRuntimeInfo,
            LPCWSTR  pEXE = NULL,
            IStream *pIStream = NULL,
            __inout_ecount_opt(*pcchVersion) LPWSTR wszVersion = NULL,
            DWORD   *pcchVersion = NULL)
        {
            HRESULT hr = S_OK;

            RuntimeInfo *pRuntimeInfo = NULL;
            IfHrFailRet(GetRuntimeInfo(&pRuntimeInfo, pEXE, pIStream, wszVersion, pcchVersion));

            *ppCLRRuntimeInfo = pRuntimeInfo->m_pRuntimeInfo;
            return hr;
        }

        // ---GetConfigImageVersion----------------------------------------------------------------
        inline
        HRESULT GetConfigImageVersion(
            __out_ecount(*pcchBuffer) LPWSTR wzBuffer,
            DWORD *pcchBuffer)
        {
            HRESULT hr = S_OK;

            RuntimeInfo *pRuntimeInfo = NULL;
            IfHrFailRet(GetRuntimeInfo(&pRuntimeInfo));

            DWORD cchBuffer = *pcchBuffer;
            *pcchBuffer = pRuntimeInfo->m_cchImageVersion;

            if (cchBuffer <= pRuntimeInfo->m_cchImageVersion)
            {
                wcsncpy_s(
                    wzBuffer,
                    cchBuffer,
                    pRuntimeInfo->m_wszImageVersion,
                    pRuntimeInfo->m_cchImageVersion);
            }
            else
            {
                IfHrFailRet(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
            }

            return hr;
        }

        // ---ICLRSTRONGNAME INTERFACE DATA--------------------------------------------------------
        SELECTANY ICLRStrongName* g_pCLRStrongName = NULL;
        SELECTANY ZeroInitGlobalReleaseHolder<ICLRStrongName*>  g_hCLRStrongName;

        // ---GETCLRSTRONGNAME---------------------------------------------------------------------
        // NOTE: Does not AddRef returned interface pointer.
        inline
        HRESULT GetCLRStrongName(
            /*out*/ ICLRStrongName **ppCLRStrongName)
        {
            HRESULT hr = S_OK;

            if (g_pCLRStrongName == NULL)
            {
                ICLRRuntimeInfo *pInfo = NULL;
                IfHrFailRet(GetCLRRuntimeInfo(&pInfo));

                ICLRStrongName *pStrongName;
                
                IfHrFailRet(pInfo->GetInterface(
                    CLSID_CLRStrongName,
                    IID_ICLRStrongName,
                    reinterpret_cast<LPVOID *>(&pStrongName)));

                //
                // Great - we got an ICLRStrongName. Now publish this to
                // g_pCLRStrongName in a thread-safe way.
                //

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<ICLRStrongName *>(
                        &g_pCLRStrongName, pStrongName, NULL) == NULL)
                {
                    // Successful publish. In this case, we also assign to the
                    // holder to ensure that the interface is released when the
                    // image is unloaded.
                    g_hCLRStrongName.ClearUnsafe();
                    g_hCLRStrongName.Assign(g_pCLRStrongName);
                }
                else
                {
                    // We were beat to the punch, don't publish this interface
                    // and make sure we use the published value for consistency.
                    pStrongName->Release();
                }
            }

            *ppCLRStrongName = g_pCLRStrongName;
            return hr;
        }

        // ---ICLRSTRONGNAME2 INTERFACE DATA--------------------------------------------------------
        SELECTANY ICLRStrongName2* g_pCLRStrongName2 = NULL;
        SELECTANY ZeroInitGlobalReleaseHolder<ICLRStrongName2*>  g_hCLRStrongName2;

        // ---GETCLRSTRONGNAME2---------------------------------------------------------------------
        // NOTE: Does not AddRef returned interface pointer.
        inline
        HRESULT GetCLRStrongName2(
            /*out*/ ICLRStrongName2 **ppCLRStrongName2)
        {
            HRESULT hr = S_OK;

            if (g_pCLRStrongName2 == NULL)
            {
                ICLRRuntimeInfo *pInfo = NULL;
                IfHrFailRet(GetCLRRuntimeInfo(&pInfo));

                ICLRStrongName2 *pStrongName;
                
                IfHrFailRet(pInfo->GetInterface(
                    CLSID_CLRStrongName,
                    IID_ICLRStrongName2,
                    reinterpret_cast<LPVOID *>(&pStrongName)));

                //
                // Great - we got an ICLRStrongName2. Now publish this to
                // g_pCLRStrongName2 in a thread-safe way.
                //

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<ICLRStrongName2 *>(
                        &g_pCLRStrongName2, pStrongName, NULL) == NULL)
                {
                    // Successful publish. In this case, we also assign to the
                    // holder to ensure that the interface is released when the
                    // image is unloaded.
                    g_hCLRStrongName2.ClearUnsafe();
                    g_hCLRStrongName2.Assign(g_pCLRStrongName2);
                }
                else
                {
                    // We were beat to the punch, don't publish this interface
                    // and make sure we use the published value for consistency.
                    pStrongName->Release();
                }
            }

            *ppCLRStrongName2 = g_pCLRStrongName2;
            return hr;
        }

        // ---AddStartupFlags------------------------------------------------------------------------------
        inline
        HRESULT AddStartupFlags(
            ICLRRuntimeInfo *pInfo,
            LPCWSTR wszBuildFlavor,
            DWORD dwStartupFlags,
            LPCWSTR wszHostConfigFile)
        {
            if (wszBuildFlavor != NULL &&
                (wszBuildFlavor[0] == L's' || wszBuildFlavor[0] == L'S') &&
                (wszBuildFlavor[1] == L'v' || wszBuildFlavor[1] == L'V') &&
                (wszBuildFlavor[2] == L'r' || wszBuildFlavor[2] == L'R') &&
                 wszBuildFlavor[3] == 0)
            {
                dwStartupFlags |= STARTUP_SERVER_GC;
            }

            HRESULT hr = S_OK;

            DWORD dwEffectiveStartupFlags = 0;
            IfHrFailRet(pInfo->GetDefaultStartupFlags(&dwEffectiveStartupFlags, NULL, NULL));
            
            // Startup flags at this point are either default (i.e. STARTUP_CONCURRENT_GC)
            // or have been set based on a config file. We want to clear the concurrent
            // GC flag because we are supplying non-defaults, and combine them with the
            // user supplied flags. Note that STARTUP_CONCURRENT_GC is never set as part
            // of reading a config so we are not losing any information here.

            dwEffectiveStartupFlags &= ~STARTUP_CONCURRENT_GC;
            dwEffectiveStartupFlags |= dwStartupFlags;

            return pInfo->SetDefaultStartupFlags(dwEffectiveStartupFlags, wszHostConfigFile);
        }

        // ------------------------------------------------------------------------------------------------
        SELECTANY HMODULE                     g_hShlwapi = NULL;
        SELECTANY ZeroInitGlobalHMODULEHolder g_hShlwapiHolder;

        // ------------------------------------------------------------------------------------------------
        inline
        HRESULT CreateIStreamFromFile(
            LPCWSTR wszFilePath,
            IStream **ppIStream)
        {
            HRESULT hr = S_OK;
            *ppIStream = NULL;

            if (g_hShlwapi == NULL)
            {
                HMODULE hShlwapi = LEGACY_ACTIVATION_SHIM_LOAD_LIBRARY(_TEXT_ENCODE("shlwapi.dll"));
                if (hShlwapi == NULL)
                    return HRESULT_FROM_WIN32(GetLastError());
                HMODULEHolder hShlwapiHolder(hShlwapi);

                if (LegacyActivationShim::Util::InterlockedCompareExchangePointerT<HMODULE>(
                    &g_hShlwapi, hShlwapi, NULL) == NULL)
                {
                    g_hShlwapiHolder.ClearUnsafe();
                    g_hShlwapiHolder.Assign(hShlwapi);
                    hShlwapiHolder.SuppressRelease();
                }
            }

            typedef HRESULT (__stdcall * SHCreateStreamOnFile_pfn)(
                LPCWSTR wszFile,
                DWORD grfMode,
                IStream **ppstm);

            SHCreateStreamOnFile_pfn pCreateStreamOnFile =
                reinterpret_cast<SHCreateStreamOnFile_pfn>(GetProcAddress(g_hShlwapi, "SHCreateStreamOnFileW"));

            if (pCreateStreamOnFile == NULL)
                return HRESULT_FROM_WIN32(GetLastError());

            //_ASSERTE(pCreateStreamOnFile != NULL);

            // Create IStream
            IStream* pStream(NULL);
            IfHrFailRet((*pCreateStreamOnFile)(wszFilePath, 0 /*STGM_READ*/, &pStream));
            ReleaseHolder<IStream*> hStream(pStream);

            // Success, prevent release and assign IStream to out parameter
            *ppIStream = pStream;
            hStream.SuppressRelease();

            return S_OK;
        }
    }; // namespace Util
}; // namespace LegacyActivationShim

#pragma pop_macro("UNUSED")
#pragma pop_macro("countof")
#pragma pop_macro("_TEXT_ENCODE")
#pragma pop_macro("SELECTANY")

#endif // __LEGACYACTIVATIONSHIMUTIL_H__

