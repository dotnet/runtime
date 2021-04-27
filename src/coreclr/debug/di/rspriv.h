// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// rspriv.
//

//
// Common include file for right-side of debugger.
//*****************************************************************************

#ifndef RSPRIV_H
#define RSPRIV_H

#include <winwrap.h>
#include <windows.h>

#include <utilcode.h>


#ifdef _DEBUG
#define LOGGING
#endif

#include <log.h>
#include <corerror.h>

#include "cor.h"

#include "cordebug.h"
#include "xcordebug.h"
#include "cordbpriv.h"
#include "mscoree.h"

#include <cordbpriv.h>
#include <dbgipcevents.h>

#include "common.h"
#include "primitives.h"

#include "dacdbiinterface.h"

#include "helpers.h"

struct MachineInfo;

#include "processdescriptor.h"
#include "nativepipeline.h"
#include "stringcopyholder.h"


#include "eventchannel.h"

#undef ASSERT
#define CRASH(x)  _ASSERTE(!(x))
#define ASSERT(x) _ASSERTE(x)

// We want to keep the 'worst' HRESULT - if one has failed (..._E_...) & the
// other hasn't, take the failing one.  If they've both/neither failed, then
// it doesn't matter which we take.
// Note that this macro favors retaining the first argument
#define WORST_HR(hr1,hr2) (FAILED(hr1)?hr1:hr2)

// #UseDataTarget
// Forbid usage of OS APIs that we should be using the data-target for
#define ReadProcessMemory DONT_USE_READPROCESS_MEMORY
#define WriteProcessMemory DONT_USE_WRITEPROCESS_MEMORY


/* ------------------------------------------------------------------------- *
 * Forward class declarations
 * ------------------------------------------------------------------------- */

class CordbBase;
class CordbValue;
class CordbModule;
class CordbClass;
class CordbFunction;
class CordbCode;
class CordbFrame;
class CordbJITILFrame;
class CordbInternalFrame;
class CordbContext;
class CordbThread;
class CordbVariableHome;

#ifdef FEATURE_INTEROP_DEBUGGING
class CordbUnmanagedThread;
struct CordbUnmanagedEvent;
#endif

class CordbProcess;
class CordbAppDomain;
class CordbAssembly;
class CordbBreakpoint;
class CordbStepper;
class Cordb;
class CordbEnCSnapshot;
class CordbWin32EventThread;
class CordbRCEventThread;
class CordbRegisterSet;
class CordbNativeFrame;
class CordbObjectValue;
class CordbReferenceValue;
class CordbEnCErrorInfo;
class CordbEnCErrorInfoEnum;
class Instantiation;
class CordbType;
class CordbNativeCode;
class CordbILCode;
class CordbReJitILCode;
class CordbEval;

class CordbMDA;

class CorpubPublish;
class CorpubProcess;
class CorpubAppDomain;
class CorpubProcessEnum;
class CorpubAppDomainEnum;


class RSLock;
class NeuterList;

class IDacDbiInterface;

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
class DbgTransportTarget;
class DbgTransportSession;
#endif // FEATURE_DBGIPC_TRANSPORT_DI

// @dbgtodo  private shim hook - the RS has private hooks into the shim to help bridge the V2/V3 gap.
// This helps provide a working dogfooding story throughout our transition.
// These hooks must be removed before shipping.
class ShimProcess;

template <class T>
class CordbSafeHashTable;


//---------------------------------------------------------------------------------------
//
// This is an encapsulation of the information necessary to connect to the debugger proxy on a remote machine.
// It includes the IP address and the port number.  The IP address can be set via the env var
// COMPlus_DbgTransportProxyAddress, and the port number is fixed when Mac debugging is configured.
//

struct MachineInfo
{
public:
    void Init(DWORD dwIPAddress, USHORT usPort)
    {
        m_dwIPAddress = dwIPAddress;
        m_usPort      = usPort;
    }

    void Clear()
    {
        m_dwIPAddress = 0;
        m_usPort      = 0;
    }

    DWORD  GetIPAddress() {return m_dwIPAddress;};
    USHORT GetPort() {return m_usPort;};

private:
    DWORD  m_dwIPAddress;
    USHORT m_usPort;
};

extern forDbiWorker forDbi;

// for dbi we just default to new, but we need to have these defined for both dac and dbi
inline void * operator new(size_t lenBytes, const forDbiWorker &)
{
    void * result = new BYTE[lenBytes];
    if (result == NULL)
    {
        ThrowOutOfMemory();
    }
    return result;
}

inline void * operator new[](size_t lenBytes, const forDbiWorker &)
{
    void * result = new BYTE[lenBytes];
    if (result == NULL)
    {
        ThrowOutOfMemory();
    }
    return result;
}

// Helper to delete memory used with the IDacDbiInterface::IAllocator  interface.
template<class T> inline
void DeleteDbiMemory(T *p)
{
    delete p;
}



//---------------------------------------------------------------------------------------
//
// Simple array of holders (either RSSmartPtrs or RSExtSmartPtrs).
// Holds a reference to each element.
//
// Notes:
//    T is the base type and HOLDER_T is the type of the holder.  All functions implemented on this base
//    class must work for both RSSmartPtrs and RSExtSmartPtrs.  For example, there is no concept of neutering
//    for RSExtSmartPtrs.
//

template<typename T, typename HOLDER_T>
class BaseRSPtrArray
{
public:
    BaseRSPtrArray()
    {
        m_pArray = NULL;
        m_cElements = 0;
    }

    // Is the array emtpy?
    bool IsEmpty() const
    {
        return (m_pArray == NULL);
    }

    // Allocate an array of ptrs.
    // Returns false if not enough memory; else true.
    bool Alloc(unsigned int cElements)
    {
        // Caller should have already Neutered
        _ASSERTE(IsEmpty());

        // It's legal to allocate 0 items. We'll succeed the allocation, but still claim that IsEmpty() == true.
        if (cElements == 0)
        {
            return true;
        }

        // RSSmartPtr ctor will ensure all elements are null initialized.
        m_pArray = new (nothrow) HOLDER_T [cElements];
        if (m_pArray == NULL)
        {
            return false;
        }

        m_cElements = cElements;
        return true;
    }

    // Allocate an array of ptrs.
    // Throw on failure
    void AllocOrThrow(unsigned int cElements)
    {
        if (!Alloc(cElements))
        {
            ThrowOutOfMemory();
        }
    }

    // Release each element and empty the array.
    void Clear()
    {
        // this Invoke dtors on each element which will release each element
        delete [] m_pArray;

        m_pArray = NULL;
        m_cElements = 0;
    }

    // Array lookup. Caller gaurantees this is in range.
    // Used for reading
    T* operator [] (unsigned int index) const
    {
        _ASSERTE(m_pArray != NULL);
        CONSISTENCY_CHECK_MSGF((index <= m_cElements), ("Index out of range. Index=%u, Max=%u\n", index, m_cElements));

        return m_pArray[index];
    }

    // Assign a given index to the given value. The array holder will increment the internal reference on the value.
    void Assign(unsigned int index, T* pValue)
    {
        _ASSERTE(m_pArray != NULL);
        CONSISTENCY_CHECK_MSGF((index <= m_cElements), ("Index out of range. Index=%u, Max=%u\n", index, m_cElements));

        m_pArray[index].Assign(pValue);
    }

    // Get lenght of array in elements.
    unsigned int Length() const
    {
        return m_cElements;
    }

    // Some things need to get the address of an element in the table.
    // For example, CordbThreads have an array of CordbFrame objects, and then CordbChains describe a range
    // or frames via pointers into the CordbThread's array.
    // This is a dangerous operation because it lets us side-step reference counting and protection.
    T ** UnsafeGetAddrOfIndex(unsigned int index)
    {
        return m_pArray[index].UnsafeGetAddr();
    }

protected:
    // Raw array of values.
    HOLDER_T * m_pArray;

    // Number of elements in m_pArray. Note the following is always true: (m_cElements == 0) == (m_pArray == NULL);
    unsigned int m_cElements;
};


//-----------------------------------------------------------------------------
//
// Simple array holder of RSSmartPtrs (internal pointers).
// Holds a reference to each element.
//
// Notes:
//    This derived class adds the concept of neutering to the base pointer array.
//    Allows automatic Clear()ing; do not use this unless it is safe to do so in
//    all cases - e.g. you're holding a local.
//

template< typename T, typename HOLDER_T = RSSmartPtr<T> >   // We need to use HOLDER_T to make gcc happy.
class RSPtrArray : public BaseRSPtrArray<T, HOLDER_T>
{
private:
    typedef BaseRSPtrArray<T, HOLDER_T> Super;
    BOOL m_autoClear;

public:
    RSPtrArray() : m_autoClear(FALSE)
    {
    }

    ~RSPtrArray()
    {
        if (m_autoClear)
        {
            Super::Clear();
        }
        else
        {
            // Caller should have already Neutered
            _ASSERTE(Super::IsEmpty());
        }
    }

    void EnableAutoClear()
    {
        m_autoClear = TRUE;
    }

    // Neuter all elements in the array.
    void NeuterAndClear()
    {
        for(unsigned int i = 0; i < Super::m_cElements; i++)
        {
            if (Super::m_pArray[i] != NULL)
            {
                Super::m_pArray[i]->Neuter();
            }
        }

        Super::Clear();
    }
};


//-----------------------------------------------------------------------------
//
// Simple array holder of RSExtSmartPtrs (external pointers).
// Holds a reference to each element.
//
// Notes:
//    This derived class clears the array in its destructor.
//

template< typename T, typename HOLDER_T = RSExtSmartPtr<T> >    // We need to use HOLDER_T to make gcc happy.
class RSExtPtrArray : public BaseRSPtrArray<T, HOLDER_T>
{
private:
    typedef BaseRSPtrArray<T, HOLDER_T> Super;

public:
    ~RSExtPtrArray()
    {
        Super::Clear();
    }
};



//-----------------------------------------------------------------------------
// Table for RSptrs
// This lets us map cookies <--> RSPTR_*,
// Then we just put the cookie in the IPC block instead of the raw RSPTR.
// This will also adjust the internal-reference count on the T* object.
// This isolates the RS from bugs in the LS.
// We templatize by type for type safety.
// Caller must syncrhonize all access (preferably w/ the stop-go lock).
//-----------------------------------------------------------------------------
template <class T>
class RsPtrTable
{
public:
    RsPtrTable()
    {
        m_pTable = NULL;
        m_cEntries = 0;
    }
    ~RsPtrTable()
    {
        Clear();
    }
    void Clear()
    {
        for(UINT i = 0; i < m_cEntries; i++)
        {
            if (m_pTable[i])
            {
                m_pTable[i]->InternalRelease();
            }
        }
        delete [] m_pTable;
        m_pTable = NULL;
        m_cEntries = 0;
    }

    // Add a value into table.  Value can't be NULL.
    // Returns 0 on failure (such as oom),
    // Returns a non-zero cookie on success.
    UINT Add(T* pValue)
    {
        _ASSERTE(pValue != NULL);
        // skip 0 because it's an invalid handle.
        for(UINT i = 1; ; i++)
        {
            // If we've run out of space, allocate new space
            if( i >= m_cEntries )
            {
                if( !Grow() )
                {
                    return 0;   // failed to grow
                }
                _ASSERTE( i < m_cEntries );
                _ASSERTE( m_pTable[i] == NULL );
                // Since we grew, the next slot should now be open.
            }

            if (m_pTable[i] == NULL)
            {
                m_pTable[i] = pValue;
                pValue->InternalAddRef();
                return i;
            }
        }
        UNREACHABLE();
    }

    // Lookup the value based off the cookie, which was obtained via "Add".
    // return NULL on error.
    T* Lookup(UINT cookie)
    {
        _ASSERTE(cookie != 0);
        if (cookie >= m_cEntries)
        {
            CONSISTENCY_CHECK_MSGF(false, ("Cookie out of range.Cookie=0x%x. Size=0x%x.\n", cookie, m_cEntries));
            return NULL;
        }
        T*  p = m_pTable[cookie];
        if (p == NULL)
        {
            CONSISTENCY_CHECK_MSGF(false, ("Cookie is for empty slot.Cookie=0x%x.\n", cookie));
            return NULL; // empty!
        }
        return p;
    }

    T* LookupAndRemove(UINT cookie)
    {
        _ASSERTE(cookie != 0);
        T* p  = Lookup(cookie);
        if (p != NULL)
        {
            m_pTable[cookie] = NULL;
            p->InternalRelease();
        }
        return p;
    }

protected:
    // Resize the m_pTable array.
    bool Grow()
    {
        if (m_pTable == NULL)
        {
            _ASSERTE(m_cEntries == 0);
            size_t cSize = 10;
            m_pTable = new (nothrow) T*[cSize];
            if (m_pTable == NULL)
            {
                return false;
            }
            m_cEntries = cSize;
            ZeroMemory(m_pTable, sizeof(T*) * m_cEntries);
            return true;
        }
        size_t cNewSize = (m_cEntries * 3 / 2) + 1;
        _ASSERTE(cNewSize > m_cEntries);
        T** p = new (nothrow) T*[cNewSize];
        if (p == NULL)
        {
            return false;
        }
        ZeroMemory(p, sizeof(T*) * cNewSize);


        // Copy over old stuff
        memcpy(p, m_pTable, sizeof(T*) * m_cEntries);
        delete [] m_pTable;

        m_pTable = p;
        m_cEntries = cNewSize;
        return true;
    }

    T** m_pTable;
    size_t m_cEntries;
};



//-----------------------------------------------------------------------------
// Simple Holder for RS object intialization to cooperate with Neutering
// semantics.
// The ctor will do an addref.
// The dtor (invoked in exception) will neuter and release the object. This
// release will likely be the final release to cause a delete.
// If the object is created successfully, caller should do a SuppressRelease()
// to avoid it getting neutered.
//
// Example:
//    RSInitHolder<CordbFoo> pFoo(new CordbFoo(x,y,z));
//    pFoo->InitMore(a,b,c);
//    GiveOwnershipToSomebodyElse(pFoo); // now somebody else owns and will clean up
//    pFoo.ClearAndMarkDontNeuter();  // we no longer need to
//
// So if an exception is thrown before ClearAndMarkDontNeuter(), the dtor is invoked
// and the object is properly destroyed (deleted and neutered).
//
// Another common pattern is when initializing an object to hand off to an external:
//    RSInitHolder<CordbFoo> pFoo(new CordbFoo(x,y,z));
//    pFoo->InitMore(a,b,c);
//    pFoo.TransferOwnershipExternal(ppOutParameter);
// TransferOwnershipExternal will assign to ppOutParameter, inc external ref, and
//  call ClearAndMarkDontNeuter()
//-----------------------------------------------------------------------------
template<class T>
class RSInitHolder
{
public:
    // Default ctor. Must call Assign() later.
    RSInitHolder()
    {
    };
    RSInitHolder(T * pObject)
    {
        Assign(pObject);
    }

    void Assign(T * pObject)
    {
        _ASSERTE(m_pObject == NULL); // only assign once.
        m_pObject.Assign(pObject);
    }
    ~RSInitHolder();

    FORCEINLINE operator T *() const
    {
        return m_pObject;

    }
    FORCEINLINE T * operator->()
    {
        return m_pObject;
    }

    // This will null out m_pObject such that the dtor will not neuter it.
    // This will also release the ref we took in the ctor.
    // This will clear the current pointer.
    void ClearAndMarkDontNeuter()
    {
        m_pObject.Clear();
    }

    //
    // Transfer ownership to a pointer
    //
    // Arguments:
    //     ppOutParam - pointer to get ownership. External Reference is incremented.
    //                   this pointer should do an external release.
    //
    // Notes:
    //    This calls ClearAndMarkDontNeuter(). This holder is Empty after this.
    template <class TOther>
    void TransferOwnershipExternal(TOther ** ppOutParam)
    {
        *ppOutParam = static_cast<TOther*> (m_pObject);
        m_pObject->ExternalAddRef();

        ClearAndMarkDontNeuter();
    }


    //
    // Transfer the ownership of the wrapped object to the given hash table.
    //
    // Arguments:
    //    pHashTable - hash table to take ownership.
    //
    // Returns:
    //    the contianing object for convenience. Throws on error (particularly
    //    if it fails adding to the hash).
    //
    // Notes:
    //    This calls ClearAndMarkDontNeuter(). This holder is Empty after this.
    T* TransferOwnershipToHash(CordbSafeHashTable<T> * pHashtable)
    {
        T* pObject = m_pObject;
        pHashtable->AddBaseOrThrow(m_pObject);
        ClearAndMarkDontNeuter();
        return pObject;
    }

    //
    // Used to pass into a function that will assign to us.
    //
    // Returns:
    //     Address of this holder. This is like the & operator.
    //     This is provided for consistency with other holders which
    //     override the &operator.
    RSInitHolder<T> * GetAddr()
    {
        return this;
    }


protected:
    RSSmartPtr<T> m_pObject;
};



//-----------------------------------------------------------------------------
// Have the extra level of indirection is useful for catching Cordbg errors.
//-----------------------------------------------------------------------------
#ifdef _DEBUG
    // On debug, we have an opportunity to catch failing hresults during reproes.
    #define ErrWrapper(hr) ErrWrapperHelper(hr, __FILE__, __LINE__)

    inline HRESULT ErrWrapperHelper(HRESULT hr, const char * szFile, int line)
    {
        if (FAILED(hr))
        {
            DWORD dwErr = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgBreakOnErr);
            if (dwErr)
            {
                CONSISTENCY_CHECK_MSGF(false, ("Dbg Error break, hr=0x%08x, '%s':%d", hr, szFile, line));
            }
        }
        return hr;
    }
#else
    // On release, it's just an identity function
    #define ErrWrapper(hr) (hr)
#endif

//-----------------------------------------------------------------------------
// Quick helpers for threading semantics
//-----------------------------------------------------------------------------

bool IsWin32EventThread(CordbProcess* p);
bool IsRCEventThread(Cordb* p);

/* ------------------------------------------------------------------------- *
 * Typedefs
 * ------------------------------------------------------------------------- */

typedef void* REMOTE_PTR;


//-----------------------------------------------------------------------------
// Wrapper class for locks. This is like Crst on the LS
//-----------------------------------------------------------------------------

class RSLock
{
public:
    // Attrs, can be bitwise-or together.
    enum ELockAttr
    {
        cLockUninit     = 0x00000000,
        cLockReentrant  = 0x00000001,
        cLockFlat       = 0x00000002,

        // (unusual).  Not considered a debug API lock, for purposes of deciding whether
        // to count this lock in m_cTotalDbgApiLocks, which is asserted to be 0 on entry
        // to public APIs.  Example of such a lock: LL_SHIM_PROCESS_DISPOSE_LOCK
        cLockNonDbgApi  = 0x00000004,
    };

    // To prevent deadlocks, we order all locks.
    // A thread must acquire higher-numbered locks before lower numbered locks.
    // These are used as indices into an array, so number them accordingly!
    enum ERSLockLevel
    {
        // Size of the array..
        LL_MAX = 6,

        // The Stop-Go lock is used to make Stop + Continue be atomic operations.
        // These methods will toggle the Process-lock b/c they go between multiple threads.
        // This lock can never be taken on the Win32 ET.
        LL_STOP_GO_LOCK = 5,

        // The win32-event-thread behaves as if it held a lock at this level.
        LL_WIN32_EVENT_THREAD = 4,

        // This held for the duration of ShimProcess::Dispose(), and protects
        // ShimProcess::m_fIsDisposed, so that other ShimProcess functions can
        // safely execute serially with ShimProcess::Dispose().  This needs to be
        // a high-level lock, since ShimProcess methods that take this lock also
        // call into CorDb* objects which take many of the other locks.  In contrast,
        // LL_SHIM_LOCK must remain low-level, as there exists at least one place where
        // LL_SHIM_LOCK is taken while the CorDbProcess lock is also held (see
        // CordbThread::GetActiveFunctions which takes the CorDbProcess lock while
        // calling GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this), which
        // takes LL_SHIM_LOCK).
        LL_SHIM_PROCESS_DISPOSE_LOCK = 3,

        // The process lock is the primary lock for a CordbProcess object. It synchronizes
        // between RCET, W32ET, and user threads.
        LL_PROCESS_LOCK = 2,

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
        LL_DBG_TRANSPORT_MANAGER_LOCK = 1,

        LL_DBG_TRANSPORT_TARGET_LOCK = 0,

        LL_DD_MARSHAL_LOCK = 0,
#endif // FEATURE_DBGIPC_TRANSPORT_DI

        // These are all leaf locks (they don't take any other lock once they're held).
        LL_PROCESS_LIST_LOCK = 0,

        // Win32 send lock is shared by all processes accessing a single w32et.
        LL_WIN32_SEND_LOCK = 0,

        // Small lock around sending IPC events to support workarounds in func-eval abort.
        // See code:CordbEval::Abort for details.
        LL_FUNC_EVAL_ABORT_HACK_LOCK = 0,

        // Leaf-level lock used in the shim.
        LL_SHIM_LOCK = 0
    };

    // Initialize a lock w/ debugging info. szTag must be a string literal.
    void Init(const char * szTag, int eAttr, ERSLockLevel level);
    void Destroy();

    void Lock();
    void Unlock();

protected:
    // Accessors for holders.
    static void HolderEnter(RSLock * pLock)
    {
        pLock->Lock();
    }
    static void HolderLeave(RSLock * pLock)
    {
        pLock->Unlock();
    }


    CRITICAL_SECTION m_lock;

#ifdef _DEBUG
public:
    RSLock();
    ~RSLock();

    const char * Name() { return m_szTag; }

    // Returns true if this thread has the lock.
    bool HasLock();

    // Returns true if this is safe to take on this thread (ie, this thread
    // doesn't already hold bigger locks).
    // bool IsSafeToTake();

    ERSLockLevel GetLevel() { return m_level; }

    // If we're inited, we must have either  cLockReentrant or cLockFlat specified.
    bool IsInit() { return m_eAttr != 0; }
    bool IsReentrant() { return (m_eAttr & cLockReentrant) == cLockReentrant; }
    bool IsDbgApiLock() { return ((m_eAttr & cLockNonDbgApi) == 0); }

protected:
    ERSLockLevel m_level;
    int m_eAttr;        // Bitwise combination of ELockAttr values
    int m_count;
    DWORD m_tidOwner;
    const char * m_szTag;

#endif // #if debug

public:
    typedef Holder<RSLock *, RSLock::HolderEnter, RSLock::HolderLeave> RSLockHolder;
    typedef Holder<RSLock *, RSLock::HolderLeave, RSLock::HolderEnter> RSInverseLockHolder;

};

typedef RSLock::RSLockHolder RSLockHolder;
typedef RSLock::RSInverseLockHolder RSInverseLockHolder;

// In the RS, we should be using RSLocks instead of raw critical sections.
#define CRITICAL_SECTION USE_RSLOCK_INSTEAD_OF_CRITICAL_SECTION


/* ------------------------------------------------------------------------- *
 * Helper macros. Use the ATT_* macros below instead of these.
 * ------------------------------------------------------------------------- */

// This serves as glue for exceptions. Eventually, we shouldn't have unrecoverable
// error, and instead, errors should just propogate up.
#define SetUnrecoverableIfFailed(__p, __hr) \
    if (FAILED(__hr)) \
    { \
       CORDBSetUnrecoverableError(__p, __hr, 0); \
    }

#define CORDBSetUnrecoverableError(__p, __hr, __code) \
    ((__p)->UnrecoverableError((__hr), (__code), __FILE__, __LINE__))

#define _CORDBCheckProcessStateOK(__p) \
    (!((__p)->m_unrecoverableError) && !((__p)->m_terminated) && !((__p)->m_detached))

#define _CORDBCheckProcessStateOKAndSync(__p, __c) \
    (!((__p)->m_unrecoverableError) && !((__p)->m_terminated) && !((__p)->m_detached) && \
    (__p)->GetSynchronized())

// Worker to get failure HR from given state. If not in a failure state, it yields __defaultHR.
// If a caller knows that we're in a failure state, it can pass in a failure value for __defaultHR.
#define CORDBHRFromProcessStateWorker(__p, __c, __defaultHR) \
        ((__p)->m_unrecoverableError ? CORDBG_E_UNRECOVERABLE_ERROR : \
         ((__p)->m_detached ? CORDBG_E_PROCESS_DETACHED : \
         ((__p)->m_terminated ? CORDBG_E_PROCESS_TERMINATED : \
         (!(__p)->GetSynchronized() ? CORDBG_E_PROCESS_NOT_SYNCHRONIZED \
         : (__defaultHR)))))

#define CORDBHRFromProcessState(__p, __c) \
    CORDBHRFromProcessStateWorker(__p, __c, S_OK) \


// Have a set of helper macros to check the process state and return a failure code.
// These only should be used at public interface boundaries, in which case we should
// not be holding the process lock. But we have enough places where we use them internally,
// so we can't really assert that we're not holding the lock.

// We're very restricted in what APIs we can call on the w32et. Have
// a convenient check for this.
// If we have no shim, then nop this check because everything becomes like the w32-event-thread.
#define CORDBFailOrThrowIfOnWin32EventThread(__p, errorAction) \
    { \
        if (((__p)->GetShim() != NULL) && (__p)->IsWin32EventThread()) \
        { \
            _ASSERTE(!"Don't call on this thread"); \
            errorAction(ErrWrapper(CORDBG_E_CANT_CALL_ON_THIS_THREAD)); \
        } \
    }

#define CORDBFailIfOnWin32EventThread(__p) CORDBFailOrThrowIfOnWin32EventThread(__p, return)

#define CORDBRequireProcessStateOK(__p) { \
    if (!_CORDBCheckProcessStateOK(__p)) \
        return ErrWrapper(CORDBHRFromProcessState(__p, NULL)); }

// If we need to be synced, then we shouldn't be on the win32 Event-Thread.
#define CORDBRequireProcessStateOKAndSync(__p,__c) { \
    CORDBFailIfOnWin32EventThread(__p); \
    if (!_CORDBCheckProcessStateOKAndSync(__p, __c)) \
        return ErrWrapper(CORDBHRFromProcessState(__p, __c)); }

#define CORDBRequireProcessSynchronized(__p, __c) { \
    CORDBFailIfOnWin32EventThread(__p); \
    if (!(__p)->GetSynchronized()) return ErrWrapper(CORDBG_E_PROCESS_NOT_SYNCHRONIZED);}




//-----------------------------------------------------------------------------
// All public APIS fall into 2 categories regarding their API Threading Type (ATT)
// We use a standard set of macros to define & enforce each type.
//
// (1) ATT_REQUIRE_STOPPED
// We must be stopped (either synced or at a win32 event) to call this API.
// - We'll fail if we're not stopped.
// - If we're stopped, we'll sync. Thus after this API, we're always synced,
//   and Cordbg must call Continue to resume the process.
// - We'll take the Stop-Go-lock. This prevents another thread from continuing underneath us.
// - We may send IPC events.
// Common for APIs like Stacktracing
//
// (2) ATT_ALLOW_LIVE
// We do not have to be stopped to call this API.
// - We can be live, thus we can not take the stop-go lock (unless it's from a SC-holder).
// - If we're going to send IPC events, we must use a Stop-Continue holder.
// - Our stop-status is the same after this API as it was before.
// Common usage: read-only APIs.
//
// (2b) ATT_ALLOW_LIVE_DO_STOPGO.
// - shortcut macro to do #2, but throw in a stop-continue holder. These really
// should be in camp #1, but that would require an interface change.
//-----------------------------------------------------------------------------

// Helper macros for the ATT stuff

// Do checks that need to be done before we take the SG lock. These include checks
// where if we fail them, taking the SG lock could deadlock (such as being on win32 thread).
#define DO_PRE_STOP_GO_CHECKS(errorAction) \
    CORDBFailOrThrowIfOnWin32EventThread(__proc_for_ATT, errorAction) \
    if ((__proc_for_ATT)->m_unrecoverableError) { errorAction(CORDBG_E_UNRECOVERABLE_ERROR); } \

// Do checks after we take the SG lock. These include checks that rely on state protected
// by the SG lock.
#define DO_POST_STOP_GO_CHECKS(errorAction) \
    _ASSERTE((this->GetProcess() == __proc_for_ATT) || this->IsNeutered()); \
    if (this->IsNeutered()) { errorAction(CORDBG_E_OBJECT_NEUTERED); } \

// #1
// The exact details here are rocket-science.
// We cache the __proc value to a local variable (__proc_for_ATT) so that we don't re-evaluate __proc. (It also forces type-safety).
// This is essential in case __proc is something like "this->GetProcess()" and which can start returning NULL if 'this'
// gets neutered underneath us. Caching guarantees that we'll be able to make it to the StopGo-lock.
//
// We explicitily check some things before taking the Stop-Go lock:
// - CORDBG_E_UNRECOVERABLE_ERROR before the lock because if that's set,
//   we may have leaked locks to the outside world, so taking the StopGo lock later could fail.
// - Are we on the W32et - can't take sg lock if on W32et
// Then we immediately take the stop-go lock to prevent another thread from continuing underneath us.
// Then, if we're stopped, we ensure that we're also synced.
// Stopped includes:
// - Win32-stopped
// - fake win32-stopped. Eg, between SuspendUnmanagedThreads & ResumeUnmanagedThreads
//   (one way to get here is getting debug events during the special-deferment region)
// - synchronized
// If we're not stopped, then we fail.  This macro must never return S_OK.
//
// If not-shimmed (using V3 pipeline), then skip all checks about stop-state.
#define ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW(__proc, errorAction) \
    CordbProcess * __proc_for_ATT = (__proc); \
    DO_PRE_STOP_GO_CHECKS(errorAction); \
    RSLockHolder __ch(__proc_for_ATT->GetStopGoLock()); \
    DO_POST_STOP_GO_CHECKS(errorAction); \
    if ((__proc_for_ATT)->GetShim() != NULL) { \
        if (!__proc_for_ATT->m_initialized) { errorAction(CORDBG_E_NOTREADY); } \
        if ((__proc_for_ATT)->IsStopped()) { \
            HRESULT _hr2 = (__proc_for_ATT)->StartSyncFromWin32Stop(NULL); \
            if (FAILED(_hr2)) errorAction(_hr2); \
        } \
        if (!_CORDBCheckProcessStateOKAndSync(__proc_for_ATT, NULL)) \
            errorAction(CORDBHRFromProcessStateWorker(__proc_for_ATT, NULL, E_FAIL)); \
    }

#define ATT_REQUIRE_STOPPED_MAY_FAIL(__proc)ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW(__proc, return)

// #1b - allows it to be non-inited. This should look just like ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW
// except it doesn't do SSFW32Stop and doesn't have the m_initialized check.
#define ATT_REQUIRE_SYNCED_OR_NONINIT_MAY_FAIL(__proc) \
    CordbProcess * __proc_for_ATT = (__proc); \
    DO_PRE_STOP_GO_CHECKS(return); \
    RSLockHolder __ch(__proc_for_ATT->GetStopGoLock()); \
    DO_POST_STOP_GO_CHECKS(return); \
    if ((__proc_for_ATT)->GetShim() != NULL) { \
        if (!_CORDBCheckProcessStateOKAndSync(__proc_for_ATT, NULL)) \
            return CORDBHRFromProcessStateWorker(__proc_for_ATT, NULL, E_FAIL); \
    }



// Gross variant on #1.
// This is a very dangerous ATT contract; but we need to support it for backwards compat.
// Some APIs, like ICDProcess:EnumerateThreads can be used before the process is actually
// initialized (kind of for interop-debugging).
// These can't check the m_initialized flag b/c that may not be set yet.
// They also can't sync the runtime.
// This should only be used for non-blocking leaf activity.
#define ATT_EVERETT_HACK_REQUIRE_STOPPED_ALLOW_NONINIT(__proc) \
    CordbProcess * __proc_for_ATT = (__proc); \
    DO_PRE_STOP_GO_CHECKS(return); \
    RSLockHolder __ch(__proc_for_ATT->GetStopGoLock()); \
    DO_POST_STOP_GO_CHECKS(return); \
    if (((__proc_for_ATT)->GetShim() != NULL) && !(__proc_for_ATT)->IsStopped()) { return CORDBG_E_PROCESS_NOT_SYNCHRONIZED; } \


// #2 - caller may think debuggee is live, but throw in a Stop-Continue holder.
#define ATT_ALLOW_LIVE_DO_STOPGO(__proc) \
    CordbProcess * __proc_for_ATT = (__proc); \
    DO_PRE_STOP_GO_CHECKS(return); \
    CORDBRequireProcessStateOK(__proc_for_ATT); \
    RSLockHolder __ch(__proc_for_ATT->GetStopGoLock()); \
    DO_POST_STOP_GO_CHECKS(return); \
    StopContinueHolder __hStopGo; \
    if ((__proc_for_ATT)->GetShim() != NULL) \
    { \
        HRESULT _hr2 = __hStopGo.Init(__proc_for_ATT); \
        if (FAILED(_hr2)) return _hr2; \
        _ASSERTE((__proc_for_ATT)->GetSynchronized()); \
    } \




//-----------------------------------------------------------------------------
// StopContinueHolder. Ensure that we're synced during a certain region.
// (Particularly when sending an IPCEvent)
// Calls ICorDebugProcess::Stop & IMDArocess::Continue.
// Example usage:
//
// {
//   StopContinueHolder h;
//   IfFailRet(h.Init(process))
//   SendIPCEvent
// } // continue automatically called.
//-----------------------------------------------------------------------------

class CordbProcess;
class StopContinueHolder
{
public:
    StopContinueHolder() : m_p(NULL) { };

    HRESULT Init(CordbProcess * p);
    ~StopContinueHolder();

protected:
    CordbProcess * m_p;
};


/* ------------------------------------------------------------------------- *
 * Base class
 * ------------------------------------------------------------------------- */

#define COM_METHOD  HRESULT STDMETHODCALLTYPE

typedef enum {
    enumCordbUnknown,       //  0
    enumCordb,              //  1   1  [1]x1
    enumCordbProcess,       //  2   1  [1]x1
    enumCordbAppDomain,     //  3   1  [1]x1
    enumCordbAssembly,      //  4
    enumCordbModule,        //  5   15 [27-38,55-57]x1
    enumCordbClass,         //  6
    enumCordbFunction,      //  7
    enumCordbThread,        //  8   2  [4,7]x1
    enumCordbCode,          //  9
    enumCordbChain,         //  10
    enumCordbChainEnum,     //  11
    enumCordbContext,       //  12
    enumCordbFrame,         //  13
    enumCordbFrameEnum,     //  14
    enumCordbValueEnum,     //  15
    enumCordbRegisterSet,   //  16
    enumCordbJITILFrame,    //  17
    enumCordbBreakpoint,    //  18
    enumCordbStepper,       //  19
    enumCordbValue,         //  20
    enumCordbEnCSnapshot,   //  21
    enumCordbEval,          //  22
    enumCordbUnmanagedThread,// 23
    enumCorpubPublish,      //  24
    enumCorpubProcess,      //  25
    enumCorpubAppDomain,    //  26
    enumCorpubProcessEnum,  //  27
    enumCorpubAppDomainEnum,//  28
    enumCordbEnumFilter,    //  29
    enumCordbEnCErrorInfo,  //  30
    enumCordbEnCErrorInfoEnum,//31
    enumCordbUnmanagedEvent,//  32
    enumCordbWin32EventThread,//33
    enumCordbRCEventThread, //  34
    enumCordbNativeFrame,   //  35
    enumCordbObjectValue,   //  36
    enumCordbType,          //  37
    enumCordbNativeCode,    //  38
    enumCordbILCode,        //  39
    enumCordbEval2,         //  40
    enumCordbMDA,           //  41
    enumCordbHashTableEnum, //  42
    enumCordbCodeEnum,      //  43
    enumCordbStackWalk,     //  44
    enumCordbEnumerator,    //  45
    enumCordbHeap,          //  48
    enumCordbHeapSegments,  //  47
    enumMaxDerived,         //
    enumMaxThis = 1024
} enumCordbDerived;



//-----------------------------------------------------------------------------
// Support for Native Breakpoints
//-----------------------------------------------------------------------------
struct NativePatch
{
    void * pAddress; // pointer into the LS address space.
    PRD_TYPE opcode; // opcode to restore with.

    inline bool operator==(NativePatch p2)
    {
        return memcmp(this, &p2, sizeof(p2)) == 0;
    }
};

//-----------------------------------------------------------------------------
// Cross-platform patch operations
//-----------------------------------------------------------------------------

// Remove the int3 from the remote address
HRESULT RemoveRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress, PRD_TYPE opcode);

// This flavor is assuming our caller already knows the opcode.
HRESULT ApplyRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress);

// Apply the patch and get the opcode that we're replacing.
HRESULT ApplyRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress, PRD_TYPE * pOpcode);


class CordbHashTable;

#define CORDB_COMMON_BASE_SIGNATURE 0x0d00d96a
#define CORDB_COMMON_BASE_SIGNATURE_DEAD 0x0dead0b1

// Common base for both CorPublish + CorDebug objects.
class CordbCommonBase : public IUnknown
{
public:
    // GENERIC: made this private as I'm changing the use of m_id for CordbClass, and
    // I want to make sure I catch all the places where m_id is used directly and cast
    // to/from tokens and/or (void*).
    UINT_PTR    m_id;

#ifdef _DEBUG
    static LONG m_saDwInstance[enumMaxDerived]; // instance x this
    static LONG m_saDwAlive[enumMaxDerived];
    static PVOID m_sdThis[enumMaxDerived][enumMaxThis];
    DWORD m_dwInstance;
    enumCordbDerived m_type;
#endif



private:
    DWORD       m_signature : 30;

    // Sticky bit set when we neuter an object. All methods (besides AddRef,Release,QI)
    // should check this bit and fail via the FAIL_IF_NEUTERED macro.
    DWORD        m_fIsNeutered : 1;

    // Mark that this object can be "neutered at will". NeuterList::SweepAllNeuterAtWillObjects
    // looks at this bit.
    // For some objects, we don't explicitly mark when the lifetime is up. The only way
    // we know is when external count goes to 0. This avoids forcing us to do cleanup
    // in the dtor (which may come at a bad time). Sticky bit set in BaseRelease().
    DWORD        m_fNeuterAtWill : 1;
public:

    static LONG s_CordbObjectUID;    // Unique ID for each object.
    static LONG s_TotalObjectCount; // total number of outstanding objects.


    void ValidateObject()
    {
        if( !IsValidObject() )
        {
            STRESS_LOG1(LF_ASSERT, LL_ALWAYS, "CordbCommonBase::IsValidObject() failed: %x\n", this);
            _ASSERTE(!"CordbCommonBase::IsValidObject() failed");
            FreeBuildDebugBreak();
        }
    }

    bool IsValidObject()
    {
        return (m_signature == CORDB_COMMON_BASE_SIGNATURE);
    }

    CordbCommonBase(UINT_PTR id, enumCordbDerived type)
    {
        init(id, type);
    }

    CordbCommonBase(UINT_PTR id)
    {
        init(id, enumCordbUnknown);
    }

    void init(UINT_PTR id, enumCordbDerived type)
    {
        // To help us track object leaks, we want to log when we create & destory CordbBase objects.
#ifdef _DEBUG
        InterlockedIncrement(&s_TotalObjectCount);
        InterlockedIncrement(&s_CordbObjectUID);

        LOG((LF_CORDB, LL_EVERYTHING, "Memory: CordbBase object allocated: this=%p, count=%d, id=%p, Type=%d\n", this, s_CordbObjectUID, id, type));
#endif

        m_signature = CORDB_COMMON_BASE_SIGNATURE;
        m_fNeuterAtWill = 0;
        m_fIsNeutered = 0;

        m_id = id;
        m_RefCount = 0;

#ifdef _DEBUG
        m_type = type;
        //m_dwInstance = CordbBase::m_saDwInstance[m_type];
        //InterlockedIncrement(&CordbBase::m_saDwInstance[m_type]);
        //InterlockedIncrement(&CordbBase::m_saDwAlive[m_type]);
        //if (m_dwInstance < enumMaxThis)
        //{
        //    m_sdThis[m_type][m_dwInstance] = this;
        //}
#endif
    }

    virtual ~CordbCommonBase()
    {
        // If we're deleting, we really should have released any outstanding reference.
        // If we call Release() on a deleted object, we'll av (especially b/c Release
        // may call delete again).
        CONSISTENCY_CHECK_MSGF(m_RefCount == 0, ("Deleting w/ non-zero ref count. 0x%08x", m_RefCount));

#ifdef _DEBUG
        //InterlockedDecrement(&CordbBase::m_saDwAlive[m_type]);
        //if (m_dwInstance < enumMaxThis)
        //{
        //    m_sdThis[m_type][m_dwInstance] = NULL;
        //}
#endif
        // To help us track object leaks, we want to log when we create & destory CordbBase objects.
        LOG((LF_CORDB, LL_EVERYTHING, "Memory: CordbBase object deleted: this=%p, id=%p, Refcount=0x%x\n", this, m_id, m_RefCount));

#ifdef _DEBUG
        LONG newTotalObjectsCount = InterlockedDecrement(&s_TotalObjectCount);
        _ASSERTE(newTotalObjectsCount >= 0);
#endif

        // Don't shutdown logic until everybody is done with it.
        // If we leak objects, this may mean that we never shutdown logging at all!
#if defined(_DEBUG) && defined(LOGGING)
        if (newTotalObjectsCount == 0)
        {
            ShutdownLogging();
        }
#endif
    }

    /*
        Member function behavior of a neutered COM object:

             1. AddRef(), Release(), QueryInterface() work as normal.
                 a. This gives folks who are responsible for pairing a Release() with
                    an AddRef() a chance to dereference their pointer and call Release()
                    when they are informed, explicitly or implicitly, that the object is neutered.

             2. Any other member function will return an error code unless documented.
                 a. If a member function returns information when the COM object is
                    neutered then the semantics of that function need to be documented.
                    (ie. If an AppDomain is unloaded and you have a reference to the COM
                    object representing the AppDomain, how _should_ it behave? That behavior
                    should be documented)


        Postcondions of Neuter():

             1. All circular references (aka back-pointers) are "broken". They are broken
                by calling Release() on all "Weak References" to the object. If you're a purist,
                these pointers should also be NULLed out.
                 a. Weak References/Strong References:
                     i. If any objects are not "reachable" from the root (ie. stack or from global pointers)
                         they should be reclaimed. If they are not, they are leaked and there is an issue.
                     ii. There must be a partial order on the objects such that if A < B then:
                         1. A has a reference to B. This reference is a "strong reference"
                         2. A, and thus B, is reachable from the root
                     iii. If a reference belongs in the partial order then it is a "strong reference" else
                         it is a weak reference.
         *** 2. Sufficient conditions to ensure no COM objects are leaked: ***
                a. When Neuter() is invoked:
                     i. Calles Release on all its weak references.
                     ii. Then, for each strong reference:
                         1. invoke Neuter()
                         2. invoke Release()
                     iii. If it's derived from a CordbXXX class, call Neuter() on the base class.
                         1. Sense Neuter() is virtual, use the scope specifier Cordb[BaseClass]::Neuter().
             3. All members return error codes, except:
                 a. Members of IUknown, AddRef(), Release(), QueryInterfac()
                 b. Those documented to have functionality when the object is neutered.
                     i. Neuter() still works w/o error. If it is invoke a second time it will have already
                        released all its strong and weak references so it could just return.


        Alternate design ideas:

             DESIGN: Note that it's possible for object B to have two parents in the partial order
                     and it must be documented which one is responsible for calling Neuter() on B.
                      1. For example, CordbCode could reasonably be a sibling of CordbFunction and CordbNativeFrame.
                         Which one should call Release()? For now we have CordbFunction call Release() on CordbCode.

             DESIGN: It is not a necessary condition in that Neuter() invoke Release() on all
                     it's strong references. Instead, it would be sufficient to ensure all object are released, that
                     each object call Release() on all its strong pointers in its destructor.
                      1. This might be done if its necessary for some member to return "tombstone"
                         information after the object has been netuered() which involves the siblings (wrt poset)
                         of the object. However, no sibling could access a parent (wrt poset) because
                         Neuter called Release() on all its weak pointers.

             DESIGN: Rename Neuter() to some name that more accurately reflect the semantics.
                     1. The three operations are:
                         a. ReleaseWeakPointers()
                         b. NeuterStrongPointers()
                         c. ReleaseStrongPointers()
                             1. Assert that it's done after NeuterStrongPointers()
                     2. That would introduce a bunch of functions... but it would be clear.

             DESIGN: CordbBase could provide a function to register strong and weak references. That way CordbBase
                     could implement a general version of ReleaseWeak/ReleaseStrong/NeuterStrongPointers(). This
                     would provide a very error resistant framework for extending the object model plus it would
                     be very explicit about what is going on.
                         One thing that might trip this is idea up is that if an object has two parents,
                         like the CordbCode might, then either both objects call Neuter or one is reference
                         is made weak.


        Our implementation:

           The graph formed by the strong references must remain acyclic.
           It's up to the developer (YOU!) to ensure that each Neuter
           function maintains that invariant.

           Here is the current Partial Order on CordbXXX objects. (All these classes
           eventually chain to CordbBase.Neuter() for completeness.)

           Cordb
              CordbProcess
                  CordbAppDomain
                      CordbBreakPoints
                      CordbAssembly
                      CordbModule
                          CordbClass
                          CordbFunction
                              CordbCode (Can we assert a thread will not reference
                                          the same CordbCode as a CordbFunction?)
                 CordbThread
                     CordbChains
                     CordbNativeFrame -> CordbFrame (Chain to baseClass)
                         CordbJITILFrame


            <TODO>TODO: Some Neuter functions have not yet been implemented due to time restrictions.</TODO>

            <TODO>TODO: Some weak references never have AddRef() called on them. If that's cool then
                  it should be stated in the documentation. Else it should be changed.</TODO>
*/

    virtual void Neuter();

    // Unsafe neuter for an object that's already dead.
    void UnsafeNeuterDeadObject();


#ifdef _DEBUG
    // For debugging (asserts, logging, etc) provide a pretty name (this is 1:1 w/ the VTable)
    // We provide a default impl in the base object in case this gets called from a dtor (virtuals
    // called from dtors use the base version, not the derived). A pure call would AV in that case.
    virtual const char * DbgGetName() { return "CordbBase"; };
#endif

    bool IsNeutered() const {LIMITED_METHOD_CONTRACT;  return m_fIsNeutered == 1; }
    bool IsNeuterAtWill() const { LIMITED_METHOD_CONTRACT; return m_fNeuterAtWill == 1; }
    void MarkNeuterAtWill() { LIMITED_METHOD_CONTRACT; m_fNeuterAtWill = 1; }

    //-----------------------------------------------------------
    // IUnknown support
    //----------------------------------------------------------

private:
    // We maintain both an internal + external refcount. This allows us to catch
    // if an external caller has too many releases.
    // low  bits are internal count, high  bits are external count
    // so Total count = (m_RefCount & CordbBase_InternalRefCountMask) + (m_RefCount >> CordbBase_ExternalRefCountShift);
    typedef LONGLONG       MixedRefCountSigned;
    typedef ULONGLONG      MixedRefCountUnsigned;
    typedef LONG           ExternalRefCount;
    MixedRefCountUnsigned  m_RefCount;
public:

    // Adjust the internal ref count.
    // These aren't available to the external world, so only internal code can manipulate the internal count.
    void InternalAddRef();
    void InternalRelease();

    // Derived versions of AddRef / Release will call these.
    // External AddRef & Release
    // These do not have any additional Asserts to enforce that we're not manipulating the external count
    // from internal.
    ULONG STDMETHODCALLTYPE BaseAddRef();
    ULONG STDMETHODCALLTYPE BaseRelease();

    // External ref count versions, with extra debug count to enforce that this is done externally.
    // When derive classes use these versions, it Asserts that we're not adjusting external counts from inside.
    // Thus we can be confident that we're *never* leaking external refs to these objects.
    // @todo - eventually everything should use these.
    ULONG STDMETHODCALLTYPE BaseAddRefEnforceExternal();
    ULONG STDMETHODCALLTYPE BaseReleaseEnforceExternal();

    // Do an AddRef against the External count. This is a semantics issue.
    // We use this when an internal component Addrefs out-parameters (which Cordbg will call Release on).
    // This just does a regular external AddRef().
    void ExternalAddRef();

protected:

    static void InitializeCommon();

private:
    static void AddDebugPrivilege();
};

#define CordbBase_ExternalRefCountShift 32
#define CordbBase_InternalRefCountMask 0xFFFFFFFF
#define CordbBase_InternalRefCountMax  0x7FFFFFFF

#ifdef _DEBUG
// Does the given Cordb object type have affinity to a CordbProcess object?
// This is only used for certain asserts.
inline bool DoesCordbObjectTypeHaveProcessPtr(enumCordbDerived type)
{
    return
        (type != enumCordbCodeEnum) &&
        (type != enumCordb) &&
        (type != enumCordbHashTableEnum);
}
#endif

// Base class specifically for CorDebug objects
class CordbBase : public CordbCommonBase
{
public:
    CordbBase(CordbProcess * pProcess, UINT_PTR id, enumCordbDerived type) : CordbCommonBase(id, type)
    {
        // CordbProcess can't pass 'this' to base class, per error C4355. So we pass null and set later.
        _ASSERTE((pProcess != NULL) ||
            ((type) == enumCordbProcess) ||
            !DoesCordbObjectTypeHaveProcessPtr(type));

        m_pProcess.Assign(pProcess);
    }

    CordbBase(CordbProcess * pProcess, UINT_PTR id) : CordbCommonBase(id)
    {
        _ASSERTE(pProcess != NULL);
        m_pProcess.Assign(pProcess);
    }

    virtual ~CordbBase()
    {
        // Derived classes should not have cleared out our pointer.
        // CordbProcess's Neuter explicitly nulls out its pointer to avoid circular reference.
        _ASSERTE(m_pProcess!= NULL ||
            (CordbCommonBase::m_type == enumCordbProcess) ||
            !DoesCordbObjectTypeHaveProcessPtr(CordbCommonBase::m_type));

        // Ideally, all CorDebug objects to be neutered by the time their dtor is called.
        // @todo - we're still working out neutering semantics for a few remaining objects, so we exclude
        // those from the assert.
        _ASSERTE(IsNeutered() ||
            (m_type == enumCordbBreakpoint) ||
            (m_type == enumCordbStepper));
    }

    // Neuter just the right-side state.
    virtual void Neuter();

    // Neuter both left-side state and right-side state.
    virtual void NeuterLeftSideResources();

    // Get the CordbProcess object that this CordbBase object is associated with (or NULL if there's none).
    CordbProcess * GetProcess() const
    {
        return m_pProcess;
    }
protected:
    // All objects need a strong pointer back to the process so that they can get access to key locks
    // held by the process (StopGo lock) so that they can synchronize their operations against neutering.
    // This pointer is cleared in our dtor, and not when we're neutered. Since we can't control when the
    // dtor is called (it's controlled by external references), we classify this as an external reference too.
    //
    // This is the only "strong" reference backpointer that objects need have. All other backpointers can be weak references
    // because when a parent object is neutered, it will null out all weak reference pointers in all of its children.
    // That will also break any potential cycles.
    RSUnsafeExternalSmartPtr<CordbProcess> m_pProcess;

};





//-----------------------------------------------------------------------------
// Macro to check if a CordbXXX object is neutered, and return a standard
// error code if it is.
// We pass the 'this' pointer of the object in because it gives us some extra
// flexibility and lets us log debug info.
// It is an API breach to access a neutered object.
//-----------------------------------------------------------------------------
#define FAIL_IF_NEUTERED(pThis) \
int _____Neuter_Status_Already_Marked; \
_____Neuter_Status_Already_Marked = 0; \
{\
    if (pThis->IsNeutered()) { \
            LOG((LF_CORDB, LL_ALWAYS, "Accessing a neutered object at %p\n", pThis)); \
            return ErrWrapper(CORDBG_E_OBJECT_NEUTERED); \
    } \
}

//-----------------------------------------------------------------------------
// Macro to check if a CordbXXX object is neutered, and return a standard
// error code if it is.
// We pass the 'this' pointer of the object in because it gives us some extra
// flexibility and lets us log debug info.
// It is an API breach to access a neutered object.
//-----------------------------------------------------------------------------
#define THROW_IF_NEUTERED(pThis) \
int _____Neuter_Status_Already_Marked; \
_____Neuter_Status_Already_Marked = 0; \
{\
    if (pThis->IsNeutered()) { \
            LOG((LF_CORDB, LL_ALWAYS, "Accessing a neutered object at %p\n", pThis)); \
            ThrowHR(CORDBG_E_OBJECT_NEUTERED); \
    } \
}

// We have an OK_IF_NEUTERED macro to say that this method can be safely
// called if we're neutered. Mostly for semantic benefits.
// Also, if a method is marked OK, then somebody won't go and add a 'fail'
// This is an extremely dangerous quality because:
// 1) it means that we have no synchronization (can't take the Stop-Go lock)
// 2) none of our backpointers are usable (they may be nulled out at anytime by another thread).
//    - this also means we absolutely can't send IPC events (since that requires a CordbProcess)
// 3) The only safe data are blittalbe embedded fields (eg, a pid or stack range)
//
// Any usage of this macro should clearly specify why this is safe.
#define OK_IF_NEUTERED(pThis) \
int _____Neuter_Status_Already_Marked; \
_____Neuter_Status_Already_Marked = 0;


//-------------------------------------------------------------------------------
// Simple COM enumerator pattern on a fixed list of items
//--------------------------------------------------------------------------------
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
class CordbEnumerator : public CordbBase, public EnumInterfaceType
{
private:
    // the list of items being enumerated over
    ElemType *m_items;
    // the number of items in the list
    DWORD m_countItems;
    // the index of the next item to be returned in the enumeration
    DWORD m_nextIndex;

public:
    // makes a copy of the elements in the "items" array
    CordbEnumerator(CordbProcess* pProcess, ElemType *items, DWORD elemCount);
    // assumes ownership of the elements in the "*items" array.
    // this avoids an extra allocation + copy
    CordbEnumerator(CordbProcess* pProcess, ElemType **items, DWORD elemCount);
    ~CordbEnumerator();

// IUnknown interface
    virtual COM_METHOD QueryInterface(REFIID riid, VOID** ppInterface);
    virtual ULONG STDMETHODCALLTYPE AddRef();
    virtual ULONG STDMETHODCALLTYPE Release();

// ICorDebugEnum interface
    virtual COM_METHOD Clone(ICorDebugEnum **ppEnum);
    virtual COM_METHOD GetCount(ULONG *pcelt);
    virtual COM_METHOD Reset();
    virtual COM_METHOD Skip(ULONG celt);

// ICorDebugXXXEnum interface
    virtual COM_METHOD Next(ULONG celt, ElemPublicType items[], ULONG *pceltFetched);

// CordbBase overrides
    virtual VOID Neuter();
};

// Converts T to U* by using QueryInterface
template<typename T, typename U, REFIID iid>
U* QueryInterfaceConvert(T obj);

// No conversion, just returns the argument
template<typename T>
T IdentityConvert(T obj);

// CorDebugGuidToTypeMapping-adapter used by CordbGuidToTypeEnumerator
// in the CordbEnumerator pattern
struct RsGuidToTypeMapping
{
    GUID iid;
    RSSmartPtr<CordbType> spType;
};

inline
CorDebugGuidToTypeMapping GuidToTypeMappingConvert(RsGuidToTypeMapping m)
{
    CorDebugGuidToTypeMapping result;
    result.iid = m.iid;
    result.pType = (ICorDebugType*)(m.spType.GetValue());
    result.pType->AddRef();
    return result;
}

//
// Some useful enumerators
//
typedef CordbEnumerator<RSSmartPtr<CordbThread>,
                        ICorDebugThread*,
                        ICorDebugThreadEnum, IID_ICorDebugThreadEnum,
                        QueryInterfaceConvert<RSSmartPtr<CordbThread>, ICorDebugThread, IID_ICorDebugThread> > CordbThreadEnumerator;

typedef CordbEnumerator<CorDebugBlockingObject,
                        CorDebugBlockingObject,
                        ICorDebugBlockingObjectEnum, IID_ICorDebugBlockingObjectEnum,
                        IdentityConvert<CorDebugBlockingObject> > CordbBlockingObjectEnumerator;

// Template classes must be fully defined rather than just declared in the header
#include "rsenumerator.hpp"


typedef CordbEnumerator<COR_SEGMENT,
                        COR_SEGMENT,
                        ICorDebugHeapSegmentEnum, IID_ICorDebugHeapSegmentEnum,
                        IdentityConvert<COR_SEGMENT> > CordbHeapSegmentEnumerator;

typedef CordbEnumerator<COR_MEMORY_RANGE,
                        COR_MEMORY_RANGE,
                        ICorDebugMemoryRangeEnum, IID_ICorDebugMemoryRangeEnum,
                        IdentityConvert<COR_MEMORY_RANGE> > CordbMemoryRangeEnumerator;

typedef CordbEnumerator<CorDebugExceptionObjectStackFrame,
                        CorDebugExceptionObjectStackFrame,
                        ICorDebugExceptionObjectCallStackEnum, IID_ICorDebugExceptionObjectCallStackEnum,
                        IdentityConvert<CorDebugExceptionObjectStackFrame> > CordbExceptionObjectCallStackEnumerator;

typedef CordbEnumerator<RsGuidToTypeMapping,
                        CorDebugGuidToTypeMapping,
                        ICorDebugGuidToTypeEnum, IID_ICorDebugGuidToTypeEnum,
                        GuidToTypeMappingConvert > CordbGuidToTypeEnumerator;

typedef CordbEnumerator<RSSmartPtr<CordbVariableHome>,
                        ICorDebugVariableHome*,
                        ICorDebugVariableHomeEnum, IID_ICorDebugVariableHomeEnum,
                        QueryInterfaceConvert<RSSmartPtr<CordbVariableHome>, ICorDebugVariableHome, IID_ICorDebugVariableHome> > CordbVariableHomeEnumerator;

// ----------------------------------------------------------------------------
// Hash table for CordbBase objects.
// - Uses Internal AddRef/Release (not external)
// - Templatize for type-safety w/ Cordb objects
// - Many hashtables are implicitly protected by a lock. For debug-only, we
//   explicitly associate w/ an optional RSLock and assert that lock is held on access.
// ----------------------------------------------------------------------------

struct CordbHashEntry
{
    FREEHASHENTRY entry;
    CordbBase *pBase;
};

class CordbHashTable : private CHashTableAndData<CNewDataNoThrow>
{
private:
    bool    m_initialized;
    SIZE_T  m_count;

    BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2)
    {
        LIMITED_METHOD_CONTRACT;

        return ((ULONG_PTR)k1) != (reinterpret_cast<const CordbHashEntry *>(pc2))->pBase->m_id;
    }

    ULONG HASH(ULONG_PTR id)
    {
        return (ULONG)(id);
    }

    SIZE_T KEY(UINT_PTR id)
    {
        return (SIZE_T)id;
    }

public:
    bool IsInitialized();

#ifndef DACCESS_COMPILE
    CordbHashTable(ULONG size)
    : CHashTableAndData<CNewDataNoThrow>(size), m_initialized(false), m_count(0)
    {
#ifdef _DEBUG
    m_pDbgLock = NULL;
    m_dbgChangeCount = 0;
#endif
    }
    virtual ~CordbHashTable();

#ifdef _DEBUG
    // CordbHashTables may be protected by a lock. For debug-builds, we can associate
    // the hash w/ that lock and then assert if it's not held.
    void DebugSetRSLock(RSLock * pLock)
    {
        m_pDbgLock = pLock;
    }
    int GetChangeCount() { return m_dbgChangeCount; }
private:
    void AssertIsProtected();

    // Increment the Change count. This can be used to check if the hashtable changes while being enumerated.
    void DbgIncChangeCount() { m_dbgChangeCount++; }

    int m_dbgChangeCount;
    RSLock * m_pDbgLock;
#else
    // RSLock association is a no-op on free builds.
    void AssertIsProtected() { };
    void DbgIncChangeCount() { };
#endif // _DEBUG

public:


#endif

    ULONG32 GetCount()
    {
        return ((ULONG32)m_count);
    }

    // These operators are unsafe b/c they have no typesafety.
    // Use a derived CordbSafeHashTable<T> instead.
    HRESULT UnsafeAddBase(CordbBase *pBase);
    HRESULT UnsafeSwapBase(CordbBase* pBaseOld, CordbBase* pBaseNew);
    CordbBase *UnsafeGetBase(ULONG_PTR id, BOOL fFab = TRUE);
    CordbBase *UnsafeRemoveBase(ULONG_PTR id);

    CordbBase *UnsafeFindFirst(HASHFIND *find);
    CordbBase *UnsafeFindNext(HASHFIND *find);

    // Unlocked versions don't assert that the lock us held.
    CordbBase *UnsafeUnlockedFindFirst(HASHFIND *find);
    CordbBase *UnsafeUnlockedFindNext(HASHFIND *find);

};


// Typesafe wrapper around a normal hash table
// T is expected to be a derived clas of CordbBase
// Note that this still isn't fully typesafe.  Ideally we'd take a strongly-typed key
// instead of UINT_PTR (the type could have a fixed relationship to T, or could be
// an additional template argument like standard template hash tables like std::hash_map<K,V>)
template <class T>
class CordbSafeHashTable : public CordbHashTable
{
public:
#ifndef DACCESS_COMPILE
    CordbSafeHashTable<T>(ULONG size) : CordbHashTable(size)
    {
    }
#endif
    // Typesafe wrappers
    HRESULT AddBase(T * pBase) { return UnsafeAddBase(pBase); }

    // Either add (eg, future cals to GetBase will succeed) or throw.
    void AddBaseOrThrow(T * pBase)
    {
        HRESULT hr = AddBase(pBase);
        IfFailThrow(hr);
    }
    HRESULT SwapBase(T* pBaseOld, T* pBaseNew) { return UnsafeSwapBase(pBaseOld, pBaseNew); }
    // Move the function definition of GetBase to rspriv.inl to work around gcc 2.9.5 warnings
    T* GetBase(ULONG_PTR id, BOOL fFab = TRUE);
    T* GetBaseOrThrow(ULONG_PTR id, BOOL fFab = TRUE);

    T* RemoveBase(ULONG_PTR id) { return static_cast<T*>(UnsafeRemoveBase(id)); }

    T* FindFirst(HASHFIND *find) { return static_cast<T*>(UnsafeFindFirst(find)); }
    T* FindNext(HASHFIND *find)  { return static_cast<T*>(UnsafeFindNext(find)); }

    // Neuter all items and clear
    void NeuterAndClear(RSLock * pLock);

    void CopyToArray(RSPtrArray<T> * pArray);
    void TransferToArray(RSPtrArray<T> * pArray);
};


class CordbHashTableEnum : public CordbBase,
public ICorDebugProcessEnum,
public ICorDebugBreakpointEnum,
public ICorDebugStepperEnum,
public ICorDebugThreadEnum,
public ICorDebugModuleEnum,
public ICorDebugAppDomainEnum,
public ICorDebugAssemblyEnum
{
    // Private ctors. Use build function to access.
    CordbHashTableEnum(
        CordbBase * pOwnerObj,
        NeuterList * pOwnerList,
        CordbHashTable *table,
        const _GUID &id);

public:
    static void BuildOrThrow(
        CordbBase * pOwnerObj,
        NeuterList * pOwnerList,
        CordbHashTable *table,
        const _GUID &id,
        RSInitHolder<CordbHashTableEnum> * pHolder);

    CordbHashTableEnum(CordbHashTableEnum *cloneSrc);

    ~CordbHashTableEnum();
    virtual void Neuter();


#ifdef _DEBUG
    // For debugging (asserts, logging, etc) provide a pretty name (this is 1:1 w/ the VTable)
    virtual const char * DbgGetName() { return "CordbHashTableEnum"; };
#endif


    HRESULT Next(ULONG celt, CordbBase *bases[], ULONG *pceltFetched);

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugEnum
    //-----------------------------------------------------------

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    //-----------------------------------------------------------
    // ICorDebugProcessEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugProcess *processes[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(processes, ICorDebugProcess *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)processes, pceltFetched));
    }

    //-----------------------------------------------------------
    // ICorDebugBreakpointEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugBreakpoint *breakpoints[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(breakpoints, ICorDebugBreakpoint *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)breakpoints, pceltFetched));
    }

    //-----------------------------------------------------------
    // ICorDebugStepperEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugStepper *steppers[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(steppers, ICorDebugStepper *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)steppers, pceltFetched));
    }

    //-----------------------------------------------------------
    // ICorDebugThreadEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugThread *threads[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(threads, ICorDebugThread *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)threads, pceltFetched));
    }

    //-----------------------------------------------------------
    // ICorDebugModuleEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugModule *modules[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(modules, ICorDebugModule *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)modules, pceltFetched));
    }

    //-----------------------------------------------------------
    // ICorDebugAppDomainEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugAppDomain *appdomains[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(appdomains, ICorDebugAppDomain *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)appdomains, pceltFetched));
    }
    //-----------------------------------------------------------
    // ICorDebugAssemblyEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugAssembly *assemblies[],
                    ULONG *pceltFetched)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(assemblies, ICorDebugAssembly *,
            celt, true, true);
        VALIDATE_POINTER_TO_OBJECT(pceltFetched, ULONG *);

        return (Next(celt, (CordbBase **)assemblies, pceltFetched));
    }
private:
    // Owning object is our link to the CordbProcess* tree. Never null until we're neutered.
    // NeuterList is related to the owning object. Need to cache it so that we can pass it on
    // to our clones.
    CordbBase *     m_pOwnerObj; // provides us w/ a CordbProcess*
    NeuterList *    m_pOwnerNeuterList;


    CordbHashTable *m_table;
    bool            m_started;
    bool            m_done;
    HASHFIND        m_hashfind;
    REFIID          m_guid;
    ULONG           m_iCurElt;
    ULONG           m_count;
    BOOL            m_fCountInit;

#ifdef _DEBUG
    // timestampt of hashtable when we start enumerating it. Useful for detecting if the table
    // changes underneath us.
    int             m_DbgChangeCount;
    void AssertValid();
#else
    void AssertValid() { }
#endif

private:
    //These factor code between Next & Skip
    HRESULT PrepForEnum(CordbBase **pBase);

    // Note that the set of types advanced by Pre & by Post are disjoint, and
    // that the union of these two sets are all possible types enuerated by
    // the CordbHashTableEnum.
    HRESULT AdvancePreAssign(CordbBase **pBase);
    HRESULT AdvancePostAssign(CordbBase **pBase,
                              CordbBase     **b,
                              CordbBase   **bEnd);

    // This factors some code that initializes the module enumerator.
    HRESULT SetupModuleEnum();

};


//-----------------------------------------------------------------------------
// Neuter List
// Dtors can be called at any time (whenever Cordbg calls Release, which is outside
// of our control), so we never want to do significant work in a dtor
// (this includes sending IPC events + neutering).
// So objects can queue themselves up to be neutered at a safe time.
//
// Items in a NeuterList should only contain state in the Right-Side.
// If the item holds resources in the left-side, it should be placed on a
// code:LeftSideResourceCleanupList
//-----------------------------------------------------------------------------
class NeuterList
{
public:
    NeuterList();
    ~NeuterList();

    // Add an object to be neutered.
    // Anybody calls this to add themselves to the list.
    // This will add it to the list and maintain an internal reference to it.
    void Add(CordbProcess * pProcess, CordbBase * pObject);

    // Add w/o checking for safety. Should only be used by Process-list enum.
    void UnsafeAdd(CordbProcess * pProcess, CordbBase * pObject);

    // Neuter everything on the list.
    // This should only be called by the "owner", but we can't really enforce that.
    // This will release all internal references and empty the list.
    void NeuterAndClear(CordbProcess * pProcess);

    // Sweep for all objects that are marked as 'm_fNeuterAtWill'.
    // Neuter and remove these.
    void SweepAllNeuterAtWillObjects(CordbProcess * pProcess);

protected:
    struct Node
    {
        RSSmartPtr<CordbBase> m_pObject;
        Node * m_pNext;
    };

    // Manipulating the list is done under the Process lock.
    Node * m_pHead;
};

//-----------------------------------------------------------------------------
// This list is for objects that hold left-side resources.
// If the object does not hold left-side resources, it can be placed on a
// code:NeuterList
//-----------------------------------------------------------------------------
class LeftSideResourceCleanupList : public NeuterList
{
public:
    // dispose everything contained in the list by calling SafeDispose() on each element
    void SweepNeuterLeftSideResources(CordbProcess * pProcess);
    void NeuterLeftSideResourcesAndClear(CordbProcess * pProcess);
};

//-------------------------------------------------------------------------
//
// Optional<T>
// Stores a value along with a bit indicating whether the value is valid.
//
// This is particularly useful for LS data read via DAC.  We need to gracefully
// handle missing data, and we may want to track independent pieces of data
// separately (often with lazy initialization).  It's essential that we can't
// easily lose track of whether the data has been cached yet or not.  So
// rather than have extra "isValid" bools everywhere, we use this class to
// encapsulate the validity bit in with the data, and ASSERT that it is true
// whenever reading out the data.
// Note that the client must still remember to call GetValue only when HasValue
// is true.  Since C++ doesn't have type-safe sum types, we can't enforce this
// explicitly at compile time (ML-style datatypes and pattern matching is perfect
// for this).
//
// Note that we could consider adding some operator overloads to make using
// instances of this class more transparent.  Experience will tell if this
// is a good idea or not.
//
template <typename T>
class Optional
{
public:
    // By default, initialize to invalid
    Optional() : m_fHasValue(false), m_value(T()) {}

    // Allow implicit initialization from a value (for copyable T)
    Optional(const T& val) : m_fHasValue(true), m_value(val) {}

    // Returns true if a value has been stored
    bool HasValue() const    { return m_fHasValue; }

    // Extract the value.  Can only be called when HasValue is true.
    const T& GetValue()        { _ASSERTE(m_fHasValue); return m_value; }

    // Get a writable pointer to the value structure, for filling in uncopyable data structures
    T * GetValueAddr() { return &m_value; }

    // Explicitly mark this object as having a value (for use after writing to it directly using
    // GetValueAddr.  Not necessary for simple/primitive types).
    void SetHasValue() { m_fHasValue = true; }

    // Also gets compiler-default copy constructor and assignment operator if T has them

private:
    bool m_fHasValue;
    T m_value;
};


/* ------------------------------------------------------------------------- *
 * Cordb class
 * ------------------------------------------------------------------------- */

class Cordb : public CordbBase, public ICorDebug, public ICorDebugRemote
{
public:
    Cordb(CorDebugInterfaceVersion iDebuggerVersion);
    virtual ~Cordb();
    virtual void Neuter();



#ifdef _DEBUG_IMPL
    virtual const char * DbgGetName() { return "Cordb"; }

    // Under Debug, we keep some extra state for tracking leaks. The goal is that
    // we can assert that we aren't leaking internal refs. We'd like to assert that
    // we're not leaking external refs, but since we can't force Cordbg to release,
    // we can't really assert that.
    // So the idea is that when Cordbg has released its last Cordb object, that
    // all internal references have been released.
    // Unfortunately, certain CordbBase objects are unrooted and thus we have no
    // good time to neuter them and clean up any internal references they may hold.
    // So we keep count of those guys too.
    static LONG s_DbgMemTotalOutstandingCordb;
    static LONG s_DbgMemTotalOutstandingInternalRefs;
#endif

    //
    // Turn this on to enable an array which will contain all objects that have
    // not been completely released.
    //
    // #define TRACK_OUTSTANDING_OBJECTS 1

#ifdef TRACK_OUTSTANDING_OBJECTS

#define MAX_TRACKED_OUTSTANDING_OBJECTS 256
    static void *Cordb::s_DbgMemOutstandingObjects[MAX_TRACKED_OUTSTANDING_OBJECTS];
    static LONG Cordb::s_DbgMemOutstandingObjectMax;
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebug
    //-----------------------------------------------------------

    HRESULT SetTargetCLR(HMODULE hmodTargetCLR);

    COM_METHOD Initialize();
    COM_METHOD Terminate();
    COM_METHOD SetManagedHandler(ICorDebugManagedCallback *pCallback);
    COM_METHOD SetUnmanagedHandler(ICorDebugUnmanagedCallback *pCallback);
    COM_METHOD CreateProcess(LPCWSTR lpApplicationName,
                             __in_z LPWSTR lpCommandLine,
                             LPSECURITY_ATTRIBUTES lpProcessAttributes,
                             LPSECURITY_ATTRIBUTES lpThreadAttributes,
                             BOOL bInheritHandles,
                             DWORD dwCreationFlags,
                             PVOID lpEnvironment,
                             LPCWSTR lpCurrentDirectory,
                             LPSTARTUPINFOW lpStartupInfo,
                             LPPROCESS_INFORMATION lpProcessInformation,
                             CorDebugCreateProcessFlags debuggingFlags,
                             ICorDebugProcess **ppProcess);
    COM_METHOD DebugActiveProcess(DWORD dwProcessId, BOOL fWin32Attach, ICorDebugProcess **ppProcess);
    COM_METHOD EnumerateProcesses(ICorDebugProcessEnum **ppProcess);
    COM_METHOD GetProcess(DWORD dwProcessId, ICorDebugProcess **ppProcess);
    COM_METHOD CanLaunchOrAttach(DWORD dwProcessId, BOOL win32DebuggingEnabled);

    //-----------------------------------------------------------
    // CorDebug
    //-----------------------------------------------------------

    static COM_METHOD CreateObjectV1(REFIID id, void **object);
#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
    static COM_METHOD CreateObjectTelesto(REFIID id, void ** pObject);
#endif // FEATURE_DBGIPC_TRANSPORT_DI
    static COM_METHOD CreateObject(CorDebugInterfaceVersion iDebuggerVersion, DWORD pid, LPCWSTR lpApplicationGroupId, REFIID id, void **object);

    //-----------------------------------------------------------
    // ICorDebugRemote
    //-----------------------------------------------------------

    COM_METHOD CreateProcessEx(ICorDebugRemoteTarget * pRemoteTarget,
                               LPCWSTR lpApplicationName,
                               __in_z LPWSTR lpCommandLine,
                               LPSECURITY_ATTRIBUTES lpProcessAttributes,
                               LPSECURITY_ATTRIBUTES lpThreadAttributes,
                               BOOL bInheritHandles,
                               DWORD dwCreationFlags,
                               PVOID lpEnvironment,
                               LPCWSTR lpCurrentDirectory,
                               LPSTARTUPINFOW lpStartupInfo,
                               LPPROCESS_INFORMATION lpProcessInformation,
                               CorDebugCreateProcessFlags debuggingFlags,
                               ICorDebugProcess ** ppProcess);

    COM_METHOD DebugActiveProcessEx(ICorDebugRemoteTarget * pRemoteTarget,
                                    DWORD dwProcessId,
                                    BOOL fWin32Attach,
                                    ICorDebugProcess ** ppProcess);


    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------

    HRESULT CreateProcessCommon(ICorDebugRemoteTarget * pRemoteTarget,
                                LPCWSTR lpApplicationName,
                                __in_z LPWSTR lpCommandLine,
                                LPSECURITY_ATTRIBUTES lpProcessAttributes,
                                LPSECURITY_ATTRIBUTES lpThreadAttributes,
                                BOOL bInheritHandles,
                                DWORD dwCreationFlags,
                                PVOID lpEnvironment,
                                LPCWSTR lpCurrentDirectory,
                                LPSTARTUPINFOW lpStartupInfo,
                                LPPROCESS_INFORMATION lpProcessInformation,
                                CorDebugCreateProcessFlags debuggingFlags,
                                ICorDebugProcess **ppProcess);

    HRESULT DebugActiveProcessCommon(ICorDebugRemoteTarget * pRemoteTarget, DWORD id, BOOL win32Attach, ICorDebugProcess **ppProcess);

    void EnsureCanLaunchOrAttach(BOOL fWin32DebuggingEnabled);

    void EnsureAllowAnotherProcess();
    void AddProcess(CordbProcess* process);
    void RemoveProcess(CordbProcess* process);
    CordbSafeHashTable<CordbProcess> *GetProcessList();

    void LockProcessList();
    void UnlockProcessList();

    #ifdef _DEBUG
    bool ThreadHasProcessListLock();
    #endif


    HRESULT SendIPCEvent(CordbProcess * pProcess,
                         DebuggerIPCEvent * pEvent,
                         SIZE_T eventSize);

    void ProcessStateChanged();

    HRESULT WaitForIPCEventFromProcess(CordbProcess* process,
                                       CordbAppDomain *appDomain,
                                       DebuggerIPCEvent* event);

private:
    Cordb(CorDebugInterfaceVersion iDebuggerVersion, const ProcessDescriptor& pd);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    RSExtSmartPtr<ICorDebugManagedCallback>    m_managedCallback;
    RSExtSmartPtr<ICorDebugManagedCallback2>   m_managedCallback2;
    RSExtSmartPtr<ICorDebugManagedCallback3>   m_managedCallback3;
    RSExtSmartPtr<ICorDebugManagedCallback4>   m_managedCallback4;
    RSExtSmartPtr<ICorDebugUnmanagedCallback>  m_unmanagedCallback;

    CordbRCEventThread*         m_rcEventThread;

    CorDebugInterfaceVersion    GetDebuggerVersion() const;

#ifdef FEATURE_CORESYSTEM
    HMODULE GetTargetCLR() { return m_targetCLR; }
#endif

private:
    bool IsCreateProcessSupported();
    bool IsInteropDebuggingSupported();
    void CheckCompatibility();

    CordbSafeHashTable<CordbProcess> m_processes;

    // List to track outstanding CordbProcessEnum objects.
    NeuterList                  m_pProcessEnumList;

    RSLock                      m_processListMutex;
    BOOL                        m_initialized;

    // This is the version of the ICorDebug APIs that the debugger believes it's consuming.
    CorDebugInterfaceVersion    m_debuggerSpecifiedVersion;

    // Store information about the process to be debugged
    ProcessDescriptor m_pd;

//Note - this code could be useful outside coresystem, but keeping the change localized
// because we are late in the win8 release
#ifdef FEATURE_CORESYSTEM
    HMODULE m_targetCLR;
#endif
};




/* ------------------------------------------------------------------------- *
 * AppDomain class
 * ------------------------------------------------------------------------- */

// Provides the implementation for ICorDebugAppDomain, ICorDebugAppDomain2,
// and ICorDebugAppDomain3
class CordbAppDomain : public CordbBase,
                        public ICorDebugAppDomain,
                        public ICorDebugAppDomain2,
                        public ICorDebugAppDomain3,
                        public ICorDebugAppDomain4
{
public:
    // Create a CordbAppDomain object based on a pointer to the AppDomain instance in the CLR
    CordbAppDomain(CordbProcess *  pProcess,
                   VMPTR_AppDomain vmAppDomain);

    virtual ~CordbAppDomain();

    virtual void Neuter();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbAppDomain"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugController
    //-----------------------------------------------------------

    COM_METHOD Stop(DWORD dwTimeout);
    COM_METHOD Continue(BOOL fIsOutOfBand);
    COM_METHOD IsRunning(BOOL * pbRunning);
    COM_METHOD HasQueuedCallbacks(ICorDebugThread * pThread,
                                  BOOL *            pbQueued);
    COM_METHOD EnumerateThreads(ICorDebugThreadEnum ** ppThreads);
    COM_METHOD SetAllThreadsDebugState(CorDebugThreadState state, ICorDebugThread * pExceptThisThread);

    // Deprecated, returns E_NOTIMPL
    COM_METHOD Detach();

    COM_METHOD Terminate(unsigned int exitCode);

    COM_METHOD CanCommitChanges(
        ULONG                              cSnapshots,
        ICorDebugEditAndContinueSnapshot * pSnapshots[],
        ICorDebugErrorInfoEnum **          pError);

    COM_METHOD CommitChanges(
        ULONG                              cSnapshots,
        ICorDebugEditAndContinueSnapshot * pSnapshots[],
        ICorDebugErrorInfoEnum **          pError);

    //-----------------------------------------------------------
    // ICorDebugAppDomain
    //-----------------------------------------------------------
    /*
     * GetProcess returns the process containing the app domain
     */

    COM_METHOD GetProcess(ICorDebugProcess ** ppProcess);

    /*
     * EnumerateAssemblies enumerates all assemblies in the app domain
     */

    COM_METHOD EnumerateAssemblies(ICorDebugAssemblyEnum ** ppAssemblies);

    COM_METHOD GetModuleFromMetaDataInterface(IUnknown *         pIMetaData,
                                              ICorDebugModule ** ppModule);
    /*
     * EnumerateBreakpoints returns an enum of all active breakpoints
     * in the app domain.  This includes all types of breakpoints :
     * function breakpoints, data breakpoints, etc.
     */

    COM_METHOD EnumerateBreakpoints(ICorDebugBreakpointEnum ** ppBreakpoints);

    /*
     * EnumerateSteppers returns an enum of all active steppers in the app domain.
     */

    COM_METHOD EnumerateSteppers(ICorDebugStepperEnum ** ppSteppers);

    // Deprecated, always returns true.
    COM_METHOD IsAttached(BOOL * pfAttached);

    // Returns the friendly name of the AppDomain
    COM_METHOD GetName(ULONG32   cchName,
                       ULONG32 * pcchName,
                       __out_ecount_part_opt(cchName, *pcchName) WCHAR     szName[]);

    /*
     * GetObject returns the runtime app domain object.
     * Note:   This method is not yet implemented.
     */

    COM_METHOD GetObject(ICorDebugValue ** ppObject);

    // Deprecated, does nothing
    COM_METHOD Attach();
    COM_METHOD GetID(ULONG32 * pId);

    //-----------------------------------------------------------
    // ICorDebugAppDomain2 APIs
    //-----------------------------------------------------------
    COM_METHOD GetArrayOrPointerType(CorElementType   elementType,
                                     ULONG32          nRank,
                                     ICorDebugType *  pTypeArg,
                                     ICorDebugType ** ppResultType);

    COM_METHOD GetFunctionPointerType(ULONG32          cTypeArgs,
                                      ICorDebugType *  rgpTypeArgs[],
                                      ICorDebugType ** ppResultType);

    //-----------------------------------------------------------
    // ICorDebugAppDomain3 APIs
    //-----------------------------------------------------------
    COM_METHOD GetCachedWinRTTypesForIIDs(
                        ULONG32               cGuids,
                        GUID                * guids,
                        ICorDebugTypeEnum * * ppTypesEnum);

    COM_METHOD GetCachedWinRTTypes(
                        ICorDebugGuidToTypeEnum * * ppType);

    //-----------------------------------------------------------
    // ICorDebugAppDomain4
    //-----------------------------------------------------------
    COM_METHOD GetObjectForCCW(CORDB_ADDRESS ccwPointer, ICorDebugValue **ppManagedObject);

    // Get the VMPTR for this appdomain.
    VMPTR_AppDomain GetADToken() { return m_vmAppDomain; }

    // Given a metadata interface, find the module in this appdomain that matches it.
    CordbModule * GetModuleFromMetaDataInterface(IUnknown *pIMetaData);

    // Lookup a module from the cache.  Create and to the cache if needed.
    CordbModule * LookupOrCreateModule(VMPTR_Module vmModuleToken, VMPTR_DomainFile vmDomainFileToken);

    // Lookup a module from the cache.  Create and to the cache if needed.
    CordbModule * LookupOrCreateModule(VMPTR_DomainFile vmDomainFileToken);

    // Callback from DAC for module enumeration
    static void ModuleEnumerationCallback(VMPTR_DomainFile vmModule, void * pUserData);

    // Use DAC to add any modules for this assembly.
    void PrepopulateModules();

    void InvalidateName() { m_strAppDomainName.Clear(); }

public:
    ULONG               m_AppDomainId;

    CordbAssembly * LookupOrCreateAssembly(VMPTR_DomainAssembly vmDomainAssembly);
    CordbAssembly * LookupOrCreateAssembly(VMPTR_Assembly vmAssembly);
    void RemoveAssemblyFromCache(VMPTR_DomainAssembly vmDomainAssembly);


    CordbSafeHashTable<CordbBreakpoint>  m_breakpoints;

    // Unique objects that represent the use of some
                                         // basic ELEMENT_TYPE's as type parameters.  These
                                         // are shared across the entire process.  We could
                                         // go and try to find the classes corresponding to these
                                         // element types but it seems simpler just to keep
                                         // them as special cases.
    CordbSafeHashTable<CordbType>        m_sharedtypes;

    CordbAssembly * CacheAssembly(VMPTR_DomainAssembly vmDomainAssembly);
    CordbAssembly * CacheAssembly(VMPTR_Assembly vmAssembly);


    // Cache of modules in this appdomain. In the VM, modules live in an assembly.
    // This cache lives on the appdomain because we generally want to do appdomain (or process)
    // wide lookup.
    // This is indexed by VMPTR_DomainFile, which has appdomain affinity.
    // This is populated by code:CordbAppDomain::LookupOrCreateModule (which may be invoked
    // anytime the RS gets hold of a VMPTR), and are removed at the unload event.
    CordbSafeHashTable<CordbModule>      m_modules;
private:
    // Cache of assemblies in this appdomain.
    // This is indexed by VMPTR_DomainAssembly, which has appdomain affinity.
    // This is populated by code:CordbAppDomain::LookupOrCreateAssembly (which may be invoked
    // anytime the RS gets hold of a VMPTR), and are removed at the unload event.
    CordbSafeHashTable<CordbAssembly>    m_assemblies;

    static void AssemblyEnumerationCallback(VMPTR_DomainAssembly vmDomainAssembly, void * pThis);
    void PrepopulateAssembliesOrThrow();

    // Use DAC to refresh our name
    HRESULT RefreshName();

    StringCopyHolder    m_strAppDomainName;

    NeuterList          m_TypeNeuterList;  // List of types owned by this AppDomain.

    // List of Sweepable objects owned by this AppDomain.
    // This includes some objects taht hold resources in the left-side (mainly
    // as CordbHandleValue, see code:CordbHandleValue::Dispose), as well as:
    // - Cordb*Value objects that survive across continues and have appdomain affinity.
    LeftSideResourceCleanupList          m_SweepableNeuterList;

    VMPTR_AppDomain     m_vmAppDomain;
public:
    // The "Long" exit list is for items that don't get neutered until the appdomain exits.
    // The "Sweepable" exit list is for items that may be neuterable sooner than AD exit.
    // By splitting out the list, we can just try to sweep the "Sweepable" list and we
    // don't waste any time sweeping things on the "Long" list that aren't neuterable anyways.
    NeuterList * GetLongExitNeuterList() { return &m_TypeNeuterList; }
    LeftSideResourceCleanupList * GetSweepableExitNeuterList() { return &m_SweepableNeuterList; }

    void AddToTypeList(CordbBase *pObject);

};


/* ------------------------------------------------------------------------- *
 * Assembly class
 * ------------------------------------------------------------------------- */

class CordbAssembly : public CordbBase, public ICorDebugAssembly, ICorDebugAssembly2
{
public:
    CordbAssembly(CordbAppDomain *      pAppDomain,
                  VMPTR_Assembly        vmAssembly,
                  VMPTR_DomainAssembly  vmDomainAssembly);
    virtual ~CordbAssembly();
    virtual void Neuter();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbAssembly"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugAssembly
    //-----------------------------------------------------------

    /*
     * GetProcess returns the process containing the assembly
     */
    COM_METHOD GetProcess(ICorDebugProcess ** ppProcess);

    // Gets the AppDomain containing this assembly
    COM_METHOD GetAppDomain(ICorDebugAppDomain ** ppAppDomain);

    /*
     * EnumerateModules enumerates all modules in the assembly
     */
    COM_METHOD EnumerateModules(ICorDebugModuleEnum ** ppModules);

    /*
     * GetCodeBase returns the code base used to load the assembly
     */
    COM_METHOD GetCodeBase(ULONG32   cchName,
                           ULONG32 * pcchName,
                           __out_ecount_part_opt(cchName, *pcchName) WCHAR     szName[]);

    // returns the filename of the assembly, or "<unknown>" for in-memory assemblies
    COM_METHOD GetName(ULONG32   cchName,
                       ULONG32 * pcchName,
                       __out_ecount_part_opt(cchName, *pcchName) WCHAR     szName[]);


    //-----------------------------------------------------------
    // ICorDebugAssembly2
    //-----------------------------------------------------------

    /*
     * IsFullyTrusted returns a flag indicating whether the security system
     * has granted the assembly full trust.
     */
    COM_METHOD IsFullyTrusted(BOOL * pbFullyTrusted);

    //-----------------------------------------------------------
    // internal accessors
    //-----------------------------------------------------------

#ifdef _DEBUG
    void DbgAssertAssemblyDeleted();

    static void DbgAssertAssemblyDeletedCallback(VMPTR_DomainAssembly vmDomainAssembly, void * pUserData);
#endif // _DEBUG

    CordbAppDomain * GetAppDomain()     { return m_pAppDomain; }

    VMPTR_DomainAssembly    GetDomainAssemblyPtr() { return m_vmDomainAssembly; }
private:
    VMPTR_Assembly          m_vmAssembly;
    VMPTR_DomainAssembly    m_vmDomainAssembly;
    CordbAppDomain *        m_pAppDomain;

    StringCopyHolder        m_strAssemblyFileName;
    Optional<BOOL>          m_foptIsFullTrust;
};


//-----------------------------------------------------------------------------
// Describe what to do w/ a win32 debug event
//-----------------------------------------------------------------------------
class Reaction
{
public:
    enum Type
    {
        // Inband events: Dispatch to Cordbg
        // safe for stopping the shell and communicating with the runtime
        cInband,

        // workaround. Inband event, but NewEvent =false
        cInband_NotNewEvent,

        // This is a debug event that corresponds with getting to the beginning
        // of a first chance hijack.
        cFirstChanceHijackStarted,

        // This is the debug event that corresponds with getting to the end of
        // a hijack. To continue we need to restore an unhijacked context
        cInbandHijackComplete,

        // This is a debug event which corresponds to re-hiting a previous
        // IB event after returning from the hijack. Now we have already dispatched it
        // so we know how the user wants it to be continued
        // Continue immediately with the previously determined
        cInbandExceptionRetrigger,

        // This debug event is a breakpoint in unmanaged code that we placed. It will need
        // the M2UHandoffHijack to run the in process breakpoint handling code.
        cBreakpointRequiringHijack,

        // Oob events: Dispatch to Cordbg
        // Not safe stopping events. They must be continued immediately.
        cOOB,

        // CLR internal exception, Continue(not_handled), don't dispatch
        // The CLR expects this exception and will deal with it properly.
        cCLR,

        // Don't dispatch. Continue(DBG_CONTINUE).
        // Common for flare.
        cIgnore
    };

    Type GetType() const { return m_type; };

#ifdef _DEBUG
    const char * GetReactionName()
    {
        switch(m_type)
        {
            case cInband: return "cInband";
            case cInband_NotNewEvent: return "cInband_NotNewEvent";
            case cFirstChanceHijackStarted: return "cFirstChanceHijackStarted";
            case cInbandHijackComplete: return "cInbandHijackComplete";
            case cInbandExceptionRetrigger: return "cInbandExceptionRetrigger";
            case cBreakpointRequiringHijack: return "cBreakpointRequiringHijack";
            case cOOB: return "cOOB";
            case cCLR: return "cCLR";
            case cIgnore: return "cIgnore";
            default: return "<unknown>";
        }
    }
    int GetLine()
    {
        return m_line;
    }
#endif

    Reaction(Type t, int line) : m_type(t) {
#ifdef _DEBUG
        m_line = line;

        LOG((LF_CORDB, LL_EVERYTHING, "Reaction:%s (determined on line: %d)\n", GetReactionName(), line));
#endif
    };

    void operator=(const Reaction & other)
    {
        m_type = other.m_type;
#ifdef _DEBUG
        m_line = other.m_line;
#endif
    }

protected:
    Type m_type;

#ifdef _DEBUG
    // Under a debug build, track the line # for where this came from.
    int m_line;
#endif
};

// Macro for creating a Reaction.
#define REACTION(type) Reaction(Reaction::type, __LINE__)

// Different forms of Unmanaged Continue
enum EUMContinueType
{
    cOobUMContinue,
    cInternalUMContinue,
    cRealUMContinue
};

/* ------------------------------------------------------------------------- *
 * Process class
 * ------------------------------------------------------------------------- */


#ifdef _DEBUG
// On debug, we can afford a larger native event queue..
const int DEBUG_EVENTQUEUE_SIZE = 30;
#else
const int DEBUG_EVENTQUEUE_SIZE = 10;
#endif

void DeleteIPCEventHelper(DebuggerIPCEvent *pDel);


// Private interface on CordbProcess that ShimProcess needs to emulate V2 functionality.
// The fact that we need private hooks means that V3 is not sufficiently finished to allow building
// a V2 debugger. This interface should shrink over time (and eventually go away) as the functionality gets exposed
// publicly.
// CordbProcess calls back into ShimProcess too, so the public surface of code:ShimProcess plus
// the spots in CordbProcess that call them are additional surface area that may need to addressed
// to make the shim public.
class IProcessShimHooks
{
public:
    // Get the OS Process descriptor of the target.
    virtual const ProcessDescriptor* GetProcessDescriptor() = 0;

    // Request a synchronization for attach.
    // This essentially just sends an AsyncBreak to the left-side. Once the target is
    // synchronized, the Shim can use inspection to send all the various fake-attach events.
    //
    // Once the shim has a way of requesting a synchronization from out-of-process for an
    // arbitrary running target that's not stopped at a managed debug event, we can
    // remove this.
    virtual void QueueManagedAttachIfNeeded() = 0;

    // Hijack a thread at an unhandled exception to allow us to resume executing the target so
    // that the helper thread can run and service IPC requests. This is also needed to allow
    // func-eval at a 2nd-chance exception
    //
    // This will require an architectural change to remove. Either:
    // - actions like func-eval / synchronization may call this directly themselves.
    // - the CLR's managed Unhandled-exception event is moved out of the native
    // unhandled-exception event, thus making native unhandled exceptions uninteresting to ICorDebug.
    // - everything is out-of-process, and so the CLR doesn't need to continue after an unhandled
    // native exception.
    virtual BOOL HijackThreadForUnhandledExceptionIfNeeded(DWORD dwThreadId) = 0;

#ifdef FEATURE_INTEROP_DEBUGGING
    // Private hook to do the bulk of the interop-debugging goo. This includes hijacking inband
    // events and queueing them so that the helper-thread can run.
    //
    // We can remove this once we kill the helper-thread, or after enough functionality is
    // out-of-process that the debugger doesn't need the helper thread when stopped at an event.
    virtual void HandleDebugEventForInteropDebugging(const DEBUG_EVENT * pEvent) = 0;
#endif // FEATURE_INTEROP_DEBUGGING

    // Get the modules in the order that they were loaded. This is needed to send the fake-attach events
    // for module load in the right order.
    //
    // This can be removed once ICorDebug's enumerations are ordered.
    virtual void GetModulesInLoadOrder(
        ICorDebugAssembly * pAssembly,
        RSExtSmartPtr<ICorDebugModule>* pModules,
        ULONG countModules) = 0;

    // Get the assemblies in the order that they were loaded. This is needed to send the fake-attach events
    // for assembly load in the right order.
    //
    // This can be removed once ICorDebug's enumerations are ordered.
    virtual void GetAssembliesInLoadOrder(
        ICorDebugAppDomain * pAppDomain,
        RSExtSmartPtr<ICorDebugAssembly>* pAssemblies,
        ULONG countAssemblies) = 0;

    // Queue up fake connection events for attach.
    // ICorDebug doesn't expose any enumeration for connections, so the shim needs to call into a
    // private hook to enumerate them for attach.
    virtual void QueueFakeConnectionEvents() = 0;

    // This finishes initializing the IPC channel between the LS + RS, which includes duplicating
    // some handles and events.
    //
    // This can be removed once the IPC channel is completely gone and all communication goes
    // soley through the data-target.
    virtual void FinishInitializeIPCChannel() = 0;

    // Called when stopped at a managed debug event to request a synchronization.
    // This can be replaced when we expose synchronization from ICorDebug.
    // The fact that the debuggee is at a managed debug event greatly simplifies the request here
    // (in contrast to QueueManagedAttachIfNeeded). It means that we can just flip a flag from
    // out-of-process, and when the debuggee thread resumes, it can check that flag and do the
    // synchronization from in-process.
    virtual void RequestSyncAtEvent()= 0;

    virtual bool IsThreadSuspendedOrHijacked(ICorDebugThread * pThread) = 0;
};


// entry for the array of connections in EnumerateConnectionsData
struct EnumerateConnectionsEntry
{
public:
    StringCopyHolder m_pName;   // name of the connection
    DWORD            m_dwID;    // ID of the connection
};

// data structure used in the callback for enumerating connections (code:CordbProcess::QueueFakeConnectionEvents)
struct EnumerateConnectionsData
{
public:
    ~EnumerateConnectionsData()
    {
        if (m_pEntryArray != NULL)
        {
            delete [] m_pEntryArray;
            m_pEntryArray = NULL;
        }
    }

    CordbProcess * m_pThis;                     // the "this" process
    EnumerateConnectionsEntry * m_pEntryArray;  // an array of connections to be filled in
    UINT32         m_uIndex;                    // the next entry in the array to be filled
};

// data structure used in the callback for asserting that an appdomain has been deleted
// (code:CordbProcess::DbgAssertAppDomainDeleted)
struct DbgAssertAppDomainDeletedData
{
public:
    CordbProcess *  m_pThis;
    VMPTR_AppDomain m_vmAppDomainDeleted;
};

class CordbProcess :
    public CordbBase,
    public ICorDebugProcess,
    public ICorDebugProcess2,
    public ICorDebugProcess3,
    public ICorDebugProcess4,
    public ICorDebugProcess5,
    public ICorDebugProcess7,
    public ICorDebugProcess8,
    public ICorDebugProcess11,
    public IDacDbiInterface::IAllocator,
    public IDacDbiInterface::IMetaDataLookup,
    public IProcessShimHooks
{
    // Ctor is private. Use OpenVirtualProcess instead.
    CordbProcess(ULONG64 clrInstanceId, IUnknown * pDataTarget, HMODULE hDacModule,  Cordb * pCordb, const ProcessDescriptor * pProcessDescriptor, ShimProcess * pShim);

public:

    virtual ~CordbProcess();
    virtual void Neuter();

    // Neuter left-side resources for all children
    void NeuterChildrenLeftSideResources();

    // Neuter all of all children, but not the actual process object.
    void NeuterChildren();


    // The way to instantiate a new CordbProcess object.
    // @dbgtodo  managed pipeline - this is not fully active in all scenarios yet.
    static HRESULT OpenVirtualProcess(ULONG64 clrInstanceId,
                                      IUnknown * pDataTarget,
                                      HMODULE hDacModule,
                                      Cordb * pCordb,
                                      const ProcessDescriptor * pProcessDescriptor,
                                      ShimProcess * pShim,
                                      CordbProcess ** ppProcess);

    // Helper function to determine whether this ICorDebug is compatibile with a debugger
    // designed for the specified major version
    static bool IsCompatibleWith(DWORD clrMajorVersion);

    //-----------------------------------------------------------
    // IMetaDataLookup
    // -----------------------------------------------------------
    IMDInternalImport * LookupMetaData(VMPTR_PEFile vmPEFile, bool &isILMetaDataForNGENImage);

    // Helper functions for LookupMetaData implementation
    IMDInternalImport * LookupMetaDataFromDebugger(VMPTR_PEFile vmPEFile,
                                                   bool &isILMetaDataForNGENImage,
                                                   CordbModule * pModule);

    IMDInternalImport * LookupMetaDataFromDebuggerForSingleFile(CordbModule * pModule,
                                                                LPCWSTR pwszImagePath,
                                                                DWORD dwTimeStamp,
                                                                DWORD dwImageSize);


    //-----------------------------------------------------------
    // IDacDbiInterface::IAllocator
    //-----------------------------------------------------------

    void * Alloc(SIZE_T lenBytes);
    void Free(void * p);

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbProcess"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return BaseAddRefEnforceExternal();
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return BaseReleaseEnforceExternal();
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugController
    //-----------------------------------------------------------

    COM_METHOD Stop(DWORD dwTimeout);
    COM_METHOD Deprecated_Continue();
    COM_METHOD IsRunning(BOOL *pbRunning);
    COM_METHOD HasQueuedCallbacks(ICorDebugThread *pThread, BOOL *pbQueued);
    COM_METHOD EnumerateThreads(ICorDebugThreadEnum **ppThreads);
    COM_METHOD SetAllThreadsDebugState(CorDebugThreadState state,
                                       ICorDebugThread *pExceptThisThread);
    COM_METHOD Detach();
    COM_METHOD Terminate(unsigned int exitCode);

    COM_METHOD CanCommitChanges(
        ULONG cSnapshots,
        ICorDebugEditAndContinueSnapshot *pSnapshots[],
        ICorDebugErrorInfoEnum **pError);

    COM_METHOD CommitChanges(
        ULONG cSnapshots,
        ICorDebugEditAndContinueSnapshot *pSnapshots[],
        ICorDebugErrorInfoEnum **pError);

    COM_METHOD Continue(BOOL fIsOutOfBand);
    COM_METHOD ThreadForFiberCookie(DWORD fiberCookie,
                                    ICorDebugThread **ppThread);
    COM_METHOD GetHelperThreadID(DWORD *pThreadID);

    //-----------------------------------------------------------
    // ICorDebugProcess
    //-----------------------------------------------------------

    COM_METHOD GetID(DWORD *pdwProcessId);
    COM_METHOD GetHandle(HANDLE *phProcessHandle);
    COM_METHOD EnableSynchronization(BOOL bEnableSynchronization);
    COM_METHOD GetThread(DWORD dwThreadId, ICorDebugThread **ppThread);
    COM_METHOD EnumerateBreakpoints(ICorDebugBreakpointEnum **ppBreakpoints);
    COM_METHOD EnumerateSteppers(ICorDebugStepperEnum **ppSteppers);
    COM_METHOD EnumerateObjects(ICorDebugObjectEnum **ppObjects);
    COM_METHOD IsTransitionStub(CORDB_ADDRESS address, BOOL *pbTransitionStub);
    COM_METHOD EnumerateModules(ICorDebugModuleEnum **ppModules);
    COM_METHOD GetModuleFromMetaDataInterface(IUnknown *pIMetaData,
                                              ICorDebugModule **ppModule);
    COM_METHOD SetStopState(DWORD threadID, CorDebugThreadState state);
    COM_METHOD IsOSSuspended(DWORD threadID, BOOL *pbSuspended);
    COM_METHOD GetThreadContext(DWORD threadID, ULONG32 contextSize,
                                BYTE context[]);
    COM_METHOD SetThreadContext(DWORD threadID, ULONG32 contextSize,
                                BYTE context[]);
    COM_METHOD ReadMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[],
                          SIZE_T *read);
    COM_METHOD WriteMemory(CORDB_ADDRESS address, DWORD size, BYTE buffer[],
                           SIZE_T *written);

    COM_METHOD ClearCurrentException(DWORD threadID);

    /*
     * EnableLogMessages enables/disables sending of log messages to the
     * debugger for logging.
     */
    COM_METHOD EnableLogMessages(BOOL fOnOff);

    /*
     * ModifyLogSwitch modifies the specified switch's severity level.
     */
    COM_METHOD ModifyLogSwitch(__in_z WCHAR *pLogSwitchName, LONG lLevel);

    COM_METHOD EnumerateAppDomains(ICorDebugAppDomainEnum **ppAppDomains);
    COM_METHOD GetObject(ICorDebugValue **ppObject);

    //-----------------------------------------------------------
    // ICorDebugProcess2
    //-----------------------------------------------------------

    COM_METHOD GetThreadForTaskID(TASKID taskId, ICorDebugThread2 ** ppThread);
    COM_METHOD GetVersion(COR_VERSION* pInfo);

    COM_METHOD SetUnmanagedBreakpoint(CORDB_ADDRESS address, ULONG32 bufsize, BYTE buffer[], ULONG32 * bufLen);
    COM_METHOD ClearUnmanagedBreakpoint(CORDB_ADDRESS address);
    COM_METHOD GetCodeAtAddress(CORDB_ADDRESS address, ICorDebugCode ** pCode, ULONG32 * offset);

    COM_METHOD SetDesiredNGENCompilerFlags(DWORD pdwFlags);
    COM_METHOD GetDesiredNGENCompilerFlags(DWORD *pdwFlags );

    COM_METHOD GetReferenceValueFromGCHandle(UINT_PTR handle, ICorDebugReferenceValue **pOutValue);

    //-----------------------------------------------------------
    // ICorDebugProcess3
    //-----------------------------------------------------------

    // enables or disables CustomNotifications of a given type
    COM_METHOD SetEnableCustomNotification(ICorDebugClass * pClass, BOOL fEnable);

    //-----------------------------------------------------------
    // ICorDebugProcess4
    //-----------------------------------------------------------
    COM_METHOD Filter(
        const BYTE pRecord[],
        DWORD countBytes,
        CorDebugRecordFormat format,
        DWORD dwFlags,
        DWORD dwThreadId,
        ICorDebugManagedCallback *pCallback,
        DWORD * pContinueStatus);

    COM_METHOD ProcessStateChanged(CorDebugStateChange eChange);

    //-----------------------------------------------------------
    // ICorDebugProcess5
    //-----------------------------------------------------------
    COM_METHOD GetGCHeapInformation(COR_HEAPINFO *pHeapInfo);
    COM_METHOD EnumerateHeap(ICorDebugHeapEnum **ppObjects);
    COM_METHOD EnumerateHeapRegions(ICorDebugHeapSegmentEnum **ppRegions);
    COM_METHOD GetObject(CORDB_ADDRESS addr, ICorDebugObjectValue **pObject);
    COM_METHOD EnableNGENPolicy(CorDebugNGENPolicy ePolicy);
    COM_METHOD EnumerateGCReferences(BOOL enumerateWeakReferences, ICorDebugGCReferenceEnum **ppEnum);
    COM_METHOD EnumerateHandles(CorGCReferenceType types, ICorDebugGCReferenceEnum **ppEnum);
    COM_METHOD GetTypeID(CORDB_ADDRESS obj, COR_TYPEID *pId);
    COM_METHOD GetTypeForTypeID(COR_TYPEID id, ICorDebugType **ppType);
    COM_METHOD GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout);
    COM_METHOD GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout);
    COM_METHOD GetTypeFields(COR_TYPEID id, ULONG32 celt, COR_FIELD fields[], ULONG32 *pceltNeeded);

    //-----------------------------------------------------------
    // ICorDebugProcess7
    //-----------------------------------------------------------
    COM_METHOD SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode flags);

    //-----------------------------------------------------------
    // ICorDebugProcess8
    //-----------------------------------------------------------
    COM_METHOD EnableExceptionCallbacksOutsideOfMyCode(BOOL enableExceptionsOutsideOfJMC);

    //-----------------------------------------------------------
    // ICorDebugProcess10 (To be removed in .NET 6, in a separate cleanup PR)
    //-----------------------------------------------------------
    COM_METHOD EnableGCNotificationEvents(BOOL fEnable);

    //-----------------------------------------------------------
    // ICorDebugProcess11
    //-----------------------------------------------------------
    COM_METHOD EnumerateLoaderHeapMemoryRegions(ICorDebugMemoryRangeEnum **ppRanges);

    //-----------------------------------------------------------
    // Methods not exposed via a COM interface.
    //-----------------------------------------------------------

    HRESULT ContinueInternal(BOOL fIsOutOfBand);
    HRESULT StopInternal(DWORD dwTimeout, VMPTR_AppDomain pAppDomainToken);

    // Sets an unmanaged breakpoint at the target address
    HRESULT SetUnmanagedBreakpointInternal(CORDB_ADDRESS address, ULONG32 bufsize, BYTE buffer[], ULONG32 * bufLen);

    // Allocate a buffer within the target and return the range. Throws on error.
    TargetBuffer GetRemoteBuffer(ULONG cbBuffer); // throws

    // Same as above except also copy-in the contents of a RS buffer using WriteProcessMemory
    HRESULT GetAndWriteRemoteBuffer(CordbAppDomain *pDomain, unsigned int bufferSize, const void *bufferFrom, void **ppBuffer);

    /*
     * This will release a previously allocated left side buffer.
     * Often they are deallocated by the LS itself.
     */
    HRESULT ReleaseRemoteBuffer(void **ppBuffer);


    void TargetConsistencyCheck(bool fExpression);

    // Activate interop-debugging, after the process has initially been Init()
    void EnableInteropDebugging();

    HRESULT Init();
    void DeleteQueuedEvents();
    void CleanupHalfBakedLeftSide();
    void Terminating(BOOL fDetach);

    CordbThread * TryLookupThread(VMPTR_Thread vmThread);
    CordbThread * TryLookupOrCreateThreadByVolatileOSId(DWORD dwThreadId);
    CordbThread * TryLookupThreadByVolatileOSId(DWORD dwThreadId);
    CordbThread * LookupOrCreateThread(VMPTR_Thread vmThread);

    void QueueManagedAttachIfNeeded();
    void QueueManagedAttachIfNeededWorker();
    HRESULT QueueManagedAttach();

    void DetachShim();

    // Flush for when the process is running.
    void FlushProcessRunning();

    // Flush all state.
    void FlushAll();

    BOOL HijackThreadForUnhandledExceptionIfNeeded(DWORD dwThreadId);

    // Filter a CLR notification (subset of exceptions).
    void FilterClrNotification(
        DebuggerIPCEvent * pManagedEvent,
        RSLockHolder * pLockHolder,
        ICorDebugManagedCallback * pCallback);

    // Wrapper to invoke IClrDataTarget4::ContinueStatusChanged
    void ContinueStatusChanged(DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus);


    // Request a synchronization to occur after a debug event is dispatched.
    void RequestSyncAtEvent();

    //
    // Basic managed event plumbing
    //

    // This is called on the first IPC event from the debuggee. It initializes state.
    void FinishInitializeIPCChannel();
    void FinishInitializeIPCChannelWorker();

    // This is called on each IPC event from the debuggee.
    void HandleRCEvent(DebuggerIPCEvent * pManagedEvent, RSLockHolder * pLockHolder, ICorDebugManagedCallback * pCallback);

    // Queue the RC event.
    void QueueRCEvent(DebuggerIPCEvent * pManagedEvent);

    // This marshals a managed debug event from the
    void MarshalManagedEvent(DebuggerIPCEvent * pManagedEvent);

    // This copies a managed debug event from the IPC block and to pManagedEvent.
    // The event still needs to be marshalled.
    void CopyRCEventFromIPCBlock(DebuggerIPCEvent * pManagedEvent);

    // This copies a managed debug event out of the Native-Debug event envelope.
    // The event still needs to be marshalled.
    bool CopyManagedEventFromTarget(const EXCEPTION_RECORD * pRecord, DebuggerIPCEvent * pLocalManagedEvent);

    // Helper for Filter() to verify parameters and return a type-safe exception record.
    const EXCEPTION_RECORD * ValidateExceptionRecord(
        const BYTE pRawRecord[],
        DWORD countBytes,
        CorDebugRecordFormat format);

    // Helper to read a structure from the target.
    template<typename T>
    HRESULT SafeReadStruct(CORDB_ADDRESS pRemotePtr, T* pLocalBuffer);

    // Helper to write a structure into the target.
    template<typename T>
    HRESULT SafeWriteStruct(CORDB_ADDRESS pRemotePtr, const T* pLocalBuffer);

    // Reads a buffer from the target
    HRESULT SafeReadBuffer(TargetBuffer tb, BYTE * pLocalBuffer, BOOL throwOnError = TRUE);

    // Writes a buffer to the target
    void SafeWriteBuffer(TargetBuffer tb, const BYTE * pLocalBuffer);

#if defined(FEATURE_INTEROP_DEBUGGING)
    void DuplicateHandleToLocalProcess(HANDLE * pLocalHandle, RemoteHANDLE * pRemoteHandle);
#endif // FEATURE_INTEROP_DEBUGGING

    bool IsThreadSuspendedOrHijacked(ICorDebugThread * pICorDebugThread);

    // Handle the result of the ctrlC trap.
    void HandleControlCTrapResult(HRESULT result);

    // Helper to get ProcessDescriptor internally.
    const ProcessDescriptor* GetProcessDescriptor();

    HRESULT GetRuntimeOffsets();

    // Are we blocked waiting fo ran OOB event to be continue?
    bool IsWaitingForOOBEvent()
    {
#ifdef FEATURE_INTEROP_DEBUGGING
        return m_outOfBandEventQueue != NULL;
#else
        // If no interop, then we're never waiting for an OOB event.
        return false;
#endif
    }

    //
    // Shim  callbacks to simulate fake attach events.
    //


    // Callback for Shim to get the assemblies in load order
    void GetAssembliesInLoadOrder(
        ICorDebugAppDomain * pAppDomain,
        RSExtSmartPtr<ICorDebugAssembly>* pAssemblies,
        ULONG countAssemblies);

    // Callback for Shim to get the modules in load order
    void GetModulesInLoadOrder(
        ICorDebugAssembly * pAssembly,
        RSExtSmartPtr<ICorDebugModule>* pModules,
        ULONG countModules);

    // Functions to queue fake Connection events on attach.
    static void CountConnectionsCallback(DWORD id, LPCWSTR pName, void * pUserData);
    static void EnumerateConnectionsCallback(DWORD id, LPCWSTR pName, void * pUserData);
    void QueueFakeConnectionEvents();



    void DispatchRCEvent();

    // Dispatch a single event via the callbacks.
    void RawDispatchEvent(
        DebuggerIPCEvent *          pEvent,
        RSLockHolder *              pLockHolder,
        ICorDebugManagedCallback *  pCallback1,
        ICorDebugManagedCallback2 * pCallback2,
        ICorDebugManagedCallback3 * pCallback3,
        ICorDebugManagedCallback4 * pCallback4);

    void MarkAllThreadsDirty();

    bool CheckIfLSExited();

    void Lock()
    {
        // Lock Hierarchy - shouldn't have List lock when taking/release the process lock.

        m_processMutex.Lock();
        LOG((LF_CORDB, LL_EVERYTHING, "P::Lock enter, this=0x%p\n", this));
    }

    void Unlock()
    {
        // Lock Hierarchy - shouldn't have List lock when taking/releasing the process lock.

        LOG((LF_CORDB, LL_EVERYTHING, "P::Lock leave, this=0x%p\n", this));
        m_processMutex.Unlock();
    }

#ifdef _DEBUG
    bool ThreadHoldsProcessLock()
    {
        return m_processMutex.HasLock();
    }
#endif

    // Expose the process lock.
    // This is the main lock in V3.
    RSLock * GetProcessLock()
    {
        return &m_processMutex;
    }


    // @dbgtodo  synchronization - the SG lock goes away in V3.
    // Expose the stop-go lock b/c varios Cordb objects in our process tree may need to take it.
    RSLock * GetStopGoLock()
    {
        return &m_StopGoLock;
    }


    void UnrecoverableError(HRESULT errorHR,
                            unsigned int errorCode,
                            const char *errorFile,
                            unsigned int errorLine);
    HRESULT CheckForUnrecoverableError();
    void VerifyControlBlock();

    // The implementation of EnumerateThreads without the public API error checks
    VOID InternalEnumerateThreads(RSInitHolder<CordbHashTableEnum> * ppThreads);

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------

    // Is it safe to send events to the LS?
    bool IsSafeToSendEvents() { return !m_unrecoverableError && !m_terminated && !m_detached; }

    bool IsWin32EventThread();

    void HandleSyncCompleteRecieved();

    // Send a truly asynchronous IPC event.
    void SendAsyncIPCEvent(DebuggerIPCEventType t);

    HRESULT SendIPCEvent(DebuggerIPCEvent *event, SIZE_T eventSize)
    {
        // @dbgtodo - eventually remove this when all IPC events are gone.
        // In V3 paths, we can't send IPC events.
        if (GetShim() == NULL)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000, "!! Can't send IPC event in V3. %s", IPCENames::GetName(event->type));
            return E_NOTIMPL;
        }
        _ASSERTE(m_cordb != NULL);
        return (m_cordb->SendIPCEvent(this, event, eventSize));
    }

    void InitAsyncIPCEvent(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type,
                      VMPTR_AppDomain vmAppDomain)
    {
        // Async events only allowed for the following:
        _ASSERTE(type == DB_IPCE_ATTACHING);

        InitIPCEvent(ipce, type, false, vmAppDomain);
        ipce->asyncSend = true;
    }

    void InitIPCEvent(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type,
                      bool twoWay,
                      VMPTR_AppDomain vmAppDomain
                      )
    {
        // zero out the event in case we try and use any uninitialized fields
        memset( ipce, 0, sizeof(DebuggerIPCEvent) );

        _ASSERTE((!vmAppDomain.IsNull()) ||
                 type == DB_IPCE_GET_GCHANDLE_INFO ||
                 type == DB_IPCE_ENABLE_LOG_MESSAGES ||
                 type == DB_IPCE_MODIFY_LOGSWITCH ||
                 type == DB_IPCE_ASYNC_BREAK ||
                 type == DB_IPCE_CONTINUE ||
                 type == DB_IPCE_GET_BUFFER ||
                 type == DB_IPCE_RELEASE_BUFFER ||
                 type == DB_IPCE_IS_TRANSITION_STUB ||
                 type == DB_IPCE_ATTACHING ||
                 type == DB_IPCE_APPLY_CHANGES ||
                 type == DB_IPCE_CONTROL_C_EVENT_RESULT ||
                 type == DB_IPCE_SET_REFERENCE ||
                 type == DB_IPCE_SET_ALL_DEBUG_STATE ||
                 type == DB_IPCE_GET_THREAD_FOR_TASKID ||
                 type == DB_IPCE_DETACH_FROM_PROCESS ||
                 type == DB_IPCE_INTERCEPT_EXCEPTION ||
                 type == DB_IPCE_GET_NGEN_COMPILER_FLAGS ||
                 type == DB_IPCE_SET_NGEN_COMPILER_FLAGS ||
                 type == DB_IPCE_SET_VALUE_CLASS);

        ipce->type = type;
        ipce->hr = S_OK;
        ipce->processId = 0;
        ipce->vmAppDomain = vmAppDomain;
        ipce->vmThread = VMPTR_Thread::NullPtr();
        ipce->replyRequired = twoWay;
        ipce->asyncSend = false;
        ipce->next = NULL;
    }

    // Looks up a previously constructed CordbClass instance without creating. May return NULL if the
    // CordbClass instance doesn't exist.
    CordbClass * LookupClass(ICorDebugAppDomain * pAppDomain, VMPTR_DomainFile vmDomainFile, mdTypeDef classToken);

    CordbModule * LookupOrCreateModule(VMPTR_DomainFile vmDomainFile);

#ifdef FEATURE_INTEROP_DEBUGGING
    CordbUnmanagedThread *GetUnmanagedThread(DWORD dwThreadId)
    {
        _ASSERTE(ThreadHoldsProcessLock());
        return m_unmanagedThreads.GetBase(dwThreadId);
    }
#endif // FEATURE_INTEROP_DEBUGGING

    /*
     * This will cleanup the patch table, releasing memory,etc.
     */
    void ClearPatchTable();

    /*
     * This will grab the patch table from the left side & go through
     * it to gather info needed for faster access.  If address,size,buffer
     * are passed in, while going through the table we'll undo patches
     * in buffer at the same time
     */
    HRESULT RefreshPatchTable(CORDB_ADDRESS address = NULL, SIZE_T size = NULL, BYTE buffer[] = NULL);

    // Find if a patch exists at a given address.
    HRESULT FindPatchByAddress(CORDB_ADDRESS address, bool *patchFound, bool *patchIsUnmanaged);

    enum AB_MODE
    {
        AB_READ,
        AB_WRITE
    };

    /*
     * Once we've called RefreshPatchTable to get the patch table,
     * this routine will iterate through the patches & either apply
     * or unapply the patches to buffer. AB_READ => Replaces patches
     * in buffer with the original opcode, AB_WRTE => replace opcode
     * with breakpoint instruction, caller is responsible for
     * updating the patchtable back to the left side.
     *
     * <TODO>@todo Perf Instead of a copy, undo the changes
     * Since the 'buffer' arg is an [in] param, we're not supposed to
     * change it.  If we do, we'll allocate & copy it to bufferCopy
     * (we'll also set *pbUpdatePatchTable to true), otherwise we
     * don't manipuldate bufferCopy (so passing a NULL in for
     * reading is fine).</TODO>
     */
    HRESULT AdjustBuffer(CORDB_ADDRESS address,
                         SIZE_T size,
                         BYTE buffer[],
                         BYTE **bufferCopy,
                         AB_MODE mode,
                         BOOL *pbUpdatePatchTable = NULL);

    /*
     * AdjustBuffer, above, doesn't actually update the local patch table
     * if asked to do a write.  It stores the changes alongside the table,
     * and this will cause the changes to be written to the table (for
     * a range of left-side addresses
     */
    void CommitBufferAdjustments(CORDB_ADDRESS start,
                                 CORDB_ADDRESS end);

    /*
     * Clear the stored changes, or they'll sit there until we
     * accidentally commit them
     */
    void ClearBufferAdjustments();




    //-----------------------------------------------------------
    // Accessors for key synchronization fields.
    //-----------------------------------------------------------

    // If CAD is NULL, returns true if all appdomains (ie, the entire process)
    // is synchronized.  Otherwise, returns true if the specified appdomain is
    // synch'd.
    bool GetSynchronized();
    void SetSynchronized(bool fSynch);

    void IncStopCount();
    void DecStopCount();

    // Gets the exact stop count. You need the Proecss lock for this.
    int GetStopCount();

    // Just gets whether we're stopped or not (m_stopped > 0).
    // You only need the StopGo lock for this.
    // This is biases towards returning false.
    bool IsStopped();

    bool GetSyncCompleteRecv();
    void SetSyncCompleteRecv(bool fSyncRecv);


    // Cordbg may not always continue during a callback; but we really shouldn't do meaningful
    // work after a callback has returned yet before they've called continue. Thus we may need
    // to remember some state at the time of dispatch so that we do stuff at continue.
    // Only example here is neutering... we'd like to Neuter an object X after the ExitX callback,
    // but we can't neuter it until Continue. So remember X when we dispatch, and neuter this at continue.
    // Use a smart ptr to keep it alive until we neuter it.

    // Add objects to various neuter lists.
    // NeuterOnContinue is for all objects that can be neutered once we continue.
    // NeuterOnExit is for all objects that can survive continues (but are neutered on process shutdown).
    // If an object's external ref count goes to 0, it gets promoted to the NeuterOnContinue list.
    void AddToNeuterOnExitList(CordbBase *pObject);
    void AddToNeuterOnContinueList(CordbBase *pObject);

    NeuterList * GetContinueNeuterList() { return &m_ContinueNeuterList; }
    NeuterList * GetExitNeuterList() { return &m_ExitNeuterList; }

    void AddToLeftSideResourceCleanupList(CordbBase * pObject);

    // Routines to read and write thread context records between the processes safely.
    HRESULT SafeReadThreadContext(LSPTR_CONTEXT pRemoteContext, DT_CONTEXT * pCtx);
    HRESULT SafeWriteThreadContext(LSPTR_CONTEXT pRemoteContext, const DT_CONTEXT * pCtx);

#ifdef FEATURE_INTEROP_DEBUGGING
    // Record a win32 event for debugging purposes.
    void DebugRecordWin32Event(const DEBUG_EVENT * pEvent, CordbUnmanagedThread * pUThread);
#endif // FEATURE_INTEROP_DEBUGGING

    //-----------------------------------------------------------
    // Interop Helpers
    //-----------------------------------------------------------

    // Get the DAC interface.
    IDacDbiInterface * GetDAC();

    // Get the data-target, which provides access to the debuggee.
    ICorDebugDataTarget * GetDataTarget();

    BOOL IsDacInitialized();

    void ForceDacFlush();


#ifdef FEATURE_INTEROP_DEBUGGING
    // Deal with native debug events for the interop-debugging scenario.
    void HandleDebugEventForInteropDebugging(const DEBUG_EVENT * pEvent);

    void ResumeHijackedThreads();

    //@todo - We should try to make these all private
    CordbUnmanagedThread *HandleUnmanagedCreateThread(DWORD dwThreadId, HANDLE hThread, void *lpThreadLocalBase);

    HRESULT ContinueOOB();
    void QueueUnmanagedEvent(CordbUnmanagedThread *pUThread, const DEBUG_EVENT *pEvent);
    void DequeueUnmanagedEvent(CordbUnmanagedThread *pUThread);
    void QueueOOBUnmanagedEvent(CordbUnmanagedThread *pUThread, const DEBUG_EVENT *pEvent);
    void DequeueOOBUnmanagedEvent(CordbUnmanagedThread *pUThread);
    void DispatchUnmanagedInBandEvent();
    void DispatchUnmanagedOOBEvent();
    bool ExceptionIsFlare(DWORD exceptionCode, const void *exceptionAddress);

    bool IsSpecialStackOverflowCase(CordbUnmanagedThread *pUThread, const DEBUG_EVENT *pEvent);

    HRESULT SuspendUnmanagedThreads();
    HRESULT ResumeUnmanagedThreads();

    HRESULT HijackIBEvent(CordbUnmanagedEvent * pUnmanagedEvent);

    BOOL HasUndispatchedNativeEvents();
    BOOL HasUserUncontinuedNativeEvents();
#endif // FEATURE_INTEROP_DEBUGGING

    HRESULT StartSyncFromWin32Stop(BOOL * pfAsyncBreakSent);


    // For interop attach, we first do native, and then once Cordbg continues from
    // the loader-bp, we kick off the managed attach. This field remembers that
    // whether we need the managed attach.
    // @dbgtodo  managed pipeline - hoist to shim.
    bool m_fDoDelayedManagedAttached;



    // Table of CordbEval objects that we've sent over to the LS.
    // This is synced via the process lock.
    RsPtrTable<CordbEval> m_EvalTable;

    void PrepopulateThreadsOrThrow();

    // Lookup or create an appdomain.
    CordbAppDomain * LookupOrCreateAppDomain(VMPTR_AppDomain vmAppDomain);

    // Get the shared app domain.
    CordbAppDomain * GetSharedAppDomain();

    // Get metadata dispenser.
    IMetaDataDispenserEx * GetDispenser();

    // Sets a bitfield reflecting the managed debugging state at the time of
    // the jit attach.
    HRESULT GetAttachStateFlags(CLR_DEBUGGING_PROCESS_FLAGS *pFlags);

    HRESULT GetTypeForObject(CORDB_ADDRESS obj, CordbAppDomain* pAppDomainOverride, CordbType **ppType, CordbAppDomain **pAppDomain = NULL);

    WriteableMetadataUpdateMode GetWriteableMetadataUpdateMode() { return m_writableMetadataUpdateMode; }
private:

#ifdef _DEBUG
    // Assert that vmAppDomainDeleted doesn't show up in dac enumerations
    void DbgAssertAppDomainDeleted(VMPTR_AppDomain vmAppDomainDeleted);

    // Callback helper for DbgAssertAppDomainDeleted.
    static void DbgAssertAppDomainDeletedCallback(VMPTR_AppDomain vmAppDomain, void * pUserData);
#endif // _DEBUG

    static void ThreadEnumerationCallback(VMPTR_Thread vmThread, void * pUserData);


    // Callback for AppDomain enumeration
    static void AppDomainEnumerationCallback(VMPTR_AppDomain vmAppDomain, void * pUserData);

    // Helper to create a new CordbAppDomain around the vmptr and cache it
    CordbAppDomain * CacheAppDomain(VMPTR_AppDomain vmAppDomain);

    // Helper to traverse Appdomains in target and build up our cache.
    void PrepopulateAppDomainsOrThrow();


    void ProcessFirstLogMessage (DebuggerIPCEvent *event);
    void ProcessContinuedLogMessage (DebuggerIPCEvent *event);

    void CloseIPCHandles();
    void UpdateThreadsForAdUnload( CordbAppDomain* pAppDomain );

#ifdef FEATURE_INTEROP_DEBUGGING
    // Each win32 debug event needs to be triaged to get a Reaction.
    Reaction TriageBreakpoint(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent);
    Reaction TriageSyncComplete();
    Reaction Triage1stChanceNonSpecial(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent);
    Reaction TriageExcep1stChanceAndInit(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent);
    Reaction TriageExcep2ndChanceAndInit(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent);
    Reaction TriageWin32DebugEvent(CordbUnmanagedThread * pUnmanagedThread, const DEBUG_EVENT * pEvent);
#endif // FEATURE_INTEROP_DEBUGGING

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    RSSmartPtr<Cordb>     m_cordb;

private:
    // OS process handle to live process.
    // @dbgtodo - , Move this into the Shim. This should only be needed in the live-process
    // case. Get rid of this since it breaks the data-target abstraction.
    // For Mac debugging, this handle is of course not the real process handle.  This is just a handle to
    // wait on for process termination.
    HANDLE                m_handle;

    // Process descriptor - holds PID and App group ID for Mac debugging
    ProcessDescriptor m_processDescriptor;

public:
    // Wrapper to get the OS process handle. This is unsafe because it breaks the data-target abstraction.
    // The only things that need this should be calls to DuplicateHandle, and some shimming work.
    HANDLE  UnsafeGetProcessHandle()
    {
        return m_handle;
    }

    // Set when code:CordbProcess::Detach is called.
    // Public APIs can check this and return CORDBG_E_PROCESS_DETACHED.
    // @dbgtodo  managed pipeline - really could merge this with neuter.
    bool                  m_detached;

    // True if we code:CordbProcess::Stop is called before the managed CreateProcess event.
    // In this case, m_initialized is false, and we can't send an AsyncBreak event to the LS.
    // (since the LS isn't going to send a SyncComplete event back since the CLR isn't loaded/ready).
    // @dbgtodo  managed pipeline - move into shim, along with Stop/Continue.
    bool                  m_uninitializedStop;


    // m_exiting is true if we know the LS is starting to exit (if the
    // RS is telling the LS to exit) or if we know the LS has already exited.
    bool                  m_exiting;


    // m_terminated can only be set to true if we know 100% the LS has exited (ie, somebody
    // waited on the LS process handle).
    bool                  m_terminated;

    bool                  m_unrecoverableError;

    bool                  m_specialDeferment;
    bool                  m_helperThreadDead; // flag used for interop

    // This tracks if the loader breakpoint has been received during interop-debugging.
    // The Loader Breakpoint is an breakpoint event raised by the OS once the debugger is attached.
    // It comes in both Attach and Launch scenarios.
    // This is also used in fake-native debugging scenarios.
    bool                  m_loaderBPReceived;

private:

    // MetaData dispenser.
    RSExtSmartPtr<IMetaDataDispenserEx> m_pMetaDispenser;

    //
    // Count of the number of outstanding CordbEvals in the process.
    //
    LONG                  m_cOutstandingEvals;

    // Number of oustanding code:CordbHandleValue objects containing
    // Left-side resources. This can be used to tell if ICorDebug needs to
    // cleanup gc handles.
    LONG                  m_cOutstandingHandles;

    // Pointer to the CordbModule instance that can currently change the Jit flags.
    // There can be at most one of these. It will represent a module that has just been loaded, before the
    // Continue is sent. See code:CordbProcess::RawDispatchEvent and code:CordbProcess::ContinueInternal.
    CordbModule * m_pModuleThatCanChangeJitFlags;

public:
    LONG OutstandingEvalCount()
    {
        return m_cOutstandingEvals;
    }

    void IncrementOutstandingEvalCount()
    {
        InterlockedIncrement(&m_cOutstandingEvals);
    }

    void DecrementOutstandingEvalCount()
    {
        InterlockedDecrement(&m_cOutstandingEvals);
    }

    LONG OutstandingHandles();
    void IncrementOutstandingHandles();
    void DecrementOutstandingHandles();

    //
    // Is it OK to detach at this time
    //
    HRESULT IsReadyForDetach();


private:
    // This is a target pointer that uniquely identifies the runtime in the target.
    // This lets ICD discriminate between multiple CLRs within a single process.
    // On windows, this is the base-address of mscorwks.dll in the target.
    // If this is 0, then we have V2 semantics where there was only 1 CLR in the target.
    // In that case, we can lazily initialize it in code:CordbProcess::CopyManagedEventFromTarget.
    // This is just used for backwards compat.
    CORDB_ADDRESS         m_clrInstanceId;

    // List of things that get neutered on process exit and Continue respectively.
    NeuterList            m_ExitNeuterList;
    NeuterList            m_ContinueNeuterList;

    // List of objects that hold resources into the left-side.
    // This is currently for funceval, which cleans up resources in code:CordbEval::SendCleanup.
    // @dbgtodo - , (func-eval feature crew): we can get rid of this
    // list if we make func-eval not hold resources after it's complete.
    LeftSideResourceCleanupList m_LeftSideResourceCleanupList;

    // m_stopCount, m_synchronized, & m_syncCompleteReceived are key fields describing
    // the processes' sync status.
    DWORD                 m_stopCount;

    // m_synchronized is the Debugger's view of SyncStatus. It will go high & low for each
    // callback. Continue() will set this to false.
    // This flag is true roughly from the time that we've dispatched a managed callback
    // until the time that it's continued.
    bool                  m_synchronized;

    // m_syncCompleteReceived tells us if the runtime is _actually_ sychronized. It goes
    // high once we get a SyncComplete, and it goes low once we actually send the continue.
    // This is always set by the thread that receives the sync-complete. In interop, that's the w32et.
    // Thus this is the most accurate indication of wether the Debuggee is _actually_ synchronized or not.
    bool                  m_syncCompleteReceived;


    // Back pointer to Shim process. This is used for hooks back into the shim.
    // If this is Non-null, then we're emulating the V2 case. If this is NULL, then it's the real V3 pipeline.
    RSExtSmartPtr<ShimProcess> m_pShim;

    CordbSafeHashTable<CordbThread>           m_userThreads;

public:
    ShimProcess* GetShim();

    bool                  m_oddSync;


    void BuildThreadEnum(CordbBase * pOwnerObj, NeuterList * pOwnerList, RSInitHolder<CordbHashTableEnum> * pHolder);

#ifdef FEATURE_INTEROP_DEBUGGING
    // List of unmanaged threads. This is only populated for interop-debugging.
    CordbSafeHashTable<CordbUnmanagedThread>  m_unmanagedThreads;
#endif // FEATURE_INTEROP_DEBUGGING

    CordbSafeHashTable<CordbAppDomain>        m_appDomains;

    CordbAppDomain * m_sharedAppDomain;

    // Since a stepper can begin in one appdomain, and complete in another,
    // we put the hashtable here, rather than on specific appdomains.
    CordbSafeHashTable<CordbStepper>          m_steppers;

    //  Used to figure out if we have to refresh any reference objects
    //  on the left side.  Gets incremented each time a continue is called, or
    //  global debugee state is modified in some other way.
    UINT                  m_continueCounter;

    // Used to track whether the DAC cache has been flushed.
    // We use this information to determine whether CordbStackWalk instances need to
    // be refreshed.
    UINT                  m_flushCounter;

    // The DCB is essentially a buffer area used to temporarily hold information read from the debugger
    // control block residing on the LS helper thread. We make no assumptions about the validity of this
    // information over time, so before using a value from it on the RS, we will always update this buffer
    // with a call to UpdateRightSideDCB. This uses a ReadProcessMemory to get the current information from
    // the LS DCB.
    DebuggerIPCControlBlock * GetDCB() {return ((m_pEventChannel == NULL) ? NULL : m_pEventChannel->GetDCB());}


    DebuggerIPCRuntimeOffsets m_runtimeOffsets;
    HANDLE                    m_leftSideEventAvailable;
    HANDLE                    m_leftSideEventRead;
#if defined(FEATURE_INTEROP_DEBUGGING)
    HANDLE                    m_leftSideUnmanagedWaitEvent;
#endif // FEATURE_INTEROP_DEBUGGING


    // This becomes true when the RS receives its first managed event.
    // This goes false in shutdown cases.
    // If this is true, we can assume:
    // - the CLR is loaded.
    // - the IPC block is opened and initialized.
    // - DAC is initialized (see code:CordbProcess::IsDacInitialized)
    //
    // If this is false, we can assume:
    // - the CLR may not be loaded into the target process.
    // - We can't send IPC events to the LS (because we can't expect a response)
    //
    // Many APIs can check this bit and return CORDBG_E_NOTREADY if it's false.
    bool                  m_initialized;

#ifdef _DEBUG
    void * m_pDBGLastIPCEventType;
#endif

    bool                  m_stopRequested;
    HANDLE                m_stopWaitEvent;
    RSLock                m_processMutex;

#ifdef FEATURE_INTEROP_DEBUGGING
    // The number of threads which are IsFirstChanceHijacked
    DWORD m_cFirstChanceHijackedThreads;

    CordbUnmanagedEvent  *m_unmanagedEventQueue;
    CordbUnmanagedEvent  *m_lastQueuedUnmanagedEvent;
    CordbUnmanagedEvent  *m_lastQueuedOOBEvent;
    CordbUnmanagedEvent  *m_outOfBandEventQueue;

    CordbUnmanagedEvent  *m_lastDispatchedIBEvent;
    bool                  m_dispatchingUnmanagedEvent;
    bool                  m_dispatchingOOBEvent;
    bool                  m_doRealContinueAfterOOBBlock;

    enum
    {
        PS_WIN32_STOPPED           = 0x0001,
        PS_HIJACKS_IN_PLACE        = 0x0002,
        PS_SOME_THREADS_SUSPENDED  = 0x0004,
        PS_WIN32_ATTACHED          = 0x0008,
        PS_WIN32_OUTOFBAND_STOPPED = 0x0010,
    };

    unsigned int          m_state;
#endif // FEATURE_INTEROP_DEBUGGING

    // True if we're interop-debugging, else false.
    bool IsInteropDebugging();

    DWORD                 m_helperThreadId; // helper thread ID calculated from sniffing from UM thread-create events.

    // Is the given thread id a helper thread (real or worker?)
    bool IsHelperThreadWorked(DWORD tid);

    //
    // We cache the LS patch table on the RS.
    //

    // The array of entries. (The patchtable is a hash implemented as a single-array)
    // This array includes empty entries.
    // There is an auxillary bucket structure used to map hash codes to array indices.
    // We traverse the array, and we recognize an empty slot
    // if DebuggerControllerPatch::opcode == 0.
    // If we haven't gotten the table, then m_pPatchTable is NULL
    BYTE*                 m_pPatchTable;

    // The number of entries (both used & unused) in m_pPatchTable.
    UINT                  m_cPatch;

    // so we know where to write the changes patchtable back to
    // This has m_cPatch elements.
    BYTE                 *m_rgData;

    // Cached value of iNext entries such that:
    //      m_rgNextPatch[i] = ((DebuggerControllerPatch*)m_pPatchTable)[i]->iNext;
    //      where 0 <= i < m_cPatch
    // This provides a linked list (via indices) to traverse the used entries of m_pPatchTable.
    // This has m_cPatch elements.
    ULONG               *m_rgNextPatch;

    // This has m_cPatch elements.
    PRD_TYPE             *m_rgUncommitedOpcode;

    // CORDB_ADDRESS's are UINT_PTR's (64 bit under HOST_64BIT, 32 bit otherwise)
#if defined(TARGET_64BIT)
#define MAX_ADDRESS     (_UI64_MAX)
#else
#define MAX_ADDRESS     (_UI32_MAX)
#endif
#define MIN_ADDRESS     (0x0)
    CORDB_ADDRESS       m_minPatchAddr; //smallest patch in table
    CORDB_ADDRESS       m_maxPatchAddr;

    // <TODO>@todo port : if slots of CHashTable change, so should these</TODO>
#define DPT_TERMINATING_INDEX (UINT32_MAX)
    // Index into m_pPatchTable of the first patch (first used entry).
    ULONG                  m_iFirstPatch;

    // Initializes the DAC
    void InitDac();

    // copy new data from LS DCB to RS buffer
    void UpdateRightSideDCB();

    // copy new data from RS DCB buffer to LS DCB
    void UpdateLeftSideDCBField(void * rsFieldAddr, SIZE_T size);

    // allocate and initialize the RS DCB buffer
    void GetEventBlock(BOOL * pfBlockExists);

    IEventChannel * GetEventChannel();

    bool SupportsVersion(CorDebugInterfaceVersion featureVersion);

    void StartEventDispatch(DebuggerIPCEventType event);
    void FinishEventDispatch();
    bool AreDispatchingEvent();

    HANDLE GetHelperThreadHandle() { return m_hHelperThread; }

    CordbAppDomain* GetDefaultAppDomain() { return m_pDefaultAppDomain; }

#ifdef FEATURE_INTEROP_DEBUGGING
    // Lookup if there's a native BP at the given address. Return NULL not found.
    NativePatch * GetNativePatch(const void * pAddress);
#endif // FEATURE_INTEROP_DEBUGGING

    bool  IsBreakOpcodeAtAddress(const void * address);

private:
    //
    // handle to helper thread. Used for managed debugging.
    // Initialized only after we get the tid from the DCB.
    HANDLE m_hHelperThread;

    DebuggerIPCEventType  m_dispatchedEvent;   // what event are we currently dispatching?

    RSLock            m_StopGoLock;

    // Each process has exactly one Default AppDomain
    // @dbgtodo  appdomain : We should try and simplify things by removing this.
    // At the moment it's necessary for CordbProcess::UpdateThreadsForAdUnload.
    CordbAppDomain*     m_pDefaultAppDomain;    // owned by m_appDomains

#ifdef FEATURE_INTEROP_DEBUGGING
    // Helpers
    CordbUnmanagedThread * GetUnmanagedThreadFromEvent(const DEBUG_EVENT * pEvent);
#endif // FEATURE_INTEROP_DEBUGGING

    // Ensure we have a CLR Instance ID to debug
    HRESULT EnsureClrInstanceIdSet();

#ifdef FEATURE_INTEROP_DEBUGGING
    // // The full debug event is too large, so we just remember the important stuff.
    struct MiniDebugEvent
    {
        BYTE code; // event code from the debug event
        CordbUnmanagedThread * pUThread; // unmanaged thread this was on.
        // @todo - we should have some misc data.
        union
        {
            struct {
                void * pAddress; // address of an exception
                DWORD dwCode;
            } ExceptionData;
            struct {
                void * pBaseAddress; // for module load & unload
            } ModuleData;
        } u;
    };

    // Group fields that are just used for debug support here.
    // Some are included even in retail builds to help debug retail failures.
    struct DebugSupport
    {
        // For debugging, we keep a rolling queue of the last N Win32 debug events.
        MiniDebugEvent        m_DebugEventQueue[DEBUG_EVENTQUEUE_SIZE];
        int                   m_DebugEventQueueIdx;
        int                   m_TotalNativeEvents;

        // Breakdown of different types of native events
        int                   m_TotalIB;
        int                   m_TotalOOB;
        int                   m_TotalCLR;
    } m_DbgSupport;

    CUnorderedArray<NativePatch, 10> m_NativePatchList;
#endif // FEATURE_INTEROP_DEBUGGING

    //
    // DAC
    //

    // Try to initalize DAC, may fail
    BOOL TryInitializeDac();

    // Expect DAC initialize to succeed.
    void InitializeDac();


    void CreateDacDbiInterface();

    // Free DAC.
    void FreeDac();


    HModuleHolder             m_hDacModule;
    RSExtSmartPtr<ICorDebugDataTarget> m_pDACDataTarget;

    // The mutable version of the data target, or null if read-only
    RSExtSmartPtr<ICorDebugMutableDataTarget> m_pMutableDataTarget;

    RSExtSmartPtr<ICorDebugMetaDataLocator>   m_pMetaDataLocator;

    IDacDbiInterface *  m_pDacPrimitives;

    IEventChannel *     m_pEventChannel;

    // If true, then we'll ASSERT if we detect the target is corrupt or inconsistent
    // This switch is for diagnostics purposes only and should always be false in retail builds.
    bool                m_fAssertOnTargetInconsistency;

    // When a successful attempt to read runtime offsets from LS occurs, this flag is set.
    bool m_runtimeOffsetsInitialized;

    // controls how metadata updated in the target is handled
    WriteableMetadataUpdateMode m_writableMetadataUpdateMode;

    COM_METHOD GetObjectInternal(CORDB_ADDRESS addr, CordbAppDomain* pAppDomainOverride, ICorDebugObjectValue **pObject);
};

// Some IMDArocess APIs are supported as interop-only.
#define FAIL_IF_MANAGED_ONLY(pProcess) \
{ CordbProcess * __Proc = pProcess; if (!__Proc->IsInteropDebugging()) return CORDBG_E_MUST_BE_INTEROP_DEBUGGING; }


/* ------------------------------------------------------------------------- *
 * Module class
 * ------------------------------------------------------------------------- */

class CordbModule : public CordbBase,
                    public ICorDebugModule,
                    public ICorDebugModule2,
                    public ICorDebugModule3,
                    public ICorDebugModule4
{
public:
    CordbModule(CordbProcess *      process,
                VMPTR_Module        vmModule,
                VMPTR_DomainFile    vmDomainFile);

    virtual ~CordbModule();
    virtual void Neuter();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbModule"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugModule
    //-----------------------------------------------------------

    COM_METHOD GetProcess(ICorDebugProcess **ppProcess);
    COM_METHOD GetBaseAddress(CORDB_ADDRESS *pAddress);
    COM_METHOD GetAssembly(ICorDebugAssembly **ppAssembly);
    COM_METHOD GetName(ULONG32 cchName, ULONG32 *pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);
    COM_METHOD EnableJITDebugging(BOOL bTrackJITInfo, BOOL bAllowJitOpts);
    COM_METHOD EnableClassLoadCallbacks(BOOL bClassLoadCallbacks);

    // Gets the latest version of a function given the methodDef token
    COM_METHOD GetFunctionFromToken(mdMethodDef methodDef,
                                    ICorDebugFunction **ppFunction);
    COM_METHOD GetFunctionFromRVA(CORDB_ADDRESS rva, ICorDebugFunction **ppFunction);
    COM_METHOD GetClassFromToken(mdTypeDef typeDef,
                                 ICorDebugClass **ppClass);
    COM_METHOD CreateBreakpoint(ICorDebugModuleBreakpoint **ppBreakpoint);

    // Not implemented - legacy
    COM_METHOD GetEditAndContinueSnapshot(
        ICorDebugEditAndContinueSnapshot **ppEditAndContinueSnapshot);

    COM_METHOD GetMetaDataInterface(REFIID riid, IUnknown **ppObj);
    COM_METHOD GetToken(mdModule *pToken);
    COM_METHOD IsDynamic(BOOL *pDynamic);
    COM_METHOD GetGlobalVariableValue(mdFieldDef fieldDef,
                                   ICorDebugValue **ppValue);
    COM_METHOD GetSize(ULONG32 *pcBytes);
    COM_METHOD IsInMemory(BOOL *pInMemory);

    //-----------------------------------------------------------
    // ICorDebugModule2
    //-----------------------------------------------------------
    COM_METHOD SetJMCStatus(
        BOOL fIsUserCode,
        ULONG32 cOthers,
        mdToken others[]);

    // Applies an EnC edit to the module
    COM_METHOD ApplyChanges(
        ULONG  cbMetaData,
        BYTE   pbMetaData[],
        ULONG  cbIL,
        BYTE   pbIL[]);

    // Resolve an assembly given an AssemblyRef token. Note that
    // this will not trigger the loading of assembly. If assembly is not yet loaded,
    // this will return an CORDBG_E_CANNOT_RESOLVE_ASSEMBLY error
    COM_METHOD ResolveAssembly(mdToken tkAssemblyRef,
                                   ICorDebugAssembly **ppAssembly);

    // Sets EnC and optimization flags
    COM_METHOD SetJITCompilerFlags(DWORD dwFlags);

    // Gets EnC and optimization flags
    COM_METHOD GetJITCompilerFlags(DWORD *pdwFlags);

    //-----------------------------------------------------------
    // ICorDebugModule3
    //-----------------------------------------------------------
    COM_METHOD CreateReaderForInMemorySymbols(REFIID riid,
                                              void** ppObj);

    //-----------------------------------------------------------
    // ICorDebugModule4
    //-----------------------------------------------------------
    COM_METHOD IsMappedLayout(BOOL *isMapped);

    //-----------------------------------------------------------
    // Internal members
    //-----------------------------------------------------------

#ifdef _DEBUG
    // Debug helper to ensure that module is no longer discoverable
    void DbgAssertModuleDeleted();
#endif // _DEBUG

    // Internal help to get the "name" (filename or pretty name) of the module.
    HRESULT GetNameWorker(ULONG32 cchName, ULONG32 *pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    // Marks that the module's metadata has become invalid and needs to be refetched.
    void RefreshMetaData();

    // Cache the current continue counter as the one that the LoadEvent is
    // dispatched in.
    void SetLoadEventContinueMarker();

    // Return CORDBG_E_MUST_BE_IN_LOAD_MODULE if this module is not in its load callback.
    HRESULT EnsureModuleIsInLoadCallback();

    BOOL IsDynamic();

    // Gets the latest version of the function for the methodDef, if any
    CordbFunction * LookupFunctionLatestVersion(mdMethodDef methodToken);

    // Gets the latest version of the function. Creates a new instance if none exists yet.
    CordbFunction* LookupOrCreateFunctionLatestVersion(mdMethodDef funcMetaDataToken);

    // Finds or creates a function for the first time (not for use on EnC if function doesn't exist yet)
    CordbFunction * LookupOrCreateFunction(mdMethodDef token, SIZE_T enCVersion);

    // Creates an CordbFunction instances for the first time (not for use on EnC)
    CordbFunction * CreateFunction(mdMethodDef token, SIZE_T enCVersion);

    // Creates a CordbFunction object to represent the specified EnC version
    HRESULT UpdateFunction(mdMethodDef token,
                           SIZE_T newEnCVersion,
                           CordbFunction** ppFunction);

    CordbClass* LookupClass(mdTypeDef classToken);
    HRESULT LookupOrCreateClass(mdTypeDef classToken, CordbClass** ppClass);
    HRESULT CreateClass(mdTypeDef classToken, CordbClass** ppClass);
    HRESULT LookupClassByToken(mdTypeDef token, CordbClass **ppClass);
    HRESULT ResolveTypeRef(mdTypeRef token, CordbClass **ppClass);
    HRESULT ResolveTypeRefOrDef(mdToken token, CordbClass **ppClass);

    // Sends the event to the left side to apply the changes to the debugee
    HRESULT ApplyChangesInternal(
        ULONG cbMetaData,
        BYTE pbMetaData[],
        ULONG cbIL,
        BYTE pbIL[]);

    // Pulls new metadata if needed in order to ensure the availability of
    // the given token
    void UpdateMetaDataCacheIfNeeded(mdToken token);

    HRESULT InitPublicMetaDataFromFile(const WCHAR * pszFullPathName, DWORD dwOpenFlags, bool validateFileInfo);

    // Creates a CordbNativeCode (if it's not already created) and adds it to the
    // hash table of CordbNativeCodes belonging to the module.
    CordbNativeCode * LookupOrCreateNativeCode(mdMethodDef methodToken,
                                               VMPTR_MethodDesc methodDesc,
                                               CORDB_ADDRESS startAddress);

private:
    // Set the metadata (both public and internal) for the module.
    void InitMetaData(TargetBuffer buffer, BOOL useFileMappingOptimization);

    // Checks if the given token is in the cached metadata
    BOOL CheckIfTokenInMetaData(mdToken token);

    // Update the public metadata given a buffer in the target.
    void UpdatePublicMetaDataFromRemote(TargetBuffer bufferRemoteMetaData);

    // Initialize just the public metadata by reading from an on-disk module
    HRESULT InitPublicMetaDataFromFile();
    // Initialize just the public metadata by reading new metadata from the buffer
    void InitPublicMetaData(TargetBuffer buffer);

    // Rebuild the internal metadata given the public one.
    void UpdateInternalMetaData();

    // Determines whether the on-disk metadata for this module is usable as the
    // current metadata
    BOOL IsFileMetaDataValid();

    // Helper to copy metadata buffer from the Target to the host.
    void CopyRemoteMetaData(TargetBuffer buffer, CoTaskMemHolder<VOID> * pLocalBuffer);


    CordbAssembly * ResolveAssemblyInternal(mdToken tkAssemblyRef);

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------

public:
    CordbAppDomain *GetAppDomain()
    {
        return m_pAppDomain;
    }

    CordbAssembly * GetCordbAssembly ();

    // Get the module filename, or NULL if none.  Throws on error.
    const WCHAR * GetModulePath();

    const WCHAR * GetNGenImagePath();

    const VMPTR_DomainFile GetRuntimeDomainFile ()
    {
        return m_vmDomainFile;
    }

    const VMPTR_Module GetRuntimeModule()
    {
        return m_vmModule;
    }

    // Get symbol stream for in-memory modules.
    IDacDbiInterface::SymbolFormat GetInMemorySymbolStream(IStream ** ppStream);

    // accessor for PE file
    VMPTR_PEFile GetPEFile();


    IMetaDataImport * GetMetaDataImporter();

    // accessor for Internal MetaData importer.
    IMDInternalImport * GetInternalMD();

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    CordbAssembly*   m_pAssembly;
    CordbAppDomain*  m_pAppDomain;
    CordbSafeHashTable<CordbClass>    m_classes;

    // A collection, indexed by methodDef, of the latest version of functions in this module
    // The collection is filled lazily by LookupOrCreateFunction
    CordbSafeHashTable<CordbFunction> m_functions;

    // The real handle into the VM for a module. This is appdomain aware.
    // This is the primary VM counterpart for the CordbModule.
    VMPTR_DomainFile m_vmDomainFile;

    VMPTR_Module m_vmModule;

    DWORD            m_EnCCount;

private:

    // Base Address and size of this module in debuggee's process. Maybe null if unknown.
    TargetBuffer m_PEBuffer;

    BOOL             m_fDynamic; // Dynamic modules can grow (like Reflection Emit)
    BOOL             m_fInMemory; // In memory modules don't have file-backing.

    // Indicates that the module must serialize its metadata in process as part of metadata
    // refresh. This is required for modules updated on the fly by the profiler
    BOOL             m_fForceMetaDataSerialize;

    // Full path to module's image, if any.  Empty if none, NULL if not yet set.
    StringCopyHolder m_strModulePath;

    // Full path to the ngen file. Empty if not ngenned, NULL if not yet set.
    // This isn't exposed publicly, but we may use it internally for loading metadata.
    StringCopyHolder m_strNGenImagePath;

    // "Global" class for this module. Global functions + vars exist in this class.
    RSSmartPtr<CordbClass> m_pClass;

    // Handle to PEFile, useful for metadata lookups.
    // this should always be non-null.
    VMPTR_PEFile    m_vmPEFile;


    // Public metadata importer. This is lazily initialized and accessed from code:GetMetaDataImporter
    // This is handed out to debugger clients via code:CordbModule::GetMetaDataInterface
    // This is also tightly coupled to the internal metadata importer, m_pInternalMetaDataImport.
    RSExtSmartPtr<IMetaDataImport> m_pIMImport;

    // Internal metadata object. This is closely tied to the public metadata object (m_pIMImport).
    // They share the same backing storage, but expose different interfaces to that storage.
    // Debugger authors and tools use the public interfaces.
    // DAC-ized operations in the VM require an IMDInternalImport.
    // The public and internal must be updated together.
    // This ultimately gets handed back to DAC via code:CordbProcess::LookupMetaData
    RSExtSmartPtr<IMDInternalImport> m_pInternalMetaDataImport;

    // Continue counter of when the module was loaded.
    // See code:CordbModule::SetLoadEventContinueMarker for details
    UINT m_nLoadEventContinueCounter;

    // This is a table of all NativeCode objects in the module indexed
    // by start address
    // The collection is filled lazily by LookupOrCreateNativeCode
    CordbSafeHashTable<CordbNativeCode> m_nativeCodeTable;
};


//-----------------------------------------------------------------------------
// Cordb MDA notification
//-----------------------------------------------------------------------------
class CordbMDA : public CordbBase, public ICorDebugMDA
{
public:
    CordbMDA(CordbProcess * pProc, DebuggerMDANotification * pData);
    ~CordbMDA();

    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbMDA"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRefEnforceExternal());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseReleaseEnforceExternal());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugMDA
    //-----------------------------------------------------------

    // Get the string for the type of the MDA. Never empty.
    // This is a convenient performant alternative to getting the XML stream and extracting
    // the type from that based off the schema.
    COM_METHOD GetName(ULONG32 cchName, ULONG32 * pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    // Get a string description of the MDA. This may be empty (0-length).
    COM_METHOD GetDescription(ULONG32 cchName, ULONG32 * pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    // Get the full associated XML for the MDA. This may be empty.
    // This could be a potentially expensive operation if the xml stream is large.
    // See the MDA documentation for the schema for this XML stream.
    COM_METHOD GetXML(ULONG32 cchName, ULONG32 * pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    COM_METHOD GetFlags(CorDebugMDAFlags * pFlags);

    // Thread that the MDA is fired on. We use the os tid instead of an ICDThread in case an MDA is fired on a
    // native thread (or a managed thread that hasn't yet entered managed code and so we don't have a ICDThread
    // object for it yet)
    COM_METHOD GetOSThreadId(DWORD * pOsTid);

private:
    NewArrayHolder<WCHAR> m_szName;
    NewArrayHolder<WCHAR> m_szDescription;
    NewArrayHolder<WCHAR> m_szXml;

    DWORD m_dwOSTID;
    CorDebugMDAFlags m_flags;
};



struct CordbHangingField
{
    FREEHASHENTRY   entry;
    FieldData data;
};

// A hashtable for storing EnC hanging field information
// FieldData.m_fldMetadataToken is the key
class CordbHangingFieldTable : public CHashTableAndData<CNewDataNoThrow>
{
  private:

    BOOL Cmp(SIZE_T k1, const HASHENTRY *pc2)
    {
        LIMITED_METHOD_CONTRACT;
        return (ULONG)(UINT_PTR)(k1) !=
               (reinterpret_cast<const CordbHangingField *>(pc2))->data.m_fldMetadataToken;
    }

    ULONG HASH(mdFieldDef fldToken)
    {
        LIMITED_METHOD_CONTRACT;
        return fldToken;
    }

    SIZE_T KEY(mdFieldDef fldToken)
    {
        return (SIZE_T)fldToken;
    }

  public:

#ifndef DACCESS_COMPILE

    CordbHangingFieldTable() : CHashTableAndData<CNewDataNoThrow>(11)
    {
        NewInit(11, sizeof(CordbHangingField), 11);
    }

    FieldData * AddFieldInfo(FieldData * pInfo)
    {
        _ASSERTE(pInfo != NULL);

        CordbHangingField *pEntry = (CordbHangingField *)Add(HASH(pInfo->m_fldMetadataToken));
        pEntry->data = *pInfo; // copy everything over

        // Return a pointer to the data
        return &(pEntry->data);
    }

    void RemoveFieldInfo(mdFieldDef fldToken)
    {
        CordbHangingField *entry = (CordbHangingField*)Find(HASH(fldToken), KEY(fldToken));
        _ASSERTE(entry != NULL);
        Delete(HASH(fldToken), (HASHENTRY*)entry);
   }

#endif // #ifndef DACCESS_COMPILE

    FieldData * GetFieldInfo(mdFieldDef fldToken)
    {
        CordbHangingField * entry = (CordbHangingField *)Find(HASH(fldToken), KEY(fldToken));
        return (entry!=NULL?&(entry->data):NULL);
    }
};


/* ------------------------------------------------------------------------- *
 * Instantiation.
 *
 * This struct stores a set of type parameters.  It is used in
 * the heap-allocated data structures CordbType and CordbNativeCode.
 *
 *   CordbType::m_inst.    Stores the class type parameters if any,
 *                         or the solitary array type parameter, or the solitary parameter
 *                         to a byref type.
 *
 *   CordbJITILFrame::m_genericArgs.  Stores exact generic parameters for the generic method frame if available
 *                                 Need not be identicial if code is shared between generic instantiations.
 *                                 May be inexact if real instantiation has been optimized away off
 *                                 the frame (nb this gets reported by the left side)
 *
 * This is conceptually an array of Type-parameters, with the split (m_cClassTyPars) between
 * where the Type's type-parameters end and the Method's type-parameters begin.
 * ------------------------------------------------------------------------- */
class Instantiation
{
public:
    // Empty ctor
    Instantiation()
        : m_cInst(0), m_ppInst(NULL), m_cClassTyPars (0)
    { }

    // Instantiation for Type. 0 Method type-parameters.
    Instantiation(unsigned int _cClassInst, CordbType **_ppClassInst)
        : m_cInst(_cClassInst), m_ppInst(_ppClassInst), m_cClassTyPars(_cClassInst)
    {LIMITED_METHOD_CONTRACT;  }

    // Instantiation for Type + Function.
    Instantiation(unsigned int _cInst, CordbType **_ppInst, unsigned int numClassTyPars)
        : m_cInst(_cInst), m_ppInst(_ppInst),
        m_cClassTyPars (numClassTyPars)
    { }

    // Copy constructor.
    Instantiation(const Instantiation &inst)
        : m_cInst(inst.m_cInst), m_ppInst(inst.m_ppInst), m_cClassTyPars (inst.m_cClassTyPars)
    { }

    // Number of elements in array pointed to by m_ppInst
    unsigned int m_cInst;

    // Pointer to array of CordbType objects. Length of array is m_cInst.
    // Array is Class Type parameters followed by Function's Type parameters.
    // Eg, Instantiation for Class<Foo, Goo>::Func<Bar> would be {Foo, Goo, Bar}.
    // m_cInst = 3, m_cClassTyPars = 2.
    // In contrast, Instantiation for Class::Func<Foo, Goo, Bar> would have same
    // array, but m_cClassTyPars = 0.
    CordbType **m_ppInst;

    // Track the split between Type vs. Method type-params.
    unsigned int m_cClassTyPars;
};

//------------------------------------------------------------------------
// CordbType: replaces the use of signatures.
//
// Left Side & Right Side
// ---------------------------
// CordbTypes may come from either the Right Side (via being built up from
//   ICorDebug), or from the Left-Side (being handed back from LS operations
//   like getting the type from an Object the LS handed back).
// The RightSide CordbType corresponds to a Left-Side TypeHandle.
// CordbTypes are communicated across the LS/RS boundary by marshalling
// to BasicTypeData + ExpandedTypeData IPC events.
//
//
// Invariants on CordbType
// ---------------------------
//
//   The m_elementType is NEVER ELEMENT_TYPE_VAR or ELEMENT_TYPE_MVAR or ELEMENT_TYPE_GENERICINST
//   CordbTypes are always _ground_ types (fully instantiated generics or non-generic types). If
//   they represent an instantiated type like List<int> then m_inst will be non-empty.
//
//
//   !!!! The m_elementType is NEVER ELEMENT_TYPE_VALUETYPE !!!!
//   !!!! To find out if it is a value type call CordbType::IsValueType() !!!!
//
// Where CordbTypes are stored
// ---------------------------
//
// Because we could have a significant number of different instantiations for a given templated type,
// we need an efficient way to store and retrieve the CordbType instances for these instantiations.
// For this reason, we use a tree-like scheme to hash-cons types. To implement this we use the following
// scheme:
//   - CordbTypes are created for "partially instantiated" types,
//     e.g. CordbTypes exist for "Dict" and "Dict<int>" even if the real
//     type being manipulated by the user is "Dict<int,string>"
//   - Subordinate types (E.g. Dict<int,string> is subordinate to Dict<int>,
//     which is itself subordinate to the type for Dict) get stored
//     in the m_spinetypes hash table of the parent type.
//   - In m_spinetypes the pointers of the CordbTypes themselves
//     are used for the unique ids for entries in the table.
// Note that CordbType instances that are created for "partially instantiated" types
// are never used for any purpose other than efficient hashing. Specifically, the debugger will
// never have reason to expose a partially instantiated type outside of the hashing algorithm.
//
// CordbTypes have object identity: if 2 CordbTypes represent the same type (in the same AppDomain),
// then they will be the same CordbType instance.
//
// Thus the representation for  "Dict<class String,class Foo, class Foo* >" goes as follows:
//    1. Assume the type Foo is represented by CordbClass *5678x
//    1b. Assume the hashtable m_sharedtypes in the AppDomain maps E_T_STRING to the CordbType *0ABCx
//       Assume m_type in class Foo (i.e. CordbClass *5678x) is the CordbType *0DEFx
//       Assume m_type in class Foo maps E_T_PTR to the CordbType *0647x
//    2. The hash table m_spinetypes in "Dict" maps "0ABCx" to a new CordbType
//       representing Dict<String> (a single type application)
//    3. The hash table m_spinetypes in this new CordbType maps "0DEFx" to a
//        new CordbType representing Dict<class String,class Foo>
//    3. The hash table m_spinetypes in this new CordbType maps "0647" to a
//        new CordbType representing Dict<class String,class Foo, class Foo*>
//
// This lets us reuse the existing hash table scheme to build
// up instantiated types of arbitrary size.
//
// Array types are similar, excpet that they start with a head type
// for the "type constructor", e.g. "_ []" is a type constructor with rank 1
// and m_elementType = ELEMENT_TYPE_SZARRAY.  These head constructors are
// stored in the m_sharedtypes table in the appdomain.  The actual instantiations
// of the array types are then subordinate types to the array constructor type.
//
// Other types are simpler, and have unique objects stored in the m_sharedtypes
// table in the appdomain.  This table is indexed by CORDBTYPE_ID in RsType.cpp
//
//
// Memory Management of CordbTypes
// ---------------------------
// All CordbTypes are ultimately stored off the CordbAppDomain object.
// The most common place is in the AppDomain's neuter-list.
//
// See definition of ICorDebugType for further invariants on types.
//

class CordbType : public CordbBase, public ICorDebugType, public ICorDebugType2
{
public:
    CordbType(CordbAppDomain *appdomain, CorElementType ty, unsigned int rank);
    CordbType(CordbAppDomain *appdomain, CorElementType ty, CordbClass *c);
    CordbType(CordbType *tycon, CordbType *tyarg);
    virtual ~CordbType();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbType"; }
#endif

    // If you want to force the init to happen even if we think the class
    // is up to date, set fForceInit to TRUE
    HRESULT Init(BOOL fForceInit);

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugType
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *ty);
    COM_METHOD GetClass(ICorDebugClass **ppClass);
    COM_METHOD EnumerateTypeParameters(ICorDebugTypeEnum **ppTyParEnum);
    COM_METHOD GetFirstTypeParameter(ICorDebugType **ppType);
    COM_METHOD GetBase(ICorDebugType **ppType);
    COM_METHOD GetStaticFieldValue(mdFieldDef fieldDef,
                                   ICorDebugFrame * pFrame,
                                   ICorDebugValue ** ppValue);
    COM_METHOD GetRank(ULONG32 *pnRank);

    //-----------------------------------------------------------
    // ICorDebugType2
    //-----------------------------------------------------------
    COM_METHOD GetTypeID(COR_TYPEID *pId);

    //-----------------------------------------------------------
    // Non-COM members
    //-----------------------------------------------------------

    //-----------------------------------------------------------
    // Basic constructor operations for the algebra of types.
    // These all create unique objects within an AppDomain.
    //-----------------------------------------------------------

    // This one is used to create simple types, e.g. int32, int64, typedbyref etc.
    static HRESULT MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          CordbType ** ppResultType);

    // This one is used to create array, pointer and byref types
    static HRESULT MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          ULONG rank,
                          CordbType * pType,
                          CordbType ** ppResultType);

    // This one is used to create function pointer types.  et must be ELEMENT_TYPE_FNPTR
    static HRESULT MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          const Instantiation * pInst,
                          CordbType ** ppResultType);

    // This one is used to class and value class types, e.g. "class MyClass" or "class ArrayList<int>"
    static HRESULT MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          CordbClass * pClass,
                          const Instantiation * pInst,
                          CordbType ** ppResultType);

    // Some derived constructors...  Use this one if the type is definitely not
    // a parameterized type, e.g. to implement functions on the API where types cannot
    // be parameterized.
    static HRESULT MkUnparameterizedType(CordbAppDomain *appdomain, CorElementType et, CordbClass *cl, CordbType **ppType);

    //-----------------------------------------------------------
    // Basic destructor operations over the algebra
    //-----------------------------------------------------------
    void DestUnaryType(CordbType **pRes) ;
    void DestConstructedType(CordbClass **pClass, Instantiation *pInst);
    void DestNaryType(Instantiation *pInst);

    CorElementType GetElementType() { return m_elementType; }
    VMPTR_DomainFile GetDomainFile();
    VMPTR_Module GetModule();

    // If this is a ptr type, get the CordbType that it points to.
    // Eg, for CordbType("Int*"), returns CordbType("Int").
    // If not a ptr type, returns null.
    // Since it's all internal, no reference counting.
    // This is effectively a specialized version of DestUnaryType.
    CordbType * GetPointerElementType();


    // Create a type from metadata
    static HRESULT SigToType(CordbModule * pModule, SigParser * pSigParser, const Instantiation * pInst, CordbType ** ppResultType);

    // Create a type from from the data received from the left-side
    static HRESULT TypeDataToType(CordbAppDomain *appdomain, DebuggerIPCE_ExpandedTypeData *data, CordbType **pRes);
    static HRESULT TypeDataToType(CordbAppDomain *appdomain, DebuggerIPCE_BasicTypeData *data, CordbType **pRes);
    static HRESULT InstantiateFromTypeHandle(CordbAppDomain * appdomain,
                                             VMPTR_TypeHandle vmTypeHandle,
                                             CorElementType et,
                                             CordbClass * tycon,
                                             CordbType ** pRes);

    // Prepare data to send back to left-side during Init() and FuncEval.  Fail if the the exact
    // type data is requested but was not fetched correctly during Init()
    HRESULT TypeToBasicTypeData(DebuggerIPCE_BasicTypeData *data);
    void TypeToExpandedTypeData(DebuggerIPCE_ExpandedTypeData *data);
    void TypeToTypeArgData(DebuggerIPCE_TypeArgData *data);

    void CountTypeDataNodes(unsigned int *count);
    static void CountTypeDataNodesForInstantiation(unsigned int genericArgsCount, ICorDebugType *genericArgs[], unsigned int *count);
    static void GatherTypeData(CordbType *type, DebuggerIPCE_TypeArgData **curr_tyargData);
    static void GatherTypeDataForInstantiation(unsigned int genericArgsCount, ICorDebugType *genericArgs[], DebuggerIPCE_TypeArgData **curr_tyargData);

    HRESULT GetParentType(CordbClass * baseClass, CordbType ** ppRes);

    // These are available after Init() has been called....
    HRESULT GetUnboxedObjectSize(ULONG32 *res);
    HRESULT GetFieldInfo(mdFieldDef fldToken, FieldData ** ppFieldData);

    CordbAppDomain *GetAppDomain() { return m_appdomain; }

    bool IsValueType();

    // Is this type a GC-root.
    bool IsGCRoot();

#ifdef FEATURE_64BIT_ALIGNMENT
    // checks if the type requires 8-byte alignment.
    // this is not exposed via ICorDebug at present.
    HRESULT RequiresAlign8(BOOL* isRequired);
#endif

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    // Internal representation of the element type. This may not map exactly to the public element type.
    // Specifically, m_elementType is NEVER:
    //  ELEMENT_TYPE_VAR, ELEMENT_TYPE_MVAR, ELEMENT_TYPE_GENERICINST,
    //  or ELEMENT_TYPE_VALUETYPE.
    // To find out if this CordbType corresponds to a value type (instead of Reference type) call CordbType::IsValueType()
    CorElementType                 m_elementType;

    // The appdomain that this type lives in. Types (and their type-parameters) are all contained in a single appdomain.
    // (alhtough the types may be from different modules).
    // This is valid for all CordbType objects, regardless of m_elementType;
    CordbAppDomain *               m_appdomain;

    // The matching class for this type.
    // Initially only set for E_T_CLASS, lazily computed for E_T_STRING and E_T_OBJECT if needed
    CordbClass *                   m_pClass;

    ULONG m_rank; // Only set for E_T_ARRAY etc.

    // Array of Type Parameters for this Type.
    Instantiation                  m_inst;

    // A unique mapping from CordbType objects that are type parameters to CordbType objects.  Each mapping
    // represents the use of the containing type as type constructor.  e.g. If the containing type
    // is CordbType(CordbClass "List") then the table here will map parameters such as (CordbType(CordbClass "String")) to
    // the constructed type CordbType(CordbClass "List", <CordbType(CordbClass "String")>)
    // @dbgtodo  synchronization - this is currently protected by the Stop-Go lock. Transition to process-lock.
    CordbSafeHashTable<CordbType>  m_spinetypes;

    // Valid after Init(), only for E_T_ARRAY etc.and E_T_CLASS when m_pClass->m_classInfo.m_genericArgsCount > 0.
    // m_typeHandleExact is the precise Runtime type handle for this type.
    VMPTR_TypeHandle               m_typeHandleExact;

    // Valid after Init(), only for E_T_CLASS, and when m_pClass->m_classInfo.m_genericArgsCount > 0.
    // May not be set correctly if m_fieldInfoNeedsInit.
    SIZE_T                         m_objectSize;

    // DON'T KEEP POINTERS TO ELEMENTS OF m_pFields AROUND!!
    // This may be deleted if the class gets EnC'd.
    //
    // Valid after Init(), only for E_T_CLASS, and when m_pClass->m_classInfo.m_genericArgsCount > 0
    // All fields will be valid if we have m_typeHandleExact.
    //
    // Only some fields will be valid if we have called Init() but still have m_fieldInfoNeedsInit.
    DacDbiArrayList<FieldData>     m_fieldList;

    HRESULT ReturnedByValue();

private:
    static HRESULT MkTyAppType(CordbAppDomain * pAddDomain,
                               CordbType * pType,
                               const Instantiation * pInst,
                               CordbType ** pResultType);

    BOOL                    m_fieldInfoNeedsInit;

private:
    HRESULT InitInstantiationTypeHandle(BOOL fForceInit);
    HRESULT InitInstantiationFieldInfo(BOOL fForceInit);
    HRESULT InitStringOrObjectClass(BOOL fForceInit);
};

/* ------------------------------------------------------------------------- *
 * Class class
 * ------------------------------------------------------------------------- */

class CordbClass : public CordbBase, public ICorDebugClass, public ICorDebugClass2
{
public:
    CordbClass(CordbModule* m, mdTypeDef token);
    virtual ~CordbClass();
    virtual void Neuter();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbClass"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugClass
    //-----------------------------------------------------------

    COM_METHOD GetStaticFieldValue(mdFieldDef fieldDef,
                                   ICorDebugFrame *pFrame,
                                   ICorDebugValue **ppValue);
    COM_METHOD GetModule(ICorDebugModule **pModule);
    COM_METHOD GetToken(mdTypeDef *pTypeDef);
    //-----------------------------------------------------------
    // ICorDebugClass2
    //-----------------------------------------------------------
    COM_METHOD GetParameterizedType(CorElementType elementType,
                                    ULONG32 cTypeArgs,
                                    ICorDebugType * rgpTypeArgs[],
                                    ICorDebugType ** ppType);

    COM_METHOD SetJMCStatus(BOOL fIsUserCode);

    //-----------------------------------------------------------
    // Convenience routines and Accessors
    //-----------------------------------------------------------

    // Helper to get containing module
    CordbModule * GetModule()
    {
        return m_pModule;
    }

    // get the metadata token for this class
    mdTypeDef GetToken() { return m_token; }

    // Helper to get the AppDomain the class lives in.
    CordbAppDomain * GetAppDomain()
    {
        return m_pModule->GetAppDomain();
    }

    // This only very roughly resembles the CLASS_LOAD_LEVEL concept in the VM.
    // because DBI's needs are far more coarse grained. Also DBI
    // may contain more, equal, or less information than what is available in
    // native runtime data structures. We can have less when we are being lazy
    // and haven't yet fetched it. We can have more if use an independent data
    // source such as the metadata blob and then compute some type data ourselves
    typedef enum
    {
        // At this state the constructor has been run.
        // m_module and m_token will be valid
        Constructed,

        // At this state we have additionally certain to have initialized
        // m_fIsValueClass and m_fHasTypeParams
        // Calls to IsValueClass() and HasTypeParams() are valid
        // This stage should be achievable as long as a runtime type handle
        // exists, even if it is unrestored
        BasicInfo,

        //Everything is loaded, or at least anything created lazily from this
        //point on should be certain to succeed (ie m_type)
        FullInfo
    }
    ClassLoadLevel;

    ClassLoadLevel GetLoadLevel()
    {
        return m_loadLevel;
    }

    // determine if a load event has been sent for this class
    BOOL LoadEventSent() { return m_fLoadEventSent; }

    // set value of m_fLoadEventSent
    void SetLoadEventSent(BOOL fEventSent) { m_fLoadEventSent = fEventSent; }

    // determine if the class has been unloaded
    BOOL HasBeenUnloaded() { return m_fHasBeenUnloaded; }

    // set value of m_fHasBeenUnloaded
    void SetHasBeenUnloaded(BOOL fUnloaded) { m_fHasBeenUnloaded = (fUnloaded == TRUE); }

    // determine if this is a value class
    BOOL IsValueClassNoInit() { return m_fIsValueClass; }

    // set value of m_fIsValueClass
    void SetIsValueClass(BOOL fIsValueClass) { m_fIsValueClass = (fIsValueClass == TRUE); }

    // determine if the value class is known
    BOOL IsValueClassKnown() { return m_fIsValueClassKnown; }

    // set value of m_fIsValueClassKnown
    void SetIsValueClassKnown(BOOL fIsValueClassKnown) { m_fIsValueClassKnown = (fIsValueClassKnown == TRUE); }

    // get value of m_type
    CordbType * GetType() { return m_type; }

    void SetType(CordbType * pType) { m_type.Assign(pType); }

    // get the type parameter count
    bool HasTypeParams() { _ASSERTE(m_loadLevel >= BasicInfo); return m_fHasTypeParams; }

    // get the object size
    SIZE_T ObjectSize() { return m_classInfo.m_objectSize; }

    // get the metadata token for this class
    mdTypeDef MDToken() { return m_token; }

    // get the number of fields
    unsigned int FieldCount() { return m_classInfo.m_fieldList.Count(); }

    //-----------------------------------------------------------
    // Functionality shared for CordbType and CordbClass
    //-----------------------------------------------------------

    static HRESULT SearchFieldInfo(CordbModule *                module,
                                   DacDbiArrayList<FieldData> * pFieldList,
                                   mdTypeDef                    classToken,
                                   mdFieldDef                   fldToken,
                                   FieldData **                 ppFieldData);

    static HRESULT GetStaticFieldValue2(CordbModule *         pModule,
                                        FieldData *           pFieldData,
                                        BOOL                  fEnCHangingField,
                                        const Instantiation * pInst,
                                        ICorDebugFrame *      pFrame,
                                        ICorDebugValue **     ppValue);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    // Get information about a field that was added by EnC
    HRESULT GetEnCHangingField(mdFieldDef         fldToken,
                               FieldData **       ppFieldData,
                               CordbObjectValue * pObject);

private:
    // Get information via the DAC about a field added with Edit and Continue.
    FieldData * GetEnCFieldFromDac(BOOL               fStatic,
                                   CordbObjectValue * pObject,
                                   mdFieldDef         fieldToken);

    // Initialize an instance of EnCHangingFieldInfo.
    void InitEnCFieldInfo(EnCHangingFieldInfo * pEncField,
                          BOOL                  fStatic,
                          CordbObjectValue *    pObject,
                          mdFieldDef            fieldToken,
                          mdTypeDef             classToken);


public:

    // set or clear the custom notifications flag to control whether we ignore custom debugger notifications
    void SetCustomNotifications(BOOL fEnable) { m_fCustomNotificationsEnabled = fEnable; }
    BOOL CustomNotificationsEnabled () { return m_fCustomNotificationsEnabled; }

    HRESULT GetFieldInfo(mdFieldDef fldToken, FieldData ** ppFieldData);

    // If you want to force the init to happen even if we think the class
    // is up to date, set fForceInit to TRUE
    void Init(ClassLoadLevel desiredLoadLevel = FullInfo);

    // determine if any fields for a type are unallocated statics
    BOOL GotUnallocatedStatic(DacDbiArrayList<FieldData> * pFieldList);

    bool IsValueClass();
    HRESULT GetThisType(const Instantiation * pInst, CordbType ** ppResultType);
    static HRESULT PostProcessUnavailableHRESULT(HRESULT hr,
                               IMetaDataImport *pImport,
                               mdFieldDef fieldDef);
    mdTypeDef GetTypeDef() { return (mdTypeDef)m_id; }

#ifdef EnC_SUPPORTED
    // when we get an added field or method, mark the class to force re-init when we access it
    void MakeOld()
    {
        m_loadLevel = Constructed;
    }
#endif // EnC_SUPPORTED

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------
private:
    // contains information about the type: size and
    // field information
    ClassInfo                m_classInfo;

    ClassLoadLevel           m_loadLevel;

    // @dbgtodo  managed pipeline - can we get rid of both of these fields?
    BOOL                     m_fLoadEventSent;
    bool                     m_fHasBeenUnloaded;

    // [m_type] is the type object for when this class is used
    // as a type.  If the class is a value class then it can represent
    // either the boxed or unboxed type - it depends on the context where the
    // type is used.  For example on a CordbBoxValue it represents the type of the
    // boxed VC, on a CordbVCObjectValue it represents the type of the unboxed VC.
    //
    // The type field starts of NULL as there
    // is no need to create the type object until it is needed.
    RSSmartPtr<CordbType>    m_type;

    // Module that this Class lives in. Valid at the Constructed type level.
    CordbModule *            m_pModule;

    // the token for the type constructor - m_id cannot be used for constructed types
    // valid at the Constructed type level
    mdTypeDef                m_token;

    // Whether the class is a VC or not is discovered either by
    // seeing the class used in a signature after ELEMENT_TYPE_VALUETYPE
    // or ELEMENT_TYPE_CLASS or by going and asking the EE.
    bool                     m_fIsValueClassKnown;

    // Whether the class is a VC or not
    bool                     m_fIsValueClass;

    // Whether the class has generic type parameters in its definition
    bool                     m_fHasTypeParams;

    // Timestamp from GetProcess()->m_continueCounter, which we can use to tell if
    // the process has been continued since we last took a snapshot.
    UINT                     m_continueCounterLastSync;

    // if we add static fields with EnC after this class is loaded (in the debuggee),
    // their value will be hung off the FieldDesc.  Hold information about such fields here.
    CordbHangingFieldTable   m_hangingFieldsStatic;

    // this indicates whether we should send custom debugger notifications
    BOOL                    m_fCustomNotificationsEnabled;

};


/* ------------------------------------------------------------------------- *
 * TypeParameter enumerator class
 * ------------------------------------------------------------------------- */

class CordbTypeEnum : public CordbBase, public ICorDebugTypeEnum
{
public:
    // Factory method: Create a new instance of this class.  Returns NULL on out-of-memory.
    // On success, returns a new initialized instance of CordbTypeEnum with ref-count 0 (just like a ctor).
    // the life expectancy of the enumerator varies by caller so we require them to specify the applicable neuter list here.
    static CordbTypeEnum* Build(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, CordbType **ppTypars);
    static CordbTypeEnum* Build(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, RSSmartPtr<CordbType>*ppTypars);

    virtual ~CordbTypeEnum() ;

    virtual void Neuter();


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbTypeEnum"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugEnum
    //-----------------------------------------------------------

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    //-----------------------------------------------------------
    // ICorDebugTypeEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugType *Types[], ULONG *pceltFetched);

private:
    // Private constructor, only partially initializes the object.
    // Clients should use the 'Build' factory method to create an instance of this class.
    CordbTypeEnum( CordbAppDomain * pAppDomain, NeuterList * pNeuterList );
    template<class T> static CordbTypeEnum* BuildImpl(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, T* ppTypars );

    // Owning object.
    CordbAppDomain * m_pAppDomain;

    // Array of Types. We own the array, and share refs to the types.
    // @todo- since these are guaranteed to be kept alive as long as we're not neutered,
    // we don't need to keep refs to them.
    RSSmartPtr<CordbType> * m_ppTypars;
    UINT   m_iCurrent;
    UINT   m_iMax;
};

/* ------------------------------------------------------------------------- *
 * Code enumerator class
 * ------------------------------------------------------------------------- */

class CordbCodeEnum : public CordbBase, public ICorDebugCodeEnum
{
public:
    CordbCodeEnum(unsigned int cCode, RSSmartPtr<CordbCode> * ppCode);
    virtual ~CordbCodeEnum() ;


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbCodeEnum"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugEnum
    //-----------------------------------------------------------

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    //-----------------------------------------------------------
    // ICorDebugCodeEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugCode *Codes[], ULONG *pceltFetched);

private:
    // Ptr to an array of CordbCode*
    // We own the array.
    RSSmartPtr<CordbCode> * m_ppCodes;
    UINT   m_iCurrent;
    UINT   m_iMax;
};





typedef CUnorderedArray<CordbCode*,11> UnorderedCodeArray;
//<TODO>@todo port: different SIZE_T size/</TODO>
const int DMI_VERSION_INVALID = 0;
const int DMI_VERSION_MOST_RECENTLY_JITTED = 1;
const int DMI_VERSION_MOST_RECENTLY_EnCED = 2;


/* ------------------------------------------------------------------------- *
 * Function class
 *
 * @review .  The CordbFunction class now keeps a multiple MethodDescInfo
 * structures in a hash table indexed by tokens provided by the left-side.
 * In 99.9% of cases this hash table will only contain one entry - we only
 * use a hashtable to cover the case where we have multiple JITtings of
 * a single version of a function, in particular multiple JITtings of generic
 * code under different instantiations. This will increase space usage.
 * The way around it is to store one CordbNativeCode in-line in the CordbFunction
 * class, or at least store one such pointer so no hash table will normally
 * be needed.  This is similar to other cases, e.g. the hash table in
 * CordbClass used to indicate different CordbTypes made from that class -
 * again in the normal case these tables will only contain one element.
 *
 * However, for the moment I've focused on correctness and we can minimize
 * this space usage in due course.
 * ------------------------------------------------------------------------- */

const BOOL bNativeCode = FALSE;
const BOOL bILCode = TRUE;

//
// Each E&C version gets its own function object. So the IL that a function
// is associated w/ does not change.
// B/C of generics, a single IL function may get jitted multiple times and
// be associated w/ multiple native code blobs (CordbNativeCode).
//
class CordbFunction : public CordbBase,
                      public ICorDebugFunction,
                      public ICorDebugFunction2,
                      public ICorDebugFunction3,
                      public ICorDebugFunction4
{
public:
    //-----------------------------------------------------------
    // Create from scope and member objects.
    //-----------------------------------------------------------
    CordbFunction(CordbModule * m,
                  mdMethodDef token,
                  SIZE_T enCVersion);
    virtual ~CordbFunction();
    virtual void Neuter();



#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbFunction"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFunction
    //-----------------------------------------------------------
    COM_METHOD GetModule(ICorDebugModule **pModule);
    COM_METHOD GetClass(ICorDebugClass **ppClass);
    COM_METHOD GetToken(mdMethodDef *pMemberDef);
    COM_METHOD GetILCode(ICorDebugCode **ppCode);
    COM_METHOD GetNativeCode(ICorDebugCode **ppCode);
    COM_METHOD CreateBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);
    COM_METHOD GetLocalVarSigToken(mdSignature *pmdSig);
    COM_METHOD GetCurrentVersionNumber(ULONG32 *pnCurrentVersion);

    //-----------------------------------------------------------
    // ICorDebugFunction2
    //-----------------------------------------------------------
    COM_METHOD SetJMCStatus(BOOL fIsUserCode);
    COM_METHOD GetJMCStatus(BOOL * pfIsUserCode);
    COM_METHOD EnumerateNativeCode(ICorDebugCodeEnum **ppCodeEnum) { return E_NOTIMPL; }
    COM_METHOD GetVersionNumber(ULONG32 *pnCurrentVersion);

    //-----------------------------------------------------------
    // ICorDebugFunction3
    //-----------------------------------------------------------
    COM_METHOD GetActiveReJitRequestILCode(ICorDebugILCode **ppReJitedILCode);

    //-----------------------------------------------------------
    // ICorDebugFunction4
    //-----------------------------------------------------------
    COM_METHOD CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);

    //-----------------------------------------------------------
    // Internal members
    //-----------------------------------------------------------
protected:
    // Returns the function's ILCode and SigToken
    HRESULT GetILCodeAndSigToken();

    // Get the metadata token for the class to which a function belongs.
    mdTypeDef InitParentClassOfFunctionHelper(mdToken funcMetaDataToken);

    // Get information about one of the native code blobs for this function
    HRESULT InitNativeCodeInfo();

public:

    // Get the class to which a given function belongs
    HRESULT InitParentClassOfFunction();

    void NotifyCodeCreated(CordbNativeCode* nativeCode);

    HRESULT GetSig(SigParser *pMethodSigParser,
                   ULONG *pFunctionArgCount,
                   BOOL *pFunctionIsStatic);

    HRESULT GetArgumentType(DWORD dwIndex, const Instantiation * pInst, CordbType ** ppResultType);


    //-----------------------------------------------------------
    // Internal routines
    //-----------------------------------------------------------

    // Get the existing IL code object
    HRESULT GetILCode(CordbILCode ** ppCode);

    // Finds or creates an ILCode for a given rejit request
    HRESULT LookupOrCreateReJitILCode(VMPTR_ILCodeVersionNode vmILCodeVersionNode,
                                      CordbReJitILCode** ppILCode);


#ifdef EnC_SUPPORTED
    void MakeOld();
#endif

    //-----------------------------------------------------------
    // Accessors
    //-----------------------------------------------------------

    // Get the AppDomain that this function lives in.
    CordbAppDomain * GetAppDomain()
    {
        return (m_pModule->GetAppDomain());
    }

    // Get the CordbModule that this Function lives in.
    CordbModule * GetModule()
    {
        return m_pModule;
    }

    // Get the CordbClass this of which this function is a member
    CordbClass * GetClass()
    {
        return m_pClass;
    }

    // Get the IL code blob corresponding to this function
    CordbILCode * GetILCode()
    {
        return m_pILCode;
    }

    // Get metadata token for this function
    mdMethodDef GetMetadataToken()
    {
        return m_MDToken;
    }

    SIZE_T GetEnCVersionNumber()
    {
        return m_dwEnCVersionNumber;
    }

    CordbFunction * GetPrevVersion()
    {
        return m_pPrevVersion;
    }

    void SetPrevVersion(CordbFunction * prevVersion)
    {
        m_pPrevVersion.Assign(prevVersion);
    }

    typedef enum {kNativeOnly, kHasIL, kUnknownImpl} ImplementationKind;
    ImplementationKind IsNativeImpl()
    {
        return (m_fIsNativeImpl);
    }

    // determine whether we have a native-only implementation
    void InitNativeImpl();


    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
    // The module that this Function is contained in. It maintains a strong reference to this object
    // and will neuter this object.
    CordbModule *            m_pModule;

    // The Class that this function is contained in.
    CordbClass *             m_pClass;

    // We only have 1 IL blob associated with a given Function object.
    RSSmartPtr<CordbILCode>  m_pILCode;


    // Generics allow a single IL method to be instantiated to multiple native
    // code blobs. So CordbFunction : CordbNativeCode is 1:n.
    // This pointer is to arbitrary one of those n code bodies.
    // Someday we may need to get access to all N of them but not today
    RSSmartPtr<CordbNativeCode> m_nativeCode;

    // Metadata Token for the IL function. Scoped to m_module.
    const mdMethodDef        m_MDToken;

    // EnC version number of this instance
    SIZE_T                   m_dwEnCVersionNumber;

    // link to previous version of this function
    RSSmartPtr<CordbFunction> m_pPrevVersion;

    // Is the function implemented natively in the runtime?? (eg, it has no IL, may be an Ecall/fcall)
    ImplementationKind       m_fIsNativeImpl;

    // True if method signature (argument) values are cached.
    BOOL                     m_fCachedMethodValuesValid;

    // Cached SigParser for this Function's argument signature.
    // Only valid if m_fCachedMethodValuesValid is set.
    SigParser                m_methodSigParserCached;

    // Cached Count of arguments in the argument signature.
    // Only valid if m_fCachedMethodValuesValid is set.
    ULONG                    m_argCountCached;

    // Cached boolean if method is static or instance (part of the argument signature).
    // Only valid if m_fCachedMethodValuesValid is set.
    BOOL                     m_fIsStaticCached;

    // A collection, indexed by VMPTR_SharedReJitInfo, of IL code for rejit requests
    // The collection is filled lazily by LookupOrCreateReJitILCode
    CordbSafeHashTable<CordbReJitILCode> m_reJitILCodes;
};

//-----------------------------------------------------------------------------
// class CordbCode
// Represents either IL or Native code blobs associated with a function.
//
// See the comments at the ICorDebugCode definition for invariants about Code objects.
//
//-----------------------------------------------------------------------------
class CordbCode : public CordbBase, public ICorDebugCode
{
protected:
    CordbCode(CordbFunction * pFunction, UINT_PTR id, SIZE_T encVersion, BOOL fIsIL);

public:
    virtual ~CordbCode();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() = 0;
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugCode
    //-----------------------------------------------------------

    COM_METHOD IsIL(BOOL * pbIL);
    COM_METHOD GetFunction(ICorDebugFunction ** ppFunction);
    COM_METHOD GetAddress(CORDB_ADDRESS * pStart) = 0;
    COM_METHOD GetSize(ULONG32 * pcBytes);
    COM_METHOD CreateBreakpoint(ULONG32 offset,
                                ICorDebugFunctionBreakpoint ** ppBreakpoint);
    COM_METHOD GetCode(ULONG32 startOffset, ULONG32 endOffset,
                       ULONG32 cBufferAlloc,
                       BYTE buffer[],
                       ULONG32 * pcBufferSize);
    COM_METHOD GetVersionNumber( ULONG32 * nVersion);
    COM_METHOD GetILToNativeMapping(ULONG32 cMap,
                                    ULONG32 * pcMap,
                                    COR_DEBUG_IL_TO_NATIVE_MAP map[]) = 0;
    COM_METHOD GetEnCRemapSequencePoints(ULONG32 cMap,
                                         ULONG32 * pcMap,
                                         ULONG32 offsets[]);

    //-----------------------------------------------------------
    // Accessors and convenience routines
    //-----------------------------------------------------------

    // get the CordbFunction instance for this code object
    CordbFunction * GetFunction();

    // get the actual code bytes for this function
    virtual HRESULT ReadCodeBytes() = 0;

    // get the size in bytes of this function
    virtual ULONG32 GetSize() = 0;


    // get the metadata token for this code object
    mdMethodDef GetMetadataToken()
    {
        _ASSERTE(m_pFunction != NULL);
        return (m_pFunction->GetMetadataToken());
    }

    // get the module this code object belongs to
    CordbModule * GetModule()
    {
        _ASSERTE(m_pFunction != NULL);
        return (m_pFunction->GetModule());
    }

    // get the function signature for this code blob or throw on failure
    void GetSig(SigParser *pMethodSigParser,
                ULONG *pFunctionArgCount,
                BOOL *pFunctionIsStatic)
    {
        _ASSERTE(m_pFunction != NULL);
        IfFailThrow(m_pFunction->GetSig(pMethodSigParser, pFunctionArgCount, pFunctionIsStatic));
    }

    // get the class to which this code blob belongs
    CordbClass * GetClass()
    {
        _ASSERTE(m_pFunction != NULL);
        return (m_pFunction->GetClass());
    }

    // Quick helper to get the AppDomain that this code object lives in.
    CordbAppDomain *GetAppDomain()
    {
        _ASSERTE(m_pFunction != NULL);
        return (m_pFunction->GetAppDomain());
    }

    // Get the EnC version of this blob
    SIZE_T GetVersion() { return m_nVersion; };

    // Return true if this is an IL code blob. Else return false.
    BOOL IsIL() { return m_fIsIL; }

    // convert to CordbNativeCode as long as m_fIsIl is false.
    CordbNativeCode * AsNativeCode()
    {
        _ASSERTE(m_fIsIL == FALSE);
        return reinterpret_cast<CordbNativeCode *>(this);
    }

    // convert to CordbILCode as long as m_fIsIl is true.
    CordbILCode * AsILCode()
    {
        _ASSERTE(m_fIsIL == TRUE);
        return reinterpret_cast<CordbILCode *>(this);
    }

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
    UINT m_fIsIL : 1;

    // EnC version number.
    SIZE_T                 m_nVersion;

protected:
    // Our local copy of the code. It will be GetSize() bytes long.
    BYTE *                 m_rgbCode; // will be NULL if we can't fit it into memory

    UINT                   m_continueCounterLastSync;

    // Owning Function associated with this code.
    CordbFunction *        m_pFunction;
}; //class CordbCode





/* ------------------------------------------------------------------------- *
* CordbILCode class
* This class represents an IL code blob for a particular EnC version. Thus it is
* 1:1 with a given instantiation of CordbFunction. Provided functionality includes
* methods to get the starting address and size of an IL code blob and to read
* the actual bytes of IL into a buffer.
 * ------------------------------------------------------------------------- */

class CordbILCode : public CordbCode
{
public:
    // Initialize a new CordbILCode instance
    CordbILCode(CordbFunction *pFunction, TargetBuffer codeRegionInfo, SIZE_T nVersion, mdSignature localVarSigToken, UINT_PTR id = 0);

#ifdef _DEBUG
    const char * DbgGetName() { return "CordbILCode"; };
#endif // _DEBUG

    COM_METHOD GetAddress(CORDB_ADDRESS * pStart);
    COM_METHOD GetILToNativeMapping(ULONG32 cMap,
                                    ULONG32 * pcMap,
                                    COR_DEBUG_IL_TO_NATIVE_MAP map[]);
    // Quick helper for internal access to: GetAddress(CORDB_ADDRESS *pStart);
    CORDB_ADDRESS GetAddress() { return m_codeRegionInfo.pAddress; }

    // get total size of the IL code
    ULONG32 GetSize() { return m_codeRegionInfo.cbSize; }

#ifdef EnC_SUPPORTED
    void MakeOld();
#endif // EnC_SUPPORTED

    HRESULT GetLocalVarSig(SigParser *pLocalsSigParser, ULONG *pLocalVarCount);
    HRESULT GetLocalVariableType(DWORD dwIndex, const Instantiation * pInst, CordbType ** ppResultType);
    mdSignature GetLocalVarSigToken();

    COM_METHOD CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint);

private:
    // Read the actual bytes of IL code into the data member m_rgbCode.
    // Helper routine for GetCode
    HRESULT ReadCodeBytes();

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
#ifdef EnC_SUPPORTED
    UINT m_fIsOld : 1;           // marks this instance as an old EnC version
    bool m_encBreakpointsApplied;
#endif

    // derived types can init this
protected:
    TargetBuffer m_codeRegionInfo;  // stores the starting address and size of the
                                    // IL code blob

    // Metadata token for local's signature.
    mdSignature m_localVarSigToken;

}; // class CordbILCode

/* ------------------------------------------------------------------------- *
* CordbReJitILCode class
* This class represents an IL code blob for a particular EnC version and
* rejitID. Thus it is 1:N with a given instantiation of CordbFunction.
* ------------------------------------------------------------------------- */

class CordbReJitILCode : public CordbILCode,
                         public ICorDebugILCode,
                         public ICorDebugILCode2
{
public:
    // Initialize a new CordbILCode instance
    CordbReJitILCode(CordbFunction *pFunction, SIZE_T encVersion, VMPTR_ILCodeVersionNode vmILCodeVersionNode);

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void** ppInterface);


    //-----------------------------------------------------------
    // ICorDebugILCode
    //-----------------------------------------------------------
    COM_METHOD GetEHClauses(ULONG32 cClauses, ULONG32 * pcClauses, CorDebugEHClause clauses[]);


    //-----------------------------------------------------------
    // ICorDebugILCode2
    //-----------------------------------------------------------
    COM_METHOD GetLocalVarSigToken(mdSignature *pmdSig);
    COM_METHOD GetInstrumentedILMap(ULONG32 cMap, ULONG32 *pcMap, COR_IL_MAP map[]);

private:
    HRESULT Init(DacSharedReJitInfo* pSharedReJitInfo);

private:
    ULONG32 m_cClauses;
    NewArrayHolder<CorDebugEHClause> m_pClauses;
    ULONG32 m_cbLocalIL;
    NewArrayHolder<BYTE> m_pLocalIL;
    ULONG32 m_cILMap;
    NewArrayHolder<COR_IL_MAP> m_pILMap;
};

/* ------------------------------------------------------------------------- *
 * CordbNativeCode class. These correspond to MethodDesc's on the left-side.
 * There may or may not be a DebuggerJitInfo associated with the MethodDesc.
 * At most one CordbNativeCode is created for each native code compilation of each method
 * that is seen by the right-side.  Note that if each method were JITted only once
 * then this information could go in CordbFunction, however generics allow
 * methods to be compiled more than once.
 *
 * The purpose of this class is to encapsulate details about a blob of jitted/ngen'ed
 * code, including an optional set of mappings from IL to offsets in the native Code.
 * ------------------------------------------------------------------------- */

class CordbNativeCode : public CordbCode,
                        public ICorDebugCode2,
                        public ICorDebugCode3,
                        public ICorDebugCode4
{
public:
    CordbNativeCode(CordbFunction * pFunction,
                    const NativeCodeFunctionData * pJitData,
                    BOOL fIsInstantiatedGeneric);
#ifdef _DEBUG
    const char * DbgGetName() { return "CordbNativeCode"; };
#endif // _DEBUG

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugCode
    //-----------------------------------------------------------
    COM_METHOD GetAddress(CORDB_ADDRESS * pStart);
    COM_METHOD GetILToNativeMapping(ULONG32 cMap,
                                    ULONG32 * pcMap,
                                    COR_DEBUG_IL_TO_NATIVE_MAP map[]);
    //-----------------------------------------------------------
    // ICorDebugCode2
    //-----------------------------------------------------------
    COM_METHOD GetCodeChunks(ULONG32 cbufSize, ULONG32 * pcnumChunks, CodeChunkInfo chunks[]);

    COM_METHOD GetCompilerFlags(DWORD * pdwFlags);

    //-----------------------------------------------------------
    // ICorDebugCode3
    //-----------------------------------------------------------
    COM_METHOD GetReturnValueLiveOffset(ULONG32 ILoffset, ULONG32 bufferSize, ULONG32 *pFetched, ULONG32 *pOffsets);


    //-----------------------------------------------------------
    // ICorDebugCode4
    //-----------------------------------------------------------
    COM_METHOD EnumerateVariableHomes(ICorDebugVariableHomeEnum **ppEnum);

    //-----------------------------------------------------------
    // Internal members
    //-----------------------------------------------------------

    HRESULT ILVariableToNative(DWORD dwIndex,
                               SIZE_T ip,
                               const ICorDebugInfo::NativeVarInfo ** ppNativeInfo);
    void LoadNativeInfo();

    //-----------------------------------------------------------
    // Accessors and convenience routines
    //-----------------------------------------------------------

    // get the argument type for a generic
    void GetArgumentType(DWORD                 dwIndex,
                         const Instantiation * pInst,
                         CordbType **          ppResultType)
    {
        CordbFunction * pFunction = GetFunction();
        _ASSERTE(pFunction != NULL);
        IfFailThrow(pFunction->GetArgumentType(dwIndex, pInst, ppResultType));
    }

    // Quick helper for internall access to: GetAddress(CORDB_ADDRESS *pStart);
    CORDB_ADDRESS GetAddress() { return m_rgCodeRegions[kHot].pAddress; };

    VMPTR_MethodDesc GetVMNativeCodeMethodDescToken() { return m_vmNativeCodeMethodDescToken; };

    // Worker function for GetReturnValueLiveOffset.
    HRESULT GetReturnValueLiveOffsetImpl(Instantiation *currentInstantiation, ULONG32 ILoffset, ULONG32 bufferSize, ULONG32 *pFetched, ULONG32 *pOffsets);

    // get total size of the code including both hot and cold regions
    ULONG32 GetSize();

    // get the size of the cold region(s) only
    ULONG32 GetColdSize();

    // Return true if the Code is split into hot + cold regions.
    bool HasColdRegion() { return m_rgCodeRegions[kCold].pAddress != NULL; }

    // Get the number of fixed arguments for this function (the "this"
    // but not varargs)
    unsigned int GetFixedArgCount()
    {
        return m_nativeVarData.GetFixedArgCount();
    }

    // Get the number of all arguments for this function
    // ("this" pointer, fixed args and varargs)
    ULONG32 GetAllArgsCount()
    {
        return m_nativeVarData.GetAllArgsCount();
    }

    void SetAllArgsCount(ULONG32 count)
    {
        m_nativeVarData.SetAllArgsCount(count);
    }

    // Determine whether this is an instantiation of a generic function
    BOOL IsInstantiatedGeneric()
    {
        return m_fIsInstantiatedGeneric != 0;
    }

    // Determine whether we have initialized the native variable and
    // sequence point offsets
    BOOL IsNativeCodeValid ()
    {
        return ((m_nativeVarData.IsInitialized() != 0) &&
               (m_sequencePoints.IsInitialized() != 0));
    }

    SequencePoints * GetSequencePoints()
    {
        return &m_sequencePoints;
    }


    // Given an ILOffset in the current function, return the class token and function token of the IL call target at that
    // location.  Also fill "methodSig" with the method's signature and "genericSig" with the method's generic signature.
    HRESULT GetCallSignature(ULONG32 ILOffset, mdToken *pClass, mdToken *pMDFunction, SigParser &methodSig, SigParser &genericSig);

    // Moves a method signature from the start of the signature to the location of the return value (passing out the
    // number of generic parameters in the method).
    static HRESULT SkipToReturn(SigParser &parser, uint32_t *genArgCount = 0);

private:
    // Read the actual bytes of native code into the data member m_rgbCode.
    // Helper routine for GetCode
    HRESULT ReadCodeBytes();

    // Returns a failure HRESULT if we cannot handle the return value of the given
    // methodref, methoddef, or methodspec token, otherwise S_OK.  Does NOT return S_FALSE;
    HRESULT EnsureReturnValueAllowed(Instantiation *currentInstantiation, mdToken targetClass, SigParser &parser, SigParser &methodGenerics);
    HRESULT EnsureReturnValueAllowedWorker(Instantiation *currentInstantiation, mdToken targetClass, SigParser &parser, SigParser &methodGenerics, ULONG genCount);

    // Grabs the appropriate signature parser for a methodref, methoddef, methodspec.
    HRESULT GetSigParserFromFunction(mdToken mdFunction, mdToken *pClass, SigParser &methodSig, SigParser &genericSig);

    int GetCallInstructionLength(BYTE *buffer, ULONG32 len);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------
private:
    // offset of the beginning of the last sequence point in the sequence point map
    SIZE_T                   m_lastIL;

    // start address(es) and size(s) of hot and cold regions
    TargetBuffer             m_rgCodeRegions[MAX_REGIONS];

    // LS data structure--method desc for this instantiation.
    VMPTR_MethodDesc         m_vmNativeCodeMethodDescToken;

    bool                     m_fCodeAvailable;          // true iff the code has been jitted but not pitched

    bool                     m_fIsInstantiatedGeneric;  // true iff this is an instantiated generic

    // information in the following two classes tracks native offsets and is initialized on demand.

    // location and ID information for local variables. See code:NativeVarData for details.
    NativeVarData            m_nativeVarData;

    // mapping between IL and native code sequence points.
    SequencePoints           m_sequencePoints;

}; //class CordbNativeCode

//---------------------------------------------------------------------------------------
//
// GetActiveInternalFramesData is used to enumerate internal frames on a specific thread.
// It is used in conjunction with code:CordbThread::GetActiveInternalFramesCallback.
// We store each internal frame in ppInternalFrames as we enumerate them.
//

struct GetActiveInternalFramesData
{
public:
    // the thread we are walking
    CordbThread * pThis;

    // an array to store the internal frames
    RSPtrArray<CordbInternalFrame> pInternalFrames;

    // next element in the array to be filled
    ULONG32 uIndex;
};


/* ------------------------------------------------------------------------- *
 * Thread classes
 * ------------------------------------------------------------------------- */

class CordbThread : public CordbBase, public ICorDebugThread,
                                      public ICorDebugThread2,
                                      public ICorDebugThread3,
                                      public ICorDebugThread4
{
public:
    CordbThread(CordbProcess * pProcess, VMPTR_Thread);

    virtual ~CordbThread();
    virtual void Neuter();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbThread"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        // there's an external add ref from within RS in CordbEnumFilter
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugThread
    //-----------------------------------------------------------

    COM_METHOD GetProcess(ICorDebugProcess **ppProcess);
    COM_METHOD GetID(DWORD *pdwThreadId);
    COM_METHOD GetHandle(HANDLE * phThreadHandle);
    COM_METHOD GetAppDomain(ICorDebugAppDomain **ppAppDomain);
    COM_METHOD SetDebugState(CorDebugThreadState state);
    COM_METHOD GetDebugState(CorDebugThreadState *pState);
    COM_METHOD GetUserState(CorDebugUserState *pState);
    COM_METHOD GetCurrentException(ICorDebugValue ** ppExceptionObject);
    COM_METHOD ClearCurrentException();
    COM_METHOD CreateStepper(ICorDebugStepper **ppStepper);
    COM_METHOD EnumerateChains(ICorDebugChainEnum **ppChains);
    COM_METHOD GetActiveChain(ICorDebugChain **ppChain);
    COM_METHOD GetActiveFrame(ICorDebugFrame **ppFrame);
    COM_METHOD GetRegisterSet(ICorDebugRegisterSet **ppRegisters);
    COM_METHOD CreateEval(ICorDebugEval **ppEval);
    COM_METHOD GetObject(ICorDebugValue ** ppObject);

    // ICorDebugThread2
    COM_METHOD GetConnectionID(CONNID * pConnectionID);
    COM_METHOD GetTaskID(TASKID * pTaskID);
    COM_METHOD GetVolatileOSThreadID(DWORD * pdwTID);
    COM_METHOD GetActiveFunctions(ULONG32 cFunctions, ULONG32 * pcFunctions, COR_ACTIVE_FUNCTION pFunctions[]);
    // Intercept the current exception at the specified frame.  pFrame must be a valid ICDFrame, possibly from
    // a previous stackwalk.
    COM_METHOD InterceptCurrentException(ICorDebugFrame * pFrame);



    // ICorDebugThread3
    COM_METHOD CreateStackWalk(ICorDebugStackWalk **ppStackWalk);

    COM_METHOD GetActiveInternalFrames(ULONG32 cInternalFrames,
                                       ULONG32 * pcInternalFrames,
                                       ICorDebugInternalFrame2 * ppInternalFrames[]);

    // ICorDebugThread4
    COM_METHOD HasUnhandledException();

    COM_METHOD GetBlockingObjects(ICorDebugBlockingObjectEnum **ppBlockingObjectEnum);

    // Gets the current CustomNotification object from the thread or NULL if no such object exists
    COM_METHOD GetCurrentCustomDebuggerNotification(ICorDebugValue ** ppNotificationObject);
    //-----------------------------------------------------------
    // Internal members
    //-----------------------------------------------------------

    // callback used to enumerate the internal frames on a thread
    static void GetActiveInternalFramesCallback(const DebuggerIPCE_STRData * pFrameData,
                                                void *                 pUserData);

    CorDebugUserState GetUserState();

    // Given a FramePointer, find the matching CordbFrame.
    HRESULT FindFrame(ICorDebugFrame ** ppFrame, FramePointer fp);

    // Get the task ID for this thread.
    TASKID GetTaskID();

    void RefreshStack();
    void CleanupStack();
    void MarkStackFramesDirty();


#if defined(TARGET_X86)
    // Converts the values in the floating point register area of the context to real number values.
    void Get32bitFPRegisters(CONTEXT * pContext);

#elif defined(TARGET_AMD64) ||  defined(TARGET_ARM64) || defined(TARGET_ARM)
    // Converts the values in the floating point register area of the context to real number values.
    void Get64bitFPRegisters(FPRegister64 * rgContextFPRegisters, int start, int nRegisters);

#endif // TARGET_X86

   // Initializes the float state members of this instance of CordbThread. This function gets the context and
   // converts the floating point values from their context representation to real number values.
   void LoadFloatState();


    HRESULT SetIP(  bool fCanSetIPOnly,
                    CordbNativeCode * pNativeCode,
                    SIZE_T offset,
                    bool fIsIL );

    // Tells the LS to remap to the latest version of the function
    HRESULT SetRemapIP(SIZE_T offset);

    // Ask the left-side for the current (up-to-date) AppDomain of this thread's IP.
    // This should be preferred over using the cached value from GetAppDomain.
    HRESULT GetCurrentAppDomain(CordbAppDomain ** ppAppDomain);

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------

    // The last domain from which a debug event for this thread was sent.
    // This usually (but not always) the domain the thread is currently executing in.
    // Since this is a cache, it may sometimes be out-of-date.  I believe all current
    // usage of this is OK (we pass AppDomains around a lot without really using them),
    // but no new code should rely on this value.
    // TODO: eliminate this and the m_pAppDomain field entirely
    CordbAppDomain *GetAppDomain()
    {
        return (m_pAppDomain);
    }

    DWORD GetVolatileOSThreadID();

    //////////////////////////////////////////////////////////////////////////
    //
    // Get Context
    //
    //      <TODO>TODO: Since Thread will share the memory with RegisterSets, how
    //      do we know that the RegisterSets have relinquished all pointers
    //      to the m_pContext structure?</TODO>
    //
    // Returns: NULL if the thread's CONTEXT structure couldn't be obtained
    //   A pointer to the CONTEXT otherwise.
    //
    //
    //////////////////////////////////////////////////////////////////////////
    HRESULT GetManagedContext( DT_CONTEXT ** ppContext );
    HRESULT SetManagedContext( DT_CONTEXT * pContext );

    // API to retrieve the thread handle from the LS.
    void InternalGetHandle(HANDLE * phThread);
    void RefreshHandle(HANDLE * phThread);

    // NeuterList that's executed when this Thread's stack is refreshed.
    // Chain + Frame + some Value enums can be held on this.
    NeuterList * GetRefreshStackNeuterList()
    {
        return &m_RefreshStackNeuterList;
    }

    DWORD GetUniqueId();


    // Hijack a thread at a 2nd-chance exception so that it can execute the CLR's UEF
    void HijackForUnhandledException();

    // check whether the specified frame lives on the stack of the current thread
    bool OwnsFrame(CordbFrame *pFrame);

    // Specify that there's an outstanding exception on this thread.
    void SetExInfo(VMPTR_OBJECTHANDLE vmExcepObjHandle);

    VMPTR_OBJECTHANDLE GetThreadExceptionRawObjectHandle() { return m_vmExcepObjHandle; }
    bool HasException() { return m_fException; }

    void SetUnhandledNativeException(const EXCEPTION_RECORD * pExceptionRecord);
    bool HasUnhandledNativeException();

#ifdef _DEBUG
    // Helper to assert that this thread no longer appears in dac-dbi enumerations
    void DbgAssertThreadDeleted();

    // Callback for DbgAssertThreadDeleted
    static void DbgAssertThreadDeletedCallback(VMPTR_Thread vmThread, void * pUserData);
#endif // _DEBUG

    // Determine if the thread's current exception is managed or unmanaged.
    BOOL IsThreadExceptionManaged();

    // This is a private hook for the shim to create a CordbRegisterSet for a ShimChain.
   void CreateCordbRegisterSet(DT_CONTEXT *            pContext,
                               BOOL                    fActive,
                               CorDebugChainReason     reason,
                               ICorDebugRegisterSet ** ppRegSet);

    // This is a private hook for the shim to convert an ICDFrame into an ICDInternalFrame for a dynamic
    // method.  Refer to the function header for more information.
   BOOL ConvertFrameForILMethodWithoutMetadata(ICorDebugFrame *           pFrame,
                                               ICorDebugInternalFrame2 ** ppInternalFrame2);

    // Gets/sets m_fCreationEventQueued
    bool CreateEventWasQueued();
    void SetCreateEventQueued();

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    // RS Cache for LS context.
    // NULL if we haven't allocated memory for a Right side context
    DT_CONTEXT *          m_pContext;

    // Set to the CONTEXT pointer in the LS if this LS thread is
    // stopped in managed code. This may be either stopped for execution control
    // (breakpoint / single-step exception) or hijacked w/ a redirected frame because
    // another thread synced the LS.
    // This context is used by the RS to set enregistered vars.
    VMPTR_CONTEXT         m_vmLeftSideContext;

    // indicates whether m_pContext is up-to-date
    bool                  m_fContextFresh;

    // last domain we've seen this thread.
    // If the appdomain exits, it will clear out this value.
    CordbAppDomain       *m_pAppDomain;

    // Handle to VM's Thread* object. This is the primary key for a CordbThread object
    // @dbgtodo  ICDThread - merge with m_id;
    VMPTR_Thread          m_vmThreadToken;

    // Unique ID for this thread. See code:CordbThread::GetID for semantics of this field.
    DWORD                 m_dwUniqueID;

    CorDebugThreadState   m_debugState; // Note that this is for resume
                                        // purposes, NOT the current state of
                                        // the thread.

    // The frames are all protected under the Stop-Go lock.
    // This field indicates whether the stack is valid (i.e. no update is necessary).
    bool                  m_fFramesFresh;

    // This is a cache of V3 ICDFrames.  The cache is only used by two functions:
    //     - code:CordbThread::GetActiveFunctions
    //     - code:CordbThread::InterceptCurrentException.
    //
    //  We don't clear the cache in CleanupStack() because we don't refresh the cache every time we stop.
    //  Instead, we mark m_fFramesFresh in CleanupStack() and clear the cache only when it is used next time.
    CDynArray<CordbFrame *> m_stackFrames;

    bool                  m_fFloatStateValid;
    unsigned int          m_floatStackTop;
    double                m_floatValues[DebuggerIPCE_FloatCount];

private:
    // True for the window after an Exception callback, but before it's been continued.
    // We dispatch two exception events in a row (ICDManagedCallback::Exception and ICDManagedCallback2::Exception),
    // and a debugger may normally just skip the first one knowing it can stop on the 2nd once.
    // Both events will set this bit high. Be careful not to reset this bit inbetween them.
    bool                  m_fException;

    // True if a creation event has been queued for this thread
    // The event may or may not have been dispatched yet
    // Bugfix DevDiv2\DevDiv 77523 - this is only being set from ShimProcess::QueueFakeThreadAttachEventsNativeOrder
    bool                  m_fCreationEventQueued;

    // Object handle for Exception object in debuggee.
    VMPTR_OBJECTHANDLE    m_vmExcepObjHandle;

public:

    //Returns true if current user state of a thread is USER_WAIT_SLEEP_JOIN
    bool IsThreadWaitingOrSleeping();

    // Returns true if the thread is dead. See function header for definition.
    bool IsThreadDead();

    // Return CORDBG_E_BAD_THREAD_STATE if the thread is dead.
    HRESULT EnsureThreadIsAlive();

    // On a RemapBreakpoint, the debugger will eventually call RemapFunction and
    // we need to communicate the IP back to LS. So we stash the address of where
    // to store the IP here and stuff it in on RemapFunction.
    // If we're not at an outstanding RemapOpportunity, this will be NULL
    REMOTE_PTR            m_EnCRemapFunctionIP;

private:
    void ClearStackFrameCache();

    // True iff this thread has an unhandled exception on it.
    // Set high when Filter() gets noitifed of an unhandled exception.
    // Set Low if the thread is hijacked.
    bool                  m_fHasUnhandledException;

    // Exception record for last unhandled exception on this thread.
    // Lazily initialized.
    EXCEPTION_RECORD *  m_pExceptionRecord;

    static const CorDebugUserState kInvalidUserState = CorDebugUserState(-1);
    CorDebugUserState     m_userState;  // This is the current state of the
                                        // thread, at the time that the
                                        // left side synchronized

    // NeuterList that's executed when this Thread's stack is refreshed.
    // This list is for everything related to stackwalking, i.e. everything which is invalidated
    // if the stack changes in any way.  This list is cleared when any of the following is called:
    //     1) Continue()
    //     2) SetIP()
    //     3) RemapFunction()
    //     4) ICDProcess::SetThreadContext()
    NeuterList            m_RefreshStackNeuterList;

    // The following two data members are used for caching thread handles.
    // @dbgtodo  - Remove in V3 (can't have local handles with data-target abstraction);
    // offload to the shim to support V2 scenarios.
    HANDLE                m_hCachedThread;
    HANDLE                m_hCachedOutOfProcThread;
};

/* ------------------------------------------------------------------------- *
 * StackWalk class
 * ------------------------------------------------------------------------- */

class CordbStackWalk : public CordbBase, public ICorDebugStackWalk
{
public:
    CordbStackWalk(CordbThread * pCordbThread);
    virtual ~CordbStackWalk();
    virtual void Neuter();

    // helper function for Neuter
    virtual void DeleteAll();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbStackWalk"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugStackWalk
    //-----------------------------------------------------------

    COM_METHOD GetContext(ULONG32   contextFlags,
                          ULONG32   contextBufSize,
                          ULONG32 * pContextSize,
                          BYTE      pbContextBuf[]);
    COM_METHOD SetContext(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[]);
    COM_METHOD Next();
    COM_METHOD GetFrame(ICorDebugFrame **ppFrame);

    //-----------------------------------------------------------
    // Internal members
    //-----------------------------------------------------------

    void SetContextWorker(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[]);
    HRESULT GetFrameWorker(ICorDebugFrame **ppFrame);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    void Init();

private:
    // handle legacy V2 hijacking for unhandled hardware exceptions
    void CheckForLegacyHijackCase();

    // refresh the data for this instance of CordbStackWalk if we have had an IPC event followed by a
    // continue since we got the information.
    void RefreshIfNeeded();

    // unwind the frame and update m_context with the new context
    BOOL UnwindStackFrame();

    // the thread on which this CordbStackWalk is created
    CordbThread * m_pCordbThread;

    // This is the same iterator used by the runtime itself.
    IDacDbiInterface::StackWalkHandle m_pSFIHandle;

    // buffers used for stackwalking
    DT_CONTEXT m_context;

    //  Used to figure out if we have to refresh any reference objects
    //  on the left side.  We set it to CordbProcess::m_flushCounter on
    //  creation and will check it against that value when we call GetFrame or Next.
    //  If it doesn't match, an IPC event has occurred and the values will need to be
    //  refreshed via the DAC.
    UINT m_lastSyncFlushCounter;

    // cached flag used for refreshing a CordbStackWalk
    CorDebugSetContextFlag m_cachedSetContextFlag;

    // We unwind one frame ahead of time to get the FramePointer on x86.
    // These fields are used for the bookkeeping.
    RSSmartPtr<CordbFrame> m_pCachedFrame;
    HRESULT m_cachedHR;
    bool m_fIsOneFrameAhead;
};


class CordbContext : public CordbBase, public ICorDebugContext
{
public:

    CordbContext() : CordbBase(NULL, 0, enumCordbContext) {}



#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbContext"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugContext
    //-----------------------------------------------------------
private:

} ;


/* ------------------------------------------------------------------------- *
 * Frame class
 * ------------------------------------------------------------------------- */

class CordbFrame : public CordbBase, public ICorDebugFrame
{
protected:
    // Ctor to provide dummy frame that just wraps a frame-pointer
    CordbFrame(CordbProcess * pProcess, FramePointer fp);

public:
    CordbFrame(CordbThread *    pThread,
               FramePointer     fp,
               SIZE_T           ip,
               CordbAppDomain * pCurrentAppDomain);

    virtual ~CordbFrame();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbFrame"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFrame
    //-----------------------------------------------------------

    COM_METHOD GetChain(ICorDebugChain **ppChain);

    // Derived versions of Frame will implement GetCode.
    COM_METHOD GetCode(ICorDebugCode **ppCode) = 0;

    COM_METHOD GetFunction(ICorDebugFunction **ppFunction);
    COM_METHOD GetFunctionToken(mdMethodDef *pToken);

    COM_METHOD GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd);
    COM_METHOD GetCaller(ICorDebugFrame **ppFrame);
    COM_METHOD GetCallee(ICorDebugFrame **ppFrame);
    COM_METHOD CreateStepper(ICorDebugStepper **ppStepper);

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------

    CordbAppDomain *GetCurrentAppDomain()
    {
        return m_currentAppDomain;
    }

    // Internal helper to get a CordbFunction for this frame.
    virtual CordbFunction *GetFunction() = 0;

    FramePointer GetFramePointer()
    {
        return m_fp;
    }

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

    // Accessors to return NULL or typesafe cast to derived frame
    virtual CordbInternalFrame * GetAsInternalFrame()   { return NULL; }
    virtual CordbNativeFrame * GetAsNativeFrame()       { return NULL; }

    // determine if the frame pointer is in the stack range owned by the frame
    bool IsContainedInFrame(FramePointer fp);

    // This is basically a complicated cast function.  We are casting from an ICorDebugFrame to a CordbFrame.
    static CordbFrame* GetCordbFrameFromInterface(ICorDebugFrame *pFrame);

    virtual const DT_CONTEXT * GetContext() const { return NULL; }

public:
    // this represents the IL offset for a CordbJITILFrame, the native offset for a CordbNativeFrame,
    // and 0 for a CordbInternalFrame
    SIZE_T                  m_ip;

    CordbThread *           m_pThread;

    CordbAppDomain         *m_currentAppDomain;
    FramePointer            m_fp;

protected:
    // indicates whether this frame is the leaf frame; lazily initialized
    mutable Optional<bool>  m_optfIsLeafFrame;

private:
#ifdef _DEBUG
    // For tracking down neutering bugs;
    UINT                   m_DbgContinueCounter;
#endif
};

// Dummy frame that just wraps a frame pointer.
// This is used to pass a FramePointer back in the Exception2 callback.
// Currently, the callback passes back an ICorDebugFrame as a way of exposing a cross-platform
// frame pointer. However passing back an ICDFrame means we need to do a stackwalk, and
// that may not be possible in V3:
// - the stackwalk is very chatty, and may be too much work just to give an exception notification.
// - in 64-bit, we may not even be able to do the stackwalk ourselves.
//
// The shim can take the framePointer and do the stackwalk and resolve it to a real frame,
// so V2 emulation scenarios will continue to work.
// @dbgtodo  exception -  resolve this when we iron out exceptions in V3.
class CordbPlaceholderFrame : public CordbFrame
{
public:
    // Ctor to provide dummy frame that just wraps a frame-pointer
    CordbPlaceholderFrame(CordbProcess * pProcess, FramePointer fp)
        : CordbFrame(pProcess, fp)
    {
    }

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbFrame"; }
#endif

    // Provide dummy implementation for some methods. These should never be called.
    COM_METHOD GetCode(ICorDebugCode **ppCode)
    {
        _ASSERTE(!"Don't call this");
        return E_NOTIMPL;
    }
    virtual CordbFunction *GetFunction()
    {
        _ASSERTE(!"Don't call this");
        return NULL;
    }
};

class CordbInternalFrame : public CordbFrame, public ICorDebugInternalFrame, public ICorDebugInternalFrame2
{
public:
    CordbInternalFrame(CordbThread *          pThread,
                       FramePointer           fp,
                       CordbAppDomain *       pCurrentAppDomain,
                       const DebuggerIPCE_STRData * pData);

    CordbInternalFrame(CordbThread *             pThread,
                       FramePointer              fp,
                       CordbAppDomain *       pCurrentAppDomain,
                       CorDebugInternalFrameType frameType,
                       mdMethodDef               funcMetadataToken,
                       CordbFunction *           pFunction,
                       VMPTR_MethodDesc          vmMethodDesc);

    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbInternalFrame"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFrame
    //-----------------------------------------------------------

    COM_METHOD GetChain(ICorDebugChain **ppChain)
    {
        return (CordbFrame::GetChain(ppChain));
    }

    // We don't expose a code-object for stubs.
    COM_METHOD GetCode(ICorDebugCode **ppCode)
    {
        return CORDBG_E_CODE_NOT_AVAILABLE;
    }

    COM_METHOD GetFunction(ICorDebugFunction **ppFunction)
    {
        return (CordbFrame::GetFunction(ppFunction));
    }
    COM_METHOD GetFunctionToken(mdMethodDef *pToken)
    {
        return (CordbFrame::GetFunctionToken(pToken));
    }

    COM_METHOD GetCaller(ICorDebugFrame **ppFrame)
    {
        return (CordbFrame::GetCaller(ppFrame));
    }
    COM_METHOD GetCallee(ICorDebugFrame **ppFrame)
    {
        return (CordbFrame::GetCallee(ppFrame));
    }
    COM_METHOD CreateStepper(ICorDebugStepper **ppStepper)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd);

    //-----------------------------------------------------------
    // ICorDebugInternalFrame
    //-----------------------------------------------------------

    // Get the type of internal frame. This will never be STUBFRAME_NONE.
    COM_METHOD GetFrameType(CorDebugInternalFrameType * pType)
    {
        VALIDATE_POINTER_TO_OBJECT(pType, CorDebugInternalFrameType)
        *pType = m_eFrameType;
        return S_OK;
    }

    //-----------------------------------------------------------
    // ICorDebugInternalFrame2
    //-----------------------------------------------------------

    COM_METHOD GetAddress(CORDB_ADDRESS * pAddress);
    COM_METHOD IsCloserToLeaf(ICorDebugFrame * pFrameToCompare,
                              BOOL *           pIsCloser);

    BOOL IsCloserToLeafWorker(ICorDebugFrame * pFrameToCompare);

    //-----------------------------------------------------------
    // Non COM methods
    //-----------------------------------------------------------

    virtual CordbFunction *GetFunction();


    // Accessors to return NULL or typesafe cast to derived frame
    virtual CordbInternalFrame * GetAsInternalFrame()   { return this; }

    // accessor for the shim private hook code:CordbThread::ConvertFrameForILMethodWithoutMetadata
    BOOL ConvertInternalFrameForILMethodWithoutMetadata(ICorDebugInternalFrame2 ** ppInternalFrame2);

protected:
    // the frame type
    CorDebugInternalFrameType m_eFrameType;

    // the method token of the method (if any) associated with the internal frame
    mdMethodDef m_funcMetadataToken;

    // the method (if any) associated with the internal frame
    RSSmartPtr<CordbFunction> m_function;

    VMPTR_MethodDesc          m_vmMethodDesc;
};

//---------------------------------------------------------------------------------------
//
// This class implements ICorDebugRuntimeUnwindableFrame.  It is used to mark a native stack frame
// which requires special unwinding and which doesn't correspond to any IL code.  It is really
// just a marker to tell the debugger to use the managed unwinder.  The debugger is still responsible
// to do all the inspection and symbol lookup.  An example is the hijack stub.
//

class CordbRuntimeUnwindableFrame : public CordbFrame, public ICorDebugRuntimeUnwindableFrame
{
public:
    CordbRuntimeUnwindableFrame(CordbThread *    pThread,
                                FramePointer     fp,
                                CordbAppDomain * pCurrentAppDomain,
                                DT_CONTEXT *     pContext);

    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbRuntimeUnwindableFrame"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }

    COM_METHOD QueryInterface(REFIID riid, void ** ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFrame
    //-----------------------------------------------------------

    //
    // Just return E_NOTIMPL for everything.
    // See the class comment.
    //

    COM_METHOD GetChain(ICorDebugChain ** ppChain)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetCode(ICorDebugCode ** ppCode)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetFunction(ICorDebugFunction ** ppFunction)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetFunctionToken(mdMethodDef * pToken)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetCaller(ICorDebugFrame ** ppFrame)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetCallee(ICorDebugFrame ** ppFrame)
    {
        return E_NOTIMPL;
    }

    COM_METHOD CreateStepper(ICorDebugStepper ** ppStepper)
    {
        return E_NOTIMPL;
    }

    COM_METHOD GetStackRange(CORDB_ADDRESS * pStart, CORDB_ADDRESS * pEnd)
    {
        return E_NOTIMPL;
    }

    //-----------------------------------------------------------
    // Non COM methods
    //-----------------------------------------------------------

    virtual CordbFunction * GetFunction()
    {
        return NULL;
    }

    virtual const DT_CONTEXT * GetContext() const;

private:
    DT_CONTEXT m_context;
};


class CordbValueEnum : public CordbBase, public ICorDebugValueEnum
{
public:
    enum ValueEnumMode {
        LOCAL_VARS_ORIGINAL_IL,
        LOCAL_VARS_REJIT_IL,
        ARGS,
    } ;

    CordbValueEnum(CordbNativeFrame *frame, ValueEnumMode mode);
    HRESULT Init();
    ~CordbValueEnum();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbValueEnum"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugEnum
    //-----------------------------------------------------------

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    //-----------------------------------------------------------
    // ICorDebugValueEnum
    //-----------------------------------------------------------

    COM_METHOD Next(ULONG celt, ICorDebugValue *values[], ULONG *pceltFetched);

private:
    CordbNativeFrame*     m_frame;
    ValueEnumMode   m_mode;
    UINT            m_iCurrent;
    UINT            m_iMax;
};


/* ------------------------------------------------------------------------- *
 * Misc Info for the Native Frame class
 * ------------------------------------------------------------------------- */

struct CordbMiscFrame
{
public:
    CordbMiscFrame();

    // new-style constructor
    CordbMiscFrame(DebuggerIPCE_JITFuncData * pJITFuncData);

#ifdef FEATURE_EH_FUNCLETS
    SIZE_T             parentIP;
    FramePointer       fpParentOrSelf;
    bool               fIsFilterFunclet;
#endif // FEATURE_EH_FUNCLETS
};


/* ------------------------------------------------------------------------- *
 * Native Frame class
 * ------------------------------------------------------------------------- */

class CordbNativeFrame : public CordbFrame, public ICorDebugNativeFrame, public ICorDebugNativeFrame2
{
public:
    CordbNativeFrame(CordbThread *        pThread,
                     FramePointer         fp,
                     CordbNativeCode *    pNativeCode,
                     SIZE_T               ip,
                     DebuggerREGDISPLAY * pDRD,
                     TADDR                addrAmbientESP,
                     bool                 fQuicklyUnwound,
                     CordbAppDomain *     pCurrentAppDomain,
                     CordbMiscFrame *     pMisc = NULL,
                     DT_CONTEXT *         pContext = NULL);
    virtual ~CordbNativeFrame();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbNativeFrame"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFrame
    //-----------------------------------------------------------

    COM_METHOD GetChain(ICorDebugChain **ppChain)
    {
        return (CordbFrame::GetChain(ppChain));
    }
    COM_METHOD GetCode(ICorDebugCode **ppCode);
    COM_METHOD GetFunction(ICorDebugFunction **ppFunction)
    {
        return (CordbFrame::GetFunction(ppFunction));
    }
    COM_METHOD GetFunctionToken(mdMethodDef *pToken)
    {
        return (CordbFrame::GetFunctionToken(pToken));
    }
    COM_METHOD GetCaller(ICorDebugFrame **ppFrame)
    {
        return (CordbFrame::GetCaller(ppFrame));
    }
    COM_METHOD GetCallee(ICorDebugFrame **ppFrame)
    {
        return (CordbFrame::GetCallee(ppFrame));
    }
    COM_METHOD CreateStepper(ICorDebugStepper **ppStepper)
    {
        return (CordbFrame::CreateStepper(ppStepper));
    }

    COM_METHOD GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd);

    //-----------------------------------------------------------
    // ICorDebugNativeFrame
    //-----------------------------------------------------------

    COM_METHOD GetIP(ULONG32* pnOffset);
    COM_METHOD SetIP(ULONG32 nOffset);
    COM_METHOD GetRegisterSet(ICorDebugRegisterSet **ppRegisters);
    COM_METHOD GetLocalRegisterValue(CorDebugRegister reg,
                                     ULONG cbSigBlob,
                                     PCCOR_SIGNATURE pvSigBlob,
                                     ICorDebugValue ** ppValue);

    COM_METHOD GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                           CorDebugRegister lowWordReg,
                                           ULONG cbSigBlob,
                                           PCCOR_SIGNATURE pvSigBlob,
                                           ICorDebugValue ** ppValue);

    COM_METHOD GetLocalMemoryValue(CORDB_ADDRESS address,
                                   ULONG cbSigBlob,
                                   PCCOR_SIGNATURE pvSigBlob,
                                   ICorDebugValue ** ppValue);

    COM_METHOD GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                           CORDB_ADDRESS lowWordAddress,
                                           ULONG cbSigBlob,
                                           PCCOR_SIGNATURE pvSigBlob,
                                           ICorDebugValue ** ppValue);

    COM_METHOD GetLocalMemoryRegisterValue(CORDB_ADDRESS highWordAddress,
                                           CorDebugRegister lowWordRegister,
                                           ULONG cbSigBlob,
                                           PCCOR_SIGNATURE pvSigBlob,
                                           ICorDebugValue ** ppValue);

    COM_METHOD CanSetIP(ULONG32 nOffset);

    //-----------------------------------------------------------
    // ICorDebugNativeFrame2
    //-----------------------------------------------------------

    COM_METHOD IsChild(BOOL * pIsChild);

    COM_METHOD IsMatchingParentFrame(ICorDebugNativeFrame2 *pPotentialParentFrame,
                                     BOOL * pIsParent);

    COM_METHOD GetStackParameterSize(ULONG32 * pSize);

    //-----------------------------------------------------------
    // Non-COM members
    //-----------------------------------------------------------

    // Accessors to return NULL or typesafe cast to derived frame
    virtual CordbNativeFrame * GetAsNativeFrame()       { return this; }

    CordbFunction * GetFunction();
    CordbNativeCode * GetNativeCode();
    virtual const DT_CONTEXT * GetContext() const;

    // Given the native variable information of a variable, return its value.
    // This function assumes that the value is either in a register or on the stack
    // (i.e. VLT_REG or VLT_STK).
    SIZE_T  GetRegisterOrStackValue(const ICorDebugInfo::NativeVarInfo * pNativeVarInfo);

    HRESULT GetLocalRegisterValue(CorDebugRegister reg,
                                     CordbType * pType,
                                     ICorDebugValue **ppValue);
    HRESULT GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                           CorDebugRegister lowWordReg,
                                           CordbType * pType,
                                           ICorDebugValue **ppValue);
    HRESULT GetLocalMemoryValue(CORDB_ADDRESS address,
                                   CordbType * pType,
                                   ICorDebugValue **ppValue);
    HRESULT GetLocalByRefMemoryValue(CORDB_ADDRESS address,
                                        CordbType * pType,
                                        ICorDebugValue **ppValue);
    HRESULT GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                           CORDB_ADDRESS lowWordAddress,
                                           CordbType * pType,
                                           ICorDebugValue **ppValue);
    HRESULT GetLocalMemoryRegisterValue(CORDB_ADDRESS highWordAddress,
                                           CorDebugRegister lowWordRegister,
                                           CordbType * pType,
                                           ICorDebugValue **ppValue);
    UINT_PTR * GetAddressOfRegister(CorDebugRegister regNum) const;
    CORDB_ADDRESS GetLeftSideAddressOfRegister(CorDebugRegister regNum) const;
    HRESULT GetLocalFloatingPointValue(DWORD index,
                                            CordbType * pType,
                                            ICorDebugValue **ppValue);


    CORDB_ADDRESS GetLSStackAddress(ICorDebugInfo::RegNum regNum, signed offset);

    bool IsLeafFrame() const;

    // Return the offset used for inspection purposes.
    // Refer to the comment at the beginning of the function definition in RsThread.cpp for more information.
    SIZE_T GetInspectionIP();

    ULONG32 GetIPOffset();

    // whether this is a funclet frame
    bool      IsFunclet();
    bool      IsFilterFunclet();

#ifdef FEATURE_EH_FUNCLETS
    // return the offset of the parent method frame at which an exception occurs
    SIZE_T    GetParentIP();
#endif // FEATURE_EH_FUNCLETS

    TADDR GetAmbientESP() { return m_taAmbientESP; }
    TADDR GetReturnRegisterValue();

    // accessor for the shim private hook code:CordbThread::ConvertFrameForILMethodWithoutMetadata
    BOOL ConvertNativeFrameForILMethodWithoutMetadata(ICorDebugInternalFrame2 ** ppInternalFrame2);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    // the register set
    DebuggerREGDISPLAY m_rd;

    // This field is only true for Enter-Managed chain.  It means that the register set is invalid.
    bool               m_quicklyUnwound;

    // each CordbNativeFrame corresponds to exactly one CordbJITILFrame and one CordbNativeCode
    RSSmartPtr<CordbJITILFrame> m_JITILFrame;
    RSSmartPtr<CordbNativeCode> m_nativeCode;

    // auxiliary information only used on 64-bit to find the parent stack pointer and offset for funclets
    CordbMiscFrame     m_misc;

private:
    // the ambient SP value only used on x86 to retrieve sp-relative local variables
    // (most likely in a frameless method)
    TADDR    m_taAmbientESP;

    // @dbgtodo  inspection - When we DACize the various Cordb*Value classes, we should consider getting rid of the
    // DebuggerREGDISPLAY and just use the CONTEXT.  A lot of simplification can be done here.
    DT_CONTEXT  m_context;
};


/* ------------------------------------------------------------------------- *
 * CordbRegisterSet class
 *
 * This can be obtained via GetRegisterSet from
 *      CordbNativeFrame
 *      CordbThread
 *
 * ------------------------------------------------------------------------- */

#define SETBITULONG64( x ) ( (ULONG64)1 << (x) )
#define SET_BIT_MASK(_mask, _reg)      (_mask[(_reg) >> 3] |=  (1 << ((_reg) & 7)))
#define RESET_BIT_MASK(_mask, _reg)    (_mask[(_reg) >> 3] &= ~(1 << ((_reg) & 7)))
#define IS_SET_BIT_MASK(_mask, _reg)   (_mask[(_reg) >> 3] &   (1 << ((_reg) & 7)))


class CordbRegisterSet : public CordbBase, public ICorDebugRegisterSet, public ICorDebugRegisterSet2
{
public:
    CordbRegisterSet(DebuggerREGDISPLAY * pRegDisplay,
                     CordbThread *        pThread,
                     bool fActive,
                     bool fQuickUnwind,
                     bool fTakeOwnershipOfDRD = false);


    ~CordbRegisterSet();



    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbRegisterSet"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRefEnforceExternal());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseReleaseEnforceExternal());
    }

    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);



    //-----------------------------------------------------------
    // ICorDebugRegisterSet
    // More extensive explanation are in Src/inc/CorDebug.idl
    //-----------------------------------------------------------
    COM_METHOD GetRegistersAvailable(ULONG64 *pAvailable);

    COM_METHOD GetRegisters(ULONG64 mask,
                            ULONG32 regCount,
                            CORDB_REGISTER regBuffer[]);
    COM_METHOD SetRegisters( ULONG64 mask,
                             ULONG32 regCount,
                             CORDB_REGISTER regBuffer[])
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER,
                                         regCount, true, true);

        return E_NOTIMPL;
    }

    COM_METHOD GetThreadContext(ULONG32 contextSize, BYTE context[]);

    // SetThreadContexthad a very problematic implementation in v1.1.
    // We've ripped it out in V2.0 and E_NOTIMPL it. See V1.1 sources for what it used to look like
    // in case we ever want to re-add it.
    // If we ever re-implement it consider the following:
    // - must fail on non-leaf frames (just check m_active).
    // - must make sure that GetThreadContext() is fully accurate. If we don't have SetThCtx, then
    //   GetThreadCtx bugs are much more benign.
    // - be sure to update any shared reg displays (what if a frame + chain have the same rd) and
    //   also update any cached contexts (such as CordbThread::m_context).
    // - be sure to honor the context flags and only setting what we can set.
    //
    // Friday, July 16, 2004. (This date will be useful for Source control history)
    COM_METHOD SetThreadContext(ULONG32 contextSize, BYTE context[])
    {
        return E_NOTIMPL;
    }

    //-----------------------------------------------------------
    // ICorDebugRegisterSet2
    // More extensive explanation are in Src/inc/CorDebug.idl
    //-----------------------------------------------------------
    COM_METHOD GetRegistersAvailable(ULONG32 regCount,
                                     BYTE    pAvailable[]);

    COM_METHOD GetRegisters(ULONG32 maskCount,
                            BYTE    mask[],
                            ULONG32 regCount,
                            CORDB_REGISTER regBuffer[]);

    COM_METHOD SetRegisters(ULONG32 maskCount,
                            BYTE    mask[],
                            ULONG32 regCount,
                            CORDB_REGISTER regBuffer[])
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER,
                                         regCount, true, true);

        return E_NOTIMPL;
    }

protected:
    // Platform specific helper for GetThreadContext.
    void InternalCopyRDToContext(DT_CONTEXT * pContext);

    // Adapters to impl v2.0 interfaces on top of v1.0 interfaces.
    HRESULT GetRegistersAvailableAdapter(ULONG32 regCount, BYTE pAvailable[]);
    HRESULT GetRegistersAdapter(ULONG32 maskCount, BYTE mask[], ULONG32 regCount, CORDB_REGISTER regBuffer[]);


    // This CordbRegisterSet is responsible to free this memory if m_fTakeOwnershipOfDRD is true.  Otherwise,
    // this memory is freed by the CordbNativeFrame or CordbThread which creates this CordbRegisterSet.
    DebuggerREGDISPLAY  *m_rd;
    CordbThread         *m_thread;
    bool                m_active; // true if we're the leafmost register set.
    bool                m_quickUnwind;

    // true if the CordbRegisterSet owns the DebuggerREGDISPLAY pointer and needs to free the memory
    bool                m_fTakeOwnershipOfDRD;
} ;




/* ------------------------------------------------------------------------- *
 * JIT-IL Frame class
 * ------------------------------------------------------------------------- */

class CordbJITILFrame : public CordbBase, public ICorDebugILFrame, public ICorDebugILFrame2, public ICorDebugILFrame3, public ICorDebugILFrame4
{
public:
    CordbJITILFrame(CordbNativeFrame *    pNativeFrame,
                    CordbILCode *         pCode,
                    UINT_PTR              ip,
                    CorDebugMappingResult mapping,
                    GENERICS_TYPE_TOKEN   exactGenericArgsToken,
                    DWORD                 dwExactGenericArgsTokenIndex,
                    bool                  fVarArgFnx,
                    CordbReJitILCode *    pReJitCode);
    HRESULT Init();
    virtual ~CordbJITILFrame();
    virtual void Neuter();


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbJITILFrame"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugFrame
    //-----------------------------------------------------------

    COM_METHOD GetChain(ICorDebugChain **ppChain);
    COM_METHOD GetCode(ICorDebugCode **ppCode);
    COM_METHOD GetFunction(ICorDebugFunction **ppFunction);
    COM_METHOD GetFunctionToken(mdMethodDef *pToken);
    COM_METHOD GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd);
    COM_METHOD CreateStepper(ICorDebugStepper **ppStepper);
    COM_METHOD GetCaller(ICorDebugFrame **ppFrame);
    COM_METHOD GetCallee(ICorDebugFrame **ppFrame);

    //-----------------------------------------------------------
    // ICorDebugILFrame
    //-----------------------------------------------------------

    COM_METHOD GetIP(ULONG32* pnOffset, CorDebugMappingResult *pMappingResult);
    COM_METHOD SetIP(ULONG32 nOffset);
    COM_METHOD EnumerateLocalVariables(ICorDebugValueEnum **ppValueEnum);
    COM_METHOD GetLocalVariable(DWORD dwIndex, ICorDebugValue **ppValue);
    COM_METHOD EnumerateArguments(ICorDebugValueEnum **ppValueEnum);
    COM_METHOD GetArgument(DWORD dwIndex, ICorDebugValue ** ppValue);
    COM_METHOD GetStackDepth(ULONG32 *pDepth);
    COM_METHOD GetStackValue(DWORD dwIndex, ICorDebugValue **ppValue);
    COM_METHOD CanSetIP(ULONG32 nOffset);

    //-----------------------------------------------------------
    // ICorDebugILFrame2
    //-----------------------------------------------------------

    // Called at an EnC remap opportunity to remap to the latest version of a function
    COM_METHOD RemapFunction(ULONG32 nOffset);

    COM_METHOD EnumerateTypeParameters(ICorDebugTypeEnum **ppTyParEnum);

    //-----------------------------------------------------------
    // ICorDebugILFrame3
    //-----------------------------------------------------------

    COM_METHOD GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue** ppReturnValue);

    //-----------------------------------------------------------
    // ICorDebugILFrame4
    //-----------------------------------------------------------

    COM_METHOD EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum **ppValueEnum);
    COM_METHOD GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex, ICorDebugValue **ppValue);
    COM_METHOD GetCodeEx(ILCodeKind flags, ICorDebugCode **ppCode);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    CordbModule *GetModule();

    HRESULT GetNativeVariable(CordbType *type,
                              const ICorDebugInfo::NativeVarInfo *pNativeVarInfo,
                              ICorDebugValue **ppValue);

    CordbAppDomain *GetCurrentAppDomain();

    CordbFunction *GetFunction();

    // ILVariableToNative serves to let the frame intercept accesses
    // to var args variables.
    HRESULT ILVariableToNative(DWORD dwIndex,
                               const ICorDebugInfo::NativeVarInfo ** ppNativeInfo);

    // Fills in our array of var args variables
    HRESULT FabricateNativeInfo(DWORD dwIndex,
                                const ICorDebugInfo::NativeVarInfo ** ppNativeInfo);

    HRESULT GetArgumentType(DWORD dwIndex,
                            CordbType ** ppResultType);

    // load the generics type and method arguments into a cache
    void LoadGenericArgs();

    HRESULT QueryInterfaceInternal(REFIID id, void** pInterface);

    // Builds an generic Instaniation object from the mdClass and generic signature
    // for what we are calling into.
    static HRESULT BuildInstantiationForCallsite(CordbModule *pModule, NewArrayHolder<CordbType*> &types, Instantiation &inst, Instantiation *currentInstantiation, mdToken targetClass, SigParser funcGenerics);

    CordbILCode* GetOriginalILCode();
    CordbReJitILCode* GetReJitILCode();

private:
    void    RefreshCachedVarArgSigParserIfNeeded();

    // Worker function for GetReturnValueForILOffset.
    HRESULT GetReturnValueForILOffsetImpl(ULONG32 ILoffset, ICorDebugValue** ppReturnValue);

    // Given pType, fills ppReturnValue with the correct value.
    HRESULT GetReturnValueForType(CordbType *pType, ICorDebugValue **ppReturnValue);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    // each CordbJITILFrame corresponds to exactly one CordbNativeFrame and one CordbILCode
    CordbNativeFrame * m_nativeFrame;
    CordbILCode *      m_ilCode;

    // the IL offset and the mapping result for the offset
    UINT_PTR          m_ip;
    CorDebugMappingResult m_mapping;

    // <vararg-specific fields>

    // whether this is a vararg function
    bool              m_fVarArgFnx;

    // the number of arguments, including the var args
    uint32_t          m_allArgsCount;

    // This byte array is used to store the signature for vararg methods.
    // It points to the underlying memory used by m_sigParserCached, and it enables us to easily delete
    // the underlying memory when the CordbJITILFrame is neutered.
    BYTE *            m_rgbSigParserBuf;

    // Do not mutate this, instead make copies of it and use the copies, that way we are guaranteed to
    // start at the correct position in the signature each time.
    // The underlying memory used for the signature in the SigParser must not be in the DAC cache.
    // Otherwise it may be flushed underneath us, and we would AV when we try to access it.
    SigParser         m_sigParserCached;

    // the address of the first arg; only used for vararg functions
    CORDB_ADDRESS     m_FirstArgAddr;

    // This is an array of variable information for the arguments.
    // The variable information is fabricated by the RS.
    ICorDebugInfo::NativeVarInfo * m_rgNVI;

    // </vararg-specific fields>

    Instantiation     m_genericArgs;        // the generics type arguments
    BOOL              m_genericArgsLoaded;  // whether we have loaded and cached the generics type arguments

    // An extra token to help fetch information about any generic
    // parameters passed to the method, perhaps dynamically.
    // This is the so-called generics type context/token.
    //
    // This token comes from the stackwalker and it may be NULL, in which case we need to retrieve the token
    // ourselves using m_dwFrameParamsTokenIndex and the variable lifetime information.
    GENERICS_TYPE_TOKEN m_frameParamsToken;

    // IL Variable index of the Generics Arg Token.
    DWORD               m_dwFrameParamsTokenIndex;

    // if this frame is instrumented with rejit, this will point to the instrumented IL code
    RSSmartPtr<CordbReJitILCode> m_pReJitCode;
};

/* ------------------------------------------------------------------------- *
 * Breakpoint class
 * ------------------------------------------------------------------------- */

enum CordbBreakpointType
{
    CBT_FUNCTION,
    CBT_MODULE,
    CBT_VALUE
};

class CordbBreakpoint : public CordbBase, public ICorDebugBreakpoint
{
public:
    CordbBreakpoint(CordbProcess * pProcess, CordbBreakpointType bpType);
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbBreakpoint"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugBreakpoint
    //-----------------------------------------------------------

    COM_METHOD BaseIsActive(BOOL *pbActive);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------
    CordbBreakpointType GetBPType()
    {
        return m_type;
    }

    virtual void Disconnect() {}

    CordbAppDomain *GetAppDomain()
    {
        return m_pAppDomain;
    }
    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    bool                m_active;
    CordbAppDomain *m_pAppDomain;
    CordbBreakpointType m_type;
};

/* ------------------------------------------------------------------------- *
 * Function Breakpoint class
 * ------------------------------------------------------------------------- */

class CordbFunctionBreakpoint : public CordbBreakpoint,
                                public ICorDebugFunctionBreakpoint
{
public:
    CordbFunctionBreakpoint(CordbCode *code, SIZE_T offset, BOOL offsetIsIl);
    ~CordbFunctionBreakpoint();

    virtual void Neuter();
#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbFunctionBreakpoint"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugBreakpoint
    //-----------------------------------------------------------

    COM_METHOD GetFunction(ICorDebugFunction **ppFunction);
    COM_METHOD GetOffset(ULONG32 *pnOffset);
    COM_METHOD Activate(BOOL bActive);
    COM_METHOD IsActive(BOOL *pbActive)
    {
        VALIDATE_POINTER_TO_OBJECT(pbActive, BOOL *);

        return BaseIsActive(pbActive);
    }

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    void Disconnect();

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------


    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

    // Get a point to the LS BP object.
    LSPTR_BREAKPOINT GetLsPtrBP();
public:

    // We need to have a strong pointer because we may access the m_code object after we're neutered.
    // @todo - use external pointer b/c Breakpoints aren't yet rooted, and so this reference could be
    // leaked.
    RSExtSmartPtr<CordbCode> m_code;
    SIZE_T          m_offset;
    BOOL            m_offsetIsIl;
};

/* ------------------------------------------------------------------------- *
 * Module Breakpoint class
 * ------------------------------------------------------------------------- */

class CordbModuleBreakpoint : public CordbBreakpoint,
                              public ICorDebugModuleBreakpoint
{
public:
    CordbModuleBreakpoint(CordbModule *pModule);



#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbModuleBreakpoint"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugModuleBreakpoint
    //-----------------------------------------------------------

    COM_METHOD GetModule(ICorDebugModule **ppModule);
    COM_METHOD Activate(BOOL bActive);
    COM_METHOD IsActive(BOOL *pbActive)
    {
        VALIDATE_POINTER_TO_OBJECT(pbActive, BOOL *);

        return BaseIsActive(pbActive);
    }

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    void Disconnect();

public:
    CordbModule       *m_module;
};


/* ------------------------------------------------------------------------- *
 * Stepper class
 * ------------------------------------------------------------------------- */

class CordbStepper : public CordbBase, public ICorDebugStepper, public ICorDebugStepper2
{
public:
    CordbStepper(CordbThread *thread, CordbFrame *frame = NULL);



#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbStepper"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugStepper
    //-----------------------------------------------------------

    COM_METHOD IsActive(BOOL *pbActive);
    COM_METHOD Deactivate();
    COM_METHOD SetInterceptMask(CorDebugIntercept mask);
    COM_METHOD SetUnmappedStopMask(CorDebugUnmappedStop mask);
    COM_METHOD Step(BOOL bStepIn);
    COM_METHOD StepRange(BOOL bStepIn,
                         COR_DEBUG_STEP_RANGE ranges[],
                         ULONG32 cRangeCount);
    COM_METHOD StepOut();
    COM_METHOD SetRangeIL(BOOL bIL);

    //-----------------------------------------------------------
    // ICorDebugStepper2
    //-----------------------------------------------------------
    COM_METHOD SetJMC(BOOL fIsJMCStepper);

    //-----------------------------------------------------------
    // Convenience routines
    //-----------------------------------------------------------

    CordbAppDomain *GetAppDomain()
    {
        return (m_thread->GetAppDomain());
    }

    LSPTR_STEPPER GetLsPtrStepper();

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

    CordbThread     *m_thread;
    CordbFrame      *m_frame;
    REMOTE_PTR      m_stepperToken;
    bool            m_active;
    bool            m_rangeIL;
    bool            m_fIsJMCStepper;
    CorDebugUnmappedStop m_rgfMappingStop;
    CorDebugIntercept m_rgfInterceptStop;
};

#define REG_SIZE sizeof(SIZE_T)

// class RegisterInfo: encapsulates information necessary to identify and access a specific register in a
// register display
class RegisterInfo
{
public:
    // constructor for an instance of RegisterInfo
    // Arguments:
    //     input:  kNumber - value from CorDebugRegister to identify the register
    //             addr    - address in remote register display that holds the value
    //     output: no out parameters, but this instance of RegisterInfo has been initialized
    RegisterInfo(const CorDebugRegister kNumber, CORDB_ADDRESS addr, SIZE_T value):
        m_kRegNumber((CorDebugRegister)kNumber),
        m_regAddr(addr),
        m_regValue(value)
    {};


    // copy constructor
    // Arguments:
    //     input:  regInfo - register info from which the values for this instance will come
    //     output: no out parameters, but this instance of RegisterInfo has been initialized
    RegisterInfo(const RegisterInfo * pRegInfo):
        m_kRegNumber(pRegInfo->m_kRegNumber),
        m_regAddr(pRegInfo->m_regAddr),
        m_regValue(pRegInfo->m_regValue)
    {};


    //-------------------------------------
    // data members
    //-------------------------------------

    // enumeration value to identify the register, e.g., REGISTER_X86_EAX, or REGISTER_AMD64_XMM0
    CorDebugRegister  m_kRegNumber;

    // address in a context or frame register display of the register value
    CORDB_ADDRESS     m_regAddr;

    // the actual value of the register
    SIZE_T            m_regValue;
}; // class RegisterInfo

// class EnregisteredValueHome: abstract class to encapsulate basic information for a register value, and
// serve as a base class for values residing in register-based locations, such as a single register, a
// register pair, or a register and memory location.
class EnregisteredValueHome
{
public:

    // constructor to initialize an instance of EnregisteredValueHome
    EnregisteredValueHome(const CordbNativeFrame * pFrame);

    virtual ~EnregisteredValueHome() {}

    // virtual "copy constructor" to make a copy of "this" to be owned by a different instance of
    // Cordb*Value. If an instance of CordbVCObjectValue represents an enregistered value class, it means
    // there is a single field. This implies that the register for the CordbVCObject instance is the same as
    // the register for its field. When we create a Cordb*Value to represent this field, we need to make a
    // copy of the EnregisteredValueHome belonging to the CordbVCObject instance to become the
    // EnregisteredValueHome of the Cord*Value representing the field.
    // returns:
    //   a new cloned copy of this object, allocated on the heap.
    //   Caller is responsible for deleting the  memory (using the standard delete operator).
    // note:
    //    C++ allows derived implementations to differ on return type, thus allowing
    //    derived impls to return the cloned copy as its actual derived type, and not just as a base type.


    virtual
    EnregisteredValueHome * Clone() const = 0;

    // set a remote enregistered location to a new value
    // Arguments:
    //     input:  pNewValue - buffer containing the new value along with its size
    //             pContext  - context from which the value comes
    //             fIsSigned - indicates whether the value is signed or not. The value provided may be smaller than
    //                         a register, in which case we'll need to extend it to a full register width. To do this
    //                         correctly, we need to know whether to sign extend or zero extend. Currently, only
    //                         the RegValueHome virtual function uses this, but we may need it if we introduce
    //                         types that don't completely occupy the size of two registers.
    //     output: updates the remote enregistered value on success
    // Note: Throws E_FAIL for invalid input or various HRESULTs from an
    //                         unsuccessful call to WriteProcessMemory
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned) = 0;

    // Gets an enregistered value and returns it to the caller
    // Arguments:
    //     input:  pValueOutBuffer - buffer in which to return the value, along with its size
    //     output: pValueOutBuffer - filled with the value
    // Note: Throws E_NOTIMPL for attempts to get an enregistered value for a float register
    // (implementation for derived class FloatRegValueHome)
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer) = 0;

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    // Arguments: input:  none--uses fields of "this"
    //            output: pRegAddr - address of an instance of RemoteAddress with field values set to corresponding
    //            field values of "this"
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr) = 0;

    // accessor
    const CordbNativeFrame * GetFrame() const { return m_pFrame; };

    //-------------------------------------
    // data members
    //-------------------------------------
protected:
    // The frame on which the value resides
    const CordbNativeFrame * m_pFrame;

}; // class EnregisteredValueHome

typedef NewHolder<EnregisteredValueHome> EnregisteredValueHomeHolder;

// class RegValueHome: encapsulates basic information for a value that resides in a single register
// and serves as a base class for values residing in a register pair.
class RegValueHome: public EnregisteredValueHome
{
public:

    // initializing constructor
    // Arguments:
    //     input:  pFrame  - frame to which the value belongs
    //             regNum  - enumeration value corresponding to the particular hardware register in
    //                       which the value resides
    //             regAddr - remote address within a register display (in a context or frame) of the
    //                       register value
    //     output: no out parameters, but the instance has been initialized
    RegValueHome(const CordbNativeFrame *  pFrame,
                 CorDebugRegister          regNum):
        EnregisteredValueHome(pFrame),
        m_reg1Info(regNum,
                   pFrame->GetLeftSideAddressOfRegister(regNum),
                   *(pFrame->GetAddressOfRegister(regNum)))
    {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    RegValueHome(const RegValueHome * pRemoteRegAddr):
        EnregisteredValueHome(pRemoteRegAddr->m_pFrame),
        m_reg1Info(pRemoteRegAddr->m_reg1Info)
    {};

    // make a copy of this instance of RegValueHome
    virtual
    RegValueHome * Clone() const { return new RegValueHome(*this); };

    // updates a register in a given context, and in the regdisplay of a given frame.
    void SetContextRegister(DT_CONTEXT *     pContext,
                            CorDebugRegister regNum,
                            SIZE_T           newVal);

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned);

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer);
    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of RegValueHome
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

    //-------------------------------------
    // data members
    //-------------------------------------
protected:
    // The information for the register in which the value resides.
    const RegisterInfo               m_reg1Info;
}; // class RegValueHome

// class RegRegValueHome
// derived class to add a second register for values that live in a pair of registers
class RegRegValueHome: public RegValueHome
{
public:
    // initializing constructor
    // Arguments:
    //     input:  pFrame   - frame to which the value belongs
    //             reg1Num  - enumeration value corresponding to the first particular hardware register in
    //                        which the value resides
    //             reg1Addr - remote address within a register display (in a context or frame) of the
    //                        first register
    //             reg2Num  - enumeration value corresponding to the second particular hardware register in
    //                        which the value resides
    //             reg2Addr - remote address within a register display (in a context or frame) of the
    //                        second register
    //     output: no out parameters, but the instance has been initialized
    RegRegValueHome(const CordbNativeFrame * pFrame,
                    CorDebugRegister         reg1Num,
                    CorDebugRegister         reg2Num):
        RegValueHome(pFrame, reg1Num),
        m_reg2Info(reg2Num,
                   pFrame->GetLeftSideAddressOfRegister(reg2Num),
                   *(pFrame->GetAddressOfRegister(reg2Num)))
    {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    RegRegValueHome(const RegRegValueHome * pRemoteRegAddr):
        RegValueHome(pRemoteRegAddr),
        m_reg2Info(pRemoteRegAddr->m_reg2Info)
    {};

    // make a copy of this instance of RegRegValueHome
    virtual
    RegRegValueHome * Clone() const { return new RegRegValueHome(*this); };

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned);

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer);

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    void CopyToIPCEType(RemoteAddress * pRegAddr);

    //-------------------------------------
    // data members
    //-------------------------------------

protected:
    // The information for the second of two registers in which the value resides.
    const RegisterInfo               m_reg2Info;
}; // class RegRegValueHome

// class RegAndMemBaseValueHome
// derived from RegValueHome, this class is also a base class for RegMemValueHome
// and MemRegValueHome, which add a memory location for reg-mem or mem-reg values
class RegAndMemBaseValueHome: public RegValueHome
{
public:
    // initializing constructor
    // Arguments:
    //     input:  pFrame   - frame to which the value belongs
    //             reg1Num  - enumeration value corresponding to the first particular hardware register in
    //                        which the value resides
    //             reg1Addr - remote address within a register display (in a context or frame) of the
    //                        register component of the value
    //             memAddr  - remote address for the memory component of the value
    //     output: no out parameters, but the instance has been initialized
    RegAndMemBaseValueHome(const CordbNativeFrame *      pFrame,
                           CorDebugRegister              reg1Num,
                           CORDB_ADDRESS                 memAddr):
        RegValueHome(pFrame, reg1Num),
        m_memAddr(memAddr)
    {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    RegAndMemBaseValueHome(const RegAndMemBaseValueHome * pRemoteRegAddr):
        RegValueHome(pRemoteRegAddr),
        m_memAddr()
    {};

    // make a copy of this instance of RegRegValueHome
    virtual
    RegAndMemBaseValueHome * Clone() const = 0;

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * DT_pContext, bool fIsSigned) = 0;

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer) = 0;

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr) = 0;

    //-------------------------------------
    // data members
    //-------------------------------------

protected:
    // remote address for the memory component of the value
    CORDB_ADDRESS m_memAddr;

}; // class RegAndMemBaseValueHome;

// class RegMemValueHome
// type derived from abstract class RegAndMemBaseValueHome to represent a Register/Memory location where the
// high order part of the value is kept in a register, and the low order part is kept in memory
class RegMemValueHome: public RegAndMemBaseValueHome
{
public:

    // initializing constructor
    // Arguments:
    //     input:  pFrame   - frame to which the value belongs
    //             reg1Num  - enumeration value corresponding to the first particular hardware register in
    //                        which the value resides
    //             reg1Addr - remote address within a register display (in a context or frame) of the
    //                        register component of the value
    //             memAddr  - remote address for the memory component of the value
    //     output: no out parameters, but the instance has been initialized
    RegMemValueHome(const CordbNativeFrame *      pFrame,
                    CorDebugRegister              reg1Num,
                    CORDB_ADDRESS                 memAddr):
       RegAndMemBaseValueHome(pFrame, reg1Num, memAddr)
   {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    RegMemValueHome(const RegMemValueHome * pRemoteRegAddr):
        RegAndMemBaseValueHome(pRemoteRegAddr)
    {};

    // make a copy of this instance of RegMemValueHome
    virtual
    RegMemValueHome * Clone() const { return new RegMemValueHome(*this); };

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned);

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer);

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

}; // class RegMemValueHome;

// class MemRegValueHome
// type derived from abstract class RegAndMemBaseValueHome to represent a Register/Memory location where the
// low order part of the value is kept in a register, and the high order part is kept in memory
class MemRegValueHome: public RegAndMemBaseValueHome
{
public:

    // initializing constructor
    // Arguments:
    //     input:  pFrame   - frame to which the value belongs
    //             reg1Num  - enumeration value corresponding to the first particular hardware register in
    //                        which the value resides
    //             reg1Addr - remote address within a register display (in a context or frame) of the
    //                        register component of the value
    //             memAddr  - remote address for the memory component of the value
    //     output: no out parameters, but the instance has been initialized
    MemRegValueHome(const CordbNativeFrame *      pFrame,
                    CorDebugRegister              reg1Num,
                    CORDB_ADDRESS                 memAddr):
       RegAndMemBaseValueHome(pFrame, reg1Num, memAddr)
   {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    MemRegValueHome(const MemRegValueHome * pRemoteRegAddr):
        RegAndMemBaseValueHome(pRemoteRegAddr)
    {};

    // make a copy of this instance of MemRegValueHome
    virtual
    MemRegValueHome * Clone() const { return new MemRegValueHome(*this); };

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned);

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer);

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

}; // class MemRegValueHome;

// class FloatRegValueHome
// derived class to add an index into the FP register stack for a floating point value
class FloatRegValueHome: public EnregisteredValueHome
{
public:
    // initializing constructor
    // Arguments:
    //     input:  pFrame - frame to which the value belongs
    //             index  - index into the floating point stack where the value resides
    //     output: no out parameters, but the instance has been initialized
    FloatRegValueHome(const CordbNativeFrame *      pFrame,
                      DWORD                         index):
        EnregisteredValueHome(pFrame),
        m_floatIndex(index)
    {};

    // copy constructor
    // Arguments:
    //     input:  pRemoteRegAddr - instance of a remote register address from which the values for this
    //                              instance will come
    //     output: no out parameters, but the instance has been initialized
    FloatRegValueHome(const FloatRegValueHome * pRemoteRegAddr):
        EnregisteredValueHome(pRemoteRegAddr->m_pFrame),
        m_floatIndex(pRemoteRegAddr->m_floatIndex)
    {};

    // make a copy of this instance of FloatRegValueHome
    virtual
    FloatRegValueHome * Clone() const { return new FloatRegValueHome(*this); };

    // set the value of a remote enregistered value
    virtual
    void SetEnregisteredValue(MemoryRange newValue, DT_CONTEXT * pContext, bool fIsSigned);

    // Gets an enregistered value and returns it to the caller
    virtual
    void GetEnregisteredValue(MemoryRange valueOutBuffer);

    // initialize an instance of RemoteAddress for use in an IPC event buffer with values from this
    // instance of a derived class of EnregisteredValueHome
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

    //-------------------------------------
    // data members
    //-------------------------------------

protected:
    // index into the FP registers for the register in which the floating point value resides
    const DWORD            m_floatIndex;
 }; // class FloatRegValueHome

// ----------------------------------------------------------------------------
// Type hierarchy for value locations
//              		                 ValueHome
//              		                   | | |
//             		     ------------------  |  -------------------
//                      |                    |                     |
//              RemoteValueHome      RegisterValueHome       HandleValueHome
//                 | 	   |
//         --------         -------
//        |	                       |
// VCRemoteValueHome      RefRemoteValueHome
//
// ValueHome:           abstract base class, provides remote read and write utilities
// RemoteValueHome:     used for CordbObjectValue, CordbArrayValue, and CordbBoxValue instances,
//                      which have only remote locations, and for other ICDValues with a remote address
// RegisterValueHome:   used for CordbGenericValue and CordbReferenceValue instances with
//                      only a register location
// HandleValueHome:     used for CordbReferenceValue instances with only an object handle
// VCRemoteValueHome:   used for CordbVCObjectValue instances to supply special operation CreateInternalValue for
//                      value class objects with only a remote location
// RefRemoteValueHome:  used for CordbReferenceValue instances with only a remote location
//
// In addition, we have a special type for the ValueHome field for CordbReferenceValue instances:
// RefValueHome. This will have a field of type ValueHome and will implement extra operations only relevant
// for object references.
//
// ----------------------------------------------------------------------------
//
class ValueHome
{
public:
    ValueHome(CordbProcess * pProcess):
      m_pProcess(pProcess) { _ASSERTE(pProcess != NULL); };

    virtual
    ~ValueHome() {}

    // releases resources as necessary
    virtual
    void Clear() = 0;

    // gets the remote address for the value or returns NULL if none exists
    virtual
        CORDB_ADDRESS GetAddress() = 0;

    // Gets a value and returns it in dest
    // Argument:
    //     input:  none (uses fields of the instance)
    //     output: dest - buffer containing the value retrieved as long as the returned HRESULT doesn't
    //     indicate an error.
    // Note: Throws errors from read process memory operation or GetThreadContext operation
    virtual
    void GetValue(MemoryRange dest) = 0;

    // Sets a location to the value provided in src
    // Arguments:
    //     input:  src -   buffer containing the new value to be set--memory for this buffer is owned by the caller
    //             pType - type information about the value
    //     output: none, but on success, changes m_remoteValue to hold the new value
    // Note: Throws errors from SafeWriteBuffer
    virtual
    void SetValue(MemoryRange src, CordbType * pType) = 0;

    // creates an ICDValue for a field or array element or for the value type of a boxed object
    // Arguments:
    //     input:  pType        - type of the internal value
    //             offset       - offset to the internal value
    //             localAddress - address of thelogical buffer within the parent class' local cached
    //                            copy that holds the internal element
    //             size         - size of the internal value
    //    output:  ppValue      - the newly created ICDValue instance
    // Note: Throws for a variety of possible failures: OOM, E_FAIL, errors from
    //               ReadProcessMemory.
    virtual
    void CreateInternalValue(CordbType *       pType,
                             SIZE_T            offset,
                             void *            localAddress,
                             ULONG32           size,
                             ICorDebugValue ** ppValue) = 0;

    // Gets the value of a field or element of an existing ICDValue instance and returns it in dest
    // Arguments
    //     input:  offset - offset within the value to the internal field or element
    //     output: dest   - buffer to hold the value--memory for this buffer is owned by the caller
    // Note: Throws process memory write errors
    virtual
    void GetInternalValue(MemoryRange dest, SIZE_T offset) = 0;

    // copies register information from this to a RemoteAddress instance for FuncEval
    // Arguments:
    //     output: pRegAddr - copy of information in m_pRemoteRegAddr, converted to
    //                        an instance of RemoteAddress
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr) = 0;

private:
    // unimplemented copy constructor to prevent passing by value
    ValueHome(ValueHome * pValHome);

protected:
    // --------------
    // data member
    // --------------
   CordbProcess * m_pProcess;
}; // class ValueHome

// ============================================================================
// RemoteValueHome class
// ============================================================================
// to be used for CordbObjectValue, CordbArrayValue, and CordbBoxValue, none of which ever have anything but
// a remote address
class RemoteValueHome: public ValueHome
{
public:
    // constructor
    // Note: It's possible that remoteValue.pAddress may be NULL--FuncEval makes
    // empty GenericValues for literals in which case we would have neither a remote address nor a
    // register address
    RemoteValueHome(CordbProcess * pProcess, TargetBuffer remoteValue);

    // gets the remote address for the value
    virtual
    CORDB_ADDRESS GetAddress() { return m_remoteValue.pAddress; };

    // releases resources as necessary
    virtual
    void Clear() {};

    // Gets a value and returns it in dest
    virtual
    void GetValue(MemoryRange dest);

    // Sets a location to the value provided in src
    virtual
    void SetValue(MemoryRange src, CordbType * pType);

    // creates an ICDValue for a field or array element or for the value type of a boxed object
    virtual
    void CreateInternalValue(CordbType *       pType,
                                SIZE_T            offset,
                                void *            localAddress,
                                ULONG32           size,
                                ICorDebugValue ** ppValue);

    // Gets the value of a field or element of an existing ICDValue instance and returns it in dest
    virtual
    void GetInternalValue(MemoryRange dest, SIZE_T offset);

    // copies register information from this to a RemoteAddress instance for FuncEval
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);


    // ----------------
    // data member
    // ----------------

protected:
    TargetBuffer  m_remoteValue;
}; // class RemoteValueHome

// ============================================================================
// RegisterValueHome class
// ============================================================================
// for values that may either have a remote location or be enregistered--
// to be used for CordbGenericValue, and as base for CordbVCObjectValue and CordbReferenceValue
class RegisterValueHome: public ValueHome
{
public:
    // constructor
    RegisterValueHome(CordbProcess *                pProcess,
                      EnregisteredValueHomeHolder * ppRemoteRegAddr);

    // clean up resources
    virtual
    void Clear();

    // gets the remote address for the value or returns NULL if none exists
    virtual
    CORDB_ADDRESS GetAddress() { return NULL; };

    // Gets a value and returns it in dest
    virtual
    void GetValue(MemoryRange dest);

    // Sets a location to the value provided in src
    virtual
    void SetValue(MemoryRange src, CordbType * pType);

    // creates an ICDValue for a field or array element or for the value type of a boxed object
    virtual
    void CreateInternalValue(CordbType *       pType,
                             SIZE_T            offset,
                             void *            localAddress,
                             ULONG32           size,
                             ICorDebugValue ** ppValue);

    // Gets the value of a field or element of an existing ICDValue instance and returns it in dest
    virtual
    void GetInternalValue(MemoryRange dest, SIZE_T offset);

    // copies the register information from this to a RemoteAddress instance
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

protected:

    // sets a remote enregistered location to a new value
    void SetEnregisteredValue(MemoryRange src, bool fIsSigned);

    // gets a value from an enregistered location
    void GetEnregisteredValue(MemoryRange dest);

    bool IsSigned(CorElementType elementType);

    // ----------------
    // data member
    // ----------------

protected:
    // Left Side register location info for various kinds of (partly) enregistered values.
    EnregisteredValueHome * m_pRemoteRegAddr;

}; // class RegisterValueHome

// ============================================================================
// HandleValueHome class
// ============================================================================

class HandleValueHome: public ValueHome
{
public:
    // constructor
    // Arguments:
    //     input:  pProcess   -  process to which the value belongs
    //             vmObjHandle - objectHandle holding the object address
    HandleValueHome(CordbProcess * pProcess, VMPTR_OBJECTHANDLE vmObjHandle):
        ValueHome(pProcess),
        m_vmObjectHandle(vmObjHandle) {};

    // releases resources as necessary
    virtual
    void Clear() {};

    // gets the remote address for the value or returns NULL if none exists
    virtual
    CORDB_ADDRESS GetAddress();

    // Gets a value and returns it in dest
    virtual
    void GetValue(MemoryRange dest);

    // Sets a location to the value provided in src
    virtual
    void SetValue(MemoryRange src, CordbType * pType);

    // creates an ICDValue for a field or array element or for the value type of a boxed object
    virtual
    void CreateInternalValue(CordbType *       pType,
                             SIZE_T            offset,
                             void *            localAddress,
                             ULONG32           size,
                             ICorDebugValue ** ppValue);

    // Gets the value of a field or element of an existing ICDValue instance and returns it in dest
    virtual
    void GetInternalValue(MemoryRange dest, SIZE_T offset);

    // copies the register information from this to a RemoteAddress instance
    virtual
    void CopyToIPCEType(RemoteAddress * pRegAddr);

    // ----------------
    // data member
    // ----------------
private:
    VMPTR_OBJECTHANDLE m_vmObjectHandle;
}; // class HandleValueHome;

// ============================================================================
// VCRemoteValueHome class
// ============================================================================
// used only for CordbVCObjectValue
class VCRemoteValueHome: public RemoteValueHome
{
public:
    // constructor
    VCRemoteValueHome(CordbProcess * pProcess,
                      TargetBuffer   remoteValue):
        RemoteValueHome(pProcess, remoteValue) {};

    // Sets a location to the value provided in src
    virtual
    void SetValue(MemoryRange src, CordbType * pType);

}; // class VCRemoteValueHome

// ============================================================================
// RefRemoteValueHome class
// ============================================================================

// used only for CordbReferenceValue
class RefRemoteValueHome: public RemoteValueHome
{
public:
    // constructor
    // Arguments
    RefRemoteValueHome(CordbProcess *                pProcess,
                       TargetBuffer                  remoteValue);

    // Sets a location to the value provided in src
    virtual
    void SetValue(MemoryRange src, CordbType * pType);

}; // class RefRemoteValueHome

// ============================================================================
// RefValueHome class
// ============================================================================

// abstract superclass for derivations RefRemoteValueHome and RefRegValueHome
class RefValueHome
{
public:
    // constructor
    RefValueHome() { m_pHome = NULL; m_fNullObjHandle = true; };

    // constructor
    RefValueHome(CordbProcess *                pProcess,
                 TargetBuffer                  remoteValue,
                 EnregisteredValueHomeHolder * ppRemoteRegAddr,
                 VMPTR_OBJECTHANDLE            vmObjHandle);

    // indicates whether the object handle is null
    bool ObjHandleIsNull() { return m_fNullObjHandle; };
    void SetObjHandleFlag(bool isNull) { m_fNullObjHandle = isNull; };

    // ----------------
    // data members
    // ----------------
    // appropriate instantiation of ValueHome
    ValueHome * m_pHome;

private:
    // true iff m_pHome is an instantiation of RemoteValueHome or RegisterValueHome
    bool m_fNullObjHandle;
}; // class RefValueHome

typedef enum {kUnboxed, kBoxed} BoxedValue;
#define EMPTY_BUFFER TargetBuffer(PTR_TO_CORDB_ADDRESS((void *)NULL), 0)

/* ------------------------------------------------------------------------- *
 * Variable Home class
 * ------------------------------------------------------------------------- */
class CordbVariableHome : public CordbBase, public ICorDebugVariableHome
{
public:
    CordbVariableHome(CordbNativeCode *pCode,
                      const ICorDebugInfo::NativeVarInfo nativeVarInfo,
                      BOOL isLoc,
                      ULONG index);
    ~CordbVariableHome();
    virtual void Neuter();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbVariableHome"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }

    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugVariableHome
    //-----------------------------------------------------------

    COM_METHOD GetCode(ICorDebugCode **ppCode);

    COM_METHOD GetSlotIndex(ULONG32 *pSlotIndex);

    COM_METHOD GetArgumentIndex(ULONG32* pArgumentIndex);

    COM_METHOD GetLiveRange(ULONG32* pStartOffset,
                            ULONG32 *pEndOffset);

    COM_METHOD GetLocationType(VariableLocationType *pLocationType);

    COM_METHOD GetRegister(CorDebugRegister *pRegister);

    COM_METHOD GetOffset(LONG *pOffset);
private:
    RSSmartPtr<CordbNativeCode> m_pCode;
    ICorDebugInfo::NativeVarInfo m_nativeVarInfo;
    BOOL m_isLocal;
    ULONG m_index;
};


// for an inheritance graph of the ICDValue types, // See file:./ICorDebugValueTypes.vsd for a diagram of the types.
/* ------------------------------------------------------------------------- *
 * Value class
 * ------------------------------------------------------------------------- */

class CordbValue : public CordbBase
{
public:
    //-----------------------------------------------------------
    // Constructor/destructor
    //-----------------------------------------------------------
    CordbValue(CordbAppDomain * appdomain,
               CordbType *      type,
               CORDB_ADDRESS    id,
               bool             isLiteral,
               NeuterList *     pList = NULL);

    virtual ~CordbValue();
    virtual void Neuter();

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *pType)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT(pType, CorElementType *);

        *pType = m_type->m_elementType;
        return (S_OK);
    }

    COM_METHOD GetSize(ULONG32 *pSize)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT(pSize, ULONG32 *);

        if (m_size > UINT32_MAX)
        {
            *pSize = UINT32_MAX;
            return (COR_E_OVERFLOW);
        }

        *pSize = (ULONG)m_size;
        return (S_OK);
    }

    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType);

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT(pSize, ULONG64 *);

        *pSize = m_size;
        return (S_OK);
    }

    virtual HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS *pAddress) = 0;

    //-----------------------------------------------------------
    // Methods not exported through COM
    //-----------------------------------------------------------

    // Helper for code:CordbValue::CreateValueByType. Create a new instance of CordbGenericValue
    static
    void CreateGenericValue(CordbAppDomain *               pAppdomain,
                            CordbType *                    pType,
                            TargetBuffer                   remoteValue,
                            MemoryRange                    localValue,
                            EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                            ICorDebugValue**               ppValue);

    // Helper for code:CordbValue::CreateValueByType. Create a new instance of CordbVCObjectValue or
    // CordbReferenceValue
    static
    void CreateVCObjOrRefValue(CordbAppDomain *               pAppdomain,
                               CordbType *                    pType,
                               bool                           boxed,
                               TargetBuffer                   remoteValue,
                               MemoryRange                    localValue,
                               EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                               ICorDebugValue**               ppValue);

    // Create the proper ICDValue instance based on the given element type.
    static void CreateValueByType(CordbAppDomain *               appdomain,
                                  CordbType *                    type,
                                  bool                           boxed,
                                  TargetBuffer                   remoteValue,
                                  MemoryRange                    localValue,
                                  EnregisteredValueHomeHolder *  ppRemoteRegAddr,
                                  ICorDebugValue**               ppValue);

    // Create the proper ICDValue instance based on the given remote heap object
    static ICorDebugValue* CreateHeapValue(CordbAppDomain* pAppDomain,
                                           VMPTR_Object vmObj);

    // Creates a proper CordbReferenceValue instance based on the given remote heap object
    static CordbReferenceValue* CreateHeapReferenceValue(CordbAppDomain* pAppDomain,
                                                         VMPTR_Object vmObj);

    // Returns a pointer to the ValueHome field of this instance of CordbValue if one exists or NULL
    // otherwise. Therefore, this also tells us indirectly whether this instance of CordbValue is also an
    // instance of one of its derived types and thus has a ValueHome field.
    virtual
    ValueHome * GetValueHome() { return NULL; };

    static ULONG32 GetSizeForType(CordbType * pType, BoxedValue boxing);

    virtual CordbAppDomain *GetAppDomain()
    {
        return m_appdomain;
    }

    HRESULT InternalCreateHandle(
        CorDebugHandleType handleType,
        ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    CordbAppDomain *            m_appdomain;
    RSSmartPtr<CordbType>       m_type;

    // size of the value
    SIZE_T                      m_size;

    // true if the value is a RS fabrication.
    bool                        m_isLiteral;

};

/* ------------------------------------------------------------------------- *
* Value Breakpoint class
* ------------------------------------------------------------------------- */

class CordbValueBreakpoint : public CordbBreakpoint,
    public ICorDebugValueBreakpoint
{
public:
    CordbValueBreakpoint(CordbValue *pValue);


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbValueBreakpoint"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValueBreakpoint
    //-----------------------------------------------------------

    COM_METHOD GetValue(ICorDebugValue **ppValue);
    COM_METHOD Activate(BOOL bActive);
    COM_METHOD IsActive(BOOL *pbActive)
    {
        VALIDATE_POINTER_TO_OBJECT(pbActive, BOOL *);

        return BaseIsActive(pbActive);
    }

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    void Disconnect();

public:
    CordbValue * m_value;
};

/* ------------------------------------------------------------------------- *
* Generic Value class
* ------------------------------------------------------------------------- */

class CordbGenericValue : public CordbValue, public ICorDebugGenericValue, public ICorDebugValue2, public ICorDebugValue3
{
public:
    CordbGenericValue(CordbAppDomain *              appdomain,
                      CordbType *                   type,
                      TargetBuffer                  remoteValue,
                      EnregisteredValueHomeHolder * ppRemoteRegAddr);

    CordbGenericValue(CordbType * pType);
    // destructor
    ~CordbGenericValue();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbGenericValue"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    // gets the type of the value
    // Arguments:
    //     output: pType - the type of the value. The caller must guarantee that pType is non-null.
    // Return Value: S_OK on success, E_INVALIDARG on failure
    COM_METHOD GetType(CorElementType *pType)
    {
        return (CordbValue::GetType(pType));
    }

    // gets the size of the value
    // Arguments:
    //     output: pSize - the size of the value. The caller must guarantee that pSize is non-null.
    // Return Value: S_OK on success, E_INVALIDARG on failure
    COM_METHOD GetSize(ULONG32 *pSize)
    {
        return (CordbValue::GetSize(pSize));
    }
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
    {
        return (CordbValue::CreateBreakpoint(ppBreakpoint));
    }

    // gets the remote (LS) address of the value. This may return NULL if the
    // value is a literal or resides in a register.
    // Arguments:
    //     output: pAddress - the address of the value. The caller must guarantee is
    //             non-Null
    // Return Value: S_OK on success or E_INVALIDARG if pAddress is null
    COM_METHOD GetAddress(CORDB_ADDRESS *pAddress)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT_OR_NULL(pAddress, CORDB_ADDRESS *);

        *pAddress = m_pValueHome ? m_pValueHome->GetAddress() : NULL;
        return (S_OK);
    }

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        return (CordbValue::GetSize64(pSize));
    }

    //-----------------------------------------------------------
    // ICorDebugGenericValue
    //-----------------------------------------------------------

    COM_METHOD GetValue(void *pTo);
    COM_METHOD SetValue(void *pFrom);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    // initialize a generic value by copying the necessary data, either
    // from the remote process or from another value in this process.
    void Init(MemoryRange localValue);
    bool CopyLiteralData(BYTE *pBuffer);

    // Returns a pointer to the ValueHome field
    virtual
    ValueHome * GetValueHome() { return m_pValueHome; };

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
    // hold copies of up to 64-bit values.
    BYTE  m_pCopyOfData[8];

    // location information--remote or register address
    ValueHome * m_pValueHome;
};


/* ------------------------------------------------------------------------- *
 * Reference Value class
 * ------------------------------------------------------------------------- */

class CordbReferenceValue : public CordbValue, public ICorDebugReferenceValue, public ICorDebugValue2, public ICorDebugValue3
{
public:
    CordbReferenceValue(CordbAppDomain *              pAppdomain,
                        CordbType *                   pType,
                        MemoryRange                   localValue,
                        TargetBuffer                  remoteValue,
                        EnregisteredValueHomeHolder * ppRegAddr,
                        VMPTR_OBJECTHANDLE            vmObjectHandle);
    CordbReferenceValue(CordbType * pType);
    virtual ~CordbReferenceValue();
    virtual void Neuter();


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbReferenceValue"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *pType);

    // get the size of the reference
    // Arguments:
    //     output: pSize - the size of the value--this must be non-NULL
    // Return Value: S_OK on success or E_INVALIDARG
    COM_METHOD GetSize(ULONG32 *pSize)
    {
        return (CordbValue::GetSize(pSize));
    }

    COM_METHOD GetAddress(CORDB_ADDRESS *pAddress);
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
    {
        return (CordbValue::CreateBreakpoint(ppBreakpoint));
    }

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        return (CordbValue::GetSize64(pSize));
    }

    //-----------------------------------------------------------
    // ICorDebugReferenceValue
    //-----------------------------------------------------------

    COM_METHOD IsNull(BOOL * pfIsNull);
    COM_METHOD GetValue(CORDB_ADDRESS *pAddress);
    COM_METHOD SetValue(CORDB_ADDRESS address);
    COM_METHOD Dereference(ICorDebugValue **ppValue);
    COM_METHOD DereferenceStrong(ICorDebugValue **ppValue);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    // Helper function for SanityCheckPointer. Make an attempt to read memory at the address which is the
    // value of the reference.
    void TryDereferencingTarget();

    // Do a sanity check on the pointer which is the value of the object reference. We can't efficiently
    // ensure that the pointer is really good, so we settle for a quick check just to make sure the memory at
    // the address is readable. We're actually just checking that we can dereference the pointer.
    // If the address is invalid, this will throw.
    void SanityCheckPointer (CorElementType type);

    // get information about the reference when it's not an object address but another kind of pointer type:
    // ELEMENT_TYPE_BYREF, ELEMENT_TYPE_PTR or ELEMENT_TYPE_FNPTR
    void GetPointerData(CorElementType type, MemoryRange localValue);

    // get basic object specific data when a reference points to an object, plus extra data if the object is
    // an array or string
    static
    void GetObjectData(CordbProcess *            pProcess,
                       void *                    objectAddress,
                       CorElementType            type,
                       VMPTR_AppDomain           vmAppdomain,
                       DebuggerIPCE_ObjectData * pInfo);

    // get information about a TypedByRef object when the reference is the address of a TypedByRef structure.
    static
    void GetTypedByRefData(CordbProcess *            pProcess,
                           CORDB_ADDRESS             pTypedByRef,
                           CorElementType            type,
                           VMPTR_AppDomain           vmAppDomain,
                           DebuggerIPCE_ObjectData * pInfo);

    //  get the address of the object referenced
    void * GetObjectAddress(MemoryRange localValue);

    // update type information after initializing -- when we initialize, we may get more exact type
    // information than we previously had
    void UpdateTypeInfo();

    // Initialize this CordbReferenceValue. This may involve inspecting the LS to get information about the
    // referent.
    HRESULT InitRef(MemoryRange localValue);

    bool CopyLiteralData(BYTE *pBuffer);

    static HRESULT Build(CordbAppDomain *              appdomain,
                         CordbType *                   type,
                         TargetBuffer                  remoteValue,
                         MemoryRange                   localValue,
                         VMPTR_OBJECTHANDLE            vmObjectHandle,
                         EnregisteredValueHomeHolder * ppRemoteRegAddr,
                         CordbReferenceValue**         ppValue);

    static HRESULT BuildFromGCHandle(CordbAppDomain *pAppDomain, VMPTR_OBJECTHANDLE gcHandle, ICorDebugReferenceValue ** pOutRef);

    // Common dereference routine shared by both CordbReferenceValue + CordbHandleValue
    static HRESULT DereferenceCommon(CordbAppDomain *          pAppDomain,
                                     CordbType *               pType,
                                     CordbType *               pRealTypeOfTypedByref,
                                     DebuggerIPCE_ObjectData * m_pInfo,
                                     ICorDebugValue **         ppValue);

    // Returns a pointer to the ValueHome field
    virtual
    ValueHome * GetValueHome() { return m_valueHome.m_pHome; };

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

public:
    DebuggerIPCE_ObjectData  m_info;
    CordbType *              m_realTypeOfTypedByref; // weak ref

    RefValueHome             m_valueHome;

    // Indicates when we last syncronized our stored data (m_info) from the left side
    UINT                     m_continueCounterLastSync;
};

/* ------------------------------------------------------------------------- *
 * Object Value class
 *
 * Because of the oddness of string objects in the Runtime we have one
 * object that implements both ObjectValue and StringValue. There is a
 * definite string type, but its really just an object of the string
 * class. Furthermore, you can have a variable whose type is listed as
 * "class", but its an instance of the string class and therefore needs
 * to be treated like a string.
 * ------------------------------------------------------------------------- */

class CordbObjectValue : public CordbValue,
                         public ICorDebugObjectValue,
                         public ICorDebugObjectValue2,
                         public ICorDebugGenericValue,
                         public ICorDebugStringValue,
                         public ICorDebugValue2,
                         public ICorDebugValue3,
                         public ICorDebugHeapValue2,
                         public ICorDebugHeapValue3,
                         public ICorDebugHeapValue4,
                         public ICorDebugExceptionObjectValue,
                         public ICorDebugComObjectValue,
                         public ICorDebugDelegateObjectValue
{
public:

    CordbObjectValue(CordbAppDomain *          appdomain,
                     CordbType *               type,
                     TargetBuffer              remoteValue,
                     DebuggerIPCE_ObjectData * pObjectData );

    virtual ~CordbObjectValue();


    virtual void Neuter();
#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbObjectValue"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void ** ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType * pType);
    COM_METHOD GetSize(ULONG32 * pSize);
    COM_METHOD GetAddress(CORDB_ADDRESS * pAddress);
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint ** ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType ** ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize);

    //-----------------------------------------------------------
    // ICorDebugHeapValue
    //-----------------------------------------------------------

    COM_METHOD IsValid(BOOL * pfIsValid);
    COM_METHOD CreateRelocBreakpoint(ICorDebugValueBreakpoint ** ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugHeapValue2
    //-----------------------------------------------------------
    COM_METHOD CreateHandle(CorDebugHandleType type, ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugHeapValue3
    //-----------------------------------------------------------
    COM_METHOD GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount);
    COM_METHOD GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum);

    //-----------------------------------------------------------
    // ICorDebugHeapValue4
    //-----------------------------------------------------------
    COM_METHOD CreatePinnedHandle(ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugObjectValue
    //-----------------------------------------------------------

    COM_METHOD GetClass(ICorDebugClass ** ppClass);
    COM_METHOD GetFieldValue(ICorDebugClass *  pClass,
                             mdFieldDef        fieldDef,
                             ICorDebugValue ** ppValue);
    COM_METHOD GetVirtualMethod(mdMemberRef memberRef,
                                ICorDebugFunction **ppFunction);
    COM_METHOD GetContext(ICorDebugContext ** ppContext);
    COM_METHOD IsValueClass(BOOL * pfIsValueClass);
    COM_METHOD GetManagedCopy(IUnknown ** ppObject);
    COM_METHOD SetFromManagedCopy(IUnknown * pObject);

    COM_METHOD GetFieldValueForType(ICorDebugType *   pType,
                                    mdFieldDef        fieldDef,
                                    ICorDebugValue ** ppValue);

    COM_METHOD GetVirtualMethodAndType(mdMemberRef          memberRef,
                                       ICorDebugFunction ** ppFunction,
                                       ICorDebugType **     ppType);

    //-----------------------------------------------------------
    // ICorDebugGenericValue
    //-----------------------------------------------------------

    COM_METHOD GetValue(void * pTo);
    COM_METHOD SetValue(void * pFrom);

    //-----------------------------------------------------------
    // ICorDebugStringValue
    //-----------------------------------------------------------
    COM_METHOD GetLength(ULONG32 * pcchString);
    COM_METHOD GetString(ULONG32   cchString,
                         ULONG32 * ppcchStrin,
                         __out_ecount_opt(cchString) WCHAR     szString[]);

    //-----------------------------------------------------------
    // ICorDebugExceptionObjectValue
    //-----------------------------------------------------------
    COM_METHOD EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum);

    //-----------------------------------------------------------
    // ICorDebugComObjectValue
    //-----------------------------------------------------------
    COM_METHOD GetCachedInterfaceTypes(BOOL bIInspectableOnly,
                        ICorDebugTypeEnum** ppInterfacesEnum);

    COM_METHOD GetCachedInterfacePointers(BOOL bIInspectableOnly,
                        ULONG32 celt,
                        ULONG32 *pcEltFetched,
                        CORDB_ADDRESS * ptrs);

    //-----------------------------------------------------------
    // ICorDebugComObjectValue
    //-----------------------------------------------------------
    COM_METHOD GetTarget(ICorDebugReferenceValue** ppObject);
    COM_METHOD GetFunction(ICorDebugFunction** ppFunction);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    HRESULT Init();

    DebuggerIPCE_ObjectData GetInfo() { return m_info; }
    CordbHangingFieldTable * GetHangingFieldTable() { return &m_hangingFieldsInstance; }

    // Returns a pointer to the ValueHome field
    virtual
    RemoteValueHome * GetValueHome() { return &m_valueHome; };

protected:
    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------
    DebuggerIPCE_ObjectData  m_info;
    BYTE *                   m_pObjectCopy;     // local cached copy of the object
    BYTE *                   m_objectLocalVars; // var base in _this_ process
                                                // points _into_ m_pObjectCopy
    BYTE *                   m_stringBuffer;    // points _into_ m_pObjectCopy

    // remote location information
    RemoteValueHome          m_valueHome;

    // If instances fields are added by EnC, their storage will be off the objects
    // syncblock.  Cache per-object information about such fields here.
    CordbHangingFieldTable   m_hangingFieldsInstance;

private:
    HRESULT IsExceptionObject();

    BOOL                     m_fIsExceptionObject;

    HRESULT IsRcw();

    BOOL                     m_fIsRcw;

    HRESULT IsDelegate();
    HRESULT GetFunctionHelper(ICorDebugFunction **ppFunction);
    HRESULT GetTargetHelper(ICorDebugReferenceValue **ppTarget);

    BOOL                     m_fIsDelegate;
};

/* ------------------------------------------------------------------------- *
 * Value Class Object Value class
 * ------------------------------------------------------------------------- */

class CordbVCObjectValue : public CordbValue,
                           public ICorDebugObjectValue, public ICorDebugObjectValue2,
                           public ICorDebugGenericValue, public ICorDebugValue2,
                           public ICorDebugValue3
{
public:
    CordbVCObjectValue(CordbAppDomain *               pAppdomain,
                       CordbType *                    pType,
                       TargetBuffer                   remoteValue,
                       EnregisteredValueHomeHolder *  ppRemoteRegAddr);
    virtual ~CordbVCObjectValue();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbVCObjectValue"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *pType);

    COM_METHOD GetSize(ULONG32 *pSize)
    {
        return (CordbValue::GetSize(pSize));
    }
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
    {
        return (CordbValue::CreateBreakpoint(ppBreakpoint));
    }

    COM_METHOD GetAddress(CORDB_ADDRESS *pAddress)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);

        *pAddress = m_pValueHome->GetAddress();
        return (S_OK);
    }

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        return (CordbValue::GetSize64(pSize));
    }

    //-----------------------------------------------------------
    // ICorDebugObjectValue
    //-----------------------------------------------------------

    COM_METHOD GetClass(ICorDebugClass **ppClass);
    COM_METHOD GetFieldValue(ICorDebugClass *pClass,
                             mdFieldDef fieldDef,
                             ICorDebugValue **ppValue);
    COM_METHOD GetVirtualMethod(mdMemberRef memberRef,
                                ICorDebugFunction **ppFunction);
    COM_METHOD GetContext(ICorDebugContext **ppContext);
    COM_METHOD IsValueClass(BOOL *pbIsValueClass);
    COM_METHOD GetManagedCopy(IUnknown **ppObject);
    COM_METHOD SetFromManagedCopy(IUnknown *pObject);
    COM_METHOD GetFieldValueForType(ICorDebugType * pType,
                                    mdFieldDef fieldDef,
                                    ICorDebugValue ** ppValue);
    COM_METHOD GetVirtualMethodAndType(mdMemberRef memberRef,
                                       ICorDebugFunction **ppFunction,
                                       ICorDebugType **ppType);

    //-----------------------------------------------------------
    // ICorDebugGenericValue
    //-----------------------------------------------------------

    COM_METHOD GetValue(void *pTo);
    COM_METHOD SetValue(void *pFrom);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    // Initializes the Right-Side's representation of a Value Class object.
    HRESULT Init(MemoryRange localValue);
    //HRESULT ResolveValueClass();
    CordbClass *GetClass();

    // Returns a pointer to the ValueHome field
    virtual
    ValueHome * GetValueHome() { return m_pValueHome; };

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:

    // local cached copy of the value class
    BYTE *   m_pObjectCopy;

    // location information
    ValueHome * m_pValueHome;
};


/* ------------------------------------------------------------------------- *
 * Box Value class
 * ------------------------------------------------------------------------- */

class CordbBoxValue : public CordbValue,
                      public ICorDebugBoxValue,
                      public ICorDebugGenericValue,
                      public ICorDebugValue2,
                      public ICorDebugValue3,
                      public ICorDebugHeapValue2,
                      public ICorDebugHeapValue3,
                      public ICorDebugHeapValue4
{
public:
    CordbBoxValue(CordbAppDomain *  appdomain,
                  CordbType *       type,
                  TargetBuffer      remoteValue,
                  ULONG32           size,
                  SIZE_T            offsetToVars);
    virtual ~CordbBoxValue();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbBoxValue"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *pType);

    COM_METHOD GetSize(ULONG32 *pSize)
    {
        return (CordbValue::GetSize(pSize));
    }
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
    {
        return (CordbValue::CreateBreakpoint(ppBreakpoint));
    }

     COM_METHOD GetAddress(CORDB_ADDRESS *pAddress)
    {
        LIMITED_METHOD_CONTRACT;

        FAIL_IF_NEUTERED(this);
        VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);

        *pAddress = m_valueHome.GetAddress();
        return (S_OK);
    }

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        return (CordbValue::GetSize64(pSize));
    }

    //-----------------------------------------------------------
    // ICorDebugHeapValue
    //-----------------------------------------------------------

    COM_METHOD IsValid(BOOL *pbValid);
    COM_METHOD CreateRelocBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugHeapValue2
    //-----------------------------------------------------------
    COM_METHOD CreateHandle(CorDebugHandleType type, ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugHeapValue3
    //-----------------------------------------------------------
    COM_METHOD GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount);
    COM_METHOD GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum);

    //-----------------------------------------------------------
    // ICorDebugHeapValue4
    //-----------------------------------------------------------
    COM_METHOD CreatePinnedHandle(ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugGenericValue
    //-----------------------------------------------------------

    COM_METHOD GetValue(void *pTo);
    COM_METHOD SetValue(void *pFrom);

    //-----------------------------------------------------------
    // ICorDebugBoxValue
    //-----------------------------------------------------------
    COM_METHOD GetObject(ICorDebugObjectValue **ppObject);

    // Returns a pointer to the ValueHome field
    virtual
    RemoteValueHome * GetValueHome() { return &m_valueHome; };

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
    SIZE_T          m_offsetToVars;

    // remote location information
    RemoteValueHome m_valueHome;

};

/* ------------------------------------------------------------------------- *
 * Array Value class
 * ------------------------------------------------------------------------- */

class CordbArrayValue : public CordbValue,
                        public ICorDebugArrayValue,
                        public ICorDebugGenericValue,
                        public ICorDebugValue2,
                        public ICorDebugValue3,
                        public ICorDebugHeapValue2,
                        public ICorDebugHeapValue3,
                        public ICorDebugHeapValue4
{
public:
    CordbArrayValue(CordbAppDomain *          appdomain,
                    CordbType *               type,
                    DebuggerIPCE_ObjectData * pObjectInfo,
                    TargetBuffer              remoteValue);
    virtual ~CordbArrayValue();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbArrayValue"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugValue
    //-----------------------------------------------------------

    COM_METHOD GetType(CorElementType *pType)
    {
        return (CordbValue::GetType(pType));
    }
    COM_METHOD GetSize(ULONG32 *pSize)
    {
        return (CordbValue::GetSize(pSize));
    }
    COM_METHOD GetAddress(CORDB_ADDRESS *pAddress)
    {
        VALIDATE_POINTER_TO_OBJECT(pAddress, CORDB_ADDRESS *);
        *pAddress = m_valueHome.GetAddress();
        return (S_OK);
    }
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint)
    {
        return (CordbValue::CreateBreakpoint(ppBreakpoint));
    }

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize)
    {
        return (CordbValue::GetSize64(pSize));
    }

    //-----------------------------------------------------------
    // ICorDebugHeapValue
    //-----------------------------------------------------------

    COM_METHOD IsValid(BOOL *pbValid);
    COM_METHOD CreateRelocBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugHeapValue2
    //-----------------------------------------------------------
    COM_METHOD CreateHandle(CorDebugHandleType type, ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugHeapValue3
    //-----------------------------------------------------------
    COM_METHOD GetThreadOwningMonitorLock(ICorDebugThread **ppThread, DWORD *pAcquisitionCount);
    COM_METHOD GetMonitorEventWaitList(ICorDebugThreadEnum **ppThreadEnum);

    //-----------------------------------------------------------
    // ICorDebugHeapValue4
    //-----------------------------------------------------------
    COM_METHOD CreatePinnedHandle(ICorDebugHandleValue ** ppHandle);

    //-----------------------------------------------------------
    // ICorDebugArrayValue
    //-----------------------------------------------------------

    COM_METHOD GetElementType(CorElementType * pType);
    COM_METHOD GetRank(ULONG32 * pnRank);
    COM_METHOD GetCount(ULONG32 * pnCount);
    COM_METHOD GetDimensions(ULONG32 cdim, ULONG32 dims[]);
    COM_METHOD HasBaseIndicies(BOOL * pbHasBaseIndices);
    COM_METHOD GetBaseIndicies(ULONG32 cdim, ULONG32 indices[]);
    COM_METHOD GetElement(ULONG32 cdim, ULONG32 indices[], ICorDebugValue ** ppValue);
    COM_METHOD GetElementAtPosition(ULONG32 nIndex, ICorDebugValue ** ppValue);

    //-----------------------------------------------------------
    // ICorDebugGenericValue
    //-----------------------------------------------------------

    COM_METHOD GetValue(void *pTo);
    COM_METHOD SetValue(void *pFrom);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    HRESULT Init();

    // Returns a pointer to the ValueHome field
    virtual
    RemoteValueHome * GetValueHome() { return &m_valueHome; };

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

private:
    // contains information about the array, such as rank, number of elements, element size, etc.
    DebuggerIPCE_ObjectData  m_info;

    // type of the elements
    CordbType               *m_elemtype;

    // consists of three parts: a vector containing the lower bounds for each dimension,
    //                          a vector containing the upper bounds for each dimension,
    //                          a local cached copy of (part of) the array--initialized lazily when we
    //                             request a particular element. If the array is large, we will store only
    //                             part of it, swapping out the cached segment as necessary to retrieve
    //                             requested elements.
    BYTE *                   m_pObjectCopy;

    // points to the beginning of the vector containing the lower bounds for each dimension in m_pObjectCopy
    DWORD *                  m_arrayLowerBase;

    // points to the beginning of the vector containing the lower bounds for each dimension in m_pObjectCopy
    DWORD *                  m_arrayUpperBase;
    // index of lower bound of data currently stored in m_pObjectCopy
    SIZE_T                   m_idxLower;

    // index of upper bound of data currently stored in m_pObjectCopy
    SIZE_T                   m_idxUpper;

    // remote location information
    RemoteValueHome m_valueHome;

};

class CordbHandleValue : public CordbValue, public ICorDebugHandleValue, public ICorDebugValue2, public ICorDebugValue3
{
public:
    CordbHandleValue(CordbAppDomain *appdomain,
                     CordbType *type,
                     CorDebugHandleType handleType);
    HRESULT Init(VMPTR_OBJECTHANDLE pHandle);

    virtual ~CordbHandleValue();

    virtual void Neuter();
    virtual void NeuterLeftSideResources();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbHandleValue"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugHandleValue interface
    //-----------------------------------------------------------
    COM_METHOD GetHandleType(CorDebugHandleType *pType);


    /*
      * The final release of the interface will also dispose of the handle. This
      * API provides the ability for client to early dispose the handle.
      *
      */
    COM_METHOD Dispose();

    //-----------------------------------------------------------
    // ICorDebugValue interface
    //-----------------------------------------------------------
    COM_METHOD GetType(CorElementType *pType);
    COM_METHOD GetSize(ULONG32 *pSize);
    COM_METHOD GetAddress(CORDB_ADDRESS *pAddress);
    COM_METHOD CreateBreakpoint(ICorDebugValueBreakpoint **ppBreakpoint);

    //-----------------------------------------------------------
    // ICorDebugValue2
    //-----------------------------------------------------------

    COM_METHOD GetExactType(ICorDebugType **ppType)
    {
        FAIL_IF_NEUTERED(this);

        // If AppDomain is already unloaded, return error
        if (m_appdomain->IsNeutered() == TRUE)
        {
            return COR_E_APPDOMAINUNLOADED;
        }
        if (m_vmHandle.IsNull())
        {
            return CORDBG_E_HANDLE_HAS_BEEN_DISPOSED;
        }

        return (CordbValue::GetExactType(ppType));
    }

    //-----------------------------------------------------------
    // ICorDebugValue3
    //-----------------------------------------------------------

    COM_METHOD GetSize64(ULONG64 *pSize);

    //-----------------------------------------------------------
    // ICorDebugReferenceValue interface
    //-----------------------------------------------------------

    COM_METHOD IsNull(BOOL *pbNull);
    COM_METHOD GetValue(CORDB_ADDRESS *pValue);
    COM_METHOD SetValue(CORDB_ADDRESS value);
    COM_METHOD Dereference(ICorDebugValue **ppValue);
    COM_METHOD DereferenceStrong(ICorDebugValue **ppValue);

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------

    // Returns a pointer to the ValueHome field
    virtual
    RemoteValueHome * GetValueHome() { return NULL; };

private:
    //BOOL RefreshHandleValue(void **pObjectToken);
    HRESULT RefreshHandleValue();

    // EE object handle pointer. Can be casted to OBJECTHANDLE when go to LS
    // This instance owns the handle object and must call into the VM to release
    // it.
    // If this is non-null, then we increment code:CordbProces::IncrementOutstandingHandles.
    // Once it goes null, we should decrement the count.
    // Use AssignHandle, ClearHandle to keep this in sync.
    VMPTR_OBJECTHANDLE  m_vmHandle;


    void AssignHandle(VMPTR_OBJECTHANDLE handle);
    void ClearHandle();

    BOOL                m_fCanBeValid;      // true if object "can" be valid. False when object is no longer valid.
    CorDebugHandleType m_handleType;        // handle type can be strong or weak
    DebuggerIPCE_ObjectData  m_info;
; // ICORDebugClass of this object when we create the handle
};

// This class actually has the implementation for ICorDebugHeap3 interfaces. Any value which implements
// the interface just delegates to these static calls.
class CordbHeapValue3Impl
{
public:
    static HRESULT GetThreadOwningMonitorLock(CordbProcess* pProcess,
                                              CORDB_ADDRESS remoteObjAddress,
                                              ICorDebugThread **ppThread,
                                              DWORD *pAcquistionCount);
    static HRESULT GetMonitorEventWaitList(CordbProcess* pProcess,
                                           CORDB_ADDRESS remoteObjAddress,
                                           ICorDebugThreadEnum **ppThreadEnum);
};

/* ------------------------------------------------------------------------- *
 * Eval class
 * ------------------------------------------------------------------------- */

class CordbEval : public CordbBase, public ICorDebugEval, public ICorDebugEval2
{
public:
    CordbEval(CordbThread* pThread);
    virtual ~CordbEval();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbEval"; }
#endif

    virtual void Neuter();
    virtual void NeuterLeftSideResources();

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorDebugEval
    //-----------------------------------------------------------

    COM_METHOD CallFunction(ICorDebugFunction *pFunction,
                            ULONG32 nArgs,
                            ICorDebugValue *ppArgs[]);
    COM_METHOD NewObject(ICorDebugFunction *pConstructor,
                         ULONG32 nArgs,
                         ICorDebugValue *ppArgs[]);
    COM_METHOD NewObjectNoConstructor(ICorDebugClass *pClass);
    COM_METHOD NewString(LPCWSTR string);
    COM_METHOD NewArray(CorElementType elementType,
                        ICorDebugClass *pElementClass,
                        ULONG32 rank,
                        ULONG32 dims[],
                        ULONG32 lowBounds[]);
    COM_METHOD IsActive(BOOL *pbActive);
    COM_METHOD Abort();
    COM_METHOD GetResult(ICorDebugValue **ppResult);
    COM_METHOD GetThread(ICorDebugThread **ppThread);
    COM_METHOD CreateValue(CorElementType elementType,
                           ICorDebugClass *pElementClass,
                           ICorDebugValue **ppValue);
    COM_METHOD NewStringWithLength(LPCWSTR wszString, UINT iLength);

    COM_METHOD CallParameterizedFunction(ICorDebugFunction * pFunction,
                                         ULONG32 nTypeArgs,
                                         ICorDebugType * rgpTypeArgs[],
                                         ULONG32 nArgs,
                                         ICorDebugValue * rgpArgs[]);

    COM_METHOD CreateValueForType(ICorDebugType *pType,
                                  ICorDebugValue **ppValue);

    COM_METHOD NewParameterizedObject(ICorDebugFunction * pConstructor,
                                      ULONG32 nTypeArgs,
                                      ICorDebugType * rgpTypeArgs[],
                                      ULONG32 nArgs,
                                      ICorDebugValue * rgpArgs[]);

    COM_METHOD NewParameterizedObjectNoConstructor(ICorDebugClass * pClass,
                                                   ULONG32 nTypeArgs,
                                                   ICorDebugType * rgpTypeArgs[]);

    COM_METHOD NewParameterizedArray(ICorDebugType * pElementType,
                                     ULONG32 rank,
                                     ULONG32 dims[],
                                     ULONG32 lowBounds[]);

    //-----------------------------------------------------------
    // ICorDebugEval2
    //-----------------------------------------------------------

    COM_METHOD RudeAbort();

    //-----------------------------------------------------------
    // Non-COM methods
    //-----------------------------------------------------------
    HRESULT GatherArgInfo(ICorDebugValue *pValue,
                          DebuggerIPCE_FuncEvalArgData *argData);
    HRESULT SendCleanup();

    // Create a RS literal for primitive type funceval result. In case the result is used as an argument for
    // another funceval, we need to make sure that we're not relying on the LS value, which will be freed and
    // thus unavailable.
    HRESULT CreatePrimitiveLiteral(CordbType *       pType,
                                   ICorDebugValue ** ppValue);

    //-----------------------------------------------------------
    // Data members
    //-----------------------------------------------------------

    bool IsEvalDuringException() { return m_evalDuringException; }
private:
    // We must keep a strong reference to the thread so we can properly fail out of SendCleanup if someone releases an
    // ICorDebugEval after the process has completely gone away.
    RSSmartPtr<CordbThread>    m_thread;

    CordbFunction             *m_function;
    CordbClass                *m_class;
    DebuggerIPCE_FuncEvalType  m_evalType;

    HRESULT SendFuncEval(unsigned int genericArgsCount, ICorDebugType *genericArgs[], void *argData1, unsigned int argData1Size, void *argData2, unsigned int argData2Size, DebuggerIPCEvent * event);
    HRESULT FilterHR(HRESULT hr);
    BOOL DoAppDomainsMatch( CordbAppDomain* pAppDomain, ULONG32 nTypes, ICorDebugType *pTypes[], ULONG32 nValues, ICorDebugValue *pValues[] );

public:
    bool                       m_complete;
    bool                       m_successful;
    bool                       m_aborted;
    void                      *m_resultAddr;

    // This is an OBJECTHANDLE on the LS if func-eval creates a strong handle.
    // This is a resource in the left-side and must be cleaned up in the left-side.
    // This gets handled off to a CordbHandleValue (m_pHandleValue) once code:CordbEval::GetResult
    // and then the CordbHandle is responsible for releasing it in the left-side.
    // Issue!! This will be leaked if nobody calls GetResult().
    VMPTR_OBJECTHANDLE         m_vmObjectHandle;

    // This is the corresponding cached CordbHandleValue for GetResult.
    // This takes ownership of the strong handle, m_objectHandle.
    // This is an External reference, which keeps the Value from being neutered
    // on a NeuterAtWill sweep.
    RSExtSmartPtr<CordbHandleValue> m_pHandleValue;

    DebuggerIPCE_ExpandedTypeData m_resultType;
    VMPTR_AppDomain            m_resultAppDomainToken;

    // Left-side memory that needs to be freed.
    LSPTR_DEBUGGEREVAL         m_debuggerEvalKey;


    // If we're evalling during a thread's exception, remember the info so that we can restore it when we're done.
    bool                       m_evalDuringException;     // flag whether we're during the thread's exception.
    VMPTR_OBJECTHANDLE  m_vmThreadOldExceptionHandle; // object handle for thread's managed exception object.

#ifdef _DEBUG
    // Func-eval should perturb the the thread's current appdomain. So we remember it at start
    // and then ensure that the func-eval complete restores it.
    CordbAppDomain *           m_DbgAppDomainStarted;
#endif
};


/* ------------------------------------------------------------------------- *
 * Win32 Event Thread class
 * ------------------------------------------------------------------------- */
const unsigned int CW32ET_UNKNOWN_PROCESS_SLOT = 0xFFffFFff; // it's a managed process,
        //but we don't know which slot it's in - for Detach.

//---------------------------------------------------------------------------------------
//
// Dedicated thread for win32 debugging operations.
//
// Notes:
//    This is owned by the ShimProcess object. That will both create this and destroy it.
//    OS restriction is that all win32 debugging APIs (CreateProcess, DebugActiveProcess,
//    DebugActiveProcessStop, WaitForDebugEvent, ContinueDebugEvent, etc) are on the same thread.
//
class CordbWin32EventThread
{
    friend class CordbProcess; //so that Detach can call ExitProcess
public:
    CordbWin32EventThread(Cordb * pCordb, ShimProcess * pShim);
    virtual ~CordbWin32EventThread();

    //
    // You create a new instance of this class, call Init() to set it up,
    // then call Start() start processing events. Stop() terminates the
    // thread and deleting the instance cleans all the handles and such
    // up.
    //
    HRESULT Init();
    HRESULT Start();
    HRESULT Stop();

    HRESULT SendCreateProcessEvent(MachineInfo machineInfo,
                                   LPCWSTR programName,
                                   __in_z LPWSTR  programArgs,
                                   LPSECURITY_ATTRIBUTES lpProcessAttributes,
                                   LPSECURITY_ATTRIBUTES lpThreadAttributes,
                                   BOOL bInheritHandles,
                                   DWORD dwCreationFlags,
                                   PVOID lpEnvironment,
                                   LPCWSTR lpCurrentDirectory,
                                   LPSTARTUPINFOW lpStartupInfo,
                                   LPPROCESS_INFORMATION lpProcessInformation,
                                   CorDebugCreateProcessFlags corDebugFlags);

    HRESULT SendDebugActiveProcessEvent(MachineInfo machineInfo,
                                        const ProcessDescriptor *pProcessDescriptor,
                                        bool fWin32Attach,
                                        CordbProcess *pProcess);

    HRESULT SendDetachProcessEvent(CordbProcess *pProcess);

#ifdef FEATURE_INTEROP_DEBUGGING
    HRESULT SendUnmanagedContinue(CordbProcess *pProcess,
                                  EUMContinueType eContType);
    HRESULT UnmanagedContinue(CordbProcess *pProcess,
                              EUMContinueType eContType);
    void DoDbgContinue(CordbProcess * pProcess,
                       CordbUnmanagedEvent * pUnmanagedEvent);
    void ForceDbgContinue(CordbProcess *pProcess,
                          CordbUnmanagedThread *ut,
                          DWORD contType,
                          bool contProcess);

#endif //FEATURE_INTEROP_DEBUGGING

    void LockSendToWin32EventThreadMutex()
    {
        LOG((LF_CORDB, LL_INFO10000, "W32ET::LockSendToWin32EventThreadMutex\n"));
        m_sendToWin32EventThreadMutex.Lock();
    }

    void UnlockSendToWin32EventThreadMutex()
    {
        m_sendToWin32EventThreadMutex.Unlock();
        LOG((LF_CORDB, LL_INFO10000, "W32ET::UnlockSendToWin32EventThreadMutex\n"));
    }

    bool IsWin32EventThread()
    {
        return (m_threadId == GetCurrentThreadId());
    }

    void Win32EventLoop();


    INativeEventPipeline * GetNativePipeline();
private:
    void ThreadProc();
    static DWORD WINAPI ThreadProc(LPVOID parameter);

    void CreateProcess();


    INativeEventPipeline * m_pNativePipeline;


    void AttachProcess();

    void HandleUnmanagedContinue();

    void ExitProcess(bool fDetach);

private:
    RSSmartPtr<Cordb>    m_cordb;

    HANDLE               m_thread;
    DWORD                m_threadId;
    HANDLE               m_threadControlEvent;
    HANDLE               m_actionTakenEvent;
    BOOL                 m_run;

    // The process that we're 1:1 with.
    // This is set when we get a Create / Attach event.
    // This is only used on the W32ET, which guarantees it will free of races.
    RSSmartPtr<CordbProcess> m_pProcess;


    ShimProcess * m_pShim;

    // @todo - convert this into Stop-Go lock?
    RSLock               m_sendToWin32EventThreadMutex;

    unsigned int         m_action;
    HRESULT              m_actionResult;
    union
    {
        struct
        {
            MachineInfo machineInfo;
            LPCWSTR programName;
            LPWSTR  programArgs;
            LPSECURITY_ATTRIBUTES lpProcessAttributes;
            LPSECURITY_ATTRIBUTES lpThreadAttributes;
            BOOL bInheritHandles;
            DWORD dwCreationFlags;
            PVOID lpEnvironment;
            LPCWSTR lpCurrentDirectory;
            LPSTARTUPINFOW lpStartupInfo;
            LPPROCESS_INFORMATION lpProcessInformation;
            CorDebugCreateProcessFlags corDebugFlags;
        } createData;

        struct
        {
            MachineInfo machineInfo;
            ProcessDescriptor processDescriptor;
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
            bool fWin32Attach;
#endif
            CordbProcess *pProcess;

            // Wrapper to determine if we're interop-debugging.
            bool IsInteropDebugging()
            {
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
                return fWin32Attach;
#else
                return false;
#endif
            }
        } attachData;

        struct
        {
            CordbProcess    *pProcess;
        } detachData;

        struct
        {
            CordbProcess *process;
            EUMContinueType eContType;
        } continueData;
    }                    m_actionData;
};


// Thread-safe stack which.
template <typename T>
class InterlockedStack
{
public:
    InterlockedStack();
    ~InterlockedStack();

    // Thread safe pushes + pops.
    // Many threads can push simultaneously.
    // Only 1 thread can pop.
    void Push(T * pItem);
    T * Pop();

protected:
    T * m_pHead;
};

//-----------------------------------------------------------------------------
// Workitem to be placed on RCET worker queue.
// There's 1 RCET for to be shared by all processes.
//-----------------------------------------------------------------------------
class RCETWorkItem
{
public:

    virtual ~RCETWorkItem() {}

    // Item is executed and then removed from the list and deleted.
    virtual void Do() = 0;

    CordbProcess * GetProcess() { return m_pProcess; }

protected:
    RCETWorkItem(CordbProcess * pProcess)
    {
        m_pProcess.Assign(pProcess);
        m_next = NULL;
    }

    RSSmartPtr<CordbProcess> m_pProcess;

    // This field is accessed by the InterlockedStack.
    friend class InterlockedStack<RCETWorkItem>;
    RCETWorkItem * m_next;
};


// Item to do Neutering work on ExitProcess.
class ExitProcessWorkItem : public RCETWorkItem
{
public:
    ExitProcessWorkItem(CordbProcess * pProc) : RCETWorkItem(pProc)
    {
    }

    virtual void Do();
};

// Item to do send Attach event.
class SendAttachProcessWorkItem : public RCETWorkItem
{
public:
    SendAttachProcessWorkItem(CordbProcess * pProc) : RCETWorkItem(pProc)
    {
    }

    virtual void Do();
};


/* ------------------------------------------------------------------------- *
 * Runtime Controller Event Thread class
 * ------------------------------------------------------------------------- */

class CordbRCEventThread
{
public:
    CordbRCEventThread(Cordb* cordb);
    virtual ~CordbRCEventThread();

    //
    // You create a new instance of this class, call Init() to set it up,
    // then call Start() start processing events. Stop() terminates the
    // thread and deleting the instance cleans all the handles and such
    // up.
    //
    HRESULT Init();
    HRESULT Start();
    HRESULT Stop();

    // RCET will take ownership of this item and delete it.
    void QueueAsyncWorkItem(RCETWorkItem * pItem);

    HRESULT SendIPCEvent(CordbProcess* process,
                         DebuggerIPCEvent* event,
                         SIZE_T eventSize);

    void ProcessStateChanged();
    void FlushQueuedEvents(CordbProcess* process);

    HRESULT WaitForIPCEventFromProcess(CordbProcess* process,
                                       CordbAppDomain *pAppDomain,
                                       DebuggerIPCEvent* event);

    bool IsRCEventThread();

private:
    void DrainWorkerQueue();

    void ThreadProc();
    static DWORD WINAPI ThreadProc(LPVOID parameter);


private:
    InterlockedStack<class RCETWorkItem> m_WorkerStack;

    RSSmartPtr<Cordb>    m_cordb;
    HANDLE               m_thread;
    DWORD                m_threadId;
    BOOL                 m_run;
    HANDLE               m_threadControlEvent;
    BOOL                 m_processStateChanged;
};

#ifdef FEATURE_INTEROP_DEBUGGING
/* ------------------------------------------------------------------------- *
 * Unmanaged Event struct
 * ------------------------------------------------------------------------- */

enum CordbUnmanagedEventState
{

    // The continued flags get set in one of a few patterns.
    // 1) The event is continued having never been hijacked =>
    //      EventContinuedUnhijacked is set
    // 2) The event is continued having been hijacked and then the process terminates or
    //    an error occurs before the hijack finishes =>
    //      EventContinuedHijacked is set
    // 3) The event is continued having been hijacked, then the hijack completes and
    //    execution resumes in the debuggee
    //      EventContinuedHijacked is set
    //      EventContinuedUnhijacked is set

    CUES_None                     = 0x00,
    CUES_ExceptionCleared         = 0x01,
    CUES_EventContinuedHijacked   = 0x02,
    CUES_EventContinuedUnhijacked = 0x04,
    CUES_Dispatched               = 0x08,
    CUES_ExceptionUnclearable     = 0x10,

    // This is set when a user continues the event by calling
    // Continue()
    CUES_UserContinued            = 0x20,
    // This is true if the event is an IB event
    CUES_IsIBEvent                = 0x40,
};

struct CordbUnmanagedEvent
{
public:
    BOOL IsExceptionCleared() { return m_state & CUES_ExceptionCleared; }
    BOOL IsEventContinuedHijacked() { return m_state & CUES_EventContinuedHijacked; }
    BOOL IsEventContinuedUnhijacked() { return m_state & CUES_EventContinuedUnhijacked; }
    BOOL IsEventUserContinued() { return m_state & CUES_UserContinued; }
    BOOL IsEventWaitingForContinue()
    {
        return (!IsEventContinuedHijacked() && !IsEventContinuedUnhijacked());
    }
    BOOL IsDispatched() { return m_state & CUES_Dispatched; }
    BOOL IsExceptionUnclearable() { return m_state & CUES_ExceptionUnclearable; }
    BOOL IsIBEvent() { return m_state & CUES_IsIBEvent; }

    void SetState(CordbUnmanagedEventState state) { m_state = (CordbUnmanagedEventState)(m_state | state); }
    void ClearState(CordbUnmanagedEventState state) { m_state = (CordbUnmanagedEventState)(m_state & ~state); }

    CordbUnmanagedThread     *m_owner;
    CordbUnmanagedEventState  m_state;
    DEBUG_EVENT               m_currentDebugEvent;
    CordbUnmanagedEvent      *m_next;
};


/* ------------------------------------------------------------------------- *
 * Unmanaged Thread class
 * ------------------------------------------------------------------------- */

enum CordbUnmanagedThreadState
{
    CUTS_None                        = 0x0000,
    CUTS_Deleted                     = 0x0001,
    CUTS_FirstChanceHijacked         = 0x0002,
    // Set when interop debugging needs the SS flag to be enabled
    // regardless of what the user wants it to be
    CUTS_IsSSFlagNeeded              = 0x0004,
    CUTS_GenericHijacked             = 0x0008,
    // when the m_raiseExceptionEntryContext is valid
    CUTS_HasRaiseExceptionEntryCtx   = 0x0010,
    CUTS_BlockingForSync             = 0x0020,
    CUTS_Suspended                   = 0x0040,
    CUTS_IsSpecialDebuggerThread     = 0x0080,
    // when the thread is re-executing RaiseException to retrigger an exception
    CUTS_IsRaiseExceptionHijacked    = 0x0100,
    CUTS_HasIBEvent                  = 0x0200,
    CUTS_HasOOBEvent                 = 0x0400,
    CUTS_HasSpecialStackOverflowCase = 0x0800,
#ifdef _DEBUG
    CUTS_DEBUG_SingleStep            = 0x1000,
#endif
    CUTS_SkippingNativePatch         = 0x2000,
    CUTS_HasContextSet               = 0x4000,
    // Set when interop debugging is making use of the single step flag
    // but the user has not set it
    CUTS_IsSSFlagHidden              = 0x8000

};

class CordbUnmanagedThread : public CordbBase
{
public:
    CordbUnmanagedThread(CordbProcess *pProcess, DWORD dwThreadId, HANDLE hThread, void *lpThreadLocalBase);
    ~CordbUnmanagedThread();

    using CordbBase::GetProcess;

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbUnmanagedThread"; }
#endif

    // CordbUnmanagedThread is a purely internal object. It's not exposed via ICorDebug APIs and so
    // we should never use External AddRef.
    ULONG STDMETHODCALLTYPE AddRef() { _ASSERTE(!"Don't use external addref on a CordbUnmanagedThread"); return (BaseAddRef());}
    ULONG STDMETHODCALLTYPE Release() { _ASSERTE(!"Don't use external release on a CordbUnmanagedThread"); return (BaseRelease());}

    COM_METHOD QueryInterface(REFIID riid, void **ppInterface)
    {
        _ASSERTE(!"Don't use QI on a CordbUnmanagedThread");
        // Not really used since we never expose this class. If we ever do expose this class via the ICorDebug API then
        // we should, of course, implement this.
        return E_NOINTERFACE;
    }

    HRESULT LoadTLSArrayPtr();

    // Hijacks this thread to a hijack worker function which recieves the current
    // context and the provided exception record. The reason determines what code
    // the hijack worker executes
    HRESULT SetupFirstChanceHijack(EHijackReason::EHijackReason reason, const EXCEPTION_RECORD * pExceptionRecord);
    HRESULT SetupFirstChanceHijackForSync();

    HRESULT SetupGenericHijack(DWORD eventCode, const EXCEPTION_RECORD * pRecord);
    HRESULT FixupFromGenericHijack();

    HRESULT FixupAfterOOBException(CordbUnmanagedEvent * ue);

    void SetupForSkipBreakpoint(NativePatch * pNativePatch);
    void FixupForSkipBreakpoint();
    bool IsCantStop();

    // These are wrappers for the OS calls which hide
    // the effects of hijacking and internal SS flag usage
    HRESULT GetThreadContext(DT_CONTEXT * pContext);
    HRESULT SetThreadContext(DT_CONTEXT * pContext);

    // Turns on and off the internal usage of the SS flag
    VOID BeginStepping();
    VOID EndStepping();

    // An accessor for &m_context, this value generally stores
    // a context we may need to restore after a hijack completes
    DT_CONTEXT * GetHijackCtx();

private:
    CORDB_ADDRESS m_stackBase;
    CORDB_ADDRESS m_stackLimit;

public:
    BOOL GetStackRange(CORDB_ADDRESS *pBase, CORDB_ADDRESS *pLimit);

    BOOL IsDeleted() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_Deleted; }
    BOOL IsFirstChanceHijacked() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_FirstChanceHijacked; }
    BOOL IsGenericHijacked() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_GenericHijacked; }
    BOOL IsBlockingForSync() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_BlockingForSync; }
    BOOL IsSuspended() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_Suspended; }
    BOOL IsSpecialDebuggerThread() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_IsSpecialDebuggerThread; }
    BOOL HasIBEvent() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_HasIBEvent; }
    BOOL HasOOBEvent() { return m_state & CUTS_HasOOBEvent; }
    BOOL HasSpecialStackOverflowCase() {LIMITED_METHOD_CONTRACT;  return m_state & CUTS_HasSpecialStackOverflowCase; }
#ifdef _DEBUG
    BOOL IsDEBUGTrace() { return m_state & CUTS_DEBUG_SingleStep; }
#endif
    BOOL IsSkippingNativePatch() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_SkippingNativePatch; }
    BOOL IsContextSet() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_HasContextSet; }
    BOOL IsSSFlagNeeded() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_IsSSFlagNeeded; }
    BOOL IsSSFlagHidden() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_IsSSFlagHidden; }
    BOOL HasRaiseExceptionEntryCtx() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_HasRaiseExceptionEntryCtx; }
    BOOL IsRaiseExceptionHijacked() { LIMITED_METHOD_CONTRACT; return m_state & CUTS_IsRaiseExceptionHijacked; }

    void SetState(CordbUnmanagedThreadState state)
    {
        LIMITED_METHOD_CONTRACT;
        m_state = (CordbUnmanagedThreadState)(m_state | state);
        _ASSERTE(!IsSuspended() || !IsBlockingForSync());
        _ASSERTE(!IsSuspended() || !IsFirstChanceHijacked());
    }
    void ClearState(CordbUnmanagedThreadState state) {LIMITED_METHOD_CONTRACT;  m_state = (CordbUnmanagedThreadState)(m_state & ~state); }

    void HijackToRaiseException();
    void RestoreFromRaiseExceptionHijack();
    void SaveRaiseExceptionEntryContext();
    void ClearRaiseExceptionEntryContext();
    BOOL IsExceptionFromLastRaiseException(const EXCEPTION_RECORD* pExceptionRecord);

    CordbUnmanagedEvent *IBEvent()  {LIMITED_METHOD_CONTRACT;  return &m_IBEvent; }
    CordbUnmanagedEvent *IBEvent2() {LIMITED_METHOD_CONTRACT;  return &m_IBEvent2; }
    CordbUnmanagedEvent *OOBEvent() { return &m_OOBEvent; }

    DWORD GetOSTid()
    {
        return (DWORD) this->m_id;
    }

#ifdef TARGET_X86
    // Stores the thread's current leaf SEH handler
    HRESULT SaveCurrentLeafSeh();
    // Restores the thread's leaf SEH handler from the previously saved value
    HRESULT RestoreLeafSeh();
#endif

    // Logs basic data about a context to the debugging log
    static VOID LogContext(DT_CONTEXT* pContext);

public:
    HANDLE                     m_handle;

    // @dbgtodo - the TLS reading is only used for interop hijacks; which goes away in Arrowhead.
    // Target address of the Thread Information Block (TIB).
    void                      *m_threadLocalBase;

    // Target address of the Thread Local Storage (TLS) array. This is for slots 0 -63.
    void                      *m_pTLSArray;

    // Target Address of extended Thread local Storage array. These are for slots about 63.
    // This may be NULL if extended storage is not yet allocated.
    void                      *m_pTLSExtendedArray;


    CordbUnmanagedThreadState  m_state;

    CordbUnmanagedEvent        m_IBEvent;
    CordbUnmanagedEvent        m_IBEvent2;
    CordbUnmanagedEvent        m_OOBEvent;

    LSPTR_CONTEXT              m_pLeftSideContext;
    void                      *m_originalHandler;

private:
    // Spare context used for various purposes.
    // See CordbUnmanagedThread::GetThreadContext for details
    DT_CONTEXT                 m_context;

    // The context of the thread the last time it called into kernel32!RaiseException
    DT_CONTEXT                 m_raiseExceptionEntryContext;

    DWORD                      m_raiseExceptionExceptionCode;
    DWORD                      m_raiseExceptionExceptionFlags;
    DWORD                      m_raiseExceptionNumberParameters;
    ULONG_PTR                  m_raiseExceptionExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS];


#ifdef TARGET_X86
    // the SEH handler which was the leaf when SaveCurrentSeh was called (prior to hijack)
    REMOTE_PTR                 m_pSavedLeafSeh;
#endif

    HRESULT EnableSSAfterBP();

    HRESULT GetTlsSlot(DWORD slot, REMOTE_PTR *pValue);
    HRESULT SetTlsSlot(DWORD slot, REMOTE_PTR value);
    REMOTE_PTR GetPreDefTlsSlot(SIZE_T slot);

    void * m_pPatchSkipAddress;

    UINT m_continueCountCached;

    DWORD_PTR GetEEThreadValue();
    HRESULT GetClrModuleTlsDataAddress(REMOTE_PTR* pAddress);

public:
    HRESULT GetEEDebuggerWord(REMOTE_PTR *pValue);
    HRESULT SetEEDebuggerWord(REMOTE_PTR value);
    HRESULT GetEEThreadPtr(REMOTE_PTR *ppEEThread);

    bool GetEEPGCDisabled();
    void GetEEState(bool *threadStepping, bool *specialManagedException);
    bool GetEEFrame();
};
#endif // FEATURE_INTEROP_DEBUGGING


//********************************************************************************
//**************** App Domain Publishing Service API *****************************
//********************************************************************************


class EnumElement
{
public:
    EnumElement()
    {
        m_pData = NULL;
        m_pNext = NULL;
    }

    void SetData (void *pData) { m_pData = pData;}
    void *GetData () { return m_pData;}
    void SetNext (EnumElement *pNext) { m_pNext = pNext;}
    EnumElement *GetNext () { return m_pNext;}

private:
    void        *m_pData;
    EnumElement *m_pNext;
};

#if defined(FEATURE_DBG_PUBLISH)

// Prototype of psapi!GetModuleFileNameEx.
typedef DWORD FPGetModuleFileNameEx(HANDLE, HMODULE, LPTSTR, DWORD);


class CorpubPublish : public CordbCommonBase, public ICorPublish
{
public:
    CorpubPublish();
    virtual ~CorpubPublish();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbPublish"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorPublish
    //-----------------------------------------------------------

    COM_METHOD EnumProcesses(
        COR_PUB_ENUMPROCESS Type,
        ICorPublishProcessEnum **ppIEnum);

    COM_METHOD GetProcess(
        unsigned pid,
        ICorPublishProcess **ppProcess);

    //-----------------------------------------------------------
    // CreateObject
    //-----------------------------------------------------------
    static COM_METHOD CreateObject(REFIID id, void **object)
    {
        *object = NULL;

        if (id != IID_IUnknown && id != IID_ICorPublish)
            return (E_NOINTERFACE);

        CorpubPublish *pCorPub = new (nothrow) CorpubPublish();

        if (pCorPub == NULL)
            return (E_OUTOFMEMORY);

        *object = (ICorPublish*)pCorPub;
        pCorPub->AddRef();

        return (S_OK);
    }

private:
    HRESULT GetProcessInternal( unsigned pid, CorpubProcess **ppProcess );

    // Cached information to get the process name. Not available on all platforms, so may be null.
    HModuleHolder m_hPSAPIdll;
    FPGetModuleFileNameEx * m_fpGetModuleFileNameEx;
};

class CorpubProcess : public CordbCommonBase, public ICorPublishProcess
{
public:
    CorpubProcess(const ProcessDescriptor * pProcessDescriptor,
        bool fManaged,
        HANDLE hProcess,
        HANDLE hMutex,
        AppDomainEnumerationIPCBlock *pAD,
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
        IPCReaderInterface *pIPCReader,
#endif // !FEATURE_DBGIPC_TRANSPORT_DI
        FPGetModuleFileNameEx * fpGetModuleFileNameEx);
    virtual ~CorpubProcess();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CorpubProcess"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorPublishProcess
    //-----------------------------------------------------------
    COM_METHOD IsManaged(BOOL *pbManaged);

    /*
     * Enumerate the list of known application domains in the target process.
     */
    COM_METHOD EnumAppDomains(ICorPublishAppDomainEnum **ppEnum);

    /*
     * Returns the OS ID for the process in question.
     */
    COM_METHOD GetProcessID(unsigned *pid);

    /*
     * Get the display name for a process.
     */
    COM_METHOD GetDisplayName(ULONG32 cchName,
                                ULONG32 *pcchName,
                                __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    CorpubProcess   *GetNextProcess () { return m_pNext;}
    void SetNext (CorpubProcess *pNext) { m_pNext = pNext;}

    // Helper to tell if this process has exited
    bool IsExited();

public:
    ProcessDescriptor               m_processDescriptor;

private:
    bool                            m_fIsManaged;
    HANDLE                          m_hProcess;
    HANDLE                          m_hMutex;
    AppDomainEnumerationIPCBlock    *m_AppDomainCB;
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    IPCReaderInterface              *m_pIPCReader;  // controls the lifetime of the AppDomainEnumerationIPCBlock
#endif // !FEATURE_DBGIPC_TRANSPORT_DI
    CorpubProcess                   *m_pNext;   // pointer to the next process in the process list
    WCHAR                           *m_szProcessName;

};

class CorpubAppDomain  : public CordbCommonBase, public ICorPublishAppDomain
{
public:
    CorpubAppDomain (__in LPWSTR szAppDomainName, ULONG Id);
    virtual ~CorpubAppDomain();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CorpubAppDomain"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface (REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorPublishAppDomain
    //-----------------------------------------------------------

    /*
     * Get the name and ID for an application domain.
     */
    COM_METHOD GetID (ULONG32 *pId);

    /*
     * Get the name for an application domain.
     */
    COM_METHOD GetName (ULONG32 cchName,
                        ULONG32 *pcchName,
                        __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);

    CorpubAppDomain *GetNextAppDomain () { return m_pNext;}
    void SetNext (CorpubAppDomain *pNext) { m_pNext = pNext;}

private:
    CorpubAppDomain *m_pNext;
    WCHAR           *m_szAppDomainName;
    ULONG           m_id;

};

class CorpubProcessEnum : public CordbCommonBase, public ICorPublishProcessEnum
{
public:
    CorpubProcessEnum(CorpubProcess *pFirst);
    virtual ~CorpubProcessEnum();

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CorpubProcessEnum"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorPublishProcessEnum
    //-----------------------------------------------------------

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorPublishEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);
    COM_METHOD Next(ULONG celt,
                    ICorPublishProcess *objects[],
                    ULONG *pceltFetched);

private:
    CorpubProcess       *m_pFirst;
    CorpubProcess       *m_pCurrent;

};

class CorpubAppDomainEnum : public CordbCommonBase, public ICorPublishAppDomainEnum
{
public:
    CorpubAppDomainEnum(CorpubAppDomain *pFirst);
    virtual ~CorpubAppDomainEnum();


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbAppDomainEnum"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // ICorPublishAppDomainEnum
    //-----------------------------------------------------------
    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorPublishEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    COM_METHOD Next(ULONG celt,
                    ICorPublishAppDomain *objects[],
                    ULONG *pceltFetched);

private:
    CorpubAppDomain     *m_pFirst;
    CorpubAppDomain     *m_pCurrent;

};

#endif // defined(FEATURE_DBG_PUBLISH)

class CordbHeapEnum : public CordbBase, public ICorDebugHeapEnum
{
public:
    CordbHeapEnum(CordbProcess *proc);

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbHeapEnum"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    COM_METHOD Next(ULONG celt,
                    COR_HEAPOBJECT objects[],
                    ULONG *pceltFetched);

    virtual void Neuter()
    {
        Clear();
        CordbBase::Neuter();
    }
private:
    void Clear();

private:
    IDacDbiInterface::HeapWalkHandle mHeapHandle;
};


class CordbRefEnum : public CordbBase, public ICorDebugGCReferenceEnum
{
public:
    CordbRefEnum(CordbProcess *proc, BOOL walkWeakRefs);
    CordbRefEnum(CordbProcess *proc, CorGCReferenceType types);

#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbHeapEnum"; }
#endif

    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);

    COM_METHOD Next(ULONG celt,
                    COR_GC_REFERENCE refs[],
                    ULONG *pceltFetched);

    virtual void Neuter();

private:
    RefWalkHandle mRefHandle;
    BOOL mEnumStacksFQ;
    UINT32 mHandleMask;
};

// Since the hash table of modules is per app domain (and
// threads is per process) (for fast lookup from the appdomain/process),
// we need this wrapper
// here which allows us to iterate through an assembly's
// modules.  Is basically filters out modules/threads that aren't
// in the assembly/appdomain. This slow & awkward for assemblies, but fast
// for the common case - appdomain lookup.
class CordbEnumFilter : public CordbBase,
                        public ICorDebugThreadEnum,
                        public ICorDebugModuleEnum
{
public:
    CordbEnumFilter(CordbBase * pOwnerObj, NeuterList * pOwnerList);
    CordbEnumFilter(CordbEnumFilter*src);
    virtual ~CordbEnumFilter();

    virtual void Neuter();


#ifdef _DEBUG
    virtual const char * DbgGetName() { return "CordbEnumFilter"; }
#endif


    //-----------------------------------------------------------
    // IUnknown
    //-----------------------------------------------------------

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release()
    {
        return (BaseRelease());
    }
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //-----------------------------------------------------------
    // Common methods
    //-----------------------------------------------------------
    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum **ppEnum);
    COM_METHOD GetCount(ULONG *pcelt);
    //-----------------------------------------------------------
    // ICorDebugModuleEnum
    //-----------------------------------------------------------
    COM_METHOD Next(ULONG celt,
                    ICorDebugModule *objects[],
                    ULONG *pceltFetched);

    //-----------------------------------------------------------
    // ICorDebugThreadEnum
    //-----------------------------------------------------------
    COM_METHOD Next(ULONG celt,
                    ICorDebugThread *objects[],
                    ULONG *pceltFetched);

    HRESULT Init (ICorDebugModuleEnum *pModEnum, CordbAssembly *pAssembly);
    HRESULT Init (ICorDebugThreadEnum *pThreadEnum, CordbAppDomain *pAppDomain);


private:
    HRESULT NextWorker(ULONG celt, ICorDebugModule *objects[], ULONG *pceltFetched);
    HRESULT NextWorker(ULONG celt,ICorDebugThread *objects[], ULONG *pceltFetched);

    // Owning object is our link to the CordbProcess* tree. Never null until we're neutered.
    // NeuterList is related to the owning object. Need to cache it so that we can pass it on
    // to our clones.
    CordbBase *     m_pOwnerObj; // provides us w/ a CordbProcess*
    NeuterList *    m_pOwnerNeuterList;


    EnumElement *m_pFirst;
    EnumElement *m_pCurrent;
    int         m_iCount;
};

// Helpers to double-check the RS results against DAC.
#if defined(_DEBUG)
void CheckAgainstDAC(CordbFunction * pFunc, void * pIP, mdMethodDef mdExpected);
#endif

HRESULT CopyOutString(const WCHAR * pInputString, ULONG32 cchName, ULONG32 * pcchName, __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]);



inline UINT AllocCookieCordbEval(CordbProcess *pProc, CordbEval* p)
{
    _ASSERTE(pProc->GetProcessLock()->HasLock());
    return pProc->m_EvalTable.Add(p);
}
inline CordbEval * UnwrapCookieCordbEval(CordbProcess *pProc, UINT cookie)
{
    _ASSERTE(pProc->GetProcessLock()->HasLock());
    return pProc->m_EvalTable.LookupAndRemove(cookie);
}


// We defined this at the top of the file - undef it now so that we don't pollute other files.
#undef CRITICAL_SECTION


#ifdef RSCONTRACTS

//-----------------------------------------------------------------------------
// For debug builds, we maintain some thread-state to track debug bits
// to help us do some more aggressive asserts.
//-----------------------------------------------------------------------------

class PublicAPIHolder;
class PublicReentrantAPIHolder;
class PublicCallbackHolder;
class PublicDebuggerErrorCallbackHolder;

class DbgRSThread
{
public:
    friend class PublicAPIHolder;
    friend class PublicReentrantAPIHolder;
    friend class PublicCallbackHolder;
    friend class PublicDebuggerErrorCallbackHolder;
    friend class PrivateShimCallbackHolder;

    DbgRSThread();

    // The TLS slot that we'll put this thread object in.
#ifndef __GNUC__
    static __declspec(thread) DbgRSThread* t_pCurrent;
#else  // !__GNUC__
    static __thread DbgRSThread* t_pCurrent;
#endif // !__GNUC__

    static LONG s_Total; // Total count of thread objects

    // Get a thread object for the current thread via a TLS lookup.
    static DbgRSThread * GetThread();

    // Call during DllMain to release this.
    static DbgRSThread * Create()
    {
        InterlockedIncrement(&s_Total);

        DbgRSThread * p = new (nothrow) DbgRSThread();
        t_pCurrent = p;
        return p;
    }

    void Destroy()
    {
        InterlockedDecrement(&s_Total);

        t_pCurrent = NULL;

        delete this;
    }

    // Return true if this thread is inside the RS.
    bool IsInRS() { return m_cInsideRS > 0; }

    // Locking API..
    // These will assert if the operation is unsafe.
    void NotifyTakeLock(RSLock * pLock);
    void NotifyReleaseLock(RSLock * pLock);

    // Used to map other resources (like thread access) into the lock hierachy.
    // Note this only effects lock leveling checks and doesn't effect HoldsAnyLock().
    void TakeVirtualLock(RSLock::ERSLockLevel level);
    void ReleaseVirtualLock(RSLock::ERSLockLevel level);

    // return true if this thread is holding any RS locks. Useful to check on Public API transition boundaries.
    bool HoldsAnyDbgApiLocks() { return m_cTotalDbgApiLocks > 0; }

    enum EThreadType
    {
        cOther,
        cW32ET
    };
    void SetThreadType(EThreadType e) { m_eThreadType = e; }

    bool IsWin32EventThread() { return m_eThreadType == cW32ET; }

    void SetUnrecoverableCallback(bool fIsUnrecoverableErrorCallback)
    {
        // Not reentrant.
        _ASSERTE(m_fIsUnrecoverableErrorCallback != fIsUnrecoverableErrorCallback);

        m_fIsUnrecoverableErrorCallback = fIsUnrecoverableErrorCallback;
    }

    inline void AssertThreadIsLockFree()
    {
        // If we're in an unrecoverable callback, we may hold locks.
        _ASSERTE(m_fIsUnrecoverableErrorCallback
            || !HoldsAnyDbgApiLocks() ||
            !"Thread should not have locks on public/internal transition");
    }

protected:
    EThreadType m_eThreadType;

    // More debugging tidbits - tid that we're on, and a sanity checking cookie.
    DWORD m_tid;
    DWORD m_Cookie;

    enum ECookie
    {
        COOKIE_VALUE = 0x12345678
    };


    // This tells us if the thread is currently in the scope of a PublicAPIHolder.
    int m_cInsideRS;

    // This tells us if a thread is currently being dispatched via a callback.
    bool m_fIsInCallback;

    // We explicitly track if this thread is in an unrecoverable error callback
    // b/c that will weaken some other asserts.
    // It would be nice to clean up the unrecoverable error callback and have it
    // behave like all the other callbacks. Then we can remove this.
    bool m_fIsUnrecoverableErrorCallback;

    // Locking context. Used to tell what levels of locks we hold so we can determine if a lock is safe to take.
    int m_cLocks[RSLock::LL_MAX];
    int m_cTotalDbgApiLocks;
};

//-----------------------------------------------------------------------------
// Mark when we enter / exit public APIs
//-----------------------------------------------------------------------------

// Holder for Non-reentrant Public API (this is the vast majority)
class PublicAPIHolder
{
public:
    PublicAPIHolder()
    {
        // on entry
        DbgRSThread * pThread = DbgRSThread::GetThread();
        pThread->m_cInsideRS++;
        _ASSERTE(pThread->m_cInsideRS == 1 || !"Non-reentrant API being called re-entrantly");

        // Should never be in public w/ these locks
        pThread->AssertThreadIsLockFree();
    }
    ~PublicAPIHolder() {
        // On exit.
        DbgRSThread * pThread = DbgRSThread::GetThread();
        pThread->m_cInsideRS--;
        _ASSERTE(!pThread->IsInRS());

        // Should never be in public w/ these locks. If we assert here,
        // then we're leaking locks.
        pThread->AssertThreadIsLockFree();
    }
};

// Holder for reentrant public API
class PublicReentrantAPIHolder
{
public:
    PublicReentrantAPIHolder()
    {
        // on entry
        DbgRSThread * pThread = DbgRSThread::GetThread();
        pThread->m_cInsideRS++;

        // Cache count now so that we can calidate it in the dtor.
        m_oldCount = pThread->m_cInsideRS;
        // Since a we may have been called from within the RS, we may hold locks
    }
    ~PublicReentrantAPIHolder()
    {

        // On exit.
        DbgRSThread * pThread = DbgRSThread::GetThread();

        // Ensure that our children were balanced
        _ASSERTE(pThread->m_cInsideRS == m_oldCount);

        pThread->m_cInsideRS--;
        _ASSERTE(pThread->m_cInsideRS >= 0);

        // Since a we may have been called from within the RS, we may hold locks
    }
private:
    int  m_oldCount;
};

// Special holder for DebuggerError callback. This adjusts InsideRS count w/o
// verifying locks. This is very dangerous. We allow this b/c the Debugger Error callback can come at any time.
class PublicDebuggerErrorCallbackHolder
{
public:
    PublicDebuggerErrorCallbackHolder()
    {
        // Exiting from RS; entering Cordbg via a callback
        DbgRSThread * pThread = DbgRSThread::GetThread();

        // This callback is called from within the RS
        _ASSERTE(pThread->IsInRS());

        // Debugger error callback may be called from deep within the RS (after many nestings).
        // So immediately jump to outside. We'll restore this in dtor.
        m_oldCount = pThread->m_cInsideRS;
        pThread->m_cInsideRS = 0;

        _ASSERTE(!pThread->IsInRS());

        // We may be leaking locks for the unrecoverable callback. We mark that so that
        // the asserts about locking can be relaxed.
        pThread->SetUnrecoverableCallback(true);
    }

    ~PublicDebuggerErrorCallbackHolder()
    {
        // Re-entering RS from after a callback.
        DbgRSThread * pThread = DbgRSThread::GetThread();

        pThread->SetUnrecoverableCallback(false);
        pThread->m_cInsideRS = m_oldCount;

        // Our status of being "Inside the RS" is now restored.
        _ASSERTE(pThread->IsInRS());
    }
private:
    int m_oldCount;
};

//---------------------------------------------------------------------------------------
//
// This is the same as the PublicCallbackHolder, except that this class doesn't assert that we are not holding
// any locks when we call out to the shim.
//
// Notes:
//    @dbgtodo  shim, synchronization - We need to settle on one consistent relationshipo between the RS
//    and the shim.  Then we can clean up the sychronization story.  Right now some code considers the shim
//    to be outside of the RS, and so we cannot hold any locks when we call out to the shim.  However, there
//    are cases where we must hold a lock when we call out to the shim.  For example, when we call out to the
//    shim to do a V2-style stackwalk, we need to be holding the stop-go lock so that another thread can't
//    come in and call Continue().  Finally, when we fix this, we should fix
//    PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM() as well.
//

class PrivateShimCallbackHolder
{
public:
    PrivateShimCallbackHolder()
    {
        // Exiting from RS; entering Cordbg via a callback
        DbgRSThread * pThread = DbgRSThread::GetThread();

        // This callback is called from within the RS
        _ASSERTE(pThread->IsInRS());

        // Debugger error callback may be called from deep within the RS (after many nestings).
        // So immediately jump to outside. We'll restore this in dtor.
        m_oldCount = pThread->m_cInsideRS;
        pThread->m_cInsideRS = 0;

        _ASSERTE(!pThread->IsInRS());
    }

    ~PrivateShimCallbackHolder()
    {
        // Re-entering RS from after a callback.
        DbgRSThread * pThread = DbgRSThread::GetThread();

        pThread->m_cInsideRS = m_oldCount;

        // Our status of being "Inside the RS" is now restored.
        _ASSERTE(pThread->IsInRS());
    }
private:
    int m_oldCount;
};

class InternalAPIHolder
{
public:
    InternalAPIHolder()
    {
        DbgRSThread * pThread = DbgRSThread::GetThread();

        // Internal APIs should already be inside the RS.
        _ASSERTE(pThread->IsInRS() ||!"Internal API being called directly from outside (there should be a public API on the stack)");
    }
    void dummy() {}
};

//---------------------------------------------------------------------------------------
//
// This is a simple holder to assert that the current thread is holding the process lock.  The purpose of
// having this holder is to enforce a lock ordering between the process lock in the RS and the DD lock in DAC.
// If a thread needs to take the process lock, it must do so BEFORE taking the DD lock.  Otherwise we could have
// a deadlock between the process lock and the DD lock.
//
// Normally we take the process lock before calling out to DAC, and every DAC API takes the DD lock on entry.
// Moreover, normally DAC doesn't call back into the RS.  The exceptions we currently have are:
// 1) enumeration callbacks (e.g. code:CordbProcess::AppDomainEnumerationCallback)
// 2) code:IDacDbiInterface::IMetaDataLookup
// 3) code:IDacDbiInterface::IAllocator
// 4) code:IStringHolder
//
// Note that the last two are fine because they don't need to take the process lock.  The first two categories
// need to take the process lock before calling into DAC to avoid potential deadlocks.
//

class InternalDacCallbackHolder
{
public:
    InternalDacCallbackHolder(CordbProcess * pProcess)
    {
        _ASSERTE(pProcess->ThreadHoldsProcessLock());
    }
};

// cotract that occurs at public builds.
#define PUBLIC_CONTRACT \
    CONTRACTL { NOTHROW; } CONTRACTL_END;


// Private hook for Shim to call into DBI.
// Since Shim is considered outside DBI, we need to mark that we've re-entered.
// Big difference is that we can throw across this boundary.
// @dbgtodo  private shim hook - Eventually, these will all go away since the shim will be fully public.
#define PUBLIC_API_ENTRY_FOR_SHIM(_pThis) \
    PublicAPIHolder __pah;


#define PUBLIC_API_UNSAFE_ENTRY_FOR_SHIM(_pThis) \
    PublicDebuggerErrorCallbackHolder __pahCallback;

// @dbgtodo  shim, synchronization - Because of the problem mentioned in the comments for
// PrivateShimCallbackHolder, we need this macro so that we don't hit an assertion when we come back into
// the RS from the shim.
#define PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM(_pThis) \
    PublicReentrantAPIHolder __pah;

//-----------------------------------------------------------------------------
// Declare whether an API is public or internal
// Public APIs have the following:
// - We may be called concurrently from multiple threads (ie, not thread safe)
// - This thread does not hold any RS Locks while entering or leaving this function.
// - May or May-not be reentrant.
// Internal APIs:
// - let us specifically mark that we're not a public API, and
// - we're only being called through a public API.
//-----------------------------------------------------------------------------
#define PUBLIC_API_ENTRY(_pThis) \
    STRESS_LOG2(LF_CORDB, LL_INFO1000, "[Public API '%s', this=0x%p]\n", __FUNCTION__, _pThis); \
    PUBLIC_CONTRACT; \
    PublicAPIHolder __pah;

// Mark public APIs that are re-entrant.
// Very few of our APIs should be re-entrant. Even for field access APIs (like GetXXX), the
// public version is heavier (eg, checking the HRESULT) so we benefit from having a fast
// internal version and calling that directly.
#define PUBLIC_REENTRANT_API_ENTRY(_pThis) \
    STRESS_LOG2(LF_CORDB, LL_INFO1000, "[Public API (re) '%s', this=0x%p]\n", __FUNCTION__, _pThis); \
    PUBLIC_CONTRACT; \
    PublicReentrantAPIHolder __pah;



// Mark internal APIs.
// All internal APIs are reentrant (duh)
#define INTERNAL_API_ENTRY(_pThis) InternalAPIHolder __pah; __pah.dummy();

// Mark an internal API from ATT_REQUIRE_STOP / ATT_ALLOW_LIVE_DO_STOP_GO.
// This can assert that we're safe to send IPC events (that we're stopped and hold the SG lock)
// @dbgtodo  synchronization - in V2, this would assert that we were synced.
// In V3, our definition of Sync is in flux.  Need to resolve this with the synchronization feature crew.
#define INTERNAL_SYNC_API_ENTRY(pProc)  \
    CordbProcess * __pProc = (pProc); \
    _ASSERTE(__pProc->GetStopGoLock()->HasLock() || !"Must have stop go lock for internal-sync-api"); \
    InternalAPIHolder __pah; __pah.dummy();



// Mark that a thread is owned by us. Thus the thread's "Inside RS" count > 0.
#define INTERNAL_THREAD_ENTRY(_pThis) \
    STRESS_LOG1(LF_CORDB, LL_INFO1000, "[Internal thread started, this=0x%p]\n", _pThis); \
    PUBLIC_CONTRACT; \
    PublicAPIHolder __pah;

// @dbgtodo  unrecoverable error - This sould be deprecated once we deprecate UnrecoverableError.
#define PUBLIC_CALLBACK_IN_THIS_SCOPE_DEBUGGERERROR(_pThis) \
    PublicDebuggerErrorCallbackHolder __pahCallback;

#define PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(_pThis) \
    PrivateShimCallbackHolder __pahCallback;

// Mark places where DAC may call back into DBI.  We need to assert that we are holding the process lock in
// these places, since otherwise we could deadlock between the DD lock and the process lock.
#define INTERNAL_DAC_CALLBACK(__pProcess) \
    InternalDacCallbackHolder __idch(__pProcess);


// Helper to log debug events.
inline void StressLogNativeDebugEvent(const DEBUG_EVENT * pDebugEvent, bool fOOB)
{
    if ((pDebugEvent)->dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
    {
        STRESS_LOG4(LF_CORDB, LL_EVERYTHING, "[Dispatching Win32 code=1 (EXCEPTION_DEBUG_EVENT, tid=%x, oob=%d, code=0x%x, 1st=%d]\n",
            pDebugEvent->dwThreadId,
            fOOB,
            pDebugEvent->u.Exception.ExceptionRecord.ExceptionCode,
            pDebugEvent->u.Exception.dwFirstChance);
    }
    else
    {
        STRESS_LOG3(LF_CORDB, LL_EVERYTHING, "[Dispatching Win32 code=%d, tid=%x, oob=%d.]\n",
            pDebugEvent->dwDebugEventCode, pDebugEvent->dwThreadId, fOOB);
    }

}

#define PUBLIC_WIN32_CALLBACK_IN_THIS_SCOPE(_pThis, _pDebugEvent, _fOOB) \
    StressLogNativeDebugEvent(_pDebugEvent, _fOOB); \
    PublicCallbackHolder __pahCallback(DB_IPCE_INVALID_EVENT);

// Visisbility spec for dtors.
// Currently, dtors are like public methods b/c they can be called from Release.
// But they're also reentrant since they may be called from an internal-release.
// @todo - we'd like to get all "useful" work out of the dtor; in which case we may
// be able to change this to something more aggressive.
#define DTOR_ENTRY(_pThis) PUBLIC_REENTRANT_API_ENTRY(_pThis)


//-----------------------------------------------------------------------------
// Typesafe bool for thread safety. This typesafety forces us to use
// an specific reason for thread-safety, taken from a well-known list.
// This is mostly concerned w/ being serialized.
// Note that this assertion must be done on a per function basis and we
// can't have any sort of 'ThreadSafetyReason CallerIsSafe()' b/c we can't
// enforce that all of our callers are thread safe (only that our current caller is safe).
//-----------------------------------------------------------------------------
struct ThreadSafetyReason
{
public:
    ThreadSafetyReason(bool f) { fIsSafe = f; }

    bool fIsSafe;
};

// Different valid reasons that we may be threads safe.
inline ThreadSafetyReason HoldsLock(RSLock * pLock)
{
    _ASSERTE(pLock != NULL);
    return ThreadSafetyReason(pLock->HasLock());
}
inline ThreadSafetyReason OnW32ET(CordbProcess * pProc)
{
    return ThreadSafetyReason(IsWin32EventThread(pProc));
}

inline ThreadSafetyReason OnRCET(Cordb *pCordb)
{
    return ThreadSafetyReason (IsRCEventThread(pCordb));
}

// We use this when we assume that a function is thread-safe (b/c it's serialized).
// The reason also lets us assert that our assumption is true.
// By using a function, we enforce typesafety and thus require a valid reason
// (as opposed to an arbitrary bool)
inline void AssertThreadSafeHelper(ThreadSafetyReason r) {
    _ASSERTE(r.fIsSafe);
}

//-----------------------------------------------------------------------------
// Assert that the given scope is always called on a single thread b/c of
// xReason. Common reasons may be b/c we hold a lock or we're always
// called on a specific thread (Eg w32et).
// The only valid reasons are of type ThreadSafetyReason (thus forcing us to
// choose from a well-known list of valid reasons).
//-----------------------------------------------------------------------------
#define ASSERT_SINGLE_THREAD_ONLY(xReason) \
    AssertThreadSafeHelper(xReason);

#else

//-----------------------------------------------------------------------------
// Retail versions just nop. See the debug implementation for these
// for their semantics.
//-----------------------------------------------------------------------------

#define PUBLIC_CONTRACT
#define PUBLIC_API_ENTRY_FOR_SHIM(_pThis)
#define PUBLIC_API_UNSAFE_ENTRY_FOR_SHIM(_pThis)
#define PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM(_pThis)
#define PUBLIC_API_ENTRY(_pThis)
#define PUBLIC_REENTRANT_API_ENTRY(_pThis)
#define INTERNAL_API_ENTRY(_pThis)
#define INTERNAL_SYNC_API_ENTRY(pProc)
#define INTERNAL_THREAD_ENTRY(_pThis)
#define PUBLIC_CALLBACK_IN_THIS_SCOPE_DEBUGGERERROR(_pThis)
#define PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(_pThis)
#define INTERNAL_DAC_CALLBACK(__pProcess)
#define PUBLIC_WIN32_CALLBACK_IN_THIS_SCOPE(_pThis, _pDebugEvent, _fOOB)
#define DTOR_ENTRY(_pThis)


#define ASSERT_SINGLE_THREAD_ONLY(x)

#endif // #if RSCONTRACTS


class PublicCallbackHolder
{
public:
    PublicCallbackHolder(RSLockHolder * pHolder, DebuggerIPCEventType type)
    {
        m_pHolder = pHolder;
        _ASSERTE(!pHolder->IsNull()); // acquired

        // Release the lock. We'll reacquire it at the dtor.
        m_pHolder->Release();

        Init(type);
    }

    PublicCallbackHolder(DebuggerIPCEventType type)
    {
        m_pHolder = NULL;
        Init(type);
    }

    void Init(DebuggerIPCEventType type)
    {
        m_type = type;

#if defined(RSCONTRACTS)
        // Exiting from RS; entering Cordbg via a callback
        DbgRSThread * pThread = DbgRSThread::GetThread();

        // m_cInsideRS may be arbitrarily large if we're called from a PUBLIC_REENTRANT_API,
        // so just remember the current count and blast it back to 0.
        m_oldCount = pThread->m_cInsideRS;
        pThread->m_cInsideRS = 0;

        _ASSERTE(!pThread->IsInRS());

        // Should never be in public w/ these locks. (Even if we're re-entrant.)
        pThread->AssertThreadIsLockFree();
#endif // RSCONTRACTS
    }

    ~PublicCallbackHolder()
    {
#if defined(RSCONTRACTS)
        // Re-entering RS from after a callback.
        DbgRSThread * pThread = DbgRSThread::GetThread();
        _ASSERTE(!pThread->IsInRS());

        pThread->m_cInsideRS = m_oldCount;

        // Should never be in public w/ these locks. (Even if we're re-entrant.)
        pThread->AssertThreadIsLockFree();
#endif // RSCONTRACTS

        // Reacquire the lock
        if (m_pHolder != NULL)
        {
            m_pHolder->Acquire();
        }
    }
protected:
    int m_oldCount;
    DebuggerIPCEventType m_type;
    RSLockHolder * m_pHolder;
};


// Mark that a thread is calling out via a callback. This will adjust the "Inside RS" counter.
#define PUBLIC_CALLBACK_IN_THIS_SCOPE(_pThis, pLockHolder, event) \
    STRESS_LOG1(LF_CORDB, LL_EVERYTHING, "[Dispatching '%s']\n", IPCENames::GetName((event)->type)); \
    PublicCallbackHolder __pahCallback(pLockHolder, (event)->type);

#define PUBLIC_CALLBACK_IN_THIS_SCOPE1(_pThis, pLockHolder, event, formatLiteralString, arg0) \
    STRESS_LOG2(LF_CORDB, LL_EVERYTHING, "[Dispatching '%s' " formatLiteralString "]\n", IPCENames::GetName((event)->type), arg0); \
    PublicCallbackHolder __pahCallback(pLockHolder, (event)->type);

#define PUBLIC_CALLBACK_IN_THIS_SCOPE2(_pThis, pLockHolder, event, formatLiteralString, arg0, arg1) \
    STRESS_LOG3(LF_CORDB, LL_EVERYTHING, "[Dispatching '%s' " formatLiteralString "]\n", IPCENames::GetName((event)->type), arg0, arg1); \
    PublicCallbackHolder __pahCallback(pLockHolder, (event)->type);

#define PUBLIC_CALLBACK_IN_THIS_SCOPE3(_pThis, pLockHolder, event, formatLiteralString, arg0, arg1, arg2) \
    STRESS_LOG4(LF_CORDB, LL_EVERYTHING, "[Dispatching '%s' " formatLiteralString "]\n", IPCENames::GetName((event)->type), arg0, arg1, arg2); \
    PublicCallbackHolder __pahCallback(pLockHolder, (event)->type);


#define PUBLIC_CALLBACK_IN_THIS_SCOPE0_NO_LOCK(_pThis) \
    PublicCallbackHolder __pahCallback(DB_IPCE_INVALID_EVENT);

#define PUBLIC_CALLBACK_IN_THIS_SCOPE0(_pThis, pLockHolder) \
    PublicCallbackHolder __pahCallback(pLockHolder, DB_IPCE_INVALID_EVENT);


//-----------------------------------------------------------------------------
// Helpers
inline void ValidateOrThrow(const void * p)
{
    if (p == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }
}

// aligns argBase on platforms that require it else it's a no-op
inline void AlignAddressForType(CordbType* pArgType, CORDB_ADDRESS& argBase)
{
#ifdef TARGET_ARM
// TODO: review the following
#ifdef FEATURE_64BIT_ALIGNMENT
    BOOL align = FALSE;
    HRESULT hr = pArgType->RequiresAlign8(&align);
    _ASSERTE(SUCCEEDED(hr));

    if (align)
        argBase = ALIGN_ADDRESS(argBase, 8);
#endif // FEATURE_64BIT_ALIGNMENT
#endif // TARGET_ARM
}

//-----------------------------------------------------------------------------
// Macros to mark public ICorDebug functions
// Usage:
//
//  HRESULT CordbXYZ:Function(...)
//  {
//      HRESULT hr = S_OK;
//      PUBLIC_API_BEGIN(this);
//         // body, may throw
//      PUBLIC_API_END(hr);
//      return hr;
//  }
#define PUBLIC_API_BEGIN(__this) \
    CordbBase * __pThis = (__this); \
    PUBLIC_API_ENTRY(__pThis); \
    EX_TRY { \
       RSLockHolder __lockHolder(__pThis->GetProcess()->GetProcessLock()); \
       THROW_IF_NEUTERED(__pThis); \

// You should not use this in general. We're adding it as a temporary workaround for a
// particular scenario until we do the synchronization feature crew
#define PUBLIC_API_NO_LOCK_BEGIN(__this) \
    CordbBase * __pThis = (__this); \
    PUBLIC_API_ENTRY(__pThis); \
    EX_TRY { \
       THROW_IF_NEUTERED(__pThis); \

// Some APIs (that invoke callbacks), need to toggle the lock.
#define GET_PUBLIC_LOCK_HOLDER() (&__lockHolder)

#define PUBLIC_API_END(__hr) \
    } EX_CATCH_HRESULT(__hr); \

// @todo: clean up API constracts. Should we really be taking the Process lock for
// reentrant APIS??
#define PUBLIC_REENTRANT_API_BEGIN(__this) \
    CordbBase * __pThis = (__this); \
    PUBLIC_REENTRANT_API_ENTRY(__pThis); \
    EX_TRY { \
       RSLockHolder __lockHolder(__pThis->GetProcess()->GetProcessLock()); \
       THROW_IF_NEUTERED(__pThis); \

#define PUBLIC_REENTRANT_API_END(__hr) \
    } EX_CATCH_HRESULT(__hr); \

// If an API needs to take the stop/go lock as well as the process lock, the
// stop/go lock has to be taken first. This is an alternative to PUBLIC_REENTRANT_API_BEGIN
// that allows this, since it doesn't take the process lock. It should be closed with
// PUBLIC_REENTRANT_API_END
#define PUBLIC_REENTRANT_API_NO_LOCK_BEGIN(__this) \
    CordbBase * __pThis = (__this); \
    PUBLIC_REENTRANT_API_ENTRY(__pThis); \
    EX_TRY { \
       THROW_IF_NEUTERED(__pThis); \


//-----------------------------------------------------------------------------
// For debugging ease, cache some global values.
// Include these in retail & free because that's where we need them the most!!
// Optimized builds may not let us view locals & parameters. So Having these
// cached as global values should let us inspect almost all of
// the interesting parts of the RS even in a Retail build!
//-----------------------------------------------------------------------------
struct RSDebuggingInfo
{
    // There should only be 1 global Cordb object. Store it here.
    Cordb * m_Cordb;

    // We have lots of processes. Keep a pointer to the most recently touched
    // (subjective) process, as a hint about what our "current" process is.
    // If we're only debugging 1 process, this will be sufficient.
    CordbProcess * m_MRUprocess;

    CordbRCEventThread * m_RCET;
};

#include "rspriv.inl"

#endif // #if RSPRIV_H
