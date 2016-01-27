// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: mixedmode.H
// 

//
// MIXEDMODE.H defines classes to support mixed mode dlls
// ===========================================================================


#ifndef _MIXEDMODE_H_
#define _MIXEDMODE_H_

#ifdef FEATURE_MIXEDMODE

#ifdef _TARGET_X86_ 
extern "C" VOID __stdcall IJWNOADThunkJumpTarget(void);
#endif


#define IJWNOADThunkStubCacheSize 4

struct IJWNOADThunkStubCache
{
    ADID    m_AppDomainID;  // Must be the first member of the struct.
    LPCVOID m_CodeAddr;
};



// Be sure to keep this structure and the assembly view in sync
class IJWNOADThunk
{
    UMEntryThunkCode m_code;

protected:
    static void __cdecl MakeCall();
    static void SafeNoModule();
    static void NoModule();

    HMODULE   m_pModulebase;
    DWORD   m_dwIndex;
    mdToken m_Token;

    DWORD   m_fAccessingCache;
    
public:
    IJWNOADThunkStubCache m_cache[IJWNOADThunkStubCacheSize];

    BOOL IsCachedAppDomainID(ADID pID)
    {
        LIMITED_METHOD_CONTRACT;

        for (int i=0; i < IJWNOADThunkStubCacheSize; i++)
        {
            if (m_cache[i].m_AppDomainID == pID)
                return TRUE;
        }
        
        return FALSE;
    };

    void GetCachedInfo(ADID pID, LPCVOID* pCode)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            INSTANCE_CHECK;
            SO_TOLERANT;
            PRECONDITION(CheckPointer(pCode));
        }
        CONTRACTL_END;

        *pCode = NULL;
        
        for (int i=0; i < IJWNOADThunkStubCacheSize; i++)
        {
            if (m_cache[i].m_AppDomainID == pID)
                *pCode = m_cache[i].m_CodeAddr;
        }
    }

    void SetCachedInfo(ADID pID, LPCVOID pCode)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INSTANCE_CHECK;
            SO_INTOLERANT;
        }
        CONTRACTL_END;
        
        YIELD_WHILE (FastInterlockCompareExchange((LONG*)&m_fAccessingCache, 1, 0) != 0);

        // Don't cache if the cache is already full.
        for (int i=0; i < IJWNOADThunkStubCacheSize; i++)
        {
            if (m_cache[i].m_AppDomainID == (ADID)-1)
            {
                m_cache[i].m_CodeAddr = pCode;
                MemoryBarrier();
                m_cache[i].m_AppDomainID = pID;
            }
        }

        m_fAccessingCache = 0;
    }

    static IJWNOADThunk* FromCode(PCODE pCodeAddr)
    {
        LIMITED_METHOD_CONTRACT;
        return (IJWNOADThunk*)(PCODEToPINSTR(pCodeAddr) - offsetof(IJWNOADThunk, m_code) - UMEntryThunkCode::GetEntryPointOffset());
    };
    mdToken GetToken()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Token;
    }

    IJWNOADThunk(HMODULE pModulebase, DWORD dwIndex, mdToken Token);

    LPCBYTE GetCode()
    {
        WRAPPER_NO_CONTRACT;
        return m_code.GetEntryPoint();
    }

    LPCVOID IJWNOADThunk::FindThunkTarget();
};

#endif //FEATURE_MIXEDMODE

#endif // _MIXEDMODE_H_
