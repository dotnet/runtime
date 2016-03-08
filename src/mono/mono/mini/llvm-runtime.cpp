#include <config.h>
#include "llvm-runtime.h"

#include <glib.h>

extern "C" {

void
mono_llvm_cpp_throw_exception (void)
{
#ifdef MONO_LLVM_LOADED
	g_assert_not_reached ();
#else
	gint32 *ex = NULL;

	/* The generated code catches an int32* */
	throw ex;
#endif
}

}
