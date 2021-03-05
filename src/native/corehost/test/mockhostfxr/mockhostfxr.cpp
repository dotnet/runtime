#include "error_codes.h"
#include "hostfxr.h"
#include "host_startup_info.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

namespace
{
    void trace_hostfxr_entry_point(const pal::char_t *entry_point)
    {
        trace::setup();
        trace::info(_X("--- Invoked hostfxr mock - %s"), entry_point);
    }
}

SHARED_API int HOSTFXR_CALLTYPE hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path)
{
    trace_hostfxr_entry_point(_X("hostfxr_main_startupinfo"));

    const pal::string_t dotnet_folder = get_directory(dotnet_root);

    if (pal::strcmp(dotnet_folder.c_str(), _X("hostfxrFrameworkMissingFailure")))
    {
        return StatusCode::FrameworkMissingFailure;
    }

    return StatusCode::Success;
}
