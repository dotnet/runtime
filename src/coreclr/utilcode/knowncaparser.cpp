// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "../md/compiler/custattr.h"
#include "contract.h"
#include "corerror.h"
#include "posterror.h"
#include "utilcode.h"
#include <cstring>

HRESULT ParseEncodedType(CustomAttributeParser &ca, CaType* pCaType)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaType));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CorSerializationType* pType = &pCaType->tag;
    IfFailGo(ca.GetTag(pType));

    if (*pType == SERIALIZATION_TYPE_SZARRAY)
    {
        IfFailGo(ca.GetTag(&pCaType->arrayType));
        pType = &pCaType->arrayType;
    }
    if (*pType == SERIALIZATION_TYPE_ENUM)
    {
        pCaType->enumType = SERIALIZATION_TYPE_UNDEFINED;
        IfFailGo(ca.GetNonNullString(&pCaType->szEnumName, &pCaType->cEnumName));
    }

ErrExit:
    return hr;
}

HRESULT ParseKnownCaValue(CustomAttributeParser &ca, CaValue* pCaArg, CaType* pCaParam)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaArg));
        PRECONDITION(CheckPointer(pCaParam));
        PRECONDITION(pCaParam->tag != SERIALIZATION_TYPE_TAGGED_OBJECT && pCaParam->tag != SERIALIZATION_TYPE_SZARRAY);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CorSerializationType underlyingType;
    pCaArg->type = *pCaParam;
    underlyingType = pCaArg->type.tag == SERIALIZATION_TYPE_ENUM ? pCaArg->type.enumType : pCaArg->type.tag;

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
    default:
        _ASSERTE(!"Unexpected internal error");
        hr = E_FAIL;
        break;
    }

ErrExit:
    return hr;
}

HRESULT ParseKnownCaNamedArgs(CustomAttributeParser &ca, CaNamedArg* pNamedParams, ULONG cNamedParams)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    ULONG ixParam;
    INT32 ixArg;
    INT16 cActualArgs;
    CaNamedArgCtor namedArg;
    CaNamedArg* pNamedParam;

    if (FAILED(ca.GetI2(&cActualArgs)))
        cActualArgs = 0;

    for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        pNamedParams[ixParam].val.type.tag = SERIALIZATION_TYPE_UNDEFINED;

    for (ixArg = 0; ixArg < cActualArgs; ixArg++)
    {
        IfFailGo(ca.GetTag(&namedArg.propertyOrField));
        if (namedArg.propertyOrField != SERIALIZATION_TYPE_FIELD && namedArg.propertyOrField != SERIALIZATION_TYPE_PROPERTY)
            IfFailGo(PostError(META_E_CA_INVALID_ARGTYPE));

        IfFailGo(ParseEncodedType(ca, &namedArg.type));
        if (FAILED(ca.GetNonEmptyString(&namedArg.szName, &namedArg.cName)))
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));

        for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        {
            pNamedParam = &pNamedParams[ixParam];
            if (pNamedParam->type.tag != SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                if (namedArg.type.tag != pNamedParam->type.tag)
                    continue;

                if (namedArg.type.tag == SERIALIZATION_TYPE_SZARRAY &&
                    pNamedParam->type.arrayType != SERIALIZATION_TYPE_TAGGED_OBJECT &&
                    namedArg.type.arrayType != pNamedParam->type.arrayType)
                    continue;
            }

            if ((pNamedParam->cName != namedArg.cName) ||
                (strncmp(pNamedParam->szName, namedArg.szName, namedArg.cName) != 0))
                continue;

            if (pNamedParam->type.tag == SERIALIZATION_TYPE_ENUM ||
                (pNamedParam->type.tag == SERIALIZATION_TYPE_SZARRAY && pNamedParam->type.arrayType == SERIALIZATION_TYPE_ENUM))
            {
                if (pNamedParam->type.cEnumName > namedArg.type.cEnumName)
                    continue;

                if (strncmp(pNamedParam->type.szEnumName, namedArg.type.szEnumName, pNamedParam->type.cEnumName) != 0 ||
                    (pNamedParam->type.cEnumName < namedArg.type.cEnumName &&
                     namedArg.type.szEnumName[pNamedParam->type.cEnumName] != ','))
                    continue;

                namedArg.type.enumType = pNamedParam->type.enumType;
            }

            break;
        }

        if (ixParam == cNamedParams)
            IfFailGo(PostError(META_E_CA_UNKNOWN_ARGUMENT, namedArg.cName, namedArg.szName));

        if (pNamedParams[ixParam].val.type.tag != SERIALIZATION_TYPE_UNDEFINED)
            IfFailGo(PostError(META_E_CA_REPEATED_ARG, namedArg.cName, namedArg.szName));

        IfFailGo(ParseKnownCaValue(ca, &pNamedParams[ixParam].val, &namedArg.type));
    }

ErrExit:
    return hr;
}

HRESULT ParseKnownCaArgs(CustomAttributeParser &ca, CaArg* pArgs, ULONG cArgs)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    if (FAILED(ca.ValidateProlog()))
        IfFailGo(PostError(META_E_CA_INVALID_BLOB));

    for (ULONG ix = 0; ix < cArgs; ++ix)
        IfFailGo(ParseKnownCaValue(ca, &pArgs[ix].val, &pArgs[ix].type));

ErrExit:
    return hr;
}
