// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __CAHLPR_H__
#define __CAHLPR_H__

#include "sarray.h"
#include "caparser.h"

//*****************************************************************************
// This class assists in the parsing of CustomAttribute blobs.
//*****************************************************************************
struct CaType
{
    void Init(CorSerializationType _type)
    {
        _ASSERTE(_type != SERIALIZATION_TYPE_SZARRAY && _type != SERIALIZATION_TYPE_ENUM);
        tag = _type;
        arrayType = SERIALIZATION_TYPE_UNDEFINED;
        enumType = SERIALIZATION_TYPE_UNDEFINED;
        szEnumName = NULL;
        cEnumName = 0;
    }

    void Init(CorSerializationType _type, CorSerializationType _arrayType, CorSerializationType _enumType, LPCSTR _szEnumName, ULONG _cEnumName)
    {
        tag = _type;
        arrayType = _arrayType;
        enumType = _enumType;
        szEnumName = _szEnumName;
        cEnumName = _cEnumName;
    }

    CorSerializationType tag;
    CorSerializationType arrayType;
    CorSerializationType enumType;
    LPCSTR szEnumName;
    ULONG cEnumName;
};

struct CaTypeCtor : public CaType
{
    CaTypeCtor(CorSerializationType _type)
    {
        Init(_type);
    }

    CaTypeCtor(CorSerializationType _type, CorSerializationType _arrayType, CorSerializationType _enumType, LPCSTR _szEnumName, ULONG _cEnumName)
    {
        Init(_type, _arrayType, _enumType, _szEnumName, _cEnumName);
    }
};

typedef struct CaValue
{
    union
    {
        unsigned __int8     boolean;
        signed __int8       i1;
        unsigned __int8     u1;
        signed __int16      i2;
        unsigned __int16    u2;
        signed __int32      i4;
        unsigned __int32    u4;
        signed __int64      i8;
        unsigned __int64    u8;
        float               r4;
        double              r8;

        struct
        {
            CorSerializationType tag;
            SArray<CaValue>* pSArray;
            ULONG length;
            inline CaValue &operator[](int index) { return (*pSArray)[index]; }
        } arr;

        struct
        {
            LPCUTF8 pStr;
            ULONG cbStr;
        } str;
    };

    CaType type;

} CaValue;



#endif // __CAHLPR_H__
