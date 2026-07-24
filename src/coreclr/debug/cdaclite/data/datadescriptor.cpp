// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datadescriptor.cpp
//
// Implementation of the DataDescriptor model declared in datadescriptor.h.
//*****************************************************************************

#include "datadescriptor.h"

#include <stdio.h>
#include <string.h>
#include <vector>

namespace cdac
{
    namespace
    {
        // Contract descriptor magic: "DNCCDAC\0" in target (== native) endianness.
        const uint64_t ContractDescriptorMagic = 0x0043414443434e44ull;
        // Guard against reading absurd JSON blobs from a possibly-corrupt target.
        const uint32_t MaxDescriptorSize = 8 * 1024 * 1024;
        // Guard against sub-descriptor cycles.
        const int MaxSubDescriptorDepth = 8;
    }

    const TypeInfo* DataDescriptor::FindType(const std::string& name) const
    {
        std::map<std::string, TypeInfo>::const_iterator it = m_types.find(name);
        return (it == m_types.end()) ? nullptr : &it->second;
    }

    bool DataDescriptor::TryGetFieldOffset(const std::string& typeName, const std::string& fieldName, uint32_t& offset) const
    {
        const TypeInfo* type = FindType(typeName);
        if (type == nullptr)
        {
            return false;
        }
        std::map<std::string, FieldInfo>::const_iterator it = type->fields.find(fieldName);
        if (it == type->fields.end())
        {
            return false;
        }
        offset = it->second.offset;
        return true;
    }

    bool DataDescriptor::TryGetGlobalValue(const std::string& name, uint64_t& value) const
    {
        std::map<std::string, GlobalInfo>::const_iterator it = m_globals.find(name);
        if (it == m_globals.end() || it->second.isIndirect || !it->second.hasNumericValue)
        {
            return false;
        }
        value = it->second.numericValue;
        return true;
    }

    void DataDescriptor::ParseTypes(const json::Value& types)
    {
        if (!types.IsObject())
        {
            return;
        }

        for (std::map<std::string, json::Value>::const_iterator it = types.object.begin(); it != types.object.end(); ++it)
        {
            const std::string& typeName = it->first;
            const json::Value& fieldDict = it->second;
            if (!fieldDict.IsObject())
            {
                continue;
            }

            TypeInfo info;
            for (std::map<std::string, json::Value>::const_iterator f = fieldDict.object.begin(); f != fieldDict.object.end(); ++f)
            {
                const std::string& key = f->first;
                const json::Value& fieldValue = f->second;

                // The special "!" key gives the total size of the struct.
                if (key == "!")
                {
                    uint64_t size = 0;
                    if (fieldValue.TryGetUInt64(size))
                    {
                        info.hasSize = true;
                        info.size = (uint32_t)size;
                    }
                    continue;
                }

                FieldInfo field;
                if (fieldValue.IsArray())
                {
                    // [offset, "type"]
                    if (fieldValue.array.size() >= 1)
                    {
                        uint64_t offset = 0;
                        if (fieldValue.array[0].TryGetUInt64(offset))
                        {
                            field.offset = (uint32_t)offset;
                        }
                    }
                    if (fieldValue.array.size() >= 2 && fieldValue.array[1].IsString())
                    {
                        field.type = fieldValue.array[1].string;
                    }
                }
                else
                {
                    // Just an offset.
                    uint64_t offset = 0;
                    if (!fieldValue.TryGetUInt64(offset))
                    {
                        continue;
                    }
                    field.offset = (uint32_t)offset;
                }

                info.fields[key] = field;
            }

            m_types[typeName] = info;
        }
    }

    void DataDescriptor::ParseGlobalValue(const json::Value& value, GlobalInfo& info)
    {
        // Grammar (compact form, see data_descriptor.md):
        //   <global_value>          = <number> | <string> | [ <index> ]   (1-elem array = indirect)
        //   with an optional type:  [ <global_value>, "type" ]
        if (value.IsArray())
        {
            if (value.array.size() == 1)
            {
                // [index] -> indirect value in the pointer_data array.
                uint64_t index = 0;
                if (value.array[0].TryGetUInt64(index))
                {
                    info.isIndirect = true;
                    info.pointerIndex = (uint32_t)index;
                }
                return;
            }
            if (value.array.size() >= 2)
            {
                // [value, "type"] where value itself may be [index], a number, or a string.
                if (value.array[1].IsString())
                {
                    info.type = value.array[1].string;
                }
                const json::Value& inner = value.array[0];
                if (inner.IsArray() && inner.array.size() == 1)
                {
                    uint64_t index = 0;
                    if (inner.array[0].TryGetUInt64(index))
                    {
                        info.isIndirect = true;
                        info.pointerIndex = (uint32_t)index;
                    }
                }
                else
                {
                    uint64_t numeric = 0;
                    if (inner.TryGetUInt64(numeric))
                    {
                        info.hasNumericValue = true;
                        info.numericValue = numeric;
                    }
                    else if (inner.IsString())
                    {
                        info.isString = true;
                        info.stringValue = inner.string;
                    }
                }
            }
            return;
        }

        uint64_t numeric = 0;
        if (value.TryGetUInt64(numeric))
        {
            info.hasNumericValue = true;
            info.numericValue = numeric;
        }
        else if (value.IsString())
        {
            info.isString = true;
            info.stringValue = value.string;
        }
    }

    void DataDescriptor::ParseGlobals(const json::Value& globals)
    {
        if (!globals.IsObject())
        {
            return;
        }

        for (std::map<std::string, json::Value>::const_iterator it = globals.object.begin(); it != globals.object.end(); ++it)
        {
            GlobalInfo info;
            ParseGlobalValue(it->second, info);
            m_globals[it->first] = info;
        }
    }

    void DataDescriptor::ParseContracts(const json::Value& contracts)
    {
        if (!contracts.IsObject())
        {
            return;
        }

        for (std::map<std::string, json::Value>::const_iterator it = contracts.object.begin(); it != contracts.object.end(); ++it)
        {
            const json::Value& version = it->second;
            if (version.IsNumber())
            {
                m_contracts[it->first] = version.rawNumber;
            }
            else if (version.IsString())
            {
                m_contracts[it->first] = version.string;
            }
        }
    }

    void DataDescriptor::ParseSubDescriptors(const json::Value& subDescriptors)
    {
        if (!subDescriptors.IsObject())
        {
            return;
        }

        // Each sub-descriptor value is an indirect pointer, e.g. [[44],"pointer"] or [44].
        for (std::map<std::string, json::Value>::const_iterator it = subDescriptors.object.begin(); it != subDescriptors.object.end(); ++it)
        {
            GlobalInfo info;
            ParseGlobalValue(it->second, info);
            if (info.isIndirect)
            {
                m_subDescriptors[it->first] = info;
            }
        }
    }

    void DataDescriptor::ResolveIndirectGlobals(const uint64_t* pointerData, uint32_t pointerDataCount)
    {
        for (std::map<std::string, GlobalInfo>::iterator it = m_globals.begin(); it != m_globals.end(); ++it)
        {
            GlobalInfo& global = it->second;
            if (!global.isIndirect)
            {
                continue;
            }
            if (pointerData != nullptr && global.pointerIndex < pointerDataCount)
            {
                global.numericValue = pointerData[global.pointerIndex];
                global.hasNumericValue = true;
                global.isAddress = true; // numericValue is now a target variable address
            }
            else
            {
                global.hasNumericValue = false;
            }
            global.isIndirect = false;
        }
    }

    void DataDescriptor::Merge(const DataDescriptor& other)
    {
        for (std::map<std::string, TypeInfo>::const_iterator it = other.m_types.begin(); it != other.m_types.end(); ++it)
        {
            m_types.insert(*it); // existing entries take precedence
        }
        for (std::map<std::string, GlobalInfo>::const_iterator it = other.m_globals.begin(); it != other.m_globals.end(); ++it)
        {
            m_globals.insert(*it);
        }
        for (std::map<std::string, std::string>::const_iterator it = other.m_contracts.begin(); it != other.m_contracts.end(); ++it)
        {
            m_contracts.insert(*it);
        }
    }

    bool DataDescriptor::Load(ReadMemoryCallback readMemory, void* context, uint64_t contractDescriptorAddr, std::string& error)
    {
        m_types.clear();
        m_globals.clear();
        m_contracts.clear();
        m_subDescriptors.clear();
        m_descriptorAddresses.clear();
        return LoadMerged(readMemory, context, contractDescriptorAddr, 0, error);
    }

    bool DataDescriptor::LoadMerged(ReadMemoryCallback readMemory, void* context, uint64_t address, int depth, std::string& error)
    {
        if (readMemory == nullptr)
        {
            error = "no read callback";
            return false;
        }
        if (depth > MaxSubDescriptorDepth)
        {
            return true; // stop recursing; not fatal
        }

        // Read the ContractDescriptor header. Same-platform build: pointer size is native.
        const uint32_t ptrSize = (uint32_t)sizeof(void*);
        uint64_t magic = 0;
        uint32_t descriptorSize = 0;
        uint64_t descriptorPtr = 0;
        uint32_t pointerDataCount = 0;
        uint64_t pointerDataPtr = 0;
        if (!readMemory(context, address + 0, &magic, sizeof(magic)) ||
            !readMemory(context, address + 12, &descriptorSize, sizeof(descriptorSize)) ||
            !readMemory(context, address + 16, &descriptorPtr, ptrSize) ||
            !readMemory(context, address + 16 + ptrSize, &pointerDataCount, sizeof(pointerDataCount)) ||
            !readMemory(context, address + 16 + ptrSize + 8, &pointerDataPtr, ptrSize))
        {
            error = "failed to read contract descriptor header";
            return false;
        }

        if (magic != ContractDescriptorMagic)
        {
            error = "bad contract descriptor magic";
            return false;
        }
        if (descriptorSize == 0 || descriptorSize > MaxDescriptorSize)
        {
            error = "descriptor_size out of range";
            return false;
        }

        // Record this descriptor's address (main + each sub-descriptor) so the
        // bootstrap contract can include every descriptor's struct/JSON/pointer_data.
        m_descriptorAddresses.push_back(address);

        // Read the JSON blob and parse it into a temporary descriptor.
        std::vector<char> json(descriptorSize + 1);
        if (!readMemory(context, descriptorPtr, json.data(), descriptorSize))
        {
            error = "failed to read JSON descriptor blob";
            return false;
        }
        json[descriptorSize] = '\0';

        if (depth == 0)
        {
            const char* dumpPath = getenv("CDACLITE_DUMP_JSON");
            if (dumpPath != nullptr)
            {
                FILE* f = fopen(dumpPath, "wb");
                if (f != nullptr)
                {
                    fwrite(json.data(), 1, descriptorSize, f);
                    fclose(f);
                }
            }
        }

        DataDescriptor local;
        if (!local.Parse(json.data(), descriptorSize, error))
        {
            return false;
        }

        // Read pointer_data and resolve this descriptor's indirect globals.
        std::vector<uint64_t> pointerData(pointerDataCount, 0);
        for (uint32_t i = 0; i < pointerDataCount; i++)
        {
            if (!readMemory(context, pointerDataPtr + (uint64_t)i * ptrSize, &pointerData[i], ptrSize))
            {
                error = "failed to read pointer_data entry";
                return false;
            }
        }
        local.ResolveIndirectGlobals(pointerData.empty() ? nullptr : pointerData.data(), pointerDataCount);

        // Merge into the accumulated descriptor (existing entries take precedence).
        Merge(local);

        // Recurse into sub-descriptors. Each is an index into this descriptor's
        // pointer_data; that slot holds the address of a variable whose value is the
        // sub ContractDescriptor's address.
        for (std::map<std::string, GlobalInfo>::const_iterator it = local.m_subDescriptors.begin();
             it != local.m_subDescriptors.end(); ++it)
        {
            uint32_t index = it->second.pointerIndex;
            if (index >= pointerData.size())
            {
                continue;
            }
            uint64_t variableAddr = pointerData[index];
            uint64_t subDescriptorAddr = 0;
            if (variableAddr == 0 ||
                !readMemory(context, variableAddr, &subDescriptorAddr, ptrSize) ||
                subDescriptorAddr == 0)
            {
                continue; // sub-descriptor not populated yet (e.g. GC not initialized)
            }

            std::string subError;
            LoadMerged(readMemory, context, subDescriptorAddr, depth + 1, subError);
            // A failed/absent sub-descriptor is non-fatal; keep what we have.
        }

        return true;
    }

    bool DataDescriptor::Parse(const char* json, size_t length, std::string& error)
    {
        m_types.clear();
        m_globals.clear();
        m_contracts.clear();
        m_subDescriptors.clear();

        json::Value root;
        if (!json::Parse(json, length, root, error))
        {
            return false;
        }
        if (!root.IsObject())
        {
            error = "descriptor root is not an object";
            return false;
        }

        const json::Value* types = root.Find("types");
        if (types != nullptr)
        {
            ParseTypes(*types);
        }

        const json::Value* globals = root.Find("globals");
        if (globals != nullptr)
        {
            ParseGlobals(*globals);
        }

        const json::Value* contracts = root.Find("contracts");
        if (contracts != nullptr)
        {
            ParseContracts(*contracts);
        }

        const json::Value* subDescriptors = root.Find("subDescriptors");
        if (subDescriptors == nullptr)
        {
            subDescriptors = root.Find("sub-descriptors");
        }
        if (subDescriptors != nullptr)
        {
            ParseSubDescriptors(*subDescriptors);
        }

        error.clear();
        return true;
    }
}
