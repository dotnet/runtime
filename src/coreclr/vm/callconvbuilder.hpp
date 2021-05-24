// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CALLCONVBUILDER_H__
#define __CALLCONVBUILDER_H__

#include "corinfo.h"

// Attempts to parse the provided calling convention names to construct a
// calling convention with known modifiers.
//
// The Add* functions return false if the type is known but cannot be applied.
// Otherwise, they return true.
class CallConvBuilder final
{
public:
    enum CallConvModifiers
    {
        CALL_CONV_MOD_NONE = 0,
        CALL_CONV_MOD_SUPPRESSGCTRANSITION = 0x1,
        CALL_CONV_MOD_MEMBERFUNCTION = 0x2
    };

    struct State
    {
        CorInfoCallConvExtension CallConvBase;
        CallConvModifiers CallConvModifiers;
    };

private:
    State _state;

public:
    // The initial "unset" base calling convention.
    static const CorInfoCallConvExtension UnsetValue;

    CallConvBuilder();

    // Add a fully qualified type name to the calling convention computation.
    bool AddFullyQualifiedTypeName(
        _In_ size_t typeLength,
        _In_z_ LPCSTR typeName);

    // Add a simple type name to the calling convention computation.
    bool AddTypeName(
        _In_ size_t typeLength,
        _In_z_ LPCSTR typeName);

    // Get the currently computed calling convention value.
    CorInfoCallConvExtension GetCurrentCallConv() const;

    // Check if the modifier is set on the computed calling convention.
    bool IsCurrentCallConvModSet(_In_ CallConvModifiers mod) const;
};

namespace CallConv
{
    static CorInfoCallConvExtension GetDefaultUnmanagedCallingConvention()
    {
#ifdef TARGET_UNIX
        return CorInfoCallConvExtension::C;
#else // TARGET_UNIX
        return CorInfoCallConvExtension::Stdcall;
#endif // !TARGET_UNIX
    }

    //-------------------------------------------------------------------------
    // Gets the unmanaged calling convention by reading any modopts.
    //
    // Returns:
    //   S_OK - No errors
    //   COR_E_BADIMAGEFORMAT - Signature had an invalid format
    //   COR_E_INVALIDPROGRAM - Program is considered invalid (more
    //                          than one calling convention specified)
    //-------------------------------------------------------------------------
    HRESULT TryGetUnmanagedCallingConventionFromModOpt(
        _In_ CORINFO_MODULE_HANDLE pModule,
        _In_ PCCOR_SIGNATURE pSig,
        _In_ ULONG cSig,
        _Inout_ CallConvBuilder *builder,
        _Out_ UINT *errorResID);

    //-------------------------------------------------------------------------
    // Gets the calling convention from the UnmanagedCallConv attribute
    //
    // Returns:
    //   S_OK - No errors
    //   S_FALSE - UnmanagedCallConv or UnmanagedCallConv.CallConvs not specified
    //   COR_E_INVALIDPROGRAM - Program is considered invalid (more
    //                          than one calling convention specified)
    //-------------------------------------------------------------------------
    HRESULT TryGetCallingConventionFromUnmanagedCallConv(
        _In_ MethodDesc* pMD,
        _Inout_ CallConvBuilder* builder,
        _Out_opt_ UINT* errorResID);

    //-------------------------------------------------------------------------
    // Gets the unmanaged calling convention from the UnmanagedCallersOnly attribute.
    //
    // Returns:
    //   true  - No errors
    //   false - Not specified or invalid (more than one calling convention specified)
    //-------------------------------------------------------------------------
    bool TryGetCallingConventionFromUnmanagedCallersOnly(_In_ MethodDesc* pMD, _Out_ CorInfoCallConvExtension* callConv);
}

#endif // __CALLCONVBUILDER_H__
