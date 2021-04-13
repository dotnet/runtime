#include <config.h>
#include "llvm-runtime.h"

#include <glib.h>
#include <mono/utils/mono-logger-internals.h>

#include <mono/metadata/mono-debug.h>
#include <mono/metadata/profiler.h>
#include "trace.h"

extern "C" {

void
mono_llvm_cpp_throw_exception (void)
{
	gint32 *ex = NULL;

	if (mono_trace_is_enabled ())
		mono_runtime_printf_err ("Native Stacktrace (mono_llvm_cpp_throw_exception)\n"); 
		
	/* The generated code catches an int32* */
	throw ex;
}

void
mono_llvm_cpp_catch_exception (MonoLLVMInvokeCallback cb, gpointer arg, gboolean *out_thrown)
{
	*out_thrown = FALSE;
	try {
		cb (arg);
	} catch (int*) {
		*out_thrown = TRUE;
	}
}

}
