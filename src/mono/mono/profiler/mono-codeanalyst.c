/*
 * mono-codeanalyst.c: AMD CodeAnalyst profiler
 *

 * Copyright 2011 Jonathan Chambers (joncham@gmail.com)
 */
#include <mono/metadata/profiler.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <string.h>
#include <glib.h>

#define bool char

#include "CAJITNTFLib.h"

/*
 * Coverage profiler. Compile with:
 * gcc -Wall -shared -o mono-profiler-cov.so mono-cov.c `pkg-config --cflags --libs mono`
 * Install the binary where the dynamic loader can find it (/usr/local/lib, for example,
 * or set the env var LD_LIBRARY_PATH to the directory where the file is).
 * Then run mono with:
 * mono --profile=cov:yourassembly test_suite.exe
 * mono --profile=cov:yourassembly/namespace test_suite.exe
 */

struct _MonoProfiler {
	GHashTable *hash;
	char* assembly_name;
	char* class_name;
	MonoAssembly *assembly;
	GList *bb_coverage;
};


/* called at the end of the program */
static void
codeanalyst_shutdown (MonoProfiler *prof)
{
	MonoImage *image;
	MonoMethod *method;
	int i;
	char *name;

	CAJIT_CompleteJITLog ();
}

static void
method_jit_result (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result) {
	if (result == MONO_PROFILE_OK) {
		gunichar2* name_utf16;
		MonoClass *klass = mono_method_get_class (method);
		char *signature = mono_signature_get_desc (mono_method_signature (method), TRUE);
		char *name = g_strdup_printf ("%s.%s.%s (%s)", mono_class_get_namespace (klass), mono_class_get_name (klass), mono_method_get_name (method), signature);
		gpointer code_start = mono_jit_info_get_code_start (jinfo);
		int code_size = mono_jit_info_get_code_size (jinfo);
		
		//if (op_write_native_code (name, code_start, code_size)) {
		//	g_warning ("Problem calling op_write_native_code\n");
		//}
		
		name_utf16 = g_utf8_to_utf16 (name, strlen (name), NULL, NULL, NULL);
		
		CAJIT_LogJITCode ((uintptr_t)code_start, code_size, (wchar_t*)name_utf16);
		
		g_free (signature);
		g_free (name);
		g_free (name_utf16);
	}
}

void
mono_profiler_startup (const char *desc);

/* the entry point */
void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;

	prof = g_new0 (MonoProfiler, 1);
	prof->hash = g_hash_table_new (NULL, NULL);
	if (strncmp ("cov:", desc, 4) == 0 && desc [4]) {
		char *cname;
		prof->assembly_name = g_strdup (desc + 4);
		cname = strchr (prof->assembly_name, '/');
		if (cname) {
			*cname = 0;
			prof->class_name = cname + 1;
		}
	} else {
		prof->assembly_name = g_strdup ("mscorlib");
	}

	CAJIT_Initialize ();

	mono_profiler_install (prof, codeanalyst_shutdown);
	
	mono_profiler_install_jit_end (method_jit_result);
	mono_profiler_set_events (MONO_PROFILE_JIT_COMPILATION);
}


