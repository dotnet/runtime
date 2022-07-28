// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
