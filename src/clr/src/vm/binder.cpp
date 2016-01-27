// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

 


#include "common.h"

#include "binder.h"
#include "ecall.h"

#include "field.h"
#include "excep.h"
#ifdef FEATURE_REMOTING
#include "message.h"
#endif // FEATURE_REMOTING
#include "eeconfig.h"
#include "rwlock.h"
#include "runtimehandles.h"
#include "customattribute.h"
#include "debugdebugger.h"
#include "dllimport.h"
#include "nativeoverlapped.h"
#include "clrvarargs.h"
#include "sigbuilder.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

//
// Retrieve structures from ID.
// 
NOINLINE PTR_MethodTable MscorlibBinder::LookupClass(BinderClassID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_Mscorlib)->LookupClassLocal(id);
}

PTR_MethodTable MscorlibBinder::GetClassLocal(BinderClassID id)
{
    WRAPPER_NO_CONTRACT;

    PTR_MethodTable pMT = VolatileLoad(&(m_pClasses[id]));
    if (pMT == NULL) 
        return LookupClassLocal(id);
    return pMT;
}

PTR_MethodTable MscorlibBinder::LookupClassLocal(BinderClassID id)
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

    // Binder methods are used for loading "known" types from mscorlib.dll. Thus they are unlikely to be part
    // of a recursive cycle. This is used too broadly to force manual overrides at every callsite. 
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    const MscorlibClassDescription *d = m_classDescriptions + (int)id;

    pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), d->nameSpace, d->name).AsMethodTable();

    _ASSERTE(pMT->GetModule() == GetModule());

#ifndef DACCESS_COMPILE
    VolatileStore(&m_pClasses[id], pMT);
#endif

    return pMT;
}

NOINLINE MethodDesc * MscorlibBinder::LookupMethod(BinderMethodID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_Mscorlib)->LookupMethodLocal(id);
}

MethodDesc * MscorlibBinder::GetMethodLocal(BinderMethodID id)
{
    WRAPPER_NO_CONTRACT;

    MethodDesc * pMD = VolatileLoad(&(m_pMethods[id]));
    if (pMD == NULL) 
        return LookupMethodLocal(id);
    return pMD;
}

MethodDesc * MscorlibBinder::LookupMethodLocal(BinderMethodID id)
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

    const MscorlibMethodDescription *d = m_methodDescriptions + (id - 1);

    MethodTable * pMT = GetClassLocal(d->classID);
    _ASSERTE(pMT != NULL && "Couldn't find a type in mscorlib!");

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

NOINLINE FieldDesc * MscorlibBinder::LookupField(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;
    return (&g_Mscorlib)->LookupFieldLocal(id);
}

FieldDesc * MscorlibBinder::GetFieldLocal(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;

    FieldDesc * pFD = VolatileLoad(&(m_pFields[id]));
    if (pFD == NULL) 
        return LookupFieldLocal(id);
    return pFD;
}

FieldDesc * MscorlibBinder::LookupFieldLocal(BinderFieldID id)
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

    const MscorlibFieldDescription *d = m_fieldDescriptions + (id - 1);

    MethodTable * pMT = GetClassLocal(d->classID);

    pFD = MemberLoader::FindField(pMT, d->name, NULL, 0, NULL);

#ifndef DACCESS_COMPILE
    PREFIX_ASSUME_MSGF(pFD != NULL, ("EE expects field to exist: %s:%s\n", pMT->GetDebugClassName(), d->name));

    VolatileStore(&(m_pFields[id]), pFD);
#endif

    return pFD;
}

NOINLINE PTR_MethodTable MscorlibBinder::LookupClassIfExist(BinderClassID id)
{
    CONTRACTL
    {
        GC_NOTRIGGER; 
        NOTHROW;
        FORBID_FAULT;
        MODE_ANY;

        PRECONDITION(id != CLASS__NIL);
        PRECONDITION(id <= (&g_Mscorlib)->m_cClasses);
    }
    CONTRACTL_END;

    // Run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // Binder methods are used for loading "known" types from mscorlib.dll. Thus they are unlikely to be part
    // of a recursive cycle. This is used too broadly to force manual overrides at every callsite. 
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    const MscorlibClassDescription *d = (&g_Mscorlib)->m_classDescriptions + (int)id;

    PTR_MethodTable pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), d->nameSpace, d->name,
        ClassLoader::ReturnNullIfNotFound, ClassLoader::DontLoadTypes, CLASS_LOAD_UNRESTOREDTYPEKEY).AsMethodTable();

    _ASSERTE((pMT == NULL) || (pMT->GetModule() == GetModule()));

#ifndef DACCESS_COMPILE
    if ((pMT != NULL) && pMT->IsFullyLoaded())
        VolatileStore(&(g_Mscorlib.m_pClasses[id]), pMT);
#endif

    return pMT;
}

Signature MscorlibBinder::GetSignature(LPHARDCODEDMETASIG pHardcodedSig)
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

    return (&g_Mscorlib)->GetSignatureLocal(pHardcodedSig);
}

Signature MscorlibBinder::GetTargetSignature(LPHARDCODEDMETASIG pHardcodedSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

#ifdef CROSSGEN_COMPILE
    return GetModule()->m_pBinder->GetSignatureLocal(pHardcodedSig);
#else
    return (&g_Mscorlib)->GetSignatureLocal(pHardcodedSig);
#endif
}

// Get the metasig, do a one-time conversion if necessary
Signature MscorlibBinder::GetSignatureLocal(LPHARDCODEDMETASIG pHardcodedSig)
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

//------------------------------------------------------------------
// Resolve type references in the hardcoded metasig.
// Returns a new signature with type refences resolved.
//------------------------------------------------------------------
void MscorlibBinder::BuildConvertedSignature(const BYTE* pSig, SigBuilder * pSigBuilder)
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
    INDEBUG(bool bSomethingResolved = false;)

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
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)NULL);
        argCount = 0;
    }

    // <= because we want to include the return value or the field
    for (unsigned i = 0; i <= argCount; i++) {

        for (;;) {
            BinderClassID id = CLASS__NIL;
            bool again = false;

            CorElementType type = (CorElementType)*pSig++;

            switch (type)
            {
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_SZARRAY:
                again = true;
                break;

            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_VALUETYPE:
                // The binder class id may overflow 1 byte. Use 2 bytes to encode it.
                id = (BinderClassID) (*pSig + 0x100 * *(pSig + 1));
                pSig += 2;
                break;

            case ELEMENT_TYPE_VOID:
                if (i != 0) {
                    if (pSig[-2] != ELEMENT_TYPE_PTR)
                        THROW_BAD_FORMAT(BFA_ONLY_VOID_PTR_IN_ARGS, (Module*)NULL); // only pointer to void allowed in arguments
                }
                break;

            default:
                break;
            }

            pSigBuilder->AppendElementType(type);

            if (id != CLASS__NIL)
            {
                pSigBuilder->AppendToken(GetClassLocal(id)->GetCl());

                INDEBUG(bSomethingResolved = true;)
            }

            if (!again)
                break;
        }
    }

    _ASSERTE(bSomethingResolved);
}

const BYTE* MscorlibBinder::ConvertSignature(LPHARDCODEDMETASIG pHardcodedSig, const BYTE* pSig)
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
void MscorlibBinder::TriggerGCUnderStress()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    _ASSERTE (GetThread ());
    TRIGGERSGC ();
    // Force a GC here because GetClass could trigger GC nondeterminsticly
    if (g_pConfig->GetGCStressLevel() != 0)
    {
        DEBUG_ONLY_REGION();
        Thread * pThread = GetThread ();
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

DWORD MscorlibBinder::GetFieldOffset(BinderFieldID id)
{
    WRAPPER_NO_CONTRACT;

    return GetField(id)->GetOffset(); 
}

#ifndef DACCESS_COMPILE

CrstStatic MscorlibBinder::s_SigConvertCrst;

/*static*/
void MscorlibBinder::Startup()
{
    WRAPPER_NO_CONTRACT
    s_SigConvertCrst.Init(CrstSigConvert);
}

#if defined(_DEBUG) && !defined(CROSSGEN_COMPILE)

// NoClass is used to suppress check for unmanaged and managed size match
#define NoClass char[USHRT_MAX]

const MscorlibBinder::OffsetAndSizeCheck MscorlibBinder::OffsetsAndSizes[] =
{
    #define DEFINE_CLASS_U(nameSpace, stringName, unmanagedType) \
        { PTR_CSTR((TADDR) g_ ## nameSpace ## NS ), PTR_CUTF8((TADDR) # stringName), sizeof(unmanagedType), 0, 0, 0 },
    
    #define DEFINE_FIELD_U(stringName, unmanagedContainingType, unmanagedOffset) \
        { 0, 0, 0, PTR_CUTF8((TADDR) # stringName), offsetof(unmanagedContainingType, unmanagedOffset), sizeof(((unmanagedContainingType*)1)->unmanagedOffset) },
    #include "mscorlib.h"
};

//
// check the basic consistency between mscorlib and mscorwks
//
void MscorlibBinder::Check()
{
    STANDARD_VM_CONTRACT;

    MethodTable * pMT = NULL;

    for (unsigned i = 0; i < NumItems(OffsetsAndSizes); i++)
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
            // This assert will fire if there is DEFINE_FIELD_U macro without preceeding DEFINE_CLASS_U macro in mscorlib.h
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

    char* pUnmanagedSig = NULL;

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
    char* pUnmanagedArg = pUnmanagedSig;
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

            char* pUnmanagedArgEnd = strchr(pUnmanagedArg, ',');

            char* pUnmanagedTypeEnd = (pUnmanagedArgEnd != NULL) ? 
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
            StackScratchBuffer buffer;
            const char * pUnManagedType = ssUnmanagedType.GetANSI(buffer);

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
                    bSigError = !IsStrInArray(pUnmanagedArg, len, aInt32Type, NumItems(aInt32Type));
                }
                else if (argType == ELEMENT_TYPE_U4)
                {
                    bSigError = !IsStrInArray(pUnmanagedArg, len, aUInt32Type, NumItems(aUInt32Type));
                }
                else if (argType == ELEMENT_TYPE_VALUETYPE)
                {
                    // we already did special check for value type
                    bSigError = false;
                }
                else
                {
                    bSigError = IsStrInArray(pUnmanagedArg, len, aType, NumItems(aType));
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

//
// extended check of consistency between mscorlib and mscorwks:
//  - verifies that all references from mscorlib to mscorwks are present
//  - verifies that all references from mscorwks to mscorlib are present
//  - limited detection of mismatches between managed and unmanaged fcall signatures
//
void MscorlibBinder::CheckExtended()
{
    STANDARD_VM_CONTRACT;

    // check the consistency of BCL and VM
    // note: it is not enabled by default because of it is time consuming and 
    // changes the bootstrap sequence of the EE
    if (!CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ConsistencyCheck))
        return;

    //
    // VM referencing BCL (mscorlib.h)
    //
    for (BinderClassID cID = (BinderClassID) 1; (int)cID < m_cClasses; cID = (BinderClassID) (cID + 1)) 
    {
        bool fError = false;
        EX_TRY
        {
            if (MscorlibBinder::GetClassName(cID) != NULL) // Allow for CorSigElement entries with no classes
            {
                if (NULL == MscorlibBinder::GetClass(cID))
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
            printf("CheckExtended: VM expects type to exist:  %s.%s\n", MscorlibBinder::GetClassNameSpace(cID), MscorlibBinder::GetClassName(cID));
        }
    }

    for (BinderMethodID mID = (BinderMethodID) 1; mID < (BinderMethodID) MscorlibBinder::m_cMethods; mID = (BinderMethodID) (mID + 1))
    {
        bool fError = false;
        BinderClassID cID = m_methodDescriptions[mID-1].classID;
        EX_TRY
        {
            if (NULL == MscorlibBinder::GetMethod(mID))
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
            printf("CheckExtended: VM expects method to exist:  %s.%s::%s\n", MscorlibBinder::GetClassNameSpace(cID), MscorlibBinder::GetClassName(cID), MscorlibBinder::GetMethodName(mID));
        }
    }

    for (BinderFieldID fID = (BinderFieldID) 1; fID < (BinderFieldID) MscorlibBinder::m_cFields; fID = (BinderFieldID) (fID + 1))
    {
        bool fError = false;
        BinderClassID cID = m_fieldDescriptions[fID-1].classID;
        EX_TRY
        {
            if (NULL == MscorlibBinder::GetField(fID))
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
            printf("CheckExtended: VM expects field to exist:  %s.%s::%s\n", MscorlibBinder::GetClassNameSpace(cID), MscorlibBinder::GetClassName(cID), MscorlibBinder::GetFieldName(fID));
        }
    }

    //
    // BCL referencing VM (ecall.cpp)
    //
    SetSHash<DWORD> usedECallIds;

    HRESULT hr = S_OK;
    Module *pModule = MscorlibBinder::m_pModule;
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
            printf("CheckExtended: Unable to load class from mscorlib: %s.%s\n", pszNameSpace, pszClassName);
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
            PInvokeStaticSigInfo sigInfo;
            NDirect::PopulateNDirectMethodDesc((NDirectMethodDesc *)pMD, &sigInfo);

            if (pMD->IsQCall())
            {
                id = ((NDirectMethodDesc *)pMD)->GetECallID();
                if (id == 0) {
                    id = ECall::GetIDForMethod(pMD);
                }
            }
            else
            {
                continue;
            }
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

        if (pMD->IsFCall())
        {
            FCallCheckSignature(pMD, ECall::GetFCallImpl(pMD));
        }
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

    _ASSERTE(sizeof(VARIANT) == MscorlibBinder::GetClass(CLASS__NATIVEVARIANT)->GetNativeSize());

    printf("CheckExtended: completed without exception.\n");

ErrExit:
    _ASSERTE(SUCCEEDED(hr));
}

#endif // _DEBUG && !CROSSGEN_COMPILE

extern const MscorlibClassDescription c_rgMscorlibClassDescriptions[];
extern const USHORT c_nMscorlibClassDescriptions;

extern const MscorlibMethodDescription c_rgMscorlibMethodDescriptions[];
extern const USHORT c_nMscorlibMethodDescriptions;

extern const MscorlibFieldDescription c_rgMscorlibFieldDescriptions[];
extern const USHORT c_nMscorlibFieldDescriptions;

#ifdef CROSSGEN_COMPILE
namespace CrossGenMscorlib
{
    extern const MscorlibClassDescription c_rgMscorlibClassDescriptions[];
    extern const USHORT c_nMscorlibClassDescriptions;

    extern const MscorlibMethodDescription c_rgMscorlibMethodDescriptions[];
    extern const USHORT c_nMscorlibMethodDescriptions;

    extern const MscorlibFieldDescription c_rgMscorlibFieldDescriptions[];
    extern const USHORT c_nMscorlibFieldDescriptions;
};
#endif

void MscorlibBinder::AttachModule(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    MscorlibBinder * pGlobalBinder = &g_Mscorlib;

    pGlobalBinder->SetDescriptions(pModule,
        c_rgMscorlibClassDescriptions,  c_nMscorlibClassDescriptions,
        c_rgMscorlibMethodDescriptions, c_nMscorlibMethodDescriptions,
        c_rgMscorlibFieldDescriptions,  c_nMscorlibFieldDescriptions);

#if defined(FEATURE_PREJIT) && !defined(CROSSGEN_COMPILE)
    MscorlibBinder * pPersistedBinder = pModule->m_pBinder;

    if (pPersistedBinder != NULL 
        // Do not use persisted binder for profiling native images. See comment in code:MscorlibBinder::Fixup.
        && !(pModule->GetNativeImage()->GetNativeVersionInfo()->wConfigFlags  & CORCOMPILE_CONFIG_PROFILING))
    {
        pGlobalBinder->m_pClasses = pPersistedBinder->m_pClasses;
        pGlobalBinder->m_pMethods = pPersistedBinder->m_pMethods;
        pGlobalBinder->m_pFields = pPersistedBinder->m_pFields;

        pModule->m_pBinder = pGlobalBinder;
        return;
    }
#endif // FEATURE_PREJIT && CROSSGEN_COMPILE

    pGlobalBinder->AllocateTables();

#ifdef CROSSGEN_COMPILE
    MscorlibBinder * pTargetBinder = (MscorlibBinder *)(void *)
        pModule->GetAssembly()->GetLowFrequencyHeap()
            ->AllocMem(S_SIZE_T(sizeof(MscorlibBinder)));

    pTargetBinder->SetDescriptions(pModule,
        CrossGenMscorlib::c_rgMscorlibClassDescriptions,  CrossGenMscorlib::c_nMscorlibClassDescriptions,
        CrossGenMscorlib::c_rgMscorlibMethodDescriptions, CrossGenMscorlib::c_nMscorlibMethodDescriptions,
        CrossGenMscorlib::c_rgMscorlibFieldDescriptions,  CrossGenMscorlib::c_nMscorlibFieldDescriptions);

    pTargetBinder->AllocateTables();

    pModule->m_pBinder = pTargetBinder;
#else
    pModule->m_pBinder = pGlobalBinder;
#endif
}

void MscorlibBinder::SetDescriptions(Module * pModule, 
    const MscorlibClassDescription * pClassDescriptions, USHORT nClasses,
    const MscorlibMethodDescription * pMethodDescriptions, USHORT nMethods,
    const MscorlibFieldDescription * pFieldDescriptions, USHORT nFields)
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

void MscorlibBinder::AllocateTables()
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

PTR_MethodTable MscorlibBinder::LoadPrimitiveType(CorElementType et)
{
    STANDARD_VM_CONTRACT;

    PTR_MethodTable pMT = g_Mscorlib.m_pClasses[et];

    // Primitive types hit cyclic reference on binder during type loading so we have to load them in two steps
    if (pMT == NULL)
    {
        const MscorlibClassDescription *d = (&g_Mscorlib)->m_classDescriptions + (int)et;

        pMT = ClassLoader::LoadTypeByNameThrowing(GetModule()->GetAssembly(), d->nameSpace, d->name,
            ClassLoader::ThrowIfNotFound, ClassLoader::LoadTypes, CLASS_LOAD_APPROXPARENTS).AsMethodTable();
        g_Mscorlib.m_pClasses[et] = pMT;

        ClassLoader::EnsureLoaded(pMT);
    }

    return pMT;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
void MscorlibBinder::BindAll()
{
    STANDARD_VM_CONTRACT;

    for (BinderClassID cID = (BinderClassID) 1; cID < m_cClasses; cID = (BinderClassID) (cID + 1))
    {
        if (m_classDescriptions[cID].name != NULL) // Allow for CorSigElement entries with no classes
            GetClassLocal(cID);
    }

    for (BinderMethodID mID = (BinderMethodID) 1; mID < m_cMethods; mID = (BinderMethodID) (mID + 1))
        GetMethodLocal(mID);

    for (BinderFieldID fID = (BinderFieldID) 1; fID < m_cFields; fID = (BinderFieldID) (fID + 1))
        GetFieldLocal(fID);
}

void MscorlibBinder::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    image->StoreStructure(this, sizeof(MscorlibBinder),
                          DataImage::ITEM_BINDER);

    image->StoreStructure(m_pClasses, m_cClasses * sizeof(*m_pClasses),
                          DataImage::ITEM_BINDER_ITEMS);

    image->StoreStructure(m_pMethods, m_cMethods * sizeof(*m_pMethods),
                          DataImage::ITEM_BINDER_ITEMS);

    image->StoreStructure(m_pFields, m_cFields * sizeof(*m_pFields),
                          DataImage::ITEM_BINDER_ITEMS);
}

void MscorlibBinder::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    image->FixupPointerField(this, offsetof(MscorlibBinder, m_pModule));

    int i;

    image->FixupPointerField(this, offsetof(MscorlibBinder, m_pClasses));
    for (i = 1; i < m_cClasses; i++) 
    {
#if _DEBUG
        //
        // We do not want to check for restore at runtime for performance reasons.
        // If there is ever a case that requires restore, it should be special
        // cased here and restored explicitly by GetClass/GetField/GetMethod caller.
        //
        // Profiling NGen images force restore for all types. We are still going to save
        // the binder for nidump, but we are not going to use it at runtime.
        //
        if (m_pClasses[i] != NULL && !GetAppDomain()->ToCompilationDomain()->m_fForceProfiling)
        {
            _ASSERTE(!m_pClasses[i]->NeedsRestore(image));
        }
#endif
        image->FixupPointerField(m_pClasses, i * sizeof(m_pClasses[0]));
    }

    image->FixupPointerField(this, offsetof(MscorlibBinder, m_pMethods));
    for (i = 1; i < m_cMethods; i++) 
    {
#if _DEBUG
        // See comment above.
        if (m_pMethods[i] != NULL && !GetAppDomain()->ToCompilationDomain()->m_fForceProfiling)
        {
            _ASSERTE(!m_pMethods[i]->NeedsRestore(image));
        }
#endif

        image->FixupPointerField(m_pMethods, i * sizeof(m_pMethods[0]));
    }

    image->FixupPointerField(this, offsetof(MscorlibBinder, m_pFields));
    for (i = 1; i < m_cFields; i++) 
    {
        image->FixupPointerField(m_pFields, i * sizeof(m_pFields[0]));
    }

    image->ZeroPointerField(this, offsetof(MscorlibBinder, m_classDescriptions));
    image->ZeroPointerField(this, offsetof(MscorlibBinder, m_methodDescriptions));
    image->ZeroPointerField(this, offsetof(MscorlibBinder, m_fieldDescriptions));
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
MscorlibBinder::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    DacEnumMemoryRegion(dac_cast<TADDR>(m_classDescriptions),
                        m_cClasses * sizeof(MscorlibClassDescription));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_methodDescriptions),
                        (m_cMethods - 1) * sizeof(MscorlibMethodDescription));
    DacEnumMemoryRegion(dac_cast<TADDR>(m_fieldDescriptions),
                        (m_cFields - 1) * sizeof(MscorlibFieldDescription));

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

GVAL_IMPL(MscorlibBinder, g_Mscorlib);
