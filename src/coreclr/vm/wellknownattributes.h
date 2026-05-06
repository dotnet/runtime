// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __WELLKNOWNATTRIBUTES_H_
#define __WELLKNOWNATTRIBUTES_H_

enum class WellKnownAttribute : DWORD
{
    ParamArray,
    DefaultMember,
    FixedAddressValueType,
    UnsafeValueType,
    BestFitMapping,
    ClassInterface,
    CoClass,
    ComCompatibleVersion,
    ComDefaultInterface,
    ComEventInterface,
    ComSourceInterfaces,
    ComVisible,
    SuppressGCTransition,
    DefaultDllImportSearchPaths,
    Guid,
    LCIDConversion,
    ImportedFromTypeLib,
    Intrinsic,
    IsByRefLike,
    PrimaryInteropAssembly,
    ManagedToNativeComInteropStub,
    UnmanagedCallConv,
    UnmanagedCallersOnly,
    TypeIdentifier,
    UnmanagedFunctionPointer,
    ThreadStatic,
    PreserveBaseOverridesAttribute,
    ObjectiveCTrackedTypeAttribute,
    InlineArrayAttribute,
    UnsafeAccessorAttribute,
    UnsafeAccessorTypeAttribute,
    ExtendedLayoutAttribute,

    CountOfWellKnownAttributes
};

inline const char *GetWellKnownAttributeName(WellKnownAttribute attribute)
{
    LIMITED_METHOD_CONTRACT;

    const char* ret;
    switch (attribute)
    {
        case WellKnownAttribute::ParamArray:
            ret = "System.ParamArrayAttribute";
            break;
        case WellKnownAttribute::DefaultMember:
            ret = "System.Reflection.DefaultMemberAttribute";
            break;
        case WellKnownAttribute::FixedAddressValueType:
            ret = "System.Runtime.CompilerServices.FixedAddressValueTypeAttribute";
            break;
        case WellKnownAttribute::UnsafeValueType:
            ret = "System.Runtime.CompilerServices.UnsafeValueTypeAttribute";
            break;
        case WellKnownAttribute::BestFitMapping:
            ret = "System.Runtime.InteropServices.BestFitMappingAttribute";
            break;
        case WellKnownAttribute::ClassInterface:
            ret = "System.Runtime.InteropServices.ClassInterfaceAttribute";
            break;
        case WellKnownAttribute::CoClass:
            ret = "System.Runtime.InteropServices.CoClassAttribute";
            break;
        case WellKnownAttribute::ComCompatibleVersion:
            ret = "System.Runtime.InteropServices.ComCompatibleVersionAttribute";
            break;
        case WellKnownAttribute::ComDefaultInterface:
            ret = "System.Runtime.InteropServices.ComDefaultInterfaceAttribute";
            break;
        case WellKnownAttribute::ComEventInterface:
            ret = "System.Runtime.InteropServices.ComEventInterfaceAttribute";
            break;
        case WellKnownAttribute::ComSourceInterfaces:
            ret = "System.Runtime.InteropServices.ComSourceInterfacesAttribute";
            break;
        case WellKnownAttribute::ComVisible:
            ret = "System.Runtime.InteropServices.ComVisibleAttribute";
            break;
        case WellKnownAttribute::SuppressGCTransition:
            ret = "System.Runtime.InteropServices.SuppressGCTransitionAttribute";
            break;
        case WellKnownAttribute::DefaultDllImportSearchPaths:
            ret = "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute";
            break;
        case WellKnownAttribute::Guid:
            ret = "System.Runtime.InteropServices.GuidAttribute";
            break;
        case WellKnownAttribute::LCIDConversion:
            ret = "System.Runtime.InteropServices.LCIDConversionAttribute";
            break;
        case WellKnownAttribute::ImportedFromTypeLib:
            ret = "System.Runtime.InteropServices.ImportedFromTypeLibAttribute";
            break;
        case WellKnownAttribute::Intrinsic:
            ret = "System.Runtime.CompilerServices.IntrinsicAttribute";
            break;
        case WellKnownAttribute::IsByRefLike:
            ret = "System.Runtime.CompilerServices.IsByRefLikeAttribute";
            break;
        case WellKnownAttribute::PrimaryInteropAssembly:
            ret = "System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute";
            break;
        case WellKnownAttribute::ManagedToNativeComInteropStub:
            ret = "System.Runtime.InteropServices.ManagedToNativeComInteropStubAttribute";
            break;
        case WellKnownAttribute::UnmanagedCallConv:
            ret = "System.Runtime.InteropServices.UnmanagedCallConvAttribute";
            break;
        case WellKnownAttribute::UnmanagedCallersOnly:
            ret = "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute";
            break;
        case WellKnownAttribute::TypeIdentifier:
            ret = "System.Runtime.InteropServices.TypeIdentifierAttribute";
            break;
        case WellKnownAttribute::UnmanagedFunctionPointer:
            ret = "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute";
            break;
        case WellKnownAttribute::ThreadStatic:
            ret = "System.ThreadStaticAttribute";
            break;
        case WellKnownAttribute::PreserveBaseOverridesAttribute:
            ret = "System.Runtime.CompilerServices.PreserveBaseOverridesAttribute";
            break;
        case WellKnownAttribute::ObjectiveCTrackedTypeAttribute:
            ret = "System.Runtime.InteropServices.ObjectiveC.ObjectiveCTrackedTypeAttribute";
            break;
        case WellKnownAttribute::InlineArrayAttribute:
            ret = "System.Runtime.CompilerServices.InlineArrayAttribute";
            break;
        case WellKnownAttribute::UnsafeAccessorAttribute:
            ret = "System.Runtime.CompilerServices.UnsafeAccessorAttribute";
            break;
        case WellKnownAttribute::UnsafeAccessorTypeAttribute:
            ret = "System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute";
            break;
        case WellKnownAttribute::ExtendedLayoutAttribute:
            ret = "System.Runtime.InteropServices.ExtendedLayoutAttribute";
            break;
        case WellKnownAttribute::CountOfWellKnownAttributes:
        default:
            ret = nullptr;
            break;
    }

#ifdef _DEBUG
    _ASSERTE(ret != nullptr); // Should not be possible

    // ReadyToRun special cases what attributes it detects.
    // Currently it special cases a few and then assumes all other
    // attributes are under the "System.Runtime" namespace.
    // See AttributePresenceFilterNode.cs.
    const char prefix[] = "System.Runtime.";
    bool readyToRunAware =
        attribute == WellKnownAttribute::ThreadStatic
        || attribute == WellKnownAttribute::ParamArray
        || attribute == WellKnownAttribute::DefaultMember
        || strncmp(prefix, ret, ARRAY_SIZE(prefix) - 1) == 0;
    _ASSERTE(readyToRunAware);
#endif // _DEBUG

    return ret;
}

#endif // __WELLKNOWNATTRIBUTES_H_
