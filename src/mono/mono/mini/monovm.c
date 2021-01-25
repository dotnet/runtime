#include <config.h>
#include <mono/utils/mono-compiler.h>
#include "monovm.h"

#if ENABLE_NETCORE

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/native-library.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/mini.h>
#include <mono/utils/mono-logger-internals.h>

typedef struct {
	int assembly_count;
	char **basenames; /* Foo.dll */
	int *basename_lens;
	char **assembly_filepaths; /* /blah/blah/blah/Foo.dll */
} MonoCoreTrustedPlatformAssemblies;

typedef struct {
	int dir_count;
	char **dirs;
} MonoCoreLookupPaths;

static MonoCoreTrustedPlatformAssemblies *trusted_platform_assemblies;
static MonoCoreLookupPaths *native_lib_paths;
static MonoCoreLookupPaths *app_paths;
static MonoCoreLookupPaths *app_ni_paths;
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
	a->basename_lens = g_new0 (int, asm_count + 1);
	for (int i = 0; i < asm_count; ++i) {
		a->basenames [i] = g_path_get_basename (a->assembly_filepaths [i]);
		a->basename_lens [i] = strlen (a->basenames [i]);
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
mono_core_preload_hook (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, char **assemblies_path, gboolean refonly, gpointer user_data, MonoError *error)
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
	g_assert (!refonly);
	/* alc might be a user ALC - we get here from alc.LoadFromAssemblyName(), but we should load TPA assemblies into the default alc */
	MonoAssemblyLoadContext *default_alc;
	default_alc = mono_domain_default_alc (mono_alc_domain (alc));

	basename = g_strconcat (aname->name, ".dll", (const char*)NULL); /* TODO: make sure CoreCLR never needs to load .exe files */

	size_t basename_len;
	basename_len = strlen (basename);

	for (int i = 0; i < a->assembly_count; ++i) {
		if (basename_len == a->basename_lens [i] && !g_strncasecmp (basename, a->basenames [i], a->basename_lens [i])) {
			MonoAssemblyOpenRequest req;
			mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, default_alc);
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
	mono_install_assembly_preload_hook_v2 (mono_core_preload_hook, (void*)trusted_platform_assemblies, FALSE, FALSE);
}

static gboolean
parse_properties (int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	// A partial list of relevant properties is at:
	// https://docs.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting#step-3---prepare-runtime-properties

	for (int i = 0; i < propertyCount; ++i) {
		size_t prop_len = strlen (propertyKeys [i]);
		if (prop_len == 27 && !strncmp (propertyKeys [i], "TRUSTED_PLATFORM_ASSEMBLIES", 27)) {
			parse_trusted_platform_assemblies (propertyValues[i]);
		} else if (prop_len == 9 && !strncmp (propertyKeys [i], "APP_PATHS", 9)) {
			app_paths = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 12 && !strncmp (propertyKeys [i], "APP_NI_PATHS", 12)) {
			app_ni_paths = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 23 && !strncmp (propertyKeys [i], "PLATFORM_RESOURCE_ROOTS", 23)) {
			platform_resource_roots = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 29 && !strncmp (propertyKeys [i], "NATIVE_DLL_SEARCH_DIRECTORIES", 29)) {
			native_lib_paths = parse_lookup_paths (propertyValues [i]);
		} else if (prop_len == 16 && !strncmp (propertyKeys [i], "PINVOKE_OVERRIDE", 16)) {
			PInvokeOverrideFn override_fn = (PInvokeOverrideFn)(uintptr_t)strtoull (propertyValues [i], NULL, 0);
			mono_loader_install_pinvoke_override (override_fn);
		} else if (prop_len == 30 && !strncmp (propertyKeys [i], "System.Globalization.Invariant", 30)) {
			// TODO: Ideally we should propagate this through AppContext options
			g_setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", propertyValues [i], TRUE);
		} else if (prop_len == 27 && !strncmp (propertyKeys [i], "System.Globalization.UseNls", 27)) {
			// TODO: Ideally we should propagate this through AppContext options
			g_setenv ("DOTNET_SYSTEM_GLOBALIZATION_USENLS", propertyValues [i], TRUE);
		} else if (prop_len == 32 && !strncmp (propertyKeys [i], "System.Globalization.AppLocalIcu", 32)) {
			// TODO: Ideally we should propagate this through AppContext options
			g_setenv ("DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", propertyValues [i], TRUE);
		} else {
#if 0
			// can't use mono logger, it's not initialized yet.
			printf ("\t Unprocessed property %03d '%s': <%s>\n", i, propertyKeys[i], propertyValues[i]);
#endif
		}
	}
	return TRUE;
}

int
monovm_initialize (int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	mono_runtime_register_appctx_properties (propertyCount, propertyKeys, propertyValues);

	if (!parse_properties (propertyCount, propertyKeys, propertyValues))
		return 0x80004005; /* E_FAIL */

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

#else

int
monovm_initialize (int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	return -1;
}

int
monovm_execute_assembly (int argc, const char **argv, const char *managedAssemblyPath, unsigned int *exitCode)
{
	return -1;
}

int
monovm_shutdown (int *latchedExitCode)
{
	return -1;
}

#endif // ENABLE_NETCORE
