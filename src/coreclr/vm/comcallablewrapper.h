// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  Com Callable wrapper  classes
**

===========================================================*/

#ifndef _COMCALLABLEWRAPPER_H
#define _COMCALLABLEWRAPPER_H

#ifdef FEATURE_COMINTEROP

#include "vars.hpp"
#include "stdinterfaces.h"
#include "threads.h"
#include "comutilnative.h"
#include "spinlock.h"
#include "comtoclrcall.h"
#include "dispatchinfo.h"
#include "wrappers.h"
#include "internalunknownimpl.h"
#include "util.hpp"

class CCacheLineAllocator;
class ConnectionPoint;
class MethodTable;
class ComCallWrapper;
struct SimpleComCallWrapper;
class RCWHolder;
struct ComMethodTable;

typedef DPTR(struct SimpleComCallWrapper) PTR_SimpleComCallWrapper;

// Terminator to indicate that indicates the end of a chain of linked wrappers.
#define LinkedWrapperTerminator (PTR_ComCallWrapper)-1

class ComCallWrapperCache
{
public:
    // Encapsulate a SpinLockHolder, so that clients of our lock don't have to know
    // the details of our implementation.
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(ComCallWrapperCache *pCache)
            : CrstHolder(&pCache->m_lock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    ComCallWrapperCache();
    ~ComCallWrapperCache();

    // create a new WrapperCache (one per each LoaderAllocator)
    static ComCallWrapperCache* Create(LoaderAllocator *pLoaderAllocator);

    // refcount
    LONG    AddRef();
    LONG    Release();

    CCacheLineAllocator* GetCacheLineAllocator()
    {
        CONTRACT (CCacheLineAllocator*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN m_pCacheLineAllocator;
    }

    LoaderAllocator* GetLoaderAllocator()
    {
        CONTRACT (LoaderAllocator*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pLoaderAllocator;
    }

private:
    LONG                    m_cbRef;
    CCacheLineAllocator*    m_pCacheLineAllocator;
    LoaderAllocator*        m_pLoaderAllocator;

    // spin lock for fast synchronization
    Crst                    m_lock;
};


//---------------------------------------------------------------------------------
// COM called wrappers on CLR objects
//  Purpose: Expose CLR objects as COM classic Interfaces
//  Reqmts:  Wrapper has to have the same layout as the COM2 interface
//
//  The wrapper objects are aligned at 16 bytes, and the original this
//  pointer is replicated every 16 bytes, so for any COM2 interface
//  within the wrapper, the original 'this' can be obtained by masking
//  low 4 bits of COM2 IP.
//
//           16 byte aligned                            COM2 Vtable
//           +-----------+
//           | Org. this |
//           +-----------+                              +-----+
// COM2 IP-->| VTable ptr|----------------------------->|slot1|
//           +-----------+           +-----+            +-----+
// COM2 IP-->| VTable ptr|---------->|slot1|            |slot2|
//           +-----------+           +-----+            +     +
//           | VTable ptr|           | ....|            | ... |
//           +-----------+           +     +            +     +
//           | Org. this |           |slotN|            |slotN|
//           +           +           +-----+            +-----+
//           |  ....     |
//           +           +
//           |  |
//           +-----------+
//
//
//  The first slot of the first CCW is used to hold the basic interface -
//   an interface that implements the methods of IUnknown & IDispatch.  The basic
//   interface's IDispatch implementation will call through to the class methods
//   as if it were the class interface.
//
//  The second slot of the first CCW is used to hold the IClassX interface -
//   an interface that implements IUnknown, IDispatch, and a custom interface
//   that contains all of the members of the class and its hierarchy.  This
//   will only be generated on demand and is only usable if the class and all
//   of its parents are visible to COM.
//
//  VTable and Stubs: can share stub code, we need to have different vtables
//                    for different interfaces, so the stub can jump to different
//                    marshalling code.
//  Stubs : adjust this pointer and jump to the approp. address,
//  Marshalling params and results, based on the method signature the stub jumps to
//  approp. code to handle marshalling and unmarshalling.
//
//
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// COM callable wrappers for CLR objects
//--------------------------------------------------------------------------------
typedef DPTR(class ComCallWrapperTemplate) PTR_ComCallWrapperTemplate;
class ComCallWrapperTemplate
{
    friend class ClrDataAccess;

public:
    // Small "L1" cache to speed up QI's on CCWs with variance. It caches both positive and negative
    // results (i.e. also keeps track of IIDs that the QI doesn't respond to).
    class IIDToInterfaceTemplateCache
    {
        enum
        {
            // There is also some number of IIDs QI'ed for by external code that we won't
            // recognize - this number is potentially unbounded so even if this was a different data
            // structure, we would want to limit its size. Simple sequentially searched array seems to
            // work the best both in terms of memory footprint and lookup performance.
            CACHE_SIZE = 16,
        };

        struct CacheItem
        {
            IID          m_iid;

            // The lowest bit indicates whether this item is being used (since NULL is a legal value).
            // The second lowest bit indicates whether the item has been accessed since the last eviction.
            // The rest of the bits contain ComCallWrapperTemplate pointer.
            SIZE_T       m_pTemplate;

            bool IsFree()
            {
                LIMITED_METHOD_CONTRACT;
                return (m_pTemplate == 0);
            }

            bool IsHot()
            {
                LIMITED_METHOD_CONTRACT;
                return ((m_pTemplate & 0x2) == 0x2);
            }

            ComCallWrapperTemplate *GetTemplate()
            {
                LIMITED_METHOD_CONTRACT;
                return (ComCallWrapperTemplate *)(m_pTemplate & ~0x3);
            }

            void SetTemplate(ComCallWrapperTemplate *pTemplate)
            {
                LIMITED_METHOD_CONTRACT;
                m_pTemplate = ((SIZE_T)pTemplate | 0x1);
            }

            void MarkHot()
            {
                LIMITED_METHOD_CONTRACT;
                m_pTemplate |= 0x2;
            }

            void MarkCold()
            {
                LIMITED_METHOD_CONTRACT;
                m_pTemplate &= ~0x2;
            }
        };

        // array of cache items
        CacheItem m_items[CACHE_SIZE];

        // spin lock to protect concurrent access to m_items
        SpinLock  m_lock;

    public:
        IIDToInterfaceTemplateCache()
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
            }
            CONTRACTL_END;

            ZeroMemory(this, sizeof(IIDToInterfaceTemplateCache));
            m_lock.Init(LOCK_TYPE_DEFAULT);
        }

        bool LookupInterfaceTemplate(REFIID riid, ComCallWrapperTemplate **ppTemplate);
        void InsertInterfaceTemplate(REFIID riid, ComCallWrapperTemplate *pTemplate);
    };

    // Iterates COM-exposed interfaces of a class.
    class CCWInterfaceMapIterator
    {
    private:
        struct InterfaceProps
        {
            MethodTable *m_pItfMT;
        };

        StackSArray<InterfaceProps> m_Interfaces;
        COUNT_T m_Index;

        inline const InterfaceProps &GetCurrentInterfaceProps() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_Interfaces[(COUNT_T)m_Index];
        }

        InterfaceProps &AppendInterface(MethodTable *pItfMT);

    public:
        CCWInterfaceMapIterator(TypeHandle thClass);

        BOOL Next()
        {
            LIMITED_METHOD_CONTRACT;
            return (++m_Index < GetCount());
        }

        MethodTable *GetInterface() const
        {
            LIMITED_METHOD_CONTRACT;
            return GetCurrentInterfaceProps().m_pItfMT;
        }

        DWORD GetIndex() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_Index;
        }

        DWORD GetCount() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_Interfaces.GetCount();
        }

        void Reset()
        {
            LIMITED_METHOD_CONTRACT;
            m_Index = (COUNT_T)-1;
        }
    };

    // Static initializer run at startup.
    static void Init();

    // Template accessor, creates a template if one is not already cached.
    static ComCallWrapperTemplate* GetTemplate(TypeHandle thClass);

    // Ref-count the templates
    LONG AddRef();
    LONG Release();

    // Properties
    ComMethodTable* GetClassComMT();
    ComMethodTable* GetComMTForItf(MethodTable *pItfMT);
    ComMethodTable* GetBasicComMT();
    ULONG           GetNumInterfaces();
    SLOT*           GetVTableSlot(ULONG index);
    void            CheckParentComVisibility(BOOL fForIDispatch);
    BOOL            CheckParentComVisibilityNoThrow(BOOL fForIDispatch);

    // Calls GetDefaultInterfaceForClassInternal and caches the result.
    DefaultInterfaceType GetDefaultInterface(MethodTable **ppDefaultItf);

    // Sets up the class method table for the IClassX and also lays it out.
    static ComMethodTable *SetupComMethodTableForClass(MethodTable *pMT, BOOL bLayOutComMT);

    MethodDesc * GetICustomQueryInterfaceGetInterfaceMD();

    IIDToInterfaceTemplateCache *GetOrCreateIIDToInterfaceTemplateCache();

    BOOL HasInvisibleParent()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_InvisibleParent);
    }

    BOOL SupportsICustomQueryInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_ImplementsICustomQueryInterface);
    }

    BOOL RepresentsVariantInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_RepresentsVariantInterface);
    }

    BOOL IsUseOleAutDispatchImpl()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_UseOleAutDispatchImpl);
    }

    BOOL ImplementsIMarshal()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_ImplementsIMarshal);
    }

    BOOL SupportsIClassX()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_SupportsIClassX);
    }

    TypeHandle GetClassType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_thClass;
    }

    BOOL IsSafeTypeForMarshalling();

    // Creates a new Template and caches it on the MethodTable or class factory.
    static ComCallWrapperTemplate *CreateTemplate(TypeHandle thClass);

    // Creates a new Template for just one interface. Used for lazily created CCWs for interfaces with variance.
    static ComCallWrapperTemplate *CreateTemplateForInterface(MethodTable *pItfMT);

private:

    // Hide the constructor
    ComCallWrapperTemplate();

    // Cleanup called when the ref-count hits zero.
    void Cleanup();

    // Helper method called by code:CreateTemplate.
    ComMethodTable* InitializeForInterface(MethodTable *pParentMT, MethodTable *pItfMT, DWORD dwIndex);

    // Create a non laid out COM method table for the specified class or interface.
    ComMethodTable* CreateComMethodTableForClass(MethodTable *pClassMT);
    ComMethodTable* CreateComMethodTableForInterface(MethodTable* pInterfaceMT);
    ComMethodTable* CreateComMethodTableForBasic(MethodTable* pClassMT);

    void DetermineComVisibility();
    ComCallWrapperTemplate* FindInvisibleParent();

private:
    LONG                                    m_cbRefCount;
    ComCallWrapperTemplate*                 m_pParent;
    TypeHandle                              m_thClass;
    MethodTable*                            m_pDefaultItf;
    ComMethodTable*                         m_pClassComMT;
    ComMethodTable*                         m_pBasicComMT;

    enum
    {
        // first 3 bits are interpreted as DefaultInterfaceType
        enum_DefaultInterfaceTypeMask         = 0x7,
        enum_DefaultInterfaceTypeComputed     = 0x10,

        enum_InvisibleParent                  = 0x20,
        enum_ImplementsICustomQueryInterface  = 0x40,
        // enum_Unused                        = 0x80,
        enum_SupportsIClassX                  = 0x100,

        enum_RepresentsVariantInterface       = 0x400, // this is a template for an interface with variance

        enum_UseOleAutDispatchImpl            = 0x800, // the class is decorated with IDispatchImplAttribute(CompatibleImpl)

        enum_ImplementsIMarshal               = 0x1000, // the class implements a managed interface with Guid == IID_IMarshal

        enum_IsSafeTypeForMarshalling         = 0x2000, // The class can be safely marshalled out of process via DCOM
    };
    DWORD                                   m_flags;
    MethodDesc*                             m_pICustomQueryInterfaceGetInterfaceMD;
    Volatile<IIDToInterfaceTemplateCache *> m_pIIDToInterfaceTemplateCache;
    ULONG                                   m_cbInterfaces;
    SLOT*                                   m_rgpIPtr[1];
};

inline void ComCallWrapperTemplateRelease(ComCallWrapperTemplate *value)
{
    WRAPPER_NO_CONTRACT;

    if (value)
    {
        value->Release();
    }
}

typedef Wrapper<ComCallWrapperTemplate *, DoNothing<ComCallWrapperTemplate *>, ComCallWrapperTemplateRelease, NULL> ComCallWrapperTemplateHolder;


//--------------------------------------------------------------------------------
// Header on top of Vtables that we create for COM callable interfaces
//--------------------------------------------------------------------------------
#pragma pack(push)
#pragma pack(1)

struct IUnkVtable
{
    SLOT          m_qi; // IUnk::QI
    SLOT          m_addref; // IUnk::AddRef
    SLOT          m_release; // IUnk::Release
};

struct IDispatchVtable : IUnkVtable
{
    // idispatch methods
    SLOT        m_GetTypeInfoCount;
    SLOT        m_GetTypeInfo;
    SLOT        m_GetIDsOfNames;
    SLOT        m_Invoke;
};

enum Masks
{
    enum_InterfaceTypeMask              = 0x00000003,
    enum_ClassInterfaceTypeMask         = 0x00000003,
    enum_ClassVtableMask                = 0x00000004,
    enum_LayoutComplete                 = 0x00000010,
    enum_ComVisible                     = 0x00000040,
    // enum_unused                      = 0x00000080,
    // enum_unused                      = 0x00000100,
    enum_ComClassItf                    = 0x00000200,
    enum_GuidGenerated                  = 0x00000400,
    // enum_unused                      = 0x00001000,
    enum_IsBasic                        = 0x00002000,
    // enum_unused                      = 0x00004000,
    // enum_unused                      = 0x00008000,
    // enum_unused                      = 0x00010000,
    // enum_unused                      = 0x00020000,
    // enum_unused                      = 0x00040000,
};

typedef DPTR(struct ComMethodTable) PTR_ComMethodTable;
struct ComMethodTable
{
    friend class ComCallWrapperTemplate;

    // Cleanup, frees all the stubs and the vtable
    void Cleanup();

    // The appropriate finalize method must be called before the COM method table is
    // exposed to COM or before any methods are called on it.
    void LayOutClassMethodTable();
    BOOL LayOutInterfaceMethodTable(MethodTable* pClsMT);
    void LayOutBasicMethodTable();

    // Accessor for the IDispatch information.
    DispatchInfo* GetDispatchInfo();

    LONG AddRef()
    {
        LIMITED_METHOD_CONTRACT;

        ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable));
        return InterlockedIncrement(&comMTWriterHolder.GetRW()->m_cbRefCount);
    }

    LONG Release()
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(m_cbRefCount > 0);
        }
        CONTRACTL_END;

        ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable));
        // use a different var here becuase cleanup will delete the object
        // so can no longer make member refs
        LONG cbRef = InterlockedDecrement(&comMTWriterHolder.GetRW()->m_cbRefCount);
        if (cbRef == 0)
            Cleanup();

        return cbRef;
    }

    CorIfaceAttr GetInterfaceType()
    {
        WRAPPER_NO_CONTRACT;

        if (IsIClassXOrBasicItf())
            return ifDual;
        else
            return (CorIfaceAttr)(m_Flags & enum_InterfaceTypeMask);
    }

    CorClassIfaceAttr GetClassInterfaceType()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsIClassXOrBasicItf());
        return (CorClassIfaceAttr)(m_Flags & enum_ClassInterfaceTypeMask);
    }

    BOOL IsIClassX()
    {
        LIMITED_METHOD_CONTRACT;
        return (IsIClassXOrBasicItf() && !IsBasic());
    }

    BOOL IsIClassXOrBasicItf()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_ClassVtableMask) != 0;
    }

    BOOL IsComClassItf()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_ComClassItf) != 0;
    }

    BOOL IsLayoutComplete()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_LayoutComplete) != 0;
    }

    BOOL IsComVisible()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_ComVisible) != 0;
    }

    BOOL IsBasic()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsBasic) != 0;
    }

    BOOL HasInvisibleParent()
    {
        LIMITED_METHOD_CONTRACT;
        return ((ComCallWrapperTemplate*)m_pMT->GetComCallWrapperTemplate())->HasInvisibleParent();
    }

    DWORD GetSlots()
    {
        LIMITED_METHOD_CONTRACT;
        return m_cbSlots;
    }

    ITypeInfo *GetITypeInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pITypeInfo;
    }

    void SetITypeInfo(ITypeInfo *pITI);

    static WORD GetNumExtraSlots(CorIfaceAttr ItfType)
    {
        LIMITED_METHOD_CONTRACT;

        switch (ItfType)
        {
            case ifVtable:      return (sizeof(IUnkVtable) / sizeof(SLOT));
            default:            return (sizeof(IDispatchVtable) / sizeof(SLOT));
        }
    }

    // Gets the ComCallMethodDesc out of a Vtable slot correctly for all platforms
    ComCallMethodDesc* ComCallMethodDescFromSlot(unsigned i);

    BOOL IsSlotAField(unsigned i)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(IsLayoutComplete());
            PRECONDITION(i < m_cbSlots);
        }
        CONTRACTL_END;

        i += GetNumExtraSlots(GetInterfaceType());
        ComCallMethodDesc* pCMD = ComCallMethodDescFromSlot(i);
        return pCMD->IsFieldCall();
    }

    MethodDesc* GetMethodDescForSlot(unsigned i)
    {
        CONTRACT (MethodDesc*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(IsLayoutComplete());
            PRECONDITION(i < m_cbSlots);
            PRECONDITION(!IsSlotAField(i));
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        i += GetNumExtraSlots(GetInterfaceType());

        ComCallMethodDesc* pCMD;

        pCMD = ComCallMethodDescFromSlot(i);
        _ASSERTE(pCMD->IsMethodCall());

        RETURN pCMD->GetMethodDesc();
    }

    ComCallMethodDesc* GetFieldCallMethodDescForSlot(unsigned i)
    {
        CONTRACT (ComCallMethodDesc*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(IsLayoutComplete());
            PRECONDITION(i < m_cbSlots);
            PRECONDITION(IsSlotAField(i));
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        i += GetNumExtraSlots(GetInterfaceType());
        ComCallMethodDesc* pCMD = ComCallMethodDescFromSlot(i);

        _ASSERTE(pCMD->IsFieldCall());
        RETURN (ComCallMethodDesc *)pCMD;
    }

    BOOL OwnedbyThisMT(unsigned slotIndex)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACTL_END;

        if (!IsIClassXOrBasicItf())
            return TRUE;

        if (m_pMDescr != NULL)
        {
            // These are the methods from the default interfaces such as IUnknown.
            unsigned cbExtraSlots = GetNumExtraSlots(GetInterfaceType());

            // Refer to ComMethodTable::LayOutClassMethodTable().
            ULONG cbSize     = *(ULONG *)m_pMDescr;
            ULONG cbNewSlots = cbSize / (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc));
            _ASSERTE( (cbSize % (COMMETHOD_PREPAD + sizeof(ComCallMethodDesc))) == 0);

            // m_cbSlots is the total number of methods in addition to the ones from the
            // default interfaces.  cbNewSlots is the total number of methods introduced
            // by this class (== m_cbSlots - <slots from parent MT>).
            return (slotIndex >= (cbExtraSlots + m_cbSlots - cbNewSlots));
        }

        return FALSE;
    }

    ComMethodTable *GetParentClassComMT();

    static inline PTR_ComMethodTable ComMethodTableFromIP(PTR_IUnknown pUnk)
    {
        CONTRACT (PTR_ComMethodTable)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pUnk));
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        PTR_ComMethodTable pMT = dac_cast<PTR_ComMethodTable>(*PTR_TADDR(pUnk) - sizeof(ComMethodTable));

        // validate the object
        _ASSERTE((SLOT)(size_t)0xDEADC0FF == pMT->m_ptReserved );

        RETURN pMT;
    }

    ULONG GetNumSlots()
    {
        LIMITED_METHOD_CONTRACT;
        return m_cbSlots;
    }

    PTR_MethodTable GetMethodTable()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMT;
    }


    inline REFIID GetIID()
    {
        // Cannot use a normal CONTRACT since the return type is ref type which
        // causes problems with normal CONTRACTs.
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACTL_END;

        // Generate the IClassX IID if it hasn't been generated yet.
        if (!(m_Flags & enum_GuidGenerated))
        {
            ExecutableWriterHolder<ComMethodTable> comMTWriterHolder(this, sizeof(ComMethodTable));
            GenerateClassItfGuid(TypeHandle(m_pMT), &comMTWriterHolder.GetRW()->m_IID);
            comMTWriterHolder.GetRW()->m_Flags |= enum_GuidGenerated;
        }

        return m_IID;
    }

    void CheckParentComVisibility(BOOL fForIDispatch)
    {
        WRAPPER_NO_CONTRACT;

        ((ComCallWrapperTemplate*)m_pMT->GetComCallWrapperTemplate())->CheckParentComVisibility(fForIDispatch);
    }

    BOOL CheckParentComVisibilityNoThrow(BOOL fForIDispatch)
    {
        WRAPPER_NO_CONTRACT;

        return ((ComCallWrapperTemplate*)m_pMT->GetComCallWrapperTemplate())->CheckParentComVisibilityNoThrow(fForIDispatch);
    }

private:
    SLOT             m_ptReserved; //= (SLOT) 0xDEADC0FF;  reserved
    PTR_MethodTable  m_pMT; // pointer to the VMs method table
    ULONG            m_cbSlots; // number of slots in the interface (excluding IUnk/IDisp)
    LONG             m_cbRefCount; // ref-count the vtable as it is being shared
    size_t           m_Flags; // make sure this is initialized to zero
    LPVOID           m_pMDescr; // pointer to methoddescr.s owned by this MT
    ITypeInfo*       m_pITypeInfo; // cached pointer to ITypeInfo
    DispatchInfo*    m_pDispatchInfo; // The dispatch info used to expose IDispatch to COM.
    IID              m_IID; // The IID of the interface.
};

#pragma pack(pop)


struct GetComIPFromCCW
{
    enum flags
    {
        None                                = 0,
        CheckVisibility                     = 1,
        SuppressCustomizedQueryInterface    = 2
    };
};

inline GetComIPFromCCW::flags operator|(GetComIPFromCCW::flags lhs, GetComIPFromCCW::flags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<GetComIPFromCCW::flags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
}
inline GetComIPFromCCW::flags operator|=(GetComIPFromCCW::flags & lhs, GetComIPFromCCW::flags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<GetComIPFromCCW::flags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
    return lhs;
}

class ComCallWrapper
{
    friend class MarshalNative;
    friend class ClrDataAccess;

private:
    enum
    {
        NumVtablePtrs = 5,
#ifdef HOST_64BIT
        enum_ThisMask = ~0x3f, // mask on IUnknown ** to get at the OBJECT-REF handle
#else
        enum_ThisMask = ~0x1f, // mask on IUnknown ** to get at the OBJECT-REF handle
#endif
        Slot_Basic = 0,
        Slot_IClassX = 1,
        Slot_FirstInterface = 2,
    };

public:
    BOOL IsHandleWeak();
    VOID MarkHandleWeak();
    VOID ResetHandleStrength();

    BOOL IsComActivated();
    VOID MarkComActivated();

    OBJECTHANDLE GetObjectHandle() { LIMITED_METHOD_CONTRACT; return m_ppThis; }

    // don't instantiate this class directly
    ComCallWrapper() = delete;
    ~ComCallWrapper() = delete;

protected:
#ifndef DACCESS_COMPILE
    static void SetNext(ComCallWrapper* pWrap, ComCallWrapper* pNextWrapper)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(CheckPointer(pWrap));
            PRECONDITION(CheckPointer(pNextWrapper, NULL_OK));
        }
        CONTRACTL_END;

        pWrap->m_pNext = pNextWrapper;
    }
#endif // !DACCESS_COMPILE

    static PTR_ComCallWrapper GetNext(PTR_ComCallWrapper pWrap)
    {
        CONTRACT (PTR_ComCallWrapper)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pWrap));
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN (LinkedWrapperTerminator == pWrap->m_pNext ? NULL : pWrap->m_pNext);
    }

    // Helper to create a wrapper, pClassCCW must be specified if pTemplate->RepresentsVariantInterface()
    static ComCallWrapper* CreateWrapper(OBJECTREF* pObj, ComCallWrapperTemplate *pTemplate, ComCallWrapper *pClassCCW);

    // helper to get wrapper from sync block
    static PTR_ComCallWrapper GetStartWrapper(PTR_ComCallWrapper pWrap);

    // helper to create a wrapper from a template
    static ComCallWrapper* CopyFromTemplate(ComCallWrapperTemplate* pTemplate,
                                            ComCallWrapperCache *pWrapperCache,
                                            OBJECTHANDLE oh);

    static SLOT** GetComIPLocInWrapper(ComCallWrapper* pWrap, unsigned int iIndex);

public:
    SLOT** GetFirstInterfaceSlot();

    // walk the list and free all blocks
    void FreeWrapper(ComCallWrapperCache *pWrapperCache);

    BOOL IsWrapperActive();

    // IsLinkedWrapper
    inline BOOL IsLinked()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            INSTANCE_CHECK;
        }
        CONTRACTL_END;

        return m_pNext != NULL;
    }

    // wrapper is not guaranteed to be present
    // accessor to wrapper object in the sync block
    inline static PTR_ComCallWrapper GetWrapperForObject(OBJECTREF pObj, ComCallWrapperTemplate *pTemplate = NULL)
    {
        CONTRACT (PTR_ComCallWrapper)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            SUPPORTS_DAC;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        PTR_SyncBlock pSync = pObj->PassiveGetSyncBlock();
        if (!pSync)
            RETURN NULL;

        PTR_InteropSyncBlockInfo pInteropInfo = pSync->GetInteropInfoNoCreate();
        if (!pInteropInfo)
            RETURN NULL;

        PTR_ComCallWrapper pCCW = pInteropInfo->GetCCW();

        if (pTemplate != NULL)
        {
            // make sure we use the right CCW - the object may have multiple CCWs associated
            // with it which were created based on different CCW templates
            while (pCCW != NULL && pCCW->GetComCallWrapperTemplate() != pTemplate)
            {
                pCCW = GetNext(pCCW);
            }
        }

        RETURN pCCW;
    }

    // get inner unknown
    HRESULT GetInnerUnknown(void **pv);

    // Init outer unknown
    void InitializeOuter(IUnknown* pOuter);

    // is the object aggregated by a COM component
    BOOL IsAggregated();

    // is the object extends from (aggregates) a COM component
    BOOL IsExtendsCOMObject();

    // get syncblock stored in the simplewrapper
    SyncBlock* GetSyncBlock();

    // get the CCW template this wrapper is based on
    PTR_ComCallWrapperTemplate GetComCallWrapperTemplate();

    // get outer unk
    IUnknown* GetOuter();

    // Get IClassX interface pointer from the wrapper.
    //   The inspectionOnly parameter should only be true to this function if you are
    //   only passively inspecting the value and not using the interface (such as
    //   passing out the pointer via ETW or in the dac).
    IUnknown* GetIClassXIP(bool inspectionOnly=false);

    // Get the basic interface pointer from the wrapper.
    //   The inspectionOnly parameter should only be true to this function if you are
    //   only passively inspecting the value and not using the interface (such as
    //   passing out the pointer via ETW or in the dac).
    IUnknown* GetBasicIP(bool inspectionOnly=false);

    // Get the IDispatch interface pointer from the wrapper.
    IDispatch *GetIDispatchIP();

    // Get ObjectRef from wrapper - this is called by GetObjectRef and GetStartWrapper.
    // Need this becuase GetDomainSynchronized will call GetStartWrapper which will call
    // GetObjectRef which will cause a little bit of nasty infinite recursion.
    inline OBJECTREF GetObjectRef()
    {
        CONTRACT (OBJECTREF)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(m_ppThis));
        }
        CONTRACT_END;

        if (m_ppThis == NULL)
        {
            // Force a fail fast if this CCW is already neutered
            AccessNeuteredCCW_FailFast();
        }

        RETURN ObjectFromHandle(m_ppThis);
    }

    //
    // Force a fail fast for better diagnostics
    // Don't inline so that this call would show up in the callstack
    //
    NOINLINE void AccessNeuteredCCW_FailFast()
    {
        LIMITED_METHOD_CONTRACT;

        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_ACCESSING_CCW);
    }

    // A MODE_ANY helper to get the MethodTable of the 'this' object.  This helper keeps
    // the GCX_COOP transition out of the caller (it implies a holder which implies an
    // FS:0 handler on x86).
    MethodTable* GetMethodTableOfObjectRef();

    // clean up an object wrapper
    void Cleanup();

    // If the object gets collected while the CCW is still active, neuter (disconnect) the CCW.
    void Neuter();
    void ClearHandle();

    // fast access to wrapper for a com+ object,
    // inline check, and call out of line to create, out of line version might cause gc
    //to be enabled
    static ComCallWrapper* __stdcall InlineGetWrapper(OBJECTREF* pObj, ComCallWrapperTemplate *pTemplate = NULL,
                                                      ComCallWrapper *pClassCCW = NULL);

    // Get RefCount
    inline ULONG GetRefCount();

    // AddRef a wrapper
    inline ULONG AddRef();

    // AddRef a wrapper
    inline ULONG AddRefWithAggregationCheck();

    // Release for a Wrapper object
    inline ULONG Release();

    // Initialize the simple wrapper.
    static void InitSimpleWrapper(ComCallWrapper* pWrap, SimpleComCallWrapper* pSimpleWrap);

    // Clear the simple wrapper. This must be called on the start wrapper.
    static void ClearSimpleWrapper(ComCallWrapper* pWrap);

    //Get Simple wrapper, for std interfaces such as IProvideClassInfo
    PTR_SimpleComCallWrapper GetSimpleWrapper()
    {
        CONTRACT (PTR_SimpleComCallWrapper)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            INSTANCE_CHECK;
            POSTCONDITION(CheckPointer(RETVAL));
            SUPPORTS_DAC;
        }
        CONTRACT_END;

        RETURN m_pSimpleWrapper;
    }


    // Get wrapper from IP, for std. interfaces like IDispatch
    inline static PTR_ComCallWrapper GetWrapperFromIP(PTR_IUnknown pUnk);

#if !defined(DACCESS_COMPILE)
    inline static ComCallWrapper* GetStartWrapperFromIP(IUnknown* pUnk)
    {
        CONTRACT (ComCallWrapper*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(CheckPointer(pUnk));
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        ComCallWrapper* pWrap = GetWrapperFromIP(pUnk);
        if (pWrap->IsLinked())
            pWrap = GetStartWrapper(pWrap);

        RETURN pWrap;
    }
#endif // DACCESS_COMPILE

    // Get an interface from wrapper, based on riid or pIntfMT
    static IUnknown* GetComIPFromCCW(ComCallWrapper *pWrap, REFIID riid, MethodTable* pIntfMT, GetComIPFromCCW::flags flags = GetComIPFromCCW::None);

private:
    // pointer to OBJECTREF
    OBJECTHANDLE            m_ppThis;

    // Pointer to the simple wrapper.
    PTR_SimpleComCallWrapper    m_pSimpleWrapper;

    // Block of vtable pointers.
    SLOT*                   m_rgpIPtr[NumVtablePtrs];

    // Pointer to the next wrapper.
    PTR_ComCallWrapper      m_pNext;
};

FORCEINLINE void CCWRelease(ComCallWrapper* p)
{
    WRAPPER_NO_CONTRACT;

    p->Release();
}

class CCWHolder : public Wrapper<ComCallWrapper*, CCWHolderDoNothing, CCWRelease, NULL>
{
public:
    CCWHolder(ComCallWrapper* p = NULL)
        : Wrapper<ComCallWrapper*, CCWHolderDoNothing, CCWRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(ComCallWrapper* p)
    {
        WRAPPER_NO_CONTRACT;

        Wrapper<ComCallWrapper*, CCWHolderDoNothing, CCWRelease, NULL>::operator=(p);
    }
};
//
// Uncommonly used data on Simple CCW
// Created on-demand
//
// We used to have two fields now it only has one field, and I'm keeping this structure
// just in case we want to put more stuff in here later
//
struct SimpleCCWAuxData
{
    VolatilePtr<DispatchExInfo>     m_pDispatchExInfo;  // Information required by the IDispatchEx standard interface

    SimpleCCWAuxData()
    {
        LIMITED_METHOD_CONTRACT;

        m_pDispatchExInfo = NULL;
    }

    ~SimpleCCWAuxData()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pDispatchExInfo)
        {
            delete m_pDispatchExInfo;
            m_pDispatchExInfo = NULL;
        }
    }
};

//--------------------------------------------------------------------------------
// simple ComCallWrapper for all simple std interfaces, that are not used very often
// like IProvideClassInfo, ISupportsErrorInfo etc.
//--------------------------------------------------------------------------------
struct SimpleComCallWrapper
{
private:
    friend class ComCallWrapper;
    friend class ClrDataAccess;

    enum SimpleComCallWrapperFlags
    {
        enum_IsAggregated                      = 0x1,
        enum_IsExtendsCom                      = 0x2,
        enum_IsHandleWeak                      = 0x4,
        enum_IsComActivated                    = 0x8,
        // unused                              = 0x10,
        // unused                              = 0x80,
        // unused                              = 0x100,
        enum_CustomQIRespondsToIMarshal        = 0x200,
        enum_CustomQIRespondsToIMarshal_Inited = 0x400,
    };

public :
    enum : LONGLONG
    {
        CLEANUP_SENTINEL        = 0x0000000080000000,       // Sentinel -> 1 bit
        COM_REFCOUNT_MASK       = 0x000000007FFFFFFF,       // COM -> 31 bits
        EXT_COM_REFCOUNT_MASK   = 0x00000000FFFFFFFF,       // For back-compat, preserve the higher-bit so that outside can observe it
        ALL_REFCOUNT_MASK       = 0xFFFFFFFF7FFFFFFF,
    };

    #define GET_COM_REF(x)      ((ULONG)((x) & SimpleComCallWrapper::COM_REFCOUNT_MASK))
    #define GET_EXT_COM_REF(x)  ((ULONG)((x) & SimpleComCallWrapper::EXT_COM_REFCOUNT_MASK))

#ifdef HOST_64BIT
    #define READ_REF(x)         (x)
#else
    #define READ_REF(x)         (::InterlockedCompareExchange64((LONGLONG *)&x, 0, 0))
#endif

public:
    HRESULT IErrorInfo_hr();
    BSTR    IErrorInfo_bstrDescription();
    BSTR    IErrorInfo_bstrSource();
    BSTR    IErrorInfo_bstrHelpFile();
    DWORD   IErrorInfo_dwHelpContext();
    GUID    IErrorInfo_guid();

    // non virtual methods
    SimpleComCallWrapper();

    VOID Cleanup();

    // Used to neuter a CCW if its AD is being unloaded underneath it.
    VOID Neuter();

    ~SimpleComCallWrapper();


    VOID ResetSyncBlock()
    {
        LIMITED_METHOD_CONTRACT;
        m_pSyncBlock = NULL;
    }

    SyncBlock* GetSyncBlock()
    {
        CONTRACT (SyncBlock*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACT_END;

        RETURN m_pSyncBlock;
    }

    // Init pointer to the vtable of the interface
    // and the main ComCallWrapper if the interface needs it
    void InitNew(OBJECTREF oref, ComCallWrapperCache *pWrapperCache, ComCallWrapper* pWrap,
                 ComCallWrapper *pClassWrap, SyncBlock* pSyncBlock,
                 ComCallWrapperTemplate* pTemplate);

    // used by reconnect wrapper to new object
    void ReInit(SyncBlock* pSyncBlock);

    void InitOuter(IUnknown* pOuter);

    void ResetOuter();

    IUnknown* GetOuter();

    // get inner unknown
    HRESULT GetInnerUnknown(void **ppv)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            PRECONDITION(CheckPointer(ppv));
        }
        CONTRACTL_END;

        *ppv = QIStandardInterface(enum_InnerUnknown);
        if (*ppv)
            return S_OK;
        else
            return E_NOINTERFACE;
    }

    OBJECTREF GetObjectRef()
    {
        CONTRACT (OBJECTREF)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_COOPERATIVE;
        }
        CONTRACT_END;

        RETURN (GetMainWrapper()->GetObjectRef());
    }

    ComCallWrapperCache* GetWrapperCache()
    {
        CONTRACT (ComCallWrapperCache*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN m_pWrapperCache;
    }

    // Connection point helper methods.
    BOOL FindConnectionPoint(REFIID riid, IConnectionPoint **ppCP);
    void EnumConnectionPoints(IEnumConnectionPoints **ppEnumCP);

    // is the object aggregated by a COM component
    BOOL IsAggregated()
    {
        LIMITED_METHOD_CONTRACT;

        return m_flags & enum_IsAggregated;
    }

    void MarkAggregated()
    {
        WRAPPER_NO_CONTRACT;

        FastInterlockOr((ULONG*)&m_flags, enum_IsAggregated);
    }

    void UnMarkAggregated()
    {
        WRAPPER_NO_CONTRACT;

        FastInterlockAnd((ULONG*)&m_flags, ~enum_IsAggregated);
    }

    BOOL IsHandleWeak()
    {
        LIMITED_METHOD_CONTRACT;

        return m_flags & enum_IsHandleWeak;
    }

    void MarkHandleWeak()
    {
        WRAPPER_NO_CONTRACT;

        FastInterlockOr((ULONG*)&m_flags, enum_IsHandleWeak);
    }

    VOID ResetHandleStrength()
    {
        WRAPPER_NO_CONTRACT;

        FastInterlockAnd((ULONG*)&m_flags, ~enum_IsHandleWeak);
    }

    // is the object extends from (aggregates) a COM component
    BOOL IsExtendsCOMObject()
    {
        LIMITED_METHOD_CONTRACT;

        return m_flags & enum_IsExtendsCom;
    }

    BOOL IsComActivated()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_IsComActivated;
    }

    void MarkComActivated()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr((ULONG*)&m_flags, enum_IsComActivated);
    }

    // Determines if the type associated with the ComCallWrapper supports exceptions.
    static BOOL SupportsExceptions(MethodTable *pMT);

    // Determines if the type supports IReflect.
    static BOOL SupportsIReflect(MethodTable *pMT);

    NOINLINE BOOL ShouldUseManagedIProvideClassInfo();

    //--------------------------------------------------------------------------
    // Retrieves the simple wrapper from an IUnknown pointer that is for one
    // of the interfaces exposed by the simple wrapper.
    //--------------------------------------------------------------------------
    static PTR_SimpleComCallWrapper GetWrapperFromIP(PTR_IUnknown pUnk)
    {
        CONTRACT (SimpleComCallWrapper*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pUnk));
            POSTCONDITION(CheckPointer(RETVAL));
            SUPPORTS_DAC;
        }
        CONTRACT_END;

        int i = GetStdInterfaceKind(pUnk);
        PTR_SimpleComCallWrapper pSimpleWrapper = dac_cast<PTR_SimpleComCallWrapper>(dac_cast<TADDR>(pUnk) - sizeof(LPBYTE) * i - offsetof(SimpleComCallWrapper,m_rgpVtable));

        // We should never getting back a built-in interface from a SimpleCCW that represents a variant interface
        _ASSERTE(pSimpleWrapper->m_pClassWrap == NULL);

        RETURN pSimpleWrapper;
    }

    // get the main wrapper
    PTR_ComCallWrapper GetMainWrapper()
    {
        CONTRACT (PTR_ComCallWrapper)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            INSTANCE_CHECK;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN m_pWrap;
    }

    inline ULONG GetRefCount()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_COM_REF(READ_REF(m_llRefCount));
    }

    inline BOOL IsNeutered()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return !!(READ_REF(m_llRefCount) & CLEANUP_SENTINEL);
    }

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    // CCW refcount logging consists of two steps. BuildRefCountLogMessage is an instance method which
    // must be called at a point where the CCW is guaranteed to be alive. LogRefCount is static because
    // we generally don't know the new refcount (the one we want to log) until the CCW is at risk of
    // having been destroyed by other threads.
    void BuildRefCountLogMessage(LPCWSTR wszOperation, StackSString &ssMessage, ULONG dwEstimatedRefCount);
    static void LogRefCount(ComCallWrapper *pWrap, StackSString &ssMessage, ULONG dwRefCountToLog);

    NOINLINE HRESULT LogCCWAddRef(ULONG newRefCount)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        SetupForComCallHR();

        // we can safely assume that the CCW is still alive since this is an AddRef
        StackSString ssMessage;
        BuildRefCountLogMessage(W("AddRef"), ssMessage, newRefCount);
        LogRefCount(GetMainWrapper(), ssMessage, newRefCount);

        return S_OK;
    }

    inline ULONG AddRef()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (m_pClassWrap)
        {
            // Forward to the real wrapper if this CCW represents a variant interface
            return m_pClassWrap->GetSimpleWrapper()->AddRef();
        }

        LONGLONG newRefCount = ::InterlockedIncrement64(&m_llRefCount);
        if (g_pConfig->LogCCWRefCountChangeEnabled())
        {
            LogCCWAddRef(GET_EXT_COM_REF(newRefCount));
        }
        return GET_EXT_COM_REF(newRefCount);
    }

    inline ULONG AddRefWithAggregationCheck()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        // aggregation check
        IUnknown* pOuter = this->GetOuter();
        if (pOuter != NULL)
            return SafeAddRef(pOuter);

        return this->AddRef();
    }

private:
    LONGLONG ReleaseImplWithLogging(LONGLONG * pRefCount);

    NOINLINE void ReleaseImplCleanup()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_pWrap->Cleanup();
    }
public:

    inline ULONG Release()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (m_pClassWrap)
        {
            // Forward to the real wrapper if this CCW represents a variant interface
            return m_pClassWrap->GetSimpleWrapper()->Release();
        }

        LONGLONG *pRefCount = &m_llRefCount;
        ULONG ulComRef = GET_COM_REF(READ_REF(*pRefCount));

        if (ulComRef <= 0)
        {
            _ASSERTE(!"Invalid Release() call on already released object. A managed object exposed to COM is being over-released from unmanaged code");
            return -1;
        }

        // Null the outer pointer if refcount is about to drop to 0. We cannot perform this
        // operation after decrementing the refcount as that would race with the finalizer
        // that may clean this CCW up any time after the refcount drops to 0. With this pre-
        // decrement reset, we are racing with other Release's and may call ResetOuter multiple
        // times (which is fine - it's thread safe and idempotent) or call it when the refcount
        // doesn't really drop to 0 (which is also fine - it would have dropped to 0 under
        // slightly different timing and the COM client is responsible for preventing this).
        if (ulComRef == 1)
            ResetOuter();

        LONGLONG newRefCount;
        if (g_pConfig->LogCCWRefCountChangeEnabled())
        {
            newRefCount = ReleaseImplWithLogging(pRefCount);
        }
        else
        {
            // Decrement the ref count
            newRefCount = ::InterlockedDecrement64(pRefCount);
        }

        // IMPORTANT: Do not touch instance fields or any other data associated with the CCW beyond this
        // point unless newRefCount equals CLEANUP_SENTINEL (it's the only case when we know that Neuter
        // or another Release could not swoop in and destroy our data structures).

        // If we hit the sentinel value in COM ref count == 0, it's our responsibility to clean up.
        if (newRefCount == CLEANUP_SENTINEL)
        {
            ReleaseImplCleanup();
            return 0;
        }

        return GET_EXT_COM_REF(newRefCount);
    }

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    MethodTable* GetMethodTable()
    {
        CONTRACT (MethodTable*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN m_pMT;
    }

    DispatchExInfo* GetDispatchExInfo()
    {
        CONTRACT (DispatchExInfo*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACT_END;

        if (m_pAuxData.Load() == NULL)
            RETURN NULL;
        else
            RETURN m_pAuxData->m_pDispatchExInfo;
    }

    BOOL SupportsICustomQueryInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pTemplate->SupportsICustomQueryInterface();
    }

    PTR_ComCallWrapperTemplate GetComCallWrapperTemplate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pTemplate;
    }

    // Returns TRUE if the ICustomQI implementation returns Handled or Failed for IID_IMarshal.
    BOOL CustomQIRespondsToIMarshal();

    SimpleCCWAuxData *GetOrCreateAuxData()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (m_pAuxData.Load())
            return m_pAuxData;

        NewHolder<SimpleCCWAuxData> pAuxData = new SimpleCCWAuxData();
        if (InterlockedCompareExchangeT(&m_pAuxData, (SimpleCCWAuxData *)pAuxData, NULL) == NULL)
            pAuxData.SuppressRelease();

        return m_pAuxData;
    }

private:
    // Methods to initialize the DispatchEx and exception info.
    void InitExceptionInfo();
    void InitDispatchExInfo();

    // Methods to set up the connection point list.
    void SetUpCPList();
    void SetUpCPListHelper(MethodTable **apSrcItfMTs, int cSrcItfs);
    ConnectionPoint *CreateConnectionPoint(ComCallWrapper *pWrap, MethodTable *pEventMT);
    ConnectionPoint *TryCreateConnectionPoint(ComCallWrapper *pWrap, MethodTable *pEventMT);
    CQuickArray<ConnectionPoint*> *CreateCPArray();

    // QI for well known interfaces from within the runtime direct fetch, instead of guid comparisons
    IUnknown* QIStandardInterface(Enum_StdInterfaces index);

    // QI for well known interfaces from within the runtime based on an IID.
    IUnknown* QIStandardInterface(REFIID riid);

    CQuickArray<ConnectionPoint*>*  m_pCPList;

    // syncblock for the ObjecRef
    SyncBlock*                      m_pSyncBlock;

    //outer unknown cookie
    IUnknown*                       m_pOuter;

    // array of pointers to std. vtables
    SLOT const*                     m_rgpVtable[enum_LastStdVtable];

    PTR_ComCallWrapper              m_pWrap;      // the first ComCallWrapper associated with this SimpleComCallWrapper
    PTR_ComCallWrapper              m_pClassWrap; // the first ComCallWrapper associated with the class (only if m_pMT is an interface)
    MethodTable*                    m_pMT;
    ComCallWrapperCache*            m_pWrapperCache;
    PTR_ComCallWrapperTemplate      m_pTemplate;

    // Points to uncommonly used data that are dynamically allocated
    VolatilePtr<SimpleCCWAuxData>   m_pAuxData;

    DWORD                           m_flags;

    // This maintains the 32-bit COM refcount in 64-bits
    // to enable also tracking the Cleanup sentinel. See code:CLEANUP_SENTINEL
    LONGLONG                        m_llRefCount;
 };

//--------------------------------------------------------------------------------
// ComCallWrapper* ComCallWrapper::InlineGetWrapper(OBJECTREF* ppObj, ComCallWrapperTemplate *pTemplate)
// returns the wrapper for the object, if not yet created, creates one
// returns null for out of memory scenarios.
// Note: the wrapper is returned AddRef'd and should be Released when finished
// with.
//--------------------------------------------------------------------------------
inline ComCallWrapper* __stdcall ComCallWrapper::InlineGetWrapper(OBJECTREF* ppObj, ComCallWrapperTemplate *pTemplate /*= NULL*/,
                                                                  ComCallWrapper *pClassCCW /*= NULL*/)
{
    CONTRACT (ComCallWrapper*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(ppObj));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // get the wrapper for this com+ object
    ComCallWrapper* pWrap = GetWrapperForObject(*ppObj, pTemplate);

    if (NULL == pWrap)
    {
        pWrap = CreateWrapper(ppObj, pTemplate, pClassCCW);
    }
    _ASSERTE(pTemplate == NULL || pTemplate == pWrap->GetSimpleWrapper()->GetComCallWrapperTemplate());

    // All threads will have the same resulting CCW at this point, and
    // they should all check to see if the CCW they got back is
    // appropriate for the current AD.  If not, then we must mark the
    // CCW as agile.
    // If we are creating a CCW that represents a variant interface, use the pClassCCW (which is the main CCW)
    ComCallWrapper *pMainWrap;
    if (pClassCCW)
        pMainWrap = pClassCCW;
    else
        pMainWrap = pWrap;

    pWrap->AddRef();

    RETURN pWrap;
}

inline ULONG ComCallWrapper::GetRefCount()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    return m_pSimpleWrapper->GetRefCount();
}

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
inline ULONG ComCallWrapper::AddRef()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    return m_pSimpleWrapper->AddRef();
}

inline ULONG ComCallWrapper::AddRefWithAggregationCheck()
{
    WRAPPER_NO_CONTRACT;
    return m_pSimpleWrapper->AddRefWithAggregationCheck();
}

inline ULONG ComCallWrapper::Release()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(m_pSimpleWrapper));
    }
    CONTRACTL_END;

    return m_pSimpleWrapper->Release();
}

inline void ComCallWrapper::InitSimpleWrapper(ComCallWrapper* pWrap, SimpleComCallWrapper* pSimpleWrap)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pSimpleWrap));
        PRECONDITION(pSimpleWrap->GetMainWrapper() == pWrap);
    }
    CONTRACTL_END;

    while (pWrap)
    {
        pWrap->m_pSimpleWrapper = pSimpleWrap;
        pWrap = GetNext(pWrap);
    }
}

inline void ComCallWrapper::ClearSimpleWrapper(ComCallWrapper* pWrap)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    // clear the m_pSimpleWrapper field in all wrappers that share the same SimpleComCallWrapper
    SimpleComCallWrapper *pSimpleWrapper = pWrap->m_pSimpleWrapper;

    while (pWrap && pWrap->m_pSimpleWrapper == pSimpleWrapper)
    {
        pWrap->m_pSimpleWrapper = NULL;
        pWrap = GetNext(pWrap);
    }
}
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

inline PTR_ComCallWrapper ComCallWrapper::GetWrapperFromIP(PTR_IUnknown pUnk)
{
    CONTRACT (PTR_ComCallWrapper)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // This code path may be exercised from out-of-process.  Unfortunately, we need to manipulate the
    // target address here, and so we need to do some non-trivial casting.  First, cast the PTR type
    // to the target address first, and then mask off the least significant bits.  Then use the end
    // result as a target address to instantiate a ComCallWrapper.  The line below is equivalent to:
    // ComCallWrapper* pWrap = (ComCallWrapper*)((size_t)pUnk & enum_ThisMask);
    PTR_ComCallWrapper pWrap = dac_cast<PTR_ComCallWrapper>(dac_cast<TADDR>(pUnk) & enum_ThisMask);

    // Use class wrapper if this CCW represents a variant interface
    PTR_ComCallWrapper pClassWrapper = pWrap->GetSimpleWrapper()->m_pClassWrap;
    if (pClassWrapper)
    {
        _ASSERTE(pClassWrapper->GetSimpleWrapper()->m_pClassWrap == NULL);

        RETURN pClassWrapper;
    }

    RETURN pWrap;
}

//--------------------------------------------------------------------------
// PTR_ComCallWrapper ComCallWrapper::GetStartWrapper(PTR_ComCallWrapper pWrap)
// get outermost wrapper, given a linked wrapper
// get the start wrapper from the sync block
//--------------------------------------------------------------------------
inline PTR_ComCallWrapper ComCallWrapper::GetStartWrapper(PTR_ComCallWrapper pWrap)
{
    CONTRACT (PTR_ComCallWrapper)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(pWrap->IsLinked());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    PTR_SimpleComCallWrapper pSimpleWrap = pWrap->GetSimpleWrapper();
    RETURN (pSimpleWrap->GetMainWrapper());
}

//--------------------------------------------------------------------------
// PTR_ComCallWrapperTemplate ComCallWrapper::GetComCallWrapperTemplate()
inline PTR_ComCallWrapperTemplate ComCallWrapper::GetComCallWrapperTemplate()
{
    LIMITED_METHOD_CONTRACT;
    return GetSimpleWrapper()->GetComCallWrapperTemplate();
}

inline BOOL ComCallWrapper::IsWrapperActive()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Since its called by GCPromote, we assume that this is the start wrapper

    ULONG cbRef = m_pSimpleWrapper->GetRefCount();

    BOOL bHasStrongCOMRefCount = cbRef > 0;

    BOOL bIsWrapperActive = (bHasStrongCOMRefCount && !m_pSimpleWrapper->IsHandleWeak());

    LOG((LF_INTEROP, LL_INFO1000,
         "CCW 0x%p: cbRef = 0x%x, IsHandleWeak = %d\n",
         this,
         cbRef, m_pSimpleWrapper->IsHandleWeak()));
    LOG((LF_INTEROP, LL_INFO1000, "CCW 0x%p: IsWrapperActive returned %d\n", this, bIsWrapperActive));

    return bIsWrapperActive;
}


#endif // FEATURE_COMINTEROP

#endif // _COMCALLABLEWRAPPER_H
