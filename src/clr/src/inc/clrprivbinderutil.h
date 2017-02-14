// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Contains helper types for assembly binding host infrastructure.

#ifndef __CLRPRIVBINDERUTIL_H__
#define __CLRPRIVBINDERUTIL_H__

#include "holder.h"
#include "internalunknownimpl.h"
#include "clrprivbinding.h"
#include "slist.h"
#ifdef FEATURE_COMINTEROP
#include "windowsstring.h"
#endif // FEATURE_COMINTEROP
#include "strongnameholders.h"

//=====================================================================================================================
#define STANDARD_BIND_CONTRACT  \
    CONTRACTL {                 \
        NOTHROW;                \
        GC_TRIGGERS;            \
        MODE_PREEMPTIVE;        \
    } CONTRACTL_END

//=====================================================================================================================
// Forward declarations
interface ICLRPrivAssembly;
typedef DPTR(ICLRPrivAssembly) PTR_ICLRPrivAssembly;
typedef DPTR(ICLRPrivBinder) PTR_ICLRPrivBinder;
class PEAssembly;
class AssemblySpec;

//=====================================================================================================================
#define VALIDATE_CONDITION(condition, fail_op)  \
    do {                                        \
        _ASSERTE((condition));                  \
        if (!(condition))                       \
            fail_op;                            \
    } while (false)

#define VALIDATE_PTR_RET(val) VALIDATE_CONDITION(val != nullptr, return E_POINTER)
#define VALIDATE_PTR_THROW(val) VALIDATE_CONDITION(val != nullptr, ThrowHR(E_POINTER))
#define VALIDATE_ARG_RET(condition) VALIDATE_CONDITION(condition, return E_INVALIDARG)
#define VALIDATE_ARG_THROW(condition) VALIDATE_CONDITION(condition, ThrowHR(E_INVALIDARG))

//=====================================================================================================================
namespace CLRPrivBinderUtil
{
    //=================================================================================================================
    enum BindFlags
    {
        BF_BindIL      = 1,
        BF_BindNI      = 2,
        BF_Default     = BF_BindIL | BF_BindNI,
    };

    //=================================================================================================================
    template <typename ItfT, typename ObjT>
    inline ItfT * ToInterface(
        ObjT * && pObj)
    {
        STATIC_CONTRACT_THROWS;
        
        ItfT * pItf = nullptr;
        IfFailThrow(pObj->QueryInterface(__uuidof(ItfT), (void **)&pItf));
        return pItf;
    }
    
    //=================================================================================================================
    template <typename ItfT, typename ObjT>
    inline ItfT * ToInterface_NoThrow(
        ObjT * && pObj)
    {
        LIMITED_METHOD_CONTRACT;
        
        ItfT * pItf = nullptr;
        if (FAILED(pObj->QueryInterface(__uuidof(ItfT), (void **)&pItf)))
        {
            return nullptr;
        }
        return pItf;
    }

    //=====================================================================================================================

    //=================================================================================================================
    // Used to create an identity-only ICLRPrivAssembly from an ICLRPrivBinder. This is currently used when
    // creating dynamic assemblies as these use the parent assembly's ICLRPrivBinder object to provide binding
    // functionaltiy.

    class CLRPrivBinderAsAssemblyWrapper :
        public IUnknownCommon<ICLRPrivAssembly>
    {
    public:
        //-----------------------------------------------------------------------------------------------------------------
        CLRPrivBinderAsAssemblyWrapper(
            ICLRPrivBinder *pWrapped)
            : _pWrapped(clr::SafeAddRef(pWrapped))
        {
            STANDARD_VM_CONTRACT;
            VALIDATE_ARG_THROW(pWrapped);
        }

        //-----------------------------------------------------------------------------------------------------------------
        // Forwards to wrapped binder.
        STDMETHOD(BindAssemblyByName)(
            IAssemblyName * pAssemblyName,
            ICLRPrivAssembly ** ppAssembly)
        {
            WRAPPER_NO_CONTRACT;
            return _pWrapped->BindAssemblyByName(pAssemblyName, ppAssembly);
        }
        
        //-----------------------------------------------------------------------------------------------------------------
        // Forwards to wrapped binder.
        STDMETHOD(VerifyBind)(
            IAssemblyName *pAssemblyName,
            ICLRPrivAssembly *pAssembly,
            ICLRPrivAssemblyInfo *pAssemblyInfo)
        {
            WRAPPER_NO_CONTRACT;
            return _pWrapped->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
        }
        
        //---------------------------------------------------------------------------------------------
        // Forwards to wrapped binder.
        STDMETHOD(GetBinderFlags)(
            DWORD *pBinderFlags)
        {
            WRAPPER_NO_CONTRACT;
            return _pWrapped->GetBinderFlags(pBinderFlags);
        }

        //-----------------------------------------------------------------------------------------------------------------
        // Forwards to wrapped binder.
        STDMETHOD(GetBinderID)(
            UINT_PTR *pBinderId)
        {
            WRAPPER_NO_CONTRACT;
            return _pWrapped->GetBinderID(pBinderId);
        }

        //-----------------------------------------------------------------------------------------------------------------
        // Forwards to wrapped binder.
        STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
            LPVOID pvAssemblySpec,
            HRESULT * pResult,
            ICLRPrivAssembly ** ppAssembly)
        { STATIC_CONTRACT_WRAPPER; return _pWrapped->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly); }

        //-----------------------------------------------------------------------------------------------------------------
        // ICLRPrivAssembly method is unsupported.
        STDMETHOD(IsShareable)(
            BOOL * pbIsShareable)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE_MSG(false, "CLRPrivBinderAsAssemblyWrapper does not support ICLRPrivAssembly methods (just ICLRPrivBinder ones)!");
            VALIDATE_ARG_RET(pbIsShareable);
            *pbIsShareable = FALSE;
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }
   
        //-----------------------------------------------------------------------------------------------------------------
        // ICLRPrivAssembly method is unsupported.
        STDMETHOD(GetAvailableImageTypes)(
            LPDWORD pdwImageTypes)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE_MSG(false, "CLRPrivBinderAsAssemblyWrapper does not support ICLRPrivAssembly methods (just ICLRPrivBinder ones)!");
            VALIDATE_ARG_RET(pdwImageTypes);
            *pdwImageTypes = 0;
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }
   
        //-----------------------------------------------------------------------------------------------------------------
        // ICLRPrivAssembly method is unsupported.
        STDMETHOD(GetImageResource)(
            DWORD dwImageType,
            DWORD* pdwImageType,
            ICLRPrivResource ** ppIResource)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE_MSG(false, "CLRPrivBinderAsAssemblyWrapper does not support ICLRPrivAssembly methods (just ICLRPrivBinder ones)!");
            VALIDATE_ARG_RET(pdwImageType);
            *pdwImageType = 0;
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }

    private:
        ReleaseHolder<ICLRPrivBinder> _pWrapped;
    };

    //=================================================================================================================
    // Provides a struct that can be accessed at the QWORD, DWORD, or WORD level, and is structured 

    struct AssemblyVersion
    {
#if BIGENDIAN
        union
        {
            UINT64 qwMajorMinorBuildRevision;
            struct
            {
                union
                {
                    DWORD dwMajorMinor;
                    struct
                    {
                        WORD wMajor;
                        WORD wMinor;
                    };
                };
                union
                {
                    DWORD dwBuildRevision;
                    struct
                    {
                        WORD wBuild;
                        WORD wRevision;
                    };
                };
            };
        };
#else
        union
        {
            UINT64 qwMajorMinorBuildRevision;
            struct
            {
                union
                {
                    DWORD dwBuildRevision;
                    struct
                    {
                        WORD wRevision;
                        WORD wBuild;
                    };
                };
                union
                {
                    DWORD dwMajorMinor;
                    struct
                    {
                        WORD wMinor;
                        WORD wMajor;
                    };
                };
            };
        };
#endif

        // Default value is 0.0.0.0
        AssemblyVersion()
            : qwMajorMinorBuildRevision(static_cast<UINT64>(0))
        { LIMITED_METHOD_CONTRACT; }

        // Copy constructor
        AssemblyVersion(AssemblyVersion const & other)
            : qwMajorMinorBuildRevision(other.qwMajorMinorBuildRevision)
        { LIMITED_METHOD_CONTRACT; }

        // Initialize version using an IAssemblyName object.
        HRESULT Initialize(IAssemblyName * pName);

        // Initialize version using an ICLRPrivAssemblyInfo object.
        HRESULT Initialize(ICLRPrivAssemblyInfo * pInfo);

        // Relative ordering of versions.
        static inline int Compare(
            AssemblyVersion const & left,
            AssemblyVersion const & right)
        {
            LIMITED_METHOD_CONTRACT;

            if (left.qwMajorMinorBuildRevision < right.qwMajorMinorBuildRevision)
                return -1;
            else if (left.qwMajorMinorBuildRevision == right.qwMajorMinorBuildRevision)
                return 0;
            else
                return 1;
        }
    };  // struct AssemblyVersion

    inline bool operator ==(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision == rhs.qwMajorMinorBuildRevision; }

    inline bool operator !=(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision != rhs.qwMajorMinorBuildRevision; }

    inline bool operator  <(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision <  rhs.qwMajorMinorBuildRevision; }

    inline bool operator <=(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision <= rhs.qwMajorMinorBuildRevision; }

    inline bool operator  >(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision >  rhs.qwMajorMinorBuildRevision; }

    inline bool operator >=(AssemblyVersion const & lhs, AssemblyVersion const & rhs)
    { LIMITED_METHOD_CONTRACT; return lhs.qwMajorMinorBuildRevision >= rhs.qwMajorMinorBuildRevision; }

    //=================================================================================================================
    // Encapsulates PublicKey value, can be initialized using a variety of data sources.

    struct PublicKey
    {
        // Defaults to empty value.
        PublicKey()
            : m_key(nullptr)
            , m_key_owned(false)
            , m_size((DWORD)-1)
        { LIMITED_METHOD_CONTRACT; }

        // Construct directly from existing public key data.
        PublicKey(PBYTE pbKey, DWORD cbKey)
            : m_key(pbKey)
            , m_key_owned(false)
            , m_size(cbKey)
        { LIMITED_METHOD_CONTRACT; }

        ~PublicKey()
        { WRAPPER_NO_CONTRACT; Uninitialize(); }

        // Frees any public key data and resets to default value.
        void Uninitialize()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_key_owned)
            {
                delete [] m_key;
                m_key_owned = false;
            }
            m_key = nullptr;
            m_size = 0;
        }

        // Initialize PK data form an ICLRPrivAssemblyInfo object.
        HRESULT Initialize(ICLRPrivAssemblyInfo * pAssemblyInfo);

        // Returns PK data pointer.
        inline BYTE const * GetKey() const
        { LIMITED_METHOD_CONTRACT; return m_key; }

        // Returns size in bytes of the PK data.
        inline DWORD GetSize() const
        { LIMITED_METHOD_CONTRACT; return m_size; }

    private:
        PBYTE                   m_key;
        bool                    m_key_owned;
        DWORD                   m_size;
    };

    //=================================================================================================================
    // Encapsulates PublicKeyToken value, can be initialized using a variety of data sources.
    //
    // Constraints: assumes that non-empty PKT data will always be 8 bytes long.
    //

    struct PublicKeyToken
    {
        PublicKeyToken()
            : m_cbKeyToken(0)
        { LIMITED_METHOD_CONTRACT; ZeroMemory(&m_rbKeyToken, sizeof(m_rbKeyToken)); }

        PublicKeyToken(PublicKeyToken const & other)
            : m_cbKeyToken(other.m_cbKeyToken)
        { LIMITED_METHOD_CONTRACT; CopyMemory(m_rbKeyToken, other.m_rbKeyToken, sizeof(m_rbKeyToken)); }

        // Initialize directly from PKT data.
        HRESULT Initialize(BYTE * pbKeyToken, DWORD cbKeyToken);

        // Converts PK data to PKT data.
        HRESULT Initialize(PublicKey const & pk);

        // Initialize using the PKT value contained by pName; returns S_FALSE if there is no associated PKT.
        HRESULT Initialize(IAssemblyName * pName);

        // Initialize using the PK data contained by pInfo; returns S_FALSE if there is no associated PK.
        HRESULT Initialize(ICLRPrivAssemblyInfo * pInfo);

        // PKT data.
        BYTE const * GetToken() const
        { LIMITED_METHOD_CONTRACT; return m_rbKeyToken; }

        // Size in bytes of the PKT (should always be 0 or 8).
        DWORD GetSize() const
        { LIMITED_METHOD_CONTRACT; return m_cbKeyToken; }

    private:
        static const DWORD PUBLIC_KEY_TOKEN_LEN1 = 8;
        BYTE    m_rbKeyToken[PUBLIC_KEY_TOKEN_LEN1];
        DWORD   m_cbKeyToken;
    };

    bool operator==(PublicKeyToken const & lhs, PublicKeyToken const & rhs);

    inline bool operator!=(PublicKeyToken const & lhs, PublicKeyToken const & rhs)
    { WRAPPER_NO_CONTRACT; return !(lhs == rhs); }

    //=================================================================================================================
    // Encapsulates data required for packaged assembly identity: simple name, version, and public key token.
    //
    // Constraints: assumes that the assembly simple name is no longer than _MAX_PATH
    //

    struct AssemblyIdentity
    {
        AssemblyIdentity()
        { LIMITED_METHOD_CONTRACT; Name[0] = W('\0'); }

        AssemblyIdentity(AssemblyIdentity const & other)
            : Version(other.Version)
            , KeyToken(other.KeyToken)
        { LIMITED_METHOD_CONTRACT; CopyMemory(Name, other.Name, sizeof(Name)); }

        // Initialize from assembly simple name; default version and empty PKT values are used.
        HRESULT Initialize(LPCWSTR wzName);

        // Initialize from an ICLRPrivAssemblyInfo object.
        HRESULT Initialize(ICLRPrivAssemblyInfo * pAssemblyInfo);

        // Initialize from an IAssemblyName object.
        HRESULT Initialize(IAssemblyName * pAssemblyName);

        // Initialize from an AssemblySpec object.
        HRESULT Initialize(AssemblySpec * pSpec);

        // Assembly simple name
        WCHAR                                   Name[_MAX_PATH];

        // Assembly version; defaults to 0.0.0.0.
        CLRPrivBinderUtil::AssemblyVersion      Version;

        // Assembly public key token; defaults to none.
        CLRPrivBinderUtil::PublicKeyToken       KeyToken;
    };

    //=================================================================================================================
    HRESULT VerifyBind(
        IAssemblyName *pRefAssemblyName,
        ICLRPrivAssemblyInfo *pDefAssemblyInfo);

    //=================================================================================================================
    HRESULT VerifyBind(
        CLRPrivBinderUtil::AssemblyIdentity const & refIdentity,
        CLRPrivBinderUtil::AssemblyIdentity const & defIdentity);

    //=================================================================================================
    template <typename ItfT>
    struct CLRPrivResourceBase :
        public IUnknownCommon<ICLRPrivResource>
    {
        //---------------------------------------------------------------------------------------------
        STDMETHOD(GetResourceType)(
            IID *pIID)
        {
            LIMITED_METHOD_CONTRACT;
            if (pIID == nullptr)
                return E_INVALIDARG;
            *pIID = __uuidof(ItfT);
            return S_OK;
        }
    };

    //=================================================================================================================
    class CLRPrivResourcePathImpl :
        public IUnknownCommon< ItfBase< CLRPrivResourceBase< ICLRPrivResourcePath > >,
                               ICLRPrivResourcePath >
    {
    public:
        //---------------------------------------------------------------------------------------------
        CLRPrivResourcePathImpl(LPCWSTR wzPath);

        //---------------------------------------------------------------------------------------------
        LPCWSTR GetPath()
        { return m_wzPath; }

        //
        // ICLRPrivResourcePath methods
        //

        //---------------------------------------------------------------------------------------------
        STDMETHOD(GetPath)(
            DWORD cchBuffer,
            LPDWORD pcchBuffer,
            __inout_ecount_part(cchBuffer, *pcchBuffer) LPWSTR wzBuffer);

    private:
        //---------------------------------------------------------------------------------------------
        NewArrayHolder<WCHAR> m_wzPath;
    };

    //=================================================================================================================
    class CLRPrivResourceStreamImpl :
        public IUnknownCommon< ItfBase< CLRPrivResourceBase<ICLRPrivResourceStream > >,
                               ICLRPrivResourceStream>
    {
    public:
        //---------------------------------------------------------------------------------------------
        CLRPrivResourceStreamImpl(IStream * pStream);

        //---------------------------------------------------------------------------------------------
        STDMETHOD(GetStream)(
            REFIID riid,
            LPVOID * ppvStream);

    private:
        //---------------------------------------------------------------------------------------------
        ReleaseHolder<IStream> m_pStream;
    };

    //=================================================================================================================
    // Helper to prioritize binder errors. This class will ensure that all other errors have priority over
    // CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT. This class should be used just like an HRESULT variable.
    class BinderHRESULT
    {
    public:
        BinderHRESULT()
            : m_hr(S_OK)
        {}

        BinderHRESULT(HRESULT hr)
            : m_hr(hr)
        {}

        operator HRESULT() const
        { return m_hr; }

        BinderHRESULT & operator=(HRESULT hr)
        {
            // Always record change in success/failure status.
            if (FAILED(hr) != FAILED(m_hr))
                m_hr = hr;

            if (FAILED(hr))
            {
                if (SUCCEEDED(m_hr))
                    m_hr = hr;
                else if (m_hr == CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT)
                    m_hr = hr;
            }
            else
            {
                m_hr = hr;
            }
            return *this;
        }

    private:
        HRESULT m_hr;
    };  // class BinderHRESULT
    
    //=================================================================================================================
    // Types for WStringList (used in WinRT binders)
    
    typedef SListElem< PTR_WSTR > WStringListElem;
    typedef DPTR(WStringListElem) PTR_WStringListElem;
    typedef SList< WStringListElem, false /* = fHead default value */, PTR_WStringListElem > WStringList;
    typedef DPTR(WStringList)     PTR_WStringList;
    
    // Destroys list of strings (code:WStringList).
    void WStringList_Delete(WStringList * pList);
    
#ifndef DACCESS_COMPILE
    //=====================================================================================================================
    // Holder of allocated code:WStringList (helper class for WinRT binders - e.g. code:CLRPrivBinderWinRT::GetFileNameListForNamespace).
    class WStringListHolder
    {
    public:
        WStringListHolder(WStringList * pList = nullptr)
        {
            LIMITED_METHOD_CONTRACT;
            m_pList = pList;
        }
        ~WStringListHolder()
        {
            LIMITED_METHOD_CONTRACT;
            Destroy();
        }
        
        void InsertTail(LPCWSTR wszValue)
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;
            
            NewArrayHolder<WCHAR> wszElemValue = DuplicateStringThrowing(wszValue);
            NewHolder<WStringListElem> pElem = new WStringListElem(wszElemValue);
            
            if (m_pList == nullptr)
            {
                m_pList = new WStringList();
            }
            
            m_pList->InsertTail(pElem.Extract());
            // The string is now owned by the list
            wszElemValue.SuppressRelease();
        }
        
        WStringList * GetValue()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pList;
        }
        
        WStringList * Extract()
        {
            LIMITED_METHOD_CONTRACT;
            
            WStringList * pList = m_pList;
            m_pList = nullptr;
            return pList;
        }
        
    private:
        void Destroy()
        {
            LIMITED_METHOD_CONTRACT;
            
            if (m_pList != nullptr)
            {
                WStringList_Delete(m_pList);
                m_pList = nullptr;
            }
        }
        
    private:
        WStringList * m_pList;
    };  // class WStringListHolder
#endif //!DACCESS_COMPILE
    
#ifdef FEATURE_COMINTEROP
    //=====================================================================================================================
    // Holder of allocated array of HSTRINGs (helper class for WinRT binders - e.g. code:CLRPrivBinderWinRT::m_rgAltPaths).
    class HSTRINGArrayHolder
    {
    public:
        HSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;
        
            m_cValues = 0;
            m_rgValues = nullptr;
        }
#ifndef DACCESS_COMPILE
        ~HSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;
            Destroy();
        }
        
        // Destroys current array and allocates a new one with cValues elements.
        void Allocate(DWORD cValues)
        {
            STANDARD_VM_CONTRACT;
        
            Destroy();
            _ASSERTE(m_cValues == 0);
        
            if (cValues > 0)
            {
                m_rgValues = new HSTRING[cValues];
                m_cValues = cValues;
            
                // Initialize the array values
                for (DWORD i = 0; i < cValues; i++)
                {
                    m_rgValues[i] = nullptr;
                }
            }
        }
#endif //!DACCESS_COMPILE

        HSTRING GetAt(DWORD index) const
        {
            LIMITED_METHOD_CONTRACT;
            return m_rgValues[index];
        }
    
        HSTRING * GetRawArray()
        {
            LIMITED_METHOD_CONTRACT;
            return m_rgValues;
        }
    
        DWORD GetCount()
        {
            LIMITED_METHOD_CONTRACT;
            return m_cValues;
        }
    
    private:
#ifndef DACCESS_COMPILE
        void Destroy()
        {
            LIMITED_METHOD_CONTRACT;
        
            for (DWORD i = 0; i < m_cValues; i++)
            {
                if (m_rgValues[i] != nullptr)
                {
                    WindowsDeleteString(m_rgValues[i]);
                }
            }
            m_cValues = 0;
        
            if (m_rgValues != nullptr)
            {
                delete [] m_rgValues;
                m_rgValues = nullptr;
            }
        }
#endif //!DACCESS_COMPILE
    
    private:
        DWORD     m_cValues;
        HSTRING * m_rgValues;
    };  // class HSTRINGArrayHolder
    
#endif // FEATURE_COMINTEROP

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    ///// ----------------------------- Questionable stuff  -------------------------------------------
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    /** probably should be exposed on an instance (of something) method rather that magically calling GetAppDomain() **/
    ICLRPrivAssembly* RaiseAssemblyResolveEvent(IAssemblyName *pAssemblyName, ICLRPrivAssembly* pRequestingAssembly);

    /** Ultimately, only the binder can do ref-def matching, and it should be opaque to CLR. 
     This is not trivial to do, however, since we cannot do data conversion as the function is nofault **/
    BOOL CompareHostBinderSpecs(AssemblySpec* a1, AssemblySpec* a2);

    /** PLACEHOLDER - the same issue as  CompareHostBinderSpecs applies to hashing assemblyspecs  **/
} // namespace CLRPrivBinderUtil

#endif // __CLRPRIVBINDERUTIL_H__
