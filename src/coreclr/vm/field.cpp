// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: Field.cpp
//

// ===========================================================================
// This file contains the implementation for FieldDesc methods.
// ===========================================================================
//


#include "common.h"

#include "encee.h"
#include "field.h"
#include "generics.h"

#include "peimagelayout.inl"

#ifndef DACCESS_COMPILE
VOID FieldDesc::SetStaticOBJECTREF(OBJECTREF objRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    GCPROTECT_BEGIN(objRef);
    OBJECTREF *pObjRef = (OBJECTREF *)GetCurrentStaticAddress();
    SetObjectReference(pObjRef, objRef);
    GCPROTECT_END();
}
#endif

// called from code:MethodTableBuilder::InitializeFieldDescs#InitCall
VOID FieldDesc::Init(mdFieldDef mb, CorElementType FieldType, DWORD dwMemberAttrs, BOOL fIsStatic, BOOL fIsRVA, BOOL fIsThreadLocal, LPCSTR pszFieldName)
{
    LIMITED_METHOD_CONTRACT;

    // We allow only a subset of field types here - all objects must be set to TYPE_CLASS
    // By-value classes are ELEMENT_TYPE_VALUETYPE
    _ASSERTE(
        FieldType == ELEMENT_TYPE_I1 ||
        FieldType == ELEMENT_TYPE_BOOLEAN ||
        FieldType == ELEMENT_TYPE_U1 ||
        FieldType == ELEMENT_TYPE_I2 ||
        FieldType == ELEMENT_TYPE_U2 ||
        FieldType == ELEMENT_TYPE_CHAR ||
        FieldType == ELEMENT_TYPE_I4 ||
        FieldType == ELEMENT_TYPE_U4 ||
        FieldType == ELEMENT_TYPE_I8 ||
        FieldType == ELEMENT_TYPE_I  ||
        FieldType == ELEMENT_TYPE_U  ||
        FieldType == ELEMENT_TYPE_U8 ||
        FieldType == ELEMENT_TYPE_R4 ||
        FieldType == ELEMENT_TYPE_R8 ||
        FieldType == ELEMENT_TYPE_CLASS ||
        FieldType == ELEMENT_TYPE_VALUETYPE ||
        FieldType == ELEMENT_TYPE_PTR ||
        FieldType == ELEMENT_TYPE_FNPTR
        );
    _ASSERTE(fIsStatic || (!fIsRVA && !fIsThreadLocal));
    _ASSERTE(fIsRVA + fIsThreadLocal <= 1);

    m_requiresFullMbValue = 0;
    SetMemberDef(mb);

    m_type = FieldType;
    m_prot = fdFieldAccessMask & dwMemberAttrs;
    m_isStatic = fIsStatic != 0;
    m_isRVA = fIsRVA != 0;
    m_isThreadLocal = fIsThreadLocal != 0;

#ifdef _DEBUG
    m_debugName = (LPUTF8)pszFieldName;
#endif

    _ASSERTE(GetMemberDef() == mb);                 // no truncation
    _ASSERTE(GetFieldType() == FieldType);
    _ASSERTE(GetFieldProtection() == (fdFieldAccessMask & dwMemberAttrs));
    _ASSERTE((BOOL) IsStatic() == (fIsStatic != 0));
}

// Return whether the field is a GC ref type
BOOL FieldDesc::IsObjRef()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return CorTypeInfo::IsObjRef_NoThrow(GetFieldType());
}

#ifndef DACCESS_COMPILE
void FieldDesc::PrecomputeNameHash()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCompilationProcess());
    }
    CONTRACTL_END;

    // We only have space for the name hash when we can use the packed mb layout
    if (m_requiresFullMbValue)
    {
        return;
    }

    // Store a case-insensitive hash so that we can use this value for
    // both case-sensitive and case-insensitive name lookups
    SString name(SString::Utf8Literal, GetName());
    ULONG nameHashValue = name.HashCaseInsensitive() & enum_packedMbLayout_NameHashMask;

    // We should never overwrite any other bits
    _ASSERTE((m_mb & enum_packedMbLayout_NameHashMask) == 0 ||
               (m_mb & enum_packedMbLayout_NameHashMask) == nameHashValue);

    m_mb |= nameHashValue;
}
#endif

BOOL FieldDesc::MightHaveName(ULONG nameHashValue)
{
    LIMITED_METHOD_CONTRACT;

    g_IBCLogger.LogFieldDescsAccess(this);

    // We only have space for a name hash when we are using the packed mb layout
    if (m_requiresFullMbValue)
    {
        return TRUE;
    }

    ULONG thisHashValue = m_mb & enum_packedMbLayout_NameHashMask;

    // A zero value might mean no hash has ever been set
    // (checking this way is better than dedicating a bit to tell us)
    if (thisHashValue == 0)
    {
        return TRUE;
    }

    ULONG testHashValue = nameHashValue & enum_packedMbLayout_NameHashMask;

    return (thisHashValue == testHashValue);
}

#ifndef DACCESS_COMPILE //we don't require DAC to special case simple types
// Return the type of the field, as a class, but only if it's loaded.
TypeHandle FieldDesc::LookupFieldTypeHandle(ClassLoadLevel level, BOOL dropGenericArgumentLevel)
{

    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // This function is called during GC promotion.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // Caller should have handled all the non-class cases, already.
    _ASSERTE(GetFieldType() == ELEMENT_TYPE_CLASS ||
             GetFieldType() == ELEMENT_TYPE_VALUETYPE);

    MetaSig        sig(this);
    CorElementType type;

    type = sig.NextArg();

    // This may be the real type which includes other things
    //  beside class and value class such as arrays
    _ASSERTE(type == ELEMENT_TYPE_CLASS ||
             type == ELEMENT_TYPE_VALUETYPE ||
             type == ELEMENT_TYPE_STRING ||
             type == ELEMENT_TYPE_SZARRAY ||
             type == ELEMENT_TYPE_VAR
             );

    // == FailIfNotLoaded, can also assert that the thing is restored
    return sig.GetLastTypeHandleThrowing(ClassLoader::DontLoadTypes, level, dropGenericArgumentLevel);
}
#else //simplified version
TypeHandle FieldDesc::LookupFieldTypeHandle(ClassLoadLevel level, BOOL dropGenericArgumentLevel)
{
    WRAPPER_NO_CONTRACT;
    MetaSig        sig(this);
    CorElementType type;
    type = sig.NextArg();
    return sig.GetLastTypeHandleThrowing(ClassLoader::DontLoadTypes, level, dropGenericArgumentLevel);
}
#endif //DACCESS_COMPILE

TypeHandle FieldDesc::GetFieldTypeHandleThrowing(ClassLoadLevel level/*=CLASS_LOADED*/,
                                                 BOOL dropGenericArgumentLevel /*=FALSE*/)
{
    WRAPPER_NO_CONTRACT;

    MetaSig sig(this);
    sig.NextArg();

    return sig.GetLastTypeHandleThrowing(ClassLoader::LoadTypes, level, dropGenericArgumentLevel);
}

#ifndef DACCESS_COMPILE

void* FieldDesc::GetStaticAddress(void *base)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;      // Needed by profiler and server GC
    }
    CONTRACTL_END;

    void* ret = GetStaticAddressHandle(base);       // Get the handle

        // For value classes, the handle points at an OBJECTREF
        // which holds the boxed value class, so derefernce and unbox.
    if (GetFieldType() == ELEMENT_TYPE_VALUETYPE && !IsRVA())
    {
        OBJECTREF obj = ObjectToOBJECTREF(*(Object**) ret);
        ret = obj->GetData();
    }
    return ret;
}

MethodTable * FieldDesc::GetExactDeclaringType(MethodTable * ownerOrSubType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable * pMT = GetApproxEnclosingMethodTable();

    // Fast path for typical case.
    if (ownerOrSubType == pMT)
        return pMT;

    return ownerOrSubType->GetMethodTableMatchingParentClass(pMT);
}

#endif // #ifndef DACCESS_COMPILE

    // static value classes are actually stored in their boxed form.
    // this means that their address moves.
PTR_VOID FieldDesc::GetStaticAddressHandle(PTR_VOID base)
{

    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(IsStatic());
        PRECONDITION(GetEnclosingMethodTable()->IsRestored_NoLogging());
    }
    CONTRACTL_END

     g_IBCLogger.LogFieldDescsAccess(this);

    _ASSERTE(IsStatic());
#ifdef EnC_SUPPORTED
    if (IsEnCNew())
    {
        EnCFieldDesc * pFD = dac_cast<PTR_EnCFieldDesc>(this);
        _ASSERTE_IMPL(pFD->GetApproxEnclosingMethodTable()->SanityCheck());
        _ASSERTE(pFD->GetModule()->IsEditAndContinueEnabled());

        EditAndContinueModule *pModule = (EditAndContinueModule*)pFD->GetModule();
        _ASSERTE(pModule->IsEditAndContinueEnabled());

        PTR_VOID retVal = NULL;

#ifdef DACCESS_COMPILE
        DacNotImpl();
#else
        {
            GCX_COOP();
            // This routine doesn't have a failure semantic - but Resolve*Field(...) does.
            // Something needs to be rethought here and I think it's E&C.
            CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|GCViolation);   //B#25680 (Fix Enc violations)
            retVal = (void *)(pModule->ResolveOrAllocateField(NULL, pFD));
        }
#endif // !DACCESS_COMPILE
        return retVal;
    }
#endif // EnC_SUPPORTED


    if (IsRVA())
    {
        Module* pModule = GetModule();
        PTR_VOID ret = pModule->GetRvaField(GetOffset(), IsZapped());

        _ASSERTE(!pModule->IsPEFile() || !pModule->IsRvaFieldTls(GetOffset()));

        return(ret);
    }

    CONSISTENCY_CHECK(CheckPointer(base));

    PTR_VOID ret = PTR_VOID(dac_cast<PTR_BYTE>(base) + GetOffset());


    return ret;
}



// These routines encapsulate the operation of getting and setting
// fields.
void    FieldDesc::GetInstanceField(OBJECTREF o, VOID * pOutVal)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED() ) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED() ) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED() ) FORBID_FAULT; else INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

      // We know that it isn't going to be null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(o != NULL);

    // Check whether we are getting a field value on a proxy. If so, then ask
    // remoting services to extract the value from the instance.

    // Unbox the value class
    TADDR pFieldAddress = (TADDR)GetInstanceAddress(o);
    UINT cbSize = GetSize();

    switch (cbSize)
    {
    case 1:
        *(INT8*)pOutVal = VolatileLoad<INT8>(PTR_INT8(pFieldAddress));
        break;

    case 2:
        *(INT16*)pOutVal = VolatileLoad<INT16>(PTR_INT16(pFieldAddress));
        break;

    case 4:
        *(INT32*)pOutVal = VolatileLoad<INT32>(PTR_INT32(pFieldAddress));
        break;

    case 8:
        *(INT64*)pOutVal = VolatileLoad<INT64>(PTR_INT64(pFieldAddress));
        break;

    default:
        UNREACHABLE();
        break;
    }
}

#ifndef DACCESS_COMPILE
void    FieldDesc::SetInstanceField(OBJECTREF o, const VOID * pInVal)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

#ifdef _DEBUG
    //
    // assert that o is derived from MT of enclosing class
    //
    // walk up o's inheritence chain to make sure m_pMTOfEnclosingClass is along it
    //
    MethodTable* pCursor = o->GetMethodTable();

    //<TODO>@todo : work out exactly why instantiations aren't matching; probably
    // because of approx loads on field types in the loader</TODO>
    while (pCursor && (GetApproxEnclosingMethodTable()->HasSameTypeDefAs(pCursor)))
    {
        pCursor = pCursor->GetParentMethodTable();
    }
    _ASSERTE(pCursor != NULL);
#endif // _DEBUG

    // Unbox the value class
    LPVOID pFieldAddress = GetInstanceAddress(o);

    CorElementType fieldType = GetFieldType();

    if (fieldType == ELEMENT_TYPE_CLASS)
    {
        OBJECTREF ref = ObjectToOBJECTREF(*(Object**)pInVal);

        SetObjectReference((OBJECTREF*)pFieldAddress, ref);
    }
    else if (fieldType == ELEMENT_TYPE_VALUETYPE)
    {
        CONSISTENCY_CHECK(!LookupFieldTypeHandle().IsNull());
        // The Approximate MT is enough to do the copy
        CopyValueClass(pFieldAddress,
                        (void*)pInVal,
                        LookupFieldTypeHandle().GetMethodTable());
    }
    else
    {
        UINT cbSize = LoadSize();

        switch (cbSize)
        {
            case 1:
                VolatileStore<INT8>((INT8*)pFieldAddress, *(INT8*)pInVal);
                break;

            case 2:
                VolatileStore<INT16>((INT16*)pFieldAddress, *(INT16*)pInVal);
                break;

            case 4:
                VolatileStore<INT32>((INT32*)pFieldAddress, *(INT32*)pInVal);
                break;

            case 8:
                VolatileStore<INT64>((INT64*)pFieldAddress, *(INT64*)pInVal);
                break;

            default:
                UNREACHABLE();
                break;
        }
    }
}
#endif // #ifndef DACCESS_COMPILE


// This function is used for BYREF support of fields.  Since it generates
// interior pointers, you really have to watch the lifetime of the pointer
// so that GCs don't happen while you have the reference active
PTR_VOID FieldDesc::GetAddressNoThrowNoGC(PTR_VOID o)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(!IsEnCNew());
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD dwOffset = GetOffset();
    if (!IsFieldOfValueType())
    {
        dwOffset += sizeof(Object);
    }
    return dac_cast<PTR_BYTE>(o) + dwOffset;
}

PTR_VOID FieldDesc::GetAddress(PTR_VOID o)
{
    CONTRACTL
    {
        if(IsEnCNew()) {THROWS;} else {DISABLED(THROWS);};
        if(IsEnCNew()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);};
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef DACCESS_COMPILE
    _ASSERTE(!IsEnCNew()); // when we call this while finding an EnC field via the DAC,
                           // the field desc is for the EnCHelper, not the new EnC field
#endif
    g_IBCLogger.LogFieldDescsAccess(this);

#if defined(EnC_SUPPORTED) && !defined(DACCESS_COMPILE)
    // EnC added fields aren't at a simple offset like normal fields.
    if (IsEnCNew())
    {
        // We'll have to go through some effort to compute the address of this field.
        return ((EnCFieldDesc *)this)->GetAddress(o);
    }
#endif //  defined(EnC_SUPPORTED) && !defined(DACCESS_COMPILE)
    return GetAddressNoThrowNoGC(o);
}

void *FieldDesc::GetInstanceAddress(OBJECTREF o)
{
    CONTRACTL
    {
        if(IsEnCNew()) {THROWS;} else {DISABLED(THROWS);};
        if(IsEnCNew()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);};
    }
    CONTRACTL_END;

    g_IBCLogger.LogFieldDescsAccess(this);

    DWORD dwOffset = m_dwOffset; // GetOffset()

#ifdef EnC_SUPPORTED
    // EnC added fields aren't at a simple offset like normal fields.
    if (dwOffset == FIELD_OFFSET_NEW_ENC) // IsEnCNew()
    {
        // We'll have to go through some effort to compute the address of this field.
        return ((EnCFieldDesc *)this)->GetAddress(OBJECTREFToObject(o));
    }
#endif

    return (void *) (dac_cast<TADDR>(o->GetData()) + dwOffset);
}

// And here's the equivalent, when you are guaranteed that the enclosing instance of
// the field is in the GC Heap.  So if the enclosing instance is a value type, it had
// better be boxed.  We ASSERT this.
void *FieldDesc::GetAddressGuaranteedInHeap(void *o)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(!IsEnCNew());

    return ((BYTE*)(o)) + sizeof(Object) + m_dwOffset;
}


DWORD   FieldDesc::GetValue32(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;

    DWORD val;
    GetInstanceField(o, (LPVOID)&val);
    return val;
}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetValue32(OBJECTREF o, DWORD dwValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    SetInstanceField(o, (LPVOID)&dwValue);
}
#endif // #ifndef DACCESS_COMPILE

void*   FieldDesc::GetValuePtr(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;

    void* val;
    GetInstanceField(o, (LPVOID)&val);
    return val;
}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetValuePtr(OBJECTREF o, void* pValue)
{
    WRAPPER_NO_CONTRACT;

    SetInstanceField(o, (LPVOID)&pValue);
}
#endif // #ifndef DACCESS_COMPILE

OBJECTREF FieldDesc::GetRefValue(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;

    OBJECTREF val = NULL;

#ifdef PROFILING_SUPPORTED
    GCPROTECT_BEGIN(val);
#endif

    GetInstanceField(o, (LPVOID)&val);

#ifdef PROFILING_SUPPORTED
    GCPROTECT_END();
#endif

    return val;
}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetRefValue(OBJECTREF o, OBJECTREF orValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    VALIDATEOBJECTREF(o);
    VALIDATEOBJECTREF(orValue);

    SetInstanceField(o, (LPVOID)&orValue);
}
#endif // #ifndef DACCESS_COMPILE

USHORT  FieldDesc::GetValue16(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;

    USHORT val;
    GetInstanceField(o, (LPVOID)&val);
    return val;
}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetValue16(OBJECTREF o, DWORD dwValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    USHORT val = (USHORT)dwValue;
    SetInstanceField(o, (LPVOID)&val);
}
#endif // #ifndef DACCESS_COMPILE

BYTE    FieldDesc::GetValue8(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;

    BYTE val;
    GetInstanceField(o, (LPVOID)&val);
    return val;

}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetValue8(OBJECTREF o, DWORD dwValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BYTE val = (BYTE)dwValue;
    SetInstanceField(o, (LPVOID)&val);
}
#endif // #ifndef DACCESS_COMPILE

__int64 FieldDesc::GetValue64(OBJECTREF o)
{
    WRAPPER_NO_CONTRACT;
    __int64 val;
    GetInstanceField(o, (LPVOID)&val);
    return val;

}

#ifndef DACCESS_COMPILE
VOID    FieldDesc::SetValue64(OBJECTREF o, __int64 value)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    SetInstanceField(o, (LPVOID)&value);
}
#endif // #ifndef DACCESS_COMPILE


UINT FieldDesc::LoadSize()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    CorElementType type = GetFieldType();
    UINT size = GetSizeForCorElementType(type);
    if (size == (UINT) -1)
    {
        //        LOG((LF_CLASSLOADER, LL_INFO10000, "FieldDesc::LoadSize %s::%s\n", GetApproxEnclosingMethodTable()->GetDebugClassName(), m_debugName));
        CONSISTENCY_CHECK(GetFieldType() == ELEMENT_TYPE_VALUETYPE);
        size = GetApproxFieldTypeHandleThrowing().GetMethodTable()->GetNumInstanceFieldBytes();
    }

    return size;
}

UINT FieldDesc::GetSize()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END

    CorElementType type = GetFieldType();
    UINT size = GetSizeForCorElementType(type);
    if (size == (UINT) -1)
    {
        LOG((LF_CLASSLOADER, LL_INFO10000, "FieldDesc::GetSize %s::%s\n", GetApproxEnclosingMethodTable()->GetDebugClassName(), m_debugName));
        CONSISTENCY_CHECK(GetFieldType() == ELEMENT_TYPE_VALUETYPE);
        TypeHandle t = LookupApproxFieldTypeHandle();
        if (!t.IsNull())
        {
            size = t.GetMethodTable()->GetNumInstanceFieldBytes();
        }
    }

    return size;
}

// See field.h for details
Instantiation FieldDesc::GetExactClassInstantiation(TypeHandle possibleObjType)
{
    WRAPPER_NO_CONTRACT;

    // We know that it isn't going to be null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(GetApproxEnclosingMethodTable()!=NULL);
    if (possibleObjType.IsNull())
    {
        return GetApproxEnclosingMethodTable()->GetInstantiation();
    }
    else
    {
        PREFIX_ASSUME(GetApproxEnclosingMethodTable()!=NULL);
        return possibleObjType.GetInstantiationOfParentClass(GetApproxEnclosingMethodTable());
    }
}

// Given, { List<String>, List<__Canon>._items } where _items is of type T[],
// it returns String[].

TypeHandle FieldDesc::GetExactFieldType(TypeHandle owner)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(owner, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    if (GetApproxEnclosingMethodTable() == owner.AsMethodTable())
    {
        //Yes, this is exactly the type I was looking for.
        RETURN(GetFieldTypeHandleThrowing());
    }
    else
    {
        //This FieldDesc doesn't exactly represent the owner type.  Go look up the exact type.

        // We need to figure out the precise type of the field.
        // First, get the signature of the field
        PCCOR_SIGNATURE pSig;
        DWORD cSig;
        GetSig(&pSig, &cSig);
        SigPointer sig(pSig, cSig);

        uint32_t callConv;
        IfFailThrow(sig.GetCallingConv(&callConv));
        _ASSERTE(callConv == IMAGE_CEE_CS_CALLCONV_FIELD);

        // Get the generics information
        SigTypeContext sigTypeContext(GetExactClassInstantiation(owner), Instantiation());

        // Load the exact type
        RETURN (sig.GetTypeHandleThrowing(GetModule(), &sigTypeContext));
    }
}

#if !defined(DACCESS_COMPILE)
REFLECTFIELDREF FieldDesc::GetStubFieldInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    REFLECTFIELDREF retVal;
    REFLECTFIELDREF fieldRef = (REFLECTFIELDREF)AllocateObject(CoreLibBinder::GetClass(CLASS__STUBFIELDINFO));
    GCPROTECT_BEGIN(fieldRef);

    fieldRef->SetField(this);
    LoaderAllocator *pLoaderAllocatorOfMethod = this->GetApproxEnclosingMethodTable()->GetLoaderAllocator();
    if (pLoaderAllocatorOfMethod->IsCollectible())
        fieldRef->SetKeepAlive(pLoaderAllocatorOfMethod->GetExposedObject());

    retVal = fieldRef;
    GCPROTECT_END();

    return retVal;
}
#endif // !DACCESS_COMPILE
