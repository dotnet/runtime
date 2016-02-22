// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "utils.h"
#include "corehost.h"
#include "fx_ver.h"
#include "error_codes.h"
#include "policy_load.h"
#include <array>

#define LIBFXR_NAME MAKE_LIBNAME("hostfxr")

bool corehost_t::hostpolicy_exists_in_svc(pal::string_t* resolved_dir)
{
#ifdef COREHOST_PACKAGE_SERVICING
    pal::string_t svc_dir;
    if (!pal::getenv(_X("DOTNET_SERVICING"), &svc_dir))
    {
        return false;
    }

    pal::string_t path = svc_dir;
    append_path(&path, COREHOST_PACKAGE_NAME);
    append_path(&path, COREHOST_PACKAGE_VERSION);
    append_path(&path, COREHOST_PACKAGE_COREHOST_RELATIVE_DIR);
    if (library_exists_in_dir(path, LIBHOST_NAME))
    {
        resolved_dir->assign(path);
    }
    return true;
#else
    return false;
#endif
}

pal::string_t corehost_t::resolve_fxr_path(const pal::string_t& own_dir)
{
    pal::string_t fxr_path;

    pal::string_t fxr_dir = own_dir;
    append_path(&fxr_dir, _X("dotnethost"));
    append_path(&fxr_dir, _X("fxr"));
    if (pal::directory_exists(fxr_dir))
    {
        trace::info(_X("Reading fx resolver directory=[%s]"), fxr_dir.c_str());

        std::vector<pal::string_t> list;
        pal::readdir(fxr_dir, &list);

        fx_ver_t max_ver(-1, -1, -1);
        for (const auto& dir : list)
        {
            trace::info(_X("Considering fxr version=[%s]..."), dir.c_str());

            pal::string_t ver = get_filename(dir);

            fx_ver_t fx_ver(-1, -1, -1);
            if (fx_ver_t::parse(ver, &fx_ver, false))
            {
                max_ver = std::max(max_ver, fx_ver);
            }
        }

        pal::string_t max_ver_str = max_ver.as_str();
        append_path(&fxr_dir, max_ver_str.c_str());
        trace::info(_X("Detected latest fxr version=[%s]..."), fxr_dir.c_str());
    }   

    const pal::string_t* dirs[] = { &fxr_dir, &own_dir };
    for (const auto& dir : dirs)
    {
        trace::info(_X("Considering fxr dir=[%s]..."), fxr_dir.c_str());
        if (policy_load_t::library_exists_in_dir(*dir, LIBFXR_NAME, &fxr_path))
        {
            trace::info(_X("Resolved fxr [%s]..."), fxr_path.c_str());
            return fxr_path;
        }
    }
    return pal::string_t();
}

int corehost_t::resolve_fx_and_execute_app(const pal::string_t& own_dir, const int argc, const pal::char_t* argv[])
{
    pal::dll_t fxr;

    pal::string_t fxr_path = resolve_fxr_path(own_dir);

    // Load library
    if (!pal::load_library(fxr_path.c_str(), &fxr))
    {
        trace::info(_X("Load library of %s failed"), fxr_path.c_str());
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain entrypoint symbols
    hostfxr_main_fn main_fn = (hostfxr_main_fn) pal::get_symbol(fxr, "hostfxr_main");
    return main_fn(argc, argv);
}

int corehost_t::run(const int argc, const pal::char_t* argv[])
{
    pal::string_t own_dir;
    auto mode = detect_operating_mode(argc, argv, &own_dir);

    switch (mode)
    {
    case muxer:
        trace::info(_X("Host operating in Muxer mode"));
        return resolve_fx_and_execute_app(own_dir, argc, argv);

    case split_fx:
        {
            trace::info(_X("Host operating in split mode; own dir=[%s]"), own_dir.c_str());
            corehost_init_t init(_X(""), _X(""), own_dir, host_mode_t::split_fx, nullptr);
            return policy_load_t::execute_app(own_dir, &init, argc, argv);
        }

    case standalone:
        {
            trace::info(_X("Host operating from standalone app dir %s"), own_dir.c_str());

            pal::string_t svc_dir;
            corehost_init_t init(_X(""), _X(""), _X(""), host_mode_t::standalone, nullptr);
            return policy_load_t::execute_app(
                hostpolicy_exists_in_svc(&svc_dir) ? svc_dir : own_dir, &init, argc, argv);
        }
        return StatusCode::CoreHostLibMissingFailure;

    default:
        trace::error(_X("Unknown mode detected or could not resolve the mode."));
        return StatusCode::CoreHostResolveModeFailure;
    }
}

#include <cassert>

#include "deps_format.h"

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    trace::setup();

    //deps_json_t deps(true, _X("H:\\code\\sharedfx\\PortableApp\\PortableAppWithNative.deps.json"));
    //deps_json_t deps2(false, _X("H:\\code\\sharedfx\\StandaloneApp\\StandaloneApp.deps.json"));

    if (trace::is_enabled())
    {
        trace::info(_X("--- Invoked host main = {"));
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));
    }
    corehost_t corehost;
    return corehost.run(argc, argv);
}

