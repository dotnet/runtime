// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/component/event_pipe.h"
#include "mono/component/diagnostics_server.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/components.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"

typedef MonoComponent * (*MonoComponentInitFn) (void);

typedef struct _MonoComponentEntry {
	const char *name;
	MonoComponentInitFn init;
	MonoComponent **component;
	MonoDl *lib;
} MonoComponentEntry;

#ifdef STATIC_COMPONENTS
#define COMPONENT_INIT_FUNC(name) (MonoComponentInitFn) mono_component_ ## name ## _init
#else
#define COMPONENT_INIT_FUNC(name) (MonoComponentInitFn) mono_component_ ## name ## _stub_init
#endif

#define HOT_RELOAD_MODULE_NAME "hot_reload"
MonoComponentHotReload *hot_reload = NULL;

#ifdef ENABLE_PERFTRACING
MonoComponentEventPipe *event_pipe = NULL;
MonoComponentDiagnosticsServer *diagnostics_server = NULL;

// DiagnosticsServer component currently hosted by EventPipe module.
#define EVENT_PIPE_MODULE_NAME "event_pipe"
#define DIAGNOSTICS_SERVER_MODULE_NAME EVENT_PIPE_MODULE_NAME
#endif

/* One per component */
MonoComponentEntry components[] = {
	{ HOT_RELOAD_MODULE_NAME, COMPONENT_INIT_FUNC (hot_reload), (MonoComponent**)&hot_reload, NULL },
#ifdef ENABLE_PERFTRACING
	{ EVENT_PIPE_MODULE_NAME, COMPONENT_INIT_FUNC (event_pipe), (MonoComponent**)&event_pipe, NULL },
	{ DIAGNOSTICS_SERVER_MODULE_NAME, COMPONENT_INIT_FUNC (diagnostics_server), (MonoComponent**)&diagnostics_server, NULL },
#endif
};

#ifndef STATIC_COMPONENTS
static MonoComponent*
get_component (const char *component_name, MonoDl **component_lib);
#endif

void
mono_components_init (void)
{
#ifdef STATIC_COMPONENTS
	/* directly call each components init function */
	/* TODO: support disabled components. 
	 *
	 * The issue here is that we need to do static linking, so if we don't
	 * directly reference mono_component_<component_name>_init anywhere (ie
	 * if we dlsym from RTLD_DEFAULT) the static linking of the final
	 * binary won't actually include that symbol (unless we play
	 * platform-specific linker tricks).
	 *
	 * So maybe we will need some API hook so that embedders need to call
	 * to pass us the address of each component that isn't disabled.
	 *
	 */
	for (int i = 0; i < G_N_ELEMENTS (components); ++i)
		*components [i].component = components [i].init ();
#else
	/* call get_component for each component and init it or its stubs and add it to loaded_components */
	MonoDl *lib = NULL;
	
	for (int i = 0; i < G_N_ELEMENTS (components); ++i) {
		*components [i].component = get_component (components [i].name, &lib);
		components [i].lib = lib;
		if (!*components [i].component)
			*components [i].component = components [i].init ();
	}
#endif
}

void
mono_components_cleanup (void)
{
	/* call each components cleanup fn */
	for (int i = 0; i < G_N_ELEMENTS (components); ++i) {
		if (*components [i].component)
			(*components [i].component)->cleanup (*components [i].component);
		if (components [i].lib)
			mono_dl_close (components [i].lib);
	}
}

static char*
component_init_name (const char *component)
{
	return g_strdup_printf ("mono_component_%s_init", component);
}

static gpointer
load_component_entrypoint (MonoDl *lib, const char *component_name)
{
	char *component_init = component_init_name (component_name);
	gpointer sym = NULL;
	char *error_msg = mono_dl_symbol (lib, component_init, &sym);
	if (error_msg) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s library does not have symbol %s: %s", component_name, component_init, error_msg);
		g_free (error_msg);
		g_free (component_init);
		return NULL;
	}
	g_free (component_init);
	return sym;
}

#ifndef STATIC_COMPONENTS
static char*
component_library_base_name (const char *component)
{
	return g_strdup_printf ("mono-component-%s", component);
}

static char *
components_dir (void)
{
	static char *dir = NULL;
	if (!dir) {
		/* FIXME: this is right for self-contained apps, but if we're
		 * started by a host, the components are next to
		 * libmonosgen-2.0.so, not next to the host app.
		 */
		char buf[4096];
		if (mono_dl_get_executable_path (buf, sizeof(buf)) != -1) {
			char *resolvedname = mono_path_resolve_symlinks (buf);
			dir = g_path_get_dirname (resolvedname);
			g_free (resolvedname);
		}
	}
	return dir;
}

static MonoDl*
try_load (const char* dir, const char *component_name, const char* component_base_lib)
{
	MonoDl *lib = NULL;
	void *iter = NULL;
	char *path = NULL;
	while ((path = mono_dl_build_path (dir, component_base_lib, &iter)) && !lib) {
		char *error_msg = NULL;
		lib = mono_dl_open (path, MONO_DL_EAGER, &error_msg);
		if (!lib) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s not found: %s", component_name, error_msg);
			g_free (error_msg);
			continue;
		}
	}
	if (lib)
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s found at %s", component_name, path);
	g_free (path);
	return lib;
}

static MonoComponentInitFn
load_component (const char *component_name, MonoDl **lib_out)
{
	char *component_base_lib = component_library_base_name (component_name);
	MonoComponentInitFn result = NULL;

	/* FIXME: just copy what mono_profiler_load does, assuming it works */

	/* FIXME: do I need to provide a path? */
	MonoDl *lib = NULL;
	lib = try_load (components_dir (), component_name, component_base_lib);
	if (!lib)
		lib = try_load (NULL, component_name, component_base_lib);

	g_free (component_base_lib);
	if (!lib)
		goto done;

	gpointer sym = load_component_entrypoint (lib, component_name);

	result = (MonoComponentInitFn)sym;
	*lib_out = lib;
done:
	return result;
}

MonoComponent*
get_component (const char *component_name, MonoDl **lib_out)
{
	MonoComponentInitFn initfn = load_component (component_name, lib_out);
	if (!initfn)
		return NULL;
	return initfn();
}
#endif
