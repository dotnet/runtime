/*
 * vtune.c: VTune profiler
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
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/utils/mono-publib.h>
#include <string.h>
#include <glib.h>

#define bool char

#include <jitprofiling.h>

static const char*
code_buffer_desc (MonoProfilerCodeBufferType type)
{
	switch (type) {
	case MONO_PROFILER_CODE_BUFFER_METHOD:
		return "code_buffer_method";
	case MONO_PROFILER_CODE_BUFFER_METHOD_TRAMPOLINE:
		return "code_buffer_method_trampoline";
	case MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE:
		return "code_buffer_unbox_trampoline";
	case MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE:
		return "code_buffer_imt_trampoline";
	case MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE:
		return "code_buffer_generics_trampoline";
	case MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE:
		return "code_buffer_specific_trampoline";
	case MONO_PROFILER_CODE_BUFFER_HELPER:
		return "code_buffer_misc_helper";
	case MONO_PROFILER_CODE_BUFFER_MONITOR:
		return "code_buffer_monitor";
	case MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE:
		return "code_buffer_delegate_invoke";
	case MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING:
		return "code_buffer_exception_handling";
	default:
		return "unspecified";
	}
}

/* called at the end of the program */
static void
codeanalyst_shutdown (MonoProfiler *prof)
{
	iJIT_NotifyEvent(iJVM_EVENT_TYPE_SHUTDOWN, NULL);
}

static void
method_jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo)
{
	int i;
	MonoDebugSourceLocation *sourceLoc;
	MonoDebugMethodJitInfo *dmji;
	MonoClass *klass = mono_method_get_class (method);
	char *signature = mono_signature_get_desc (mono_method_signature_internal (method), TRUE);
	char *name = g_strdup_printf ("%s(%s)", mono_method_get_name (method), signature);
	char *classname = g_strdup_printf ("%s%s%s", m_class_get_name_space (klass), m_class_get_name_space (klass)[0] != 0 ? "::" : "", m_class_get_name (klass));
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
				g_free (vtuneMethod.line_number_table);
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
		g_free (vtuneMethod.source_file_name);
	if (vtuneMethod.line_number_table != NULL)
		g_free (vtuneMethod.line_number_table);

	g_free (signature);
	g_free (name);
	g_free (classname);
}

static void
code_buffer_new (MonoProfiler *prof, void *buffer, int size, MonoProfilerCodeBufferType type, void *data)
{
	char *name;
	iJIT_Method_Load vtuneMethod;

	if (type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE)
		name = g_strdup_printf ("code_buffer_specific_trampoline_%s", (char*) data);
	else
		name = (char*) code_buffer_desc (type);

	memset (&vtuneMethod, 0, sizeof (vtuneMethod));
	vtuneMethod.method_id = iJIT_GetNewMethodID ();
	vtuneMethod.method_name = name;
	vtuneMethod.method_load_address = buffer;
	vtuneMethod.method_size = size;

	iJIT_NotifyEvent (iJVM_EVENT_TYPE_METHOD_LOAD_FINISHED, &vtuneMethod);

	if (type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE) {
		g_free (name);
	}
}

MONO_API void
mono_profiler_init_vtune (const char *desc);

/* the entry point */
void
mono_profiler_init_vtune (const char *desc)
{
	iJIT_IsProfilingActiveFlags flags = iJIT_IsProfilingActive();
	if (flags == iJIT_SAMPLING_ON)
	{
		MonoProfilerHandle handle = mono_profiler_create (NULL);
		mono_profiler_set_jit_done_callback (handle, method_jit_done);
		mono_profiler_set_jit_code_buffer_callback (handle, code_buffer_new);
	}
}
