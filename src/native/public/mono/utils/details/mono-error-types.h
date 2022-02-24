// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_ERROR_TYPES_H
#define _MONO_ERROR_TYPES_H

#include <mono/utils/details/mono-publib-types.h>

enum {
	/*
	The supplied strings were dup'd by means of calling mono_error_dup_strings.
	*/
	MONO_ERROR_FREE_STRINGS = 0x0001,

	/*
	Something happened while processing the error and the resulting message is incomplete.
	*/
	MONO_ERROR_INCOMPLETE = 0x0002,
	/*
	This MonoError is heap allocated in a mempool
        */
	MONO_ERROR_MEMPOOL_BOXED = 0x0004
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
	MONO_ERROR_ARGUMENT_NULL = 11,
	MONO_ERROR_ARGUMENT_OUT_OF_RANGE = 14,
	MONO_ERROR_NOT_VERIFIABLE = 8,
	MONO_ERROR_INVALID_PROGRAM = 12,
	MONO_ERROR_MEMBER_ACCESS = 13,

	/*
	 * This is a generic error mechanism is you need to raise an arbitrary corlib exception.
	 * You must pass the exception name otherwise prepare_exception will fail with internal execution.
	 */
	MONO_ERROR_GENERIC = 9,
	/* This one encapsulates a managed exception instance */
	MONO_ERROR_EXCEPTION_INSTANCE = 10,

	/* Not a valid error code - indicates that the error was cleaned up and reused */
	MONO_ERROR_CLEANUP_CALLED_SENTINEL = 0xffff
};

#ifdef _MSC_VER
__pragma(warning (push))
__pragma(warning (disable:4201))
#endif

/*Keep in sync with MonoErrorInternal*/
typedef union _MonoError {
	// Merge two uint16 into one uint32 so it can be initialized
	// with one instruction instead of two.
	uint32_t init;
	struct {
		uint16_t error_code;
		uint16_t private_flags; /*DON'T TOUCH */
		void *hidden_1 [12]; /*DON'T TOUCH */
	};
} MonoErrorExternal;

#ifdef _MSC_VER
__pragma(warning (pop))
#endif

#ifdef MONO_INSIDE_RUNTIME
typedef union _MonoErrorInternal MonoError;
#else
typedef MonoErrorExternal MonoError;
#endif

/* Mempool-allocated MonoError.*/
typedef struct _MonoErrorBoxed MonoErrorBoxed;

#endif
