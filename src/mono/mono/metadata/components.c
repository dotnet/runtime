// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <glib.h>
#include "mono/component/component.h"
#include "mono/component/hot_reload.h"
#include "mono/metadata/components.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-logger-internals.h"

typedef MonoComponent * (*MonoComponentInitFn) (void);

/* List of MonoDl* for each loaded component */
static GSList *loaded_components;

/* One static per component */
static MonoComponentHotReload *hot_reload;

static MonoComponentInitFn
get_component (const char *component_name, MonoDl **component_lib);

void
mono_components_init (void)
{
#ifdef STATIC_LINK_COMPONENTS
	/* directly call each components init function */
	hot_reload = mono_component_hot_reload_init ();
#else
	/* call get_component for each component and init it or its stubs and add it to loaded_components  */
	MonoDl *lib = NULL;
	
	/* Repeat for each component */
	hot_reload = (MonoComponentHotReload*)get_component ("hot_reload", &lib);
	if (hot_reload) {
		loaded_components = g_slist_prepend (loaded_components, lib);
	} else {
		hot_reload = mono_component_hot_reload_stub_init ();
	}
		
#endif
}


void
mono_components_cleanup (void)
{
	/* call each components cleanup fn */
	if (hot_reload) {
		hot_reload->component.cleanup (&hot_reload->component);
		hot_reload = NULL;
	}
#ifndef STATIC_LINK_COMPONENTS
	for (GSList *p = loaded_components; p != NULL; p = p->next) {
		mono_dl_close ((MonoDl*)p->data);
	}
#endif
}

static char*
component_library_name (const char *component)
{
	return g_strdup_printf ("mono-component-%s%s", component, MONO_SOLIB_EXT);
}

static char*
component_init_name (const char *component)
{
	return g_strdup_printf ("mono_component_%s_init", component);
}

MonoComponentInitFn
get_component (const char *component_name, MonoDl **lib_out)
{
	char *component_lib = component_library_name (component_name);
	char *error_msg = NULL;
	MonoComponentInitFn result = NULL;
	MonoDl *lib = mono_dl_open (component_lib, MONO_DL_EAGER, &error_msg);
	if (!lib) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s not found: %s", component_name, error_msg);
		g_free (error_msg);
		g_free (component_lib);
		goto done;
	}

	char *component_init = component_init_name (component_name);
	gpointer sym = NULL;
	error_msg = mono_dl_symbol (lib, component_init, &sym);
	if (error_msg) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Component %s library does not have symbol %s: %s", component_name, component_init, error_msg);
		g_free (error_msg);
		g_free (component_init);
		goto done;
	}

	result = sym;
	*lib_out = lib;
done:
	return result;
}
