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
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

#ifdef TARGET_WIN32
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
#else
#define SOPREFIX "lib"
static const char suffixes [][4] = {
	".so"
};
#endif

#ifdef TARGET_WIN32

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
#include <unistd.h>

#ifdef __MACH__
#include <mach-o/dyld.h>
#endif


#ifndef RTLD_LAZY
#define RTLD_LAZY       1
#endif  /* RTLD_LAZY */

#define SO_HANDLE_TYPE void*
#ifdef PLATFORM_ANDROID
/* Bionic doesn't support NULL filenames */
#  define LL_SO_OPEN(file,flags) ((file) ? dlopen ((file), (flags)) : NULL)
#else
#  define LL_SO_OPEN(file,flags) dlopen ((file), (flags))
#endif
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

#else
/* no dynamic loader supported */
#define SO_HANDLE_TYPE void*
#define LL_SO_OPEN(file,flags) NULL
#define LL_SO_CLOSE(module) 
#define LL_SO_SYMBOL(module, name) NULL
#define LL_SO_TRFLAGS(flags) (flags)
#define LL_SO_ERROR() g_strdup ("No support for dynamic loader")

#endif

static GSList *fallback_handlers;

struct MonoDlFallbackHandler {
	MonoDlFallbackLoad load_func;
	MonoDlFallbackSymbol symbol_func;
	MonoDlFallbackClose close_func;
	void *user_data;
};
	
struct _MonoDl {
	SO_HANDLE_TYPE handle;
	int main_module;

	/* If not NULL, use the methods in MonoDlFallbackHandler instead of the LL_* methods */
	MonoDlFallbackHandler *dl_fallback;
};

#ifdef TARGET_WIN32

static char*
w32_dlerror (void)
{
	char* ret = NULL;
	wchar_t* buf = NULL;
	DWORD code = GetLastError ();

	if (FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
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
 * case of failure.   The error must be released with g_free.
 *
 * Returns: a MonoDl pointer on success, NULL on failure.
 */
MonoDl*
mono_dl_open (const char *name, int flags, char **error_msg)
{
	MonoDl *module;
	void *lib;
	MonoDlFallbackHandler *dl_fallback = NULL;
	int lflags = LL_SO_TRFLAGS (flags);
	gboolean found = FALSE;

	if (error_msg)
		*error_msg = NULL;

	module = malloc (sizeof (MonoDl));
	if (!module) {
		if (error_msg)
			*error_msg = g_strdup ("Out of memory");
		return NULL;
	}
	module->main_module = name == NULL? TRUE: FALSE;
#ifdef PLATFORM_ANDROID
	/* android-ndk-r10c defines RTLD_DEFAULT as 0 on arm64... (Android Issue Tracker #80446) */
	if (!name) {
		lib = RTLD_DEFAULT;
		found = TRUE;
	} else {
		lib = LL_SO_OPEN (name, lflags);
		found = lib != NULL;
	}
#else
	lib = LL_SO_OPEN (name, lflags);
	found = lib != NULL;
#endif
	if (!found) {
		GSList *node;
		for (node = fallback_handlers; node != NULL; node = node->next){
			MonoDlFallbackHandler *handler = (MonoDlFallbackHandler *) node->data;
			if (error_msg)
				*error_msg = NULL;
			
			lib = handler->load_func (name, lflags, error_msg, handler->user_data);
			if (error_msg && *error_msg != NULL)
				g_free (*error_msg);
			
			if (lib != NULL){
				dl_fallback = handler;
				break;
			}
		}
		found = lib != NULL;
	}
	if (!found && !dl_fallback) {
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
	module->dl_fallback = dl_fallback;
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
	char *err = NULL;

	if (module->dl_fallback) {
		sym = module->dl_fallback->symbol_func (module->handle, name, &err, module->dl_fallback->user_data);
	} else {
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
	}

	if (sym) {
		if (symbol)
			*symbol = sym;
		return NULL;
	}
	if (symbol)
		*symbol = NULL;
	return (module->dl_fallback != NULL) ? err :  LL_SO_ERROR ();
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
	MonoDlFallbackHandler *dl_fallback = module->dl_fallback;
	
	if (dl_fallback){
		if (dl_fallback->close_func != NULL)
			dl_fallback->close_func (module->handle, dl_fallback->user_data);
	} else
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
	gboolean first_call;
	int prlen;
	int suffixlen;
	char *res;

	if (!iter)
		return NULL;

	/*
	  The first time we are called, idx = 0 (as *iter is initialized to NULL). This is our
	  "bootstrap" phase in which we check the passed name verbatim and only if we fail to find
	  the dll thus named, we start appending suffixes, each time increasing idx twice (since now
	  the 0 value became special and we need to offset idx to a 0-based array index). This is
	  done to handle situations when mapped dll name is specified as libsomething.so.1 or
	  libsomething.so.1.1 or libsomething.so - testing it algorithmically would be an overkill
	  here.
	 */
	idx = GPOINTER_TO_UINT (*iter);
	if (idx == 0) {
		first_call = TRUE;
		suffix = "";
		suffixlen = 0;
	} else {
		idx--;
		if (idx >= G_N_ELEMENTS (suffixes))
			return NULL;
		first_call = FALSE;
		suffix = suffixes [idx];
		suffixlen = strlen (suffix);
	}

	prlen = strlen (SOPREFIX);
	if (prlen && strncmp (name, SOPREFIX, prlen) != 0)
		prefix = SOPREFIX;
	else
		prefix = "";

	if (first_call || (suffixlen && strstr (name, suffix) == (name + strlen (name) - suffixlen)))
		suffix = "";

	if (directory && *directory)
		res = g_strconcat (directory, G_DIR_SEPARATOR_S, prefix, name, suffix, NULL);
	else
		res = g_strconcat (prefix, name, suffix, NULL);
	++idx;
	if (!first_call)
		idx++;
	*iter = GUINT_TO_POINTER (idx);
	return res;
}

MonoDlFallbackHandler *
mono_dl_fallback_register (MonoDlFallbackLoad load_func, MonoDlFallbackSymbol symbol_func, MonoDlFallbackClose close_func, void *user_data)
{
	MonoDlFallbackHandler *handler;
	
	g_return_val_if_fail (load_func != NULL, NULL);
	g_return_val_if_fail (symbol_func != NULL, NULL);

	handler = g_new (MonoDlFallbackHandler, 1);
	handler->load_func = load_func;
	handler->symbol_func = symbol_func;
	handler->close_func = close_func;
	handler->user_data = user_data;

	fallback_handlers = g_slist_prepend (fallback_handlers, handler);
	
	return handler;
}

void
mono_dl_fallback_unregister (MonoDlFallbackHandler *handler)
{
	GSList *found;

	found = g_slist_find (fallback_handlers, handler);
	if (found == NULL)
		return;

	g_slist_remove (fallback_handlers, handler);
	g_free (handler);
}


#if defined (HAVE_DL_LOADER)

static MonoDl*
try_load (const char *lib_name, char *dir, int flags, char **err)
{
	gpointer iter;
	MonoDl *runtime_lib;
	char *path;
	iter = NULL;
	*err = NULL;
	while ((path = mono_dl_build_path (dir, lib_name, &iter))) {
		g_free (*err);
		runtime_lib = mono_dl_open (path, flags, err);
		g_free (path);
		if (runtime_lib)
			return runtime_lib;
	}
	return NULL;
}

MonoDl*
mono_dl_open_runtime_lib (const char* lib_name, int flags, char **error_msg)
{
	MonoDl *runtime_lib = NULL;
	char buf [4096];
	int binl;
	binl = readlink ("/proc/self/exe", buf, sizeof (buf)-1);
	*error_msg = NULL;

#ifdef __MACH__
	if (binl == -1) {
		uint32_t bsize = sizeof (buf);
		if (_NSGetExecutablePath (buf, &bsize) == 0) {
			binl = strlen (buf);
		}
	}
#endif
	if (binl != -1) {
		char *base;
		char *resolvedname, *name;
		buf [binl] = 0;
		resolvedname = mono_path_resolve_symlinks (buf);
		base = g_path_get_dirname (resolvedname);
		name = g_strdup_printf ("%s/.libs", base);
		runtime_lib = try_load (lib_name, name, flags, error_msg);
		g_free (name);
		if (!runtime_lib) {
			char *newbase = g_path_get_dirname (base);
			name = g_strdup_printf ("%s/lib", newbase);
			runtime_lib = try_load (lib_name, name, flags, error_msg);
			g_free (name);
		}
#ifdef __MACH__
		if (!runtime_lib) {
			char *newbase = g_path_get_dirname (base);
			name = g_strdup_printf ("%s/Libraries", newbase);
			runtime_lib = try_load (lib_name, name, flags, error_msg);
			g_free (name);
		}
#endif
		g_free (base);
		g_free (resolvedname);
	}
	if (!runtime_lib)
		runtime_lib = try_load (lib_name, NULL, flags, error_msg);

	return runtime_lib;
}

#else

MonoDl*
mono_dl_open_runtime_lib (const char* lib_name, int flags, char **error_msg)
{
	return NULL;
}

#endif
