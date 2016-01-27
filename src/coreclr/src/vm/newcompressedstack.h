// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


//

#ifndef __newcompressedstack_h__
#define __newcompressedstack_h__
#ifdef FEATURE_COMPRESSEDSTACK

#include "objectlist.h"
// Returns true if the low bit in the ptr argument is set to 1
#define IS_LOW_BIT_SET(ptr) (((UINT_PTR)ptr) & 1)
// Sets the low bit in the ptr passed in
#define SET_LOW_BIT(ptr) (((UINT_PTR)ptr)|1)
// Reset the low bit to 0
#define UNSET_LOW_BIT(ptr) (((UINT_PTR)ptr)& ~((size_t)1))

class DomainCompressedStack;
class NewCompressedStack;



// This is the class that will contain an array of entries.
// All the entries will be for a single AppDomain
class DomainCompressedStack
{
    friend class NewCompressedStack;
public:
    ADID GetMyDomain()
    {
        // It is OK if m_DomainID gets set to -1 by ADU code and we return a valid AD.
        // (what that means is that tmp_adid is not invalid, but m_DomainID is set to invalid by ADU code
        // after we cached it)
        // Two cases:
        // 1. AD has set NoEnter
        // 1.a) current thread is finalizer: it will be allowed to enter the AD, but Destroy() takes a lock and checks again. So we're good
        // 1.b) current thread is not finalizer: it will not be allowed to enter AD and the value returned here will be NULL
        // 2. AD has not set NoEnter, but is in the process of CS processing at ADU
        //    A valid AD pointer is returned, which is all that this function is required to do. Since ADU unload is done handling this DCS, we'll not 
        //    enter that AD, but use the blob in the DCS.
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            SO_TOLERANT;
        } 
        CONTRACTL_END;

        return m_DomainID;
    }
    // Construction and maintenence
    DomainCompressedStack(ADID domainID); //ctor
    BOOL IsAssemblyPresent(ISharedSecurityDescriptor* ssd);
    void AddEntry(void *ptr);
#ifndef DACCESS_COMPILE
    void AddFrameEntry(AppDomain * pAppDomain, FRAMESECDESCREF fsdRef, BOOL bIsAHDMFrame, OBJECTREF dynamicResolverRef);
#endif
    void Destroy(void);

    // Demand evaluation
    static FCDECL1(DWORD, GetDescCount, DomainCompressedStack* dcs);
    static FCDECL3(void, GetDomainPermissionSets, DomainCompressedStack* dcs, OBJECTREF* ppGranted, OBJECTREF* ppDenied);
    static FCDECL6(FC_BOOL_RET, GetDescriptorInfo, DomainCompressedStack* dcs, DWORD index, OBJECTREF* ppGranted, OBJECTREF* ppDenied, OBJECTREF* ppAssembly, OBJECTREF* ppFSD);
    static FCDECL1(FC_BOOL_RET, IgnoreDomain, DomainCompressedStack* dcs);
    OBJECTREF GetDomainCompressedStackInternal(AppDomain *pDomain);

    // AppDomain unload
    void ClearDomainInfo(void);
    static void AllHandleAppDomainUnload(AppDomain* pDomain, ADID domainId, ObjectList* list );
    static void ReleaseDomainCompressedStack( DomainCompressedStack* dcs ) { 
        WRAPPER_NO_CONTRACT;
        dcs->Destroy(); 
    };
    static DomainCompressedStack* GetNextEntryFromADList(AppDomain* pDomain, ObjectList::Iterator iter);
    void AppDomainUnloadDone(AppDomain* pDomain);



private:
    ArrayList m_EntryList;
    ADID m_DomainID; // either a valid domain ID or INVALID_APPDOMAIN_ID (set by unloading AppDomain to that value)
    BOOL m_ignoreAD; // Do not look at domain grant set since we have a CS at the threadbaseobject.
    DWORD m_dwOverridesCount;    
    DWORD m_dwAssertCount;
    BOOL m_Homogeneous;
    
    DWORD GetOverridesCount( void )
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwOverridesCount;
    }
    
    DWORD GetAssertCount( void )
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwAssertCount;
    }

    BOOL IgnoreDomainInternal();
};
typedef Holder<DomainCompressedStack*, DoNothing< DomainCompressedStack* >, DomainCompressedStack::ReleaseDomainCompressedStack > DomainCompressedStackHolder;

class NewCompressedStack
{
    
private:
        DomainCompressedStack** m_DCSList;
        DWORD m_DCSListCount;
        DomainCompressedStack *m_currentDCS;
        Frame *m_pCtxTxFrame; // Be super careful where you use this. Remember that this is a stack location and is not always valid.
        ADID m_CSAD;
        AppDomainStack m_ADStack;
        DWORD adStackIndex;
        DWORD m_dwOverridesCount;
        DWORD m_dwAssertCount;

        
        void CreateDCS(ADID domainID);
        BOOL IsAssemblyPresent(ADID domainID, ISharedSecurityDescriptor* pSsd);
        BOOL IsDCSContained(DomainCompressedStack *pDCS);
        BOOL IsNCSContained(NewCompressedStack *pCS);
        void ProcessSingleDomainNCS(NewCompressedStack *pCS, AppDomain* pAppDomain);
public:
    DWORD GetDCSListCount(void)
    {
        // Returns # of non-NULL DCSList entries;
        LIMITED_METHOD_CONTRACT;
        return m_DCSListCount;
    }
    void Destroy( CLR_BOOL bEntriesOnly = FALSE);

    static void DestroyCompressedStack( NewCompressedStack* stack ) { 
        WRAPPER_NO_CONTRACT;
        stack->Destroy(); 
    };
    
    AppDomainStack& GetAppDomainStack( void )
    {
        LIMITED_METHOD_CONTRACT;
        return m_ADStack;
    }

    DWORD GetOverridesCount( void )
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwOverridesCount;
    }
    
    DWORD GetAssertCount( void )
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwAssertCount;
    }
    DWORD GetInnerAppDomainOverridesCount(void)
    {
        WRAPPER_NO_CONTRACT;
        return m_ADStack.GetInnerAppDomainOverridesCount();
    }
    DWORD GetInnerAppDomainAssertCount(void)
    {
        WRAPPER_NO_CONTRACT;
        return m_ADStack.GetInnerAppDomainAssertCount();
    }
    
    // This is called every time we hit a stack frame on a stack walk. it will be called with ASD, SSD, FSDs and this func will determine what
    // (if any) action needs to be performed. 
    // For example:
    // on seeing a new SSD, we'll add an entry to the current DCS
    // on seeing an SSD we've already seen, we'll do nothing
    // on seeing a new ASD, a new DCS will be created on this CS
#ifndef DACCESS_COMPILE
    void ProcessAppDomainTransition(void);
    DWORD ProcessFrame(AppDomain* pAppDomain, Assembly* pAssembly, MethodDesc* pFunc, ISharedSecurityDescriptor* pSsd, FRAMESECDESCREF* pFsdRef);
    void ProcessCS(AppDomain* pAppDomain, COMPRESSEDSTACKREF csRef, Frame *pFrame);

#endif 
    // ctor
    NewCompressedStack();
    OBJECTREF GetCompressedStackInner();

    // FCALLS
    static FCDECL1(DWORD, FCallGetDCSCount, SafeHandle* hcsUNSAFE);
    static FCDECL2(FC_BOOL_RET, FCallIsImmediateCompletionCandidate, SafeHandle* hcsUNSAFE, OBJECTREF *innerCS);
    static FCDECL2(Object*, GetDomainCompressedStack, SafeHandle* hcsUNSAFE, DWORD index);
    static FCDECL1(void, DestroyDCSList, SafeHandle* hcsUNSAFE);
    static FCDECL1(void, FCallGetHomogeneousPLS, Object* hgPLSUnsafe);

};
typedef Holder<NewCompressedStack*, DoNothing< NewCompressedStack* >, NewCompressedStack::DestroyCompressedStack > NewCompressedStackHolder;
#endif // #ifdef FEATURE_COMPRESSEDSTACK
#endif /* __newcompressedstack_h__ */


