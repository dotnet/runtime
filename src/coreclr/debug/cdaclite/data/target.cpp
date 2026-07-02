// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// target.cpp
//
// Implementation of the Target abstraction declared in target.h.
//*****************************************************************************

#include "target.h"

#include <string.h>

namespace cdac
{
    Target::Target(const DataDescriptor* descriptor,
                   ReadMemoryCallback readMemory,
                   void* readContext)
        : m_descriptor(descriptor)
        , m_readMemory(readMemory)
        , m_readContext(readContext)
    {
    }

    bool Target::ReadBuffer(uint64_t address, void* buffer, uint32_t size) const
    {
        if (m_readMemory == nullptr)
        {
            return false;
        }
        return m_readMemory(m_readContext, address, buffer, size);
    }

    bool Target::TryReadUInt8(uint64_t address, uint8_t& value) const
    {
        return ReadBuffer(address, &value, sizeof(value));
    }

    bool Target::TryReadUInt16(uint64_t address, uint16_t& value) const
    {
        return ReadBuffer(address, &value, sizeof(value));
    }

    bool Target::TryReadUInt32(uint64_t address, uint32_t& value) const
    {
        return ReadBuffer(address, &value, sizeof(value));
    }

    bool Target::TryReadUInt64(uint64_t address, uint64_t& value) const
    {
        return ReadBuffer(address, &value, sizeof(value));
    }

    bool Target::TryReadPointer(uint64_t address, uint64_t& value) const
    {
        // Same-platform build: a pointer is exactly sizeof(void*) bytes, native endianness.
        value = 0;
        return ReadBuffer(address, &value, (uint32_t)sizeof(void*));
    }

    bool Target::TryGetFieldAddress(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& address) const
    {
        if (m_descriptor == nullptr)
        {
            return false;
        }
        uint32_t offset = 0;
        if (!m_descriptor->TryGetFieldOffset(typeName, fieldName, offset))
        {
            return false;
        }
        address = baseAddress + offset;
        return true;
    }

    bool Target::TryReadFieldUInt32(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint32_t& value) const
    {
        uint64_t address = 0;
        return TryGetFieldAddress(baseAddress, typeName, fieldName, address) && TryReadUInt32(address, value);
    }

    bool Target::TryReadFieldUInt64(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& value) const
    {
        uint64_t address = 0;
        return TryGetFieldAddress(baseAddress, typeName, fieldName, address) && TryReadUInt64(address, value);
    }

    bool Target::TryReadFieldPointer(uint64_t baseAddress, const std::string& typeName, const std::string& fieldName, uint64_t& value) const
    {
        uint64_t address = 0;
        return TryGetFieldAddress(baseAddress, typeName, fieldName, address) && TryReadPointer(address, value);
    }

    bool Target::TryGetTypeSize(const std::string& typeName, uint32_t& size) const
    {
        if (m_descriptor == nullptr)
        {
            return false;
        }
        const TypeInfo* type = m_descriptor->FindType(typeName);
        if (type == nullptr || !type->hasSize)
        {
            return false;
        }
        size = type->size;
        return true;
    }

    void Target::EmitStructMemory(const char* typeName, uint64_t address) const
    {
        if (m_enumMemSink == nullptr || typeName == nullptr || m_descriptor == nullptr)
        {
            return;
        }

        // Prefer the descriptor's exact type size (the analog of the DAC's sizeof(type)).
        uint32_t size = 0;
        if (TryGetTypeSize(typeName, size) && size > 0)
        {
            m_enumMemSink(m_enumMemContext, address, size);
            return;
        }

        // Most runtime types are CDAC_TYPE_INDETERMINATE (offsets only, no size). Approximate
        // the footprint from the largest declared field: max(offset) + 8 covers every
        // scalar/pointer field (8 == largest primitive on this 64-bit build). This mirrors the
        // set of bytes any cDAC consumer can read via the descriptor's field offsets.
        const TypeInfo* type = m_descriptor->FindType(typeName);
        if (type == nullptr || type->fields.empty())
        {
            return;
        }
        uint32_t maxEnd = 0;
        for (std::map<std::string, FieldInfo>::const_iterator it = type->fields.begin(); it != type->fields.end(); ++it)
        {
            uint32_t end = it->second.offset + (uint32_t)sizeof(uint64_t);
            if (end > maxEnd)
            {
                maxEnd = end;
            }
        }
        if (maxEnd > 0)
        {
            m_enumMemSink(m_enumMemContext, address, maxEnd);
        }
    }

    bool Target::TryGetGlobalValue(const std::string& name, uint64_t& value) const
    {
        if (m_descriptor == nullptr)
        {
            return false;
        }
        const std::map<std::string, GlobalInfo>& globals = m_descriptor->Globals();
        std::map<std::string, GlobalInfo>::const_iterator it = globals.find(name);
        if (it == globals.end() || !it->second.hasNumericValue)
        {
            return false;
        }
        value = it->second.numericValue;
        return true;
    }

    bool Target::TryReadGlobalPointer(const std::string& name, uint64_t& value) const
    {
        uint64_t address = 0;
        return TryGetGlobalValue(name, address) && TryReadPointer(address, value);
    }

    bool Target::TryReadGlobalUInt32(const std::string& name, uint32_t& value) const
    {
        uint64_t address = 0;
        return TryGetGlobalValue(name, address) && TryReadUInt32(address, value);
    }

    bool Target::TryGetGlobalString(const std::string& name, std::string& value) const
    {
        if (m_descriptor == nullptr)
        {
            return false;
        }
        const std::map<std::string, GlobalInfo>& globals = m_descriptor->Globals();
        std::map<std::string, GlobalInfo>::const_iterator it = globals.find(name);
        if (it == globals.end() || !it->second.isString)
        {
            return false;
        }
        value = it->second.stringValue;
        return true;
    }
}
