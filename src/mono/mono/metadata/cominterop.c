/**
 * \file
 * COM Interop Support
 *
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include <glib.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/loader.h>
#include "cil-coff.h"
#include "metadata/abi-details.h"
#include "metadata/cominterop.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/tabledefs.h"
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include "metadata/reflection-internals.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/method-builder-ilgen-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/utils/atomic.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-error-internals.h"
#include <string.h>
#include <errno.h>
#include <mono/utils/w32api.h>
#if defined (HOST_WIN32)
MONO_PRAGMA_WARNING_PUSH()
MONO_PRAGMA_WARNING_DISABLE (4115) // warning C4115: 'IRpcStubBuffer': named type definition in parentheses
#include <oleauto.h>
MONO_PRAGMA_WARNING_POP()
#include <mono/utils/w32subset.h>
#endif
#include "icall-decl.h"
#include "icall-signatures.h"

// func is an identifier, that names a function, and is also in jit-icall-reg.h,
// and therefore a field in mono_jit_icall_info and can be token pasted into an enum value.
//
// The name of func must be linkable for AOT, for example g_free does not work (monoeg_g_free instead),
// nor does the C++ overload fmod (mono_fmod instead). These functions therefore
// must be extern "C".
#ifndef DISABLE_JIT
#define register_icall(func, sig, no_wrapper) \
	(mono_register_jit_icall_info (&mono_get_jit_icall_info ()->func, (gconstpointer)func, #func, (sig), (no_wrapper), #func))
#else
/* No need for the name/C symbol */
#define register_icall(func, sig, no_wrapper) \
	(mono_register_jit_icall_info (&mono_get_jit_icall_info ()->func, (gconstpointer)func, NULL, (sig), (no_wrapper), NULL))
#endif

mono_bstr
mono_string_to_bstr_impl (MonoStringHandle s, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (s))
		return NULL;

	MonoGCHandle gchandle = NULL;
	mono_bstr const res = mono_ptr_to_bstr (mono_string_handle_pin_chars (s, &gchandle), mono_string_handle_length (s));
	mono_gchandle_free_internal (gchandle);
	return res;
}

void
mono_cominterop_init (void)
{
	/*FIXME

	This icalls are used by the marshal code when doing PtrToStructure and StructureToPtr and pinvoke.

	If we leave them out and the FullAOT compiler finds the need to emit one of the above 3 wrappers it will
	g_assert.

	The proper fix would be to emit warning, remove them from marshal.c when DISABLE_COM is used and
	emit an exception in the generated IL.
	*/
	register_icall (mono_string_to_bstr, mono_icall_sig_ptr_obj, FALSE);
	register_icall (mono_string_from_bstr_icall, mono_icall_sig_obj_ptr, FALSE);
	register_icall (mono_free_bstr, mono_icall_sig_void_ptr, FALSE);
}

// This function is used regardless of the BSTR type, so cast the return value
// Inputted string length, in bytes, should include the null terminator
// Returns the start of the string itself
static gpointer
mono_bstr_alloc (size_t str_byte_len)
{
	// Allocate string length plus pointer-size integer to store the length, aligned to 16 bytes
	size_t alloc_size = str_byte_len + SIZEOF_VOID_P;
	alloc_size += (16 - 1);
	alloc_size &= ~(16 - 1);
	gpointer ret = g_malloc0 (alloc_size);
	return ret ? (char *)ret + SIZEOF_VOID_P : NULL;
}

static void
mono_bstr_set_length (gunichar2 *bstr, int slen)
{
	*((guint32 *)bstr - 1) = slen * sizeof (gunichar2);
}

static mono_bstr
default_ptr_to_bstr (const gunichar2* ptr, int slen)
{
	// In Mono, historically BSTR was allocated with a guaranteed size prefix of 4 bytes regardless of platform.
	// Presumably this is due to the BStr documentation page, which indicates that behavior and then directs you to call
	// SysAllocString on Windows to handle the allocation for you. Unfortunately, this is not actually how it works:
	// The allocation pre-string is pointer-sized, and then only 4 bytes are used for the length regardless. Additionally,
	// the total length is also aligned to a 16-byte boundary. This preserves the old behavior on legacy and fixes it for
	// netcore moving forward.
	mono_bstr const s = (mono_bstr)mono_bstr_alloc ((slen + 1) * sizeof (gunichar2));
	if (s == NULL)
		return NULL;

	mono_bstr_set_length (s, slen);
	if (ptr)
		memcpy (s, ptr, slen * sizeof (gunichar2));
	s [slen] = 0;
	return s;
}

/* PTR can be NULL */
mono_bstr
mono_ptr_to_bstr (const gunichar2* ptr, int slen)
{
#if HAVE_API_SUPPORT_WIN32_BSTR
	return SysAllocStringLen (ptr, slen);
#else
	return default_ptr_to_bstr (ptr, slen);
#endif // HAVE_API_SUPPORT_WIN32_BSTR
}

char *
mono_ptr_to_ansibstr (const char *ptr, size_t slen)
{
	char *s = (char *)mono_bstr_alloc ((slen + 1) * sizeof(char));
	if (s == NULL)
		return NULL;
	*((guint32 *)s - 1) = (guint32)(slen * sizeof (char));
	if (ptr)
		memcpy (s, ptr, slen * sizeof (char));
	s [slen] = 0;
	return s;
}

MonoStringHandle
mono_string_from_bstr_checked (mono_bstr_const bstr, MonoError *error)
{
	if (!bstr)
		return NULL_HANDLE_STRING;
#if HAVE_API_SUPPORT_WIN32_BSTR
	return mono_string_new_utf16_handle (bstr, SysStringLen ((BSTR)bstr), error);
#else
	return mono_string_new_utf16_handle (bstr, *((guint32 *)bstr - 1) / sizeof (gunichar2), error);
#endif // HAVE_API_SUPPORT_WIN32_BSTR
}

MonoString *
mono_string_from_bstr (/*mono_bstr_const*/gpointer bstr)
{
	// FIXME gcmode
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoStringHandle result = mono_string_from_bstr_checked ((mono_bstr_const)bstr, error);
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

MonoStringHandle
mono_string_from_bstr_icall_impl (mono_bstr_const bstr, MonoError *error)
{
	return mono_string_from_bstr_checked (bstr, error);
}

MONO_API void
mono_free_bstr (/*mono_bstr_const*/gpointer bstr)
{
	if (!bstr)
		return;
#if HAVE_API_SUPPORT_WIN32_BSTR
	SysFreeString ((BSTR)bstr);
#else
	g_free (((char *)bstr) - SIZEOF_VOID_P);
#endif // HAVE_API_SUPPORT_WIN32_BSTR
}

gboolean
mono_marshal_free_ccw (MonoObject* object)
{
	return FALSE;
}

mono_bstr
ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR (const gunichar2* ptr, int len)
{
	return mono_ptr_to_bstr (ptr, len);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (mono_bstr_const ptr)
{
	mono_free_bstr ((gpointer)ptr);
}
