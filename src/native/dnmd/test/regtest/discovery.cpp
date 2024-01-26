#include "fixtures.h"
#include "baseline.h"
#include <pal.hpp>
#include <filesystem>
#include <algorithm>
#include <unordered_map>

#ifdef BUILD_WINDOWS
#include <wil/registry.h>
#else
#define THROW_IF_FAILED(x) do { HRESULT hr = (x); if (FAILED(hr)) { throw std::runtime_error("Failed HR when running '" #x "'"); } } while (false)
#endif

#include <internal/dnmd_tools_platform.hpp>

#ifdef BUILD_WINDOWS
#define DNNE_API_OVERRIDE __declspec(dllimport)
#endif

namespace
{
    std::filesystem::path baselinePath;
    std::string regressionAssemblyPath;

    template<typename T>
    struct OnExit
    {
        T callback;
        ~OnExit()
        {
            callback();
        }
    };

    template<typename T>
    [[nodiscard]] OnExit<T> on_scope_exit(T callback)
    {
        return { callback };
    }

    malloc_span<uint8_t> ReadMetadataFromFile(std::filesystem::path path)
    {
        malloc_span<uint8_t> b;
        if (!pal::ReadFile(path, b)
            || !get_metadata_from_pe(b))
        {
            return {};
        }

        return b;
    }

    // Create an image with indirection tables, like an image that has had a delta applied to it.
    // This is used to test that the importer can handle out-of-order rows.
    // This image is intentinally minimal as our other regression tests cover more full-filled metadata scenarios.
    malloc_span<uint8_t> CreateImageWithIndirectionTables()
    {
        std::cout << "Creating image with indirection tables" << std::endl;
        dncp::com_ptr<IMetaDataEmit> image;
        THROW_IF_FAILED(TestBaseline::DeltaMetadataBuilder->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&image));

        THROW_IF_FAILED(image->SetModuleProps(W("IndirectionTables.dll")));

        dncp::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
        THROW_IF_FAILED(image->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));

        ASSEMBLYMETADATA assemblyMetadata = { 0 };

        mdAssemblyRef systemRuntimeRef;
        THROW_IF_FAILED(assemblyEmit->DefineAssemblyRef(nullptr, 0, W("System.Runtime"), &assemblyMetadata, nullptr, 0, 0, &systemRuntimeRef));

        mdTypeRef systemObject;
        THROW_IF_FAILED(image->DefineTypeRefByName(systemRuntimeRef, W("System.Object"), &systemObject));
        
        // Define two types so we can define out-of-order rows.
        mdTypeDef type1;
        THROW_IF_FAILED(image->DefineTypeDef(W("Type1"), tdSealed, systemObject, nullptr, &type1));

        mdTypeDef type2;
        THROW_IF_FAILED(image->DefineTypeDef(W("Type2"), tdSealed, systemObject, nullptr, &type2));

        // Define a signature that has two parameters and a return type.
        // This will provide us with enough structure to define out-of-order Param rows.
        std::array signature = { (uint8_t)IMAGE_CEE_CS_CALLCONV_DEFAULT, (uint8_t)0x02, (uint8_t)ELEMENT_TYPE_I4, (uint8_t)ELEMENT_TYPE_I2, (uint8_t)ELEMENT_TYPE_I8};

        mdMethodDef method1;
        THROW_IF_FAILED(image->DefineMethod(type1, W("Method1"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method1));

        mdParamDef param1;
        THROW_IF_FAILED(image->DefineParam(method1, 2, W("Param2"), 0, 0, nullptr, 0, &param1));

        // Define the Param row for the first parameter after we've already defined the second parameter.
        mdParamDef paramOutOfOrder;
        THROW_IF_FAILED(image->DefineParam(method1, 1, W("Param1"), 0, 0, nullptr, 0, &paramOutOfOrder));

        mdMethodDef method2;
        THROW_IF_FAILED(image->DefineMethod(type2, W("Method2"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method2));
        
        // Define a method on the first type after we've already defined a method on the second type.
        mdMethodDef methodOutOfOrder;
        THROW_IF_FAILED(image->DefineMethod(type1, W("MethodOutOfOrder"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &methodOutOfOrder));

        std::array fieldSignature = { (uint8_t)IMAGE_CEE_CS_CALLCONV_FIELD, (uint8_t)ELEMENT_TYPE_I4 };

        mdFieldDef field1;
        THROW_IF_FAILED(image->DefineField(type1, W("Field1"), 0, fieldSignature.data(), (ULONG)fieldSignature.size(), 0, nullptr, 0, &field1));

        mdFieldDef field2;
        THROW_IF_FAILED(image->DefineField(type2, W("Field2"), 0, fieldSignature.data(), (ULONG)fieldSignature.size(), 0, nullptr, 0, &field2));

        // Define a field on the first type after we've already defined a field on the second type.
        mdFieldDef fieldOutOfOrder;
        THROW_IF_FAILED(image->DefineField(type1, W("FieldOutOfOrder"), 0, fieldSignature.data(), (ULONG)fieldSignature.size(), 0, nullptr, 0, &fieldOutOfOrder));

        std::array propertySignature = { (uint8_t)IMAGE_CEE_CS_CALLCONV_PROPERTY, (uint8_t)ELEMENT_TYPE_I4 };
        std::array getterSignature = { (uint8_t)IMAGE_CEE_CS_CALLCONV_DEFAULT, (uint8_t)0x00, (uint8_t)ELEMENT_TYPE_I4 };
        
        mdMethodDef getter1;
        THROW_IF_FAILED(image->DefineMethod(type1, W("get_Property1"), 0, getterSignature.data(), (ULONG)getterSignature.size(), 0, 0, &getter1));

        mdProperty property1;
        THROW_IF_FAILED(image->DefineProperty(type1, W("Property1"), 0, propertySignature.data(), (ULONG)propertySignature.size(), 0, nullptr, 0, getter1, mdMethodDefNil, nullptr, &property1));

        mdMethodDef getter2;
        THROW_IF_FAILED(image->DefineMethod(type2, W("get_Property2"), 0, getterSignature.data(), (ULONG)getterSignature.size(), 0, 0, &getter2));

        mdProperty property2;
        THROW_IF_FAILED(image->DefineProperty(type2, W("Property2"), 0, propertySignature.data(), (ULONG)propertySignature.size(), 0, nullptr, 0, getter2, mdMethodDefNil, nullptr, &property2));

        // Define a property on the first type after we've already defined a property on the second type.
        mdProperty propertyOutOfOrder;
        THROW_IF_FAILED(image->DefineProperty(type1, W("PropertyOutOfOrder"), 0, propertySignature.data(), (ULONG)propertySignature.size(), 0, nullptr, 0, mdMethodDefNil, mdMethodDefNil, nullptr, &propertyOutOfOrder));

        mdTypeRef eventHandlerRef;
        THROW_IF_FAILED(image->DefineTypeRefByName(systemRuntimeRef, W("System.EventHandler"), &eventHandlerRef));
        
        mdEvent event1;
        THROW_IF_FAILED(image->DefineEvent(type1, W("Event1"), 0, eventHandlerRef, mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, nullptr, &event1));

        mdEvent event2;
        THROW_IF_FAILED(image->DefineEvent(type2, W("Event2"), 0, eventHandlerRef, mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, nullptr, &event2));

        // Define an event on the first type after we've already defined an event on the second type.
        mdEvent eventOutOfOrder;
        THROW_IF_FAILED(image->DefineEvent(type1, W("EventOutOfOrder"), 0, eventHandlerRef, mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, nullptr, &eventOutOfOrder));

        ULONG size;
        THROW_IF_FAILED(image->GetSaveSize(cssAccurate, &size));

        malloc_span<uint8_t> imageWithIndirectionTables{ (uint8_t*)malloc(size), size };
        THROW_IF_FAILED(image->SaveToMemory(imageWithIndirectionTables, size));

        return imageWithIndirectionTables;
    }

    malloc_span<uint8_t> GetMetadataFromKey(std::string key)
    {
        if (key == IndirectionTablesKey)
        {
            return CreateImageWithIndirectionTables();
        }
        return {};
    }
}

std::vector<MetadataFile> MetadataFilesInDirectory(std::string directory)
{
    std::cout << "Discovering metadata files in directory: " << directory << std::endl;
    std::vector<MetadataFile> scenarios;

    if (!std::filesystem::exists(directory))
    {
        std::cout << "Directory '" << directory << "' does not exist" << std::endl;
        return scenarios;
    }

    for (auto& entry : std::filesystem::directory_iterator(directory))
    {
        if (entry.is_regular_file())
        {
            auto path = entry.path();
            auto ext = path.extension();
            if (ext == ".dll")
            {
                // Some of the DLLs in our search paths are native,
                // so we need to filter to the managed ones.
                // We could try opening them and skip them if they don't have any metadata,
                // but that is slow and we don't want to do that for test discovery.
                // Instead, we'll use the following heuristic to determine if the DLL is managed:
                // - If the file name contains '.Native.', then it's not managed
                // - If the file name contains '.Thunk.', then it's not managed
                // - If the file name starts with 'System.' or 'Microsoft.', then it's managed

                auto fileName = path.filename().generic_string();

                if (fileName.find(".Native.") != std::string::npos
                    || fileName.find(".Thunk.") != std::string::npos)
                {
                    continue;
                }

                if (fileName.find("System.") != 0
                    && fileName.find("Microsoft.") != 0)
                {
                    continue;
                }

#ifdef BUILD_WINDOWS
                std::wcout << "Found file: " << entry.path().filename() << std::endl;
#else
                std::cout << "Found file: " << entry.path().filename() << std::endl;
#endif

                scenarios.emplace_back(MetadataFile::Kind::OnDisk, path.generic_string());
            }
        }
    }

    return scenarios;
}

std::vector<MetadataFile> CoreLibFiles()
{
    std::cout << "Discovering CoreLib files" << std::endl;
    std::vector<MetadataFile> scenarios;

    scenarios.emplace_back(MetadataFile::Kind::OnDisk, (baselinePath.parent_path() / "System.Private.CoreLib.dll").generic_string(), "System_Private_CoreLib");

#ifdef BUILD_WINDOWS
    scenarios.emplace_back(MetadataFile::Kind::OnDisk, (std::filesystem::path(FindFrameworkInstall("v4.0.30319")) / "mscorlib.dll").generic_string(), "4_0_mscorlib");

    auto fx2mscorlib = std::filesystem::path(FindFrameworkInstall("v2.0.50727")) / "mscorlib.dll";
    if (std::filesystem::exists(fx2mscorlib))
    {
        scenarios.emplace_back(MetadataFile::Kind::OnDisk, fx2mscorlib.generic_string(), "2_0_mscorlib");
    }
#endif
    return scenarios;
}

namespace
{
    std::mutex metadataCacheMutex;

    struct MetadataFileHash
    {
        size_t operator()(const MetadataFile& file) const
        {
            return std::hash<std::string>{}(file.pathOrKey);
        }
    };

    std::unordered_map<MetadataFile, malloc_span<uint8_t>, MetadataFileHash> metadataCache;
}

span<uint8_t> GetMetadataForFile(MetadataFile file)
{
    std::lock_guard<std::mutex> lock{ metadataCacheMutex };
    auto it = metadataCache.find(file);
    if (it != metadataCache.end())
    {
        return it->second;
    }

    malloc_span<uint8_t> b;
    if (file.kind == MetadataFile::Kind::OnDisk)
    {
        auto path = baselinePath.parent_path() / file.pathOrKey;
        b = ReadMetadataFromFile(path);
    }
    else
    {
        b = GetMetadataFromKey(file.pathOrKey.c_str());
    }

    if (b.size() == 0)
    {
        return {};
    }

    span<uint8_t> spanToReturn = b;

    [[maybe_unused]] auto [_, inserted] = metadataCache.emplace(std::move(file), std::move(b));
    assert(inserted);
    return spanToReturn;
}

std::string PrintName(testing::TestParamInfo<MetadataFile> info)
{
    if (info.param.testNameOverride.size() > 0)
    {
        return info.param.testNameOverride;
    }
    std::string name;
    if (info.param.kind == MetadataFile::Kind::OnDisk)
    {
        name = std::filesystem::path(info.param.pathOrKey).stem().generic_string();
        std::replace(name.begin(), name.end(), '.', '_');
    }
    else
    {
        name = info.param.pathOrKey + "_InMemory";
    }
    return name;
}

std::string GetBaselineDirectory()
{
    return baselinePath.parent_path().string();
}

void SetBaselineModulePath(std::filesystem::path path)
{
    baselinePath = std::move(path);
}

void SetRegressionAssemblyPath(std::string path)
{
    regressionAssemblyPath = path;
}

malloc_span<uint8_t> GetRegressionAssemblyMetadata()
{
    return ReadMetadataFromFile(regressionAssemblyPath);
}

std::string FindFrameworkInstall(std::string version)
{
    std::cout << "Discovering framework install for version: " << version << std::endl;
#ifdef BUILD_WINDOWS
    auto key = wil::reg::create_unique_key(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\.NETFramework");
    std::filesystem::path installPath{ wil::reg::get_value_string(key.get(), L"InstallRoot") };
    return (installPath / version).generic_string();
#else
    return {};
#endif
}