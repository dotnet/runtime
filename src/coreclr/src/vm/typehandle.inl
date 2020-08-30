// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: typehandle.inl
//

#ifndef _TYPEHANDLE_INL_
#define _TYPEHANDLE_INL_

#include "typehandle.h"

inline mdTypeDef TypeHandle::GetCl() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    PREFIX_ASSUME(GetMethodTable() != NULL);
    return GetMethodTable()->GetCl();
}

inline PTR_MethodTable TypeHandle::GetMethodTable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsTypeDesc())
        return(AsTypeDesc()->GetMethodTable());
    else
        return AsMethodTable();
}

inline void TypeHandle::SetIsFullyLoaded()
{
    LIMITED_METHOD_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->SetIsFullyLoaded();
    else
        return AsMethodTable()->SetIsFullyLoaded();
}

inline MethodTable* TypeHandle::GetMethodTableOfRootTypeParam() const
{
    LIMITED_METHOD_CONTRACT;

    TypeHandle current = *this;
    while (current.HasTypeParam())
        current = current.GetTypeParam();

    return current.GetMethodTable();
}

inline TypeHandle TypeHandle::GetArrayElementTypeHandle() const
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsArray());

    return AsMethodTable()->GetArrayElementTypeHandle();
}

inline unsigned int TypeHandle::GetRank() const
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsArray());

    return AsMethodTable()->GetRank();
}

inline BOOL TypeHandle::IsZapped() const
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_PREJIT
    return (GetZapModule() != NULL);
#else
    return FALSE;
#endif
}

// Methods to allow you get get a the two possible representations
inline PTR_MethodTable TypeHandle::AsMethodTable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(!IsTypeDesc());

    return PTR_MethodTable(m_asTAddr);
}

inline PTR_TypeDesc TypeHandle::AsTypeDesc() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(IsTypeDesc());

    PTR_TypeDesc result = PTR_TypeDesc(m_asTAddr - 2);
    PREFIX_ASSUME(result != NULL);
    return result;
}

inline FnPtrTypeDesc* TypeHandle::AsFnPtrType() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(IsFnPtrType());

    FnPtrTypeDesc* result = PTR_FnPtrTypeDesc(m_asTAddr - 2);
    PREFIX_ASSUME(result != NULL);
    return result;
}

inline TypeVarTypeDesc* TypeHandle::AsGenericVariable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(IsGenericVariable());

    TypeVarTypeDesc* result = PTR_TypeVarTypeDesc(m_asTAddr - 2);
    PREFIX_ASSUME(result != NULL);
    return result;
}

inline BOOL TypeHandle::IsNativeValueType() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (IsTypeDesc() && AsTypeDesc()->IsNativeValueType());
}

inline MethodTable *TypeHandle::AsNativeValueType() const
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsNativeValueType());
    return AsTypeDesc()->GetMethodTable();
}

inline BOOL TypeHandle::IsTypicalTypeDefinition() const
{
    LIMITED_METHOD_CONTRACT;

    return !HasInstantiation() || IsGenericTypeDefinition();
}

inline BOOL TypeHandle::HasTypeEquivalence() const
{
    LIMITED_METHOD_CONTRACT;

    if (IsTypeDesc())
        return AsTypeDesc()->HasTypeEquivalence();
    else
        return AsMethodTable()->HasTypeEquivalence();
}


//--------------------------------------------------------------------------------------
// IsEquivalentTo is based on Guid and TypeIdentifier attributes to support the "no-PIA"
// feature. The idea is that compilers pull types from the PIA into different assemblies
// and these types - represented by separate MTs/TypeHandles - are considered equivalent
// for certain operations.


#ifndef DACCESS_COMPILE
inline BOOL TypeHandle::IsEquivalentTo(TypeHandle type COMMA_INDEBUG(TypeHandlePairList *pVisited /*= NULL*/)) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (*this == type)
        return TRUE;

#ifdef FEATURE_TYPEEQUIVALENCE
    // bail early for normal types
    if (!HasTypeEquivalence() || !type.HasTypeEquivalence())
        return FALSE;

    if (IsTypeDesc())
        return AsTypeDesc()->IsEquivalentTo(type COMMA_INDEBUG(pVisited));

    if (type.IsTypeDesc())
        return FALSE;

    return AsMethodTable()->IsEquivalentTo_Worker(type.AsMethodTable() COMMA_INDEBUG(pVisited));
#else
    return FALSE;
#endif
}
#endif

// Execute the callback functor for each MethodTable that makes up the given type handle.  This method
// does not invoke the functor for generic variables
template<class T>
inline void TypeHandle::ForEachComponentMethodTable(T &callback) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (HasTypeParam())
    {
        // If we have a type parameter, then we just need to invoke ourselves on that parameter
        GetTypeParam().ForEachComponentMethodTable(callback);
    }
    else if (IsFnPtrType())
    {
        // If we are a function pointer, then we need to invoke the callback method on the function
        // pointer's return type as well as each of its argument types
        FnPtrTypeDesc *pFnPtr = AsFnPtrType();
        for (DWORD iArg = 0; iArg < pFnPtr->GetNumArgs() + 1; ++iArg)
        {
            pFnPtr->GetRetAndArgTypesPointer()[iArg].ForEachComponentMethodTable(callback);
        }
    }
    else if (HasInstantiation())
    {
        // If we have a generic instantiation, we need to invoke the callback on each of the generic
        // parameters as well as the root method table.
        callback(GetMethodTable());

        Instantiation instantiation = GetInstantiation();
        for (DWORD iGenericArg = 0; iGenericArg < instantiation.GetNumArgs(); ++iGenericArg)
        {
            instantiation[iGenericArg].ForEachComponentMethodTable(callback);
        }
    }
    else if (IsGenericVariable())
    {
        // We don't invoke the callback on generic variables since they don't have method tables
        return;
    }
    else
    {
        // Otherwise, we must be a simple type, so just do the callback directly on the method table
        callback(GetMethodTable());
    }
}

#ifndef CROSSGEN_COMPILE
FORCEINLINE OBJECTREF TypeHandle::GetManagedClassObjectFast() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    OBJECTREF o = NULL;

    if (!IsTypeDesc())
    {
        o = AsMethodTable()->GetManagedClassObjectIfExists();
    }
    else
    {
        switch (AsTypeDesc()->GetInternalCorElementType())
        {
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_PTR:
            o = dac_cast<PTR_ParamTypeDesc>(AsTypeDesc())->GetManagedClassObjectFast();
            break;

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
            o = dac_cast<PTR_TypeVarTypeDesc>(AsTypeDesc())->GetManagedClassObjectFast();
            break;

        case ELEMENT_TYPE_FNPTR:
            // A function pointer is mapped into typeof(IntPtr). It results in a loss of information.
            o = CoreLibBinder::GetElementType(ELEMENT_TYPE_I)->GetManagedClassObjectIfExists();
            break;

        default:
            _ASSERTE(!"Bad Element Type");
            return NULL;
        }
    }

    return o;
}
#endif // CROSSGEN_COMPILE

#endif  // _TYPEHANDLE_INL_
