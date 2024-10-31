#ifndef _TEST_REGPAL_PAL_H_
#define _TEST_REGPAL_PAL_H_

#include <cstdint>
#include <cstddef>

#include <internal/dnmd_platform.hpp>
#include <filesystem>
#include <internal/span.hpp>

namespace pal
{
    std::filesystem::path GetCoreClrPath();
    HRESULT GetBaselineMetadataDispenser(IMetaDataDispenser** dispenser);
    bool ReadFile(std::filesystem::path path, malloc_span<uint8_t>& b);
}

#endif // !_TEST_REGPAL_PAL_H_