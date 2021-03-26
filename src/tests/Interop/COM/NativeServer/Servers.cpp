// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "Servers.h"

namespace
{
    const WCHAR EntryKeyFmt[] = L"SOFTWARE\\Classes\\CLSID\\%s";

    struct OleStr : public std::unique_ptr<std::remove_pointer<LPOLESTR>::type, decltype(&::CoTaskMemFree)>
    {
        OleStr(_In_ LPOLESTR raw)
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

        WCHAR regKeyServerPath[1024];
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

        WCHAR fullPath[MAX_PATH + 1];

        HMODULE mod;
        if (FALSE == ::GetModuleHandleExW(
            (GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT),
            reinterpret_cast<LPCWSTR>(&RegisterClsid),
            &mod))
        {
            return HRESULT_FROM_WIN32(::GetLastError());
        }

        ::GetModuleFileNameW(mod, fullPath, ARRAYSIZE(fullPath));

        // The default value for the key is the path to the DLL
        res = ::RegSetValueExW(
            regKey.get(),
            nullptr,
            0,
            REG_SZ,
            reinterpret_cast<const BYTE*>(fullPath),
            static_cast<DWORD>(::TP_slen(fullPath) + 1) * sizeof(fullPath[0]));
        if (res != ERROR_SUCCESS)
            return __HRESULT_FROM_WIN32(res);

        // Set the threading model if provided
        if (threadingModel != nullptr)
        {
            res = ::RegSetValueExW(
                regKey.get(),
                L"ThreadingModel",
                0,
                REG_SZ,
                reinterpret_cast<const BYTE*>(threadingModel),
                static_cast<DWORD>(::TP_slen(threadingModel) + 1) * sizeof(threadingModel[0]));
            if (res != ERROR_SUCCESS)
                return __HRESULT_FROM_WIN32(res);
        }

        return S_OK;
    }
}

STDAPI DllRegisterServer(void)
{
    HRESULT hr;

    RETURN_IF_FAILED(RegisterClsid(__uuidof(NumericTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(ArrayTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(StringTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(ErrorMarshalTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(DispatchTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(EventTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(AggregationTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(ColorTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(InspectableTesting), L"Both"));
    RETURN_IF_FAILED(RegisterClsid(__uuidof(TrackMyLifetimeTesting), L"Both"));

    return S_OK;
}

STDAPI DllUnregisterServer(void)
{
    HRESULT hr;

    RETURN_IF_FAILED(RemoveClsid(__uuidof(NumericTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(ArrayTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(StringTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(ErrorMarshalTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(DispatchTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(EventTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(AggregationTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(ColorTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(InspectableTesting)));
    RETURN_IF_FAILED(RemoveClsid(__uuidof(TrackMyLifetimeTesting)));

    return S_OK;
}

STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Out_ LPVOID FAR* ppv)
{
    if (rclsid == __uuidof(NumericTesting))
        return ClassFactoryBasic<NumericTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(ArrayTesting))
        return ClassFactoryBasic<ArrayTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(StringTesting))
        return ClassFactoryBasic<StringTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(ErrorMarshalTesting))
        return ClassFactoryBasic<ErrorMarshalTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(DispatchTesting))
        return ClassFactoryBasic<DispatchTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(EventTesting))
        return ClassFactoryBasic<EventTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(AggregationTesting))
        return ClassFactoryAggregate<AggregationTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(ColorTesting))
        return ClassFactoryBasic<ColorTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(LicenseTesting))
        return ClassFactoryLicense<LicenseTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(InspectableTesting))
        return ClassFactoryBasic<InspectableTesting>::Create(riid, ppv);

    if (rclsid == __uuidof(TrackMyLifetimeTesting))
        return ClassFactoryBasic<TrackMyLifetimeTesting>::Create(riid, ppv);

    return CLASS_E_CLASSNOTAVAILABLE;
}
