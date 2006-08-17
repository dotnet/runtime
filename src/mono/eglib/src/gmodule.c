/*
 * gmodule.c: dl* functions, glib style
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
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

