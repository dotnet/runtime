#ifndef _TEST_REGTEST_FIXTURES_H_
#define _TEST_REGTEST_FIXTURES_H_

#include <gtest/gtest.h>

#include <internal/dnmd_platform.hpp>

#include <vector>
#include <internal/span.hpp>
#include "pal.hpp"

struct MetadataFile final
{
    enum class Kind
    {
        OnDisk,
        Generated
    } kind;

    MetadataFile(Kind kind, std::string pathOrKey, std::string testNameOverride = "")
    : kind(kind), pathOrKey(std::move(pathOrKey)), testNameOverride(testNameOverride) {}

#ifdef BUILD_WINDOWS
    MetadataFile(Kind kind, pal::path pathOrKey, std::string testNameOverride = "")
    : kind(kind), pathOrKey(), testNameOverride(testNameOverride)
    {
        ULONG length = WideCharToMultiByte(CP_UTF8, 0, pathOrKey.c_str(), (int)pathOrKey.size(), nullptr, 0, nullptr, nullptr);
        this->pathOrKey.resize(length);
        WideCharToMultiByte(CP_UTF8, 0, pathOrKey.c_str(), (int)pathOrKey.size(), &this->pathOrKey[0], length, nullptr, nullptr);
    }
#endif

    std::string pathOrKey;
    std::string testNameOverride;

    bool operator==(const MetadataFile& rhs) const noexcept
    {
        return kind == rhs.kind && pathOrKey == rhs.pathOrKey;
    }
};

inline static std::string IndirectionTablesKey = "IndirectionTables";

std::string PrintName(testing::TestParamInfo<MetadataFile> info);

std::vector<MetadataFile> MetadataFilesInDirectory(pal::path directory);

std::vector<MetadataFile> CoreLibFiles();

span<uint8_t> GetMetadataForFile(MetadataFile file);

malloc_span<uint8_t> GetRegressionAssemblyMetadata();

pal::path FindFrameworkInstall(pal::path version);

pal::path GetBaselineDirectory();

void SetBaselineModulePath(pal::path path);

void SetRegressionAssemblyPath(pal::path path);

class RegressionTest : public ::testing::TestWithParam<MetadataFile>
{
protected:
    using TokenList = std::vector<uint32_t>;
};

#endif // !_TEST_REGTEST_FIXTURES_H_
