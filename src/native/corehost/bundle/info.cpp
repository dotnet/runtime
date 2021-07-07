// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "trace.h"
#include "info.h"
#include "utils.h"

using namespace bundle;

// Global single-file bundle information, if any
const info_t* info_t::the_app = nullptr;

info_t::info_t(const pal::char_t* bundle_path,
    const pal::char_t* app_path,
    int64_t header_offset)
    : m_bundle_path(bundle_path)
    , m_bundle_size(0)
    , m_header_offset(header_offset)
    , m_header(0, 0, 0)
{
    m_base_path = get_directory(m_bundle_path);

    // Single-file bundles currently only support deps/runtime config json files
    // named based on the app.dll. Any other name for these configuration files
    // mentioned via the command line are assumed to be actual files on disk.
    // 
    // Supporting custom names for these config files is straightforward (with associated changes in bundler and SDK).
    // There is no known use-case for it yet, and the facility is TBD.

    m_deps_json = config_t(get_deps_from_app_binary(m_base_path, app_path));
    m_runtimeconfig_json = config_t(get_runtime_config_path(m_base_path, get_filename_without_ext(app_path)));
}

StatusCode info_t::process_bundle(const pal::char_t* bundle_path, const pal::char_t* app_path, int64_t header_offset)
{
    if (header_offset == 0)
    {
        // Not a single-file bundle.
        return StatusCode::Success;
    }

    static info_t info(bundle_path, app_path, header_offset);
    StatusCode status = info.process_header();

    if (status != StatusCode::Success)
    {
        return status;
    }

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
        const char* addr = map_bundle();

        reader_t reader(addr, m_bundle_size, m_header_offset);
        m_offset_in_file = reader.offset_in_file();

        m_header = header_t::read(reader);
        m_deps_json.set_location(&m_header.deps_json_location());
        m_runtimeconfig_json.set_location(&m_header.runtimeconfig_json_location());

        unmap_bundle(addr);

        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        return e;
    }
}

char* info_t::config_t::map(const pal::string_t& path, const location_t* &location)
{
    assert(is_single_file_bundle());

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
    // * The host is the only code that is currently running and trying to map the bundle.
    // * Files can only be memory mapped at page-aligned offsets, and in whole page units.
    //   Therefore, mapping only portions of the bundle will involve align-down/round-up calculations, and associated offset adjustments.
    //   We choose the simpler approach of rounding to the whole file
    // * There is no performance limitation due to a larger sized mapping, since we actually only read the pages with relevant contents.
    // * Files that are too large to be mapped (ex: that exhaust 32-bit virtual address space) are not supported. 

    char* addr = (char*)pal::mmap_copy_on_write(app->m_bundle_path);
    if (addr == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to map bundle file [%s]"), path.c_str());
    }

    trace::info(_X("Mapped bundle for [%s]"), path.c_str());

    return addr + location->offset + app->m_offset_in_file;
}

void info_t::config_t::unmap(const char* addr, const location_t* location)
{
    // Adjust to the beginning of the bundle.
    const bundle::info_t* app = bundle::info_t::the_app;
    addr -= location->offset - app->m_offset_in_file;

    bundle::info_t::the_app->unmap_bundle(addr);
}

const char* info_t::map_bundle()
{
    const void *addr = pal::mmap_read(m_bundle_path, &m_bundle_size);

    if (addr == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Couldn't memory map the bundle file for reading."));
        throw StatusCode::BundleExtractionIOError;
    }

    trace::info(_X("Mapped application bundle"));

    return (const char *)addr;
}

void info_t::unmap_bundle(const char* addr) const
{
    if (!pal::munmap((void*)addr, m_bundle_size))
    {
        trace::warning(_X("Failed to unmap bundle after extraction."));
    }
    else
    {
        trace::info(_X("Unmapped application bundle"));
    }
}


