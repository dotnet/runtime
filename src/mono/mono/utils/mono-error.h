#ifndef __MONO_ERROR_H__
#define __MONO_ERROR_H__

#include <mono/utils/mono-publib.h>

enum {
	/*
	The supplied strings were dup'd by means of calling mono_error_dup_strings.
	*/
	MONO_ERROR_FREE_STRINGS = 0x0001,

	/*
	Something happened while processing the error and the resulting message is incomplete.
	*/
	MONO_ERROR_INCOMPLETE = 0x0002
};

enum {
	MONO_ERROR_NONE = 0,
	MONO_ERROR_MISSING_METHOD = 1,
	MONO_ERROR_MISSING_FIELD = 2,
	MONO_ERROR_TYPE_LOAD = 3,
	MONO_ERROR_FILE_NOT_FOUND = 4,
	MONO_ERROR_BAD_IMAGE = 5,
	MONO_ERROR_OUT_OF_MEMORY = 6,
	MONO_ERROR_ARGUMENT = 7,
	MONO_ERROR_NOT_VERIFIABLE = 8,
	/*
	 * This is a generic error mechanism is you need to raise an arbitrary corlib exception.
	 * You must pass the exception name otherwise prepare_exception will fail with internal execution. 
	 */
	MONO_ERROR_GENERIC = 9
};

/*Keep in sync with MonoErrorInternal*/
typedef struct _MonoError {
	unsigned short error_code;
    unsigned short hidden_0; /*DON'T TOUCH */

	void *hidden_1 [12]; /*DON'T TOUCH */
    char hidden_2 [128]; /*DON'T TOUCH */
} MonoError;

MONO_BEGIN_DECLS

MONO_API void
mono_error_init (MonoError *error);

MONO_API void
mono_error_init_flags (MonoError *error, unsigned short flags);

MONO_API void
mono_error_cleanup (MonoError *error);

MONO_API mono_bool
mono_error_ok (MonoError *error);

MONO_API unsigned short
mono_error_get_error_code (MonoError *error);

MONO_API const char*
mono_error_get_message (MonoError *error);

MONO_END_DECLS

#endif
