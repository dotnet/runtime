// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <errno.h>
#include <sys/stat.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-debug.h>

#include <mono/metadata/mono-private-unstable.h>

#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl-fallback.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

#include "gc-common.h"
#include "driver.h"
#include "runtime.h"

int mono_wasm_register_root (char *start, size_t size, const char *name);
void mono_wasm_deregister_root (char *addr);

char *monoeg_g_getenv(const char *variable);
int monoeg_g_setenv(const char *variable, const char *value, int overwrite);
int32_t monoeg_g_hasenv(const char *variable);
void mono_free (void*);
char *mono_method_get_full_name (MonoMethod *method);
#ifndef INVARIANT_TIMEZONE
extern void mono_register_timezones_bundle (void);
#endif /* INVARIANT_TIMEZONE */
#ifdef WASM_SINGLE_FILE
extern void mono_register_assemblies_bundle (void);
extern void mono_register_runtimeconfig_bin (void);
extern bool mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out);
#ifndef INVARIANT_GLOBALIZATION
extern void mono_register_icu_bundle (void);
#endif /* INVARIANT_GLOBALIZATION */
#endif /* WASM_SINGLE_FILE */

extern void mono_bundled_resources_add_assembly_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, void (*free_func)(void *, void *), void *free_data);
extern void mono_bundled_resources_add_assembly_symbol_resource (const char *id, const uint8_t *data, uint32_t size, void (*free_func)(void *, void *), void *free_data);
extern void mono_bundled_resources_add_satellite_assembly_resource (const char *id, const char *name, const char *culture, const uint8_t *data, uint32_t size, void (*free_func)(void *, void *), void *free_data);

extern const char* dotnet_wasi_getentrypointassemblyname();
int32_t mono_wasi_load_icu_data(const void* pData);
void load_icu_data (void);

int
mono_string_instance_is_interned (MonoString *str_raw);

#define g_new(type, size)  ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type, size) ((type *) calloc (sizeof (type), (size)))

static MonoDomain *root_domain;

#define RUNTIMECONFIG_BIN_FILE "runtimeconfig.bin"

static void
wasi_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
	if (strcmp(log_level, "error") == 0)
		fprintf(stderr, "[MONO] %s: %s\n", log_level, message);
	else
		printf("[MONO] %s: %s\n", log_level, message);
	if (fatal) {
		// make it trap so we could see the stack trace
		// (*(int*)(void*)-1)++;
		exit(1);
	}
}

typedef uint32_t target_mword;
typedef target_mword SgenDescriptor;
typedef SgenDescriptor MonoGCDescriptor;

static void
bundled_resources_free_func (void *resource, void *free_data)
{
	free (free_data);
}

int
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	/*printf("wasi: mono_wasm_add_assembly: %s size: %u\n", name, size);*/
	int len = strlen (name);
	if (!strcasecmp (".pdb", &name [len - 4])) {
		char *new_name = strdup (name);
		//FIXME handle debugging assemblies with .exe extension
		strcpy (&new_name [len - 3], "dll");
		mono_bundled_resources_add_assembly_symbol_resource (new_name, data, size, bundled_resources_free_func, new_name);
		return 1;
	}
	char *assembly_name = strdup (name);
	assert (assembly_name);
	mono_bundled_resources_add_assembly_resource (assembly_name, assembly_name, data, size, bundled_resources_free_func, assembly_name);
	return mono_has_pdb_checksum ((char*)data, size);
}

char* gai_strerror(int code) {
	char* result = malloc(256);
	sprintf(result, "Error code %i", code);
	return result;
}

static void
bundled_resources_free_slots_func (void *resource, void *free_data)
{
	if (free_data) {
		void **slots = (void **)free_data;
		for (int i = 0; slots [i]; i++)
			free (slots [i]);
	}
}

void
mono_wasm_add_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
	int id_len = strlen (culture) + 1 + strlen (name); // +1 is for the "/"
	char *id = (char *)malloc (sizeof (char) * (id_len + 1)); // +1 is for the terminating null character
	assert (id);

	int num_char = snprintf (id, (id_len + 1), "%s/%s", culture, name);
	assert (num_char > 0 && num_char == id_len);

	char *satellite_assembly_name = strdup (name);
	assert (satellite_assembly_name);

	char *satellite_assembly_culture = strdup (culture);
	assert (satellite_assembly_culture);

	void **slots = malloc (sizeof (void *) * 4);
	assert (slots);
	slots [0] = id;
	slots [1] = satellite_assembly_name;
	slots [2] = satellite_assembly_culture;
	slots [3] = NULL;

	mono_bundled_resources_add_satellite_assembly_resource (id, satellite_assembly_name, satellite_assembly_culture, data, size, bundled_resources_free_slots_func, slots);
}

#ifndef INVARIANT_GLOBALIZATION
void load_icu_data (void)
{
#ifdef WASM_SINGLE_FILE
	mono_register_icu_bundle ();

	const uint8_t *buffer = NULL;
	uint32_t data_len = 0;
	if (!mono_bundled_resources_get_data_resource_values ("icudt.dat", &buffer, &data_len)) {
		printf("Could not load icudt.dat from the bundle");
		assert(buffer);
	}
#else /* WASM_SINGLE_FILE */
	FILE *fileptr;
	unsigned char *buffer;
	long filelen;
	char filename[256];
	sprintf(filename, "./icudt.dat");

	fileptr = fopen(filename, "rb");
	if (fileptr == 0) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}

	fseek(fileptr, 0, SEEK_END);
	filelen = ftell(fileptr);
	rewind(fileptr);

	buffer = (unsigned char *)malloc(filelen * sizeof(char));
	if(!fread(buffer, filelen, 1, fileptr)) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}
	fclose(fileptr);
#endif /* WASM_SINGLE_FILE */

	assert(mono_wasi_load_icu_data(buffer));
}
#endif /* INVARIANT_GLOBALIZATION */

void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
	free (args);
	free (user_data);
}

static void
load_runtimeconfig (void)
{
#ifdef WASM_SINGLE_FILE
	mono_register_runtimeconfig_bin ();

	const uint8_t *buffer = NULL;
	uint32_t data_len = 0;
	if (!mono_bundled_resources_get_data_resource_values (RUNTIMECONFIG_BIN_FILE, &buffer, &data_len)) {
		printf("Could not load " RUNTIMECONFIG_BIN_FILE " from the bundle\n");
		assert(buffer);
	}

	MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
	arg->kind = 1; // kind: image pointer
	arg->runtimeconfig.data.data = buffer;
	arg->runtimeconfig.data.data_len = data_len;
	monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, NULL);
#else
	const char *file_name = RUNTIMECONFIG_BIN_FILE;
	int str_len = strlen (file_name) + 1; // +1 is for the "/"
	char *file_path = (char *)malloc (sizeof (char) * (str_len +1)); // +1 is for the terminating null character
	int num_char = snprintf (file_path, (str_len + 1), "/%s", file_name);
	struct stat buffer;

	assert (num_char > 0 && num_char == str_len);

	if (stat (file_path, &buffer) == 0) {
		MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
		arg->kind = 0;
		arg->runtimeconfig.name.path = file_path;
		monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, file_path);
	} else {
		free (file_path);
	}
#endif
}

void
mono_wasm_load_runtime (const char *unused, int debug_level)
{
	const char *interp_opts = "";

#ifndef INVARIANT_GLOBALIZATION
	char* invariant_globalization = monoeg_g_getenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
	if (strcmp(invariant_globalization, "true") != 0 && strcmp(invariant_globalization, "1") != 0)
		load_icu_data();
#endif /* INVARIANT_GLOBALIZATION */


	char* debugger_fd = monoeg_g_getenv ("DEBUGGER_FD");
	if (debugger_fd != 0)
	{
		const char *debugger_str = "--debugger-agent=transport=wasi_socket,debugger_fd=%-2s,loglevel=0";
		char *debugger_str_with_fd = (char *)malloc (sizeof (char) * (strlen(debugger_str) + strlen(debugger_fd) + 1));
		snprintf (debugger_str_with_fd, strlen(debugger_str) + strlen(debugger_fd) + 1, debugger_str, debugger_fd);
		mono_jit_parse_options (1, &debugger_str_with_fd);
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);
		// Disable optimizations which interfere with debugging
		interp_opts = "-all";
		free (debugger_str_with_fd);
	}
	// When the list of app context properties changes, please update RuntimeConfigReservedProperties for
	// target _WasmGenerateRuntimeConfig in BrowserWasmApp.targets file
	const char *appctx_keys[2];
	appctx_keys [0] = "APP_CONTEXT_BASE_DIRECTORY";
	appctx_keys [1] = "RUNTIME_IDENTIFIER";

	const char *appctx_values[2];
	appctx_values [0] = "/";
	appctx_values [1] = "wasi-wasm";

	load_runtimeconfig();
	monovm_initialize (2, appctx_keys, appctx_values);

#ifndef INVARIANT_TIMEZONE
	mono_register_timezones_bundle ();
#endif /* INVARIANT_TIMEZONE */
#ifdef WASM_SINGLE_FILE
	mono_register_assemblies_bundle ();
#endif

	root_domain = mono_wasm_load_runtime_common (debug_level, wasi_trace_logger, interp_opts);
}

MonoMethod*
mono_wasi_assembly_get_entry_point (MonoAssembly *assembly)
{
	MonoImage *image;
	MonoMethod *method;

	MONO_ENTER_GC_UNSAFE;
	image = mono_assembly_get_image (assembly);
	uint32_t entry = mono_image_get_entry_point (image);
	if (!entry)
		goto end;

	mono_domain_ensure_entry_assembly (root_domain, assembly);
	method = mono_get_method (image, entry, NULL);

	end:
	MONO_EXIT_GC_UNSAFE;
	return method;
}

int
mono_unbox_int (MonoObject *obj)
{
	if (!obj)
		return 0;
	MonoType *type = mono_class_get_type (mono_object_get_class(obj));

	void *ptr = mono_object_unbox (obj);
	switch (mono_type_get_type (type)) {
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
		return *(signed char*)ptr;
	case MONO_TYPE_U1:
		return *(unsigned char*)ptr;
	case MONO_TYPE_I2:
		return *(short*)ptr;
	case MONO_TYPE_U2:
		return *(unsigned short*)ptr;
	case MONO_TYPE_I4:
	case MONO_TYPE_I:
		return *(int*)ptr;
	case MONO_TYPE_U4:
		return *(unsigned int*)ptr;
	case MONO_TYPE_CHAR:
		return *(short*)ptr;
	default:
		printf ("Invalid type %d to mono_unbox_int\n", mono_type_get_type (type));
		return 0;
	}
}


void
mono_wasm_string_get_data_ref (
	MonoString **string, mono_unichar2 **outChars, int *outLengthBytes, int *outIsInterned
) {
	MONO_ENTER_GC_UNSAFE;
	if (!string || !(*string)) {
		if (outChars)
			*outChars = 0;
		if (outLengthBytes)
			*outLengthBytes = 0;
		if (outIsInterned)
			*outIsInterned = 1;
	} else {
	if (outChars)
		*outChars = mono_string_chars (*string);
	if (outLengthBytes)
		*outLengthBytes = mono_string_length (*string) * 2;
	if (outIsInterned)
		*outIsInterned = mono_string_instance_is_interned (*string);
	}
	MONO_EXIT_GC_UNSAFE;
}

void
mono_wasm_string_get_data (
	MonoString *string, mono_unichar2 **outChars, int *outLengthBytes, int *outIsInterned
) {
	mono_wasm_string_get_data_ref(&string, outChars, outLengthBytes, outIsInterned);
}

void add_assembly(const char* base_dir, const char *name) {
	FILE *fileptr;
	unsigned char *buffer;
	long filelen;
	char filename[256];
	sprintf(filename, "%s/%s", base_dir, name);
	// printf("Loading %s...\n", filename);

	fileptr = fopen(filename, "rb");
	if (fileptr == 0) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}

	fseek(fileptr, 0, SEEK_END);
	filelen = ftell(fileptr);
	rewind(fileptr);

	buffer = (unsigned char *)malloc(filelen * sizeof(char));
	if(!fread(buffer, filelen, 1, fileptr)) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}
	fclose(fileptr);

	assert(mono_wasm_add_assembly(name, buffer, filelen));
}

MonoMethod* lookup_dotnet_method(const char* assembly_name, const char* namespace, const char* type_name, const char* method_name, int num_params) {
	MonoAssembly* assembly = mono_wasm_assembly_load (assembly_name);
	assert (assembly);
	MonoClass* class = mono_wasm_assembly_find_class (assembly, namespace, type_name);
	assert (class);
	MonoMethod* method = mono_wasm_assembly_find_method (class, method_name, num_params);
	assert (method);
	return method;
}

MonoArray*
mono_wasm_string_array_new (int size)
{
	return mono_array_new (root_domain, mono_get_string_class (), size);
}
