#ifndef _TEST_REGTEST_FIXTURES_H_
#define _TEST_REGTEST_FIXTURES_H_

#include <gtest/gtest.h>

#include <internal/dnmd_platform.hpp>

#include <vector>
#include <filesystem>
#include <internal/span.hpp>

struct MetadataFile final
{
    enum class Kind
    {
        OnDisk,
        Generated
    } kind;

    MetadataFile(Kind kind, std::string pathOrKey, std::string testNameOverride = "")
    : kind(kind), pathOrKey(std::move(pathOrKey)), testNameOverride(testNameOverride) {}

    std::string pathOrKey;
    std::string testNameOverride;
    
    bool operator==(const MetadataFile& rhs) const noexcept
    {
        return kind == rhs.kind && pathOrKey == rhs.pathOrKey;
    }
};

inline static std::string IndirectionTablesKey = "IndirectionTables";

std::string PrintName(testing::TestParamInfo<MetadataFile> info);

std::vector<MetadataFile> MetadataFilesInDirectory(std::string directory);

std::vector<MetadataFile> CoreLibFiles();

span<uint8_t> GetMetadataForFile(MetadataFile file);

malloc_span<uint8_t> GetRegressionAssemblyMetadata();

std::string FindFrameworkInstall(std::string version);

std::string GetBaselineDirectory();

void SetBaselineModulePath(std::filesystem::path path);

void SetRegressionAssemblyPath(std::string path);

class RegressionTest : public ::testing::TestWithParam<MetadataFile>
{
protected:
    using TokenList = std::vector<uint32_t>;
};

#endif // !_TEST_REGTEST_FIXTURES_H_