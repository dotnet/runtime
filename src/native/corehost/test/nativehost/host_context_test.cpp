// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <future>
#include <array>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <corehost_context_contract.h>
#include "hostfxr_exports.h"
#include "host_context_test.h"
#include <thread>
#include <utils.h>

namespace
{
    const pal::char_t *app_log_prefix = _X("[APP] ");
    const pal::char_t *config_log_prefix = _X("[CONFIG] ");
    const pal::char_t *secondary_log_prefix = _X("[SECONDARY] ");

    const hostfxr_delegate_type first_delegate_type = hostfxr_delegate_type::hdt_com_activation;
    const hostfxr_delegate_type secondary_delegate_type = hostfxr_delegate_type::hdt_load_in_memory_assembly;

    void get_property_value(
        const hostfxr_exports &hostfxr,
        hostfxr_handle handle,
        int property_count,
        const pal::char_t *property_keys[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        for (int i = 0; i < property_count; ++i)
        {
            const pal::char_t *key = property_keys[i];
            const pal::char_t *value;
            int rc = hostfxr.get_prop_value(handle, key, &value);
            if (rc == StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_property_value succeeded for property: ")
                    << key << _X("=") << value << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_property_value failed for property: ") << key
                    << _X(" - ") << std::hex << std::showbase << rc << std::endl;
            }
        }
    }

    void set_property_value(
        const hostfxr_exports &hostfxr,
        hostfxr_handle handle,
        int property_count,
        const pal::char_t *property_keys[],
        bool remove,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        for (int i = 0; i < property_count; ++i)
        {
            const pal::char_t *key = property_keys[i];
            const pal::char_t *value = remove ? nullptr : _X("VALUE_FROM_HOST");
            int rc = hostfxr.set_prop_value(handle, key, value);
            if (rc == StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_set_runtime_property_value succeeded for property: ") << key << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_set_runtime_property_value failed for property: ") << key
                    << _X(" - ") << std::hex << std::showbase << rc << std::endl;
            }
        }
    }

    void get_properties(
        const hostfxr_exports &hostfxr,
        hostfxr_handle handle,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        size_t count = 0;
        std::vector<const pal::char_t*> keys;
        std::vector<const pal::char_t*> values;
        int rc = hostfxr.get_properties(handle, &count, nullptr, nullptr);
        if (static_cast<StatusCode>(rc) == StatusCode::HostApiBufferTooSmall)
        {
            keys.resize(count);
            values.resize(count);
	        rc = hostfxr.get_properties(handle, &count, keys.data(), values.data());
        }

        if (rc != StatusCode::Success)
        {
            test_output << log_prefix << _X("hostfxr_get_runtime_properties failed - ")
                << std::hex << std::showbase << rc << std::endl;
            return;
        }

        test_output << log_prefix << _X("hostfxr_get_runtime_properties succeeded.") << std::endl;
        for (size_t i = 0; i < keys.size(); ++i)
        {
            test_output << log_prefix << _X("hostfxr_get_runtime_properties: ")
                << keys[i] << _X("=") << values[i] << std::endl;
        }
    }

    void inspect_modify_properties(
        host_context_test::check_properties scenario,
        const hostfxr_exports &hostfxr,
        hostfxr_handle handle,
        int key_count,
        const pal::char_t *keys[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        switch (scenario)
        {
            case host_context_test::check_properties::get:
                get_property_value(hostfxr, handle, key_count, keys, log_prefix, test_output);
                break;
            case host_context_test::check_properties::set:
                set_property_value(hostfxr, handle, key_count, keys, false /*remove*/, log_prefix, test_output);
                break;
            case host_context_test::check_properties::remove:
                set_property_value(hostfxr, handle, key_count, keys, true /*remove*/, log_prefix, test_output);
                break;
            case host_context_test::check_properties::get_all:
                get_properties(hostfxr, handle, log_prefix, test_output);
                break;
            case host_context_test::check_properties::get_active:
                get_property_value(hostfxr, nullptr, key_count, keys, log_prefix, test_output);
                break;
            case host_context_test::check_properties::get_all_active:
                get_properties(hostfxr, nullptr, log_prefix, test_output);
                break;
            case host_context_test::check_properties::none:
            default:
                break;
        }
    }

    bool config_test(
        const hostfxr_exports &hostfxr,
        host_context_test::check_properties check_properties,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        hostfxr_delegate_type delegate_type,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        hostfxr_handle handle;
        int rc = hostfxr.init_config(config_path, nullptr, &handle);
        if (!STATUS_CODE_SUCCEEDED(rc))
        {
            test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config failed: ") << std::hex << std::showbase << rc << std::endl;
            return false;
        }

        test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config succeeded: ") << std::hex << std::showbase << rc << std::endl;

        inspect_modify_properties(check_properties, hostfxr, handle, argc, argv, log_prefix, test_output);

        void *delegate;
        rc = hostfxr.get_delegate(handle, delegate_type, &delegate);
        if (rc != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    int run_app_with_try_except(
        hostfxr_run_app_fn run_app,
        const hostfxr_handle handle,
        pal::stringstream_t &test_output)
    {
#if defined(WIN32)
        __try
#endif
        {
            int rc = run_app(handle);
            if (rc != StatusCode::Success)
                test_output << _X("hostfxr_run_app failed: ") << std::hex << std::showbase << rc << std::endl;

            return rc;
        }
#if defined(WIN32)
        __except(GetExceptionCode() != 0)
        {
            test_output << _X("hostfxr_run_app threw exception: ") << std::hex << std::showbase << GetExceptionCode() << std::endl;
        }
#endif

        return -1;
    }

    int call_delegate_with_try_except(
        component_entry_point_fn component_entry_point,
        const pal::char_t *method_name,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
#if defined(WIN32)
        __try
#endif
        {
            int result = component_entry_point((void*)(static_cast<size_t>(0xdeadbeef)), 42);
            test_output << log_prefix << method_name << _X(" delegate result: ") << std::hex << std::showbase << result << std::endl;

            return StatusCode::Success;
        }
#if defined(WIN32)
        __except(GetExceptionCode() != 0)
        {
            test_output << log_prefix << method_name << _X(" delegate threw exception: ") << std::hex << std::showbase << GetExceptionCode() << std::endl;
        }
#endif

        return -1;
    }

    struct _printable_delegate_name_t
    {
        const pal::char_t* name;
    };

    std::basic_ostream<pal::char_t>& operator<<(std::basic_ostream<pal::char_t>& stream, const _printable_delegate_name_t &p)
    {
        if (p.name == nullptr)
        {
            return stream << _X("nullptr");
        }
        else if (p.name == UNMANAGEDCALLERSONLY_METHOD)
        {
            return stream << _X("UNMANAGEDCALLERSONLY_METHOD");
        }
        else
        {
            return stream << _X("\"") << p.name << _X("\"");
        }
    }

    const _printable_delegate_name_t to_printable_delegate_name(const pal::char_t *delegate_name)
    {
        return _printable_delegate_name_t{ delegate_name };
    }

    int call_load_assembly_and_get_function_pointer_flavour(
        load_assembly_and_get_function_pointer_fn delegate,
        const pal::char_t *assembly_path,
        const pal::char_t *type_name,
        const pal::char_t *method_name,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        const pal::char_t *delegate_name = nullptr;
        pal::string_t method_name_local{ method_name };
        if (pal::string_t::npos != method_name_local.find(_X("Unmanaged")))
            delegate_name = UNMANAGEDCALLERSONLY_METHOD;

        test_output << log_prefix << _X("calling load_assembly_and_get_function_pointer(\"")
            << assembly_path << _X("\", \"")
            << type_name << _X("\", \"")
            << method_name << _X("\", ")
            << to_printable_delegate_name(delegate_name) << _X(", ")
            << _X("nullptr, &componentEntryPointDelegate)")
            << std::endl;

        component_entry_point_fn componentEntryPointDelegate = nullptr;
        int rc = delegate(assembly_path,
                        type_name,
                        method_name,
                        delegate_name,
                        nullptr /* reserved */,
                        (void **)&componentEntryPointDelegate);

        if (rc != StatusCode::Success)
        {
            test_output << log_prefix << _X("load_assembly_and_get_function_pointer failed: ") << std::hex << std::showbase << rc << std::endl;
        }
        else
        {
            test_output << log_prefix << _X("load_assembly_and_get_function_pointer succeeded: ") << std::hex << std::showbase << rc << std::endl;
            rc = call_delegate_with_try_except(componentEntryPointDelegate, method_name, log_prefix, test_output);
        }

        return rc;
    }

    int call_get_function_pointer_flavour(
        get_function_pointer_fn delegate,
        const pal::char_t *type_name,
        const pal::char_t *method_name,
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        const pal::char_t *delegate_name = nullptr;
        pal::string_t method_name_local{ method_name };
        if (pal::string_t::npos != method_name_local.find(_X("Unmanaged")))
            delegate_name = UNMANAGEDCALLERSONLY_METHOD;

        test_output << log_prefix << _X("calling get_function_pointer(\"")
            << type_name << _X("\", \"")
            << method_name << _X("\", ")
            << to_printable_delegate_name(delegate_name) << _X(", ")
            << _X("nullptr, nullptr, &functionPointerDelegate)")
            << std::endl;

        component_entry_point_fn functionPointerDelegate = nullptr;
        int rc = delegate(type_name,
                          method_name,
                          delegate_name,
                          nullptr /* reserved */,
                          nullptr /* reserved */,
                          (void **)&functionPointerDelegate);

        if (rc != StatusCode::Success)
        {
            test_output << log_prefix << _X("get_function_pointer failed: ") << std::hex << std::showbase << rc << std::endl;
        }
        else
        {
            test_output << log_prefix << _X("get_function_pointer succeeded: ") << std::hex << std::showbase << rc << std::endl;
            rc = call_delegate_with_try_except(functionPointerDelegate, method_name, log_prefix, test_output);
        }

        return rc;
    }

    bool component_load_assembly_and_get_function_pointer_test(
        const hostfxr_exports &hostfxr,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        hostfxr_handle handle;
        int rc = hostfxr.init_config(config_path, nullptr, &handle);
        if (!STATUS_CODE_SUCCEEDED(rc))
        {
            test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config failed: ") << std::hex << std::showbase << rc << std::endl;
            return false;
        }

        test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config succeeded: ") << std::hex << std::showbase << rc << std::endl;

        for (int i = 0; i <= argc - 3; i += 3)
        {
            const pal::char_t *assembly_path = argv[i];
            const pal::char_t *type_name = argv[i + 1];
            const pal::char_t *method_name = argv[i + 2];

            load_assembly_and_get_function_pointer_fn delegate = nullptr;
            rc = hostfxr.get_delegate(handle, hostfxr_delegate_type::hdt_load_assembly_and_get_function_pointer, (void **)&delegate);
            if (rc != StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate succeeded: ") << std::hex << std::showbase << rc << std::endl;
                rc = call_load_assembly_and_get_function_pointer_flavour(delegate, assembly_path, type_name, method_name, log_prefix, test_output);
            }
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    bool app_load_assembly_and_get_function_pointer_test(
        const hostfxr_exports &hostfxr,
        int argc,
        const pal::char_t *argv[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        hostfxr_handle handle;
        int rc = hostfxr.init_command_line(argc, argv, nullptr, &handle);
        if (rc != StatusCode::Success)
        {
            test_output << _X("hostfxr_initialize_for_command_line failed: ") << std::hex << std::showbase << rc << std::endl;
            return false;
        }

        test_output << log_prefix << _X("hostfxr_initialize_for_command_line succeeded: ") << std::hex << std::showbase << rc << std::endl;

        for (int i = 1; i <= argc - 3; i += 3)
        {
            const pal::char_t *assembly_path = argv[i];
            const pal::char_t *type_name = argv[i + 1];
            const pal::char_t *method_name = argv[i + 2];

            load_assembly_and_get_function_pointer_fn delegate = nullptr;
            rc = hostfxr.get_delegate(handle, hostfxr_delegate_type::hdt_load_assembly_and_get_function_pointer, (void **)&delegate);
            if (rc != StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate succeeded: ") << std::hex << std::showbase << rc << std::endl;
                rc = call_load_assembly_and_get_function_pointer_flavour(delegate, assembly_path, type_name, method_name, log_prefix, test_output);
            }
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    bool component_get_function_pointer_test(
        const hostfxr_exports &hostfxr,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        hostfxr_handle handle;
        int rc = hostfxr.init_config(config_path, nullptr, &handle);
        if (!STATUS_CODE_SUCCEEDED(rc))
        {
            test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config failed: ") << std::hex << std::showbase << rc << std::endl;
            return false;
        }

        test_output << log_prefix << _X("hostfxr_initialize_for_runtime_config succeeded: ") << std::hex << std::showbase << rc << std::endl;

        for (int i = 0; i <= argc - 2; i += 2)
        {
            const pal::char_t *type_name = argv[i];
            const pal::char_t *method_name = argv[i + 1];

            get_function_pointer_fn delegate = nullptr;
            rc = hostfxr.get_delegate(handle, hostfxr_delegate_type::hdt_get_function_pointer, (void **)&delegate);
            if (rc != StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate succeeded: ") << std::hex << std::showbase << rc << std::endl;
                rc = call_get_function_pointer_flavour(delegate, type_name, method_name, log_prefix, test_output);
            }
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    bool app_get_function_pointer_test(
        const hostfxr_exports &hostfxr,
        int argc,
        const pal::char_t *argv[],
        const pal::char_t *log_prefix,
        pal::stringstream_t &test_output)
    {
        hostfxr_handle handle;
        int rc = hostfxr.init_command_line(argc, argv, nullptr, &handle);
        if (rc != StatusCode::Success)
        {
            test_output << _X("hostfxr_initialize_for_command_line failed: ") << std::hex << std::showbase << rc << std::endl;
            return false;
        }

        test_output << log_prefix << _X("hostfxr_initialize_for_command_line succeeded: ") << std::hex << std::showbase << rc << std::endl;

        for (int i = 1; i <= argc - 2; i += 2)
        {
            const pal::char_t *type_name = argv[i];
            const pal::char_t *method_name = argv[i + 1];

            get_function_pointer_fn delegate = nullptr;
            rc = hostfxr.get_delegate(handle, hostfxr_delegate_type::hdt_get_function_pointer, (void **)&delegate);
            if (rc != StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate succeeded: ") << std::hex << std::showbase << rc << std::endl;
                rc = call_get_function_pointer_flavour(delegate, type_name, method_name, log_prefix, test_output);
            }
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }
}

host_context_test::check_properties host_context_test::check_properties_from_string(const pal::char_t *str)
{
    if (pal::strcmp(str, _X("get")) == 0)
    {
        return host_context_test::check_properties::get;
    }
    else if (pal::strcmp(str, _X("set")) == 0)
    {
        return host_context_test::check_properties::set;
    }
    else if (pal::strcmp(str, _X("remove")) == 0)
    {
        return host_context_test::check_properties::remove;
    }
    else if (pal::strcmp(str, _X("get_all")) == 0)
    {
        return host_context_test::check_properties::get_all;
    }
    else if (pal::strcmp(str, _X("get_active")) == 0)
    {
        return host_context_test::check_properties::get_active;
    }
    else if (pal::strcmp(str, _X("get_all_active")) == 0)
    {
        return host_context_test::check_properties::get_all_active;
    }

    return host_context_test::check_properties::none;
}

bool host_context_test::app(
    check_properties check_properties,
    const pal::string_t &hostfxr_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr { hostfxr_path };

    hostfxr_handle handle;
    int rc = hostfxr.init_command_line(argc, argv, nullptr, &handle);
    if (rc != StatusCode::Success)
    {
        test_output << _X("hostfxr_initialize_for_command_line failed: ") << std::hex << std::showbase << rc << std::endl;
        return false;
    }

    inspect_modify_properties(check_properties, hostfxr, handle, argc, argv, app_log_prefix, test_output);

    rc = run_app_with_try_except(hostfxr.run_app, handle, test_output);

    int rcClose = hostfxr.close(handle);
    if (rcClose != StatusCode::Success)
        test_output << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;

    return rc == StatusCode::Success && rcClose == StatusCode::Success;
}

bool host_context_test::config(
    check_properties check_properties,
    const pal::string_t &hostfxr_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr { hostfxr_path };

    return config_test(hostfxr, check_properties, config_path, argc, argv, first_delegate_type, config_log_prefix, test_output);
}

bool host_context_test::config_multiple(
    check_properties check_properties,
    const pal::string_t &hostfxr_path,
    const pal::char_t *config_path,
    const pal::char_t *secondary_config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr { hostfxr_path };

    if (!config_test(hostfxr, check_properties, config_path, argc, argv, first_delegate_type, config_log_prefix, test_output))
        return false;

    return config_test(hostfxr, check_properties, secondary_config_path, argc, argv, secondary_delegate_type, secondary_log_prefix, test_output);
}

namespace
{
    class block_mock_execute_assembly
    {
    public:
        block_mock_execute_assembly()
        {
            if (pal::getenv(_X("TEST_BLOCK_MOCK_EXECUTE_ASSEMBLY"), &_path))
                pal::touch_file(_path);
        }

        ~block_mock_execute_assembly()
        {
            unblock();
        }

        void unblock()
        {
            if (_path.empty())
                return;

            pal::remove(_path.c_str());
            _path.clear();
        }

    private:
        pal::string_t _path;
    };

    void wait_for_signal_mock_execute_assembly()
    {
        pal::string_t path;
        if (!pal::getenv(_X("TEST_SIGNAL_MOCK_EXECUTE_ASSEMBLY"), &path))
            return;

        while (!pal::file_exists(path))
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    }
}

bool host_context_test::mixed(
    check_properties check_properties,
    const pal::string_t &hostfxr_path,
    const pal::char_t *app_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr { hostfxr_path };

    std::vector<const pal::char_t*> argv_local;
    argv_local.push_back(app_path);
    for (int i = 0; i < argc; ++i)
        argv_local.push_back(argv[i]);

    hostfxr_handle handle;
    int rc = hostfxr.init_command_line(static_cast<int32_t>(argv_local.size()), argv_local.data(), nullptr, &handle);
    if (rc != StatusCode::Success)
    {
        test_output << _X("hostfxr_initialize_for_command_line failed: ") << std::hex << std::showbase << rc << std::endl;
        return false;
    }

    inspect_modify_properties(check_properties, hostfxr, handle, argc, argv, app_log_prefix, test_output);

    block_mock_execute_assembly block_mock;

    pal::stringstream_t run_app_output;
    auto run_app = [&]{
        int rc = hostfxr.run_app(handle);
        if (rc != StatusCode::Success)
            run_app_output << _X("hostfxr_run_app failed: ") << std::hex << std::showbase << rc << std::endl;

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            run_app_output << _X("hostfxr_close failed: ") << std::hex << std::showbase << rcClose << std::endl;
    };
    std::thread app_start = std::thread(run_app);

    wait_for_signal_mock_execute_assembly();

    bool success = config_test(hostfxr, check_properties, config_path, argc, argv, secondary_delegate_type, secondary_log_prefix, test_output);
    block_mock.unblock();
    app_start.join();
    test_output << run_app_output.str();
    return success;
}

bool host_context_test::non_context_mixed(
    check_properties check_properties,
    const pal::string_t &hostfxr_path,
    const pal::char_t *app_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    bool launch_as_if_dotnet,
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr { hostfxr_path };

    pal::string_t host_path;
    if (!pal::get_own_executable_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), host_path.c_str());
        return false;
    }

    block_mock_execute_assembly block_mock;

    std::vector<const pal::char_t*> argv_local;
    if (launch_as_if_dotnet)
        argv_local.push_back(host_path.c_str());

    argv_local.push_back(app_path);
    for (int i = 0; i < argc; ++i)
        argv_local.push_back(argv[i]);

    pal::stringstream_t run_app_output;
    auto run_app = [&]{
        // Imitate running as dotnet by passing empty as app_path to hostfxr_main_startupinfo
        const pal::char_t *app_path_local = launch_as_if_dotnet ? _X("") : app_path;
        int rc = hostfxr.main_startupinfo(static_cast<int32_t>(argv_local.size()), argv_local.data(), host_path.c_str(), get_dotnet_root_from_fxr_path(hostfxr_path).c_str(), app_path_local);
        if (rc != StatusCode::Success)
            run_app_output << _X("hostfxr_main_startupinfo failed: ") << std::hex << std::showbase << rc << std::endl;
    };
    std::thread app_start = std::thread(run_app);

    wait_for_signal_mock_execute_assembly();

    bool success = config_test(hostfxr, check_properties, config_path, argc, argv, secondary_delegate_type, secondary_log_prefix, test_output);
    block_mock.unblock();
    app_start.join();
    test_output << run_app_output.str();
    return success;
}

bool host_context_test::component_load_assembly_and_get_function_pointer(
    const pal::string_t &hostfxr_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    return component_load_assembly_and_get_function_pointer_test(hostfxr, config_path, argc, argv, config_log_prefix, test_output);
}

bool host_context_test::app_load_assembly_and_get_function_pointer(
    const pal::string_t &hostfxr_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    return app_load_assembly_and_get_function_pointer_test(hostfxr, argc, argv, config_log_prefix, test_output);
}

bool host_context_test::component_get_function_pointer(
    const pal::string_t &hostfxr_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    return component_get_function_pointer_test(hostfxr, config_path, argc, argv, config_log_prefix, test_output);
}

bool host_context_test::app_get_function_pointer(
    const pal::string_t &hostfxr_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    return app_get_function_pointer_test(hostfxr, argc, argv, config_log_prefix, test_output);
}
