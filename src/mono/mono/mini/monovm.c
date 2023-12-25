#include <config.h>
#include <mono/utils/mono-compiler.h>
#include "monovm.h"

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/native-library.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/webcil-loader.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/mini.h>
#include <mono/utils/mono-logger-internals.h>

#include <mono/metadata/components.h>

#include <corehost/host_runtime_contract.h>

static MonoCoreTrustedPlatformAssemblies *trusted_platform_assemblies;
static MonoCoreLookupPaths *native_lib_paths;
static MonoCoreLookupPaths *app_paths;
static MonoCoreLookupPaths *platform_resource_roots;

static void
mono_core_trusted_platform_assemblies_free (MonoCoreTrustedPlatformAssemblies *a)
{
	if (!a)
		return;
	g_strfreev (a->basenames);
	g_strfreev (a->assembly_filepaths);
	g_free (a);
}

static void
mono_core_lookup_paths_free (MonoCoreLookupPaths *dl)
{
	if (!dl)
		return;
	g_strfreev (dl->dirs);
	g_free (dl);
}

static gboolean
parse_trusted_platform_assemblies (const char *assemblies_paths)
{
	// From
	// https://docs.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting#step-3---prepare-runtime-properties
	// this is ';' separated on Windows and ':' separated elsewhere.
	char **parts = g_strsplit (assemblies_paths, G_SEARCHPATH_SEPARATOR_S, 0);
	int asm_count = 0;
	for (char **p = parts; *p != NULL && **p != '\0'; p++) {
#if 0
		const char *part = *p;
		// can't use logger, it's not initialized yet.
		printf ("\t\tassembly %d = <%s>\n", asm_count, part);
#endif
		asm_count++;
	}
	MonoCoreTrustedPlatformAssemblies *a = g_new0 (MonoCoreTrustedPlatformAssemblies, 1);
	a->assembly_count = asm_count;
	a->assembly_filepaths = parts;
	a->basenames = g_new0 (char*, asm_count + 1);
	a->basename_lens = g_new0 (uint32_t, asm_count + 1);
	for (int i = 0; i < asm_count; ++i) {
		a->basenames [i] = g_path_get_basename (a->assembly_filepaths [i]);
		a->basename_lens [i] = (uint32_t)strlen (a->basenames [i]);
	}
	a->basenames [asm_count] = NULL;
	a->basename_lens [asm_count] = 0;

	trusted_platform_assemblies = a;
	return TRUE;
}

static MonoCoreLookupPaths *
parse_lookup_paths (const char *search_path)
{
	char **parts = g_strsplit (search_path, G_SEARCHPATH_SEPARATOR_S, 0);
	int dir_count = 0;
	for (char **p = parts; *p != NULL && **p != '\0'; p++) {
#if 0
		const char *part = *p;
		// can't use logger, it's not initialized yet.
		printf ("\t\tnative search dir %d = <%s>\n", dir_count, part);
#endif
		dir_count++;
	}
	MonoCoreLookupPaths *dl = g_new0 (MonoCoreLookupPaths, 1);
	dl->dirs = parts;
	dl->dir_count = dir_count;
	return dl;
}

static MonoAssembly*
mono_core_preload_hook (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, char **assemblies_path, gpointer user_data, MonoError *error)
{
	MonoAssembly *result = NULL;
	MonoCoreTrustedPlatformAssemblies *a = (MonoCoreTrustedPlatformAssemblies *)user_data;
	/* TODO: check that CoreCLR wants the strong name semantics here */
	MonoAssemblyCandidatePredicate predicate = &mono_assembly_candidate_predicate_sn_same_name;
	void* predicate_ud = aname;
	char *basename = NULL;

	if (a == NULL) // no TPA paths set
		goto leave;

	g_assert (aname);
	g_assert (aname->name);
	/* alc might be a user ALC - we get here from alc.LoadFromAssemblyName(), but we should load TPA assemblies into the default alc */
	MonoAssemblyLoadContext *default_alc;
	default_alc = mono_alc_get_default ();

	basename = g_strconcat (aname->name, ".dll", (const char*)NULL); /* TODO: make sure CoreCLR never needs to load .exe files */

	size_t basename_len;
	basename_len = strlen (basename);

	for (guint32 i = 0; i < a->assembly_count; ++i) {
		if (basename_len == a->basename_lens [i] && !g_strncasecmp (basename, a->basenames [i], a->basename_lens [i])) {
			MonoAssemblyOpenRequest req;
			mono_assembly_request_prepare_open (&req, default_alc);
			req.request.predicate = predicate;
			req.request.predicate_ud = predicate_ud;

			const char *fullpath = a->assembly_filepaths [i];

			gboolean found = g_file_test (fullpath, G_FILE_TEST_IS_REGULAR);

			if (found) {
				MonoImageOpenStatus status;
				result = mono_assembly_request_open (fullpath, &req, &status);
				/* TODO: do something with the status at the end? */
				if (result)
					break;
			}
#ifdef ENABLE_WEBCIL
			else {
				/* /path/foo.dll -> /path/foo.webcil */
				size_t n = strlen (fullpath);
				if (n < strlen(".dll"))
					continue;
				n -= strlen(".dll");
				char *fullpath2 = g_malloc (n + strlen(".webcil") + 1);
				g_strlcpy (fullpath2, fullpath, n + 1);
				g_strlcpy (fullpath2 + n, ".webcil", strlen(".webcil") + 1);
				if (g_file_test (fullpath2, G_FILE_TEST_IS_REGULAR)) {
					MonoImageOpenStatus status;
					result = mono_assembly_request_open (fullpath2, &req, &status);
				}
				g_free (fullpath2);
				if (result)
					break;
				char *fullpath3 = g_malloc (n + strlen(MONO_WEBCIL_IN_WASM_EXTENSION) + 1);
				g_strlcpy (fullpath3, fullpath, n + 1);
				g_strlcpy (fullpath3 + n, MONO_WEBCIL_IN_WASM_EXTENSION, strlen(MONO_WEBCIL_IN_WASM_EXTENSION) + 1);
				if (g_file_test (fullpath3, G_FILE_TEST_IS_REGULAR)) {
					MonoImageOpenStatus status;
					result = mono_assembly_request_open (fullpath3, &req, &status);
				}
				g_free (fullpath3);
				if (result)
					break;
			}
#endif
		}
	}

leave:
	g_free (basename);

	if (!result) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "netcore preload hook: did not find '%s'.", aname->name);
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "netcore preload hook: loading '%s' from '%s'.", aname->name, result->image->name);
	}
	return result;
}

static void
install_assembly_loader_hooks (void)
{
	mono_install_assembly_preload_hook_v2 (mono_core_preload_hook, (void*)trusted_platform_assemblies, FALSE);
}

static gboolean
parse_properties (int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	// A partial list of relevant properties is at:
	// https://docs.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting#step-3---prepare-runtime-properties

	PInvokeOverrideFn override_fn = NULL;
	for (int i = 0; i < propertyCount; ++i) {
		size_t prop_len = strlen (propertyKeys [i]);
		if (prop_len == 27 && !strncmp (propertyKeys [i], HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES, 27)) {
			parse_trusted_platform_assemblies (propertyValues[i]);
		} else if (prop_len == 9 && !strncmp (propertyKeys [i], HOST_PROPERTY_APP_PATHS, 9)) {
			app_paths = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 23 && !strncmp (propertyKeys [i], HOST_PROPERTY_PLATFORM_RESOURCE_ROOTS, 23)) {
			platform_resource_roots = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 29 && !strncmp (propertyKeys [i], HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES, 29)) {
			native_lib_paths = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 16 && !strncmp (propertyKeys [i], HOST_PROPERTY_PINVOKE_OVERRIDE, 16)) {
			if (override_fn == NULL) {
				override_fn = (PInvokeOverrideFn)(uintptr_t)strtoull (propertyValues [i], NULL, 0);
			}
		} else if (prop_len == STRING_LENGTH(HOST_PROPERTY_RUNTIME_CONTRACT) && !strncmp (propertyKeys [i], HOST_PROPERTY_RUNTIME_CONTRACT, STRING_LENGTH(HOST_PROPERTY_RUNTIME_CONTRACT))) {
			// Functions in HOST_RUNTIME_CONTRACT have priority over the individual properties
			// for callbacks, so we set them as long as the contract has a non-null function.
			struct host_runtime_contract* contract = (struct host_runtime_contract*)(uintptr_t)strtoull (propertyValues [i], NULL, 0);
			if (contract->pinvoke_override != NULL) {
				override_fn = (PInvokeOverrideFn)contract->pinvoke_override;
			}
		} else {
#if 0
			// can't use mono logger, it's not initialized yet.
			printf ("\t Unprocessed property %03d '%s': <%s>\n", i, propertyKeys[i], propertyValues[i]);
#endif
		}
	}

	if (override_fn != NULL)
		mono_loader_install_pinvoke_override (override_fn);

	return TRUE;
}

static void
finish_initialization (void)
{
	install_assembly_loader_hooks ();

	if (native_lib_paths != NULL)
		mono_set_pinvoke_search_directories (native_lib_paths->dir_count, g_strdupv (native_lib_paths->dirs));
	// Our load hooks don't distinguish between normal, AOT'd, and satellite lookups the way CoreCLR's does.
	// For now, just set assemblies_path with APP_PATHS and leave the rest.
	if (app_paths != NULL)
		mono_set_assemblies_path_direct (g_strdupv (app_paths->dirs));

	/*
	 * Don't use Mono's legacy assembly name matching behavior - respect
	 * the requested version and culture.
	 */
	mono_loader_set_strict_assembly_name_check (TRUE);
}

int
monovm_initialize (int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	mono_runtime_register_appctx_properties (propertyCount, propertyKeys, propertyValues);

	if (!parse_properties (propertyCount, propertyKeys, propertyValues))
		return 0x80004005; /* E_FAIL */

	finish_initialization ();

	return 0;
}

int
monovm_initialize_preparsed (MonoCoreRuntimeProperties *parsed_properties, int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	mono_runtime_register_appctx_properties (propertyCount, propertyKeys, propertyValues);

	trusted_platform_assemblies = parsed_properties->trusted_platform_assemblies;
	app_paths = parsed_properties->app_paths;
	native_lib_paths = parsed_properties->native_dll_search_directories;
	mono_loader_install_pinvoke_override (parsed_properties->pinvoke_override);

	finish_initialization ();

	return 0;
}

// Initialize monovm with properties set by runtimeconfig.json. Primarily used by mobile targets.
int
monovm_runtimeconfig_initialize (MonovmRuntimeConfigArguments *arg, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void *user_data)
{
	mono_runtime_register_runtimeconfig_json_properties (arg, cleanup_fn, user_data);
	return 0;
}

int
monovm_execute_assembly (int argc, const char **argv, const char *managedAssemblyPath, unsigned int *exitCode)
{
	if (exitCode == NULL)
	{
		return -1;
	}

	//
	// Make room for program name and executable assembly
	//
	int mono_argc = argc + 2;

	char **mono_argv = (char **) malloc (sizeof (char *) * (mono_argc + 1 /* null terminated */));
	const char **ptr = (const char **) mono_argv;

	*ptr++ = NULL;

	// executable assembly
	*ptr++ = (char*) managedAssemblyPath;

	// the rest
	for (int i = 0; i < argc; ++i)
		*ptr++ = argv [i];

	*ptr = NULL;

	mono_parse_env_options (&mono_argc, &mono_argv);

	// TODO: Should be return code of Main only (mono_jit_exec result)
	*exitCode = mono_main (mono_argc, mono_argv);

	return 0;
}

int
monovm_shutdown (int *latchedExitCode)
{
	*latchedExitCode = mono_environment_exitcode_get ();

	return 0;
}

static int
monovm_create_delegate_impl (const char* assemblyName, const char* typeName, const char *methodName, void **delegate);


int
monovm_create_delegate (const char *assemblyName, const char *typeName, const char *methodName, void **delegate)
{
	int result;
	/* monovm_create_delegate may be called instead of monovm_execute_assembly.  Initialize the
	 * runtime if it isn't already. */
	if (!mono_get_root_domain())
		mini_init (assemblyName);
	MONO_ENTER_GC_UNSAFE;
	result = monovm_create_delegate_impl (assemblyName, typeName, methodName, delegate);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

int
monovm_create_delegate_impl (const char* assemblyName, const char* typeName, const char *methodName, void **delegate)
{
	// Load an assembly and a type and a method from the type, then return a pointer to a native
	// callable version of the method.  See CorHost2::CreateDelegate
	//
	// We have to do this in general, but the CoreCLR hostpolicy only calls this with a handful
	// of methods from the [CoreLib]Internal.Runtime.InteropServices.ComponentActivator
	// class. See hostpolicy.cpp get_delegate()

	ERROR_DECL (error);

	const int failure = 0x80004005; /* E_FAIL */

	if (!delegate)
		return failure;
	*delegate = NULL;

	MonoAssemblyLoadContext *alc = mono_alc_get_default ();

	MonoImage *image;
	if (!strcmp (MONO_ASSEMBLY_CORLIB_NAME, assemblyName)) {
		image = mono_defaults.corlib;
	} else {
		MonoAssemblyName aname = {0};
		aname.name = assemblyName;
		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare_byname (&req, alc);
		MonoImageOpenStatus status = MONO_IMAGE_OK;
		MonoAssembly *assm = mono_assembly_request_byname (&aname, &req, &status);

		if (!assm || status != MONO_IMAGE_OK)
			return failure;
		image = assm->image;
	}

	MonoType *t = mono_reflection_type_from_name_checked ((char*)typeName, alc, image, error);
	goto_if_nok (error, fail);

	g_assert (t);
	MonoClass *klass = mono_class_from_mono_type_internal (t);


	MonoMethod *method = mono_class_get_method_from_name_checked (klass, methodName, -1, 0, error);
	goto_if_nok (error, fail);

	if (!mono_method_has_unmanaged_callers_only_attribute (method)) {
		mono_error_set_not_supported (error, "MonoVM only supports UnmanagedCallersOnly implementations of hostfxr_get_runtime_delegate delegate types");
		goto fail;
	}

	MonoClass *delegate_klass = NULL;
	MonoGCHandle target_handle = 0;
	MonoMethod *wrapper = mono_marshal_get_managed_wrapper (method, delegate_klass, target_handle, error);
	goto_if_nok (error, fail);

	gpointer addr = mono_compile_method_checked (wrapper, error);
	goto_if_nok (error, fail);

	*delegate = addr;
	return 0;

fail:
	g_warning ("coreclr_create_delegate: failed due to %s", mono_error_get_message (error));
	mono_error_cleanup (error);
	return failure;
}
