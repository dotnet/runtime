// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// target.h
//
// The Target abstraction: typed reads of a target process/dump's memory,
// combined with the parsed data descriptor. Modeled on the managed cDAC's
// Target type, but simplified: cdac-lite is built for the *same* platform as
// the target it inspects, so reads use the native pointer size and endianness
// (no byte-swapping or pointer-size branching).
//
// The Target is intentionally decoupled from any COM/ICLRDataTarget type. It
// reads memory through a caller-provided callback so this module can be unit
// tested and reused independently.
//*****************************************************************************

#ifndef CDACLITE_TARGET_H
#define CDACLITE_TARGET_H

#include <stdint.h>
#include <stddef.h>
#include <string>

#include "datadescriptor.h"

namespace cdac
{
    // Reads 'size' bytes at 'address' from the target into 'buffer'. Returns true
    // only if all bytes were read. 'context' is passed through from Target::Create.
    typedef bool (*ReadMemoryCallback)(void* context, uint64_t address, void* buffer, uint32_t size);

    // The memory-enumeration tier requested for a dump. Normal captures only the state a stack
    // walk reaches (stacks, code, method metadata); Heap additionally captures the GC heap and
    // the heap-side structures (handles, sync blocks, stress log, COM interop).
    enum class DumpTier
    {
        Normal,
        Heap,
    };

    class Target
    {
    public:
        // Builds a Target from a descriptor whose globals have already been resolved to
        // absolute addresses/values (see DataDescriptor::ResolveIndirectGlobals). Reads
        // memory through the provided callback. Does not take ownership of 'descriptor'.
        Target(const DataDescriptor* descriptor,
               ReadMemoryCallback readMemory,
               void* readContext);

        // Native pointer size of the target (== this build's pointer size).
        uint32_t PointerSize() const { return (uint32_t)sizeof(void*); }

        const DataDescriptor* Descriptor() const { return m_descriptor; }

        // The enumeration tier for the current dump. Contracts can query this to scope their
        // work; the enumeration driver uses it to decide which contracts to run.
        DumpTier Tier() const { return m_tier; }
        void SetTier(DumpTier tier) { m_tier = tier; }

        // --- Typed struct reads (Data-type layer) ---------------------------

        // Reads a Data type (any struct with `bool Load(const Target&, uint64_t)`)
        // at 'address'. Mirrors the managed cDAC ProcessedData.GetOrAdd<T>() pattern.
        // When an EnumMem sink is set (see SetEnumMemSink), a successful read also records
        // the structure's own memory -- the cdac-lite analog of the DAC's DPTR::EnumMem().
        template <typename TData>
        bool TryRead(uint64_t address, TData& data) const
        {
            if (!data.Load(*this, address))
            {
                return false;
            }
            if (m_enumMemSink != nullptr)
            {
                EmitStructMemory(data.TypeName(), address);
            }
            return true;
        }

        // --- Implicit metadata enumeration (DAC DPTR::EnumMem() analog) -------

        // Observer invoked with [address, address+size) for each structure read while a walk
        // is in progress. Lets a walk implicitly capture the metadata structures it traverses,
        // exactly as the DAC records every DPTR it dereferences during EnumMemoryRegions.
        typedef void (*EnumMemCallback)(void* context, uint64_t address, uint32_t size);

        // Sets (or clears, with nullptr) the EnumMem observer. Const because it toggles a
        // transient "enumeration mode" rather than logical target state.
        void SetEnumMemSink(EnumMemCallback sink, void* context) const
        {
            m_enumMemSink = sink;
            m_enumMemContext = context;
        }

        // Records [address, address+size) through the EnumMem sink, if one is set. Use for
        // memory a walk reads that is not a Data-type struct (e.g. pointer arrays).
        void EmitMemory(uint64_t address, uint32_t size) const
        {
            if (m_enumMemSink != nullptr && size > 0)
            {
                m_enumMemSink(m_enumMemContext, address, size);
            }
        }

        // --- Raw reads -------------------------------------------------------

        // Reads 'size' bytes into 'buffer'. Returns true iff all bytes were read.
        bool ReadBuffer(uint64_t address, void* buffer, uint32_t size) const;

        bool TryReadUInt8(uint64_t address, uint8_t& value) const;
        bool TryReadUInt16(uint64_t address, uint16_t& value) const;
        bool TryReadUInt32(uint64_t address, uint32_t& value) const;
        bool TryReadUInt64(uint64_t address, uint64_t& value) const;

        // Reads a native-pointer-sized value.
        bool TryReadPointer(uint64_t address, uint64_t& value) const;

        // --- Descriptor-aware reads -----------------------------------------

        // Reads a field of a type instance at 'baseAddress'. The field offset comes
        // from the data descriptor. Returns false if the type/field is unknown or
        // the read fails.
        bool TryReadFieldUInt32(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint32_t& value) const;
        bool TryReadFieldUInt64(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& value) const;
        bool TryReadFieldPointer(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& value) const;

        // Computes the absolute address of a field within an instance.
        bool TryGetFieldAddress(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& address) const;

        // Returns the size of a type, if known.
        bool TryGetTypeSize(const std::string& typeName, uint32_t& size) const;

        // --- Globals ---------------------------------------------------------

        // Returns the resolved value stored for a global. For a global that points at a
        // variable this is the variable's address; for a direct value global it is the
        // value itself. Returns false if the global is absent/unresolved.
        bool TryGetGlobalValue(const std::string& name, uint64_t& value) const;

        // Reads a native pointer at the global's resolved address (one dereference).
        bool TryReadGlobalPointer(const std::string& name, uint64_t& value) const;

        // Reads a uint32 at the global's resolved address (one dereference).
        bool TryReadGlobalUInt32(const std::string& name, uint32_t& value) const;

        // Reads a string global's value, if present.
        bool TryGetGlobalString(const std::string& name, std::string& value) const;

    private:
        // Records a Data-type struct's own memory through the EnumMem sink. Uses the
        // descriptor type size (the analog of the DAC's sizeof(type)).
        void EmitStructMemory(const char* typeName, uint64_t address) const;

        const DataDescriptor* m_descriptor;
        ReadMemoryCallback m_readMemory;
        void* m_readContext;
        DumpTier m_tier = DumpTier::Heap;
        mutable EnumMemCallback m_enumMemSink = nullptr;
        mutable void* m_enumMemContext = nullptr;
    };
}

#endif // CDACLITE_TARGET_H
