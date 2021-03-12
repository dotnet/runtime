#include <config.h>
#include "llvm-runtime.h"

#include <glib.h>

#ifdef HOST_WASM
#include <emscripten.h>
#endif

extern "C" {

void
mono_llvm_cpp_throw_exception (void)
{
	gint32 *ex = NULL;

#ifdef HOST_WASM
		EM_ASM(
			   var err = new Error();
			   console.log ("Throw stacktrace: \n");
			   console.log (err.stack);
			   );
#endif

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
