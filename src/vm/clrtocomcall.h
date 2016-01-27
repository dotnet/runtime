// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: CLRtoCOMCall.h 
//

// 
// Used to handle stub creation for managed to unmanaged transitions.
// 


#ifndef __COMPLUSCALL_H__
#define __COMPLUSCALL_H__

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "util.hpp"

class ComPlusCall
{
    public:
        //---------------------------------------------------------
        // Debugger helper function
        //---------------------------------------------------------
        static TADDR GetFrameCallIP(FramedMethodFrame *frame);

        static MethodDesc* GetILStubMethodDesc(MethodDesc* pMD, DWORD dwStubFlags);
        static PCODE       GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD);

        static ComPlusCallInfo *PopulateComPlusCallMethodDesc(MethodDesc* pMD, DWORD* pdwStubFlags);
        static MethodDesc *GetWinRTFactoryMethodForCtor(MethodDesc *pMDCtor, BOOL *pComposition);
        static MethodDesc *GetWinRTFactoryMethodForStatic(MethodDesc *pMDStatic);

#ifdef _TARGET_X86_
        static void Init();
        static LPVOID GetRetThunk(UINT numStackBytes);
#endif // _TARGET_X86_
    private:
        ComPlusCall();     // prevent "new"'s on this class

#ifdef _TARGET_X86_
    struct RetThunkCacheElement
    {
        RetThunkCacheElement()
        {
            LIMITED_METHOD_CONTRACT;
            m_cbStack = 0;
            m_pRetThunk = NULL;
        }

        UINT m_cbStack;
        LPVOID m_pRetThunk;
    };

    class RetThunkSHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<RetThunkCacheElement> >
    {
    public:
        typedef UINT key_t;
        static key_t GetKey(element_t e)       { LIMITED_METHOD_CONTRACT; return e.m_cbStack; }
        static BOOL Equals(key_t k1, key_t k2) { LIMITED_METHOD_CONTRACT; return (k1 == k2); }
        static count_t Hash(key_t k)           { LIMITED_METHOD_CONTRACT; return (count_t)(size_t)k; }
        static const element_t Null()          { LIMITED_METHOD_CONTRACT; return RetThunkCacheElement(); }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return (e.m_pRetThunk == NULL); }
    };

    static SHash<RetThunkSHashTraits> *s_pRetThunkCache;
    static CrstStatic s_RetThunkCacheCrst;
#endif // _TARGET_X86_
};

#endif // __COMPLUSCALL_H__
