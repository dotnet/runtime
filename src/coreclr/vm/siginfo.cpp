// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// siginfo.cpp
//
// Signature parsing code
//


#include "common.h"

#include "siginfo.hpp"
#include "clsload.hpp"
#include "vars.hpp"
#include "excep.h"
#include "gcheaputilities.h"
#include "field.h"
#include "eeconfig.h"
#include "runtimehandles.h" // for SignatureNative
#include "winwrap.h"
#include <formattype.h>
#include "sigbuilder.h"
#include "../md/compiler/custattr.h"
#include <corhlprpriv.h>
#include "argdestination.h"
#include "multicorejit.h"

/*******************************************************************/
const CorTypeInfo::CorTypeInfoEntry CorTypeInfo::info[ELEMENT_TYPE_MAX] =
{
#define TYPEINFO(enumName,nameSpace,className,size,gcType,isArray,isPrim,isFloat,isModifier,isGenVar) \
    { nameSpace, className, enumName, size, gcType, isArray, isPrim, isFloat, isModifier, isGenVar },
#include "cortypeinfo.h"
#   undef TYPEINFO
};

/*******************************************************************/
/* static */
CorElementType
CorTypeInfo::FindPrimitiveType(LPCUTF8 name)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(name != NULL);

    for (unsigned int i = 1; i < _countof(CorTypeInfo::info); i++)
    {   // can skip ELEMENT_TYPE_END (index 0)
        if ((info[i].className != NULL) && (strcmp(name, info[i].className) == 0))
            return (CorElementType)i;
    }

    return ELEMENT_TYPE_END;
}

const ElementTypeInfo gElementTypeInfo[] = {

#ifdef _DEBUG
#define DEFINEELEMENTTYPEINFO(etname, cbsize, gcness, inreg) {(int)(etname),cbsize,gcness,inreg},
#else
#define DEFINEELEMENTTYPEINFO(etname, cbsize, gcness, inreg) {cbsize,gcness,inreg},
#endif

// Meaning of columns:
//
//     name     - The checked build uses this to verify that the table is sorted
//                correctly. This is a lookup table that uses ELEMENT_TYPE_*
//                as an array index.
//
//     cbsize   - The byte size of this value as returned by SizeOf(). SPECIAL VALUE: -1
//                requires type-specific treatment.
//
//     gc       - 0    no embedded objectrefs
//                1    value is an objectref
//                2    value is an interior pointer - promote it but don't scan it
//                3    requires type-specific treatment
//
//     reg      - put in a register?
//
// Note: This table is very similar to the one in file:corTypeInfo.h with these exceptions:
//  reg column is missing in corTypeInfo.h
//  ELEMENT_TYPE_VAR, ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_MVAR ... size -1 vs. TARGET_POINTER_SIZE in corTypeInfo.h
//  ELEMENT_TYPE_CMOD_REQD, ELEMENT_TYPE_CMOD_OPT, ELEMENT_TYPE_INTERNAL ... size -1 vs. 0 in corTypeInfo.h
//  ELEMENT_TYPE_INTERNAL ... GC type is TYPE_GC_NONE vs. TYPE_GC_OTHER in corTypeInfo.h
//
//                    name                         cbsize                gc             reg
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_END,            -1,                   TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_VOID,           0,                    TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_BOOLEAN,        1,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_CHAR,           2,                    TYPE_GC_NONE,  1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_I1,             1,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_U1,             1,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_I2,             2,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_U2,             2,                    TYPE_GC_NONE,  1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_I4,             4,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_U4,             4,                    TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_I8,             8,                    TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_U8,             8,                    TYPE_GC_NONE,  0)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_R4,             4,                    TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_R8,             8,                    TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_STRING,         TARGET_POINTER_SIZE,  TYPE_GC_REF,   1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_PTR,            TARGET_POINTER_SIZE,  TYPE_GC_NONE,  1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_BYREF,          TARGET_POINTER_SIZE,  TYPE_GC_BYREF, 1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_VALUETYPE,      -1,                   TYPE_GC_OTHER, 0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_CLASS,          TARGET_POINTER_SIZE,  TYPE_GC_REF,   1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_VAR,            -1,                   TYPE_GC_OTHER, 1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_ARRAY,          TARGET_POINTER_SIZE,  TYPE_GC_REF,   1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_GENERICINST,    -1,                   TYPE_GC_OTHER, 0)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_TYPEDBYREF,     TARGET_POINTER_SIZE*2,TYPE_GC_BYREF, 0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_VALUEARRAY_UNSUPPORTED, -1,           TYPE_GC_NONE,  0)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_I,              TARGET_POINTER_SIZE,  TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_U,              TARGET_POINTER_SIZE,  TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_R_UNSUPPORTED,  -1,                   TYPE_GC_NONE,  0)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_FNPTR,          TARGET_POINTER_SIZE,  TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_OBJECT,         TARGET_POINTER_SIZE,  TYPE_GC_REF,   1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_SZARRAY,        TARGET_POINTER_SIZE,  TYPE_GC_REF,   1)

DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_MVAR,           -1,                   TYPE_GC_OTHER, 1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_CMOD_REQD,      -1,                   TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_CMOD_OPT,       -1,                   TYPE_GC_NONE,  1)
DEFINEELEMENTTYPEINFO(ELEMENT_TYPE_INTERNAL,       -1,                   TYPE_GC_NONE,  0)
};

unsigned GetSizeForCorElementType(CorElementType etyp)
{
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(gElementTypeInfo[etyp].m_elementType == etyp);
        return gElementTypeInfo[etyp].m_cbSize;
}

const ElementTypeInfo* GetElementTypeInfo(CorElementType etyp)
{
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(gElementTypeInfo[etyp].m_elementType == etyp);
        return &gElementTypeInfo[etyp];
}

#ifndef DACCESS_COMPILE

void SigPointer::ConvertToInternalExactlyOne(Module* pSigModule, SigTypeContext *pTypeContext, SigBuilder * pSigBuilder, BOOL bSkipCustomModifier)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pSigModule));
    }
    CONTRACTL_END

    SigPointer sigStart = *this;

    CorElementType typ = ELEMENT_TYPE_END;

    // Check whether we need to skip custom modifier
    // Only preserve custom modifier when calculating IL stub hash blob
    if (bSkipCustomModifier)
    {
        // GetElemType eats sentinel and custom modifiers
        IfFailThrowBF(GetElemType(&typ), BFA_BAD_COMPLUS_SIG, pSigModule);
    }
    else
    {
        BYTE byElemType;

        IfFailThrowBF(SkipAnyVASentinel(), BFA_BAD_COMPLUS_SIG, pSigModule);

        // Call GetByte and make sure we don't lose custom modifiers
        IfFailThrowBF(GetByte(&byElemType), BFA_BAD_COMPLUS_SIG, pSigModule);
        typ = (CorElementType) byElemType;
    }

    if (typ == ELEMENT_TYPE_CLASS || typ == ELEMENT_TYPE_VALUETYPE)
    {
        IfFailThrowBF(GetToken(NULL), BFA_BAD_COMPLUS_SIG, pSigModule);
        TypeHandle th = sigStart.GetTypeHandleThrowing(pSigModule, pTypeContext);

        pSigBuilder->AppendElementType(ELEMENT_TYPE_INTERNAL);
        pSigBuilder->AppendPointer(th.AsPtr());
        return;
    }

    if (pTypeContext != NULL)
    {
        uint32_t varNum;
        if (typ == ELEMENT_TYPE_VAR)
        {
            IfFailThrowBF(GetData(&varNum), BFA_BAD_COMPLUS_SIG, pSigModule);
            THROW_BAD_FORMAT_MAYBE(varNum < pTypeContext->m_classInst.GetNumArgs(), BFA_BAD_COMPLUS_SIG, pSigModule);

            pSigBuilder->AppendElementType(ELEMENT_TYPE_INTERNAL);
            pSigBuilder->AppendPointer(pTypeContext->m_classInst[varNum].AsPtr());
            return;
        }
        if (typ == ELEMENT_TYPE_MVAR)
        {
            IfFailThrowBF(GetData(&varNum), BFA_BAD_COMPLUS_SIG, pSigModule);
            THROW_BAD_FORMAT_MAYBE(varNum < pTypeContext->m_methodInst.GetNumArgs(), BFA_BAD_COMPLUS_SIG, pSigModule);

            pSigBuilder->AppendElementType(ELEMENT_TYPE_INTERNAL);
            pSigBuilder->AppendPointer(pTypeContext->m_methodInst[varNum].AsPtr());
            return;
        }
    }

    pSigBuilder->AppendElementType(typ);

    if (!CorIsPrimitiveType(typ))
    {
        switch (typ)
        {
            default:
                THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pSigModule);
                break;
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
                {
                    uint32_t varNum;
                    // Skip variable number
                    IfFailThrowBF(GetData(&varNum), BFA_BAD_COMPLUS_SIG, pSigModule);
                    pSigBuilder->AppendData(varNum);
                }
                break;
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_TYPEDBYREF:
                break;

            case ELEMENT_TYPE_BYREF: //fallthru
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
                ConvertToInternalExactlyOne(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);
                break;

            case ELEMENT_TYPE_FNPTR:
                ConvertToInternalSignature(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);
                break;

            case ELEMENT_TYPE_ARRAY:
                {
                    ConvertToInternalExactlyOne(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);

                    uint32_t rank = 0; // Get rank
                    IfFailThrowBF(GetData(&rank), BFA_BAD_COMPLUS_SIG, pSigModule);
                    pSigBuilder->AppendData(rank);

                    if (rank)
                    {
                        uint32_t nsizes = 0;
                        IfFailThrowBF(GetData(&nsizes), BFA_BAD_COMPLUS_SIG, pSigModule);
                        pSigBuilder->AppendData(nsizes);

                        while (nsizes--)
                        {
                            uint32_t data = 0;
                            IfFailThrowBF(GetData(&data), BFA_BAD_COMPLUS_SIG, pSigModule);
                            pSigBuilder->AppendData(data);
                        }

                        uint32_t nlbounds = 0;
                        IfFailThrowBF(GetData(&nlbounds), BFA_BAD_COMPLUS_SIG, pSigModule);
                        pSigBuilder->AppendData(nlbounds);

                        while (nlbounds--)
                        {
                            uint32_t data = 0;
                            IfFailThrowBF(GetData(&data), BFA_BAD_COMPLUS_SIG, pSigModule);
                            pSigBuilder->AppendData(data);
                        }
                    }
                }
                break;

            case ELEMENT_TYPE_INTERNAL:
                {
                    // this check is not functional in DAC and provides no security against a malicious dump
                    // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
                    if (pSigModule->IsSigInIL(m_ptr))
                        THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pSigModule);
#endif

                    TypeHandle hType;

                    IfFailThrowBF(GetPointer((void**)&hType), BFA_BAD_COMPLUS_SIG, pSigModule);

                    pSigBuilder->AppendPointer(hType.AsPtr());
                }
                break;

            case ELEMENT_TYPE_GENERICINST:
                {
                    TypeHandle genericType = GetGenericInstType(pSigModule);

                    pSigBuilder->AppendElementType(ELEMENT_TYPE_INTERNAL);
                    pSigBuilder->AppendPointer(genericType.AsPtr());

                    uint32_t argCnt = 0; // Get number of parameters
                    IfFailThrowBF(GetData(&argCnt), BFA_BAD_COMPLUS_SIG, pSigModule);
                    pSigBuilder->AppendData(argCnt);

                    while (argCnt--)
                    {
                        ConvertToInternalExactlyOne(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);
                    }
                }
                break;

            // Note: the following is only for correctly computing IL stub hash for modifiers in order to support C++ scenarios
            case ELEMENT_TYPE_CMOD_OPT:
            case ELEMENT_TYPE_CMOD_REQD:
                {
                    mdToken tk;
                    IfFailThrowBF(GetToken(&tk), BFA_BAD_COMPLUS_SIG, pSigModule);
                    TypeHandle th = ClassLoader::LoadTypeDefOrRefThrowing(pSigModule, tk);
                    pSigBuilder->AppendElementType(ELEMENT_TYPE_INTERNAL);
                    pSigBuilder->AppendPointer(th.AsPtr());

                    ConvertToInternalExactlyOne(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);
                }
                break;
        }
    }
}

void SigPointer::ConvertToInternalSignature(Module* pSigModule, SigTypeContext *pTypeContext, SigBuilder * pSigBuilder, BOOL bSkipCustomModifier)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pSigModule));
    }
    CONTRACTL_END

    BYTE uCallConv = 0;
    IfFailThrowBF(GetByte(&uCallConv), BFA_BAD_COMPLUS_SIG, pSigModule);

    if ((uCallConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_FIELD)
        THROW_BAD_FORMAT(BFA_UNEXPECTED_FIELD_SIGNATURE, pSigModule);

    pSigBuilder->AppendByte(uCallConv);

    // Skip type parameter count
    if (uCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        uint32_t nParams = 0;
        IfFailThrowBF(GetData(&nParams), BFA_BAD_COMPLUS_SIG, pSigModule);
        pSigBuilder->AppendData(nParams);
    }

    // Get arg count;
    uint32_t cArgs = 0;
    IfFailThrowBF(GetData(&cArgs), BFA_BAD_COMPLUS_SIG, pSigModule);
    pSigBuilder->AppendData(cArgs);

    cArgs++; // +1 for return type

    // Skip args.
    while (cArgs)
    {
        ConvertToInternalExactlyOne(pSigModule, pTypeContext, pSigBuilder, bSkipCustomModifier);
        cArgs--;
    }
}
#endif // DACCESS_COMPILE


//---------------------------------------------------------------------------------------
//
// Default constructor for creating an empty Signature, i.e. with a NULL raw PCCOR_SIGNATURE pointer.
//

Signature::Signature()
{
    LIMITED_METHOD_CONTRACT;

    m_pSig = NULL;
    m_cbSig = 0;
}

//---------------------------------------------------------------------------------------
//
// Primary constructor for creating a Signature.
//
// Arguments:
//    pSig  - raw PCCOR_SIGNATURE pointer
//    cbSig - length of the signature
//

Signature::Signature(PCCOR_SIGNATURE pSig,
                     DWORD           cbSig)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;   // host-only data structure - not marshalled

    m_pSig = pSig;
    m_cbSig = cbSig;
}

//---------------------------------------------------------------------------------------
//
// Check if the Signature is empty, i.e. has a NULL raw PCCOR_SIGNATURE
//
// Return Value:
//    TRUE if the raw PCCOR_SIGNATURE is NULL
//

BOOL Signature::IsEmpty() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return (m_pSig == NULL);
}

//---------------------------------------------------------------------------------------
//
// Create a SigParser over the Signature.  In DAC builds, grab the signature bytes from out of process first.
//
// Return Value:
//    a SigpParser for this particular Signature
//

SigParser Signature::CreateSigParser() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#if defined(DACCESS_COMPILE)
    // Copy the signature bytes from the target process.
    PCCOR_SIGNATURE pTargetSig = (PCCOR_SIGNATURE)DacInstantiateTypeByAddress((TADDR)m_pSig, m_cbSig, true);
    return SigParser(pTargetSig, m_cbSig);
#else  // !DACCESS_COMPILE
    return SigParser(m_pSig, m_cbSig);
#endif // !DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
// Create a SigPointer over the Signature.  In DAC builds, grab the signature bytes from out of process first.
//
// Return Value:
//    a SigPointer for this particular Signature
//

SigPointer Signature::CreateSigPointer() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#if defined(DACCESS_COMPILE)
    // Copy the signature bytes from the target process.
    PCCOR_SIGNATURE pTargetSig = (PCCOR_SIGNATURE)DacInstantiateTypeByAddress((TADDR)m_pSig, m_cbSig, true);
    return SigPointer(pTargetSig, m_cbSig);
#else  // !DACCESS_COMPILE
    return SigPointer(m_pSig, m_cbSig);
#endif // !DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
// Pretty-print the Signature.  This is just a wrapper over code:PrettyPrintSig().
//
// Arguments:
//    pszMethodName - the name of the method in question
//    pqbOut        - a CQuickBytes array for allocating memory
//    pIMDI         - a IMDInternalImport interface for resolving tokens
//
// Return Value:
//    whatever PrettyPrintSig() returns
//

void Signature::PrettyPrint(const CHAR * pszMethodName,
                            CQuickBytes * pqbOut,
                            IMDInternalImport * pIMDI) const
{
    WRAPPER_NO_CONTRACT;
    PrettyPrintSig(this->GetRawSig(), this->GetRawSigLen(), pszMethodName, pqbOut, pIMDI, NULL);
}

//---------------------------------------------------------------------------------------
//
// Get the raw signature pointer contained in this Siganture.
//
// Return Value:
//    the raw signature pointer
//
// Notes:
//    Use this ONLY IF there is no other way to do what you want to do!
//    In most cases you just want a SigParser/SigPointer from the Signature.
//

PCCOR_SIGNATURE Signature::GetRawSig() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return m_pSig;
}

//---------------------------------------------------------------------------------------
//
// Get the length of the raw signature contained in this Siganture.
//
// Return Value:
//    the length of the raw signature
//
// Notes:
//    Use this ONLY IF there is no other way to do what you want to do!
//    In most cases you just want a SigParser/SigPointer from the Signature.
//

DWORD Signature::GetRawSigLen() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_cbSig;
}


//---------------------------------------------------------------------------------------
//
// Constructor.
//
void MetaSig::Init(
    PCCOR_SIGNATURE        szMetaSig,
    DWORD                  cbMetaSig,
    Module *               pModule,
    const SigTypeContext * pTypeContext,
    MetaSigKind            kind)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(szMetaSig));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pTypeContext, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACTL_END


#ifdef _DEBUG
    FillMemory(this, sizeof(*this), 0xcc);
#endif

    // Copy the type context
    SigTypeContext::InitTypeContext(pTypeContext,&m_typeContext);
    m_pModule = pModule;

    SigPointer psig(szMetaSig, cbMetaSig);

    HRESULT hr;

    switch (kind)
    {
        case sigLocalVars:
        {
            uint32_t data = 0;
            IfFailGo(psig.GetCallingConvInfo(&data)); // Store calling convention
            m_CallConv = (BYTE)data;

            IfFailGo(psig.GetData(&data));  // Store number of arguments.
            m_nArgs = data;

            m_pRetType = SigPointer(NULL, 0);
            break;
        }
        case sigMember:
        {
            uint32_t data = 0;
            IfFailGo(psig.GetCallingConvInfo(&data)); // Store calling convention
            m_CallConv = (BYTE)data;

            // Store type parameter count
            if (m_CallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                IfFailGo(psig.GetData(NULL));
            }

            IfFailGo(psig.GetData(&data));  // Store number of arguments.
            m_nArgs = data;
            m_pRetType = psig;
            IfFailGo(psig.SkipExactlyOne());
            break;
        }
        case sigField:
        {
            uint32_t data = 0;
            IfFailGo(psig.GetCallingConvInfo(&data)); // Store calling convention
            m_CallConv = (BYTE)data;

            m_nArgs = 1; //There's only 1 'arg' - the type.
            m_pRetType = SigPointer(NULL, 0);
            break;
        }
        default:
        {
            UNREACHABLE();
            goto ErrExit;
        }
    }


    m_pStart = psig;

    m_flags = 0;

    // Reset the iterator fields
    Reset();

    return;

ErrExit:
    // Invalid signature or parameter
    m_CallConv = 0;
    INDEBUG(m_CallConv = 0xff;)

    m_nArgs = 0;
    m_pRetType = SigPointer(NULL, 0);
} // MetaSig::MetaSig


// Helper constructor that constructs a method signature MetaSig from a MethodDesc
// IMPORTANT: if classInst/methodInst is omitted and the MethodDesc is shared between generic
// instantiations then the instantiation info for the method will be representative.  This
// is OK for GC, field layout etc. but not OK where exact types matter.
//
// Also, if used on a shared instantiated method descriptor or instance method in a shared generic struct
// then the calling convention is fixed up to include the extra dictionary argument
//
// For method descs from array types the "instantiation" is set to the element type of the array
// This lets us use VAR in the signatures for Get, Set and Address
MetaSig::MetaSig(MethodDesc *pMD, Instantiation classInst, Instantiation methodInst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SigTypeContext typeContext(pMD, classInst, methodInst);

    PCCOR_SIGNATURE pSig;
    DWORD cbSigSize;
    pMD->GetSig(&pSig, &cbSigSize);

    Init(pSig, cbSigSize, pMD->GetModule(),&typeContext);

    if (pMD->RequiresInstArg())
        SetHasParamTypeArg();
}

MetaSig::MetaSig(MethodDesc *pMD, TypeHandle declaringType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SigTypeContext typeContext(pMD, declaringType);
    PCCOR_SIGNATURE pSig;
    DWORD cbSigSize;
    pMD->GetSig(&pSig, &cbSigSize);

    Init(pSig, cbSigSize, pMD->GetModule(),&typeContext);

    if (pMD->RequiresInstArg())
        SetHasParamTypeArg();
}

#ifdef _DEBUG
//*******************************************************************************
static BOOL MethodDescMatchesSig(MethodDesc* pMD, PCCOR_SIGNATURE pSig, DWORD cSig, Module * pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    PCCOR_SIGNATURE pSigOfMD;
    DWORD cSigOfMD;
    pMD->GetSig(&pSigOfMD, &cSigOfMD);

    return MetaSig::CompareMethodSigs(pSig, cSig, pModule, NULL,
                                      pSigOfMD, cSigOfMD, pMD->GetModule(), NULL, FALSE);
}
#endif // _DEBUG

MetaSig::MetaSig(BinderMethodID id)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    Signature sig = CoreLibBinder::GetMethodSignature(id);

    _ASSERTE(MethodDescMatchesSig(CoreLibBinder::GetMethod(id),
        sig.GetRawSig(), sig.GetRawSigLen(), CoreLibBinder::GetModule()));

    Init(sig.GetRawSig(), sig.GetRawSigLen(), CoreLibBinder::GetModule(), NULL);
}

MetaSig::MetaSig(LPHARDCODEDMETASIG pwzMetaSig)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    Signature sig = CoreLibBinder::GetSignature(pwzMetaSig);

    Init(sig.GetRawSig(), sig.GetRawSigLen(), CoreLibBinder::GetModule(), NULL);
}

// Helper constructor that constructs a field signature MetaSig from a FieldDesc
// IMPORTANT: the classInst is omitted then the instantiation info for the field
// will be representative only as FieldDescs can be shared
//
MetaSig::MetaSig(FieldDesc *pFD, TypeHandle declaringType)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END

    PCCOR_SIGNATURE pSig;
    DWORD           cSig;

    pFD->GetSig(&pSig, &cSig);

    SigTypeContext typeContext(pFD, declaringType);

    Init(pSig, cSig, pFD->GetModule(),&typeContext, sigField);
}

//---------------------------------------------------------------------------------------
//
// Returns type of current argument index. Returns ELEMENT_TYPE_END
// if already past end of arguments.
//
CorElementType
MetaSig::PeekArg() const
{
    WRAPPER_NO_CONTRACT;

    if (m_iCurArg == m_nArgs)
    {
        return ELEMENT_TYPE_END;
    }
    return m_pWalk.PeekElemTypeClosed(GetModule(), &m_typeContext);
}

//---------------------------------------------------------------------------------------
//
// Returns type of current argument index. Returns ELEMENT_TYPE_END
// if already past end of arguments.
//
CorElementType
MetaSig::PeekArgNormalized(TypeHandle * pthValueType) const
{
    WRAPPER_NO_CONTRACT;

    if (m_iCurArg == m_nArgs)
    {
        return ELEMENT_TYPE_END;
    }
    return m_pWalk.PeekElemTypeNormalized(m_pModule, &m_typeContext, pthValueType);
}

//---------------------------------------------------------------------------------------
//
// Returns type of current argument, then advances the argument
// index. Returns ELEMENT_TYPE_END if already past end of arguments.
//
CorElementType
MetaSig::NextArg()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    m_pLastType = m_pWalk;

    if (m_iCurArg == m_nArgs)
    {
        return ELEMENT_TYPE_END;
    }
    m_iCurArg++;
    CorElementType mt = m_pWalk.PeekElemTypeClosed(GetModule(), &m_typeContext);
    if (FAILED(m_pWalk.SkipExactlyOne()))
    {
        m_pWalk = m_pLastType;
        return ELEMENT_TYPE_END;
    }
    return mt;
}

//---------------------------------------------------------------------------------------
//
// Advance the argument index. Can be used with GetArgProps() to
// to iterate when you do not have a valid type context
//
void
MetaSig::SkipArg()
{
    WRAPPER_NO_CONTRACT;

    m_pLastType = m_pWalk;

    if (m_iCurArg < m_nArgs)
    {
        m_iCurArg++;
        if (FAILED(m_pWalk.SkipExactlyOne()))
        {
            m_pWalk = m_pLastType;
            m_iCurArg = m_nArgs;
        }
    }
}

//---------------------------------------------------------------------------------------
//
// reset: goto start pos
//
VOID
MetaSig::Reset()
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_pWalk = m_pStart;
    m_iCurArg  = 0;
    return;
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
BOOL
IsTypeRefOrDef(
    LPCSTR   szClassName,
    Module * pModule,
    mdToken  token)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END

    LPCUTF8  pclsname;
    LPCUTF8 pszNamespace;

    IMDInternalImport *pInternalImport = pModule->GetMDImport();

    if (TypeFromToken(token) == mdtTypeDef)
    {
        if (FAILED(pInternalImport->GetNameOfTypeDef(token, &pclsname, &pszNamespace)))
        {
            return false;
        }
    }
    else if (TypeFromToken(token) == mdtTypeRef)
    {
        if (FAILED(pInternalImport->GetNameOfTypeRef(token, &pszNamespace, &pclsname)))
        {
            return false;
        }
    }
    else
    {
            return false;
    }

    // If the namespace is not the same.
    int iLen = (int)strlen(pszNamespace);
    if (iLen)
    {
        if (strncmp(szClassName, pszNamespace, iLen) != 0)
            return(false);

        if (szClassName[iLen] != NAMESPACE_SEPARATOR_CHAR)
            return(false);
        ++iLen;
    }

    if (strcmp(&szClassName[iLen], pclsname) != 0)
        return(false);
    return(true);
} // IsTypeRefOrDef

TypeHandle SigPointer::GetTypeHandleNT(Module* pModule,
                                       const SigTypeContext *pTypeContext) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    TypeHandle th;
    EX_TRY
    {
        th = GetTypeHandleThrowing(pModule, pTypeContext);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
    return(th);
}

#endif // #ifndef DACCESS_COMPILE


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

// Method: TypeHandle SigPointer::GetTypeHandleThrowing()
// pZapSigContext is only set when decoding zapsigs
//
TypeHandle SigPointer::GetTypeHandleThrowing(
                 Module *                    pModule,
                 const SigTypeContext *      pTypeContext,
                 ClassLoader::LoadTypesFlag  fLoadTypes/*=LoadTypes*/,
                 ClassLoadLevel              level/*=CLASS_LOADED*/,
                 BOOL                        dropGenericArgumentLevel/*=FALSE*/,
                 const Substitution *        pSubst/*=NULL*/,
                 // ZapSigContext is only set when decoding zapsigs
                 const ZapSig::Context *     pZapSigContext,
                 MethodTable *               pMTInterfaceMapOwner) const
{
    CONTRACT(TypeHandle)
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != ClassLoader::LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == ClassLoader::LoadTypes) ? NULL_NOT_OK : NULL_OK)));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    // We have an invariant that before we call a method, we must have loaded all of the valuetype parameters of that
    // method visible from the signature of the method. Normally we do this via type loading before the method is called
    // by walking the signature of the callee method at jit time, and loading all of the valuetype arguments at that time.
    // For NGEN, we record which valuetypes need to be loaded, and force load those types when the caller method is first executed.
    // However, in certain circumstances involving generics the jit does not have the opportunity to observe the complete method
    // signature that may be used a signature walk time. See example below.
    //
    //
//using System;
//
//struct A<T> { }
//struct B<T> { }
//
//interface Interface<T>
//{ A<T> InterfaceFunc(); }
//
//class Base<T>
//{ public virtual B<T> Func() { return default(B<T>); }  }
//
//class C<U,T> where U:Base<T>, Interface<T>
//{
//  public static void CallFunc(U u) { u.Func(); }
//  public static void CallInterfaceFunc(U u) { u.InterfaceFunc(); }
//}
//
//class Problem : Base<object>, Interface<object>
//{
//    public A<object> InterfaceFunc() { return new A<object>(); }
//    public override B<object> Func() { return new B<object>(); }
//}
//
//class Test
//{
//    static void Main()
//    {
//        C<Problem, object>.CallFunc(new Problem());
//        C<Problem, object>.CallInterfaceFunc(new Problem());
//    }
//}
//
    // In this example, when CallFunc and CallInterfaceFunc are jitted, the types that will
    // be loaded during JIT time are A<__Canon> and <__Canon>. Thus we need to be able to only
    // search for canonical type arguments during these restricted time periods. IsGCThread() || IsStackWalkerThread() is the current
    // predicate for determining this.

#ifdef _DEBUG
    if ((IsGCThread() || IsStackWalkerThread()) && (fLoadTypes == ClassLoader::LoadTypes))
    {
        // The callers are expected to pass the right arguments in
        _ASSERTE(level == CLASS_LOAD_APPROXPARENTS);
        _ASSERTE(dropGenericArgumentLevel == TRUE);
    }
#endif

    TypeHandle thRet;
    SigPointer     psig = *this;
    CorElementType typ = ELEMENT_TYPE_END;
    IfFailThrowBF(psig.GetElemType(&typ), BFA_BAD_SIGNATURE, pModule);

    if ((typ < ELEMENT_TYPE_MAX) &&
        (CorTypeInfo::IsPrimitiveType_NoThrow(typ) || (typ == ELEMENT_TYPE_STRING) || (typ == ELEMENT_TYPE_OBJECT)))
    {
        // case ELEMENT_TYPE_VOID     = 0x01,
        // case ELEMENT_TYPE_BOOLEAN  = 0x02,
        // case ELEMENT_TYPE_CHAR     = 0x03,
        // case ELEMENT_TYPE_I1       = 0x04,
        // case ELEMENT_TYPE_U1       = 0x05,
        // case ELEMENT_TYPE_I2       = 0x06,
        // case ELEMENT_TYPE_U2       = 0x07,
        // case ELEMENT_TYPE_I4       = 0x08,
        // case ELEMENT_TYPE_U4       = 0x09,
        // case ELEMENT_TYPE_I8       = 0x0a,
        // case ELEMENT_TYPE_U8       = 0x0b,
        // case ELEMENT_TYPE_R4       = 0x0c,
        // case ELEMENT_TYPE_R8       = 0x0d,
        // case ELEMENT_TYPE_I        = 0x18,
        // case ELEMENT_TYPE_U        = 0x19,
        //
        // case ELEMENT_TYPE_STRING   = 0x0e,
        // case ELEMENT_TYPE_OBJECT   = 0x1c,
        //
        thRet = TypeHandle(CoreLibBinder::GetElementType(typ));
    }
    else
    {
#ifdef _DEBUG_IMPL
        // This verifies that we won't try and load a type
        // if FORBIDGC_LOADER_USE_ENABLED is true.
        //
        // The FORBIDGC_LOADER_USE is limited to very specific scenarios that need to retrieve
        // GC_OTHER typehandles for size and gcroot information. This assert attempts to prevent
        // this abuse from proliferating.
        //
        if (FORBIDGC_LOADER_USE_ENABLED() && (fLoadTypes == ClassLoader::LoadTypes))
        {
            TypeHandle th = GetTypeHandleThrowing(pModule,
                                                  pTypeContext,
                                                  ClassLoader::DontLoadTypes,
                                                  level,
                                                  dropGenericArgumentLevel,
                                                  pSubst,
                                                  pZapSigContext);
            _ASSERTE(!th.IsNull());
        }
#endif
        //
        // pOrigModule is the original module that contained this ZapSig
        //
        Module * pOrigModule = (pZapSigContext != NULL) ? pZapSigContext->pInfoModule : pModule;

        ClassLoader::NotFoundAction  notFoundAction;
        CorInternalStates            tdTypes;

        switch((DWORD)typ) {
        case ELEMENT_TYPE_TYPEDBYREF:
        {
            thRet = TypeHandle(g_TypedReferenceMT);
            break;
        }

        case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
        {
#ifndef DACCESS_COMPILE
            TypeHandle baseType = psig.GetTypeHandleThrowing(pModule,
                                                             pTypeContext,
                                                             fLoadTypes,
                                                             level,
                                                             dropGenericArgumentLevel,
                                                             pSubst,
                                                             pZapSigContext);
            if (baseType.IsNull())
            {
                thRet = baseType;
            }
            else
            {
                thRet = ClassLoader::LoadNativeValueTypeThrowing(baseType, fLoadTypes, level);
            }
#else
            DacNotImpl();
            thRet = TypeHandle();
#endif
            break;
        }

        case ELEMENT_TYPE_CANON_ZAPSIG:
        {
#ifndef DACCESS_COMPILE
            assert(g_pCanonMethodTableClass != NULL);
            thRet = TypeHandle(g_pCanonMethodTableClass);
#else
            DacNotImpl();
            thRet = TypeHandle();
#endif
            break;
        }

        case ELEMENT_TYPE_MODULE_ZAPSIG:
        {
#ifndef DACCESS_COMPILE
            uint32_t ix;
            IfFailThrowBF(psig.GetData(&ix), BFA_BAD_SIGNATURE, pModule);
#ifdef FEATURE_MULTICOREJIT
            if (pZapSigContext->externalTokens == ZapSig::MulticoreJitTokens)
            {
                pModule = MulticoreJitManager::DecodeModuleFromIndex(pZapSigContext->pModuleContext, ix);
            }
            else
#endif
            {
                pModule = pZapSigContext->GetZapSigModule()->GetModuleFromIndex(ix);
            }

            if ((pModule != NULL) && pModule->IsInCurrentVersionBubble())
            {
                thRet = psig.GetTypeHandleThrowing(pModule,
                                                   pTypeContext,
                                                   fLoadTypes,
                                                   level,
                                                   dropGenericArgumentLevel,
                                                   pSubst,
                                                   pZapSigContext);
            }
            else
            {
                // For ReadyToRunCompilation we return a null TypeHandle when we reference a non-local module
                //
                thRet = TypeHandle();
            }
#else
            DacNotImpl();
            thRet = TypeHandle();
#endif
            break;
        }

        case ELEMENT_TYPE_VAR_ZAPSIG:
        {
#ifndef DACCESS_COMPILE
            RID rid;
            IfFailThrowBF(psig.GetData(&rid), BFA_BAD_SIGNATURE, pModule);

            mdGenericParam tkTyPar = TokenFromRid(rid, mdtGenericParam);

            TypeVarTypeDesc *pTypeVarTypeDesc = pModule->LookupGenericParam(tkTyPar);
            if (pTypeVarTypeDesc == NULL && (fLoadTypes == ClassLoader::LoadTypes))
            {
                mdToken tkOwner;
                IfFailThrow(pModule->GetMDImport()->GetGenericParamProps(tkTyPar, NULL, NULL, &tkOwner, NULL, NULL));

                if (TypeFromToken(tkOwner) == mdtMethodDef)
                {
                    MemberLoader::GetMethodDescFromMethodDef(pModule, tkOwner, FALSE);
                }
                else
                {
                    ClassLoader::LoadTypeDefThrowing(pModule, tkOwner,
                        ClassLoader::ThrowIfNotFound,
                        ClassLoader::PermitUninstDefOrRef);
                }

                pTypeVarTypeDesc = pModule->LookupGenericParam(tkTyPar);
                if (pTypeVarTypeDesc == NULL)
                {
                    THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pOrigModule);
                }
            }
            thRet = TypeHandle(pTypeVarTypeDesc);
#else
            DacNotImpl();
            thRet = TypeHandle();
#endif
            break;
        }

        case ELEMENT_TYPE_VAR:
        {
            if ((pSubst != NULL) && !pSubst->GetInst().IsNull())
            {
#ifdef _DEBUG_IMPL
                _ASSERTE(!FORBIDGC_LOADER_USE_ENABLED());
#endif
                uint32_t index;
                IfFailThrow(psig.GetData(&index));

                SigPointer inst = pSubst->GetInst();
                for (uint32_t i = 0; i < index; i++)
                {
                    IfFailThrowBF(inst.SkipExactlyOne(), BFA_BAD_SIGNATURE, pOrigModule);
                }

                thRet =  inst.GetTypeHandleThrowing(
                    pSubst->GetModule(),
                    pTypeContext,
                    fLoadTypes,
                    level,
                    dropGenericArgumentLevel,
                    pSubst->GetNext(),
                    pZapSigContext);
            }
            else
            {
                thRet = (psig.GetTypeVariableThrowing(pModule, typ, fLoadTypes, pTypeContext));
                if (fLoadTypes == ClassLoader::LoadTypes)
                    ClassLoader::EnsureLoaded(thRet, level);
            }
            break;
        }

        case ELEMENT_TYPE_MVAR:
        {
            thRet = (psig.GetTypeVariableThrowing(pModule, typ, fLoadTypes, pTypeContext));
            if (fLoadTypes == ClassLoader::LoadTypes)
                ClassLoader::EnsureLoaded(thRet, level);
            break;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            mdTypeDef tkGenericType = mdTypeDefNil;
            Module *pGenericTypeModule = NULL;

            // Before parsing the generic instantiation, determine if the signature tells us its module and token.
            // This is the common case, and when true we can avoid dereferencing the resulting TypeHandle to ask for them.
            bool typeAndModuleKnown = false;
            if (pZapSigContext && pZapSigContext->externalTokens == ZapSig::NormalTokens && psig.IsTypeDef(&tkGenericType))
            {
                typeAndModuleKnown = true;
                pGenericTypeModule = pModule;
            }

            TypeHandle genericType = psig.GetGenericInstType(pModule, fLoadTypes, level < CLASS_LOAD_APPROXPARENTS ? level : CLASS_LOAD_APPROXPARENTS, pZapSigContext);

            if (genericType.IsNull())
            {
                thRet = genericType;
                break;
            }

            if (!typeAndModuleKnown)
            {
                tkGenericType = genericType.GetCl();
                pGenericTypeModule = genericType.GetModule();
            }
            else
            {
                _ASSERTE(tkGenericType == genericType.GetCl());
                _ASSERTE(pGenericTypeModule == genericType.GetModule());
            }

            if (level == CLASS_LOAD_APPROXPARENTS && dropGenericArgumentLevel && genericType.IsInterface())
            {
                thRet = genericType;
                break;
            }

            // The number of type parameters follows
            uint32_t ntypars = 0;
            IfFailThrowBF(psig.GetData(&ntypars), BFA_BAD_SIGNATURE, pOrigModule);

            DWORD dwAllocaSize = 0;
            if (!ClrSafeInt<DWORD>::multiply(ntypars, sizeof(TypeHandle), dwAllocaSize))
                ThrowHR(COR_E_OVERFLOW);

            TypeHandle *thisinst = (TypeHandle*) _alloca(dwAllocaSize);

            // Finally we gather up the type arguments themselves, loading at the level specified for generic arguments
            for (unsigned i = 0; i < ntypars; i++)
            {
                ClassLoadLevel argLevel = level;
                TypeHandle typeHnd = TypeHandle();
                BOOL argDrop = FALSE;

                if (dropGenericArgumentLevel)
                {
                    if (level == CLASS_LOAD_APPROXPARENTS)
                    {
                        SigPointer tempsig = psig;

                        CorElementType elemType = ELEMENT_TYPE_END;
                        IfFailThrowBF(tempsig.GetElemType(&elemType), BFA_BAD_SIGNATURE, pOrigModule);

                        if (elemType == (CorElementType) ELEMENT_TYPE_MODULE_ZAPSIG)
                        {
                            // Skip over the module index
                            IfFailThrowBF(tempsig.GetData(NULL), BFA_BAD_SIGNATURE, pModule);
                            // Read the next elemType
                            IfFailThrowBF(tempsig.GetElemType(&elemType), BFA_BAD_SIGNATURE, pModule);
                        }

                        if (elemType == ELEMENT_TYPE_GENERICINST)
                        {
                            CorElementType tmpEType = ELEMENT_TYPE_END;
                            IfFailThrowBF(tempsig.PeekElemType(&tmpEType), BFA_BAD_SIGNATURE, pOrigModule);

                            if (tmpEType == ELEMENT_TYPE_CLASS)
                                typeHnd = TypeHandle(g_pCanonMethodTableClass);
                        }
                        else if ((elemType == (CorElementType)ELEMENT_TYPE_CANON_ZAPSIG) ||
                                 (CorTypeInfo::GetGCType_NoThrow(elemType) == TYPE_GC_REF))
                        {
                            typeHnd = TypeHandle(g_pCanonMethodTableClass);
                        }

                        argDrop = TRUE;
                    }
                    else
                    // We need to make sure that typekey is always restored. Otherwise, we may run into unrestored typehandles while using
                    // the typekey for lookups. It is safe to not drop the levels for initial NGen-specific loading levels since there cannot
                    // be cycles in typekeys.
                    if (level > CLASS_LOAD_APPROXPARENTS)
                    {
                        argLevel = (ClassLoadLevel) (level-1);
                    }
                }

                if (typeHnd.IsNull())
                {
                    typeHnd = psig.GetTypeHandleThrowing(pOrigModule,
                                                         pTypeContext,
                                                         fLoadTypes,
                                                         argLevel,
                                                         argDrop,
                                                         pSubst,
                                                         pZapSigContext);
                    if (typeHnd.IsNull())
                    {
                        // Indicate failure by setting thisinst to NULL
                        thisinst = NULL;
                        break;
                    }

                    if (dropGenericArgumentLevel && level == CLASS_LOAD_APPROXPARENTS)
                    {
                        typeHnd = ClassLoader::CanonicalizeGenericArg(typeHnd);
                    }
                }
                thisinst[i] = typeHnd;
                IfFailThrowBF(psig.SkipExactlyOne(), BFA_BAD_SIGNATURE, pOrigModule);
            }

            // If we failed to get all of the instantiation type arguments then we return the null type handle
            if (thisinst == NULL)
            {
                thRet = TypeHandle();
                break;
            }

            Instantiation genericLoadInst(thisinst, ntypars);

            if (pMTInterfaceMapOwner != NULL && genericLoadInst.ContainsAllOneType(pMTInterfaceMapOwner))
            {
                thRet = ClassLoader::LoadTypeDefThrowing(pGenericTypeModule, tkGenericType, ClassLoader::ThrowIfNotFound, ClassLoader::PermitUninstDefOrRef, 0, level);
            }
            else
            {
                // Group together the current signature type context and substitution chain, which
                // we may later use to instantiate constraints of type arguments that turn out to be
                // typespecs, i.e. generic types.
                InstantiationContext instContext(pTypeContext, pSubst);

                // Now make the instantiated type
                // The class loader will check the arity
                // When we know it was correctly computed at NGen time, we ask the class loader to skip that check.
                thRet = (ClassLoader::LoadGenericInstantiationThrowing(pGenericTypeModule,
                                                                    tkGenericType,
                                                                    genericLoadInst,
                                                                    fLoadTypes, level,
                                                                    &instContext,
                                                                    pZapSigContext && pZapSigContext->externalTokens == ZapSig::NormalTokens));
            }
            break;
        }

        case ELEMENT_TYPE_CLASS:
            // intentional fallthru to ELEMENT_TYPE_VALUETYPE
        case ELEMENT_TYPE_VALUETYPE:
        {
            mdTypeRef typeToken = 0;

            IfFailThrowBF(psig.GetToken(&typeToken), BFA_BAD_SIGNATURE, pOrigModule);

#if defined(FEATURE_NATIVE_IMAGE_GENERATION) && !defined(DACCESS_COMPILE)
            if ((pOrigModule != pModule) && (pZapSigContext->externalTokens == ZapSig::IbcTokens))
            {
                // ibcExternalType tokens are actually encoded as mdtTypeDef tokens in the signature
                RID            typeRid  = RidFromToken(typeToken);
                idExternalType ibcToken = RidToToken(typeRid, ibcExternalType);
                typeToken = pOrigModule->LookupIbcTypeToken(pModule, ibcToken);

                if (IsNilToken(typeToken))
                {
                    COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_TYPE);
                }
            }
#endif

            if ((TypeFromToken(typeToken) != mdtTypeRef) && (TypeFromToken(typeToken) != mdtTypeDef))
                THROW_BAD_FORMAT(BFA_UNEXPECTED_TOKEN_AFTER_CLASSVALTYPE, pOrigModule);

            if (IsNilToken(typeToken))
                THROW_BAD_FORMAT(BFA_UNEXPECTED_TOKEN_AFTER_CLASSVALTYPE, pOrigModule);

            if (fLoadTypes == ClassLoader::LoadTypes)
            {
                notFoundAction = ClassLoader::ThrowButNullV11McppWorkaround;
                tdTypes = tdNoTypes;
            }
            else
            {
                notFoundAction = ClassLoader::ReturnNullIfNotFound;
                tdTypes = tdAllTypes;
            }

            TypeHandle loadedType =
                ClassLoader::LoadTypeDefOrRefThrowing(pModule,
                                                      typeToken,
                                                      notFoundAction,
                                                      // pZapSigContext is only set when decoding zapsigs
                                                      // ZapSigs use uninstantiated tokens to represent the GenericTypeDefinition
                                                      (pZapSigContext ? ClassLoader::PermitUninstDefOrRef : ClassLoader::FailIfUninstDefOrRef),
                                                      tdTypes,
                                                      level);

            // Everett C++ compiler can generate a TypeRef with RS=0 without respective TypeDef for unmanaged valuetypes,
            // referenced only by pointers to them. For this case we treat this as an ELEMENT_TYPE_VOID, and perform the
            // same operations as the appropriate case block above.
            if (loadedType.IsNull())
            {
                if (TypeFromToken(typeToken) == mdtTypeRef)
                {
                        loadedType = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_VOID));
                        thRet = loadedType;
                        break;
                }
            }

#ifndef DACCESS_COMPILE
            //
            // Check that the type that we loaded matches the signature
            //   with regards to ET_CLASS and ET_VALUETYPE
            //
            if (fLoadTypes == ClassLoader::LoadTypes)
            {
                // Skip this check when using zap sigs; it should have been correctly computed at NGen time
                // and a change from one to the other would have invalidated the image.
                if (pZapSigContext == NULL || pZapSigContext->externalTokens != ZapSig::NormalTokens)
                {
                    bool typFromSigIsClass = (typ == ELEMENT_TYPE_CLASS);
                    bool typLoadedIsClass  = (loadedType.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS);

                    if (typFromSigIsClass != typLoadedIsClass)
                    {
                        if (pModule->GetMDImport()->GetMetadataStreamVersion() != MD_STREAM_VER_1X)
                        {
                            pOrigModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(),
                                                                                typeToken,
                                                                                BFA_CLASSLOAD_VALUETYPEMISMATCH);
                        }
                    }
                }

                // Assert that our reasoning above was valid (that there is never a zapsig that gets this wrong)
                _ASSERTE(((typ == ELEMENT_TYPE_CLASS) == (loadedType.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS)) ||
                          pZapSigContext == NULL || pZapSigContext->externalTokens != ZapSig::NormalTokens);

            }
#endif // #ifndef DACCESS_COMPILE

            thRet = loadedType;
            break;
        }

        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        {
            TypeHandle elemType = psig.GetTypeHandleThrowing(pModule,
                                                             pTypeContext,
                                                             fLoadTypes,
                                                             level,
                                                             dropGenericArgumentLevel,
                                                             pSubst,
                                                             pZapSigContext);
            if (elemType.IsNull())
            {
                    thRet = elemType;
                    break;
            }

            uint32_t rank = 0;
            if (typ == ELEMENT_TYPE_ARRAY) {
                IfFailThrowBF(psig.SkipExactlyOne(), BFA_BAD_SIGNATURE, pOrigModule);
                IfFailThrowBF(psig.GetData(&rank), BFA_BAD_SIGNATURE, pOrigModule);

                _ASSERTE(0 < rank);
            }
            thRet = ClassLoader::LoadArrayTypeThrowing(elemType, typ, rank, fLoadTypes, level);
                break;
        }

        case ELEMENT_TYPE_PINNED:
            // Return what follows
            thRet = psig.GetTypeHandleThrowing(pModule,
                                               pTypeContext,
                                               fLoadTypes,
                                               level,
                                               dropGenericArgumentLevel,
                                               pSubst,
                                               pZapSigContext);
                break;

        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_PTR:
        {
            TypeHandle baseType = psig.GetTypeHandleThrowing(pModule,
                                                             pTypeContext,
                                                             fLoadTypes,
                                                             level,
                                                             dropGenericArgumentLevel,
                                                             pSubst,
                                                             pZapSigContext);
            if (baseType.IsNull())
            {
                thRet = baseType;
            }
            else
            {
                    thRet = ClassLoader::LoadPointerOrByrefTypeThrowing(typ, baseType, fLoadTypes, level);
            }
                break;
        }

        case ELEMENT_TYPE_FNPTR:
            {
#ifndef DACCESS_COMPILE
                uint32_t uCallConv = 0;
                IfFailThrowBF(psig.GetData(&uCallConv), BFA_BAD_SIGNATURE, pOrigModule);

                if ((uCallConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_FIELD)
                    THROW_BAD_FORMAT(BFA_FNPTR_CANNOT_BE_A_FIELD, pOrigModule);

                if ((uCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC) > 0)
                    THROW_BAD_FORMAT(BFA_FNPTR_CANNOT_BE_GENERIC, pOrigModule);

                // Get arg count;
                uint32_t cArgs = 0;
                IfFailThrowBF(psig.GetData(&cArgs), BFA_BAD_SIGNATURE, pOrigModule);

                uint32_t cAllocaSize;
                if (!ClrSafeInt<uint32_t>::addition(cArgs, 1, cAllocaSize) ||
                    !ClrSafeInt<uint32_t>::multiply(cAllocaSize, sizeof(TypeHandle), cAllocaSize))
                {
                    ThrowHR(COR_E_OVERFLOW);
                }

                TypeHandle *retAndArgTypes = (TypeHandle*) _alloca(cAllocaSize);
                bool fReturnTypeOrParameterNotLoaded = false;

                for (unsigned i = 0; i <= cArgs; i++)
                {
                    retAndArgTypes[i] = psig.GetTypeHandleThrowing(pOrigModule,
                                                                   pTypeContext,
                                                                   fLoadTypes,
                                                                   level,
                                                                   dropGenericArgumentLevel,
                                                                   pSubst,
                                                                   pZapSigContext);
                    if (retAndArgTypes[i].IsNull())
                    {
                        thRet = TypeHandle();
                        fReturnTypeOrParameterNotLoaded = true;
                        break;
                    }

                    IfFailThrowBF(psig.SkipExactlyOne(), BFA_BAD_SIGNATURE, pOrigModule);
                }

                if (fReturnTypeOrParameterNotLoaded)
                {
                    break;
                }

                // Now make the function pointer type
                thRet = ClassLoader::LoadFnptrTypeThrowing((BYTE) uCallConv, cArgs, retAndArgTypes, fLoadTypes, level);
#else
            DacNotImpl();
                thRet = TypeHandle();
#endif
            break;
            }

        case ELEMENT_TYPE_INTERNAL :
            {
                TypeHandle hType;
                // this check is not functional in DAC and provides no security against a malicious dump
                // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
                if (pModule->IsSigInIL(m_ptr))
                    THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)pModule);
#endif
                CorSigUncompressPointer(psig.GetPtr(), (void**)&hType);
                thRet = hType;
                break;
            }

        case ELEMENT_TYPE_SENTINEL:
            {
#ifndef DACCESS_COMPILE

                mdToken token = 0;

                IfFailThrowBF(psig.GetToken(&token), BFA_BAD_SIGNATURE, pOrigModule);

                pOrigModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(),
                                                                   token,
                                                                   IDS_CLASSLOAD_GENERAL);
#else
                DacNotImpl();
                break;
#endif // #ifndef DACCESS_COMPILE
            }

            default:
#ifdef _DEBUG_IMPL
                _ASSERTE(!FORBIDGC_LOADER_USE_ENABLED());
#endif
                THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pOrigModule);
    }

    }

    RETURN thRet;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

TypeHandle SigPointer::GetGenericInstType(Module *        pModule,
                                    ClassLoader::LoadTypesFlag  fLoadTypes/*=LoadTypes*/,
                                    ClassLoadLevel              level/*=CLASS_LOADED*/,
                                    const ZapSig::Context *     pZapSigContext)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(return TypeHandle();); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != ClassLoader::LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    Module * pOrigModule   = (pZapSigContext != NULL) ? pZapSigContext->pInfoModule : pModule;

    CorElementType typ = ELEMENT_TYPE_END;
    IfFailThrowBF(GetElemType(&typ), BFA_BAD_SIGNATURE, pOrigModule);

    TypeHandle genericType;

    if (typ == ELEMENT_TYPE_INTERNAL)
    {
        // this check is not functional in DAC and provides no security against a malicious dump
        // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
        if (pModule->IsSigInIL(m_ptr))
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)pModule);
#endif

        IfFailThrow(GetPointer((void**)&genericType));
    }
    else
    {
        mdToken typeToken = mdTypeRefNil;
        IfFailThrowBF(GetToken(&typeToken), BFA_BAD_SIGNATURE, pOrigModule);

#if defined(FEATURE_NATIVE_IMAGE_GENERATION) && !defined(DACCESS_COMPILE)
        if ((pOrigModule != pModule) && (pZapSigContext->externalTokens == ZapSig::IbcTokens))
        {
            // ibcExternalType tokens are actually encoded as mdtTypeDef tokens in the signature
            RID            typeRid  = RidFromToken(typeToken);
            idExternalType ibcToken = RidToToken(typeRid, ibcExternalType);
            typeToken = pOrigModule->LookupIbcTypeToken(pModule, ibcToken);

            if (IsNilToken(typeToken))
            {
                COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_TYPE);
            }
        }
#endif

        if ((TypeFromToken(typeToken) != mdtTypeRef) && (TypeFromToken(typeToken) != mdtTypeDef))
            THROW_BAD_FORMAT(BFA_UNEXPECTED_TOKEN_AFTER_GENINST, pOrigModule);

        if (IsNilToken(typeToken))
            THROW_BAD_FORMAT(BFA_UNEXPECTED_TOKEN_AFTER_GENINST, pOrigModule);

        ClassLoader::NotFoundAction  notFoundAction;
        CorInternalStates            tdTypes;

        if (fLoadTypes == ClassLoader::LoadTypes)
        {
            notFoundAction = ClassLoader::ThrowIfNotFound;
            tdTypes = tdNoTypes;
        }
        else
        {
            notFoundAction = ClassLoader::ReturnNullIfNotFound;
            tdTypes = tdAllTypes;
        }

        genericType = ClassLoader::LoadTypeDefOrRefThrowing(pModule,
                                                            typeToken,
                                                            notFoundAction,
                                                            ClassLoader::PermitUninstDefOrRef,
                                                            tdTypes,
                                                            level);

        if (genericType.IsNull())
        {
            return genericType;
        }

#ifndef DACCESS_COMPILE
        if (fLoadTypes == ClassLoader::LoadTypes)
        {
            // Skip this check when using zap sigs; it should have been correctly computed at NGen time
            // and a change from one to the other would have invalidated the image.  Leave in the code for debug so we can assert below.
            if (pZapSigContext == NULL || pZapSigContext->externalTokens != ZapSig::NormalTokens)
            {
                bool typFromSigIsClass = (typ == ELEMENT_TYPE_CLASS);
                bool typLoadedIsClass  = (genericType.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS);

                if (typFromSigIsClass != typLoadedIsClass)
                {
                    pOrigModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(),
                                                                       typeToken,
                                                                       BFA_CLASSLOAD_VALUETYPEMISMATCH);
                }
            }

            // Assert that our reasoning above was valid (that there is never a zapsig that gets this wrong)
            _ASSERTE(((typ == ELEMENT_TYPE_CLASS) == (genericType.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS)) ||
                      pZapSigContext == NULL || pZapSigContext->externalTokens != ZapSig::NormalTokens);
        }
#endif // #ifndef DACCESS_COMPILE
    }

    return genericType;
}

// SigPointer should be just after E_T_VAR or E_T_MVAR
TypeHandle SigPointer::GetTypeVariableThrowing(Module *pModule, // unused - may be used later for better error reporting
                                               CorElementType et,
                                               ClassLoader::LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                               const SigTypeContext *pTypeContext)
{
    CONTRACT(TypeHandle)
    {
        INSTANCE_CHECK;
        PRECONDITION(CorTypeInfo::IsGenericVariable_NoThrow(et));
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == ClassLoader::LoadTypes) ? NULL_NOT_OK : NULL_OK)));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    TypeHandle res = GetTypeVariable(et, pTypeContext);
#ifndef DACCESS_COMPILE
    if (res.IsNull() && (fLoadTypes == ClassLoader::LoadTypes))
    {
       COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
#endif
    RETURN(res);
}

// SigPointer should be just after E_T_VAR or E_T_MVAR
TypeHandle SigPointer::GetTypeVariable(CorElementType et,
                                       const SigTypeContext *pTypeContext)
{

    CONTRACT(TypeHandle)
    {
        INSTANCE_CHECK;
        PRECONDITION(CorTypeInfo::IsGenericVariable_NoThrow(et));
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK)); // will return TypeHandle() if index is out of range
        SUPPORTS_DAC;
#ifndef DACCESS_COMPILE
        //        POSTCONDITION(RETVAL.IsNull() || RETVAL.IsRestored() || RETVAL.GetMethodTable()->IsRestoring());
#endif
        MODE_ANY;
    }
    CONTRACT_END

    uint32_t index;
    if (FAILED(GetData(&index)))
    {
        TypeHandle thNull;
        RETURN(thNull);
    }

    if (!pTypeContext
        ||
        (et == ELEMENT_TYPE_VAR &&
         (index >= pTypeContext->m_classInst.GetNumArgs()))
        ||
        (et == ELEMENT_TYPE_MVAR &&
         (index >= pTypeContext->m_methodInst.GetNumArgs())))
    {
        LOG((LF_ALWAYS, LL_INFO1000, "GENERICS: Error: GetTypeVariable on out-of-range type variable\n"));
        BAD_FORMAT_NOTHROW_ASSERT(!"Invalid type context: either this is an ill-formed signature (e.g. an invalid type variable number) or you have not provided a non-empty SigTypeContext where one is required.  Check back on the callstack for where the value of pTypeContext is first provided, and see if it is acquired from the correct place.  For calls originating from a JIT it should be acquired from the context parameter, which indicates the method being compiled.  For calls from other locations it should be acquired from the MethodTable, EEClass, TypeHandle, FieldDesc or MethodDesc being analyzed.");
        TypeHandle thNull;
        RETURN(thNull);
    }
    if (et == ELEMENT_TYPE_VAR)
    {
        RETURN(pTypeContext->m_classInst[index]);
    }
    else
    {
        RETURN(pTypeContext->m_methodInst[index]);
    }
}


#ifndef DACCESS_COMPILE

// Does this type contain class or method type parameters whose instantiation cannot
// be determined at JIT-compile time from the instantiations in the method context?
// Return a combination of hasClassVar and hasMethodVar flags.
// See header file for more info.
VarKind SigPointer::IsPolyType(const SigTypeContext *pTypeContext) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    SigPointer psig = *this;
    CorElementType typ;

    if (FAILED(psig.GetElemType(&typ)))
        return hasNoVars;

    switch(typ) {
        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        {
            VarKind res = (typ == ELEMENT_TYPE_VAR ? hasClassVar : hasMethodVar);
            if (pTypeContext != NULL)
            {
                TypeHandle ty = psig.GetTypeVariable(typ, pTypeContext);
                if (ty.IsCanonicalSubtype())
                    res = (VarKind) (res | (typ == ELEMENT_TYPE_VAR ? hasSharableClassVar : hasSharableMethodVar));
            }
            return (res);
        }

        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_TYPEDBYREF:
            return(hasNoVars);

        case ELEMENT_TYPE_GENERICINST:
          {
            VarKind k = psig.IsPolyType(pTypeContext);
            if (FAILED(psig.SkipExactlyOne()))
                return hasNoVars;

            uint32_t ntypars;
            if(FAILED(psig.GetData(&ntypars)))
                return hasNoVars;

            for (uint32_t i = 0; i < ntypars; i++)
            {
              k = (VarKind) (psig.IsPolyType(pTypeContext) | k);
              if (FAILED(psig.SkipExactlyOne()))
                return hasNoVars;
            }
            return(k);
          }

        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_PTR:
        {
            return(psig.IsPolyType(pTypeContext));
        }

        case ELEMENT_TYPE_FNPTR:
        {
            if (FAILED(psig.GetData(NULL)))
                return hasNoVars;

            // Get arg count;
            uint32_t cArgs;
            if (FAILED(psig.GetData(&cArgs)))
                return hasNoVars;

            VarKind k = psig.IsPolyType(pTypeContext);
            if (FAILED(psig.SkipExactlyOne()))
                return hasNoVars;

            for (unsigned i = 0; i < cArgs; i++)
            {
                k = (VarKind) (psig.IsPolyType(pTypeContext) | k);
                if (FAILED(psig.SkipExactlyOne()))
                    return hasNoVars;
            }

            return(k);
        }

        default:
            BAD_FORMAT_NOTHROW_ASSERT(!"Bad type");
    }
    return(hasNoVars);
}

BOOL SigPointer::IsStringType(Module* pModule, const SigTypeContext *pTypeContext) const
{
    WRAPPER_NO_CONTRACT;

    return IsStringTypeHelper(pModule, pTypeContext, FALSE);
}


BOOL SigPointer::IsStringTypeThrowing(Module* pModule, const SigTypeContext *pTypeContext) const
{
    WRAPPER_NO_CONTRACT;

    return IsStringTypeHelper(pModule, pTypeContext, TRUE);
}

BOOL SigPointer::IsStringTypeHelper(Module* pModule, const SigTypeContext* pTypeContext, BOOL fThrow) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (fThrow)
        {
            THROWS;
            GC_TRIGGERS;
        }
        else
        {
            NOTHROW;
            GC_NOTRIGGER;
        }

        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END

    IMDInternalImport *pInternalImport = pModule->GetMDImport();
    SigPointer psig = *this;
    CorElementType typ;
    if (FAILED(psig.GetElemType(&typ)))
    {
        if (fThrow)
        {
            ThrowHR(META_E_BAD_SIGNATURE);
        }
        else
        {
            return FALSE;
        }
    }

    switch (typ)
    {
        case ELEMENT_TYPE_STRING :
            return TRUE;

        case ELEMENT_TYPE_CLASS :
        {
            LPCUTF8 pclsname;
            LPCUTF8 pszNamespace;
            mdToken token;

            if (FAILED( psig.GetToken(&token)))
            {
                if (fThrow)
                {
                    ThrowHR(META_E_BAD_SIGNATURE);
                }
                else
                {
                    return FALSE;
                }
            }

            if (TypeFromToken(token) == mdtTypeDef)
            {
                if (FAILED(pInternalImport->GetNameOfTypeDef(token, &pclsname, &pszNamespace)))
                {
                    if (fThrow)
                    {
                        ThrowHR(COR_E_BADIMAGEFORMAT);
                    }
                    else
                    {
                        return FALSE;
                    }
                }
            }
            else
            {
                BAD_FORMAT_NOTHROW_ASSERT(TypeFromToken(token) == mdtTypeRef);
                if (FAILED(pInternalImport->GetNameOfTypeRef(token, &pszNamespace, &pclsname)))
                {
                    if (fThrow)
                    {
                        ThrowHR(COR_E_BADIMAGEFORMAT);
                    }
                    else
                    {
                        return FALSE;
                    }
                }
            }

            if (strcmp(pclsname, g_StringName) != 0)
                return FALSE;

            if (pszNamespace == NULL)
                return FALSE;

            return (strcmp(pszNamespace, g_SystemNS) == 0);
        }

        case ELEMENT_TYPE_VAR :
        case ELEMENT_TYPE_MVAR :
        {
            TypeHandle ty;

            if (fThrow)
            {
                ty = psig.GetTypeVariableThrowing(pModule, typ, ClassLoader::LoadTypes, pTypeContext);
            }
            else
            {
                ty = psig.GetTypeVariable(typ, pTypeContext);
            }

            TypeHandle th(g_pStringClass);
            return (ty == th);
        }

        default:
            break;
    }
    return FALSE;
}


//------------------------------------------------------------------------
// Tests if the element class name is szClassName.
//------------------------------------------------------------------------
BOOL SigPointer::IsClass(Module* pModule, LPCUTF8 szClassName, const SigTypeContext *pTypeContext) const
{
    WRAPPER_NO_CONTRACT;

    return IsClassHelper(pModule, szClassName, pTypeContext, FALSE);
}


//------------------------------------------------------------------------
// Tests if the element class name is szClassName.
//------------------------------------------------------------------------
BOOL SigPointer::IsClassThrowing(Module* pModule, LPCUTF8 szClassName, const SigTypeContext *pTypeContext) const
{
    WRAPPER_NO_CONTRACT;

    return IsClassHelper(pModule, szClassName, pTypeContext, TRUE);
}

BOOL SigPointer::IsClassHelper(Module* pModule, LPCUTF8 szClassName, const SigTypeContext* pTypeContext, BOOL fThrow) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;

        if (fThrow)
        {
            THROWS;
            GC_TRIGGERS;
        }
        else
        {
            NOTHROW;
            GC_NOTRIGGER;
        }

        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(szClassName));
    }
    CONTRACTL_END

    SigPointer psig = *this;
    CorElementType typ;
    if (FAILED(psig.GetElemType(&typ)))
    {
        if (fThrow)
            ThrowHR(META_E_BAD_SIGNATURE);
        else
            return FALSE;
    }

    BAD_FORMAT_NOTHROW_ASSERT((typ == ELEMENT_TYPE_VAR)      || (typ == ELEMENT_TYPE_MVAR)      ||
                              (typ == ELEMENT_TYPE_CLASS)    || (typ == ELEMENT_TYPE_VALUETYPE) ||
                              (typ == ELEMENT_TYPE_OBJECT)   || (typ == ELEMENT_TYPE_STRING)    ||
                              (typ == ELEMENT_TYPE_INTERNAL) || (typ == ELEMENT_TYPE_GENERICINST));


    if (typ == ELEMENT_TYPE_VAR || typ == ELEMENT_TYPE_MVAR)
    {
        TypeHandle ty;

        if (fThrow)
            ty = psig.GetTypeVariableThrowing(pModule, typ, ClassLoader::LoadTypes, pTypeContext);
        else
            ty = psig.GetTypeVariable(typ, pTypeContext);

        return(!ty.IsNull() && IsTypeRefOrDef(szClassName, ty.GetModule(), ty.GetCl()));
    }
    else if ((typ == ELEMENT_TYPE_CLASS) || (typ == ELEMENT_TYPE_VALUETYPE))
    {
        mdTypeRef typeref;
        if (FAILED(psig.GetToken(&typeref)))
        {
            if (fThrow)
                ThrowHR(META_E_BAD_SIGNATURE);
            else
                return FALSE;
        }

        return( IsTypeRefOrDef(szClassName, pModule, typeref) );
    }
    else if (typ == ELEMENT_TYPE_OBJECT)
    {
        return( !strcmp(szClassName, g_ObjectClassName) );
    }
    else if (typ == ELEMENT_TYPE_STRING)
    {
        return( !strcmp(szClassName, g_StringClassName) );
    }
    else if (typ == ELEMENT_TYPE_INTERNAL)
    {
        TypeHandle th;

        // this check is not functional in DAC and provides no security against a malicious dump
        // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
        if (pModule->IsSigInIL(m_ptr))
        {
            if (fThrow)
                ThrowHR(META_E_BAD_SIGNATURE);
            else
                return FALSE;
        }
#endif

        CorSigUncompressPointer(psig.GetPtr(), (void**)&th);
        _ASSERTE(!th.IsNull());
        return(IsTypeRefOrDef(szClassName, th.GetModule(), th.GetCl()));
    }

    return( false );
}

//------------------------------------------------------------------------
// Tests for the existence of a custom modifier
//------------------------------------------------------------------------
BOOL SigPointer::HasCustomModifier(Module *pModule, LPCSTR szModName, CorElementType cmodtype) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END


    BAD_FORMAT_NOTHROW_ASSERT(cmodtype == ELEMENT_TYPE_CMOD_OPT || cmodtype == ELEMENT_TYPE_CMOD_REQD);

    SigPointer sp = *this;
    CorElementType etyp;
    if (sp.AtSentinel())
        sp.m_ptr++;

    BYTE data;

    if (FAILED(sp.GetByte(&data)))
        return FALSE;

    etyp = (CorElementType)data;


    while (etyp == ELEMENT_TYPE_CMOD_OPT || etyp == ELEMENT_TYPE_CMOD_REQD) {

        mdToken tk;
        if (FAILED(sp.GetToken(&tk)))
            return FALSE;

        if (etyp == cmodtype && IsTypeRefOrDef(szModName, pModule, tk))
        {
            return(TRUE);
        }

        if (FAILED(sp.GetByte(&data)))
            return FALSE;

        etyp = (CorElementType)data;


    }
    return(FALSE);
}

#endif // #ifndef DACCESS_COMPILE

//------------------------------------------------------------------------
// Tests for ELEMENT_TYPE_CLASS or ELEMENT_TYPE_VALUETYPE followed by a TypeDef,
// and returns the TypeDef
//------------------------------------------------------------------------
BOOL SigPointer::IsTypeDef(mdTypeDef* pTypeDef) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;

    SigPointer sigTemp(*this);

    CorElementType etype = ELEMENT_TYPE_END;
    HRESULT hr = sigTemp.GetElemType(&etype);
    if (FAILED(hr))
        return FALSE;

    if (etype != ELEMENT_TYPE_CLASS && etype != ELEMENT_TYPE_VALUETYPE)
        return FALSE;

    mdToken token = mdTypeRefNil;
    hr = sigTemp.GetToken(&token);
    if (FAILED(hr))
        return FALSE;

    if (TypeFromToken(token) != mdtTypeDef)
        return FALSE;

    if (pTypeDef)
        *pTypeDef = (mdTypeDef)token;

    return TRUE;
}

CorElementType SigPointer::PeekElemTypeNormalized(Module* pModule, const SigTypeContext *pTypeContext, TypeHandle * pthValueType) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    CorElementType type = PeekElemTypeClosed(pModule, pTypeContext);
    _ASSERTE(type != ELEMENT_TYPE_INTERNAL);

    if (type == ELEMENT_TYPE_VALUETYPE)
    {
        {
            // Everett C++ compiler can generate a TypeRef with RS=0
            // without respective TypeDef for unmanaged valuetypes,
            // referenced only by pointers to them.
            // In such case, GetTypeHandleThrowing returns null handle,
            // and we return E_T_VOID
            TypeHandle th = GetTypeHandleThrowing(pModule, pTypeContext, ClassLoader::LoadTypes, CLASS_LOAD_APPROXPARENTS, TRUE);
            if(th.IsNull())
            {
                th = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_VOID));
            }

            type = th.GetInternalCorElementType();
            if (pthValueType != NULL)
                *pthValueType = th;
        }
    }

    return(type);
}

//---------------------------------------------------------------------------------------
//
CorElementType
SigPointer::PeekElemTypeClosed(
    Module *               pModule,
    const SigTypeContext * pTypeContext) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END


    CorElementType type;

    if (FAILED(PeekElemType(&type)))
        return ELEMENT_TYPE_END;

    if ((type == ELEMENT_TYPE_GENERICINST) ||
        (type == ELEMENT_TYPE_VAR) ||
        (type == ELEMENT_TYPE_MVAR) ||
        (type == ELEMENT_TYPE_INTERNAL))
    {
        SigPointer sp(*this);
        if (FAILED(sp.GetElemType(NULL))) // skip over E_T_XXX
            return ELEMENT_TYPE_END;

        switch (type)
        {
            case ELEMENT_TYPE_GENERICINST:
            {
                if (FAILED(sp.GetElemType(&type)))
                    return ELEMENT_TYPE_END;

                if (type != ELEMENT_TYPE_INTERNAL)
                    return type;
            }

            FALLTHROUGH;

            case ELEMENT_TYPE_INTERNAL:
            {
                TypeHandle th;

                // this check is not functional in DAC and provides no security against a malicious dump
                // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
                if ((pModule != NULL) && pModule->IsSigInIL(m_ptr))
                {
                    return ELEMENT_TYPE_END;
                }
#endif

                if (FAILED(sp.GetPointer((void **)&th)))
                {
                    return ELEMENT_TYPE_END;
                }
                _ASSERTE(!th.IsNull());

                return th.GetSignatureCorElementType();
            }
            case ELEMENT_TYPE_VAR :
            case ELEMENT_TYPE_MVAR :
            {
                TypeHandle th = sp.GetTypeVariable(type, pTypeContext);
                if (th.IsNull())
                {
                    BAD_FORMAT_NOTHROW_ASSERT(!"You either have bad signature or caller forget to pass valid type context");
                    return ELEMENT_TYPE_END;
                }

                return th.GetSignatureCorElementType();
            }
            default:
                UNREACHABLE();
        }
    }

    return type;
} // SigPointer::PeekElemTypeClosed


//---------------------------------------------------------------------------------------
//
mdTypeRef SigPointer::PeekValueTypeTokenClosed(Module *pModule, const SigTypeContext *pTypeContext, Module **ppModuleOfToken) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(PeekElemTypeClosed(NULL, pTypeContext) == ELEMENT_TYPE_VALUETYPE);
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END


    mdToken token;
    CorElementType type;

    *ppModuleOfToken = pModule;

    if (FAILED(PeekElemType(&type)))
        return mdTokenNil;

    switch (type)
    {
    case ELEMENT_TYPE_GENERICINST:
        {
            SigPointer sp(*this);
            if (FAILED(sp.GetElemType(NULL)))
                return mdTokenNil;

            CorElementType subtype;
            if (FAILED(sp.GetElemType(&subtype)))
                return mdTokenNil;
            if (subtype == ELEMENT_TYPE_INTERNAL)
                return mdTokenNil;
            _ASSERTE(subtype == ELEMENT_TYPE_VALUETYPE);

            if (FAILED(sp.GetToken(&token)))
                return mdTokenNil;

            return token;
        }
    case ELEMENT_TYPE_VAR :
    case ELEMENT_TYPE_MVAR :
        {
            SigPointer sp(*this);

            if (FAILED(sp.GetElemType(NULL)))
                return mdTokenNil;

            TypeHandle th = sp.GetTypeVariable(type, pTypeContext);
            *ppModuleOfToken = th.GetModule();
            _ASSERTE(!th.IsNull());
            return(th.GetCl());
        }
    case ELEMENT_TYPE_INTERNAL:
        // we have no way to give back a token for the E_T_INTERNAL so we return  a null one
        // and make the caller deal with it
        return mdTokenNil;

    default:
        {
            _ASSERTE(type == ELEMENT_TYPE_VALUETYPE);
            SigPointer sp(*this);

            if (FAILED(sp.GetElemType(NULL)))
                return mdTokenNil;

            if (FAILED(sp.GetToken(&token)))
                return mdTokenNil;

            return token;
        }
    }
}

//---------------------------------------------------------------------------------------
//
UINT MetaSig::GetElemSize(CorElementType etype, TypeHandle thValueType)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if ((UINT)etype >= COUNTOF(gElementTypeInfo))
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_COMPLUS_SIG);

    int cbsize = gElementTypeInfo[(UINT)etype].m_cbSize;
    if (cbsize != -1)
        return(cbsize);

    if (!thValueType.IsNull())
        return thValueType.GetSize();

    if (etype == ELEMENT_TYPE_VAR || etype == ELEMENT_TYPE_MVAR)
    {
        LOG((LF_ALWAYS, LL_INFO1000, "GENERICS: Warning: SizeOf on VAR without instantiation\n"));
        return(sizeof(LPVOID));
    }

    ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_ELEM_IN_SIZEOF);
}

//---------------------------------------------------------------------------------------
//
// Assumes that the SigPointer points to the start of an element type.
// Returns size of that element in bytes. This is the minimum size that a
// field of this type would occupy inside an object.
//
UINT SigPointer::SizeOf(Module* pModule, const SigTypeContext *pTypeContext, TypeHandle* pTypeHandle) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        UNCHECKED(PRECONDITION(CheckPointer(pModule)));
        UNCHECKED(PRECONDITION(CheckPointer(pTypeContext, NULL_OK)));
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    CorElementType etype = PeekElemTypeNormalized(pModule, pTypeContext, pTypeHandle);
    return MetaSig::GetElemSize(etype, *pTypeHandle);
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Determines if the current argument is System.String.
// Caller must determine first that the argument type is ELEMENT_TYPE_CLASS.
//
BOOL MetaSig::IsStringType() const
{
    WRAPPER_NO_CONTRACT

    return m_pLastType.IsStringType(m_pModule, &m_typeContext);
}

//---------------------------------------------------------------------------------------
//
// Determines if the current argument is a particular class.
// Caller must determine first that the argument type is ELEMENT_TYPE_CLASS.
//
BOOL MetaSig::IsClass(LPCUTF8 szClassName) const
{
    WRAPPER_NO_CONTRACT

    return m_pLastType.IsClass(m_pModule, szClassName, &m_typeContext);
}

//---------------------------------------------------------------------------------------
//
// Return the type of an reference if the array is the param type
//  The arg type must be an ELEMENT_TYPE_BYREF
//  ref to array needs additional arg
//
CorElementType MetaSig::GetByRefType(TypeHandle *pTy) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    SigPointer sigptr(m_pLastType);

    CorElementType typ = ELEMENT_TYPE_END;
    IfFailThrowBF(sigptr.GetElemType(&typ), BFA_BAD_SIGNATURE, GetModule());

    _ASSERTE(typ == ELEMENT_TYPE_BYREF);
    typ = (CorElementType)sigptr.PeekElemTypeClosed(GetModule(), &m_typeContext);

    if (!CorIsPrimitiveType(typ))
    {
        if (typ == ELEMENT_TYPE_TYPEDBYREF)
            THROW_BAD_FORMAT(BFA_TYPEDBYREFCANNOTHAVEBYREF, GetModule());
        TypeHandle th = sigptr.GetTypeHandleThrowing(m_pModule, &m_typeContext);
        *pTy = th;
        return(th.GetSignatureCorElementType());
    }
    return(typ);
}

//---------------------------------------------------------------------------------------
//
HRESULT CompareTypeTokensNT(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2)
{
    STATIC_CONTRACT_NOTHROW;

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (CompareTypeTokens(tk1, tk2, pModule1, pModule2))
            hr = S_OK;
        else
            hr = S_FALSE;
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);
    return hr;
}

#ifdef FEATURE_TYPEEQUIVALENCE

//---------------------------------------------------------------------------------------
//
// Returns S_FALSE if the type is not decorated with TypeIdentifierAttribute.
//
HRESULT TypeIdentifierData::Init(Module *pModule, mdToken tk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(TypeFromToken(tk) == mdtTypeDef);
    }
    CONTRACTL_END

    IMDInternalImport *pInternalImport = pModule->GetMDImport();
    HRESULT hr = S_OK;

    DWORD dwAttrType;
    IfFailRet(pInternalImport->GetTypeDefProps(tk, &dwAttrType, NULL));

    if (IsTdWindowsRuntime(dwAttrType))
    {
        // no type equivalence support for WinRT types
        return S_FALSE;
    }

    ULONG cbData;
    const BYTE *pData;

    IfFailRet(pModule->GetCustomAttribute(
        tk,
        WellKnownAttribute::TypeIdentifier,
        (const void **)&pData,
        &cbData));

    if (hr == S_OK)
    {
        CustomAttributeParser caType(pData, cbData);

        if (cbData > 4)
        {
            // parameterless blob is 01 00 00 00 which means that the two arguments must follow now
            CaArg args[2];

            args[0].Init(SERIALIZATION_TYPE_STRING, 0);
            args[1].Init(SERIALIZATION_TYPE_STRING, 0);
            IfFailRet(ParseKnownCaArgs(caType, args, lengthof(args)));

            m_cbScope = args[0].val.str.cbStr;
            m_pchScope = args[0].val.str.pStr;
            m_cbIdentifierName = args[1].val.str.cbStr;
            m_pchIdentifierName = args[1].val.str.pStr;
        }
        else
        {
            // no arguments follow but we should still verify the blob
            IfFailRet(caType.ValidateProlog());
        }
    }
    else
    {
        // no TypeIdentifierAttribute -> the assembly must be a type library
        bool has_eq = !pModule->GetAssembly()->IsDynamic();

#ifdef FEATURE_COMINTEROP
        has_eq = has_eq && pModule->GetAssembly()->IsPIAOrImportedFromTypeLib();
#endif // FEATURE_COMINTEROP

        if (!has_eq)
        {
            // this type is not opted into type equivalence
            return S_FALSE;
        }
    }

    if (m_pchIdentifierName == NULL)
    {
        // we have got no data from the TypeIdentifier attribute -> we have to get it elsewhere
        if (IsTdInterface(dwAttrType) && IsTdImport(dwAttrType))
        {
            // ComImport interfaces get scope from their GUID
            hr = pModule->GetCustomAttribute(tk, WellKnownAttribute::Guid, (const void **)&pData, &cbData);
        }
        else
        {
            // other equivalent types get it from the declaring assembly
            hr = pModule->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::Guid, (const void **)&pData, &cbData);
        }

        if (hr != S_OK)
        {
            // no GUID is available
            return hr;
        }

        CustomAttributeParser caType(pData, cbData);
        CaArg guidarg;

        guidarg.Init(SERIALIZATION_TYPE_STRING, 0);
        IfFailRet(ParseKnownCaArgs(caType, &guidarg, 1));

        m_cbScope = guidarg.val.str.cbStr;
        m_pchScope = guidarg.val.str.pStr;

        // all types get their identifier from their namespace and name
        LPCUTF8 pszName;
        LPCUTF8 pszNamespace;
        IfFailRet(pInternalImport->GetNameOfTypeDef(tk, &pszName, &pszNamespace));

        m_cbIdentifierNamespace = (pszNamespace != NULL ? strlen(pszNamespace) : 0);
        m_pchIdentifierNamespace = pszNamespace;

        m_cbIdentifierName = strlen(pszName);
        m_pchIdentifierName = pszName;

        hr = S_OK;
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
BOOL TypeIdentifierData::IsEqual(const TypeIdentifierData & data) const
{
    LIMITED_METHOD_CONTRACT;

    // scope needs to be the same
    if (m_cbScope != data.m_cbScope || _strnicmp(m_pchScope, data.m_pchScope, m_cbScope) != 0)
        return FALSE;

    // identifier needs to be the same
    if (m_cbIdentifierNamespace == 0 && data.m_cbIdentifierNamespace == 0)
    {
        // we are comparing only m_pchIdentifierName
        return (m_cbIdentifierName == data.m_cbIdentifierName) &&
               (memcmp(m_pchIdentifierName, data.m_pchIdentifierName, m_cbIdentifierName) == 0);
    }

    if (m_cbIdentifierNamespace != 0 && data.m_cbIdentifierNamespace != 0)
    {
        // we are comparing both m_pchIdentifierNamespace and m_pchIdentifierName
        return (m_cbIdentifierName == data.m_cbIdentifierName) &&
               (m_cbIdentifierNamespace == data.m_cbIdentifierNamespace) &&
               (memcmp(m_pchIdentifierName, data.m_pchIdentifierName, m_cbIdentifierName) == 0) &&
               (memcmp(m_pchIdentifierNamespace, data.m_pchIdentifierNamespace, m_cbIdentifierNamespace) == 0);
    }

    if (m_cbIdentifierNamespace == 0 && data.m_cbIdentifierNamespace != 0)
    {
        // we are comparing m_cbIdentifierName with (data.m_pchIdentifierNamespace + '.' + data.m_pchIdentifierName)
        if (m_cbIdentifierName != data.m_cbIdentifierNamespace + 1 + data.m_cbIdentifierName)
            return FALSE;

        return (memcmp(m_pchIdentifierName, data.m_pchIdentifierNamespace, data.m_cbIdentifierNamespace) == 0) &&
               (m_pchIdentifierName[data.m_cbIdentifierNamespace] == NAMESPACE_SEPARATOR_CHAR) &&
               (memcmp(m_pchIdentifierName + data.m_cbIdentifierNamespace + 1, data.m_pchIdentifierName, data.m_cbIdentifierName) == 0);
    }

    _ASSERTE(m_cbIdentifierNamespace != 0 && data.m_cbIdentifierNamespace == 0);

    // we are comparing (m_pchIdentifierNamespace + '.' + m_pchIdentifierName) with data.m_cbIdentifierName
    if (m_cbIdentifierNamespace + 1 + m_cbIdentifierName != data.m_cbIdentifierName)
        return FALSE;

    return (memcmp(m_pchIdentifierNamespace, data.m_pchIdentifierName, m_cbIdentifierNamespace) == 0) &&
           (data.m_pchIdentifierName[m_cbIdentifierNamespace] == NAMESPACE_SEPARATOR_CHAR) &&
           (memcmp(m_pchIdentifierName, data.m_pchIdentifierName + m_cbIdentifierNamespace + 1, m_cbIdentifierName) == 0);
}

//---------------------------------------------------------------------------------------
//
static BOOL CompareStructuresForEquivalence(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, BOOL fEnumMode, TokenPairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    // make sure the types don't declare any methods
    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    HENUMInternalHolder hMethodEnum1(pInternalImport1);
    HENUMInternalHolder hMethodEnum2(pInternalImport2);

    hMethodEnum1.EnumInit(mdtMethodDef, tk1);
    hMethodEnum2.EnumInit(mdtMethodDef, tk2);

    if (hMethodEnum1.EnumGetCount() != 0 || hMethodEnum2.EnumGetCount() != 0)
        return FALSE;

    // compare field types for equivalence
    HENUMInternalHolder hFieldEnum1(pInternalImport1);
    HENUMInternalHolder hFieldEnum2(pInternalImport2);

    hFieldEnum1.EnumInit(mdtFieldDef, tk1);
    hFieldEnum2.EnumInit(mdtFieldDef, tk2);

    while (true)
    {
        mdToken tkField1, tkField2;

        DWORD dwAttrField1, dwAttrField2;
        bool res1, res2;

        while ((res1 = hFieldEnum1.EnumNext(&tkField1)) == true)
        {
            IfFailThrow(pInternalImport1->GetFieldDefProps(tkField1, &dwAttrField1));

            if (IsFdPublic(dwAttrField1) && !IsFdStatic(dwAttrField1))
                break;

            if (!fEnumMode || !IsFdLiteral(dwAttrField1)) // ignore literals in enums
                return FALSE;
        }

        while ((res2 = hFieldEnum2.EnumNext(&tkField2)) == true)
        {
            IfFailThrow(pInternalImport2->GetFieldDefProps(tkField2, &dwAttrField2));

            if (IsFdPublic(dwAttrField2) && !IsFdStatic(dwAttrField2))
                break;

            if (!fEnumMode || !IsFdLiteral(dwAttrField2)) // ignore literals in enums
                return FALSE;
        }

        if (!res1 && !res2)
        {
            // we ran out of fields in both types
            break;
        }

        if (res1 != res2)
        {
            // we ran out of fields in one type
            return FALSE;
        }

        // now we have tokens of two instance fields that need to be compared for equivalence
        PCCOR_SIGNATURE pSig1, pSig2;
        DWORD cbSig1, cbSig2;

        IfFailThrow(pInternalImport1->GetSigOfFieldDef(tkField1, &cbSig1, &pSig1));
        IfFailThrow(pInternalImport2->GetSigOfFieldDef(tkField2, &cbSig2, &pSig2));

        if (!MetaSig::CompareFieldSigs(pSig1, cbSig1, pModule1, pSig2, cbSig2, pModule2, pVisited))
            return FALSE;
    }

    if (!fEnumMode)
    {
        // compare layout (layout kind, charset, packing, size, offsets, marshaling)
        if (!CompareTypeLayout(tk1, tk2, pModule1, pModule2))
            return FALSE;
    }

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
static void GetDelegateInvokeMethodSignature(mdToken tkDelegate, Module *pModule, DWORD *pcbSig, PCCOR_SIGNATURE *ppSig)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    IMDInternalImport *pInternalImport = pModule->GetMDImport();

    HENUMInternalHolder hEnum(pInternalImport);
    hEnum.EnumInit(mdtMethodDef, tkDelegate);

    mdToken tkMethod;
    while (hEnum.EnumNext(&tkMethod))
    {
        LPCUTF8 pszName;
        IfFailThrow(pInternalImport->GetNameAndSigOfMethodDef(tkMethod, ppSig, pcbSig, &pszName));

        if (strcmp(pszName, "Invoke") == 0)
            return;
    }

    ThrowHR(COR_E_BADIMAGEFORMAT);
}

static BOOL CompareDelegatesForEquivalence(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, TokenPairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    PCCOR_SIGNATURE pSig1;
    PCCOR_SIGNATURE pSig2;
    DWORD cbSig1;
    DWORD cbSig2;

    // find the Invoke methods
    GetDelegateInvokeMethodSignature(tk1, pModule1, &cbSig1, &pSig1);
    GetDelegateInvokeMethodSignature(tk2, pModule2, &cbSig2, &pSig2);

    return MetaSig::CompareMethodSigs(pSig1, cbSig1, pModule1, NULL, pSig2, cbSig2, pModule2, NULL, FALSE, pVisited);
}

#endif // FEATURE_TYPEEQUIVALENCE
#endif // #ifndef DACCESS_COMPILE

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
BOOL IsTypeDefExternallyVisible(mdToken tk, Module *pModule, DWORD dwAttrClass)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL bIsVisible = TRUE;

    if (!IsTdPublic(dwAttrClass))
    {
        if (!IsTdNestedPublic(dwAttrClass))
            return FALSE;

        IMDInternalImport *pInternalImport = pModule->GetMDImport();

        DWORD dwAttrEnclosing;

        mdTypeDef tdCurrent = tk;
        do
        {
            mdTypeDef tdEnclosing = mdTypeDefNil;

            if (FAILED(pInternalImport->GetNestedClassProps(tdCurrent, &tdEnclosing)))
                return FALSE;

            tdCurrent = tdEnclosing;

            // We stop searching as soon as we hit the first non NestedPublic type.
            // So logically, we can't possibly fall off the top of the hierarchy.
            _ASSERTE(tdEnclosing != mdTypeDefNil);

            mdToken tkJunk = mdTokenNil;

            if (FAILED(pInternalImport->GetTypeDefProps(tdEnclosing, &dwAttrEnclosing, &tkJunk)))
            {
                return FALSE;
            }
        }
        while (IsTdNestedPublic(dwAttrEnclosing));

        bIsVisible = IsTdPublic(dwAttrEnclosing);
    }

    return bIsVisible;
}
#endif

#ifndef FEATURE_TYPEEQUIVALENCE
#ifndef DACCESS_COMPILE
BOOL IsTypeDefEquivalent(mdToken tk, Module *pModule)
{
    LIMITED_METHOD_CONTRACT;
    return FALSE;
}
#endif
#endif

#ifdef FEATURE_TYPEEQUIVALENCE
#ifndef DACCESS_COMPILE
BOOL IsTypeDefEquivalent(mdToken tk, Module *pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;


    IMDInternalImport *pInternalImport = pModule->GetMDImport();

    if (tk == mdTypeDefNil)
        return FALSE;

    DWORD dwAttrType;
    mdToken tkExtends;

    IfFailThrow(pInternalImport->GetTypeDefProps(tk, &dwAttrType, &tkExtends));

    if (IsTdWindowsRuntime(dwAttrType))
    {
        // no type equivalence support for WinRT types
        return FALSE;
    }

    // Check for the TypeIdentifierAttribute and auto opt-in
    HRESULT hr = pModule->GetCustomAttribute(tk, WellKnownAttribute::TypeIdentifier, NULL, NULL);
    IfFailThrow(hr);

    // 1. Type is within assembly marked with ImportedFromTypeLibAttribute or PrimaryInteropAssemblyAttribute
    if (hr != S_OK)
    {
        // no TypeIdentifierAttribute -> the assembly must be a type library
        bool has_eq = !pModule->GetAssembly()->IsDynamic();

#ifdef FEATURE_COMINTEROP
        has_eq = has_eq && pModule->GetAssembly()->IsPIAOrImportedFromTypeLib();
#endif // FEATURE_COMINTEROP

        if (!has_eq)
            return FALSE;
    }
    else if (hr == S_OK)
    {
        // Type has TypeIdentifierAttribute. It is marked as type equivalent.
        return TRUE;
    }

    mdToken tdEnum = g_pEnumClass->GetCl();
    Module *pSystemModule = g_pEnumClass->GetModule();
    mdToken tdValueType = g_pValueTypeClass->GetCl();
    _ASSERTE(pSystemModule == g_pValueTypeClass->GetModule());
    mdToken tdMCDelegate = g_pMulticastDelegateClass->GetCl();
    _ASSERTE(pSystemModule == g_pMulticastDelegateClass->GetModule());

    // 2. Type is a COMImport/COMEvent interface, enum, struct, or delegate
    BOOL fIsCOMInterface = FALSE;
    if (IsTdInterface(dwAttrType))
    {
        if (IsTdImport(dwAttrType))
        {
            // COMImport
            fIsCOMInterface = TRUE;
        }
        else
        {
            // COMEvent
            hr = pModule->GetCustomAttribute(tk, WellKnownAttribute::ComEventInterface, NULL, NULL);
            IfFailThrow(hr);

            if (hr == S_OK)
                fIsCOMInterface = TRUE;
        }
    }

    if (fIsCOMInterface ||
        (!IsTdInterface(dwAttrType) && (tkExtends != mdTypeDefNil) &&
        ((CompareTypeTokens(tkExtends, tdEnum, pModule, pSystemModule)) ||
         (CompareTypeTokens(tkExtends, tdValueType, pModule, pSystemModule) && (tk != tdEnum || pModule != pSystemModule)) ||
         (CompareTypeTokens(tkExtends, tdMCDelegate, pModule, pSystemModule)))))
    {
        HENUMInternal   hEnumGenericPars;
        IfFailThrow(pInternalImport->EnumInit(mdtGenericParam, tk, &hEnumGenericPars));
        DWORD numGenericArgs = pInternalImport->EnumGetCount(&hEnumGenericPars);

        // 3. Type is not generic
        if (numGenericArgs > 0)
            return FALSE;

        // 4. Type is externally visible (i.e. public)
        if (!IsTypeDefExternallyVisible(tk, pModule, dwAttrType))
            return FALSE;

        // since the token has not been loaded yet,
        // its module might be not fully initialized in this domain
        // take care of that possibility
        pModule->EnsureAllocated();

        // 6. If type is nested, nesting type must be equivalent.
        if (IsTdNested(dwAttrType))
        {
            mdTypeDef tdEnclosing = mdTypeDefNil;

            IfFailThrow(pInternalImport->GetNestedClassProps(tk, &tdEnclosing));

            if (!IsTypeDefEquivalent(tdEnclosing, pModule))
                return FALSE;
        }

        // Type meets all of the requirements laid down above. Type is considered to be marked as equivalent.
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}
#endif
#endif // FEATURE_TYPEEQUIVALENCE

BOOL CompareTypeDefsForEquivalence(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, TokenPairList *pVisited)
{
#if !defined(DACCESS_COMPILE) && defined(FEATURE_TYPEEQUIVALENCE)
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    if (TokenPairList::InTypeEquivalenceForbiddenScope(pVisited))
    {
        // we limit variance on generics only to interfaces
        return FALSE;
    }
    if (TokenPairList::Exists(pVisited, tk1, pModule1, tk2, pModule2))
    {
        // we are in the process of comparing these tokens already
        return TRUE;
    }
    TokenPairList newVisited(tk1, pModule1, tk2, pModule2, pVisited);

    DWORD dwAttrType1;
    DWORD dwAttrType2;
    mdToken tkExtends1;
    mdToken tkExtends2;
    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    // *************************************************************************
    // 1. both types must opt into type equivalence and be able to acquire their equivalence set
    // *************************************************************************
    TypeIdentifierData data1;
    TypeIdentifierData data2;
    HRESULT hr;

    IfFailThrow(hr = data1.Init(pModule1, tk1));
    BOOL has_eq1 = (hr == S_OK);

    IfFailThrow(hr = data2.Init(pModule2, tk2));
    BOOL has_eq2 = (hr == S_OK);

    if (!has_eq1 || !has_eq2)
        return FALSE;

    // Check to ensure that the types are actually opted into equivalence.
    if (!IsTypeDefEquivalent(tk1, pModule1) || !IsTypeDefEquivalent(tk2, pModule2))
        return FALSE;

    // *************************************************************************
    // 2. the two types have the same type identity
    // *************************************************************************
    if (!data1.IsEqual(data2))
        return FALSE;

    IfFailThrow(pInternalImport1->GetTypeDefProps(tk1, &dwAttrType1, &tkExtends1));
    IfFailThrow(pInternalImport2->GetTypeDefProps(tk2, &dwAttrType2, &tkExtends2));

    // *************************************************************************
    // 2a. the two types have the same name and namespace
    // *************************************************************************
    {
        LPCUTF8 pszName1;
        LPCUTF8 pszNamespace1;
        LPCUTF8 pszName2;
        LPCUTF8 pszNamespace2;

        IfFailThrow(pInternalImport1->GetNameOfTypeDef(tk1, &pszName1, &pszNamespace1));
        IfFailThrow(pInternalImport2->GetNameOfTypeDef(tk2, &pszName2, &pszNamespace2));

        if (strcmp(pszName1, pszName2) != 0 || strcmp(pszNamespace1, pszNamespace2) != 0)
        {
            return FALSE;
        }
    }

    // *************************************************************************
    // 2b. the two types must not be nested... or they must have an equivalent enclosing type
    // *************************************************************************
    {
        if (!!IsTdNested(dwAttrType1) != !!IsTdNested(dwAttrType2))
        {
            return FALSE;
        }

        if (IsTdNested(dwAttrType1))
        {
            mdToken tkEnclosing1;
            mdToken tkEnclosing2;

            IfFailThrow(pInternalImport1->GetNestedClassProps(tk1, &tkEnclosing1));
            IfFailThrow(pInternalImport2->GetNestedClassProps(tk2, &tkEnclosing2));

            if (!CompareTypeDefsForEquivalence(tkEnclosing1, tkEnclosing2, pModule1, pModule2, pVisited))
            {
                return FALSE;
            }
        }
    }

    // *************************************************************************
    // 3. type is an interface, struct, enum, or delegate
    // *************************************************************************
    if (IsTdInterface(dwAttrType1))
    {
        // interface
        if (!IsTdInterface(dwAttrType2))
            return FALSE;
    }
    else
    {
        mdToken tdEnum = g_pEnumClass->GetCl();
        Module *pSystemModule = g_pEnumClass->GetModule();

        if (CompareTypeTokens(tkExtends1, tdEnum, pModule1, pSystemModule, &newVisited))
        {
            // enum (extends System.Enum)
            if (!CompareTypeTokens(tkExtends2, tdEnum, pModule2, pSystemModule, &newVisited))
                return FALSE;

            if (!CompareStructuresForEquivalence(tk1, tk2, pModule1, pModule2, TRUE, &newVisited))
                return FALSE;
        }
        else
        {
            mdToken tdValueType = g_pValueTypeClass->GetCl();
            _ASSERTE(pSystemModule == g_pValueTypeClass->GetModule());

            if (CompareTypeTokens(tkExtends1, tdValueType, pModule1, pSystemModule, &newVisited) &&
                (tk1 != tdEnum || pModule1 != pSystemModule))
            {
                // struct (extends System.ValueType but is not System.Enum)
                if (!CompareTypeTokens(tkExtends2, tdValueType, pModule2, pSystemModule, &newVisited) ||
                    (tk2 == tdEnum && pModule2 == pSystemModule))
                    return FALSE;

                if  (!CompareStructuresForEquivalence(tk1, tk2, pModule1, pModule2, FALSE, &newVisited))
                    return FALSE;
            }
            else
            {
                mdToken tdMCDelegate = g_pMulticastDelegateClass->GetCl();
                _ASSERTE(pSystemModule == g_pMulticastDelegateClass->GetModule());

                if (CompareTypeTokens(tkExtends1, tdMCDelegate, pModule1, pSystemModule, &newVisited))
                {
                    // delegate (extends System.MulticastDelegate)
                    if (!CompareTypeTokens(tkExtends2, tdMCDelegate, pModule2, pSystemModule, &newVisited))
                        return FALSE;

                    if (!CompareDelegatesForEquivalence(tk1, tk2, pModule1, pModule2, &newVisited))
                        return FALSE;
                }
                else
                {
                    // the type is neither interface, struct, enum, nor delegate
                    return FALSE;
                }
            }
        }
    }
    return TRUE;

#else //!defined(DACCESS_COMPILE) && defined(FEATURE_TYPEEQUIVALENCE)

#ifdef DACCESS_COMPILE
    // We shouldn't execute this code in dac builds.
    _ASSERTE(FALSE);
#endif
    return (tk1 == tk2) && (pModule1 == pModule2);
#endif //!defined(DACCESS_COMPILE) && defined(FEATURE_COMINTEROP)
}


BOOL CompareTypeTokens(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2, TokenPairList *pVisited /*= NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    HRESULT hr;
    IMDInternalImport *pInternalImport1;
    IMDInternalImport *pInternalImport2;
    LPCUTF8 pszName1;
    LPCUTF8 pszNamespace1;
    LPCUTF8 pszName2;
    LPCUTF8 pszNamespace2;
    mdToken enclosingTypeTk1;
    mdToken enclosingTypeTk2;

    if (dac_cast<TADDR>(pModule1) == dac_cast<TADDR>(pModule2) &&
        tk1 == tk2)
    {
        return TRUE;
    }

    pInternalImport1 = pModule1->GetMDImport();
    if (!pInternalImport1->IsValidToken(tk1))
    {
        BAD_FORMAT_NOTHROW_ASSERT(!"Invalid token");
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }

    pInternalImport2 = pModule2->GetMDImport();
    if (!pInternalImport2->IsValidToken(tk2))
    {
        BAD_FORMAT_NOTHROW_ASSERT(!"Invalid token");
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }

    pszName1 = NULL;
    pszNamespace1 = NULL;
    if (TypeFromToken(tk1) == mdtTypeRef)
    {
        IfFailGo(pInternalImport1->GetNameOfTypeRef(tk1, &pszNamespace1, &pszName1));
    }
    else if (TypeFromToken(tk1) == mdtTypeDef)
    {
        if (TypeFromToken(tk2) == mdtTypeDef)
        {
#ifdef FEATURE_TYPEEQUIVALENCE
            // two type defs can't be the same unless they are identical or resolve to
            // equivalent types (equivalence based on GUID and TypeIdentifierAttribute)
            return CompareTypeDefsForEquivalence(tk1, tk2, pModule1, pModule2, pVisited);
#else // FEATURE_TYPEEQUIVALENCE
            // two type defs can't be the same unless they are identical
            return FALSE;
#endif // FEATURE_TYPEEQUIVALENCE
        }
        IfFailGo(pInternalImport1->GetNameOfTypeDef(tk1, &pszName1, &pszNamespace1));
    }
    else
    {
        return FALSE;  // comparing a type against a module or assemblyref, no match
    }

    pszName2 = NULL;
    pszNamespace2 = NULL;
    if (TypeFromToken(tk2) == mdtTypeRef)
    {
        IfFailGo(pInternalImport2->GetNameOfTypeRef(tk2, &pszNamespace2, &pszName2));
    }
    else if (TypeFromToken(tk2) == mdtTypeDef)
    {
        IfFailGo(pInternalImport2->GetNameOfTypeDef(tk2, &pszName2, &pszNamespace2));
    }
    else
    {
        return FALSE;       // comparing a type against a module or assemblyref, no match
    }

    _ASSERTE((pszNamespace1 != NULL) && (pszNamespace2 != NULL));
    if (strcmp(pszName1, pszName2) != 0 || strcmp(pszNamespace1, pszNamespace2) != 0)
    {
        return FALSE;
    }

    //////////////////////////////////////////////////////////////////////
    // OK names pass, see if it is nested, and if so that the nested classes are the same

    enclosingTypeTk1 = mdTokenNil;
    if (TypeFromToken(tk1) == mdtTypeRef)
    {
        IfFailGo(pInternalImport1->GetResolutionScopeOfTypeRef(tk1, &enclosingTypeTk1));
        if (enclosingTypeTk1 == mdTypeRefNil)
        {
            enclosingTypeTk1 = mdTokenNil;
        }
    }
    else
    {
        if (FAILED(hr = pInternalImport1->GetNestedClassProps(tk1, &enclosingTypeTk1)))
        {
            if (hr != CLDB_E_RECORD_NOTFOUND)
            {
                IfFailGo(hr);
            }
            enclosingTypeTk1 = mdTokenNil;
        }
    }

    enclosingTypeTk2 = mdTokenNil;
    if (TypeFromToken(tk2) == mdtTypeRef)
    {
        IfFailGo(pInternalImport2->GetResolutionScopeOfTypeRef(tk2, &enclosingTypeTk2));
        if (enclosingTypeTk2 == mdTypeRefNil)
        {
            enclosingTypeTk2 = mdTokenNil;
        }
    }
    else
    {
        if (FAILED(hr = pInternalImport2->GetNestedClassProps(tk2, &enclosingTypeTk2)))
        {
            if (hr != CLDB_E_RECORD_NOTFOUND)
            {
                IfFailGo(hr);
            }
            enclosingTypeTk2 = mdTokenNil;
        }
    }

    if (TypeFromToken(enclosingTypeTk1) == mdtTypeRef || TypeFromToken(enclosingTypeTk1) == mdtTypeDef)
    {
        if (!CompareTypeTokens(enclosingTypeTk1, enclosingTypeTk2, pModule1, pModule2, pVisited))
            return FALSE;

        // TODO: We could return TRUE if we knew that type equivalence was not exercised during the previous call.
    }
    else
    {
        // Check if tk1 is non-nested, but tk2 is nested
        if (TypeFromToken(enclosingTypeTk2) == mdtTypeRef || TypeFromToken(enclosingTypeTk2) == mdtTypeDef)
            return FALSE;
    }

    //////////////////////////////////////////////////////////////////////
    // OK, we have non-nested types or the the enclosing types are equivalent


    // Do not load the type! (Or else you may run into circular dependency loading problems.)
    Module* pFoundModule1;
    mdToken foundTypeDefToken1;
    if (!ClassLoader::ResolveTokenToTypeDefThrowing(pModule1,
                                                    tk1,
                                                    &pFoundModule1,
                                                    &foundTypeDefToken1))
    {
        return FALSE;
    }
    _ASSERTE(TypeFromToken(foundTypeDefToken1) == mdtTypeDef);

    Module* pFoundModule2;
    mdToken foundTypeDefToken2;
    if (!ClassLoader::ResolveTokenToTypeDefThrowing(pModule2,
                                                    tk2,
                                                    &pFoundModule2,
                                                    &foundTypeDefToken2))
    {
        return FALSE;
    }
    _ASSERTE(TypeFromToken(foundTypeDefToken2) == mdtTypeDef);

    _ASSERTE(TypeFromToken(foundTypeDefToken1) == mdtTypeDef && TypeFromToken(foundTypeDefToken2) == mdtTypeDef);
    return CompareTypeTokens(foundTypeDefToken1, foundTypeDefToken2, pFoundModule1, pFoundModule2, pVisited);

ErrExit:
#ifdef DACCESS_COMPILE
    ThrowHR(hr);
#else
    EEFileLoadException::Throw(pModule2->GetFile(), hr);
#endif //!DACCESS_COMPILE
} // CompareTypeTokens

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

//---------------------------------------------------------------------------------------
//
// Compare the next elements in two sigs.
//
// static
BOOL
MetaSig::CompareElementType(
    PCCOR_SIGNATURE &    pSig1,
    PCCOR_SIGNATURE &    pSig2,
    PCCOR_SIGNATURE      pEndSig1,
    PCCOR_SIGNATURE      pEndSig2,
    Module *             pModule1,
    Module *             pModule2,
    const Substitution * pSubst1,
    const Substitution * pSubst2,
    TokenPairList *      pVisited) // = NULL
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

 redo:
    // We jump here if the Type was a ET_CMOD prefix.
    // The caller expects us to handle CMOD's but not present them as types on their own.

    if ((pSig1 >= pEndSig1) || (pSig2 >= pEndSig2))
    {   // End of sig encountered prematurely
        return FALSE;
    }

    if ((*pSig2 == ELEMENT_TYPE_VAR) && (pSubst2 != NULL) && !pSubst2->GetInst().IsNull())
    {
        SigPointer inst = pSubst2->GetInst();
        pSig2++;
        DWORD index;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &index));

        for (DWORD i = 0; i < index; i++)
        {
            IfFailThrow(inst.SkipExactlyOne());
        }
        PCCOR_SIGNATURE pSig3 = inst.GetPtr();
        IfFailThrow(inst.SkipExactlyOne());
        PCCOR_SIGNATURE pEndSig3 = inst.GetPtr();

        return CompareElementType(
            pSig1,
            pSig3,
            pEndSig1,
            pEndSig3,
            pModule1,
            pSubst2->GetModule(),
            pSubst1,
            pSubst2->GetNext(),
            pVisited);
    }

    if ((*pSig1 == ELEMENT_TYPE_VAR) && (pSubst1 != NULL) && !pSubst1->GetInst().IsNull())
    {
        SigPointer inst = pSubst1->GetInst();
        pSig1++;
        DWORD index;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &index));

        for (DWORD i = 0; i < index; i++)
        {
            IfFailThrow(inst.SkipExactlyOne());
        }
        PCCOR_SIGNATURE pSig3 = inst.GetPtr();
        IfFailThrow(inst.SkipExactlyOne());
        PCCOR_SIGNATURE pEndSig3 = inst.GetPtr();

        return CompareElementType(
            pSig3,
            pSig2,
            pEndSig3,
            pEndSig2,
            pSubst1->GetModule(),
            pModule2,
            pSubst1->GetNext(),
            pSubst2,
            pVisited);
    }

    CorElementType Type1 = ELEMENT_TYPE_MAX; // initialize to illegal
    CorElementType Type2 = ELEMENT_TYPE_MAX; // initialize to illegal

    IfFailThrow(CorSigUncompressElementType_EndPtr(pSig1, pEndSig1, &Type1));
    IfFailThrow(CorSigUncompressElementType_EndPtr(pSig2, pEndSig2, &Type2));

    if (Type1 == ELEMENT_TYPE_INTERNAL)
    {
        // this check is not functional in DAC and provides no security against a malicious dump
        // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
        if (pModule1->IsSigInIL(pSig1))
        {
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module *)pModule1);
        }
#endif

    }

    if (Type2 == ELEMENT_TYPE_INTERNAL)
    {
        // this check is not functional in DAC and provides no security against a malicious dump
        // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
        if (pModule2->IsSigInIL(pSig2))
        {
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module *)pModule2);
        }
#endif
    }

    if (Type1 != Type2)
    {
        if ((Type1 == ELEMENT_TYPE_INTERNAL) || (Type2 == ELEMENT_TYPE_INTERNAL))
        {
            TypeHandle     hInternal;
            CorElementType eOtherType;
            Module *       pOtherModule;

            // One type is already loaded, collect all the necessary information to identify the other type.
            if (Type1 == ELEMENT_TYPE_INTERNAL)
            {
                IfFailThrow(CorSigUncompressPointer_EndPtr(pSig1, pEndSig1, (void**)&hInternal));

                eOtherType = Type2;
                pOtherModule = pModule2;
            }
            else
            {
                IfFailThrow(CorSigUncompressPointer_EndPtr(pSig2, pEndSig2, (void **)&hInternal));

                eOtherType = Type1;
                pOtherModule = pModule1;
            }

            // Internal types can only correspond to types or value types.
            switch (eOtherType)
            {
                case ELEMENT_TYPE_OBJECT:
                {
                    return (hInternal.AsMethodTable() == g_pObjectClass);
                }
                case ELEMENT_TYPE_STRING:
                {
                    return (hInternal.AsMethodTable() == g_pStringClass);
                }
                case ELEMENT_TYPE_VALUETYPE:
                case ELEMENT_TYPE_CLASS:
                {
                    mdToken tkOther;
                    if (Type1 == ELEMENT_TYPE_INTERNAL)
                    {
                        IfFailThrow(CorSigUncompressToken_EndPtr(pSig2, pEndSig2, &tkOther));
                    }
                    else
                    {
                        IfFailThrow(CorSigUncompressToken_EndPtr(pSig1, pEndSig1, &tkOther));
                    }

                    TypeHandle hOtherType = ClassLoader::LoadTypeDefOrRefThrowing(
                        pOtherModule,
                        tkOther,
                        ClassLoader::ReturnNullIfNotFound,
                        ClassLoader::FailIfUninstDefOrRef);

                    return (hInternal == hOtherType);
                }
                default:
                {
                    return FALSE;
                }
            }
        }
        else
        {
            return FALSE; // types must be the same
        }
    }

    switch (Type1)
    {
        default:
        {
            // Unknown type!
            THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pModule1);
        }

        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        {
            return TRUE;
        }

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        {
            DWORD varNum1;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &varNum1));
            DWORD varNum2;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &varNum2));

            return (varNum1 == varNum2);
        }

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            mdToken tk1, tk2;

            IfFailThrow(CorSigUncompressToken_EndPtr(pSig1, pEndSig1, &tk1));
            IfFailThrow(CorSigUncompressToken_EndPtr(pSig2, pEndSig2, &tk2));

#ifndef DACCESS_COMPILE
            if (!CompareTypeDefOrRefOrSpec(
                    pModule1,
                    tk1,
                    pSubst1,
                    pModule2,
                    tk2,
                    pSubst2,
                    pVisited))
            {
                return FALSE;
            }
#endif //!DACCESS_COMPILE

            goto redo;
        }

        // These take an additional argument, which is the element type
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        {
            if (!CompareElementType(
                    pSig1,
                    pSig2,
                    pEndSig1,
                    pEndSig2,
                    pModule1,
                    pModule2,
                    pSubst1,
                    pSubst2,
                    pVisited))
            {
                return FALSE;
            }
            return TRUE;
        }

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            mdToken tk1, tk2;

            IfFailThrow(CorSigUncompressToken_EndPtr(pSig1, pEndSig1, &tk1));
            IfFailThrow(CorSigUncompressToken_EndPtr(pSig2, pEndSig2, &tk2));

            return CompareTypeTokens(tk1, tk2, pModule1, pModule2, pVisited);
        }

        case ELEMENT_TYPE_FNPTR:
        {
            // Compare calling conventions
            // Note: We used to read them as compressed integers, which is wrong, but works for correct
            // signatures as the highest bit is always 0 for calling conventions
            CorElementType callingConvention1 = ELEMENT_TYPE_MAX; // initialize to illegal
            IfFailThrow(CorSigUncompressElementType_EndPtr(pSig1, pEndSig1, &callingConvention1));
            CorElementType callingConvention2 = ELEMENT_TYPE_MAX; // initialize to illegal
            IfFailThrow(CorSigUncompressElementType_EndPtr(pSig2, pEndSig2, &callingConvention2));
            if (callingConvention1 != callingConvention2)
            {
                return FALSE;
            }

            DWORD argCnt1;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &argCnt1));
            DWORD argCnt2;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &argCnt2));
            if (argCnt1 != argCnt2)
            {
                return FALSE;
            }

            // Compressed integer values can be only 0-0x1FFFFFFF
            _ASSERTE(argCnt1 < MAXDWORD);
            // Add return parameter into the parameter count (it cannot overflow)
            argCnt1++;

            TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(pVisited);
            // Compare all parameters, incl. return parameter
            while (argCnt1 > 0)
            {
                if (!CompareElementType(
                        pSig1,
                        pSig2,
                        pEndSig1,
                        pEndSig2,
                        pModule1,
                        pModule2,
                        pSubst1,
                        pSubst2,
                        &newVisited))
                {
                    return FALSE;
                }
                --argCnt1;
            }
            return TRUE;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            TokenPairList newVisited = TokenPairList::AdjustForTypeSpec(
                pVisited,
                pModule1,
                pSig1 - 1,
                (DWORD)(pEndSig1 - pSig1) + 1);
            TokenPairList newVisitedAlwaysForbidden = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(pVisited);

            // Type constructors - The actual type is never permitted to participate in type equivalence.
            if (!CompareElementType(
                    pSig1,
                    pSig2,
                    pEndSig1,
                    pEndSig2,
                    pModule1,
                    pModule2,
                    pSubst1,
                    pSubst2,
                    &newVisitedAlwaysForbidden))
            {
                return FALSE;
            }

            DWORD argCnt1;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &argCnt1));
            DWORD argCnt2;
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &argCnt2));
            if (argCnt1 != argCnt2)
            {
                return FALSE;
            }

            while (argCnt1 > 0)
            {
                if (!CompareElementType(
                        pSig1,
                        pSig2,
                        pEndSig1,
                        pEndSig2,
                        pModule1,
                        pModule2,
                        pSubst1,
                        pSubst2,
                        &newVisited))
                {
                    return FALSE;
                }
                --argCnt1;
            }
            return TRUE;
        }

        case ELEMENT_TYPE_ARRAY:
        {
            // syntax: ARRAY <base type> rank <count n> <size 1> .... <size n> <lower bound m>
            // <lb 1> .... <lb m>
            DWORD rank1, rank2;
            DWORD dimension_sizes1, dimension_sizes2;
            DWORD dimension_lowerb1, dimension_lowerb2;
            DWORD i;

            // element type
            if (!CompareElementType(
                    pSig1,
                    pSig2,
                    pEndSig1,
                    pEndSig2,
                    pModule1,
                    pModule2,
                    pSubst1,
                    pSubst2,
                    pVisited))
            {
                return FALSE;
            }

            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &rank1));
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &rank2));
            if (rank1 != rank2)
            {
                return FALSE;
            }
            // A zero ends the array spec
            if (rank1 == 0)
            {
                return TRUE;
            }

            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &dimension_sizes1));
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &dimension_sizes2));
            if (dimension_sizes1 != dimension_sizes2)
            {
                return FALSE;
            }

            for (i = 0; i < dimension_sizes1; i++)
            {
                DWORD size1, size2;

                if (pSig1 == pEndSig1)
                {   // premature end ok
                    return TRUE;
                }

                IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &size1));
                IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &size2));
                if (size1 != size2)
                {
                    return FALSE;
                }
            }

            if (pSig1 == pEndSig1)
            {   // premature end ok
                return TRUE;
            }

            // # dimensions for lower bounds
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &dimension_lowerb1));
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &dimension_lowerb2));
            if (dimension_lowerb1 != dimension_lowerb2)
            {
                return FALSE;
            }

            for (i = 0; i < dimension_lowerb1; i++)
            {
                DWORD size1, size2;

                if (pSig1 == pEndSig1)
                {   // premature end ok
                    return TRUE;
                }

                IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &size1));
                IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &size2));
                if (size1 != size2)
                {
                    return FALSE;
                }
            }
            return TRUE;
        }

        case ELEMENT_TYPE_INTERNAL:
        {
            TypeHandle hType1, hType2;

            IfFailThrow(CorSigUncompressPointer_EndPtr(pSig1, pEndSig1, (void **)&hType1));
            IfFailThrow(CorSigUncompressPointer_EndPtr(pSig2, pEndSig2, (void **)&hType2));

            return (hType1 == hType2);
        }
    } // switch
    // Unreachable
    UNREACHABLE();
} // MetaSig::CompareElementType
#ifdef _PREFAST_
#pragma warning(pop)
#endif


//---------------------------------------------------------------------------------------
//
BOOL
MetaSig::CompareTypeDefsUnderSubstitutions(
    MethodTable *        pTypeDef1,
    MethodTable *        pTypeDef2,
    const Substitution * pSubst1,
    const Substitution * pSubst2,
    TokenPairList *      pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    bool fSameTypeDef = (pTypeDef1->GetTypeDefRid() == pTypeDef2->GetTypeDefRid()) && (pTypeDef1->GetModule() == pTypeDef2->GetModule());

    if (!fSameTypeDef)
    {
        if (!pTypeDef1->GetClass()->IsEquivalentType() || !pTypeDef2->GetClass()->IsEquivalentType() || TokenPairList::InTypeEquivalenceForbiddenScope(pVisited))
        {
            return FALSE;
        }
        else
        {
            if (!CompareTypeDefsForEquivalence(pTypeDef1->GetCl(), pTypeDef2->GetCl(), pTypeDef1->GetModule(), pTypeDef2->GetModule(), pVisited))
            {
                return FALSE;
            }
        }
    }

    if (pTypeDef1->GetNumGenericArgs() != pTypeDef2->GetNumGenericArgs())
        return FALSE;

    if (pTypeDef1->GetNumGenericArgs() == 0)
        return TRUE;

    if ((pSubst1 == NULL) || (pSubst2 == NULL) || pSubst1->GetInst().IsNull() || pSubst2->GetInst().IsNull())
        return FALSE;

    SigPointer inst1 = pSubst1->GetInst();
    SigPointer inst2 = pSubst2->GetInst();
    for (DWORD i = 0; i < pTypeDef1->GetNumGenericArgs(); i++)
    {
        PCCOR_SIGNATURE startInst1 = inst1.GetPtr();
        IfFailThrow(inst1.SkipExactlyOne());
        PCCOR_SIGNATURE endInst1ptr = inst1.GetPtr();
        PCCOR_SIGNATURE startInst2 = inst2.GetPtr();
        IfFailThrow(inst2.SkipExactlyOne());
        PCCOR_SIGNATURE endInst2ptr = inst2.GetPtr();
        if (!CompareElementType(
                startInst1,
                startInst2,
                endInst1ptr,
                endInst2ptr,
                pSubst1->GetModule(),
                pSubst2->GetModule(),
                pSubst1->GetNext(),
                pSubst2->GetNext(),
                pVisited))
        {
            return FALSE;
        }
    }
    return TRUE;

} // MetaSig::CompareTypeDefsUnderSubstitutions

//---------------------------------------------------------------------------------------
//
BOOL
TypeHandleCompareHelper(
    TypeHandle th1,
    TypeHandle th2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE
    return th1.IsEquivalentTo(th2);
#else
    return TRUE;
#endif // #ifndef DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
//static
BOOL
MetaSig::CompareMethodSigs(
    MetaSig & msig1,
    MetaSig & msig2,
    BOOL      ignoreCallconv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    if (!ignoreCallconv &&
        ((msig1.GetCallingConventionInfo() & IMAGE_CEE_CS_CALLCONV_MASK)
         != (msig2.GetCallingConventionInfo() & IMAGE_CEE_CS_CALLCONV_MASK)))
    {
        return FALSE; // calling convention mismatch
    }

    if (msig1.NumFixedArgs() != msig2.NumFixedArgs())
        return FALSE; // number of arguments don't match

    // check that the argument types are equal
    for (DWORD i = 0; i<msig1.NumFixedArgs(); i++) //@GENERICSVER: does this really do the return type too?
    {
        CorElementType  et1 = msig1.NextArg();
        CorElementType  et2 = msig2.NextArg();
        if (et1 != et2)
            return FALSE;
        if (!CorTypeInfo::IsPrimitiveType(et1))
        {
            if (!TypeHandleCompareHelper(msig1.GetLastTypeHandleThrowing(), msig2.GetLastTypeHandleThrowing()))
                return FALSE;
        }
    }

    CorElementType  ret1 = msig1.GetReturnType();
    CorElementType  ret2 = msig2.GetReturnType();
    if (ret1 != ret2)
        return FALSE;

    if (!CorTypeInfo::IsPrimitiveType(ret1))
    {
        return TypeHandleCompareHelper(msig1.GetRetTypeHandleThrowing(), msig2.GetRetTypeHandleThrowing());
    }

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
//static
HRESULT
MetaSig::CompareMethodSigsNT(
    PCCOR_SIGNATURE      pSignature1,
    DWORD                cSig1,
    Module *             pModule1,
    const Substitution * pSubst1,
    PCCOR_SIGNATURE      pSignature2,
    DWORD                cSig2,
    Module *             pModule2,
    const Substitution * pSubst2,
    TokenPairList *      pVisited) //= NULL
{
    STATIC_CONTRACT_NOTHROW;

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (CompareMethodSigs(pSignature1, cSig1, pModule1, pSubst1, pSignature2, cSig2, pModule2, pSubst2, FALSE, pVisited))
            hr = S_OK;
        else
            hr = S_FALSE;
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Compare two method sigs and return whether they are the same.
// @GENERICS: instantiation of the type variables in the second signature
//
//static
BOOL
MetaSig::CompareMethodSigs(
    PCCOR_SIGNATURE      pSignature1,
    DWORD                cSig1,
    Module *             pModule1,
    const Substitution * pSubst1,
    PCCOR_SIGNATURE      pSignature2,
    DWORD                cSig2,
    Module *             pModule2,
    const Substitution * pSubst2,
    BOOL                 skipReturnTypeSig,
    TokenPairList *      pVisited) //= NULL
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    PCCOR_SIGNATURE pSig1 = pSignature1;
    PCCOR_SIGNATURE pSig2 = pSignature2;
    PCCOR_SIGNATURE pEndSig1 = pSignature1 + cSig1;
    PCCOR_SIGNATURE pEndSig2 = pSignature2 + cSig2;
    DWORD           ArgCount1;
    DWORD           ArgCount2;
    DWORD           i;

    // If scopes are the same, and sigs are same, can return.
    // If the sigs aren't the same, but same scope, can't return yet, in
    // case there are two AssemblyRefs pointing to the same assembly or such.
    if ((pModule1 == pModule2) &&
        (cSig1 == cSig2) &&
        (pSubst1 == NULL) &&
        (pSubst2 == NULL) &&
        (memcmp(pSig1, pSig2, cSig1) == 0))
    {
        return TRUE;
    }

    if ((*pSig1 & ~CORINFO_CALLCONV_PARAMTYPE) != (*pSig2 & ~CORINFO_CALLCONV_PARAMTYPE))
    {   // Calling convention or hasThis mismatch
        return FALSE;
    }

    __int8 callConv = *pSig1;

    pSig1++;
    pSig2++;

    if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        DWORD TyArgCount1;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &TyArgCount1));
        DWORD TyArgCount2;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &TyArgCount2));

        if (TyArgCount1 != TyArgCount2)
            return FALSE;
    }

    IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &ArgCount1));
    IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &ArgCount2));

    if (ArgCount1 != ArgCount2)
    {
        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_VARARG)
            return FALSE;

        // Signature #1 is the caller.  We proceed until we hit the sentinel, or we hit
        // the end of the signature (which is an implied sentinel).  We never worry about
        // what follows the sentinel, because that is the ... part, which is not
        // involved in matching.
        //
        // Theoretically, it's illegal for a sentinel to be the last element in the
        // caller's signature, because it's redundant.  We don't waste our time checking
        // that case, but the metadata validator should.  Also, it is always illegal
        // for a sentinel to appear in a callee's signature.  We assert against this,
        // but in the shipping product the comparison would simply fail.
        //
        // Signature #2 is the callee.  We must hit the exact end of the callee, because
        // we are trying to match on everything up to the variable part.  This allows us
        // to correctly handle overloads, where there are a number of varargs methods
        // to pick from, like m1(int,...) and m2(int,int,...), etc.

        // <= because we want to include a check of the return value!
        for (i = 0; i <= ArgCount1; i++)
        {
            // We may be just going out of bounds on the callee, but no further than that.
            _ASSERTE(i <= ArgCount2 + 1);

            // If we matched all the way on the caller, is the callee now complete?
            if (*pSig1 == ELEMENT_TYPE_SENTINEL)
                return (i > ArgCount2);

            // if we have more to compare on the caller side, but the callee side is
            // exhausted, this isn't our match
            if (i > ArgCount2)
                return FALSE;

            // This would be a breaking change to make this throw... see comment above
            _ASSERT(*pSig2 != ELEMENT_TYPE_SENTINEL);

            if (i == 0 && skipReturnTypeSig)
            {
                SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
                IfFailThrow(ptr1.SkipExactlyOne());
                pSig1 = ptr1.GetPtr();

                SigPointer ptr2(pSig2, (DWORD)(pEndSig2 - pSig2));
                IfFailThrow(ptr2.SkipExactlyOne());
                pSig2 = ptr2.GetPtr();
            }
            else
            {
                // We are in bounds on both sides.  Compare the element.
                if (!CompareElementType(
                    pSig1,
                    pSig2,
                    pEndSig1,
                    pEndSig2,
                    pModule1,
                    pModule2,
                    pSubst1,
                    pSubst2,
                    pVisited))
                {
                    return FALSE;
                }
            }
        }

        // If we didn't consume all of the callee signature, then we failed.
        if (i <= ArgCount2)
            return FALSE;

        return TRUE;
    }

    // do return type as well
    for (i = 0; i <= ArgCount1; i++)
    {
        if (i == 0 && skipReturnTypeSig)
        {
            SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
            IfFailThrow(ptr1.SkipExactlyOne());
            pSig1 = ptr1.GetPtr();

            SigPointer ptr2(pSig2, (DWORD)(pEndSig2 - pSig2));
            IfFailThrow(ptr2.SkipExactlyOne());
            pSig2 = ptr2.GetPtr();
        }
        else
        {
            if (!CompareElementType(
                pSig1,
                pSig2,
                pEndSig1,
                pEndSig2,
                pModule1,
                pModule2,
                pSubst1,
                pSubst2,
                pVisited))
            {
                return FALSE;
            }
        }
    }

    return TRUE;
} // MetaSig::CompareMethodSigs

//---------------------------------------------------------------------------------------
//
//static
BOOL MetaSig::CompareFieldSigs(
    PCCOR_SIGNATURE pSignature1,
    DWORD           cSig1,
    Module *        pModule1,
    PCCOR_SIGNATURE pSignature2,
    DWORD           cSig2,
    Module *        pModule2,
    TokenPairList * pVisited) //= NULL
{
    WRAPPER_NO_CONTRACT;

    PCCOR_SIGNATURE pSig1 = pSignature1;
    PCCOR_SIGNATURE pSig2 = pSignature2;
    PCCOR_SIGNATURE pEndSig1;
    PCCOR_SIGNATURE pEndSig2;

#if 0
    // <TODO>@TODO: If scopes are the same, use identity rule - for now, don't, so that we test the code paths</TODO>
    if (cSig1 != cSig2)
        return(FALSE); // sigs must be same size if they are in the same scope
#endif

    if (*pSig1 != *pSig2)
        return(FALSE); // calling convention, must be IMAGE_CEE_CS_CALLCONV_FIELD

    pEndSig1 = pSig1 + cSig1;
    pEndSig2 = pSig2 + cSig2;

    return(CompareElementType(++pSig1, ++pSig2, pEndSig1, pEndSig2, pModule1, pModule2, NULL, NULL, pVisited));
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
//static
BOOL
MetaSig::CompareElementTypeToToken(
    PCCOR_SIGNATURE &    pSig1,
    PCCOR_SIGNATURE      pEndSig1, // end of sig1
    mdToken              tk2,
    Module *             pModule1,
    Module *             pModule2,
    const Substitution * pSubst1,
    TokenPairList *      pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE((TypeFromToken(tk2) == mdtTypeDef) ||
             (TypeFromToken(tk2) == mdtTypeRef));

    if (pSig1 >= pEndSig1)
    {   // End of sig encountered prematurely
        return FALSE;
    }

    if ((*pSig1 == ELEMENT_TYPE_VAR) && (pSubst1 != NULL) && !pSubst1->GetInst().IsNull())
    {
        SigPointer inst = pSubst1->GetInst();
        pSig1++;
        DWORD index;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &index));

        for (DWORD i = 0; i < index; i++)
        {
            IfFailThrow(inst.SkipExactlyOne());
        }
        PCCOR_SIGNATURE pSig3 = inst.GetPtr();
        IfFailThrow(inst.SkipExactlyOne());
        PCCOR_SIGNATURE pEndSig3 = inst.GetPtr();

        return CompareElementTypeToToken(
            pSig3,
            pEndSig3,
            tk2,
            pSubst1->GetModule(),
            pModule2,
            pSubst1->GetNext(),
            pVisited);
    }

    CorElementType Type1 = ELEMENT_TYPE_MAX; // initialize to illegal

    IfFailThrow(CorSigUncompressElementType_EndPtr(pSig1, pEndSig1, &Type1));
    _ASSERTE(Type1 != ELEMENT_TYPE_INTERNAL);

    if (Type1 == ELEMENT_TYPE_INTERNAL)
    {
        // this check is not functional in DAC and provides no security against a malicious dump
        // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
        if (pModule1->IsSigInIL(pSig1))
        {
            THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)pModule1);
        }
#endif
    }

    switch (Type1)
    {
        default:
        {   // Unknown type!
            THROW_BAD_FORMAT(BFA_BAD_COMPLUS_SIG, pModule1);
        }

        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        {
            break;
        }

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        {
           return FALSE;
        }
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        {
            return FALSE;
        }
        // These take an additional argument, which is the element type
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        {
           return FALSE;
        }
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            mdToken tk1;

            IfFailThrow(CorSigUncompressToken_EndPtr(pSig1, pEndSig1, &tk1));

            return CompareTypeTokens(
                tk1,
                tk2,
                pModule1,
                pModule2,
                pVisited);
        }
        case ELEMENT_TYPE_FNPTR:
        {
            return FALSE;
        }
        case ELEMENT_TYPE_GENERICINST:
        {
            return FALSE;
        }
        case ELEMENT_TYPE_ARRAY:
        {
            return FALSE;
        }
        case ELEMENT_TYPE_INTERNAL:
        {
            return FALSE;
        }
    }

    return CompareTypeTokens(
        CoreLibBinder::GetElementType(Type1)->GetCl(),
        tk2,
        CoreLibBinder::GetModule(),
        pModule2,
        pVisited);
} // MetaSig::CompareElementTypeToToken

/* static */
BOOL MetaSig::CompareTypeSpecToToken(mdTypeSpec tk1,
                            mdToken tk2,
                            Module *pModule1,
                            Module *pModule2,
                            const Substitution *pSubst1,
                            TokenPairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE(TypeFromToken(tk1) == mdtTypeSpec);
    _ASSERTE(TypeFromToken(tk2) == mdtTypeDef ||
             TypeFromToken(tk2) == mdtTypeRef);

    IMDInternalImport *pInternalImport = pModule1->GetMDImport();

    PCCOR_SIGNATURE pSig1;
    ULONG cSig1;
    IfFailThrow(pInternalImport->GetTypeSpecFromToken(tk1, &pSig1, &cSig1));

    TokenPairList newVisited = TokenPairList::AdjustForTypeSpec(pVisited, pModule1, pSig1, cSig1);

    return CompareElementTypeToToken(pSig1,pSig1+cSig1,tk2,pModule1,pModule2,pSubst1,&newVisited);
} // MetaSig::CompareTypeSpecToToken


/* static */
BOOL MetaSig::CompareTypeDefOrRefOrSpec(Module *pModule1, mdToken tok1,
                                        const Substitution *pSubst1,
                                        Module *pModule2, mdToken tok2,
                                        const Substitution *pSubst2,
                                        TokenPairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    if (TypeFromToken(tok1) != mdtTypeSpec && TypeFromToken(tok2) != mdtTypeSpec)
    {
        _ASSERTE(TypeFromToken(tok1) == mdtTypeDef || TypeFromToken(tok1) == mdtTypeRef);
        _ASSERTE(TypeFromToken(tok2) == mdtTypeDef || TypeFromToken(tok2) == mdtTypeRef);
        return CompareTypeTokens(tok1,tok2,pModule1,pModule2,pVisited);
    }

    if (TypeFromToken(tok1) != TypeFromToken(tok2))
    {
        if (TypeFromToken(tok1) == mdtTypeSpec)
        {
            return CompareTypeSpecToToken(tok1,tok2,pModule1,pModule2,pSubst1,pVisited);
        }
        else
        {
            _ASSERTE(TypeFromToken(tok2) == mdtTypeSpec);
            return CompareTypeSpecToToken(tok2,tok1,pModule2,pModule1,pSubst2,pVisited);
        }
    }

    _ASSERTE(TypeFromToken(tok1) == mdtTypeSpec &&
             TypeFromToken(tok2) == mdtTypeSpec);

    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    PCCOR_SIGNATURE pSig1,pSig2;
    ULONG cSig1,cSig2;
    IfFailThrow(pInternalImport1->GetTypeSpecFromToken(tok1, &pSig1, &cSig1));
    IfFailThrow(pInternalImport2->GetTypeSpecFromToken(tok2, &pSig2, &cSig2));
    return MetaSig::CompareElementType(pSig1, pSig2, pSig1 + cSig1, pSig2 + cSig2, pModule1, pModule2, pSubst1, pSubst2, pVisited);
} // MetaSig::CompareTypeDefOrRefOrSpec

/* static */
BOOL MetaSig::CompareVariableConstraints(const Substitution *pSubst1,
                                         Module *pModule1, mdGenericParam tok1, //overriding
                                         const Substitution *pSubst2,
                                         Module *pModule2, mdGenericParam tok2) //overridden
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    DWORD specialConstraints1,specialConstraints2;

     // check special constraints
    {
        IfFailThrow(pInternalImport1->GetGenericParamProps(tok1, NULL, &specialConstraints1, NULL, NULL, NULL));
        IfFailThrow(pInternalImport2->GetGenericParamProps(tok2, NULL, &specialConstraints2, NULL, NULL, NULL));
        specialConstraints1 = specialConstraints1 & gpSpecialConstraintMask;
        specialConstraints2 = specialConstraints2 & gpSpecialConstraintMask;

        if ((specialConstraints1 & gpNotNullableValueTypeConstraint) != 0)
        {
            if ((specialConstraints2 & gpNotNullableValueTypeConstraint) == 0)
                return FALSE;
        }
        if ((specialConstraints1 & gpReferenceTypeConstraint) != 0)
        {
            if ((specialConstraints2 & gpReferenceTypeConstraint) == 0)
                return FALSE;
        }
        if ((specialConstraints1 & gpDefaultConstructorConstraint) != 0)
        {
            if ((specialConstraints2 & (gpDefaultConstructorConstraint | gpNotNullableValueTypeConstraint)) == 0)
                return FALSE;
        }
    }


    HENUMInternalHolder hEnum1(pInternalImport1);
    mdGenericParamConstraint tkConstraint1;
    hEnum1.EnumInit(mdtGenericParamConstraint, tok1);

    while (pInternalImport1->EnumNext(&hEnum1, &tkConstraint1))
    {
        mdToken tkConstraintType1, tkParam1;
        IfFailThrow(pInternalImport1->GetGenericParamConstraintProps(tkConstraint1, &tkParam1, &tkConstraintType1));
        _ASSERTE(tkParam1 == tok1);

        // for each non-object constraint,
        // and, in the case of a notNullableValueType, each non-ValueType constraint,
        // find an equivalent constraint on tok2
        // NB: we do not attempt to match constraints equivalent to object (and ValueType when tok1 is notNullable)
        // because they
        // a) are vacuous, and
        // b) may be implicit (ie. absent) in the overriden variable's declaration
        if (!(CompareTypeDefOrRefOrSpec(pModule1, tkConstraintType1, NULL,
                                       CoreLibBinder::GetModule(), g_pObjectClass->GetCl(), NULL, NULL) ||
          (((specialConstraints1 & gpNotNullableValueTypeConstraint) != 0) &&
           (CompareTypeDefOrRefOrSpec(pModule1, tkConstraintType1, NULL,
                      CoreLibBinder::GetModule(), g_pValueTypeClass->GetCl(), NULL, NULL)))))
        {
            HENUMInternalHolder hEnum2(pInternalImport2);
            mdGenericParamConstraint tkConstraint2;
            hEnum2.EnumInit(mdtGenericParamConstraint, tok2);

            BOOL found = FALSE;
            while (!found && pInternalImport2->EnumNext(&hEnum2, &tkConstraint2) )
            {
                mdToken tkConstraintType2, tkParam2;
                IfFailThrow(pInternalImport2->GetGenericParamConstraintProps(tkConstraint2, &tkParam2, &tkConstraintType2));
                _ASSERTE(tkParam2 == tok2);

                found = CompareTypeDefOrRefOrSpec(pModule1, tkConstraintType1, pSubst1, pModule2, tkConstraintType2, pSubst2, NULL);
            }
            if (!found)
            {
                //none of the constrains on tyvar2 match, exit early
                return FALSE;
            }
        }
        //check next constraint of tok1
    }

    return TRUE;
}

/* static */
BOOL MetaSig::CompareMethodConstraints(const Substitution *pSubst1,
                                       Module *pModule1,
                                       mdMethodDef tok1, //implementation
                                       const Substitution *pSubst2,
                                       Module *pModule2,
                                       mdMethodDef tok2) //declaration w.r.t subsitution
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    HENUMInternalHolder hEnumTyPars1(pInternalImport1);
    HENUMInternalHolder hEnumTyPars2(pInternalImport2);

    hEnumTyPars1.EnumInit(mdtGenericParam, tok1);
    hEnumTyPars2.EnumInit(mdtGenericParam, tok2);

    mdGenericParam    tkTyPar1,tkTyPar2;

    // enumerate the variables
    DWORD numTyPars1 = pInternalImport1->EnumGetCount(&hEnumTyPars1);
    DWORD numTyPars2 = pInternalImport2->EnumGetCount(&hEnumTyPars2);

    _ASSERTE(numTyPars1 == numTyPars2);
    if (numTyPars1 != numTyPars2) //play it safe
        return FALSE; //throw bad format exception?

    for(unsigned int i = 0; i < numTyPars1; i++)
    {
        pInternalImport1->EnumNext(&hEnumTyPars1, &tkTyPar1);
        pInternalImport2->EnumNext(&hEnumTyPars2, &tkTyPar2);
        if (!CompareVariableConstraints(pSubst1, pModule1, tkTyPar1, pSubst2, pModule2, tkTyPar2))
        {
            return FALSE;
        }
    }
    return TRUE;
}

#endif // #ifndef DACCESS_COMPILE

// PromoteCarefully
//
// Clients who know they MAY have an interior pointer should come through here.  We
// can efficiently check whether our object lives on the current stack.  If so, our
// reference to it is not an interior pointer.  This is more efficient than asking
// the heap to verify whether our reference is interior, since it would have to
// check all the heap segments, including those containing large objects.
//
// Note that we only have to check against the thread we are currently crawling.  It
// would be illegal for us to have a ByRef from someone else's stack.  And this will
// be asserted if we pass this reference to the heap as a potentially interior pointer.
//
// But the thread we are currently crawling is not the currently executing thread (in
// the general case).  We rely on fragile caching of the interesting thread, in our
// call to UpdateCachedStackInfo() where we initiate the crawl in GcScanRoots() above.
//
// The flags must indicate that the have an interior pointer GC_CALL_INTERIOR
// additionally the flags may indicate that we also have a pinned local byref
//
void PromoteCarefully(promote_func   fn,
                      PTR_PTR_Object ppObj,
                      ScanContext*   sc,
                      uint32_t       flags /* = GC_CALL_INTERIOR*/ )
{
    LIMITED_METHOD_CONTRACT;

    //
    // Sanity check that the flags contain only these three values
    //
    assert((flags & ~(GC_CALL_INTERIOR|GC_CALL_PINNED)) == 0);

    //
    // Sanity check that GC_CALL_INTERIOR FLAG is set
    //
    assert(flags & GC_CALL_INTERIOR);

#if !defined(DACCESS_COMPILE)

    //
    // Sanity check the stack scan limit
    //
    assert(sc->stack_limit != 0);

    // Note that the base is at a higher address than the limit, since the stack
    // grows downwards.
    // To check whether the object is in the stack or not, we also need to check the sc->stack_limit.
    // The reason is that on Unix, the stack size can be unlimited. In such case, the system can
    // shrink the current reserved stack space. That causes the real limit of the stack to move up and
    // the range can be reused for other purposes. But the sc->stack_limit is stable during the scan.
    // Even on Windows, we care just about the stack above the stack_limit.
    if ((sc->thread_under_crawl->IsAddressInStack(*ppObj)) && (PTR_TO_TADDR(*ppObj) >= sc->stack_limit))
    {
        return;
    }

#ifndef CROSSGEN_COMPILE
    if (sc->promotion)
    {
        LoaderAllocator*pLoaderAllocator = LoaderAllocator::GetAssociatedLoaderAllocator_Unsafe(PTR_TO_TADDR(*ppObj));
        if (pLoaderAllocator != NULL)
        {
            GcReportLoaderAllocator(fn, sc, pLoaderAllocator);
        }
    }
#endif // CROSSGEN_COMPILE
#endif // !defined(DACCESS_COMPILE)

    (*fn) (ppObj, sc, flags);
}

void ReportPointersFromValueType(promote_func *fn, ScanContext *sc, PTR_MethodTable pMT, PTR_VOID pSrc)
{
    WRAPPER_NO_CONTRACT;

    if (pMT->IsByRefLike())
    {
        FindByRefPointerOffsetsInByRefLikeObject(
            pMT,
            0 /* baseOffset */,
            [&](SIZE_T pointerOffset)
            {
                PTR_PTR_Object fieldRef = dac_cast<PTR_PTR_Object>(PTR_BYTE(pSrc) + pointerOffset);
                (*fn)(fieldRef, sc, GC_CALL_INTERIOR);
            });
    }

    if (!pMT->ContainsPointers())
        return;

    CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
    CGCDescSeries* cur = map->GetHighestSeries();
    CGCDescSeries* last = map->GetLowestSeries();
    DWORD size = pMT->GetBaseSize();
    _ASSERTE(cur >= last);

    do
    {
        // offset to embedded references in this series must be
        // adjusted by the VTable pointer, when in the unboxed state.
        size_t offset = cur->GetSeriesOffset() - TARGET_POINTER_SIZE;
        PTR_OBJECTREF srcPtr = dac_cast<PTR_OBJECTREF>(PTR_BYTE(pSrc) + offset);
        PTR_OBJECTREF srcPtrStop = dac_cast<PTR_OBJECTREF>(PTR_BYTE(srcPtr) + cur->GetSeriesSize() + size);
        while (srcPtr < srcPtrStop)
        {
            (*fn)(dac_cast<PTR_PTR_Object>(srcPtr), sc, 0);
            srcPtr = (PTR_OBJECTREF)(PTR_BYTE(srcPtr) + TARGET_POINTER_SIZE);
        }
        cur--;
    } while (cur >= last);
}

void ReportPointersFromValueTypeArg(promote_func *fn, ScanContext *sc, PTR_MethodTable pMT, ArgDestination *pSrc)
{
    WRAPPER_NO_CONTRACT;

    if (!pMT->ContainsPointers() && !pMT->IsByRefLike())
    {
        return;
    }

#if defined(UNIX_AMD64_ABI)
    if (pSrc->IsStructPassedInRegs())
    {
        pSrc->ReportPointersFromStructInRegisters(fn, sc, pMT->GetNumInstanceFieldBytes());
        return;
    }
#endif // UNIX_AMD64_ABI

    ReportPointersFromValueType(fn, sc, pMT, pSrc->GetDestinationAddress());
}

//------------------------------------------------------------------
// Perform type-specific GC promotion on the value (based upon the
// last type retrieved by NextArg()).
//------------------------------------------------------------------
VOID MetaSig::GcScanRoots(ArgDestination *pValue,
                          promote_func *fn,
                          ScanContext* sc,
                          promote_carefully_func *fnc)
{

    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END


    PTR_PTR_Object pArgPtr = (PTR_PTR_Object)pValue->GetDestinationAddress();
    if (fnc == NULL)
        fnc = &PromoteCarefully;

    TypeHandle thValueType;
    CorElementType  etype = m_pLastType.PeekElemTypeNormalized(m_pModule, &m_typeContext, &thValueType);

    _ASSERTE(etype >= 0 && etype < ELEMENT_TYPE_MAX);

#ifdef _DEBUG
    PTR_Object pOldLocation;
#endif

    switch (gElementTypeInfo[etype].m_gc)
    {
        case TYPE_GC_NONE:
            // do nothing
            break;

        case TYPE_GC_REF:
            LOG((LF_GC, INFO3,
                 "        Argument at" FMT_ADDR "causes promotion of " FMT_OBJECT "\n",
                 DBG_ADDR(pArgPtr), DBG_ADDR(*pArgPtr) ));
#ifdef _DEBUG
            pOldLocation = *pArgPtr;
#endif
            (*fn)(pArgPtr, sc, 0 );

            // !!! Do not cast to (OBJECTREF*)
            // !!! If we are in the relocate phase, we may have updated root,
            // !!! but we have not moved the GC heap yet.
            // !!! The root then points to bad locations until GC is done.
#ifdef LOGGING
            if (pOldLocation != *pArgPtr)
                LOG((LF_GC, INFO3,
                     "        Relocating from" FMT_ADDR "to " FMT_ADDR "\n",
                     DBG_ADDR(pOldLocation), DBG_ADDR(*pArgPtr)));
#endif
            break;

        case TYPE_GC_BYREF:
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
        case_TYPE_GC_BYREF:
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE

            // value is an interior pointer
            LOG((LF_GC, INFO3,
                 "        Argument at" FMT_ADDR "causes promotion of interior pointer" FMT_ADDR "\n",
                 DBG_ADDR(pArgPtr), DBG_ADDR(*pArgPtr) ));

#ifdef _DEBUG
            pOldLocation = *pArgPtr;
#endif

            (*fnc)(fn, pArgPtr, sc, GC_CALL_INTERIOR);

            // !!! Do not cast to (OBJECTREF*)
            // !!! If we are in the relocate phase, we may have updated root,
            // !!! but we have not moved the GC heap yet.
            // !!! The root then points to bad locations until GC is done.
#ifdef LOGGING
            if (pOldLocation != *pArgPtr)
                LOG((LF_GC, INFO3,
                     "        Relocating from" FMT_ADDR "to " FMT_ADDR "\n",
                     DBG_ADDR(pOldLocation), DBG_ADDR(*pArgPtr)));
#endif
            break;

        case TYPE_GC_OTHER:
            // value is a ValueClass, generic type parameter
            // See one of the go_through_object() macros in
            // gc.cpp for the code we are emulating here.  But note that the GCDesc
            // for value classes describes the state of the instance in its boxed
            // state.  Here we are dealing with an unboxed instance, so we must adjust
            // the object size and series offsets appropriately.
            _ASSERTE(etype == ELEMENT_TYPE_VALUETYPE);
            {
                PTR_MethodTable pMT = thValueType.AsMethodTable();

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                if (ArgIterator::IsArgPassedByRef(thValueType))
                {
                    goto case_TYPE_GC_BYREF;
                }
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE

                ReportPointersFromValueTypeArg(fn, sc, pMT, pValue);
            }
            break;

        default:
            _ASSERTE(0); // can't get here.
    }
}


#ifndef DACCESS_COMPILE

void MetaSig::EnsureSigValueTypesLoaded(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    SigTypeContext typeContext(pMD);

    Module * pModule = pMD->GetModule();

    // The signature format is approximately:
    // CallingConvention   NumberOfArguments    ReturnType   Arg1  ...
    // There is also a blob length at pSig-1.
    SigPointer ptr(pMD->GetSig());

    // Skip over calling convention.
    IfFailThrowBF(ptr.GetCallingConv(NULL), BFA_BAD_SIGNATURE, pModule);

    uint32_t numArgs = 0;
    IfFailThrowBF(ptr.GetData(&numArgs), BFA_BAD_SIGNATURE, pModule);

    // Force a load of value type arguments.
    for(ULONG i=0; i <= numArgs; i++)
    {
        ptr.PeekElemTypeNormalized(pModule,&typeContext);
        // Move to next argument token.
        IfFailThrowBF(ptr.SkipExactlyOne(), BFA_BAD_SIGNATURE, pModule);
    }
}

// this walks the sig and checks to see if all  types in the sig can be loaded

// This is used by ComCallableWrapper to give good error reporting
/*static*/
void MetaSig::CheckSigTypesCanBeLoaded(MethodDesc * pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END

    SigTypeContext typeContext(pMD);

    Module * pModule = pMD->GetModule();

    // The signature format is approximately:
    // CallingConvention   NumberOfArguments    ReturnType   Arg1  ...
    // There is also a blob length at pSig-1.
    SigPointer ptr(pMD->GetSig());

    // Skip over calling convention.
    IfFailThrowBF(ptr.GetCallingConv(NULL), BFA_BAD_SIGNATURE, pModule);

    uint32_t numArgs = 0;
    IfFailThrowBF(ptr.GetData(&numArgs), BFA_BAD_SIGNATURE, pModule);

    // must do a skip so we skip any class tokens associated with the return type
    IfFailThrowBF(ptr.SkipExactlyOne(), BFA_BAD_SIGNATURE, pModule);

    // Force a load of value type arguments.
    for(uint32_t i=0; i < numArgs; i++)
    {
        unsigned type = ptr.PeekElemTypeNormalized(pModule,&typeContext);
        if (type == ELEMENT_TYPE_VALUETYPE || type == ELEMENT_TYPE_CLASS)
        {
            ptr.GetTypeHandleThrowing(pModule, &typeContext);
        }
        // Move to next argument token.
        IfFailThrowBF(ptr.SkipExactlyOne(), BFA_BAD_SIGNATURE, pModule);
    }
}

#endif // #ifndef DACCESS_COMPILE

CorElementType MetaSig::GetReturnTypeNormalized(TypeHandle * pthValueType) const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if ((m_flags & SIG_RET_TYPE_INITTED) &&
        ((pthValueType == NULL) || (m_corNormalizedRetType !=  ELEMENT_TYPE_VALUETYPE)))
    {
        return( m_corNormalizedRetType );
    }

    MetaSig * pSig = const_cast<MetaSig *>(this);
    pSig->m_corNormalizedRetType = m_pRetType.PeekElemTypeNormalized(m_pModule, &m_typeContext, pthValueType);
    pSig->m_flags |= SIG_RET_TYPE_INITTED;

    return( m_corNormalizedRetType );
}

BOOL MetaSig::IsObjectRefReturnType()
{
    WRAPPER_NO_CONTRACT;

    switch (GetReturnTypeNormalized())
        {
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_VAR:
            return( TRUE );
        default:
            break;
        }
    return( FALSE );
}

CorElementType MetaSig::GetReturnType() const
{
    WRAPPER_NO_CONTRACT;
    return m_pRetType.PeekElemTypeClosed(GetModule(), &m_typeContext);
}

BOOL MetaSig::IsReturnTypeVoid() const
{
    WRAPPER_NO_CONTRACT;
    return (GetReturnType() == ELEMENT_TYPE_VOID);
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Substitution from a token (TypeDef and TypeRef have empty instantiation, TypeSpec gets it from MetaData).
//
Substitution::Substitution(
    mdToken              parentTypeDefOrRefOrSpec,
    Module *             pModule,
    const Substitution * pNext)
{
    LIMITED_METHOD_CONTRACT;

    m_pModule = pModule;
    m_pNext = pNext;

    if (IsNilToken(parentTypeDefOrRefOrSpec) ||
        (TypeFromToken(parentTypeDefOrRefOrSpec) != mdtTypeSpec))
    {
        return;
    }

    ULONG           cbSig;
    PCCOR_SIGNATURE pSig = NULL;
    if (FAILED(pModule->GetMDImport()->GetTypeSpecFromToken(
            parentTypeDefOrRefOrSpec,
            &pSig,
            &cbSig)))
    {
        return;
    }
    SigPointer sigptr = SigPointer(pSig, cbSig);
    CorElementType type;

    if (FAILED(sigptr.GetElemType(&type)))
        return;

    // The only kind of type specs that we recognise are instantiated types
    if (type != ELEMENT_TYPE_GENERICINST)
        return;

    if (FAILED(sigptr.GetElemType(&type)))
        return;

    if (type != ELEMENT_TYPE_CLASS)
        return;

    /* mdToken genericTok = */
    if (FAILED(sigptr.GetToken(NULL)))
        return;
    /* DWORD ntypars = */
    if (FAILED(sigptr.GetData(NULL)))
        return;

    m_sigInst = sigptr;
} // Substitution::Substitution

//---------------------------------------------------------------------------------------
//
void
Substitution::CopyToArray(
    Substitution * pTarget) const
{
    LIMITED_METHOD_CONTRACT;

    const Substitution * pChain = this;
    DWORD i = 0;
    for (; pChain != NULL; pChain = pChain->GetNext())
    {
        CONSISTENCY_CHECK(CheckPointer(pChain->GetModule()));

        Substitution * pNext = (pChain->GetNext() != NULL) ? &pTarget[i + 1] : NULL;
        pTarget[i++] = Substitution(pChain->GetModule(), pChain->GetInst(), pNext);
    }
}

//---------------------------------------------------------------------------------------
//
DWORD Substitution::GetLength() const
{
    LIMITED_METHOD_CONTRACT;
    DWORD res = 0;
    for (const Substitution * pChain = this; pChain != NULL; pChain = pChain->m_pNext)
    {
        res++;
    }
    return res;
}

//---------------------------------------------------------------------------------------
//
void Substitution::DeleteChain()
{
    LIMITED_METHOD_CONTRACT;
    if (m_pNext != NULL)
    {
        ((Substitution *)m_pNext)->DeleteChain();
    }
    delete this;
}

#endif // #ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
// static
TokenPairList TokenPairList::AdjustForTypeSpec(TokenPairList *pTemplate, Module *pTypeSpecModule, PCCOR_SIGNATURE pTypeSpecSig, DWORD cbTypeSpecSig)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    TokenPairList result(pTemplate);

    if (InTypeEquivalenceForbiddenScope(&result))
    {
        // it cannot get any worse
        return result;
    }

    SigParser sig(pTypeSpecSig, cbTypeSpecSig);
    CorElementType elemType;

    IfFailThrow(sig.GetElemType(&elemType));
    if (elemType != ELEMENT_TYPE_GENERICINST)
    {
        // we don't care about anything else than generic instantiations
        return result;
    }

    IfFailThrow(sig.GetElemType(&elemType));

    if (elemType == ELEMENT_TYPE_CLASS)
    {
        mdToken tkType;
        IfFailThrow(sig.GetToken(&tkType));

        Module *pModule;
        if (!ClassLoader::ResolveTokenToTypeDefThrowing(pTypeSpecModule,
                                                        tkType,
                                                        &pModule,
                                                        &tkType))
        {
            // we couldn't prove otherwise so assume that this is not an interface
            result.m_bInTypeEquivalenceForbiddenScope = TRUE;
        }
        else
        {
            DWORD dwAttrType;
            IfFailThrow(pModule->GetMDImport()->GetTypeDefProps(tkType, &dwAttrType, NULL));

            result.m_bInTypeEquivalenceForbiddenScope = !IsTdInterface(dwAttrType);
        }
    }
    else
    {
        _ASSERTE(elemType == ELEMENT_TYPE_VALUETYPE);
        result.m_bInTypeEquivalenceForbiddenScope = TRUE;
    }

    return result;
}

// static
TokenPairList TokenPairList::AdjustForTypeEquivalenceForbiddenScope(TokenPairList *pTemplate)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    TokenPairList result(pTemplate);
    result.m_bInTypeEquivalenceForbiddenScope = TRUE;
    return result;
}

// TRUE if the two TypeDefs have the same layout and field marshal information.
BOOL CompareTypeLayout(mdToken tk1, mdToken tk2, Module *pModule1, Module *pModule2)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(TypeFromToken(tk1) == mdtTypeDef);
        PRECONDITION(TypeFromToken(tk2) == mdtTypeDef);
    }
    CONTRACTL_END

    DWORD dwAttr1, dwAttr2;
    IMDInternalImport *pInternalImport1 = pModule1->GetMDImport();
    IMDInternalImport *pInternalImport2 = pModule2->GetMDImport();

    IfFailThrow(pInternalImport1->GetTypeDefProps(tk1, &dwAttr1, NULL));
    IfFailThrow(pInternalImport2->GetTypeDefProps(tk2, &dwAttr2, NULL));

    // we need both to have sequential or explicit layout
    BOOL fExplicitLayout = FALSE;
    if (IsTdSequentialLayout(dwAttr1))
    {
        if (!IsTdSequentialLayout(dwAttr2))
            return FALSE;
    }
    else if (IsTdExplicitLayout(dwAttr1))
    {
        if (!IsTdExplicitLayout(dwAttr2))
            return FALSE;

        fExplicitLayout = TRUE;
    }
    else
    {
        return FALSE;
    }

    // they must have the same charset
    if ((dwAttr1 & tdStringFormatMask) != (dwAttr2 & tdStringFormatMask))
        return FALSE;

    // they must have the same packing
    DWORD dwPackSize1, dwPackSize2;
    HRESULT hr1 = pInternalImport1->GetClassPackSize(tk1, &dwPackSize1);
    HRESULT hr2 = pInternalImport2->GetClassPackSize(tk2, &dwPackSize2);

    if (hr1 == CLDB_E_RECORD_NOTFOUND)
        dwPackSize1 = 0;
    else
        IfFailThrow(hr1);

    if (hr2 == CLDB_E_RECORD_NOTFOUND)
        dwPackSize2 = 0;
    else
        IfFailThrow(hr2);

    if (dwPackSize1 != dwPackSize2)
        return FALSE;

    // they must have the same explicit size
    DWORD dwTotalSize1, dwTotalSize2;
    hr1 = pInternalImport1->GetClassTotalSize(tk1, &dwTotalSize1);
    hr2 = pInternalImport2->GetClassTotalSize(tk2, &dwTotalSize2);

    if (hr1 == CLDB_E_RECORD_NOTFOUND)
        dwTotalSize1 = 0;
    else
        IfFailThrow(hr1);

    if (hr2 == CLDB_E_RECORD_NOTFOUND)
        dwTotalSize2 = 0;
    else
        IfFailThrow(hr2);

    if (dwTotalSize1 != dwTotalSize2)
        return FALSE;

    // same offsets, same field marshal
    HENUMInternalHolder hFieldEnum1(pInternalImport1);
    HENUMInternalHolder hFieldEnum2(pInternalImport2);

    hFieldEnum1.EnumInit(mdtFieldDef, tk1);
    hFieldEnum2.EnumInit(mdtFieldDef, tk2);

    mdToken tkField1, tkField2;

    while (hFieldEnum1.EnumNext(&tkField1))
    {
        if (!hFieldEnum2.EnumNext(&tkField2))
            return FALSE;

        // check for same offsets
        if (fExplicitLayout)
        {
            ULONG uOffset1, uOffset2;
            IfFailThrow(pInternalImport1->GetFieldOffset(tkField1, &uOffset1));
            IfFailThrow(pInternalImport2->GetFieldOffset(tkField2, &uOffset2));

            if (uOffset1 != uOffset2)
                return FALSE;
        }

        // check for same field marshal
        DWORD dwAttrField1, dwAttrField2;
        IfFailThrow(pInternalImport1->GetFieldDefProps(tkField1, &dwAttrField1));
        IfFailThrow(pInternalImport2->GetFieldDefProps(tkField2, &dwAttrField2));

        if (IsFdHasFieldMarshal(dwAttrField1) != IsFdHasFieldMarshal(dwAttrField2))
            return FALSE;

        if (IsFdHasFieldMarshal(dwAttrField1))
        {
            // both fields have field marshal info - make sure it's same
            PCCOR_SIGNATURE pNativeSig1, pNativeSig2;
            ULONG cbNativeSig1, cbNativeSig2;

            IfFailThrow(pInternalImport1->GetFieldMarshal(tkField1, &pNativeSig1, &cbNativeSig1));
            IfFailThrow(pInternalImport2->GetFieldMarshal(tkField2, &pNativeSig2, &cbNativeSig2));

            // just check if the blobs are identical
            if (cbNativeSig1 != cbNativeSig2 || memcmp(pNativeSig1, pNativeSig2, cbNativeSig1) != 0)
                return FALSE;
        }
    }

    return TRUE;
}
