#include "error_codes.h"
#include "host_no_context_test.h"

bool host_no_context_test::get_test_result(const pal::char_t* export_fn, const pal::string_t& hostfxr_path)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    if (pal::strcmp(export_fn, _X("get_prop_value")) == 0)
    {
        return host_no_context_test::get_hostfxr_runtime_property_value(hostfxr);
    }
    else if (pal::strcmp(export_fn, _X("get_properties")) == 0)
    {
        return host_no_context_test::get_hostfxr_runtime_properties(hostfxr);
    }
    else
    {
        std::cerr << "Invalid export function" << std::endl;
    }
}

bool host_no_context_test::get_hostfxr_runtime_property_value(hostfxr_exports& hostfxr)
{
    const pal::char_t* value;
    int rc = hostfxr.get_prop_value(nullptr, value, &value);

    if (rc != StatusCode::HostInvalidState)
    {
        std::cerr << "hostfxr_get_runtime_property_value returned unexpected status code: "
            << std::hex << std::showbase << rc << std::endl;
        return false;
    }

    if (value != nullptr)
    {
        std::cerr << "hostfxr_get_runtime_property_value did not set `value` to nullptr" << std::endl;
        return false;
    }

    return true;
}

bool host_no_context_test::get_hostfxr_runtime_properties(hostfxr_exports& hostfxr)
{
    size_t count = 0;
    const pal::char_t* values;
    int rc = hostfxr.get_properties(nullptr, &count, &values, &values);

    if (rc != StatusCode::HostInvalidState)
    {
        std::cerr << "hostfxr_get_runtime_properties returned unexpected status code: "
            << std::hex << std::showbase << rc << std::endl;
        return false;
    }

    if (values != nullptr)
    {
        std::cerr << "hostfxr_get_runtime_properties did not set `values` to nullptr" << std::endl;
        return false;
    }

    return true;
}
