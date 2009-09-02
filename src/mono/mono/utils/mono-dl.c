/*
 * mono-dl.c: Interface to the dynamic linker
 *
 * Author:
 *    Mono Team (http://www.mono-project.com)
 *
 * Copyright 2001-2004 Ximian, Inc.
 * Copyright 2004-2009 Novell, Inc.
 */
#include "config.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

#ifdef PLATFORM_WIN32
#define SOPREFIX ""
static const char suffixes [][5] = {
	".dll"
};
#elif defined(__APPLE__)
#define SOPREFIX "lib"
static const char suffixes [][8] = {
	".dylib",
	".so",
	".bundle"
};
#elif EMBEDDED_PINVOKE
#define SOPREFIX ""
static const char suffixes [][1] = {
	""
};
#else
#define SOPREFIX "lib"
static const char suffixes [][4] = {
	".so"
};
#endif

#ifdef PLATFORM_WIN32

#include <windows.h>
#include <psapi.h>

#define SO_HANDLE_TYPE HMODULE
#define LL_SO_OPEN(file,flags) w32_load_module ((file), (flags))
#define LL_SO_CLOSE(module) do { if (!(module)->main_module) FreeLibrary ((module)->handle); } while (0)
#define LL_SO_SYMBOL(module, name) w32_find_symbol ((module), (name))
#define LL_SO_TRFLAGS(flags) 0
#define LL_SO_ERROR() w32_dlerror ()

#elif defined (HAVE_DL_LOADER)

#include <dlfcn.h>

#ifndef RTLD_LAZY
#define RTLD_LAZY       1
#endif  /* RTLD_LAZY */

#define SO_HANDLE_TYPE void*
#define LL_SO_OPEN(file,flags) dlopen ((file), (flags))
#define LL_SO_CLOSE(module) dlclose ((module)->handle)
#define LL_SO_SYMBOL(module, name) dlsym ((module)->handle, (name))
#define LL_SO_TRFLAGS(flags) convert_flags ((flags))
#define LL_SO_ERROR() g_strdup (dlerror ())

static int
convert_flags (int flags)
{
	int lflags = flags & MONO_DL_LOCAL? 0: RTLD_GLOBAL;

	if (flags & MONO_DL_LAZY)
		lflags |= RTLD_LAZY;
	else
		lflags |= RTLD_NOW;
	return lflags;
}

#elif EMBEDDED_PINVOKE
#define SO_HANDLE_TYPE void*
void *LL_SO_OPEN   (const char *file, int flags);
int   LL_SO_CLOSE  (void *handle);
#define LL_SO_SYMBOL(module,symbol) _LL_SO_SYMBOL((module)->handle, (symbol))
void *_LL_SO_SYMBOL (void *handle, const char *symbol);
char *LL_SO_ERROR();
#define LL_SO_TRFLAGS(flags)      0

#else
/* no dynamic loader supported */
#define SO_HANDLE_TYPE void*
#define LL_SO_OPEN(file,flags) NULL
#define LL_SO_CLOSE(module) 
#define LL_SO_SYMBOL(module, name) NULL
#define LL_SO_TRFLAGS(flags) (flags)
#define LL_SO_ERROR() g_strdup ("No support for dynamic loader")

#endif

struct _MonoDl {
	SO_HANDLE_TYPE handle;
	int main_module;
};

#ifdef PLATFORM_WIN32

static char*
w32_dlerror (void)
{
	char* ret = NULL;
	wchar_t* buf = NULL;
	DWORD code = GetLastError ();

	if (FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&buf, 0, NULL))
	{
		ret = g_utf16_to_utf8 (buf, wcslen(buf), NULL, NULL, NULL);
		LocalFree (buf);
	} else {
		g_assert_not_reached ();
	}
	return ret;
}

static gpointer
w32_find_symbol (MonoDl *module, const gchar *symbol_name)
{
	HMODULE *modules;
	DWORD buffer_size = sizeof (HMODULE) * 1024;
	DWORD needed, i;
	gpointer proc = NULL;

	/* get the symbol directly from the specified module */
	if (!module->main_module)
		return GetProcAddress (module->handle, symbol_name);

	/* get the symbol from the main module */
	proc = GetProcAddress (module->handle, symbol_name);
	if (proc != NULL)
		return proc;

	/* get the symbol from the loaded DLLs */
	modules = (HMODULE *) g_malloc (buffer_size);
	if (modules == NULL)
		return NULL;

	if (!EnumProcessModules (GetCurrentProcess (), modules,
				 buffer_size, &needed)) {
		g_free (modules);
		return NULL;
	}

	/* check whether the supplied buffer was too small, realloc, retry */
	if (needed > buffer_size) {
		g_free (modules);

		buffer_size = needed;
		modules = (HMODULE *) g_malloc (buffer_size);

		if (modules == NULL)
			return NULL;

		if (!EnumProcessModules (GetCurrentProcess (), modules,
					 buffer_size, &needed)) {
			g_free (modules);
			return NULL;
		}
	}

	for (i = 0; i < needed / sizeof (HANDLE); i++) {
		proc = GetProcAddress (modules [i], symbol_name);
		if (proc != NULL) {
			g_free (modules);
			return proc;
		}
	}

	g_free (modules);
	return NULL;
}


static gpointer
w32_load_module (const char* file, int flags)
{
	gpointer hModule = NULL;
	if (file) {
		gunichar2* file_utf16 = g_utf8_to_utf16 (file, strlen (file), NULL, NULL, NULL);
		guint last_sem = SetErrorMode (SEM_FAILCRITICALERRORS);
		guint32 last_error = 0;

		hModule = LoadLibrary (file_utf16);
		if (!hModule)
			last_error = GetLastError ();

		SetErrorMode (last_sem);
		g_free (file_utf16);

		if (!hModule)
			SetLastError (last_error);
	} else {
		hModule = GetModuleHandle (NULL);
	}
	return hModule;
}
#endif

/*
 * read a value string from line with any of the following formats:
 * \s*=\s*'string'
 * \s*=\s*"string"
 * \s*=\s*non_white_space_string
 */
static char*
read_string (char *p, FILE *file)
{
	char *endp;
	char *startp;
	while (*p && isspace (*p))
		++p;
	if (*p == 0)
		return NULL;
	if (*p == '=')
		p++;
	while (*p && isspace (*p))
		++p;
	if (*p == '\'' || *p == '"') {
		char t = *p;
		p++;
		startp = p;
		endp = strchr (p, t);
		/* FIXME: may need to read more from file... */
		if (!endp)
			return NULL;
		*endp = 0;
		return g_memdup (startp, (endp - startp) + 1);
	}
	if (*p == 0)
		return NULL;
	startp = p;
	while (*p && !isspace (*p))
		++p;
	*p = 0;
	return g_memdup (startp, (p - startp) + 1);
}

/*
 * parse a libtool .la file and return the path of the file to dlopen ()
 * handling both the installed and uninstalled cases
 */
static char*
get_dl_name_from_libtool (const char *libtool_file)
{
	FILE* file;
	char buf [512];
	char *line, *dlname = NULL, *libdir = NULL, *installed = NULL;
	if (!(file = fopen (libtool_file, "r")))
		return NULL;
	while ((line = fgets (buf, 512, file))) {
		while (*line && isspace (*line))
			++line;
		if (*line == '#' || *line == 0)
			continue;
		if (strncmp ("dlname", line, 6) == 0) {
			g_free (dlname);
			dlname = read_string (line + 6, file);
		} else if (strncmp ("libdir", line, 6) == 0) {
			g_free (libdir);
			libdir = read_string (line + 6, file);
		} else if (strncmp ("installed", line, 9) == 0) {
			g_free (installed);
			installed = read_string (line + 9, file);
		}
	}
	fclose (file);
	line = NULL;
	if (installed && strcmp (installed, "no") == 0) {
		char *dir = g_path_get_dirname (libtool_file);
		if (dlname)
			line = g_strconcat (dir, G_DIR_SEPARATOR_S ".libs" G_DIR_SEPARATOR_S, dlname, NULL);
		g_free (dir);
	} else {
		if (libdir && dlname)
			line = g_strconcat (libdir, G_DIR_SEPARATOR_S, dlname, NULL);
	}
	g_free (dlname);
	g_free (libdir);
	g_free (installed);
	return line;
}

/**
 * mono_dl_open:
 * @name: name of file containing shared module
 * @flags: flags
 * @error_msg: pointer for error message on failure
 *
 * Load the given file @name as a shared library or dynamically loadable
 * module. @name can be NULL to indicate loading the currently executing
 * binary image.
 * @flags can have the MONO_DL_LOCAL bit set to avoid exporting symbols
 * from the module to the shared namespace. The MONO_DL_LAZY bit can be set
 * to lazily load the symbols instead of resolving everithing at load time.
 * @error_msg points to a string where an error message will be stored in
 * case of failure.
 *
 * Returns: a MonoDl pointer on success, NULL on failure.
 */
MonoDl*
mono_dl_open (const char *name, int flags, char **error_msg)
{
	MonoDl *module;
	void *lib;
	int lflags = LL_SO_TRFLAGS (flags);

	if (error_msg)
		*error_msg = NULL;

	module = malloc (sizeof (MonoDl));
	if (!module) {
		if (error_msg)
			*error_msg = g_strdup ("Out of memory");
		return NULL;
	}
	module->main_module = name == NULL? TRUE: FALSE;
	lib = LL_SO_OPEN (name, lflags);
	if (!lib) {
		char *lname;
		char *llname;
		const char *suff;
		const char *ext;
		/* This platform does not support dlopen */
		if (name == NULL) {
			free (module);
			return NULL;
		}
		
		suff = ".la";
		ext = strrchr (name, '.');
		if (ext && strcmp (ext, ".la") == 0)
			suff = "";
		lname = g_strconcat (name, suff, NULL);
		llname = get_dl_name_from_libtool (lname);
		g_free (lname);
		if (llname) {
			lib = LL_SO_OPEN (llname, lflags);
			g_free (llname);
		}
		if (!lib) {
			if (error_msg) {
				*error_msg = LL_SO_ERROR ();
			}
			free (module);
			return NULL;
		}
	}
	module->handle = lib;
	return module;
}

/**
 * mono_dl_symbol:
 * @module: a MonoDl pointer
 * @name: symbol name
 * @symbol: pointer for the result value
 *
 * Load the address of symbol @name from the given @module.
 * The address is stored in the pointer pointed to by @symbol.
 *
 * Returns: NULL on success, an error message on failure
 */
char*
mono_dl_symbol (MonoDl *module, const char *name, void **symbol)
{
	void *sym;

#if MONO_DL_NEED_USCORE
	{
		char *usname = malloc (strlen (name) + 2);
		*usname = '_';
		strcpy (usname + 1, name);
		sym = LL_SO_SYMBOL (module, usname);
		free (usname);
	}
#else
	sym = LL_SO_SYMBOL (module, name);
#endif
	if (sym) {
		if (symbol)
			*symbol = sym;
		return NULL;
	}
	if (symbol)
		*symbol = NULL;
	return LL_SO_ERROR ();
}

/**
 * mono_dl_close:
 * @module: a MonoDl pointer
 *
 * Unload the given module and free the module memory.
 *
 * Returns: 0 on success.
 */
void
mono_dl_close (MonoDl *module)
{
	LL_SO_CLOSE (module);
	free (module);
}

/**
 * mono_dl_build_path:
 * @directory: optional directory
 * @name: base name of the library
 * @iter: iterator token
 *
 * Given a directory name and the base name of a library, iterate
 * over the possible file names of the library, taking into account
 * the possible different suffixes and prefixes on the host platform.
 *
 * The returned file name must be freed by the caller.
 * @iter must point to a NULL pointer the first time the function is called
 * and then passed unchanged to the following calls.
 * Returns: the filename or NULL at the end of the iteration
 */
char*
mono_dl_build_path (const char *directory, const char *name, void **iter)
{
	int idx;
	const char *prefix;
	const char *suffix;
	int prlen;
	char *res;
	if (!iter)
		return NULL;
	idx = GPOINTER_TO_UINT (*iter);
	if (idx >= G_N_ELEMENTS (suffixes))
		return NULL;

	prlen = strlen (SOPREFIX);
	if (prlen && strncmp (name, SOPREFIX, prlen) != 0)
		prefix = SOPREFIX;
	else
		prefix = "";
	/* if the platform prefix is already provided, we suppose the caller knows the full name already */
	if (prlen && strncmp (name, SOPREFIX, prlen) == 0)
		suffix = "";
	else
		suffix = suffixes [idx];
	if (directory && *directory)
		res = g_strconcat (directory, G_DIR_SEPARATOR_S, prefix, name, suffixes [idx], NULL);
	else
		res = g_strconcat (prefix, name, suffixes [idx], NULL);
	++idx;
	*iter = GUINT_TO_POINTER (idx);
	return res;
}

#if EMBEDDED_PINVOKE
static GHashTable *mono_dls;
static char *ll_last_error = "";

/**
 * mono_dl_register_library:
 * @name: Library name, this is the name used by the DllImport as the external library name
 * @mappings: the mappings to register for P/Invoke.
 *
 * This function is only available on builds that define
 * EMBEDDED_PINVOKE, this is available for systems that do not provide
 * a dynamic linker but still want to use DllImport to easily invoke
 * code from the managed side into the unmanaged world.
 *
 * Mappings is a pointer to the first element of an array of
 * MonoDlMapping values.  The list must be terminated with both 
 * the name and addr fields set to NULL.
 *
 * This is typically used like this:
 * MonoDlMapping sample_library_mappings [] = {
 *   { "CallMe", CallMe },
 *   { NULL, NULL }
 * };
 *
 * ...
 * main ()
 * {
 *    ...
 *    mono_dl_register_library ("sample", sample_library_mappings);
 *    ...
 * }
 *
 * Then the C# code can use this P/Invoke signature:
 *
 * 	[DllImport ("sample")]
 *	extern static int CallMe (int f);
 */
void
mono_dl_register_library (const char *name, MonoDlMapping *mappings)
{
	if (mono_dls == NULL)
		mono_dls = g_hash_table_new (g_str_hash, g_str_equal);
	
	printf ("Inserting: 0x%p\n", mappings);
	g_hash_table_insert (mono_dls, g_strdup (name), mappings);
}

void *
LL_SO_OPEN (const char *file, int flag)
{
	void *mappings;
	
	if (mono_dls == NULL){
		ll_last_error = "Library not registered";
		return NULL;
	}
		
	mappings = g_hash_table_lookup (mono_dls, file);
	ll_last_error = mappings == NULL ? "File not registered" : "";
	printf ("Returning mappings=0x%p\n", mappings);
	return mappings;
}

int LL_SO_CLOSE (void *handle)
{
	// No-op
	return 0;
}

void *
_LL_SO_SYMBOL (void *handle, const char *symbol)
{
	MonoDlMapping *mappings = (MonoDlMapping *) handle;
	
	printf ("During lookup: 0x%p\n", handle);
	for (;mappings->name; mappings++){
		if (strcmp (symbol, mappings->name) == 0){
			ll_last_error = "";
			return mappings->addr;
		}
	}
	ll_last_error = "Symbol not found";
	return NULL;
}

char *
LL_SO_ERROR (void)
{
	return ll_last_error;
}
#endif
