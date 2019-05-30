// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <future>
#include <hostfxr.h>
#include <corehost_context_contract.h>
#include "host_context_test.h"
#include <utils.h>

namespace
{
    const pal::char_t *app_log_prefix = _X("[APP] ");
    const pal::char_t *config_log_prefix = _X("[CONFIG] ");
    const pal::char_t *secondary_log_prefix = _X("[SECONDARY] ");

    const hostfxr_delegate_type first_delegate_type = hostfxr_delegate_type::com_activation;
    const hostfxr_delegate_type secondary_delegate_type = hostfxr_delegate_type::load_in_memory_assembly;

    class hostfxr_exports
    {
    public:
        hostfxr_initialize_for_dotnet_command_line_fn init_command_line;
        hostfxr_run_app_fn run_app;

        hostfxr_initialize_for_runtime_config_fn init_config;
        hostfxr_get_runtime_delegate_fn get_delegate;

        hostfxr_get_runtime_property_value_fn get_prop_value;
        hostfxr_set_runtime_property_value_fn set_prop_value;
        hostfxr_get_runtime_properties_fn get_properties;

        hostfxr_close_fn close;

        hostfxr_main_startupinfo_fn main_startupinfo;

    public:
        hostfxr_exports(const pal::string_t &hostfxr_path)
        {
            if (!pal::load_library(&hostfxr_path, &_dll))
            {
                std::cout << "Load library of hostfxr failed" << std::endl;
                throw StatusCode::CoreHostLibLoadFailure;
            }

            init_command_line = (hostfxr_initialize_for_dotnet_command_line_fn)pal::get_symbol(_dll, "hostfxr_initialize_for_dotnet_command_line");
            run_app = (hostfxr_run_app_fn)pal::get_symbol(_dll, "hostfxr_run_app");

            init_config = (hostfxr_initialize_for_runtime_config_fn)pal::get_symbol(_dll, "hostfxr_initialize_for_runtime_config");
            get_delegate = (hostfxr_get_runtime_delegate_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_delegate");

            get_prop_value = (hostfxr_get_runtime_property_value_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_property_value");
            set_prop_value = (hostfxr_set_runtime_property_value_fn)pal::get_symbol(_dll, "hostfxr_set_runtime_property_value");
            get_properties = (hostfxr_get_runtime_properties_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_properties");

            close = (hostfxr_close_fn)pal::get_symbol(_dll, "hostfxr_close");

            main_startupinfo = (hostfxr_main_startupinfo_fn)pal::get_symbol(_dll, "hostfxr_main_startupinfo");

            if (init_command_line == nullptr || run_app == nullptr
                || init_config == nullptr || get_delegate == nullptr
                || get_prop_value == nullptr || set_prop_value == nullptr
                || get_properties == nullptr || close == nullptr
                || main_startupinfo == nullptr)
            {
                std::cout << "Failed to get hostfxr entry points" << std::endl;
                throw StatusCode::CoreHostEntryPointFailure;
            }
        }

        ~hostfxr_exports()
        {
            pal::unload_library(_dll);
        }

    private:
        pal::dll_t _dll;
    };

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
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rc  << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    bool load_assembly_and_get_function_pointer(
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

            LoadAssemblyAndGetFunctionPointer delegate = nullptr;
            rc = hostfxr.get_delegate(handle, hostfxr_delegate_type::load_assembly_and_get_function_pointer, (void **)&delegate);
            if (rc != StatusCode::Success)
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate failed: ") << std::hex << std::showbase << rc << std::endl;
            }
            else
            {
                test_output << log_prefix << _X("hostfxr_get_runtime_delegate succeeded: ") << std::hex << std::showbase << rc << std::endl;

                test_output << log_prefix << _X("calling LoadAssemblyAndGetFunctionPointer(\"")
                    << assembly_path << _X("\", \"")
                    << type_name << _X("\", \"")
                    << method_name << _X("\", \"")
                    << _X("nullptr, nullptr, &componentEntryPointDelegate)")
                    << std::endl;

                ComponentEntryPointDelegate componentEntryPointDelegate = nullptr;
                rc = delegate(assembly_path,
                              type_name,
                              method_name,
                              nullptr /* delegateTypeNative */,
                              nullptr /* reserved */,
                              (void **)&componentEntryPointDelegate);

                if (rc != StatusCode::Success)
                {
                    test_output << log_prefix << _X("LoadAssemblyAndGetFunctionPointer failed: ") << std::hex << std::showbase << rc << std::endl;
                }
                else
                {
                    test_output << log_prefix << _X("LoadAssemblyAndGetFunctionPointer succeeded: ") << std::hex << std::showbase << rc << std::endl;

                    int result = componentEntryPointDelegate((void*)(static_cast<size_t>(0xdeadbeef)), 42);

                    test_output << log_prefix << method_name << _X(" delegate result: ") << std::hex << std::showbase << result << std::endl;
                }
            }
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << log_prefix << _X("hostfxr_close failed: ") << std::hex << std::showbase << rc << std::endl;

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

    rc = hostfxr.run_app(handle);
    if (rc != StatusCode::Success)
        test_output << _X("hostfxr_run_app failed: ") << std::hex << std::showbase << rc << std::endl;

    int rcClose = hostfxr.close(handle);
    if (rcClose != StatusCode::Success)
        test_output << _X("hostfxr_close failed: ") << std::hex << std::showbase << rc  << std::endl;

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
    int rc = hostfxr.init_command_line(argv_local.size(), argv_local.data(), nullptr, &handle);
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
            run_app_output << _X("hostfxr_close failed: ") << std::hex << std::showbase << rc  << std::endl;
    };
    std::thread app_start = std::thread(run_app);

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
    argv_local.push_back(app_path);
    for (int i = 0; i < argc; ++i)
        argv_local.push_back(argv[i]);

    pal::stringstream_t run_app_output;
    auto run_app = [&]{
        int rc = hostfxr.main_startupinfo(argv_local.size(), argv_local.data(), host_path.c_str(), get_dotnet_root_from_fxr_path(hostfxr_path).c_str(), app_path);
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

bool host_context_test::load_assembly_and_get_function_pointer(
    const pal::string_t &hostfxr_path,
    const pal::char_t *config_path,
    int argc,
    const pal::char_t *argv[],
    pal::stringstream_t &test_output)
{
    hostfxr_exports hostfxr{ hostfxr_path };

    return load_assembly_and_get_function_pointer(hostfxr, config_path, argc, argv, config_log_prefix, test_output);
}