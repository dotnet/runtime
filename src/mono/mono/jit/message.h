#ifndef _MONO_JIT_MESSAGE_H_
#define _MONO_JIT_MESSAGE_H_

#include <glib.h>

#include <mono/metadata/object.h>

MonoMethodMessage *
mono_method_call_message_new       (MonoMethod *method, 
				    gpointer stack);

void
mono_method_return_message_restore (MonoMethod *method, 
				    gpointer stack, 
				    MonoObject *result,
				    MonoArray *out_args);

void
ves_icall_MonoMethodMessage_InitMessage (MonoMethodMessage *this, 
					 MonoReflectionMethod *method,
					 MonoArray *out_args);

#endif
