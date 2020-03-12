// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "trace.h"
#include "info.h"
#include "utils.h"

using namespace bundle;

// Global single-file bundle information, if any
const info_t* info_t::the_app = nullptr;

StatusCode info_t::process_bundle(const pal::char_t* bundle_path, const pal::char_t* app_path, int64_t header_offset)
{
    if (header_offset == 0)
    {
        // Not a single-file bundle.
        return StatusCode::Success;
    }

    static info_t info(bundle_path, header_offset);
    StatusCode status = info.process_header();

    if (status != StatusCode::Success)
    {
        return status;
    }

    info.init_config(app_path);

    trace::info(_X("Single-File bundle details:"));
    trace::info(_X("DepsJson Offset:[%lx] Size[%lx]"), info.m_header.deps_json_location().offset, info.m_header.deps_json_location().size);
    trace::info(_X("RuntimeConfigJson Offset:[%lx] Size[%lx]"), info.m_header.runtimeconfig_json_location().offset, info.m_header.runtimeconfig_json_location().size);
    trace::info(_X(".net core 3 compatibility mode: [%s]"), info.m_header.is_netcoreapp3_compat_mode() ? _X("Yes") : _X("No"));

    the_app = &info;

    return StatusCode::Success;
}

StatusCode info_t::process_header()
{
    try
    {
        const int8_t* addr = map_bundle();

        reader_t reader(addr, m_bundle_size, m_header_offset);

        m_header = header_t::read(reader);

        unmap_bundle(addr);

        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        return e;
    }
}

void info_t::init_config(const pal::string_t& app_path)
{
    // Single-file bundles currently only support deps/runtime config json files
    // named based on the app.dll. Any other name for these configuration files
    // mentioned via the command line are assumed to be actual files on disk.
    // 
    // Supporting custom names for these config files is straightforward (with associated changes in bundler and SDK).
    // There is no known use-case for it yet, and the facility is TBD.

    m_base_path = get_directory(m_bundle_path);
    pal::string_t deps_json_name = get_deps_from_app_binary(m_base_path, app_path);
    pal::string_t runtimeconfig_json_name = get_runtime_config_path(m_base_path, get_filename_without_ext(app_path));

    m_deps_json = config_t(deps_json_name, &m_header.deps_json_location());
    m_runtimeconfig_json = config_t(runtimeconfig_json_name, &m_header.runtimeconfig_json_location());
}

const int8_t* info_t::config_t::map(const pal::string_t& path, const location_t* &location)
{
    const bundle::info_t* app = bundle::info_t::the_app;

    if (app->m_deps_json.matches(path))
    {
        location = app->m_deps_json.m_location;
    }
    else if (app->m_runtimeconfig_json.matches(path))
    {
        location = app->m_runtimeconfig_json.m_location;
    }
    else
    {
        return nullptr;
    }

    // When necessary to map the deps.json or runtimeconfig.json files, we map the whole single-file bundle,
    // and return the address at the appropriate offset.
    // This is because:
    // * The host is the only code that is currently running, and attempting to map the bundle at this time
    // * Files can only be memory mapped at page-aligned offsets, and in whole page units.
    //   Therefore, mapping only portions of the bundle will involve align-down/round-up calculations, and associated offset adjustments.
    //   We choose the simpler approach of rounding to the whole file
    // * There is no performance limitation due to a larger sized mapping, since we actually only read the pages with relevant contents.

    const int8_t* addr = (const int8_t*)pal::mmap_read(app->m_bundle_path);
    if (addr == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to map bundle file [%s]"), path.c_str());
    }

    return addr + location->offset;
}

void info_t::config_t::unmap(const int8_t* addr, const location_t* location)
{
    // Adjust to the beginning of the bundle.
    addr -= location->offset;

    bundle::info_t::the_app->unmap_bundle(addr);
}

const int8_t* info_t::map_bundle()
{
    void *addr = pal::mmap_read(m_bundle_path, &m_bundle_size);

    if (addr == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Couldn't memory map the bundle file for reading."));
        throw StatusCode::BundleExtractionIOError;
    }

    return (int8_t *)addr;
}

void info_t::unmap_bundle(const int8_t* addr) const
{
    if (!pal::munmap((void*)addr, m_bundle_size))
    {
        trace::warning(_X("Failed to unmap bundle after extraction."));
    }
}


