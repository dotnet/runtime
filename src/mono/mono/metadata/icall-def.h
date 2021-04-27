/**
 * \file
 * This file contains the default set of the mono internal calls.
 * Each type that has internal call methods must be declared here
 * with the ICALL_TYPE macro as follows:
 *
 * 	ICALL_TYPE(typeid, typename, first_icall_id)
 *
 * typeid must be a C symbol name unique to the type, don't worry about namespace
 * 	pollution, since it will be automatically prefixed to avoid it.
 * typename is a C string containing the full name of the type
 * first_icall_id is the symbol ID of the first internal call of the declared
 * 	type (see below)
 *
 * The list of internal calls of the methods of a type must follow the
 * type declaration. Each internal call is defined by the following macro:
 *
 * 	ICALL(icallid, methodname, cfuncptr)
 *
 * icallid must be a C symbol, unique for each icall defined in this file and
 * typically equal to the typeid + '_' + a sequential number.
 * methodname is a C string defining the method name and the optional signature
 * (the signature is required only when several internal calls in the type
 * have the same name)
 * cfuncptr is the C function that implements the internal call. Note that this
 * file is included at the end of metadata/icall.c, so the C function must be
 * visible to the compiler there.
 *
 * *** Adding a new internal call ***
 * Remember that ICALL_TYPE declarations must be kept sorted wrt each other
 * ICALL_TYPE declaration. The same happens for ICALL declarations, but only
 * limited to the icall list of each type. The sorting is based on the type or
 * method name.
 * When adding a new icall, make sure it is inserted correctly in the list and
 * that it defines a unique ID. ID are currently numbered and ordered, but if
 * you need to insert a method in the middle, don't bother renaming all the symbols.
 * Remember to change also the first_icall_id argument in the ICALL_TYPE
 * declaration if you add a new icall at the beginning of a type's icall list.
 *
 *
 * *** (Experimental) Cooperative GC support via Handles and MonoError ***
 * An icall can use the coop GC handles infrastructure from handles.h to avoid some
 * boilerplate when manipulating managed objects from runtime code and to use MonoError for
 * threading exceptions out to managed callerrs:
 *
 * HANDLES(icallid, methodname, cfuncptr, return-type, number-of-parameters, (parameter-types))
 * types:
 *   managed types are just MonoObject, MonoString, etc. `*` and Handle prefix are appended automatically.
 *   Types must be single identifiers, and be handled in icall-table.h.
 *   MonoError is added to the list automatically.
 *   A function with no parameters is "0, ()"
 *   "Out" and "InOut" types are appended with "Out" and "InOut". In is assumed.
 *   Out and InOut raw pointers get "**" appended. In gets just "*".
 *   Out/InOut only applied to managed pointers/handles.
 *   Things like "int*" are supported by typedefs like "typedef int *int_ptr".
 *   "void*" and "HANDLE" are written "gpointer".
 *   The list of available types is in icall-table.h.
 *   Using a type not there errors unceremoniously.
 *
 * An icall with a HANDLES() declaration wrapped around it will have a generated wrapper
 * that:
 *   (1) Updates the coop handle stack on entry and exit
 *   (2) Call the cfuncptr with a new signature:
 *     (a) All managed object reference in arguments will be wrapped in a type-unsafe handle
 *         (i.e., MonoString* becomes struct { MonoString* raw }* aka MonoRawHandle*)
 *     (b) the same for the return value (MonoObject* return becomes MonoObjectHandle)
 *     (c) An additional final argument is added of type MonoError*
 *     example:    class object {
 *                     [MethodImplOptions(InternalCall)]
 *                     String some_icall (object[] x);
 *                 }
 *     should be implemented as:
 *        MonoStringHandle some_icall (MonoObjectHandle this_handle, MonoArrayHandle x_handle, MonoError *error);
 *   (3) The wrapper will automatically call mono_error_set_pending_exception (error) and raise the resulting exception.
 * Note:  valuetypes use the same calling convention as normal.
 *
 * HANDLES() wrappers are generated dynamically by marshal-ilgen.c, using metadata to see types, producing type-unsafe handles (void*).
 * HANDLES() additional small wrappers are generated statically by icall-table.h, using the signatures here, producing
 * type-safe handles, i.e. MonoString* => MonoRawHandle* => struct { MonoString** raw }.
 *
 */

//
// _ref means marshal-ilgen.c created a handle for an interior pointer.
// _ptr means marshal-ilgen.c passed the parameter through unchanged.
// At the C level, they are the same.
//
// If the C# managed declaration for an icall, with 7 parameters, is:
// 	object your_internal_call (int x, object y, ref int z, IntPtr p, ref MyStruct c, ref MyClass, out string s);
//
// you should write:
// 	HANDLES(ID_n, "your_internal_call", "ves_icall_your_internal_call", MonoObject, 7, (gint32, MonoObject, gint32_ref, gpointer, MyStruct_ref, MyClassInOut, MonoStringOut))
//
// 7 is the number of parameters, the length of the last macro parameter.
// IntPtr is unchecked. You could also say gsize or gssize.
//
// and marshal-ilgen.c will generate a call to
// 	MonoRawHandle* ves_icall_your_internal_call_raw (gint32, MonoRawHandle*, gint32*, gpointer, MyStruct*, MonoRawHandle*, MonoRawHandle*);
//
// whose body will be generated by the HANDLES() macro, and which will call the following function that you have to implement:
// 	MonoObjectHandle ves_icall_your_internal_call (gint32, MonoObjectHandle, gint32*, gpointer, MyStruct*, MyClassHandleInOut, MonoStringOut, MonoError *error);
//
// Note the extra MonoError* argument.
// Note that "ref" becomes "HandleInOut" for managed types.
// "_ref" becomes "*" for unmanaged types.
// "_out" becomes "HandleOut" or "*".
// "HandleIn" is the default for managed types, and is just called "Handle".
//

#include "icall-def-netcore.h"

// This is similar to HANDLES() but is for icalls passed to register_jit_icall.
// There is no metadata for these. No signature matching.
// Presently their wrappers are less efficient, but hopefully that can be fixed,
// by making a direct call to them, inserting the LMF below them (possibly,
// providing it to them via a function pointer, if it cannot be done in C),
// and of course using managed-style coop handles within them.
// Alternately, ilgen.
//
// This is not just for register_icall, for any time coop wrappers are needed,
// that there is no metadata for. For example embedding API.

// helper for the managed alloc support
MONO_HANDLE_REGISTER_ICALL (ves_icall_string_alloc, MonoString, 1, (int))

// Windows: Allocates with CoTaskMemAlloc.
// Unix: Allocates with g_malloc.
// Either way: Free with mono_marshal_free (Windows:CoTaskMemFree, Unix:g_free).
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf8str, gpointer, 1, (MonoString))

MONO_HANDLE_REGISTER_ICALL (mono_array_to_byte_byvalarray, void, 3, (gpointer, MonoArray, guint32))
MONO_HANDLE_REGISTER_ICALL (mono_array_to_lparray, gpointer, 1, (MonoArray))
MONO_HANDLE_REGISTER_ICALL (mono_array_to_savearray, gpointer, 1, (MonoArray))
MONO_HANDLE_REGISTER_ICALL (mono_byvalarray_to_byte_array, void, 3, (MonoArray, const_char_ptr, guint32))
MONO_HANDLE_REGISTER_ICALL (mono_delegate_to_ftnptr, gpointer, 1, (MonoDelegate))
MONO_HANDLE_REGISTER_ICALL (mono_free_lparray, void, 2, (MonoArray, gpointer_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_ftnptr_to_delegate, MonoDelegate, 2, (MonoClass_ptr, gpointer))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_asany, gpointer, 3, (MonoObject, MonoMarshalNative, int))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_free_asany, void, 4, (MonoObject, gpointer, MonoMarshalNative, int))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_string_to_utf16_copy, gunichar2_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_object_isinst_icall, MonoObject, 2, (MonoObject, MonoClass_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_builder_to_utf16, gunichar2_ptr, 1, (MonoStringBuilder))
MONO_HANDLE_REGISTER_ICALL (mono_string_builder_to_utf8, char_ptr, 1, (MonoStringBuilder))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_ansibstr, MonoString, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_bstr_icall, MonoString, 1, (mono_bstr_const))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalstr, MonoString, 2, (const_char_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalwstr, MonoString, 2, (const_gunichar2_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_tbstr, MonoString, 1, (gpointer))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_len_wrapper, MonoString, 2, (const_char_ptr, guint))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_wrapper_internal, MonoString, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_ansibstr, char_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_bstr, mono_bstr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalstr, void, 3, (char_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalwstr, void, 3, (gunichar2_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_tbstr, gpointer, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf16_internal, mono_unichar2_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf32_internal, mono_unichar4_ptr, 1, (MonoString)) // embedding API
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder, void, 2, (MonoStringBuilder, const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder2, MonoStringBuilder, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder, void, 2, (MonoStringBuilder, const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder2, MonoStringBuilder, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_type_from_handle, MonoReflectionType, 1, (MonoType_ptr)) // called by icalls
MONO_HANDLE_REGISTER_ICALL (ves_icall_marshal_alloc, gpointer, 1, (gsize))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_from_utf16, MonoString, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_to_utf8, char_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (ves_icall_string_new_wrapper, MonoString, 1, (const_char_ptr))
