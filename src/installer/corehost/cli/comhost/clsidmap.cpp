// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comhost.h"
#include <cstring>
#include <mutex>
#include <trace.h>
#include <utils.h>
#include <error_codes.h>

#include <wintrust.h>
#include <Softpub.h>

#include "rapidjson/document.h"
#include "rapidjson/istreamwrapper.h"
#include "json_parser.h"

using comhost::clsid_map_entry;
using comhost::clsid_map;

namespace
{
    HRESULT string_to_clsid(_In_ const pal::string_t &str, _Out_ CLSID &clsid)
    {
        pal::char_t guid_buf[] = _X("{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}");
        const pal::char_t *guid_maybe = str.data();

        // If the first character of the GUID is not '{' COM will
        // interpret the string as a ProgID. The COM host doesn't
        // support ProgIDs so coerce strings into GUID format.
        // The buffer size is minus 2 to account for null and first '{'.
        if (str[0] != _X('{')
            && str.size() < ((sizeof(guid_buf) / sizeof(pal::char_t)) - 2))
        {
            // Increment the output buffer 1 to skip over the '{'.
            std::memcpy(guid_buf + 1, str.data(), str.size() * sizeof(pal::char_t));
            guid_maybe = guid_buf;
        }

        if (FAILED(::CLSIDFromString(guid_maybe, &clsid)))
            return __HRESULT_FROM_WIN32(ERROR_INVALID_DATA);

        return S_OK;
    }

    clsid_map get_map_from_json(_In_ const json_parser_t &json)
    {
        // Process JSON and construct a map
        HRESULT hr;
        clsid_map mapping;
        for (const auto &prop : json.document().GetObject())
        {
            CLSID clsidMaybe;
            hr = string_to_clsid(prop.name.GetString(), clsidMaybe);
            if (FAILED(hr))
            {
                assert(false && "Invalid CLSID");
                trace::error(_X("Invalid CLSID format in .clsidmap"));
                continue;
            }

            clsid_map_entry e{};

            e.clsid = clsidMaybe;

            const auto &val = prop.value.GetObject();
            e.assembly = val[_X("assembly")].GetString();
            e.type = val[_X("type")].GetString();

            // Check if a ProgID was defined.
            const auto &prodIdMaybe = val.FindMember(_X("progid"));
            if (prodIdMaybe != val.MemberEnd())
                e.progid = prodIdMaybe->value.GetString();

            mapping[clsidMaybe] = std::move(e);
        }

        return mapping;
    }

    clsid_map get_json_map_from_resource(bool &found_resource)
    {
        found_resource = false;
        HMODULE hMod;
        if (!pal::get_current_module((pal::dll_t*)&hMod))
            return{};

        HRSRC resHandle = ::FindResourceW(hMod, MAKEINTRESOURCEW(RESOURCEID_CLSIDMAP), MAKEINTRESOURCEW(RESOURCETYPE_CLSIDMAP));
        if (resHandle == nullptr)
            return {};

        found_resource = true;
        DWORD size = ::SizeofResource(hMod, resHandle);
        HGLOBAL resData = ::LoadResource(hMod, resHandle);
        if (resData == nullptr || size == 0)
            throw HResultException{ HRESULT_FROM_WIN32(::GetLastError()) };

        LPVOID data = ::LockResource(resData);
        if (data == nullptr)
            throw HResultException{ E_UNEXPECTED }; // This should never happen in Windows 7+

        json_parser_t json;
        if (!json.parse_raw_data(reinterpret_cast<char*>(data), size, _X("<embedded .clsidmap>")))
        {
            trace::error(_X("Embedded .clsidmap format is invalid"));
            throw HResultException{ StatusCode::InvalidConfigFile };
        }

        return get_map_from_json(json);
    }

    bool is_binary_unsigned(const pal::string_t &path)
    {
        // Use the default verifying provider
        GUID policy = WINTRUST_ACTION_GENERIC_VERIFY_V2;

        // File from disk must be used since there is no support for blob verification of a DLL
        // https://docs.microsoft.com/windows/desktop/api/wintrust/ns-wintrust-wintrust_file_info
        WINTRUST_FILE_INFO fileData{};
        fileData.cbStruct = sizeof(WINTRUST_FILE_INFO);
        fileData.pcwszFilePath = path.c_str();
        fileData.hFile = nullptr;
        fileData.pgKnownSubject = nullptr;

        // https://docs.microsoft.com/windows/desktop/api/wintrust/ns-wintrust-_wintrust_data
        WINTRUST_DATA trustData{};
        trustData.cbStruct = sizeof(trustData);
        trustData.pPolicyCallbackData = nullptr;
        trustData.pSIPClientData = nullptr;
        trustData.dwUIChoice = WTD_UI_NONE;
        trustData.fdwRevocationChecks = WTD_REVOKE_NONE;
        trustData.dwUnionChoice = WTD_CHOICE_FILE;
        trustData.dwStateAction = WTD_STATEACTION_VERIFY;
        trustData.hWVTStateData = nullptr;
        trustData.pwszURLReference = nullptr;
        trustData.dwProvFlags = 0;
        trustData.dwUIContext = 0;
        trustData.pFile = &fileData;

        // https://docs.microsoft.com/windows/desktop/api/wintrust/nf-wintrust-winverifytrust
        LONG res = ::WinVerifyTrust(nullptr, &policy, &trustData);
        const DWORD err = ::GetLastError();
        if (trustData.hWVTStateData != nullptr)
        {
            // The verification provider did something, so it must be closed.
            trustData.dwStateAction = WTD_STATEACTION_CLOSE;
            (void)::WinVerifyTrust(nullptr, &policy, &trustData);
        }

        // Success indicates the signature was verified
        if (res == ERROR_SUCCESS)
            return false;

        // The only acceptable error code for indicating not-signed
        // is going to be the explicit 'no signature' error code.
        // The 'TRUST_E_NOSIGNATURE' result from the function call
        // indicates a category of issues rather than 'no signature'.
        // When the 'TRUST_E_NOSIGNATURE' error code is returned from
        // 'GetLastError()' the indication is actually 'no signature'.
        return (err == TRUST_E_NOSIGNATURE);
    }

    clsid_map get_json_map_from_file()
    {
        pal::string_t this_module;
        if (!pal::get_own_module_path(&this_module))
            return {};

        if (!is_binary_unsigned(this_module))
        {
            trace::verbose(_X("Binary is signed, disabling loose .clsidmap file discovery"));
            return {};
        }

        pal::string_t map_file_name = std::move(this_module);
        map_file_name += _X(".clsidmap");
        if (!pal::file_exists(map_file_name))
            return {};

        json_parser_t json;
        if (!json.parse_file(map_file_name))
        {
            trace::error(_X("File .clsidmap format is invalid"));
            throw HResultException{ StatusCode::InvalidConfigFile };
        }

        return get_map_from_json(json);
    }
}

clsid_map comhost::get_clsid_map()
{
    static pal::mutex_t static_map_lock;
    static bool static_map_set = false;
    static clsid_map static_map{};

    std::lock_guard<pal::mutex_t> lock{ static_map_lock };
    if (static_map_set)
        return static_map;

    // CLSID map format
    // {
    //      "<clsid>": {
    //          "assembly": <assembly_name>,
    //          "type": <type_name>,
    //          "progid": <prog_id> [Optional]
    //      },
    //      ...
    // }

    // If a mapping as a resource was found, we don't
    // want to fall back to looking on disk.
    bool found_resource;

    // Find the JSON data that describes the CLSID mapping
    clsid_map mapping = get_json_map_from_resource(found_resource);
    if (!found_resource && mapping.empty())
    {
        trace::verbose(_X("JSON map resource stream not found"));

        mapping = get_json_map_from_file();
        if (mapping.empty())
            trace::verbose(_X("JSON map .clsidmap file not found"));
    }

    // Make a copy to retain
    static_map = mapping;
    static_map_set = true;
    return mapping;
}
