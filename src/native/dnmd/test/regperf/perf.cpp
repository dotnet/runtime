#include <pal.hpp>
#include <internal/dnmd_platform.hpp>
#include <dnmd_interfaces.hpp>
#include <internal/dnmd_tools_platform.hpp>

#include <benchmark/benchmark.h>

#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cassert>
#include <iostream>
#include <filesystem>

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER

#define RETURN_IF_FAILED(x) { auto hr = x; if (FAILED(hr)) return hr; }

namespace
{
    void const* g_data;
    uint32_t g_dataLen;

    IMetaDataDispenser* g_baselineDisp;
    IMetaDataDispenser* g_currentDisp;

    IMetaDataImport* g_baselineImport;
    IMetaDataImport* g_currentImport;

    HRESULT CreateImport(IMetaDataDispenser* disp, IMetaDataImport** import)
    {
        assert(disp != nullptr && import != nullptr);
        return disp->OpenScopeOnMemory(
            g_data,
            g_dataLen,
            CorOpenFlags::ofReadOnly,
            IID_IMetaDataImport,
            reinterpret_cast<IUnknown**>(import));
    }

    HRESULT EnumTypeDefs(IMetaDataImport* import)
    {
        assert(import != nullptr);
        HRESULT hr;
        HCORENUM hcorenum = {};
        uint32_t buffer[1];
        uint32_t count;
        while (S_OK == (hr = import->EnumTypeDefs(&hcorenum, buffer, ARRAY_SIZE(buffer), (ULONG*)&count))
            && count != 0)
        {
        }
        return hr;
    }

    HRESULT GetScopeProps(IMetaDataImport* import)
    {
        assert(import != nullptr);
        WCHAR name[512];
        ULONG nameLen;
        GUID mvid;
        return import->GetScopeProps(name, ARRAY_SIZE(name), &nameLen, &mvid);
    }

    HRESULT EnumUserStrings(IMetaDataImport* import)
    {
        assert(import != nullptr);
        HRESULT hr;
        HCORENUM hcorenum = {};
        uint32_t buffer[1];
        uint32_t count;
        while (S_OK == (hr = import->EnumUserStrings(&hcorenum, buffer, ARRAY_SIZE(buffer), (ULONG*)&count))
            && count != 0)
        {
        }
        return hr;
    }

    HRESULT GetCustomAttributeByName(IMetaDataImport* import)
    {
        assert(import != nullptr);
        mdToken tk = TokenFromRid(2, mdtTypeDef);
        void const* data;
        uint32_t dataLen;
        return import->GetCustomAttributeByName(tk, W("NotAnAttribute"), &data, (ULONG*)&dataLen);
    }
}

HRESULT PerfInitialize(
    void const* data,
    uint32_t dataLen)
{
    if (data == nullptr)
        return E_INVALIDARG;

    g_data = data;
    g_dataLen = dataLen;

    RETURN_IF_FAILED(CreateImport(g_baselineDisp, &g_baselineImport));

    RETURN_IF_FAILED(CreateImport(g_currentDisp, &g_currentImport));

    return S_OK;
}

void CreateImport(benchmark::State& state, IMetaDataDispenser* disp)
{
    IMetaDataImport* import;
    for (auto _ : state)
    {
        if (SUCCEEDED(CreateImport(disp, &import)))
            (void)import->Release();
    }
}

BENCHMARK_CAPTURE(CreateImport, BaselineCreateImport, g_baselineDisp);
BENCHMARK_CAPTURE(CreateImport, CurrentCreateImport, g_currentDisp);

#define IMPORT_BENCHMARK(func) \
    BENCHMARK_CAPTURE(func, Baseline##func, g_baselineImport); \
    BENCHMARK_CAPTURE(func, Current##func, g_currentImport);

void EnumTypeDefs(benchmark::State& state, IMetaDataImport* import)
{
    for (auto _ : state)
    {
        if (FAILED(EnumTypeDefs(import)))
        {
            state.SkipWithError("Failed to enumerate typedefs");
        }
    }
}

IMPORT_BENCHMARK(EnumTypeDefs);

void GetScopeProps(benchmark::State& state, IMetaDataImport* import)
{
    HRESULT hr;
    for (auto _ : state)
    {
        if (FAILED(hr = GetScopeProps(import)))
        {
            state.SkipWithError("Failed to get scope props");
        }
    }
}

IMPORT_BENCHMARK(GetScopeProps);

void EnumUserStrings(benchmark::State& state, IMetaDataImport* import)
{
    HRESULT hr;
    for (auto _ : state)
    {
        if (FAILED(hr = EnumUserStrings(import)))
        {
            state.SkipWithError("Failed to enumerate user strings");
        }
    }
}

IMPORT_BENCHMARK(EnumUserStrings);

void EnumCustomAttributeByName(benchmark::State& state, IMetaDataImport* import)
{
    HRESULT hr;
    for (auto _ : state)
    {
        if (FAILED(hr = GetCustomAttributeByName(import)))
        {
            state.SkipWithError("Failed to get custom attributes");
        }
    }
}

IMPORT_BENCHMARK(EnumCustomAttributeByName);

int main(int argc, char** argv)
{
    RETURN_IF_FAILED(pal::GetBaselineMetadataDispenser(&g_baselineDisp));
    RETURN_IF_FAILED(GetDispenser(IID_IMetaDataDispenser, reinterpret_cast<void**>(&g_currentDisp)));
    auto coreClrPath = pal::GetCoreClrPath();
    if (coreClrPath.empty())
    {
        std::cerr << "Failed to get coreclr path" << std::endl;
        return -1;
    }

    std::filesystem::path dataImagePath = std::move(coreClrPath);
    dataImagePath.replace_filename("System.Private.CoreLib.dll");

    std::cerr << "Loading System.Private.CoreLib from: " << dataImagePath << std::endl;
    
    malloc_span<uint8_t> dataImage;
    if (!pal::ReadFile(dataImagePath, dataImage))
    {
        std::cerr << "Failed to read System.Private.CoreLib" << std::endl;
        return EXIT_FAILURE;
    }

    if (!get_metadata_from_pe(dataImage))
    {
        std::cerr << "Failed to get metadata from System.Private.CoreLib" << std::endl;
        return EXIT_FAILURE;
    }

    RETURN_IF_FAILED(PerfInitialize(
        dataImage,
        (uint32_t)dataImage.size()));

    benchmark::Initialize(&argc, argv);
    benchmark::RunSpecifiedBenchmarks();
    benchmark::Shutdown();
    return EXIT_SUCCESS;
}