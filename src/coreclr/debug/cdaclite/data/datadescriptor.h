// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datadescriptor.h
//
// Parses the in-memory data descriptor JSON (compact format, no baseline) into
// a structured model: types (with field offsets), globals, and contracts.
//
// See docs/design/datacontracts/data_descriptor.md for the format.
//*****************************************************************************

#ifndef CDACLITE_DATADESCRIPTOR_H
#define CDACLITE_DATADESCRIPTOR_H

#include <stdint.h>
#include <string>
#include <map>
#include <vector>

#include "json.h"

namespace cdac
{
    // Reads 'size' bytes at 'address' from the target into 'buffer'. Returns true
    // only if all bytes were read. 'context' is caller-defined.
    typedef bool (*ReadMemoryCallback)(void* context, uint64_t address, void* buffer, uint32_t size);

    struct FieldInfo
    {
        uint32_t offset = 0;
        std::string type; // may be empty if not specified in the descriptor
    };

    struct TypeInfo
    {
        bool hasSize = false;
        uint32_t size = 0;
        std::map<std::string, FieldInfo> fields;
    };

    struct GlobalInfo
    {
        // Direct: a numeric or string value is embedded in the descriptor.
        // Indirect: the value lives in the contract descriptor's pointer_data
        // array at 'pointerIndex' and must be read from the target.
        bool isIndirect = false;
        uint32_t pointerIndex = 0;

        bool hasNumericValue = false;
        uint64_t numericValue = 0;

        // True once an indirect global has been resolved: numericValue holds a
        // target address (the location of the runtime variable), not a constant.
        bool isAddress = false;

        bool isString = false;
        std::string stringValue;

        std::string type; // may be empty
    };

    class DataDescriptor
    {
    public:
        // Reads the contract descriptor at 'contractDescriptorAddr' from the target
        // (via 'readMemory'), parses its in-memory JSON, resolves its indirect globals,
        // and recursively loads and merges any sub-descriptors (e.g. the GC descriptor).
        // The result is a single merged descriptor. Returns false and fills 'error' on
        // failure to read/parse the root descriptor.
        bool Load(ReadMemoryCallback readMemory, void* context, uint64_t contractDescriptorAddr, std::string& error);

        // Parses the descriptor JSON. Returns false and fills 'error' on failure.
        bool Parse(const char* json, size_t length, std::string& error);

        // Converts this descriptor's indirect globals ([index]) into direct values by
        // reading pointerData[index]. After this, globals carry absolute addresses/values
        // and no longer depend on a pointer_data array. Indices out of range are dropped.
        void ResolveIndirectGlobals(const uint64_t* pointerData, uint32_t pointerDataCount);

        // Merges another (already-resolved) descriptor's types, globals, and contracts
        // into this one. Existing entries take precedence (are not overwritten).
        void Merge(const DataDescriptor& other);

        const std::map<std::string, TypeInfo>& Types() const { return m_types; }
        const std::map<std::string, GlobalInfo>& Globals() const { return m_globals; }
        const std::map<std::string, std::string>& Contracts() const { return m_contracts; }

        // Sub-descriptor references (name -> pointer_data index), e.g. "GC".
        const std::map<std::string, GlobalInfo>& SubDescriptors() const { return m_subDescriptors; }

        const TypeInfo* FindType(const std::string& name) const;

        // Convenience: looks up a field offset for a given type.
        bool TryGetFieldOffset(const std::string& typeName, const std::string& fieldName, uint32_t& offset) const;

        // Convenience: looks up a global's direct numeric value. Returns false for
        // indirect globals (which require a target read) or non-numeric globals.
        bool TryGetGlobalValue(const std::string& name, uint64_t& value) const;

        // Addresses of every ContractDescriptor loaded by Load() -- the main
        // descriptor plus each recursively-loaded sub-descriptor. The bootstrap
        // contract emits struct/JSON/pointer_data for each of these.
        const std::vector<uint64_t>& DescriptorAddresses() const { return m_descriptorAddresses; }

    private:
        void ParseTypes(const json::Value& types);
        void ParseGlobals(const json::Value& globals);
        void ParseGlobalValue(const json::Value& value, GlobalInfo& info);
        void ParseContracts(const json::Value& contracts);
        void ParseSubDescriptors(const json::Value& subDescriptors);

        // Recursively reads the descriptor at 'address', merges it into this descriptor,
        // and follows its sub-descriptors. 'depth' guards against cycles.
        bool LoadMerged(ReadMemoryCallback readMemory, void* context, uint64_t address, int depth, std::string& error);

        std::map<std::string, TypeInfo> m_types;
        std::map<std::string, GlobalInfo> m_globals;
        std::map<std::string, std::string> m_contracts;
        std::map<std::string, GlobalInfo> m_subDescriptors;
        std::vector<uint64_t> m_descriptorAddresses;
    };
}

#endif // CDACLITE_DATADESCRIPTOR_H
