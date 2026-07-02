// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// statics.cpp
//
// Implementation of the contract-bootstrap walk declared in statics.h.
//*****************************************************************************

#include "statics.h"
#include "datadescriptor.h"

#include <map>
#include <string>
#include <vector>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Reports one ContractDescriptor's struct + JSON + pointer_data.
        int EmitDescriptor(const Target& target, uint64_t address, RegionCallback sink, void* sinkContext)
        {
            const uint32_t ptrSize = target.PointerSize();

            // ContractDescriptor layout (contract-descriptor.h):
            //   +0  magic (8)   +8 flags (4)   +12 descriptor_size (4)
            //   +16 descriptor (ptr)  +16+ptr pointer_data_count (4)  +pad  +pointer_data (ptr)
            uint32_t descriptorSize = 0;
            uint64_t descriptorPtr = 0;
            uint32_t pointerDataCount = 0;
            uint64_t pointerDataPtr = 0;
            target.TryReadUInt32(address + 12, descriptorSize);
            target.TryReadPointer(address + 16, descriptorPtr);
            target.TryReadUInt32(address + 16 + ptrSize, pointerDataCount);
            target.TryReadPointer(address + 16 + ptrSize + 8, pointerDataPtr);

            int count = 0;

            // The descriptor struct itself.
            sink(sinkContext, "contract-descriptor", address, 16 + 2 * ptrSize + 8);
            count++;

            // The UTF-8 JSON data descriptor.
            if (descriptorPtr != 0 && descriptorSize != 0)
            {
                sink(sinkContext, "descriptor-json", descriptorPtr, descriptorSize);
                count++;
            }

            // The pointer_data array: holds the addresses of the indirect globals.
            if (pointerDataPtr != 0 && pointerDataCount != 0)
            {
                sink(sinkContext, "pointer-data", pointerDataPtr, (uint64_t)pointerDataCount * ptrSize);
                count++;
            }

            return count;
        }
    }

    int EnumerateStaticRegions(const Target& target, uint64_t contractDescriptorAddr,
                               RegionCallback sink, void* sinkContext)
    {
        if (contractDescriptorAddr == 0)
        {
            return 0;
        }

        int count = 0;
        const uint32_t ptrSize = target.PointerSize();
        const DataDescriptor* descriptor = target.Descriptor();

        // Emit every ContractDescriptor the reader loaded: the main descriptor AND
        // each recursively-loaded sub-descriptor (e.g. the GC descriptor). Without the
        // sub-descriptors, a contract tool reading the dump could follow the
        // sub-descriptor pointer but find no descriptor/JSON/pointer_data there.
        if (descriptor != nullptr && !descriptor->DescriptorAddresses().empty())
        {
            const std::vector<uint64_t>& addresses = descriptor->DescriptorAddresses();
            for (size_t i = 0; i < addresses.size(); i++)
            {
                count += EmitDescriptor(target, addresses[i], sink, sinkContext);
            }
        }
        else
        {
            // Fall back to just the main descriptor if the address list is unavailable.
            count += EmitDescriptor(target, contractDescriptorAddr, sink, sinkContext);
        }

        // The global variable storage referenced by pointer_data. A contract tool
        // reading the dump resolves an indirect global by dereferencing its
        // pointer_data entry, so the memory at each of those addresses must be present.
        // (Merged globals include both main and sub-descriptor globals.)
        if (descriptor != nullptr)
        {
            const std::map<std::string, GlobalInfo>& globals = descriptor->Globals();
            for (std::map<std::string, GlobalInfo>::const_iterator it = globals.begin(); it != globals.end(); ++it)
            {
                const GlobalInfo& global = it->second;
                if (global.isAddress && global.numericValue != 0)
                {
                    sink(sinkContext, "global-var", global.numericValue, ptrSize);
                    count++;
                }
            }
        }

        return count;
    }
}
} // namespace contracts
