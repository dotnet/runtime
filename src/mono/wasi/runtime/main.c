#include <string.h>
#include <driver.h>
#include <mono/metadata/assembly.h>

// This symbol's implementation is generated during the build
const char* dotnet_wasi_getentrypointassemblyname();

#ifdef WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
// This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
WASI_AFTER_RUNTIME_LOADED_DECLARATIONS
#endif

int main(int argc, char * argv[]) {

#ifdef WASI_AFTER_RUNTIME_LOADED_CALLS
	// This is supplied from the MSBuild itemgroup @(WasiAfterRuntimeLoaded)
	WASI_AFTER_RUNTIME_LOADED_CALLS
#endif
#ifndef WASM_SINGLE_FILE
	mono_set_assemblies_path("managed");
#endif
	mono_wasm_load_runtime("", 0);

	const char* assembly_name = dotnet_wasi_getentrypointassemblyname();
	MonoAssembly* assembly = mono_assembly_open(assembly_name, NULL);
	MonoMethod* entry_method = mono_wasi_assembly_get_entry_point (assembly);
	if (!entry_method) {
		fprintf(stderr, "Could not find entrypoint in assembly %s\n", assembly_name);
		exit(1);
	}

	MonoObject* out_exc;
	MonoObject* out_res;
	int ret = mono_runtime_run_main(entry_method, argc, argv, &out_exc);
	if (out_exc)
	{
		mono_print_unhandled_exception(out_exc);
		exit(1);
	}
	return ret < 0 ? -ret : ret;
}
