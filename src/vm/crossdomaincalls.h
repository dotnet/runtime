// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: CrossDomainCalls.h
// 

// 
// Purpose: Provides a fast path for cross domain calls.
// 


#ifndef __CROSSDOMAINCALLS_H__
#define __CROSSDOMAINCALLS_H__

#ifndef FEATURE_REMOTING
#error FEATURE_REMOTING is not set, please do no include crossdomaincalls.h
#endif

#include "methodtable.h"

class   SimpleRWLock;

// These are flags set inside the real proxy. Indicates what kind of type is the proxy cast to
// whether its method table layout is equivalent to the server type etc
#define OPTIMIZATION_FLAG_INITTED               0x01000000
#define OPTIMIZATION_FLAG_PROXY_EQUIVALENT      0x02000000
#define OPTIMIZATION_FLAG_PROXY_SHARED_TYPE     0x04000000
#define OPTIMIZATION_FLAG_DEPTH_MASK            0x00FFFFFF

// This struct has info about methods on MBR objects and Interfaces
struct RemotableMethodInfo
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
    /*
    if XAD_BLITTABLE_ARGS is set, (m_OptFlags & XAD_ARG_COUNT_MASK) contains the number of stack dwords to copy
    */
    enum XADOptimizationType
    { 
        XAD_FLAGS_INITIALIZED   = 0x01000000,
        XAD_NOT_OPTIMIZABLE     = 0x02000000,    // Method call has to go through managed remoting path
        XAD_BLITTABLE_ARGS      = 0x04000000,    // Arguments blittable across domains. Could be scalars or agile gc refs
        XAD_BLITTABLE_RET       = 0x08000000,    // Return Value blittable across domains. Could be scalars or agile gc refs
        XAD_BLITTABLE_ALL       = XAD_BLITTABLE_ARGS | XAD_BLITTABLE_RET,

        XAD_RET_FLOAT           = 0x10000000,
        XAD_RET_DOUBLE          = 0x20000000,
#ifdef FEATURE_HFA
        XAD_RET_HFA_TYPE        = 0x40000000,
#endif
        XAD_RET_GC_REF          = 0x70000000,    // To differentiate agile objects like string which can be blitted across domains, but are gc refs
        XAD_RET_TYPE_MASK       = 0x70000000,

        XAD_METHOD_IS_VIRTUAL   = 0x80000000,    // MethodDesc::IsVirtual is slow. Should consider fixing IsVirtual rather than have a flag here
        XAD_ARGS_HAVE_A_FLOAT   = 0x00800000,

        XAD_FLAG_MASK           = 0xFF800000,
        XAD_ARG_COUNT_MASK      = 0x007FFFFF
    } ;

    static XADOptimizationType IsCrossAppDomainOptimizable(MethodDesc *pMeth, DWORD *pNumDwordsToCopy);

    static BOOL TypeIsConduciveToBlitting(MethodTable *pFromMT, MethodTable *pToMT);
        
    static BOOL AreArgsBlittable(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return (enumVal & XAD_BLITTABLE_ARGS) && IsReturnBlittable(enumVal);
    }
    static BOOL IsReturnBlittable(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return enumVal & XAD_BLITTABLE_RET;
    }
    static BOOL IsReturnGCRef(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return XAD_RET_GC_REF == (enumVal & XAD_RET_TYPE_MASK);
    }

    static UINT GetFPReturnSize(XADOptimizationType enumVal)
    {
        WRAPPER_NO_CONTRACT;
        switch (enumVal & XAD_RET_TYPE_MASK)
        {
        case XAD_RET_FLOAT:
            return sizeof(float);

        case XAD_RET_DOUBLE:
            return sizeof(double);

#ifdef FEATURE_HFA
        case XAD_RET_FLOAT | XAD_RET_HFA_TYPE:
            return 4 * sizeof(float);

        case XAD_RET_DOUBLE | XAD_RET_HFA_TYPE:
            return 4 * sizeof(double);
#endif

        default:
            return 0;
        }
    }

    static DWORD GetRetTypeFlagsFromFPReturnSize(UINT fpRetSize)
    {
        LIMITED_METHOD_CONTRACT;

        DWORD flags = 0;
        switch (fpRetSize)
        {
        case 0:
            break;

        case sizeof(float):
            flags = XAD_RET_FLOAT;
            break;

        case sizeof(double):
            flags = XAD_RET_DOUBLE;
            break;

#ifdef FEATURE_HFA
        case 4 * sizeof(float):
            flags = XAD_RET_FLOAT | XAD_RET_HFA_TYPE;
            break;

        case 4 * sizeof(double):
            flags = XAD_RET_DOUBLE | XAD_RET_HFA_TYPE;
            break;
#endif
        default:
            _ASSERTE(false);
            break;
        }

        _ASSERTE(fpRetSize == GetFPReturnSize((XADOptimizationType)flags));

        return flags;
    }

    static BOOL DoArgsContainAFloat(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return enumVal & XAD_ARGS_HAVE_A_FLOAT;
    }

    static BOOL IsCallNotOptimizable(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return enumVal & XAD_NOT_OPTIMIZABLE;
    }
    static BOOL IsMethodVirtual(XADOptimizationType enumVal)
    {
        LIMITED_METHOD_CONTRACT;
        return enumVal & XAD_METHOD_IS_VIRTUAL;
    }

    private:

    static DWORD DoStaticAnalysis(MethodDesc *pMeth);
    
    DWORD       m_OptFlags;
    
} ;

class CrossDomainOptimizationInfo
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
    RemotableMethodInfo m_rmi[0];

    public:

    static SIZE_T SizeOf(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        return SizeOf(pMT->GetNumVtableSlots());
    }

    static SIZE_T SizeOf(DWORD dwNumVtableSlots)
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(CrossDomainOptimizationInfo, m_rmi) + (sizeof(RemotableMethodInfo) * dwNumVtableSlots);
    }

    RemotableMethodInfo *GetRemotableMethodInfo()
    {
        return &(m_rmi[0]);
    }
};

class CrossDomainTypeMap
{
    class MTMapEntry
    {
        public:
            MTMapEntry(AppDomain *pFromDomain, MethodTable *pFromMT, AppDomain *pToDomain, MethodTable *pToMT);
            UPTR GetHash()
            {
                LIMITED_METHOD_CONTRACT;
                DWORD hash = _rotl((UINT)(SIZE_T)m_pFromMT, 1) + m_dwFromDomain.m_dwId;
                hash = _rotl(hash, 1) + m_dwToDomain.m_dwId;
                return (UPTR)hash;
            }
            ADID        m_dwFromDomain;
            ADID        m_dwToDomain;
            MethodTable *m_pFromMT;
            MethodTable *m_pToMT;
    };

    static BOOL                 CompareMTMapEntry (UPTR val1, UPTR val2);
    static PtrHashMap           *s_crossDomainMTMap; // Maps a MT to corresponding MT in another domain
    static SimpleRWLock         *s_MTMapLock;
    static PtrHashMap *         GetTypeMap();

public:
    static MethodTable *GetMethodTableForDomain(MethodTable *pFrom, AppDomain *pFromDomain, AppDomain *pToDomain);
    static void SetMethodTableForDomain(MethodTable *pFromMT, AppDomain *pFromDomain, MethodTable *pToMT, AppDomain *pToDomain);       
    static void FlushStaleEntries();
};

struct MarshalAndCallArgs;
void MarshalAndCall_Wrapper2(MarshalAndCallArgs * pArgs);

class CrossDomainChannel
{
private:
    friend void MarshalAndCall_Wrapper2(MarshalAndCallArgs * pArgs);
    

    BOOL GetTargetAddressFast(DWORD optFlags, MethodTable *pSrvMT, BOOL bFindServerMD);
    BOOL GetGenericMethodAddress(MethodTable *pSrvMT);
    BOOL GetInterfaceMethodAddressFast(DWORD optFlags, MethodTable *pSrvMT, BOOL bFindServerMD);
    BOOL BlitAndCall();
    BOOL MarshalAndCall();
    void MarshalAndCall_Wrapper(MarshalAndCallArgs * pArgs);
    BOOL ExecuteCrossDomainCall();
    VOID RenewLease();
    OBJECTREF GetServerObject();
    BOOL InitServerInfo();
    OBJECTREF ReadPrincipal();
    VOID RestorePrincipal(OBJECTREF *prefPrincipal);
    VOID ResetPrincipal();

public:

    UINT GetFPReturnSize()
    {
        WRAPPER_NO_CONTRACT;
        return RemotableMethodInfo::GetFPReturnSize(m_xret);
    }
    
    BOOL CheckCrossDomainCall(TPMethodFrame *pFrame);

private:
    MethodDesc                          *m_pCliMD;
    MethodDesc                          *m_pSrvMD;
    RemotableMethodInfo::XADOptimizationType    m_xret;
    DWORD                               m_numStackSlotsToCopy;
    OBJECTHANDLE                        m_refSrvIdentity;
    AppDomain                           *m_pCliDomain;
    ADID                                m_pSrvDomain;
    PCODE                               m_pTargetAddress;
    TPMethodFrame                       *m_pFrame;
};

#endif

