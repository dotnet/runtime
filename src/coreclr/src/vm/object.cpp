// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// OBJECT.CPP
//
// Definitions of a Com+ Object
//



#include "common.h"

#include "vars.hpp"
#include "class.h"
#include "object.h"
#include "threads.h"
#include "excep.h"
#include "eeconfig.h"
#include "gcheaputilities.h"
#include "field.h"
#include "argdestination.h"


SVAL_IMPL(INT32, ArrayBase, s_arrayBoundsZero);

// follow the necessary rules to get a new valid hashcode for an object
DWORD Object::ComputeHashCode()
{
    DWORD hashCode;
   
    // note that this algorithm now uses at most HASHCODE_BITS so that it will
    // fit into the objheader if the hashcode has to be moved back into the objheader
    // such as for an object that is being frozen
    do
    {
        // we use the high order bits in this case because they're more random
        hashCode = GetThread()->GetNewHashCode() >> (32-HASHCODE_BITS);
    }
    while (hashCode == 0);   // need to enforce hashCode != 0

    // verify that it really fits into HASHCODE_BITS
     _ASSERTE((hashCode & ((1<<HASHCODE_BITS)-1)) == hashCode);

    return hashCode;
}

#ifndef DACCESS_COMPILE    
INT32 Object::GetHashCodeEx()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    // This loop exists because we're inspecting the header dword of the object
    // and it may change under us because of races with other threads.
    // On top of that, it may have the spin lock bit set, in which case we're
    // not supposed to change it.
    // In all of these case, we need to retry the operation.
    DWORD iter = 0;
    DWORD dwSwitchCount = 0;
    while (true)
    {
        DWORD bits = GetHeader()->GetBits();

        if (bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
        {
            if (bits & BIT_SBLK_IS_HASHCODE)
            {
                // Common case: the object already has a hash code
                return  bits & MASK_HASHCODE;
            }
            else
            {
                // We have a sync block index. This means if we already have a hash code,
                // it is in the sync block, otherwise we generate a new one and store it there
                SyncBlock *psb = GetSyncBlock();
                DWORD hashCode = psb->GetHashCode();
                if (hashCode != 0)
                    return  hashCode;

                hashCode = ComputeHashCode();

                return psb->SetHashCode(hashCode);
            }
        }
        else
        {
            // If a thread is holding the thin lock we need a syncblock
            if ((bits & (SBLK_MASK_LOCK_THREADID)) != 0)
            {
                GetSyncBlock();
                // No need to replicate the above code dealing with sync blocks
                // here - in the next iteration of the loop, we'll realize
                // we have a syncblock, and we'll do the right thing.
            }
            else
            {
                // We want to change the header in this case, so we have to check the BIT_SBLK_SPIN_LOCK bit first
                if (bits & BIT_SBLK_SPIN_LOCK)
                {
                    iter++;
                    if ((iter % 1024) != 0 && g_SystemInfo.dwNumberOfProcessors > 1)
                    {
                        YieldProcessorNormalized(); // indicate to the processor that we are spinning
                    }
                    else
                    {
                        __SwitchToThread(0, ++dwSwitchCount);
                    }
                    continue;
                }

                DWORD hashCode = ComputeHashCode();

                DWORD newBits = bits | BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE | hashCode;

                if (GetHeader()->SetBits(newBits, bits) == bits)
                    return hashCode;
                // Header changed under us - let's restart this whole thing.
            }
        }
    }    
}
#endif // #ifndef DACCESS_COMPILE

BOOL Object::ValidateObjectWithPossibleAV()
{
    CANNOT_HAVE_CONTRACT;
    SUPPORTS_DAC;

    return GetGCSafeMethodTable()->ValidateWithPossibleAV();
}


#ifndef DACCESS_COMPILE

TypeHandle Object::GetTrueTypeHandle()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_pMethTab->IsArray())
        return ((ArrayBase*) this)->GetTypeHandle();
    else
        return TypeHandle(GetMethodTable());
}

// There are cases where it is not possible to get a type handle during a GC.
// If we can get the type handle, this method will return it.
// Otherwise, the method will return NULL.
TypeHandle Object::GetGCSafeTypeHandleIfPossible() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if(!IsGCThread()) { MODE_COOPERATIVE; }
    } 
    CONTRACTL_END;

    // Although getting the type handle is unsafe and could cause recursive type lookups
    // in some cases, it's always safe and straightforward to get to the MethodTable.
    MethodTable * pMT = GetGCSafeMethodTable();
    _ASSERTE(pMT != NULL);

    if (pMT == g_pFreeObjectMethodTable)
    {
        return NULL;
    }

    // Don't look at types that belong to an unloading AppDomain, or else
    // pObj->GetGCSafeTypeHandle() can AV. For example, we encountered this AV when pObj
    // was an array like this:
    //     
    //     MyValueType1<MyValueType2>[] myArray
    //
    // where MyValueType1<T> & MyValueType2 are defined in different assemblies. In such
    // a case, looking up the type handle for myArray requires looking in
    // MyValueType1<T>'s module's m_AssemblyRefByNameTable, which is garbage if its
    // AppDomain is unloading.
    // 
    // Another AV was encountered in a similar case,
    // 
    //     MyRefType1<MyRefType2>[] myArray
    // 
    // where MyRefType2's module was unloaded by the time the GC occurred. In at least
    // one case, the GC was caused by the AD unload itself (AppDomain::Unload ->
    // AppDomain::Exit -> GCInterface::AddMemoryPressure -> WKS::GCHeapUtilities::GarbageCollect).
    // 
    // To protect against all scenarios, verify that
    // 
    //     * The MT of the object is not getting unloaded, OR
    //     * In the case of arrays (potentially of arrays of arrays of arrays ...), the
    //         MT of the innermost element is not getting unloaded. This then ensures the
    //         MT of the original object (i.e., array) itself must not be getting
    //         unloaded either, since the MTs of arrays and of their elements are
    //         allocated on the same loader heap, except the case where the array is
    //         Object[], in which case its MT is in mscorlib and thus doesn't unload.

    MethodTable * pMTToCheck = pMT;
    if (pMTToCheck->IsArray())
    {
        TypeHandle thElem = static_cast<const ArrayBase * const>(this)->GetArrayElementTypeHandle();

        // Ideally, we would just call thElem.GetLoaderModule() here. Unfortunately, the
        // current TypeDesc::GetLoaderModule() implementation depends on data structures
        // that might have been unloaded already. So we just simulate
        // TypeDesc::GetLoaderModule() for the limited array case that we care about. In
        // case we're dealing with an array of arrays of arrays etc. traverse until we
        // find the deepest element, and that's the type we'll check
        while (thElem.HasTypeParam()) 
        {
            thElem = thElem.GetTypeParam();
        }

        pMTToCheck = thElem.GetMethodTable();
    }

    Module * pLoaderModule = pMTToCheck->GetLoaderModule();

    // Don't look up types that are unloading due to Collectible Assemblies. Haven't been
    // able to find a case where we actually encounter objects like this that can cause
    // problems; however, it seems prudent to add this protection just in case.
    LoaderAllocator * pLoaderAllocator = pLoaderModule->GetLoaderAllocator();
    _ASSERTE(pLoaderAllocator != NULL);
    if ((pLoaderAllocator->IsCollectible()) &&
        (ObjectHandleIsNull(pLoaderAllocator->GetLoaderAllocatorObjectHandle())))
    {
        return NULL;
    }

    // Ok, it should now be safe to get the type handle
    return GetGCSafeTypeHandle();
}

/* static */ BOOL Object::SupportsInterface(OBJECTREF pObj, MethodTable* pInterfaceMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pInterfaceMT));
        PRECONDITION(pObj->GetMethodTable()->IsRestored_NoLogging());
        PRECONDITION(pInterfaceMT->IsInterface());
    }
    CONTRACTL_END

    BOOL bSupportsItf = FALSE;

    GCPROTECT_BEGIN(pObj)
    {
        // Make sure the interface method table has been restored.
        pInterfaceMT->CheckRestore();

        // Check to see if the static class definition indicates we implement the interface.
        MethodTable * pMT = pObj->GetMethodTable();
        if (pMT->CanCastToInterface(pInterfaceMT))
        {
            bSupportsItf = TRUE;
        }
#ifdef FEATURE_COMINTEROP
        else
        if (pMT->IsComObjectType())
        {
            // If this is a COM object, the static class definition might not be complete so we need
            // to check if the COM object implements the interface.
            bSupportsItf = ComObject::SupportsInterface(pObj, pInterfaceMT);
        }
#endif // FEATURE_COMINTEROP
    }
    GCPROTECT_END();

    return bSupportsItf;
}

Assembly *AssemblyBaseObject::GetAssembly()
{
    WRAPPER_NO_CONTRACT;
    return m_pAssembly->GetAssembly();
}

STRINGREF AllocateString(SString sstr)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;
    
    COUNT_T length = sstr.GetCount(); // count of WCHARs excluding terminating NULL
    STRINGREF strObj = AllocateString(length);
    memcpyNoGCRefs(strObj->GetBuffer(), sstr.GetUnicode(), length*sizeof(WCHAR));

    return strObj;
}

CHARARRAYREF AllocateCharArray(DWORD dwArrayLength)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    return (CHARARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_CHAR, dwArrayLength);
}

void Object::ValidatePromote(ScanContext *sc, DWORD flags)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


#if defined (VERIFY_HEAP)
    Validate();
#endif
}

void Object::ValidateHeap(Object *from, BOOL bDeep)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

#if defined (VERIFY_HEAP)
    //no need to verify next object's header in this case
    //since this is called in verify_heap, which will verfiy every object anyway
    Validate(bDeep, FALSE); 
#endif
}

void Object::SetOffsetObjectRef(DWORD dwOffset, size_t dwValue)
{ 
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    OBJECTREF*  location;
    OBJECTREF   o;

    location = (OBJECTREF *) &GetData()[dwOffset];
    o        = ObjectToOBJECTREF(*(Object **)  &dwValue);

    SetObjectReference( location, o );
}

void SetObjectReferenceUnchecked(OBJECTREF *dst,OBJECTREF ref)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    // Assign value. We use casting to avoid going thru the overloaded
    // OBJECTREF= operator which in this case would trigger a false
    // write-barrier violation assert.
    VolatileStore((Object**)dst, OBJECTREFToObject(ref));
#ifdef _DEBUG
    Thread::ObjectRefAssign(dst);
#endif
    ErectWriteBarrier(dst, ref);
}
    
void STDCALL CopyValueClassUnchecked(void* dest, void* src, MethodTable *pMT) 
{

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    _ASSERTE(!pMT->IsArray());  // bunch of assumptions about arrays wrong. 

    // <TODO> @todo Only call MemoryBarrier() if needed.
    // Reflection is a known use case where this is required.
    // Unboxing is a use case where this should not be required.
    // </TODO>
    MemoryBarrier();

        // Copy the bulk of the data, and any non-GC refs. 
    switch (pMT->GetNumInstanceFieldBytes())
    {        
    case 1:
        *(UINT8*)dest = *(UINT8*)src;
        break;
#ifndef ALIGN_ACCESS
        // we can hit an alignment fault if the value type has multiple 
        // smaller fields.  Example: if there are two I4 fields, the 
        // value class can be aligned to 4-byte boundaries, yet the 
        // NumInstanceFieldBytes is 8
    case 2:
        *(UINT16*)dest = *(UINT16*)src;
        break;
    case 4:
        *(UINT32*)dest = *(UINT32*)src;
        break;
    case 8:
        *(UINT64*)dest = *(UINT64*)src;
        break;
#endif // !ALIGN_ACCESS
    default:
        memcpyNoGCRefs(dest, src, pMT->GetNumInstanceFieldBytes());
        break;
    }

        // Tell the GC about any copies.  
    if (pMT->ContainsPointers())
    {   
        CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
        CGCDescSeries* cur = map->GetHighestSeries();
        CGCDescSeries* last = map->GetLowestSeries();
        DWORD size = pMT->GetBaseSize();
        _ASSERTE(cur >= last);
        do                                                                  
        {   
            // offset to embedded references in this series must be
            // adjusted by the VTable pointer, when in the unboxed state.
            size_t offset = cur->GetSeriesOffset() - sizeof(void*);
            OBJECTREF* srcPtr = (OBJECTREF*)(((BYTE*) src) + offset);
            OBJECTREF* destPtr = (OBJECTREF*)(((BYTE*) dest) + offset);
            OBJECTREF* srcPtrStop = (OBJECTREF*)((BYTE*) srcPtr + cur->GetSeriesSize() + size);         
            while (srcPtr < srcPtrStop)                                         
            {   
                SetObjectReference(destPtr, ObjectToOBJECTREF(*(Object**)srcPtr));
                srcPtr++;
                destPtr++;
            }                                                               
            cur--;                                                              
        } while (cur >= last);                                              
    }
}

// Copy value class into the argument specified by the argDest.
// The destOffset is nonzero when copying values into Nullable<T>, it is the offset
// of the T value inside of the Nullable<T>
void STDCALL CopyValueClassArgUnchecked(ArgDestination *argDest, void* src, MethodTable *pMT, int destOffset) 
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

#if defined(UNIX_AMD64_ABI)

    if (argDest->IsStructPassedInRegs())
    {
        argDest->CopyStructToRegisters(src, pMT->GetNumInstanceFieldBytes(), destOffset);
        return;
    }

#elif defined(_TARGET_ARM64_)

    if (argDest->IsHFA())
    {
        argDest->CopyHFAStructToRegister(src, pMT->GetAlignedNumInstanceFieldBytes());
        return;
    }

#endif // UNIX_AMD64_ABI
    // destOffset is only valid for Nullable<T> passed in registers
    _ASSERTE(destOffset == 0);

    CopyValueClassUnchecked(argDest->GetDestinationAddress(), src, pMT);
}

// Initialize the value class argument to zeros
void InitValueClassArg(ArgDestination *argDest, MethodTable *pMT)
{ 
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

#if defined(UNIX_AMD64_ABI)

    if (argDest->IsStructPassedInRegs())
    {
        argDest->ZeroStructInRegisters(pMT->GetNumInstanceFieldBytes());
        return;
    }

#endif    
    InitValueClass(argDest->GetDestinationAddress(), pMT);
}

#if defined (VERIFY_HEAP)

#include "dbginterface.h"

    // make the checking code goes as fast as possible!
#if defined(_MSC_VER)
#pragma optimize("tgy", on)
#endif

#define CREATE_CHECK_STRING(x) #x
#define CHECK_AND_TEAR_DOWN(x)                                      \
    do{                                                             \
        if (!(x))                                                   \
        {                                                           \
            _ASSERTE(!CREATE_CHECK_STRING(x));                      \
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);     \
        }                                                           \
    } while (0)

VOID Object::Validate(BOOL bDeep, BOOL bVerifyNextHeader, BOOL bVerifySyncBlock)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (this == NULL)
    {
        return;     // NULL is ok
    }

    if (g_IBCLogger.InstrEnabled() && !GCStress<cfg_any>::IsEnabled())
    {
        // If we are instrumenting for IBC (and GCStress is not enabled)
        // then skip these Object::Validate() as they slow down the
        // instrument phase by an order of magnitude
        return;
    }

    if (g_fEEShutDown & ShutDown_Phase2)
    {
        // During second phase of shutdown the code below is not guaranteed to work.
        return;
    }

#ifdef _DEBUG
    {
        BEGIN_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;
        Thread *pThread = GetThread();

        if (pThread != NULL && !(pThread->PreemptiveGCDisabled()))
        {
            // Debugger helper threads are special in that they take over for
            // what would normally be a nonEE thread (the RCThread).  If an
            // EE thread is doing RCThread duty, then it should be treated
            // as such.
            //
            // There are some GC threads in the same kind of category.  Note that
            // GetThread() sometimes returns them, if DLL_THREAD_ATTACH notifications
            // have run some managed code.
            if (!dbgOnly_IsSpecialEEThread() && !IsGCSpecialThread())
                _ASSERTE(!"OBJECTREF being accessed while thread is in preemptive GC mode.");
        }
        END_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;
    }
#endif


    {   // ValidateInner can throw or fault on failure which violates contract.
        CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);

        // using inner helper because of TRY and stack objects with destructors.
        ValidateInner(bDeep, bVerifyNextHeader, bVerifySyncBlock);
    }
}

VOID Object::ValidateInner(BOOL bDeep, BOOL bVerifyNextHeader, BOOL bVerifySyncBlock)
{
    STATIC_CONTRACT_THROWS; // See CONTRACT_VIOLATION above
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT; // See CONTRACT_VIOLATION above
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    int lastTest = 0;

    EX_TRY
    {
        // in order to avoid contract violations in the EH code we'll allow AVs here, 
        // they'll be handled in the catch block
        AVInRuntimeImplOkayHolder avOk;

        MethodTable *pMT = GetGCSafeMethodTable();

        lastTest = 1;

        CHECK_AND_TEAR_DOWN(pMT && pMT->Validate());
        lastTest = 2;

        bool noRangeChecks =
            (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_NO_RANGE_CHECKS) == EEConfig::HEAPVERIFY_NO_RANGE_CHECKS;

        // noRangeChecks depends on initial values being FALSE
        BOOL bSmallObjectHeapPtr = FALSE, bLargeObjectHeapPtr = FALSE;
        if (!noRangeChecks)
        {
            bSmallObjectHeapPtr = GCHeapUtilities::GetGCHeap()->IsHeapPointer(this, true);
            if (!bSmallObjectHeapPtr)
                bLargeObjectHeapPtr = GCHeapUtilities::GetGCHeap()->IsHeapPointer(this);
                
            CHECK_AND_TEAR_DOWN(bSmallObjectHeapPtr || bLargeObjectHeapPtr);
        }

        lastTest = 3;

        if (bDeep)
        {
            CHECK_AND_TEAR_DOWN(GetHeader()->Validate(bVerifySyncBlock));
        }
        
        lastTest = 4;

        if (bDeep && (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC)) {
            GCHeapUtilities::GetGCHeap()->ValidateObjectMember(this);
        }

        lastTest = 5;

        // since bSmallObjectHeapPtr is initialized to FALSE
        // we skip checking noRangeChecks since if skipping
        // is enabled bSmallObjectHeapPtr will always be false.
        if (bSmallObjectHeapPtr) {
            CHECK_AND_TEAR_DOWN(!GCHeapUtilities::GetGCHeap()->IsObjectInFixedHeap(this));
        }

        lastTest = 6;

        lastTest = 7;

        _ASSERTE(GCHeapUtilities::IsGCHeapInitialized());
        // try to validate next object's header
        if (bDeep 
            && bVerifyNextHeader 
            && GCHeapUtilities::GetGCHeap()->RuntimeStructuresValid()
            //NextObj could be very slow if concurrent GC is going on
            && !GCHeapUtilities::GetGCHeap ()->IsConcurrentGCInProgress ())
        {
            Object * nextObj = GCHeapUtilities::GetGCHeap ()->NextObj (this);
            if ((nextObj != NULL) &&
                (nextObj->GetGCSafeMethodTable() != g_pFreeObjectMethodTable))
            {
                // we need a read barrier here - to make sure we read the object header _after_                
                // reading data that tells us that the object is eligible for verification
                // (also see: gc.cpp/a_fit_segment_end_p)
                VOLATILE_MEMORY_BARRIER();
                CHECK_AND_TEAR_DOWN(nextObj->GetHeader()->Validate(FALSE));
            }
        }

        lastTest = 8;

#ifdef FEATURE_64BIT_ALIGNMENT
        if (pMT->RequiresAlign8())
        {
            CHECK_AND_TEAR_DOWN((((size_t)this) & 0x7) == (pMT->IsValueType()? 4:0));
        }
        lastTest = 9;
#endif // FEATURE_64BIT_ALIGNMENT

    }
    EX_CATCH
    {
        STRESS_LOG3(LF_ASSERT, LL_ALWAYS, "Detected use of corrupted OBJECTREF: %p [MT=%p] (lastTest=%d)", this, lastTest > 0 ? (*(size_t*)this) : 0, lastTest);
        CHECK_AND_TEAR_DOWN(!"Detected use of a corrupted OBJECTREF. Possible GC hole.");
    }
    EX_END_CATCH(SwallowAllExceptions);
}


#endif   // VERIFY_HEAP

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
void ArrayBase::AssertArrayTypeDescLoaded()
{
    _ASSERTE (m_pMethTab->IsArray());

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // The type should already be loaded
    // See also: MethodTable::DoFullyLoad
    TypeHandle th = ClassLoader::LoadArrayTypeThrowing(m_pMethTab->GetApproxArrayElementTypeHandle(),
                                                       m_pMethTab->GetInternalCorElementType(),
                                                       m_pMethTab->GetRank(),
                                                       ClassLoader::DontLoadTypes);

    _ASSERTE(!th.IsNull());
}
#endif // DEBUG
#endif // !DACCESS_COMPILE

/*==================================NewString===================================
**Action:  Creates a System.String object.
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
STRINGREF StringObject::NewString(INT32 length) {
    CONTRACTL {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(length>=0);
    } CONTRACTL_END;

    STRINGREF pString;

    if (length<0) {
        return NULL;
    } else if (length == 0) {
        return GetEmptyString();
    } else {
        pString = AllocateString(length);
        _ASSERTE(pString->GetBuffer()[length] == 0);

        return pString;
    }
}


/*==================================NewString===================================
**Action: Many years ago, VB didn't have the concept of a byte array, so enterprising
**        users created one by allocating a BSTR with an odd length and using it to
**        store bytes.  A generation later, we're still stuck supporting this behavior.
**        The way that we do this is to take advantage of the difference between the
**        array length and the string length.  The string length will always be the
**        number of characters between the start of the string and the terminating 0.
**        If we need an odd number of bytes, we'll take one wchar after the terminating 0.
**        (e.g. at position StringLength+1).  The high-order byte of this wchar is
**        reserved for flags and the low-order byte is our odd byte. This function is
**        used to allocate a string of that shape, but we don't actually mark the
**        trailing byte as being in use yet.
**Returns: A newly allocated string.  Null if length is less than 0.
**Arguments: length -- the length of the string to allocate
**           bHasTrailByte -- whether the string also has a trailing byte.
**Exceptions: OutOfMemoryException if AllocateString fails.
==============================================================================*/
STRINGREF StringObject::NewString(INT32 length, BOOL bHasTrailByte) {
    CONTRACTL {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(length>=0 && length != INT32_MAX);
    } CONTRACTL_END;

    STRINGREF pString;
    if (length<0 || length == INT32_MAX) {
        return NULL;
    } else if (length == 0) {
        return GetEmptyString();
    } else {
        pString = AllocateString(length);
        _ASSERTE(pString->GetBuffer()[length]==0);
        if (bHasTrailByte) {
            _ASSERTE(pString->GetBuffer()[length+1]==0);
        }
    }

    return pString;
}

//========================================================================
// Creates a System.String object and initializes from
// the supplied null-terminated C string.
//
// Maps NULL to null. This function does *not* return null to indicate
// error situations: it throws an exception instead.
//========================================================================
STRINGREF StringObject::NewString(const WCHAR *pwsz)
{
    CONTRACTL {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    if (!pwsz)
    {
        return NULL;
    }
    else
    {

        DWORD nch = (DWORD)wcslen(pwsz);
        if (nch==0) {
            return GetEmptyString();
        }

#if 0        
        //        
        // This assert is disabled because it is valid for us to get a 
        // pointer from the gc heap here as long as it is pinned.  This
        // can happen when a string is marshalled to unmanaged by 
        // pinning and then later put into a struct and that struct is
        // then marshalled to managed.  
        //
        _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsHeapPointer((BYTE *) pwsz) ||
                 !"pwsz can not point to GC Heap");
#endif // 0

        STRINGREF pString = AllocateString( nch );

        memcpyNoGCRefs(pString->GetBuffer(), pwsz, nch*sizeof(WCHAR));
        _ASSERTE(pString->GetBuffer()[nch] == 0);
        return pString;
    }
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("y", on)        // Small critical routines, don't put in EBP frame 
#endif

STRINGREF StringObject::NewString(const WCHAR *pwsz, int length) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(length>=0);
    } CONTRACTL_END;

    if (!pwsz)
    {
        return NULL;
    }
    else if (length <= 0) {
        return GetEmptyString();
    } else {
#if 0        
        //        
        // This assert is disabled because it is valid for us to get a 
        // pointer from the gc heap here as long as it is pinned.  This
        // can happen when a string is marshalled to unmanaged by 
        // pinning and then later put into a struct and that struct is
        // then marshalled to managed.  
        //
        _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsHeapPointer((BYTE *) pwsz) ||
                 !"pwsz can not point to GC Heap");
#endif // 0
        STRINGREF pString = AllocateString(length);

        memcpyNoGCRefs(pString->GetBuffer(), pwsz, length*sizeof(WCHAR));
        _ASSERTE(pString->GetBuffer()[length] == 0);
        return pString;
    }
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("", on)        // Go back to command line default optimizations
#endif

STRINGREF StringObject::NewString(LPCUTF8 psz)
{
    CONTRACTL {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
        PRECONDITION(CheckPointer(psz));
    } CONTRACTL_END;

    int length = (int)strlen(psz);
    if (length == 0) {
        return GetEmptyString();
    }
    CQuickBytes qb;
    WCHAR* pwsz = (WCHAR*) qb.AllocThrows((length) * sizeof(WCHAR));
    length = WszMultiByteToWideChar(CP_UTF8, 0, psz, length, pwsz, length);
    if (length == 0) {
        COMPlusThrow(kArgumentException, W("Arg_InvalidUTF8String"));
    }
    return NewString(pwsz, length);
}

STRINGREF StringObject::NewString(LPCUTF8 psz, int cBytes)
{
    CONTRACTL {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
        PRECONDITION(CheckPointer(psz, NULL_OK));
    } CONTRACTL_END;

    if (!psz)
        return NULL;

    _ASSERTE(psz);
    _ASSERTE(cBytes >= 0);
    if (cBytes == 0) {
        return GetEmptyString();
    }
    int cWszBytes = 0;
    if (!ClrSafeInt<int>::multiply(cBytes, sizeof(WCHAR), cWszBytes))
        COMPlusThrowOM();
    CQuickBytes qb;
    WCHAR* pwsz = (WCHAR*) qb.AllocThrows(cWszBytes);
    int length = WszMultiByteToWideChar(CP_UTF8, 0, psz, cBytes, pwsz, cBytes);
    if (length == 0) {
        COMPlusThrow(kArgumentException, W("Arg_InvalidUTF8String"));
    }
    return NewString(pwsz, length);
}

//
//
// STATIC MEMBER VARIABLES
//
//
STRINGREF* StringObject::EmptyStringRefPtr=NULL;

//The special string helpers are used as flag bits for weird strings that have bytes
//after the terminating 0.  The only case where we use this right now is the VB BSTR as
//byte array which is described in MakeStringAsByteArrayFromBytes.
#define SPECIAL_STRING_VB_BYTE_ARRAY 0x100

FORCEINLINE BOOL MARKS_VB_BYTE_ARRAY(WCHAR x)
{
    return static_cast<BOOL>(x & SPECIAL_STRING_VB_BYTE_ARRAY);
}

FORCEINLINE WCHAR MAKE_VB_TRAIL_BYTE(BYTE x)
{
    return static_cast<WCHAR>(x) | SPECIAL_STRING_VB_BYTE_ARRAY;
}

FORCEINLINE BYTE GET_VB_TRAIL_BYTE(WCHAR x)
{
    return static_cast<BYTE>(x & 0xFF);
}


/*==============================InitEmptyStringRefPtr============================
**Action:  Gets an empty string refptr, cache the result.
**Returns: The retrieved STRINGREF.
==============================================================================*/
STRINGREF* StringObject::InitEmptyStringRefPtr() {
    CONTRACTL {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    } CONTRACTL_END;

    GCX_COOP();

    EEStringData data(0, W(""), TRUE);
    EmptyStringRefPtr = SystemDomain::System()->DefaultDomain()->GetLoaderAllocator()->GetStringObjRefPtrFromUnicodeString(&data);
    return EmptyStringRefPtr;
}

// strAChars must be null-terminated, with an appropriate aLength
// strBChars must be null-terminated, with an appropriate bLength OR bLength == -1
// If bLength == -1, we stop on the first null character in strBChars
BOOL StringObject::CaseInsensitiveCompHelper(__in_ecount(aLength) WCHAR *strAChars, __in_z INT8 *strBChars, INT32 aLength, INT32 bLength, INT32 *result) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(strAChars));
        PRECONDITION(CheckPointer(strBChars));
        PRECONDITION(CheckPointer(result));
    } CONTRACTL_END;

    WCHAR *strAStart = strAChars;
    INT8  *strBStart = strBChars;
    unsigned charA;
    unsigned charB;

    for(;;) {        
        charA = *strAChars;
        charB = (unsigned) *strBChars;

        //Case-insensitive comparison on chars greater than 0x7F
        //requires a locale-aware casing operation and we're not going there.
        if ((charA|charB)>0x7F) {
            *result = 0;
            return FALSE;
        }

        // uppercase both chars. 
        if (charA>='a' && charA<='z') {
            charA ^= 0x20;
        } 
        if (charB>='a' && charB<='z') {
            charB ^= 0x20;
        }

        //Return the (case-insensitive) difference between them.
        if (charA!=charB) {
            *result = (int)(charA-charB);
            return TRUE;
        }


        if (charA==0)   // both strings have null character
        {
            if (bLength == -1)
            {
                *result = aLength - static_cast<INT32>(strAChars - strAStart); 
                return TRUE;
            }
            if (strAChars==strAStart + aLength || strBChars==strBStart + bLength)
            {
                *result = aLength - bLength; 
                return TRUE;
            }
            // else both embedded zeros
        } 

        // Next char
        strAChars++; strBChars++;
    }
    
}

/*=============================InternalHasHighChars=============================
**Action:  Checks if the string can be sorted quickly.  The requirements are that
**         the string contain no character greater than 0x80 and that the string not
**         contain an apostrophe or a hypen.  Apostrophe and hyphen are excluded so that
**         words like co-op and coop sort together.
**Returns: Void.  The side effect is to set a bit on the string indicating whether or not
**         the string contains high chars.
**Arguments: The String to be checked.
**Exceptions: None
==============================================================================*/
DWORD StringObject::InternalCheckHighChars() {
    WRAPPER_NO_CONTRACT;

    WCHAR *chars;
    WCHAR c;
    INT32 length;

    RefInterpretGetStringValuesDangerousForGC((WCHAR **) &chars, &length);

    DWORD stringState = STRING_STATE_FAST_OPS;

    for (int i=0; i<length; i++) {
        c = chars[i];
        if (c>=0x80) {
            SetHighCharState(STRING_STATE_HIGH_CHARS);
            return STRING_STATE_HIGH_CHARS;
        } else if (HighCharHelper::IsHighChar((int)c)) {
            //This means that we have a character which forces special sorting,
            //but doesn't necessarily force slower casing and indexing.  We'll
            //set a value to remember this, but we need to check the rest of
            //the string because we may still find a charcter greater than 0x7f.
            stringState = STRING_STATE_SPECIAL_SORT;
        }
    }

    SetHighCharState(stringState);
    return stringState;
}

#ifdef VERIFY_HEAP
/*=============================ValidateHighChars=============================
**Action:  Validate if the HighChars bits is set correctly, no side effect
**Returns: BOOL for result of validation
**Arguments: The String to be checked.
**Exceptions: None
==============================================================================*/
BOOL StringObject::ValidateHighChars()
{
    WRAPPER_NO_CONTRACT;
    DWORD curStringState = GetHighCharState ();
    // state could always be undetermined
    if (curStringState == STRING_STATE_UNDETERMINED)
    {
        return TRUE;
    }

    WCHAR *chars;
    INT32 length;
    RefInterpretGetStringValuesDangerousForGC((WCHAR **) &chars, &length);

    DWORD stringState = STRING_STATE_FAST_OPS;
    for (int i=0; i<length; i++) {
        WCHAR c = chars[i];
        if (c>=0x80) 
        {
            // if there is a high char in the string, the state has to be STRING_STATE_HIGH_CHARS
            return curStringState == STRING_STATE_HIGH_CHARS;
        } 
        else if (HighCharHelper::IsHighChar((int)c)) {
            //This means that we have a character which forces special sorting,
            //but doesn't necessarily force slower casing and indexing.  We'll
            //set a value to remember this, but we need to check the rest of
            //the string because we may still find a charcter greater than 0x7f.
            stringState = STRING_STATE_SPECIAL_SORT;
        }
    }
    
    return stringState == curStringState;
}

#endif //VERIFY_HEAP

/*============================InternalTrailByteCheck============================
**Action: Many years ago, VB didn't have the concept of a byte array, so enterprising
**        users created one by allocating a BSTR with an odd length and using it to
**        store bytes.  A generation later, we're still stuck supporting this behavior.
**        The way that we do this is stick the trail byte in the sync block
**        whenever we encounter such a situation. Since we expect this to be a very corner case
**        accessing the sync block seems like a good enough solution
**
**Returns: True if <CODE>str</CODE> contains a VB trail byte, false otherwise.
**Arguments: str -- The string to be examined.
**Exceptions: None
==============================================================================*/
BOOL StringObject::HasTrailByte() {
    WRAPPER_NO_CONTRACT;
    
    SyncBlock * pSyncBlock = PassiveGetSyncBlock();
    if(pSyncBlock != NULL)
    {
        return pSyncBlock->HasCOMBstrTrailByte();
    }

    return FALSE;
}

/*=================================GetTrailByte=================================
**Action:  If <CODE>str</CODE> contains a vb trail byte, returns a copy of it.
**Returns: True if <CODE>str</CODE> contains a trail byte.  *bTrailByte is set to
**         the byte in question if <CODE>str</CODE> does have a trail byte, otherwise
**         it's set to 0.
**Arguments: str -- The string being examined.
**           bTrailByte -- An out param to hold the value of the trail byte.
**Exceptions: None.
==============================================================================*/
BOOL StringObject::GetTrailByte(BYTE *bTrailByte) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(bTrailByte);
    *bTrailByte=0;

    BOOL retValue = HasTrailByte();

    if(retValue)
    {
        *bTrailByte = GET_VB_TRAIL_BYTE(GetHeader()->PassiveGetSyncBlock()->GetCOMBstrTrailByte());
    }

    return retValue;
}

/*=================================SetTrailByte=================================
**Action: Sets the trail byte in the sync block
**Returns: True.
**Arguments: str -- The string into which to set the trail byte.
**           bTrailByte -- The trail byte to be added to the string.
**Exceptions: None.
==============================================================================*/
BOOL StringObject::SetTrailByte(BYTE bTrailByte) {
    WRAPPER_NO_CONTRACT;

    GetHeader()->GetSyncBlock()->SetCOMBstrTrailByte(MAKE_VB_TRAIL_BYTE(bTrailByte));
    return TRUE;
}

#ifdef USE_CHECKED_OBJECTREFS

//-------------------------------------------------------------
// Default constructor, for non-initializing declarations:
//
//      OBJECTREF or;
//-------------------------------------------------------------
OBJECTREF::OBJECTREF()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    m_asObj = (Object*)POISONC;
    Thread::ObjectRefNew(this);
}

//-------------------------------------------------------------
// Copy constructor, for passing OBJECTREF's as function arguments.
//-------------------------------------------------------------
OBJECTREF::OBJECTREF(const OBJECTREF & objref)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_FORBID_FAULT;

    VALIDATEOBJECT(objref.m_asObj);

    // !!! If this assert is fired, there are two possibilities:
    // !!! 1.  You are doing a type cast, e.g.  *(OBJECTREF*)pObj
    // !!!     Instead, you should use ObjectToOBJECTREF(*(Object**)pObj),
    // !!!                          or ObjectToSTRINGREF(*(StringObject**)pObj)
    // !!! 2.  There is a real GC hole here.
    // !!! Either way you need to fix the code.
    _ASSERTE(Thread::IsObjRefValid(&objref));
    if ((objref.m_asObj != 0) &&
        ((IGCHeap*)GCHeapUtilities::GetGCHeap())->IsHeapPointer( (BYTE*)this ))
    {
        _ASSERTE(!"Write Barrier violation. Must use SetObjectReference() to assign OBJECTREF's into the GC heap!");
    }
    m_asObj = objref.m_asObj;
    
    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }

    Thread::ObjectRefNew(this);
}


//-------------------------------------------------------------
// To allow NULL to be used as an OBJECTREF.
//-------------------------------------------------------------
OBJECTREF::OBJECTREF(TADDR nul)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    //_ASSERTE(nul == 0);
    m_asObj = (Object*)nul;
    if( m_asObj != NULL)
    {
        // REVISIT_TODO: fix this, why is this constructor being used for non-null object refs?
        STATIC_CONTRACT_VIOLATION(ModeViolation);

        VALIDATEOBJECT(m_asObj);
        ENABLESTRESSHEAP();
    }
    Thread::ObjectRefNew(this);
}

//-------------------------------------------------------------
// This is for the GC's use only. Non-GC code should never
// use the "Object" class directly. The unused "int" argument
// prevents C++ from using this to implicitly convert Object*'s
// to OBJECTREF.
//-------------------------------------------------------------
OBJECTREF::OBJECTREF(Object *pObject)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_FORBID_FAULT;

    DEBUG_ONLY_FUNCTION;
    
    if ((pObject != 0) &&
        ((IGCHeap*)GCHeapUtilities::GetGCHeap())->IsHeapPointer( (BYTE*)this ))
    {
        _ASSERTE(!"Write Barrier violation. Must use SetObjectReference() to assign OBJECTREF's into the GC heap!");
    }
    m_asObj = pObject;
    VALIDATEOBJECT(m_asObj);
    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }
    Thread::ObjectRefNew(this);
}

void OBJECTREF::Validate(BOOL bDeep, BOOL bVerifyNextHeader, BOOL bVerifySyncBlock)
{
    LIMITED_METHOD_CONTRACT;
    m_asObj->Validate(bDeep, bVerifyNextHeader, bVerifySyncBlock);
}

//-------------------------------------------------------------
// Test against NULL.
//-------------------------------------------------------------
int OBJECTREF::operator!() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // We don't do any validation here, as we want to allow zero comparison in preemptive mode
    return !m_asObj;
}

//-------------------------------------------------------------
// Compare two OBJECTREF's.
//-------------------------------------------------------------
int OBJECTREF::operator==(const OBJECTREF &objref) const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (objref.m_asObj != NULL) // Allow comparison to zero in preemptive mode
    {
        // REVISIT_TODO: Weakening the contract system a little bit here. We should really
        // add a special NULLOBJECTREF which can be used for these situations and have
        // a seperate code path for that with the correct contract protections.
        STATIC_CONTRACT_VIOLATION(ModeViolation);

        VALIDATEOBJECT(objref.m_asObj);

        // !!! If this assert is fired, there are two possibilities:
        // !!! 1.  You are doing a type cast, e.g.  *(OBJECTREF*)pObj
        // !!!     Instead, you should use ObjectToOBJECTREF(*(Object**)pObj),
        // !!!                          or ObjectToSTRINGREF(*(StringObject**)pObj)
        // !!! 2.  There is a real GC hole here.
        // !!! Either way you need to fix the code.
        _ASSERTE(Thread::IsObjRefValid(&objref));
        VALIDATEOBJECT(m_asObj);
        // If this assert fires, you probably did not protect
        // your OBJECTREF and a GC might have occurred.  To
        // where the possible GC was, set a breakpoint in Thread::TriggersGC 
        _ASSERTE(Thread::IsObjRefValid(this));

        if (m_asObj != 0 || objref.m_asObj != 0) {
            ENABLESTRESSHEAP();
        }
    }
    return m_asObj == objref.m_asObj;
}

//-------------------------------------------------------------
// Compare two OBJECTREF's.
//-------------------------------------------------------------
int OBJECTREF::operator!=(const OBJECTREF &objref) const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (objref.m_asObj != NULL)  // Allow comparison to zero in preemptive mode
    {
        // REVISIT_TODO: Weakening the contract system a little bit here. We should really
        // add a special NULLOBJECTREF which can be used for these situations and have
        // a seperate code path for that with the correct contract protections.
        STATIC_CONTRACT_VIOLATION(ModeViolation);

        VALIDATEOBJECT(objref.m_asObj);

        // !!! If this assert is fired, there are two possibilities:
        // !!! 1.  You are doing a type cast, e.g.  *(OBJECTREF*)pObj
        // !!!     Instead, you should use ObjectToOBJECTREF(*(Object**)pObj),
        // !!!                          or ObjectToSTRINGREF(*(StringObject**)pObj)
        // !!! 2.  There is a real GC hole here.
        // !!! Either way you need to fix the code.
        _ASSERTE(Thread::IsObjRefValid(&objref));
        VALIDATEOBJECT(m_asObj);
        // If this assert fires, you probably did not protect
        // your OBJECTREF and a GC might have occurred.  To
        // where the possible GC was, set a breakpoint in Thread::TriggersGC 
        _ASSERTE(Thread::IsObjRefValid(this));

        if (m_asObj != 0 || objref.m_asObj != 0) {
            ENABLESTRESSHEAP();
        }
    }

    return m_asObj != objref.m_asObj;
}


//-------------------------------------------------------------
// Forward method calls.
//-------------------------------------------------------------
Object* OBJECTREF::operator->()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    VALIDATEOBJECT(m_asObj);
        // If this assert fires, you probably did not protect
        // your OBJECTREF and a GC might have occurred.  To
        // where the possible GC was, set a breakpoint in Thread::TriggersGC 
    _ASSERTE(Thread::IsObjRefValid(this));

    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }

    // if you are using OBJECTREF directly,
    // you probably want an Object *
    return (Object *)m_asObj;
}


//-------------------------------------------------------------
// Forward method calls.
//-------------------------------------------------------------
const Object* OBJECTREF::operator->() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    VALIDATEOBJECT(m_asObj);
        // If this assert fires, you probably did not protect
        // your OBJECTREF and a GC might have occurred.  To
        // where the possible GC was, set a breakpoint in Thread::TriggersGC 
    _ASSERTE(Thread::IsObjRefValid(this));

    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }

    // if you are using OBJECTREF directly,
    // you probably want an Object *
    return (Object *)m_asObj;
}


//-------------------------------------------------------------
// Assignment. We don't validate the destination so as not
// to break the sequence:
//
//      OBJECTREF or;
//      or = ...;
//-------------------------------------------------------------
OBJECTREF& OBJECTREF::operator=(const OBJECTREF &objref)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    VALIDATEOBJECT(objref.m_asObj);

    // !!! If this assert is fired, there are two possibilities:
    // !!! 1.  You are doing a type cast, e.g.  *(OBJECTREF*)pObj
    // !!!     Instead, you should use ObjectToOBJECTREF(*(Object**)pObj),
    // !!!                          or ObjectToSTRINGREF(*(StringObject**)pObj)
    // !!! 2.  There is a real GC hole here.
    // !!! Either way you need to fix the code.
    _ASSERTE(Thread::IsObjRefValid(&objref));

    if ((objref.m_asObj != 0) &&
        ((IGCHeap*)GCHeapUtilities::GetGCHeap())->IsHeapPointer( (BYTE*)this ))
    {
        _ASSERTE(!"Write Barrier violation. Must use SetObjectReference() to assign OBJECTREF's into the GC heap!");
    }
    Thread::ObjectRefAssign(this);

    m_asObj = objref.m_asObj;
    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }
    return *this;
}

//-------------------------------------------------------------
// Allows for the assignment of NULL to a OBJECTREF 
//-------------------------------------------------------------

OBJECTREF& OBJECTREF::operator=(TADDR nul)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(nul == 0);
    Thread::ObjectRefAssign(this);
    m_asObj = (Object*)nul;
    if (m_asObj != 0) {
        ENABLESTRESSHEAP();
    }
    return *this;
}
#endif  // DEBUG

#ifdef _DEBUG

void* __cdecl GCSafeMemCpy(void * dest, const void * src, size_t len)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (!(((*(BYTE**)&dest) <  g_lowest_address ) ||
          ((*(BYTE**)&dest) >= g_highest_address)))
    {
        Thread* pThread = GetThread();

        // GCHeapUtilities::IsHeapPointer has race when called in preemptive mode. It walks the list of segments
        // that can be modified by GC. Do the check below only if it is safe to do so.
        if (pThread != NULL && pThread->PreemptiveGCDisabled())
        {
            // Note there is memcpyNoGCRefs which will allow you to do a memcpy into the GC
            // heap if you really know you don't need to call the write barrier

            _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsHeapPointer((BYTE *) dest) ||
                     !"using memcpy to copy into the GC heap, use CopyValueClass");
        }
    }
    return memcpyNoGCRefs(dest, src, len);
}

#endif // _DEBUG

// This function clears a piece of memory in a GC safe way.  It makes the guarantee
// that it will clear memory in at least pointer sized chunks whenever possible.
// Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
// We must make this guarantee whenever we clear memory in the GC heap that could contain 
// object references.  The GC or other user threads can read object references at any time, 
// clearing them bytewise can result in a read on another thread getting incorrect data.  
void __fastcall ZeroMemoryInGCHeap(void* mem, size_t size)
{
    WRAPPER_NO_CONTRACT;
    BYTE* memBytes = (BYTE*) mem;
    BYTE* endBytes = &memBytes[size];

    // handle unaligned bytes at the beginning
    while (!IS_ALIGNED(memBytes, sizeof(PTR_PTR_VOID)) && memBytes < endBytes)
        *memBytes++ = 0;

    // now write pointer sized pieces
    // volatile ensures that this doesn't get optimized back into a memset call
    size_t nPtrs = (endBytes - memBytes) / sizeof(PTR_PTR_VOID);
    PTR_VOID volatile * memPtr = (PTR_PTR_VOID) memBytes;
    for (size_t i = 0; i < nPtrs; i++)
        *memPtr++ = 0;

    // handle remaining bytes at the end
    memBytes = (BYTE*) memPtr;
    while (memBytes < endBytes)
        *memBytes++ = 0;
}

void StackTraceArray::Append(StackTraceElement const * begin, StackTraceElement const * end)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame((OBJECTREF*)this));
    }
    CONTRACTL_END;

    // ensure that only one thread can write to the array
    EnsureThreadAffinity();

    size_t newsize = Size() + (end - begin);
    Grow(newsize);
    memcpyNoGCRefs(GetData() + Size(), begin, (end - begin) * sizeof(StackTraceElement));
    MemoryBarrier();  // prevent the newsize from being reordered with the array copy
    SetSize(newsize);

#if defined(_DEBUG)
    CheckState();
#endif
}

void StackTraceArray::CheckState() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    if (!m_array)
        return;

    assert(GetObjectThread() == GetThread());
    
    size_t size = Size();
    StackTraceElement const * p;
    p = GetData();
    for (size_t i = 0; i < size; ++i)
        assert(p[i].pFunc != NULL);
}

void StackTraceArray::Grow(size_t grow_size)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory(););
        PRECONDITION(IsProtectedByGCFrame((OBJECTREF*)this));
    }
    CONTRACTL_END;

    size_t raw_size = grow_size * sizeof(StackTraceElement) + sizeof(ArrayHeader);

    if (!m_array)
    {
        SetArray(I1ARRAYREF(AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(raw_size))));
        SetSize(0);
        SetObjectThread();
    }
    else
    {
        if (Capacity() >= raw_size)
            return;

        // allocate a new array, copy the data
        size_t new_capacity = Max(Capacity() * 2, raw_size);

        _ASSERTE(new_capacity >= grow_size * sizeof(StackTraceElement) + sizeof(ArrayHeader));
        
        I1ARRAYREF newarr = (I1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(new_capacity));
        memcpyNoGCRefs(newarr->GetDirectPointerToNonObjectElements(),
                       GetRaw(),
                       Size() * sizeof(StackTraceElement) + sizeof(ArrayHeader));

        SetArray(newarr);
    }
}

void StackTraceArray::EnsureThreadAffinity()
{
    WRAPPER_NO_CONTRACT;

    if (!m_array)
        return;

    if (GetObjectThread() != GetThread())
    {
        // object is being changed by a thread different from the one which created it
        // make a copy of the array to prevent a race condition when two different threads try to change it
        StackTraceArray copy;
        GCPROTECT_BEGIN(copy);
            copy.CopyFrom(*this);
            this->Swap(copy);
        GCPROTECT_END();
    }
}

#ifdef _MSC_VER
#pragma warning(disable: 4267) 
#endif

// Deep copies the stack trace array
void StackTraceArray::CopyFrom(StackTraceArray const & src)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory(););
        PRECONDITION(IsProtectedByGCFrame((OBJECTREF*)this));
        PRECONDITION(IsProtectedByGCFrame((OBJECTREF*)&src));
    }
    CONTRACTL_END;

    m_array = (I1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(src.Capacity()));

    Volatile<size_t> size = src.Size();
    memcpyNoGCRefs(GetRaw(), src.GetRaw(), size * sizeof(StackTraceElement) + sizeof(ArrayHeader));

    SetSize(size);  // set size to the exact value which was used when we copied the data
                    // another thread might have changed it at the time of copying
    SetObjectThread();  // affinitize the newly created array with the current thread
}

#ifdef _MSC_VER
#pragma warning(default: 4267)
#endif


#ifdef _DEBUG
//===============================================================================
// Code that insures that our unmanaged version of Nullable is consistant with
// the managed version Nullable<T> for all T.  

void Nullable::CheckFieldOffsets(TypeHandle nullableType) 
{
    LIMITED_METHOD_CONTRACT;

/***
        // The non-instantiated method tables like List<T> that are used
        // by reflection and verification do not have correct field offsets
        // but we never make instances of these anyway.
    if (nullableMT->ContainsGenericVariables())
        return;
***/

    MethodTable* nullableMT = nullableType.GetMethodTable();

        // insure that the managed version of the table is the same as the
        // unmanaged.  Note that we can't do this in mscorlib.h because this
        // class is generic and field layout depends on the instantiation.

    _ASSERTE(nullableMT->GetNumInstanceFields() == 2);
    FieldDesc* field = nullableMT->GetApproxFieldDescListRaw();

    _ASSERTE(strcmp(field->GetDebugName(), "hasValue") == 0);
//     _ASSERTE(field->GetOffset() == offsetof(Nullable, hasValue));
    field++;

    _ASSERTE(strcmp(field->GetDebugName(), "value") == 0);
//     _ASSERTE(field->GetOffset() == offsetof(Nullable, value));
}
#endif

//===============================================================================
// Returns true if nullableMT is Nullable<T> for T is equivalent to paramMT

BOOL Nullable::IsNullableForTypeHelper(MethodTable* nullableMT, MethodTable* paramMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (!nullableMT->IsNullable())
        return FALSE;

    // we require the parameter types to be equivalent
    return TypeHandle(paramMT).IsEquivalentTo(nullableMT->GetInstantiation()[0]);
}

//===============================================================================
// Returns true if nullableMT is Nullable<T> for T == paramMT

BOOL Nullable::IsNullableForTypeHelperNoGC(MethodTable* nullableMT, MethodTable* paramMT)
{
    LIMITED_METHOD_CONTRACT;
    if (!nullableMT->IsNullable())
        return FALSE;

    // we require an exact match of the parameter types 
    return TypeHandle(paramMT) == nullableMT->GetInstantiation()[0];
}
    
//===============================================================================
CLR_BOOL* Nullable::HasValueAddr(MethodTable* nullableMT) {

    LIMITED_METHOD_CONTRACT;

    _ASSERTE(strcmp(nullableMT->GetApproxFieldDescListRaw()[0].GetDebugName(), "hasValue") == 0);
    _ASSERTE(nullableMT->GetApproxFieldDescListRaw()[0].GetOffset() == 0);
    return (CLR_BOOL*) this;
}

//===============================================================================
void* Nullable::ValueAddr(MethodTable* nullableMT) {

    LIMITED_METHOD_CONTRACT;

    _ASSERTE(strcmp(nullableMT->GetApproxFieldDescListRaw()[1].GetDebugName(), "value") == 0);
    return (((BYTE*) this) + nullableMT->GetApproxFieldDescListRaw()[1].GetOffset());
}

//===============================================================================
// Special Logic to box a nullable<T> as a boxed<T>

OBJECTREF Nullable::Box(void* srcPtr, MethodTable* nullableMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    FAULT_NOT_FATAL();      // FIX_NOW: why do we need this?

    Nullable* src = (Nullable*) srcPtr;

    _ASSERTE(IsNullableType(nullableMT));
        // We better have a concrete instantiation, or our field offset asserts are not useful
    _ASSERTE(!nullableMT->ContainsGenericVariables());

    if (!*src->HasValueAddr(nullableMT))
        return NULL;

    OBJECTREF obj = 0;
    GCPROTECT_BEGININTERIOR (src);
    MethodTable* argMT = nullableMT->GetInstantiation()[0].GetMethodTable();
    obj = argMT->Allocate();
    CopyValueClass(obj->UnBox(), src->ValueAddr(nullableMT), argMT);
    GCPROTECT_END ();

    return obj;
}

//===============================================================================
// Special Logic to unbox a boxed T as a nullable<T>

BOOL Nullable::UnBox(void* destPtr, OBJECTREF boxedVal, MethodTable* destMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    Nullable* dest = (Nullable*) destPtr;
    BOOL fRet = TRUE;

        // We should only get here if we are unboxing a T as a Nullable<T>
    _ASSERTE(IsNullableType(destMT));

        // We better have a concrete instantiation, or our field offset asserts are not useful
    _ASSERTE(!destMT->ContainsGenericVariables());

    if (boxedVal == NULL) 
    {
        // Logically we are doing *dest->HasValueAddr(destMT) = false;
        // We zero out the whole structure becasue it may contain GC references
        // and these need to be initialized to zero.   (could optimize in the non-GC case)
        InitValueClass(destPtr, destMT);
        fRet = TRUE;
    }
    else 
    {
        GCPROTECT_BEGIN(boxedVal);
        if (!IsNullableForType(destMT, boxedVal->GetMethodTable()))
        {
            // For safety's sake, also allow true nullables to be unboxed normally.  
            // This should not happen normally, but we want to be robust
            if (destMT->IsEquivalentTo(boxedVal->GetMethodTable()))
            {
                CopyValueClass(dest, boxedVal->GetData(), destMT);
                fRet = TRUE;
            }
            else
            {
                fRet = FALSE;
            }
        }
        else
        {
            *dest->HasValueAddr(destMT) = true;
            CopyValueClass(dest->ValueAddr(destMT), boxedVal->UnBox(), boxedVal->GetMethodTable());
            fRet = TRUE;
        }
        GCPROTECT_END();
    }
    return fRet;
}

//===============================================================================
// Special Logic to unbox a boxed T as a nullable<T>
// Does not handle type equivalence (may conservatively return FALSE)
BOOL Nullable::UnBoxNoGC(void* destPtr, OBJECTREF boxedVal, MethodTable* destMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    Nullable* dest = (Nullable*) destPtr;

        // We should only get here if we are unboxing a T as a Nullable<T>
    _ASSERTE(IsNullableType(destMT));

        // We better have a concrete instantiation, or our field offset asserts are not useful
    _ASSERTE(!destMT->ContainsGenericVariables());

    if (boxedVal == NULL) 
    {
        // Logically we are doing *dest->HasValueAddr(destMT) = false;
        // We zero out the whole structure becasue it may contain GC references
        // and these need to be initialized to zero.   (could optimize in the non-GC case)
        InitValueClass(destPtr, destMT);
    }
    else 
    {
        if (!IsNullableForTypeNoGC(destMT, boxedVal->GetMethodTable()))
        {
            // For safety's sake, also allow true nullables to be unboxed normally.  
            // This should not happen normally, but we want to be robust
            if (destMT == boxedVal->GetMethodTable())
            {
                CopyValueClass(dest, boxedVal->GetData(), destMT);
                return TRUE;
            }
            return FALSE;
        }

        *dest->HasValueAddr(destMT) = true;
        CopyValueClass(dest->ValueAddr(destMT), boxedVal->UnBox(), boxedVal->GetMethodTable());
    }
    return TRUE;
}

//===============================================================================
// Special Logic to unbox a boxed T as a nullable<T> into an argument 
// specified by the argDest.
// Does not handle type equivalence (may conservatively return FALSE)
BOOL Nullable::UnBoxIntoArgNoGC(ArgDestination *argDest, OBJECTREF boxedVal, MethodTable* destMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#if defined(UNIX_AMD64_ABI)
    if (argDest->IsStructPassedInRegs())
    {
        // We should only get here if we are unboxing a T as a Nullable<T>
        _ASSERTE(IsNullableType(destMT));

        // We better have a concrete instantiation, or our field offset asserts are not useful
        _ASSERTE(!destMT->ContainsGenericVariables());

        if (boxedVal == NULL) 
        {
            // Logically we are doing *dest->HasValueAddr(destMT) = false;
            // We zero out the whole structure becasue it may contain GC references
            // and these need to be initialized to zero.   (could optimize in the non-GC case)
            InitValueClassArg(argDest, destMT);
        }
        else 
        {
            if (!IsNullableForTypeNoGC(destMT, boxedVal->GetMethodTable()))
            {
                // For safety's sake, also allow true nullables to be unboxed normally.  
                // This should not happen normally, but we want to be robust
                if (destMT == boxedVal->GetMethodTable())
                {
                    CopyValueClassArg(argDest, boxedVal->GetData(), destMT, 0);
                    return TRUE;
                }
                return FALSE;
            }

            Nullable* dest = (Nullable*)argDest->GetStructGenRegDestinationAddress();
            *dest->HasValueAddr(destMT) = true;
            int destOffset = (BYTE*)dest->ValueAddr(destMT) - (BYTE*)dest;
            CopyValueClassArg(argDest, boxedVal->UnBox(), boxedVal->GetMethodTable(), destOffset);
        }
        return TRUE;
    }

#endif // UNIX_AMD64_ABI

    return UnBoxNoGC(argDest->GetDestinationAddress(), boxedVal, destMT);
}

//===============================================================================
// Special Logic to unbox a boxed T as a nullable<T>
// Does not do any type checks.
void Nullable::UnBoxNoCheck(void* destPtr, OBJECTREF boxedVal, MethodTable* destMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    Nullable* dest = (Nullable*) destPtr;

        // We should only get here if we are unboxing a T as a Nullable<T>
    _ASSERTE(IsNullableType(destMT));

        // We better have a concrete instantiation, or our field offset asserts are not useful
    _ASSERTE(!destMT->ContainsGenericVariables());

    if (boxedVal == NULL) 
    {
        // Logically we are doing *dest->HasValueAddr(destMT) = false;
        // We zero out the whole structure becasue it may contain GC references
        // and these need to be initialized to zero.   (could optimize in the non-GC case)
        InitValueClass(destPtr, destMT);
    }
    else 
    {
        if (IsNullableType(boxedVal->GetMethodTable()))
        {
            // For safety's sake, also allow true nullables to be unboxed normally.  
            // This should not happen normally, but we want to be robust
            CopyValueClass(dest, boxedVal->GetData(), destMT);
        }

        *dest->HasValueAddr(destMT) = true;
        CopyValueClass(dest->ValueAddr(destMT), boxedVal->UnBox(), boxedVal->GetMethodTable());
    }
}

//===============================================================================
// a boxed Nullable<T> should either be null or a boxed T, but sometimes it is
// useful to have a 'true' boxed Nullable<T> (that is it has two fields).  This
// function returns a 'normalized' version of this pointer.

OBJECTREF Nullable::NormalizeBox(OBJECTREF obj) {
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (obj != NULL) {
        MethodTable* retMT = obj->GetMethodTable();
        if (Nullable::IsNullableType(retMT)) 
            obj = Nullable::Box(obj->GetData(), retMT);
    }
    return obj;
}


void ThreadBaseObject::SetInternal(Thread *it)
{
    WRAPPER_NO_CONTRACT;

    // only allow a transition from NULL to non-NULL
    _ASSERTE((m_InternalThread == NULL) && (it != NULL));
    m_InternalThread = it;

    // Now the native Thread will only be destroyed after the managed Thread is collected.
    // Tell the GC that the managed Thread actually represents much more memory.
    GCInterface::NewAddMemoryPressure(sizeof(Thread));
}

void ThreadBaseObject::ClearInternal()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_InternalThread != NULL);
    m_InternalThread = NULL;
    GCInterface::NewRemoveMemoryPressure(sizeof(Thread));
}

#endif // #ifndef DACCESS_COMPILE


StackTraceElement const & StackTraceArray::operator[](size_t index) const
{
    WRAPPER_NO_CONTRACT;
    return GetData()[index];
}

StackTraceElement & StackTraceArray::operator[](size_t index)
{
    WRAPPER_NO_CONTRACT;
    return GetData()[index];
}

#if !defined(DACCESS_COMPILE)
// Define the lock used to access stacktrace from an exception object
SpinLock g_StackTraceArrayLock;

void ExceptionObject::SetStackTrace(StackTraceArray const & stackTrace, PTRARRAYREF dynamicMethodArray)
{        
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    Thread *m_pThread = GetThread();
    SpinLock::AcquireLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);

    SetObjectReference((OBJECTREF*)&_stackTrace, (OBJECTREF)stackTrace.Get());
    SetObjectReference((OBJECTREF*)&_dynamicMethods, (OBJECTREF)dynamicMethodArray);

    SpinLock::ReleaseLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);

}

void ExceptionObject::SetNullStackTrace()
{        
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    Thread *m_pThread = GetThread();
    SpinLock::AcquireLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);

    I1ARRAYREF stackTraceArray = NULL;
    PTRARRAYREF dynamicMethodArray = NULL;

    SetObjectReference((OBJECTREF*)&_stackTrace, (OBJECTREF)stackTraceArray);
    SetObjectReference((OBJECTREF*)&_dynamicMethods, (OBJECTREF)dynamicMethodArray);

    SpinLock::ReleaseLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);
}

#endif // !defined(DACCESS_COMPILE)

void ExceptionObject::GetStackTrace(StackTraceArray & stackTrace, PTRARRAYREF * outDynamicMethodArray /*= NULL*/) const
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#if !defined(DACCESS_COMPILE)
    Thread *m_pThread = GetThread();
    SpinLock::AcquireLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);
#endif // !defined(DACCESS_COMPILE)

    StackTraceArray temp(_stackTrace);
    stackTrace.Swap(temp);

    if (outDynamicMethodArray != NULL)
    {
        *outDynamicMethodArray = _dynamicMethods;
    }

#if !defined(DACCESS_COMPILE)
    SpinLock::ReleaseLock(&g_StackTraceArrayLock, SPINLOCK_THREAD_PARAM_ONLY_IN_SOME_BUILDS);
#endif // !defined(DACCESS_COMPILE)

}

bool LAHashDependentHashTrackerObject::IsLoaderAllocatorLive()
{
    return (ObjectFromHandle(_dependentHandle) != NULL);
}

void LAHashDependentHashTrackerObject::GetDependentAndLoaderAllocator(OBJECTREF *pLoaderAllocatorRef, GCHEAPHASHOBJECTREF *pGCHeapHash)
{
    OBJECTREF primary = ObjectFromHandle(_dependentHandle);
    if (pLoaderAllocatorRef != NULL)
        *pLoaderAllocatorRef = primary;

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    // Secondary is tracked only if primary is non-null
    if (pGCHeapHash != NULL)
        *pGCHeapHash = (GCHEAPHASHOBJECTREF)(OBJECTREF)((primary != NULL) ? mgr->GetDependentHandleSecondary(_dependentHandle) : NULL);
}
