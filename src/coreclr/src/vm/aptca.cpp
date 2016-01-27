// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// aptca.h
//
// Functions for handling allow partially trusted callers assemblies
//

// 
//--------------------------------------------------------------------------


#include "common.h"
#include "aptca.h"

//
// Conditional APTCA cache implementation
//

ConditionalAptcaCache::ConditionalAptcaCache(AppDomain *pAppDomain) 
    : m_pAppDomain(pAppDomain),
      m_canonicalListIsNull(false),
      m_domainState(kDomainStateUnknown)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pAppDomain != NULL);
}

ConditionalAptcaCache::~ConditionalAptcaCache()
{
    WRAPPER_NO_CONTRACT;
}

void ConditionalAptcaCache::SetCachedState(PTR_PEImage pImage, ConditionalAptcaCache::State state)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pImage));
        PRECONDITION(state != kUnknown);
    }
    CONTRACTL_END;

    if (state == kNotCAptca)
    {
        pImage->SetIsNotConditionalAptca();
    }
}

ConditionalAptcaCache::State ConditionalAptcaCache::GetCachedState(PTR_PEImage pImage)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pImage));
    }
    CONTRACTL_END;

    if (!pImage->MayBeConditionalAptca())
    {
        return kNotCAptca;
    }

    return kUnknown;
}

void ConditionalAptcaCache::SetCanonicalConditionalAptcaList(LPCWSTR wszCanonicalConditionalAptcaList)
{
    WRAPPER_NO_CONTRACT;
    m_canonicalListIsNull = (wszCanonicalConditionalAptcaList == NULL);
    m_canonicalList.Set(wszCanonicalConditionalAptcaList);
}

#ifndef CROSSGEN_COMPILE
ConditionalAptcaCache::DomainState ConditionalAptcaCache::GetConditionalAptcaDomainState()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_domainState == kDomainStateUnknown)
    {
        IApplicationSecurityDescriptor *pASD = m_pAppDomain->GetSecurityDescriptor();
        DomainState domainState = kDomainStateUnknown;

        // In the full trust case we only need to look at the conditional APTCA list in the case that the host
        // has configured one on the default domain (for instance WPF).  Otherwise, all full trust domains have
        // all conditional APTCA assemblies enabled.
        bool processFullTrustAptcaList = false;
        if (m_pAppDomain->IsCompilationDomain())
        {
            processFullTrustAptcaList = false;
        }
        else if (m_pAppDomain->IsDefaultDomain())
        {
            processFullTrustAptcaList = !m_canonicalListIsNull;
        }
        else
        {
            processFullTrustAptcaList = ConsiderFullTrustConditionalAptcaLists();
        }

        // Consider the domain to be fully trusted if it really is fully trusted, or if we're currently
        // setting the domain up, it looks like it will be fully trusted, and the AppDomainManager has
        // promised that won't change.
        bool isFullTrustDomain = !m_pAppDomain->GetSecurityDescriptor()->DomainMayContainPartialTrustCode();
        if (pASD->IsInitializationInProgress() && (m_pAppDomain->GetAppDomainManagerInitializeNewDomainFlags() & eInitializeNewDomainFlags_NoSecurityChanges))
        {
            BOOL preResolveFullTrust;
            BOOL preResolveHomogenous;
            pASD->PreResolve(&preResolveFullTrust, &preResolveHomogenous);

            isFullTrustDomain = preResolveFullTrust && preResolveHomogenous;
        }

        if (m_pAppDomain->IsCompilationDomain())
        {
            // NGEN always enables all conditional APTCA assemblies
            domainState = kAllEnabled;
        }
        else if (!isFullTrustDomain || processFullTrustAptcaList)
        {
            if (m_canonicalList.GetCount() == 0)
            {
                // A null or empty conditional APTCA list means that no assemblies are enabled in this domain
                domainState = kAllDisabled;
            }
            else
            {
                // We're in a domain that supports conditional APTCA and an interesting list is supplied.  In
                // this domain, some assemblies are enabled.
                domainState = kSomeEnabled;
            }
        }
        else
        {
            domainState = kAllEnabled;
        }

        _ASSERTE(domainState != kDomainStateUnknown);
        InterlockedCompareExchange(reinterpret_cast<volatile LONG *>(&m_domainState), domainState, kDomainStateUnknown);
    }

    return m_domainState;
}

// static
bool ConditionalAptcaCache::ConsiderFullTrustConditionalAptcaLists()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (GetAppDomain()->IsCompilationDomain())
    {
        return false;
    }

    IApplicationSecurityDescriptor *pASD = SystemDomain::System()->DefaultDomain()->GetSecurityDescriptor();
    ConditionalAptcaCache *pDefaultDomainCaptca = pASD->GetConditionalAptcaCache();

    // The only way that we use CAPTCA lists is if the host has configured the default domain to not be all
    // enabled (that is, the host has setup a CAPTCA list of any sort for the default domain)
    return pDefaultDomainCaptca->GetConditionalAptcaDomainState() != kAllEnabled;
}

// APTCA killbit list helper functions
namespace
{
    static const LPCWSTR wszAptcaRootKey = W("SOFTWARE\\Microsoft\\.NETFramework\\Policy\\APTCA");

    //--------------------------------------------------------------------------------------------------------
    //
    // The AptcaKillBitList class is responsible for holding the machine wide list of assembly name / file
    // versions which have been disabled for APTCA on the machine.
    //

    class AptcaKillBitList
    {
    private:
        ArrayList m_killBitList;

    public:
        ~AptcaKillBitList();

        bool AreAnyAssembliesKillBitted();
        bool IsAssemblyKillBitted(PEAssembly *pAssembly);
        bool IsAssemblyKillBitted(IAssemblyName *pAssemblyName, ULARGE_INTEGER fileVersion);

        static AptcaKillBitList *ReadMachineKillBitList();

    private:
        AptcaKillBitList();
        AptcaKillBitList(const AptcaKillBitList &other); // not implemented

    private:
        static const LPCWSTR wszKillBitValue;

    private:
        static bool FileVersionsAreEqual(ULARGE_INTEGER targetVersion, IAssemblyName *pKillBitAssemblyName);
    };
    const LPCWSTR AptcaKillBitList::wszKillBitValue = W("APTCA_FLAG");
    
    AptcaKillBitList::AptcaKillBitList()
    {
        LIMITED_METHOD_CONTRACT;
    }

    AptcaKillBitList::~AptcaKillBitList()
    {
        WRAPPER_NO_CONTRACT;

        // Release all of the IAssemblyName objects stored in this list
        for (DWORD i = 0; i < m_killBitList.GetCount(); ++i)
        {
            IAssemblyName *pKillBitAssemblyName = reinterpret_cast<IAssemblyName *>(m_killBitList.Get(i));
            if (pKillBitAssemblyName != NULL)
            {
                pKillBitAssemblyName->Release();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine if any assemblies are on the APTCA killbit list
    //

    bool AptcaKillBitList::AreAnyAssembliesKillBitted()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        // We don't consider the killbit for NGEN, as ngened code always assumes that APTCA is enabled.
        if (GetAppDomain()->IsCompilationDomain())
        {
            return false;
        }

        return m_killBitList.GetCount() > 0;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Compare the file versions of an assembly with the verison that is being killbitted to see if they
    // match.  For compatibility with v3.5, we assume any failure means that the versions do not match.
    //

    // static
    bool AptcaKillBitList::FileVersionsAreEqual(ULARGE_INTEGER targetVersion, IAssemblyName *pKillBitAssemblyName)
    {
        DWORD dwKillBitMajorVersion = 0;
        DWORD dwVersionSize = sizeof(dwKillBitMajorVersion);
        if (FAILED(pKillBitAssemblyName->GetProperty(ASM_NAME_FILE_MAJOR_VERSION, &dwKillBitMajorVersion, &dwVersionSize)) ||
            dwVersionSize == 0)
        {
            return false;
        }

        DWORD dwKillBitMinorVersion = 0;
        dwVersionSize = sizeof(dwKillBitMinorVersion);
        if (FAILED(pKillBitAssemblyName->GetProperty(ASM_NAME_FILE_MINOR_VERSION, &dwKillBitMinorVersion, &dwVersionSize)) ||
            dwVersionSize == 0)
        {
            return false;
        }

        DWORD dwKillBitBuildVersion = 0;
        dwVersionSize = sizeof(dwKillBitBuildVersion);
        if (FAILED(pKillBitAssemblyName->GetProperty(ASM_NAME_FILE_BUILD_NUMBER, &dwKillBitBuildVersion, &dwVersionSize)) ||
            dwVersionSize == 0)
        {
            return false;
        }

        DWORD dwKillBitRevisionVersion = 0;
        dwVersionSize = sizeof(dwKillBitRevisionVersion);
        if (FAILED(pKillBitAssemblyName->GetProperty(ASM_NAME_FILE_REVISION_NUMBER, &dwKillBitRevisionVersion, &dwVersionSize)) ||
            dwVersionSize == 0)
        {
            return false;
        }

        DWORD dwTargetMajorVersion = (targetVersion.HighPart & 0xFFFF0000) >> 16;
        DWORD dwTargetMinorVersion = targetVersion.HighPart & 0x0000FFFF;
        DWORD dwTargetBuildVersion = (targetVersion.LowPart & 0xFFFF0000) >> 16;
        DWORD dwTargetRevisionVersion = targetVersion.LowPart & 0x0000FFFF;

        return dwTargetMajorVersion == dwKillBitMajorVersion &&
               dwTargetMinorVersion == dwKillBitMinorVersion &&
               dwTargetBuildVersion == dwKillBitBuildVersion &&
               dwTargetRevisionVersion == dwKillBitRevisionVersion;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine if a specific assembly is on the killbit list
    //

    bool AptcaKillBitList::IsAssemblyKillBitted(PEAssembly *pAssembly)
    {
        STANDARD_VM_CONTRACT;

        IAssemblyName *pTargetAssemblyName = pAssembly->GetFusionAssemblyName();

        // For compat with v3.5, we use hte Win32 file version here rather than the Fusion version
        LPCWSTR pwszPath = pAssembly->GetPath().GetUnicode();
        if (pwszPath != NULL)
        {
            ULARGE_INTEGER fileVersion = { 0, 0 };
            HRESULT hr = GetFileVersion(pwszPath, &fileVersion);
            if (SUCCEEDED(hr))
            {
                return IsAssemblyKillBitted(pTargetAssemblyName, fileVersion);
            }
        }

        return false;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine if a specific assembly is on the killbit list
    //

    bool AptcaKillBitList::IsAssemblyKillBitted(IAssemblyName *pTargetAssemblyName, ULARGE_INTEGER fileVersion)
    {
        STANDARD_VM_CONTRACT;

        // If nothing is killbitted, then this assembly cannot be killbitted
        if (!AreAnyAssembliesKillBitted())
        {
            return false;
        }

        for (DWORD i = 0; i < m_killBitList.GetCount(); ++i)
        {
            IAssemblyName *pKillBitAssemblyName = reinterpret_cast<IAssemblyName *>(m_killBitList.Get(i));

            // By default, we compare all fields of the assembly's name, however if the culture was neutral,
            // we strip that out.
            DWORD dwCmpFlags = ASM_CMPF_IL_ALL;

            DWORD cbCultureSize = 0;
            SString strCulture;
            HRESULT hrCulture = pKillBitAssemblyName->GetProperty(ASM_NAME_CULTURE, NULL, &cbCultureSize);
            if (hrCulture == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            {
                DWORD cchCulture = (cbCultureSize / sizeof(WCHAR)) - 1;
                WCHAR *wszCultureBuffer = strCulture.OpenUnicodeBuffer(cchCulture);
                hrCulture = pKillBitAssemblyName->GetProperty(ASM_NAME_CULTURE, wszCultureBuffer, &cbCultureSize);
                strCulture.CloseBuffer();
            }

            if (SUCCEEDED(hrCulture))
            {
                if (cbCultureSize == 0 || strCulture.EqualsCaseInsensitive(W("")) || strCulture.EqualsCaseInsensitive(W("neutral")))
                {
                    dwCmpFlags &= ~ASM_CMPF_CULTURE;
                }
            }

            // If the input assembly matches the kill bit assembly's name and file version, then we need to
            // kill it.
            if (pTargetAssemblyName->IsEqual(pKillBitAssemblyName, dwCmpFlags) == S_OK &&
                FileVersionsAreEqual(fileVersion, pKillBitAssemblyName))
            {
                return true;
            }
        }

        return false;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Read the machine-wide APTCA kill bit list into a kill bit list object.  For compatibility with v3.5,
    // errors during this initialization are ignored - leading to APTCA entries that may not be considered
    // for kill bitting.
    //

    // static
    AptcaKillBitList *AptcaKillBitList::ReadMachineKillBitList()
    {
        CONTRACT(AptcaKillBitList *)
        {
            STANDARD_VM_CHECK;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        NewHolder<AptcaKillBitList> pKillBitList(new AptcaKillBitList);

        HKEYHolder hKeyAptca;

        // Open the APTCA subkey in the registry.
        if (WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, wszAptcaRootKey, 0, KEY_READ, &hKeyAptca) == ERROR_SUCCESS)
        {

            DWORD cchSubKeySize = 0;
            if (WszRegQueryInfoKey(hKeyAptca, NULL, NULL, NULL, NULL, &cchSubKeySize, NULL, NULL, NULL, NULL, NULL, NULL) != ERROR_SUCCESS)
            {
                cchSubKeySize = MAX_PATH_FNAME;
            }
            ++cchSubKeySize;

            NewArrayHolder<WCHAR> wszSubKey(new WCHAR[cchSubKeySize]);

            DWORD dwKey = 0;
            DWORD cchWszSubKey = cchSubKeySize;
            // Assembly specific records are represented as subkeys of the key we've just opened with names
            // equal to the strong name of the assembly being kill bitted, and a value of APTCA_FLAG = 1.
            while (WszRegEnumKeyEx(hKeyAptca, dwKey, wszSubKey, &cchWszSubKey, NULL, NULL, NULL, NULL) == ERROR_SUCCESS)
            {
                ++dwKey;
                cchWszSubKey = cchSubKeySize;

                // Open the subkey: the key name is the full name of the assembly to potentially kill-bit
                HKEYHolder hSubKey;
                if (WszRegOpenKeyEx(hKeyAptca, wszSubKey, 0, KEY_READ, &hSubKey) != ERROR_SUCCESS)
                {
                    continue;
                }

                DWORD dwKillbit = 0;
                DWORD dwType = REG_DWORD;
                DWORD dwSize = sizeof(dwKillbit);

                // look for the APTCA flag
                LONG queryValue =  WszRegQueryValueEx(hSubKey,
                                                      wszKillBitValue,
                                                      NULL,
                                                      &dwType,
                                                      reinterpret_cast<LPBYTE>(&dwKillbit),
                                                      &dwSize);
                if (queryValue == ERROR_SUCCESS && dwKillbit == 1)
                {
                    // We have a strong named assembly with an APTCA killbit value set - parse the key into
                    // an assembly name, and add it to our list
                    ReleaseHolder<IAssemblyName> pKillBitAssemblyName;
                    HRESULT hrAssemblyName = CreateAssemblyNameObject(&pKillBitAssemblyName, wszSubKey, CANOF_PARSE_DISPLAY_NAME, NULL);
                    if (FAILED(hrAssemblyName))
                    {
                        continue;
                    }

                    //
                    // For compatibility with v3.5, we only accept kill bit entries which have four part
                    // assembly versions, names, and public key tokens.
                    //

                    // Verify the version first
                    bool validVersion = true;
                    for (DWORD dwVersionPartId = ASM_NAME_MAJOR_VERSION; dwVersionPartId <= ASM_NAME_REVISION_NUMBER; ++dwVersionPartId)
                    {
                        DWORD dwVersionPart;
                        DWORD cbVersionPart = sizeof(dwVersionPart);
                        HRESULT hrVersion = pKillBitAssemblyName->GetProperty(dwVersionPartId, &dwVersionPart, &cbVersionPart);
                        if (FAILED(hrVersion) || cbVersionPart == 0)
                        {
                            validVersion = false;
                        }
                    }
                    if (!validVersion)
                    {
                        continue;
                    }

                    // Make sure there is a simple name
                    DWORD cbNameSize = 0;
                    HRESULT hrName = pKillBitAssemblyName->GetProperty(ASM_NAME_NAME, NULL, &cbNameSize);
                    if (hrName != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                    {
                        continue;
                    }

                    // Verify the killbit assembly has a public key token
                    DWORD cbPublicKeyTokenSize = 0;
                    HRESULT hrPublicKey = pKillBitAssemblyName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, NULL, &cbPublicKeyTokenSize);
                    if (hrPublicKey != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                    {
                        continue;
                    }

                    // Verify the killbit assembly has either no culture or a valid culture token
                    DWORD cbCultureSize = 0;
                    HRESULT hrCulture = pKillBitAssemblyName->GetProperty(ASM_NAME_CULTURE, NULL, &cbCultureSize);
                    if (FAILED(hrCulture) && hrCulture != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                    {
                        continue;
                    }

                    // The name checks out, so add the kill bit entry
                    LOG((LF_SECURITY,
                         LL_INFO10,
                         "APTCA killbit added for assembly '%S'.\n",
                         wszSubKey));
                    pKillBitList->m_killBitList.Append(pKillBitAssemblyName.Extract());
                }
            }
        }

        RETURN(pKillBitList.Extract());
    }

    VolatilePtr<AptcaKillBitList> g_pAptcaKillBitList(NULL);

    //--------------------------------------------------------------------------------------------------------
    //
    // Get the APTCA killbit list
    //

    AptcaKillBitList *GetKillBitList()
    {
        STANDARD_VM_CONTRACT;

        if (g_pAptcaKillBitList.Load() == NULL)
        {
            NewHolder<AptcaKillBitList> pAptcaKillBitList(AptcaKillBitList::ReadMachineKillBitList());

            LPVOID pvOldValue = InterlockedCompareExchangeT(g_pAptcaKillBitList.GetPointer(),
                                                            pAptcaKillBitList.GetValue(),
                                                            NULL);
            if (pvOldValue == NULL)
            {
                pAptcaKillBitList.SuppressRelease();
            }
        }

        _ASSERTE(g_pAptcaKillBitList.Load() != NULL);
        return g_pAptcaKillBitList.Load();
    }
}

// APTCA helper functions
namespace
{
    enum ConditionalAptcaSharingMode
    {
        kShareUnknown,
        kShareIfEnabled,        // Share an assembly only if all conditional APTCA assemblies in its closure are enabled
        kShareIfDisabled,       // Share an assembly only if all conditional APTCA assemblies in its closure are disabled
    };

    //--------------------------------------------------------------------------------------------------------
    //
    // Get the name of an assembly as it would appear in the APTCA enabled list of an AppDomain
    //

    void GetAssemblyNameForConditionalAptca(Assembly *pAssembly, SString *pAssemblyName)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pAssembly));
            PRECONDITION(CheckPointer(pAssemblyName));
        }
        CONTRACTL_END;

        GCX_COOP();

        // Call assembly.GetName().GetNameWithPublicKey() to get the name the user would have to add to the 
        // whitelist to enable this assembly
        struct
        {
            OBJECTREF orAssembly;
            STRINGREF orAssemblyName;
        }
        gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.orAssembly = pAssembly->GetExposedObject();
        MethodDescCallSite getAssemblyName(METHOD__ASSEMBLY__GET_NAME_FOR_CONDITIONAL_APTCA, &gc.orAssembly);
        ARG_SLOT args[1] =
        { 
            ObjToArgSlot(gc.orAssembly)
        };
        gc.orAssemblyName = getAssemblyName.Call_RetSTRINGREF(args);
            
        // Copy to assemblyName
        pAssemblyName->Set(gc.orAssemblyName->GetBuffer());

        GCPROTECT_END();
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine which types of conditional APTCA assemblies may be shared
    //

    ConditionalAptcaSharingMode GetConditionalAptcaSharingMode()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        static ConditionalAptcaSharingMode sharingMode = kShareUnknown;

        if (sharingMode == kShareUnknown)
        {
            // If the default domain has any conditional APTCA assemblies enabled in it, then we share in the
            // enabled direction.  Otherwise, the default domain has all conditional APTCA assemblies disabled
            // so we need to share in the disabled direction
            ConditionalAptcaCache *pDefaultDomainCache = SystemDomain::System()->DefaultDomain()->GetSecurityDescriptor()->GetConditionalAptcaCache();
            ConditionalAptcaCache::DomainState domainState = pDefaultDomainCache->GetConditionalAptcaDomainState();
            
            if (domainState == ConditionalAptcaCache::kAllDisabled)
            {
                sharingMode = kShareIfDisabled;
            }
            else
            {
                sharingMode = kShareIfEnabled;
            }
        }

        return sharingMode;
    }

    /* XXX Fri 7/17/2009
     * I can't call DomainAssembly::IsConditionalAPTCAVisible() here.  That requires an Assembly which means
     * we have to be at FILE_LOAD_ALLOCATE.  There are two problems:
     * 1) We don't want to load dependencies here if we can avoid it
     * 2) We can't load them anyway (hard bound dependencies can't get past
     *      FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES.
     *
     * We're going to do a relaxed check here.  Instead of checking the public key, we're
     * only going to check the public key token.  See
     * code:AppDomain::IsAssemblyOnAptcaVisibleListRaw for more information.
     *
     * pAsmName - The name of the assembly to check.
     * pDomainAssembly - The Domain Assembly used for logging.
     */
    bool IsAssemblyOnAptcaVisibleList(IAssemblyName * pAsmName, DomainAssembly *pDomainAssembly)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pAsmName));
        }
        CONTRACTL_END;

        ConditionalAptcaCache *pDomainCache = pDomainAssembly->GetAppDomain()->GetSecurityDescriptor()->GetConditionalAptcaCache();
        if (pDomainCache->GetConditionalAptcaDomainState() == ConditionalAptcaCache::kAllEnabled)
        {
            return true;
        }

        CQuickBytes qbName;
        LPWSTR pszName;
        DWORD cbName = 0;
        HRESULT hr = pAsmName->GetProperty(ASM_NAME_NAME, NULL, &cbName);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            pszName = (LPWSTR)qbName.AllocThrows(cbName);
        }
        else
        {
            pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting native image / code sharing because there was an ")
                                         W("error checking for conditional APTCA: 0x%x"), hr);
            return false;
        }
        hr = pAsmName->GetProperty(ASM_NAME_NAME, (void *)pszName, &cbName);
        if (FAILED(hr))
        {
            pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting native image / code sharing because there was an ")
                                         W("error checking for conditional APTCA: 0x%x"), hr);
            return false;
        }
        BYTE rgPublicKeyToken[8];
        DWORD cbPkt = _countof(rgPublicKeyToken);
        hr = pAsmName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                                                  (void*)rgPublicKeyToken, &cbPkt);
        if (FAILED(hr))
        {
            pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting native image / code sharing because there was an ")
                                         W("error obtaining the public key token for %s: 0x%x"),
                                         pszName, hr);
            return false;
        }

        GCX_COOP();

        CLR_BOOL isVisible = FALSE;

        struct
        {
            OBJECTREF orThis;
        }
        gc;
        ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);
        gc.orThis = pDomainAssembly->GetAppDomain()->GetExposedObject();

        MethodDescCallSite assemblyVisible(METHOD__APP_DOMAIN__IS_ASSEMBLY_ON_APTCA_VISIBLE_LIST_RAW,
                                           &gc.orThis);
        ARG_SLOT args[] = {
            ObjToArgSlot(gc.orThis),
            (ARG_SLOT)pszName,
            (ARG_SLOT)wcslen(pszName),
            (ARG_SLOT)rgPublicKeyToken,
            (ARG_SLOT)cbPkt
        };
        isVisible = assemblyVisible.Call_RetBool(args);
        GCPROTECT_END();

        return isVisible;
    }

    bool IsAssemblyOnAptcaVisibleList(DomainAssembly *pAssembly)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pAssembly));
            PRECONDITION(GetAppDomain() == pAssembly->GetAppDomain());
        }
        CONTRACTL_END;

        ConditionalAptcaCache *pDomainCache = pAssembly->GetAppDomain()->GetSecurityDescriptor()->GetConditionalAptcaCache();
        if (pDomainCache->GetConditionalAptcaDomainState() == ConditionalAptcaCache::kAllEnabled)
        {
            return true;
        }

        GCX_COOP();

        bool foundInList = false;

        // Otherwise, we need to transition into the BCL code to find out if the assembly is on the list
        struct
        {
            OBJECTREF orAppDomain;
            OBJECTREF orAssembly;
        }
        gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        MethodDescCallSite isAssemblyOnAptcaVisibleList(METHOD__APP_DOMAIN__IS_ASSEMBLY_ON_APTCA_VISIBLE_LIST);
        gc.orAppDomain = GetAppDomain()->GetExposedObject();
        gc.orAssembly = pAssembly->GetAssembly()->GetExposedObject();

        ARG_SLOT args[] =
        { 
            ObjToArgSlot(gc.orAppDomain),
            ObjToArgSlot(gc.orAssembly)
        };

        foundInList = isAssemblyOnAptcaVisibleList.Call_RetBool(args);

        GCPROTECT_END();

        return foundInList;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine if an assembly is APTCA in the current domain or not
    //
    // Arguments:
    //    pDomainAssembly - Assembly to check for APTCA-ness
    //    tokenFlags      - raw metadata security bits from the assembly
    //
    // Return Value:
    //    true if the assembly is APTCA, false if it is not
    //

    bool IsAssemblyAptcaEnabled(DomainAssembly *pDomainAssembly, TokenSecurityDescriptorFlags tokenFlags)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pDomainAssembly));
        }
        CONTRACTL_END;

#ifdef _DEBUG
        SString strAptcaAssemblyBreak(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_AptcaAssemblyBreak));
        SString strAssemblySimpleName(SString::Utf8, pDomainAssembly->GetSimpleName());
        if (strAptcaAssemblyBreak.EqualsCaseInsensitive(strAssemblySimpleName))
        {
            _ASSERTE(!"Checking APTCA-ness of an APTCA break assembly");
        }
#endif // _DEBUG

        // If the assembly is not marked APTCA, then it cannot possibly be APTCA enabled
        if ((tokenFlags & TokenSecurityDescriptorFlags_APTCA) == TokenSecurityDescriptorFlags_None)
        {
            return false;
        }

        GCX_PREEMP();

        // Additionally, if the assembly is on the APTCA kill list, then no matter what it says in its metadata,
        // it should not be considered APTCA
        if (GetKillBitList()->IsAssemblyKillBitted(pDomainAssembly->GetFile()))
        {
            return false;
        }

        // If the assembly is conditionally APTCA, then we need to check the current AppDomain's APTCA enabled
        // list to figure out if it is APTCA in this domain.
        if (tokenFlags & TokenSecurityDescriptorFlags_ConditionalAPTCA)
        {
            return IsAssemblyOnAptcaVisibleList(pDomainAssembly);
        }

        // Otherwise, the assembly is APTCA
        return true;
    }

    //--------------------------------------------------------------------------------------------------------
    //
    // Determine if the assembly matches the conditional APTCA sharing mode.  That is, if we are sharing
    // enabled conditional APTCA assemblies check that this assembly is enabled.  Similarly, if we are
    // sharing disabled conditional APTCA assemblies check that this assembly is disabled.
    // 
    // This method assumes that the assembly is conditionally APTCA
    //

    bool AssemblyMatchesShareMode(IAssemblyName *pAsmName, DomainAssembly *pDomainAssembly)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pAsmName));
            PRECONDITION(GetConditionalAptcaSharingMode() != kShareUnknown);
        }
        CONTRACTL_END;

        if (IsAssemblyOnAptcaVisibleList(pAsmName, pDomainAssembly))
        {
            return GetConditionalAptcaSharingMode() == kShareIfEnabled;
        }
        else
        {
            return GetConditionalAptcaSharingMode() == kShareIfDisabled;
        }
    }

    bool AssemblyMatchesShareMode(ConditionalAptcaCache::State state)
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(state == ConditionalAptcaCache::kEnabled || state == ConditionalAptcaCache::kDisabled);

        if (state == ConditionalAptcaCache::kEnabled)
        {
            return GetConditionalAptcaSharingMode() == kShareIfEnabled;
        }
        else
        {
            return GetConditionalAptcaSharingMode() == kShareIfDisabled;
        }
    }
}

//------------------------------------------------------------------------------------------------------------
//
// Determine if the AppDomain can share an assembly or if APTCA restrictions prevent sharing
// 

bool DomainCanShareAptcaAssembly(DomainAssembly *pDomainAssembly)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pDomainAssembly));
    }
    CONTRACTL_END;

#ifdef _DEBUG
    DWORD dwAptcaAssemblyDomainBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_AptcaAssemblySharingDomainBreak);
    if (dwAptcaAssemblyDomainBreak == 0 || ADID(dwAptcaAssemblyDomainBreak) == pDomainAssembly->GetAppDomain()->GetId())
    {
        SString strAptcaAssemblySharingBreak(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_AptcaAssemblySharingBreak));
        SString strAssemblySimpleName(SString::Utf8, pDomainAssembly->GetSimpleName());

        if (strAptcaAssemblySharingBreak.EqualsCaseInsensitive(strAssemblySimpleName))
        {
            _ASSERTE(!"Checking code sharing for APTCA break assembly");
        }
    }
#endif // _DEBUG

    //
    // We can only share an assembly if all conditional APTCA assemblies in its full closure of dependencies
    // are enabled.
    //

    // We always allow sharing of mscorlib
    if (pDomainAssembly->IsSystem())
    {
        return true;
    }

    IApplicationSecurityDescriptor *pDomainSecDesc = pDomainAssembly->GetAppDomain()->GetSecurityDescriptor();
    ConditionalAptcaCache *pConditionalAptcaCache = pDomainSecDesc->GetConditionalAptcaCache();

    // If all assemblies in the domain match the sharing mode, then we can share the assembly
    ConditionalAptcaCache::DomainState domainState = pConditionalAptcaCache->GetConditionalAptcaDomainState();
    if (GetConditionalAptcaSharingMode() == kShareIfEnabled)
    {
        if (domainState == ConditionalAptcaCache::kAllEnabled)
        {
            return true;
        }
    }
    else
    {
        if (domainState == ConditionalAptcaCache::kAllDisabled)
        {
            return true;
        }
    }

    // If the root assembly is conditionally APTCA, then it needs to be enabled
    ReleaseHolder<IMDInternalImport> pRootImport(pDomainAssembly->GetFile()->GetMDImportWithRef());
    TokenSecurityDescriptorFlags rootSecurityAttributes =
        TokenSecurityDescriptor::ReadSecurityAttributes(pRootImport, TokenFromRid(1, mdtAssembly));
    if (rootSecurityAttributes & TokenSecurityDescriptorFlags_ConditionalAPTCA)
    {
        if (!AssemblyMatchesShareMode(pDomainAssembly->GetFile()->GetFusionAssemblyName(), pDomainAssembly))
        {
            return false;
        }
    }

    // Now we need to get the full closure of assemblies that this assembly depends upon and ensure that each
    // one of those is either not conditional APTCA or is enabled in the domain.  We get a new assembly
    // closure object here rather than using DomainAssembly::GetAssemblyBindingClosure because we don't want
    // to force that closure to walk the full dependency graph (and therefore not be considered equal to
    // closures which weren't fully walked).
    IUnknown *pFusionAssembly;
    if (pDomainAssembly->GetFile()->IsIStream())
    {
        pFusionAssembly = pDomainAssembly->GetFile()->GetIHostAssembly();
    }
    else
    {
        pFusionAssembly = pDomainAssembly->GetFile()->GetFusionAssembly();
    }

    // Get the closure and force it to do a full dependency walk, not stopping at framework assemblies
    SafeComHolder<IAssemblyBindingClosure> pClosure;


    LPCWSTR pNIPath = NULL;
    PEAssembly *pPEAsm = pDomainAssembly->GetFile();
    if (pPEAsm->HasNativeImage())
    {
        ReleaseHolder<PEImage> pNIImage = pPEAsm->GetNativeImageWithRef();
        pNIPath = pNIImage->GetPath().GetUnicode();
    }

    IfFailThrow(pDomainAssembly->GetAppDomain()->GetFusionContext()->GetAssemblyBindingClosure(pFusionAssembly, pNIPath, &pClosure));
    IfFailThrow(pClosure->EnsureWalked(pFusionAssembly, pDomainAssembly->GetAppDomain()->GetFusionContext(), LEVEL_FXPROBED));

    // Now iterate the closure looking for conditional APTCA assemblies
    SafeComHolder<IAssemblyBindingClosureEnumerator> pClosureEnumerator;
    IfFailThrow(pClosure->EnumerateAssemblies(&pClosureEnumerator));
    LPCOLESTR szDependentAssemblyPath = NULL;
    LPCOLESTR szDependentNIAssemblyPath = NULL;

    for (HRESULT hr = pClosureEnumerator->GetNextAssemblyPath(&szDependentAssemblyPath, &szDependentNIAssemblyPath);
         SUCCEEDED(hr);
         hr = pClosureEnumerator->GetNextAssemblyPath(&szDependentAssemblyPath, &szDependentNIAssemblyPath))
    {
        // Make sure we've succesfully enumerated an item
        if (hr != S_OK && hr != HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS))
        {
            pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting code sharing because of an error enumerating dependent assemblies: 0x%x"), hr);
            return false;
        }
        else if (szDependentAssemblyPath == NULL)
        {
            // This means we have an assembly but no way to verify the image at this point -- should we get
            // into this state, we'll be conservative and fail the share
            pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting code sharing because an assembly in the closure does not have a path"));
            return false;
        }
        else
        {
            // We have succesfully found a new item in the closure of assemblies - now check to ensure that
            // it is either not conditionally APTCA or is enabled in tihs domain.
            PEImageHolder pDependentImage;

            // Use the native image if it is loaded. 
            if (szDependentNIAssemblyPath != NULL)
            {
                SString strNIAssemblyPath(szDependentNIAssemblyPath);
                pDependentImage = PEImage::OpenImage(strNIAssemblyPath, MDInternalImport_OnlyLookInCache);
                if (pDependentImage != NULL && !pDependentImage->HasLoadedLayout())
                {
                    pDependentImage = NULL;
                }
                else
                {
#if FEATURE_CORECLR
#error Coreclr needs to check native image version here.
#endif
                }
            }

            if (pDependentImage == NULL)
            {
                SString strAssemblyPath(szDependentAssemblyPath);
                pDependentImage = PEImage::OpenImage(strAssemblyPath);
            }

            // See if we already know if this image is enabled in the current domain or not
            ConditionalAptcaCache::State dependentState = pConditionalAptcaCache->GetCachedState(pDependentImage);

            // We don't know this assembly's conditional APTCA state in this domain, so we need to figure it
            // out now.
            if (dependentState == ConditionalAptcaCache::kUnknown)
            {
                // First figure out if the assembly is even conditionally APTCA to begin with
                IMDInternalImport *pDependentImport = pDependentImage->GetMDImport();
                TokenSecurityDescriptorFlags dependentSecurityAttributes =
                    TokenSecurityDescriptor::ReadSecurityAttributes(pDependentImport, TokenFromRid(1, mdtAssembly));

                if (dependentSecurityAttributes & TokenSecurityDescriptorFlags_ConditionalAPTCA)
                {
                    // The the assembly name of the dependent assembly so we can check it to the domain
                    // enabled list
                    ReleaseHolder<IAssemblyName> pDependentAssemblyName;
                    AssemblySpec dependentAssemblySpec(pDomainAssembly->GetAppDomain());
                    dependentAssemblySpec.InitializeSpec(TokenFromRid(1, mdtAssembly), pDependentImport);
                    IfFailThrow(dependentAssemblySpec.CreateFusionName(&pDependentAssemblyName, FALSE));

                    // Check the domain list to see if the assembly is on it
                    if (IsAssemblyOnAptcaVisibleList(pDependentAssemblyName, pDomainAssembly))
                    {
                        dependentState = ConditionalAptcaCache::kEnabled;
                    }
                    else
                    {
                        dependentState = ConditionalAptcaCache::kDisabled;
                    }
                }
                else
                {
                    // The dependent assembly doesn't have the conditional APTCA bit set on it, so we don't
                    // need to do any checking to see if it's enabled
                    dependentState = ConditionalAptcaCache::kNotCAptca;
                }

                // Cache the result of evaluating conditional APTCA on this assembly in the domain
                pConditionalAptcaCache->SetCachedState(pDependentImage, dependentState);
            }

            // If the dependent assembly does not match the sharing mode, then we cannot share the
            // dependency. We can always share dependencies which are not conditionally APTCA, so don't
            // bother checking the share mode for them.
            if (dependentState != ConditionalAptcaCache::kNotCAptca)
            {
                if (!AssemblyMatchesShareMode(dependentState))
                {
                    pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting code sharing because a dependent assembly did not match the conditional APTCA share mode"));
                    return false;
                }
            }
        }
    }

    // The root assembly and all of its dependents were either on the conditional APTCA list or are not
    // conditional APTCA, so we can share this assembly
    return true;   
}

//------------------------------------------------------------------------------------------------------------
//
// Get an exception string indicating how to enable a conditional APTCA assembly if it was disabled and
// caused an exception
// 

SString GetConditionalAptcaAccessExceptionContext(Assembly *pTargetAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pTargetAssembly));
    }
    CONTRACTL_END;

    SString exceptionContext;

    ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pTargetAssembly);

    if (pMSD->GetTokenFlags() & TokenSecurityDescriptorFlags_ConditionalAPTCA)
    {
        GCX_PREEMP();

        if (!IsAssemblyOnAptcaVisibleList(pTargetAssembly->GetDomainAssembly()))
        {
            // We have a conditional APTCA assembly which is not on the visible list for the current
            // AppDomain, provide information on how to enable it.
            SString assemblyDisplayName;
            pTargetAssembly->GetDisplayName(assemblyDisplayName);

            SString assemblyConditionalAptcaName;
            GetAssemblyNameForConditionalAptca(pTargetAssembly, &assemblyConditionalAptcaName);

            EEException::GetResourceMessage(IDS_ACCESS_EXCEPTION_CONTEXT_CONDITIONAL_APTCA,
                                            exceptionContext,
                                            assemblyDisplayName,
                                            assemblyConditionalAptcaName);
        }
    }

    return exceptionContext;
}

//------------------------------------------------------------------------------------------------------------
//
// Get an exception string indicating that an assembly was on the kill bit list if it caused an exception
//

SString GetAptcaKillBitAccessExceptionContext(Assembly *pTargetAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pTargetAssembly));
    }
    CONTRACTL_END;

    GCX_PREEMP();

    SString exceptionContext;

    if (GetKillBitList()->IsAssemblyKillBitted(pTargetAssembly->GetDomainAssembly()->GetFile()))
    {
        SString assemblyDisplayName;
        pTargetAssembly->GetDisplayName(assemblyDisplayName);

        EEException::GetResourceMessage(IDS_ACCESS_EXCEPTION_CONTEXT_APTCA_KILLBIT,
                                        exceptionContext,
                                        assemblyDisplayName);
    }

    return exceptionContext;
}

//------------------------------------------------------------------------------------------------------------
//
// Determine if a native image is valid to use from the perspective of APTCA.  This means that the image
// itself and all of its dependencies must:
//   1. Not be killbitted
//   2. Be enabled if they are conditionally APTCA
//
// Arguments:
//    pNativeImage    -  native image to accept or reject
//    pDomainAssembly -  assembly that is being loaded
//
// Return Value:
//    true if the native image can be accepted due to APTCA-ness, false if we need to reject it
//

bool NativeImageHasValidAptcaDependencies(PEImage *pNativeImage, DomainAssembly *pDomainAssembly)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pNativeImage));
        PRECONDITION(CheckPointer(pDomainAssembly));
    }
    CONTRACTL_END;

    AptcaKillBitList *pKillBitList = GetKillBitList();

    ConditionalAptcaCache *pDomainCache = pDomainAssembly->GetAppDomain()->GetSecurityDescriptor()->GetConditionalAptcaCache();
    // If we have any killbitted assemblies, then we need to make sure that the current assembly and its dependencies
    BOOL aptcaChecks = pKillBitList->AreAnyAssembliesKillBitted();
    BOOL conditionalAptcaChecks = pDomainCache->GetConditionalAptcaDomainState() != ConditionalAptcaCache::kAllEnabled;
    if (!aptcaChecks && !conditionalAptcaChecks)
        return true;

    //
    // Check to see if the NGEN image itself is APTCA and killbitted
    //

    ReleaseHolder<IMDInternalImport> pAssemblyMD(pDomainAssembly->GetFile()->GetMDImportWithRef());
    TokenSecurityDescriptorFlags assemblySecurityAttributes =
        TokenSecurityDescriptor::ReadSecurityAttributes(pAssemblyMD, TokenFromRid(1, mdtAssembly));

    if (aptcaChecks)
    {
        if ((assemblySecurityAttributes & TokenSecurityDescriptorFlags_APTCA) &&
            pKillBitList->IsAssemblyKillBitted(pDomainAssembly->GetFile()))
        {
            return false;
        }
    }
    if (conditionalAptcaChecks
        && (assemblySecurityAttributes & TokenSecurityDescriptorFlags_ConditionalAPTCA))
    {
        //
        // First check to see if we're disabled.
        //

        AssemblySpec spec;
        spec.InitializeSpec(pDomainAssembly->GetFile());
        ReleaseHolder<IAssemblyName> pAsmName;
        IfFailThrow(spec.CreateFusionName(&pAsmName, FALSE));

        if (!IsAssemblyOnAptcaVisibleList(pAsmName, pDomainAssembly))
        {
            //IsAssemblyOnAptcaVisibleList has already logged an error.
            return false;
        }
    }

    if (aptcaChecks || conditionalAptcaChecks)
    {
        //
        // Also check its dependencies
        //

        COUNT_T dependencyCount;
        PEImageLayout *pNativeLayout = pNativeImage->GetLoadedLayout();
        CORCOMPILE_DEPENDENCY *pDependencies = pNativeLayout->GetNativeDependencies(&dependencyCount);

        for (COUNT_T i = 0; i < dependencyCount; ++i)
        {
            CORCOMPILE_DEPENDENCY* pDependency = &(pDependencies[i]);
            // Look for any dependency which is APTCA
            if (pDependencies[i].dwAssemblyDef != mdAssemblyRefNil)
            {
                AssemblySpec name;
                name.InitializeSpec(pDependency->dwAssemblyRef,
                                    pNativeImage->GetNativeMDImport(),
                                    NULL,
                                    pDomainAssembly->GetFile()->IsIntrospectionOnly());

                ReleaseHolder<IAssemblyName> pDependencyAssemblyName;
                HRESULT hr = name.CreateFusionName(&pDependencyAssemblyName, FALSE);

                // If we couldn't build the assemlby name up conservatively discard the image
                if (FAILED(hr))
                {
                    pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting native image because could not get ")
                                                 W("name for assemblyref 0x%x for native image dependency: ")
                                                 W("hr=0x%x"), pDependency->dwAssemblyRef, hr);
                    return false;
                }

                if (pDependencies[i].dependencyInfo & (CORCOMPILE_DEPENDENCY_IS_APTCA))
                {
                    ULARGE_INTEGER fileVersion;

                    //This is a workaround for Dev10# 743602
                    fileVersion.QuadPart = GET_UNALIGNED_VAL64(&(pDependencies[i].uliFileVersion));
                    // If the dependency really is killbitted, then discard the image
                    if (pKillBitList->IsAssemblyKillBitted(pDependencyAssemblyName, fileVersion))
                    {
                        pDomainAssembly->ExternalLog(LL_ERROR, W("Rejecting native image because dependency ")
                                                     W("assemblyref 0x%x is killbitted."),
                                                     pDependency->dwAssemblyRef);
                        return false;
                    }
                }
                if (pDependencies[i].dependencyInfo & (CORCOMPILE_DEPENDENCY_IS_CAPTCA))
                {
                    if (!IsAssemblyOnAptcaVisibleList(pDependencyAssemblyName, pDomainAssembly))
                    {
                        //IsAssemblyOnAptcaVisibleList has already logged an error.
                        return false;
                    }
                }
            }
        }
    }
    return true;
}
#else // CROSSGEN_COMPILE
namespace
{
    bool IsAssemblyAptcaEnabled(DomainAssembly *pDomainAssembly, TokenSecurityDescriptorFlags tokenFlags)
    {
        // No killbits or conditional APTCA for crossgen. Just check whether the assembly is marked APTCA.
        return ((tokenFlags & TokenSecurityDescriptorFlags_APTCA) != TokenSecurityDescriptorFlags_None);
    }
}
#endif // CROSSGEN_COMPILE

//------------------------------------------------------------------------------------------------------------
//
// Process an assembly's real APTCA flags to determine if the assembly should be considered
// APTCA or not
//
// Arguments:
//    pDomainAssembly - Assembly to check for APTCA-ness
//    tokenFlags      - raw metadata security bits from the assembly
//
// Return Value:
//    updated token security descriptor flags which indicate the assembly's true APTCA state
//

TokenSecurityDescriptorFlags ProcessAssemblyAptcaFlags(DomainAssembly *pDomainAssembly,
                                                       TokenSecurityDescriptorFlags tokenFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDomainAssembly));
    }
    CONTRACTL_END;

    const TokenSecurityDescriptorFlags aptcaFlags = TokenSecurityDescriptorFlags_APTCA |
                                                    TokenSecurityDescriptorFlags_ConditionalAPTCA;

    if (IsAssemblyAptcaEnabled(pDomainAssembly, tokenFlags))
    {
        // The assembly is APTCA - temporarially remove all of its APTCA bits, and then add back the
        // unconditionally APTCA bit
        tokenFlags = tokenFlags & ~aptcaFlags;
        return tokenFlags | TokenSecurityDescriptorFlags_APTCA;
    }
    else
    {
        // The assembly is not APTCA, so remove all of its APTCA bits from the token security descriptor
        return tokenFlags & ~aptcaFlags;
    }
}
