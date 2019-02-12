// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "comhost.h"
#include <trace.h>
#include <utils.h>
#include <error_codes.h>

#include <cpprest/json.h>
using namespace web;

using comhost::clsid_map_entry;
using comhost::clsid_map;

namespace
{
    HRESULT string_to_clsid(_In_ const pal::string_t &str, _Out_ CLSID &clsid)
    {
        // If the first character of the GUID is not '{' then COM will
        // attempt to look up the string in the CLSID table.
        if (str[0] == _X('{'))
        {
            if (SUCCEEDED(::CLSIDFromString(str.data(), &clsid)))
                return S_OK;
        }

        return __HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    clsid_map parse_stream(_Inout_ pal::istream_t &json_map_raw)
    {
        skip_utf8_bom(&json_map_raw);

        // Parse JSON
        json::value json_map;
        try
        {
            json_map = json::value::parse(json_map_raw);
        }
        catch (const json::json_exception&)
        {
            trace::error(_X("Embedded .clsidmap format is invalid"));
            throw HResultException{ StatusCode::InvalidConfigFile };
        }

        json::object &json_obj = json_map.as_object();

        // Process JSON and construct a map
        HRESULT hr;
        clsid_map mapping;
        for (std::pair<utility::string_t, json::value> &prop : json_obj)
        {
            CLSID clsidMaybe;
            hr = string_to_clsid(prop.first, clsidMaybe);
            if (FAILED(hr))
            {
                assert(false && "Invalid CLSID");
                continue;
            }

            clsid_map_entry e{};

            json::object &val = prop.second.as_object();
            e.assembly = val.at(_X("assembly")).as_string();
            e.type = val.at(_X("type")).as_string();

            mapping[clsidMaybe] = std::move(e);
        }

        return mapping;
    }

    class memory_buffer : public std::basic_streambuf<pal::istream_t::char_type>
    {
    public:
        memory_buffer(_In_ DWORD dataInBytes, _In_reads_bytes_(dataInBytes) void *data)
        {
            auto raw_begin = reinterpret_cast<pal::istream_t::char_type*>(data);
            setg(raw_begin, raw_begin, raw_begin + (dataInBytes / sizeof(pal::istream_t::char_type)));
        }
    };

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

        memory_buffer resourceBuffer{ size, data };
        pal::istream_t stream{ &resourceBuffer };
        return parse_stream(stream);
    }

    clsid_map get_json_map_from_file()
    {
        pal::string_t map_file_name;
        if (pal::get_own_module_path(&map_file_name))
        {
            map_file_name += _X(".clsidmap");
            if (pal::file_exists(map_file_name))
            {
                pal::ifstream_t file{ map_file_name };
                return parse_stream(file);
            }
        }

        return{};
    }
}

clsid_map comhost::get_clsid_map()
{
    // CLSID map format
    // {
    //      "<clsid>": {
    //          "assembly": <assembly_name>,
    //          "type": <type_name>
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

    return mapping;
}