#ifndef _TEST_REGFUZZ_BASELINE_H_
#define _TEST_REGFUZZ_BASELINE_H_

#include <internal/dnmd_tools_platform.hpp>
#include <cor.h>
#include <pal.hpp>
#include <filesystem>

namespace TestBaseline
{
    inline IMetaDataDispenser* GetBaseline()
    {
        static minipal::com_ptr<IMetaDataDispenser> dispenser;
        if (dispenser == nullptr)
        {
            pal::GetBaselineMetadataDispenser(&dispenser);
        }
        return dispenser;
    }

    inline malloc_span<uint8_t> GetSeedMetadata()
    {
        malloc_span<uint8_t> b;
        std::filesystem::path baselineModulePath{pal::GetCoreClrPath()};
        if (!pal::ReadFile((baselineModulePath.parent_path() / X("System.Private.CoreLib.dll")).native(), b)
            || !get_metadata_from_pe(b))
        {
            return {};
        }

        return b;
    }
}

#endif // !_TEST_REGFUZZ_BASELINE_H_
