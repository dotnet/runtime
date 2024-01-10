/**
 * \file
 */

#ifndef __MONO_ERROR_INTERNALS_H__
#define __MONO_ERROR_INTERNALS_H__

#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-forward.h>
#include "mono/utils/mono-compiler.h"

MONO_DISABLE_WARNING(4201) // nonstandard extension used: nameless struct/union
/*Keep in sync with MonoError*/
typedef union _MonoErrorInternal {
	// Merge two uint16 into one uint32 so it can be initialized
	// with one instruction instead of two.
	guint32 init; // Written by JITted code
	struct {
		guint16 error_code;
		guint16 flags;

		/*These name are suggestions of their content. MonoError internals might use them for something else.*/
		// type_name must be right after error_code and flags, see mono_error_init_deferred.
		const char *type_name;
		const char *assembly_name;
		const char *member_name;
		const char *exception_name_space;
		const char *exception_name;
		union {
			/* Valid if error_code != MONO_ERROR_EXCEPTION_INSTANCE.
			 * Used by type or field load errors and generic error specified by class.
			 */
			MonoClass *klass;
			/* Valid if error_code == MONO_ERROR_EXCEPTION_INSTANCE.
			 * Generic error specified by a managed instance.
			 */
			MonoGCHandle instance_handle;
		} exn;
		const char *full_message;
		const char *full_message_with_fields;
		const char *first_argument;

		// MonoErrorExternal:
		//void *padding [3];
	};
} MonoErrorInternal;
MONO_RESTORE_WARNING

/* Invariant: the error strings are allocated in the mempool of the given image */
struct _MonoErrorBoxed {
	MonoError error;
	MonoImage *image;
};

/*
Historically MonoError initialization was deferred, but always had to occur,
	even in success paths, as cleanup could be done unconditionally.
	This was confusing.

ERROR_DECL (error)
	This is the overwhelmingly common case.
	Declare and initialize a local variable, named "error",
	pointing to an initialized MonoError (named "error_value",
	using token pasting).

MONO_API_ERROR_INIT
	This is used for MonoError in/out parameter on a public interface,
	which must be presumed uninitialized. These are often
	marked with MONO_API, MONO_RT_EXTERNAL_ONLY, MONO_PROFILER_API, etc.
	Tnis includes functions called from dis, profiler, pedump, and driver.
	dis, profiler, and pedump make sense, these are actually external and
	uninitialized. Driver less so.
	Presently this is unused and error_init is used instead.

error_init
	Initialize a MonoError. These are historical and usually
	but not always redundant, and should be reduced/eliminated.
	All the non-redundant ones should be renamed and all the redundant
	ones removed.

error_init_reuse
	This indicates an error has been cleaned up and will be reused.
	A common usage is to reduce stack pressure, e.g. in the interpreter.
	Consider also changing mono_error_cleanup to call error_init_internal,
	and then remove these.

error_init_internal
	Rare cases without a better name.
	For example, setting up an icall frame, or initializing member data.

new0, calloc, static
	A zeroed MonoError is valid and initialized.
	Zeroing an entire MonoError is overkill, unless it is near other
	bulk zeroing.

All initialization is actually bottlenecked to error_init_internal.
Different names indicate different scenarios, but the same code.
*/
#define ERROR_DECL(x) 			MonoError x ## _value; error_init_internal (& x ## _value); MonoError * const x = &x ## _value
#define error_init_internal(error) 	((void)((error)->init = 0))
#define MONO_API_ERROR_INIT(error) 	error_init_internal (error)
#define error_init_reuse(error) 	error_init_internal (error)

#define ERROR_LOCAL_BEGIN(local, parent, skip_overwrite) do { \
MonoError local; \
gboolean local ## _overwrite_temp = skip_overwrite; \
error_init_internal (&local); \
if (!local ## _overwrite_temp) \
	parent = &local; \

#define ERROR_LOCAL_END(local) if (!local ## _overwrite_temp) \
	mono_error_cleanup (&local); \
} while (0) \

// Historical deferred initialization was called error_init.

// possible bug detection that did not work
//#define error_init(error) (is_ok (error))

// FIXME Eventually all error_init should be removed, however it is prudent
// to leave them in for now, at least most of them, while we sort out
// the few that are needed and to experiment with adding them back in bulk,
// i.e. in an entire source file. Some are obviously not needed.
//#define error_init(error) // nothing
#define error_init(error) error_init_internal (error)
// Function for experimentation, should go away.
//void error_init(MonoError*);

#define is_ok(error) ((error)->error_code == MONO_ERROR_NONE)

#define return_if_nok(error) do { if (!is_ok ((error))) return; } while (0)
#define return_val_if_nok(error,val) do { if (!is_ok ((error))) return (val); } while (0)

#define goto_if(expr, label) 	  do { if (expr) goto label; } while (0)
#define goto_if_ok(error, label)  goto_if (is_ok (error), label)
#define goto_if_nok(error, label) goto_if (!is_ok (error), label)

/* Only use this in icalls */
#define return_val_and_set_pending_if_nok(error, value) \
do { 							\
	if (mono_error_set_pending_exception ((error)))	\
		return (value); 			\
} while (0)						\

/*
 * Three macros to assert that a MonoError is ok:
 * 1. mono_error_assert_ok(e) when you just want to print the error's message on failure
 * 2. mono_error_assert_ok(e,msg) when you want to print "msg, due to <e's message>"
 * 3. mono_error_assertf_ok(e,fmt,args...) when you want to print "<formatted msg>, due to <e's message>"
 *    (fmt should specify the formatting just for args).
 *
 * What's the difference between mono_error_assert_msg_ok (e, "foo") and
 * mono_error_assertf_ok (e, "foo") ?  The former works as you expect, the
 * latter unhelpfully expands to
 *
 * g_assertf (is_ok (e), "foo, due to %s", ,  mono_error_get_message (err)).
 *
 * Note the double commas.  Turns out that to get rid of that extra comma
 * portably we would have to write really ugly preprocessor macros.
 */
#define mono_error_assert_ok(error)            g_assertf (is_ok (error), "%s", mono_error_get_message (error))
#define mono_error_assert_msg_ok(error, msg)   g_assertf (is_ok (error), msg ", due to %s", mono_error_get_message (error))
#define mono_error_assertf_ok(error, fmt, ...) g_assertf (is_ok (error), fmt ", due to %s", __VA_ARGS__, mono_error_get_message (error))

/*
* Returns a pointer to the error message, without fields, empty string if no message is available.
* Caller should NOT release returned pointer, owned by MonoError.
*/
static inline
const char *
mono_error_get_message_without_fields (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	return error->full_message ? error->full_message : "";
}

void
mono_error_dup_strings (MonoError *error, gboolean dup_strings);

/* This function is not very useful as you can't provide any details beyond the message.*/
MONO_COMPONENT_API
void
mono_error_set_error (MonoError *error, int error_code, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_type_load_class (MonoError *error, MonoClass *klass, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_vset_type_load_class (MonoError *error, MonoClass *klass, const char *msg_format, va_list args);

MONO_COMPONENT_API
void
mono_error_set_type_load_name (MonoError *error, const char *type_name, const char *assembly_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(4,5);

void
mono_error_set_out_of_memory (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_argument_format (MonoError *error, const char *argument, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_argument (MonoError *error, const char *argument, const char *msg);

void
mono_error_set_argument_null (MonoError *oerror, const char *argument, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_argument_out_of_range (MonoError *error, const char *name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_not_verifiable (MonoError *oerror, MonoMethod *method, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_generic_error (MonoError *error, const char * name_space, const char *name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(4,5);

void
mono_error_set_execution_engine (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

MONO_COMPONENT_API
void
mono_error_set_not_implemented (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

MONO_COMPONENT_API
void
mono_error_set_not_supported (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_ambiguous_implementation (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

MONO_COMPONENT_API
void
mono_error_set_invalid_operation (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_exception_instance (MonoError *error, MonoException *exc);

void
mono_error_set_invalid_program (MonoError *oerror, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_member_access (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_invalid_cast (MonoError *oerror);

static inline void
mono_error_set_divide_by_zero (MonoError *error)
{
	mono_error_set_generic_error (error, "System", "DivideByZeroException", NULL);
}

static inline void
mono_error_set_index_out_of_range (MonoError *error)
{
	mono_error_set_generic_error (error, "System", "IndexOutOfRangeException", NULL);
}

static inline void
mono_error_set_overflow (MonoError *error)
{
	mono_error_set_generic_error (error, "System", "OverflowException", NULL);
}

static inline void
mono_error_set_synchronization_lock (MonoError *error, const char *message)
{
	mono_error_set_generic_error (error, "System.Threading", "SynchronizationLockException", "%s", message);
}

static inline void
mono_error_set_thread_interrupted (MonoError *error)
{
	mono_error_set_generic_error (error, "System.Threading", "ThreadInterruptedException", NULL);
}

static inline void
mono_error_set_null_reference (MonoError *error)
{
	mono_error_set_generic_error (error, "System", "NullReferenceException", NULL);
}

static inline void
mono_error_set_duplicate_wait_object (MonoError *error)
{
	mono_error_set_generic_error (error, "System", "DuplicateWaitObjectException", "Duplicate objects in argument.");
}

static inline void
mono_error_set_cannot_unload_appdomain (MonoError *error, const char *message)
{
	mono_error_set_generic_error (error, "System", "CannotUnloadAppDomainException", "%s", message);
}

static inline void
mono_error_set_platform_not_supported (MonoError *error, const char *message)
{
	mono_error_set_generic_error (error, "System", "PlatformNotSupportedException", "%s", message);
}

MonoException*
mono_error_prepare_exception (MonoError *error, MonoError *error_out);

MONO_COMPONENT_API MonoException*
mono_error_convert_to_exception (MonoError *error);

void
mono_error_move (MonoError *dest, MonoError *src);

MonoErrorBoxed*
mono_error_box (const MonoError *error, MonoImage *image);

gboolean
mono_error_set_from_boxed (MonoError *error, const MonoErrorBoxed *from);

const char*
mono_error_get_exception_name (MonoError *oerror);

const char*
mono_error_get_exception_name_space (MonoError *oerror);

void
mono_error_set_specific (MonoError *error, int error_code, const char *missing_method);

void
mono_error_set_first_argument (MonoError *oerror, const char *first_argument);

#if HOST_WIN32
#if HOST_X86 || HOST_AMD64

#include <windows.h>

// Single instruction inlinable form of GetLastError.
//
// Naming violation so can search disassembly for GetLastError.
//
#define GetLastError mono_GetLastError

static inline
unsigned long
__stdcall
GetLastError (void)
{
#if HOST_X86
    return __readfsdword (0x34);
#elif HOST_AMD64
    return __readgsdword (0x68);
#else
#error Unreachable, see above.
#endif
}

// Single instruction inlinable valid subset of SetLastError.
//
// Naming violation so can search disassembly for SetLastError.
//
// This is useful, for example, if you want to set a breakpoint
// on SetLastError, but do not want to break on merely "restoring" it,
// only "originating" it.
//
// A generic name is used in case there are other use-cases.
//
static inline
void
__stdcall
mono_SetLastError (unsigned long err)
{
#if HOST_X86
    __writefsdword (0x34, err);
#elif HOST_AMD64
    __writegsdword (0x68, err);
#else
#error Unreachable, see above.
#endif
}

#else // arm, arm64, etc.

#define mono_SetLastError SetLastError

#endif // processor
#endif // win32

#endif
