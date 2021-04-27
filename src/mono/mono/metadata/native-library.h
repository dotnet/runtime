#ifndef _MONO_METADATA_NATIVE_LIBRARY_H_
#define _MONO_METADATA_NATIVE_LIBRARY_H_

#include <glib.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-coop-mutex.h>

typedef enum {
	LOOKUP_PINVOKE_ERR_OK = 0, /* No error */
	LOOKUP_PINVOKE_ERR_NO_LIB, /* DllNotFoundException */
	LOOKUP_PINVOKE_ERR_NO_SYM, /* EntryPointNotFoundException */
} MonoLookupPInvokeErr;

/* We should just use a MonoError, but mono_lookup_pinvoke_call has this legacy
 * error reporting mechanism where it returns an exception class and a string
 * message.  So instead we return an error code and message, and for internal
 * callers convert it to a MonoError.
 *
 * Don't expose this type to the runtime.  It's just an implementation
 * detail for backward compatability.
 */
typedef struct MonoLookupPInvokeStatus {
	MonoLookupPInvokeErr err_code;
	char *err_arg;
} MonoLookupPInvokeStatus;

gpointer
mono_lookup_pinvoke_qcall_internal (MonoMethod *method, MonoLookupPInvokeStatus *error);

typedef struct MonoQCallDef
{
    const char *class_name;
    const char *namespace_name;
    const void **functions;
} MonoQCallDef;

typedef struct MonoQCallFunc {
    intptr_t        flags; //legal values (0x01 - end of array mareker, 0x08 - qcall)
    void           *implementation;
    const char     *method_name;
} MonoQCallFunc;

typedef void * (*PInvokeOverrideFn) (const char *libraryName, const char *entrypointName);

void
mono_loader_install_pinvoke_override (PInvokeOverrideFn override_fn);

#endif
