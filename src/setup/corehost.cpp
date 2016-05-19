// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "utils.h"
#include "pal.h"
#include "fx_ver.h"
#include "error_codes.h"

#define LIBFXR_NAME MAKE_LIBNAME("hostfxr")

typedef int(*hostfxr_main_fn) (const int argc, const pal::char_t* argv[]);

pal::string_t resolve_fxr_path(const pal::string_t& own_dir)
{
    pal::string_t fxr_dir = own_dir;
    append_path(&fxr_dir, _X("host"));
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

        pal::string_t ret_path;
        if (library_exists_in_dir(fxr_dir, LIBFXR_NAME, &ret_path))
        {
            trace::info(_X("Resolved fxr [%s]..."), ret_path.c_str());
            return ret_path;
        }
    }

    pal::string_t fxr_path;
    if (library_exists_in_dir(own_dir, LIBFXR_NAME, &fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), fxr_path.c_str());
        return fxr_path;
    }
    return pal::string_t();
}

int run(const int argc, const pal::char_t* argv[])
{
    pal::string_t own_path;
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), own_path.c_str());
        return StatusCode::CoreHostCurExeFindFailure;
    }

    pal::dll_t fxr;

    pal::string_t own_dir = get_directory(own_path);

    // Load library
    pal::string_t fxr_path = resolve_fxr_path(own_dir);
    if (!pal::load_library(fxr_path.c_str(), &fxr))
    {
        trace::info(_X("Load library of %s failed"), fxr_path.c_str());
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain entrypoint symbols
    hostfxr_main_fn main_fn = (hostfxr_main_fn) pal::get_symbol(fxr, "hostfxr_main");
    int code = main_fn(argc, argv);
    pal::unload_library(fxr);
    return code;
}

static char sccsid[] = "@(#)"            \
                       "version: "       \
                       HOST_PKG_VER      \
                       "; commit: "      \
                       REPO_COMMIT_HASH  \
                       "; built: "       \
                       __DATE__          \
                       " "               \
                       __TIME__          \
                       ;

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    trace::setup();

    if (trace::is_enabled())
    {
        trace::info(_X("--- Invoked dotnet [version: %s, commit hash: %s] main = {"), _STRINGIFY(HOST_PKG_VER), _STRINGIFY(REPO_COMMIT_HASH));
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));
    }

    return run(argc, argv);
}

