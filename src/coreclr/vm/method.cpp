// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: Method.CPP
//

//
// See the book of the runtime entry for overall design:
// file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
//

#include "common.h"
#include "excep.h"
#include "dbginterface.h"
#include "ecall.h"
#include "eeconfig.h"
#include "mlinfo.h"
#include "dllimport.h"
#include "generics.h"
#include "genericdict.h"
#include "typedesc.h"
#include "typestring.h"
#include "virtualcallstub.h"
#include "jitinterface.h"
#include "runtimehandles.h"
#include "eventtrace.h"
#include "interoputil.h"
#include "prettyprintsig.h"
#include "formattype.h"
#include "fieldmarshaler.h"
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#endif

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4244)
#endif // _MSC_VER

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
GVAL_IMPL(DWORD, g_MiniMetaDataBuffMaxSize);
GVAL_IMPL(TADDR, g_MiniMetaDataBuffAddress);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

// forward decl
bool FixupSignatureContainingInternalTypes(
    DataImage *     image,
    PCCOR_SIGNATURE pSig,
    DWORD           cSig,
    bool checkOnly = false);

// Alias ComPlusCallMethodDesc to regular MethodDesc to simplify definition of the size table
#ifndef FEATURE_COMINTEROP
#define ComPlusCallMethodDesc MethodDesc
#endif

// Verify that the structure sizes of our MethodDescs support proper
// aligning for atomic stub replacement.
//
static_assert_no_msg((sizeof(MethodDescChunk)       & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(MethodDesc)            & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(FCallMethodDesc)       & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(NDirectMethodDesc)     & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(EEImplMethodDesc)      & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(ArrayMethodDesc)       & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(ComPlusCallMethodDesc) & MethodDesc::ALIGNMENT_MASK) == 0);
static_assert_no_msg((sizeof(DynamicMethodDesc)     & MethodDesc::ALIGNMENT_MASK) == 0);

#define METHOD_DESC_SIZES(adjustment)                                       \
    adjustment + sizeof(MethodDesc),                 /* mcIL            */  \
    adjustment + sizeof(FCallMethodDesc),            /* mcFCall         */  \
    adjustment + sizeof(NDirectMethodDesc),          /* mcNDirect       */  \
    adjustment + sizeof(EEImplMethodDesc),           /* mcEEImpl        */  \
    adjustment + sizeof(ArrayMethodDesc),            /* mcArray         */  \
    adjustment + sizeof(InstantiatedMethodDesc),     /* mcInstantiated  */  \
    adjustment + sizeof(ComPlusCallMethodDesc),      /* mcComInterOp    */  \
    adjustment + sizeof(DynamicMethodDesc)           /* mcDynamic       */

const BYTE MethodDesc::s_ClassificationSizeTable[] = {
    // This is the raw
    METHOD_DESC_SIZES(0),

    // This extended part of the table is used for faster MethodDesc size lookup.
    // We index using optional slot flags into it
    METHOD_DESC_SIZES(sizeof(NonVtableSlot)),
    METHOD_DESC_SIZES(sizeof(MethodImpl)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(MethodImpl)),

    METHOD_DESC_SIZES(sizeof(NativeCodeSlot)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(NativeCodeSlot)),
    METHOD_DESC_SIZES(sizeof(MethodImpl) + sizeof(NativeCodeSlot)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(MethodImpl) + sizeof(NativeCodeSlot)),

#ifdef FEATURE_COMINTEROP
    METHOD_DESC_SIZES(sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(MethodImpl) + sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(MethodImpl) + sizeof(ComPlusCallInfo)),

    METHOD_DESC_SIZES(sizeof(NativeCodeSlot) + sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(NativeCodeSlot) + sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(MethodImpl) + sizeof(NativeCodeSlot) + sizeof(ComPlusCallInfo)),
    METHOD_DESC_SIZES(sizeof(NonVtableSlot) + sizeof(MethodImpl) + sizeof(NativeCodeSlot) + sizeof(ComPlusCallInfo))
#endif
};

#ifndef FEATURE_COMINTEROP
#undef ComPlusCallMethodDesc
#endif

class ArgIteratorBaseForPInvoke : public ArgIteratorBase
{
protected:
    FORCEINLINE BOOL IsRegPassedStruct(MethodTable* pMT)
    {
        return pMT->GetNativeLayoutInfo()->IsNativeStructPassedInRegisters();
    }
};

class PInvokeArgIterator : public ArgIteratorTemplate<ArgIteratorBaseForPInvoke>
{
public:
    PInvokeArgIterator(MetaSig* pSig)
    {
        m_pSig = pSig;
    }
};


//*******************************************************************************
SIZE_T MethodDesc::SizeOf()
{
    LIMITED_METHOD_DAC_CONTRACT;

    SIZE_T size = s_ClassificationSizeTable[m_wFlags &
        (mdcClassification
        | mdcHasNonVtableSlot
        | mdcMethodImpl
#ifdef FEATURE_COMINTEROP
        | mdcHasComPlusCallInfo
#endif
        | mdcHasNativeCodeSlot)];

    return size;
}

/*********************************************************************/
#ifndef DACCESS_COMPILE
BOOL NDirectMethodDesc::HasDefaultDllImportSearchPathsAttribute()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(IsDefaultDllImportSearchPathsAttributeCached())
    {
        return (ndirect.m_wFlags  & kDefaultDllImportSearchPathsStatus) != 0;
    }

    BOOL attributeIsFound = GetDefaultDllImportSearchPathsAttributeValue(GetModule(),GetMemberDef(),&ndirect.m_DefaultDllImportSearchPathsAttributeValue);

    if(attributeIsFound )
    {
        InterlockedSetNDirectFlags(kDefaultDllImportSearchPathsIsCached | kDefaultDllImportSearchPathsStatus);
    }
    else
    {
        InterlockedSetNDirectFlags(kDefaultDllImportSearchPathsIsCached);
    }

    return (ndirect.m_wFlags  & kDefaultDllImportSearchPathsStatus) != 0;
}
#endif //!DACCESS_COMPILE

//*******************************************************************************
#ifndef DACCESS_COMPILE
VOID MethodDesc::EnsureActive()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    GetMethodTable()->EnsureInstanceActive();
    if (HasMethodInstantiation() && !IsGenericMethodDefinition())
    {
        Instantiation methodInst = GetMethodInstantiation();
        for (DWORD i = 0; i < methodInst.GetNumArgs(); ++i)
        {
            MethodTable * pMT = methodInst[i].GetMethodTable();
            if (pMT)
                pMT->EnsureInstanceActive();
        }
    }
}
#endif //!DACCESS_COMPILE

//*******************************************************************************
CHECK MethodDesc::CheckActivated()
{
    WRAPPER_NO_CONTRACT;
    CHECK(GetModule()->CheckActivated());
    CHECK_OK;
}

//*******************************************************************************
BaseDomain *MethodDesc::GetDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    return AppDomain::GetCurrentDomain();
}

#ifndef DACCESS_COMPILE

//*******************************************************************************
LoaderAllocator * MethodDesc::GetDomainSpecificLoaderAllocator()
{
    if (GetLoaderModule()->IsCollectible())
    {
        return GetLoaderAllocator();
    }
    else
    {
        return ::GetAppDomain()->GetLoaderAllocator();
    }

}

#endif //!DACCESS_COMPILE

//*******************************************************************************
LPCUTF8 MethodDesc::GetName(USHORT slot)
{
    // MethodDesc::GetDeclMethodDesc can throw.
    WRAPPER_NO_CONTRACT;
    MethodDesc *pDeclMD = GetDeclMethodDesc((UINT32)slot);
    CONSISTENCY_CHECK(IsInterface() || !pDeclMD->IsInterface());
    return pDeclMD->GetName();
}

//*******************************************************************************
LPCUTF8 MethodDesc::GetName()
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS; // MethodImpl::FindMethodDesc can throw.
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }CONTRACTL_END;

    g_IBCLogger.LogMethodDescAccess(this);

    if (IsArray())
    {
        // Array classes don't have metadata tokens
        return dac_cast<PTR_ArrayMethodDesc>(this)->GetMethodName();
    }
    else if (IsNoMetadata())
    {
        // LCG methods don't have metadata tokens
        return dac_cast<PTR_DynamicMethodDesc>(this)->GetMethodName();
    }
    else
    {
        // Get the metadata string name for this method
        LPCUTF8 result = NULL;

        if (FAILED(GetMDImport()->GetNameOfMethodDef(GetMemberDef(), &result)))
        {
            result = NULL;
        }

        return(result);
    }
}

#ifndef DACCESS_COMPILE
/*
 * Function to get a method's name, its namespace
 */
VOID MethodDesc::GetMethodInfoNoSig(SString &namespaceOrClassName, SString &methodName)
{
    static LPCWSTR pDynamicClassName = W("dynamicClass");

    // namespace
    if(IsDynamicMethod())
        namespaceOrClassName.Append(pDynamicClassName);
    else
        TypeString::AppendType(namespaceOrClassName, TypeHandle(GetMethodTable()));

    // name
    methodName.AppendUTF8(GetName());
}

/*
 * Function to get a method's name, its namespace and signature (legacy format)
 */
VOID MethodDesc::GetMethodInfo(SString &namespaceOrClassName, SString &methodName, SString &methodSignature)
{
    GetMethodInfoNoSig(namespaceOrClassName, methodName);

    // signature
    CQuickBytes qbOut;
    ULONG cSig = 0;
    PCCOR_SIGNATURE pSig;

    GetSig(&pSig, &cSig);
    PrettyPrintSigInternalLegacy(pSig, cSig, " ", &qbOut, GetMDImport());
    methodSignature.AppendUTF8((char *)qbOut.Ptr());
}

/*
 * Function to get a method's name, its namespace and signature (new format)
 */
VOID MethodDesc::GetMethodInfoWithNewSig(SString &namespaceOrClassName, SString &methodName, SString &methodSignature)
{
    GetMethodInfoNoSig(namespaceOrClassName, methodName);

    // signature
    CQuickBytes qbOut;
    ULONG cSig = 0;
    PCCOR_SIGNATURE pSig;

    GetSig(&pSig, &cSig);
    PrettyPrintSig(pSig, (DWORD)cSig, "", &qbOut, GetMDImport(), NULL);
    methodSignature.AppendUTF8((char *)qbOut.Ptr());
}

/*
 * Function to get a method's full name, something like
 * void [mscorlib]System.StubHelpers.BSTRMarshaler::ClearNative(native int)
 */
VOID MethodDesc::GetFullMethodInfo(SString& fullMethodSigName)
{
    SString namespaceOrClassName, methodName;
    GetMethodInfoNoSig(namespaceOrClassName, methodName);

    // signature
    CQuickBytes qbOut;
    ULONG cSig = 0;
    PCCOR_SIGNATURE pSig;

    SString methodFullName;
    StackScratchBuffer namespaceNameBuffer, methodNameBuffer;
    methodFullName.AppendPrintf(
        (LPCUTF8)"[%s] %s::%s",
        GetModule()->GetAssembly()->GetSimpleName(),
        namespaceOrClassName.GetUTF8(namespaceNameBuffer),
        methodName.GetUTF8(methodNameBuffer));

    GetSig(&pSig, &cSig);

    StackScratchBuffer buffer;
    PrettyPrintSig(pSig, (DWORD)cSig, methodFullName.GetUTF8(buffer), &qbOut, GetMDImport(), NULL);
    fullMethodSigName.AppendUTF8((char *)qbOut.Ptr());
}

#endif

//*******************************************************************************
BOOL MethodDesc::MightHaveName(ULONG nameHashValue)
{
    LIMITED_METHOD_CONTRACT;

    // We only have space for a name hash when we are using the packed slot layout
    if (RequiresFullSlotNumber())
    {
        return TRUE;
    }

    WORD thisHashValue = m_wSlotNumber & enum_packedSlotLayout_NameHashMask;

    // A zero value might mean no hash has ever been set
    // (checking this way is better than dedicating a bit to tell us)
    if (thisHashValue == 0)
    {
        return TRUE;
    }

    WORD testHashValue = (WORD) nameHashValue & enum_packedSlotLayout_NameHashMask;

    return (thisHashValue == testHashValue);
}

//*******************************************************************************
void MethodDesc::GetSig(PCCOR_SIGNATURE *ppSig, DWORD *pcSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (HasStoredSig())
    {
        PTR_StoredSigMethodDesc pSMD = dac_cast<PTR_StoredSigMethodDesc>(this);
        if (pSMD->HasStoredMethodSig() || GetClassification()==mcDynamic)
        {
            *ppSig = pSMD->GetStoredMethodSig(pcSig);
            PREFIX_ASSUME(*ppSig != NULL);

            return;
        }
    }

    GetSigFromMetadata(GetMDImport(), ppSig, pcSig);
    PREFIX_ASSUME(*ppSig != NULL);
}

//*******************************************************************************
// get a function signature from its metadata
// Arguments:
//    input:
//        importer   the metatdata importer to be used
//    output:
//        ppSig      the function signature
//        pcSig      number of elements in the signature


void MethodDesc::GetSigFromMetadata(IMDInternalImport * importer,
                                    PCCOR_SIGNATURE   * ppSig,
                                    DWORD             * pcSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (FAILED(importer->GetSigOfMethodDef(GetMemberDef(), pcSig, ppSig)))
    {   // Class loader already asked for signature, so this should always succeed (unless there's a
        // bug or a new code path)
        _ASSERTE(!"If this ever fires, then this method should return HRESULT");
        *ppSig = NULL;
        *pcSig = 0;
    }
}

//*******************************************************************************
PCCOR_SIGNATURE MethodDesc::GetSig()
{
    WRAPPER_NO_CONTRACT;

    PCCOR_SIGNATURE pSig;
    DWORD           cSig;

    GetSig(&pSig, &cSig);

    PREFIX_ASSUME(pSig != NULL);

    return pSig;
}

Signature MethodDesc::GetSignature()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    PCCOR_SIGNATURE pSig;
    DWORD           cSig;

    GetSig(&pSig, &cSig);

    PREFIX_ASSUME(pSig != NULL);

    return Signature(pSig, cSig);
}

PCODE MethodDesc::GetMethodEntryPoint()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Similarly to SetMethodEntryPoint(), it is up to the caller to ensure that calls to this function are appropriately
    // synchronized

    // Keep implementations of MethodDesc::GetMethodEntryPoint and MethodDesc::GetAddrOfSlot in sync!

    g_IBCLogger.LogMethodDescAccess(this);

    if (HasNonVtableSlot())
    {
        SIZE_T size = GetBaseSize();

        TADDR pSlot = dac_cast<TADDR>(this) + size;

        return *PTR_PCODE(pSlot);
    }

    _ASSERTE(GetMethodTable()->IsCanonicalMethodTable());
    return GetMethodTable_NoLogging()->GetSlot(GetSlot());
}

PTR_PCODE MethodDesc::GetAddrOfSlot()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Keep implementations of MethodDesc::GetMethodEntryPoint and MethodDesc::GetAddrOfSlot in sync!

    if (HasNonVtableSlot())
    {
        SIZE_T size = GetBaseSize();

        return PTR_PCODE(dac_cast<TADDR>(this) + size);
    }

    _ASSERTE(GetMethodTable()->IsCanonicalMethodTable());
    return GetMethodTable()->GetSlotPtr(GetSlot());
}

//*******************************************************************************
PTR_MethodDesc MethodDesc::GetDeclMethodDesc(UINT32 slotNumber)
{
    CONTRACTL {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        INSTANCE_CHECK;
    } CONTRACTL_END;

    MethodDesc *pMDResult = this;

    // If the MethodDesc is not itself a methodImpl, but it is not in its native
    // slot, then someone (perhaps itself) must have overridden a methodImpl
    // in a parent, which causes the method to get put into all of the methodImpl
    // slots. So, the MethodDesc is implicitly a methodImpl without containing
    // the data. To find the real methodImpl MethodDesc, climb the inheritance
    // hierarchy checking the native slot on the way.
    if ((UINT32)pMDResult->GetSlot() != slotNumber)
    {
        while (!pMDResult->IsMethodImpl())
        {
            CONSISTENCY_CHECK(CheckPointer(pMDResult->GetMethodTable()->GetParentMethodTable()));
            CONSISTENCY_CHECK(slotNumber < pMDResult->GetMethodTable()->GetParentMethodTable()->GetNumVirtuals());
            pMDResult = pMDResult->GetMethodTable()->GetParentMethodTable()->GetMethodDescForSlot(slotNumber);
        }

        {
            CONSISTENCY_CHECK(pMDResult->IsMethodImpl());
            MethodImpl *pImpl = pMDResult->GetMethodImpl();
            pMDResult = pImpl->FindMethodDesc(slotNumber, PTR_MethodDesc(pMDResult));
        }

        // It is possible that a methodImpl'd slot got copied into another slot because
        // of slot unification, for example:
        //      C1::A is methodImpled with C2::B
        //      C1::B is methodImpled with C2::C
        // this means that through slot unification that A is tied to B and B is tied to C,
        // so A is tied to C even though C does not have a methodImpl entry specifically
        // relating to that slot. In this case, we recurse to the parent type and ask the
        // same question again.
        if (pMDResult->GetSlot() != slotNumber)
        {
            MethodTable * pMTOfMD = pMDResult->GetMethodTable();
            CONSISTENCY_CHECK(slotNumber < pMTOfMD->GetParentMethodTable()->GetNumVirtuals());
            pMDResult = pMTOfMD->GetParentMethodTable()->GetMethodDescForSlot(slotNumber);
            pMDResult = pMDResult->GetDeclMethodDesc(slotNumber);
        }
    }

    CONSISTENCY_CHECK(CheckPointer(pMDResult));
    CONSISTENCY_CHECK((UINT32)pMDResult->GetSlot() == slotNumber);
    return PTR_MethodDesc(pMDResult);
}

//*******************************************************************************
// Returns a hash for the method.
// The hash will be the same for the method across multiple process runs.
#ifndef DACCESS_COMPILE
COUNT_T MethodDesc::GetStableHash()
{
    WRAPPER_NO_CONTRACT;
    const char *  className = NULL;

    if (IsLCGMethod())
    {
        className = "DynamicClass";
    }
    else if (IsILStub())
    {
        className = ILStubResolver::GetStubClassName(this);
    }

    if (className == NULL)
    {
        return GetVersionResilientMethodHashCode(this);
    }
    else
    {
        int typeHash = ComputeNameHashCode("", className);
        return typeHash ^ ComputeNameHashCode(GetName());
    }
}
#endif // DACCESS_COMPILE

//*******************************************************************************
// Get the number of type parameters to a generic method
DWORD MethodDesc::GetNumGenericMethodArgs()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    g_IBCLogger.LogMethodDescAccess(this);

    if (GetClassification() == mcInstantiated)
    {
        InstantiatedMethodDesc *pIMD = AsInstantiatedMethodDesc();
        return pIMD->m_wNumGenericArgs;
    }
    else return 0;
}

//*******************************************************************************
MethodTable * MethodDesc::GetExactDeclaringType(MethodTable * ownerOrSubType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable * pMT = GetMethodTable();

    // Fast path for typical case.
    if (ownerOrSubType == pMT)
        return pMT;

    // If we come here for array method, the typedef tokens inside GetMethodTableMatchingParentClass
    // will match, but the types are actually from unrelated arrays, so the result would be incorrect.
    _ASSERTE(!IsArray());

    return ownerOrSubType->GetMethodTableMatchingParentClass(pMT);
}

//*******************************************************************************
Instantiation MethodDesc::GetExactClassInstantiation(TypeHandle possibleObjType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END


    return (possibleObjType.IsNull()
            ? GetClassInstantiation()
            : possibleObjType.GetInstantiationOfParentClass(GetMethodTable()));
}

//*******************************************************************************
BOOL MethodDesc::HasSameMethodDefAs(MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    if (this == pMD)
        return TRUE;

    return (GetMemberDef() == pMD->GetMemberDef()) && (GetModule() == pMD->GetModule());
}

//*******************************************************************************
BOOL MethodDesc::IsTypicalSharedInstantiation()
{
    WRAPPER_NO_CONTRACT;

    Instantiation classInst = GetMethodTable()->GetInstantiation();
    if (!ClassLoader::IsTypicalSharedInstantiation(classInst))
        return FALSE;

    if (IsGenericMethodDefinition())
        return FALSE;

    Instantiation methodInst = GetMethodInstantiation();
    if (!ClassLoader::IsTypicalSharedInstantiation(methodInst))
        return FALSE;

    return TRUE;
}

//*******************************************************************************
Instantiation MethodDesc::LoadMethodInstantiation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    if (IsGenericMethodDefinition() && !IsTypicalMethodDefinition())
    {
        return LoadTypicalMethodDefinition()->GetMethodInstantiation();
    }
    else
        return GetMethodInstantiation();
}

//*******************************************************************************
Module *MethodDesc::GetDefiningModuleForOpenMethod()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    Module *pModule = GetMethodTable()->GetDefiningModuleForOpenType();
    if (pModule != NULL)
        return pModule;

    if (IsGenericMethodDefinition())
        return GetModule();

    Instantiation inst = GetMethodInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        // Encoded types are never open
        if (!inst[i].IsEncodedFixup())
        {
            pModule = inst[i].GetDefiningModuleForOpenType();
            if (pModule != NULL)
                return pModule;
        }
    }

    return NULL;
}


//*******************************************************************************
BOOL MethodDesc::ContainsGenericVariables()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // If this is a method of a generic type, does the type have
    // non-instantiated type arguments

    if (TypeHandle(GetMethodTable()).ContainsGenericVariables())
        return TRUE;

    if (IsGenericMethodDefinition())
        return TRUE;

    // If this is an instantiated generic method, are there are any generic type variables
    if (GetNumGenericMethodArgs() != 0)
    {
        Instantiation methodInst = GetMethodInstantiation();
        for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
        {
            if (methodInst[i].ContainsGenericVariables())
                return TRUE;
        }
    }

    return FALSE;
}

//*******************************************************************************
BOOL MethodDesc::IsTightlyBoundToMethodTable()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Anything with the real vtable slot is tightly bound
    if (!HasNonVtableSlot())
        return TRUE;

    // All instantiations of generic methods are stored in the InstMethHashTable.
    if (HasMethodInstantiation())
    {
        if (IsGenericMethodDefinition())
            return TRUE;
        else
            return FALSE;
    }

    // Wrapper stubs are stored in the InstMethHashTable, e.g. for static methods in generic classes
    if (IsWrapperStub())
        return FALSE;

    return TRUE;
}

#ifndef DACCESS_COMPILE

//*******************************************************************************
// Update flags in a thread safe manner.
WORD MethodDesc::InterlockedUpdateFlags(WORD wMask, BOOL fSet)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    WORD    wOldState = m_wFlags;
    DWORD   dwMask = wMask;

    // We need to make this operation atomic (multiple threads can play with the flags field at the same time). But the flags field
    // is a word and we only have interlock operations over dwords. So we round down the flags field address to the nearest aligned
    // dword (along with the intended bitfield mask). Note that we make the assumption that the flags word is aligned itself, so we
    // only have two possibilites: the field already lies on a dword boundary or it's precisely one word out.
    DWORD* pdwFlags = (DWORD*)((ULONG_PTR)&m_wFlags - (offsetof(MethodDesc, m_wFlags) & 0x3));

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6326) // "Suppress PREFast warning about comparing two constants"
#endif // _PREFAST_

#if BIGENDIAN
    if ((offsetof(MethodDesc, m_wFlags) & 0x3) == 0) {
#else // !BIGENDIAN
    if ((offsetof(MethodDesc, m_wFlags) & 0x3) != 0) {
#endif // !BIGENDIAN
        static_assert_no_msg(sizeof(m_wFlags) == 2);
        dwMask <<= 16;
    }
#ifdef _PREFAST_
#pragma warning(pop)
#endif

    g_IBCLogger.LogMethodDescWriteAccess(this);

    if (fSet)
        FastInterlockOr(pdwFlags, dwMask);
    else
        FastInterlockAnd(pdwFlags, ~dwMask);

    return wOldState;
}

WORD MethodDesc::InterlockedUpdateFlags3(WORD wMask, BOOL fSet)
{
    LIMITED_METHOD_CONTRACT;

    WORD    wOldState = m_wFlags3AndTokenRemainder;
    DWORD   dwMask = wMask;

    // We need to make this operation atomic (multiple threads can play with the flags field at the same time). But the flags field
    // is a word and we only have interlock operations over dwords. So we round down the flags field address to the nearest aligned
    // dword (along with the intended bitfield mask). Note that we make the assumption that the flags word is aligned itself, so we
    // only have two possibilites: the field already lies on a dword boundary or it's precisely one word out.
    DWORD* pdwFlags = (DWORD*)((ULONG_PTR)&m_wFlags3AndTokenRemainder - (offsetof(MethodDesc, m_wFlags3AndTokenRemainder) & 0x3));

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6326) // "Suppress PREFast warning about comparing two constants"
#endif // _PREFAST_

#if BIGENDIAN
    if ((offsetof(MethodDesc, m_wFlags3AndTokenRemainder) & 0x3) == 0) {
#else // !BIGENDIAN
    if ((offsetof(MethodDesc, m_wFlags3AndTokenRemainder) & 0x3) != 0) {
#endif // !BIGENDIAN
        static_assert_no_msg(sizeof(m_wFlags3AndTokenRemainder) == 2);
        dwMask <<= 16;
    }
#ifdef _PREFAST_
#pragma warning(pop)
#endif

    g_IBCLogger.LogMethodDescWriteAccess(this);

    if (fSet)
        FastInterlockOr(pdwFlags, dwMask);
    else
        FastInterlockAnd(pdwFlags, ~dwMask);

    return wOldState;
}

#endif // !DACCESS_COMPILE

//*******************************************************************************
// Returns the address of the native code.
//
// Methods which have no native code are either implemented by stubs or not jitted yet.
// For example, NDirectMethodDesc's have no native code.  They are treated as
// implemented by stubs.  On WIN64, these stubs are IL stubs, which DO have native code.
//
// This function returns null if the method has no native code.
PCODE MethodDesc::GetNativeCode()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(!IsDefaultInterfaceMethod() || HasNativeCodeSlot());

    g_IBCLogger.LogMethodDescAccess(this);

    if (HasNativeCodeSlot())
    {
        // When profiler is enabled, profiler may ask to rejit a code even though we
        // we have ngen code for this MethodDesc.  (See MethodDesc::DoPrestub).
        // This means that *GetAddrOfNativeCodeSlot()
        // is not stable. It can turn from non-zero to zero.
        PCODE pCode = *GetAddrOfNativeCodeSlot();
#ifdef TARGET_ARM
        if (pCode != NULL)
            pCode |= THUMB_CODE;
#endif
        return pCode;
    }

    if (!HasStableEntryPoint() || HasPrecode())
        return NULL;

    return GetStableEntryPoint();
}

//*******************************************************************************
PTR_PCODE MethodDesc::GetAddrOfNativeCodeSlot()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(HasNativeCodeSlot());

    SIZE_T size = s_ClassificationSizeTable[m_wFlags & (mdcClassification | mdcHasNonVtableSlot |  mdcMethodImpl)];

    return (PTR_PCODE)(dac_cast<TADDR>(this) + size);
}

//*******************************************************************************
BOOL MethodDesc::IsVoid()
{
    WRAPPER_NO_CONTRACT;

    MetaSig sig(this);
    return sig.IsReturnTypeVoid();
}

//*******************************************************************************
BOOL MethodDesc::HasRetBuffArg()
{
    WRAPPER_NO_CONTRACT;

    MetaSig sig(this);
    ArgIterator argit(&sig);
    return argit.HasRetBuffArg();
}

//*******************************************************************************
// This returns the offset of the IL.
// The offset is relative to the base of the IL image.
ULONG MethodDesc::GetRVA()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (IsRuntimeSupplied())
    {
        return 0;
    }

    // Methods without metadata don't have an RVA.  Examples are IL stubs and LCG methods.
    if (IsNoMetadata())
    {
        return 0;
    }

    if (GetMemberDef() & 0x00FFFFFF)
    {
        Module *pModule = GetModule();
        PREFIX_ASSUME(pModule != NULL);

        DWORD dwDescrOffset;
        DWORD dwImplFlags;
        if (FAILED(pModule->GetMDImport()->GetMethodImplProps(GetMemberDef(), &dwDescrOffset, &dwImplFlags)))
        {   // Class loader already asked for MethodImpls, so this should always succeed (unless there's a
            // bug or a new code path)
            _ASSERTE(!"If this ever fires, then this method should return HRESULT");
            return 0;
        }
        BAD_FORMAT_NOTHROW_ASSERT(IsNDirect() || IsMiIL(dwImplFlags) || IsMiOPTIL(dwImplFlags) || dwDescrOffset == 0);
        return dwDescrOffset;
    }

    return 0;
}

//*******************************************************************************
BOOL MethodDesc::IsVarArg()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    SUPPORTS_DAC;

    Signature signature = GetSignature();
    _ASSERTE(!signature.IsEmpty());
    return MetaSig::IsVarArg(signature);
}

//*******************************************************************************
COR_ILMETHOD* MethodDesc::GetILHeader(BOOL fAllowOverrides /*=FALSE*/)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(IsIL());
        PRECONDITION(!IsUnboxingStub());
    }
    CONTRACTL_END

    Module *pModule = GetModule();

    // Always pickup 'permanent' overrides like reflection emit, EnC, etc.
    // but only grab temporary overrides (like profiler rewrites) if asked to
    TADDR pIL = pModule->GetDynamicIL(GetMemberDef(), fAllowOverrides);

    if (pIL == NULL)
    {
        pIL = pModule->GetIL(GetRVA());
    }

#ifdef _DEBUG_IMPL
    if (pIL != NULL)
    {
        //
        // This is convenient place to verify that COR_ILMETHOD_DECODER::GetOnDiskSize is in sync
        // with our private DACized copy in PEDecoder::ComputeILMethodSize
        //
        COR_ILMETHOD_DECODER header((COR_ILMETHOD *)pIL);
        SIZE_T size1 = header.GetOnDiskSize((COR_ILMETHOD *)pIL);
        SIZE_T size2 = PEDecoder::ComputeILMethodSize(pIL);
        _ASSERTE(size1 == size2);
    }
#endif

#ifdef DACCESS_COMPILE
    return (pIL != NULL) ? DacGetIlMethod(pIL) : NULL;
#else // !DACCESS_COMPILE
    return PTR_COR_ILMETHOD(pIL);
#endif // !DACCESS_COMPILE
}

//*******************************************************************************
ReturnKind MethodDesc::ParseReturnKindFromSig(INDEBUG(bool supportStringConstructors))
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    TypeHandle thValueType;

    MetaSig sig(this);
    CorElementType et = sig.GetReturnTypeNormalized(&thValueType);

    switch (et)
    {
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_VAR:
            return RT_Object;

#ifdef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
        case ELEMENT_TYPE_VALUETYPE:
            // We return value types in registers if they fit in ENREGISTERED_RETURNTYPE_MAXSIZE
            // These valuetypes could contain gc refs.
            {
                ArgIterator argit(&sig);
                if (!argit.HasRetBuffArg())
                {
                    // the type must already be loaded
                    _ASSERTE(!thValueType.IsNull());
                    if (!thValueType.IsTypeDesc())
                    {
                        MethodTable * pReturnTypeMT = thValueType.AsMethodTable();
#ifdef UNIX_AMD64_ABI
                        if (pReturnTypeMT->IsRegPassedStruct())
                        {
                            // The Multi-reg return case using the classhandle is only implemented for AMD64 SystemV ABI.
                            // On other platforms, multi-reg return is not supported with GcInfo v1.
                            // So, the relevant information must be obtained from the GcInfo tables (which requires version2).
                            EEClass* eeClass = pReturnTypeMT->GetClass();
                            ReturnKind regKinds[2] = { RT_Unset, RT_Unset };
                            int orefCount = 0;
                            for (int i = 0; i < 2; i++)
                            {
                                if (eeClass->GetEightByteClassification(i) == SystemVClassificationTypeIntegerReference)
                                {
                                    regKinds[i] = RT_Object;
                                }
                                else if (eeClass->GetEightByteClassification(i) == SystemVClassificationTypeIntegerByRef)
                                {
                                    regKinds[i] = RT_ByRef;
                                }
                                else
                                {
                                    regKinds[i] = RT_Scalar;
                                }
                            }
                            ReturnKind structReturnKind = GetStructReturnKind(regKinds[0], regKinds[1]);
                            return structReturnKind;
                        }
#endif // UNIX_AMD64_ABI

                        if (pReturnTypeMT->ContainsPointers() || pReturnTypeMT->IsByRefLike())
                        {
                            if (pReturnTypeMT->GetNumInstanceFields() == 1)
                            {
                                _ASSERTE(pReturnTypeMT->GetNumInstanceFieldBytes() == sizeof(void*));
                                // Note: we can't distinguish RT_Object from RT_ByRef, the caller has to tolerate that.
                                return RT_Object;
                            }
                            else
                            {
                                // Multi reg return case with pointers, can't restore the actual kind.
                                return RT_Illegal;
                            }
                        }
                    }
                }
            }
            break;
#endif // ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE

#ifdef _DEBUG
        case ELEMENT_TYPE_VOID:
            // String constructors return objects.  We should not have any ecall string
            // constructors, except when called from gc coverage codes (which is only
            // done under debug).  We will therefore optimize the retail version of this
            // method to not support string constructors.
            if (IsCtor() && GetMethodTable()->HasComponentSize())
            {
                _ASSERTE(supportStringConstructors);
                return RT_Object;
            }
            break;
#endif // _DEBUG

        case ELEMENT_TYPE_BYREF:
            return RT_ByRef;

        default:
            break;
    }

    return RT_Scalar;
}

ReturnKind MethodDesc::GetReturnKind(INDEBUG(bool supportStringConstructors))
{
    // For simplicity, we don't hijack in funclets, but if you ever change that,
    // be sure to choose the OnHijack... callback type to match that of the FUNCLET
    // not the main method (it would probably be Scalar).

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
    // Mark that we are performing a stackwalker like operation on the current thread.
    // This is necessary to allow the signature parsing functions to work without triggering any loads
    StackWalkerWalkingThreadHolder threadStackWalking(GetThread());

#ifdef TARGET_X86
    MetaSig msig(this);
    if (msig.HasFPReturn())
    {
        // Figuring out whether the function returns FP or not is hard to do
        // on-the-fly, so we use a different callback helper on x86 where this
        // piece of information is needed in order to perform the right save &
        // restore of the return value around the call to OnHijackScalarWorker.
        return RT_Float;
    }
#endif // TARGET_X86

    return ParseReturnKindFromSig(INDEBUG(supportStringConstructors));
}

#ifdef FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE

//*******************************************************************************
LONG MethodDesc::GetComDispid()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    ULONG dispid = -1;
    HRESULT hr = GetMDImport()->GetDispIdOfMemberDef(
                                    GetMemberDef(),   // The member for which to get props.
                                    &dispid // return dispid.
                                    );
    if (FAILED(hr))
        return -1;

    return (LONG)dispid;
}

//*******************************************************************************
WORD MethodDesc::GetComSlot()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    MethodTable * pMT = GetMethodTable();

    _ASSERTE(pMT->IsInterface());

    // COM slots are biased from MethodTable slots depending on interface type
    WORD numExtraSlots = ComMethodTable::GetNumExtraSlots(pMT->GetComInterfaceType());

    // Normal interfaces are layed out the same way as in the MethodTable, while
    // sparse interfaces need to go through an extra layer of mapping.
    WORD slot;

    if (pMT->IsSparseForCOMInterop())
        slot = numExtraSlots + pMT->GetClass()->GetSparseCOMInteropVTableMap()->LookupVTSlot(GetSlot());
    else
        slot = numExtraSlots + GetSlot();

    return slot;
}

#endif // !DACCESS_COMPILE

#endif // FEATURE_COMINTEROP

//*******************************************************************************
DWORD MethodDesc::GetAttrs() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    if (IsArray())
        return dac_cast<PTR_ArrayMethodDesc>(this)->GetAttrs();
    else if (IsNoMetadata())
        return dac_cast<PTR_DynamicMethodDesc>(this)->GetAttrs();;

    DWORD dwAttributes;
    if (FAILED(GetMDImport()->GetMethodDefProps(GetMemberDef(), &dwAttributes)))
    {   // Class loader already asked for attributes, so this should always succeed (unless there's a
        // bug or a new code path)
        _ASSERTE(!"If this ever fires, then this method should return HRESULT");
        return 0;
    }
    return dwAttributes;
}

//*******************************************************************************
DWORD MethodDesc::GetImplAttrs()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    DWORD props;
    if (FAILED(GetMDImport()->GetMethodImplProps(GetMemberDef(), NULL, &props)))
    {   // Class loader already asked for MethodImpls, so this should always succeed (unless there's a
        // bug or a new code path)
        _ASSERTE(!"If this ever fires, then this method should return HRESULT");
        return 0;
    }
    return props;
}

//*******************************************************************************
Module* MethodDesc::GetLoaderModule()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (HasMethodInstantiation() && !IsGenericMethodDefinition())
    {
        Module *retVal = ClassLoader::ComputeLoaderModule(GetMethodTable(),
                                                GetMemberDef(),
                                                GetMethodInstantiation());
        return retVal;
    }
    else
    {
        return GetMethodTable()->GetLoaderModule();
    }
}

//*******************************************************************************
Module *MethodDesc::GetModule() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    g_IBCLogger.LogMethodDescAccess(this);
    Module *pModule = GetModule_NoLogging();

    return pModule;
}

//*******************************************************************************
Module *MethodDesc::GetModule_NoLogging() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    MethodTable* pMT = GetMethodDescChunk()->GetMethodTable();
    return pMT->GetModule();
}

//*******************************************************************************
// Is this an instantiating stub for generics?  This does not include those
// BoxedEntryPointStubs which call an instantiating stub.
BOOL MethodDesc::IsInstantiatingStub()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return
        (GetClassification() == mcInstantiated)
         && !IsUnboxingStub()
         && AsInstantiatedMethodDesc()->IMD_IsWrapperStubWithInstantiations();
}

//*******************************************************************************
BOOL MethodDesc::IsWrapperStub()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return (IsUnboxingStub() || IsInstantiatingStub());
}

#ifndef DACCESS_COMPILE
//*******************************************************************************

MethodDesc *MethodDesc::GetWrappedMethodDesc()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsWrapperStub());

    if (IsUnboxingStub())
    {
        return this->GetMethodTable()->GetUnboxedEntryPointMD(this);
    }

    if (IsInstantiatingStub())
    {
        MethodDesc *pRet = AsInstantiatedMethodDesc()->IMD_GetWrappedMethodDesc();
#ifdef _DEBUG
        MethodDesc *pAltMD  =
               MethodDesc::FindOrCreateAssociatedMethodDesc(this,
                                                            this->GetMethodTable(),
                                                            FALSE, /* no unboxing entrypoint */
                                                            this->GetMethodInstantiation(),
                                                            TRUE /* get shared code */ );
        _ASSERTE(pAltMD == pRet);
#endif // _DEBUG
        return pRet;
    }
    return NULL;
}


MethodDesc *MethodDesc::GetExistingWrappedMethodDesc()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsWrapperStub());

    if (IsUnboxingStub())
    {
        return this->GetMethodTable()->GetExistingUnboxedEntryPointMD(this);
    }

    if (IsInstantiatingStub())
    {
        MethodDesc *pRet = AsInstantiatedMethodDesc()->IMD_GetWrappedMethodDesc();
        return pRet;
    }
    return NULL;
}



#endif // !DACCESS_COMPILE

//*******************************************************************************
BOOL MethodDesc::IsSharedByGenericInstantiations()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsWrapperStub())
        return FALSE;
    else if (GetMethodTable()->IsSharedByGenericInstantiations())
        return TRUE;
    else return IsSharedByGenericMethodInstantiations();
}

//*******************************************************************************
BOOL MethodDesc::IsSharedByGenericMethodInstantiations()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (GetClassification() == mcInstantiated)
        return AsInstantiatedMethodDesc()->IMD_IsSharedByGenericMethodInstantiations();
    else return FALSE;
}

//*******************************************************************************
// Does this method require an extra MethodTable argument for instantiation information?
// This is the case for
// * per-inst static methods in shared-code instantiated generic classes (e.g. static void MyClass<string>::m())
//     - there is no this pointer providing generic dictionary info
// * shared-code instance methods in instantiated generic structs (e.g. void MyValueType<string>::m())
//     - unboxed 'this' pointer in value-type instance methods don't have MethodTable pointer by definition
// * shared instance and default interface methods called via interface dispatch (e. g. IFoo<string>.Foo calling into IFoo<object>::Foo())
//     - this pointer is ambiguous as it can implement more than one IFoo<T>
BOOL MethodDesc::RequiresInstMethodTableArg()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return
        IsSharedByGenericInstantiations() &&
        !HasMethodInstantiation() &&
        (IsStatic() || GetMethodTable()->IsValueType() || (GetMethodTable()->IsInterface() && !IsAbstract()));

}

//*******************************************************************************
// Does this method require an extra InstantiatedMethodDesc argument for instantiation information?
// This is the case for
// * shared-code instantiated generic methods
BOOL MethodDesc::RequiresInstMethodDescArg()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return IsSharedByGenericInstantiations() &&
        HasMethodInstantiation();
}

//*******************************************************************************
// Does this method require any kind of extra argument for instantiation information?
BOOL MethodDesc::RequiresInstArg()
{
    LIMITED_METHOD_DAC_CONTRACT;

    BOOL fRet = IsSharedByGenericInstantiations() &&
        (HasMethodInstantiation() || IsStatic() || GetMethodTable()->IsValueType() || (GetMethodTable()->IsInterface() && !IsAbstract()));

    _ASSERT(fRet == (RequiresInstMethodTableArg() || RequiresInstMethodDescArg()));
    return fRet;
}

//*******************************************************************************
BOOL MethodDesc::IsRuntimeMethodHandle()
{
    WRAPPER_NO_CONTRACT;

    // <TODO> Refine this check further for BoxedEntryPointStubs </TODO>
    return (!HasMethodInstantiation() || !IsSharedByGenericMethodInstantiations());
}

//*******************************************************************************
// Strip off method and class instantiation if present e.g.
// C1<int>.m1<string> -> C1.m1
// C1<int>.m2 -> C1.m2
// C2.m2<int> -> C2.m2
// C2.m2 -> C2.m2
MethodDesc* MethodDesc::LoadTypicalMethodDefinition()
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->IsTypicalMethodDefinition());
    }
    CONTRACT_END

#ifndef DACCESS_COMPILE
    if (HasClassOrMethodInstantiation())
    {
        MethodTable *pMT = GetMethodTable();
        if (!pMT->IsTypicalTypeDefinition())
            pMT = ClassLoader::LoadTypeDefThrowing(pMT->GetModule(),
                                                   pMT->GetCl(),
                                                   ClassLoader::ThrowIfNotFound,
                                                   ClassLoader::PermitUninstDefOrRef).GetMethodTable();
        CONSISTENCY_CHECK(TypeHandle(pMT).CheckFullyLoaded());
        MethodDesc *resultMD = pMT->GetParallelMethodDesc(this);
        PREFIX_ASSUME(resultMD != NULL);
        resultMD->CheckRestore();
        RETURN (resultMD);
    }
    else
#endif // !DACCESS_COMPILE
        RETURN(this);
}

//*******************************************************************************
BOOL MethodDesc::IsTypicalMethodDefinition() const
{
    LIMITED_METHOD_CONTRACT;

    if (HasMethodInstantiation() && !IsGenericMethodDefinition())
        return FALSE;

    if (HasClassInstantiation() && !GetMethodTable()->IsGenericTypeDefinition())
        return FALSE;

    return TRUE;
}

//*******************************************************************************
BOOL MethodDesc::AcquiresInstMethodTableFromThis() {
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return
        IsSharedByGenericInstantiations()  &&
        !HasMethodInstantiation() &&
        !IsStatic() &&
        !GetMethodTable()->IsValueType() &&
        !(GetMethodTable()->IsInterface() && !IsAbstract());
}

//*******************************************************************************
UINT MethodDesc::SizeOfArgStack()
{
    WRAPPER_NO_CONTRACT;
    MetaSig msig(this);
    ArgIterator argit(&msig);
    return argit.SizeOfArgStack();
}


UINT MethodDesc::SizeOfNativeArgStack()
{
#ifndef UNIX_AMD64_ABI
    return SizeOfArgStack();
#else
    WRAPPER_NO_CONTRACT;
    MetaSig msig(this);
    PInvokeArgIterator argit(&msig);
    return argit.SizeOfArgStack();
#endif
}

#ifdef TARGET_X86
//*******************************************************************************
UINT MethodDesc::CbStackPop()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    MetaSig msig(this);
    ArgIterator argit(&msig);

    bool fCtorOfVariableSizedObject = msig.HasThis() && (GetMethodTable() == g_pStringClass) && IsCtor();
    if (fCtorOfVariableSizedObject)
    {
        msig.ClearHasThis();
    }
    
    return argit.CbStackPop();
}
#endif // TARGET_X86

#ifndef DACCESS_COMPILE

//*******************************************************************************
// Strip off the method instantiation (if present) e.g.
// C<int>.m<string> -> C<int>.m
// D.m<string> -> D.m
// Note that this also canonicalizes the owning method table
// @todo check uses and clean this up
MethodDesc* MethodDesc::StripMethodInstantiation()
{
    CONTRACT(MethodDesc*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    if (!HasClassOrMethodInstantiation())
        RETURN(this);

    MethodTable *pMT = GetMethodTable()->GetCanonicalMethodTable();
    MethodDesc *resultMD = pMT->GetParallelMethodDesc(this);
    _ASSERTE(resultMD->IsGenericMethodDefinition() || !resultMD->HasMethodInstantiation());
    RETURN(resultMD);
}

//*******************************************************************************
MethodDescChunk *MethodDescChunk::CreateChunk(LoaderHeap *pHeap, DWORD methodDescCount,
    DWORD classification, BOOL fNonVtableSlot, BOOL fNativeCodeSlot, BOOL fComPlusCallInfo, MethodTable *pInitialMT, AllocMemTracker *pamTracker)
{
    CONTRACT(MethodDescChunk *)
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(CheckPointer(pHeap));
        PRECONDITION(CheckPointer(pInitialMT));
        PRECONDITION(CheckPointer(pamTracker));

        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SIZE_T oneSize = MethodDesc::GetBaseSize(classification);

    if (fNonVtableSlot)
        oneSize += sizeof(MethodDesc::NonVtableSlot);

    if (fNativeCodeSlot)
        oneSize += sizeof(MethodDesc::NativeCodeSlot);

#ifdef FEATURE_COMINTEROP
    if (fComPlusCallInfo)
        oneSize += sizeof(ComPlusCallInfo);
#else // FEATURE_COMINTEROP
    _ASSERTE(!fComPlusCallInfo);
#endif // FEATURE_COMINTEROP

    _ASSERTE((oneSize & MethodDesc::ALIGNMENT_MASK) == 0);

    DWORD maxMethodDescsPerChunk = MethodDescChunk::MaxSizeOfMethodDescs / oneSize;

    if (methodDescCount == 0)
        methodDescCount = maxMethodDescsPerChunk;

    MethodDescChunk * pFirstChunk = NULL;

    do
    {
        DWORD count = min(methodDescCount, maxMethodDescsPerChunk);

        void * pMem = pamTracker->Track(
                pHeap->AllocMem(S_SIZE_T(sizeof(TADDR) + sizeof(MethodDescChunk) + oneSize * count)));

        // Skip pointer to temporary entrypoints
        MethodDescChunk * pChunk = (MethodDescChunk *)((BYTE*)pMem + sizeof(TADDR));

        pChunk->SetSizeAndCount(oneSize * count, count);
        pChunk->SetMethodTable(pInitialMT);

        MethodDesc * pMD = pChunk->GetFirstMethodDesc();
        for (DWORD i = 0; i < count; i++)
        {
            pMD->SetChunkIndex(pChunk);

            pMD->SetClassification(classification);
            if (fNonVtableSlot)
                pMD->SetHasNonVtableSlot();
            if (fNativeCodeSlot)
                pMD->SetHasNativeCodeSlot();
#ifdef FEATURE_COMINTEROP
            if (fComPlusCallInfo)
                pMD->SetupGenericComPlusCall();
#endif // FEATURE_COMINTEROP

            _ASSERTE(pMD->SizeOf() == oneSize);

            pMD = (MethodDesc *)((BYTE *)pMD + oneSize);
        }

        pChunk->m_next = pFirstChunk;
        pFirstChunk = pChunk;

        methodDescCount -= count;
    }
    while (methodDescCount > 0);

    RETURN pFirstChunk;
}

//--------------------------------------------------------------------
// Virtual Resolution on Objects
//
// Given a MethodDesc and an Object, return the target address
// and/or the target MethodDesc and/or make a call.
//
// Some of the implementation of this logic is in
// MethodTable::GetMethodDescForInterfaceMethodAndServer.
// Those functions should really be moved here.
//--------------------------------------------------------------------

//*******************************************************************************
// The following resolve virtual dispatch for the given method on the given
// object down to an actual address to call, including any
// handling of context proxies and other thunking layers.
MethodDesc* MethodDesc::ResolveGenericVirtualMethod(OBJECTREF *orThis)
{
    CONTRACT(MethodDesc *)
    {
        THROWS;
        GC_TRIGGERS;

        PRECONDITION(IsVtableMethod());
        PRECONDITION(HasMethodInstantiation());
        PRECONDITION(!ContainsGenericVariables());
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->HasMethodInstantiation());
    }
    CONTRACT_END;

    // Method table of target (might be instantiated)
    MethodTable *pObjMT = (*orThis)->GetMethodTable();

    // This is the static method descriptor describing the call.
    // It is not the destination of the call, which we must compute.
    MethodDesc* pStaticMD = this;

    // Strip off the method instantiation if present
    MethodDesc* pStaticMDWithoutGenericMethodArgs = pStaticMD->StripMethodInstantiation();

    // Compute the target, though we have not yet applied the type arguments.
    MethodDesc *pTargetMDBeforeGenericMethodArgs =
        pStaticMD->IsInterface()
        ? MethodTable::GetMethodDescForInterfaceMethodAndServer(TypeHandle(pStaticMD->GetMethodTable()),
                                                                pStaticMDWithoutGenericMethodArgs,orThis)
        : pObjMT->GetMethodDescForSlot(pStaticMDWithoutGenericMethodArgs->GetSlot());

    pTargetMDBeforeGenericMethodArgs->CheckRestore();

    // The actual destination may lie anywhere in the inheritance hierarchy.
    // between the static descriptor and the target object.
    // So now compute where we are really going!  This may be an instantiated
    // class type if the generic virtual lies in a generic class.
    MethodTable *pTargetMT = pTargetMDBeforeGenericMethodArgs->GetMethodTable();

    // No need to find/create a new generic instantiation if the target is the
    // same as the static, i.e. the virtual method has not been overriden.
    if (!pTargetMT->IsSharedByGenericInstantiations() && !pTargetMT->IsValueType() &&
        pTargetMDBeforeGenericMethodArgs == pStaticMDWithoutGenericMethodArgs)
        RETURN(pStaticMD);

    if (pTargetMT->IsSharedByGenericInstantiations())
    {
        pTargetMT = ClassLoader::LoadGenericInstantiationThrowing(pTargetMT->GetModule(),
                                                                  pTargetMT->GetCl(),
                                                                  pTargetMDBeforeGenericMethodArgs->GetExactClassInstantiation(TypeHandle(pObjMT))).GetMethodTable();
    }

    RETURN(MethodDesc::FindOrCreateAssociatedMethodDesc(
        pTargetMDBeforeGenericMethodArgs,
        pTargetMT,
        (pTargetMT->IsValueType()), /* get unboxing entry point if a struct*/
        pStaticMD->GetMethodInstantiation(),
        FALSE /* no allowInstParam */ ));
}

//*******************************************************************************
PCODE MethodDesc::GetSingleCallableAddrOfVirtualizedCode(OBJECTREF *orThis, TypeHandle staticTH)
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(IsVtableMethod());

    MethodTable *pObjMT = (*orThis)->GetMethodTable();

    if (HasMethodInstantiation())
    {
        CheckRestore();
        MethodDesc *pResultMD = ResolveGenericVirtualMethod(orThis);

        // If we're remoting this call we can't call directly on the returned
        // method desc, we need to go through a stub that guarantees we end up
        // in the remoting handler. The stub we use below is normally just for
        // non-virtual calls on virtual methods (that have the same problem
        // where we could end up bypassing the remoting system), but it serves
        // our purpose here (basically pushes our correctly instantiated,
        // resolved method desc on the stack and calls the remoting code).

        return pResultMD->GetSingleCallableAddrOfCode();
    }

    if (IsInterface())
    {
        MethodDesc * pTargetMD = MethodTable::GetMethodDescForInterfaceMethodAndServer(staticTH,this,orThis);
        return pTargetMD->GetSingleCallableAddrOfCode();
    }

    return pObjMT->GetRestoredSlot(GetSlot());
}

//*******************************************************************************
// The following resolve virtual dispatch for the given method on the given
// object down to an actual address to call, including any
// handling of context proxies and other thunking layers.
PCODE MethodDesc::GetMultiCallableAddrOfVirtualizedCode(OBJECTREF *orThis, TypeHandle staticTH)
{
    CONTRACT(PCODE)
    {
        THROWS;
        GC_TRIGGERS;

        PRECONDITION(IsVtableMethod());
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    // Method table of target (might be instantiated)
    MethodTable *pObjMT = (*orThis)->GetMethodTable();

    // This is the static method descriptor describing the call.
    // It is not the destination of the call, which we must compute.
    MethodDesc* pStaticMD = this;
    MethodDesc *pTargetMD;

    if (pStaticMD->HasMethodInstantiation())
    {
        CheckRestore();
        pTargetMD = ResolveGenericVirtualMethod(orThis);

        // If we're remoting this call we can't call directly on the returned
        // method desc, we need to go through a stub that guarantees we end up
        // in the remoting handler. The stub we use below is normally just for
        // non-virtual calls on virtual methods (that have the same problem
        // where we could end up bypassing the remoting system), but it serves
        // our purpose here (basically pushes our correctly instantiated,
        // resolved method desc on the stack and calls the remoting code).

        RETURN(pTargetMD->GetMultiCallableAddrOfCode());
    }

    if (pStaticMD->IsInterface())
    {
        pTargetMD = MethodTable::GetMethodDescForInterfaceMethodAndServer(staticTH,pStaticMD,orThis);
        RETURN(pTargetMD->GetMultiCallableAddrOfCode());
    }


    pTargetMD = pObjMT->GetMethodDescForSlot(pStaticMD->GetSlot());

    RETURN (pTargetMD->GetMultiCallableAddrOfCode());
}

//*******************************************************************************
PCODE MethodDesc::GetMultiCallableAddrOfCode(CORINFO_ACCESS_FLAGS accessFlags /*=CORINFO_ACCESS_LDFTN*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    PCODE ret = TryGetMultiCallableAddrOfCode(accessFlags);

    if (ret == NULL)
    {
        GCX_COOP();

        // We have to allocate funcptr stub
        ret = GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(this);
    }

    return ret;
}

//*******************************************************************************
//
// Returns a callable entry point for a function.
// Multiple entry points could be used for a single function.
// ie. this function is not idempotent
//

// We must ensure that GetMultiCallableAddrOfCode works
// correctly for all of the following cases:
// 1.   shared generic method instantiations
// 2.   unshared generic method instantiations
// 3.   instance methods in shared generic classes
// 4.   instance methods in unshared generic classes
// 5.   static methods in shared generic classes.
// 6.   static methods in unshared generic classes.
//
// For case 1 and 5 the methods are implemented using
// an instantiating stub (i.e. IsInstantiatingStub()
// should be true).  These stubs pass on to
// shared-generic-code-which-requires-an-extra-type-context-parameter.
// So whenever we use LDFTN on these we need to give out
// the address of an instantiating stub.
//
// For cases 2, 3, 4 and 6 we can just use the standard technique for LdFtn:
// (for 2 we give out the address of the fake "slot" in InstantiatedMethodDescs)
// (for 3 it doesn't matter if the code is shared between instantiations
// because the instantiation context is picked up from the "this" parameter.)

PCODE MethodDesc::TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_FLAGS accessFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    // Record this method desc if required
    g_IBCLogger.LogMethodDescAccess(this);

    if (IsGenericMethodDefinition())
    {
        _ASSERTE(!"Cannot take the address of an uninstantiated generic method.");
        COMPlusThrow(kInvalidProgramException);
    }

    if (accessFlags & CORINFO_ACCESS_LDFTN)
    {
        // Whenever we use LDFTN on shared-generic-code-which-requires-an-extra-parameter
        // we need to give out the address of an instantiating stub.  This is why we give
        // out GetStableEntryPoint() for the IsInstantiatingStub() case: this is
        // safe.  But first we assert that we only use GetMultiCallableAddrOfCode on
        // the instantiating stubs and not on the shared code itself.
        _ASSERTE(!RequiresInstArg());
        _ASSERTE(!IsSharedByGenericMethodInstantiations());

        // No other access flags are valid with CORINFO_ACCESS_LDFTN
        _ASSERTE((accessFlags & ~CORINFO_ACCESS_LDFTN) == 0);
    }

    // We create stable entrypoints for these upfront
    if (IsWrapperStub() || IsEnCAddedMethod())
        return GetStableEntryPoint();


    // For EnC always just return the stable entrypoint so we can update the code
    if (IsEnCMethod())
        return GetStableEntryPoint();

    // If the method has already been jitted, we can give out the direct address
    // Note that we may have previously created a FuncPtrStubEntry, but
    // GetMultiCallableAddrOfCode() does not need to be idempotent.

    if (IsFCall())
    {
        // Call FCalls directly when possible
        if (!IsInterface() && !GetMethodTable()->ContainsGenericVariables())
        {
            BOOL fSharedOrDynamicFCallImpl;
            PCODE pFCallImpl = ECall::GetFCallImpl(this, &fSharedOrDynamicFCallImpl);

            if (!fSharedOrDynamicFCallImpl)
                return pFCallImpl;

            // Fake ctors share one implementation that has to be wrapped by prestub
            GetOrCreatePrecode();
        }
    }
    else
    {
        if (IsPointingToStableNativeCode())
            return GetNativeCode();
    }

    if (HasStableEntryPoint())
        return GetStableEntryPoint();

    if (IsVersionableWithVtableSlotBackpatch())
    {
        // Caller has to call via slot or allocate funcptr stub
        return NULL;
    }

    // Force the creation of the precode if we would eventually got one anyway
    if (MayHavePrecode())
        return GetOrCreatePrecode()->GetEntryPoint();

#ifdef HAS_COMPACT_ENTRYPOINTS
    // Caller has to call via slot or allocate funcptr stub
    return NULL;
#else // HAS_COMPACT_ENTRYPOINTS
    //
    // Embed call to the temporary entrypoint into the code. It will be patched
    // to point to the actual code later.
    //
    return GetTemporaryEntryPoint();
#endif // HAS_COMPACT_ENTRYPOINTS
}

//*******************************************************************************
PCODE MethodDesc::GetCallTarget(OBJECTREF* pThisObj, TypeHandle ownerType)
{
    CONTRACTL
    {
        THROWS;                 // Resolving a generic virtual method can throw
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

    PCODE pTarget;

    if (IsVtableMethod() && !GetMethodTable()->IsValueType())
    {
        CONSISTENCY_CHECK(NULL != pThisObj);
        if (ownerType.IsNull())
            ownerType = GetMethodTable();
        pTarget = GetSingleCallableAddrOfVirtualizedCode(pThisObj, ownerType);
    }
    else
    {
        pTarget = GetSingleCallableAddrOfCode();
    }

    return pTarget;
}

MethodDesc* NonVirtualEntry2MethodDesc(PCODE entryPoint)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    RangeSection* pRS = ExecutionManager::FindCodeRange(entryPoint, ExecutionManager::GetScanFlags());
    if (pRS == NULL)
        return NULL;

    MethodDesc* pMD;
    if (pRS->pjit->JitCodeToMethodInfo(pRS, entryPoint, &pMD, NULL))
        return pMD;

    if (pRS->pjit->GetStubCodeBlockKind(pRS, entryPoint) == STUB_CODE_BLOCK_PRECODE)
        return MethodDesc::GetMethodDescFromStubAddr(entryPoint);

    // We should never get here
    _ASSERTE(!"NonVirtualEntry2MethodDesc failed for RangeSection");
    return NULL;
}

//*******************************************************************************
// convert an entry point into a method desc
MethodDesc* Entry2MethodDesc(PCODE entryPoint, MethodTable *pMT)
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(RETVAL->SanityCheck());
    }
    CONTRACT_END

    MethodDesc* pMD = NonVirtualEntry2MethodDesc(entryPoint);
    if (pMD != NULL)
        RETURN(pMD);

    pMD = VirtualCallStubManagerManager::Entry2MethodDesc(entryPoint, pMT);
    if (pMD != NULL)
        RETURN(pMD);

    // Is it an FCALL?
    pMD = ECall::MapTargetBackToMethod(entryPoint);
    if (pMD != NULL)
        RETURN(pMD);

    // We should never get here
    _ASSERTE(!"Entry2MethodDesc failed");
    RETURN (NULL);
}

//*******************************************************************************
BOOL MethodDesc::IsPointingToPrestub()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!HasStableEntryPoint())
    {
        if (IsVersionableWithVtableSlotBackpatch())
        {
            return GetMethodEntryPoint() == GetTemporaryEntryPoint();
        }
        return TRUE;
    }

    if (!HasPrecode())
        return FALSE;

    return GetPrecode()->IsPointingToPrestub();
}

//*******************************************************************************
void MethodDesc::Reset()
{
    WRAPPER_NO_CONTRACT;

    // This method is not thread-safe since we are updating
    // different pieces of data non-atomically.
    // Use this only if you can guarantee thread-safety somehow.

    _ASSERTE(IsEnCMethod() || // The process is frozen by the debugger
             IsDynamicMethod() || // These are used in a very restricted way
             GetLoaderModule()->IsReflection()); // Rental methods

    // Reset any flags relevant to the old code
    ClearFlagsOnUpdate();

    if (HasPrecode())
    {
        GetPrecode()->Reset();
    }
    else
    {
        // We should go here only for the rental methods
        _ASSERTE(GetLoaderModule()->IsReflection());

        InterlockedUpdateFlags2(enum_flag2_HasStableEntryPoint | enum_flag2_HasPrecode, FALSE);

        *GetAddrOfSlot() = GetTemporaryEntryPoint();
    }

    if (HasNativeCodeSlot())
    {
        *GetAddrOfNativeCodeSlot() = NULL;
    }
    _ASSERTE(!HasNativeCode());
}

//*******************************************************************************
Dictionary* MethodDesc::GetMethodDictionary()
{
    WRAPPER_NO_CONTRACT;

    return
        (GetClassification() == mcInstantiated)
        ? (Dictionary*) (AsInstantiatedMethodDesc()->IMD_GetMethodDictionary())
        : NULL;
}

//*******************************************************************************
DictionaryLayout* MethodDesc::GetDictionaryLayout()
{
    WRAPPER_NO_CONTRACT;

    return
        ((GetClassification() == mcInstantiated) && !IsUnboxingStub())
        ? AsInstantiatedMethodDesc()->IMD_GetDictionaryLayout()
        : NULL;
}

#endif // !DACCESS_COMPILE

//*******************************************************************************
MethodImpl *MethodDesc::GetMethodImpl()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(HasMethodImplSlot());
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    SIZE_T size = s_ClassificationSizeTable[m_wFlags & (mdcClassification | mdcHasNonVtableSlot)];

    return PTR_MethodImpl(dac_cast<TADDR>(this) + size);
}

#ifndef DACCESS_COMPILE

//*******************************************************************************
BOOL MethodDesc::RequiresMethodDescCallingConvention(BOOL fEstimateForChunk /*=FALSE*/)
{
    LIMITED_METHOD_CONTRACT;

    // Interop marshaling is implemented using shared stubs
    if (IsNDirect() || IsComPlusCall() || IsGenericComPlusCall())
        return TRUE;


    return FALSE;
}

//*******************************************************************************
BOOL MethodDesc::RequiresStableEntryPoint(BOOL fEstimateForChunk /*=FALSE*/)
{
    LIMITED_METHOD_CONTRACT;

    // Create precodes for versionable methods
    if (IsVersionableWithPrecode())
        return TRUE;

    // Create precodes for edit and continue to make methods updateable
    if (IsEnCMethod() || IsEnCAddedMethod())
        return TRUE;

    // Precreate precodes for LCG methods so we do not leak memory when the method descs are recycled
    if (IsLCGMethod())
        return TRUE;

    if (fEstimateForChunk)
    {
        // Make a best guess based on the method table of the chunk.
        if (IsInterface())
            return TRUE;
    }
    else
    {
        // Wrapper stubs are stored in generic dictionary that's not backpatched
        if (IsWrapperStub())
            return TRUE;

        // TODO: Can we avoid early allocation of precodes for interfaces and cominterop?
        if ((IsInterface() && !IsStatic() && IsVirtual()) || IsComPlusCall())
            return TRUE;
    }

    return FALSE;
}

#endif // !DACCESS_COMPILE

//*******************************************************************************
BOOL MethodDesc::MayHaveNativeCode()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    // This code flow of this method should roughly match the code flow of MethodDesc::DoPrestub.

    switch (GetClassification())
    {
    case mcIL:              // IsIL() case. Handled below.
        break;
    case mcFCall:           // FCalls do not have real native code.
        return FALSE;
    case mcNDirect:         // NDirect never have native code (note that the NDirect method
        return FALSE;       //  does not appear as having a native code even for stubs as IL)
    case mcEEImpl:          // Runtime provided implementation. No native code.
        return FALSE;
    case mcArray:           // Runtime provided implementation. No native code.
        return FALSE;
    case mcInstantiated:    // IsIL() case. Handled below.
        break;
#ifdef FEATURE_COMINTEROP
    case mcComInterop:      // Generated stub. No native code.
        return FALSE;
#endif // FEATURE_COMINTEROP
    case mcDynamic:         // LCG or stub-as-il.
        return TRUE;
    default:
        _ASSERTE(!"Unknown classification");
    }

    _ASSERTE(IsIL());

    if (IsWrapperStub() || ContainsGenericVariables() || IsAbstract())
    {
        return FALSE;
    }

    return TRUE;
}

//*******************************************************************************
void MethodDesc::CheckRestore(ClassLoadLevel level)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    if (!GetMethodTable()->IsFullyLoaded())
    {
        g_IBCLogger.LogMethodDescAccess(this);

        if (GetClassification() == mcInstantiated)
        {
#ifndef DACCESS_COMPILE
            InstantiatedMethodDesc *pIMD = AsInstantiatedMethodDesc();

            // First restore method table pointer in singleton chunk;
            // it might be out-of-module
            ClassLoader::EnsureLoaded(TypeHandle(GetMethodTable()), level);

            g_IBCLogger.LogMethodDescWriteAccess(this);

            pIMD->m_wFlags2 = pIMD->m_wFlags2 & ~InstantiatedMethodDesc::Unrestored;

            if (ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER))
            {
                ETW::MethodLog::MethodRestored(this);
            }

#else // DACCESS_COMPILE
            DacNotImpl();
#endif // DACCESS_COMPILE
        }
        else if (IsILStub()) // the only stored-sig MD type that uses ET_INTERNAL
        {
            ClassLoader::EnsureLoaded(TypeHandle(GetMethodTable()), level);

#ifndef DACCESS_COMPILE
            if (ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER))
            {
                ETW::MethodLog::MethodRestored(this);
            }
#else // DACCESS_COMPILE
            DacNotImpl();
#endif // DACCESS_COMPILE
        }
        else
        {
            ClassLoader::EnsureLoaded(TypeHandle(GetMethodTable()), level);
        }
    }
}

// static
MethodDesc* MethodDesc::GetMethodDescFromStubAddr(PCODE addr, BOOL fSpeculative /*=FALSE*/)
{
    CONTRACT(MethodDesc *)
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACT_END;

    MethodDesc *  pMD = NULL;

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (MethodDescChunk::IsCompactEntryPointAtAddress(addr))
    {
        pMD = MethodDescChunk::GetMethodDescFromCompactEntryPoint(addr, fSpeculative);
        RETURN(pMD);
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    // Otherwise this must be some kind of precode
    //
    Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(addr, fSpeculative);
    PREFIX_ASSUME(fSpeculative || (pPrecode != NULL));
    if (pPrecode != NULL)
    {
        pMD = pPrecode->GetMethodDesc(fSpeculative);
        RETURN(pMD);
    }

    RETURN(NULL); // Not found
}

#ifdef HAS_COMPACT_ENTRYPOINTS

#if defined(TARGET_X86)

#include <pshpack1.h>
static const struct CentralJumpCode {
    BYTE m_movzxEAX[3];
    BYTE m_shlEAX[3];
    BYTE m_addEAX[1];
    MethodDesc* m_pBaseMD;
    BYTE m_jmp[1];
    INT32 m_rel32;

    inline void Setup(CentralJumpCode* pCodeRX, MethodDesc* pMD, PCODE target, LoaderAllocator *pLoaderAllocator) {
        WRAPPER_NO_CONTRACT;
        m_pBaseMD = pMD;
        m_rel32 = rel32UsingJumpStub(&pCodeRX->m_rel32, target, pMD, pLoaderAllocator);
    }

    inline BOOL CheckTarget(TADDR target) {
        LIMITED_METHOD_CONTRACT;
        TADDR addr = rel32Decode(PTR_HOST_MEMBER_TADDR(CentralJumpCode, this, m_rel32));
        return (addr == target);
    }
}
c_CentralJumpCode = {
    { 0x0F, 0xB6, 0xC0 },                         //   movzx eax,al
    { 0xC1, 0xE0, MethodDesc::ALIGNMENT_SHIFT },  //   shl   eax, MethodDesc::ALIGNMENT_SHIFT
    { 0x05 }, NULL,                               //   add   eax, pBaseMD
    { 0xE9 }, 0                                   //   jmp   PreStub
};
#include <poppack.h>

#elif defined(TARGET_ARM)

#include <pshpack1.h>
struct CentralJumpCode {
    BYTE m_ldrPC[4];
    BYTE m_short[2];
    MethodDescChunk *m_pChunk;
    PCODE m_target;

    inline void Setup(PCODE target, MethodDescChunk *pChunk) {
        WRAPPER_NO_CONTRACT;

        m_target = target;
        m_pChunk = pChunk;
    }

    inline BOOL CheckTarget(TADDR target) {
        WRAPPER_NO_CONTRACT;
        return ((TADDR)m_target == target);
    }
}
c_CentralJumpCode = {
    { 0xDF, 0xF8, 0x08, 0xF0 },                         //   ldr pc, =pTarget
    { 0x00, 0x00 },                                     //   short offset for alignment
    0,                                                  //   pChunk
    0                                                   //   pTarget
};
#include <poppack.h>

#else
#error Unsupported platform
#endif

typedef DPTR(struct CentralJumpCode) PTR_CentralJumpCode;
#define TEP_CENTRAL_JUMP_SIZE   sizeof(c_CentralJumpCode)
static_assert_no_msg((TEP_CENTRAL_JUMP_SIZE & 1) == 0);

#define TEP_ENTRY_SIZE          4

#ifdef TARGET_ARM

#define TEP_HALF_ENTRY_SIZE (TEP_ENTRY_SIZE / 2)

// Compact entry point on arm consists of two thumb instructions:
//   mov r12, pc
//   b CentralJumpCode

// First instruction 0x46fc
#define TEP_ENTRY_INSTR1_BYTE1 0xFC
#define TEP_ENTRY_INSTR1_BYTE2 0x46

// Mask for unconditional branch opcode
#define TEP_ENTRY_INSTR2_MASK1 0xE0

// Mask for opcode
#define TEP_ENTRY_INSTR2_MASK2 0xF8

// Bit used for ARM to identify compact entry points
#define COMPACT_ENTRY_ARM_CODE 0x2

/* static */ int MethodDescChunk::GetCompactEntryPointMaxCount ()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB / TEP_ENTRY_SIZE;
}

// Get offset from the start of current compact entry point to the CentralJumpCode
static uint16_t DecodeOffsetFromBranchToCentralJump (uint16_t instr)
{
    int16_t offset = decodeUnconditionalBranchThumb ((LPBYTE) &instr);

    offset += PC_REG_RELATIVE_OFFSET + TEP_HALF_ENTRY_SIZE;

    _ASSERTE (offset >= TEP_ENTRY_SIZE && (offset % TEP_ENTRY_SIZE == 0));

    return (uint16_t) offset;
}

#ifndef DACCESS_COMPILE

// Encode branch instruction to central jump for current compact entry point
static uint16_t EncodeBranchToCentralJump (int16_t offset)
{
    _ASSERTE (offset >= 0 && (offset % TEP_ENTRY_SIZE == 0));

    offset += TEP_HALF_ENTRY_SIZE - PC_REG_RELATIVE_OFFSET;

    uint16_t instr;
    emitUnconditionalBranchThumb ((LPBYTE) &instr, offset);

    return instr;
}

#endif // DACCESS_COMPILE

#else // TARGET_ARM

#define TEP_MAX_BEFORE_INDEX    (1 + (127 / TEP_ENTRY_SIZE))
#define TEP_MAX_BLOCK_INDEX     (TEP_MAX_BEFORE_INDEX + (128 - TEP_CENTRAL_JUMP_SIZE) / TEP_ENTRY_SIZE)
#define TEP_FULL_BLOCK_SIZE     (TEP_MAX_BLOCK_INDEX * TEP_ENTRY_SIZE + TEP_CENTRAL_JUMP_SIZE)

#endif // TARGET_ARM

BOOL MethodDescChunk::IsCompactEntryPointAtAddress(PCODE addr)
{
    LIMITED_METHOD_DAC_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64)

    // Compact entrypoints start at odd addresses
    return (addr & 1) != 0;

#elif defined(TARGET_ARM)

    // Compact entrypoints start at odd addresses (thumb) with second bit set to 1
    uint8_t compactEntryPointMask = THUMB_CODE | COMPACT_ENTRY_ARM_CODE;
    return (addr & compactEntryPointMask) == compactEntryPointMask;

#else
    #error Unsupported platform
#endif
}

//*******************************************************************************
/* static */ MethodDesc* MethodDescChunk::GetMethodDescFromCompactEntryPoint(PCODE addr, BOOL fSpeculative /*=FALSE*/)
{
    LIMITED_METHOD_CONTRACT;

#ifdef DACCESS_COMPILE
    // Always use speculative checks with DAC
    fSpeculative = TRUE;
#endif

    // Always do consistency check in debug
    if (fSpeculative INDEBUG(|| TRUE))
    {
#ifdef TARGET_ARM
        TADDR instrCodeAddr = PCODEToPINSTR(addr);
        if (!IsCompactEntryPointAtAddress(addr) ||
            *PTR_BYTE(instrCodeAddr) != TEP_ENTRY_INSTR1_BYTE1 ||
            *PTR_BYTE(instrCodeAddr+1) != TEP_ENTRY_INSTR1_BYTE2)
#else // TARGET_ARM
        if ((addr & 3) != 1 ||
            *PTR_BYTE(addr) != X86_INSTR_MOV_AL ||
            *PTR_BYTE(addr+2) != X86_INSTR_JMP_REL8)
#endif // TARGET_ARM
        {
            if (fSpeculative) return NULL;
            _ASSERTE(!"Unexpected code in temporary entrypoint");
        }
    }

#ifdef TARGET_ARM

    // On ARM compact entry points are thumb
    _ASSERTE ((addr & THUMB_CODE) != 0);
    addr = addr - THUMB_CODE;

    // Get offset for CentralJumpCode from current compact entry point
    PTR_UINT16 pBranchInstr = (PTR_UINT16(addr)) + 1;
    uint16_t offset = DecodeOffsetFromBranchToCentralJump (*pBranchInstr);

    TADDR centralJump = addr + offset;
    int index = (centralJump - addr - TEP_ENTRY_SIZE) / TEP_ENTRY_SIZE;

#else // TARGET_ARM

    int index = *PTR_BYTE(addr+1);
    TADDR centralJump = addr + 4 + *PTR_SBYTE(addr+3);

#endif // TARGET_ARM

    CentralJumpCode* pCentralJumpCode = PTR_CentralJumpCode(centralJump);

    // Always do consistency check in debug
    if (fSpeculative INDEBUG(|| TRUE))
    {
        SIZE_T i;
        for (i = 0; i < TEP_CENTRAL_JUMP_SIZE; i++)
        {
            BYTE b = ((BYTE*)&c_CentralJumpCode)[i];
            if (b != 0 && b != *PTR_BYTE(centralJump+i))
            {
                if (fSpeculative) return NULL;
                _ASSERTE(!"Unexpected code in temporary entrypoint");
            }
        }

#ifdef TARGET_ARM

        _ASSERTE_IMPL(pCentralJumpCode->CheckTarget(GetPreStubCompactARMEntryPoint()));

#else // TARGET_ARM

        _ASSERTE_IMPL(pCentralJumpCode->CheckTarget(GetPreStubEntryPoint()));

#endif // TARGET_ARM
    }

#ifdef TARGET_ARM
    // Go through all MethodDesc in MethodDescChunk and find the one with the required index
    PTR_MethodDescChunk pChunk = *((DPTR(PTR_MethodDescChunk))(centralJump + offsetof(CentralJumpCode, m_pChunk)));
    TADDR pMD = PTR_HOST_TO_TADDR (pChunk->GetFirstMethodDesc ());

    _ASSERTE (index >= 0 && index < ((int) pChunk->GetCount ()));

    index = ((int) pChunk->GetCount ()) - 1 - index;

    SIZE_T totalSize = 0;
    int curIndex = 0;

    while (index != curIndex)
    {
        SIZE_T sizeCur = (PTR_MethodDesc (pMD))->SizeOf ();
        totalSize += sizeCur;

        pMD += sizeCur;
        ++curIndex;
    }

    return PTR_MethodDesc (pMD);
#else // TARGET_ARM
    return PTR_MethodDesc((TADDR)pCentralJumpCode->m_pBaseMD + index * MethodDesc::ALIGNMENT);
#endif // TARGET_ARM
}

//*******************************************************************************
SIZE_T MethodDescChunk::SizeOfCompactEntryPoints(int count)
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef TARGET_ARM

    return COMPACT_ENTRY_ARM_CODE + count * TEP_ENTRY_SIZE + TEP_CENTRAL_JUMP_SIZE;

#else // TARGET_ARM

    int fullBlocks = count / TEP_MAX_BLOCK_INDEX;
    int remainder = count % TEP_MAX_BLOCK_INDEX;

    return 1 + (fullBlocks * TEP_FULL_BLOCK_SIZE) +
        (remainder * TEP_ENTRY_SIZE) + ((remainder != 0) ? TEP_CENTRAL_JUMP_SIZE : 0);

#endif // TARGET_ARM
}

#ifndef DACCESS_COMPILE
TADDR MethodDescChunk::AllocateCompactEntryPoints(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    int count = GetCount();

    SIZE_T size = SizeOfCompactEntryPoints(count);

    TADDR temporaryEntryPoints = (TADDR)pamTracker->Track(pLoaderAllocator->GetPrecodeHeap()->AllocAlignedMem(size, sizeof(TADDR)));
    ExecutableWriterHolder<void> temporaryEntryPointsWriterHolder((void *)temporaryEntryPoints, size);
    size_t rxOffset = temporaryEntryPoints - (TADDR)temporaryEntryPointsWriterHolder.GetRW();

#ifdef TARGET_ARM
    BYTE* p = (BYTE*)temporaryEntryPointsWriterHolder.GetRW() + COMPACT_ENTRY_ARM_CODE;
    int relOffset        = count * TEP_ENTRY_SIZE - TEP_ENTRY_SIZE; // relative offset for the short jump

    _ASSERTE (relOffset < MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB);
#else // TARGET_ARM
    // make the temporary entrypoints unaligned, so they are easy to identify
    BYTE* p = (BYTE*)temporaryEntryPointsWriterHolder.GetRW() + 1;
    int indexInBlock     = TEP_MAX_BLOCK_INDEX;         // recompute relOffset in first iteration
    int relOffset        = 0;                           // relative offset for the short jump
#endif // TARGET_ARM

    MethodDesc * pBaseMD = 0;                   // index of the start of the block

    MethodDesc * pMD = GetFirstMethodDesc();
    for (int index = 0; index < count; index++)
    {
#ifdef TARGET_ARM

        uint8_t *pMovInstrByte1 = (uint8_t *)p;
        uint8_t *pMovInstrByte2 = (uint8_t *)p+1;
        uint16_t *pBranchInstr = ((uint16_t *)p)+1;

        *pMovInstrByte1 = TEP_ENTRY_INSTR1_BYTE1;
        *pMovInstrByte2 = TEP_ENTRY_INSTR1_BYTE2;
        *pBranchInstr = EncodeBranchToCentralJump ((int16_t) relOffset);

        p += TEP_ENTRY_SIZE;

#else // TARGET_ARM

        if (indexInBlock == TEP_MAX_BLOCK_INDEX)
        {
            relOffset = (min(count - index, TEP_MAX_BEFORE_INDEX) - 1) * TEP_ENTRY_SIZE;
            indexInBlock = 0;
            pBaseMD = pMD;
        }

        *(p+0) = X86_INSTR_MOV_AL;
        int methodDescIndex = pMD->GetMethodDescIndex() - pBaseMD->GetMethodDescIndex();
        _ASSERTE(FitsInU1(methodDescIndex));
        *(p+1) = (BYTE)methodDescIndex;

        *(p+2) = X86_INSTR_JMP_REL8;
        _ASSERTE(FitsInI1(relOffset));
        *(p+3) = (BYTE)relOffset;

        p += TEP_ENTRY_SIZE; static_assert_no_msg(TEP_ENTRY_SIZE == 4);

        if (relOffset == 0)
        {
            CentralJumpCode* pCode = (CentralJumpCode*)p;
            CentralJumpCode* pCodeRX = (CentralJumpCode*)(p + rxOffset);

            memcpy(pCode, &c_CentralJumpCode, TEP_CENTRAL_JUMP_SIZE);

            pCode->Setup(pCodeRX, pBaseMD, GetPreStubEntryPoint(), pLoaderAllocator);

            p += TEP_CENTRAL_JUMP_SIZE;

            relOffset -= TEP_CENTRAL_JUMP_SIZE;
        }

        indexInBlock++;

#endif // TARGET_ARM

        relOffset -= TEP_ENTRY_SIZE;
        pMD = (MethodDesc *)((BYTE *)pMD + pMD->SizeOf());
    }

#ifdef TARGET_ARM

    CentralJumpCode* pCode = (CentralJumpCode*)p;
    memcpy(pCode, &c_CentralJumpCode, TEP_CENTRAL_JUMP_SIZE);
    pCode->Setup (GetPreStubCompactARMEntryPoint(), this);

    _ASSERTE(p + TEP_CENTRAL_JUMP_SIZE == (BYTE*)temporaryEntryPointsWriterHolder.GetRW() + size);

#else // TARGET_ARM

    _ASSERTE(p == (BYTE*)temporaryEntryPointsWriterHolder.GetRW() + size);

#endif // TARGET_ARM

    ClrFlushInstructionCache((LPVOID)temporaryEntryPoints, size);

    SetHasCompactEntryPoints();
    return temporaryEntryPoints;
}
#endif // !DACCESS_COMPILE

#endif // HAS_COMPACT_ENTRYPOINTS

//*******************************************************************************
PCODE MethodDescChunk::GetTemporaryEntryPoint(int index)
{
    LIMITED_METHOD_CONTRACT;

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (HasCompactEntryPoints())
    {
#ifdef TARGET_ARM

        return GetTemporaryEntryPoints() + COMPACT_ENTRY_ARM_CODE + THUMB_CODE + index * TEP_ENTRY_SIZE;

#else // TARGET_ARM

        int fullBlocks = index / TEP_MAX_BLOCK_INDEX;
        int remainder = index % TEP_MAX_BLOCK_INDEX;

        return GetTemporaryEntryPoints() + 1 + (fullBlocks * TEP_FULL_BLOCK_SIZE) +
            (remainder * TEP_ENTRY_SIZE) + ((remainder >= TEP_MAX_BEFORE_INDEX) ? TEP_CENTRAL_JUMP_SIZE : 0);

#endif // TARGET_ARM
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    return Precode::GetPrecodeForTemporaryEntryPoint(GetTemporaryEntryPoints(), index)->GetEntryPoint();
}

PCODE MethodDesc::GetTemporaryEntryPoint()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDescChunk* pChunk = GetMethodDescChunk();
    int lo = 0, hi = pChunk->GetCount() - 1;

    // Find the temporary entrypoint in the chunk by binary search
    while (lo < hi)
    {
        int mid = (lo + hi) / 2;

        TADDR pEntryPoint = pChunk->GetTemporaryEntryPoint(mid);

        MethodDesc * pMD = MethodDesc::GetMethodDescFromStubAddr(pEntryPoint);
        if (PTR_HOST_TO_TADDR(this) == PTR_HOST_TO_TADDR(pMD))
            return pEntryPoint;

        if (PTR_HOST_TO_TADDR(this) > PTR_HOST_TO_TADDR(pMD))
            lo = mid + 1;
        else
            hi = mid - 1;
    }

    _ASSERTE(lo == hi);

    TADDR pEntryPoint = pChunk->GetTemporaryEntryPoint(lo);

#ifdef _DEBUG
    MethodDesc * pMD = MethodDesc::GetMethodDescFromStubAddr(pEntryPoint);
    _ASSERTE(PTR_HOST_TO_TADDR(this) == PTR_HOST_TO_TADDR(pMD));
#endif

    return pEntryPoint;
}

#ifndef DACCESS_COMPILE
//*******************************************************************************
void MethodDesc::SetTemporaryEntryPoint(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    WRAPPER_NO_CONTRACT;

    GetMethodDescChunk()->EnsureTemporaryEntryPointsCreated(pLoaderAllocator, pamTracker);

    PTR_PCODE pSlot = GetAddrOfSlot();
    _ASSERTE(*pSlot == NULL);
    *pSlot = GetTemporaryEntryPoint();

    if (RequiresStableEntryPoint())
    {
        // The rest of the system assumes that certain methods always have stable entrypoints.
        // Create them now.
        GetOrCreatePrecode();
    }
}

//*******************************************************************************
void MethodDescChunk::CreateTemporaryEntryPoints(LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(GetTemporaryEntryPoints() == NULL);

    TADDR temporaryEntryPoints = Precode::AllocateTemporaryEntryPoints(this, pLoaderAllocator, pamTracker);

#ifdef HAS_COMPACT_ENTRYPOINTS
    // Precodes allocated only if they provide more compact representation or if it is required
    if (temporaryEntryPoints == NULL)
    {
        temporaryEntryPoints = AllocateCompactEntryPoints(pLoaderAllocator, pamTracker);
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    *(((TADDR *)this)-1) = temporaryEntryPoints;

    _ASSERTE(GetTemporaryEntryPoints() != NULL);
}

//*******************************************************************************
void MethodDesc::InterlockedUpdateFlags2(BYTE bMask, BOOL fSet)
{
    WRAPPER_NO_CONTRACT;

    ULONG* pLong = (ULONG*)(&m_bFlags2 - 3);
    static_assert_no_msg(offsetof(MethodDesc, m_bFlags2) % sizeof(LONG) == 3);

#if BIGENDIAN
    if (fSet)
        FastInterlockOr(pLong, (ULONG)bMask);
    else
        FastInterlockAnd(pLong, ~(ULONG)bMask);
#else // !BIGENDIAN
    if (fSet)
        FastInterlockOr(pLong, (ULONG)bMask << (3 * 8));
    else
        FastInterlockAnd(pLong, ~((ULONG)bMask << (3 * 8)));
#endif // !BIGENDIAN
}

//*******************************************************************************
Precode* MethodDesc::GetOrCreatePrecode()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!IsVersionableWithVtableSlotBackpatch());

    if (HasPrecode())
    {
        return GetPrecode();
    }

    PTR_PCODE pSlot = GetAddrOfSlot();
    PCODE tempEntry = GetTemporaryEntryPoint();

    PrecodeType requiredType = GetPrecodeType();
    PrecodeType availableType = PRECODE_INVALID;

    if (!GetMethodDescChunk()->HasCompactEntryPoints())
    {
        availableType = Precode::GetPrecodeFromEntryPoint(tempEntry)->GetType();
    }

    // Allocate the precode if necessary
    if (requiredType != availableType)
    {
        // code:Precode::AllocateTemporaryEntryPoints should always create precode of the right type for dynamic methods.
        // If we took this path for dynamic methods, the precode may leak since we may allocate it in domain-neutral loader heap.
        _ASSERTE(!IsLCGMethod());

        AllocMemTracker amt;
        Precode* pPrecode = Precode::Allocate(requiredType, this, GetLoaderAllocator(), &amt);


        if (FastInterlockCompareExchangePointer(pSlot, pPrecode->GetEntryPoint(), tempEntry) == tempEntry)
            amt.SuppressRelease();
    }

    // Set the flags atomically
    InterlockedUpdateFlags2(enum_flag2_HasStableEntryPoint | enum_flag2_HasPrecode, TRUE);

    return Precode::GetPrecodeFromEntryPoint(*pSlot);
}

bool MethodDesc::DetermineAndSetIsEligibleForTieredCompilation()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_TIERED_COMPILATION
#ifndef FEATURE_CODE_VERSIONING
    #error Tiered compilation requires code versioning
#endif

    // Keep in-sync with MethodTableBuilder::NeedsNativeCodeSlot(bmtMDMethod * pMDMethod)
    // to ensure native slots are available where needed.
    if (
        // Policy
        g_pConfig->TieredCompilation() &&

        // Functional requirement - The NativeCodeSlot is required to hold the code pointer for the default code version because
        // the method's entry point slot will point to a precode or to the current code entry point
        HasNativeCodeSlot() &&

        // Functional requirement - These methods have no IL that could be optimized
        !IsWrapperStub() &&

        // Functional requirement
        CodeVersionManager::IsMethodSupported(this) &&

        // Policy - If QuickJit is disabled and the module does not have any pregenerated code, the method would effectively not
        // be tiered currently, so make the method ineligible for tiering to avoid some unnecessary overhead
        (g_pConfig->TieredCompilation_QuickJit() || GetModule()->IsReadyToRun()) &&

        // Policy - Generating optimized code is not disabled
        !IsJitOptimizationDisabled() &&

        // Policy - Tiered compilation is not disabled by the profiler
        !CORProfilerDisableTieredCompilation())
    {
        m_bFlags2 |= enum_flag2_IsEligibleForTieredCompilation;
        _ASSERTE(IsVersionable());
        return true;
    }
#endif

    return false;
}

#endif // !DACCESS_COMPILE

bool MethodDesc::IsJitOptimizationDisabled()
{
    WRAPPER_NO_CONTRACT;

    return
        g_pConfig->JitMinOpts() ||
#ifdef _DEBUG
        g_pConfig->GenDebuggableCode() ||
#endif
        CORDisableJITOptimizations(GetModule()->GetDebuggerInfoBits()) ||
        (!IsNoMetadata() && IsMiNoOptimization(GetImplAttrs()));
}

#ifndef DACCESS_COMPILE

void MethodDesc::RecordAndBackpatchEntryPointSlot(
    LoaderAllocator *slotLoaderAllocator, // the loader allocator from which the slot's memory is allocated
    TADDR slot,
    EntryPointSlots::SlotType slotType)
{
    WRAPPER_NO_CONTRACT;

    GCX_PREEMP();

    LoaderAllocator *mdLoaderAllocator = GetLoaderAllocator();
    MethodDescBackpatchInfoTracker::ConditionalLockHolderForGCCoop slotBackpatchLockHolder;

    RecordAndBackpatchEntryPointSlot_Locked(
        mdLoaderAllocator,
        slotLoaderAllocator,
        slot,
        slotType,
        GetEntryPointToBackpatch_Locked());
}

// This function tries to record a slot that would contain an entry point for the method, and backpatches the slot to contain
// method's current entry point. Once recorded, changes to the entry point due to tiering will cause the slot to be backpatched
// as necessary.
void MethodDesc::RecordAndBackpatchEntryPointSlot_Locked(
    LoaderAllocator *mdLoaderAllocator,
    LoaderAllocator *slotLoaderAllocator, // the loader allocator from which the slot's memory is allocated
    TADDR slot,
    EntryPointSlots::SlotType slotType,
    PCODE currentEntryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
    _ASSERTE(mdLoaderAllocator != nullptr);
    _ASSERTE(mdLoaderAllocator == GetLoaderAllocator());
    _ASSERTE(slotLoaderAllocator != nullptr);
    _ASSERTE(slot != NULL);
    _ASSERTE(slotType < EntryPointSlots::SlotType_Count);
    _ASSERTE(MayHaveEntryPointSlotsToBackpatch());

    // The specified current entry point must actually be *current* in the sense that it must have been retrieved inside the
    // lock, such that a recorded slot is guaranteed to point to the entry point at the time at which it was recorded, in order
    // to synchronize with backpatching in MethodDesc::BackpatchEntryPointSlots(). If a slot pointing to an older entry point
    // were to be recorded due to concurrency issues, it would not get backpatched to point to the more recent, actually
    // current, entry point until another entry point change, which may never happen.
    _ASSERTE(currentEntryPoint == GetEntryPointToBackpatch_Locked());

    MethodDescBackpatchInfoTracker *backpatchTracker = mdLoaderAllocator->GetMethodDescBackpatchInfoTracker();
    backpatchTracker->AddSlotAndPatch_Locked(this, slotLoaderAllocator, slot, slotType, currentEntryPoint);
}

FORCEINLINE bool MethodDesc::TryBackpatchEntryPointSlots(
    PCODE entryPoint,
    bool isPrestubEntryPoint,
    bool onlyFromPrestubEntryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(MayHaveEntryPointSlotsToBackpatch());
    _ASSERTE(entryPoint != NULL);
    _ASSERTE(isPrestubEntryPoint == (entryPoint == GetPrestubEntryPointToBackpatch()));
    _ASSERTE(!isPrestubEntryPoint || !onlyFromPrestubEntryPoint);
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());

    LoaderAllocator *mdLoaderAllocator = GetLoaderAllocator();
    MethodDescBackpatchInfoTracker *backpatchInfoTracker = mdLoaderAllocator->GetMethodDescBackpatchInfoTracker();

    // Get the entry point to backpatch inside the lock to synchronize with backpatching in MethodDesc::DoBackpatch()
    PCODE previousEntryPoint = GetEntryPointToBackpatch_Locked();
    if (previousEntryPoint == entryPoint)
    {
        return true;
    }

    if (onlyFromPrestubEntryPoint && previousEntryPoint != GetPrestubEntryPointToBackpatch())
    {
        return false;
    }

    if (IsVersionableWithVtableSlotBackpatch())
    {
        // Backpatch the func ptr stub if it was created
        FuncPtrStubs *funcPtrStubs = mdLoaderAllocator->GetFuncPtrStubsNoCreate();
        if (funcPtrStubs != nullptr)
        {
            Precode *funcPtrPrecode = funcPtrStubs->Lookup(this);
            if (funcPtrPrecode != nullptr)
            {
                if (isPrestubEntryPoint)
                {
                    funcPtrPrecode->ResetTargetInterlocked();
                }
                else
                {
                    funcPtrPrecode->SetTargetInterlocked(entryPoint, FALSE /* fOnlyRedirectFromPrestub */);
                }
            }
        }
    }

    backpatchInfoTracker->Backpatch_Locked(this, entryPoint);

    // Set the entry point to backpatch inside the lock to synchronize with backpatching in MethodDesc::DoBackpatch(), and set
    // it last in case there are exceptions above, as setting the entry point indicates that all recorded slots have been
    // backpatched
    SetEntryPointToBackpatch_Locked(entryPoint);
    return true;
}

void MethodDesc::TrySetInitialCodeEntryPointForVersionableMethod(
    PCODE entryPoint,
    bool mayHaveEntryPointSlotsToBackpatch)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(entryPoint != NULL);
    _ASSERTE(IsVersionable());
    _ASSERTE(mayHaveEntryPointSlotsToBackpatch == MayHaveEntryPointSlotsToBackpatch());

    if (mayHaveEntryPointSlotsToBackpatch)
    {
        TryBackpatchEntryPointSlotsFromPrestub(entryPoint);
    }
    else
    {
        _ASSERTE(IsVersionableWithPrecode());
        GetOrCreatePrecode()->SetTargetInterlocked(entryPoint, TRUE /* fOnlyRedirectFromPrestub */);
    }
}

void MethodDesc::SetCodeEntryPoint(PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(entryPoint != NULL);

    if (MayHaveEntryPointSlotsToBackpatch())
    {
        BackpatchEntryPointSlots(entryPoint);
    }
    else if (IsVersionable())
    {
        _ASSERTE(IsVersionableWithPrecode());
        GetOrCreatePrecode()->SetTargetInterlocked(entryPoint, FALSE /* fOnlyRedirectFromPrestub */);

        // SetTargetInterlocked() would return false if it lost the race with another thread. That is fine, this thread
        // can continue assuming it was successful, similarly to it successfully updating the target and another thread
        // updating the target again shortly afterwards.
    }
    else if (HasPrecode())
    {
        GetPrecode()->SetTargetInterlocked(entryPoint);
    }
    else if (!HasStableEntryPoint())
    {
        SetStableEntryPointInterlocked(entryPoint);
    }
}

void MethodDesc::ResetCodeEntryPoint()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsVersionable());

    if (MayHaveEntryPointSlotsToBackpatch())
    {
        BackpatchToResetEntryPointSlots();
        return;
    }

    _ASSERTE(IsVersionableWithPrecode());
    if (HasPrecode())
    {
        GetPrecode()->ResetTargetInterlocked();
    }
}

void MethodDesc::ResetCodeEntryPointForEnC()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!IsVersionable());
    _ASSERTE(!IsVersionableWithPrecode());
    _ASSERTE(!MayHaveEntryPointSlotsToBackpatch());

    if (HasPrecode())
    {
        GetPrecode()->ResetTargetInterlocked();
    }

    if (HasNativeCodeSlot())
    {
        *GetAddrOfNativeCodeSlot() = NULL;
    }
}


//*******************************************************************************
BOOL MethodDesc::SetNativeCodeInterlocked(PCODE addr, PCODE pExpected /*=NULL*/)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(!IsDefaultInterfaceMethod() || HasNativeCodeSlot());

    if (HasNativeCodeSlot())
    {
#ifdef TARGET_ARM
        _ASSERTE(IsThumbCode(addr) || (addr==NULL));
        addr &= ~THUMB_CODE;

        if (pExpected != NULL)
        {
            _ASSERTE(IsThumbCode(pExpected));
            pExpected &= ~THUMB_CODE;
        }
#endif

        PTR_PCODE pSlot = GetAddrOfNativeCodeSlot();
        NativeCodeSlot expected;

        expected = *pSlot;

        return FastInterlockCompareExchangePointer(reinterpret_cast<TADDR*>(pSlot),
            (TADDR&)addr, (TADDR&)expected) == (TADDR&)expected;
    }

    _ASSERTE(pExpected == NULL);
    return SetStableEntryPointInterlocked(addr);
}


//*******************************************************************************
void MethodDesc::SetMethodEntryPoint(PCODE addr)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(addr != NULL);

    // Similarly to GetMethodEntryPoint(), it is up to the caller to ensure that calls to this function are appropriately
    // synchronized. Currently, the only caller synchronizes with the following lock.
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());

    *GetAddrOfSlot() = addr;
}


//*******************************************************************************
BOOL MethodDesc::SetStableEntryPointInterlocked(PCODE addr)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(!HasPrecode());
    _ASSERTE(!IsVersionable());

    PCODE pExpected = GetTemporaryEntryPoint();
    PTR_PCODE pSlot = GetAddrOfSlot();

    BOOL fResult = FastInterlockCompareExchangePointer(pSlot, addr, pExpected) == pExpected;

    InterlockedUpdateFlags2(enum_flag2_HasStableEntryPoint, TRUE);

    return fResult;
}

BOOL NDirectMethodDesc::ComputeMarshalingRequired()
{
    WRAPPER_NO_CONTRACT;

    return NDirect::MarshalingRequired(this);
}

/**********************************************************************************/
// Forward declare the NDirectImportWorker function - See dllimport.cpp
EXTERN_C LPVOID STDCALL NDirectImportWorker(NDirectMethodDesc*);
void *NDirectMethodDesc::ResolveAndSetNDirectTarget(_In_ NDirectMethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END

// This build conditional is here due to dllimport.cpp
// not being relevant during the crossgen build.
    LPVOID targetMaybe = NDirectImportWorker(pMD);
    _ASSERTE(targetMaybe != nullptr);
    pMD->SetNDirectTarget(targetMaybe);
    return targetMaybe;

}

BOOL NDirectMethodDesc::TryResolveNDirectTargetForNoGCTransition(_In_ MethodDesc* pMD, _Out_ void** ndirectTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(ndirectTarget));
    }
    CONTRACTL_END

    if (!pMD->ShouldSuppressGCTransition())
        return FALSE;

    _ASSERTE(pMD->IsNDirect());
    *ndirectTarget = ResolveAndSetNDirectTarget((NDirectMethodDesc*)pMD);
    return TRUE;

}

//*******************************************************************************
void NDirectMethodDesc::InterlockedSetNDirectFlags(WORD wFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    // Since InterlockedCompareExchange only works on ULONGs,
    // we'll have to operate on the entire ULONG. Ugh.

    WORD *pFlags = &ndirect.m_wFlags;

    // Make sure that m_flags is aligned on a 4 byte boundry
    _ASSERTE( ( ((size_t) pFlags) & (sizeof(ULONG)-1) ) == 0);

    // Ensure we won't be reading or writing outside the bounds of the NDirectMethodDesc.
    _ASSERTE((BYTE*)pFlags >= (BYTE*)this);
    _ASSERTE((BYTE*)pFlags+sizeof(ULONG) <= (BYTE*)(this+1));

    DWORD dwMask = 0;

    // Set the flags in the mask
    ((WORD*)&dwMask)[0] |= wFlags;

    // Now, slam all 32 bits atomically.
    FastInterlockOr((DWORD*)pFlags, dwMask);
}


#ifdef TARGET_WINDOWS
FARPROC NDirectMethodDesc::FindEntryPointWithMangling(NATIVE_LIBRARY_HANDLE hMod, PTR_CUTF8 entryPointName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FARPROC pFunc = GetProcAddress(hMod, entryPointName);

#if defined(TARGET_X86)

    if (pFunc)
    {
        return pFunc;
    }

    if (IsStdCall())
    {
        EnsureStackArgumentSize();

        DWORD probedEntrypointNameLength = (DWORD)(strlen(entryPointName) + 1); // 1 for null terminator
        int dstbufsize = (int)(sizeof(char) * (probedEntrypointNameLength + 10)); // 10 for stdcall mangling
        LPSTR szProbedEntrypointName = ((LPSTR)_alloca(dstbufsize + 1));
        szProbedEntrypointName[0] = '_';
        strcpy_s(szProbedEntrypointName + 1, dstbufsize, entryPointName);
        szProbedEntrypointName[probedEntrypointNameLength] = '\0'; // Add an extra '\0'.

        UINT16 numParamBytesMangle = GetStackArgumentSize();

        sprintf_s(szProbedEntrypointName + probedEntrypointNameLength, dstbufsize - probedEntrypointNameLength + 1, "@%lu", (ULONG)numParamBytesMangle);
        pFunc = GetProcAddress(hMod, szProbedEntrypointName);
    }

#endif

    return pFunc;
}

FARPROC NDirectMethodDesc::FindEntryPointWithSuffix(NATIVE_LIBRARY_HANDLE hMod, PTR_CUTF8 entryPointName, char suffix)
{
    // Allocate space for a copy of the entry point name.
    DWORD entryPointWithSuffixLen = (DWORD)(strlen(entryPointName) + 1); // +1 for charset decorations
    int dstbufsize = (int)(sizeof(char) * (entryPointWithSuffixLen + 1)); // +1 for the null terminator
    LPSTR entryPointWithSuffix = ((LPSTR)_alloca(dstbufsize));

    // Copy the name so we can mangle it.
    strcpy_s(entryPointWithSuffix, dstbufsize, entryPointName);
    entryPointWithSuffix[entryPointWithSuffixLen] = '\0'; // Null terminator
    entryPointWithSuffix[entryPointWithSuffixLen - 1] = suffix; // Charset suffix

    // Look for entry point with the suffix based on charset
    return FindEntryPointWithMangling(hMod, entryPointWithSuffix);
}

#endif

//*******************************************************************************
LPVOID NDirectMethodDesc::FindEntryPoint(NATIVE_LIBRARY_HANDLE hMod)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    char const * funcName = GetEntrypointName();

#ifndef TARGET_WINDOWS
    return reinterpret_cast<LPVOID>(PAL_GetProcAddressDirect(hMod, funcName));
#else
    // Handle ordinals.
    if (funcName[0] == '#')
    {
        long ordinal = atol(funcName + 1);
        return reinterpret_cast<LPVOID>(GetProcAddress(hMod, (LPCSTR)(size_t)((UINT16)ordinal)));
    }

    FARPROC pFunc = NULL;
    if (IsNativeNoMangled())
    {
        // Look for the user-provided entry point name only
        pFunc = FindEntryPointWithMangling(hMod, funcName);
    }
    else if (IsNativeAnsi())
    {
        // For ANSI, look for the user-provided entry point name first.
        // If that does not exist, try the charset suffix.
        pFunc = FindEntryPointWithMangling(hMod, funcName);
        if (pFunc == NULL)
            pFunc = FindEntryPointWithSuffix(hMod, funcName, 'A');
    }
    else
    {
        // For Unicode, look for the entry point name with the charset suffix first.
        // The 'W' API takes precedence over the undecorated one.
        pFunc = FindEntryPointWithSuffix(hMod, funcName, 'W');
        if (pFunc == NULL)
            pFunc = FindEntryPointWithMangling(hMod, funcName);
    }

    return reinterpret_cast<LPVOID>(pFunc);
#endif
}

#if defined(TARGET_X86)
//*******************************************************************************
void NDirectMethodDesc::EnsureStackArgumentSize()
{
    STANDARD_VM_CONTRACT;

    if (ndirect.m_cbStackArgumentSize == 0xFFFF)
    {
        // Marshalling required check sets the stack size as side-effect when marshalling is not required.
        if (MarshalingRequired())
        {
            // Generating interop stub sets the stack size as side-effect in all cases
            GetStubForInteropMethod(this, NDIRECTSTUB_FL_FOR_NUMPARAMBYTES);
        }
    }
}
#endif


//*******************************************************************************
void NDirectMethodDesc::InitEarlyBoundNDirectTarget()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    _ASSERTE(IsEarlyBound());

    if (IsClassConstructorTriggeredAtLinkTime())
    {
        GetMethodTable()->CheckRunClassInitThrowing();
    }

    const void *target = GetModule()->GetInternalPInvokeTarget(GetRVA());
    _ASSERTE(target != 0);

    if (HeuristicDoesThisLookLikeAGetLastErrorCall((LPBYTE)target))
        target = (BYTE*)FalseGetLastError;

    // As long as we've set the NDirect target field we don't need to backpatch the import thunk glue.
    // All NDirect calls all through the NDirect target, so if it's updated, then we won't go into
    // NDirectImportThunk().  In fact, backpatching the import thunk glue leads to race conditions.
    SetNDirectTarget((LPVOID)target);
}

//*******************************************************************************
BOOL MethodDesc::HasUnmanagedCallersOnlyAttribute()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (IsILStub())
    {
        return AsDynamicMethodDesc()->IsUnmanagedCallersOnlyStub();
    }

    HRESULT hr = GetCustomAttribute(
        WellKnownAttribute::UnmanagedCallersOnly,
        nullptr,
        nullptr);
    if (hr != S_OK)
    {
        // See https://github.com/dotnet/runtime/issues/37622
        hr = GetCustomAttribute(
            WellKnownAttribute::NativeCallableInternal,
            nullptr,
            nullptr);
    }

    return (hr == S_OK) ? TRUE : FALSE;
}

//*******************************************************************************
BOOL MethodDesc::ShouldSuppressGCTransition()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    MethodDesc* tgt = nullptr;
    if (IsNDirect())
    {
        tgt = this;
    }
    else if (IsILStub())
    {
        // From the IL stub, determine if the actual target has been
        // marked to suppress the GC transition.
        PTR_DynamicMethodDesc ilStubMD = AsDynamicMethodDesc();
        PTR_ILStubResolver ilStubResolver = ilStubMD->GetILStubResolver();
        tgt = ilStubResolver->GetStubTargetMethodDesc();

        // In the event we can't get or don't have a target, there is no way
        // to determine if we should suppress the GC transition.
        if (tgt == nullptr)
            return FALSE;
    }
    else
    {
        return FALSE;
    }

    _ASSERTE(tgt != nullptr);
    bool suppressGCTransition;
    NDirect::GetCallingConvention_IgnoreErrors(tgt, NULL /*callConv*/, &suppressGCTransition);
    return suppressGCTransition ? TRUE : FALSE;
}

#ifdef FEATURE_COMINTEROP
//*******************************************************************************
void ComPlusCallMethodDesc::InitComEventCallInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    MethodTable *pItfMT = GetInterfaceMethodTable();
    MethodDesc *pItfMD = this;
    MethodTable *pSrcItfClass = NULL;
    MethodTable *pEvProvClass = NULL;

    // Retrieve the event provider class.
    WORD cbExtraSlots = ComMethodTable::GetNumExtraSlots(pItfMT->GetComInterfaceType());
    WORD itfSlotNum = (WORD) m_pComPlusCallInfo->m_cachedComSlot - cbExtraSlots;
    pItfMT->GetEventInterfaceInfo(&pSrcItfClass, &pEvProvClass);
    m_pComPlusCallInfo->m_pEventProviderMD = MemberLoader::FindMethodForInterfaceSlot(pEvProvClass, pItfMT, itfSlotNum);

    // If we could not find the method, then the event provider does not support
    // this event. This is a fatal error.
    if (!m_pComPlusCallInfo->m_pEventProviderMD)
    {
        // Init the interface MD for error reporting.
        pItfMD = (ComPlusCallMethodDesc*)pItfMT->GetMethodDescForSlot(itfSlotNum);

        // Retrieve the event provider class name.
        StackSString ssEvProvClassName;
        pEvProvClass->_GetFullyQualifiedNameForClass(ssEvProvClassName);

        // Retrieve the COM event interface class name.
        StackSString ssEvItfName;
        pItfMT->_GetFullyQualifiedNameForClass(ssEvItfName);

        // Convert the method name to unicode.
        StackSString ssMethodName(SString::Utf8, pItfMD->GetName());

        // Throw the exception.
        COMPlusThrow(kTypeLoadException, IDS_EE_METHOD_NOT_FOUND_ON_EV_PROV,
                     ssMethodName.GetUnicode(), ssEvItfName.GetUnicode(), ssEvProvClassName.GetUnicode());
    }
}
#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE


#ifdef DACCESS_COMPILE

//*******************************************************************************
void
MethodDesc::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    if (DacHasMethodDescBeenEnumerated(this))
    {
        return;
    }

    // Save away the whole MethodDescChunk as in many
    // places RecoverChunk is called on a method desc so
    // the whole chunk must be available.  This also
    // automatically picks up any prestubs and such.
    GetMethodDescChunk()->EnumMemoryRegions(flags);

    if (HasPrecode())
    {
        GetPrecode()->EnumMemoryRegions(flags);
    }

    // Need to save the Debug-Info for this method so that we can see it in a debugger later.
    DebugInfoManager::EnumMemoryRegionsForMethodDebugInfo(flags, this);

    if (!IsNoMetadata() ||IsILStub())
    {
        // The assembling of the string below implicitly dumps the memory we need.

        StackSString str;
        TypeString::AppendMethodInternal(str, this, TypeString::FormatSignature|TypeString::FormatNamespace|TypeString::FormatFullInst);

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
        if (flags == CLRDATA_ENUM_MEM_MINI || flags == CLRDATA_ENUM_MEM_TRIAGE)
        {
            // we want to save just the method name, so truncate at the open paranthesis
            SString::Iterator it = str.Begin();
            if (str.Find(it, W('(')))
            {
                // ensure the symbol ends in "()" to minimize regressions
                // in !analyze assuming the existence of the argument list
                str.Truncate(++it);
                str.Append(W(')'));
            }

            DacMdCacheAddEEName(dac_cast<TADDR>(this), str);
        }
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

        // The module path is used in the output of !clrstack and !pe if the
        // module is not available when the minidump is inspected. By retrieving
        // the path here, the required memory is implicitly dumped.
        Module* pModule = GetModule();
        if (pModule)
        {
            pModule->GetPath();
        }
    }

#ifdef FEATURE_CODE_VERSIONING
    // Make sure the active IL and native code version are in triage dumps.
    CodeVersionManager* pCodeVersionManager = GetCodeVersionManager();
    ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(dac_cast<PTR_MethodDesc>(this));
    if (!ilVersion.IsNull())
    {
        ilVersion.GetActiveNativeCodeVersion(dac_cast<PTR_MethodDesc>(this));
        ilVersion.GetVersionId();
        ilVersion.GetRejitState();
        ilVersion.GetIL();
    }
#endif

    // Also, call DacValidateMD to dump the memory it needs. !clrstack calls
    // DacValidateMD before it retrieves the method name. We don't expect
    // DacValidateMD to fail, but if it does, ignore the failure and try to assemble the
    // string anyway so that clients that don't validate the MD still work.

    DacValidateMD(this);

    DacSetMethodDescEnumerated(this);

}

//*******************************************************************************
void
StoredSigMethodDesc::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    // 'this' already done, see below.
    DacEnumMemoryRegion(GetSigRVA(), m_cSig);
}

//*******************************************************************************
void
MethodDescChunk::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    DAC_CHECK_ENUM_THIS();
    EMEM_OUT(("MEM: %p MethodDescChunk\n", dac_cast<TADDR>(this)));

    DacEnumMemoryRegion(dac_cast<TADDR>(this), SizeOf());

    PTR_MethodTable pMT = GetMethodTable();

    if (pMT.IsValid())
    {
        pMT->EnumMemoryRegions(flags);
    }

    SIZE_T size;

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (HasCompactEntryPoints())
    {
        size = SizeOfCompactEntryPoints(GetCount());
    }
    else
#endif // HAS_COMPACT_ENTRYPOINTS
    {
        size = Precode::SizeOfTemporaryEntryPoints(GetTemporaryEntryPoints(), GetCount());
    }

    DacEnumMemoryRegion(GetTemporaryEntryPoints(), size);

    MethodDesc * pMD = GetFirstMethodDesc();
    MethodDesc * pOldMD = NULL;
    while (pMD != NULL && pMD != pOldMD)
    {
        pOldMD = pMD;
        EX_TRY
        {
            if (pMD->IsMethodImpl())
            {
                pMD->GetMethodImpl()->EnumMemoryRegions(flags);
            }
        }
        EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

        EX_TRY
        {
            if (pMD->HasStoredSig())
            {
                dac_cast<PTR_StoredSigMethodDesc>(pMD)->EnumMemoryRegions(flags);
            }

            // Check whether the next MethodDesc is within the bounds of the current chunks
            TADDR pNext = dac_cast<TADDR>(pMD) + pMD->SizeOf();
            TADDR pEnd = dac_cast<TADDR>(this) + this->SizeOf();

            pMD = (pNext < pEnd) ? PTR_MethodDesc(pNext) : NULL;
        }
        EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED
    }
}

#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
//*******************************************************************************
MethodDesc *MethodDesc::GetInterfaceMD()
{
    CONTRACT (MethodDesc*) {
        THROWS;
        GC_TRIGGERS;
        INSTANCE_CHECK;
        PRECONDITION(!IsInterface());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    } CONTRACT_END;
    MethodTable *pMT = GetMethodTable();
    RETURN(pMT->ReverseInterfaceMDLookup(GetSlot()));
}
#endif // !DACCESS_COMPILE

PTR_LoaderAllocator MethodDesc::GetLoaderAllocator()
{
    WRAPPER_NO_CONTRACT;
    return GetLoaderModule()->GetLoaderAllocator();
}

#if !defined(DACCESS_COMPILE)
REFLECTMETHODREF MethodDesc::GetStubMethodInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    REFLECTMETHODREF retVal;
    REFLECTMETHODREF methodRef = (REFLECTMETHODREF)AllocateObject(CoreLibBinder::GetClass(CLASS__STUBMETHODINFO));
    GCPROTECT_BEGIN(methodRef);

    methodRef->SetMethod(this);
    LoaderAllocator *pLoaderAllocatorOfMethod = this->GetLoaderAllocator();
    if (pLoaderAllocatorOfMethod->IsCollectible())
        methodRef->SetKeepAlive(pLoaderAllocatorOfMethod->GetExposedObject());

    retVal = methodRef;
    GCPROTECT_END();

    return retVal;
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
typedef void (*WalkValueTypeParameterFnPtr)(Module *pModule, mdToken token, Module *pDefModule, mdToken tkDefToken, SigPointer *ptr, SigTypeContext *pTypeContext, void *pData);

void MethodDesc::WalkValueTypeParameters(MethodTable *pMT, WalkValueTypeParameterFnPtr function, void *pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    uint32_t numArgs = 0;
    Module *pModule = this->GetModule();
    SigPointer ptr = this->GetSigPointer();

    // skip over calling convention.
    uint32_t         callConv = 0;
    IfFailThrowBF(ptr.GetCallingConvInfo(&callConv), BFA_BAD_SIGNATURE, pModule);

    // If calling convention is generic, skip GenParamCount
    if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        IfFailThrowBF(ptr.GetData(NULL), BFA_BAD_SIGNATURE, pModule);
    }

    IfFailThrowBF(ptr.GetData(&numArgs), BFA_BAD_SIGNATURE, pModule);

    SigTypeContext typeContext(this, TypeHandle(pMT));

    // iterate over the return type and parameters
    for (DWORD j = 0; j <= numArgs; j++)
    {
        CorElementType type = ptr.PeekElemTypeClosed(pModule, &typeContext);
        if (type != ELEMENT_TYPE_VALUETYPE)
            goto moveToNextToken;

        mdToken token;
        Module *pTokenModule;
        token = ptr.PeekValueTypeTokenClosed(pModule, &typeContext, &pTokenModule);

        if (token == mdTokenNil)
            goto moveToNextToken;

        DWORD dwAttrType;
        Module *pDefModule;
        mdToken defToken;

        dwAttrType = 0;
        if (ClassLoader::ResolveTokenToTypeDefThrowing(pTokenModule, token, &pDefModule, &defToken))
        {
            if (function != NULL)
                function(pModule, token, pDefModule, defToken, &ptr, &typeContext, pData);
        }

moveToNextToken:
        // move to next argument token
        IfFailThrowBF(ptr.SkipExactlyOne(), BFA_BAD_SIGNATURE, pModule);
    }

    if (!HaveValueTypeParametersBeenWalked())
    {
        SetValueTypeParametersWalked();
    }
}

PrecodeType MethodDesc::GetPrecodeType()
{
    LIMITED_METHOD_CONTRACT;

    PrecodeType precodeType = PRECODE_INVALID;

#ifdef HAS_FIXUP_PRECODE
    if (!RequiresMethodDescCallingConvention())
    {
        // Use the more efficient fixup precode if possible
        precodeType = PRECODE_FIXUP;
    }
    else
#endif // HAS_FIXUP_PRECODE
    {
        precodeType = PRECODE_STUB;
    }

    return precodeType;
}

#endif // !DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
#ifndef DACCESS_COMPILE
void ComPlusCallMethodDesc::InitRetThunk()
{
    WRAPPER_NO_CONTRACT;

#ifdef TARGET_X86
    if (m_pComPlusCallInfo->m_pRetThunk != NULL)
        return;

    // Record the fact that we are writting into the ComPlusCallMethodDesc
    g_IBCLogger.LogMethodDescAccess(this);

    UINT numStackBytes = CbStackPop();

    LPVOID pRetThunk = ComPlusCall::GetRetThunk(numStackBytes);

    FastInterlockCompareExchangePointer<void *>(&m_pComPlusCallInfo->m_pRetThunk, pRetThunk, NULL);
#endif // TARGET_X86
}
#endif //!DACCESS_COMPILE
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
void MethodDesc::PrepareForUseAsADependencyOfANativeImageWorker()
{
    STANDARD_VM_CONTRACT;

    // This function ensures that a method is ready for use as a dependency of a native image
    // The current requirement is only that valuetypes can be resolved to their type defs as much
    // as is possible. (If the method is actually called, then this will not throw, but there
    // are cases where we call this method and we are unaware if this method will actually be called
    // or accessed as a native image dependency. This explains the contract (STANDARD_VM_CONTRACT)
    //  - This method should be callable only when general purpose VM code can be called
    // , as well as the TRY/CATCH.
    //  - This function should not introduce failures

    EX_TRY
    {
        WalkValueTypeParameters(this->GetMethodTable(), NULL, NULL);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    _ASSERTE(HaveValueTypeParametersBeenWalked());
}

static void CheckForEquivalenceAndLoadType(Module *pModule, mdToken token, Module *pDefModule, mdToken defToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BOOL *pHasEquivalentParam = (BOOL *)pData;

#ifdef FEATURE_TYPEEQUIVALENCE
    *pHasEquivalentParam = IsTypeDefEquivalent(defToken, pDefModule);
#else
    _ASSERTE(*pHasEquivalentParam == FALSE); // Assert this is always false.
#endif // FEATURE_TYPEEQUIVALENCE

    SigPointer sigPtr(*ptr);
    TypeHandle th = sigPtr.GetTypeHandleThrowing(pModule, pTypeContext);
    _ASSERTE(!th.IsNull());
}

void MethodDesc::PrepareForUseAsAFunctionPointer()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Since function pointers are unsafe and can enable type punning, all
    // value type parameters must be loaded prior to providing a function pointer.
    if (HaveValueTypeParametersBeenLoaded())
        return;

    BOOL fHasTypeEquivalentStructParameters = FALSE;
    WalkValueTypeParameters(this->GetMethodTable(), CheckForEquivalenceAndLoadType, &fHasTypeEquivalentStructParameters);

#ifdef FEATURE_TYPEEQUIVALENCE
    if (!fHasTypeEquivalentStructParameters)
        SetDoesNotHaveEquivalentValuetypeParameters();
#endif // FEATURE_TYPEEQUIVALENCE

    SetValueTypeParametersLoaded();
}
#endif //!DACCESS_COMPILE

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER: warning C4244
