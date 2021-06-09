// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include <gmodule.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/component/event_pipe.h"
#include "mono/component/diagnostics_server.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/components.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"
#include "mono/utils/mono-time.h"

static gint64 event_pipe_100ns_ticks;

typedef MonoComponent * (*MonoComponentInitFn) (void);

typedef struct _MonoComponentEntry {
	const char *lib_name;
	const char *name;
	MonoComponentInitFn init;
	MonoComponent **component;
	MonoDl *lib;
} MonoComponentEntry;

#define COMPONENT_INIT_FUNC(name) (MonoComponentInitFn) mono_component_ ## name ## _init

#define HOT_RELOAD_LIBRARY_NAME "hot_reload"
#define HOT_RELOAD_COMPONENT_NAME HOT_RELOAD_LIBRARY_NAME
MonoComponentHotReload *hot_reload = NULL;

MonoComponentEventPipe *event_pipe = NULL;
MonoComponentDiagnosticsServer *diagnostics_server = NULL;

// DiagnosticsServer/EventPipe components currently hosted by diagnostics_tracing library.
#define DIAGNOSTICS_TRACING_LIBRARY_NAME "diagnostics_tracing"
#define EVENT_PIPE_COMPONENT_NAME "event_pipe"
#define DIAGNOSTICS_SERVER_COMPONENT_NAME "diagnostics_server"

/* One per component */
MonoComponentEntry components[] = {
	{ HOT_RELOAD_LIBRARY_NAME, HOT_RELOAD_COMPONENT_NAME, COMPONENT_INIT_FUNC (hot_reload), (MonoComponent**)&hot_reload, NULL },
	{ DIAGNOSTICS_TRACING_LIBRARY_NAME, EVENT_PIPE_COMPONENT_NAME, COMPONENT_INIT_FUNC (event_pipe), (MonoComponent**)&event_pipe, NULL },
	{ DIAGNOSTICS_TRACING_LIBRARY_NAME, DIAGNOSTICS_SERVER_COMPONENT_NAME, COMPONENT_INIT_FUNC (diagnostics_server), (MonoComponent**)&diagnostics_server, NULL },
};

#ifndef STATIC_COMPONENTS
static MonoComponent*
get_component (const MonoComponentEntry *component, MonoDl **component_lib);
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
		*components [i].component = get_component (&components [i], &lib);
		components [i].lib = lib;
		if (!*components [i].component)
			*components [i].component = components [i].init ();
	}
#endif
	/* validate components interface version */
	for (int i = 0; i < G_N_ELEMENTS (components); ++i) {
		guint64 version = (guint64)(*components [i].component)->itf_version;
		g_assertf (version == MONO_COMPONENT_ITF_VERSION, "%s component returned unexpected interface version (expected %" PRIu64 " got %" PRIu64 ")", components [i].name, (guint64)MONO_COMPONENT_ITF_VERSION, version);
	}
}

static char*
component_init_name (const MonoComponentEntry *component)
{
	return g_strdup_printf ("mono_component_%s_init", component->name);
}

static gpointer
load_component_entrypoint (MonoDl *lib, const MonoComponentEntry *component)
{
	char *component_init = component_init_name (component);
	gpointer sym = NULL;
	char *error_msg = mono_dl_symbol (lib, component_init, &sym);
	if (error_msg) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s library does not have symbol %s: %s", component->name, component_init, error_msg);
		g_free (error_msg);
		g_free (component_init);
		return NULL;
	}
	g_free (component_init);
	return sym;
}

#ifndef STATIC_COMPONENTS
static char*
component_library_base_name (const MonoComponentEntry *component)
{
	return g_strdup_printf ("mono-component-%s", component->lib_name);
}

static char *
components_dir (void)
{
	static char *dir = NULL;
	if (!dir) {
		char buf[4096];
		if (g_module_address ((void *)components_dir, buf, sizeof (buf), NULL, NULL, 0, NULL)) {
			char *resolvedname = mono_path_resolve_symlinks (buf);
			dir = g_path_get_dirname (resolvedname);
			g_free (resolvedname);
		}
	}
	return dir;
}

static MonoDl*
try_load (const char* dir, const MonoComponentEntry *component, const char* component_base_lib)
{
	MonoDl *lib = NULL;
	char *path = NULL;
	path = g_module_build_path (dir, component_base_lib);
	if (path) {
		char *error_msg = NULL;
		lib = mono_dl_open (path, MONO_DL_EAGER | MONO_DL_LOCAL, &error_msg);
		if (!lib) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s not found: %s", component->name, error_msg);
		}
		g_free (error_msg);
	}
	if (lib)
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s found at %s", component->name, path);
	g_free (path);
	return lib;
}

static MonoComponentInitFn
load_component (const MonoComponentEntry *component, MonoDl **lib_out)
{
	// If init method has been static linked not using stub library, use that instead of dynamic component.
	if (component->init() && component->init()->available ()) {
		*lib_out = NULL;
		return component->init;
	}

	char *component_base_lib = component_library_base_name (component);
	MonoComponentInitFn result = NULL;

	/* FIXME: just copy what mono_profiler_load does, assuming it works */

	/* FIXME: do I need to provide a path? */
	MonoDl *lib = NULL;
	lib = try_load (components_dir (), component, component_base_lib);
	if (!lib)
		lib = try_load (NULL, component, component_base_lib);

	g_free (component_base_lib);
	if (!lib)
		goto done;

	gpointer sym = load_component_entrypoint (lib, component);

	result = (MonoComponentInitFn)sym;
	*lib_out = lib;
done:
	return result;
}

MonoComponent*
get_component (const MonoComponentEntry *component, MonoDl **lib_out)
{
	MonoComponentInitFn initfn = load_component (component, lib_out);
	if (!initfn)
		return NULL;
	return initfn();
}
#endif

void
mono_component_event_pipe_100ns_ticks_start (void)
{
	event_pipe_100ns_ticks = mono_100ns_ticks ();
}

gint64
mono_component_event_pipe_100ns_ticks_stop (void)
{
	return mono_100ns_ticks () - event_pipe_100ns_ticks;
}
