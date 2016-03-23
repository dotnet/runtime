// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// OBJECT.INL
//
// Definitions inline functions of a Com+ Object
//


#ifndef _OBJECT_INL_
#define _OBJECT_INL_

#include "object.h"

inline PTR_VOID Object::UnBox()       // if it is a value class, get the pointer to the first field
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(GetMethodTable()->IsValueType());
    _ASSERTE(!Nullable::IsNullableType(TypeHandle(GetMethodTable())));

    return dac_cast<PTR_BYTE>(this) + sizeof(*this);
}

inline ADIndex Object::GetAppDomainIndex()
{
    WRAPPER_NO_CONTRACT;
#ifndef _DEBUG
    // ok to cast to AppDomain because we know it's a real AppDomain if it's not shared
    if (!GetGCSafeMethodTable()->IsDomainNeutral())
        return (dac_cast<PTR_AppDomain>(GetGCSafeMethodTable()->GetDomain())->GetIndex());
#endif
        return GetHeader()->GetAppDomainIndex();
}

inline DWORD Object::GetNumComponents()
{
    LIMITED_METHOD_DAC_CONTRACT;
    // Yes, we may not even be an array, which means we are reading some of the object's memory - however,
    // ComponentSize will multiply out this value.  Therefore, m_NumComponents must be the first field in
    // ArrayBase.
    return dac_cast<PTR_ArrayBase>(this)->m_NumComponents;
}

inline SIZE_T Object::GetSize()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // mask the alignment bits because this methos is called during GC
    MethodTable *mT = GetGCSafeMethodTable();

    // strings have component size2, all other non-arrays should have 0
    _ASSERTE(( mT->GetComponentSize() <= 2) || mT->IsArray());

    size_t s = mT->GetBaseSize();
    if (mT->HasComponentSize())
        s += (size_t)GetNumComponents() * mT->RawGetComponentSize();
    return s;
}

__forceinline /*static*/ SIZE_T StringObject::GetSize(DWORD strLen)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Extra WCHAR for null terminator
    return ObjSizeOf(StringObject) + sizeof(WCHAR) + strLen * sizeof(WCHAR);
}

#ifdef DACCESS_COMPILE

inline void Object::EnumMemoryRegions(void)
{
    SUPPORTS_DAC;

    PTR_MethodTable methodTable = GetGCSafeMethodTable();

    TADDR ptr = dac_cast<TADDR>(this) - sizeof(ObjHeader);
    SIZE_T size = sizeof(ObjHeader) + sizeof(Object);

    // If it is unsafe to touch the MethodTable so just enumerate
    // the base object.
    if (methodTable.IsValid())
    {
        size = sizeof(ObjHeader) + GetSize();
    }

#if defined (_DEBUG)
    // Test hook: when testing on debug builds, we want an easy way to test that the following while
    // correctly terminates in the face of ridiculous stuff from the target.
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget) == 1)
    {
        // Pretend all objects are incredibly large.
        size |= 0xf0000000;
    }
#endif // defined (_DEBUG)

    // Unfortunately, DacEnumMemoryRegion takes only ULONG32 as size argument
    while (size > 0) {
        // Use 0x10000000 instead of MAX_ULONG32 so that the chunks stays aligned
        SIZE_T chunk = min(size, 0x10000000);
        // If for any reason we can't enumerate the memory, stop.  This would generally mean
        // that we have target corruption, or that the target is executing, etc.
        if (!DacEnumMemoryRegion(ptr, chunk))
            break;
        ptr += chunk; size -= chunk;
    }

    // As an Object is very low-level don't propagate
    // the enumeration to the MethodTable.
}

#endif // #ifdef DACCESS_COMPILE
    

inline TypeHandle ArrayBase::GetTypeHandle() const
{ 
    WRAPPER_NO_CONTRACT; 
    return GetTypeHandle(GetMethodTable()); 
}

inline /* static */ TypeHandle ArrayBase::GetTypeHandle(MethodTable * pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    _ASSERTE(pMT != NULL);

    // This ensures that we can always get the typehandle for an object in hand
    // without triggering the noisy parts of the loader.
    //
    // The debugger can cause this routine to be called on an unmanaged thread
    // so this really is important.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    CorElementType kind = pMT->GetInternalCorElementType();
    unsigned rank = pMT->GetRank();
    // Note that this load should always succeed because there is an invariant that
    // if we have allocated an array object of type T then the ArrayTypeDesc
    // for T[] is available and restored

    // @todo  This should be turned into a probe with a hard SO when we have one
    CONTRACT_VIOLATION(SOToleranceViolation);
    // == FailIfNotLoadedOrNotRestored
    TypeHandle arrayType = ClassLoader::LoadArrayTypeThrowing(pMT->GetApproxArrayElementTypeHandle(), kind, rank, ClassLoader::DontLoadTypes);  
    CONSISTENCY_CHECK(!arrayType.IsNull()); 
    return(arrayType);
}

        // Get the CorElementType for the elements in the array.  Avoids creating a TypeHandle
inline CorElementType ArrayBase::GetArrayElementType() const 
{
    WRAPPER_NO_CONTRACT;
    return GetMethodTable()->GetArrayElementType();
}

inline unsigned ArrayBase::GetRank() const 
{
    WRAPPER_NO_CONTRACT;
    return GetMethodTable()->GetRank();
}

// Total element count for the array
inline DWORD ArrayBase::GetNumComponents() const 
{ 
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return m_NumComponents; 
}

inline /* static */ unsigned ArrayBase::GetDataPtrOffset(MethodTable* pMT)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
#if !defined(DACCESS_COMPILE)
    _ASSERTE(pMT->IsArray());
#endif // DACCESS_COMPILE
    // The -sizeof(ObjHeader) is because of the sync block, which is before "this"
    return pMT->GetBaseSize() - sizeof(ObjHeader);
}

inline /* static */ unsigned ArrayBase::GetBoundsOffset(MethodTable* pMT) 
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMT->IsArray());
    if (!pMT->IsMultiDimArray()) 
        return(offsetof(ArrayBase, m_NumComponents));
    _ASSERTE(pMT->GetInternalCorElementType() == ELEMENT_TYPE_ARRAY);
    return sizeof(ArrayBase);
}
inline /* static */ unsigned ArrayBase::GetLowerBoundsOffset(MethodTable* pMT) 
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pMT->IsArray());
    // There is no good offset for this for a SZARRAY.  
    _ASSERTE(pMT->GetInternalCorElementType() == ELEMENT_TYPE_ARRAY);
    // Lower bounds info is after total bounds info
    // and total bounds info has rank elements
    return GetBoundsOffset(pMT) +
        dac_cast<PTR_ArrayClass>(pMT->GetClass())->GetRank() *
        sizeof(INT32);
}

// Get the element type for the array, this works whether the the element
// type is stored in the array or not
inline TypeHandle ArrayBase::GetArrayElementTypeHandle() const 
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return GetGCSafeMethodTable()->GetApproxArrayElementTypeHandle();
}

//===============================================================================
// Returns true if this pMT is Nullable<T> for T is equivalent to paramMT

__forceinline BOOL Nullable::IsNullableForType(TypeHandle type, MethodTable* paramMT)
{
    if (type.IsTypeDesc())
        return FALSE;
    if (!type.AsMethodTable()->HasInstantiation())            // shortcut, if it is not generic it can't be Nullable<T>
        return FALSE;
	return Nullable::IsNullableForTypeHelper(type.AsMethodTable(), paramMT);
}

//===============================================================================
// Returns true if this pMT is Nullable<T> for T == paramMT

__forceinline BOOL Nullable::IsNullableForTypeNoGC(TypeHandle type, MethodTable* paramMT) 
{
    if (type.IsTypeDesc())
        return FALSE;
    if (!type.AsMethodTable()->HasInstantiation())            // shortcut, if it is not generic it can't be Nullable<T>
        return FALSE;
	return Nullable::IsNullableForTypeHelperNoGC(type.AsMethodTable(), paramMT);
}

//===============================================================================
// Returns true if this type is Nullable<T> for some T.  

inline BOOL Nullable::IsNullableType(TypeHandle type) 
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    
    if (type.IsTypeDesc())
        return FALSE;

    return type.AsMethodTable()->IsNullable();
}

inline TypeHandle Object::GetTypeHandle()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    _ASSERTE(m_pMethTab == GetGCSafeMethodTable());

    if (m_pMethTab->IsArray())
        return (dac_cast<PTR_ArrayBase>(this))->GetTypeHandle();
    else 
        return TypeHandle(m_pMethTab);
}

inline TypeHandle Object::GetGCSafeTypeHandle() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable * pMT = GetGCSafeMethodTable();
    _ASSERTE(pMT != NULL);

    if (pMT->IsArray())
        return ArrayBase::GetTypeHandle(pMT);
    else 
        return TypeHandle(pMT);
}

#endif  // _OBJECT_INL_
