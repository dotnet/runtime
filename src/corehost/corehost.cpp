// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "utils.h"
#include "pal.h"
#include "fx_ver.h"
#include "error_codes.h"

#if FEATURE_APPHOST
#define CURHOST_TYPE    _X("apphost")
#define CUREXE_PKG_VER APPHOST_PKG_VER
#else // !FEATURE_APPHOST
#define CURHOST_TYPE    _X("dotnet")
#define CUREXE_PKG_VER HOST_PKG_VER
#endif // !FEATURE_APPHOST

typedef int(*hostfxr_main_fn) (const int argc, const pal::char_t* argv[]);

#if FEATURE_APPHOST

/**
 * Detect if the apphost executable is allowed to load and execute a managed assembly.
 *
 *    - The exe is built with a known hash string at some offset in the image
 *    - The exe is useless as is with the built-in hash value, and will fail with an error message
 *    - The hash value should be replaced with the managed DLL filename using "NUL terminated UTF-8" by "dotnet build"
 *    - The exe may be signed at this point by the app publisher
 *    - When the exe runs, the managed DLL name is validated against the executable's own name
 *    - If validation passes, the embedded managed DLL name will be loaded by the exe
 *    - Note: the maximum size of the managed DLL file name can be 1024 bytes in UTF-8 (not including NUL)
 *        o https://en.wikipedia.org/wiki/Comparison_of_file_systems
 *          has more details on maximum file name sizes.
 */
#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89" // SHA-256 of "foobar" in UTF-8
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8    (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8) // NUL terminated
bool is_exe_enabled_for_execution(const pal::string_t& own_path)
{
    constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]);
    constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025); // 1024 DLL name length, 1 NUL
    static const char embed[EMBED_MAX] = EMBED_HASH_FULL_UTF8;     // series of NULs followed by embed hash string
    static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
    static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

    // At this point the "embed" variable may contain the embed hash value specified above at compile time
    // or the managed DLL name replaced by "dotnet build".
    std::string binding(&embed[0]);
    pal::string_t pal_binding;
    if (!pal::utf8_palstring(binding, &pal_binding))
    {
        trace::error(_X("The managed DLL bound to this executable could not be retrieved from the executable image."));
        return false;
    }

    // Since the single static string is replaced by editing the executable, a reference string is needed to do the compare.
    // So use two parts of the string that will be unaffected by the edit.
    size_t hi_len = (sizeof(hi_part) / sizeof(hi_part[0])) - 1;
    size_t lo_len = (sizeof(lo_part) / sizeof(lo_part[0])) - 1;

    if ((binding.size() >= (hi_len + lo_len)) && 
        binding.compare(0, hi_len, &hi_part[0]) == 0 &&
        binding.compare(hi_len, lo_len, &lo_part[0]) == 0)
    {
        trace::error(_X("This executable is not bound to a managed DLL to execute. The binding value is: '%s'"), pal_binding.c_str());
        return false;
    }

    pal::string_t own_name = get_filename(own_path);
    pal::string_t own_dll_filename = get_executable(own_name) + _X(".dll");

    if (pal::strcasecmp(own_dll_filename.c_str(), pal_binding.c_str()) != 0)
    {
        trace::error(_X("The managed DLL bound to this executable: '%s', did not match own name '%s'."), pal_binding.c_str(), own_dll_filename.c_str());
        return false;
    }

    trace::info(_X("The managed DLL bound to this executable is: '%s'"), pal_binding.c_str());
    return true;
}
#endif // FEATURE_APPHOST

pal::string_t resolve_fxr_path(const pal::string_t& own_dir)
{
#if FEATURE_APPHOST
    pal::string_t fxr_path;
    if (library_exists_in_dir(own_dir, LIBFXR_NAME, &fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), fxr_path.c_str());
        return fxr_path;
    }
    return pal::string_t();
#else
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
    // TODO: Issue #215 Do not allow dotnet to load hostfxr side-by-side.
    pal::string_t fxr_path;
    if (library_exists_in_dir(own_dir, LIBFXR_NAME, &fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), fxr_path.c_str());
        return fxr_path;
    }
    return pal::string_t();
#endif
}

int run(const int argc, const pal::char_t* argv[])
{
    pal::string_t own_path;
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), own_path.c_str());
        return StatusCode::CoreHostCurExeFindFailure;
    }

#ifdef FEATURE_APPHOST
    if (!is_exe_enabled_for_execution(own_path))
    {
        trace::error(_X("A fatal error was encountered. This executable was not bound to load a managed DLL."));
        return StatusCode::AppHostExeNotBoundFailure;
    }
#endif

    pal::dll_t fxr;

    pal::string_t own_dir = get_directory(own_path);

    // Load library
    pal::string_t fxr_path = resolve_fxr_path(own_dir);
    if (fxr_path.empty())
    {
        trace::error(_X("A fatal error occurred, the required library %s could not be found at %s"), LIBFXR_NAME, own_dir.c_str());
        return StatusCode::CoreHostLibMissingFailure;
    }

    if (!pal::load_library(fxr_path.c_str(), &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain the entrypoints.
    hostfxr_main_fn main_fn = (hostfxr_main_fn) pal::get_symbol(fxr, "hostfxr_main");
    int code = main_fn(argc, argv);
    pal::unload_library(fxr);
    return code;
}

static char sccsid[] = "@(#)"            \
                       "version: "       \
                       CUREXE_PKG_VER    \
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
        trace::info(_X("--- Invoked %s [version: %s, commit hash: %s] main = {"), CURHOST_TYPE, _STRINGIFY(CUREXE_PKG_VER), _STRINGIFY(REPO_COMMIT_HASH));
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));
    }

    return run(argc, argv);
}

