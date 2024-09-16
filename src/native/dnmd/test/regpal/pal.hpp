#ifndef _TEST_REGPAL_PAL_H_
#define _TEST_REGPAL_PAL_H_

#include <cstdint>
#include <cstddef>

#include <internal/dnmd_platform.hpp>
#include <string>
#include <internal/span.hpp>

#ifdef BUILD_WINDOWS
#define X(str) W(str)
#else
#define X(str) str
#endif

namespace pal
{
#ifdef BUILD_WINDOWS
    using path = std::wstring;
#else
    using path = std::string;
#endif
    path GetCoreClrPath();
    HRESULT GetBaselineMetadataDispenser(IMetaDataDispenser** dispenser);
    bool ReadFile(path path, malloc_span<uint8_t>& b);

    bool FileExists(path path);

#ifdef BUILD_WINDOWS
    std::wostream& cout();
    std::wostream& cerr();
#else
    std::ostream& cout();
    std::ostream& cerr();
#endif
}

#endif // !_TEST_REGPAL_PAL_H_
