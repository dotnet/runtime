#ifdef NO_PORT
#include "mono/interpreter/interp.h"

MonoPIFunc
mono_create_trampoline (MonoMethod *method)
{
	g_error ("Unsupported arch");
	return NULL;
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	g_error ("Unsupported arch");
	return NULL;
}

#endif

