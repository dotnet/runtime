// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// TypeUtils.cpp - Utility code for working with managed types
//----------------------------------------------------------

#include "standardpch.h"
#include "typeutils.h"
#include "errorhandling.h"
#include "spmiutil.h"

// Returns a string representation of the given CorInfoType. The naming scheme is based on JITtype2varType
// in src/jit/ee_il_dll.hpp.
const char* TypeUtils::GetCorInfoTypeName(CorInfoType type)
{
    switch (type)
    {
        case CORINFO_TYPE_VOID:
            return "void";

        case CORINFO_TYPE_BOOL:
            return "bool";

        case CORINFO_TYPE_CHAR:
            return "char";

        case CORINFO_TYPE_BYTE:
            return "byte";

        case CORINFO_TYPE_UBYTE:
            return "ubyte";

        case CORINFO_TYPE_SHORT:
            return "short";

        case CORINFO_TYPE_USHORT:
            return "ushort";

        case CORINFO_TYPE_INT:
            return "int";

        case CORINFO_TYPE_UINT:
            return "uint";

        case CORINFO_TYPE_LONG:
            return "long";

        case CORINFO_TYPE_ULONG:
            return "ulong";

        case CORINFO_TYPE_FLOAT:
            return "float";

        case CORINFO_TYPE_DOUBLE:
            return "double";

        case CORINFO_TYPE_BYREF:
            return "byref";

        case CORINFO_TYPE_VALUECLASS:
        case CORINFO_TYPE_REFANY:
            return "struct";

        case CORINFO_TYPE_STRING:
        case CORINFO_TYPE_CLASS:
        case CORINFO_TYPE_VAR:
            return "ref";

        case CORINFO_TYPE_NATIVEINT:
        case CORINFO_TYPE_NATIVEUINT:
            // Emulates the JIT's concept of TYP_I_IMPL
            return IsSpmiTarget64Bit() ? "long" : "int";

        case CORINFO_TYPE_PTR:
            // The JIT just treats this as a TYP_I_IMPL because this isn't a GC root,
            // but we don't care about GC-ness: we care about pointer-sized.
            return "ptr";

        default:
            LogException(EXCEPTIONCODE_TYPEUTILS, "Unknown type passed into GetCorInfoTypeName (0x%x)", type);
            return "UNKNOWN";
    }
}

bool TypeUtils::IsFloatingPoint(CorInfoType type)
{
    return (type == CORINFO_TYPE_FLOAT || type == CORINFO_TYPE_DOUBLE);
}

bool TypeUtils::IsPointer(CorInfoType type)
{
    return (type == CORINFO_TYPE_STRING || type == CORINFO_TYPE_PTR || type == CORINFO_TYPE_BYREF ||
            type == CORINFO_TYPE_CLASS);
}

bool TypeUtils::IsValueClass(CorInfoType type)
{
    return (type == CORINFO_TYPE_VALUECLASS || type == CORINFO_TYPE_REFANY);
}

// Determines if a value class, represented by the given class handle, is required to be passed
// by reference (i.e. it cannot be stuffed as-is into a register or stack slot).
bool TypeUtils::ValueClassRequiresByref(MethodContext* mc, CORINFO_CLASS_HANDLE clsHnd)
{
    if (GetSpmiTargetArchitecture() == SPMI_TARGET_ARCHITECTURE_AMD64)
    {
        size_t size = mc->repGetClassSize(clsHnd);
        return ((size > SpmiTargetPointerSize()) || ((size & (size - 1)) != 0));
    }
    else
    {
        LogException(EXCEPTIONCODE_TYPEUTILS, "unsupported architecture", "");
        return false;
    }
}

// Returns the size of the given CorInfoType. If there is no applicable size (e.g. CORINFO_TYPE_VOID,
// CORINFO_TYPE_UNDEF), this throws an exception and returns -1. Taken largely from the MSDN documentation
// for managed sizeof.
size_t TypeUtils::SizeOfCorInfoType(CorInfoType type)
{
    switch (type)
    {
        case CORINFO_TYPE_BYTE:
        case CORINFO_TYPE_UBYTE:
        case CORINFO_TYPE_BOOL:
            return sizeof(BYTE);

        case CORINFO_TYPE_CHAR: // 2 bytes for Unicode
        case CORINFO_TYPE_SHORT:
        case CORINFO_TYPE_USHORT:
            return sizeof(WORD);

        case CORINFO_TYPE_INT:
        case CORINFO_TYPE_UINT:
        case CORINFO_TYPE_FLOAT:
            return sizeof(DWORD);

        case CORINFO_TYPE_LONG:
        case CORINFO_TYPE_ULONG:
        case CORINFO_TYPE_DOUBLE:
            return sizeof(DWORDLONG);

        case CORINFO_TYPE_NATIVEINT:
        case CORINFO_TYPE_NATIVEUINT:
        case CORINFO_TYPE_STRING:
        case CORINFO_TYPE_PTR:
        case CORINFO_TYPE_BYREF:
        case CORINFO_TYPE_CLASS:
            return SpmiTargetPointerSize();

        // This should be obtained via repGetClassSize
        case CORINFO_TYPE_VALUECLASS:
        case CORINFO_TYPE_REFANY:
            LogException(EXCEPTIONCODE_TYPEUTILS,
                         "SizeOfCorInfoType does not support value types; use repGetClassSize instead (type: 0x%x)",
                         type);
            return 0;

        case CORINFO_TYPE_UNDEF:
        case CORINFO_TYPE_VOID:
        default:
            LogException(EXCEPTIONCODE_TYPEUTILS, "Unsupported type (0x%x) passed into SizeOfCorInfoType", type);
            return 0;
    }
}
