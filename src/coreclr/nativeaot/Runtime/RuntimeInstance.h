// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RuntimeInstance_h__
#define __RuntimeInstance_h__

class ThreadStore;
typedef DPTR(ThreadStore) PTR_ThreadStore;
class ICodeManager;
class TypeManager;
enum GenericVarianceType : uint8_t;

#include "ICodeManager.h"

extern "C" void PopulateDebugHeaders();

class RuntimeInstance
{
    friend class AsmOffsets;
    friend struct DefaultSListTraits<RuntimeInstance>;
    friend class Thread;
    friend void PopulateDebugHeaders();

    PTR_ThreadStore             m_pThreadStore;
    HANDLE                      m_hPalInstance; // this is the HANDLE passed into DllMain

public:
    struct OsModuleEntry;
    typedef DPTR(OsModuleEntry) PTR_OsModuleEntry;
    struct OsModuleEntry
    {
        // os Module list is add-only, so we can use PushHeadInterlocked and iterate without synchronization.
        // m_pNext is volatile - to make sure there are no re-reading optimizations when iterating.
        PTR_OsModuleEntry      volatile m_pNext;
        HANDLE                 m_osModule;
    };

    typedef SList<OsModuleEntry> OsModuleList;
private:
    OsModuleList                m_OsModuleList;

    ICodeManager*               m_CodeManager;

    // we support only one code manager for now, so we just record the range.
    void*                       m_pvManagedCodeStartRange;
    uint32_t                    m_cbManagedCodeRange;

public:
    struct TypeManagerEntry
    {
        // TypeManager list is add-only, so we can use PushHeadInterlocked and iterate without synchronization.
        // m_pNext is volatile - to make sure there are no re-reading optimizations when iterating.
        TypeManagerEntry*         volatile m_pNext;
        TypeManager*              m_pTypeManager;
    };

    typedef SList<TypeManagerEntry> TypeManagerList;

private:
    TypeManagerList             m_TypeManagerList;

    bool                        m_conservativeStackReportingEnabled;

    struct  UnboxingStubsRegion
    {
        PTR_VOID                m_pRegionStart;
        uint32_t                  m_cbRegion;
        UnboxingStubsRegion*    m_pNextRegion;

        UnboxingStubsRegion() : m_pRegionStart(0), m_cbRegion(0), m_pNextRegion(NULL) { }
    };

    UnboxingStubsRegion*        m_pUnboxingStubsRegion;

    RuntimeInstance();

    SList<Module>* GetModuleList();

    SList<TypeManager*>* GetModuleManagerList();

    bool BuildGenericTypeHashTable();

    ICodeManager * FindCodeManagerForClasslibFunction(PTR_VOID address);

public:
    ~RuntimeInstance();
    ThreadStore *   GetThreadStore();
    HANDLE          GetPalInstance();

    PTR_uint8_t FindMethodStartAddress(PTR_VOID ControlPC);
    PTR_uint8_t GetTargetOfUnboxingAndInstantiatingStub(PTR_VOID ControlPC);
    void EnableConservativeStackReporting();
    bool IsConservativeStackReportingEnabled() { return m_conservativeStackReportingEnabled; }

    void RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);

    ICodeManager * GetCodeManagerForAddress(PTR_VOID ControlPC);
    PTR_VOID GetClasslibFunctionFromCodeAddress(PTR_VOID address, ClasslibFunctionId functionId);

    bool RegisterTypeManager(TypeManager * pTypeManager);
    TypeManagerList& GetTypeManagerList();
    OsModuleList* GetOsModuleList();

    bool RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange);
    bool IsUnboxingStub(uint8_t* pCode);

    bool IsManaged(PTR_VOID pvAddress);

    static bool Initialize(HANDLE hPalInstance);
    void Destroy();

    bool ShouldHijackCallsiteForGcStress(uintptr_t CallsiteIP);
    bool ShouldHijackLoopForGcStress(uintptr_t CallsiteIP);
};
typedef DPTR(RuntimeInstance) PTR_RuntimeInstance;


PTR_RuntimeInstance GetRuntimeInstance();

#endif // __RuntimeInstance_h__
