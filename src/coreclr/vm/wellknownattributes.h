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
    IDispatchImpl,
    ImportedFromTypeLib,
    Intrinsic,
    IsByRefLike,
    PrimaryInteropAssembly,
    ManagedToNativeComInteropStub,
    UnmanagedCallersOnly,
    NativeCallableInternal, // This is needed to support MCG scenarios
    TypeIdentifier,
    UnmanagedFunctionPointer,
    ThreadStatic,
    WinRTMarshalingBehaviorAttribute,
    PreserveBaseOverridesAttribute,
    ObjectiveCTrackedTypeAttribute,

    CountOfWellKnownAttributes
};

inline const char *GetWellKnownAttributeName(WellKnownAttribute attribute)
{
    switch (attribute)
    {
        case WellKnownAttribute::ParamArray:
            return "System.ParamArrayAttribute";
        case WellKnownAttribute::DefaultMember:
            return "System.Reflection.DefaultMemberAttribute";
        case WellKnownAttribute::FixedAddressValueType:
            return "System.Runtime.CompilerServices.FixedAddressValueTypeAttribute";
        case WellKnownAttribute::UnsafeValueType:
            return "System.Runtime.CompilerServices.UnsafeValueTypeAttribute";
        case WellKnownAttribute::BestFitMapping:
            return "System.Runtime.InteropServices.BestFitMappingAttribute";
        case WellKnownAttribute::ClassInterface:
            return "System.Runtime.InteropServices.ClassInterfaceAttribute";
        case WellKnownAttribute::CoClass:
            return "System.Runtime.InteropServices.CoClassAttribute";
        case WellKnownAttribute::ComCompatibleVersion:
            return "System.Runtime.InteropServices.ComCompatibleVersionAttribute";
        case WellKnownAttribute::ComDefaultInterface:
            return "System.Runtime.InteropServices.ComDefaultInterfaceAttribute";
        case WellKnownAttribute::ComEventInterface:
            return "System.Runtime.InteropServices.ComEventInterfaceAttribute";
        case WellKnownAttribute::ComSourceInterfaces:
            return "System.Runtime.InteropServices.ComSourceInterfacesAttribute";
        case WellKnownAttribute::ComVisible:
            return "System.Runtime.InteropServices.ComVisibleAttribute";
        case WellKnownAttribute::SuppressGCTransition:
            return "System.Runtime.InteropServices.SuppressGCTransitionAttribute";
        case WellKnownAttribute::DefaultDllImportSearchPaths:
            return "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute";
        case WellKnownAttribute::Guid:
            return "System.Runtime.InteropServices.GuidAttribute";
        case WellKnownAttribute::LCIDConversion:
            return "System.Runtime.InteropServices.LCIDConversionAttribute";
        case WellKnownAttribute::IDispatchImpl:
            return "System.Runtime.InteropServices.IDispatchImplAttribute";
        case WellKnownAttribute::ImportedFromTypeLib:
            return "System.Runtime.InteropServices.ImportedFromTypeLibAttribute";
        case WellKnownAttribute::Intrinsic:
            return "System.Runtime.CompilerServices.IntrinsicAttribute";
        case WellKnownAttribute::IsByRefLike:
            return "System.Runtime.CompilerServices.IsByRefLikeAttribute";
        case WellKnownAttribute::PrimaryInteropAssembly:
            return "System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute";
        case WellKnownAttribute::ManagedToNativeComInteropStub:
            return "System.Runtime.InteropServices.ManagedToNativeComInteropStubAttribute";
        case WellKnownAttribute::UnmanagedCallersOnly:
            return "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute";
        case WellKnownAttribute::NativeCallableInternal:
            return "System.Runtime.InteropServices.NativeCallableInternalAttribute";
        case WellKnownAttribute::TypeIdentifier:
            return "System.Runtime.InteropServices.TypeIdentifierAttribute";
        case WellKnownAttribute::UnmanagedFunctionPointer:
            return "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute";
        case WellKnownAttribute::ThreadStatic:
            return "System.ThreadStaticAttribute";
        case WellKnownAttribute::WinRTMarshalingBehaviorAttribute:
            return "Windows.Foundation.Metadata.MarshalingBehaviorAttribute";
        case WellKnownAttribute::PreserveBaseOverridesAttribute:
            return "System.Runtime.CompilerServices.PreserveBaseOverridesAttribute";
        case WellKnownAttribute::ObjectiveCTrackedTypeAttribute:
            return "System.Runtime.InteropServices.ObjectiveC.ObjectiveCTrackedTypeAttribute";
        case WellKnownAttribute::CountOfWellKnownAttributes:
        default:
            break; // Silence compiler warnings
    }
    _ASSERTE(false); // Should not be possible
    return nullptr;
}

#endif // __WELLKNOWNATTRIBUTES_H_
