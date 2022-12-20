// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File: typedesc.cpp
//
// This file contains definitions for methods in the code:TypeDesc class and its
// subclasses
//     code:ParamTypeDesc,
//     code:TyVarTypeDesc,
//     code:FnPtrTypeDesc
//
// ============================================================================

#include "common.h"
#include "typedesc.h"
#include "typestring.h"
#include "array.h"
#include "castcache.h"

#ifndef DACCESS_COMPILE
#ifdef _DEBUG

BOOL ParamTypeDesc::Verify() {

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_SUPPORTS_DAC;

    _ASSERTE(!GetTypeParam().IsNull());
    _ASSERTE(CorTypeInfo::IsModifier_NoThrow(GetInternalCorElementType()) ||
                              GetInternalCorElementType() == ELEMENT_TYPE_VALUETYPE);
    GetTypeParam().Verify();
    return(true);
}

#endif

#endif // #ifndef DACCESS_COMPILE

TypeHandle TypeDesc::GetRootTypeParam()
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(HasTypeParam());

    TypeHandle th = GetTypeParam();
    while (th.HasTypeParam())
    {
        th = th.GetTypeParam();
    }
    _ASSERTE(!th.IsNull());

    return th;
}

PTR_Module TypeDesc::GetLoaderModule()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    if (HasTypeParam())
    {
        return GetRootTypeParam().GetLoaderModule();
    }
    else if (IsGenericVariable())
    {
        return dac_cast<PTR_TypeVarTypeDesc>(this)->GetModule();
    }
    else
    {
        PTR_Module retVal = NULL;
        BOOL fFail = FALSE;

        _ASSERTE(GetInternalCorElementType() == ELEMENT_TYPE_FNPTR);
        PTR_FnPtrTypeDesc asFnPtr = dac_cast<PTR_FnPtrTypeDesc>(this);
        if (!fFail)
        {
            retVal = ClassLoader::ComputeLoaderModuleForFunctionPointer(asFnPtr->GetRetAndArgTypesPointer(), asFnPtr->GetNumArgs()+1);
        }
        return retVal;
    }
}

PTR_BaseDomain TypeDesc::GetDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    return dac_cast<PTR_BaseDomain>(AppDomain::GetCurrentDomain());
}

PTR_Module TypeDesc::GetModule() {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
        // Function pointer types belong to no module
        //PRECONDITION(GetInternalCorElementType() != ELEMENT_TYPE_FNPTR);
    }
    CONTRACTL_END

    // Note here we are making the assumption that a typeDesc lives in
    // the classloader of its element type.

    if (HasTypeParam())
    {
        return GetRootTypeParam().GetModule();
    }

    if (IsGenericVariable())
    {
        PTR_TypeVarTypeDesc asVar = dac_cast<PTR_TypeVarTypeDesc>(this);
        return asVar->GetModule();
    }

    _ASSERTE(GetInternalCorElementType() == ELEMENT_TYPE_FNPTR);

    return GetLoaderModule();
}

Assembly* TypeDesc::GetAssembly() {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    Module *pModule = GetModule();
    PREFIX_ASSUME(pModule!=NULL);
    return pModule->GetAssembly();
}

void TypeDesc::GetName(SString &ssBuf)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    CorElementType kind = GetInternalCorElementType();
    TypeHandle th;
    int rank;

    if (CorTypeInfo::IsModifier(kind))
        th = GetTypeParam();
    else
        th = TypeHandle(this);

    if (CorTypeInfo::IsGenericVariable(kind))
        rank = dac_cast<PTR_TypeVarTypeDesc>(this)->GetIndex();
    else
        rank = 0;

    ConstructName(kind, th, rank, ssBuf);
}

void TypeDesc::ConstructName(CorElementType kind,
                             TypeHandle param,
                             int rank,
                             SString &ssBuff)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM()); // SString operations can allocate.
    }
    CONTRACTL_END

    if (CorTypeInfo::IsModifier(kind))
    {
        param.GetName(ssBuff);
    }

    switch(kind)
    {
    case ELEMENT_TYPE_BYREF:
        ssBuff.Append(W('&'));
        break;

    case ELEMENT_TYPE_PTR:
        ssBuff.Append(W('*'));
        break;

    case ELEMENT_TYPE_SZARRAY:
        ssBuff.Append(W("[]"));
        break;

    case ELEMENT_TYPE_ARRAY:
        ssBuff.Append(W('['));

        if (rank == 1)
        {
            ssBuff.Append(W('*'));
        }
        else
        {
            while(--rank > 0)
            {
                ssBuff.Append(W(','));
            }
        }

        ssBuff.Append(W(']'));
        break;

    case ELEMENT_TYPE_VAR:
    case ELEMENT_TYPE_MVAR:
    {
        if (kind == ELEMENT_TYPE_VAR)
        {
            ssBuff.Append(W('!'));
        }
        else
        {
            ssBuff.Append(W("!!"));
        }

        WCHAR buffer[MaxSigned32BitDecString + 1];
        FormatInteger(buffer, ARRAY_SIZE(buffer), "%d", rank);
        ssBuff.Append(buffer);
    }
        break;

    case ELEMENT_TYPE_FNPTR:
        ssBuff.Set(W("FNPTR"));
        break;

    default:
        LPCUTF8 namesp = CorTypeInfo::GetNamespace(kind);
        if(namesp && *namesp) {
            ssBuff.AppendUTF8(namesp);
            ssBuff.Append(W('.'));
        }

        LPCUTF8 name = CorTypeInfo::GetName(kind);
        BAD_FORMAT_NOTHROW_ASSERT(name);
        if (name && *name) {
            ssBuff.AppendUTF8(name);
        }
    }
}

BOOL TypeDesc::IsGenericVariable()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return CorTypeInfo::IsGenericVariable_NoThrow(GetInternalCorElementType());
}

BOOL TypeDesc::IsFnPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (GetInternalCorElementType() == ELEMENT_TYPE_FNPTR);
}

BOOL TypeDesc::IsNativeValueType()
{
    WRAPPER_NO_CONTRACT;
    return (GetInternalCorElementType() == ELEMENT_TYPE_VALUETYPE);
}

BOOL TypeDesc::HasTypeParam()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return CorTypeInfo::IsModifier_NoThrow(GetInternalCorElementType()) ||
           GetInternalCorElementType() == ELEMENT_TYPE_VALUETYPE;
}

#ifndef DACCESS_COMPILE

BOOL TypeDesc::CanCastTo(TypeHandle toTypeHnd, TypeHandlePairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    if (TypeHandle(this) == toTypeHnd)
        return TRUE;

    BOOL fCast = FALSE;

    //A boxed variable type can be cast to any of its constraints, or object, if none are specified
    if (IsGenericVariable())
    {
        TypeVarTypeDesc *tyvar = (TypeVarTypeDesc*) this;

        if (toTypeHnd == g_pObjectClass)
        {
            fCast = TRUE;
        }
        else if (toTypeHnd == g_pValueTypeClass)
        {
            mdGenericParam genericParamToken = tyvar->GetToken();
            DWORD flags;
            if (!FAILED(tyvar->GetModule()->GetMDImport()->GetGenericParamProps(genericParamToken, NULL, &flags, NULL, NULL, NULL)))
            {
                DWORD specialConstraints = flags & gpSpecialConstraintMask;
                if ((specialConstraints & gpNotNullableValueTypeConstraint) != 0)
                {
                    fCast = TRUE;
                }
            }
        }
        else
        {
            DWORD numConstraints;
            TypeHandle* constraints = tyvar->GetConstraints(&numConstraints, CLASS_DEPENDENCIES_LOADED);

            if (constraints != NULL)
            {
                for (DWORD i = 0; i < numConstraints; i++)
                {
                    if (constraints[i].CanCastTo(toTypeHnd, pVisited))
                    {
                        fCast = TRUE;
                        break;
                    }
                }
            }
        }
    }
    else if (toTypeHnd.IsTypeDesc())
    {
        TypeDesc* toTypeDesc = toTypeHnd.AsTypeDesc();
        CorElementType toKind = toTypeDesc->GetInternalCorElementType();
        CorElementType fromKind = GetInternalCorElementType();

        // The element kinds must match
        if (toKind == fromKind)
        {
            switch (toKind)
            {
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_PTR:
                fCast = TypeDesc::CanCastParam(dac_cast<PTR_ParamTypeDesc>(this)->GetTypeParam(), dac_cast<PTR_ParamTypeDesc>(toTypeDesc)->GetTypeParam(), pVisited);
                break;
            default:
                break;
            }
        }
    }

    CastCache::TryAddToCache(TypeHandle(this), toTypeHnd, fCast);
    return fCast;
}

BOOL TypeDesc::CanCastParam(TypeHandle fromParam, TypeHandle toParam, TypeHandlePairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

        // While boxed value classes inherit from object their
        // unboxed versions do not.  Parameterized types have the
        // unboxed version, thus, if the from type parameter is value
        // class then only an exact match/equivalence works.
    if (fromParam.IsEquivalentTo(toParam))
        return TRUE;

        // Object parameters dont need an exact match but only inheritance, check for that
    CorElementType fromParamCorType = fromParam.GetVerifierCorElementType();
    if (CorTypeInfo::IsObjRef(fromParamCorType))
    {
        return fromParam.CanCastTo(toParam, pVisited);
    }
    else if (CorTypeInfo::IsGenericVariable(fromParamCorType))
    {
        TypeVarTypeDesc* varFromParam = fromParam.AsGenericVariable();

        if (!varFromParam->ConstraintsLoaded())
            varFromParam->LoadConstraints(CLASS_DEPENDENCIES_LOADED);

        if (!varFromParam->ConstrainedAsObjRef())
            return FALSE;

        return fromParam.CanCastTo(toParam, pVisited);
    }
    else if(CorTypeInfo::IsPrimitiveType(fromParamCorType))
    {
        CorElementType toParamCorType = toParam.GetVerifierCorElementType();
        if(CorTypeInfo::IsPrimitiveType(toParamCorType))
        {
            if (GetNormalizedIntegralArrayElementType(toParamCorType) == GetNormalizedIntegralArrayElementType(fromParamCorType))
                return TRUE;
        } // end if(CorTypeInfo::IsPrimitiveType(toParamCorType))
    } // end if(CorTypeInfo::IsPrimitiveType(fromParamCorType))

        // Anything else is not a match.
    return FALSE;
}

TypeHandle::CastResult TypeDesc::CanCastToCached(TypeHandle toType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END

    return CastCache::TryGetFromCache(TypeHandle(this), toType);
}

BOOL TypeDesc::IsEquivalentTo(TypeHandle type COMMA_INDEBUG(TypeHandlePairList *pVisited))
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (TypeHandle(this) == type)
        return TRUE;

    if (!type.IsTypeDesc())
        return FALSE;

    TypeDesc *pOther = type.AsTypeDesc();

    // bail early for normal types
    if (!HasTypeEquivalence() || !pOther->HasTypeEquivalence())
        return FALSE;

    // if the TypeDesc types are different, then they are not equivalent
    if (GetInternalCorElementType() != pOther->GetInternalCorElementType())
        return FALSE;

    if (HasTypeParam())
    {
        // pointer, byref
        return GetTypeParam().IsEquivalentTo(pOther->GetTypeParam() COMMA_INDEBUG(pVisited));
    }

    // var, mvar, fnptr
    return FALSE;
}
#endif // #ifndef DACCESS_COMPILE



TypeHandle TypeDesc::GetParent() {

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    CorElementType kind = GetInternalCorElementType();

    if (CorTypeInfo::IsPrimitiveType_NoThrow(kind))
        return (MethodTable*)g_pObjectClass;
    return TypeHandle();
}

#ifndef DACCESS_COMPILE

OBJECTREF ParamTypeDesc::GetManagedClassObject()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        INJECT_FAULT(COMPlusThrowOM());

        PRECONDITION(GetInternalCorElementType() == ELEMENT_TYPE_ARRAY ||
                     GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY ||
                     GetInternalCorElementType() == ELEMENT_TYPE_BYREF ||
                     GetInternalCorElementType() == ELEMENT_TYPE_PTR);
    }
    CONTRACTL_END;

    if (m_hExposedClassObject == NULL)
    {
        TypeHandle(this).AllocateManagedClassObject(&m_hExposedClassObject);
    }
    return GetManagedClassObjectIfExists();
}

#endif // #ifndef DACCESS_COMPILE

BOOL TypeDesc::IsRestored()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SUPPORTS_DAC;

    return IsRestored_NoLogging();
}

BOOL TypeDesc::IsRestored_NoLogging()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SUPPORTS_DAC;

    return (m_typeAndFlags & TypeDesc::enum_flag_Unrestored) == 0;
}

ClassLoadLevel TypeDesc::GetLoadLevel()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    if (m_typeAndFlags & TypeDesc::enum_flag_UnrestoredTypeKey)
    {
        return CLASS_LOAD_UNRESTOREDTYPEKEY;
    }
    else if (m_typeAndFlags & TypeDesc::enum_flag_Unrestored)
    {
        return CLASS_LOAD_UNRESTORED;
    }
    else if (m_typeAndFlags & TypeDesc::enum_flag_IsNotFullyLoaded)
    {
        if (m_typeAndFlags & TypeDesc::enum_flag_DependenciesLoaded)
        {
            return CLASS_DEPENDENCIES_LOADED;
        }
        else
        {
            return CLASS_LOAD_EXACTPARENTS;
        }
    }

    return CLASS_LOADED;
}


// Recursive worker that pumps the transitive closure of a type's dependencies to the specified target level.
// Dependencies include:
//
//   - parent
//   - interfaces
//   - canonical type, for non-canonical instantiations
//   - typical type, for non-typical instantiations
//
// Parameters:
//
//   pVisited - used to prevent endless recursion in the case of cyclic dependencies
//
//   level    - target level to pump to - must be CLASS_DEPENDENCIES_LOADED or CLASS_LOADED
//
//              if CLASS_DEPENDENCIES_LOADED, all transitive dependencies are resolved to their
//                 exact types.
//
//              if CLASS_LOADED, all type-safety checks are done on the type and all its transitive
//                 dependencies. Note that for the CLASS_LOADED case, some types may be left
//                 on the pending list rather that pushed to CLASS_LOADED in the case of cyclic
//                 dependencies - the root caller must handle this.
//
//
//   pfBailed - if we or one of our dependencies bails early due to cyclic dependencies, we
//              must set *pfBailed to TRUE. Otherwise, we must *leave it unchanged* (thus, the
//              boolean acts as a cumulative OR.)
//
//   pPending - if one of our dependencies bailed, the type cannot yet be promoted to CLASS_LOADED
//              as the dependencies will be checked later and may fail a security check then.
//              Instead, DoFullyLoad() will add the type to the pending list - the root caller
//              is responsible for promoting the type after the full transitive closure has been
//              walked. Note that it would be just as correct to always defer to the pending list -
//              however, that is a little less performant.
//
//  pInstContext - instantiation context created in code:SigPointer.GetTypeHandleThrowing and ultimately
//                 passed down to code:TypeVarTypeDesc.SatisfiesConstraints.
//
void TypeDesc::DoFullyLoad(Generics::RecursionGraph *pVisited, ClassLoadLevel level,
                           DFLPendingList *pPending, BOOL *pfBailed, const InstantiationContext *pInstContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    _ASSERTE(level == CLASS_LOADED || level == CLASS_DEPENDENCIES_LOADED);
    _ASSERTE(pfBailed != NULL);
    _ASSERTE(!(level == CLASS_LOADED && pPending == NULL));


#ifndef DACCESS_COMPILE

    if (Generics::RecursionGraph::HasSeenType(pVisited, TypeHandle(this)))
    {
        *pfBailed = TRUE;
        return;
    }

    if (GetLoadLevel() >= level)
    {
        return;
    }

    if (level == CLASS_LOADED)
    {
        UINT numTH = pPending->Count();
        TypeHandle *pTypeHndPending = pPending->Table();
        for (UINT idxPending = 0; idxPending < numTH; idxPending++)
        {
            if (pTypeHndPending[idxPending].IsTypeDesc() && pTypeHndPending[idxPending].AsTypeDesc() == this)
            {
                *pfBailed = TRUE;
                return;
            }
        }

    }


    BOOL fBailed = FALSE;

    // First ensure that we're loaded to just below CLASS_LOADED
    ClassLoader::EnsureLoaded(TypeHandle(this), (ClassLoadLevel) (level-1));

    if (HasTypeParam())
    {
        Generics::RecursionGraph newVisited(pVisited, TypeHandle(this));

        // Fully load the type parameter
        GetTypeParam().DoFullyLoad(&newVisited, level, pPending, &fBailed, pInstContext);
    }

    switch (level)
    {
        case CLASS_DEPENDENCIES_LOADED:
            InterlockedOr((LONG*)&m_typeAndFlags, TypeDesc::enum_flag_DependenciesLoaded);
            break;

        case CLASS_LOADED:
            if (fBailed)
            {
                // We couldn't complete security checks on some dependency because it is already being processed by one of our callers.
                // Do not mark this class fully loaded yet. Put it on the pending list and it will be marked fully loaded when
                // everything unwinds.

                *pfBailed = TRUE;

                TypeHandle* pthPending = pPending->AppendThrowing();
                *pthPending = TypeHandle(this);
            }
            else
            {
                // Finally, mark this method table as fully loaded
                SetIsFullyLoaded();
            }
            break;

        default:
            _ASSERTE(!"Can't get here.");
            break;
    }
#endif
}

#ifndef DACCESS_COMPILE

MethodDesc * TypeVarTypeDesc::LoadOwnerMethod()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(TypeFromToken(m_typeOrMethodDef) == mdtMethodDef);
    }
    CONTRACTL_END;

    MethodDesc *pMD = GetModule()->LookupMethodDef(m_typeOrMethodDef);
    if (pMD == NULL)
    {
        pMD = MemberLoader::GetMethodDescFromMethodDef(GetModule(), m_typeOrMethodDef, FALSE);
    }
    return pMD;
}

TypeHandle TypeVarTypeDesc::LoadOwnerType()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(TypeFromToken(m_typeOrMethodDef) == mdtTypeDef);
    }
    CONTRACTL_END;

    TypeHandle genericType = GetModule()->LookupTypeDef(m_typeOrMethodDef);
    if (genericType.IsNull())
    {
        genericType = ClassLoader::LoadTypeDefThrowing(GetModule(), m_typeOrMethodDef,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::PermitUninstDefOrRef);
    }
    return genericType;
}

TypeHandle* TypeVarTypeDesc::GetCachedConstraints(DWORD *pNumConstraints)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(CheckPointer(pNumConstraints));
    PRECONDITION(m_numConstraints != (DWORD) -1);

    *pNumConstraints = m_numConstraints;
    return m_constraints;
}




TypeHandle* TypeVarTypeDesc::GetConstraints(DWORD *pNumConstraints, ClassLoadLevel level /* = CLASS_LOADED */)
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(CheckPointer(pNumConstraints));
    PRECONDITION(level == CLASS_DEPENDENCIES_LOADED || level == CLASS_LOADED);

    if (m_numConstraints == (DWORD) -1)
        LoadConstraints(level);

    *pNumConstraints = m_numConstraints;
    return m_constraints;
}


void TypeVarTypeDesc::LoadConstraints(ClassLoadLevel level /* = CLASS_LOADED */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        INJECT_FAULT(COMPlusThrowOM());

        PRECONDITION(level == CLASS_DEPENDENCIES_LOADED || level == CLASS_LOADED);
    }
    CONTRACTL_END;

    _ASSERTE(((INT_PTR)&m_constraints) % sizeof(m_constraints) == 0);
    _ASSERTE(((INT_PTR)&m_numConstraints) % sizeof(m_numConstraints) == 0);

    DWORD numConstraints = m_numConstraints;

    if (numConstraints == (DWORD) -1)
    {
        IMDInternalImport* pInternalImport = GetModule()->GetMDImport();

        HENUMInternalHolder hEnum(pInternalImport);
        mdGenericParamConstraint tkConstraint;

        SigTypeContext typeContext;
        mdToken defToken = GetTypeOrMethodDef();

        MethodTable *pMT = NULL;
        if (TypeFromToken(defToken) == mdtMethodDef)
        {
            MethodDesc *pMD = LoadOwnerMethod();
            _ASSERTE(pMD->IsGenericMethodDefinition());

            SigTypeContext::InitTypeContext(pMD,&typeContext);

            _ASSERTE(!typeContext.m_methodInst.IsEmpty());
            pMT = pMD->GetMethodTable();
        }
        else
        {
            _ASSERTE(TypeFromToken(defToken) == mdtTypeDef);
            TypeHandle genericType = LoadOwnerType();
            _ASSERTE(genericType.IsGenericTypeDefinition());

            SigTypeContext::InitTypeContext(genericType,&typeContext);
        }

        hEnum.EnumInit(mdtGenericParamConstraint, GetToken());
        numConstraints = pInternalImport->EnumGetCount(&hEnum);
        if (numConstraints != 0)
        {
            LoaderAllocator* pAllocator = GetModule()->GetLoaderAllocator();
            // If there is a single class constraint we place it at index 0 of the array
            AllocMemHolder<TypeHandle> constraints
                (pAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(numConstraints) * S_SIZE_T(sizeof(TypeHandle))));

            DWORD i = 0;
            while (pInternalImport->EnumNext(&hEnum, &tkConstraint))
            {
                _ASSERTE(i <= numConstraints);
                mdToken tkConstraintType, tkParam;
                if (FAILED(pInternalImport->GetGenericParamConstraintProps(tkConstraint, &tkParam, &tkConstraintType)))
                {
                    GetModule()->GetAssembly()->ThrowTypeLoadException(pInternalImport, pMT->GetCl(), IDS_CLASSLOAD_BADFORMAT);
                }
                _ASSERTE(tkParam == GetToken());
                TypeHandle thConstraint = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(GetModule(), tkConstraintType,
                                                                                      &typeContext,
                                                                                      ClassLoader::ThrowIfNotFound,
                                                                                      ClassLoader::FailIfUninstDefOrRef,
                                                                                      ClassLoader::LoadTypes,
                                                                                      level);

                constraints[i++] = thConstraint;

                // Method type constraints behave contravariantly
                // (cf Bounded polymorphism e.g. see
                //     Cardelli & Wegner, On understanding types, data abstraction and polymorphism, Computing Surveys 17(4), Dec 1985)
                if (pMT != NULL && pMT->HasVariance() && TypeFromToken(tkConstraintType) == mdtTypeSpec)
                {
                    ULONG cSig;
                    PCCOR_SIGNATURE pSig;
                    if (FAILED(pInternalImport->GetTypeSpecFromToken(tkConstraintType, &pSig, &cSig)))
                    {
                        GetModule()->GetAssembly()->ThrowTypeLoadException(pInternalImport, pMT->GetCl(), IDS_CLASSLOAD_BADFORMAT);
                    }
                    if (!EEClass::CheckVarianceInSig(pMT->GetNumGenericArgs(),
                                                     pMT->GetClass()->GetVarianceInfo(),
                                                     pMT->GetModule(),
                                                     SigPointer(pSig, cSig),
                                                     gpContravariant))
                    {
                        GetModule()->GetAssembly()->ThrowTypeLoadException(pInternalImport, pMT->GetCl(), IDS_CLASSLOAD_VARIANCE_IN_CONSTRAINT);
                    }
                }
            }

            if (InterlockedCompareExchangeT(&m_constraints, constraints.operator->(), NULL) == NULL)
            {
                constraints.SuppressRelease();
            }
        }

        m_numConstraints = numConstraints;
    }

    for (DWORD i = 0; i < numConstraints; i++)
    {
        ClassLoader::EnsureLoaded(m_constraints[i], level);
    }
}

BOOL TypeVarTypeDesc::ConstrainedAsObjRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(ConstraintsLoaded());
    }
    CONTRACTL_END;

    IMDInternalImport* pInternalImport = GetModule()->GetMDImport();
    mdGenericParam genericParamToken = GetToken();
    DWORD flags;
    if (FAILED(pInternalImport->GetGenericParamProps(genericParamToken, NULL, &flags, NULL, NULL, NULL)))
    {
        return FALSE;
    }
    DWORD specialConstraints = flags & gpSpecialConstraintMask;

    if ((specialConstraints & gpReferenceTypeConstraint) != 0)
        return TRUE;

    return ConstrainedAsObjRefHelper();
}

// A recursive helper that helps determine whether this variable is constrained as ObjRef.
// Please note that we do not check the gpReferenceTypeConstraint special constraint here
// because this property does not propagate up the constraining hierarchy.
// (e.g. "class A<S, T> where S : T, where T : class" does not guarantee that S is ObjRef)
BOOL TypeVarTypeDesc::ConstrainedAsObjRefHelper()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD dwNumConstraints = 0;
    TypeHandle* constraints = GetCachedConstraints(&dwNumConstraints);

    for (DWORD i = 0; i < dwNumConstraints; i++)
    {
        TypeHandle constraint = constraints[i];

        if (constraint.IsGenericVariable() && constraint.AsGenericVariable()->ConstrainedAsObjRefHelper())
            return TRUE;

        if (!constraint.IsInterface() && CorTypeInfo::IsObjRef_NoThrow(constraint.GetInternalCorElementType()))
        {
            // Object, ValueType, and Enum are ObjRefs but they do not constrain the var to ObjRef!
            MethodTable *mt = constraint.GetMethodTable();

            if (mt != g_pObjectClass &&
                mt != g_pValueTypeClass &&
                mt != g_pEnumClass)
            {
                return TRUE;
            }
        }
    }

    return FALSE;
}

BOOL TypeVarTypeDesc::ConstrainedAsValueType()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(ConstraintsLoaded());
    }
    CONTRACTL_END;

    IMDInternalImport* pInternalImport = GetModule()->GetMDImport();
    mdGenericParam genericParamToken = GetToken();
    DWORD flags;
    if (FAILED(pInternalImport->GetGenericParamProps(genericParamToken, NULL, &flags, NULL, NULL, NULL)))
    {
        return FALSE;
    }
    DWORD specialConstraints = flags & gpSpecialConstraintMask;

    if ((specialConstraints & gpNotNullableValueTypeConstraint) != 0)
        return TRUE;

    DWORD dwNumConstraints = 0;
    TypeHandle* constraints = GetCachedConstraints(&dwNumConstraints);

    for (DWORD i = 0; i < dwNumConstraints; i++)
    {
        TypeHandle constraint = constraints[i];

        if (constraint.IsGenericVariable())
        {
            if (constraint.AsGenericVariable()->ConstrainedAsValueType())
                return TRUE;
        }
        else
        {
            // the following condition will also disqualify interfaces
            if (!CorTypeInfo::IsObjRef_NoThrow(constraint.GetInternalCorElementType()))
                return TRUE;
        }
    }

    return FALSE;
}

//---------------------------------------------------------------------------------------------------------------------
// Loads the type of a constraint given the constraint token and instantiation context. If pInstContext is
// not NULL and the constraint's type is a typespec, pInstContext will be used to instantiate the typespec.
// Otherwise typical instantiation is returned if the constraint type is generic.
//---------------------------------------------------------------------------------------------------------------------
static
TypeHandle LoadTypeVarConstraint(TypeVarTypeDesc *pTypeVar, mdGenericParamConstraint tkConstraint,
                                 const InstantiationContext *pInstContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
        PRECONDITION(CheckPointer(pTypeVar));
    }
    CONTRACTL_END;

    Module *pTyModule = pTypeVar->GetModule();
    IMDInternalImport* pInternalImport = pTyModule->GetMDImport();

    mdToken tkConstraintType, tkParam;
    IfFailThrow(pInternalImport->GetGenericParamConstraintProps(tkConstraint, &tkParam, &tkConstraintType));
    _ASSERTE(tkParam == pTypeVar->GetToken());
    mdToken tkOwnerToken = pTypeVar->GetTypeOrMethodDef();

    if (TypeFromToken(tkConstraintType) == mdtTypeSpec && pInstContext != NULL)
    {
        if(pInstContext->m_pSubstChain == NULL)
        {
            // The substitution chain will be null in situations
            // where we are instantiating types that are open, and therefore
            // we should be using the fully open TypeVar constraint instantiation code
            // below. However, in the case of a open method on a closed generic class
            // we will also have a null substitution chain. In this case, if we can ensure
            // that the instantiation type parameters are non type-var types, it is valid
            // to use the passed in instantiation when instantiating the type var constraint.
            BOOL fContextContainsValidGenericTypeParams = FALSE;

            if (TypeFromToken(tkOwnerToken) == mdtMethodDef)
            {
                SigTypeContext sigTypeContext;

                MethodDesc *pMD = pTypeVar->LoadOwnerMethod();

                SigTypeContext::InitTypeContext(pMD, &sigTypeContext);
                fContextContainsValidGenericTypeParams = SigTypeContext::IsValidTypeOnlyInstantiationOf(&sigTypeContext, pInstContext->m_pArgContext);
            }

            if (!fContextContainsValidGenericTypeParams)
                goto LoadConstraintOnOpenType;
        }

        // obtain the constraint type's signature if it's a typespec
        ULONG cbSig;
        PCCOR_SIGNATURE ptr;

        IfFailThrow(pInternalImport->GetSigFromToken(tkConstraintType, &cbSig, &ptr));

        SigPointer pSig(ptr, cbSig);

        // instantiate the signature using the current InstantiationContext
        return pSig.GetTypeHandleThrowing(pTyModule,
                                          pInstContext->m_pArgContext,
                                          ClassLoader::LoadTypes, CLASS_DEPENDENCIES_LOADED, FALSE,
                                          pInstContext->m_pSubstChain);
    }
    else
    {
LoadConstraintOnOpenType:

        SigTypeContext sigTypeContext;

        switch (TypeFromToken(tkOwnerToken))
        {
            case mdtTypeDef:
            {
                // the type variable is declared by a type - load the handle of the type
                TypeHandle thOwner = pTyModule->GetClassLoader()->LoadTypeDefThrowing(pTyModule,
                                                                                      tkOwnerToken,
                                                                                      ClassLoader::ThrowIfNotFound,
                                                                                      ClassLoader::PermitUninstDefOrRef,
                                                                                      tdNoTypes,
                                                                                      CLASS_LOAD_APPROXPARENTS
                                                                                     );

                SigTypeContext::InitTypeContext(thOwner, &sigTypeContext);
                break;
            }

            case mdtMethodDef:
            {
                // the type variable is declared by a method - load its method desc
                MethodDesc *pMD = pTyModule->LookupMethodDef(tkOwnerToken);

                SigTypeContext::InitTypeContext(pMD, &sigTypeContext);
                break;
            }

            default:
            {
                COMPlusThrow(kBadImageFormatException);
            }
        }

        // load the (typical instantiation of) constraint type
        return ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pTyModule,
                                                           tkConstraintType,
                                                           &sigTypeContext,
                                                           ClassLoader::ThrowIfNotFound,
                                                           ClassLoader::FailIfUninstDefOrRef,
                                                           ClassLoader::LoadTypes,
                                                           CLASS_DEPENDENCIES_LOADED);
    }
}

//---------------------------------------------------------------------------------------------------------------------
// We come here only if a type parameter with a special constraint is instantiated by an argument that is itself
// a type parameter. In this case, we'll need to examine *its* constraints to see if the range of types that would satisfy its
// constraints is a subset of the range of types that would satisfy the special constraint.
//
// This routine will return TRUE if it can prove that argument "pTyArg" has a constraint that will satisfy the special constraint.
//
// (NOTE: It does not check against anything other than one specific specialConstraint (it doesn't even know what they are.) This is
// just one step in the checking of constraints.)
//---------------------------------------------------------------------------------------------------------------------
static
BOOL SatisfiesSpecialConstraintRecursive(TypeVarTypeDesc *pTyArg, DWORD specialConstraint, TypeHandleList *pVisitedVars = NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
        PRECONDITION(CheckPointer(pTyArg));
    }
    CONTRACTL_END;

    // The caller must invoke for all special constraints that apply - this fcn can only reliably test against one
    // constraint at a time.
    _ASSERTE(specialConstraint == gpNotNullableValueTypeConstraint
          || specialConstraint == gpReferenceTypeConstraint
          || specialConstraint == gpDefaultConstructorConstraint);

    IMDInternalImport* pInternalImport = pTyArg->GetModule()->GetMDImport();

    // Get the argument type's own special constraints
    DWORD argFlags;
    IfFailThrow(pTyArg->GetModule()->GetMDImport()->GetGenericParamProps(pTyArg->GetToken(), NULL, &argFlags, NULL, NULL, NULL));

    DWORD argSpecialConstraints = argFlags & gpSpecialConstraintMask;

    // First, if the argument's own special constraints match the parameter's special constraints,
    // we can safely conclude the constraint is satisfied.
    switch (specialConstraint)
    {
        case gpNotNullableValueTypeConstraint:
        {
            if ((argSpecialConstraints & gpNotNullableValueTypeConstraint) != 0)
            {
                return TRUE;
            }
            break;
        }

        case gpReferenceTypeConstraint:
        {
            // gpReferenceTypeConstraint is not "inherited" so ignore it if pTyArg is a variable
            // constraining the argument rather than the argument itself.

            if (pVisitedVars == NULL && (argSpecialConstraints & gpReferenceTypeConstraint) != 0)
            {
                return TRUE;
            }
            break;
        }

        case gpDefaultConstructorConstraint:
        {
            // gpDefaultConstructorConstraint is not "inherited" so ignore it if pTyArg is a variable
            // constraining the argument rather than the argument itself.

            if ((pVisitedVars == NULL && (argSpecialConstraints & gpDefaultConstructorConstraint) != 0) ||
                (argSpecialConstraints & gpNotNullableValueTypeConstraint) != 0)
            {
                return TRUE;
            }
            break;
        }
    }

    // The special constraints did not match. However, we may find a primary type constraint
    // that would always satisfy the special constraint.
    HENUMInternalHolder hEnum(pInternalImport);
    hEnum.EnumInit(mdtGenericParamConstraint, pTyArg->GetToken());

    mdGenericParamConstraint tkConstraint;
    while (pInternalImport->EnumNext(&hEnum, &tkConstraint))
    {
        // We pass NULL instantiation context here because when checking for special constraints, it makes
        // no difference whether we load a typical (e.g. A<T>) or concrete (e.g. A<string>) instantiation.
        TypeHandle thConstraint = LoadTypeVarConstraint(pTyArg, tkConstraint, NULL);

        if (thConstraint.IsGenericVariable())
        {
            // The variable is constrained by another variable, which we need to check recursively. An
            // example of why this is necessary follows:
            //
            // class A<T> where T : class
            // { }
            // class B<S, R> : A<S> where S : R where R : EventArgs
            // { }
            //
            if (!TypeHandleList::Exists(pVisitedVars, thConstraint))
            {
                TypeHandleList newVisitedVars(thConstraint, pVisitedVars);
                if (SatisfiesSpecialConstraintRecursive(thConstraint.AsGenericVariable(),
                                                        specialConstraint,
                                                        &newVisitedVars))
                {
                    return TRUE;
                }
            }
        }
        else if (thConstraint.IsInterface())
        {
            // This is a secondary constraint - this tells us nothing about the eventual instantiation that
            // we can use here.
        }
        else
        {
            // This is a type constraint. Remember that the eventual instantiation is only guaranteed to be
            // something *derived* from this type, not the actual type itself. To emphasize, we rename the local.

            TypeHandle thAncestorOfType = thConstraint;

            if (specialConstraint == gpNotNullableValueTypeConstraint)
            {
                if (thAncestorOfType.IsValueType() && !(thAncestorOfType.AsMethodTable()->IsNullable()))
                {
                    return TRUE;
                }
            }

            if (specialConstraint == gpReferenceTypeConstraint)
            {

                if (!thAncestorOfType.IsTypeDesc())
                {
                    MethodTable *pAncestorMT = thAncestorOfType.AsMethodTable();

                    if ((!(pAncestorMT->IsValueType())) && pAncestorMT != g_pObjectClass && pAncestorMT != g_pValueTypeClass)
                    {
                        // ValueTypes are sealed except when they aren't (cough, cough, System.Enum...). Sigh.
                        // Don't put all our trust in IsValueType() here - check the ancestry chain as well.
                        BOOL fIsValueTypeAnAncestor = FALSE;
                        MethodTable *pParentMT = pAncestorMT->GetParentMethodTable();
                        while (pParentMT)
                        {
                            if (pParentMT == g_pValueTypeClass)
                            {
                                fIsValueTypeAnAncestor = TRUE;
                                break;
                            }
                            pParentMT = pParentMT->GetParentMethodTable();
                        }

                        if (!fIsValueTypeAnAncestor)
                        {
                            return TRUE;
                        }
                    }
                }
            }

            if (specialConstraint == gpDefaultConstructorConstraint)
            {
                // If a valuetype, just check to ensure that doesn't have a private default ctor.
                // If not a valuetype, not much we can conclude knowing just an ancestor class.
                if (thAncestorOfType.IsValueType() && thAncestorOfType.GetMethodTable()->HasExplicitOrImplicitPublicDefaultConstructor())
                {
                    return TRUE;
                }
            }

        }
    }

    // If we got here, we found no evidence that the argument's constraints are strict enough to satisfy the parameter's constraints.
    return FALSE;
}

//---------------------------------------------------------------------------------------------------------------------
// Walks the "constraining chain" of a type variable and appends all concrete constraints as well as type vars
// to the provided ArrayList. Upon leaving the function, the list contains all types that the type variable is
// known to be assignable to.
//
// E.g.
// class A<S, T> where S : T, IComparable where T : EventArgs
// {
//     void f<U>(U u) where U : S, IDisposable { }
// }
// This would put 5 types to the U's list: S, T, IDisposable, IComparable, and EventArgs.
//---------------------------------------------------------------------------------------------------------------------
static
void GatherConstraintsRecursive(TypeVarTypeDesc *pTyArg, ArrayList *pArgList, const InstantiationContext *pInstContext,
                                TypeHandleList *pVisitedVars = NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
        PRECONDITION(CheckPointer(pTyArg));
        PRECONDITION(CheckPointer(pArgList));
    }
    CONTRACTL_END;

    IMDInternalImport* pInternalImport = pTyArg->GetModule()->GetMDImport();

    // enumerate constraints of the pTyArg
    HENUMInternalHolder hEnum(pInternalImport);
    hEnum.EnumInit(mdtGenericParamConstraint, pTyArg->GetToken());

    mdGenericParamConstraint tkConstraint;
    while (pInternalImport->EnumNext(&hEnum, &tkConstraint))
    {
        TypeHandle thConstraint = LoadTypeVarConstraint(pTyArg, tkConstraint, pInstContext);

        if (thConstraint.IsGenericVariable())
        {
            // see if it's safe to recursively call ourselves
            if (!TypeHandleList::Exists(pVisitedVars, thConstraint))
            {
                pArgList->Append(thConstraint.AsPtr());

                TypeHandleList newVisitedVars(thConstraint, pVisitedVars);
                GatherConstraintsRecursive(thConstraint.AsGenericVariable(), pArgList, pInstContext, &newVisitedVars);
            }

            // Note: circular type parameter constraints will be detected and reported later in
            // MethodTable::DoFullyLoad, we just have to avoid SO here.
        }
        else
        {
            pArgList->Append(thConstraint.AsPtr());
        }
    }
}

// pTypeContextOfConstraintDeclarer = type context of the generic type that declares the constraint
//                                    This is needed to load the "X" type when the constraint is the frm
//                                    "where T:X".
//                                    Caution: Do NOT use it to load types or constraints attached to "thArg".
//
// thArg                            = typehandle of the type being substituted for the type parameter.
//
// pInstContext                     = the instantiation context (type context + substitution chain) to be
//                                    used when loading constraints attached to "thArg".
//
BOOL TypeVarTypeDesc::SatisfiesConstraints(SigTypeContext *pTypeContextOfConstraintDeclarer, TypeHandle thArg,
                                           const InstantiationContext *pInstContext/*=NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(!thArg.IsNull());
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // During EEStartup, we cannot safely validate constraints, but we can also be confident that the code doesn't violate them
    // Just skip validation and declare that the constraints are satisfied.
    if (g_fEEInit)
        return TRUE;

    IMDInternalImport* pInternalImport = GetModule()->GetMDImport();
    mdGenericParamConstraint tkConstraint;

    INDEBUG(mdToken defToken = GetTypeOrMethodDef());
    _ASSERTE(TypeFromToken(defToken) == mdtMethodDef || TypeFromToken(defToken) == mdtTypeDef);

    // prepare for the enumeration of this variable's general constraints
    mdGenericParam genericParamToken = GetToken();

    HENUMInternalHolder hEnum(pInternalImport);
    hEnum.EnumInit(mdtGenericParamConstraint, genericParamToken);

    ArrayList argList;

    // First check special constraints
    DWORD flags;
    IfFailThrow(pInternalImport->GetGenericParamProps(genericParamToken, NULL, &flags, NULL, NULL, NULL));

    DWORD specialConstraints = flags & gpSpecialConstraintMask;

    if (thArg.IsGenericVariable())
    {
        TypeVarTypeDesc *pTyVar = thArg.AsGenericVariable();

        if ((specialConstraints & gpNotNullableValueTypeConstraint) != 0)
        {
            if (!SatisfiesSpecialConstraintRecursive(pTyVar, gpNotNullableValueTypeConstraint))
            {
                return FALSE;
            }
        }

        if ((specialConstraints & gpReferenceTypeConstraint) != 0)
        {
            if (!SatisfiesSpecialConstraintRecursive(pTyVar, gpReferenceTypeConstraint))
            {
                return FALSE;
            }
        }

        if ((specialConstraints & gpDefaultConstructorConstraint) != 0)
        {
            if (!SatisfiesSpecialConstraintRecursive(pTyVar, gpDefaultConstructorConstraint))
            {
                return FALSE;
            }
        }

        if (hEnum.EnumGetCount() == 0)
        {
            // return immediately if there are no general constraints to satisfy (fast path)
            return TRUE;
        }

        // Now walk the "constraining chain" of type variables and gather all constraint types.
        //
        // This work should not be left to code:TypeHandle.CanCastTo because we need typespec constraints
        // to be instantiated in pInstContext. If we just do thArg.CanCastTo(thConstraint), it would load
        // typical instantiations of the constraints and the can-cast-to check may fail. In addition,
        // code:TypeHandle.CanCastTo will SO if the constraints are circular.
        //
        // Consider:
        //
        // class A<T>
        // {
        //     void f<U>(B<U, T> b) where U : A<T> { }
        // }
        // class B<S, R> where S : A<R> { }
        //
        // If we load the signature of, say, A<int>.f<U> (concrete class but typical method), and end up
        // here verifying that S : A<R> is satisfied by U : A<T>, we must instantiate the constraint type
        // A<T> using pInstContext so that it becomes A<int>. Otherwise the constraint check fails.
        //
        GatherConstraintsRecursive(pTyVar, &argList, pInstContext);
    }
    else
    {
        if ((specialConstraints & gpNotNullableValueTypeConstraint) != 0)
        {
            if (!thArg.IsValueType())
                return FALSE;
            else
            {
                // the type argument is a value type, however if it is any kind of Nullable we want to fail
                // as the constraint accepts any value type except Nullable types (Nullable itself is a value type)
                if (thArg.AsMethodTable()->IsNullable())
                    return FALSE;
            }
        }

        if ((specialConstraints & gpReferenceTypeConstraint) != 0)
        {
            if (thArg.IsValueType())
                return FALSE;
        }

        if ((specialConstraints & gpDefaultConstructorConstraint) != 0)
        {
            if (thArg.IsTypeDesc() || (!thArg.AsMethodTable()->HasExplicitOrImplicitPublicDefaultConstructor()))
                return FALSE;
        }

        if (thArg.IsByRefLike() && (specialConstraints & gpAcceptByRefLike) == 0)
            return FALSE;
    }

    // Complete the list by adding thArg itself. If thArg is not a generic variable this will be the only
    // item in the list. If it is a generic variable, we need it in the list as well in addition to all the
    // constraints gathered by GatherConstraintsRecursive, because e.g. class A<S, T> : where S : T
    // can be instantiated using A<U, U>.
    argList.Append(thArg.AsPtr());

    // At this point argList contains all types that thArg is known to be assignable to. The list may
    // contain duplicates and it consists of zero or more type variables, zero or more possibly generic
    // interfaces, and at most one possibly generic class.

    // Now check general subtype constraints
    while (pInternalImport->EnumNext(&hEnum, &tkConstraint))
    {
        mdToken tkConstraintType, tkParam;
        IfFailThrow(pInternalImport->GetGenericParamConstraintProps(tkConstraint, &tkParam, &tkConstraintType));

        _ASSERTE(tkParam == GetToken());
        TypeHandle thConstraint = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(GetModule(),
                                                                              tkConstraintType,
                                                                              pTypeContextOfConstraintDeclarer,
                                                                              ClassLoader::ThrowIfNotFound,
                                                                              ClassLoader::FailIfUninstDefOrRef,
                                                                              ClassLoader::LoadTypes,
                                                                              CLASS_DEPENDENCIES_LOADED);

        // System.Object constraint will be always satisfied - even if argList is empty
        if (!thConstraint.IsObjectType())
        {
            BOOL fCanCast = FALSE;

            // loop over all types that we know the arg will be assignable to
            ArrayList::Iterator iter = argList.Iterate();
            while (iter.Next())
            {
                TypeHandle thElem = TypeHandle::FromPtr(iter.GetElement());

                if (thElem.IsGenericVariable())
                {
                    // if a generic variable equals to the constraint, then this constraint will be satisfied
                    if (thElem == thConstraint)
                    {
                        fCanCast = TRUE;
                        break;
                    }

                    // and any variable with the gpNotNullableValueTypeConstraint special constraint
                    // satisfies the "derived from System.ValueType" general subtype constraint
                    if (thConstraint == g_pValueTypeClass)
                    {
                        TypeVarTypeDesc *pTyElem = thElem.AsGenericVariable();
                        IfFailThrow(pTyElem->GetModule()->GetMDImport()->GetGenericParamProps(
                            pTyElem->GetToken(),
                            NULL,
                            &flags,
                            NULL,
                            NULL,
                            NULL));

                        if ((flags & gpNotNullableValueTypeConstraint) != 0)
                        {
                            fCanCast = TRUE;
                            break;
                        }
                    }
                }
                else
                {
                    // if a concrete type can be cast to the constraint, then this constraint will be satisfied
                    if (thElem.CanCastTo(thConstraint))
                    {
                        // Static virtual methods need an extra check when an abstract type is used for instantiation
                        // to ensure that the implementation of the constraint is complete
                        //
                        // Do not apply this check when the generic argument is exactly a generic variable, as those
                        // do not hold the correct detail for checking, and do not need to do so. This constraint rule
                        // is only applicable for generic arguments which have been specialized to some extent
                        if (!thArg.IsGenericVariable() &&
                            !thElem.IsTypeDesc() &&
                            thElem.AsMethodTable()->IsAbstract() &&
                            thConstraint.IsInterface() &&
                            thConstraint.AsMethodTable()->HasVirtualStaticMethods())
                        {
                            MethodTable *pInterfaceMT = thConstraint.AsMethodTable();
                            bool virtualStaticResolutionCheckFailed = false;
                            for (MethodTable::MethodIterator it(pInterfaceMT); it.IsValid(); it.Next())
                            {
                                MethodDesc *pMD = it.GetMethodDesc();
                                if (pMD->IsVirtual() &&
                                    pMD->IsStatic() &&
                                    (pMD->IsAbstract() && !thElem.AsMethodTable()->ResolveVirtualStaticMethod(
                                        pInterfaceMT, pMD, /* allowNullResult */ TRUE, /* verifyImplemented */ TRUE)))
                                {
                                    virtualStaticResolutionCheckFailed = true;
                                    break;
                                }
                            }

                            if (virtualStaticResolutionCheckFailed)
                                continue;
                        }
                        fCanCast = TRUE;
                        break;
                    }
                }
            }

            if (!fCanCast)
                return FALSE;
        }
    }
    return TRUE;
}

OBJECTREF TypeVarTypeDesc::GetManagedClassObject()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        INJECT_FAULT(COMPlusThrowOM());

        PRECONDITION(IsGenericVariable());
    }
    CONTRACTL_END;

    if (m_hExposedClassObject == NULL)
    {
        TypeHandle(this).AllocateManagedClassObject(&m_hExposedClassObject);
    }
    return GetManagedClassObjectIfExists();
}

#endif //!DACCESS_COMPILE

TypeHandle *
FnPtrTypeDesc::GetRetAndArgTypes()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    return m_RetAndArgTypes;
} // FnPtrTypeDesc::GetRetAndArgTypes

#ifndef DACCESS_COMPILE

// Returns TRUE if all return and argument types are externally visible.
BOOL
FnPtrTypeDesc::IsExternallyVisible() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    const TypeHandle * rgRetAndArgTypes = GetRetAndArgTypes();
    for (DWORD i = 0; i <= m_NumArgs; i++)
    {
        if (!rgRetAndArgTypes[i].IsExternallyVisible())
        {
            return FALSE;
        }
    }
    // All return/arguments types are externally visible
    return TRUE;
} // FnPtrTypeDesc::IsExternallyVisible

#endif //DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
ParamTypeDesc::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    m_Arg.EnumMemoryRegions(flags);
}

void
TypeVarTypeDesc::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    PTR_TypeVarTypeDesc ptrThis(this);

    if (GetModule().IsValid())
    {
        GetModule()->EnumMemoryRegions(flags, true);
    }

    if (m_numConstraints != (DWORD)-1)
    {
        PTR_TypeHandle constraint = m_constraints;
        for (DWORD i = 0; i < m_numConstraints; i++)
        {
            if (constraint.IsValid())
            {
                constraint->EnumMemoryRegions(flags);
            }
            constraint++;
        }
    }
} // TypeVarTypeDesc::EnumMemoryRegions

void
FnPtrTypeDesc::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    for (DWORD i = 0; i < m_NumArgs; i++)
    {
        m_RetAndArgTypes[i].EnumMemoryRegions(flags);
    }
}

#endif //DACCESS_COMPILE
