// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "objecthandle.h"
#include "comutilnative.h"
#include "spinlock.h"
#include "comtoclrcall.h"
#include "dispatchinfo.h"
#include "wrappers.h"
#include "internalunknownimpl.h"
#include "rcwwalker.h"
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
    enum
    {
        AD_IS_UNLOADING = 0x01,
    };
    
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

    // create a new WrapperCache (one per domain)
    static ComCallWrapperCache* Create(AppDomain *pDomain);

    // Called when the domain is going away.  We may have outstanding references to this cache,
    //  so we keep it around in a neutered state.
    void    Neuter();

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

    AppDomain* GetDomain()
    {
        CONTRACT (AppDomain*)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;
        
        RETURN ((AppDomain*)((size_t)m_pDomain & ~AD_IS_UNLOADING));
    }

    void ClearDomain()
    {
        LIMITED_METHOD_CONTRACT;
        
        m_pDomain = (AppDomain *)AD_IS_UNLOADING;
    }

    void SetDomainIsUnloading()
    {
        LIMITED_METHOD_CONTRACT;
        
        m_pDomain = (AppDomain*)((size_t)m_pDomain | AD_IS_UNLOADING);
    }

    void ResetDomainIsUnloading()
    {
        LIMITED_METHOD_CONTRACT;
        
        m_pDomain = (AppDomain*)((size_t)m_pDomain & (~AD_IS_UNLOADING));
    }

    BOOL IsDomainUnloading()
    {
        LIMITED_METHOD_CONTRACT;
        
        return ((size_t)m_pDomain & AD_IS_UNLOADING) != 0;
    }

private:
    LONG                    m_cbRef;
    CCacheLineAllocator*    m_pCacheLineAllocator;
    AppDomain*              m_pDomain;

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

class WinRTManagedClassFactory;

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
            // There is a small number of types that the class is castable to via variance and QI'ed for
            // (typically just IFoo<object> where the class implements IFoo<IBar> and IFoo is covariant).
            // There is also some number of IIDs QI'ed for by external code (e.g. Jupiter) that we won't
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

    // Iterates COM-exposed interfaces of a class. Handles arrays which support IIterable<T>,
    // IVector<T>, and IVectorView<T>, as well as WinRT class factories which support factory
    // and static interfaces. It is also aware of redirected interfaces - both the .NET and the
    // corresponding WinRT type are reported
    class CCWInterfaceMapIterator
    {
    private:
        struct InterfaceProps
        {
            MethodTable *m_pItfMT;

            WinMDAdapter::RedirectedTypeIndex m_RedirectedIndex; // valid if m_dwIsRedirectedInterface is set

            DWORD m_dwIsRedirectedInterface : 1;
            DWORD m_dwIsFactoryInterface    : 1;
            DWORD m_dwIsStaticInterface     : 1;
        };

        StackSArray<InterfaceProps> m_Interfaces;
        COUNT_T m_Index;

        inline const InterfaceProps &GetCurrentInterfaceProps() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_Interfaces[(COUNT_T)m_Index];
        }

        InterfaceProps &AppendInterface(MethodTable *pItfMT, bool isRedirected);

    public:
        CCWInterfaceMapIterator(TypeHandle thClass, WinRTManagedClassFactory *pClsFact, bool fIterateRedirectedInterfaces);

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

        BOOL IsFactoryInterface() const
        {
            LIMITED_METHOD_CONTRACT;
            return GetCurrentInterfaceProps().m_dwIsFactoryInterface;
        }

        BOOL IsStaticInterface() const
        {
            LIMITED_METHOD_CONTRACT;
            return GetCurrentInterfaceProps().m_dwIsStaticInterface;
        }

        BOOL IsRedirectedInterface() const
        {
            LIMITED_METHOD_CONTRACT;
            return GetCurrentInterfaceProps().m_dwIsRedirectedInterface;
        }

        WinMDAdapter::RedirectedTypeIndex GetRedirectedInterfaceIndex() const
        {
            LIMITED_METHOD_CONTRACT;
            return GetCurrentInterfaceProps().m_RedirectedIndex;
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
    ComMethodTable* GetComMTForIndex(ULONG ulItfIndex);
    ComMethodTable* GetBasicComMT();
    ULONG           GetNumInterfaces();
    SLOT*           GetVTableSlot(ULONG index);
    BOOL            HasInvisibleParent();
    void            CheckParentComVisibility(BOOL fForIDispatch);
    BOOL            CheckParentComVisibilityNoThrow(BOOL fForIDispatch);
    
    // Calls GetDefaultInterfaceForClassInternal and caches the result.
    DefaultInterfaceType GetDefaultInterface(MethodTable **ppDefaultItf);

    // Sets up the class method table for the IClassX and also lays it out.
    static ComMethodTable *SetupComMethodTableForClass(MethodTable *pMT, BOOL bLayOutComMT);

    MethodDesc * GetICustomQueryInterfaceGetInterfaceMD();

    IIDToInterfaceTemplateCache *GetOrCreateIIDToInterfaceTemplateCache();

    BOOL SupportsICustomQueryInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_ImplementsICustomQueryInterface);
    }

    BOOL SupportsIInspectable()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_SupportsIInspectable);
    }

    BOOL SupportsVariantInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_SupportsVariantInterface);
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

    MethodTable *GetWinRTRuntimeClass()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pWinRTRuntimeClass;
    }

    BOOL IsSafeTypeForMarshalling();

    // Creates a new Template and caches it on the MethodTable/ArrayTypeDesc or class factory.
    static ComCallWrapperTemplate *CreateTemplate(TypeHandle thClass, WinRTManagedClassFactory *pClsFact = NULL);

    // Creates a new Template for just one interface. Used for lazily created CCWs for interfaces with variance.
    static ComCallWrapperTemplate *CreateTemplateForInterface(MethodTable *pItfMT);

private:
    
    enum ComCallWrapperTemplateFlags
    {
        // first 3 bits are interpreted as DefaultInterfaceType
        enum_DefaultInterfaceType             = 0x7,
        enum_DefaultInterfaceTypeComputed     = 0x10,

        enum_InvisibleParent                  = 0x20,
        enum_ImplementsICustomQueryInterface  = 0x40,
        enum_SupportsIInspectable             = 0x80,
        enum_SupportsIClassX                  = 0x100,

        enum_SupportsVariantInterface         = 0x200, // this is a template for a class that implements an interface with variance
        enum_RepresentsVariantInterface       = 0x400, // this is a template for an interface with variance

        enum_UseOleAutDispatchImpl            = 0x800, // the class is decorated with IDispatchImplAttribute(CompatibleImpl)

        enum_ImplementsIMarshal               = 0x1000, // the class implements a managed interface with Guid == IID_IMarshal

        enum_IsSafeTypeForMarshalling         = 0x2000, // The class can be safely marshalled out of process via DCOM
    };

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
    ComMethodTable* CreateComMethodTableForDelegate(MethodTable *pDelegateMT);

    void            DetermineComVisibility();

private:
    LONG                                    m_cbRefCount;
    ComCallWrapperTemplate*                 m_pParent;
    TypeHandle                              m_thClass;
    MethodTable*                            m_pDefaultItf;
    MethodTable*                            m_pWinRTRuntimeClass;
    ComMethodTable*                         m_pClassComMT;
    ComMethodTable*                         m_pBasicComMT;
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

struct IInspectableVtable : IUnkVtable
{
    SLOT        m_GetIIDs;
    SLOT        m_GetRuntimeClassName;
    SLOT        m_GetTrustLevel;
};

enum Masks
{
    enum_InterfaceTypeMask              = 0x00000003,
    enum_ClassInterfaceTypeMask         = 0x00000003,
    enum_ClassVtableMask                = 0x00000004,
    enum_LayoutComplete                 = 0x00000010,
    enum_ComVisible                     = 0x00000040,
    enum_SigClassCannotLoad             = 0x00000080,
    enum_SigClassLoadChecked            = 0x00000100,
    enum_ComClassItf                    = 0x00000200,
    enum_GuidGenerated                  = 0x00000400,
    enum_IsUntrusted                    = 0x00001000,
    enum_IsBasic                        = 0x00002000,
    enum_IsWinRTDelegate                = 0x00004000,
    enum_IsWinRTTrivialAggregate        = 0x00008000,
    enum_IsWinRTFactoryInterface        = 0x00010000,
    enum_IsWinRTStaticInterface         = 0x00020000,
    enum_IsWinRTRedirectedInterface     = 0x00040000,
    
    enum_WinRTRedirectedInterfaceMask   = 0xFF000000, // the highest byte contains redirected interface index
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
    void LayOutDelegateMethodTable();
    
    // Accessor for the IDispatch information.
    DispatchInfo* GetDispatchInfo();

    LONG AddRef()
    {
        LIMITED_METHOD_CONTRACT;
        
        return InterlockedIncrement(&m_cbRefCount);
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
        
        // use a different var here becuase cleanup will delete the object
        // so can no longer make member refs
        LONG cbRef = InterlockedDecrement(&m_cbRefCount);
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

    BOOL IsDefinedInUntrustedCode()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsUntrusted) ? TRUE : FALSE;
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

    BOOL IsWinRTDelegate()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsWinRTDelegate) != 0;
    }

    BOOL IsWinRTTrivialAggregate()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsWinRTTrivialAggregate) != 0;
    }

    BOOL IsWinRTFactoryInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsWinRTFactoryInterface) != 0;
    }

    BOOL IsWinRTStaticInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsWinRTStaticInterface) != 0;
    }

    VOID SetIsWinRTFactoryInterface()
    {
        LIMITED_METHOD_CONTRACT;
        m_Flags |= enum_IsWinRTFactoryInterface;
    }

    VOID SetIsWinRTStaticInterface()
    {
        LIMITED_METHOD_CONTRACT;
        m_Flags |= enum_IsWinRTStaticInterface;
    }

    BOOL IsWinRTRedirectedInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_IsWinRTRedirectedInterface) != 0;
    }

    WinMDAdapter::RedirectedTypeIndex GetWinRTRedirectedInterfaceIndex()
    {
        LIMITED_METHOD_CONTRACT;
        return (WinMDAdapter::RedirectedTypeIndex)((m_Flags & enum_WinRTRedirectedInterfaceMask) >> 24);
    }

    void SetWinRTRedirectedInterfaceIndex(WinMDAdapter::RedirectedTypeIndex index)
    {
        LIMITED_METHOD_CONTRACT;
        
        m_Flags |= ((size_t)index << 24);
        m_Flags |= enum_IsWinRTRedirectedInterface;
        _ASSERTE(GetWinRTRedirectedInterfaceIndex() == index);
    }

    BOOL HasInvisibleParent()
    {
        LIMITED_METHOD_CONTRACT;
        return ((ComCallWrapperTemplate*)m_pMT->GetComCallWrapperTemplate())->HasInvisibleParent();
    }
    
    BOOL IsSigClassLoadChecked()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_Flags & enum_SigClassLoadChecked) != 0;
    }

    BOOL IsSigClassCannotLoad()
    {
        LIMITED_METHOD_CONTRACT;
        return 0 != (m_Flags & enum_SigClassCannotLoad);
    }

    VOID SetSigClassCannotLoad()
    {
        LIMITED_METHOD_CONTRACT;
        m_Flags |= enum_SigClassCannotLoad;
    }

    VOID SetSigClassLoadChecked()
    {
        LIMITED_METHOD_CONTRACT;
        m_Flags |= enum_SigClassLoadChecked;
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
            case ifInspectable: return (sizeof(IInspectableVtable) / sizeof(SLOT)); 
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
            GenerateClassItfGuid(TypeHandle(m_pMT), &m_IID);
            m_Flags |= enum_GuidGenerated;
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
    // Helper methods.
    BOOL CheckSigTypesCanBeLoaded(MethodTable *pItfClass);
    
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
        SuppressSecurityCheck               = 2, 
        SuppressCustomizedQueryInterface    = 4
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
#ifdef _WIN64
        NumVtablePtrs = 5,
        enum_ThisMask = ~0x3f,                  // mask on IUnknown ** to get at the OBJECT-REF handle
#else

        NumVtablePtrs = 5,
        enum_ThisMask = ~0x1f, // mask on IUnknown ** to get at the OBJECT-REF handle
#endif
        Slot_IClassX  = 1,
        Slot_Basic    = 0,

        Slot_FirstInterface = 2,
    };
    
public:
    ADID GetDomainID();

    // The first overload respects the is-agile flag and context, the other two respect the flag but
    // ignore the context (this is mostly for back compat reasons, new code should call the first overload).
    BOOL NeedToSwitchDomains(Thread *pThread, ADID *pTargetADID, Context **ppTargetContext);
    BOOL NeedToSwitchDomains(Thread *pThread);
    BOOL NeedToSwitchDomains(ADID appdomainID);

    void MakeAgile(OBJECTREF pObj);
    void CheckMakeAgile(OBJECTREF pObj);

    VOID ResetHandleStrength();
    VOID MarkHandleWeak();

    BOOL IsHandleWeak();

    OBJECTHANDLE GetObjectHandle();
    OBJECTHANDLE GetRawObjectHandle() { LIMITED_METHOD_CONTRACT; return m_ppThis; } // no NULL check

protected:
    // don't instantiate this class directly
    ComCallWrapper()
    {
        LIMITED_METHOD_CONTRACT;
    }
    ~ComCallWrapper()
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    void Init();

#ifndef DACCESS_COMPILE
    inline static void SetNext(ComCallWrapper* pWrap, ComCallWrapper* pNextWrapper)
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

    inline static PTR_ComCallWrapper GetNext(PTR_ComCallWrapper pWrap)
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

    // Helper to perform a security check for passing out CCWs late-bound to scripting code.
    void DoScriptingSecurityCheck();

    // Helper to create a wrapper, pClassCCW must be specified if pTemplate->RepresentsVariantInterface()
    static ComCallWrapper* CreateWrapper(OBJECTREF* pObj, ComCallWrapperTemplate *pTemplate, ComCallWrapper *pClassCCW);

    // helper to get the IUnknown* within a wrapper
    static SLOT** GetComIPLocInWrapper(ComCallWrapper* pWrap, unsigned iIndex);

    // helper to get index within the interface map for an interface that matches
    // the interface MT
    static signed GetIndexForIntfMT(ComCallWrapperTemplate *pTemplate, MethodTable *pIntfMT);

    // helper to get wrapper from sync block
    static PTR_ComCallWrapper GetStartWrapper(PTR_ComCallWrapper pWrap);

    // helper to create a wrapper from a template
    static ComCallWrapper* CopyFromTemplate(ComCallWrapperTemplate* pTemplate,
                                            ComCallWrapperCache *pWrapperCache,
                                            OBJECTHANDLE oh);

    // helper to find a covariant supertype of pMT with the given IID
    static MethodTable *FindCovariantSubtype(MethodTable *pMT, REFIID riid);

    // Like GetComIPFromCCW, but will try to find riid/pIntfMT among interfaces implemented by this
    // object that have variance. Assumes that call GetComIPFromCCW with same arguments has failed.
    IUnknown *GetComIPFromCCWUsingVariance(REFIID riid, MethodTable *pIntfMT, GetComIPFromCCW::flags flags);

    static IUnknown * GetComIPFromCCW_VariantInterface(
                            ComCallWrapper * pWrap, REFIID riid, MethodTable * pIntfMT, GetComIPFromCCW::flags flags,
                            ComCallWrapperTemplate * pTemplate);

    inline static IUnknown * GetComIPFromCCW_VisibilityCheck(
                            IUnknown * pIntf, MethodTable * pIntfMT, ComMethodTable * pIntfComMT, 
                            GetComIPFromCCW::flags flags);

    static IUnknown * GetComIPFromCCW_HandleExtendsCOMObject(
                            ComCallWrapper * pWrap, REFIID riid, MethodTable * pIntfMT,
                            ComCallWrapperTemplate * pTemplate, signed imapIndex, unsigned intfIndex);

    static IUnknown * GetComIPFromCCW_ForIID_Worker(
                            ComCallWrapper * pWrap, REFIID riid, MethodTable * pIntfMT, GetComIPFromCCW::flags flags,
                            ComCallWrapperTemplate * pTemplate);

    static IUnknown * GetComIPFromCCW_ForIntfMT_Worker(
                            ComCallWrapper * pWrap, MethodTable * pIntfMT, GetComIPFromCCW::flags flags);


public:
    static bool GetComIPFromCCW_HandleCustomQI(
                            ComCallWrapper * pWrap, REFIID riid, MethodTable * pIntfMT, IUnknown ** ppUnkOut);

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

    // is the object a transparent proxy
    BOOL IsObjectTP();

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
            SO_TOLERANT;
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

    // Get Jupiter RefCount
    inline ULONG GetJupiterRefCount();

    // AddRef Jupiter Ref Count
    // Jupiter Ref count becomes strong ref if pegged, otherwise weak ref
    inline ULONG AddJupiterRef();

    // Release Jupiter Ref Count
    // Jupiter Ref count becomes strong ref if pegged, otherwise weak ref
    inline ULONG ReleaseJupiterRef();

    // Return whether this CCW is pegged or not by Jupiter
    inline BOOL IsPegged();

    // Return whether this CCW is pegged or not (either by Jupiter, or globally)
    // We globally peg every Jupiter CCW outside Gen 2 GCs
    inline BOOL IsConsideredPegged();
    
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
            SO_TOLERANT;
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
    static IUnknown* GetComIPFromCCWNoThrow(ComCallWrapper *pWrap, REFIID riid, MethodTable* pIntfMT, GetComIPFromCCW::flags flags = GetComIPFromCCW::None);


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

typedef DPTR(class WeakReferenceImpl) PTR_WeakReferenceImpl;

//
// Represents a domain-bound weak reference to the object (not the CCW)
//
class WeakReferenceImpl : public IUnknownCommon<IWeakReference>
{
private:
    ADID                m_adid;                 // AppDomain ID of where this weak reference is created
    Context             *m_pContext;            // Saved context
    OBJECTHANDLE        m_ppObject;             // Short weak global handle points back to the object, 
                                                // created in domain ID = m_adid
    
public:
    WeakReferenceImpl(SimpleComCallWrapper *pSimpleWrapper, Thread *pCurrentThread);
    virtual ~WeakReferenceImpl();
    
    // IWeakReference methods
    virtual HRESULT STDMETHODCALLTYPE Resolve(REFIID riid, IInspectable **ppvObject);

private :
    static void Resolve_Callback(LPVOID lpData);    
    static void Resolve_Callback_SwitchToPreemp(LPVOID lpData);
    
    HRESULT ResolveInternal(Thread *pThread, REFIID riid, IInspectable **ppvObject);
        
    HRESULT Cleanup();
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
                                                        // Not available on WinRT types

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
    friend class WeakReferenceImpl;

    enum SimpleComCallWrapperFlags
    {
        enum_IsAggregated                      = 0x1,
        enum_IsExtendsCom                      = 0x2,
        enum_IsHandleWeak                      = 0x4,
        enum_IsObjectTP                        = 0x8,
        enum_IsAgile                           = 0x10,
        enum_IsPegged                          = 0x80,
        enum_HasOverlappedRef                  = 0x100,
        enum_CustomQIRespondsToIMarshal        = 0x200,
        enum_CustomQIRespondsToIMarshal_Inited = 0x400,
    }; 

public :
    enum : LONGLONG
    {
        CLEANUP_SENTINEL        = 0x0000000080000000,       // Sentinel -> 1 bit
        COM_REFCOUNT_MASK       = 0x000000007FFFFFFF,       // COM -> 31 bits
        JUPITER_REFCOUNT_MASK   = 0xFFFFFFFF00000000,       // Jupiter -> 32 bits
        JUPITER_REFCOUNT_SHIFT  = 32,
        JUPITER_REFCOUNT_INC    = 0x0000000100000000,
        EXT_COM_REFCOUNT_MASK   = 0x00000000FFFFFFFF,       // For back-compat, preserve the higher-bit so that outside can observe it
        ALL_REFCOUNT_MASK       = 0xFFFFFFFF7FFFFFFF,
    };

    #define GET_JUPITER_REF(x)  ((ULONG)(((x) & SimpleComCallWrapper::JUPITER_REFCOUNT_MASK) >> SimpleComCallWrapper::JUPITER_REFCOUNT_SHIFT))
    #define GET_COM_REF(x)      ((ULONG)((x) & SimpleComCallWrapper::COM_REFCOUNT_MASK))
    #define GET_EXT_COM_REF(x)  ((ULONG)((x) & SimpleComCallWrapper::EXT_COM_REFCOUNT_MASK))

#ifdef _WIN64
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
    VOID Neuter(bool fSkipHandleCleanup = false);

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
                 ComCallWrapper *pClassWrap, Context* pContext, SyncBlock* pSyncBlock,
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
            SO_TOLERANT;
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

    ADID GetDomainID()
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACTL_END;
        
        if (IsAgile())
            return GetThread()->GetDomain()->GetId();

        return m_dwDomainId;
    }

    ADID GetRawDomainID()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_dwDomainId;
    }

#ifndef CROSSGEN_COMPILE
    inline BOOL NeedToSwitchDomains(Thread *pThread, ADID *pTargetADID, Context **ppTargetContext)
    {
        CONTRACTL
        {
            NOTHROW;
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;
        
        if (IsAgile())
            return FALSE;

        if (m_dwDomainId == pThread->GetDomain()->GetId() && m_pContext == pThread->GetContext())
            return FALSE;

        // we intentionally don't provide any other way to read m_pContext so the caller always
        // gets ADID & Context that are guaranteed to be in sync (note that GetDomainID() lies
        // if the CCW is agile and using it together with m_pContext leads to issues)
        *pTargetADID = m_dwDomainId;
        *ppTargetContext = m_pContext;

        return TRUE;
    }

    // if you call this you must either pass TRUE for throwIfUnloaded or check
    // after the result before accessing any pointers that may be invalid.
    inline BOOL NeedToSwitchDomains(ADID appdomainID)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Check for a direct domain ID match first -- this is more common than agile wrappers.
        return (m_dwDomainId != appdomainID) && !IsAgile();
    }
#endif //CROSSGEN_COMPILE

    BOOL ShouldBeAgile()
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
        }
        CONTRACTL_END;
        
        return (!IsAgile() && GetThread()->GetDomain()->GetId()!= m_dwDomainId);
    }

    void MakeAgile(OBJECTHANDLE origHandle)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        
        m_hOrigDomainHandle = origHandle;
        FastInterlockOr((ULONG*)&m_flags, enum_IsAgile);
    }

    BOOL IsAgile()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_flags & enum_IsAgile;
    }

    BOOL IsObjectTP()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_flags & enum_IsObjectTP;
    }

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

    inline BOOL IsPegged()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_flags & enum_IsPegged;
    }

    inline void MarkPegged()
    {
        LIMITED_METHOD_CONTRACT;

        FastInterlockOr((ULONG*)&m_flags, enum_IsPegged);
    }
    
    inline void UnMarkPegged()
    {
        LIMITED_METHOD_CONTRACT;
            
        FastInterlockAnd((ULONG*)&m_flags, ~enum_IsPegged);
    }
    
    inline BOOL HasOverlappedRef()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_flags & enum_HasOverlappedRef;
    }    
    
    // Used for the creation and deletion of simple wrappers
    static SimpleComCallWrapper* CreateSimpleWrapper();

    // Determines if the type associated with the ComCallWrapper supports exceptions.
    static BOOL SupportsExceptions(MethodTable *pMT);
    static BOOL SupportsIStringable(MethodTable *pMT);

    // Determines if the type supports IReflect / IExpando.
    static BOOL SupportsIReflect(MethodTable *pMT);
    static BOOL SupportsIExpando(MethodTable *pMT);

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
            SO_TOLERANT;
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
            SO_TOLERANT;
        }
        CONTRACT_END;
        
        RETURN m_pWrap;
    }

    inline PTR_ComCallWrapper GetClassWrapper()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_pMT->IsInterface());
        _ASSERTE(m_pClassWrap != NULL);

        return m_pClassWrap;
    }

    inline ULONG GetRefCount()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_COM_REF(READ_REF(m_llRefCount));
    }

    // Returns the unmarked raw ref count
    // Make sure we always make a copy of the value instead of inlining
    NOINLINE LONGLONG GetRealRefCount()
    {
        LIMITED_METHOD_CONTRACT;

        return READ_REF(m_llRefCount);
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
            SO_TOLERANT;
        }
        CONTRACTL_END;

        SetupForComCallHRNoHostNotifNoCheckCanRunManagedCode();

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
            SO_TOLERANT;
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
            SO_TOLERANT;
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
            SO_TOLERANT;
        }
        CONTRACTL_END;

        if (!CanRunManagedCode())
            return;
        SO_INTOLERANT_CODE_NOTHROW(GetThread(), return; );
        ReverseEnterRuntimeHolderNoThrow REHolder;
        if (CLRTaskHosted())                      
        {                                         
            HRESULT hr = REHolder.AcquireNoThrow();
            if (FAILED(hr))
                return;
        }

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
            SO_TOLERANT;
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

        // If we hit the sentinel value in COM ref count and jupiter ref count == 0, it's our responsibility to clean up.
        if (newRefCount == CLEANUP_SENTINEL)
        {
            ReleaseImplCleanup();
            return 0;
        }

        return GET_EXT_COM_REF(newRefCount);
    }

    inline ULONG AddJupiterRef()
    {
        WRAPPER_NO_CONTRACT;

        LONGLONG llOldRefCount;
        LONGLONG llNewRefCount;
        
        do {
            llOldRefCount = m_llRefCount;
            llNewRefCount = llOldRefCount + JUPITER_REFCOUNT_INC;
        } while (InterlockedCompareExchange64(&m_llRefCount, llNewRefCount, llOldRefCount) != llOldRefCount);
        
        LOG((LF_INTEROP, LL_INFO1000, 
            "SimpleComCallWrapper::AddJupiterRef() called on SimpleComCallWrapper 0x%p, cbRef = 0x%x, cbJupiterRef = 0x%x\n", this, GET_COM_REF(llNewRefCount), GET_JUPITER_REF(llNewRefCount)));

        return GET_JUPITER_REF(llNewRefCount);
    }

    inline ULONG ReleaseJupiterRef()
    {
        WRAPPER_NO_CONTRACT;

        LONGLONG llOldRefCount;
        LONGLONG llNewRefCount;
        
        do {
            llOldRefCount = m_llRefCount;
            llNewRefCount = llOldRefCount - JUPITER_REFCOUNT_INC;
        } while (InterlockedCompareExchange64(&m_llRefCount, llNewRefCount, llOldRefCount) != llOldRefCount);

        LOG((LF_INTEROP, LL_INFO1000, 
            "SimpleComCallWrapper::ReleaseJupiterRef() called on SimpleComCallWrapper 0x%p, cbRef = 0x%x, cbJupiterRef = 0x%x\n", this, GET_COM_REF(llNewRefCount), GET_JUPITER_REF(llNewRefCount)));

        if (llNewRefCount == CLEANUP_SENTINEL)
        {
            // If we hit the sentinel value, it's our responsibility to clean up.
            m_pWrap->Cleanup();
        }
        
        return GET_JUPITER_REF(llNewRefCount);
    }

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    inline ULONG GetJupiterRefCount()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_JUPITER_REF(READ_REF(m_llRefCount));
    }    

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

    // Creates new AddRef-ed IWeakReference*
    IWeakReference *CreateWeakReference(Thread *pCurrentThread)
    {        
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            PRECONDITION(pCurrentThread == GetThread());
        }
        CONTRACTL_END;

        // Create a WeakReferenceImpl with RefCount = 1
        // No need to call AddRef
        WeakReferenceImpl *pWeakRef = new WeakReferenceImpl(this, pCurrentThread);

        return pWeakRef;
    }

    void StoreOverlappedPointer(LPOVERLAPPED lpOverlapped)
    {
        LIMITED_METHOD_CONTRACT;

        this->m_operlappedPtr = lpOverlapped;
        MarkOverlappedRef();
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

    // These values are never used at the same time, so we can save a few bytes for each CCW by using a union.
    // Use the inline methods HasOverlappedRef(), MarkOverlappedRef(), and UnMarkOverlappedRef() to differentiate
    // how this union is to be interpreted.
    union
    {
        CQuickArray<ConnectionPoint*>*  m_pCPList;
        LPOVERLAPPED m_operlappedPtr;
    };
    
    // syncblock for the ObjecRef
    SyncBlock*                      m_pSyncBlock;

    //outer unknown cookie
    IUnknown*                       m_pOuter;

    // array of pointers to std. vtables
    SLOT const*                     m_rgpVtable[enum_LastStdVtable];
    
    PTR_ComCallWrapper              m_pWrap;      // the first ComCallWrapper associated with this SimpleComCallWrapper
    PTR_ComCallWrapper              m_pClassWrap; // the first ComCallWrapper associated with the class (only if m_pMT is an interface)
    MethodTable*                    m_pMT;
    Context*                        m_pContext;
    ComCallWrapperCache*            m_pWrapperCache;    
    PTR_ComCallWrapperTemplate      m_pTemplate;
    
    // when we make the object agile, need to save off the original handle so we can clean
    // it up when the object goes away.
    // <TODO>Would be nice to overload one of the other values with this, but then
    // would have to synchronize on it too</TODO>
    OBJECTHANDLE                    m_hOrigDomainHandle;

    // Points to uncommonly used data that are dynamically allocated
    VolatilePtr<SimpleCCWAuxData>   m_pAuxData;         

    ADID                            m_dwDomainId;

    DWORD                           m_flags;

    // This maintains both COM ref and Jupiter ref in 64-bit
    LONGLONG                        m_llRefCount;
    
    inline void MarkOverlappedRef()
    {
        LIMITED_METHOD_CONTRACT;

        FastInterlockOr((ULONG*)&m_flags, enum_HasOverlappedRef);
    }
    
    inline void UnMarkOverlappedRef()
    {
        LIMITED_METHOD_CONTRACT;
        
        FastInterlockAnd((ULONG*)&m_flags, ~enum_HasOverlappedRef);
    }
};

inline OBJECTHANDLE ComCallWrapper::GetObjectHandle()
{
    CONTRACT (OBJECTHANDLE)
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    
    RETURN m_ppThis;
}

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
    
    pMainWrap->CheckMakeAgile(*ppObj);
    
    // If the object is agile, and this domain doesn't have UmgdCodePermission
    //  fail the call.
    if (pMainWrap->GetSimpleWrapper()->IsAgile())
        pMainWrap->DoScriptingSecurityCheck();

    pWrap->AddRef();
    
    RETURN pWrap;
}

#ifndef CROSSGEN_COMPILE

inline BOOL ComCallWrapper::NeedToSwitchDomains(Thread *pThread, ADID *pTargetADID, Context **ppTargetContext)
{
    WRAPPER_NO_CONTRACT;
    
    return GetSimpleWrapper()->NeedToSwitchDomains(pThread, pTargetADID, ppTargetContext);
}

inline BOOL ComCallWrapper::NeedToSwitchDomains(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    return NeedToSwitchDomains(pThread->GetDomain()->GetId());
}


inline BOOL ComCallWrapper::NeedToSwitchDomains(ADID appdomainID)
{
    WRAPPER_NO_CONTRACT;
    
    return GetSimpleWrapper()->NeedToSwitchDomains(appdomainID);
}

#endif // CROSSGEN_COMPILE

inline ADID ComCallWrapper::GetDomainID()
{
    WRAPPER_NO_CONTRACT;
    
    return GetSimpleWrapper()->GetDomainID();
}


inline void ComCallWrapper::CheckMakeAgile(OBJECTREF pObj)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    if (GetSimpleWrapper()->ShouldBeAgile())
        MakeAgile(pObj);
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
        SO_TOLERANT;
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
        SO_TOLERANT;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(m_pSimpleWrapper));
    }
    CONTRACTL_END;
    
    return m_pSimpleWrapper->Release();
}

inline ULONG ComCallWrapper::AddJupiterRef()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;
    
    return m_pSimpleWrapper->AddJupiterRef();
}

inline ULONG ComCallWrapper::ReleaseJupiterRef()
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
    
    return m_pSimpleWrapper->ReleaseJupiterRef();
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

inline BOOL ComCallWrapper::IsPegged()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;
    
    return m_pSimpleWrapper->IsPegged();
}

inline BOOL ComCallWrapper::IsConsideredPegged()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_pSimpleWrapper->IsPegged() || RCWWalker::IsGlobalPeggingOn();
}

inline ULONG ComCallWrapper::GetJupiterRefCount()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;
    
    return m_pSimpleWrapper->GetJupiterRefCount();
}



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
        SO_TOLERANT;
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

//--------------------------------------------------------------------------
//  BOOL ComCallWrapper::BOOL IsHandleWeak()
// check if the wrapper has been deactivated
// Moved here to make DAC build happy and hopefully get it inlined
//--------------------------------------------------------------------------
inline BOOL ComCallWrapper::IsHandleWeak()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    SimpleComCallWrapper* pSimpleWrap = GetSimpleWrapper();
    _ASSERTE(pSimpleWrap);
    
    return pSimpleWrap->IsHandleWeak();
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

    LONGLONG llRefCount = m_pSimpleWrapper->GetRealRefCount();
    ULONG cbRef = GET_COM_REF(llRefCount);
    ULONG cbJupiterRef = GET_JUPITER_REF(llRefCount);

    // We only consider jupiter ref count to be a "strong" ref count if it is pegged and it is alive
    // Note that there is no concern for resurrecting this CCW in the next Gen0/1 GC 
    // because this CCW will be promoted to Gen 2 very quickly
    BOOL bHasJupiterStrongRefCount = (cbJupiterRef > 0 && IsConsideredPegged());
        
    BOOL bHasStrongCOMRefCount = ((cbRef > 0) || bHasJupiterStrongRefCount);

    BOOL bIsWrapperActive = (bHasStrongCOMRefCount && !IsHandleWeak());

    LOG((LF_INTEROP, LL_INFO1000, 
         "CCW 0x%p: cbRef = 0x%x, cbJupiterRef = 0x%x, IsPegged = %d, GlobalPegging = %d, IsHandleWeak = %d\n", 
         this, 
         cbRef, cbJupiterRef, IsPegged(), RCWWalker::IsGlobalPeggingOn(), IsHandleWeak()));
    LOG((LF_INTEROP, LL_INFO1000, "CCW 0x%p: IsWrapperActive returned %d\n", this, bIsWrapperActive));
    
    return bIsWrapperActive;    
}    


#endif // FEATURE_COMINTEROP

#endif // _COMCALLABLEWRAPPER_H
