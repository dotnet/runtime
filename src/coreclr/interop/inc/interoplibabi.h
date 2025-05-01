// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIBABI_H_
#define _INTEROP_INC_INTEROPLIBABI_H_

#include <stddef.h>
#include <interoplib.h>

namespace InteropLib
{
    namespace ABI
    {
        // Updating this also requires updating ComInterfaceDispatch::GetInstance<T>.
#ifdef HOST_64BIT
        constexpr size_t DispatchAlignmentThisPtr = 64; // Should be a power of 2.
#else
        constexpr size_t DispatchAlignmentThisPtr = 16; // Should be a power of 2.
#endif

        constexpr intptr_t DispatchThisPtrMask = ~(DispatchAlignmentThisPtr - 1);

        static_assert(sizeof(void*) < DispatchAlignmentThisPtr);

        constexpr size_t EntriesPerThisPtr = (DispatchAlignmentThisPtr / sizeof(void*)) - 1;

        struct ComInterfaceDispatch
        {
            const void* vtable;
        };

        static_assert(sizeof(ComInterfaceDispatch) == sizeof(void*), "ComInterfaceDispatch must be pointer-sized.");

        struct ManagedObjectWrapperLayout;

        struct InternalComInterfaceDispatch
        {
        private:
            ManagedObjectWrapperLayout* _thisPtr;
        public:
            ComInterfaceDispatch _entries[EntriesPerThisPtr];
        };

        struct ComInterfaceEntry
        {
            GUID IID;
            const void* Vtable;
        };

        // Managed object wrapper layout.
        // This is designed to codify the binary layout.
        struct ManagedObjectWrapperLayout
        {
#ifdef DACCESS_COMPILE
            friend class ClrDataAccess;
#endif // DACCESS_COMPILE
        public:
            LONGLONG GetRawRefCount() const
            {
                return _refCount;
            }

        protected:
            Volatile<InteropLib::OBJECTHANDLE> _target;
            int64_t _refCount;

            Volatile<InteropLib::Com::CreateComInterfaceFlagsEx> _flags;
            int32_t _userDefinedCount;
            ComInterfaceEntry* _userDefined;
            InternalComInterfaceDispatch* _dispatches;
        };

        // Given the entry index, compute the dispatch index.
        inline ComInterfaceDispatch* IndexIntoDispatchSection(int32_t i, InternalComInterfaceDispatch* dispatches)
        {
            InternalComInterfaceDispatch* dispatch = dispatches + i / EntriesPerThisPtr;
            ComInterfaceDispatch* entries = dispatch->_entries;
            return entries + (i % EntriesPerThisPtr);
        }
    }
}

#endif // _INTEROP_INC_INTEROPLIBABI_H_
