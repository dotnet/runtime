// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: typehandle.cpp
//

#include "common.h"
#include "class.h"
#include "typehandle.h"
#include "eeconfig.h"
#include "generics.h"
#include "typedesc.h"
#include "typekey.h"
#include "typestring.h"
#include "classloadlevel.h"
#include "array.h"
#include "castcache.h"

#ifdef FEATURE_PREJIT
#include "zapsig.h"
#endif

#ifdef _DEBUG_IMPL

BOOL TypeHandle::Verify()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_SUPPORTS_DAC;

    if (IsNull())
        return(TRUE);

    // If you try to do IBC logging of a type being created, the type
    // will look inconsistent. IBC logging knows to filter out such types.
    if (g_IBCLogger.InstrEnabled())
        return TRUE;

    if (!IsRestored_NoLogging())
        return TRUE;

    if (IsArray())
    {
        GetArrayElementTypeHandle().Verify();
    }
    else if (!IsTypeDesc())
    {
        _ASSERTE(AsMethodTable()->SanityCheck());   // Sane method table
    }

    return(TRUE);
}

#endif // _DEBUG_IMPL

unsigned TypeHandle::GetSize()  const
{
    LIMITED_METHOD_DAC_CONTRACT;

    CorElementType type = GetInternalCorElementType();

    if (type == ELEMENT_TYPE_VALUETYPE)
    {
        if (IsTypeDesc())
            return(AsNativeValueType()->GetNativeSize());
        else
            return(AsMethodTable()->GetNumInstanceFieldBytes());
    }

    return(GetSizeForCorElementType(type));
}

PTR_Module TypeHandle::GetModule() const {
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetModule();
    return(AsMethodTable()->GetModule());
}

Assembly* TypeHandle::GetAssembly() const {
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetAssembly();
    return(AsMethodTable()->GetAssembly());
}

BOOL TypeHandle::IsArray() const {
    LIMITED_METHOD_DAC_CONTRACT;

    return !IsTypeDesc() && AsMethodTable()->IsArray();
}

BOOL TypeHandle::IsGenericVariable() const {
    LIMITED_METHOD_DAC_CONTRACT;

    return(IsTypeDesc() && CorTypeInfo::IsGenericVariable_NoThrow(AsTypeDesc()->GetInternalCorElementType()));
}

BOOL TypeHandle::HasTypeParam() const {
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsArray())
        return TRUE;

    if (!IsTypeDesc())
        return FALSE;

    CorElementType etype = AsTypeDesc()->GetInternalCorElementType();
    return(CorTypeInfo::IsModifier_NoThrow(etype) || etype == ELEMENT_TYPE_VALUETYPE);
}

Module *TypeHandle::GetDefiningModuleForOpenType() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    Module* returnValue = NULL;

    if (IsGenericVariable())
    {
        PTR_TypeVarTypeDesc pTyVar = dac_cast<PTR_TypeVarTypeDesc>(AsTypeDesc());
        returnValue = pTyVar->GetModule();
        goto Exit;
    }

    if (HasTypeParam())
    {
        returnValue = GetTypeParam().GetDefiningModuleForOpenType();
    }
    else if (HasInstantiation())
    {
        returnValue = GetMethodTable()->GetDefiningModuleForOpenType();
    }
Exit:

    return returnValue;
}

BOOL TypeHandle::ContainsGenericVariables(BOOL methodOnly /*=FALSE*/) const
{
    STATIC_CONTRACT_NOTHROW;
    SUPPORTS_DAC;

    if (HasTypeParam())
    {
        return GetTypeParam().ContainsGenericVariables(methodOnly);
    }

    if (IsGenericVariable())
    {
        if (!methodOnly)
            return TRUE;

        PTR_TypeVarTypeDesc pTyVar = dac_cast<PTR_TypeVarTypeDesc>(AsTypeDesc());
        return TypeFromToken(pTyVar->GetTypeOrMethodDef()) == mdtMethodDef;
    }
    else if (HasInstantiation())
    {
        if (GetMethodTable()->ContainsGenericVariables(methodOnly))
            return TRUE;
    }

    return FALSE;
}

//@GENERICS:
// Return the number of type parameters in the instantiation of an instantiated type
// or the number of type parameters to a generic type
// Return 0 otherwise.
DWORD TypeHandle::GetNumGenericArgs() const {
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return 0;
    else
        return AsMethodTable()->GetNumGenericArgs();
}

BOOL TypeHandle::IsGenericTypeDefinition() const {
    LIMITED_METHOD_DAC_CONTRACT;

    if (!IsTypeDesc())
        return AsMethodTable()->IsGenericTypeDefinition();
    else
        return FALSE;
}

PTR_MethodTable TypeHandle::GetCanonicalMethodTable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
    {
        PTR_MethodTable pMT = AsTypeDesc()->GetMethodTable();
        if (pMT != NULL)
            pMT = pMT->GetCanonicalMethodTable();
        return pMT;
    }
    else
    {
        return AsMethodTable()->GetCanonicalMethodTable();
    }
}

// Obtain instantiation from an instantiated type or a pointer to the
// element type of an array or pointer type
Instantiation TypeHandle::GetClassOrArrayInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
    {
        return AsTypeDesc()->GetClassOrArrayInstantiation();
    }
    else if (IsArray())
    {
        return AsMethodTable()->GetArrayInstantiation();
    }
    else
    {
        return GetInstantiation();
    }
}

Instantiation TypeHandle::GetInstantiationOfParentClass(MethodTable *pWhichParent) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetMethodTable()->GetInstantiationOfParentClass(pWhichParent);
}

// Obtain element type from a byref or pointer type
TypeHandle TypeHandle::GetTypeParam() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsArray())
        return GetArrayElementTypeHandle();

    if (IsTypeDesc())
        return AsTypeDesc()->GetTypeParam();

    return TypeHandle();
}

#ifndef DACCESS_COMPILE
TypeHandle TypeHandle::Instantiate(Instantiation inst) const
{
    STATIC_CONTRACT_WRAPPER;
    return ClassLoader::LoadGenericInstantiationThrowing(GetModule(), GetCl(), inst);
}

TypeHandle TypeHandle::MakePointer() const
{
    STATIC_CONTRACT_WRAPPER;
    return ClassLoader::LoadPointerOrByrefTypeThrowing(ELEMENT_TYPE_PTR, *this);
}

TypeHandle TypeHandle::MakeByRef() const
{
    STATIC_CONTRACT_WRAPPER;
    return ClassLoader::LoadPointerOrByrefTypeThrowing(ELEMENT_TYPE_BYREF, *this);
}

TypeHandle TypeHandle::MakeSZArray() const
{
    STATIC_CONTRACT_WRAPPER;
    return ClassLoader::LoadArrayTypeThrowing(*this);
}

TypeHandle TypeHandle::MakeArray(int rank) const
{
    STATIC_CONTRACT_WRAPPER;
    return ClassLoader::LoadArrayTypeThrowing(*this, ELEMENT_TYPE_ARRAY, rank);
}

// The returned TypeHandle is a ParamTypeDesc that acts like a facade for the original valuetype. It makes the
// valuetype look like its unmanaged view, i.e. GetSize() returns GetNativeSize(), IsBlittable() returns TRUE,
// and JIT interface special-cases it when reporting GC pointers to the JIT.
TypeHandle TypeHandle::MakeNativeValueType() const
{
    STATIC_CONTRACT_WRAPPER;

    return ClassLoader::LoadNativeValueTypeThrowing(*this);
}

#endif // #ifndef DACCESS_COMPILE

PTR_Module TypeHandle::GetLoaderModule() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetLoaderModule();
    else
        return AsMethodTable()->GetLoaderModule();
}

PTR_Module TypeHandle::GetZapModule() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetZapModule();
    else
        return AsMethodTable()->GetZapModule();
}

PTR_BaseDomain TypeHandle::GetDomain() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetDomain();
    else
        return AsMethodTable()->GetDomain();

}

PTR_LoaderAllocator TypeHandle::GetLoaderAllocator() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    if (IsTypeDesc())
    {
        return AsTypeDesc()->GetLoaderAllocator();
    }
    else
    {
        return AsMethodTable()->GetLoaderAllocator();
    }
}

BOOL TypeHandle::IsSharedByGenericInstantiations() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsArray())
    {
        return GetArrayElementTypeHandle().IsCanonicalSubtype();
    }
    else if (!IsTypeDesc())
    {
        return AsMethodTable()->IsSharedByGenericInstantiations();
    }

    return FALSE;
}

BOOL TypeHandle::IsCanonicalSubtype() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (*this == TypeHandle(g_pCanonMethodTableClass)) || IsSharedByGenericInstantiations();
}

/* static */ BOOL TypeHandle::IsCanonicalSubtypeInstantiation(Instantiation inst)
{
    LIMITED_METHOD_DAC_CONTRACT;

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        if (inst[i].IsCanonicalSubtype())
            return TRUE;
    }
    return FALSE;
}

// Obtain instantiation from an instantiated type.
// Return NULL if it's not one.
Instantiation TypeHandle::GetInstantiation() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!IsTypeDesc()) return AsMethodTable()->GetInstantiation();
    else return Instantiation();
}


BOOL TypeHandle::IsValueType()  const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!IsTypeDesc()) return AsMethodTable()->IsValueType();
    else return AsTypeDesc()->IsNativeValueType();
}

BOOL TypeHandle::IsInterface() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return !IsTypeDesc() && AsMethodTable()->IsInterface();
}

BOOL TypeHandle::IsAbstract() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->IsAbstract();
}

bool TypeHandle::IsHFA() const
{
    WRAPPER_NO_CONTRACT;

    if (!IsTypeDesc())
        return AsMethodTable()->IsHFA();

    if (AsTypeDesc()->IsNativeValueType())
        return AsNativeValueType()->IsNativeHFA();

    return false;
}

CorInfoHFAElemType TypeHandle::GetHFAType() const
{
    WRAPPER_NO_CONTRACT;

    if (!IsTypeDesc())
        return AsMethodTable()->GetHFAType();

    if (AsTypeDesc()->IsNativeValueType())
        return AsNativeValueType()->GetNativeHFAType();

    return CORINFO_HFA_ELEM_NONE;
}


#ifdef FEATURE_64BIT_ALIGNMENT
bool TypeHandle::RequiresAlign8() const
{
    WRAPPER_NO_CONTRACT;

    if (IsNativeValueType())
        return AsNativeValueType()->NativeRequiresAlign8();

    return GetMethodTable()->RequiresAlign8();
}
#endif // FEATURE_64BIT_ALIGNMENT

#ifndef DACCESS_COMPILE

BOOL TypeHandle::IsBlittable() const
{
    LIMITED_METHOD_CONTRACT;

    if (IsArray())
    {
        // Single dimentional array's of blittable types are also blittable.
        return (GetRank() == 1 && GetArrayElementTypeHandle().IsBlittable());
    }

    if (!IsTypeDesc())
    {
        // This is a simple type (not an array, ptr or byref) so if
        // simply check to see if the type is blittable.
        return AsMethodTable()->IsBlittable();
    }
    else if (AsTypeDesc()->IsNativeValueType())
    {
        return TRUE;
    }

    return FALSE;
}

BOOL TypeHandle::HasLayout() const
{
    WRAPPER_NO_CONTRACT;
    MethodTable *pMT = GetMethodTable();
    return pMT ? pMT->HasLayout() : FALSE;
}

#ifdef FEATURE_COMINTEROP

TypeHandle TypeHandle::GetCoClassForInterface() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->GetCoClassForInterface();
}

DWORD TypeHandle::IsComClassInterface() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->IsComClassInterface();
}

BOOL TypeHandle::IsComObjectType() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->IsComObjectType();
}

BOOL TypeHandle::IsComEventItfType() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->IsComEventItfType();
}

CorIfaceAttr TypeHandle::GetComInterfaceType() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->GetComInterfaceType();
}

TypeHandle TypeHandle::GetDefItfForComClassItf() const
{
    WRAPPER_NO_CONTRACT;
    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->GetDefItfForComClassItf();
}

ComCallWrapperTemplate *TypeHandle::GetComCallWrapperTemplate() const
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(!IsTypeDesc());

    return AsMethodTable()->GetComCallWrapperTemplate();
}

BOOL TypeHandle::SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PRECONDITION(!IsTypeDesc());

    return AsMethodTable()->SetComCallWrapperTemplate(pTemplate);
}

#endif // FEATURE_COMINTEROP

//--------------------------------------------------------------------------------------
// CanCastTo is necessary but not sufficient, as it assumes that any valuetype
// involved is in its boxed form.

BOOL TypeHandle::IsBoxedAndCanCastTo(TypeHandle type, TypeHandlePairList *pPairList) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());

        LOADS_TYPE(CLASS_LOAD_EXACTPARENTS);

        // The caller should check for an exact match.
        // That will cover the cast of a (unboxed) valuetype to itself.
        PRECONDITION(*this != type);
    }
    CONTRACTL_END


    CorElementType fromParamCorType = GetVerifierCorElementType();

    if (CorTypeInfo::IsObjRef(fromParamCorType))
    {
        // fromParamCorType is a reference type. We can just use CanCastTo
        return CanCastTo(type, pPairList);
    }
    else if (CorTypeInfo::IsGenericVariable(fromParamCorType))
    {
        TypeVarTypeDesc* varFromParam = AsGenericVariable();

        if (!varFromParam->ConstraintsLoaded())
            varFromParam->LoadConstraints(CLASS_DEPENDENCIES_LOADED);

        // A generic type parameter cannot be compatible with another type
        // as it could be substitued with a valuetype. However, if it is
        // constrained to a reference type, then we can use CanCastTo.
        if (varFromParam->ConstrainedAsObjRef())
            return CanCastTo(type, pPairList);
    }

    return FALSE;
}

//--------------------------------------------------------------------------------------
// CanCastTo is necessary but not sufficient, as it assumes that any valuetype
// involved is in its boxed form. See IsBoxedAndCanCastTo() if the valuetype
// is not guaranteed to be in its boxed form.

BOOL TypeHandle::CanCastTo(TypeHandle type, TypeHandlePairList *pVisited)  const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());

        LOADS_TYPE(CLASS_LOAD_EXACTPARENTS);
    }
    CONTRACTL_END

    if (*this == type)
        return true;

    if (!IsTypeDesc() && type.IsTypeDesc())
        return false;

    {
        GCX_COOP();

        TypeHandle::CastResult result = CastCache::TryGetFromCache(*this, type);
        if (result != TypeHandle::MaybeCast)
        {
            return (BOOL)result;
        }

        if (IsTypeDesc())
            return AsTypeDesc()->CanCastTo(type, pVisited);

#ifndef CROSSGEN_COMPILE
        // we check nullable case first because it is not cacheable.
        // object castability and type castability disagree on T --> Nullable<T>,
        // so we can't put this in the cache
        if (Nullable::IsNullableForType(type, AsMethodTable()))
        {
            // do not allow type T to be cast to Nullable<T>
            return FALSE;
        }
#endif  //!CROSSGEN_COMPILE

        return AsMethodTable()->CanCastTo(type.AsMethodTable(), pVisited);
    }
}

#include <optsmallperfcritical.h>
TypeHandle::CastResult TypeHandle::CanCastToCached(TypeHandle type)  const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (*this == type)
        return CanCast;

    if (!IsTypeDesc() && type.IsTypeDesc())
        return CannotCast;

    return CastCache::TryGetFromCache(*this, type);
}
#include <optdefault.h>

#endif // #ifndef DACCESS_COMPILE

void TypeHandle::GetName(SString &result) const
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    if (IsTypeDesc())
    {
        AsTypeDesc()->GetName(result);
        return;
    }

    AsMethodTable()->_GetFullyQualifiedNameForClass(result);

    // Tack the instantiation on the end
    Instantiation inst = GetInstantiation();
    if (!inst.IsEmpty())
        TypeString::AppendInst(result, inst);
}

TypeHandle TypeHandle::GetParent()  const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (IsTypeDesc())
        return(AsTypeDesc()->GetParent());
    else
        return TypeHandle(AsMethodTable()->GetParentMethodTable());
}
#ifndef DACCESS_COMPILE

/* static */
TypeHandle TypeHandle::MergeClassWithInterface(TypeHandle tClass, TypeHandle tInterface)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    MethodTable *pMTClass = tClass.AsMethodTable();

    // Check if the class implements the interface
    if (pMTClass->ImplementsEquivalentInterface(tInterface.AsMethodTable()))
    {
        // The class implements the interface or its equivalent, so our merged state should be the interface
        return tInterface;
    }

    // Check if the class and the interface implement a common interface
    MethodTable *pMTInterface = tInterface.AsMethodTable();
    MethodTable::InterfaceMapIterator intIt = pMTInterface->IterateInterfaceMap();
    while (intIt.Next())
    {
        MethodTable *pMT = intIt.GetInterface();
        if (pMTClass->ImplementsEquivalentInterface(pMT))
        {
            // Found a common interface.  If there are multiple common interfaces, then
            // the problem is ambiguous so we'll just take the first one--it's the best
            // we can do.  If an ensuing code path relies on another common interface,
            // the verifier will think the code is unverifiable, but it would require a
            // major redesign of the verifier to fix that.
            return TypeHandle(pMT);
        }
    }

    // No compatible merge found - using Object
    return TypeHandle(g_pObjectClass);
}

/* static */
TypeHandle TypeHandle::MergeTypeHandlesToCommonParent(TypeHandle ta, TypeHandle tb)
{
    CONTRACTL
    {
      THROWS;
      GC_TRIGGERS;
      INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    _ASSERTE(!ta.IsNull() && !tb.IsNull());

    if (ta == tb)
        return ta;

    // Handle the array case
    if (ta.IsArray())
    {
        if (tb.IsArray())
            return MergeArrayTypeHandlesToCommonParent(ta, tb);

        if (tb.IsInterface() && tb.HasInstantiation())
        {
            //Check to see if we can merge the array to a common interface (such as Derived[] and IList<Base>)
            if (ta.CanCastTo(tb, /* pVisited */ NULL))
                return tb;
        }

        ta = TypeHandle(g_pArrayClass);         // keep merging from here.
    }
    else if (tb.IsArray())
    {
        if (ta.IsInterface() && ta.HasInstantiation())
        {
            //Check to see if we can merge the array to a common interface (such as Derived[] and IList<Base>)
            if (tb.CanCastTo(ta, /* pVisited */ NULL))
                return ta;
        }

        tb = TypeHandle(g_pArrayClass);
    }


    // If either is a (by assumption boxed) type variable
    // return the supertype, if they are related, or object if they are incomparable.
    if (ta.IsGenericVariable() || tb.IsGenericVariable())
    {
        if (ta.CanCastTo(tb))
            return tb;
        if (tb.CanCastTo(ta))
            return ta;
        return TypeHandle(g_pObjectClass);
    }


    _ASSERTE(!ta.IsTypeDesc() && !tb.IsTypeDesc());


    MethodTable *pMTa = ta.AsMethodTable();
    MethodTable *pMTb = tb.AsMethodTable();

    if (pMTb->IsInterface())
    {

        if (pMTa->IsInterface())
        {
            //
            // Both classes are interfaces.  Check that if one
            // interface extends the other.
            //
            // Does tb extend ta ?
            //

            if (pMTb->ImplementsEquivalentInterface(pMTa))
            {
                // tb extends ta, so our merged state should be ta
                return ta;
            }

            //
            // Does tb extend ta ?
            //
            if (pMTa->ImplementsEquivalentInterface(pMTb))
            {
                // ta extends tb, so our merged state should be tb
                return tb;
            }

            // No compatible merge found - using Object
            return TypeHandle(g_pObjectClass);
        }
        else
            return MergeClassWithInterface(ta, tb);
    }
    else if (pMTa->IsInterface())
        return MergeClassWithInterface(tb, ta);

    DWORD   aDepth = 0;
    DWORD   bDepth = 0;
    TypeHandle tSearch;

    // find the depth in the class hierarchy for each class
    for (tSearch = ta; (!tSearch.IsNull()); tSearch = tSearch.GetParent())
        aDepth++;

    for (tSearch = tb; (!tSearch.IsNull()); tSearch = tSearch.GetParent())
        bDepth++;

    // for whichever class is lower down in the hierarchy, walk up the superclass chain
    // to the same level as the other class
    while (aDepth > bDepth)
    {
        ta = ta.GetParent();
        aDepth--;
    }

    while (bDepth > aDepth)
    {
        tb = tb.GetParent();
        bDepth--;
    }

    while (!ta.IsEquivalentTo(tb))
    {
        ta = ta.GetParent();
        tb = tb.GetParent();
    }

    // If no compatible merge is found, we end up using Object

    _ASSERTE(!ta.IsNull());

    return ta;
}

/* static */
TypeHandle TypeHandle::MergeArrayTypeHandlesToCommonParent(TypeHandle ta, TypeHandle tb)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    CorElementType taKind = ta.GetInternalCorElementType();
    CorElementType tbKind = tb.GetInternalCorElementType();
    _ASSERTE(CorTypeInfo::IsArray(taKind) && CorTypeInfo::IsArray(tbKind));

    TypeHandle taElem;
    TypeHandle tMergeElem;

    // If they match we are good to go.
    if (ta == tb)
        return ta;

    if (ta == TypeHandle(g_pArrayClass))
        return ta;
    else if (tb == TypeHandle(g_pArrayClass))
        return tb;

    // Get the rank and kind of the first array
    DWORD rank = ta.GetRank();
    CorElementType mergeKind = taKind;

    // if no match on the rank the common ancestor is System.Array
    if (rank != tb.GetRank())
        return TypeHandle(g_pArrayClass);

    if (tbKind != taKind)
    {
        if (CorTypeInfo::IsArray(tbKind) &&
            CorTypeInfo::IsArray(taKind) && rank == 1)
            mergeKind = ELEMENT_TYPE_ARRAY;
        else
        return TypeHandle(g_pArrayClass);
    }

    // If both are arrays of reference types, return an array of the common
    // ancestor.
    taElem = ta.GetArrayElementTypeHandle();
    if (taElem.IsEquivalentTo(tb.GetArrayElementTypeHandle()))
    {
        // The element types match/are equivalent, so we are good to go.
        tMergeElem = taElem;
    }
    else if (taElem.IsArray() && tb.GetArrayElementTypeHandle().IsArray())
    {
        // Arrays - Find the common ancestor of the element types.
        tMergeElem = MergeArrayTypeHandlesToCommonParent(taElem, tb.GetArrayElementTypeHandle());
    }
    else if (CorTypeInfo::IsObjRef(taElem.GetSignatureCorElementType()) &&
            CorTypeInfo::IsObjRef(tb.GetArrayElementTypeHandle().GetSignatureCorElementType()))
    {
        // Find the common ancestor of the element types.
        tMergeElem = MergeTypeHandlesToCommonParent(taElem, tb.GetArrayElementTypeHandle());
    }
    else
    {
        // The element types have nothing in common.
        return TypeHandle(g_pArrayClass);
    }


    {
        // This should just result in resolving an already loaded type.
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
        // == FailIfNotLoadedOrNotRestored
        TypeHandle result = ClassLoader::LoadArrayTypeThrowing(tMergeElem, mergeKind, rank, ClassLoader::DontLoadTypes);
        _ASSERTE(!result.IsNull());

        // <TODO> should be able to assert IsRestored here </TODO>
        return result;
    }
}

#endif // #ifndef DACCESS_COMPILE

BOOL TypeHandle::IsEnum()  const
{
    LIMITED_METHOD_CONTRACT;

    return (!IsTypeDesc() && AsMethodTable()->IsEnum());
}

BOOL TypeHandle::IsFnPtrType() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (IsTypeDesc() &&
            (GetSignatureCorElementType() == ELEMENT_TYPE_FNPTR));
}

BOOL TypeHandle::IsRestored_NoLogging() const
{
    LIMITED_METHOD_CONTRACT;

    if (!IsTypeDesc())
    {
        return AsMethodTable()->IsRestored_NoLogging();
    }
    else
    {
        return AsTypeDesc()->IsRestored_NoLogging();
    }
}

BOOL TypeHandle::IsRestored() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!IsTypeDesc())
    {
        return AsMethodTable()->IsRestored();
    }
    else
    {
        return AsTypeDesc()->IsRestored();
    }
}

BOOL TypeHandle::IsEncodedFixup() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return CORCOMPILE_IS_POINTER_TAGGED(m_asTAddr);
}

BOOL TypeHandle::HasUnrestoredTypeKey()  const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (IsTypeDesc())
        return AsTypeDesc()->HasUnrestoredTypeKey();
    else
        return AsMethodTable()->HasUnrestoredTypeKey();
}

#ifdef FEATURE_PREJIT
void TypeHandle::DoRestoreTypeKey()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!IsEncodedFixup());
    }
    CONTRACT_END

#ifndef DACCESS_COMPILE
    if (IsTypeDesc())
    {
        AsTypeDesc()->DoRestoreTypeKey();
    }
    else
    {
        MethodTable* pMT = AsMethodTable();
        PREFIX_ASSUME(pMT != NULL);
        pMT->DoRestoreTypeKey();
    }
#endif

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    if (LoggingOn(LF_CLASSLOADER, LL_INFO10000))
    {
        StackSString name;
        TypeString::AppendTypeDebug(name, *this);
        LOG((LF_CLASSLOADER, LL_INFO10000, "GENERICS:RestoreTypeKey: type %S at %p\n", name.GetUnicode(), AsPtr()));
    }
#endif
#endif


    RETURN;
}
#endif

void TypeHandle::CheckRestore() const
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        PRECONDITION(!IsEncodedFixup());
    }
    CONTRACTL_END

    if (!IsFullyLoaded())
    {
        ClassLoader::EnsureLoaded(*this);
        _ASSERTE(IsFullyLoaded());
    }

    g_IBCLogger.LogTypeMethodTableAccess(this);
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
BOOL TypeHandle::ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited) const
{
    STATIC_STANDARD_VM_CONTRACT;

    _ASSERTE(GetAppDomain()->IsCompilationDomain());

    if (!IsTypeDesc())
        return AsMethodTable()->ComputeNeedsRestore(image, pVisited);
    else
        return AsTypeDesc()->ComputeNeedsRestore(image, pVisited);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

BOOL
TypeHandle::IsExternallyVisible() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    if (!IsTypeDesc())
    {
        return AsMethodTable()->IsExternallyVisible();
    }

    if (IsGenericVariable())
    {   // VAR, MVAR
        return TRUE;
    }

    if (IsFnPtrType())
    {   // FNPTR
        // Function pointer has to check its all argument types
        return AsFnPtrType()->IsExternallyVisible();
    }
    // PTR, BYREF
    _ASSERTE(HasTypeParam());

    TypeHandle paramType = AsTypeDesc()->GetTypeParam();
    _ASSERTE(!paramType.IsNull());

    return paramType.IsExternallyVisible();
} // TypeHandle::IsExternallyVisible

#ifndef CROSSGEN_COMPILE
OBJECTREF TypeHandle::GetManagedClassObject() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // Force a GC here because GetManagedClassObject could trigger GC nondeterminsticaly
    GCStress<cfg_any, PulseGcTriggerPolicy>::MaybeTrigger();
#endif // _DEBUG

    if (!IsTypeDesc())
    {
        return AsMethodTable()->GetManagedClassObject();
    }
    else
    {
        switch(GetInternalCorElementType())
        {
            case ELEMENT_TYPE_ARRAY:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_PTR:
                return ((ParamTypeDesc*)AsTypeDesc())->GetManagedClassObject();

            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
                return ((TypeVarTypeDesc*)AsTypeDesc())->GetManagedClassObject();

            case ELEMENT_TYPE_FNPTR:
                // A function pointer is mapped into typeof(IntPtr). It results in a loss of information.
                return CoreLibBinder::GetElementType(ELEMENT_TYPE_I)->GetManagedClassObject();

            default:
                _ASSERTE(!"Bad Element Type");
                return NULL;
        }
    }
}
#endif // CROSSGEN_COMPILE

#endif // #ifndef DACCESS_COMPILE

BOOL TypeHandle::IsByRef() const
{
    LIMITED_METHOD_CONTRACT;

    return (IsTypeDesc() && AsTypeDesc()->IsByRef());
}

BOOL TypeHandle::IsByRefLike() const
{
    LIMITED_METHOD_CONTRACT;

    return (!IsTypeDesc() && AsMethodTable()->IsByRefLike());
}

BOOL TypeHandle::IsPointer() const
{
    LIMITED_METHOD_CONTRACT;

    return (IsTypeDesc() && AsTypeDesc()->IsPointer());
}

//
// The internal type is the type that most of the runtime cares about.  This type has had two normalizations
// applied to it
//
//    * Enumerated type have been normalized to the primitive type that underlies them (typically int)
//    * Value types that look like ints (which include RuntimeTypeHandles, etc), have been morphed to be
//        their underlying type (much like enumeration types.  See
// * see code:MethodTable#KindsOfElementTypes for more
// * This value is set by code:EEClass::ComputeInternalCorElementTypeForValueType
CorElementType TypeHandle::GetInternalCorElementType()  const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->GetInternalCorElementType();
    else
        return AsMethodTable()->GetInternalCorElementType();
}

BOOL TypeHandle::HasInstantiation()  const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc()) return false;
    if (IsNull()) return false;
    return AsMethodTable()->HasInstantiation();
}

ClassLoadLevel TypeHandle::GetLoadLevel() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
    {
        return AsTypeDesc()->GetLoadLevel();
    }
    else
    {
        return AsMethodTable()->GetLoadLevel();
    }
}

BOOL TypeHandle::IsFullyLoaded() const
{
    LIMITED_METHOD_CONTRACT;

    if (IsTypeDesc())
    {
        return AsTypeDesc()->IsFullyLoaded();
    }
    else
    {
        return AsMethodTable()->IsFullyLoaded();
    }
}

void TypeHandle::DoFullyLoad(Generics::RecursionGraph *pVisited, ClassLoadLevel level,
                             DFLPendingList *pPending, BOOL *pfBailed, const InstantiationContext *pInstContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(level == CLASS_LOADED || level == CLASS_DEPENDENCIES_LOADED);
    _ASSERTE(pfBailed != NULL);
    _ASSERTE(!(level == CLASS_LOADED && pPending == NULL));


    if (IsTypeDesc())
    {
        return AsTypeDesc()->DoFullyLoad(pVisited, level, pPending, pfBailed, pInstContext);
    }
    else
    {
        return AsMethodTable()->DoFullyLoad(pVisited, level, pPending, pfBailed, pInstContext);
    }
}

// As its name suggests, this returns the type as it is in the meta-data signature.  No morphing to deal
// with verification or with value types that are treated as primitives is done.
// see code:MethodTable#KindsOfElementTypes for more
CorElementType TypeHandle::GetSignatureCorElementType() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    // This gets used by
    //     MethodTable::DoRestoreTypeKey() -->
    //     Module::RestoreMethodTablePointer() -->
    //     ZapSig::DecodeType() -->
    //     SigPointer::GetTypeHandleThrowing -->
    //     TypeHandle::GetSignatureCorElementType
    // early on during the process of restoring, i.e. after the EEClass for the
    // MT is restored but not the parent method table.  Thus we cannot
    // assume that the parent method table is even yet a valid pointer.
    // However both MethodTable::GetClass and MethodTable::IsValueType work
    // even if the parent method table pointer has not been restored.

    if (IsTypeDesc())
    {
        return AsTypeDesc()->GetInternalCorElementType();
    }
    else
    {
        return AsMethodTable()->GetSignatureCorElementType();
    }
}

// As its name suggests, this returns the type used by the IL verifier. The basic difference between this
// type and the type in the meta-data is that enumerations have been normalized to their underlieing
// primitive type. see code:MethodTable#KindsOfElementTypes for more
CorElementType TypeHandle::GetVerifierCorElementType() const
{
    LIMITED_METHOD_CONTRACT;

    if (IsTypeDesc())
    {
        return AsTypeDesc()->GetInternalCorElementType();
    }
    else
    {
        return AsMethodTable()->GetVerifierCorElementType();
    }
}


#ifdef DACCESS_COMPILE

void
TypeHandle::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    if (!m_asTAddr)
    {
        return;
    }

    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
    (
        if (IsGenericVariable())
        {
            AsGenericVariable()->EnumMemoryRegions(flags);
        }
        else if (IsFnPtrType())
        {
            AsFnPtrType()->EnumMemoryRegions(flags);
        }
        else if (IsTypeDesc())
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(AsTypeDesc()), sizeof(TypeDesc));
        }
        else
        {
            AsMethodTable()->EnumMemoryRegions(flags);
        }
    );
}

#endif // DACCESS_COMPILE



//--------------------------------------------------------------------------------------
// For generic instantiations, check that it satisfies constraints.
//
// Because this is really a part of DoFullyLoad() that is broken out for readability reasons,
// it takes both the typehandle and its template typehandle as a parameter (DoFullyLoad
// already has the latter typehandle so this way, we avoid a second call to the loader.)
//
// Return value:
//
//   Returns TRUE if constraints are satisfied.
//
//   Returns FALSE if constraints are violated and the type is a canonical instantiation. (We
//     have to let these load as these form the basis of every instantiation. The canonical
//     methodtable is not available to users.
//
//   THROWS if constraints are violated
//
//
//--------------------------------------------------------------------------------------
BOOL SatisfiesClassConstraints(TypeHandle instanceTypeHnd, TypeHandle typicalTypeHnd,
                               const InstantiationContext *pInstContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!instanceTypeHnd.IsCanonicalSubtype());
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    Instantiation formalInst = typicalTypeHnd.GetInstantiation();
    Instantiation actualInst = instanceTypeHnd.GetInstantiation();
    _ASSERTE(formalInst.GetNumArgs() == actualInst.GetNumArgs());

    for (DWORD i = 0; i < actualInst.GetNumArgs(); i++)
    {
        TypeHandle thActualArg = actualInst[i];

        SigTypeContext typeContext;
        SigTypeContext::InitTypeContext(instanceTypeHnd, &typeContext);

        // Log the TypeVarTypeDesc access
        g_IBCLogger.LogTypeMethodTableWriteableAccess(&thActualArg);

        BOOL bSatisfiesConstraints =
            formalInst[i].AsGenericVariable()->SatisfiesConstraints(&typeContext, thActualArg, pInstContext);

        if (!bSatisfiesConstraints)
        {
            SString argNum;
            argNum.Printf("%d", i);

            SString typicalTypeHndName;
            TypeString::AppendType(typicalTypeHndName, typicalTypeHnd);

            SString actualParamName;
            TypeString::AppendType(actualParamName, actualInst[i]);

            SString formalParamName;
            TypeString::AppendType(formalParamName, formalInst[i]);

            COMPlusThrow(kTypeLoadException,
                         IDS_EE_CLASS_CONSTRAINTS_VIOLATION,
                         argNum,
                         actualParamName,
                         typicalTypeHndName,
                         formalParamName
                        );
        }
    }

    return TRUE;

#else
    return TRUE;
#endif
}




#ifndef DACCESS_COMPILE
BOOL TypeHandle::SatisfiesClassConstraints() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    BOOL returnValue = FALSE;
    Instantiation classInst;
    TypeHandle thCanonical;
    Instantiation typicalInst;
    SigTypeContext typeContext;
    TypeHandle thParent;

    //TODO: cache (positive?) result in methodtable using, say, enum_flag2_UNUSEDxxx

    //TODO: reconsider this check
    thParent = GetParent();

    if (!thParent.IsNull() && !thParent.SatisfiesClassConstraints())
    {
        return FALSE;
    }

    if (!HasInstantiation())
    {
        return TRUE;
    }

    classInst = GetInstantiation();
    thCanonical = ClassLoader::LoadTypeDefThrowing(
                                    GetModule(),
                                    GetCl(),
                                    ClassLoader::ThrowIfNotFound,
                                    ClassLoader::PermitUninstDefOrRef);
    typicalInst = thCanonical.GetInstantiation();

    SigTypeContext::InitTypeContext(*this, &typeContext);

    for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
    {
        TypeHandle thArg = classInst[i];
        _ASSERTE(!thArg.IsNull());

        TypeVarTypeDesc* tyvar = typicalInst[i].AsGenericVariable();
        _ASSERTE(tyvar != NULL);
        _ASSERTE(TypeFromToken(tyvar->GetTypeOrMethodDef()) == mdtTypeDef);

        tyvar->LoadConstraints(); //TODO: is this necessary for anything but the typical class?

        if (!tyvar->SatisfiesConstraints(&typeContext, thArg))
        {
            return FALSE;
        }
    }

    return TRUE;
}

TypeKey TypeHandle::GetTypeKey() const
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(!IsGenericVariable());

    if (IsTypeDesc())
    {
        TypeDesc *pTD = AsTypeDesc();
        CorElementType etype = pTD->GetInternalCorElementType();
        if (CorTypeInfo::IsModifier_NoThrow(etype) || etype == ELEMENT_TYPE_VALUETYPE)
        {
            TypeKey tk(etype, pTD->GetTypeParam());
            return tk;
        }
        else
        {
            CONSISTENCY_CHECK(etype == ELEMENT_TYPE_FNPTR);
            FnPtrTypeDesc* pFTD = (FnPtrTypeDesc*) pTD;
            TypeKey tk(pFTD->GetCallConv(), pFTD->GetNumArgs(), pFTD->GetRetAndArgTypesPointer());
            return tk;
        }
    }
    else
    {
        MethodTable *pMT = AsMethodTable();
        if (pMT->IsArray())
        {
            TypeKey tk(pMT->GetInternalCorElementType(), pMT->GetArrayElementTypeHandle(), pMT->GetRank());
            return tk;
        }
        else if (pMT->IsTypicalTypeDefinition())
        {
            TypeKey tk(pMT->GetModule(), pMT->GetCl());
            return tk;
        }
        else
        {
            TypeKey tk(pMT->GetModule(), pMT->GetCl(), pMT->GetInstantiation());
            return tk;
        }
    }
}


#ifdef _DEBUG
// Check that a type handle matches the key provided
CHECK TypeHandle::CheckMatchesKey(TypeKey *pKey) const
{
    WRAPPER_NO_CONTRACT;
    CONTRACT_VIOLATION(TakesLockViolation);        // this is debug-only code
    CONSISTENCY_CHECK(!IsGenericVariable());

    // Check first to avoid creating debug name
    if (!GetTypeKey().Equals(pKey))
    {
        StackSString typeKeyString;
        CONTRACT_VIOLATION(GCViolation|ThrowsViolation);
        TypeString::AppendTypeKeyDebug(typeKeyString, pKey);
        if (!IsTypeDesc() && AsMethodTable()->IsArray())
        {
            MethodTable *pMT = AsMethodTable();
            CHECK_MSGF(pMT->GetInternalCorElementType() == pKey->GetKind(),
                       ("CorElementType %d of Array MethodTable does not match key %S", pMT->GetArrayElementType(), typeKeyString.GetUnicode()));

            CHECK_MSGF(pMT->GetArrayElementTypeHandle() == pKey->GetElementType(),
                       ("Element type of Array MethodTable does not match key %S",typeKeyString.GetUnicode()));

            CHECK_MSGF(pMT->GetRank() == pKey->GetRank(),
                       ("Rank %d of Array MethodTable does not match key %S", pMT->GetRank(), typeKeyString.GetUnicode()));
        }
        else
        if (IsTypeDesc())
        {
            TypeDesc *pTD = AsTypeDesc();
            CHECK_MSGF(pTD->GetInternalCorElementType() == pKey->GetKind(),
                       ("CorElementType %d of TypeDesc does not match key %S", pTD->GetInternalCorElementType(), typeKeyString.GetUnicode()));

            if (CorTypeInfo::IsModifier(pKey->GetKind()))
            {
                CHECK_MSGF(pTD->GetTypeParam() == pKey->GetElementType(),
                           ("Element type of TypeDesc does not match key %S",typeKeyString.GetUnicode()));
            }
        }
        else
        {
            MethodTable *pMT = AsMethodTable();
            CHECK_MSGF(pMT->GetModule() == pKey->GetModule(), ("Module of MethodTable does not match key %S", typeKeyString.GetUnicode()));
            CHECK_MSGF(pMT->GetCl() == pKey->GetTypeToken(),
                       ("TypeDef %x of Methodtable does not match TypeDef %x of key %S", pMT->GetCl(), pKey->GetTypeToken(),
                        typeKeyString.GetUnicode()));

            if (pMT->IsTypicalTypeDefinition())
            {
                CHECK_MSGF(pKey->GetNumGenericArgs() == 0 && !pKey->HasInstantiation(),
                           ("Key %S for Typical MethodTable has non-zero number of generic arguments", typeKeyString.GetUnicode()));
            }
            else
            {
                CHECK_MSGF(pMT->GetNumGenericArgs() == pKey->GetNumGenericArgs(),
                           ("Number of generic params %d in MethodTable does not match key %S", pMT->GetNumGenericArgs(), typeKeyString.GetUnicode()));
                if (pKey->HasInstantiation())
                {
                    for (DWORD i = 0; i < pMT->GetNumGenericArgs(); i++)
                    {
#ifdef FEATURE_PREJIT
                        CHECK_MSGF(ZapSig::CompareTypeHandleFieldToTypeHandle(pMT->GetInstantiation().GetRawArgs()[i].GetValuePtr(), pKey->GetInstantiation()[i]),
                               ("Generic argument %d in MethodTable does not match key %S", i, typeKeyString.GetUnicode()));
#else
                        CHECK_MSGF(pMT->GetInstantiation()[i] == pKey->GetInstantiation()[i],
                               ("Generic argument %d in MethodTable does not match key %S", i, typeKeyString.GetUnicode()));
#endif
                    }
                }
            }
        }
    }
    CHECK_OK;
}

const char * const classLoadLevelName[] =
{
    "BEGIN",
    "UNRESTOREDTYPEKEY",
    "UNRESTORED",
    "APPROXPARENTS",
    "EXACTPARENTS",
    "DEPENDENCIES_LOADED",
    "LOADED",
};

// Check that this type is loaded up to the level indicated
// Also check that it is non-null
CHECK TypeHandle::CheckLoadLevel(ClassLoadLevel requiredLevel)
{
    CHECK(!IsNull());
    //    CHECK_MSGF(!IsNull(), ("Type is null, required load level is %s", classLoadLevelName[requiredLevel]));
    static_assert_no_msg(NumItems(classLoadLevelName) == (1 + CLASS_LOAD_LEVEL_FINAL));

    // Quick check to avoid creating debug string
    ClassLoadLevel actualLevel = GetLoadLevel();
    if (actualLevel < requiredLevel)
    {
        //        SString debugTypeName;
        //        TypeString::AppendTypeDebug(debugTypeName, *this);
        CHECK(actualLevel >= requiredLevel);
        //        CHECK_MSGF(actualLevel >= requiredLevel,
        //                   ("Type has not been sufficiently loaded (actual level is %d, required level is %d)",
        //                    /* debugTypeName.GetUnicode(), */ actualLevel, requiredLevel /* classLoadLevelName[actualLevel], classLoadLevelName[requiredLevel] */));
    }
    CONSISTENCY_CHECK((actualLevel > CLASS_LOAD_UNRESTORED) == IsRestored());
    CONSISTENCY_CHECK((actualLevel == CLASS_LOAD_UNRESTOREDTYPEKEY) == HasUnrestoredTypeKey());
    CHECK_OK;
}

// Check that this type is fully loaded (i.e. to level CLASS_LOADED)
CHECK TypeHandle::CheckFullyLoaded()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (IsGenericVariable())
    {
        CHECK_OK;
    }
    CheckLoadLevel(CLASS_LOADED);
    CHECK_OK;
}

#endif //DEBUG

#endif //DACCESS_COMPILE
