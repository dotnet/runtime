// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CoreShim.h"

#include <set>
#include <sstream>
#include <vector>
#include <mutex>

namespace
{
    template<typename CT>
    struct PathBuffer
    {
        PathBuffer()
            : DefBuffer{}
            , Buf{ DefBuffer }
            , Len{ ARRAY_SIZE(DefBuffer) }
        { }

        void SetLength(_In_ DWORD len)
        {
            if (len > Len)
            {
                Buf = BigBuffer.data();
                Len = static_cast<DWORD>(BigBuffer.size());
            }
        }

        void ExpandBuffer(_In_ DWORD factor = 2)
        {
            SetLength(Len * factor);
        }

        operator DWORD()
        {
            return Len;
        }

        operator CT *()
        {
            return Buf;
        }

        CT DefBuffer[MAX_PATH];
        std::vector<CT> BigBuffer;

        CT *Buf;
        DWORD Len;
    };

    std::wstring GetExePath()
    {
        PathBuffer<WCHAR> buffer;
        DWORD len = ::GetModuleFileNameW(nullptr, buffer, buffer);
        while (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            buffer.ExpandBuffer();
            len = ::GetModuleFileNameW(nullptr, buffer, buffer);
        }

        return std::wstring{ buffer.Buf, buffer.Buf + len };
    }

    bool TryGetEnvVar(_In_z_ const WCHAR* env, _Inout_ std::wstring& value)
    {
        DWORD len = ::GetEnvironmentVariableW(env, nullptr, 0);
        if (len == 0)
            return false;

        PathBuffer<WCHAR> buffer;
        buffer.SetLength(len);
        (void)::GetEnvironmentVariableW(env, buffer, buffer);

        value = static_cast<WCHAR *>(buffer.Buf);
        return true;
    }

    std::wstring GetEnvVar(_In_z_ const WCHAR *env)
    {
        std::wstring value;
        if (!TryGetEnvVar(env, value))
        {
            throw __HRESULT_FROM_WIN32(ERROR_ENVVAR_NOT_FOUND);
        }
        return value;
    }

    std::string ConvertWideToUtf8(_In_ const std::wstring &wide)
    {
        // [TODO] Properly convert to UTF-8
        std::string narrow;
        for (WCHAR p : wide)
            narrow.push_back(static_cast<CHAR>(p));

        return narrow;
    }

    coreclr *s_CoreClrInstance;
}

namespace Utility
{
    HRESULT TryGetEnvVar(_In_z_ const WCHAR *env, _Inout_ std::wstring &envVar)
    {
        try
        {
            envVar = GetEnvVar(env);
        }
        catch (HRESULT hr)
        {
            return hr;
        }

        return S_OK;
    }

    HRESULT GetCoreShimDirectory(_Inout_ std::wstring &dir)
    {
        HMODULE hModule;
        BOOL res = ::GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(&TryGetEnvVar),
            &hModule);
        if (res == FALSE)
            return HRESULT_FROM_WIN32(::GetLastError());

        std::wstring path;
        size_t dwModuleFileName = MAX_PATH / 2;

        do
        {
            path.resize(dwModuleFileName * 2);
            dwModuleFileName = GetModuleFileNameW(hModule, (LPWSTR)path.data(), static_cast<DWORD>(path.size()));
        } while (dwModuleFileName == path.size());

        if (dwModuleFileName == 0)
            return HRESULT_FROM_WIN32(::GetLastError());

        size_t idx = path.find_last_of(W('\\'));
        if (idx == std::wstring::npos)
            return E_UNEXPECTED;

        path.resize(idx + 1);
        dir = std::move(path);
        return S_OK;
    }

    HRESULT GetCoreShimDirectory(_Inout_ std::string &dir)
    {
        HRESULT hr;

        std::wstring dir_wide;
        RETURN_IF_FAILED(GetCoreShimDirectory(dir_wide));

        dir = ConvertWideToUtf8(dir_wide);

        return S_OK;
    }
}

bool TryLoadHostPolicy(const WCHAR* hostPolicyPath)
{
    const WCHAR *hostpolicyName = W("hostpolicy.dll");
    HMODULE hMod = ::GetModuleHandleW(hostpolicyName);
    if (hMod != nullptr)
    {
        return true;
    }

    // Check if a hostpolicy exists and if it does, load it.
    if (INVALID_FILE_ATTRIBUTES != ::GetFileAttributesW(hostPolicyPath))
    {
        hMod = ::LoadLibraryExW(hostPolicyPath, nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        if (hMod == nullptr)
            return false;

        // Initialize the hostpolicy mock to a default state
        using Set_corehost_resolve_component_dependencies_Values_fn = void(__cdecl *)(
            int returnValue,
            const WCHAR *assemblyPaths,
            const WCHAR *nativeSearchPaths,
            const WCHAR *resourceSearchPaths);
        auto set_comp_depend_values = (Set_corehost_resolve_component_dependencies_Values_fn)
            ::GetProcAddress(hMod, "Set_corehost_resolve_component_dependencies_Values");

        assert(set_comp_depend_values != nullptr);
        set_comp_depend_values(0, W(""), W(""), W(""));
    }
    return true;
}

HRESULT coreclr::GetCoreClrInstance(_Outptr_ coreclr **instance, _In_opt_z_ const WCHAR *path)
{
    if (s_CoreClrInstance != nullptr)
    {
        *instance = s_CoreClrInstance;
        return S_FALSE;
    }

    const wchar_t* mockHostPolicyEnvVar = W("MOCK_HOSTPOLICY");
    std::wstring hostPolicyPath;

    if (TryGetEnvVar(mockHostPolicyEnvVar, hostPolicyPath))
    {
        if (!TryLoadHostPolicy(hostPolicyPath.c_str()))
        {
            return E_UNEXPECTED;
        }
    }


    try
    {
        std::wstring pathLocal;
        if (path == nullptr)
        {
            pathLocal = GetEnvVar(W("CORE_ROOT"));
        }
        else
        {
            pathLocal = { path };
        }

        pathLocal.append(W("\\coreclr.dll"));

        AutoModule hmod = ::LoadLibraryExW(pathLocal.c_str() , nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        if (hmod == nullptr)
            return HRESULT_FROM_WIN32(::GetLastError());

        s_CoreClrInstance = new coreclr{ std::move(hmod) };
    }
    catch (HRESULT hr)
    {
        return hr;
    }
    catch (const std::bad_alloc&)
    {
        return E_OUTOFMEMORY;
    }

    *instance = s_CoreClrInstance;
    return S_OK;
}

HRESULT coreclr::CreateTpaList(_Inout_ std::string &tpaList, _In_opt_z_ const WCHAR *dir)
{
    assert(tpaList.empty());

    // Represents priority order
    static const WCHAR * const tpaExtensions[] =
    {
        W(".ni.dll"),
        W(".dll"),
        W(".ni.exe"),
        W(".exe"),
    };

    try
    {
        std::wstring w_dirLocal;
        if (dir == nullptr)
        {
            w_dirLocal = GetEnvVar(W("CORE_ROOT"));
        }
        else
        {
            w_dirLocal = { dir };
        }

        std::string dirLocal = ConvertWideToUtf8(w_dirLocal);
        w_dirLocal.append(W("\\*"));

        std::set<std::wstring> addedAssemblies;
        std::stringstream tpaStream;

        // Walk the directory for each extension separately so assembly types
        // are discovered in priority order - see above.
        for (int extIndex = 0; extIndex < ARRAY_SIZE(tpaExtensions); extIndex++)
        {
            const WCHAR* ext = tpaExtensions[extIndex];
            size_t extLength = ::wcslen(ext);

            WIN32_FIND_DATAW ffd;
            AutoFindFile sh = ::FindFirstFileW(w_dirLocal.c_str(), &ffd);
            if (sh == nullptr)
                break;

            // For all entries in the directory
            do
            {
                // Only examine non-directory entries
                if (!(ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
                {
                    std::wstring filename{ ffd.cFileName };

                    // Check if the extension matches
                    int extPos = static_cast<int>(filename.length() - extLength);
                    if ((extPos <= 0) || (filename.compare(extPos, extLength, ext) != 0))
                    {
                        continue;
                    }

                    std::wstring filenameWithoutExt{ filename.substr(0, extPos) };

                    // Only one type of a particular assembly instance should be inserted
                    // See extension list above.
                    if (addedAssemblies.find(filenameWithoutExt) == std::end(addedAssemblies))
                    {
                        addedAssemblies.insert(std::move(filenameWithoutExt));

                        std::string filename_utf8 = ConvertWideToUtf8(filename);
                        tpaStream << dirLocal << "\\" << filename_utf8 << ";";
                    }
                }
            } while (::FindNextFileW(sh, &ffd) != FALSE);
        }

        tpaList = tpaStream.str();
    }
    catch (HRESULT hr)
    {
        return hr;
    }
    catch (const std::bad_alloc&)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

coreclr::coreclr(_Inout_ AutoModule hmod)
    : _hmod{ std::move(hmod) }
    , _attached{ false }
    , _clrInst{ nullptr }
    , _appDomainId{ std::numeric_limits<uint32_t>::max() }
{
    _initialize = (decltype(_initialize))::GetProcAddress(_hmod, "coreclr_initialize");
    assert(_initialize != nullptr);

    _create_delegate = (decltype(_create_delegate))::GetProcAddress(_hmod, "coreclr_create_delegate");
    assert(_create_delegate != nullptr);

    _shutdown = (decltype(_shutdown))::GetProcAddress(_hmod, "coreclr_shutdown");
    assert(_shutdown != nullptr);
}

coreclr::~coreclr()
{
    if (_clrInst != nullptr && !_attached)
    {
        HRESULT hr = _shutdown(_clrInst, _appDomainId);
        assert(SUCCEEDED(hr));
        (void)hr;
    }
}

HRESULT coreclr::Initialize(
    _In_ int propertyCount,
    _In_reads_(propertyCount) const char **keys,
    _In_reads_(propertyCount) const char **values,
    _In_opt_z_ const char *appDomainName)
{
    if (_clrInst != nullptr)
        return __HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);

    if (appDomainName == nullptr)
        appDomainName = "CoreShim";

    HRESULT hr;

    // Check if this is hosted scenario - launched via CoreRun.exe
    HMODULE mod = ::GetModuleHandleW(W("CoreRun.exe"));
    if (mod != NULL)
    {
        using GetCurrentClrDetailsFunc = HRESULT(__cdecl *)(void **clrInstance, unsigned int *appDomainId);
        auto getCurrentClrDetails = (GetCurrentClrDetailsFunc)::GetProcAddress(mod, "GetCurrentClrDetails");
        RETURN_IF_FAILED(getCurrentClrDetails(&_clrInst, &_appDomainId));
        if (_clrInst != nullptr)
        {
            _attached = true;
            return S_OK;
        }
    }

    try
    {
        const std::wstring exePathW = GetExePath();
        const std::string exePath = ConvertWideToUtf8(exePathW);
        RETURN_IF_FAILED(_initialize(exePath.c_str(), appDomainName, propertyCount, keys, values, &_clrInst, &_appDomainId));
    }
    catch (const std::bad_alloc&)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

HRESULT coreclr::CreateDelegate(
    _In_z_ const char *assembly,
    _In_z_ const char *type,
    _In_z_ const char *method,
    _Out_ void **del)
{
    if (_clrInst == nullptr)
        return E_NOT_VALID_STATE;

    HRESULT hr;
    RETURN_IF_FAILED(_create_delegate(_clrInst, _appDomainId, assembly, type, method, del));

    return S_OK;
}
