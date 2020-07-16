// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __CustAttr__h__
#define __CustAttr__h__

//#include "stdafx.h"
#include "corhdr.h"
#include "cahlprinternal.h"
#include "sarray.h"
#include "factory.h"

//*****************************************************************************
// Argument parsing.  The custom attributes may have ctor arguments, and may
//  have named arguments.  The arguments are defined by the following tables.
//
//  These tables also include a member to contain the value of the argument,
//  which is used at runtime.  When parsing a given custom attribute, a copy
//  of the argument descriptors is filled in with the values for the instance
//  of the custom attribute.
//
//  For each ctor arg, there is a CaArg struct, with the type.  At runtime,
//   a value is filled in for each ctor argument.
//
//  For each named arg, there is a CaNamedArg struct, with the name of the
//   argument, the expected type of the argument, if the type is an enum,
//   the name of the enum.  Also, at runtime, a value is filled in for
//   each named argument found.
//
//  Note that arrays and variants are not supported.
//
//  At runtime, after the args have been parsed, the tag field of CaValue
//   can be used to determine if a particular arg was given.
//*****************************************************************************
struct CaArg
{
    void InitEnum(CorSerializationType _enumType, INT64 _val = 0)
    {
        CaTypeCtor caType(SERIALIZATION_TYPE_ENUM, SERIALIZATION_TYPE_UNDEFINED, _enumType, NULL, 0);
        Init(caType, _val);
    }
    void Init(CorSerializationType _type, INT64 _val = 0)
    {
        _ASSERTE(_type != SERIALIZATION_TYPE_ENUM);
        _ASSERTE(_type != SERIALIZATION_TYPE_SZARRAY);
        CaTypeCtor caType(_type);
        Init(caType, _val);
    }
    void Init(CaType _type, INT64 _val = 0)
    {
        type = _type;
        memset(&val, 0, sizeof(CaValue));
        val.i8 = _val;
    }

    CaType type;
    CaValue val;
};

struct CaNamedArg
{
    void InitI4FieldEnum(LPCSTR _szName, LPCSTR _szEnumName, INT64 _val = 0)
    {
        CaTypeCtor caType(SERIALIZATION_TYPE_ENUM, SERIALIZATION_TYPE_UNDEFINED, SERIALIZATION_TYPE_I4, _szEnumName, (ULONG)strlen(_szEnumName));
        Init(_szName, SERIALIZATION_TYPE_FIELD, caType, _val);
    }

    void InitBoolField(LPCSTR _szName, INT64 _val = 0)
    {
        CaTypeCtor caType(SERIALIZATION_TYPE_BOOLEAN);
        Init(_szName, SERIALIZATION_TYPE_FIELD, caType, _val);
    }

    void Init(LPCSTR _szName, CorSerializationType _propertyOrField, CaType _type, INT64 _val = 0)
    {
        szName = _szName;
        cName = _szName ? (ULONG)strlen(_szName) : 0;
        propertyOrField = _propertyOrField;
        type = _type;

        memset(&val, 0, sizeof(CaValue));
        val.i8 = _val;
    }

    LPCSTR szName;
    ULONG cName;
    CorSerializationType propertyOrField;
    CaType type;
    CaValue val;
};

struct CaNamedArgCtor : public CaNamedArg
{
    CaNamedArgCtor()
    {
        memset(this, 0, sizeof(CaNamedArg));
    }
};

HRESULT ParseEncodedType(
    CustomAttributeParser &ca,
    CaType* pCaType);

HRESULT ParseKnownCaArgs(
    CustomAttributeParser &ca,          // The Custom Attribute blob.
    CaArg       *pArgs,                 // Array of argument descriptors.
    ULONG       cArgs);                 // Count of argument descriptors.

HRESULT ParseKnownCaNamedArgs(
    CustomAttributeParser &ca,          // The Custom Attribute blob.
    CaNamedArg  *pNamedArgs,            // Array of argument descriptors.
    ULONG       cNamedArgs);            // Count of argument descriptors.

#endif
