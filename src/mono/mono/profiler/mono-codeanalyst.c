/*
 * mono-codeanalyst.c: AMD CodeAnalyst profiler
 *
 * Author:
 *   Jonathan Chambers (joncham@gmail.com)
 *
 * (C) 2011 Jonathan Chambers
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
#include <mono/metadata/profiler.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <string.h>
#include <glib.h>

#define bool char

#include "CAJITNTFLib.h"

/* called at the end of the program */
static void
codeanalyst_shutdown (MonoProfiler *prof)
{
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
	CAJIT_Initialize ();

	mono_profiler_install (NULL, codeanalyst_shutdown);
	mono_profiler_install_jit_end (method_jit_result);
	mono_profiler_set_events (MONO_PROFILE_JIT_COMPILATION);
}


