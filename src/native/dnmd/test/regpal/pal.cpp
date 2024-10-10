#define DNCP_DEFINE_GUID
#include "pal.hpp"

#include <nethost.h>
#include <hostfxr.h>

#ifdef BUILD_WINDOWS
#include <windows.h>
#include <wil/resource.h>
#include <shlwapi.h>
#else
#include <dlfcn.h>
#include <unistd.h>
#endif

#include <iostream>
#include <fstream>

#ifdef BUILD_WINDOWS
#define W_StdString(str) std::wstring{L##str}
#else
#define W_StdString(str) std::string{str}
#endif

#ifdef BUILD_WINDOWS
std::wostream& pal::cout()
{
    return std::wcout;
}
std::wostream& pal::cerr()
{
    return std::wcerr;
}
#else
std::ostream& pal::cout()
{
    return std::cout;
}
std::ostream& pal::cerr()
{
    return std::cerr;
}
#endif

namespace
{
    void* LoadModule(pal::path path)
    {
#ifdef BUILD_WINDOWS
        return ::LoadLibraryW(path.c_str());
#else
        return ::dlopen(path.c_str(), RTLD_LAZY);
#endif
    }

    void* GetSymbol(void* module, char const* name)
    {
#ifdef BUILD_WINDOWS
        return ::GetProcAddress((HMODULE)module, name);
#else
        return ::dlsym(module, name);
#endif
    }

    using MetaDataGetDispenser = HRESULT(STDMETHODCALLTYPE*)(REFCLSID, REFIID, LPVOID*);

    using CoreCLRInitialize = int(STDMETHODCALLTYPE*)(
            char const* exePath,
            char const* appDomainFriendlyName,
            int propertyCount,
            char const** propertyKeys,
            char const** propertyValues,
            void** hostHandle,
            uint32_t* domainId);

    MetaDataGetDispenser LoadGetDispenser()
    {
        auto coreClrPath = pal::GetCoreClrPath();
        if (coreClrPath.empty())
        {
            std::cerr << "Failed to get coreclr path" << std::endl;
            return nullptr;
        }

        auto mod = LoadModule(coreClrPath);
        if (mod == nullptr)
        {
            pal::cerr() << X("Failed to load metadata baseline module: ") << coreClrPath << std::endl;
            return nullptr;
        }
#ifndef BUILD_WINDOWS
        // On non-Windows, the metadata APIs in CoreCLR don't work until the PAL is initialized.
        // Initialize the runtime just enough to load the PAL.
        auto init = (CoreCLRInitialize)GetSymbol(mod, "coreclr_initialize");
        if (init == nullptr)
        {
            pal::cerr() << X("Failed to find coreclr_initialize in module: ") << coreClrPath << std::endl;
            return nullptr;
        }

        char const* propertyKeys[] = { "TRUSTED_PLATFORM_ASSEMBLIES" };
        char const* propertyValues[] = { coreClrPath.c_str() };
        init("regpal", "regpal", 1, propertyKeys, propertyValues, nullptr, nullptr);
#endif

        auto getDispenser = (MetaDataGetDispenser)GetSymbol(mod, "MetaDataGetDispenser");
        if (getDispenser == nullptr)
        {
            pal::cerr() << X("Failed to find MetaDataGetDispenser in module: ") << coreClrPath << std::endl;
            return nullptr;
        }

        return getDispenser;
    }

    MetaDataGetDispenser GetDispenser = LoadGetDispenser();
}

HRESULT pal::GetBaselineMetadataDispenser(IMetaDataDispenser** dispenser)
{
    if (GetDispenser == nullptr)
    {
        return E_FAIL;
    }

    return GetDispenser(CLSID_CorMetaDataDispenser, IID_IMetaDataDispenser, (void**)dispenser);
}

bool pal::ReadFile(pal::path path, malloc_span<uint8_t>& b)
{
#ifdef BUILD_WINDOWS
    wil::unique_handle file{ CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr) };
    if (!file)
        return false;

    DWORD size = GetFileSize(file.get(), nullptr);
    if (size == INVALID_FILE_SIZE)
        return false;

    b = { (uint8_t*)std::malloc(size), size };

    DWORD bytesRead;
    if (!ReadFile(file.get(), b, (DWORD)b.size(), &bytesRead, nullptr))
        return false;

    return bytesRead == b.size();
#else
    struct stat st;
    if (stat(path.c_str(), &st) != 0)
        return false;
    b = { (uint8_t*)std::malloc((size_t)st.st_size), (size_t)st.st_size };

    std::ifstream file{ path, std::ios::binary };

    if (!file)
        return false;

    file.read((char*)(uint8_t*)b, b.size());

    return true;
#endif
}

constexpr int NetHostBufferTooSmall = 0x80008098;

pal::path pal::GetCoreClrPath()
{
    int result = 0;
    size_t bufferSize = 4096;
    std::unique_ptr<char_t[]> hostfxr_path;
    do
    {
        hostfxr_path.reset(new char_t[bufferSize]);
        result = get_hostfxr_path(hostfxr_path.get(), &bufferSize, nullptr);
    } while (result == NetHostBufferTooSmall);

    if (result != 0)
    {
        std::cerr << "Failed to get hostfxr path. Error code: " << std::hex << result << std::dec << std::endl;
        return {};
    }

    pal::path hostFxrPath = hostfxr_path.get();
    void* hostfxrModule = LoadModule(hostfxr_path.get());
    if (hostfxrModule == nullptr)
    {
        cerr() << "Failed to load hostfxr module: " << hostFxrPath << std::endl;
        return {};
    }


    // The hostfxr path is in the form: <dotnet_root>/host/fxr/<version>/hostfxr.dll
    // We need to get the dotnet root, which is 3 levels up
    // We need to do this because hostfxr_get_dotnet_environment_info only returns information
    // for a globally-installed dotnet if we don't pass a path to the dotnet root.
    // The macOS machines on GitHub Actions don't have dotnet globally installed.
#ifdef BUILD_WINDOWS
    pal::path dotnetRoot = hostFxrPath.substr(0, hostFxrPath.find(X("host\\fxr"), 0));
#else
    pal::path dotnetRoot = hostFxrPath.substr(0, hostFxrPath.find(X("host/fxr"), 0));
#endif

    pal::path coreClrPath = {};
    auto getDotnetEnvironmentInfo = (hostfxr_get_dotnet_environment_info_fn)GetSymbol(hostfxrModule, "hostfxr_get_dotnet_environment_info");
    if (getDotnetEnvironmentInfo(
            dotnetRoot.c_str(),
            nullptr,
            [](const hostfxr_dotnet_environment_info* info, void* result_context)
            {
                path& coreClrPath = *(path*)result_context;
                for (size_t i = 0; i < info->framework_count; ++i)
                {
                    if (info->frameworks[i].name == W_StdString("Microsoft.NETCore.App"))
                    {
                        coreClrPath = info->frameworks[i].path;
                        coreClrPath += X('/');
                        coreClrPath += info->frameworks[i].version;
                        coreClrPath += X('/');
#ifdef BUILD_WINDOWS
                        coreClrPath += X("coreclr.dll");
#elif BUILD_MACOS
                        coreClrPath += X("libcoreclr.dylib");
#elif BUILD_UNIX
                        coreClrPath += X("libcoreclr.so");
#else
#error "Unknown platform, cannot determine name for CoreCLR executable"
#endif
                    }
                }
            },
            &coreClrPath
        ) != 0)
    {
        std::cerr << "Failed to get dotnet environment info" << std::endl;
        return {};
    }

    return coreClrPath;
}

bool pal::FileExists(pal::path path)
{
#ifdef BUILD_WINDOWS
    return PathFileExistsW(path.c_str());
#else
    return access(path.c_str(), F_OK) != -1;
#endif
}
