// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPMETHODDATA_H_
#define _INTERPMETHODDATA_H_

#include "interpalloc.h"

// Forward declarations - actual definitions in interpretershared.h
struct InterpMethod;
struct InterpByteCodeStart;
struct InterpGenericLookup;
struct InterpAsyncSuspendData;
struct InterpIntervalMapEntry;

// InterpMethodDataBuilder accumulates all persistent data for a compiled method
// during compilation, then finalizes it into a single contiguous allocation.
//
// Memory Layout after finalization:
// ┌────────────────────────────────────────┐
// │ InterpByteCodeStart (Method ptr)       │  <- baseAddress
// ├────────────────────────────────────────┤
// │ Bytecodes (int32_t[])                  │
// ├────────────────────────────────────────┤
// │ InterpMethod struct                    │
// ├────────────────────────────────────────┤
// │ DataItems array (void*[])              │
// ├────────────────────────────────────────┤
// │ InterpAsyncSuspendData structs         │
// ├────────────────────────────────────────┤
// │ InterpIntervalMapEntry arrays          │
// └────────────────────────────────────────┘

// Sections within the unified method data allocation
enum class InterpMethodDataSection : uint8_t
{
    Header,           // InterpByteCodeStart
    Bytecode,         // int32_t[] opcodes
    InterpMethod,     // InterpMethod struct
    DataItems,        // void*[] array
    AsyncSuspendData, // InterpAsyncSuspendData structs
    IntervalMaps,     // InterpIntervalMapEntry arrays
    Count
};

// Reference to a location within a section, used during compilation
// before final addresses are known
struct InterpSectionRef
{
    InterpMethodDataSection section;
    uint32_t offset;  // Offset within the section

    InterpSectionRef() : section(InterpMethodDataSection::Count), offset(0) {}
    InterpSectionRef(InterpMethodDataSection s, uint32_t o) : section(s), offset(o) {}

    bool IsNull() const { return section == InterpMethodDataSection::Count; }
};

// Tracks data for a single section during building
struct InterpSectionData
{
    uint32_t size = 0;
    uint32_t alignment = sizeof(void*);  // Default to pointer alignment
    uint32_t finalOffset = 0;            // Offset in final allocation (set during GetTotalSize)
};

class InterpMethodDataBuilder
{
private:
    InterpSectionData m_sections[(int)InterpMethodDataSection::Count];

    // Cached section base addresses after finalization
    uint8_t* m_finalBaseAddress = nullptr;
    bool m_finalized = false;

    static uint32_t AlignUp(uint32_t value, uint32_t alignment);

public:
    InterpMethodDataBuilder();
    ~InterpMethodDataBuilder();

    // Allocate space in a section and return a reference to it
    InterpSectionRef AllocateInSection(InterpMethodDataSection section, uint32_t size, uint32_t alignment = 0);

    // Set the bytecode section size (bytecodes are written directly by the compiler)
    void SetBytecodeSize(uint32_t sizeInBytes);

    // Calculate total size needed for the unified allocation
    uint32_t GetTotalSize();

    // Get the final offset of a section within the allocation
    uint32_t GetSectionOffset(InterpMethodDataSection section) const;

    // Get the size of a section
    uint32_t GetSectionSize(InterpMethodDataSection section) const;

    // Convert a section reference to a final pointer (only valid after Finalize)
    void* GetFinalPointer(InterpSectionRef ref) const;

    // Finalize the method data
    // baseAddressRW is the writable address, baseAddressRX is the executable address
    void Finalize(void* baseAddressRW, void* baseAddressRX);

    // Get the InterpByteCodeStart pointer (only valid after Finalize)
    InterpByteCodeStart* GetByteCodeStart() const;

    // Get the writable location for a section reference (for copying data during finalization)
    void* GetWritablePointer(void* baseAddressRW, InterpSectionRef ref) const;

    // Helper: Allocate InterpMethod and return its reference
    InterpSectionRef AllocateInterpMethod();

    // Helper: Allocate data items array
    InterpSectionRef AllocateDataItems(int32_t count);

    // Helper: Allocate async suspend data
    InterpSectionRef AllocateAsyncSuspendData();

    // Helper: Allocate interval map entries
    InterpSectionRef AllocateIntervalMap(int32_t count);

    bool IsFinalized() const { return m_finalized; }
};

#endif // _INTERPMETHODDATA_H_
