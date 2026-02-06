// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIB_H_
#define _INTEROP_INC_INTEROPLIB_H_

namespace InteropLibImports
{
    // Forward declaration of Runtime calling context.
    // This class is used by the consuming runtime to pass through details
    // that may be required during a subsequent callback from the InteropLib.
    // InteropLib never directly modifies or inspects supplied instances.
    struct RuntimeCallContext;
}

namespace InteropLib
{
    using OBJECTHANDLE = void*;

    namespace ABI
    {
        struct ManagedObjectWrapperLayout;
    }

    namespace Com
    {
        bool IsRooted(_In_ ABI::ManagedObjectWrapperLayout* wrapper) noexcept;

        HRESULT MarkComActivated(_In_ IUnknown* wrapper) noexcept;

        // See CreateObjectFlags in ComWrappers.cs
        enum CreateObjectFlags
        {
            CreateObjectFlags_None = 0,
            CreateObjectFlags_TrackerObject = 1,
            CreateObjectFlags_UniqueInstance = 2,
            CreateObjectFlags_Aggregated = 4,
            CreateObjectFlags_Unwrap = 8,
        };

        enum class CreateComInterfaceFlagsEx : int32_t
        {
            // Matches the managed definition of System.Runtime.InteropServices.CreateComInterfaceFlags
            None = 0,
            CallerDefinedIUnknown = 1,
            TrackerSupport = 2,

            // Highest bits are reserved for internal usage
            LacksICustomQueryInterface = 1 << 29,
            IsComActivated = 1 << 30,
            IsPegged = 1 << 31,

            InternalMask = IsPegged | IsComActivated | LacksICustomQueryInterface,
        };


        // Get internal interop IUnknown dispatch pointers.
        void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease) noexcept;

        void const* GetTaggedCurrentVersionImpl() noexcept;

        // Begin the reference tracking process on external COM objects.
        // This should only be called during a runtime's GC phase.
        HRESULT BeginExternalObjectReferenceTracking(_In_ InteropLibImports::RuntimeCallContext* cxt) noexcept;

        // End the reference tracking process.
        // This should only be called during a runtime's GC phase.
        HRESULT EndExternalObjectReferenceTracking() noexcept;

        // Detach non-promoted objects from the reference tracker.
        // This should only be called during a runtime's GC phase.
        HRESULT DetachNonPromotedObjects(_In_ InteropLibImports::RuntimeCallContext* cxt) noexcept;

        // Get the vtable for IReferenceTrackerTarget
        void const* GetIReferenceTrackerTargetVftbl() noexcept;

        // Check if a ReferenceTrackerManager has been registered.
        bool HasReferenceTrackerManager() noexcept;

        // Register a ReferenceTrackerManager if one has not already been registered.
        bool TryRegisterReferenceTrackerManager(void* manager) noexcept;
    }
}

#ifndef DEFINE_ENUM_FLAG_OPERATORS
#define DEFINE_ENUM_FLAG_OPERATORS(ENUMTYPE) \
extern "C++" { \
    inline ENUMTYPE operator | (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)|((int)b)); } \
    inline ENUMTYPE operator |= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) |= ((int)b)); } \
    inline ENUMTYPE operator & (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)&((int)b)); } \
    inline ENUMTYPE operator &= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) &= ((int)b)); } \
    inline ENUMTYPE operator ~ (ENUMTYPE a) { return (ENUMTYPE)(~((int)a)); } \
    inline ENUMTYPE operator ^ (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)^((int)b)); } \
    inline ENUMTYPE operator ^= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) ^= ((int)b)); } \
}
#endif

DEFINE_ENUM_FLAG_OPERATORS(InteropLib::Com::CreateComInterfaceFlagsEx);

#endif // _INTEROP_INC_INTEROPLIB_H_
