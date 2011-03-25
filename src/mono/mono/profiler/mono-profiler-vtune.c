/*
 * mono-codeanalyst.c: VTune profiler
 *
 * Author:
 *   Virgile Bello (virgile.bello@gmail.com)
 *
 * (C) 2011 Virgile Bello
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
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <string.h>
#include <glib.h>

#define bool char

#include "jitprofiling.h"

/* called at the end of the program */
static void
codeanalyst_shutdown (MonoProfiler *prof)
{
	iJIT_NotifyEvent(iJVM_EVENT_TYPE_SHUTDOWN, NULL);
}

static void
method_jit_result (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result) {
	if (result == MONO_PROFILE_OK) {
		int i;
		MonoDebugSourceLocation *sourceLoc;
		MonoDebugMethodInfo *methodDebugInfo;
		MonoDebugMethodJitInfo *dmji;
		MonoClass *klass = mono_method_get_class (method);
		char *signature = mono_signature_get_desc (mono_method_signature (method), TRUE);
		char *name = g_strdup_printf ("%s(%s)", mono_method_get_name (method), signature);
		char *classname = g_strdup_printf ("%s%s%s", mono_class_get_namespace (klass), mono_class_get_namespace (klass)[0] != 0 ? "::" : "", mono_class_get_name (klass));
		gpointer code_start = mono_jit_info_get_code_start (jinfo);
		int code_size = mono_jit_info_get_code_size (jinfo);
		
		iJIT_Method_Load vtuneMethod;
		memset(&vtuneMethod, 0, sizeof(vtuneMethod));
		vtuneMethod.method_id = iJIT_GetNewMethodID();
		vtuneMethod.method_name = name;
		vtuneMethod.method_load_address = code_start;
		vtuneMethod.method_size = code_size;
		vtuneMethod.class_file_name = classname;

		dmji = mono_debug_find_method (method, mono_domain_get());

		if (dmji != NULL)
		{
			vtuneMethod.line_number_size = dmji->num_line_numbers;
			vtuneMethod.line_number_table = (vtuneMethod.line_number_size != 0) ?
				(LineNumberInfo*)malloc(sizeof(LineNumberInfo) * vtuneMethod.line_number_size) : NULL;

			for (i = 0; i < dmji->num_line_numbers; ++i)
			{
				sourceLoc = mono_debug_lookup_source_location (method, dmji->line_numbers[i].native_offset, mono_domain_get());
				if (sourceLoc == NULL)
				{
					free(vtuneMethod.line_number_table);
					vtuneMethod.line_number_table = NULL;
					vtuneMethod.line_number_size = 0;
					break;
				}
				if (i == 0)
					vtuneMethod.source_file_name = strdup(sourceLoc->source_file);
				vtuneMethod.line_number_table[i].Offset = dmji->line_numbers[i].native_offset;
				vtuneMethod.line_number_table[i].LineNumber = sourceLoc->row;
				mono_debug_free_source_location (sourceLoc);
			}
			mono_debug_free_method_jit_info (dmji);
		}

		iJIT_NotifyEvent(iJVM_EVENT_TYPE_METHOD_LOAD_FINISHED, &vtuneMethod);

		if (vtuneMethod.source_file_name != NULL)
			free(vtuneMethod.source_file_name);
		if (vtuneMethod.line_number_table != NULL)
			free(vtuneMethod.line_number_table);
	
		g_free (signature);
		g_free (name);
		g_free (classname);
	}
}

/* the entry point */
void
mono_profiler_startup (const char *desc)
{
	iJIT_IsProfilingActiveFlags flags = iJIT_IsProfilingActive();
	if (flags == iJIT_SAMPLING_ON)
	{
		mono_profiler_install (NULL, codeanalyst_shutdown);
		mono_profiler_install_jit_end (method_jit_result);
		mono_profiler_set_events (MONO_PROFILE_JIT_COMPILATION);
	}
}
