// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




#include "common.h"

#include "binder.h"
#include "ecall.h"
#include "qcall.h"

#include "field.h"
#include "excep.h"
#include "eeconfig.h"
#include "runtimehandles.h"
#include "customattribute.h"
#include "debugdebugger.h"
#include "dllimport.h"
#include "clrvarargs.h"
#include "sigbuilder.h"
#include "olevariant.h"

//
// Retrieve structures from ID.
//
NOINLINE PTR_MethodTable CoreLibBinder::LookupClass(BinderClassID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_CoreLib)->LookupClassLocal(id);
}

PTR_MethodTable CoreLibBinder::GetClassLocal(BinderClassID id)
{
    WRAPPER_NO_CONTRACT;

    PTR_MethodTable pMT = VolatileLoad(&(m_pClasses[id]));
    if (pMT == NULL)
        return LookupClassLocal(id);
    return pMT;
}

PTR_MethodTable CoreLibBinder::LookupClassLocal(BinderClassID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != CLASS__NIL);
        PRECONDITION(id <= m_cClasses);
    }
    CONTRACTL_END;

    PTR_MethodTable pMT = NULL;

    // Binder methods are used for loading "known" types. Thus they are unlikely to be part
    // of a recursive cycle. This is used too broadly to force manual overrides at every callsite.
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    const CoreLibClassDescription *d = m_classDescriptions + (int)id;

    LPCUTF8 nameSpace = d->nameSpace;
    LPCUTF8 name = d->name;

    LPCUTF8 nestedTypeMaybe = strchr(name, '+');
    if (nestedTypeMaybe == NULL)
    {
        NameHandle nameHandle = NameHandle(nameSpace, name);
        pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), &nameHandle).AsMethodTable();
    }
    else
    {
        // Handle the nested type scenario.
        // The same NameHandle must be used to retain the scope to look for the nested type.
        NameHandle nameHandle(GetModule(), mdtBaseType);

        SString splitName(SString::Utf8, name, (COUNT_T)(nestedTypeMaybe - name));
        nameHandle.SetName(nameSpace, splitName.GetUTF8());

        // The side-effect of updating the scope in the NameHandle is the point of the call.
        (void)ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), &nameHandle);

        // Now load the nested type.
        nameHandle.SetName("", nestedTypeMaybe + 1);

        // We don't support nested types in nested types.
        _ASSERTE(strchr(nameHandle.GetName(), '+') == NULL);

        // We don't support nested types with explicit namespaces
        _ASSERTE(strchr(nameHandle.GetName(), '.') == NULL);
        pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), &nameHandle).AsMethodTable();
    }

    _ASSERTE(pMT->GetModule() == GetModule());

#ifndef DACCESS_COMPILE
    VolatileStore(&m_pClasses[id], pMT);
#endif

    return pMT;
}

NOINLINE MethodDesc * CoreLibBinder::LookupMethod(BinderMethodID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_CoreLib)->LookupMethodLocal(id);
}

MethodDesc * CoreLibBinder::GetMethodLocal(BinderMethodID id)
{
    WRAPPER_NO_CONTRACT;

    MethodDesc * pMD = VolatileLoad(&(m_pMethods[id]));
    if (pMD == NULL)
        return LookupMethodLocal(id);
    return pMD;
}

MethodDesc * CoreLibBinder::LookupMethodLocal(BinderMethodID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != METHOD__NIL);
        PRECONDITION(id <= m_cMethods);
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    MethodDesc * pMD = NULL;

    const CoreLibMethodDescription *d = m_methodDescriptions + (id - 1);

    MethodTable * pMT = GetClassLocal(d->classID);
    _ASSERTE(pMT != NULL && "Couldn't find a type in System.Private.CoreLib!");

    if (d->sig != NULL)
    {
        Signature sig = GetSignatureLocal(d->sig);

        pMD = MemberLoader::FindMethod(pMT, d->name, sig.GetRawSig(), sig.GetRawSigLen(), GetModule());
    }
    else
    {
        pMD = MemberLoader::FindMethodByName(pMT, d->name);
    }


    PREFIX_ASSUME_MSGF(pMD != NULL, ("EE expects method to exist: %s:%s  Sig pointer: %p\n", pMT->GetDebugClassName(), d->name, d->sig));

    VolatileStore(&m_pMethods[id], pMD);

    return pMD;
#else
    DacNotImpl();
    return NULL;
#endif
}

NOINLINE FieldDesc * CoreLibBinder::LookupField(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_CoreLib)->LookupFieldLocal(id);
}

FieldDesc * CoreLibBinder::GetFieldLocal(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;

    FieldDesc * pFD = VolatileLoad(&(m_pFields[id]));
    if (pFD == NULL)
        return LookupFieldLocal(id);
    return pFD;
}

FieldDesc * CoreLibBinder::LookupFieldLocal(BinderFieldID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());

        PRECONDITION(id != FIELD__NIL);
        PRECONDITION(id <= m_cFields);
    }
    CONTRACTL_END;

    FieldDesc * pFD = NULL;

    const CoreLibFieldDescription *d = m_fieldDescriptions + (id - 1);

    MethodTable * pMT = GetClassLocal(d->classID);

    pFD = MemberLoader::FindField(pMT, d->name, NULL, 0, NULL);

#ifndef DACCESS_COMPILE
    PREFIX_ASSUME_MSGF(pFD != NULL, ("EE expects field to exist: %s:%s\n", pMT->GetDebugClassName(), d->name));

    VolatileStore(&(m_pFields[id]), pFD);
#endif

    return pFD;
}

NOINLINE PTR_MethodTable CoreLibBinder::LookupClassIfExist(BinderClassID id)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        FORBID_FAULT;
        MODE_ANY;

        PRECONDITION(id != CLASS__NIL);
        PRECONDITION(id <= (&g_CoreLib)->m_cClasses);
    }
    CONTRACTL_END;

    // Run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // Binder methods are used for loading "known" types. Thus they are unlikely to be part
    // of a recursive cycle. This is used too broadly to force manual overrides at every callsite.
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    const CoreLibClassDescription *d = (&g_CoreLib)->m_classDescriptions + (int)id;

    PTR_MethodTable pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), d->nameSpace, d->name,
        ClassLoader::ReturnNullIfNotFound, ClassLoader::DontLoadTypes, CLASS_LOAD_APPROXPARENTS).AsMethodTable();

    _ASSERTE((pMT == NULL) || (pMT->GetModule() == GetModule()));

#ifndef DACCESS_COMPILE
    if ((pMT != NULL) && pMT->IsFullyLoaded())
        VolatileStore(&(g_CoreLib.m_pClasses[id]), pMT);
#endif

    return pMT;
}

Signature CoreLibBinder::GetSignature(LPHARDCODEDMETASIG pHardcodedSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

// Make sure all HardCodedMetaSig's are global. Because there is no individual
// cleanup of converted binary sigs, using allocated HardCodedMetaSig's
// can lead to a quiet memory leak.
#ifdef _DEBUG_IMPL

// This #include workaround generates a monster boolean expression that compares
// "this" against the address of every global defined in metasig.h
    if (! (0
#define METASIG_BODY(varname, types)    || pHardcodedSig==&gsig_ ## varname
#include "metasig.h"
    ))
    {
        _ASSERTE(!"The HardCodedMetaSig struct can only be declared as a global in metasig.h.");
    }
#endif

    return (&g_CoreLib)->GetSignatureLocal(pHardcodedSig);
}

Signature CoreLibBinder::GetTargetSignature(LPHARDCODEDMETASIG pHardcodedSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    return (&g_CoreLib)->GetSignatureLocal(pHardcodedSig);
}

// Get the metasig, do a one-time conversion if necessary
Signature CoreLibBinder::GetSignatureLocal(LPHARDCODEDMETASIG pHardcodedSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    PTR_CBYTE pMetaSig = PTR_CBYTE((TADDR)VolatileLoad(&pHardcodedSig->m_pMetaSig));

    // To minimize code and data size, the hardcoded metasigs are baked as much as possible
    // at compile time. Only the signatures with type references require one-time conversion at runtime.

    // the negative size means signature with unresolved type references
    if ((INT8)*pMetaSig < 0)
    {
#ifndef DACCESS_COMPILE
        pMetaSig = ConvertSignature(pHardcodedSig, pMetaSig);
#else
        DacNotImpl();
#endif
    }

    // The metasig has to be resolved at this point
    INT8 cbSig = (INT8)*pMetaSig;
    _ASSERTE(cbSig > 0);

#ifdef DACCESS_COMPILE
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)
        DacInstantiateTypeByAddress(dac_cast<TADDR>(pMetaSig + 1),
                                    cbSig,
                                    true);
#else
    PCCOR_SIGNATURE pSig = pMetaSig+1;
#endif

    return Signature(pSig, cbSig);
}

#ifndef DACCESS_COMPILE

bool CoreLibBinder::ConvertType(const BYTE*& pSig, SigBuilder * pSigBuilder)
{
    bool bSomethingResolved = false;

Again:
    CorElementType type = (CorElementType)*pSig++;

    switch (type)
    {
    case ELEMENT_TYPE_GENERICINST:
        {
            pSigBuilder->AppendElementType(type);
            if (ConvertType(pSig, pSigBuilder))
                bSomethingResolved = true;
            int arity = *pSig++;
            pSigBuilder->AppendData(arity);
            for (int i = 0; i < arity; i++)
            {
                if (ConvertType(pSig, pSigBuilder))
                    bSomethingResolved = true;
            }
        }
        break;

    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_SZARRAY:
        pSigBuilder->AppendElementType(type);
        if (ConvertType(pSig, pSigBuilder))
            bSomethingResolved = true;
        break;

    case ELEMENT_TYPE_CMOD_OPT:
    case ELEMENT_TYPE_CMOD_REQD:
        {
            // The binder class id may overflow 1 byte. Use 2 bytes to encode it.
            BinderClassID id = (BinderClassID)(*pSig + 0x100 * *(pSig + 1));
            pSig += 2;

            pSigBuilder->AppendElementType(type);
            pSigBuilder->AppendToken(GetClassLocal(id)->GetCl());
            bSomethingResolved = true;
        }
        goto Again;

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:
        {
            // The binder class id may overflow 1 byte. Use 2 bytes to encode it.
            BinderClassID id = (BinderClassID)(*pSig + 0x100 * *(pSig + 1));
            pSig += 2;

            pSigBuilder->AppendElementType(type);
            pSigBuilder->AppendToken(GetClassLocal(id)->GetCl());
            bSomethingResolved = true;
        }
        break;

    case ELEMENT_TYPE_VAR:
    case ELEMENT_TYPE_MVAR:
        {
            pSigBuilder->AppendElementType(type);
            pSigBuilder->AppendData(*pSig++);
        }
        break;

    default:
        pSigBuilder->AppendElementType(type);
        break;
    }

    return bSomethingResolved;
}

//------------------------------------------------------------------
// Resolve type references in the hardcoded metasig.
// Returns a new signature with type references resolved.
//------------------------------------------------------------------
void CoreLibBinder::BuildConvertedSignature(const BYTE* pSig, SigBuilder * pSigBuilder)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pSig));
        PRECONDITION(CheckPointer(pSigBuilder));
    }
    CONTRACTL_END

    unsigned argCount;
    unsigned callConv;
    bool bSomethingResolved = false;

    // calling convention
    callConv = *pSig++;
    pSigBuilder->AppendData(callConv);

    if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_DEFAULT) {
        // arg count
        argCount = *pSig++;
        pSigBuilder->AppendData(argCount);
    }
    else {
        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD)
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (ModuleBase*)NULL);
        argCount = 0;
    }

    // <= because we want to include the return value or the field
    for (unsigned i = 0; i <= argCount; i++) {
        if (ConvertType(pSig, pSigBuilder))
            bSomethingResolved = true;
    }

    _ASSERTE(bSomethingResolved);
}

const BYTE* CoreLibBinder::ConvertSignature(LPHARDCODEDMETASIG pHardcodedSig, const BYTE* pSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    GCX_PREEMP();

    SigBuilder sigBuilder;

    BuildConvertedSignature(pSig+1, &sigBuilder);

    DWORD cbCount;
    PVOID pSignature = sigBuilder.GetSignature(&cbCount);

    {
        CrstHolder ch(&s_SigConvertCrst);

        if (*(INT8*)pHardcodedSig->m_pMetaSig < 0) {

            BYTE* pResolved = (BYTE*)(void*)(SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(1) + S_SIZE_T(cbCount)));

            _ASSERTE(FitsIn<INT8>(cbCount));
            *(INT8*)pResolved = static_cast<INT8>(cbCount);
            CopyMemory(pResolved+1, pSignature, cbCount);

            // this has to be last, overwrite the pointer to the metasig with the resolved one
            VolatileStore<const BYTE *>(&const_cast<HardCodedMetaSig *>(pHardcodedSig)->m_pMetaSig, pResolved);
        }
    }

    return pHardcodedSig->m_pMetaSig;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef _DEBUG
void CoreLibBinder::TriggerGCUnderStress()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    _ASSERTE (GetThreadNULLOk());
    TRIGGERSGC ();
    // Force a GC here because GetClass could trigger GC nondeterministicly
    if (g_pConfig->GetGCStressLevel() != 0)
    {
        DEBUG_ONLY_REGION();
        Thread * pThread = GetThread();
        BOOL bInCoopMode = pThread->PreemptiveGCDisabled ();
        GCX_COOP ();
        if (bInCoopMode)
        {
            pThread->PulseGCMode ();
        }
    }
#endif //DACCESS_COMPILE
}
#endif // _DEBUG

DWORD CoreLibBinder::GetFieldOffset(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;

    return GetField(id)->GetOffset();
}

#ifndef DACCESS_COMPILE

CrstStatic CoreLibBinder::s_SigConvertCrst;

/*static*/
void CoreLibBinder::Startup()
{
    WRAPPER_NO_CONTRACT
    s_SigConvertCrst.Init(CrstSigConvert);
}

#if defined(_DEBUG)

// NoClass is used to suppress check for unmanaged and managed size match
#define NoClass char[USHRT_MAX]

const CoreLibBinder::OffsetAndSizeCheck CoreLibBinder::OffsetsAndSizes[] =
{
    #define DEFINE_CLASS_U(nameSpace, stringName, unmanagedType) \
        { PTR_CSTR((TADDR) g_ ## nameSpace ## NS ), PTR_CUTF8((TADDR) # stringName), sizeof(unmanagedType), 0, 0, 0 },

    #define DEFINE_FIELD_U(stringName, unmanagedContainingType, unmanagedOffset) \
        { 0, 0, 0, PTR_CUTF8((TADDR) # stringName), offsetof(unmanagedContainingType, unmanagedOffset), sizeof(((unmanagedContainingType*)1)->unmanagedOffset) },
    #include "corelib.h"
};

//
// check the basic consistency between CoreLib and VM
//
void CoreLibBinder::Check()
{
    STANDARD_VM_CONTRACT;

    MethodTable * pMT = NULL;

    for (unsigned i = 0; i < ARRAY_SIZE(OffsetsAndSizes); i++)
    {
        const OffsetAndSizeCheck * p = OffsetsAndSizes + i;

        if (p->className != NULL)
        {
            pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), p->classNameSpace, p->className).AsMethodTable();

            if (p->expectedClassSize == sizeof(NoClass))
                continue;

            // hidden size of the type that participates in the alignment calculation
            DWORD hiddenSize = pMT->IsValueType() ? sizeof(MethodTable*) : 0;

            DWORD size = pMT->GetBaseSize() - (sizeof(ObjHeader)+hiddenSize);

            DWORD expectedsize = (DWORD)ALIGN_UP(p->expectedClassSize + (sizeof(ObjHeader) + hiddenSize),
                DATA_ALIGNMENT) - (sizeof(ObjHeader) + hiddenSize);

            CONSISTENCY_CHECK_MSGF(size == expectedsize,
                ("Managed object size does not match unmanaged object size\n"
                "man: 0x%x, unman: 0x%x, Name: %s\n", size, expectedsize, pMT->GetDebugClassName()));
        }
        else
        if (p->fieldName != NULL)
        {
            // This assert will fire if there is DEFINE_FIELD_U macro without preceding DEFINE_CLASS_U macro in corelib.h
            _ASSERTE(pMT != NULL);

            FieldDesc * pFD = MemberLoader::FindField(pMT, p->fieldName, NULL, 0, NULL);
            _ASSERTE(pFD != NULL);

            DWORD offset = pFD->GetOffset();

            if (!pFD->IsFieldOfValueType())
            {
                offset += Object::GetOffsetOfFirstField();
            }

            CONSISTENCY_CHECK_MSGF(offset == p->expectedFieldOffset,
                ("Managed class field offset does not match unmanaged class field offset\n"
                 "man: 0x%x, unman: 0x%x, Class: %s, Name: %s\n", offset, p->expectedFieldOffset, pFD->GetApproxEnclosingMethodTable()->GetDebugClassName(), pFD->GetName()));

            DWORD size = pFD->LoadSize();

            CONSISTENCY_CHECK_MSGF(size == p->expectedFieldSize,
                ("Managed class field size does not match unmanaged class field size\n"
                "man: 0x%x, unman: 0x%x, Class: %s, Name: %s\n", size, p->expectedFieldSize, pFD->GetApproxEnclosingMethodTable()->GetDebugClassName(), pFD->GetName()));
        }
    }
}

#ifdef CHECK_FCALL_SIGNATURE
//
// check consistency of the unmanaged and managed fcall signatures
//
/* static */ FCSigCheck* FCSigCheck::g_pFCSigCheck;
const char * aType[] =
{
    "void",
    "FC_BOOL_RET",
    "CLR_BOOL",
    "FC_CHAR_RET",
    "CLR_CHAR",
    "FC_INT8_RET",
    "INT8",
    "FC_UINT8_RET",
    "UINT8",
    "FC_INT16_RET",
    "INT16",
    "FC_UINT16_RET",
    "UINT16",
    "INT64",
    "VINT64",
    "UINT64",
    "VUINT64",
    "float",
    "Vfloat",
    "double",
    "Vdouble"
};

const char * aInt32Type[] =
{
    "INT32",
    "UINT32",                   // we might remove it to have a better check
    "int",
    "unsigned int",             // we might remove it to have a better check
    "DWORD",                    // we might remove it to have a better check
    "HRESULT",                  // we might remove it to have a better check
    "mdToken",                  // we might remove it to have a better check
    "ULONG",                    // we might remove it to have a better check
    "mdMemberRef",              // we might remove it to have a better check
    "mdCustomAttribute",        // we might remove it to have a better check
    "mdTypeDef",                // we might remove it to have a better check
    "mdFieldDef",               // we might remove it to have a better check
    "LONG",
    "CLR_I4",
    "LCID"                      // we might remove it to have a better check
};

const char * aUInt32Type[] =
{
    "UINT32",
    "unsigned int",
    "DWORD",
    "INT32",                    // we might remove it to have a better check
    "ULONG"
};

static BOOL IsStrInArray(const char* sStr, size_t len, const char* aStrArray[], int nSize)
{
    STANDARD_VM_CONTRACT;
    for (int i = 0; i < nSize; i++)
    {
        if (SString::_strnicmp(aStrArray[i], sStr, (COUNT_T)len) == 0)
        {
            return TRUE;
        }
    }
    return FALSE;
}

static void FCallCheckSignature(MethodDesc* pMD, PCODE pImpl)
{
    STANDARD_VM_CONTRACT;

    const char* pUnmanagedSig = NULL;

    FCSigCheck* pSigCheck = FCSigCheck::g_pFCSigCheck;
    while (pSigCheck != NULL)
    {
        if (pImpl == (PCODE)pSigCheck->func) {
            pUnmanagedSig = pSigCheck->signature;
            break;
        }
        pSigCheck = pSigCheck->next;
    }

    MetaSig msig(pMD);
    int argIndex = -2; // start with return value
    int enregisteredArguments = 0;
    const char* pUnmanagedArg = pUnmanagedSig;
    for (;;)
    {
        CorElementType argType = ELEMENT_TYPE_END;
        TypeHandle argTypeHandle;

        if (argIndex == -2)
        {
            // return value
            argType = msig.GetReturnType();
            if (argType == ELEMENT_TYPE_VALUETYPE)
                argTypeHandle = msig.GetRetTypeHandleThrowing();
        }

        if (argIndex == -1)
        {
            // this ptr
            if (msig.HasThis())
                argType = ELEMENT_TYPE_CLASS;
            else
                argIndex++; // move on to the first argument
        }

        if (argIndex >= 0)
        {
            argType = msig.NextArg();
            if (argType == ELEMENT_TYPE_END)
                break;
            if (argType == ELEMENT_TYPE_VALUETYPE)
                argTypeHandle = msig.GetLastTypeHandleThrowing();
        }

        const char* expectedType = NULL;

        switch (argType)
        {
        case ELEMENT_TYPE_VOID:
            expectedType = pMD->IsCtor() ? NULL : "void";
            break;
        case ELEMENT_TYPE_BOOLEAN:
            expectedType = (argIndex == -2) ? "FC_BOOL_RET" : "CLR_BOOL";
            break;
        case ELEMENT_TYPE_CHAR:
            expectedType = (argIndex == -2) ? "FC_CHAR_RET" : "CLR_CHAR";
            break;
        case ELEMENT_TYPE_I1:
            expectedType = (argIndex == -2) ? "FC_INT8_RET" : "INT8";
            break;
        case ELEMENT_TYPE_U1:
            expectedType = (argIndex == -2) ? "FC_UINT8_RET" : "UINT8";
            break;
        case ELEMENT_TYPE_I2:
            expectedType = (argIndex == -2) ? "FC_INT16_RET" : "INT16";
            break;
        case ELEMENT_TYPE_U2:
            expectedType = (argIndex == -2) ? "FC_UINT16_RET" : "UINT16";
            break;
        //case ELEMENT_TYPE_I4:
        //     expectedType = "INT32";
        //     break;
        // case ELEMENT_TYPE_U4:
        //     expectedType = "UINT32";
        //     break;

        // See the comments in fcall.h on what the "V" prefix means.
        case ELEMENT_TYPE_I8:
            expectedType = ((argIndex == -2) || (enregisteredArguments >= 2)) ? "INT64" : "VINT64";
            break;
        case ELEMENT_TYPE_U8:
            expectedType = ((argIndex == -2) || (enregisteredArguments >= 2)) ? "UINT64" : "VUINT64";
            break;
        case ELEMENT_TYPE_R4:
            expectedType = ((argIndex == -2) || (enregisteredArguments >= 2)) ? "float" : "Vfloat";
            break;
        case ELEMENT_TYPE_R8:
            expectedType = ((argIndex == -2) || (enregisteredArguments >= 2)) ? "double" : "Vdouble";
            break;
        default:
            // no checks for other types
            break;
        }

        // Count number of enregistered arguments for x86
        if ((argIndex != -2) && !((expectedType != NULL) && (*expectedType == 'V')))
        {
            enregisteredArguments++;
        }

        if (pUnmanagedSig != NULL)
        {
            CONSISTENCY_CHECK_MSGF(pUnmanagedArg != NULL,
                ("Unexpected end of managed fcall signature\n"
                "Method: %s:%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

            const char* pUnmanagedArgEnd = strchr(pUnmanagedArg, ',');

            const char* pUnmanagedTypeEnd = (pUnmanagedArgEnd != NULL) ?
                pUnmanagedArgEnd : (pUnmanagedArg + strlen(pUnmanagedArg));

            if (argIndex != -2)
            {
                // skip argument name
                while(pUnmanagedTypeEnd > pUnmanagedArg)
                {
                    char c = *(pUnmanagedTypeEnd-1);
                    if ((c != '_')
                        && ((c < '0') || ('9' < c))
                        && ((c < 'a') || ('z' < c))
                        && ((c < 'A') || ('Z' < c)))
                        break;
                    pUnmanagedTypeEnd--;
                }
            }

            // skip whitespaces
            while(pUnmanagedTypeEnd > pUnmanagedArg)
            {
                char c = *(pUnmanagedTypeEnd-1);
                if ((c != 0x20) && (c != '\t') && (c != '\n') && (c != '\r'))
                    break;
                pUnmanagedTypeEnd--;
            }

            size_t len = pUnmanagedTypeEnd - pUnmanagedArg;
            // generate the unmanaged argument signature to show them in the error message if possible
            StackSString ssUnmanagedType(SString::Ascii, pUnmanagedArg, (COUNT_T)len);
            const char * pUnManagedType = ssUnmanagedType.GetUTF8();

            if (expectedType != NULL)
            {
                // when managed type is well known
                if (!(strlen(expectedType) == len && SString::_strnicmp(expectedType, pUnmanagedArg, (COUNT_T)len) == 0))
                {
                    printf("CheckExtended: The managed and unmanaged fcall signatures do not match, Method: %s:%s. Argument: %d Expecting: %s Actual: %s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, argIndex, expectedType, pUnManagedType);
                }
            }
            else
            {
                // when managed type is not wellknown, we still can find sig mismatch if native type is a well known type
                BOOL bSigError = false;
                if (argType == ELEMENT_TYPE_VOID && pMD->IsCtor())
                {
                    bSigError = false;
                }
                else if (argType == ELEMENT_TYPE_I4)
                {
                    bSigError = !IsStrInArray(pUnmanagedArg, len, aInt32Type, ARRAY_SIZE(aInt32Type));
                }
                else if (argType == ELEMENT_TYPE_U4)
                {
                    bSigError = !IsStrInArray(pUnmanagedArg, len, aUInt32Type, ARRAY_SIZE(aUInt32Type));
                }
                else if (argType == ELEMENT_TYPE_VALUETYPE)
                {
                    // we already did special check for value type
                    bSigError = false;
                }
                else
                {
                    bSigError = IsStrInArray(pUnmanagedArg, len, aType, ARRAY_SIZE(aType));
                }
                if (bSigError)
                {
                    printf("CheckExtended: The managed and unmanaged fcall signatures do not match, Method: %s:%s. Argument: %d Expecting: (CorElementType)%d actual: %s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, argIndex, argType, pUnManagedType);
                }
            }
            pUnmanagedArg = (pUnmanagedArgEnd != NULL) ? (pUnmanagedArgEnd+1) : NULL;
        }

        argIndex++;
    }

    if (pUnmanagedSig != NULL)
    {
        if (msig.IsVarArg())
        {
            if (!((pUnmanagedArg != NULL) && strcmp(pUnmanagedArg, "...") == 0))
            {
                printf("CheckExtended: Expecting varargs in unmanaged fcall signature, Method: %s:%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName);
            }
        }
        else
        {
            if (!(pUnmanagedArg == NULL))
            {
                printf("CheckExtended: Unexpected end of unmanaged fcall signature, Method: %s:%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName);
            }
        }
    }
}
#endif // CHECK_FCALL_SIGNATURE

//
// extended check of consistency between CoreLib and VM:
//  - verifies that all references from CoreLib to VM are present
//  - verifies that all references from VM to CoreLib are present
//  - limited detection of mismatches between managed and unmanaged fcall signatures
//
void CoreLibBinder::CheckExtended()
{
    STANDARD_VM_CONTRACT;

    // check the consistency of CoreLib and VM
    // note: it is not enabled by default because of it is time consuming and
    // changes the bootstrap sequence of the EE
    if (!CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ConsistencyCheck))
        return;

    //
    // VM referencing CoreLib (corelib.h)
    //
    for (BinderClassID cID = (BinderClassID) 1; (int)cID < m_cClasses; cID = (BinderClassID) (cID + 1))
    {
        bool fError = false;
        EX_TRY
        {
            if (CoreLibBinder::GetClassName(cID) != NULL) // Allow for CorSigElement entries with no classes
            {
                if (NULL == CoreLibBinder::GetClass(cID))
                {
                    fError = true;
                }
            }
        }
        EX_CATCH
        {
            fError = true;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (fError)
        {
            printf("CheckExtended: VM expects type to exist:  %s.%s\n", CoreLibBinder::GetClassNameSpace(cID), CoreLibBinder::GetClassName(cID));
        }
    }

    for (BinderMethodID mID = (BinderMethodID) 1; mID < (BinderMethodID) CoreLibBinder::m_cMethods; mID = (BinderMethodID) (mID + 1))
    {
        bool fError = false;
        BinderClassID cID = m_methodDescriptions[mID-1].classID;
        EX_TRY
        {
            if (NULL == CoreLibBinder::GetMethod(mID))
            {
                fError = true;
            }
        }
        EX_CATCH
        {
            fError = true;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (fError)
        {
            printf("CheckExtended: VM expects method to exist:  %s.%s::%s\n", CoreLibBinder::GetClassNameSpace(cID), CoreLibBinder::GetClassName(cID), CoreLibBinder::GetMethodName(mID));
        }
    }

    for (BinderFieldID fID = (BinderFieldID) 1; fID < (BinderFieldID) CoreLibBinder::m_cFields; fID = (BinderFieldID) (fID + 1))
    {
        bool fError = false;
        BinderClassID cID = m_fieldDescriptions[fID-1].classID;
        EX_TRY
        {
            if (NULL == CoreLibBinder::GetField(fID))
            {
                fError = true;
            }
        }
        EX_CATCH
        {
            fError = true;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (fError)
        {
            printf("CheckExtended: VM expects field to exist:  %s.%s::%s\n", CoreLibBinder::GetClassNameSpace(cID), CoreLibBinder::GetClassName(cID), CoreLibBinder::GetFieldName(fID));
        }
    }

    //
    // CoreLib referencing VM (ecalllist.h)
    //
    SetSHash<DWORD> usedECallIds;

    HRESULT hr = S_OK;
    Module *pModule = CoreLibBinder::m_pModule;
    IMDInternalImport *pInternalImport = pModule->GetMDImport();

    HENUMInternal hEnum;

    // for all methods...
    IfFailGo(pInternalImport->EnumAllInit(mdtMethodDef, &hEnum));

    for (;;) {
        mdTypeDef td;
        mdTypeDef tdClass;
        DWORD dwImplFlags;
        DWORD dwMemberAttrs;

        if (!pInternalImport->EnumNext(&hEnum, &td))
            break;

        pInternalImport->GetMethodImplProps(td, NULL, &dwImplFlags);

        IfFailGo(pInternalImport->GetMethodDefProps(td, &dwMemberAttrs));

        // ... that are internal calls ...
        if (!IsMiInternalCall(dwImplFlags) && !IsMdPinvokeImpl(dwMemberAttrs))
            continue;

        IfFailGo(pInternalImport->GetParentToken(td, &tdClass));

        TypeHandle type;

        EX_TRY
        {
            type = ClassLoader::LoadTypeDefOrRefThrowing(pModule, tdClass,
                                                         ClassLoader::ThrowIfNotFound,
                                                         ClassLoader::PermitUninstDefOrRef);
        }
        EX_CATCH
        {
            LPCUTF8 pszClassName;
            LPCUTF8 pszNameSpace;
            if (FAILED(pInternalImport->GetNameOfTypeDef(tdClass, &pszClassName, &pszNameSpace)))
            {
                pszClassName = pszNameSpace = "Invalid TypeDef record";
            }
            printf("CheckExtended: Unable to load class from System.Private.CoreLib: %s.%s\n", pszNameSpace, pszClassName);
        }
        EX_END_CATCH(SwallowAllExceptions)

        MethodDesc *pMD = MemberLoader::FindMethod(type.AsMethodTable(), td);
        _ASSERTE(pMD);

        // Required to support generic FCalls (only instance methods on generic types constrained to "class" are allowed)
        if (type.IsGenericTypeDefinition()) {
            pMD = pMD->FindOrCreateTypicalSharedInstantiation();
        }

        DWORD id = 0;

        if (pMD->IsFCall())
        {
            id = ((FCallMethodDesc *)pMD)->GetECallID();
            if (id == 0) {
                id = ECall::GetIDForMethod(pMD);
            }
        }
        else
        if (pMD->IsNDirect())
        {
            NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
            NDirect::PopulateNDirectMethodDesc(pNMD);

            if (pNMD->IsQCall() && QCallResolveDllImport(pNMD->GetEntrypointName()) == nullptr)
            {
                LPCUTF8 pszClassName;
                LPCUTF8 pszNameSpace;
                if (FAILED(pInternalImport->GetNameOfTypeDef(tdClass, &pszClassName, &pszNameSpace)))
                {
                    pszClassName = pszNameSpace = "Invalid TypeDef record";
                }
                LPCUTF8 pszName;
                if (FAILED(pInternalImport->GetNameOfMethodDef(td, &pszName)))
                {
                    pszName = "Invalid method name";
                }
                printf("CheckExtended: Unable to find qcall implementation: %s.%s::%s (EntryPoint name: %s)\n", pszNameSpace, pszClassName, pszName, pNMD->GetEntrypointName());
            }
            continue;
        }
        else
        {
            continue;
        }

        // ... check that the method is in the fcall table.
        if (id == 0) {
            LPCUTF8 pszClassName;
            LPCUTF8 pszNameSpace;
            if (FAILED(pInternalImport->GetNameOfTypeDef(tdClass, &pszClassName, &pszNameSpace)))
            {
                pszClassName = pszNameSpace = "Invalid TypeDef record";
            }
            LPCUTF8 pszName;
            if (FAILED(pInternalImport->GetNameOfMethodDef(td, &pszName)))
            {
                pszName = "Invalid method name";
            }
            printf("CheckExtended: Unable to find internalcall implementation: %s.%s::%s\n", pszNameSpace, pszClassName, pszName);
        }

        if (id != 0)
        {
            usedECallIds.Add(id);
        }

#ifdef CHECK_FCALL_SIGNATURE
        if (pMD->IsFCall())
        {
            FCallCheckSignature(pMD, ECall::GetFCallImpl(pMD));
        }
#endif // CHECK_FCALL_SIGNATURE
    }

    pInternalImport->EnumClose(&hEnum);

    // Verify that there are no unused entries in the ecall table
    ECall::CheckUnusedECalls(usedECallIds);

    //
    // Stub constants
    //
#define ASMCONSTANTS_C_ASSERT(cond)
#define ASMCONSTANTS_RUNTIME_ASSERT(cond) _ASSERTE(cond)
#include "asmconstants.h"

    printf("CheckExtended: completed without exception.\n");

ErrExit:
    _ASSERTE(SUCCEEDED(hr));
}

#endif // _DEBUG

extern const CoreLibClassDescription c_rgCoreLibClassDescriptions[];
extern const USHORT c_nCoreLibClassDescriptions;

extern const CoreLibMethodDescription c_rgCoreLibMethodDescriptions[];
extern const USHORT c_nCoreLibMethodDescriptions;

extern const CoreLibFieldDescription c_rgCoreLibFieldDescriptions[];
extern const USHORT c_nCoreLibFieldDescriptions;


void CoreLibBinder::AttachModule(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    CoreLibBinder * pGlobalBinder = &g_CoreLib;

    pGlobalBinder->SetDescriptions(pModule,
        c_rgCoreLibClassDescriptions,  c_nCoreLibClassDescriptions,
        c_rgCoreLibMethodDescriptions, c_nCoreLibMethodDescriptions,
        c_rgCoreLibFieldDescriptions,  c_nCoreLibFieldDescriptions);

    pGlobalBinder->AllocateTables();

    pModule->m_pBinder = pGlobalBinder;
}

void CoreLibBinder::SetDescriptions(Module * pModule,
    const CoreLibClassDescription * pClassDescriptions, USHORT nClasses,
    const CoreLibMethodDescription * pMethodDescriptions, USHORT nMethods,
    const CoreLibFieldDescription * pFieldDescriptions, USHORT nFields)
{
    LIMITED_METHOD_CONTRACT;

    m_pModule = pModule;

    m_classDescriptions = pClassDescriptions;
    m_cClasses = nClasses;

    m_methodDescriptions = pMethodDescriptions;
    m_cMethods = nMethods;

    m_fieldDescriptions = pFieldDescriptions;
    m_cFields = nFields;
}

void CoreLibBinder::AllocateTables()
{
    STANDARD_VM_CONTRACT;

    LoaderHeap * pHeap = m_pModule->GetAssembly()->GetLowFrequencyHeap();

    m_pClasses = (MethodTable **)(void *)
        pHeap->AllocMem(S_SIZE_T(m_cClasses) * S_SIZE_T(sizeof(*m_pClasses)));
    // Note: Memory allocated on loader heap is zero filled
    // ZeroMemory(m_pClasses, m_cClasses * sizeof(*m_pClasses));

    m_pMethods = (MethodDesc **)(void *)
        pHeap->AllocMem(S_SIZE_T(m_cMethods) * S_SIZE_T(sizeof(*m_pMethods)));
    // Note: Memory allocated on loader heap is zero filled
    // ZeroMemory(m_pMethods, m_cMethodMDs * sizeof(*m_pMethods));

    m_pFields = (FieldDesc **)(void *)
        pHeap->AllocMem(S_SIZE_T(m_cFields) * S_SIZE_T(sizeof(*m_pFields)));
    // Note: Memory allocated on loader heap is zero filled
    // ZeroMemory(m_pFields, m_cFieldRIDs * sizeof(*m_pFields));
}

PTR_MethodTable CoreLibBinder::LoadPrimitiveType(CorElementType et)
{
    STANDARD_VM_CONTRACT;

    PTR_MethodTable pMT = g_CoreLib.m_pClasses[et];

    // Primitive types hit cyclic reference on binder during type loading so we have to load them in two steps
    if (pMT == NULL)
    {
        const CoreLibClassDescription *d = (&g_CoreLib)->m_classDescriptions + (int)et;

        pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), d->nameSpace, d->name,
            ClassLoader::ThrowIfNotFound, ClassLoader::LoadTypes, CLASS_LOAD_APPROXPARENTS).AsMethodTable();
        g_CoreLib.m_pClasses[et] = pMT;

        ClassLoader::EnsureLoaded(pMT);
    }

    return pMT;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
CoreLibBinder::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    DacEnumMemoryRegion(dac_cast<TADDR>(m_classDescriptions),
                        m_cClasses * sizeof(CoreLibClassDescription));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_methodDescriptions),
                        (m_cMethods - 1) * sizeof(CoreLibMethodDescription));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_fieldDescriptions),
                        (m_cFields - 1) * sizeof(CoreLibFieldDescription));

    if (m_pModule.IsValid())
    {
        m_pModule->EnumMemoryRegions(flags, true);
    }

    DacEnumMemoryRegion(dac_cast<TADDR>(m_pClasses),
                        m_cClasses * sizeof(PTR_MethodTable));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_pMethods),
                        m_cMethods * sizeof(PTR_MethodDesc));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_pFields),
                        m_cFields * sizeof(PTR_FieldDesc));
}

#endif // #ifdef DACCESS_COMPILE

GVAL_IMPL(CoreLibBinder, g_CoreLib);
