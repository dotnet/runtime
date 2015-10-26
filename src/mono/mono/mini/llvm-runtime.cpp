#include <config.h>
#include "llvm-runtime.h"

#include <glib.h>

#if defined(ENABLE_LLVM_RUNTIME) || defined(ENABLE_LLVM)

extern "C" {

void
mono_llvm_cpp_throw_exception (void)
{
	gint32 *ex = NULL;

	/* The generated code catches an int32* */
	throw ex;
}

}

#else

extern "C" {

void
mono_llvm_cpp_throw_exception (void)
{
	g_assert_not_reached ();
}

}

#endif /* ENABLE_LLVM_RUNTIME */
