// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "comhost.h"
#include <corehost.h>
#include <error_codes.h>
#include <trace.h>
#include <type_traits>

using comhost::clsid_map_entry;
using comhost::clsid_map;

#if defined(_WIN32)

// COM entry points are defined without the __declspec(dllexport) attribute.
// The issue here is that the compiler will throw an error regarding linkage
// redefinion. The solution here is to the use a .def file on Windows.
#define COM_API extern "C"

#else

#define COM_API SHARED_API

#endif // _WIN32

namespace
{
    pal::stringstream_t & get_comhost_error_stream()
    {
        thread_local static pal::stringstream_t comhost_errors;

        return comhost_errors;
    }

    void reset_comhost_error_stream()
    {
        pal::stringstream_t newstream;
        get_comhost_error_stream().swap(newstream);
    }

    void comhost_error_writer(const pal::char_t* msg)
    {
        get_comhost_error_stream() << msg;
    }
}

COM_API HRESULT STDMETHODCALLTYPE DllGetClassObject(
    _In_ REFCLSID rclsid,
    _In_ REFIID riid,
    _Outptr_ LPVOID FAR* ppv)
{
    // Check if the CLSID map contains a mapping
    clsid_map map;
    RETURN_HRESULT_IF_EXCEPT(map = comhost::get_clsid_map());

    clsid_map::const_iterator iter = map.find(rclsid);
    if (iter == std::end(map))
        return CLASS_E_CLASSNOTAVAILABLE;

    HRESULT hr;
    pal::string_t app_path;
    com_activation_fn act;
    {
        reset_comhost_error_stream();
        trace::set_error_writer(comhost_error_writer);

        int ec = get_com_activation_delegate(&app_path, &act);
        if (ec != StatusCode::Success)
        {
            // Create an IErrorInfo instance with the failure data.
            pal::string_t errs = get_comhost_error_stream().str();

            ICreateErrorInfo *cei;
            if (!errs.empty() && SUCCEEDED(::CreateErrorInfo(&cei)))
            {
                if (SUCCEEDED(cei->SetGUID(rclsid)))
                {
                    if (SUCCEEDED(cei->SetDescription((LPOLESTR)errs.c_str())))
                    {
                        IErrorInfo *ei;
                        if (SUCCEEDED(cei->QueryInterface(__uuidof(ei), (void**)&ei)))
                        {
                            ::SetErrorInfo(0, ei);
                            ei->Release();
                        }
                    }
                }

                cei->Release();
            }

            return __HRESULT_FROM_WIN32(ec);
        }
    }

    // Query the CLR for the type

    IUnknown *classFactory = nullptr;
    com_activation_context cxt
    {
        rclsid,
        riid,
        app_path.c_str(),
        iter->second.assembly.c_str(),
        iter->second.type.c_str(),
        (void**)&classFactory
    };
    RETURN_IF_FAILED(act(&cxt));
    assert(classFactory != nullptr);

    hr = classFactory->QueryInterface(riid, ppv);
    classFactory->Release();
    return hr;
}

COM_API HRESULT STDMETHODCALLTYPE DllCanUnloadNow(void)
{
    return S_FALSE;
}

#if defined(_WIN32)

namespace
{
    const WCHAR EntryKeyFmt[] = L"SOFTWARE\\Classes\\CLSID\\%s";

    struct OleStr : public std::unique_ptr<std::remove_pointer<LPOLESTR>::type, decltype(&::CoTaskMemFree)>
    {
        OleStr(_In_z_ LPOLESTR raw)
            : std::unique_ptr<std::remove_pointer<LPOLESTR>::type, decltype(&::CoTaskMemFree)>(raw, ::CoTaskMemFree)
        { }
    };

    struct RegKey : public std::unique_ptr<std::remove_pointer<HKEY>::type, decltype(&::RegCloseKey)>
    {
        RegKey(_In_ HKEY raw)
            : std::unique_ptr<std::remove_pointer<HKEY>::type, decltype(&::RegCloseKey)>(raw, ::RegCloseKey)
        { }
    };

    HRESULT RemoveClsid(_In_ REFCLSID clsid)
    {
        HRESULT hr;

        LPOLESTR clsidAsStrRaw;
        RETURN_IF_FAILED(::StringFromCLSID(clsid, &clsidAsStrRaw));

        OleStr clsidAsStr{ clsidAsStrRaw };

        WCHAR regKeyPath[1024];
        ::swprintf_s(regKeyPath, EntryKeyFmt, clsidAsStr.get());

        LSTATUS res;

        // Handle sub keys
        {
            HKEY toDeleteRaw;
            res = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, regKeyPath, 0, KEY_READ | KEY_WRITE, &toDeleteRaw);
            if (ERROR_FILE_NOT_FOUND == res)
            {
                return S_OK;
            }
            else if (ERROR_SUCCESS != res)
            {
                return __HRESULT_FROM_WIN32(res);
            }

            RegKey toDelete{ toDeleteRaw };
            res = ::RegDeleteTreeW(toDelete.get(), nullptr);
            if (ERROR_SUCCESS != res)
                return __HRESULT_FROM_WIN32(res);
        }

        res = ::RegDeleteKeyW(HKEY_LOCAL_MACHINE, regKeyPath);
        if (ERROR_SUCCESS != res)
            return __HRESULT_FROM_WIN32(res);

        return S_OK;
    }

    HRESULT RegisterClsid(_In_ REFCLSID clsid, _In_opt_z_ const WCHAR *threadingModel)
    {
        HRESULT hr;

        // Remove the CLSID in case it exists and has undesirable settings
        RETURN_IF_FAILED(RemoveClsid(clsid));

        LPOLESTR clsidAsStrRaw;
        RETURN_IF_FAILED(::StringFromCLSID(clsid, &clsidAsStrRaw));

        OleStr clsidAsStr{ clsidAsStrRaw };

        WCHAR regKeyClsidPath[1024];
        ::swprintf_s(regKeyClsidPath, EntryKeyFmt, clsidAsStr.get());

        HKEY regKeyRaw;
        DWORD disp;
        LSTATUS res = ::RegCreateKeyExW(
            HKEY_LOCAL_MACHINE,
            regKeyClsidPath,
            0,
            REG_NONE,
            REG_OPTION_NON_VOLATILE,
            (KEY_READ | KEY_WRITE),
            nullptr,
            &regKeyRaw,
            &disp);
        if (res != ERROR_SUCCESS)
            return __HRESULT_FROM_WIN32(res);

        RegKey regKey{ regKeyRaw };

        WCHAR regKeyServerPath[ARRAYSIZE(regKeyClsidPath) * 2];
        ::swprintf_s(regKeyServerPath, L"%s\\InProcServer32", regKeyClsidPath);

        HKEY regServerKeyRaw;
        res = ::RegCreateKeyExW(
            HKEY_LOCAL_MACHINE,
            regKeyServerPath,
            0,
            REG_NONE,
            REG_OPTION_NON_VOLATILE,
            (KEY_READ | KEY_WRITE),
            nullptr,
            &regServerKeyRaw,
            &disp);
        if (res != ERROR_SUCCESS)
            return __HRESULT_FROM_WIN32(res);

        regKey.reset(regServerKeyRaw);

        HMODULE mod;
        if (FALSE == ::GetModuleHandleExW(
            (GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT),
            reinterpret_cast<LPCWSTR>(&RegisterClsid),
            &mod))
        {
            return HRESULT_FROM_WIN32(::GetLastError());
        }

        pal::string_t modPath;
        if (!pal::get_own_module_path(&modPath))
            return E_UNEXPECTED;

        // The default value for the key is the path to the DLL
        res = ::RegSetValueExW(
            regKey.get(),
            nullptr,
            0,
            REG_SZ,
            reinterpret_cast<const BYTE*>(modPath.data()),
            static_cast<DWORD>(modPath.size() + 1) * sizeof(modPath[0]));
        if (res != ERROR_SUCCESS)
            return __HRESULT_FROM_WIN32(res);

        // Set the threading model if provided
        if (threadingModel != nullptr)
        {
            res = ::RegSetValueExW(
                regKey.get(),
                _X("ThreadingModel"),
                0,
                REG_SZ,
                reinterpret_cast<const BYTE*>(threadingModel),
                static_cast<DWORD>(::wcslen(threadingModel) + 1) * sizeof(threadingModel[0]));
            if (res != ERROR_SUCCESS)
                return __HRESULT_FROM_WIN32(res);
        }

        return S_OK;
    }
}

COM_API HRESULT STDMETHODCALLTYPE DllRegisterServer(void)
{
    // Step 1: Get CLSID mapping
    clsid_map map;
    RETURN_HRESULT_IF_EXCEPT(map = comhost::get_clsid_map());

    // Step 2: Register each CLSID
    HRESULT hr;
    for (clsid_map::const_reference p : map)
        RETURN_IF_FAILED(RegisterClsid(p.first, _X("Both")));

    return S_OK;
}

COM_API HRESULT STDMETHODCALLTYPE DllUnregisterServer(void)
{
    // Step 1: Get CLSID mapping
    clsid_map map;
    RETURN_HRESULT_IF_EXCEPT(map = comhost::get_clsid_map());

    // Step 2: Unregister each CLSID
    HRESULT hr;
    for (clsid_map::const_reference p : map)
        RETURN_IF_FAILED(RemoveClsid(p.first));

    return S_OK;
}

#endif // _WIN32
