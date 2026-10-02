// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "customattribute.h"
#include "corerror.h"
#include "typeparse.h"
#include "typestring.h"

static TypeHandle GetTypeForEnum(LPCUTF8 szEnumName, COUNT_T cbEnumName, Assembly* pAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(szEnumName));
        PRECONDITION(cbEnumName);
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    StackSString sszEnumName(SString::Utf8, szEnumName, cbEnumName);
    return TypeName::GetTypeReferencedByCustomAttribute(sszEnumName.GetUnicode(), pAssembly);
}

static HRESULT ParseCaType(
    CustomAttributeParser &ca,
    CaType* pCaType,
    Assembly* pAssembly,
    StackSString* ss = NULL)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;

    IfFailGo(::ParseEncodedType(ca, pCaType));

    if (pCaType->tag == SERIALIZATION_TYPE_ENUM
        || (pCaType->tag == SERIALIZATION_TYPE_SZARRAY && pCaType->arrayType == SERIALIZATION_TYPE_ENUM ))
    {
        TypeHandle th = GetTypeForEnum(pCaType->szEnumName, pCaType->cEnumName, pAssembly);

        if (!th.IsNull() && th.IsEnum())
        {
            pCaType->enumType = (CorSerializationType)th.GetInternalCorElementType();

            // The assembly qualified name of th might not equal pCaType->szEnumName.
            // e.g. th could be "MyEnum, MyAssembly, Version=4.0.0.0" while
            // pCaType->szEnumName is "MyEnum, MyAssembly, Version=3.0.0.0"
            if (ss)
            {
                DWORD format = TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly;
                TypeString::AppendType(*ss, th, format);
            }
        }
        else
        {
            IfFailGo(PostError(META_E_CA_UNEXPECTED_TYPE, pCaType->cEnumName, pCaType->szEnumName));
        }
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Helper to parse the values for the ctor argument list and the named argument list.
//

static HRESULT ParseCaValue(
    CustomAttributeParser &ca,
    CaValue* pCaArg,
    CaType* pCaParam,
    CaValueArrayFactory* pCaValueArrayFactory,
    Assembly* pAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaArg));
        PRECONDITION(CheckPointer(pCaParam));
        PRECONDITION(CheckPointer(pCaValueArrayFactory));
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CorSerializationType underlyingType;
    CaType elementType;

    if (pCaParam->tag == SERIALIZATION_TYPE_TAGGED_OBJECT)
        IfFailGo(ParseCaType(ca, &pCaArg->type, pAssembly));
    else
        pCaArg->type = *pCaParam;

    underlyingType = pCaArg->type.tag == SERIALIZATION_TYPE_ENUM ? pCaArg->type.enumType : pCaArg->type.tag;

    // Grab the value.
    switch (underlyingType)
    {
    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
        IfFailGo(ca.GetU1(&pCaArg->u1));
        break;

    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
        IfFailGo(ca.GetU2(&pCaArg->u2));
        break;

    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
        IfFailGo(ca.GetU4(&pCaArg->u4));
        break;

    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
        IfFailGo(ca.GetU8(&pCaArg->u8));
        break;

    case SERIALIZATION_TYPE_R4:
        IfFailGo(ca.GetR4(&pCaArg->r4));
        break;

    case SERIALIZATION_TYPE_R8:
        IfFailGo(ca.GetR8(&pCaArg->r8));
        break;

    case SERIALIZATION_TYPE_STRING:
    case SERIALIZATION_TYPE_TYPE:
        IfFailGo(ca.GetString(&pCaArg->str.pStr, &pCaArg->str.cbStr));
        break;

    case SERIALIZATION_TYPE_SZARRAY:
        UINT32 len;
        IfFailGo(ca.GetU4(&len));
        pCaArg->arr.length = len;
        pCaArg->arr.pSArray = NULL;
        if (pCaArg->arr.length == (ULONG)-1)
            break;

        IfNullGo(pCaArg->arr.pSArray = pCaValueArrayFactory->Create());
        elementType.Init(pCaArg->type.arrayType, SERIALIZATION_TYPE_UNDEFINED,
            pCaArg->type.enumType, pCaArg->type.szEnumName, pCaArg->type.cEnumName);
        for (ULONG i = 0; i < pCaArg->arr.length; i++)
            IfFailGo(ParseCaValue(ca, &*pCaArg->arr.pSArray->Append(), &elementType, pCaValueArrayFactory, pAssembly));

        break;

    default:
        // The format of the custom attribute record is invalid.
        hr = E_FAIL;
        break;
    } // End switch

ErrExit:
    return hr;
}

static HRESULT ParseCaCtorArgs(
    CustomAttributeParser &ca,
    CaArg* pArgs,
    ULONG cArgs,
    CaValueArrayFactory* pCaValueArrayFactory,
    Assembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;

    HRESULT     hr = S_OK;              // A result.
    ULONG       ix;                     // Loop control.

    // If there is a blob, check the prolog.
    if (FAILED(ca.ValidateProlog()))
    {
        IfFailGo(PostError(META_E_CA_INVALID_BLOB));
    }

    // For each expected arg...
    for (ix=0; ix<cArgs; ++ix)
    {
        CaArg* pArg = &pArgs[ix];
        IfFailGo(ParseCaValue(ca, &pArg->val, &pArg->type, pCaValueArrayFactory, pAssembly));
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Because ParseKnowCaNamedArgs MD cannot have VM dependency, we have our own implementation here:
//   1. It needs to load the assemblies that contain the enum types for the named arguments,
//   2. It Compares the enum type name with that of the loaded enum type, not the one in the CA record.
//

static HRESULT ParseCaNamedArgs(
    CustomAttributeParser &ca,
    CaNamedArg *pNamedParams,
    ULONG cNamedParams,
    CaValueArrayFactory* pCaValueArrayFactory,
    Assembly* pAssembly)
{
    CONTRACTL {
        PRECONDITION(CheckPointer(pCaValueArrayFactory));
        PRECONDITION(CheckPointer(pAssembly));
        THROWS;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    ULONG ixParam;
    INT32 ixArg;
    INT16 cActualArgs;
    CaNamedArgCtor namedArg;
    CaNamedArg* pNamedParam;

    // Get actual count of named arguments.
    if (FAILED(ca.GetI2(&cActualArgs)))
        cActualArgs = 0; // Everett behavior

    for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        pNamedParams[ixParam].val.type.tag = SERIALIZATION_TYPE_UNDEFINED;

    // For each named argument...
    for (ixArg = 0; ixArg < cActualArgs; ixArg++)
    {
        // Field or property?
        IfFailGo(ca.GetTag(&namedArg.propertyOrField));
        if (namedArg.propertyOrField != SERIALIZATION_TYPE_FIELD && namedArg.propertyOrField != SERIALIZATION_TYPE_PROPERTY)
            IfFailGo(PostError(META_E_CA_INVALID_ARGTYPE));

        // Get argument type information
        CaType* pNamedArgType = &namedArg.type;
        StackSString ss;
        IfFailGo(ParseCaType(ca, pNamedArgType, pAssembly, &ss));

        LPCSTR szLoadedEnumName = NULL;

        if (pNamedArgType->tag == SERIALIZATION_TYPE_ENUM ||
            (pNamedArgType->tag == SERIALIZATION_TYPE_SZARRAY && pNamedArgType->arrayType == SERIALIZATION_TYPE_ENUM ))
        {
            szLoadedEnumName = ss.GetUTF8();
        }

        // Get name of Arg.
        if (FAILED(ca.GetNonEmptyString(&namedArg.szName, &namedArg.cName)))
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));

        // Match arg by name and type
        for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        {
            pNamedParam = &pNamedParams[ixParam];

            // Match type
            if (pNamedParam->type.tag != SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                if (namedArg.type.tag != pNamedParam->type.tag)
                    continue;

                // Match array type
                if (namedArg.type.tag == SERIALIZATION_TYPE_SZARRAY &&
                    pNamedParam->type.arrayType != SERIALIZATION_TYPE_TAGGED_OBJECT &&
                    namedArg.type.arrayType != pNamedParam->type.arrayType)
                    continue;
            }

            // Match name (and its length to avoid substring matching)
            if ((pNamedParam->cName != namedArg.cName) ||
                (strncmp(pNamedParam->szName, namedArg.szName, namedArg.cName) != 0))
            {
                continue;
            }

            // If enum, match enum name.
            if (pNamedParam->type.tag == SERIALIZATION_TYPE_ENUM ||
                (pNamedParam->type.tag == SERIALIZATION_TYPE_SZARRAY && pNamedParam->type.arrayType == SERIALIZATION_TYPE_ENUM ))
            {
                // pNamedParam->type.szEnumName: module->CA record->ctor token->loaded type->field/property->field/property type->field/property type name
                // namedArg.type.szEnumName:     module->CA record->named arg->enum type name
                // szLoadedEnumName:             module->CA record->named arg->enum type name->loaded enum type->loaded enum type name

                // Comparing pNamedParam->type.szEnumName against namedArg.type.szEnumName could fail if we loaded a different version
                // of the enum type than the one specified in the CA record. So we are comparing it against szLoadedEnumName instead.
                if (strncmp(pNamedParam->type.szEnumName, szLoadedEnumName, pNamedParam->type.cEnumName) != 0)
                    continue;

                if (namedArg.type.enumType != pNamedParam->type.enumType)
                {
                    IfFailGo(PostError(META_E_CA_UNEXPECTED_TYPE, pNamedParam->type.cEnumName, pNamedParam->type.szEnumName));
                }

                // TODO: For now assume the property\field array size is correct - later we should verify this
            }

            // Found a match.
            break;
        }

        // Better have found an argument.
        if (ixParam == cNamedParams)
        {
            IfFailGo(PostError(META_E_CA_UNKNOWN_ARGUMENT, namedArg.cName, namedArg.szName));
        }

        // Argument had better not have been seen already.
        if (pNamedParams[ixParam].val.type.tag != SERIALIZATION_TYPE_UNDEFINED)
        {
            IfFailGo(PostError(META_E_CA_REPEATED_ARG, namedArg.cName, namedArg.szName));
        }

        IfFailGo(ParseCaValue(ca, &pNamedParams[ixParam].val, &namedArg.type, pCaValueArrayFactory, pAssembly));
    }

ErrExit:
    return hr;
}

HRESULT CustomAttribute::ParseArgumentValues(
    void* pCa,
    INT32 cCa,
    CaValueArrayFactory* pCaValueArrayFactory,
    CaArg* pCaArgs,
    COUNT_T cArgs,
    CaNamedArg* pCaNamedArgs,
    COUNT_T cNamedArgs,
    Assembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    CustomAttributeParser cap(pCa, cCa);

    IfFailGo(ParseCaCtorArgs(cap, pCaArgs, cArgs, pCaValueArrayFactory, pAssembly));
    IfFailGo(ParseCaNamedArgs(cap, pCaNamedArgs, cNamedArgs, pCaValueArrayFactory, pAssembly));

ErrExit:
    return hr;
}

extern "C" BOOL QCALLTYPE CustomAttribute_ParseAttributeUsageAttribute(
    PVOID pData,
    ULONG cData,
    ULONG* pTargets,
    BOOL* pAllowMultiple,
    BOOL* pInherited)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    CustomAttributeParser ca(pData, cData);
    CaArg args[1];
    args[0].InitEnum(SERIALIZATION_TYPE_I4, 0);
    if (FAILED(::ParseKnownCaArgs(ca, args, ARRAY_SIZE(args))))
        return FALSE;
    *pTargets = args[0].val.u4;

    // Define index values.
    const int allowMultiple = 0;
    const int inherited = 1;

    CaNamedArg namedArgs[2];
    CaType namedArgTypes[2];
    namedArgTypes[allowMultiple].Init(SERIALIZATION_TYPE_BOOLEAN);
    namedArgTypes[inherited].Init(SERIALIZATION_TYPE_BOOLEAN);
    namedArgs[allowMultiple].Init("AllowMultiple", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[allowMultiple], FALSE);
    namedArgs[inherited].Init("Inherited", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[inherited], TRUE);
    if (FAILED(::ParseKnownCaNamedArgs(ca, namedArgs, ARRAY_SIZE(namedArgs))))
        return FALSE;

    *pAllowMultiple = namedArgs[allowMultiple].val.boolean ? TRUE : FALSE;
    *pInherited = namedArgs[inherited].val.boolean ? TRUE : FALSE;
    return TRUE;
}
