#include <glib.h>
#include <dlfcn.h>

struct _GModule {
	void *handle;
};

GModule *
g_module_open (const gchar *file, GModuleFlags flags)
{
	int f = 0;
	GModule *module;

	flags &= G_MODULE_BIND_MASK;
	if ((flags & G_MODULE_BIND_LAZY) != 0)
		f |= RTLD_LAZY;
	if ((flags & G_MODULE_BIND_LOCAL) != 0)
		f |= RTLD_LOCAL;

	module = g_malloc (sizeof (GModule));
	if (module == NULL)
		return NULL;

	module->handle = dlopen (file, f);
	return module;
}

gboolean
g_module_symbol (GModule *module, const gchar *symbol_name, gpointer *symbol)
{
	if (symbol_name == NULL || symbol == NULL)
		return FALSE;

	if (module == NULL || module->handle == NULL)
		return FALSE;

	*symbol = dlsym (module->handle, symbol_name);
	return (*symbol != NULL);
}

const gchar *
g_module_error (void)
{
	return dlerror ();
}

gboolean
g_module_close (GModule *module)
{
	void *handle;
	if (module == NULL || module->handle == NULL)
		return FALSE;

	handle = module->handle;
	module->handle = NULL;
	return (0 == dlclose (handle));
}

gchar *
g_module_build_path (const gchar *directory, const gchar *module_name)
{
	if (module_name == NULL)
		return NULL;

	if (directory)
		return g_strdup_printf ("%s/lib%s.so", directory, module_name);
	return g_strdup_printf ("lib%s.so", module_name);
}

