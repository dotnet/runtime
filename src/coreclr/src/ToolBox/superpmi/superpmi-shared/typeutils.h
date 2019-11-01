//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// TypeUtils.h - Utility code for working with managed types
//----------------------------------------------------------
#ifndef _TypeUtils
#define _TypeUtils

#include "methodcontext.h"

class TypeUtils
{
public:
    static const char* GetCorInfoTypeName(CorInfoType type);
    static bool IsFloatingPoint(CorInfoType type);
    static bool IsPointer(CorInfoType type);
    static bool IsValueClass(CorInfoType type);
    static bool ValueClassRequiresByref(MethodContext* mc, CORINFO_CLASS_HANDLE clsHnd);
    static size_t SizeOfCorInfoType(CorInfoType type);
};

#endif