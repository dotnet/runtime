// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            , Len{ ARRAYSIZE(DefBuffer) }
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

    std::wstring GetEnvVar(_In_z_ const WCHAR *env)
    {
        DWORD len = ::GetEnvironmentVariableW(env, nullptr, 0);
        if (len == 0)
            throw __HRESULT_FROM_WIN32(ERROR_ENVVAR_NOT_FOUND);

        PathBuffer<WCHAR> buffer;
        buffer.SetLength(len);
        (void)::GetEnvironmentVariableW(env, buffer, buffer);

        return static_cast<WCHAR *>(buffer.Buf);
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
    HRESULT TryGetEnvVar(_In_z_ const WCHAR *env, _Inout_ std::string &envVar)
    {
        try
        {
            std::wstring envVarLocal = GetEnvVar(env);
            envVar = ConvertWideToUtf8(envVarLocal);
        }
        catch (HRESULT hr)
        {
            return hr;
        }

        return S_OK;
    }
}

HRESULT coreclr::GetCoreClrInstance(_Outptr_ coreclr **instance, _In_opt_z_ const WCHAR *path)
{
    if (s_CoreClrInstance != nullptr)
    {
        *instance = s_CoreClrInstance;
        return S_FALSE;
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
        for (int extIndex = 0; extIndex < ARRAYSIZE(tpaExtensions); extIndex++)
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
    if (_clrInst != nullptr)
    {
        HRESULT hr = _shutdown(_clrInst, _appDomainId);
        assert(SUCCEEDED(hr));
        (void)hr;
    }
}

HRESULT coreclr::Initialize(
    _In_ int propertyCount,
    _In_reads_(propertCount) const char **keys,
    _In_reads_(propertCount) const char **values,
    _In_opt_z_ const char *appDomainName)
{
    if (_clrInst != nullptr)
        return __HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);

    if (appDomainName == nullptr)
        appDomainName = "CoreShim";

    HRESULT hr;
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
