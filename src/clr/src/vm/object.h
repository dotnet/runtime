// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// OBJECT.H
//
// Definitions of a Com+ Object
// 

// See code:EEStartup#TableOfContents for overview


#ifndef _OBJECT_H_
#define _OBJECT_H_

#include "util.hpp"
#include "syncblk.h"
#include "gcdesc.h"
#include "specialstatics.h"
#include "sstring.h"
#include "daccess.h"
#include "fcall.h"

extern "C" void __fastcall ZeroMemoryInGCHeap(void*, size_t);

void ErectWriteBarrierForMT(MethodTable **dst, MethodTable *ref);

/*
 #ObjectModel 
 * COM+ Internal Object Model
 *
 *
 * Object              - This is the common base part to all COM+ objects
 *  |                        it contains the MethodTable pointer and the
 *  |                        sync block index, which is at a negative offset
 *  |
 *  +-- code:StringObject       - String objects are specialized objects for string
 *  |                        storage/retrieval for higher performance
 *  |
 *  +-- BaseObjectWithCachedData - Object Plus one object field for caching.
 *  |       |
 *  |            +-  ReflectClassBaseObject    - The base object for the RuntimeType class
 *  |            +-  ReflectMethodObject       - The base object for the RuntimeMethodInfo class
 *  |            +-  ReflectFieldObject        - The base object for the RtFieldInfo class
 *  |
 *  +-- code:ArrayBase          - Base portion of all arrays
 *  |       |
 *  |       +-  I1Array    - Base type arrays
 *  |       |   I2Array
 *  |       |   ...
 *  |       |
 *  |       +-  PtrArray   - Array of OBJECTREFs, different than base arrays because of pObjectClass
 *  |              
 *  +-- code:AppDomainBaseObject - The base object for the class AppDomain
 *  |              
 *  +-- code:AssemblyBaseObject - The base object for the class Assembly
 *
 *
 * PLEASE NOTE THE FOLLOWING WHEN ADDING A NEW OBJECT TYPE:
 *
 *    The size of the object in the heap must be able to be computed
 *    very, very quickly for GC purposes.   Restrictions on the layout
 *    of the object guarantee this is possible.
 *
 *    Any object that inherits from Object must be able to
 *    compute its complete size by using the first 4 bytes of
 *    the object following the Object part and constants
 *    reachable from the MethodTable...
 *
 *    The formula used for this calculation is:
 *        MT->GetBaseSize() + ((OBJECTTYPEREF->GetSizeField() * MT->GetComponentSize())
 *
 *    So for Object, since this is of fixed size, the ComponentSize is 0, which makes the right side
 *    of the equation above equal to 0 no matter what the value of GetSizeField(), so the size is just the base size.
 *
 */

// <TODO>
// @TODO:  #define COW         0x04     
// @TODO: MOO, MOO - no, not bovine, really Copy On Write bit for StringBuffer, requires 8 byte align MT
// @TODL: which we don't have yet</TODO>

class MethodTable;
class Thread;
class BaseDomain;
class Assembly;
class DomainAssembly;
class AssemblyNative;
class WaitHandleNative;
class ArgDestination;

struct RCW;

#ifdef _TARGET_64BIT_
#define OBJHEADER_SIZE      (sizeof(DWORD) /* m_alignpad */ + sizeof(DWORD) /* m_SyncBlockValue */)
#else
#define OBJHEADER_SIZE      sizeof(DWORD) /* m_SyncBlockValue */
#endif

#define OBJECT_SIZE         TARGET_POINTER_SIZE /* m_pMethTab */
#define OBJECT_BASESIZE     (OBJHEADER_SIZE + OBJECT_SIZE)

#ifdef _TARGET_64BIT_
#define ARRAYBASE_SIZE      (OBJECT_SIZE /* m_pMethTab */ + sizeof(DWORD) /* m_NumComponents */ + sizeof(DWORD) /* pad */)
#else
#define ARRAYBASE_SIZE      (OBJECT_SIZE /* m_pMethTab */ + sizeof(DWORD) /* m_NumComponents */)
#endif

#define ARRAYBASE_BASESIZE  (OBJHEADER_SIZE + ARRAYBASE_SIZE)

//
// The generational GC requires that every object be at least 12 bytes
// in size.   

#define MIN_OBJECT_SIZE     (2*TARGET_POINTER_SIZE + OBJHEADER_SIZE)

#define PTRALIGNCONST (DATA_ALIGNMENT-1)

#ifndef PtrAlign
#define PtrAlign(size) \
    ((size + PTRALIGNCONST) & (~PTRALIGNCONST))
#endif //!PtrAlign

// code:Object is the respesentation of an managed object on the GC heap.
//   
// See  code:#ObjectModel for some important subclasses of code:Object 
// 
// The only fields mandated by all objects are
// 
//     * a pointer to the code:MethodTable at offset 0
//     * a poiner to a code:ObjHeader at a negative offset. This is often zero.  It holds information that
//         any addition information that we might need to attach to arbitrary objects. 
// 
class Object
{
  protected:
    PTR_MethodTable m_pMethTab;

  protected:
    Object() { LIMITED_METHOD_CONTRACT; };
   ~Object() { LIMITED_METHOD_CONTRACT; };
   
  public:
    MethodTable *RawGetMethodTable() const
    {
        return m_pMethTab;
    }

#ifndef DACCESS_COMPILE
    void RawSetMethodTable(MethodTable *pMT)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMethTab = pMT;
    }

    VOID SetMethodTable(MethodTable *pMT
                        DEBUG_ARG(BOOL bAllowArray = FALSE))
    { 
        LIMITED_METHOD_CONTRACT;
        m_pMethTab = pMT; 

#ifdef _DEBUG
        if (!bAllowArray)
        {
            AssertNotArray();
        }
#endif // _DEBUG
    }

    VOID SetMethodTableForLargeObject(MethodTable *pMT
                                      DEBUG_ARG(BOOL bAllowArray = FALSE))
    {
        // This function must be used if the allocation occurs on the large object heap, and the method table might be a collectible type
        WRAPPER_NO_CONTRACT;
        ErectWriteBarrierForMT(&m_pMethTab, pMT);

#ifdef _DEBUG
        if (!bAllowArray)
        {
            AssertNotArray();
        }
#endif // _DEBUG
    }
#endif //!DACCESS_COMPILE

#define MARKED_BIT 0x1

    PTR_MethodTable GetMethodTable() const              
    { 
        LIMITED_METHOD_DAC_CONTRACT;

#ifndef DACCESS_COMPILE
        // We should always use GetGCSafeMethodTable() if we're running during a GC. 
        // If the mark bit is set then we're running during a GC     
        _ASSERTE((dac_cast<TADDR>(m_pMethTab) & MARKED_BIT) == 0);

        return m_pMethTab;
#else //DACCESS_COMPILE

        //@dbgtodo dharvey Make this a type which supports bitwise and operations
        //when available
        return PTR_MethodTable((dac_cast<TADDR>(m_pMethTab)) & (~MARKED_BIT));
#endif //DACCESS_COMPILE
    }

    DPTR(PTR_MethodTable) GetMethodTablePtr() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<DPTR(PTR_MethodTable)>(PTR_HOST_MEMBER_TADDR(Object, this, m_pMethTab));
    }

    TypeHandle      GetTypeHandle();
    TypeHandle      GetTrueTypeHandle();

        // Methods used to determine if an object supports a given interface.
    static BOOL     SupportsInterface(OBJECTREF pObj, MethodTable *pInterfaceMT);

    inline DWORD    GetNumComponents();
    inline SIZE_T   GetSize();

    CGCDesc*        GetSlotMap()                        
    { 
        WRAPPER_NO_CONTRACT;
        return( CGCDesc::GetCGCDescFromMT(GetMethodTable())); 
    }

    // Sync Block & Synchronization services

    // Access the ObjHeader which is at a negative offset on the object (because of
    // cache lines)
    PTR_ObjHeader   GetHeader()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_ObjHeader>(this) - 1;
    }

    // Get the current address of the object (works for debug refs, too.)
    PTR_BYTE      GetAddress()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_BYTE>(this);
    }

#ifdef _DEBUG
    // TRUE if the header has a real SyncBlockIndex (i.e. it has an entry in the
    // SyncTable, though it doesn't necessarily have an entry in the SyncBlockCache)
    BOOL HasEmptySyncBlockInfo()
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->HasEmptySyncBlockInfo();
    }
#endif

    // retrieve or allocate a sync block for this object
    SyncBlock *GetSyncBlock()
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->GetSyncBlock();
    }

    DWORD GetSyncBlockIndex()
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->GetSyncBlockIndex();
    }

    ADIndex GetAppDomainIndex();

    // Get app domain of object, or NULL if it is agile
    AppDomain *GetAppDomain();

#ifndef DACCESS_COMPILE
    // Set app domain of object to current domain.
    void SetAppDomain() { WRAPPER_NO_CONTRACT; SetAppDomain(::GetAppDomain()); }
    BOOL SetAppDomainNoThrow();
    
#endif

    // Set app domain of object to given domain - it can only be set once
    void SetAppDomain(AppDomain *pDomain);

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    // For SO-tolerance contract violation purposes, define these DEBUG_ versions to identify
    // the codepaths to SetAppDomain that are called only from DEBUG code.
    void DEBUG_SetAppDomain()
    {
        WRAPPER_NO_CONTRACT;

        DEBUG_SetAppDomain(::GetAppDomain());
    }
#endif //!DACCESS_COMPILE

    void DEBUG_SetAppDomain(AppDomain *pDomain);
#endif //_DEBUG

    // DO NOT ADD ANY ASSERTS TO THIS METHOD.
    // DO NOT USE THIS METHOD.
    // Yes folks, for better or worse the debugger pokes supposed object addresses 
    // to try to see if objects are valid, possibly firing an AccessViolation or worse,
    // and then catches the AV and reports a failure to the debug client.  This makes
    // the debugger slightly more robust should any corrupted object references appear
    // in a session. Thus it is "correct" behaviour for this to AV when used with 
    // an invalid object pointer, and incorrect behaviour for it to
    // assert.  
    BOOL ValidateObjectWithPossibleAV();

    // Validate an object ref out of the Promote routine in the GC
    void ValidatePromote(ScanContext *sc, DWORD flags);

    // Validate an object ref out of the VerifyHeap routine in the GC
    void ValidateHeap(Object *from, BOOL bDeep=TRUE);

    PTR_SyncBlock PassiveGetSyncBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetHeader()->PassiveGetSyncBlock();
    }

    static DWORD ComputeHashCode();

#ifndef DACCESS_COMPILE    
    INT32 GetHashCodeEx();
#endif // #ifndef DACCESS_COMPILE
    
    // Synchronization
#ifndef DACCESS_COMPILE

    void EnterObjMonitor()
    {
        WRAPPER_NO_CONTRACT;
        GetHeader()->EnterObjMonitor();
    }

    BOOL TryEnterObjMonitor(INT32 timeOut = 0)
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->TryEnterObjMonitor(timeOut);
    }

    bool TryEnterObjMonitorSpinHelper();

    FORCEINLINE AwareLock::EnterHelperResult EnterObjMonitorHelper(Thread* pCurThread)
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->EnterObjMonitorHelper(pCurThread);
    }

    FORCEINLINE AwareLock::EnterHelperResult EnterObjMonitorHelperSpin(Thread* pCurThread)
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->EnterObjMonitorHelperSpin(pCurThread);
    }

    BOOL LeaveObjMonitor()
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->LeaveObjMonitor();
    }
    
    // should be called only from unwind code; used in the
    // case where EnterObjMonitor failed to allocate the
    // sync-object.
    BOOL LeaveObjMonitorAtException()
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->LeaveObjMonitorAtException();
    }

    FORCEINLINE AwareLock::LeaveHelperAction LeaveObjMonitorHelper(Thread* pCurThread)
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->LeaveObjMonitorHelper(pCurThread);
    }

    // Returns TRUE if the lock is owned and FALSE otherwise
    // threadId is set to the ID (Thread::GetThreadId()) of the thread which owns the lock
    // acquisitionCount is set to the number of times the lock needs to be released before
    // it is unowned
    BOOL GetThreadOwningMonitorLock(DWORD *pThreadId, DWORD *pAcquisitionCount)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return GetHeader()->GetThreadOwningMonitorLock(pThreadId, pAcquisitionCount);
    }

#endif // #ifndef DACCESS_COMPILE

    BOOL Wait(INT32 timeOut, BOOL exitContext)
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->Wait(timeOut, exitContext);
    }

    void Pulse()
    {
        WRAPPER_NO_CONTRACT;
        GetHeader()->Pulse();
    }

    void PulseAll()
    {
        WRAPPER_NO_CONTRACT;
        GetHeader()->PulseAll();
    }

   PTR_VOID UnBox();      // if it is a value class, get the pointer to the first field
  
    PTR_BYTE   GetData(void)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_BYTE>(this) + sizeof(Object);
    }

    static UINT GetOffsetOfFirstField()
    {
        LIMITED_METHOD_CONTRACT;
        return sizeof(Object);
    }

    DWORD   GetOffset32(DWORD dwOffset)
    { 
        WRAPPER_NO_CONTRACT;
        return * PTR_DWORD(GetData() + dwOffset);
    }

    USHORT  GetOffset16(DWORD dwOffset)
    { 
        WRAPPER_NO_CONTRACT;
        return * PTR_USHORT(GetData() + dwOffset);
    }

    BYTE    GetOffset8(DWORD dwOffset)
    { 
        WRAPPER_NO_CONTRACT;
        return * PTR_BYTE(GetData() + dwOffset);
    }

    __int64 GetOffset64(DWORD dwOffset)
    { 
        WRAPPER_NO_CONTRACT;
        return (__int64) * PTR_ULONG64(GetData() + dwOffset);
    }

    void *GetPtrOffset(DWORD dwOffset)
    {
        WRAPPER_NO_CONTRACT;
        return (void *)(TADDR)*PTR_TADDR(GetData() + dwOffset);
    }

#ifndef DACCESS_COMPILE
    
    void SetOffsetObjectRef(DWORD dwOffset, size_t dwValue);

    void SetOffsetPtr(DWORD dwOffset, LPVOID value)
    {
        WRAPPER_NO_CONTRACT;
        *(LPVOID *) &GetData()[dwOffset] = value;
    }
        
    void SetOffset32(DWORD dwOffset, DWORD dwValue)
    { 
        WRAPPER_NO_CONTRACT;
        *(DWORD *) &GetData()[dwOffset] = dwValue;
    }

    void SetOffset16(DWORD dwOffset, DWORD dwValue)
    { 
        WRAPPER_NO_CONTRACT;
        *(USHORT *) &GetData()[dwOffset] = (USHORT) dwValue;
    }

    void SetOffset8(DWORD dwOffset, DWORD dwValue)
    { 
        WRAPPER_NO_CONTRACT;
        *(BYTE *) &GetData()[dwOffset] = (BYTE) dwValue;
    }

    void SetOffset64(DWORD dwOffset, __int64 qwValue)
    { 
        WRAPPER_NO_CONTRACT;
        *(__int64 *) &GetData()[dwOffset] = qwValue;
    }

#endif // #ifndef DACCESS_COMPILE

    VOID            Validate(BOOL bDeep = TRUE, BOOL bVerifyNextHeader = TRUE, BOOL bVerifySyncBlock = TRUE);

    PTR_MethodTable GetGCSafeMethodTable() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        // lose GC marking bit and the reserved bit
        // A method table pointer should always be aligned.  During GC we set the least 
        // significant bit for marked objects, and the second to least significant
        // bit is reserved.  So if we want the actual MT pointer during a GC
        // we must zero out the lowest 2 bits.
        return dac_cast<PTR_MethodTable>((dac_cast<TADDR>(m_pMethTab)) & ~((UINT_PTR)3));
    }

    // There are some cases where it is unsafe to get the type handle during a GC.
    // This occurs when the type has already been unloaded as part of an in-progress appdomain shutdown.
    TypeHandle GetGCSafeTypeHandleIfPossible() const;
    
    inline TypeHandle GetGCSafeTypeHandle() const;

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(void);
#endif
    
 private:
    VOID ValidateInner(BOOL bDeep, BOOL bVerifyNextHeader, BOOL bVerifySyncBlock);

#ifdef _DEBUG
    void AssertNotArray()
    {
        if (m_pMethTab->IsArray())
        {
            _ASSERTE(!"ArrayBase::SetArrayMethodTable/ArrayBase::SetArrayMethodTableForLargeObject should be used for arrays");
        }
    }
#endif // _DEBUG
};

/*
 * Object ref setting routines.  You must use these to do 
 * proper write barrier support, as well as app domain 
 * leak checking.
 *
 * Note that the AppDomain parameter is the app domain affinity
 * of the object containing the field or value class.  It should
 * be NULL if the containing object is app domain agile. Note that
 * you typically get this value by calling obj->GetAppDomain() on 
 * the containing object.
 */

// SetObjectReference sets an OBJECTREF field

void SetObjectReferenceUnchecked(OBJECTREF *dst,OBJECTREF ref);

#ifdef _DEBUG
void EnableStressHeapHelper();
#endif

//Used to clear the object reference
inline void ClearObjectReference(OBJECTREF* dst) 
{ 
    LIMITED_METHOD_CONTRACT;
    *(void**)(dst) = NULL; 
}

// CopyValueClass sets a value class field

void STDCALL CopyValueClassUnchecked(void* dest, void* src, MethodTable *pMT);
void STDCALL CopyValueClassArgUnchecked(ArgDestination *argDest, void* src, MethodTable *pMT, int destOffset);

inline void InitValueClass(void *dest, MethodTable *pMT)
{ 
    WRAPPER_NO_CONTRACT;
    ZeroMemoryInGCHeap(dest, pMT->GetNumInstanceFieldBytes());
}

// Initialize value class argument
void InitValueClassArg(ArgDestination *argDest, MethodTable *pMT);

#define SetObjectReference(_d,_r,_a)        SetObjectReferenceUnchecked(_d, _r)
#define CopyValueClass(_d,_s,_m,_a)         CopyValueClassUnchecked(_d,_s,_m)       
#define CopyValueClassArg(_d,_s,_m,_a,_o)   CopyValueClassArgUnchecked(_d,_s,_m,_o)       

#include <pshpack4.h>


// There are two basic kinds of array layouts in COM+
//      ELEMENT_TYPE_ARRAY  - a multidimensional array with lower bounds on the dims
//      ELMENNT_TYPE_SZARRAY - A zero based single dimensional array
//
// In addition the layout of an array in memory is also affected by
// whether the method table is shared (eg in the case of arrays of object refs)
// or not.  In the shared case, the array has to hold the type handle of
// the element type.  
//
// ArrayBase encapuslates all of these details.  In theory you should never
// have to peek inside this abstraction
//
class ArrayBase : public Object
{
    friend class GCHeap;
    friend class CObjectHeader;
    friend class Object;
    friend OBJECTREF AllocateArrayEx(MethodTable *pArrayMT, INT32 *pArgs, DWORD dwNumArgs, BOOL bAllocateInLargeHeap DEBUG_ARG(BOOL bDontSetAppDomain)); 
    friend OBJECTREF FastAllocatePrimitiveArray(MethodTable* arrayType, DWORD cElements, BOOL bAllocateInLargeHeap);
    friend FCDECL2(Object*, JIT_NewArr1VC_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
    friend FCDECL2(Object*, JIT_NewArr1OBJ_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
    friend class JIT_TrialAlloc;
    friend class CheckAsmOffsets;
    friend struct _DacGlobals;

private:
    // This MUST be the first field, so that it directly follows Object.  This is because
    // Object::GetSize() looks at m_NumComponents even though it may not be an array (the
    // values is shifted out if not an array, so it's ok). 
    DWORD       m_NumComponents;
#ifdef _TARGET_64BIT_
    DWORD       pad;
#endif // _TARGET_64BIT_

    SVAL_DECL(INT32, s_arrayBoundsZero); // = 0

    // What comes after this conceputally is:
    // TypeHandle elementType;        Only present if the method table is shared among many types (arrays of pointers)
    // INT32      bounds[rank];       The bounds are only present for Multidimensional arrays   
    // INT32      lowerBounds[rank];  Valid indexes are lowerBounds[i] <= index[i] < lowerBounds[i] + bounds[i]

public:
    // Gets the unique type handle for this array object.
    // This will call the loader in don't-load mode - the allocator
    // always makes sure that the particular ArrayTypeDesc for this array
    // type is available before allocating any instances of this array type.
    inline TypeHandle GetTypeHandle() const;

    inline static TypeHandle GetTypeHandle(MethodTable * pMT);

    // Get the element type for the array, this works whether the the element
    // type is stored in the array or not
    inline TypeHandle GetArrayElementTypeHandle() const;

        // Get the CorElementType for the elements in the array.  Avoids creating a TypeHandle
    inline CorElementType GetArrayElementType() const;

    inline unsigned GetRank() const;

        // Total element count for the array
    inline DWORD GetNumComponents() const;

#ifndef DACCESS_COMPILE
    inline void SetArrayMethodTable(MethodTable *pArrayMT);
    inline void SetArrayMethodTableForLargeObject(MethodTable *pArrayMT);
#endif // !DACCESS_COMPILE

        // Get pointer to elements, handles any number of dimensions
    PTR_BYTE GetDataPtr(BOOL inGC = FALSE) const {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
#ifdef _DEBUG
#ifndef DACCESS_COMPILE
        EnableStressHeapHelper();
#endif
#endif
        return dac_cast<PTR_BYTE>(this) +
                        GetDataPtrOffset(inGC ? GetGCSafeMethodTable() : GetMethodTable());
    }

    // The component size is actually 16-bit WORD, but this method is returning SIZE_T to ensure
    // that SIZE_T is used everywhere for object size computation. It is necessary to support
    // objects bigger than 2GB.
    SIZE_T GetComponentSize() const {
        WRAPPER_NO_CONTRACT;
        MethodTable * pMT;
        pMT = GetMethodTable();
        _ASSERTE(pMT->HasComponentSize());
        return pMT->RawGetComponentSize();
    }

        // Note that this can be a multidimensional array of rank 1 
        // (for example if we had a 1-D array with lower bounds
    BOOL IsMultiDimArray() const {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return(GetMethodTable()->IsMultiDimArray());
    }

        // Get pointer to the begining of the bounds (counts for each dim)
        // Works for any array type 
    PTR_INT32 GetBoundsPtr() const {
        WRAPPER_NO_CONTRACT;
        MethodTable * pMT = GetMethodTable();
        if (pMT->IsMultiDimArray()) 
        {
            return dac_cast<PTR_INT32>(
                dac_cast<TADDR>(this) + sizeof(*this));
        }
        else
        {
            return dac_cast<PTR_INT32>(PTR_HOST_MEMBER_TADDR(ArrayBase, this,
                                                   m_NumComponents));
        }
    }

        // Works for any array type 
    PTR_INT32 GetLowerBoundsPtr() const {
        WRAPPER_NO_CONTRACT;
        if (IsMultiDimArray())
        {
            // Lower bounds info is after total bounds info
            // and total bounds info has rank elements
            return GetBoundsPtr() + GetRank();
        }
        else
            return dac_cast<PTR_INT32>(GVAL_ADDR(s_arrayBoundsZero));
    }

    static unsigned GetOffsetOfNumComponents() {
        LIMITED_METHOD_CONTRACT;
        return offsetof(ArrayBase, m_NumComponents);
    }

    inline static unsigned GetDataPtrOffset(MethodTable* pMT);

    inline static unsigned GetBoundsOffset(MethodTable* pMT);
    inline static unsigned GetLowerBoundsOffset(MethodTable* pMT);

private:
#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    void AssertArrayTypeDescLoaded();
#endif // _DEBUG
#endif // !DACCESS_COMPILE
};

//
// Template used to build all the non-object
// arrays of a single dimension
//

template < class KIND >
class Array : public ArrayBase
{
  public:
      
    typedef DPTR(KIND) PTR_KIND;
    typedef DPTR(const KIND) PTR_CKIND;

    KIND          m_Array[1];

    PTR_KIND        GetDirectPointerToNonObjectElements()
    { 
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        // return m_Array; 
        return PTR_KIND(GetDataPtr()); // This also handles arrays of dim 1 with lower bounds present

    }

    PTR_CKIND  GetDirectConstPointerToNonObjectElements() const
    { 
        WRAPPER_NO_CONTRACT;
        // return m_Array; 
        return PTR_CKIND(GetDataPtr()); // This also handles arrays of dim 1 with lower bounds present
    }
};


// Warning: Use PtrArray only for single dimensional arrays, not multidim arrays.
class PtrArray : public ArrayBase
{
    friend class GCHeap;
    friend class ClrDataAccess;
    friend OBJECTREF AllocateArrayEx(MethodTable *pArrayMT, INT32 *pArgs, DWORD dwNumArgs, BOOL bAllocateInLargeHeap); 
    friend class JIT_TrialAlloc;
    friend class CheckAsmOffsets;

public:
    TypeHandle GetArrayElementTypeHandle()
    {
        LIMITED_METHOD_CONTRACT;
        return GetMethodTable()->GetApproxArrayElementTypeHandle();
    }

    PTR_OBJECTREF GetDataPtr()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_OBJECTREF>(dac_cast<PTR_BYTE>(this) + GetDataOffset());
    }

    static SIZE_T GetDataOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(PtrArray, m_Array);
    }

    void SetAt(SIZE_T i, OBJECTREF ref)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        _ASSERTE(i < GetNumComponents());
        SetObjectReference(m_Array + i, ref, GetAppDomain());
    }

    void ClearAt(SIZE_T i)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(i < GetNumComponents());
        ClearObjectReference(m_Array + i);
    }

    OBJECTREF GetAt(SIZE_T i)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(i < GetNumComponents());

// DAC doesn't know the true size of this array
// the compiler thinks it is size 1, but really it is size N hanging off the structure
#ifndef DACCESS_COMPILE
        return m_Array[i];
#else
        TADDR arrayTargetAddress = dac_cast<TADDR>(this) + offsetof(PtrArray, m_Array);
        __ArrayDPtr<OBJECTREF> targetArray = dac_cast< __ArrayDPtr<OBJECTREF> >(arrayTargetAddress);
        return targetArray[i];
#endif
    }

    friend class StubLinkerCPU;
#ifdef FEATURE_ARRAYSTUB_AS_IL
    friend class ArrayOpLinker;
#endif
public:
    OBJECTREF    m_Array[1];
};

#define OFFSETOF__PtrArray__m_Array_              ARRAYBASE_SIZE

/* a TypedByRef is a structure that is used to implement VB's BYREF variants.  
   it is basically a tuple of an address of some data along with a TypeHandle
   that indicates the type of the address */
class TypedByRef 
{
public:

    PTR_VOID data;
    TypeHandle type;  
};

typedef DPTR(TypedByRef) PTR_TypedByRef;

typedef Array<I1>   I1Array;
typedef Array<I2>   I2Array;
typedef Array<I4>   I4Array;
typedef Array<I8>   I8Array;
typedef Array<R4>   R4Array;
typedef Array<R8>   R8Array;
typedef Array<U1>   U1Array;
typedef Array<U1>   BOOLArray;
typedef Array<U2>   U2Array;
typedef Array<WCHAR>   CHARArray;
typedef Array<U4>   U4Array;
typedef Array<U8>   U8Array;
typedef PtrArray    PTRArray;  

typedef DPTR(I1Array)   PTR_I1Array;
typedef DPTR(I2Array)   PTR_I2Array;
typedef DPTR(I4Array)   PTR_I4Array;
typedef DPTR(I8Array)   PTR_I8Array;
typedef DPTR(R4Array)   PTR_R4Array;
typedef DPTR(R8Array)   PTR_R8Array;
typedef DPTR(U1Array)   PTR_U1Array;
typedef DPTR(BOOLArray) PTR_BOOLArray;
typedef DPTR(U2Array)   PTR_U2Array;
typedef DPTR(CHARArray) PTR_CHARArray;
typedef DPTR(U4Array)   PTR_U4Array;
typedef DPTR(U8Array)   PTR_U8Array;
typedef DPTR(PTRArray)  PTR_PTRArray;

class StringObject;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ArrayBase>  BASEARRAYREF;
typedef REF<I1Array>    I1ARRAYREF;
typedef REF<I2Array>    I2ARRAYREF;
typedef REF<I4Array>    I4ARRAYREF;
typedef REF<I8Array>    I8ARRAYREF;
typedef REF<R4Array>    R4ARRAYREF;
typedef REF<R8Array>    R8ARRAYREF;
typedef REF<U1Array>    U1ARRAYREF;
typedef REF<BOOLArray>  BOOLARRAYREF;
typedef REF<U2Array>    U2ARRAYREF;
typedef REF<U4Array>    U4ARRAYREF;
typedef REF<U8Array>    U8ARRAYREF;
typedef REF<CHARArray>  CHARARRAYREF;
typedef REF<PTRArray>   PTRARRAYREF;  // Warning: Use PtrArray only for single dimensional arrays, not multidim arrays.
typedef REF<StringObject> STRINGREF;

#else   // USE_CHECKED_OBJECTREFS

typedef PTR_ArrayBase   BASEARRAYREF;
typedef PTR_I1Array     I1ARRAYREF;
typedef PTR_I2Array     I2ARRAYREF;
typedef PTR_I4Array     I4ARRAYREF;
typedef PTR_I8Array     I8ARRAYREF;
typedef PTR_R4Array     R4ARRAYREF;
typedef PTR_R8Array     R8ARRAYREF;
typedef PTR_U1Array     U1ARRAYREF;
typedef PTR_BOOLArray   BOOLARRAYREF;
typedef PTR_U2Array     U2ARRAYREF;
typedef PTR_U4Array     U4ARRAYREF;
typedef PTR_U8Array     U8ARRAYREF;
typedef PTR_CHARArray   CHARARRAYREF;
typedef PTR_PTRArray    PTRARRAYREF;  // Warning: Use PtrArray only for single dimensional arrays, not multidim arrays.
typedef PTR_StringObject STRINGREF;

#endif // USE_CHECKED_OBJECTREFS


#include <poppack.h>


/*
 * StringObject
 *
 * Special String implementation for performance.   
 *
 *   m_StringLength - Length of string in number of WCHARs
 *   m_FirstChar    - The string buffer
 *
 */


/**
 *  The high bit state can be one of three value: 
 * STRING_STATE_HIGH_CHARS: We've examined the string and determined that it definitely has values greater than 0x80
 * STRING_STATE_FAST_OPS: We've examined the string and determined that it definitely has no chars greater than 0x80
 * STRING_STATE_UNDETERMINED: We've never examined this string.
 * We've also reserved another bit for future use.
 */

#define STRING_STATE_UNDETERMINED     0x00000000
#define STRING_STATE_HIGH_CHARS       0x40000000
#define STRING_STATE_FAST_OPS         0x80000000
#define STRING_STATE_SPECIAL_SORT     0xC0000000

class StringObject : public Object
{
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif
    friend class GCHeap;
    friend class JIT_TrialAlloc;
    friend class CheckAsmOffsets;
    friend class COMString;

  private:
    DWORD   m_StringLength;
    WCHAR   m_FirstChar;

  public:
    VOID    SetStringLength(DWORD len)                   { LIMITED_METHOD_CONTRACT; _ASSERTE(len >= 0); m_StringLength = len; }

  protected:
    StringObject() {LIMITED_METHOD_CONTRACT; }
   ~StringObject() {LIMITED_METHOD_CONTRACT; }
   
  public:
    static DWORD GetBaseSize();
    static SIZE_T GetSize(DWORD stringLength);

    DWORD   GetStringLength()                           { LIMITED_METHOD_DAC_CONTRACT; return( m_StringLength );}
    WCHAR*  GetBuffer()                                 { LIMITED_METHOD_CONTRACT; _ASSERTE(this != nullptr); return (WCHAR*)( dac_cast<TADDR>(this) + offsetof(StringObject, m_FirstChar) );  }

    DWORD GetHighCharState() {
        WRAPPER_NO_CONTRACT;
        DWORD ret = GetHeader()->GetBits() & (BIT_SBLK_STRING_HIGH_CHAR_MASK);
        return ret;
    }

    VOID SetHighCharState(DWORD value) {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(value==STRING_STATE_HIGH_CHARS || value==STRING_STATE_FAST_OPS 
                 || value==STRING_STATE_UNDETERMINED || value==STRING_STATE_SPECIAL_SORT);

        // you need to clear the present state before going to a new state, but we'll allow multiple threads to set it to the same thing.
        _ASSERTE((GetHighCharState() == STRING_STATE_UNDETERMINED) || (GetHighCharState()==value));    

        static_assert_no_msg(BIT_SBLK_STRING_HAS_NO_HIGH_CHARS == STRING_STATE_FAST_OPS && 
                 STRING_STATE_HIGH_CHARS == BIT_SBLK_STRING_HIGH_CHARS_KNOWN &&
                 STRING_STATE_SPECIAL_SORT == BIT_SBLK_STRING_HAS_SPECIAL_SORT);

        GetHeader()->SetBit(value);
    }

    static UINT GetBufferOffset()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (UINT)(offsetof(StringObject, m_FirstChar));
    }
    static UINT GetStringLengthOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT)(offsetof(StringObject, m_StringLength));
    }
    VOID    GetSString(SString &result)
    {
        WRAPPER_NO_CONTRACT;
        result.Set(GetBuffer(), GetStringLength());
    }
    //========================================================================
    // Creates a System.String object. All the functions that take a length
    // or a count of bytes will add the null terminator after length
    // characters. So this means that if you have a string that has 5
    // characters and the null terminator you should pass in 5 and NOT 6.
    //========================================================================
    static STRINGREF NewString(int length);
    static STRINGREF NewString(int length, BOOL bHasTrailByte);
    static STRINGREF NewString(const WCHAR *pwsz);
    static STRINGREF NewString(const WCHAR *pwsz, int length);
    static STRINGREF NewString(LPCUTF8 psz);
    static STRINGREF NewString(LPCUTF8 psz, int cBytes);

    static STRINGREF GetEmptyString();
    static STRINGREF* GetEmptyStringRefPtr();

    static STRINGREF* InitEmptyStringRefPtr();

    DWORD InternalCheckHighChars();

    BOOL HasTrailByte();
    BOOL GetTrailByte(BYTE *bTrailByte);
    BOOL SetTrailByte(BYTE bTrailByte);
    static BOOL CaseInsensitiveCompHelper(__in_ecount(aLength) WCHAR * strA, __in_z INT8 * strB, int aLength, int bLength, int *result);

#ifdef VERIFY_HEAP
    //has to use raw object to avoid recursive validation
    BOOL ValidateHighChars ();
#endif //VERIFY_HEAP

    /*=================RefInterpretGetStringValuesDangerousForGC======================
    **N.B.: This perfoms no range checking and relies on the caller to have done this.
    **Args: (IN)ref -- the String to be interpretted.
    **      (OUT)chars -- a pointer to the characters in the buffer.
    **      (OUT)length -- a pointer to the length of the buffer.
    **Returns: void.
    **Exceptions: None.
    ==============================================================================*/
    // !!!! If you use this function, you have to be careful because chars is a pointer
    // !!!! to the data buffer of ref.  If GC happens after this call, you need to make
    // !!!! sure that you have a pin handle on ref, or use GCPROTECT_BEGINPINNING on ref.
    void RefInterpretGetStringValuesDangerousForGC(__deref_out_ecount(*length + 1) WCHAR **chars, int *length) {
        WRAPPER_NO_CONTRACT;
    
        _ASSERTE(GetGCSafeMethodTable() == g_pStringClass);
        *length = GetStringLength();
        *chars  = GetBuffer();
#ifdef _DEBUG
        EnableStressHeapHelper();
#endif
    }


private:
    static STRINGREF* EmptyStringRefPtr;
};

//The first two macros are essentially the same.  I just define both because
//having both can make the code more readable.
#define IS_FAST_SORT(state) (((state) == STRING_STATE_FAST_OPS))
#define IS_SLOW_SORT(state) (((state) != STRING_STATE_FAST_OPS))

//This macro should be used to determine things like indexing, casing, and encoding.
#define IS_FAST_OPS_EXCEPT_SORT(state) (((state)==STRING_STATE_SPECIAL_SORT) || ((state)==STRING_STATE_FAST_OPS))
#define IS_ASCII(state) (((state)==STRING_STATE_SPECIAL_SORT) || ((state)==STRING_STATE_FAST_OPS))
#define IS_FAST_CASING(state) IS_ASCII(state)
#define IS_FAST_INDEX(state)  IS_ASCII(state)
#define IS_STRING_STATE_UNDETERMINED(state) ((state)==STRING_STATE_UNDETERMINED)
#define HAS_HIGH_CHARS(state) ((state)==STRING_STATE_HIGH_CHARS)

/*================================GetEmptyString================================
**Get a reference to the empty string.  If we haven't already gotten one, we
**query the String class for a pointer to the empty string that we know was
**created at startup.
**
**Args: None
**Returns: A STRINGREF to the EmptyString
**Exceptions: None
==============================================================================*/
inline STRINGREF StringObject::GetEmptyString() {

    CONTRACTL {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    } CONTRACTL_END;
    STRINGREF* refptr = EmptyStringRefPtr;

    //If we've never gotten a reference to the EmptyString, we need to go get one.
    if (refptr==NULL) {
        refptr = InitEmptyStringRefPtr();
    }
    //We've already have a reference to the EmptyString, so we can just return it.
    return *refptr;
}

inline STRINGREF* StringObject::GetEmptyStringRefPtr() {

    CONTRACTL {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    } CONTRACTL_END;
    STRINGREF* refptr = EmptyStringRefPtr;

    //If we've never gotten a reference to the EmptyString, we need to go get one.
    if (refptr==NULL) {
        refptr = InitEmptyStringRefPtr();
    }
    //We've already have a reference to the EmptyString, so we can just return it.
    return refptr;
}

// This is used to account for the remoting cache on RuntimeType, 
// RuntimeMethodInfo, and RtFieldInfo.
class BaseObjectWithCachedData : public Object
{
};

// This is the Class version of the Reflection object.
//  A Class has adddition information.
//  For a ReflectClassBaseObject the m_pData is a pointer to a FieldDesc array that
//      contains all of the final static primitives if its defined.
//  m_cnt = the number of elements defined in the m_pData FieldDesc array.  -1 means
//      this hasn't yet been defined.
class ReflectClassBaseObject : public BaseObjectWithCachedData
{
    friend class MscorlibBinder;

protected:
    OBJECTREF           m_keepalive;
    OBJECTREF           m_cache;
    TypeHandle          m_typeHandle;

#ifdef _DEBUG
    void TypeCheck()
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_COOPERATIVE;
            GC_NOTRIGGER;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        MethodTable *pMT = GetMethodTable();
        while (pMT != g_pRuntimeTypeClass && pMT != NULL)
        {
            pMT = pMT->GetParentMethodTable();
        }
        _ASSERTE(pMT == g_pRuntimeTypeClass);
    }
#endif // _DEBUG

public:
    void SetType(TypeHandle type) {
        CONTRACTL
        {
            NOTHROW;
            MODE_COOPERATIVE;
            GC_NOTRIGGER;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        INDEBUG(TypeCheck());
        m_typeHandle = type;
    }

    void SetKeepAlive(OBJECTREF keepalive)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_COOPERATIVE;
            GC_NOTRIGGER;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        INDEBUG(TypeCheck());
        SetObjectReference(&m_keepalive, keepalive, GetAppDomain());
    }

    TypeHandle GetType() {
        CONTRACTL
        {
            NOTHROW;
            MODE_COOPERATIVE;
            GC_NOTRIGGER;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        INDEBUG(TypeCheck());
        return m_typeHandle;
    }

};

// This is the Method version of the Reflection object.
//  A Method has adddition information.
//   m_pMD - A pointer to the actual MethodDesc of the method.
//   m_object - a field that has a reference type in it. Used only for RuntimeMethodInfoStub to keep the real type alive.
// This structure matches the structure up to the m_pMD for several different managed types. 
// (RuntimeConstructorInfo, RuntimeMethodInfo, and RuntimeMethodInfoStub). These types are unrelated in the type
// system except that they all implement a particular interface. It is important that that interface is not attached to any
// type that does not sufficiently match this data structure.
class ReflectMethodObject : public BaseObjectWithCachedData
{
    friend class MscorlibBinder;

protected:
    OBJECTREF           m_object;
    OBJECTREF           m_empty1;
    OBJECTREF           m_empty2;
    OBJECTREF           m_empty3;
    OBJECTREF           m_empty4;
    OBJECTREF           m_empty5;
    OBJECTREF           m_empty6;
    OBJECTREF           m_empty7;
    MethodDesc *        m_pMD;

public:
    void SetMethod(MethodDesc *pMethod) {
        LIMITED_METHOD_CONTRACT;
        m_pMD = pMethod;
    }

    // This must only be called on instances of ReflectMethodObject that are actually RuntimeMethodInfoStub
    void SetKeepAlive(OBJECTREF keepalive)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference(&m_object, keepalive, GetAppDomain());
    }

    MethodDesc *GetMethod() {
        LIMITED_METHOD_CONTRACT;
        return m_pMD;
    }

};

// This is the Field version of the Reflection object.
//  A Method has adddition information.
//   m_pFD - A pointer to the actual MethodDesc of the method.
//   m_object - a field that has a reference type in it. Used only for RuntimeFieldInfoStub to keep the real type alive.
// This structure matches the structure up to the m_pFD for several different managed types. 
// (RtFieldInfo and RuntimeFieldInfoStub). These types are unrelated in the type
// system except that they all implement a particular interface. It is important that that interface is not attached to any
// type that does not sufficiently match this data structure.
class ReflectFieldObject : public BaseObjectWithCachedData
{
    friend class MscorlibBinder;

protected:
    OBJECTREF           m_object;
    OBJECTREF           m_empty1;
    INT32               m_empty2;
    OBJECTREF           m_empty3;
    OBJECTREF           m_empty4;
    FieldDesc *         m_pFD;

public:
    void SetField(FieldDesc *pField) {
        LIMITED_METHOD_CONTRACT;
        m_pFD = pField;
    }

    // This must only be called on instances of ReflectFieldObject that are actually RuntimeFieldInfoStub
    void SetKeepAlive(OBJECTREF keepalive)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference(&m_object, keepalive, GetAppDomain());
    }

    FieldDesc *GetField() {
        LIMITED_METHOD_CONTRACT;
        return m_pFD;
    }
};

// ReflectModuleBaseObject 
// This class is the base class for managed Module.
//  This class will connect the Object back to the underlying VM representation
//  m_ReflectClass -- This is the real Class that was used for reflection
//      This class was used to get at this object
//  m_pData -- this is a generic pointer which usually points CorModule
//  
class ReflectModuleBaseObject : public Object
{
    friend class MscorlibBinder;

  protected:
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class definition of this object.
    OBJECTREF          m_runtimeType;    
    OBJECTREF          m_runtimeAssembly;
    void*              m_ReflectClass;  // Pointer to the ReflectClass structure
    Module*            m_pData;         // Pointer to the Module
    void*              m_pGlobals;      // Global values....
    void*              m_pGlobalsFlds;  // Global Fields....

  protected:
    ReflectModuleBaseObject() {LIMITED_METHOD_CONTRACT;}
   ~ReflectModuleBaseObject() {LIMITED_METHOD_CONTRACT;}
   
  public:
    void SetModule(Module* p) {
        LIMITED_METHOD_CONTRACT;
        m_pData = p;
    }
    Module* GetModule() {
        LIMITED_METHOD_CONTRACT;
        return m_pData;
    }
    void SetAssembly(OBJECTREF assembly)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference(&m_runtimeAssembly, assembly, GetAppDomain());
    }
};

NOINLINE ReflectModuleBaseObject* GetRuntimeModuleHelper(LPVOID __me, Module *pModule, OBJECTREF keepAlive);
#define FC_RETURN_MODULE_OBJECT(pModule, refKeepAlive) FC_INNER_RETURN(ReflectModuleBaseObject*, GetRuntimeModuleHelper(__me, pModule, refKeepAlive))

class SafeHandle;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<SafeHandle> SAFEHANDLE;
typedef REF<SafeHandle> SAFEHANDLEREF;
#else // USE_CHECKED_OBJECTREFS
typedef SafeHandle * SAFEHANDLE;
typedef SafeHandle * SAFEHANDLEREF;
#endif // USE_CHECKED_OBJECTREFS



#define SYNCCTXPROPS_REQUIRESWAITNOTIFICATION 0x1 // Keep in sync with SynchronizationContext.cs SynchronizationContextFlags
class ThreadBaseObject;
class SynchronizationContextObject: public Object
{
    friend class MscorlibBinder;
private:
    // These field are also defined in the managed representation.  (SecurityContext.cs)If you
    // add or change these field you must also change the managed code so that
    // it matches these.  This is necessary so that the object is the proper
    // size. 
    INT32 _props;
public:
    BOOL IsWaitNotificationRequired()
    {
        LIMITED_METHOD_CONTRACT;
        if ((_props & SYNCCTXPROPS_REQUIRESWAITNOTIFICATION) != 0)
            return TRUE;
        return FALSE;
    }
};





typedef DPTR(class CultureInfoBaseObject) PTR_CultureInfoBaseObject;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<SynchronizationContextObject> SYNCHRONIZATIONCONTEXTREF;
typedef REF<ExecutionContextObject> EXECUTIONCONTEXTREF;
typedef REF<CultureInfoBaseObject> CULTUREINFOBASEREF;
typedef REF<ArrayBase> ARRAYBASEREF;

#else
typedef SynchronizationContextObject*     SYNCHRONIZATIONCONTEXTREF;
typedef CultureInfoBaseObject*     CULTUREINFOBASEREF;
typedef PTR_ArrayBase ARRAYBASEREF;
#endif

// Note that the name must always be "" or "en-US".  Other cases and nulls
// aren't allowed (we already checked.)
__inline bool IsCultureEnglishOrInvariant(LPCWSTR localeName)
{
    LIMITED_METHOD_CONTRACT;
    if (localeName != NULL &&
        (localeName[0] == W('\0') ||
         wcscmp(localeName, W("en-US")) == 0))
    {
        return true;
    }
    return false;
    }

class CultureInfoBaseObject : public Object
{
    friend class MscorlibBinder;

private:
    OBJECTREF compareInfo;
    OBJECTREF textInfo;
    OBJECTREF numInfo;
    OBJECTREF dateTimeInfo;
    OBJECTREF calendar;
    OBJECTREF _cultureData;
    OBJECTREF _consoleFallbackCulture;
    STRINGREF _name;                       // "real" name - en-US, de-DE_phoneb or fj-FJ
    STRINGREF _nonSortName;                // name w/o sort info (de-DE for de-DE_phoneb)
    STRINGREF _sortName;                   // Sort only name (de-DE_phoneb, en-us for fj-fj (w/us sort)
    CULTUREINFOBASEREF _parent;
    CLR_BOOL _isReadOnly;
    CLR_BOOL _isInherited;
    CLR_BOOL _useUserOverride;

public:
    CULTUREINFOBASEREF GetParent()
    {
        LIMITED_METHOD_CONTRACT;
        return _parent;
    }// GetParent


    STRINGREF GetName()
    {
        LIMITED_METHOD_CONTRACT;
        return _name;
    }// GetName

}; // class CultureInfoBaseObject

typedef DPTR(class ThreadBaseObject) PTR_ThreadBaseObject;
class ThreadBaseObject : public Object
{
    friend class ClrDataAccess;
    friend class ThreadNative;
    friend class MscorlibBinder;
    friend class Object;

private:

    // These field are also defined in the managed representation.  If you
    //  add or change these field you must also change the managed code so that
    //  it matches these.  This is necessary so that the object is the proper
    //  size.  The order here must match that order which the loader will choose
    //  when laying out the managed class.  Note that the layouts are checked
    //  at run time, not compile time.
    OBJECTREF     m_ExecutionContext;
    OBJECTREF     m_SynchronizationContext;
    STRINGREF     m_Name;
    OBJECTREF     m_Delegate;
    OBJECTREF     m_ThreadStartArg;

    // The next field (m_InternalThread) is declared as IntPtr in the managed
    // definition of Thread.  The loader will sort it next.

    // m_InternalThread is always valid -- unless the thread has finalized and been
    // resurrected.  (The thread stopped running before it was finalized).
    Thread       *m_InternalThread;
    INT32         m_Priority;    

    //We need to cache the thread id in managed code for perf reasons.
    INT32         m_ManagedThreadId;

protected:
    // the ctor and dtor can do no useful work.
    ThreadBaseObject() {LIMITED_METHOD_CONTRACT;};
   ~ThreadBaseObject() {LIMITED_METHOD_CONTRACT;};

public:
    Thread   *GetInternal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_InternalThread;
    }

    void SetInternal(Thread *it);
    void ClearInternal();

    INT32 GetManagedThreadId()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ManagedThreadId;
    }

    void SetManagedThreadId(INT32 id)
    {
        LIMITED_METHOD_CONTRACT;
        m_ManagedThreadId = id;
    }

    OBJECTREF GetThreadStartArg() { LIMITED_METHOD_CONTRACT; return m_ThreadStartArg; }
    void SetThreadStartArg(OBJECTREF newThreadStartArg) 
    {
        WRAPPER_NO_CONTRACT;
    
        _ASSERTE(newThreadStartArg == NULL);
        // Note: this is an unchecked assignment.  We are cleaning out the ThreadStartArg field when 
        // a thread starts so that ADU does not cause problems
        SetObjectReferenceUnchecked( (OBJECTREF *)&m_ThreadStartArg, newThreadStartArg);
    
    }

    STRINGREF GetName() {
        LIMITED_METHOD_CONTRACT;
        return m_Name;
    }
    OBJECTREF GetDelegate()                   { LIMITED_METHOD_CONTRACT; return m_Delegate; }
    void      SetDelegate(OBJECTREF delegate);

    CULTUREINFOBASEREF GetCurrentUserCulture();
    CULTUREINFOBASEREF GetCurrentUICulture();
    OBJECTREF GetManagedThreadCulture(BOOL bUICulture);
    void ResetManagedThreadCulture(BOOL bUICulture);
    void ResetCurrentUserCulture();
    void ResetCurrentUICulture();



    OBJECTREF GetSynchronizationContext()
    {
        LIMITED_METHOD_CONTRACT;
        return m_SynchronizationContext;
    }

    // SetDelegate is our "constructor" for the pathway where the exposed object is
    // created first.  InitExisting is our "constructor" for the pathway where an
    // existing physical thread is later exposed.
    void      InitExisting();

    void ResetCulture()
    {
        LIMITED_METHOD_CONTRACT;
        ResetCurrentUserCulture();
        ResetCurrentUICulture();
    }

    void ResetName()
    {
        LIMITED_METHOD_CONTRACT;
        m_Name = NULL;
    }
  
    void SetPriority(INT32 priority)
    {
        LIMITED_METHOD_CONTRACT;
        m_Priority = priority;
    }
    
    INT32 GetPriority() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_Priority;
    }
};

// MarshalByRefObjectBaseObject 
// This class is the base class for MarshalByRefObject
//  
class MarshalByRefObjectBaseObject : public Object
{
};

// AppDomainBaseObject 
// This class is the base class for application domains
//  
class AppDomainBaseObject : public MarshalByRefObjectBaseObject
{
    friend class AppDomain;
    friend class MscorlibBinder;

  protected:
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class definition of this object.
    OBJECTREF    m_pAssemblyEventHandler; // Delegate for 'loading assembly' event
    OBJECTREF    m_pTypeEventHandler;     // Delegate for 'resolve type' event
    OBJECTREF    m_pResourceEventHandler; // Delegate for 'resolve resource' event
    OBJECTREF    m_pAsmResolveEventHandler; // Delegate for 'resolve assembly' event
    OBJECTREF    m_pProcessExitEventHandler; // Delegate for 'process exit' event.  Only used in Default appdomain.
    OBJECTREF    m_pDomainUnloadEventHandler; // Delegate for 'about to unload domain' event
    OBJECTREF    m_pUnhandledExceptionEventHandler; // Delegate for 'unhandled exception' event

    OBJECTREF    m_pFirstChanceExceptionHandler; // Delegate for 'FirstChance Exception' event

    AppDomain*   m_pDomain;            // Pointer to the BaseDomain Structure

  protected:
    AppDomainBaseObject() { LIMITED_METHOD_CONTRACT; }
   ~AppDomainBaseObject() { LIMITED_METHOD_CONTRACT; }
   
  public:

    void SetDomain(AppDomain* p) 
    {
        LIMITED_METHOD_CONTRACT;
        m_pDomain = p;
    }
    AppDomain* GetDomain() 
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDomain;
    }

    // Returns the reference to the delegate of the first chance exception notification handler
    OBJECTREF GetFirstChanceExceptionNotificationHandler()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pFirstChanceExceptionHandler;
    }
};

// AssemblyBaseObject 
// This class is the base class for assemblies
//  
class AssemblyBaseObject : public Object
{
    friend class Assembly;
    friend class MscorlibBinder;

  protected:
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class definition of this object.
    OBJECTREF     m_pModuleEventHandler;   // Delegate for 'resolve module' event
    STRINGREF     m_fullname;              // Slot for storing assemblies fullname
    OBJECTREF     m_pSyncRoot;             // Pointer to loader allocator to keep collectible types alive, and to serve as the syncroot for assembly building in ref.emit
    DomainAssembly* m_pAssembly;           // Pointer to the Assembly Structure

  protected:
    AssemblyBaseObject() { LIMITED_METHOD_CONTRACT; }
   ~AssemblyBaseObject() { LIMITED_METHOD_CONTRACT; }
   
  public:

    void SetAssembly(DomainAssembly* p) 
    {
        LIMITED_METHOD_CONTRACT;
        m_pAssembly = p;
    }

    DomainAssembly* GetDomainAssembly() 
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAssembly;
    }

    Assembly* GetAssembly();

    void SetSyncRoot(OBJECTREF pSyncRoot)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReferenceUnchecked(&m_pSyncRoot, pSyncRoot);
    }
};
NOINLINE AssemblyBaseObject* GetRuntimeAssemblyHelper(LPVOID __me, DomainAssembly *pAssembly, OBJECTREF keepAlive);
#define FC_RETURN_ASSEMBLY_OBJECT(pAssembly, refKeepAlive) FC_INNER_RETURN(AssemblyBaseObject*, GetRuntimeAssemblyHelper(__me, pAssembly, refKeepAlive))

// AssemblyNameBaseObject 
// This class is the base class for assembly names
//  
class AssemblyNameBaseObject : public Object
{
    friend class AssemblyNative;
    friend class AppDomainNative;
    friend class MscorlibBinder;

  protected:
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class definition of this object.

    OBJECTREF     _name; 
    U1ARRAYREF    _publicKey;
    U1ARRAYREF    _publicKeyToken;
    OBJECTREF     _cultureInfo;
    OBJECTREF     _codeBase;
    OBJECTREF     _version;
    OBJECTREF     _strongNameKeyPair;
    U1ARRAYREF    _hashForControl;
    DWORD         _hashAlgorithm;
    DWORD         _hashAlgorithmForControl;
    DWORD         _versionCompatibility;
    DWORD         _flags;

  protected:
    AssemblyNameBaseObject() { LIMITED_METHOD_CONTRACT; }
   ~AssemblyNameBaseObject() { LIMITED_METHOD_CONTRACT; }
   
  public:
    OBJECTREF GetSimpleName() { LIMITED_METHOD_CONTRACT; return _name; }
    U1ARRAYREF GetPublicKey() { LIMITED_METHOD_CONTRACT; return _publicKey; }
    U1ARRAYREF GetPublicKeyToken() { LIMITED_METHOD_CONTRACT; return _publicKeyToken; }
    OBJECTREF GetStrongNameKeyPair() { LIMITED_METHOD_CONTRACT; return _strongNameKeyPair; }
    OBJECTREF GetCultureInfo() { LIMITED_METHOD_CONTRACT; return _cultureInfo; }
    OBJECTREF GetAssemblyCodeBase() { LIMITED_METHOD_CONTRACT; return _codeBase; }
    OBJECTREF GetVersion() { LIMITED_METHOD_CONTRACT; return _version; }
    DWORD GetAssemblyHashAlgorithm() { LIMITED_METHOD_CONTRACT; return _hashAlgorithm; }
    DWORD GetFlags() { LIMITED_METHOD_CONTRACT; return _flags; }
    U1ARRAYREF GetHashForControl() { LIMITED_METHOD_CONTRACT; return _hashForControl;}
    DWORD GetHashAlgorithmForControl() { LIMITED_METHOD_CONTRACT; return _hashAlgorithmForControl; }
};

// VersionBaseObject
// This class is the base class for versions
//
class VersionBaseObject : public Object
{
    friend class MscorlibBinder;

  protected:
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class definition of this object.

    int m_Major;
    int m_Minor;
    int m_Build;
    int m_Revision;
 
    VersionBaseObject() {LIMITED_METHOD_CONTRACT;}
   ~VersionBaseObject() {LIMITED_METHOD_CONTRACT;}

  public:    
    int GetMajor() { LIMITED_METHOD_CONTRACT; return m_Major; }
    int GetMinor() { LIMITED_METHOD_CONTRACT; return m_Minor; }
    int GetBuild() { LIMITED_METHOD_CONTRACT; return m_Build; }
    int GetRevision() { LIMITED_METHOD_CONTRACT; return m_Revision; }
};

class WeakReferenceObject : public Object
{
public:
    Volatile<OBJECTHANDLE> m_Handle;
};

#ifdef USE_CHECKED_OBJECTREFS

typedef REF<ReflectModuleBaseObject> REFLECTMODULEBASEREF;

typedef REF<ReflectClassBaseObject> REFLECTCLASSBASEREF;

typedef REF<ReflectMethodObject> REFLECTMETHODREF;

typedef REF<ReflectFieldObject> REFLECTFIELDREF;

typedef REF<ThreadBaseObject> THREADBASEREF;

typedef REF<AppDomainBaseObject> APPDOMAINREF;

typedef REF<MarshalByRefObjectBaseObject> MARSHALBYREFOBJECTBASEREF;

typedef REF<AssemblyBaseObject> ASSEMBLYREF;

typedef REF<AssemblyNameBaseObject> ASSEMBLYNAMEREF;

typedef REF<VersionBaseObject> VERSIONREF;


typedef REF<WeakReferenceObject> WEAKREFERENCEREF;

inline ARG_SLOT ObjToArgSlot(OBJECTREF objRef)
{
    LIMITED_METHOD_CONTRACT;
    LPVOID v;
    v = OBJECTREFToObject(objRef);
    return (ARG_SLOT)(SIZE_T)v;
}

inline OBJECTREF ArgSlotToObj(ARG_SLOT i)
{
    LIMITED_METHOD_CONTRACT;
    LPVOID v;
    v = (LPVOID)(SIZE_T)i;
    return ObjectToOBJECTREF ((Object*)v);
}

inline ARG_SLOT StringToArgSlot(STRINGREF sr)
{
    LIMITED_METHOD_CONTRACT;
    LPVOID v;
    v = OBJECTREFToObject(sr);
    return (ARG_SLOT)(SIZE_T)v;
}

inline STRINGREF ArgSlotToString(ARG_SLOT s)
{
    LIMITED_METHOD_CONTRACT;
    LPVOID v;
    v = (LPVOID)(SIZE_T)s;
    return ObjectToSTRINGREF ((StringObject*)v);
}

#else // USE_CHECKED_OBJECTREFS

typedef PTR_ReflectModuleBaseObject REFLECTMODULEBASEREF;
typedef PTR_ReflectClassBaseObject REFLECTCLASSBASEREF;
typedef PTR_ReflectMethodObject REFLECTMETHODREF;
typedef PTR_ReflectFieldObject REFLECTFIELDREF;
typedef PTR_ThreadBaseObject THREADBASEREF;
typedef PTR_AppDomainBaseObject APPDOMAINREF;
typedef PTR_AssemblyBaseObject ASSEMBLYREF;
typedef PTR_AssemblyNameBaseObject ASSEMBLYNAMEREF;

#ifndef DACCESS_COMPILE
typedef MarshalByRefObjectBaseObject* MARSHALBYREFOBJECTBASEREF;
typedef VersionBaseObject* VERSIONREF;
typedef WeakReferenceObject* WEAKREFERENCEREF;
#endif // #ifndef DACCESS_COMPILE

#define ObjToArgSlot(objref) ((ARG_SLOT)(SIZE_T)(objref))
#define ArgSlotToObj(s) ((OBJECTREF)(SIZE_T)(s))

#define StringToArgSlot(objref) ((ARG_SLOT)(SIZE_T)(objref))
#define ArgSlotToString(s)    ((STRINGREF)(SIZE_T)(s))

#endif //USE_CHECKED_OBJECTREFS

#define PtrToArgSlot(ptr) ((ARG_SLOT)(SIZE_T)(ptr))
#define ArgSlotToPtr(s)   ((LPVOID)(SIZE_T)(s))

#define BoolToArgSlot(b)  ((ARG_SLOT)(CLR_BOOL)(!!(b)))
#define ArgSlotToBool(s)  ((BOOL)(s))

STRINGREF AllocateString(SString sstr);
CHARARRAYREF AllocateCharArray(DWORD dwArrayLength);

#ifdef FEATURE_COMINTEROP

//-------------------------------------------------------------
// class ComObject, Exposed class __ComObject
// 
// 
//-------------------------------------------------------------
class ComObject : public MarshalByRefObjectBaseObject
{
    friend class MscorlibBinder;

protected:

    ComObject()
    {LIMITED_METHOD_CONTRACT;}; // don't instantiate this class directly
    ~ComObject(){LIMITED_METHOD_CONTRACT;};

public:
    OBJECTREF           m_ObjectToDataMap;

    //--------------------------------------------------------------------
    // SupportsInterface
    static BOOL SupportsInterface(OBJECTREF oref, MethodTable* pIntfTable);

    //--------------------------------------------------------------------
    // SupportsInterface
    static void ThrowInvalidCastException(OBJECTREF *pObj, MethodTable* pCastToMT);

    //-----------------------------------------------------------------
    // GetComIPFromRCW
    static IUnknown* GetComIPFromRCW(OBJECTREF *pObj, MethodTable* pIntfTable);

    //-----------------------------------------------------------------
    // GetComIPFromRCWThrowing
    static IUnknown* GetComIPFromRCWThrowing(OBJECTREF *pObj, MethodTable* pIntfTable);

    //-----------------------------------------------------------
    // create an empty ComObjectRef
    static OBJECTREF CreateComObjectRef(MethodTable* pMT);

    //-----------------------------------------------------------
    // Release all the data associated with the __ComObject.
    static void ReleaseAllData(OBJECTREF oref);

    //-----------------------------------------------------------
    // Redirection for ToString
    static FCDECL1(MethodDesc *, GetRedirectedToStringMD, Object *pThisUNSAFE);
    static FCDECL2(StringObject *, RedirectToString, Object *pThisUNSAFE, MethodDesc *pToStringMD);    

    //-----------------------------------------------------------
    // Redirection for GetHashCode
    static FCDECL1(MethodDesc *, GetRedirectedGetHashCodeMD, Object *pThisUNSAFE);
    static FCDECL2(int, RedirectGetHashCode, Object *pThisUNSAFE, MethodDesc *pGetHashCodeMD);    

    //-----------------------------------------------------------
    // Redirection for Equals
    static FCDECL1(MethodDesc *, GetRedirectedEqualsMD, Object *pThisUNSAFE);
    static FCDECL3(FC_BOOL_RET, RedirectEquals, Object *pThisUNSAFE, Object *pOtherUNSAFE, MethodDesc *pEqualsMD);    
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ComObject> COMOBJECTREF;
#else
typedef ComObject*     COMOBJECTREF;
#endif


//-------------------------------------------------------------
// class UnknownWrapper, Exposed class UnknownWrapper
// 
// 
//-------------------------------------------------------------
class UnknownWrapper : public Object
{
protected:

    UnknownWrapper(UnknownWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    UnknownWrapper() {LIMITED_METHOD_CONTRACT;}; // don't instantiate this class directly
    ~UnknownWrapper() {LIMITED_METHOD_CONTRACT;};

    OBJECTREF m_WrappedObject;

public:
    OBJECTREF GetWrappedObject()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WrappedObject;
    }

    void SetWrappedObject(OBJECTREF pWrappedObject)
    {
        LIMITED_METHOD_CONTRACT;
        m_WrappedObject = pWrappedObject;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<UnknownWrapper> UNKNOWNWRAPPEROBJECTREF;
#else
typedef UnknownWrapper*     UNKNOWNWRAPPEROBJECTREF;
#endif


//-------------------------------------------------------------
// class DispatchWrapper, Exposed class DispatchWrapper
// 
// 
//-------------------------------------------------------------
class DispatchWrapper : public Object
{
protected:

    DispatchWrapper(DispatchWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    DispatchWrapper() {LIMITED_METHOD_CONTRACT;}; // don't instantiate this class directly
    ~DispatchWrapper() {LIMITED_METHOD_CONTRACT;};

    OBJECTREF m_WrappedObject;

public:
    OBJECTREF GetWrappedObject()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WrappedObject;
    }

    void SetWrappedObject(OBJECTREF pWrappedObject)
    {
        LIMITED_METHOD_CONTRACT;
        m_WrappedObject = pWrappedObject;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<DispatchWrapper> DISPATCHWRAPPEROBJECTREF;
#else
typedef DispatchWrapper*     DISPATCHWRAPPEROBJECTREF;
#endif


//-------------------------------------------------------------
// class VariantWrapper, Exposed class VARIANTWRAPPEROBJECTREF
// 
// 
//-------------------------------------------------------------
class VariantWrapper : public Object
{
protected:

    VariantWrapper(VariantWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    VariantWrapper() {LIMITED_METHOD_CONTRACT}; // don't instantiate this class directly
    ~VariantWrapper() {LIMITED_METHOD_CONTRACT};

    OBJECTREF m_WrappedObject;

public:
    OBJECTREF GetWrappedObject()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WrappedObject;
    }

    void SetWrappedObject(OBJECTREF pWrappedObject)
    {
        LIMITED_METHOD_CONTRACT;
        m_WrappedObject = pWrappedObject;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<VariantWrapper> VARIANTWRAPPEROBJECTREF;
#else
typedef VariantWrapper*     VARIANTWRAPPEROBJECTREF;
#endif


//-------------------------------------------------------------
// class ErrorWrapper, Exposed class ErrorWrapper
// 
// 
//-------------------------------------------------------------
class ErrorWrapper : public Object
{
protected:

    ErrorWrapper(ErrorWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    ErrorWrapper() {LIMITED_METHOD_CONTRACT;}; // don't instantiate this class directly
    ~ErrorWrapper() {LIMITED_METHOD_CONTRACT;};

    INT32 m_ErrorCode;

public:
    INT32 GetErrorCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ErrorCode;
    }

    void SetErrorCode(int ErrorCode)
    {
        LIMITED_METHOD_CONTRACT;
        m_ErrorCode = ErrorCode;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ErrorWrapper> ERRORWRAPPEROBJECTREF;
#else
typedef ErrorWrapper*     ERRORWRAPPEROBJECTREF;
#endif


//-------------------------------------------------------------
// class CurrencyWrapper, Exposed class CurrencyWrapper
// 
// 
//-------------------------------------------------------------

// Keep this in sync with code:MethodTableBuilder.CheckForSystemTypes where
// alignment requirement of the managed System.Decimal structure is computed.
#if !defined(ALIGN_ACCESS) && !defined(FEATURE_64BIT_ALIGNMENT)
#include <pshpack4.h>
#endif // !ALIGN_ACCESS && !FEATURE_64BIT_ALIGNMENT

class CurrencyWrapper : public Object
{
protected:

    CurrencyWrapper(CurrencyWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    CurrencyWrapper() {LIMITED_METHOD_CONTRACT;}; // don't instantiate this class directly
    ~CurrencyWrapper() {LIMITED_METHOD_CONTRACT;};

    DECIMAL m_WrappedObject;

public:
    DECIMAL GetWrappedObject()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WrappedObject;
    }

    void SetWrappedObject(DECIMAL WrappedObj)
    {
        LIMITED_METHOD_CONTRACT;
        m_WrappedObject = WrappedObj;
    }
};

#if !defined(ALIGN_ACCESS) && !defined(FEATURE_64BIT_ALIGNMENT)
#include <poppack.h>
#endif // !ALIGN_ACCESS && !FEATURE_64BIT_ALIGNMENT

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<CurrencyWrapper> CURRENCYWRAPPEROBJECTREF;
#else
typedef CurrencyWrapper*     CURRENCYWRAPPEROBJECTREF;
#endif

//-------------------------------------------------------------
// class BStrWrapper, Exposed class BSTRWRAPPEROBJECTREF
// 
// 
//-------------------------------------------------------------
class BStrWrapper : public Object
{
protected:

    BStrWrapper(BStrWrapper &wrap) {LIMITED_METHOD_CONTRACT}; // dissalow copy construction.
    BStrWrapper() {LIMITED_METHOD_CONTRACT}; // don't instantiate this class directly
    ~BStrWrapper() {LIMITED_METHOD_CONTRACT};

    STRINGREF m_WrappedObject;

public:
    STRINGREF GetWrappedObject()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WrappedObject;
    }

    void SetWrappedObject(STRINGREF pWrappedObject)
    {
        LIMITED_METHOD_CONTRACT;
        m_WrappedObject = pWrappedObject;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<BStrWrapper> BSTRWRAPPEROBJECTREF;
#else
typedef BStrWrapper*     BSTRWRAPPEROBJECTREF;
#endif

#endif // FEATURE_COMINTEROP

class SafeHandle : public Object
{
    friend class MscorlibBinder;

  private:
    // READ ME:
    //   Modifying the order or fields of this object may require
    //   other changes to the classlib class definition of this
    //   object or special handling when loading this system class.
    Volatile<LPVOID> m_handle;
    Volatile<INT32> m_state;        // Combined ref count and closed/disposed state (for atomicity)
    Volatile<CLR_BOOL> m_ownsHandle;
    Volatile<CLR_BOOL> m_fullyInitialized;  // Did constructor finish?

    // Describe the bits in the m_state field above.
    enum StateBits
    {
        SH_State_Closed     = 0x00000001,
        SH_State_Disposed   = 0x00000002,
        SH_State_RefCount   = 0xfffffffc,
        SH_RefCountOne      = 4,            // Amount to increment state field to yield a ref count increment of 1
    };

    static WORD s_IsInvalidHandleMethodSlot;
    static WORD s_ReleaseHandleMethodSlot;

    static void RunReleaseMethod(SafeHandle* psh);
    BOOL IsFullyInitialized() const { LIMITED_METHOD_CONTRACT; return m_fullyInitialized; }

  public:
    static void Init();

    // To use the SafeHandle from native, look at the SafeHandleHolder, which
    // will do the AddRef & Release for you.
    LPVOID GetHandle() const { 
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(((unsigned int) m_state) >= SH_RefCountOne);
        return m_handle;
    }

    BOOL OwnsHandle() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_ownsHandle;
    }

    static size_t GetHandleOffset() { LIMITED_METHOD_CONTRACT; return offsetof(SafeHandle, m_handle); }

    void AddRef();
    void Release(bool fDispose = false);
    void Dispose();
    void SetHandle(LPVOID handle);

    static FCDECL1(void, DisposeNative, SafeHandle* refThisUNSAFE);
    static FCDECL1(void, Finalize, SafeHandle* refThisUNSAFE);
    static FCDECL1(void, SetHandleAsInvalid, SafeHandle* refThisUNSAFE);
    static FCDECL2(void, DangerousAddRef, SafeHandle* refThisUNSAFE, CLR_BOOL *pfSuccess);
    static FCDECL1(void, DangerousRelease, SafeHandle* refThisUNSAFE);
};

// SAFEHANDLEREF defined above because CompressedStackObject needs it

void AcquireSafeHandle(SAFEHANDLEREF* s);
void ReleaseSafeHandle(SAFEHANDLEREF* s);

typedef Holder<SAFEHANDLEREF*, AcquireSafeHandle, ReleaseSafeHandle> SafeHandleHolder;

class CriticalHandle : public Object
{
    friend class MscorlibBinder;

  private:
    // READ ME:
    //   Modifying the order or fields of this object may require
    //   other changes to the classlib class definition of this
    //   object or special handling when loading this system class.
    Volatile<LPVOID> m_handle;
    Volatile<CLR_BOOL> m_isClosed;

  public:
    LPVOID GetHandle() const { LIMITED_METHOD_CONTRACT; return m_handle; }
    static size_t GetHandleOffset() { LIMITED_METHOD_CONTRACT; return offsetof(CriticalHandle, m_handle); }

    void SetHandle(LPVOID handle) { LIMITED_METHOD_CONTRACT; m_handle = handle; }

    static FCDECL1(void, FireCustomerDebugProbe, CriticalHandle* refThisUNSAFE);
};


#ifdef USE_CHECKED_OBJECTREFS
typedef REF<CriticalHandle> CRITICALHANDLE;
typedef REF<CriticalHandle> CRITICALHANDLEREF;
#else // USE_CHECKED_OBJECTREFS
typedef CriticalHandle * CRITICALHANDLE;
typedef CriticalHandle * CRITICALHANDLEREF;
#endif // USE_CHECKED_OBJECTREFS

// WaitHandleBase
// Base class for WaitHandle 
class WaitHandleBase :public MarshalByRefObjectBaseObject
{
    friend class WaitHandleNative;
    friend class MscorlibBinder;

public:
    __inline LPVOID GetWaitHandle() {LIMITED_METHOD_CONTRACT; return m_handle;}
    __inline SAFEHANDLEREF GetSafeHandle() {LIMITED_METHOD_CONTRACT; return m_safeHandle;}

private:
    SAFEHANDLEREF   m_safeHandle;
    LPVOID          m_handle;
    CLR_BOOL        m_hasThreadAffinity;
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<WaitHandleBase> WAITHANDLEREF;
#else // USE_CHECKED_OBJECTREFS
typedef WaitHandleBase* WAITHANDLEREF;
#endif // USE_CHECKED_OBJECTREFS

// This class corresponds to System.MulticastDelegate on the managed side.
class DelegateObject : public Object
{
    friend class CheckAsmOffsets;
    friend class MscorlibBinder;

public:
    BOOL IsWrapperDelegate() { LIMITED_METHOD_CONTRACT; return _methodPtrAux == NULL; }
    
    OBJECTREF GetTarget() { LIMITED_METHOD_CONTRACT; return _target; }
    void SetTarget(OBJECTREF target) { WRAPPER_NO_CONTRACT; SetObjectReference(&_target, target, GetAppDomain()); }
    static int GetOffsetOfTarget() { LIMITED_METHOD_CONTRACT; return offsetof(DelegateObject, _target); }

    PCODE GetMethodPtr() { LIMITED_METHOD_CONTRACT; return _methodPtr; }
    void SetMethodPtr(PCODE methodPtr) { LIMITED_METHOD_CONTRACT; _methodPtr = methodPtr; }
    static int GetOffsetOfMethodPtr() { LIMITED_METHOD_CONTRACT; return offsetof(DelegateObject, _methodPtr); }

    PCODE GetMethodPtrAux() { LIMITED_METHOD_CONTRACT; return _methodPtrAux; }
    void SetMethodPtrAux(PCODE methodPtrAux) { LIMITED_METHOD_CONTRACT; _methodPtrAux = methodPtrAux; }
    static int GetOffsetOfMethodPtrAux() { LIMITED_METHOD_CONTRACT; return offsetof(DelegateObject, _methodPtrAux); }

    OBJECTREF GetInvocationList() { LIMITED_METHOD_CONTRACT; return _invocationList; }
    void SetInvocationList(OBJECTREF invocationList) { WRAPPER_NO_CONTRACT; SetObjectReference(&_invocationList, invocationList, GetAppDomain()); }
    static int GetOffsetOfInvocationList() { LIMITED_METHOD_CONTRACT; return offsetof(DelegateObject, _invocationList); }

    INT_PTR GetInvocationCount() { LIMITED_METHOD_CONTRACT; return _invocationCount; }
    void SetInvocationCount(INT_PTR invocationCount) { LIMITED_METHOD_CONTRACT; _invocationCount = invocationCount; }
    static int GetOffsetOfInvocationCount() { LIMITED_METHOD_CONTRACT; return offsetof(DelegateObject, _invocationCount); }

    void SetMethodBase(OBJECTREF newMethodBase) { LIMITED_METHOD_CONTRACT; SetObjectReference((OBJECTREF*)&_methodBase, newMethodBase, GetAppDomain()); }

    // README:
    // If you modify the order of these fields, make sure to update the definition in 
    // BCL for this object.
private:
    // System.Delegate
    OBJECTREF   _target;
    OBJECTREF   _methodBase;
    PCODE       _methodPtr;
    PCODE       _methodPtrAux;
    // System.MulticastDelegate
    OBJECTREF   _invocationList;
    INT_PTR     _invocationCount;
};

#define OFFSETOF__DelegateObject__target          OBJECT_SIZE /* m_pMethTab */
#define OFFSETOF__DelegateObject__methodPtr       (OFFSETOF__DelegateObject__target + TARGET_POINTER_SIZE /* _target */ + TARGET_POINTER_SIZE /* _methodBase */)
#define OFFSETOF__DelegateObject__methodPtrAux    (OFFSETOF__DelegateObject__methodPtr + TARGET_POINTER_SIZE /* _methodPtr */)

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<DelegateObject> DELEGATEREF;
#else // USE_CHECKED_OBJECTREFS
typedef DelegateObject* DELEGATEREF;
#endif // USE_CHECKED_OBJECTREFS


struct StackTraceElement;
class ClrDataAccess;


typedef DPTR(StackTraceElement) PTR_StackTraceElement;

class StackTraceArray
{
    struct ArrayHeader
    {
        size_t m_size;
        Thread * m_thread;
    };

    typedef DPTR(ArrayHeader) PTR_ArrayHeader;

public:
    StackTraceArray()
        : m_array(static_cast<I1Array *>(NULL))
    {
        WRAPPER_NO_CONTRACT;
    }
    
    StackTraceArray(I1ARRAYREF array)
        : m_array(array)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void Swap(StackTraceArray & rhs)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SUPPORTS_DAC;
        I1ARRAYREF t = m_array;
        m_array = rhs.m_array;
        rhs.m_array = t;
    }
    
    size_t Size() const
    {
        WRAPPER_NO_CONTRACT;
        if (!m_array)
            return 0;
        else
            return GetSize();
    }
    
    StackTraceElement const & operator[](size_t index) const;
    StackTraceElement & operator[](size_t index);

    void Append(StackTraceElement const * begin, StackTraceElement const * end);

    I1ARRAYREF Get() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_array;
    }

    // Deep copies the array
    void CopyFrom(StackTraceArray const & src);
    
private:
    StackTraceArray(StackTraceArray const & rhs) = delete;

    StackTraceArray & operator=(StackTraceArray const & rhs) = delete;

    void Grow(size_t size);
    void EnsureThreadAffinity();
    void CheckState() const;

    size_t Capacity() const
    {
        WRAPPER_NO_CONTRACT;
        assert(!!m_array);

        return m_array->GetNumComponents();
    }
    
    size_t GetSize() const
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->m_size;
    }

    void SetSize(size_t size)
    {
        WRAPPER_NO_CONTRACT;
        GetHeader()->m_size = size;
    }

    Thread * GetObjectThread() const
    {
        WRAPPER_NO_CONTRACT;
        return GetHeader()->m_thread;
    }

    void SetObjectThread()
    {
        WRAPPER_NO_CONTRACT;
        GetHeader()->m_thread = GetThread();
    }

    StackTraceElement const * GetData() const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_StackTraceElement>(GetRaw() + sizeof(ArrayHeader));
    }

    PTR_StackTraceElement GetData()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_StackTraceElement>(GetRaw() + sizeof(ArrayHeader));
    }

    I1 const * GetRaw() const
    {
        WRAPPER_NO_CONTRACT;
        assert(!!m_array);

        return const_cast<I1ARRAYREF &>(m_array)->GetDirectPointerToNonObjectElements();
    }

    PTR_I1 GetRaw()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        assert(!!m_array);

        return dac_cast<PTR_I1>(m_array->GetDirectPointerToNonObjectElements());
    }

    ArrayHeader const * GetHeader() const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_ArrayHeader>(GetRaw());
    }

    PTR_ArrayHeader GetHeader()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_ArrayHeader>(GetRaw());
    }

    void SetArray(I1ARRAYREF const & arr)
    {
        LIMITED_METHOD_CONTRACT;
        m_array = arr;
    }

private:
    // put only things here that can be protected with GCPROTECT
    I1ARRAYREF m_array;
};

#ifdef FEATURE_COLLECTIBLE_TYPES

class LoaderAllocatorScoutObject : public Object
{
    friend class MscorlibBinder;
    friend class LoaderAllocatorObject;

protected:
    LoaderAllocator * m_nativeLoaderAllocator;
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<LoaderAllocatorScoutObject> LOADERALLOCATORSCOUTREF;
#else // USE_CHECKED_OBJECTREFS
typedef LoaderAllocatorScoutObject* LOADERALLOCATORSCOUTREF;
#endif // USE_CHECKED_OBJECTREFS

class LoaderAllocatorObject : public Object
{
    friend class MscorlibBinder;

public:
    PTRARRAYREF GetHandleTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (PTRARRAYREF)m_pSlots;
    }

    void SetHandleTable(PTRARRAYREF handleTable)
    {
        LIMITED_METHOD_CONTRACT;
        SetObjectReferenceUnchecked(&m_pSlots, (OBJECTREF)handleTable);
    }

    INT32 GetSlotsUsed()
    {
        LIMITED_METHOD_CONTRACT;
        return m_slotsUsed;
    }

    void SetSlotsUsed(INT32 newSlotsUsed)
    {
        LIMITED_METHOD_CONTRACT;
        m_slotsUsed = newSlotsUsed;
    }

    void SetNativeLoaderAllocator(LoaderAllocator * pLoaderAllocator)
    {
        LIMITED_METHOD_CONTRACT;
        m_pLoaderAllocatorScout->m_nativeLoaderAllocator = pLoaderAllocator;
    }

    // README:
    // If you modify the order of these fields, make sure to update the definition in 
    // BCL for this object.
protected:
    LOADERALLOCATORSCOUTREF m_pLoaderAllocatorScout;
    OBJECTREF   m_pSlots;
    INT32       m_slotsUsed;
    OBJECTREF   m_methodInstantiationsTable;
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<LoaderAllocatorObject> LOADERALLOCATORREF;
#else // USE_CHECKED_OBJECTREFS
typedef DPTR(LoaderAllocatorObject) PTR_LoaderAllocatorObject;
typedef PTR_LoaderAllocatorObject LOADERALLOCATORREF;
#endif // USE_CHECKED_OBJECTREFS

#endif // FEATURE_COLLECTIBLE_TYPES

#if !defined(DACCESS_COMPILE)
// Define the lock used to access stacktrace from an exception object
EXTERN_C SpinLock g_StackTraceArrayLock;
#endif // !defined(DACCESS_COMPILE)

// This class corresponds to Exception on the managed side.
typedef DPTR(class ExceptionObject) PTR_ExceptionObject;
#include "pshpack4.h"
class ExceptionObject : public Object
{
    friend class MscorlibBinder;

public:
    void SetHResult(HRESULT hr)
    {
        LIMITED_METHOD_CONTRACT;
        _HResult = hr;
    }

    HRESULT GetHResult()
    {
        LIMITED_METHOD_CONTRACT;
        return _HResult;
    }

    void SetXCode(DWORD code)
    {
        LIMITED_METHOD_CONTRACT;
        _xcode = code;
    }

    DWORD GetXCode()
    {
        LIMITED_METHOD_CONTRACT;
        return _xcode;
    }

    void SetXPtrs(void* xptrs)
    {
        LIMITED_METHOD_CONTRACT;
        _xptrs = xptrs;
    }

    void* GetXPtrs()
    {
        LIMITED_METHOD_CONTRACT;
        return _xptrs;
    }

    void SetStackTrace(StackTraceArray const & stackTrace, PTRARRAYREF dynamicMethodArray);
    void SetNullStackTrace();

    void GetStackTrace(StackTraceArray & stackTrace, PTRARRAYREF * outDynamicMethodArray = NULL) const;

#ifdef DACCESS_COMPILE
    I1ARRAYREF GetStackTraceArrayObject() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return _stackTrace;
    }
#endif // DACCESS_COMPILE

    void SetInnerException(OBJECTREF innerException)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_innerException, (OBJECTREF)innerException, GetAppDomain());
    }

    OBJECTREF GetInnerException()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return _innerException;
    }

    // Returns the innermost exception object - equivalent of the
    // managed System.Exception.GetBaseException method.
    OBJECTREF GetBaseException()
    {
        LIMITED_METHOD_CONTRACT;

        // Loop and get the innermost exception object
        OBJECTREF oInnerMostException = NULL;
        OBJECTREF oCurrent = NULL;

        oCurrent = _innerException;
        while(oCurrent != NULL)
        {
            oInnerMostException = oCurrent;
            oCurrent = ((ExceptionObject*)(Object *)OBJECTREFToObject(oCurrent))->GetInnerException();
        }

        // return the innermost exception
        return oInnerMostException;
    }

    void SetMessage(STRINGREF message)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_message, (OBJECTREF)message, GetAppDomain());
    }

    STRINGREF GetMessage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return _message;
    }

    void SetStackTraceString(STRINGREF stackTraceString)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_stackTraceString, (OBJECTREF)stackTraceString, GetAppDomain());
    }

    STRINGREF GetStackTraceString()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return _stackTraceString;
    }

    STRINGREF GetRemoteStackTraceString()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return _remoteStackTraceString;
    }

    void SetHelpURL(STRINGREF helpURL)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_helpURL, (OBJECTREF)helpURL, GetAppDomain());
    }

    void SetSource(STRINGREF source)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_source, (OBJECTREF)source, GetAppDomain());
    }

    void ClearStackTraceForThrow()
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReferenceUnchecked((OBJECTREF*)&_remoteStackTraceString, NULL);
        SetObjectReferenceUnchecked((OBJECTREF*)&_stackTrace, NULL);
        SetObjectReferenceUnchecked((OBJECTREF*)&_stackTraceString, NULL);
    }

    void ClearStackTracePreservingRemoteStackTrace()
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReferenceUnchecked((OBJECTREF*)&_stackTrace, NULL);
        SetObjectReferenceUnchecked((OBJECTREF*)&_stackTraceString, NULL);
    }

    // This method will set the reference to the array
    // containing the watson bucket information (in byte[] form).
    void SetWatsonBucketReference(OBJECTREF oWatsonBucketArray)
    {
        WRAPPER_NO_CONTRACT;
        SetObjectReference((OBJECTREF*)&_watsonBuckets, (OBJECTREF)oWatsonBucketArray, GetAppDomain());
    }

    // This method will return the reference to the array
    // containing the watson buckets
    U1ARRAYREF GetWatsonBucketReference()
    {
        LIMITED_METHOD_CONTRACT;
        return _watsonBuckets;
    }

    // This method will return a BOOL to indicate if the 
    // watson buckets are present or not.
    BOOL AreWatsonBucketsPresent()
    {
        LIMITED_METHOD_CONTRACT;
        return (_watsonBuckets != NULL)?TRUE:FALSE;
    }

    // This method will save the IP to be used for watson bucketing.
    void SetIPForWatsonBuckets(UINT_PTR ip)
    {
        LIMITED_METHOD_CONTRACT;

        _ipForWatsonBuckets = ip;
    }

    // This method will return a BOOL to indicate if Watson bucketing IP
    // is present (or not).
    BOOL IsIPForWatsonBucketsPresent()
    {
        LIMITED_METHOD_CONTRACT;

        return (_ipForWatsonBuckets != NULL);
    }

    // This method returns the IP for Watson Buckets.
    UINT_PTR GetIPForWatsonBuckets()
    {
        LIMITED_METHOD_CONTRACT;

        return _ipForWatsonBuckets;
    }

    // README:
    // If you modify the order of these fields, make sure to update the definition in 
    // BCL for this object.
private:
    STRINGREF   _className;  //Needed for serialization.
    OBJECTREF   _exceptionMethod;  //Needed for serialization.
    STRINGREF   _message;
    OBJECTREF   _data;
    OBJECTREF   _innerException;
    STRINGREF   _helpURL;
    I1ARRAYREF  _stackTrace;
    U1ARRAYREF  _watsonBuckets;
    STRINGREF   _stackTraceString; //Needed for serialization.
    STRINGREF   _remoteStackTraceString;
    PTRARRAYREF _dynamicMethods;
    STRINGREF   _source;         // Mainly used by VB.

    IN_WIN64(void* _xptrs;)
    IN_WIN64(UINT_PTR    _ipForWatsonBuckets;) // Contains the IP of exception for watson bucketing
    INT32       _remoteStackIndex;
    INT32       _HResult;
    IN_WIN32(void* _xptrs;)
    INT32       _xcode;
    IN_WIN32(UINT_PTR    _ipForWatsonBuckets;) // Contains the IP of exception for watson bucketing
};

// Defined in Contracts.cs
enum ContractFailureKind
{
    CONTRACT_FAILURE_PRECONDITION = 0,
    CONTRACT_FAILURE_POSTCONDITION,
    CONTRACT_FAILURE_POSTCONDITION_ON_EXCEPTION,
    CONTRACT_FAILURE_INVARIANT,
    CONTRACT_FAILURE_ASSERT,
    CONTRACT_FAILURE_ASSUME,
};

typedef DPTR(class ContractExceptionObject) PTR_ContractExceptionObject;
class ContractExceptionObject : public ExceptionObject
{
    friend class MscorlibBinder;

public:
    ContractFailureKind GetContractFailureKind()
    {
        LIMITED_METHOD_CONTRACT;

        return static_cast<ContractFailureKind>(_Kind);
    }

private:
    // keep these in sync with ndp/clr/src/bcl/system/diagnostics/contracts/contractsbcl.cs
    IN_WIN64(INT32 _Kind;)
    STRINGREF _UserMessage;
    STRINGREF _Condition;
    IN_WIN32(INT32 _Kind;)
};
#include "poppack.h"

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ContractExceptionObject> CONTRACTEXCEPTIONREF;
#else // USE_CHECKED_OBJECTREFS
typedef PTR_ContractExceptionObject CONTRACTEXCEPTIONREF;
#endif // USE_CHECKED_OBJECTREFS

//===============================================================================
// #NullableFeature
// #NullableArchitecture
// 
// In a nutshell it is counterintuitive to have a boxed Nullable<T>, since a boxed
// object already has a representation for null (the null pointer), and having 
// multiple representations for the 'not present' value just causes grief.  Thus the
// feature is build make Nullable<T> box to a boxed<T> (not boxed<Nullable<T>).
//
// We want to do this in a way that does not impact the perf of the runtime in the 
// non-nullable case.  
//
// To do this we need to 
//     * Modify the boxing helper code:JIT_Box (we don't need a special one because
//           the JIT inlines the common case, so this only gets call in uncommon cases)
//     * Make a new helper for the Unbox case (see code:JIT_Unbox_Nullable)
//     * Plumb the JIT to ask for what kind of Boxing helper is needed 
//          (see code:CEEInfo.getBoxHelper, code:CEEInfo.getUnBoxHelper
//     * change all the places in the CLR where we box or unbox by hand, and force
//          them to use code:MethodTable.Box, and code:MethodTable.Unbox which in 
//          turn call code:Nullable.Box and code:Nullable.UnBox, most of these 
//          are in reflection, and remoting (passing and returning value types).
//
// #NullableVerification
//
// Sadly, the IL Verifier also needs to know about this change.  Basically the 'box'
// instruction returns a boxed(T) (not a boxed(Nullable<T>)) for the purposes of 
// verfication.  The JIT finds out what box returns by calling back to the EE with
// the code:CEEInfo.getTypeForBox API.  
//
// #NullableDebugging 
//
// Sadly, because the debugger also does its own boxing 'by hand' for expression 
// evaluation inside visual studio, it measn that debuggers also need to be aware
// of the fact that Nullable<T> boxes to a boxed<T>.  It is the responcibility of
// debuggers to follow this convention (which is why this is sad). 
// 

//===============================================================================
// Nullable represents the managed generic value type Nullable<T> 
//
// The runtime has special logic for this value class.  When it is boxed
// it becomes either null or a boxed T.   Similarly a boxed T can be unboxed
// either as a T (as normal), or as a Nullable<T>
//
// See code:Nullable#NullableArchitecture for more. 
//
class Nullable {
    Nullable();   // This is purposefully undefined.  Do not make instances
                  // of this class.  
public:
    static void CheckFieldOffsets(TypeHandle nullableType);
    static BOOL IsNullableType(TypeHandle nullableType);
    static BOOL IsNullableForType(TypeHandle nullableType, MethodTable* paramMT);
    static BOOL IsNullableForTypeNoGC(TypeHandle nullableType, MethodTable* paramMT);

    static OBJECTREF Box(void* src, MethodTable* nullable);
    static BOOL UnBox(void* dest, OBJECTREF boxedVal, MethodTable* destMT);
    static BOOL UnBoxNoGC(void* dest, OBJECTREF boxedVal, MethodTable* destMT);
    static BOOL UnBoxIntoArgNoGC(ArgDestination *argDest, OBJECTREF boxedVal, MethodTable* destMT);
    static void UnBoxNoCheck(void* dest, OBJECTREF boxedVal, MethodTable* destMT);
    static OBJECTREF BoxedNullableNull(TypeHandle nullableType) { return 0; }

    // if 'Obj' is a true boxed nullable, return the form we want (either null or a boxed T)
    static OBJECTREF NormalizeBox(OBJECTREF obj);

    static inline CLR_BOOL HasValue(void *src, MethodTable *nullableMT)
    {
        Nullable *nullable = (Nullable *)src;
        return *(nullable->HasValueAddr(nullableMT));
    }

    static inline void *Value(void *src, MethodTable *nullableMT)
    {
        Nullable *nullable = (Nullable *)src;
        return nullable->ValueAddr(nullableMT);
    }
    
private:
    static BOOL IsNullableForTypeHelper(MethodTable* nullableMT, MethodTable* paramMT);
    static BOOL IsNullableForTypeHelperNoGC(MethodTable* nullableMT, MethodTable* paramMT);

    CLR_BOOL* HasValueAddr(MethodTable* nullableMT);
    void* ValueAddr(MethodTable* nullableMT);
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ExceptionObject> EXCEPTIONREF;
#else // USE_CHECKED_OBJECTREFS
typedef PTR_ExceptionObject EXCEPTIONREF;
#endif // USE_CHECKED_OBJECTREFS

#endif // _OBJECT_H_
