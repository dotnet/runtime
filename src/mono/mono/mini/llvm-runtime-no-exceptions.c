#include <config.h>
#include "llvm-runtime.h"

#include <glib.h>

void
mono_llvm_cpp_throw_exception (void)
{
	g_assertf(FALSE, "Exceptions are not supported");
}

void
mono_llvm_cpp_catch_exception (MonoLLVMInvokeCallback cb, gpointer arg, gboolean *out_thrown)
{
	cb (arg);
}
