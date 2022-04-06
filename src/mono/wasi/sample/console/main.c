#include <string.h>
#include "../../mono-wasi-driver/driver.h"

int main() {
    // Assume the runtime pack has been copied into the output directory as 'runtime'
    // Otherwise we have to mount an unrelated part of the filesystem within the WASM environment
    const char* app_base_dir = "./WasiConsoleApp/bin/Release/net7.0";
    char* assemblies_path;
    asprintf(&assemblies_path, "%s:%s/runtime/native:%s/runtime/lib/net7.0", app_base_dir, app_base_dir, app_base_dir);

    add_assembly(app_base_dir, "WasiConsoleApp.dll");
    mono_set_assemblies_path(assemblies_path);

    mono_wasm_load_runtime("", 0);

    MonoAssembly* assembly = mono_wasm_assembly_load ("WasiConsoleApp.dll");
    MonoMethod* entry_method = mono_wasm_assembly_get_entry_point (assembly);
    MonoObject* out_exc;
    MonoObject *exit_code = mono_wasm_invoke_method (entry_method, NULL, NULL, &out_exc);
    return mono_unbox_int (exit_code);
}
