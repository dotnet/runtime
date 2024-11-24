// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
Macros for generating Linux user_events tracepoints via libtracepoint.

Prerequisites (if not met, register/write/unregister will be no-ops):

- Kernel built with tracefs and UserEvents (CONFIG_USER_EVENTS=y).
- tracefs mounted (e.g. /sys/kernel/tracing or /sys/kernel/debug/tracing).
- Caller must have appropriate permissions: x on tracefs mount point,
  rw on .../tracing/user_events_data.

Quick start:

    #include <tracepoint/tracepoint-provider.h>

    // Define a group of tracepoints that register and unregister together.
    TPP_DEFINE_PROVIDER(MyProvider);

    int main(int argc, char* argv[])
    {
        // Register all tracepoints in group.
        TPP_REGISTER_PROVIDER(MyProvider);

        // If the "MyTracepoint1" tracepoint is registered and enabled,
        // evaluate the field value expressions and generate a tracepoint
        // event. This tracepoint will be registered and unregistered as
        // part of the MyProvider group.
        TPP_WRITE(MyProvider, "MyTracepoint1",
            TPP_STRING("arg0", argv[0]), // field name is "arg0".
            TPP_INT32("argc", argc)); // field name is "argc".

        // Unregister all tracepoints in group.
        TPP_UNREGISTER_PROVIDER(MyProvider);
    }

    // Advanced: Define my_tracepoint_func(char const* str, int32_t num)
    // and my_tracepoint_func_enabled(void) functions for the "MyTracepoint2"
    // tracepoint. This tracepoint will be registered and unregistered as part
    // of the MyProvider group. The generated my_tracepoint_func_enabled
    // function behaves the same as TPP_WRITE, but TPP_FUNCTION can be useful
    // if you need to be able to check whether the tracepoint is enabled
    // separately from generating the event.
    TPP_FUNCTION(MyProvider, "MyTracepoint2", my_tracepoint,
        TPP_STRING("name", str),  // field name is "name".
        TPP_INT32("count", num)); // field name is "size".

The following top-level macros are provided:

- TPP_DECLARE_PROVIDER(ProviderSymbol) - forward declaration of ProviderSymbol
- TPP_DEFINE_PROVIDER(ProviderSymbol) - definition of ProviderSymbol
- TPP_REGISTER_PROVIDER(ProviderSymbol) - activates all tracepoints in group
- TPP_UNREGISTER_PROVIDER(ProviderSymbol) - deactivates all tracepoints in group
- TPP_WRITE(ProviderSymbol, "tracepoint_name", field macros...)
- TPP_FUNCTION(ProviderSymbol, "tracepoint_name", func_name, field macros...)

The following field macros are provided:

- TPP_UINT8("field_name", value) - creates a "u8" field.
- TPP_INT8("field_name", value) - creates an "s8" field.
- TPP_UINT16("field_name", value) - creates a "u16" field.
- TPP_INT16("field_name", value) - creates an "s8" field.
- TPP_UINT32("field_name", value) - creates a "u32" field.
- TPP_INT32("field_name", value) - creates an "s32" field.
- TPP_UINT64("field_name", value) - creates a "u64" field.
- TPP_INT64("field_name", value) - creates an "s64" field.
- TPP_UINTPTR("field_name", value) - creates a "u32" or "u64" field.
- TPP_INTPTR("field_name", value) - creates an "s32" or "s64" field.
- TPP_STRING("field_name", value_ptr) - creates a "__rel_loc char[]" field.
- TPP_CHAR_ARRAY("field_name", size, value_ptr) - creates a "char[SIZE]" field.
- TPP_UCHAR_ARRAY("field_name", size, value_ptr) - creates an "unsigned char[SIZE]" field.
- TPP_STRUCT_PTR("field_name", "struct_type", size, value_ptr) - creates a "struct struct_type" field.
- TPP_CUSTOM_BYVAL - custom by-value field.
- TPP_CUSTOM_BYREF - custom by-ref field.
- TPP_CUSTOM_REL_LOC - custom rel_loc by-ref field.
- TPP_CUSTOM_REL_LOC_STR - custom rel_loc nul-terminated field.

Usage notes:

Symbols starting with TPP_ are part of the public interface of this
header. Symbols starting with "_tpp" are private internal implementation
details that are not supported for use outside this header and may be changed
or removed in future versions of this header.

Tracepoint names, field names, and struct names need to be specified as string
literals like "my_field_name". They cannot be variables. Using variables may
cause compile errors or may successfully compile but lead to
incorrectly-generated tracepoints.

Tracepoint names, field names, and struct names are restricted, but the
restrictions are not validated by the macros in this header:

- Names need to be ASCII identifiers. They must start with '_' or an ASCII
  letter and contain only '_', ASCII letters, and ASCII digits.
- Tracepoint names may be followed by ":FLAG1,FLAG2...".
- Tracepoint names need to be unique: you must not define multiple tracepoints
  with the same name but different field names or field types.
- You can have multiple tracepoints with the same name, the same field names,
  and the same field types, but this is inefficient and error-prone. Prefer to
  define a function that generates the tracepoint instead of using the same
  TPP_WRITE or TPP_FUNCTION in multiple places in your code.

The Size parameter to TPP_CHAR_ARRAY and TPP_UCHAR_ARRAY must be an integer
literal like 45 or 0x20 or a macro that resolves to an integer literal. It
cannot be an expression, e.g. it cannot be sizeof(TYPE).
*/

#pragma once
#ifndef _included_tracepoint_provider_h
#define _included_tracepoint_provider_h 1

#include "tracepoint.h"
#include <stdint.h>

#ifdef __EDG__
#pragma region Public_interface
#endif

/*
This structure is left undefined to ensure a compile error for any attempt to
copy or dereference the provider symbol. The provider symbol is a token, not a
variable or handle.
*/
struct _tpp_provider_symbol;

/*
Macro TPP_DECLARE_PROVIDER(ProviderSymbol):
Invoke this macro to forward-declare a provider symbol that will be defined
elsewhere. TPP_DECLARE_PROVIDER is typically used in a header.

An invocation of

    TPP_DECLARE_PROVIDER(MyProvider);

can be thought of as expanding to something like this:

    extern "C" _tpp_provider_symbol MyProvider; // Forward declaration

A symbol declared by TPP_DECLARE_PROVIDER must later be defined in a
.c or .cpp file using the TPP_DEFINE_PROVIDER macro.
*/
#define TPP_DECLARE_PROVIDER(ProviderSymbol) \
    _tpp_EXTERN_C tracepoint_definition const* _tpp_PASTE2(__start__tppEventPtrs_, ProviderSymbol)[] __attribute__((weak, visibility("hidden"))); \
    _tpp_EXTERN_C tracepoint_definition const* _tpp_PASTE2(__stop__tppEventPtrs_, ProviderSymbol)[] __attribute__((weak, visibility("hidden"))); \
    _tpp_EXTERN_C struct _tpp_provider_symbol ProviderSymbol __attribute__((visibility("hidden"))); /* Empty provider variable to help with code navigation. */ \
    _tpp_EXTERN_C tracepoint_provider_state _tpp_PASTE2(_tppProvState_, ProviderSymbol) __attribute__((visibility("hidden")))  /* Actual provider variable is hidden behind prefix. */

/*
Macro TPP_DEFINE_PROVIDER(ProviderSymbol):
Invoke this macro to define the symbol for a provider. A provider is a
set of tracepoints that are registered and unregistered as a group.

An invocation of

    TPP_DEFINE_PROVIDER(MyProvider);

can be thought of as expanding to something like this:

    _tpp_provider_symbol MyProvider = { ... };

Note that the provider symbol is created in the "unregistered" state. A call
to TPP_WRITE with an unregistered provider is a safe no-op. Call
TPP_REGISTER_PROVIDER to register the provider.
*/
#define TPP_DEFINE_PROVIDER(ProviderSymbol) \
    TPP_DECLARE_PROVIDER(ProviderSymbol); \
    tracepoint_provider_state _tpp_PASTE2(_tppProvState_, ProviderSymbol) = TRACEPOINT_PROVIDER_STATE_INIT \

/*
Macro TPP_UNREGISTER_PROVIDER(ProviderSymbol):
Invoke this macro to unregister your provider, deactivating all of the
tracepoints that are associated with the provider. Normally you will register
at component initialization (program startup or shared object load) and
unregister at component shutdown (program exit or shared object unload).

It is ok to call TPP_UNREGISTER_PROVIDER on a provider that has not been
registered (e.g. if the call to TPP_REGISTER_PROVIDER failed). Unregistering
an unregistered provider is a safe no-op.

After unregistering a provider, it is ok to register it again. In other words,
the following sequence is ok:

    TPP_REGISTER_PROVIDER(MyProvider);
    ...
    TPP_UNREGISTER_PROVIDER(MyProvider);
    ...
    TPP_REGISTER_PROVIDER(MyProvider);
    ...
    TPP_UNREGISTER_PROVIDER(MyProvider);

Re-registering a provider should only happen because a component has been
uninitialized and then reinitialized. You should not register and unregister a
provider each time you need to write a few events.

Note that unregistration is important, especially in the case of a shared
object that might be dynamically unloaded before the process ends. Failure to
unregister before the shared object unloads may cause process memory
corruption when the kernel tries to update the enabled/disabled states of
tracepoint variables that no longer exist.
*/
#define TPP_UNREGISTER_PROVIDER(ProviderSymbol) \
    (tracepoint_close_provider( \
        &_tpp_PASTE2(_tppProvState_, ProviderSymbol) ))

/*
Macro TPP_REGISTER_PROVIDER(ProviderSymbol):
Invoke this macro to register your provider, activating all of the tracepoints
that are associated with the provider. Normally you will register at component
initialization (program startup or shared object load) and unregister at
component shutdown (program exit or shared object unload).

Returns 0 for success, errno otherwise. Result is primarily for
debugging/diagnostics and is usually ignored for production code. If
registration fails, subsequent TPP_WRITE will be a safe no-op.

Thread safety: It is NOT safe to call TPP_REGISTER_PROVIDER while a
TPP_REGISTER_PROVIDER or TPP_UNREGISTER_PROVIDER for the same provider symbol
might be in progress on another thread.

Precondition: The provider must be in the "unregistered" state. It is a fatal
error to call TPP_REGISTER_PROVIDER on a provider that is already registered.
*/
#define TPP_REGISTER_PROVIDER(ProviderSymbol) \
    (tracepoint_open_provider_with_tracepoints( \
        &_tpp_PASTE2(_tppProvState_, ProviderSymbol), \
        _tpp_PASTE2(__start__tppEventPtrs_, ProviderSymbol), \
        _tpp_PASTE2(__stop__tppEventPtrs_, ProviderSymbol) ))

/*
Macro TPP_WRITE(ProviderSymbol, "tracepoint_name", fields...):
Invoke this macro to generate a tracepoint event.

An invocation of

    TPP_WRITE(MyProvider, "MyTracepointName",
        TPP_INT32("int_field_name", my_int_value()),
        TPP_STRING("string_field_name", my_string_value()));

can be thought of as expanding to something like this:

    _tpp_enabled_MyTracepointName != 0
        ? _tpp_emit_MyTracepointName(my_int_value(), my_string_value())
        : 0

The "tracepoint_name" parameter must be a char string literal (not a variable)
and must be a valid ASCII identifier: it must start with '_' or an ASCII
letter, and must contain only '_', ASCII letters, and ASCII digits.

Supports up to 99 field parameters (subject to compiler and tracepoint system
limitations). Each field parameter must be a field macro such as TPP_UINT8,
TPP_STRING, etc.

Returns 0 if the tracepoint is written, EBADF if the tracepoint is unregistered
or disabled, or other errno for an error. Result is primarily for
debugging/diagnostics and is usually ignored for production code.
*/
#define TPP_WRITE(ProviderSymbol, TracepointNameString, ...) \
    _tppWriteImpl(ProviderSymbol, TracepointNameString, __VA_ARGS__)

/*
Macro TPP_FUNCTION(ProviderSymbol, "tracepoint_name", func_name, fields...):
Invoke this macro to define func_name(fields...) and func_name_enabled().

The generated func_name(fields...) function is equivalent to
TPP_WRITE(ProviderSymbol, "tracepoint_name", fields...), and the
func_name_enabled() function efficiently returns nonzero if "tracepoint_name"
is registered and enabled.

An invocation of

    TPP_FUNCTION(MyProvider, "MyTracepointName", my_tracepoint,
        TPP_INT32("int_field_name", myIntVar),
        TPP_STRING("string_field_name", myString));

can be thought of as expanding to something like this:

    static int _tpp_enabled_MyTracepointName; // Hidden magic variable.

    int my_tracepoint_enabled(void)
    {
        return _tpp_enabled_MyTracepointName;
    }

    int my_tracepoint(int32_t myIntVar, char const* myString)
    {
        return _tpp_enabled_MyTracepointName != 0
            ? _tpp_emit_MyTracepointName(myIntVar, myString)
            : 0
    }

The "tracepoint_name" parameter must be a char string literal (not a variable)
and must be a valid ASCII identifier: it must start with '_' or an ASCII
letter and must contain only '_', ASCII letters, and ASCII digits.

Supports up to 99 field parameters (subject to compiler and tracepoint system
limitations). Each field parameter must be a field macro such as TPP_UINT8,
TPP_STRING, etc.

The generated func_name(field values...) function returns 0 if the tracepoint
is written, EBADF if the tracepoint is unregistered or disabled, or other errno
for an error. Result is primarily for debugging/diagnostics and is usually
ignored for production code.
*/
#define TPP_FUNCTION(ProviderSymbol, TracepointNameString, FunctionName, ...) \
    _tppFunctionImpl(ProviderSymbol, TracepointNameString, FunctionName, __VA_ARGS__)

#define _tpp_NARGS(...) _tpp_NARGS_imp(_tpp_IS_EMPTY(__VA_ARGS__), (__VA_ARGS__))

/*
Field macros for use as field parameters to TPP_WRITE or TPP_FUNCTION:

Example:

    TPP_WRITE(MyProvider, "MyTracepointName",
        TPP_INT32("int_field_name", my_int_value()),
        TPP_STRING("string_field_name", my_string_value()));

The "FieldNameString" parameter must be a char string literal (not a variable)
and must be a valid ASCII identifier: it must start with '_' or an ASCII
letter and must contain only '_', ASCII letters, and ASCII digits.

When these macros are used in TPP_WRITE, the Value parameter may be a complex
expression such as a call to a function. The Value parameter's expression will
only be evaluated if the tracepoint is registered and enabled.

When these macros are used in TPP_FUNCTION, the Value parameter must be a
simple identifier. The Value parameter will be used as the name of the
corresponding function parameter.
*/
#define TPP_UINT8(FieldNameString, Value)   TPP_CUSTOM_BYVAL("u8 "  FieldNameString, uint8_t,  Value)
#define TPP_INT8(FieldNameString, Value)    TPP_CUSTOM_BYVAL("s8 "  FieldNameString, int8_t,   Value)
#define TPP_UINT16(FieldNameString, Value)  TPP_CUSTOM_BYVAL("u16 " FieldNameString, uint16_t, Value)
#define TPP_INT16(FieldNameString, Value)   TPP_CUSTOM_BYVAL("s16 " FieldNameString, int16_t,  Value)
#define TPP_UINT32(FieldNameString, Value)  TPP_CUSTOM_BYVAL("u32 " FieldNameString, uint32_t, Value)
#define TPP_INT32(FieldNameString, Value)   TPP_CUSTOM_BYVAL("s32 " FieldNameString, int32_t,  Value)
#define TPP_UINT64(FieldNameString, Value)  TPP_CUSTOM_BYVAL("u64 " FieldNameString, uint64_t, Value)
#define TPP_INT64(FieldNameString, Value)   TPP_CUSTOM_BYVAL("s64 " FieldNameString, int64_t,  Value)

#if UINTPTR_MAX == UINT64_MAX
#define TPP_UINTPTR(FieldNameString, Value) TPP_CUSTOM_BYVAL("u64 " FieldNameString, uint64_t, Value)
#define TPP_INTPTR(FieldNameString, Value)  TPP_CUSTOM_BYVAL("s64 " FieldNameString, int64_t,  Value)
#elif UINTPTR_MAX == UINT32_MAX
#define TPP_UINTPTR(FieldNameString, Value) TPP_CUSTOM_BYVAL("u32 " FieldNameString, uint32_t, Value)
#define TPP_INTPTR(FieldNameString, Value)  TPP_CUSTOM_BYVAL("s32 " FieldNameString, int32_t,  Value)
#endif // UINTPTR size

// TPP_STRING("FieldNameString", ValuePtr):
// Adds a NUL-terminated char string field to the tracepoint.
//
// Field value will be "" if ValuePtr == NULL.
// Field value size will be (ValuePtr == NULL ? 1 : strlen(ValuePtr) + 1).
// Resulting field will be defined as "__rel_loc char[] FieldNameString".
#define TPP_STRING(FieldNameString, ValuePtr) \
    TPP_CUSTOM_REL_LOC_STR("char[] " FieldNameString, char, ValuePtr)

// TPP_CHAR_ARRAY("FieldNameString", Size, ValuePtr):
// Adds a fixed-length char[Size] field to the tracepoint.
// The Size parameter must be an integer literal or a macro that
// evaluates to an integer literal. It cannot be an expression. For example,
// it could be 32 or 0x20 but it cannot be something like sizeof(TYPE).
//
// ValuePtr is treated as void const*.
// Resulting field will be defined as "char[Size] FieldNameString".
#define TPP_CHAR_ARRAY(FieldNameString, Size, ValuePtr) \
    TPP_CUSTOM_BYREF("char[" _tpp_STRINGIZE(Size) "] " FieldNameString, void, Size, ValuePtr)

// TPP_UCHAR_ARRAY("FieldNameString", Size, ValuePtr):
// Adds a fixed-length unsigned char[Size] field to the tracepoint.
// The Size parameter must be an integer literal or a macro that
// evaluates to an integer literal. It cannot be an expression. For example,
// it could be 32 or 0x20 but it cannot be something like sizeof(TYPE).
//
// ValuePtr is treated as void const*.
// Resulting field will be defined as "unsigned char[Size] FieldNameString".
#define TPP_UCHAR_ARRAY(FieldNameString, Size, ValuePtr) \
    TPP_CUSTOM_BYREF("unsigned char[" _tpp_STRINGIZE(Size) "] " FieldNameString, void, Size, ValuePtr)

// TPP_STRUCT_PTR("FieldNameString", "StructTypeString", Size, ValuePtr):
// Adds struct data to the tracepoint.
// The Size parameter must be an integer literal or a macro that
// evaluates to an integer literal. It cannot be an expression. For example,
// it could be 32 or 0x20 but it cannot be something like sizeof(TYPE).
//
// ValuePtr is treated as void const*.
// Resulting field will be defined as "struct StructTypeString FieldNameString Size".
#define TPP_STRUCT_PTR(FieldNameString, StructTypeString, Size, ValuePtr) \
    TPP_CUSTOM_BYREF("struct " StructTypeString " " FieldNameString " " _tpp_STRINGIZE(Size), void, Size, ValuePtr)

// TPP_CUSTOM_BYVAL("FieldDeclString", Ctype, Value):
// Advanced: Adds a by-value field to the tracepoint.
//
// Example: TPP_CUSTOM_BYVAL("unsigned int my_field_name", uint32_t, my_val)
//
// Value must be implicitly-convertible to Ctype.
// Field size will be sizeof(Ctype).
#define TPP_CUSTOM_BYVAL(FieldDeclString, Ctype, Value) \
    (_tppArgByVal, FieldDeclString, Ctype, Value)

// TPP_CUSTOM_BYREF("FieldDeclString", Ctype, ConstantValueSize, ValuePtr):
// Advanced: Adds a fixed-length by-ref field to the tracepoint.
//
// Example: TPP_CUSTOM_BYREF("char[16] my_field_name", UUID, sizeof(UUID), &my_uuid)
//
// ValuePtr must be implicitly-convertible to Ctype const*.
// ConstantValueSize must be the compile-time constant field size in bytes.
#define TPP_CUSTOM_BYREF(FieldDeclString, Ctype, ConstantValueSize, ValuePtr) \
    (_tppArgByRef, FieldDeclString, Ctype, ConstantValueSize, ValuePtr)

// TPP_CUSTOM_REL_LOC("FieldDeclString", Ctype, ValueSize, ValuePtr):
// Advanced: Adds a variable-length by-ref field to the tracepoint.
//
// ValueSize must be the field size in bytes.
// ValuePtr must be implicitly-convertible to Ctype const*.
// Resulting field will be defined as "__rel_loc FieldDeclString".
#define TPP_CUSTOM_REL_LOC(FieldDeclString, Ctype, ValueSize, ValuePtr) \
    (_tppArgRelLoc, FieldDeclString, Ctype, ValueSize, ValuePtr)

// TPP_CUSTOM_REL_LOC_STR("FieldDeclString", Ctype, ValuePtr):
// Advanced: Adds a nul-terminated by-ref field to the tracepoint.
//
// TPP_CUSTOM_REL_LOC_STR("FieldDeclString", Ctype, ValuePtr)
// is approximately equivalent to
// TPP_CUSTOM_REL_LOC("FieldDeclString", Ctype, ValuePtr, strlen((char*)ValuePtr))
// except that it treats NULL as "" and it only evaluates ValuePtr once.
//
// ValuePtr must be implicitly-convertible to Ctype const*.
// Resulting field will be defined as "__rel_loc FieldDeclString".
#define TPP_CUSTOM_REL_LOC_STR(FieldDeclString, Ctype, ValuePtr) \
    (_tppArgRelLocStr, FieldDeclString, Ctype, 0, ValuePtr)

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_utility macros (for internal use only)
#endif

#ifndef _tpp_NOEXCEPT
#ifdef __cplusplus
#define _tpp_NOEXCEPT noexcept
#else // __cplusplus
#define _tpp_NOEXCEPT
#endif // __cplusplus
#endif // _tpp_NOEXCEPT

#ifndef _tpp_INLINE_ATTRIBUTES
#define _tpp_INLINE_ATTRIBUTES
#endif // _tpp_INLINE_ATTRIBUTES

#ifdef __cplusplus
#define _tpp_EXTERN_C       extern "C"
#else // __cplusplus
#define _tpp_EXTERN_C       extern // In C, linkage is already "C".
#endif // __cplusplus

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_PASTE2(a, b)        _tpp_PASTE2_imp(a, b)
#define _tpp_PASTE2_imp(a, b)    a##b

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_PARENTHESIZE(...) (__VA_ARGS__)

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_STRINGIZE(x) _tpp_STRINGIZE_imp(x)
#define _tpp_STRINGIZE_imp(x) #x

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_CAT(a, ...) _tpp_CAT_imp(a, __VA_ARGS__)
#define _tpp_CAT_imp(a, ...) a##__VA_ARGS__

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_SPLIT(cond, ...) _tpp_SPLIT_imp(cond, (__VA_ARGS__))
#define _tpp_SPLIT_imp(cond, args) _tpp_PASTE2(_tpp_SPLIT_imp, cond) args
#define _tpp_SPLIT_imp0(false_val, ...) false_val
#define _tpp_SPLIT_imp1(false_val, ...) __VA_ARGS__

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_IS_PARENTHESIZED(...) \
    _tpp_SPLIT(0, _tpp_CAT(_tpp_IS_PARENTHESIZED_imp, _tpp_IS_PARENTHESIZED_imp0 __VA_ARGS__))
#define _tpp_IS_PARENTHESIZED_imp_tpp_IS_PARENTHESIZED_imp0 0,
#define _tpp_IS_PARENTHESIZED_imp0(...) 1
#define _tpp_IS_PARENTHESIZED_imp1 1,

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_IS_EMPTY(...) _tpp_SPLIT(                      \
    _tpp_IS_PARENTHESIZED(__VA_ARGS__),                     \
    _tpp_IS_PARENTHESIZED(_tpp_PARENTHESIZE __VA_ARGS__()), \
    0)

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_NARGS(...) _tpp_NARGS_imp(_tpp_IS_EMPTY(__VA_ARGS__), (__VA_ARGS__))
#define _tpp_NARGS_imp(is_empty, args) _tpp_PASTE2(_tpp_NARGS_imp, is_empty) args
#define _tpp_NARGS_imp0(...) _tpp_PASTE2(_tpp_NARGS_imp2( \
    __VA_ARGS__,                            \
    99, 98, 97, 96, 95, 94, 93, 92, 91, 90, \
    89, 88, 87, 86, 85, 84, 83, 82, 81, 80, \
    79, 78, 77, 76, 75, 74, 73, 72, 71, 70, \
    69, 68, 67, 66, 65, 64, 63, 62, 61, 60, \
    59, 58, 57, 56, 55, 54, 53, 52, 51, 50, \
    49, 48, 47, 46, 45, 44, 43, 42, 41, 40, \
    39, 38, 37, 36, 35, 34, 33, 32, 31, 30, \
    29, 28, 27, 26, 25, 24, 23, 22, 21, 20, \
    19, 18, 17, 16, 15, 14, 13, 12, 11, 10, \
    9, 8, 7, 6, 5, 4, 3, 2, 1, ), )
#define _tpp_NARGS_imp1() 0
#define _tpp_NARGS_imp2(                              \
    a1, a2, a3, a4, a5, a6, a7, a8, a9,               \
    a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, \
    a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, \
    a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, \
    a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, \
    a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, \
    a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, \
    a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, \
    a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, \
    a90, a91, a92, a93, a94, a95, a96, a97, a98, a99, \
    size, ...) size

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_foreach macro (for internal use only)
#endif

// Internal implementation detail: Not for use outside of tracepoint-provider.h.
#define _tpp_FOREACH(macro, ...) _tpp_FOR_imp(_tpp_NARGS(__VA_ARGS__), (macro, __VA_ARGS__))
#define _tpp_FOR_imp(n, macroAndArgs) _tpp_PASTE2(_tpp_FOR_imp, n) macroAndArgs
#define _tpp_FOR_imp0(f, ...)
#define _tpp_FOR_imp1(f, a0) f(0, a0)
#define _tpp_FOR_imp2(f, a0, a1) f(0, a0) f(1, a1)
#define _tpp_FOR_imp3(f, a0, a1, a2) f(0, a0) f(1, a1) f(2, a2)
#define _tpp_FOR_imp4(f, a0, a1, a2, a3) f(0, a0) f(1, a1) f(2, a2) f(3, a3)
#define _tpp_FOR_imp5(f, a0, a1, a2, a3, a4) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4)
#define _tpp_FOR_imp6(f, a0, a1, a2, a3, a4, a5) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5)
#define _tpp_FOR_imp7(f, a0, a1, a2, a3, a4, a5, a6) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6)
#define _tpp_FOR_imp8(f, a0, a1, a2, a3, a4, a5, a6, a7) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7)
#define _tpp_FOR_imp9(f, a0, a1, a2, a3, a4, a5, a6, a7, a8) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8)
#define _tpp_FOR_imp10(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9)
#define _tpp_FOR_imp11(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10)
#define _tpp_FOR_imp12(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11)
#define _tpp_FOR_imp13(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12)
#define _tpp_FOR_imp14(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13)
#define _tpp_FOR_imp15(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14)
#define _tpp_FOR_imp16(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15)
#define _tpp_FOR_imp17(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16)
#define _tpp_FOR_imp18(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17)
#define _tpp_FOR_imp19(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18)
#define _tpp_FOR_imp20(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19)
#define _tpp_FOR_imp21(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20)
#define _tpp_FOR_imp22(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21)
#define _tpp_FOR_imp23(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22)
#define _tpp_FOR_imp24(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23)
#define _tpp_FOR_imp25(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24)
#define _tpp_FOR_imp26(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25)
#define _tpp_FOR_imp27(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26)
#define _tpp_FOR_imp28(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27)
#define _tpp_FOR_imp29(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28)
#define _tpp_FOR_imp30(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29)
#define _tpp_FOR_imp31(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30)
#define _tpp_FOR_imp32(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31)
#define _tpp_FOR_imp33(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32)
#define _tpp_FOR_imp34(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33)
#define _tpp_FOR_imp35(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34)
#define _tpp_FOR_imp36(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35)
#define _tpp_FOR_imp37(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36)
#define _tpp_FOR_imp38(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37)
#define _tpp_FOR_imp39(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38)
#define _tpp_FOR_imp40(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39)
#define _tpp_FOR_imp41(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40)
#define _tpp_FOR_imp42(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41)
#define _tpp_FOR_imp43(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42)
#define _tpp_FOR_imp44(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43)
#define _tpp_FOR_imp45(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44)
#define _tpp_FOR_imp46(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45)
#define _tpp_FOR_imp47(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46)
#define _tpp_FOR_imp48(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47)
#define _tpp_FOR_imp49(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48)
#define _tpp_FOR_imp50(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49)
#define _tpp_FOR_imp51(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50)
#define _tpp_FOR_imp52(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51)
#define _tpp_FOR_imp53(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52)
#define _tpp_FOR_imp54(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53)
#define _tpp_FOR_imp55(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54)
#define _tpp_FOR_imp56(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55)
#define _tpp_FOR_imp57(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56)
#define _tpp_FOR_imp58(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57)
#define _tpp_FOR_imp59(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58)
#define _tpp_FOR_imp60(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59)
#define _tpp_FOR_imp61(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60)
#define _tpp_FOR_imp62(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61)
#define _tpp_FOR_imp63(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62)
#define _tpp_FOR_imp64(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63)
#define _tpp_FOR_imp65(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64)
#define _tpp_FOR_imp66(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65)
#define _tpp_FOR_imp67(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66)
#define _tpp_FOR_imp68(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67)
#define _tpp_FOR_imp69(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68)
#define _tpp_FOR_imp70(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69)
#define _tpp_FOR_imp71(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70)
#define _tpp_FOR_imp72(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71)
#define _tpp_FOR_imp73(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72)
#define _tpp_FOR_imp74(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73)
#define _tpp_FOR_imp75(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74)
#define _tpp_FOR_imp76(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75)
#define _tpp_FOR_imp77(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76)
#define _tpp_FOR_imp78(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77)
#define _tpp_FOR_imp79(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78)
#define _tpp_FOR_imp80(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79)
#define _tpp_FOR_imp81(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80)
#define _tpp_FOR_imp82(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81)
#define _tpp_FOR_imp83(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82)
#define _tpp_FOR_imp84(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83)
#define _tpp_FOR_imp85(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84)
#define _tpp_FOR_imp86(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85)
#define _tpp_FOR_imp87(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86)
#define _tpp_FOR_imp88(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87)
#define _tpp_FOR_imp89(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88)
#define _tpp_FOR_imp90(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89)
#define _tpp_FOR_imp91(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90)
#define _tpp_FOR_imp92(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91)
#define _tpp_FOR_imp93(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92)
#define _tpp_FOR_imp94(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93)
#define _tpp_FOR_imp95(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94)
#define _tpp_FOR_imp96(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95)
#define _tpp_FOR_imp97(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96)
#define _tpp_FOR_imp98(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96, a97) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96) f(97, a97)
#define _tpp_FOR_imp99(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96, a97, a98) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96) f(97, a97) f(98, a98)

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_functions (for internal use only)
#endif

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    static inline uint32_t
    _tppCreate1Vec(struct iovec* pVec, void const* pb, size_t cb) _tpp_NOEXCEPT _tpp_INLINE_ATTRIBUTES;
    static inline uint32_t
    _tppCreate1Vec(struct iovec* pVec, void const* pb, size_t cb) _tpp_NOEXCEPT
    {
        pVec[0].iov_base = (void*)pb;
        pVec[0].iov_len = cb;
        return (uint32_t)cb;
    }

    static inline uint32_t
    _tppCreate1RelLoc(struct iovec* pVec, void const* pb, uint16_t cb, uint32_t off, uint32_t* rel) _tpp_NOEXCEPT _tpp_INLINE_ATTRIBUTES;
    static inline uint32_t
    _tppCreate1RelLoc(struct iovec* pVec, void const* pb, uint16_t cb, uint32_t off, uint32_t* rel) _tpp_NOEXCEPT
    {
        // Before: *rel has the offset of rel_loc.
        // After: *rel has (cch<<16) | (offset from rel_loc to string).
        *rel = ((uint32_t)cb << 16) | ((off - *rel) & 0xFFFF);
        return _tppCreate1Vec(pVec, pb, cb);
    }

    static inline uint32_t
    _tppCreate1Sz_char(struct iovec* pVec, char const* sz, uint32_t off, uint32_t* rel) _tpp_NOEXCEPT _tpp_INLINE_ATTRIBUTES;
    static inline uint32_t
    _tppCreate1Sz_char(struct iovec* pVec, char const* sz, uint32_t off, uint32_t* rel) _tpp_NOEXCEPT
    {
        typedef char _tppCHAR;

        _tppCHAR const* pch;
        size_t cch;
        if (!sz)
        {
            pch = "";
            cch = sizeof("");
        }
        else
        {
            pch = sz;
            for (cch = 0; pch[cch] != 0; cch += 1) {}
            cch += 1; // nul-termination
            if (cch > (65535 / sizeof(_tppCHAR)))
            {
                pch = "...";
                cch = sizeof("...");
            }
        }

        uint16_t cb = (uint16_t)(cch * sizeof(_tppCHAR));
        return _tppCreate1RelLoc(pVec, pch, cb, off, rel);
    }

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus

// ********** NO FUNCTION DEFINITIONS BELOW THIS POINT ***********************

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_implementation macros (for internal use only)
#endif

/*
_tppApplyArgs and _tppApplyArgsN: Macro dispatchers.
_tppApplyArgs( macro,    (handler, ...)) --> macro##handler(...)
_tppApplyArgsN(macro, n, (handler, ...)) --> macro##handler(n, ...)
*/
#define _tppApplyArgs(macro, args)                  _tppApplyArgs_impA((macro, _tppApplyArgs_UNWRAP args))
#define _tppApplyArgs_impA(args)                    _tppApplyArgs_impB args
#define _tppApplyArgs_impB(macro, handler, ...)     _tppApplyArgs_CALL(macro, handler, (__VA_ARGS__))
#define _tppApplyArgs_UNWRAP(...)                   __VA_ARGS__
#define _tppApplyArgs_CALL(macro, handler, args)    macro##handler args
#define _tppApplyArgsN(macro, n, args)              _tppApplyArgsN_impA((macro, n, _tppApplyArgs_UNWRAP args))
#define _tppApplyArgsN_impA(args)                   _tppApplyArgsN_impB args
#define _tppApplyArgsN_impB(macro, n, handler, ...) _tppApplyArgs_CALL(macro, handler, (n, __VA_ARGS__))

// Field type-name strings.
#define _tppFieldString(n, args) _tppApplyArgs(_tppFieldString, args)
#define _tppFieldString_tppArgByVal( FieldDeclString, Ctype, Value)               " "           FieldDeclString ";"
#define _tppFieldString_tppArgByRef( FieldDeclString, Ctype, ConstSize, ValuePtr) " "           FieldDeclString ";"
#define _tppFieldString_tppArgRelLoc(FieldDeclString, Ctype, ValueSize, ValuePtr) " __rel_loc " FieldDeclString ";"
#define _tppFieldString_tppArgRelLocStr _tppFieldString_tppArgRelLoc

// Count the iovecs needed for event field data.
#define _tppDataDescCount(n, args) _tppApplyArgs(_tppDataDescCount, args)
#define _tppDataDescCount_tppArgByVal( FieldDeclString, Ctype, Value)               +1
#define _tppDataDescCount_tppArgByRef( FieldDeclString, Ctype, ConstSize, ValuePtr) +1
#define _tppDataDescCount_tppArgRelLoc(FieldDeclString, Ctype, ValueSize, ValuePtr) +2
#define _tppDataDescCount_tppArgRelLocStr _tppDataDescCount_tppArgRelLoc

// Temporary variables.
#define _tppDataDecls(n, args) _tppApplyArgsN(_tppDataDecls, n, args)
#define _tppDataDecls_tppArgByVal( N, FieldDeclString, Ctype, Value)               Ctype        _tppVal##N;
#define _tppDataDecls_tppArgByRef( N, FieldDeclString, Ctype, ConstSize, ValuePtr) Ctype const* _tppVal##N;
#define _tppDataDecls_tppArgRelLoc(N, FieldDeclString, Ctype, ValueSize, ValuePtr) Ctype const* _tppVal##N; uint32_t _tppRel##N;
#define _tppDataDecls_tppArgRelLocStr _tppDataDecls_tppArgRelLoc

// Fixed-offset iovecs.
// All values are evaluated in a single comma-separated expression.
// This enables support for things like TPP_STRING(ReturnStdString().c_str()).
#define _tppDataDescVal(n, args) _tppApplyArgsN(_tppDataDescVal, n, args)
#define _tppDataDescVal_tppArgByVal(N, FieldDeclString, Ctype, Value) \
    _tppVal##N = (Value), \
    _tppOff += _tppCreate1Vec(&_tppVecs[_tppIdx++], &_tppVal##N, sizeof(Ctype)),
#define _tppDataDescVal_tppArgByRef(N, FieldDeclString, Ctype, ConstSize, ValuePtr) \
    _tppVal##N = (ValuePtr), \
    _tppOff += _tppCreate1Vec(&_tppVecs[_tppIdx++], _tppVal##N, ConstSize),
#define _tppDataDescVal_tppArgRelLoc(N, FieldDeclString, Ctype, ValueSize, ValuePtr) \
    _tppVal##N = (ValuePtr), \
    _tppOff += _tppCreate1Vec(&_tppVecs[_tppIdx++], &_tppRel##N, sizeof(_tppRel##N)), \
    _tppRel##N = _tppOff,
#define _tppDataDescVal_tppArgRelLocStr _tppDataDescVal_tppArgRelLoc

// rel_loc iovecs.
#define _tppDataDescRel(n, args) _tppApplyArgsN(_tppDataDescRel, n, args)
#define _tppDataDescRel_tppArgByVal( N, FieldDeclString, Ctype, Value)
#define _tppDataDescRel_tppArgByRef( N, FieldDeclString, Ctype, ConstSize, ValuePtr)
#define _tppDataDescRel_tppArgRelLoc(N, FieldDeclString, Ctype, ValueSize, ValuePtr)    \
    _tppOff += _tppCreate1RelLoc( &_tppVecs[_tppIdx++], _tppVal##N, ValueSize,   _tppOff, &_tppRel##N),
#define _tppDataDescRel_tppArgRelLocStr(N, FieldDeclString, Ctype, ValueSize, ValuePtr) \
    _tppOff += _tppCreate1Sz_char(&_tppVecs[_tppIdx++], (char const*)_tppVal##N, _tppOff, &_tppRel##N),

// TPP_FUNCTION function parameters. Note that the comma is in _tppFuncArg, not the handler.
#define _tppFuncArg(n, args) ,_tppApplyArgs(_tppFuncArg, args)
#define _tppFuncArg_tppArgByVal( FieldDeclString, Ctype, Value)                  Ctype Value
#define _tppFuncArg_tppArgByRef( FieldDeclString, Ctype, ConstSize, ValuePtr)    Ctype const* ValuePtr
#define _tppFuncArg_tppArgRelLoc(FieldDeclString, Ctype, ValueSize, ValuePtr)    uint16_t ValueSize, Ctype const* ValuePtr
#define _tppFuncArg_tppArgRelLocStr(FieldDeclString, Ctype, ValueSize, ValuePtr) Ctype const* ValuePtr

// Function parameter list cases: func(void) or func(arg0 [, args...])
#define _tppFunctionArgs(IsEmpty, Args) _tpp_PASTE2(_tppFunctionArgs, IsEmpty) Args
#define _tppFunctionArgs1(...)          void
#define _tppFunctionArgs0(Arg0, ...)    _tppApplyArgs(_tppFuncArg, Arg0) _tpp_FOREACH(_tppFuncArg, __VA_ARGS__)

// Implement TPP_FUNCTION:
#define _tppFunctionImpl(ProviderSymbol, TracepointNameString, FunctionName, ...) \
    static tracepoint_state _tpp_PASTE2(_tppState_, FunctionName) = TRACEPOINT_STATE_INIT; \
    int _tpp_PASTE2(FunctionName, _enabled)(void) { \
        return TRACEPOINT_ENABLED(&_tpp_PASTE2(_tppState_, FunctionName)); \
    } \
    int FunctionName(_tppFunctionArgs(_tpp_IS_EMPTY(__VA_ARGS__), (__VA_ARGS__))) { \
        _tppCommonImpl(ProviderSymbol, TracepointNameString, _tpp_PASTE2(_tppState_, FunctionName), __VA_ARGS__) \
        return _tppWriteErr; \
    } \

// Implement TPP_WRITE:
#define _tppWriteImpl(ProviderSymbol, TracepointNameString, ...) ({ \
    static tracepoint_state _tppState = TRACEPOINT_STATE_INIT; \
    _tppCommonImpl(ProviderSymbol, TracepointNameString, _tppState, __VA_ARGS__) \
    _tppWriteErr; }) \

#define _tppCommonImpl(ProviderSymbol, TracepointNameString, TracepointState, ...) \
    static tracepoint_definition const _tppEvt = { \
        &TracepointState, \
        "" TracepointNameString _tpp_FOREACH(_tppFieldString, __VA_ARGS__) \
    }; \
    static tracepoint_definition const* _tppEvtPtr \
        __attribute__((section("_tppEventPtrs_" _tpp_STRINGIZE(ProviderSymbol)), used)) \
        = &_tppEvt; \
    int _tppWriteErr = 9 /*EBADF*/; \
    if (TRACEPOINT_ENABLED(&TracepointState)) { \
        struct iovec _tppVecs[1 _tpp_FOREACH(_tppDataDescCount, __VA_ARGS__)]; \
        _tppVecs[0].iov_len = 0; \
        unsigned _tppIdx = 1; /* first iovec is used by tracepoint_write */ \
        uint32_t _tppOff = 0; \
        (void)_tppOff; /* maybe unused (needed only if there is a rel_loc) */ \
        _tpp_FOREACH(_tppDataDecls, __VA_ARGS__) \
        ( /* Start of the eval-write expression. */ \
        _tpp_FOREACH(_tppDataDescVal, __VA_ARGS__) \
        _tpp_FOREACH(_tppDataDescRel, __VA_ARGS__) \
        _tppWriteErr = tracepoint_write(&TracepointState, _tppIdx, _tppVecs) \
        ) /* End of the eval-write expression. */; \
    } \

#ifdef __EDG__
#pragma endregion
#endif

#endif // _included_tracepoint_provider_h
