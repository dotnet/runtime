#include "hostfxr_exports.h"
#include "pal.h"

namespace host_no_context_test
{
    bool get_test_result(
        const pal::char_t* export_fn,
        const pal::string_t& hostfxr_path);
    bool get_hostfxr_runtime_property_value(hostfxr_exports& hostfxr);
    bool get_hostfxr_runtime_properties(hostfxr_exports& hostfxr);
}
