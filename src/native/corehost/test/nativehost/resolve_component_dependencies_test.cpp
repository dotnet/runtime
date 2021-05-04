// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "resolve_component_dependencies_test.h"
#include "hostfxr_exports.h"
#include <error_codes.h>
#include "hostpolicy_exports.h"
#include "error_writer_redirector.h"
#include <future>

namespace
{
    class resolve_component_dependencies_result
    {
    public:
        resolve_component_dependencies_result()
        {
        }

        static void HOSTPOLICY_CALLTYPE fn(
            const pal::char_t* local_assembly_paths,
            const pal::char_t* local_native_search_paths,
            const pal::char_t* local_resource_search_paths)
        {
            assembly_paths() = local_assembly_paths;
            native_search_paths() = local_native_search_paths;
            resource_search_paths() = local_resource_search_paths;
        }

        static pal::string_t& assembly_paths()
        {
            thread_local static pal::string_t assembly_paths;
            return assembly_paths;
        }

        static pal::string_t& native_search_paths()
        {
            thread_local static pal::string_t native_search_paths;
            return native_search_paths;
        }

        static pal::string_t& resource_search_paths()
        {
            thread_local static pal::string_t resource_search_paths;
            return resource_search_paths;
        }
    };

    template<typename Act>
    bool run_app_and_hostpolicy_action(
        const pal::string_t& hostfxr_path,
        const pal::string_t& app_path,
        pal::stringstream_t& test_output,
        Act action)
    {
        int rc = StatusCode::Success;
        hostfxr_exports hostfxr{ hostfxr_path };
        hostfxr_handle handle;

        {
            std::vector<const pal::char_t*> argv;
            argv.push_back(app_path.c_str());

            rc = hostfxr.init_command_line(static_cast<int32_t>(argv.size()), argv.data(), nullptr, &handle);
            if (rc != StatusCode::Success)
            {
                test_output << _X("hostfxr_initialize_for_command_line failed: ") << std::hex << std::showbase << rc << std::endl;
                return false;
            }
        }

        rc = hostfxr.run_app(handle);
        if (rc != StatusCode::Success)
        {
            test_output << _X("hostfxr_run_app failed: ") << std::hex << std::showbase << rc << std::endl;
        }
        else
        {
            hostpolicy_exports hostpolicy;
            test_output << _X("Found already loaded hostpolicy library: '") << hostpolicy.path.c_str() << _X("'.") << std::endl;

            rc = action(hostpolicy);
        }

        int rcClose = hostfxr.close(handle);
        if (rcClose != StatusCode::Success)
            test_output << _X("hostfxr_close failed: ") << std::hex << std::showbase << rc << std::endl;

        return rc == StatusCode::Success && rcClose == StatusCode::Success;
    }

    int resolve_component_helper(
        hostpolicy_exports& hostpolicy,
        const pal::string_t& component_path,
        const pal::char_t* prefix,
        pal::stringstream_t& test_output)
    {
        error_writer_redirector errors{ hostpolicy.set_error_writer, prefix };

        resolve_component_dependencies_result result;
        int rc = hostpolicy.resolve_component_dependencies(component_path.c_str(), result.fn);

        if (rc == StatusCode::Success)
        {
            // Split order and merge again the assembly_paths - the result returned by the hostpolicy is not stable (and not guaranteed to be either)
            pal::stringstream_t assembly_paths_stream(result.assembly_paths());
            std::vector<pal::string_t> resolved_assemblies;
            pal::string_t assembly_path;
            while (std::getline(assembly_paths_stream, assembly_path, PATH_SEPARATOR))
            {
                resolved_assemblies.push_back(assembly_path);
            }

            std::sort(resolved_assemblies.begin(), resolved_assemblies.end(), [](const pal::string_t& a, const pal::string_t& b)
                {
                    return a.compare(b) < 0;
                });

            assembly_paths_stream.clear();
            for (auto i : resolved_assemblies)
            {
                assembly_paths_stream << i.c_str() << PATH_SEPARATOR;
            }

            test_output << prefix << _X("corehost_resolve_component_dependencies:Success") << std::endl;
            test_output << prefix << _X("corehost_resolve_component_dependencies assemblies:[") << assembly_paths_stream.str().c_str() << _X("]") << std::endl;
            test_output << prefix << _X("corehost_resolve_component_dependencies native_search_paths:[") << result.native_search_paths().c_str() << _X("]") << std::endl;
            test_output << prefix << _X("corehost_resolve_component_dependencies resource_search_paths:[") << result.resource_search_paths().c_str() << _X("]") << std::endl;
        }
        else
        {
            test_output << prefix << _X("corehost_resolve_component_dependencies:Fail[") << std::hex << std::showbase << rc << _X("]" << std::endl);
        }

        if (errors.has_errors())
        {
            test_output << prefix << _X("corehost reported errors:") << std::endl << errors.get_errors().c_str();
        }

        return rc;
    }
}

bool resolve_component_dependencies_test::run_app_and_resolve(
    const pal::string_t& hostfxr_path,
    const pal::string_t& app_path,
    const pal::string_t& component_path,
    pal::stringstream_t& test_output)
{
    return run_app_and_hostpolicy_action(
        hostfxr_path,
        app_path,
        test_output,
        [&](hostpolicy_exports& hostpolicy)
        {
            return resolve_component_helper(
                hostpolicy,
                component_path,
                _X(""),
                test_output);
        }
    );
}

bool resolve_component_dependencies_test::run_app_and_resolve_multithreaded(
    const pal::string_t& hostfxr_path,
    const pal::string_t& app_path,
    const pal::string_t& component_path_a,
    const pal::string_t& component_path_b,
    pal::stringstream_t& test_output)
{
    return run_app_and_hostpolicy_action(
        hostfxr_path,
        app_path,
        test_output,
        [&](hostpolicy_exports& hostpolicy)
        {
            pal::stringstream_t test_output_a;
            pal::stringstream_t test_output_b;

            int rc = StatusCode::Success;
            std::thread resolve_component_a([&]
                {
                    int rc_inner = resolve_component_helper(
                        hostpolicy,
                        component_path_a,
                        _X("ComponentA: "),
                        test_output_a);
                    if (rc_inner != StatusCode::Success)
                    {
                        rc = rc_inner;
                    }
                });

            std::thread resolve_component_b([&]
                {
                    int rc_inner = resolve_component_helper(
                        hostpolicy,
                        component_path_b,
                        _X("ComponentB: "),
                        test_output_b);
                    if (rc_inner != StatusCode::Success)
                    {
                        rc = rc_inner;
                    }
                });

            resolve_component_a.join();
            resolve_component_b.join();

            test_output << test_output_a.str();
            test_output << test_output_b.str();

            return rc;
        }
    );
}
