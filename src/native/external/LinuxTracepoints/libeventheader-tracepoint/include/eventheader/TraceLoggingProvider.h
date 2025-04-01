// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
This is an implementation of TraceLoggingProvider.h that writes data to
Linux tracepoints via the UserEvents system.

Prerequisites:

- If prerequisites are not met then register/write/unregister will be no-ops.
- Kernel must be built with tracefs and UserEvents (CONFIG_USER_EVENTS=y).
- tracefs mounted (e.g. /sys/kernel/tracing or /sys/kernel/debug/tracing).
- Caller must have appropriate permissions: x on tracefs mount point,
  rw on tracefs/user_events_data.

Quick start:

#include <eventheader/TraceLoggingProvider.h>

TRACELOGGING_DEFINE_PROVIDER( // defines the MyProvider symbol
    MyProvider, // Name of the provider symbol to define
    "MyCompany_MyComponent_MyProvider", // Human-readable provider name, no ' ' or ':' chars.
    // {d5b90669-1aad-5db8-16c9-6286a7fcfe33} // Provider guid (not used on Linux)
    (0xd5b90669,0x1aad,0x5db8,0x16,0xc9,0x62,0x86,0xa7,0xfc,0xfe,0x33));

int main(int argc, char* argv[])
{
    TraceLoggingRegister(MyProvider);
    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingString(argv[0], "arg0"), // field name is "arg0"
        TraceLoggingInt32(argc)); // field name is implicitly "argc"
    TraceLoggingUnregister(MyProvider);
    return 0;
}

Usage note:

Symbols starting with "TRACELOGGING" or "TraceLogging" are part of the public
interface of this header. Symbols starting with "_tlg" are private internal
implementation details that are not supported for use outside this header and
may be changed or removed in future versions of this header.

TraceLoggingProvider.h for Linux UserEvents behaves differently from the ETW
(Windows) version:

- TRACELOGGING_DEFINE_PROVIDER requires a provider name that is less than
  EVENTHEADER_NAME_MAX (256) characters in length and contains no ' '
  or ':' characters. (Decoding tools may impose additional restrictions; for
  best compatibility, use only [A-Za-z0-9_] chars.)
- TRACELOGGING_DEFINE_PROVIDER ignores the provider GUID.
- TraceLoggingHProvider is not provided. In the UserEvents implementation of
  TraceLoggingProvider.h, providers are referenced by symbol (token), not by
  handle (pointer). You cannot store a provider symbol in a variable, pass it
  as a parameter or return it from a function. The provider symbol used as the
  first parameter to TraceLoggingWrite must be the same symbol as was used in
  TRACELOGGING_DEFINE_PROVIDER.
- TRACELOGGING_DEFINE_PROVIDER_STORAGE is not provided.
- TraceLoggingOptionGroup is not provided. Instead, use
  TraceLoggingOptionGroupName.
- TraceLoggingRegisterEx is not provided. No support for notification
  callbacks.
- TraceLoggingProviderId is not provided. Instead, use
  TraceLoggingProviderName.
- TraceLoggingSetInformation is not provided.
- TraceLoggingWriteEx is not provided.
- TraceLoggingChannel is not provided.
- TraceLoggingEventTag supports 16-bit tag. (ETW supports a 28-bit tag.)
- TraceLoggingDescription is ignored.
- TraceLoggingCustomAttribute is not provided.
- TraceLoggingValue will not accept GUID, FILETIME, SYSTEMTIME, or SID values.
- TraceLoggingCodePointer is not provided.
- TraceLoggingFileTime, TraceLoggingFileTimeUtc, TraceLoggingSystemTime, and
  TraceLoggingSystemTimeUtc are not provided. Instead, use Linux-only
  TraceLoggingTime32 or TraceLoggingTime64.
- TraceLoggingTid is not provided. Instead, use TraceLoggingPid.
- TraceLoggingWinError, TraceLoggingNTStatus, TraceLoggingHResult are not
  provided. Instead, use Linux-only TraceLoggingErrno.
- TraceLoggingAnsiString and TraceLoggingUnicodeString are not provided. On
  Windows, these are used for Windows-specific ANSI_STRING and UNICODE_STRING
  structures.
- TraceLoggingSid is not provided.
- TraceLoggingBinaryBuffer is not provided.
- TraceLoggingCustom is not provided.
- TraceLoggingPackedDataEx is not provided.
- TraceLoggingGuid expects a uint8_t[16] value in RFC 4122 (big-endian) byte
  order. In Windows, TraceLoggingGuid expects a GUID (struct) and the data is
  stored as little-endian.
- Field tag is a 16-bit value. (ETW supports a 28-bit tag.)

The following features are specific to Linux UserEvents (not present for ETW):

- Use TraceLoggingIdVersion to specify a stable id and version for an event.
- Use TraceLoggingProviderName instead of TraceLoggingProviderId.
- Use TraceLoggingOptionGroupName instead of TraceLoggingOptionGroup.
- Use TraceLoggingErrno for logging errno values.
- Use TraceLoggingTime32 and TraceLoggingTime64 for logging time_t values.
- Use TraceLoggingChar32 and TraceLoggingString32 for logging char32_t
  characters and strings.
*/

#pragma once
#ifndef _included_TraceLoggingProvider_h
#define _included_TraceLoggingProvider_h 1

#include "eventheader-tracepoint.h"
#include <assert.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>

#ifdef __EDG__
#pragma region Public_interface
#endif

/*
This structure is left undefined to ensure a compile error for any attempt to
copy or dereference the provider symbol. The provider symbol is a token, not a
variable or a handle.
*/
struct TraceLoggingProviderSymbol;

/*
Macro TRACELOGGING_DECLARE_PROVIDER(providerSymbol):
Invoke this macro to forward-declare a provider symbol.
TRACELOGGING_DECLARE_PROVIDER is typically used in a header.

An invocation of

    TRACELOGGING_DECLARE_PROVIDER(MyProvider);

can be thought of as expanding to something like this:

    extern "C" TraceLoggingProviderSymbol MyProvider;

A symbol declared by TRACELOGGING_DECLARE_PROVIDER must later be defined in a
.c or .cpp file using the TRACELOGGING_DEFINE_PROVIDER macro.
*/
#define TRACELOGGING_DECLARE_PROVIDER(providerSymbol) \
    _tlg_EXTERN_C eventheader_tracepoint const* _tlg_PASTE2(__start__tlgEventPtrs_, providerSymbol)[] __attribute__((weak, visibility("hidden"))); \
    _tlg_EXTERN_C eventheader_tracepoint const* _tlg_PASTE2(__stop__tlgEventPtrs_, providerSymbol)[] __attribute__((weak, visibility("hidden"))); \
    _tlg_EXTERN_C struct TraceLoggingProviderSymbol providerSymbol __attribute__((visibility("hidden"))); /* Empty provider variable to help with code navigation. */ \
    _tlg_EXTERN_C eventheader_provider const _tlg_PASTE2(_tlgProv_, providerSymbol) __attribute__((visibility("hidden")))  /* Actual provider variable is hidden behind prefix. */

/*
Macro TRACELOGGING_DEFINE_PROVIDER(providerSymbol, "ProviderName", (providerId), [option]):
Invoke this macro to create the global storage for a provider.

An invocation of

    TRACELOGGING_DEFINE_PROVIDER(MyProvider, "MyProviderName",
        (0xb3864c38, 0x4273, 0x58c5, 0x54, 0x5b, 0x8b, 0x36, 0x08, 0x34, 0x34, 0x71));

can be thought of as expanding to something like this:

    extern "C" TraceLoggingProviderSymbol MyProvider = { ... };

The "ProviderName" specifies a unique name that identifies the provider in the
logged events. It must be a char string literal (not a variable), must be less
than EVENTHEADER_NAME_MAX (256) characters long, and may not contain
' ' or ':' characters. Some versions of libtracefs impose additional
restrictions; for best compatibility, use only [A-Za-z0-9_] characters.

The providerId specifies a unique GUID that identifies the provider. The
providerId parameter must be a parenthesized list of 11 integers e.g.
(n1, n2, n3, ... n11). Typically the GUID is generated as a hash of the name.

Established convention for GUID generation, expressed as a Python function:

    def providerid_from_name(providername : str) -> uuid.UUID:
        sha1 = hashlib.sha1(usedforsecurity = False)
        sha1.update(b'\x48\x2C\x2D\xB2\xC3\x90\x47\xC8\x87\xF8\x1A\x15\xBF\xC1\x30\xFB')
        sha1.update(providername.upper().encode('utf_16_be'))
        arr = bytearray(sha1.digest()[0:16])
        arr[7] = (arr[7] & 0x0F) | 0x50
        return uuid.UUID(bytes_le = bytes(arr))

After the providerId GUID, you may optionally specify a
TraceLoggingOptionGroupName("...") macro to set the provider group name, e.g.

    TRACELOGGING_DEFINE_PROVIDER(MyProvider, "MyProviderName",
        (0xb3864c38, 0x4273, 0x58c5, 0x54, 0x5b, 0x8b, 0x36, 0x08, 0x34, 0x34, 0x71),
        TraceLoggingOptionGroupName("mygroupname"));

Note that the provider symbol is created in the "unregistered" state. A call
to TraceLoggingWrite with an unregistered provider is a no-op. Call
TraceLoggingRegister to register the provider.
*/
#define TRACELOGGING_DEFINE_PROVIDER(providerSymbol, providerName, providerId, ...) \
    TRACELOGGING_DECLARE_PROVIDER(providerSymbol); \
    static_assert( \
        EVENTHEADER_NAME_MAX >= sizeof("" providerName "_LnnKnnnnnnnnnnnnnnnn" _tlgProviderOptions(__VA_ARGS__)), \
        "TRACELOGGING_DEFINE_PROVIDER providerName + options is too long"); \
    _tlgParseProviderId(providerId) \
    static tracepoint_provider_state _tlg_PASTE2(_tlgProvState_, providerSymbol) = TRACEPOINT_PROVIDER_STATE_INIT; \
    _tlg_EXTERN_C_CONST eventheader_provider _tlg_PASTE2(_tlgProv_, providerSymbol) = { \
        &_tlg_PASTE2(_tlgProvState_, providerSymbol), \
        _tlgProviderOptions(__VA_ARGS__), \
        "" providerName \
    }

/*
Macro TraceLoggingOptionGroupName("groupname"):
Wrapper macro for use in TRACELOGGING_DEFINE_PROVIDER that declares the
provider's membership in a provider group.

The "groupname" specifies a string that can be used to identify a group of
related providers. This must be a char string literal containing only ASCII
digits and lowercase letters, [a-z0-9]. The total
strlen(ProviderName + groupname) must be less than
EVENTHEADER_NAME_MAX (256).
*/
#define TraceLoggingOptionGroupName(groupName) \
    TraceLoggingOptionGroupName(groupName)

/*
Macro TraceLoggingUnregister(providerSymbol):
Call this function to unregister your provider. Normally you will register at
component initialization (program startup or shared object load) and unregister
at component shutdown (program exit or shared object unload).

Thread safety: It is NOT safe to call TraceLoggingUnregister while a
TraceLoggingRegister, TraceLoggingUnregister, TraceLoggingWrite, or
TraceLoggingProviderEnabled for the same provider could be in progress on
another thread.

It is ok to call TraceLoggingUnregister on a provider that has not been
registered (e.g. if the call to TraceLoggingRegister failed). Unregistering an
unregistered provider is a safe no-op.

After unregistering a provider, it is ok to register it again. In other words,
the following sequence is ok:

    TraceLoggingRegister(MyProvider);
    ...
    TraceLoggingUnregister(MyProvider);
    ...
    TraceLoggingRegister(MyProvider);
    ...
    TraceLoggingUnregister(MyProvider);

Re-registering a provider should only happen because a component has been
uninitialized and then reinitialized. You should not register and unregister a
provider each time you need to write a few events.

Note that unregistration is important, especially in the case of a shared
object that might be dynamically unloaded before the process ends. Failure to
unregister may cause process memory corruption as the kernel tries to update
the enabled/disabled states of tracepoint variables that no longer exist.
*/
#define TraceLoggingUnregister(providerSymbol) \
    (eventheader_close_provider( \
        &_tlg_PASTE2(_tlgProv_, providerSymbol) ))

/*
Macro TraceLoggingRegister(providerSymbol):
Call this function to register your provider. Normally you will register at
component initialization (program startup or shared object load) and unregister
at component shutdown (program exit or shared object unload).

Returns 0 for success, errno otherwise. Result is primarily for
debugging/diagnostics and is usually ignored for production code. If
registration fails, subsequent TraceLoggingWrite and TraceLoggingUnregister
will be safe no-ops.

Thread safety: It is NOT safe to call TraceLoggingRegister while a
TraceLoggingRegister or TraceLoggingUnregister for the same provider might be
in progress on another thread.

The provider must be in the "unregistered" state. It is an error to call
TraceLoggingRegister on a provider that is already registered.
*/
#define TraceLoggingRegister(providerSymbol) \
    (eventheader_open_provider_with_events( \
        &_tlg_PASTE2(_tlgProv_, providerSymbol), \
        _tlg_PASTE2(__start__tlgEventPtrs_, providerSymbol), \
        _tlg_PASTE2(__stop__tlgEventPtrs_, providerSymbol) ))

/*
Macro TraceLoggingProviderEnabled(providerSymbol, eventLevel, eventKeyword):
Returns true (non-zero) if a TraceLoggingWrite using the specified
providerSymbol, eventLevel, and eventKeyword would be enabled, false if it
would be disabled.

Example:

    if (TraceLoggingProviderEnabled(MyProvider, event_level_warning, 0x1f))
    {
        // Prepare complex data needed for event.
        int myIntVar;
        wchar_t const* myString;

        ExpensiveGetIntVar(&myIntVar);
        ExpensiveGetString(&myString);

        TraceLoggingWrite(MyProvider, "MyEventName",
            TraceLoggingLevel(event_level_warning),
            TraceLoggingKeyword(0x1f),
            TraceLoggingInt32(myIntVar),
            TraceLoggingWideString(myString));

        CleanupString(myString);
    }

Note that the TraceLoggingWrite macro already checks whether the tracepoint is
enabled -- it skips evaluating the field value expressions and skips sending
the event if the tracepoint is not enabled. You only need to make your own
call to TraceLoggingProviderEnabled if you want to control something other
than TraceLoggingWrite.

Implementation details: This macro registers an inert tracepoint with the
specified provider, level, and keyword, and returns true if that tracepoint is
enabled.
*/
#define TraceLoggingProviderEnabled(providerSymbol, eventLevel, eventKeyword)  ({ \
    enum { \
        _tlgKeywordVal = (uint64_t)(eventKeyword), \
        _tlgLevelVal = (uint64_t)(eventLevel) \
    }; \
    static tracepoint_state _tlgEvtState = TRACEPOINT_STATE_INIT; \
    static eventheader_tracepoint const _tlgEvt = { \
        &_tlgEvtState, \
        (eventheader_extension*)0, \
        { \
            eventheader_flag_default, \
            0, \
            0, \
            0, \
            0, \
            _tlgLevelVal \
        }, \
        _tlgKeywordVal \
    }; \
    static eventheader_tracepoint const* const _tlgEvtPtr \
        __attribute__((section("_tlgEventPtrs_" _tlg_STRINGIZE(providerSymbol)), used)) \
        = &_tlgEvt; \
    TRACEPOINT_ENABLED(&_tlgEvtState); })

/*
Macro TraceLoggingProviderName(providerSymbol):
Returns the provider's name as a nul-terminated const char*.
*/
#define TraceLoggingProviderName(providerSymbol) \
    (&_tlg_PASTE2(_tlgProv_, providerSymbol).name[0])

/*
Macro TraceLoggingProviderOptions(providerSymbol):
Returns the provider's options as a nul-terminated const char*.
*/
#define TraceLoggingProviderOptions(providerSymbol) \
    (&_tlg_PASTE2(_tlgProv_, providerSymbol).options[0])

/*
Macro TraceLoggingWrite(providerSymbol, "EventName", args...):
Invoke this macro to log an event.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingInt32(myIntVar),
        TraceLoggingWideString(myString));

The eventName parameter must be a char string literal (not a variable) and must
not contain any ';' or '\0' characters. The name will be treated as utf-8.

Supports up to 99 args (subject to compiler limitations). Each arg must be a
wrapper macro such as TraceLoggingLevel, TraceLoggingKeyword, TraceLoggingInt32,
TraceLoggingString, etc.
*/
#define TraceLoggingWrite(providerSymbol, eventName, ...) \
    _tlgWriteImp(providerSymbol, eventName, _tlg_NULL, _tlg_NULL, ##__VA_ARGS__)

/*
Macro TraceLoggingWriteActivity(providerSymbol, "EventName", pActivityId, pRelatedActivityId, args...):
Invoke this macro to log an event with ActivityId and optional RelatedActivityId data.

Example:

    TraceLoggingWriteActivity(MyProvider, "MyEventName",
        pActivityGuid, // 128-bit ID, i.e. uint8_t[16].
        pRelatedActivityGuid, // 128-bit ID, or NULL. Usually NULL (non-NULL only when used with opcode START).
        TraceLoggingOpcode(WINEVENT_OPCODE_START),
        TraceLoggingInt32(myIntVar),
        TraceLoggingWideString(myString));

The event name must be a char string literal (not a variable) and must not
contain any ';' or '\0' characters. The name will be treated as utf-8.

Supports up to 99 args (subject to compiler limitations). Each arg must be a
wrapper macro such as TraceLoggingLevel, TraceLoggingKeyword, TraceLoggingInt32,
TraceLoggingString, etc.
*/
#define TraceLoggingWriteActivity(providerSymbol, eventName, pActivityId, pRelatedActivityId, ...) \
    _tlgWriteImp(providerSymbol, eventName, pActivityId, pRelatedActivityId, ##__VA_ARGS__)

/*
Macro TraceLoggingLevel(eventLevel)
Wrapper macro for setting the event's level.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingLevel(event_level_warning),
        TraceLoggingWideString(myString));

The eventLevel parameter must be a compile-time constant 1 to 255, typically
an event_level_??? constant from eventheader.h. If no TraceLoggingLevel(n) arg
is set on an event, the event will default to level 5 (Verbose). If multiple
TraceLoggingLevel(n) args are provided, the level from the last
TraceLoggingLevel(n) will be used.
*/
#define TraceLoggingLevel(eventLevel) _tlgArgLevel(eventLevel)

/*
Macro TraceLoggingKeyword(eventKeyword):
Wrapper macro for setting the event's keyword(s).

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingKeyword(MyNetworkingKeyword),
        TraceLoggingWideString(myString));

The eventKeyword parameter must be a compile-time constant 0 to UINT64_MAX.
Each bit in the parameter corresponds to a user-defined event category. If an
event belongs to multiple categories, the bits for each category should be
OR'd together to create the event's keyword value. If no
TraceLoggingKeyword(n) arg is provided, the default keyword is 0. If multiple
TraceLoggingKeyword(n) args are provided, they are OR'd together.
*/
#define TraceLoggingKeyword(eventKeyword) _tlgArgKeyword(eventKeyword)

/*
Macro TraceLoggingIdVersion(eventId, eventVersion):
Wrapper macro for setting the stable id and/or version for an event.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingIdVersion(123, 0),
        TraceLoggingWideString(myString));

By default, TraceLogging events have event id = 0 and version = 0, indicating
that they have not been assigned a stable numeric event id. The events are
identified by ProviderName+EventName which is usually sufficient.

In some cases, it is useful to manually assign a stable numeric event id to an
event. This can help with event routing and filtering. Use
TraceLoggingIdVersion to specify the id and version of an event.

- The id should be a manually-assigned value from 1 to 65535.
- The version must be a value from 0 to 255. It should start at 0 and should
  be incremented each time the event is changed (e.g. when a field is added,
  removed, renamed, or the field type is changed, or if event semantics change
  in some other way).

If multiple TraceLoggingIdVersion args are provided, the values from the last
TraceLoggingIdVersion are used.
*/
#define TraceLoggingIdVersion(eventId, eventVersion) _tlgArgIdVersion(eventId, eventVersion)

/*
Macro TraceLoggingOpcode(eventOpcode):
Wrapper macro for setting the event's opcode.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingOpcode(event_opcode_activity_start),
        TraceLoggingWideString(myString));

The eventOpcode parameter must be a compile-time constant 0 to 255 (typically
an event_opcode_??? constant from eventheader.h). If multiple
TraceLoggingOpcode(n) args are provided, the value from the last
TraceLoggingOpcode(n) is used.
*/
#define TraceLoggingOpcode(eventOpcode) _tlgArgOpcode(eventOpcode)

/*
Macro TraceLoggingEventTag(eventTag):
Wrapper macro for setting the event's tag.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingEventTag(0x200),
        TraceLoggingWideString(myString));

Tag is a 16-bit integer. The semantics of the tag are defined by the event
provider.
*/
#define TraceLoggingEventTag(eventTag) _tlgArgEventTag(eventTag)

/*
Macro TraceLoggingDescription(description):
Wrapper macro for setting a description for an event.

UserEvents semantics: TraceLoggingDescription has no effect and functions as a
comment.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingDescription("My event's detailed description"),
        TraceLoggingWideString(myString));
*/
#define TraceLoggingDescription(description) _tlgArgIgnored()

/*
Macro TraceLoggingStruct(fieldCount, "structName", "description", tag):
Wrapper macro for defining a group of related fields in an event.

The description and tag parameters are optional.

The fieldCount parameter must be a compile-time constant 1 to 127. It indicates
the number of fields that will be considered to be part of the struct. A struct
and all of its contained fields count as a single field in any parent structs.

The name parameter must be a char string literal (not a variable) and must not
contain any ';' or '\0' characters.

If provided, the description parameter must be a char string literal.

If provided, the tag parameter must be a 16-bit integer value.

Example:

    TraceLoggingWrite(MyProvider, "MyEventName",
        TraceLoggingStruct(2, "PersonName"),
            TraceLoggingWideString(Last),
            TraceLoggingWideString(First));
*/
#define TraceLoggingStruct(fieldCount, name, ...) \
    _tlgArgStruct(fieldCount, event_field_encoding_struct, _tlgNdt(TraceLoggingStruct, value, name, ##__VA_ARGS__))

#ifdef __cplusplus
/*
Macro TraceLoggingValue(value, "name", "description", tag):
Wrapper macro for event fields. Automatically deduces value type. C++ only.

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Examples:
- TraceLoggingValue(val1)                      // field name = "val1", description = unset,  tag = 0.
- TraceLoggingValue(val1, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingValue(val1, "name", "desc"       // field name = "name", description = "desc", tag = 0.
- TraceLoggingValue(val1, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.

Based on the type of val, TraceLoggingValue(val, ...) is equivalent to one of
the following:
- bool            --> TraceLoggingBoolean(val, ...)
- char            --> TraceLoggingChar(val, ...)
- char16_t        --> TraceLoggingChar16(val, ...)
- char32_t        --> TraceLoggingChar32(val, ...)
- wchar_t         --> TraceLoggingWChar(val, ...)
- intNN_t         --> TraceLoggingIntNN(val, ...)
- uintNN_t        --> TraceLoggingUIntNN(val, ...)
- float           --> TraceLoggingFloat32(val, ...)
- double          --> TraceLoggingFloat64(val, ...)
- const void*     --> TraceLoggingPointer(val, ...)    // Logs the pointer's value, not the data at which it points.
- const char*     --> TraceLoggingString(val, ...)     // Assumes nul-terminated latin1 string. NULL is the same as "".
- const char16_t* --> TraceLoggingString16(val, ...)   // Assumes nul-terminated utf-16 string. NULL is the same as u"".
- const char32_t* --> TraceLoggingString32(val, ...)   // Assumes nul-terminated utf-32 string. NULL is the same as U"".
- const wchar_t*  --> TraceLoggingWideString(val, ...) // Assumes nul-terminated utf-16/32 string (based on size of wchar_t). NULL is the same as L"".
*/
#define TraceLoggingValue(value, ...) _tlgArgAuto(value, _tlgNdt(TraceLoggingValue, value, ##__VA_ARGS__))
#endif // __cplusplus

/*
Wrapper macros for event fields with simple scalar values.
Usage: TraceLoggingInt32(value, "name", "description", tag).

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Notes:
- TraceLoggingBool is for 32-bit boolean values (e.g. int).
- TraceLoggingBoolean is for 8-bit boolean values (e.g. bool or char).

Examples:
- TraceLoggingInt32(val1)                      // field name = "val1", description = unset,  tag = 0.
- TraceLoggingInt32(val1, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingInt32(val1, "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingInt32(val1, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingInt8(value, ...)       _tlgArgValue(int8_t,      value,  event_field_encoding_value8,    (event_field_format_signed_int), _tlgNdt(TraceLoggingInt8, value, ##__VA_ARGS__))
#define TraceLoggingUInt8(value, ...)      _tlgArgValue(uint8_t,     value,  event_field_encoding_value8,    (),                              _tlgNdt(TraceLoggingUInt8, value, ##__VA_ARGS__))
#define TraceLoggingInt16(value, ...)      _tlgArgValue(int16_t,     value,  event_field_encoding_value16,   (event_field_format_signed_int), _tlgNdt(TraceLoggingInt16, value, ##__VA_ARGS__))
#define TraceLoggingUInt16(value, ...)     _tlgArgValue(uint16_t,    value,  event_field_encoding_value16,   (),                              _tlgNdt(TraceLoggingUInt16, value, ##__VA_ARGS__))
#define TraceLoggingInt32(value, ...)      _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_signed_int), _tlgNdt(TraceLoggingInt32, value, ##__VA_ARGS__))
#define TraceLoggingUInt32(value, ...)     _tlgArgValue(uint32_t,    value,  event_field_encoding_value32,   (),                              _tlgNdt(TraceLoggingUInt32, value, ##__VA_ARGS__))
#define TraceLoggingInt64(value, ...)      _tlgArgValue(int64_t,     value,  event_field_encoding_value64,   (event_field_format_signed_int), _tlgNdt(TraceLoggingInt64, value, ##__VA_ARGS__))
#define TraceLoggingUInt64(value, ...)     _tlgArgValue(uint64_t,    value,  event_field_encoding_value64,   (),                              _tlgNdt(TraceLoggingUInt64, value, ##__VA_ARGS__))
#define TraceLoggingIntPtr(value, ...)     _tlgArgValue(intptr_t,    value,  event_field_encoding_value_ptr, (event_field_format_signed_int), _tlgNdt(TraceLoggingIntPtr, value, ##__VA_ARGS__))
#define TraceLoggingUIntPtr(value, ...)    _tlgArgValue(uintptr_t,   value,  event_field_encoding_value_ptr, (),                              _tlgNdt(TraceLoggingUIntPtr, value, ##__VA_ARGS__))
#define TraceLoggingLong(value, ...)       _tlgArgValue(signed long, value,  event_field_encoding_value_long,(event_field_format_signed_int), _tlgNdt(TraceLoggingLong, value, ##__VA_ARGS__))
#define TraceLoggingULong(value, ...)      _tlgArgValue(unsigned long,value, event_field_encoding_value_long,(),                              _tlgNdt(TraceLoggingULong, value, ##__VA_ARGS__))
#define TraceLoggingHexInt8(value, ...)    _tlgArgValue(int8_t,      value,  event_field_encoding_value8,    (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt8, value, ##__VA_ARGS__))
#define TraceLoggingHexUInt8(value, ...)   _tlgArgValue(uint8_t,     value,  event_field_encoding_value8,    (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt8, value, ##__VA_ARGS__))
#define TraceLoggingHexInt16(value, ...)   _tlgArgValue(int16_t,     value,  event_field_encoding_value16,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt16, value, ##__VA_ARGS__))
#define TraceLoggingHexUInt16(value, ...)  _tlgArgValue(uint16_t,    value,  event_field_encoding_value16,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt16, value, ##__VA_ARGS__))
#define TraceLoggingHexInt32(value, ...)   _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt32, value, ##__VA_ARGS__))
#define TraceLoggingHexUInt32(value, ...)  _tlgArgValue(uint32_t,    value,  event_field_encoding_value32,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt32, value, ##__VA_ARGS__))
#define TraceLoggingHexInt64(value, ...)   _tlgArgValue(int64_t,     value,  event_field_encoding_value64,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt64, value, ##__VA_ARGS__))
#define TraceLoggingHexUInt64(value, ...)  _tlgArgValue(uint64_t,    value,  event_field_encoding_value64,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt64, value, ##__VA_ARGS__))
#define TraceLoggingHexIntPtr(value, ...)  _tlgArgValue(intptr_t,    value,  event_field_encoding_value_ptr, (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexIntPtr, value, ##__VA_ARGS__))
#define TraceLoggingHexUIntPtr(value, ...) _tlgArgValue(uintptr_t,   value,  event_field_encoding_value_ptr, (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUIntPtr, value, ##__VA_ARGS__))
#define TraceLoggingHexLong(value, ...)    _tlgArgValue(signed long, value,  event_field_encoding_value_long,(event_field_format_hex_int),    _tlgNdt(TraceLoggingHexLong, value, ##__VA_ARGS__))
#define TraceLoggingHexULong(value, ...)   _tlgArgValue(unsigned long,value, event_field_encoding_value_long,(event_field_format_hex_int),    _tlgNdt(TraceLoggingHexULong, value, ##__VA_ARGS__))
#define TraceLoggingFloat32(value, ...)    _tlgArgValue(float,       value,  event_field_encoding_value_float,(event_field_format_float),     _tlgNdt(TraceLoggingFloat32, value, ##__VA_ARGS__))
#define TraceLoggingFloat64(value, ...)    _tlgArgValue(double,      value,  event_field_encoding_value_double,(event_field_format_float),    _tlgNdt(TraceLoggingFloat64, value, ##__VA_ARGS__))
#define TraceLoggingBoolean(value, ...)    _tlgArgValue(uint8_t,     value,  event_field_encoding_value8,    (event_field_format_boolean),    _tlgNdt(TraceLoggingBoolean, value, ##__VA_ARGS__))
#define TraceLoggingBool(value, ...)       _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_boolean),    _tlgNdt(TraceLoggingBool, value, ##__VA_ARGS__))
#define TraceLoggingChar(value, ...)       _tlgArgValue(char,        value,  event_field_encoding_value8,    (event_field_format_string8),    _tlgNdt(TraceLoggingChar, value, ##__VA_ARGS__))
#define TraceLoggingChar16(value, ...)     _tlgArgValue(char16_t,    value,  event_field_encoding_value16,   (event_field_format_string_utf), _tlgNdt(TraceLoggingChar16, value, ##__VA_ARGS__))
#define TraceLoggingChar32(value, ...)     _tlgArgValue(char32_t,    value,  event_field_encoding_value32,   (event_field_format_string_utf), _tlgNdt(TraceLoggingChar32, value, ##__VA_ARGS__))
#define TraceLoggingWChar(value, ...)      _tlgArgValue(wchar_t,     value,  event_field_encoding_value_wchar,(event_field_format_string_utf),_tlgNdt(TraceLoggingWChar, value, ##__VA_ARGS__))
#define TraceLoggingPointer(value, ...)    _tlgArgValue(void const*, value,  event_field_encoding_value_ptr, (event_field_format_hex_int),    _tlgNdt(TraceLoggingPointer, value, ##__VA_ARGS__))
#define TraceLoggingPid(value, ...)        _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_pid),        _tlgNdt(TraceLoggingPid, value, ##__VA_ARGS__))
#define TraceLoggingPort(value, ...)       _tlgArgValue(uint16_t,    value,  event_field_encoding_value16,   (event_field_format_port),       _tlgNdt(TraceLoggingPort, value, ##__VA_ARGS__))
#define TraceLoggingErrno(value, ...)      _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_errno),      _tlgNdt(TraceLoggingErrno, value, ##__VA_ARGS__))
#define TraceLoggingTime32(value, ...)     _tlgArgValue(int32_t,     value,  event_field_encoding_value32,   (event_field_format_time),       _tlgNdt(TraceLoggingTime32, value, ##__VA_ARGS__))
#define TraceLoggingTime64(value, ...)     _tlgArgValue(int64_t,     value,  event_field_encoding_value64,   (event_field_format_time),       _tlgNdt(TraceLoggingTime64, value, ##__VA_ARGS__))

/*
Wrapper macros for GUID/UUID values in big-endian (RFC 4122) byte order.
Usage: TraceLoggingGuid(pValue, "name", "description", tag).

The pValue is expected to be a const uint8_t[16] in big-endian byte order to
match the definition of uuid_t in libuuid.

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Examples:
- TraceLoggingGuid(val1)                      // field name = "val1", description = unset,  tag = 0.
- TraceLoggingGuid(val1, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingGuid(val1, "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingGuid(val1, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingGuid(pValue, ...)   _tlgArgPackedField(uint8_t, pValue, 16, event_field_encoding_value128, (event_field_format_uuid), _tlgNdt(TraceLoggingGuid, pValue, ##__VA_ARGS__))

/*
Wrapper macros for event fields with string values.
Usage: TraceLoggingString(pszVal, "name", "description", tag), where pszVal is const char*.
Usage: TraceLoggingUtf8String(pszVal, "name", "description", tag), where pszVal is const char*.
Usage: TraceLoggingString16(pszVal, "name", "description", tag), where pszVal is const char16_t*.
Usage: TraceLoggingString32(pszVal, "name", "description", tag), where pszVal is const char32_t*.
Usage: TraceLoggingWideString(pszVal, "name", "description", tag), where pszVal is const wchar_t*.
Usage: TraceLoggingCountedString(pchVal, cchVal, "name", "description", tag), where pchVal is const char*.
Usage: TraceLoggingCountedUtf8String(pchVal, cbVal, "name", "description", tag), where pchVal is const char*.
Usage: TraceLoggingCountedString16(pchVal, cchVal, "name", "description", tag), where pchVal is const char16_t*.
Usage: TraceLoggingCountedString32(pchVal, cchVal, "name", "description", tag), where pchVal is const char32_t*.
Usage: TraceLoggingCountedWideString(pchVal, cchVal, "name", "description", tag), where pchVal is const wchar_t*.

The name, description, and tag parameters are optional.

For TraceLoggingString, TraceLoggingUtf8String, TraceLoggingString16,
TraceLoggingString32, and TraceLoggingWideString, the pszValue parameter is
treated as a nul-terminated string. If pszValue is NULL, it is treated as an
empty (zero-length) string.

For TraceLoggingCountedString, TraceLoggingCountedUtf8String,
TraceLoggingCountedString16, TraceLoggingCountedString32, and
TraceLoggingCountedWideString, the pchValue parameter is treated as a counted
string, with cchValue specifying an array element count (0 to 65535).
The pchValue parameter may be NULL only if cchValue is 0.

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Notes:
- TraceLoggingString and TraceLoggingCountedString use unspecified charset but
  are usually treated as latin1 (ISO-8859-1) or CP-1252 text.
- The other macros expect UTF-8, UTF-16 or UTF-32 data.

Examples:
- TraceLoggingString(psz1)                      // field name = "psz1", description = unset,  tag = 0.
- TraceLoggingString(psz1, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingString(psz1, "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingString(psz1, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingString(pszValue, ...)                      _tlgArgStrNul(char,     pszValue,           event_field_encoding_zstring_char8,         (event_field_format_string8),_tlgNdt(TraceLoggingString, pszValue, ##__VA_ARGS__))
#define TraceLoggingUtf8String(pszValue, ...)                  _tlgArgStrNul(char,     pszValue,           event_field_encoding_zstring_char8,         (),                          _tlgNdt(TraceLoggingUtf8String, pszValue, ##__VA_ARGS__))
#define TraceLoggingWideString(pszValue, ...)                  _tlgArgStrNul(wchar_t,  pszValue,           event_field_encoding_zstring_wchar,         (),                          _tlgNdt(TraceLoggingWideString, pszValue, ##__VA_ARGS__))
#define TraceLoggingString16(pszValue, ...)                    _tlgArgStrNul(char16_t, pszValue,           event_field_encoding_zstring_char16,        (),                          _tlgNdt(TraceLoggingString16, pszValue, ##__VA_ARGS__))
#define TraceLoggingString32(pszValue, ...)                    _tlgArgStrNul(char32_t, pszValue,           event_field_encoding_zstring_char32,        (),                          _tlgNdt(TraceLoggingString32, pszValue, ##__VA_ARGS__))
#define TraceLoggingCountedString(pchValue, cchValue, ...)     _tlgArgStrCch(char,     pchValue, cchValue, event_field_encoding_string_length16_char8, (event_field_format_string8),_tlgNdt(TraceLoggingCountedString, pchValue, ##__VA_ARGS__))
#define TraceLoggingCountedUtf8String(pchValue, cbValue, ...)  _tlgArgStrCch(char,     pchValue, cbValue,  event_field_encoding_string_length16_char8, (),                          _tlgNdt(TraceLoggingCountedUtf8String, pchValue, ##__VA_ARGS__))
#define TraceLoggingCountedWideString(pchValue, cchValue, ...) _tlgArgStrCch(wchar_t,  pchValue, cchValue, event_field_encoding_string_length16_wchar, (),                          _tlgNdt(TraceLoggingCountedWideString, pchValue, ##__VA_ARGS__))
#define TraceLoggingCountedString16(pchValue, cchValue, ...)   _tlgArgStrCch(char16_t, pchValue, cchValue, event_field_encoding_string_length16_char16,(),                          _tlgNdt(TraceLoggingCountedString16, pchValue, ##__VA_ARGS__))
#define TraceLoggingCountedString32(pchValue, cchValue, ...)   _tlgArgStrCch(char32_t, pchValue, cchValue, event_field_encoding_string_length16_char32,(),                          _tlgNdt(TraceLoggingCountedString32, pchValue, ##__VA_ARGS__))

/*
Wrapper macro for raw binary data.
Usage: TraceLoggingBinary(pValue, cbValue, "name", "description", tag).
Usage: TraceLoggingBinaryEx(pValue, cbValue, format, "name", "description", tag).

Use TraceLoggingBinary for normal binary data (event_field_format_hex_bytes).
Use TraceLoggingBinaryEx to specify a custom format.

The pValue parameter is treated as a const void* so that any kind of data can
be provided. The cbValue parameter is the data size in bytes (0 to 65535).

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Examples:
- TraceLoggingBinary(pObj, sizeof(*pObj))                      // field name = "pObj", description = unset,  tag = 0.
- TraceLoggingBinary(pObj, sizeof(*pObj), "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingBinary(pObj, sizeof(*pObj), "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingBinary(pObj, sizeof(*pObj), "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingBinary(pValue, cbValue, ...)           _tlgArgBin(void, pValue, cbValue, event_field_encoding_string_length16_char8, (event_field_format_hex_bytes), _tlgNdt(TraceLoggingBinary, pValue, ##__VA_ARGS__))
#define TraceLoggingBinaryEx(pValue, cbValue, format, ...) _tlgArgBin(void, pValue, cbValue, event_field_encoding_string_length16_char8, (format),                     _tlgNdt(TraceLoggingBinaryEx, pValue, ##__VA_ARGS__))

/*
Wrapper macro for event fields with IPv4 address values.
Usage: TraceLoggingIPv4Address(value, "name", "description", tag).

The value parameter must be a UINT32-encoded IPv4 address in
network byte order (e.g. pSock->sin_addr.s_addr).

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Example:
- TraceLoggingIPv4Address(pSockAddr->sin_addr.s_addr, "name").
*/
#define TraceLoggingIPv4Address(value, ...) _tlgArgValue(uint32_t, value, event_field_encoding_value32, (event_field_format_ipv4), _tlgNdt(TraceLoggingIPv4Address, value, ##__VA_ARGS__))

/*
Wrapper macro for event fields with IPv6 address values.
Usage: TraceLoggingIPv6Address(pValue, "name", "description", tag).

The pValue parameter must not be NULL and must point at a 16-byte buffer
(e.g. use &pSock->sin6_addr).

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Example:
- TraceLoggingIPv6Address(&pSockAddr->sin6_addr, "name").
*/
#define TraceLoggingIPv6Address(pValue, ...) _tlgArgPackedField(void, pValue, 16, event_field_encoding_value128, (event_field_format_ipv6), _tlgNdt(TraceLoggingIPv6Address, pValue, ##__VA_ARGS__))

/*
Wrapper macros for event fields with values that are fixed-length arrays.
Usage: TraceLoggingInt32FixedArray(pVals, cVals, "name", "description", tag).

The pVals parameter must be a pointer to cVals items of the specified type.

The cVals parameter must be a compile-time constant element count 1..65535.

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Examples:
- TraceLoggingUInt8FixedArray(pbX1, 32)                      // field name = "pbX1", description = unset,  tag = 0.
- TraceLoggingUInt8FixedArray(pbX1, 32, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingUInt8FixedArray(pbX1, 32, "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingUInt8FixedArray(pbX1, 32, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingInt8FixedArray(pValues, cValues, ...)       _tlgArgCArray(int8_t,      pValues, cValues, event_field_encoding_value8,   (event_field_format_signed_int), _tlgNdt(TraceLoggingInt8FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt8FixedArray(pValues, cValues, ...)      _tlgArgCArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (),                              _tlgNdt(TraceLoggingUInt8FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingInt16FixedArray(pValues, cValues, ...)      _tlgArgCArray(int16_t,     pValues, cValues, event_field_encoding_value16,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt16FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt16FixedArray(pValues, cValues, ...)     _tlgArgCArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (),                              _tlgNdt(TraceLoggingUInt16FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingInt32FixedArray(pValues, cValues, ...)      _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt32FixedArray(pValues, cValues, ...)     _tlgArgCArray(uint32_t,    pValues, cValues, event_field_encoding_value32,  (),                              _tlgNdt(TraceLoggingUInt32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingInt64FixedArray(pValues, cValues, ...)      _tlgArgCArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt64FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt64FixedArray(pValues, cValues, ...)     _tlgArgCArray(uint64_t,    pValues, cValues, event_field_encoding_value64,  (),                              _tlgNdt(TraceLoggingUInt64FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingIntPtrFixedArray(pValues, cValues, ...)     _tlgArgCArray(intptr_t,    pValues, cValues, event_field_encoding_value_ptr,(event_field_format_signed_int), _tlgNdt(TraceLoggingIntPtrFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUIntPtrFixedArray(pValues, cValues, ...)    _tlgArgCArray(uintptr_t,   pValues, cValues, event_field_encoding_value_ptr,(),                              _tlgNdt(TraceLoggingUIntPtrFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingLongFixedArray(pValues, cValues, ...)       _tlgArgCArray(signed long, pValues, cValues, event_field_encoding_value_long,(event_field_format_signed_int),_tlgNdt(TraceLoggingLongFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingULongFixedArray(pValues, cValues, ...)      _tlgArgCArray(unsigned long,pValues,cValues, event_field_encoding_value_long,(),                             _tlgNdt(TraceLoggingULongFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt8FixedArray(pValues, cValues, ...)    _tlgArgCArray(int8_t,      pValues, cValues, event_field_encoding_value8,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt8FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt8FixedArray(pValues, cValues, ...)   _tlgArgCArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt8FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt16FixedArray(pValues, cValues, ...)   _tlgArgCArray(int16_t,     pValues, cValues, event_field_encoding_value16,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt16FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt16FixedArray(pValues, cValues, ...)  _tlgArgCArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt16FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt32FixedArray(pValues, cValues, ...)   _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt32FixedArray(pValues, cValues, ...)  _tlgArgCArray(uint32_t,    pValues, cValues, event_field_encoding_value32,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt64FixedArray(pValues, cValues, ...)   _tlgArgCArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt64FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt64FixedArray(pValues, cValues, ...)  _tlgArgCArray(uint64_t,    pValues, cValues, event_field_encoding_value64,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt64FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexIntPtrFixedArray(pValues, cValues, ...)  _tlgArgCArray(intptr_t,    pValues, cValues, event_field_encoding_value_ptr, (event_field_format_hex_int),   _tlgNdt(TraceLoggingHexIntPtrFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUIntPtrFixedArray(pValues, cValues, ...) _tlgArgCArray(uintptr_t,   pValues, cValues, event_field_encoding_value_ptr, (event_field_format_hex_int),   _tlgNdt(TraceLoggingHexUIntPtrFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexLongFixedArray(pValues, cValues, ...)    _tlgArgCArray(signed long, pValues, cValues, event_field_encoding_value_long,(event_field_format_hex_int),   _tlgNdt(TraceLoggingHexLongFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexULongFixedArray(pValues, cValues, ...)   _tlgArgCArray(unsigned long,pValues,cValues, event_field_encoding_value_long,(event_field_format_hex_int),   _tlgNdt(TraceLoggingHexULongFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingFloat32FixedArray(pValues, cValues, ...)    _tlgArgCArray(float,       pValues, cValues, event_field_encoding_value_float,(event_field_format_float),    _tlgNdt(TraceLoggingFloat32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingFloat64FixedArray(pValues, cValues, ...)    _tlgArgCArray(double,      pValues, cValues, event_field_encoding_value_double,(event_field_format_float),   _tlgNdt(TraceLoggingFloat64FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingBooleanFixedArray(pValues, cValues, ...)    _tlgArgCArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (event_field_format_boolean),    _tlgNdt(TraceLoggingBooleanFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingBoolFixedArray(pValues, cValues, ...)       _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_boolean),    _tlgNdt(TraceLoggingBoolFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingCharFixedArray(pValues, cValues, ...)       _tlgArgCArray(char,        pValues, cValues, event_field_encoding_value8,   (event_field_format_string8),    _tlgNdt(TraceLoggingCharFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingChar16FixedArray(pValues, cValues, ...)     _tlgArgCArray(char16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_string_utf), _tlgNdt(TraceLoggingChar16FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingChar32FixedArray(pValues, cValues, ...)     _tlgArgCArray(char32_t,    pValues, cValues, event_field_encoding_value32,  (event_field_format_string_utf), _tlgNdt(TraceLoggingChar32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingWCharFixedArray(pValues, cValues, ...)      _tlgArgCArray(wchar_t,     pValues, cValues, event_field_encoding_value_wchar,(event_field_format_string_utf),_tlgNdt(TraceLoggingWCharFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPointerFixedArray(pValues, cValues, ...)    _tlgArgCArray(void const*, pValues, cValues, event_field_encoding_value_ptr, (event_field_format_hex_int),   _tlgNdt(TraceLoggingPointerFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPidFixedArray(pValues, cValues, ...)        _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_pid),        _tlgNdt(TraceLoggingPidFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPortFixedArray(pValues, cValues, ...)       _tlgArgCArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_port),       _tlgNdt(TraceLoggingPortFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingErrnoFixedArray(pValues, cValues, ...)      _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_errno),      _tlgNdt(TraceLoggingErrnoFixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingTime32FixedArray(pValues, cValues, ...)     _tlgArgCArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_time),       _tlgNdt(TraceLoggingTime32FixedArray, pValues, ##__VA_ARGS__))
#define TraceLoggingTime64FixedArray(pValues, cValues, ...)     _tlgArgCArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_time),       _tlgNdt(TraceLoggingTime64FixedArray, pValues, ##__VA_ARGS__))

/*
Wrapper macros for event fields with values that are variable-length arrays.
Usage: TraceLoggingInt32Array(pVals, cVals, "name", "description", tag).

The pVals parameter must be a pointer to cVals items of the specified type.

The cVals parameter must be an element count 0..65535.

The name, description, and tag parameters are optional.

If provided, the name parameter must be a char string literal (not a variable)
and must not contain any ';' or '\0' characters. If the name is not provided,
the value parameter is used to generate a name. Name is treated as utf-8.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

Examples:
- TraceLoggingUInt8Array(pbX1, cbX1)                      // field name = "pbX1", description = unset,  tag = 0.
- TraceLoggingUInt8Array(pbX1, cbX1, "name")              // field name = "name", description = unset,  tag = 0.
- TraceLoggingUInt8Array(pbX1, cbX1, "name", "desc")      // field name = "name", description = "desc", tag = 0.
- TraceLoggingUInt8Array(pbX1, cbX1, "name", "desc", 0x4) // field name = "name", description = "desc", tag = 0x4.
*/
#define TraceLoggingInt8Array(pValues, cValues, ...)       _tlgArgVArray(int8_t,      pValues, cValues, event_field_encoding_value8,   (event_field_format_signed_int), _tlgNdt(TraceLoggingInt8Array, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt8Array(pValues, cValues, ...)      _tlgArgVArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (),                              _tlgNdt(TraceLoggingUInt8Array, pValues, ##__VA_ARGS__))
#define TraceLoggingInt16Array(pValues, cValues, ...)      _tlgArgVArray(int16_t,     pValues, cValues, event_field_encoding_value16,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt16Array, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt16Array(pValues, cValues, ...)     _tlgArgVArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (),                              _tlgNdt(TraceLoggingUInt16Array, pValues, ##__VA_ARGS__))
#define TraceLoggingInt32Array(pValues, cValues, ...)      _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt32Array(pValues, cValues, ...)     _tlgArgVArray(uint32_t,    pValues, cValues, event_field_encoding_value32,  (),                              _tlgNdt(TraceLoggingUInt32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingInt64Array(pValues, cValues, ...)      _tlgArgVArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_signed_int), _tlgNdt(TraceLoggingInt64Array, pValues, ##__VA_ARGS__))
#define TraceLoggingUInt64Array(pValues, cValues, ...)     _tlgArgVArray(uint64_t,    pValues, cValues, event_field_encoding_value64,  (),                              _tlgNdt(TraceLoggingUInt64Array, pValues, ##__VA_ARGS__))
#define TraceLoggingIntPtrArray(pValues, cValues, ...)     _tlgArgVArray(intptr_t,    pValues, cValues, event_field_encoding_value_ptr,(event_field_format_signed_int), _tlgNdt(TraceLoggingIntPtrArray, pValues, ##__VA_ARGS__))
#define TraceLoggingUIntPtrArray(pValues, cValues, ...)    _tlgArgVArray(uintptr_t,   pValues, cValues, event_field_encoding_value_ptr,(),                              _tlgNdt(TraceLoggingUIntPtrArray, pValues, ##__VA_ARGS__))
#define TraceLoggingLongArray(pValues, cValues, ...)       _tlgArgVArray(signed long, pValues, cValues, event_field_encoding_value_long,(event_field_format_signed_int),_tlgNdt(TraceLoggingLongArray, pValues, ##__VA_ARGS__))
#define TraceLoggingULongArray(pValues, cValues, ...)      _tlgArgVArray(unsigned long,pValues,cValues, event_field_encoding_value_long,(),                             _tlgNdt(TraceLoggingULongArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt8Array(pValues, cValues, ...)    _tlgArgVArray(int8_t,      pValues, cValues, event_field_encoding_value8,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt8Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt8Array(pValues, cValues, ...)   _tlgArgVArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt8Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt16Array(pValues, cValues, ...)   _tlgArgVArray(int16_t,     pValues, cValues, event_field_encoding_value16,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt16Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt16Array(pValues, cValues, ...)  _tlgArgVArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt16Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt32Array(pValues, cValues, ...)   _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt32Array(pValues, cValues, ...)  _tlgArgVArray(uint32_t,    pValues, cValues, event_field_encoding_value32,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexInt64Array(pValues, cValues, ...)   _tlgArgVArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexInt64Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUInt64Array(pValues, cValues, ...)  _tlgArgVArray(uint64_t,    pValues, cValues, event_field_encoding_value64,  (event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUInt64Array, pValues, ##__VA_ARGS__))
#define TraceLoggingHexIntPtrArray(pValues, cValues, ...)  _tlgArgVArray(intptr_t,    pValues, cValues, event_field_encoding_value_ptr,(event_field_format_hex_int),    _tlgNdt(TraceLoggingHexIntPtrArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexUIntPtrArray(pValues, cValues, ...) _tlgArgVArray(uintptr_t,   pValues, cValues, event_field_encoding_value_ptr,(event_field_format_hex_int),    _tlgNdt(TraceLoggingHexUIntPtrArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexLongArray(pValues, cValues, ...)    _tlgArgVArray(signed long, pValues, cValues, event_field_encoding_value_long,(event_field_format_hex_int),   _tlgNdt(TraceLoggingHexLongArray, pValues, ##__VA_ARGS__))
#define TraceLoggingHexULongArray(pValues, cValues, ...)   _tlgArgVArray(unsigned long,pValues,cValues, event_field_encoding_value_long,(event_field_format_hex_int),   _tlgNdt(TraceLoggingHexULongArray, pValues, ##__VA_ARGS__))
#define TraceLoggingFloat32Array(pValues, cValues, ...)    _tlgArgVArray(float,       pValues, cValues, event_field_encoding_value_float,(event_field_format_float),    _tlgNdt(TraceLoggingFloat32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingFloat64Array(pValues, cValues, ...)    _tlgArgVArray(double,      pValues, cValues, event_field_encoding_value_double,(event_field_format_float),   _tlgNdt(TraceLoggingFloat64Array, pValues, ##__VA_ARGS__))
#define TraceLoggingBooleanArray(pValues, cValues, ...)    _tlgArgVArray(uint8_t,     pValues, cValues, event_field_encoding_value8,   (event_field_format_boolean),    _tlgNdt(TraceLoggingBooleanArray, pValues, ##__VA_ARGS__))
#define TraceLoggingBoolArray(pValues, cValues, ...)       _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_boolean),    _tlgNdt(TraceLoggingBoolArray, pValues, ##__VA_ARGS__))
#define TraceLoggingCharArray(pValues, cValues, ...)       _tlgArgVArray(char,        pValues, cValues, event_field_encoding_value8,   (event_field_format_string8),    _tlgNdt(TraceLoggingCharArray, pValues, ##__VA_ARGS__))
#define TraceLoggingChar16Array(pValues, cValues, ...)     _tlgArgVArray(char16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_string_utf), _tlgNdt(TraceLoggingChar16Array, pValues, ##__VA_ARGS__))
#define TraceLoggingChar32Array(pValues, cValues, ...)     _tlgArgVArray(char32_t,    pValues, cValues, event_field_encoding_value32,  (event_field_format_string_utf), _tlgNdt(TraceLoggingChar32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingWCharArray(pValues, cValues, ...)      _tlgArgVArray(wchar_t,     pValues, cValues, event_field_encoding_value_wchar,(event_field_format_string_utf),_tlgNdt(TraceLoggingWCharArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPointerArray(pValues, cValues, ...)    _tlgArgVArray(void const*, pValues, cValues, event_field_encoding_value_ptr,(event_field_format_hex_int),    _tlgNdt(TraceLoggingPointerArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPidArray(pValues, cValues, ...)        _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_pid),        _tlgNdt(TraceLoggingPidArray, pValues, ##__VA_ARGS__))
#define TraceLoggingPortArray(pValues, cValues, ...)       _tlgArgVArray(uint16_t,    pValues, cValues, event_field_encoding_value16,  (event_field_format_port),       _tlgNdt(TraceLoggingPortArray, pValues, ##__VA_ARGS__))
#define TraceLoggingErrnoArray(pValues, cValues, ...)      _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_errno),      _tlgNdt(TraceLoggingErrnoArray, pValues, ##__VA_ARGS__))
#define TraceLoggingTime32Array(pValues, cValues, ...)     _tlgArgVArray(int32_t,     pValues, cValues, event_field_encoding_value32,  (event_field_format_time),       _tlgNdt(TraceLoggingTime32Array, pValues, ##__VA_ARGS__))
#define TraceLoggingTime64Array(pValues, cValues, ...)     _tlgArgVArray(int64_t,     pValues, cValues, event_field_encoding_value64,  (event_field_format_time),       _tlgNdt(TraceLoggingTime64Array, pValues, ##__VA_ARGS__))

/*
Wrapper macros for manually-packed fields (advanced scenarios).
These macros support custom serialization of fields for use in creating events
that would otherwise be inexpressible through TraceLoggingProvider.h. For
example, these macros can be used to write fields containing arrays of strings
or arrays of structures. That the correct use of these macros requires an
understanding of how TraceLogging encodes events. If used incorrectly, these
macros will generate events that do not decode correctly. Note that to write
arrays of strings or arrays of structures, you will usually need to do
additional work such as manually marshaling the data into a buffer before
invoking TraceLoggingWrite.

TraceLoggingPackedField(pValue, cbValue, encoding, "name", "description", tag)
TraceLoggingPackedFieldEx(pValue, cbValue, encoding, format, "name", "description", tag)
TraceLoggingPackedMetadata(encoding, "name", "description", tag)
TraceLoggingPackedMetadataEx(encoding, format, "name", "description", tag)
TraceLoggingPackedStruct(fieldCount, "name", "description", tag)
TraceLoggingPackedStructArray(fieldCount, "name", "description", tag)
TraceLoggingPackedData(pValue, cbValue)

The name parameter must be a char string literal (not a variable) and must not
contain any ';' or '\0' characters. Name is treated as utf-8. For
TraceLoggingPackedField and TraceLoggingPackedFieldEx, the name parameter is
optional. If the name is not provided, the TraceLoggingPackedField and
TraceLoggingPackedFieldEx macros will use the pValue parameter to automatically
generate a field name.

If provided, the description parameter must be a string literal.
Field description has no effect and functions as a comment.

If provided, the tag parameter must be a 16-bit integer value.

A TraceLogging event contains metadata and data. The metadata is the list of
fields, each with a name and a type. The data is the payload - an array of
raw bytes that contains the values of the event fields. The metadata is
composed of compile-time-constant data, while the data can be different each
time the event is generated. The metadata is used to decode the data, so the
metadata and the data need to be coordinated. The other wrapper macros
(TraceLoggingInt32, TraceLoggingString, etc.) automatically keep the metadata
and data coordinated, but the TraceLoggingPacked macros allow direct control
over the metadata and data so incorrect use of them can result in events that
do not decode correctly.

The TraceLoggingPackedField macro adds both metadata and data. It adds an
arbitrary field to the event's type and adds arbitrary data to the event's
payload, with field format set to Default. The TraceLoggingPackedFieldEx macro
does the same, but includes a byte for the field's format in the field
descriptor so that a non-default format can be specified.

The TraceLoggingPackedMetadata macro adds only metadata. It adds a field to the
event's type without adding any data to the event's payload, with field format
set to Default. The TraceLoggingPackedMetadataEx macro does the same, but
includes a byte for the field's format in the field descriptor so that a
non-default format can be specified.

The TraceLoggingPackedStruct macro adds only metadata (a struct declaration
never contains data -- the struct's data is provided by its fields). It begins
a structure in the event. The <fieldCount> logical fields that follow the start
of the structure are considered to be part of the structure, and they will form
one logical field. (Structures can nest, and a nested structure counts as one
logical field in the parent structure.) The TraceLoggingPackedStructArray does
the same, but it begins an array of structures (which also counts as one
logical field).

The TraceLoggingPackedData macro adds data directly into the event payload
without adding a field to the event's type.

These macros can be combined in various ways to express TraceLogging field
structures not otherwise possible. Possible scenarios include:

* Write a simple field with a specific encoding/format combination that is
  not supported by the core TraceLogging macros.

  For example, to write a nul-terminated wide string that is tagged as
  containing a JSON string:

    TraceLoggingWrite(
        g_hProvider,
        "MyEventWithJsonData",
        TraceLoggingInt32(otherData1),
        TraceLoggingPackedFieldEx(
            szJson,
            (wcslen(szJson) + 1) * sizeof(wchar_t),
            event_field_encoding_zstring_charWide,
            event_field_format_string_json,
            "MyJsonFieldName"),
        TraceLoggingInt32(otherData2));

* Write a complex field that requires marshalling data into a temporary
  buffer.

  For example, to write an array of nul-terminated ANSI strings:

    // This scenario requires manually marshaling data.
    // Don't spend time marshaling data if the event is disabled.
    if (TraceLoggingProviderEnabled(g_hProvider, myevent_level, myEventKeyword))
    {
        // This example assumes that the strings will fit into 100 bytes.
        // Your production code will need to do additional error checking, or
        // perhaps use std::vector<uint8_t> and do a buf.push_back(val) instead of
        // buf[iBuf++] = val.
        uint8_t buf[100];
        unsigned iBuf = 0;

        // Packed arrays start with a uint16_t value indicating the number of
        // elements in the array.
        buf[iBuf++] = (uint8_t)cStrings;        // Low byte of the element count (assuming little-endian)
        buf[iBuf++] = (uint8_t)(cStrings >> 8); // High byte of the element count

        // Then we need to add the content of each array element.
        for (UINT i = 0; i != cStrings; i++)
        {
            for (LPCSTR pString = pStrings[i]; *pString != 0; pString++)
            {
                buf[iBuf++] = *pString;
            }
            buf[iBuf++] = 0; // nul-terminate
        }

        TraceLoggingWrite(
            g_hProvider,
            "MyEventWithArrayOfStrings",
            TraceLoggingLevel(myevent_level),
            TraceLoggingKeyword(myEventKeyword),
            TraceLoggingInt32(otherData1),
            TraceLoggingPackedField(
                buf,
                iBuf,
                event_field_encoding_zstring_char8 | event_field_encoding_varray_flag,
                "MyArrayOfStringsFieldName"),
            TraceLoggingInt32(otherData2));
    }

* Write a structure directly as a single entity instead of as a series of
  fields.

  This can be a minor performance optimization in some cases (it can reduce
  per-event CPU and reduce stack usage), since it reduces the number of
  iovec structures that need to be created and initialized when generating
  the event. Note that structures can only be written directly if the structure
  contains no internal padding or non-blittable fields. If the structure
  contains padding or non-blittable fields, you would need to buffer and repack
  the data before using this technique, in which case it would have been more
  efficient to use the normal methods for logging structures (i.e. using a
  normal TraceLoggingStruct followed by the appropriate TraceLoggingValue for
  each field).

  Overview: provide the data for the struct using TraceLoggingPackedData;
  provide the number of fields and the name of the structure with
  TraceLoggingPackedStruct; provide the names and types of the fields using
  TraceLoggingPackedMetadata.

  Note that while the order of metadata is important and the ordering of data is
  important, the ordering between metadata and data is not important. In the
  example below, the TraceLoggingPackedData macro could appear anywhere between
  otherData1 and otherData2 without changing the result. However, it could not
  appear before otherData1 or after otherData2, since each of those also emit
  data, and the data from TraceLoggingPackedData must appear after otherData1
  and before otherData2.

    TraceLoggingWrite(
        g_hProvider,
        "MyEventWithRect",
        TraceLoggingInt32(otherData1),
        TraceLoggingPackedData(&rect, sizeof(RECT)), // Data for all 4 fields
        TraceLoggingPackedStruct(4, "RectangleFieldName"), // Metadata: Structure with 4 fields
            TraceLoggingPackedMetadata(event_field_encoding_value32, "left"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "top"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "right"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "bottom"),
        TraceLoggingInt32(otherData2));

* Write an array of structures.

  Overview: Provide the data for the array (the array count and the array
  content) using TraceLoggingPackedData; provide the number of fields and the
  name of the structure with TraceLoggingPackedStructArray; provide the names
  and types of the fields using TraceLoggingPackedMetadata.

  In the example below, the array contains no padding and no non-blittable data
  (i.e. no variable-length data, out-of-line data like pointers to strings,
  etc.), so we can provide a pointer directly to the array content. If the
  array contained padding or contained non-blittable data, you would need to
  allocate a buffer and re-pack the data, inlining any non-blittable elements
  and omitting any padding. The example below needs to provide the array
  element count (uint16_t) as well as the array content, so it uses
  TraceLoggingPackedData twice.

    TraceLoggingWrite(
        g_hProvider,
        "MyEventWithArrayOfRectangles",
        TraceLoggingInt32(otherData1),
        TraceLoggingPackedData(&cRectangles, sizeof(uint16_t)), // Data for the array count
        TraceLoggingPackedData(pRectangles, cRectangles * sizeof(RECT)), // Data for the array content
        TraceLoggingPackedStructArray(4, "RectangleArrayFieldName"), // Structure with 4 fields
            TraceLoggingPackedMetadata(event_field_encoding_value32, "left"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "top"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "right"),
            TraceLoggingPackedMetadata(event_field_encoding_value32, "bottom"),
        TraceLoggingInt32(otherData2));

Notes on serializing data:

- When the decoder receives the event, it sees the event payload as a single
  block of bytes. It does not see any boundaries between chunks of data in the
  payload. If I use TraceLoggingPackedMetadata to add an Int32 field but
  provide 5 bytes of data, the decoder will not be able to correctly decode the
  remaining fields of the event. The developer must take care that the data
  written matches up with the field definitions. On the other hand, this allows
  flexibility in the way the data is encoded. For example, I might write the
  data for several fields using a single TraceLoggingPackedData macro (more
  efficient if the data is already contiguous in memory), or I might use
  multiple TraceLoggingPackedData macros to gather bits of a single field's
  value from multiple locations in memory (more efficient than recopying the
  data to make it contiguous).
- Encoding/decoding behavior only uses the encoding. The format is only a
  formatting hint and might be ignored by the decoder.
- Form an array by adding event_field_encoding_varray_flag to the encoding. For
  example, an encoding of event_field_encoding_zstring_char8 will result in a
  field that stores a single string, but an encoding of
  event_field_encoding_zstring_char8|event_field_encoding_varray_flag will result
  in a field that stores a uint16_t count followed by a sequence of strings.
- Arrays are serialized as a uint16_t element-count followed by the elements.
  The elements in an array are serialized exactly as if they were not in an
  array, even if the element has a variable length. For example, on a
  little-endian system, the payload corrsponding to the 3-element array
  { "ABC", "DE", "F" } would be:
  uint8_t a[] = { '\3', '\0', 'A', 'B', 'C', '\0', 'D', 'E', '\0', 'F', '\0' };
*/
#define TraceLoggingPackedField(pValue, cbValue, encoding, ...)           _tlgArgPackedField(void, pValue, cbValue, encoding, (),                            _tlgNdt(TraceLoggingPackedField, pValue, ##__VA_ARGS__))
#define TraceLoggingPackedFieldEx(pValue, cbValue, encoding, format, ...) _tlgArgPackedField(void, pValue, cbValue, encoding, (format),                      _tlgNdt(TraceLoggingPackedFieldEx, pValue, ##__VA_ARGS__))
#define TraceLoggingPackedMetadata(encoding, name, ...)                   _tlgArgPackedMeta(                        encoding, (),                            _tlgNdt(TraceLoggingPackedMetadata, value, name, ##__VA_ARGS__))
#define TraceLoggingPackedMetadataEx(encoding, format, name, ...)         _tlgArgPackedMeta(                        encoding, (format),                      _tlgNdt(TraceLoggingPackedMetadataEx, value, name, ##__VA_ARGS__))
#define TraceLoggingPackedStruct(fieldCount, name, ...)                   _tlgArgStruct(fieldCount, event_field_encoding_struct,                               _tlgNdt(TraceLoggingPackedStruct, value, name, ##__VA_ARGS__))
#define TraceLoggingPackedStructArray(fieldCount, name, ...)              _tlgArgStruct(fieldCount, event_field_encoding_struct|event_field_encoding_varray_flag, _tlgNdt(TraceLoggingPackedStructArray, value, name, ##__VA_ARGS__))
#define TraceLoggingPackedData(pValue, cbValue)                           _tlgArgPackedData( void, pValue, cbValue)

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_utility macros (for internal use only)
#endif

#ifndef _tlg_ASSERT
#define _tlg_ASSERT(x) assert(x)
#endif // _tlg_ASSERT

#ifndef _tlg_NOEXCEPT
#ifdef __cplusplus
#define _tlg_NOEXCEPT noexcept
#else // __cplusplus
#define _tlg_NOEXCEPT
#endif // __cplusplus
#endif // _tlg_NOEXCEPT

#ifndef _tlg_WEAK_ATTRIBUTES
#define _tlg_WEAK_ATTRIBUTES __attribute__((weak, visibility("hidden")))
#endif // _tlg_WEAK_ATTRIBUTES

#ifndef _tlg_INLINE_ATTRIBUTES
#define _tlg_INLINE_ATTRIBUTES
#endif // _tlg_INLINE_ATTRIBUTES

#ifndef _tlg_NULL
#ifdef __cplusplus
#define _tlg_NULL nullptr
#else // __cplusplus
#define _tlg_NULL NULL
#endif // __cplusplus
#endif // _tlg_NULL

#ifdef __cplusplus
#define _tlg_EXTERN_C       extern "C"
#define _tlg_EXTERN_C_CONST extern "C" const
#else // __cplusplus
#define _tlg_EXTERN_C       extern // In C, linkage is already "C".
#define _tlg_EXTERN_C_CONST const  // In C, extern with initializer is wrong.
#endif // __cplusplus

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_PASTE2(a, b)        _tlg_PASTE2_imp(a, b)
#define _tlg_PASTE2_imp(a, b)    a##b

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_PASTE3(a, b, c)     _tlg_PASTE3_imp(a, b, c)
#define _tlg_PASTE3_imp(a, b, c) a##b##c

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_FLATTEN(...) __VA_ARGS__

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_PARENTHESIZE(...) (__VA_ARGS__)

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_STRINGIZE(x) _tlg_STRINGIZE_imp(x)
#define _tlg_STRINGIZE_imp(x) #x

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_CAT(a, ...) _tlg_CAT_imp(a, __VA_ARGS__)
#define _tlg_CAT_imp(a, ...) a##__VA_ARGS__

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_SPLIT(cond, ...) _tlg_SPLIT_imp(cond, (__VA_ARGS__))
#define _tlg_SPLIT_imp(cond, args) _tlg_PASTE2(_tlg_SPLIT_imp, cond) args
#define _tlg_SPLIT_imp0(false_val, ...) false_val
#define _tlg_SPLIT_imp1(false_val, ...) __VA_ARGS__

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_IS_PARENTHESIZED(...) \
    _tlg_SPLIT(0, _tlg_CAT(_tlg_IS_PARENTHESIZED_imp, _tlg_IS_PARENTHESIZED_imp0 __VA_ARGS__))
#define _tlg_IS_PARENTHESIZED_imp_tlg_IS_PARENTHESIZED_imp0 0,
#define _tlg_IS_PARENTHESIZED_imp0(...) 1
#define _tlg_IS_PARENTHESIZED_imp1 1,

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_IS_EMPTY(...) _tlg_SPLIT(                      \
    _tlg_IS_PARENTHESIZED(__VA_ARGS__),                     \
    _tlg_IS_PARENTHESIZED(_tlg_PARENTHESIZE __VA_ARGS__()), \
    0)

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_NARGS(...) _tlg_NARGS_imp(_tlg_IS_EMPTY(__VA_ARGS__), (__VA_ARGS__))
#define _tlg_NARGS_imp(is_empty, args) _tlg_PASTE2(_tlg_NARGS_imp, is_empty) args
#define _tlg_NARGS_imp0(...) _tlg_PASTE2(_tlg_NARGS_imp2( \
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
#define _tlg_NARGS_imp1() 0
#define _tlg_NARGS_imp2(                              \
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

// Internal implementation detail: Not for use outside of TraceLoggingProvider.h.
#define _tlg_FOREACH(macro, ...) _tlg_FOR_imp(_tlg_NARGS(__VA_ARGS__), (macro, __VA_ARGS__))
#define _tlg_FOR_imp(n, macroAndArgs) _tlg_PASTE2(_tlg_FOR_imp, n) macroAndArgs
#define _tlg_FOR_imp0(f, ...)
#define _tlg_FOR_imp1(f, a0) f(0, a0)
#define _tlg_FOR_imp2(f, a0, a1) f(0, a0) f(1, a1)
#define _tlg_FOR_imp3(f, a0, a1, a2) f(0, a0) f(1, a1) f(2, a2)
#define _tlg_FOR_imp4(f, a0, a1, a2, a3) f(0, a0) f(1, a1) f(2, a2) f(3, a3)
#define _tlg_FOR_imp5(f, a0, a1, a2, a3, a4) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4)
#define _tlg_FOR_imp6(f, a0, a1, a2, a3, a4, a5) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5)
#define _tlg_FOR_imp7(f, a0, a1, a2, a3, a4, a5, a6) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6)
#define _tlg_FOR_imp8(f, a0, a1, a2, a3, a4, a5, a6, a7) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7)
#define _tlg_FOR_imp9(f, a0, a1, a2, a3, a4, a5, a6, a7, a8) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8)
#define _tlg_FOR_imp10(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9)
#define _tlg_FOR_imp11(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10)
#define _tlg_FOR_imp12(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11)
#define _tlg_FOR_imp13(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12)
#define _tlg_FOR_imp14(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13)
#define _tlg_FOR_imp15(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14)
#define _tlg_FOR_imp16(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15)
#define _tlg_FOR_imp17(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16)
#define _tlg_FOR_imp18(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17)
#define _tlg_FOR_imp19(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18)
#define _tlg_FOR_imp20(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19)
#define _tlg_FOR_imp21(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20)
#define _tlg_FOR_imp22(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21)
#define _tlg_FOR_imp23(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22)
#define _tlg_FOR_imp24(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23)
#define _tlg_FOR_imp25(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24)
#define _tlg_FOR_imp26(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25)
#define _tlg_FOR_imp27(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26)
#define _tlg_FOR_imp28(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27)
#define _tlg_FOR_imp29(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28)
#define _tlg_FOR_imp30(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29)
#define _tlg_FOR_imp31(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30)
#define _tlg_FOR_imp32(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31)
#define _tlg_FOR_imp33(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32)
#define _tlg_FOR_imp34(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33)
#define _tlg_FOR_imp35(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34)
#define _tlg_FOR_imp36(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35)
#define _tlg_FOR_imp37(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36)
#define _tlg_FOR_imp38(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37)
#define _tlg_FOR_imp39(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38)
#define _tlg_FOR_imp40(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39)
#define _tlg_FOR_imp41(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40)
#define _tlg_FOR_imp42(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41)
#define _tlg_FOR_imp43(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42)
#define _tlg_FOR_imp44(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43)
#define _tlg_FOR_imp45(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44)
#define _tlg_FOR_imp46(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45)
#define _tlg_FOR_imp47(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46)
#define _tlg_FOR_imp48(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47)
#define _tlg_FOR_imp49(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48)
#define _tlg_FOR_imp50(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49)
#define _tlg_FOR_imp51(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50)
#define _tlg_FOR_imp52(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51)
#define _tlg_FOR_imp53(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52)
#define _tlg_FOR_imp54(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53)
#define _tlg_FOR_imp55(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54)
#define _tlg_FOR_imp56(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55)
#define _tlg_FOR_imp57(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56)
#define _tlg_FOR_imp58(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57)
#define _tlg_FOR_imp59(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58)
#define _tlg_FOR_imp60(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59)
#define _tlg_FOR_imp61(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60)
#define _tlg_FOR_imp62(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61)
#define _tlg_FOR_imp63(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62)
#define _tlg_FOR_imp64(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63)
#define _tlg_FOR_imp65(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64)
#define _tlg_FOR_imp66(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65)
#define _tlg_FOR_imp67(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66)
#define _tlg_FOR_imp68(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67)
#define _tlg_FOR_imp69(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68)
#define _tlg_FOR_imp70(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69)
#define _tlg_FOR_imp71(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70)
#define _tlg_FOR_imp72(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71)
#define _tlg_FOR_imp73(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72)
#define _tlg_FOR_imp74(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73)
#define _tlg_FOR_imp75(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74)
#define _tlg_FOR_imp76(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75)
#define _tlg_FOR_imp77(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76)
#define _tlg_FOR_imp78(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77)
#define _tlg_FOR_imp79(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78)
#define _tlg_FOR_imp80(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79)
#define _tlg_FOR_imp81(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80)
#define _tlg_FOR_imp82(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81)
#define _tlg_FOR_imp83(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82)
#define _tlg_FOR_imp84(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83)
#define _tlg_FOR_imp85(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84)
#define _tlg_FOR_imp86(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85)
#define _tlg_FOR_imp87(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86)
#define _tlg_FOR_imp88(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87)
#define _tlg_FOR_imp89(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88)
#define _tlg_FOR_imp90(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89)
#define _tlg_FOR_imp91(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90)
#define _tlg_FOR_imp92(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91)
#define _tlg_FOR_imp93(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92)
#define _tlg_FOR_imp94(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93)
#define _tlg_FOR_imp95(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94)
#define _tlg_FOR_imp96(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95)
#define _tlg_FOR_imp97(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96)
#define _tlg_FOR_imp98(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96, a97) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96) f(97, a97)
#define _tlg_FOR_imp99(f, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16, a17, a18, a19, a20, a21, a22, a23, a24, a25, a26, a27, a28, a29, a30, a31, a32, a33, a34, a35, a36, a37, a38, a39, a40, a41, a42, a43, a44, a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56, a57, a58, a59, a60, a61, a62, a63, a64, a65, a66, a67, a68, a69, a70, a71, a72, a73, a74, a75, a76, a77, a78, a79, a80, a81, a82, a83, a84, a85, a86, a87, a88, a89, a90, a91, a92, a93, a94, a95, a96, a97, a98) f(0, a0) f(1, a1) f(2, a2) f(3, a3) f(4, a4) f(5, a5) f(6, a6) f(7, a7) f(8, a8) f(9, a9) f(10, a10) f(11, a11) f(12, a12) f(13, a13) f(14, a14) f(15, a15) f(16, a16) f(17, a17) f(18, a18) f(19, a19) f(20, a20) f(21, a21) f(22, a22) f(23, a23) f(24, a24) f(25, a25) f(26, a26) f(27, a27) f(28, a28) f(29, a29) f(30, a30) f(31, a31) f(32, a32) f(33, a33) f(34, a34) f(35, a35) f(36, a36) f(37, a37) f(38, a38) f(39, a39) f(40, a40) f(41, a41) f(42, a42) f(43, a43) f(44, a44) f(45, a45) f(46, a46) f(47, a47) f(48, a48) f(49, a49) f(50, a50) f(51, a51) f(52, a52) f(53, a53) f(54, a54) f(55, a55) f(56, a56) f(57, a57) f(58, a58) f(59, a59) f(60, a60) f(61, a61) f(62, a62) f(63, a63) f(64, a64) f(65, a65) f(66, a66) f(67, a67) f(68, a68) f(69, a69) f(70, a70) f(71, a71) f(72, a72) f(73, a73) f(74, a74) f(75, a75) f(76, a76) f(77, a77) f(78, a78) f(79, a79) f(80, a80) f(81, a81) f(82, a82) f(83, a83) f(84, a84) f(85, a85) f(86, a86) f(87, a87) f(88, a88) f(89, a89) f(90, a90) f(91, a91) f(92, a92) f(93, a93) f(94, a94) f(95, a95) f(96, a96) f(97, a97) f(98, a98)

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_functions (for internal use only)
#endif

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    static inline void
    _tlgCreate1Vec(struct iovec* pVec, void const* pb, size_t cb) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
    static inline void
    _tlgCreate1Vec(struct iovec* pVec, void const* pb, size_t cb) _tlg_NOEXCEPT
    {
        pVec[0].iov_base = (void*)pb;
        pVec[0].iov_len = cb * sizeof(char);
    }

#ifndef __cplusplus

    static inline void
    _tlgCreate1Sz_char(struct iovec* pVec, char const* sz) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
    static inline void
    _tlgCreate1Sz_char(struct iovec* pVec, char const* sz) _tlg_NOEXCEPT
    {
        char const* pch = sz ? sz : "";
        size_t cch;
        for (cch = 0; pch[cch] != 0; cch += 1) {}
        cch += 1; // nul-termination
        _tlgCreate1Vec(pVec, pch, cch * sizeof(char));
    }

    static inline void
    _tlgCreate1Sz_wchar_t(struct iovec* pVec, wchar_t const* sz) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
    static inline void
    _tlgCreate1Sz_wchar_t(struct iovec* pVec, wchar_t const* sz) _tlg_NOEXCEPT
    {
        wchar_t const* pch = sz ? sz : L"";
        size_t cch;
        for (cch = 0; pch[cch] != 0; cch += 1) {}
        cch += 1; // nul-termination
        _tlgCreate1Vec(pVec, pch, cch * sizeof(wchar_t));
    }

    static inline void
    _tlgCreate1Sz_char16_t(struct iovec* pVec, void const* sz) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
    static inline void
    _tlgCreate1Sz_char16_t(struct iovec* pVec, void const* sz) _tlg_NOEXCEPT
    {
        static uint16_t const Zero = 0;
        uint16_t const* pch = sz ? (uint16_t const*)sz : &Zero;
        size_t cch;
        for (cch = 0; pch[cch] != 0; cch += 1) {}
        cch += 1; // nul-termination
        _tlgCreate1Vec(pVec, pch, cch * sizeof(uint16_t));
    }

    static inline void
    _tlgCreate1Sz_char32_t(struct iovec* pVec, void const* sz) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
    static inline void
    _tlgCreate1Sz_char32_t(struct iovec* pVec, void const* sz) _tlg_NOEXCEPT
    {
        static uint32_t const Zero = 0;
        uint32_t const* pch = sz ? (uint32_t const*)sz : &Zero;
        size_t cch;
        for (cch = 0; pch[cch] != 0; cch += 1) {}
        cch += 1; // nul-termination
        _tlgCreate1Vec(pVec, pch, cch * sizeof(uint32_t));
    }

#else // __cplusplus
} // extern "C"

template<class ctype>
inline void _tlgCppCreate1Val(struct iovec* pVec, ctype const& value) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate1Val(struct iovec* pVec, ctype const& value) _tlg_NOEXCEPT
{
    _tlgCreate1Vec(&pVec[0], &value, sizeof(ctype));
}

template<class ctype>
inline void _tlgCppCreate1SizeVals(struct iovec* pVec, ctype const* pValues, uint16_t cbValues) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate1SizeVals(struct iovec* pVec, ctype const* pValues, uint16_t cbValues) _tlg_NOEXCEPT
{
    _tlgCreate1Vec(&pVec[0], pValues, cbValues);
}

template<class ctype>
inline void _tlgCppCreate1ValsNul(struct iovec* pVec, ctype const* pszValue) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate1ValsNul(struct iovec* pVec, ctype const* pszValue) _tlg_NOEXCEPT
{
    static ctype const Zero = 0;
    ctype const* pch = pszValue ? pszValue : &Zero;
    size_t cch;
    for (cch = 0; pch[cch] != 0; cch += 1) {}
    cch += 1; // nul-termination
    _tlgCreate1Vec(&pVec[0], pch, cch * sizeof(ctype));
}

template<class ctype>
inline void _tlgCppCreate2CountVals(struct iovec* pVec, ctype const* pValues, uint16_t const& cValues) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate2CountVals(struct iovec* pVec, ctype const* pValues, uint16_t const& cValues) _tlg_NOEXCEPT
{
    _tlgCreate1Vec(&pVec[0], &cValues, sizeof(cValues));
    _tlgCreate1Vec(&pVec[1], pValues, cValues * sizeof(ctype));
}

template<class ctype>
inline void _tlgCppCreate2SizeVals(struct iovec* pVec, ctype const* pValues, uint16_t const& cbValues) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate2SizeVals(struct iovec* pVec, ctype const* pValues, uint16_t const& cbValues) _tlg_NOEXCEPT
{
    _tlgCreate1Vec(&pVec[0], &cbValues, sizeof(cbValues));
    _tlgCreate1Vec(&pVec[1], pValues, cbValues);
}

// TraceLoggingValue support (implicit type detection)

// Remove reference
template <class T>
struct _tlgRemoveReference
{
    typedef T type;
};
template <class T>
struct _tlgRemoveReference<T&>
{
    typedef T type;
};
template <class T>
struct _tlgRemoveReference<T&&>
{
    typedef T type;
};

// Remove const/volatile
template <class T>
struct _tlgRemoveCV
{
    typedef T type;
};
template <class T>
struct _tlgRemoveCV<T const>
{
    typedef T type;
};
template <class T>
struct _tlgRemoveCV<T volatile>
{
    typedef T type;
};
template <class T>
struct _tlgRemoveCV<T const volatile>
{
    typedef T type;
};

// Given non-ref type, remove const/volatile.
template <class T>
struct _tlgDecay_impl
{
    typedef typename _tlgRemoveCV<T>::type type;
};
template <class T>
struct _tlgDecay_impl<T[]>
{
    typedef T* type;
};
template <class T, size_t n>
struct _tlgDecay_impl<T[n]>
{
    typedef T* type;
};

// Remove reference, remove const/volatile, arrays decay to pointers.
template <class T>
struct _tlgDecay
{
    typedef typename _tlgDecay_impl<typename _tlgRemoveReference<T>::type>::type type;
};

/*
Convert a type into encoding + format.
*/
template<class T> struct _tlgTypeMapBase
{
    static_assert(sizeof(T) == 0, "The type is not supported by TraceLoggingValue.");
};

template<class T> struct _tlgTypeMap
    : _tlgTypeMapBase<typename _tlgDecay<T>::type> { };

template<uint8_t encoding, uint8_t format, bool hasTag>
struct _tlgTypeMapVal {
#if __BYTE_ORDER == __LITTLE_ENDIAN
    static uint16_t const value =
        ((encoding | 0x80) << 0) |
        ((format | (hasTag ? 0x80 : 0x00)) << 8);
#else
    static uint16_t const value =
        ((encoding | 0x80) << 8) |
        ((format | (hasTag ? 0x80 : 0x00)) << 0);
#endif
};

// _tlgTypeMapBaseDecl: format is 0.
#define _tlgTypeMapBaseDecl(simple, ctype, encoding) \
    template<> struct _tlgTypeMapBase<ctype> \
    { \
        typedef uint8_t  _tlgTypeType0; /* No field tag: Don't need to store format. */ \
        typedef uint16_t _tlgTypeType1; /* Yes field tag: Need to store format = 0. */ \
        static bool const _tlgIsSimple = simple; \
        static _tlgTypeType0 const _tlgType0 = encoding | 0x00; \
        static _tlgTypeType1 const _tlgType1 = _tlgTypeMapVal<encoding, 0, true>::value; \
    }

// _tlgTypeMapBaseDeclFmt: format is not 0.
#define _tlgTypeMapBaseDeclFmt(simple, ctype, encoding, format) \
    template<> struct _tlgTypeMapBase<ctype> \
    { \
        typedef uint16_t _tlgTypeType0; /* Need to store format+encoding. */ \
        typedef uint16_t _tlgTypeType1; /* Need to store format+encoding. */ \
        static bool const _tlgIsSimple = simple; \
        static _tlgTypeType0 const _tlgType0 = _tlgTypeMapVal<encoding, format, false>::value; \
        static _tlgTypeType1 const _tlgType1 = _tlgTypeMapVal<encoding, format, true>::value; \
    }

// _tlgCppCreate1Auto normal case (where we want to write sizeof(T) bytes of data):

template<class ctype>
inline void _tlgCppCreate1Auto(struct iovec* pVec, ctype const& value) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES;
template<class ctype>
inline void _tlgCppCreate1Auto(struct iovec* pVec, ctype const& value) _tlg_NOEXCEPT
{
    static_assert(_tlgTypeMap<ctype>::_tlgIsSimple, "Missing _tlgCppCreate1Auto overload");
    _tlgCreate1Vec(&pVec[0], &value, sizeof(ctype));
}

#define _tlgTypeMapBaseDecl0(ctype, encoding        ) _tlgTypeMapBaseDecl(   true, ctype, encoding)
#define _tlgTypeMapBaseDecl1(ctype, encoding, format) _tlgTypeMapBaseDeclFmt(true, ctype, encoding, format)
static_assert(sizeof(bool) == 1, "TraceLoggingValue implementation incomplete for bool.");
static_assert(sizeof(char) == 1, "TraceLoggingValue implementation incomplete for char.");
static_assert(sizeof(short) == 2, "TraceLoggingValue implementation incomplete for short.");
static_assert(sizeof(int) == 4, "TraceLoggingValue implementation incomplete for int.");
static_assert(sizeof(long long) == 8, "TraceLoggingValue implementation incomplete for long long.");
_tlgTypeMapBaseDecl1(bool,           event_field_encoding_value8, event_field_format_boolean);
_tlgTypeMapBaseDecl1(char,           event_field_encoding_value8, event_field_format_string8);
_tlgTypeMapBaseDecl1(char16_t,       event_field_encoding_value16, event_field_format_string_utf);
_tlgTypeMapBaseDecl1(char32_t,       event_field_encoding_value32, event_field_format_string_utf);
_tlgTypeMapBaseDecl1(wchar_t,        event_field_encoding_value_wchar, event_field_format_string_utf);
_tlgTypeMapBaseDecl1(signed   char,  event_field_encoding_value8, event_field_format_signed_int);
_tlgTypeMapBaseDecl0(unsigned char,  event_field_encoding_value8);
_tlgTypeMapBaseDecl1(signed   short, event_field_encoding_value16, event_field_format_signed_int);
_tlgTypeMapBaseDecl0(unsigned short, event_field_encoding_value16);
_tlgTypeMapBaseDecl1(signed   int,   event_field_encoding_value32, event_field_format_signed_int);
_tlgTypeMapBaseDecl0(unsigned int,   event_field_encoding_value32);
_tlgTypeMapBaseDecl1(signed   long,  event_field_encoding_value_long, event_field_format_signed_int);
_tlgTypeMapBaseDecl0(unsigned long,  event_field_encoding_value_long);
_tlgTypeMapBaseDecl1(signed   long long, event_field_encoding_value64, event_field_format_signed_int);
_tlgTypeMapBaseDecl0(unsigned long long, event_field_encoding_value64);
_tlgTypeMapBaseDecl1(float,          event_field_encoding_value_float, event_field_format_float);
_tlgTypeMapBaseDecl1(double,         event_field_encoding_value_double, event_field_format_float);
_tlgTypeMapBaseDecl1(void*,          event_field_encoding_value_ptr, event_field_format_hex_int);
_tlgTypeMapBaseDecl1(void const*,    event_field_encoding_value_ptr, event_field_format_hex_int);
#undef _tlgTypeMapBaseDecl0
#undef _tlgTypeMapBaseDecl1

// _tlgCppCreate1Auto special cases (not writing sizeof(T) bytes of data):

#define _tlgCppCreateAutoDecl(CHAR) \
    inline void _tlgCppCreate1Auto(struct iovec* pVec, CHAR* sz) _tlg_NOEXCEPT _tlg_INLINE_ATTRIBUTES; \
    inline void _tlgCppCreate1Auto(struct iovec* pVec, CHAR* sz) _tlg_NOEXCEPT \
    { \
        _tlgCppCreate1ValsNul(&pVec[0], sz); \
    }
_tlgCppCreateAutoDecl(char);
_tlgCppCreateAutoDecl(char const);
_tlgCppCreateAutoDecl(char16_t);
_tlgCppCreateAutoDecl(char16_t const);
_tlgCppCreateAutoDecl(char32_t);
_tlgCppCreateAutoDecl(char32_t const);
_tlgCppCreateAutoDecl(wchar_t);
_tlgCppCreateAutoDecl(wchar_t const);

_tlgTypeMapBaseDeclFmt(false, char*, event_field_encoding_zstring_char8, event_field_format_string8);
_tlgTypeMapBaseDeclFmt(false, char const*, event_field_encoding_zstring_char8, event_field_format_string8);
_tlgTypeMapBaseDecl(false, char16_t*, event_field_encoding_zstring_char16);
_tlgTypeMapBaseDecl(false, char16_t const*, event_field_encoding_zstring_char16);
_tlgTypeMapBaseDecl(false, char32_t*, event_field_encoding_zstring_char32);
_tlgTypeMapBaseDecl(false, char32_t const*, event_field_encoding_zstring_char32);
_tlgTypeMapBaseDecl(false, wchar_t*, event_field_encoding_zstring_wchar);
_tlgTypeMapBaseDecl(false, wchar_t const*, event_field_encoding_zstring_wchar);

// _tlgCppCreate1Auto special cases

#endif // __cplusplus

// ********** NO FUNCTION DEFINITIONS BELOW THIS POINT ***********************

#ifdef __EDG__
#pragma endregion
#endif

#ifdef __EDG__
#pragma region Internal_implementation macros (for internal use only)
#endif

#define _tlgParseProviderId(...) \
    _tlgParseProviderId_impN(_tlg_NARGS(__VA_ARGS__), __VA_ARGS__)
#define _tlgParseProviderId_impN(n, providerId) \
    _tlg_PASTE2(_tlgParseProviderId_imp, n)(providerId)
#define _tlgParseProviderId_imp0(...) /* parameter not provided - error case */ \
    static_assert(0, "TRACELOGGING_DEFINE_PROVIDER providerId must be specified as eleven integers, e.g. (1,2,3,4,5,6,7,8,9,10,11).");
#define _tlgParseProviderId_imp1(providerId) \
    _tracelogging_SyntaxError_ProviderIdMustBeEnclosedInParentheses providerId
#define _tracelogging_SyntaxError_ProviderIdMustBeEnclosedInParentheses(...)                                                                          \
    static_assert(_tlg_NARGS(__VA_ARGS__) == 11, "TRACELOGGING_DEFINE_PROVIDER providerId must be eleven integers, e.g. (1,2,3,4,5,6,7,8,9,10,11)."); \
    static_assert(1 _tlg_FOREACH(_tlgParseProviderId_CheckInt, __VA_ARGS__), "TRACELOGGING_DEFINE_PROVIDER providerId must be eleven integers, e.g. (1,2,3,4,5,6,7,8,9,10,11).");
#define _tlgParseProviderId_CheckInt(n, val) +(val)

#define _tlgProviderOptions(...)             _tlgProviderOptions_impA(_tlg_NARGS(__VA_ARGS__), __VA_ARGS__)
#define _tlgProviderOptions_impA(nargs, ...) _tlgProviderOptions_impB(_tlg_PASTE2(_tlgProviderOptions_imp, nargs), (__VA_ARGS__))
#define _tlgProviderOptions_impB(macro, args) macro args
#define _tlgProviderOptions_imp0(...) ""
#define _tlgProviderOptions_imp1(option) _tlg_TraceLogging_Unrecognized_provider_option_##option
#define _tlg_TraceLogging_Unrecognized_provider_option_TraceLoggingOptionGroupName(groupName) "G" groupName

/*
_tlgExpandType(typeParam)  --> typeParam, hasType // typeParam should be a parenthesized value
_tlgExpandType(())         --> (0),       0
_tlgExpandType((typeVal))  --> (typeVal), 1
*/
#define _tlgExpandType(typeParam)         _tlgExpandType_impA(_tlg_NARGS typeParam, typeParam)
#define _tlgExpandType_impA(n, typeParam) _tlgExpandType_impB(_tlg_PASTE2(_tlgExpandType_imp, n), typeParam)
#define _tlgExpandType_impB(macro, args)  macro args
#define _tlgExpandType_imp0()             (0),       0
#define _tlgExpandType_imp1(typeVal)      (typeVal), 1

/*
_tlgNdt: Extracts Name/Description/Tag from varargs of wrapper macro with optional name.
_tlgNdt(macroname, value, __VA_ARGS__) --> "fieldName", L"description", tag, hasTag
*/
#define _tlgNdt(macroname, value, ...) _tlgNdt_impA(_tlg_NARGS(__VA_ARGS__), (macroname, value, __VA_ARGS__))
#define _tlgNdt_impA(n, args)          _tlgNdt_impB(_tlg_PASTE2(_tlgNdt_imp, n), args)
#define _tlgNdt_impB(macro, args)      macro args
#define _tlgNdt_imp0(macroname, value, ...)                (#value, , , 0)
#define _tlgNdt_imp1(macroname, value, name)               (name  , , , 0)
#define _tlgNdt_imp2(macroname, value, name, desc)         (name, L##desc, , 0)
#define _tlgNdt_imp3(macroname, value, name, desc, ftag)   (name, L##desc, ftag, 1)
#define _tlgNdt_imp4(macroname, ...) (too_many_values_passed_to_##macroname, , , 0)
#define _tlgNdt_imp5(macroname, ...) (too_many_values_passed_to_##macroname, , , 0)

#define _tlgNdtName(ndt) _tlgNdtName_imp ndt
#define _tlgNdtName_imp(name, desc, ftag, hasTag) name
#define _tlgNdtFtag(ndt) _tlgNdtFtag_imp ndt
#define _tlgNdtFtag_imp(name, desc, ftag, hasTag) ftag
#define _tlgNdtHasTag(ndt) _tlgNdtHasTag_imp ndt
#define _tlgNdtHasTag_imp(name, desc, ftag, hasTag) hasTag

/*
_tlgApplyArgs and _tlgApplyArgsN: Macro dispatchers.
_tlgApplyArgs( macro,    (handler, ...)) --> macro##handler(...)
_tlgApplyArgsN(macro, n, (handler, ...)) --> macro##handler(n, ...)
*/
#define _tlgApplyArgs(macro, args)                  _tlgApplyArgs_impA((macro, _tlgApplyArgs_UNWRAP args))
#define _tlgApplyArgs_impA(args)                    _tlgApplyArgs_impB args
#define _tlgApplyArgs_impB(macro, handler, ...)     _tlgApplyArgs_CALL(macro, handler, (__VA_ARGS__))
#define _tlgApplyArgs_UNWRAP(...)                   __VA_ARGS__
#define _tlgApplyArgs_CALL(macro, handler, args)    macro##handler args
#define _tlgApplyArgsN(macro, n, args)              _tlgApplyArgsN_impA((macro, n, _tlgApplyArgs_UNWRAP args))
#define _tlgApplyArgsN_impA(args)                   _tlgApplyArgsN_impB args
#define _tlgApplyArgsN_impB(macro, n, handler, ...) _tlgApplyArgs_CALL(macro, handler, (n, __VA_ARGS__))

// Internal implementation details: Not for use outside of TraceLoggingProvider.h.
#define _tlgArgIgnored()                                                     /* for TraceLoggingDescription, etc. */ \
          (_tlgIgnored)
#define _tlgArgKeyword(    eventKeyword)                                     /* for TraceLoggingKeyword. */ \
          (_tlgKeyword,    eventKeyword)
#define _tlgArgOpcode(     eventOpcode)                                      /* for TraceLoggingOpcode. */ \
          (_tlgOpcode,     eventOpcode)
#define _tlgArgEventTag(   eventTag)                                         /* for TraceLoggingEventTag. */ \
          (_tlgEventTag,   eventTag)
#define _tlgArgIdVersion(  eventId, eventVersion)                            /* for TraceLoggingIdVersion. */ \
          (_tlgIdVersion,  eventId, eventVersion)
#define _tlgArgLevel(      eventLevel)                                       /* for TraceLoggingLevel. */ \
          (_tlgLevel,      eventLevel)
#define _tlgArgAuto(              value,                                ndt) /* for TraceLoggingValue. */ \
          (_tlgAuto,              value,                                                ndt)
#define _tlgArgValue(      ctype, value,              encoding, format, ndt) /* for by-val scalar. */ \
          (_tlgValue,      ctype, value,              encoding, _tlgExpandType(format), ndt)
#define _tlgArgStrNul(     ctype, pszValue,           encoding, format, ndt) /* for zero-terminated string. */ \
          (_tlgStrNul,     ctype, pszValue,           encoding, _tlgExpandType(format), ndt)
#define _tlgArgStrCch(     ctype, pchValue, cchValue, encoding, format, ndt) /* for counted strings. */ \
          (_tlgStrCch,     ctype, pchValue, cchValue, encoding, _tlgExpandType(format), ndt)
#define _tlgArgBin(        ctype, pValue,   cbValue,  encoding, format, ndt) /* for binary data. */ \
          (_tlgBin,        ctype, pValue,   cbValue,  encoding, _tlgExpandType(format), ndt)
#define _tlgArgVArray(     ctype, pValues,  cValues,  encoding, format, ndt) /* for variable-length array with count of elements. */ \
          (_tlgVArray,     ctype, pValues,  cValues,  encoding, _tlgExpandType(format), ndt)
#define _tlgArgCArray(     ctype, pValues,  cValues,  encoding, format, ndt) /* for fixed-length array with count of elements. */ \
          (_tlgCArray,     ctype, pValues,  cValues,  encoding, _tlgExpandType(format), ndt)
#define _tlgArgPackedField(ctype, pValue,   cbValue,  encoding, format, ndt) /* for user-marshalled data and metadata. */ \
          (_tlgPackedField,ctype, pValue,   cbValue,  encoding, _tlgExpandType(format), ndt)
#define _tlgArgPackedMeta(                            encoding, format, ndt) /* for user-marshalled metadata. */ \
          (_tlgPackedMeta,                            encoding, _tlgExpandType(format), ndt)
#define _tlgArgPackedData( ctype, pValue,   cbValue                        ) /* for user-marshalled data. */ \
          (_tlgPackedData, ctype, pValue,   cbValue)
#define _tlgArgStruct(     fieldCount,                encoding,         ndt) /* for struct and array of struct. */ \
          (_tlgStruct,     fieldCount,                encoding,                         ndt)

// Extract TraceLoggingKeyword (OR'ed together).
#define _tlgKeywordVal(n, args) _tlgApplyArgs(_tlgKeywordVal, args)
#define _tlgKeywordVal_tlgIgnored(    ...                                                     )
#define _tlgKeywordVal_tlgKeyword(    eventKeyword                                            ) | (eventKeyword)
#define _tlgKeywordVal_tlgOpcode(     eventOpcode                                             )
#define _tlgKeywordVal_tlgEventTag(   eventTag                                                )
#define _tlgKeywordVal_tlgIdVersion(  eventId, eventVersion                                   )
#define _tlgKeywordVal_tlgLevel(      eventLevel                                              )
#define _tlgKeywordVal_tlgAuto(              value,                                        ndt)
#define _tlgKeywordVal_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgKeywordVal_tlgPackedData( ctype, pValue,   cbValue                                )
#define _tlgKeywordVal_tlgStruct(            fieldCount,         encoding,                 ndt)

// Extract TraceLoggingOpcode into a constant (last one wins).
#define _tlgOpcodeVal(n, args) _tlgApplyArgs(_tlgOpcodeVal, args)
#define _tlgOpcodeVal_tlgIgnored(    ...                                                     )
#define _tlgOpcodeVal_tlgKeyword(    eventKeyword                                            )
#define _tlgOpcodeVal_tlgOpcode(     eventOpcode                                             ) & 0u) | ((eventOpcode)
#define _tlgOpcodeVal_tlgEventTag(   eventTag                                                )
#define _tlgOpcodeVal_tlgIdVersion(  eventId, eventVersion                                   )
#define _tlgOpcodeVal_tlgLevel(      eventLevel                                              )
#define _tlgOpcodeVal_tlgAuto(              value,                                        ndt)
#define _tlgOpcodeVal_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgOpcodeVal_tlgPackedData( ctype, pValue,   cbValue                                )
#define _tlgOpcodeVal_tlgStruct(            fieldCount,         encoding,                 ndt)

// Extract TraceLoggingEventTag into a constant (last one wins).
#define _tlgEventTagVal(n, args) _tlgApplyArgs(_tlgEventTagVal, args)
#define _tlgEventTagVal_tlgIgnored(    ...                                                     )
#define _tlgEventTagVal_tlgKeyword(    eventKeyword                                            )
#define _tlgEventTagVal_tlgOpcode(     eventOpcode                                             )
#define _tlgEventTagVal_tlgEventTag(   eventTag                                                ) & 0u) | ((eventTag)
#define _tlgEventTagVal_tlgIdVersion(  eventId, eventVersion                                   )
#define _tlgEventTagVal_tlgLevel(      eventLevel                                              )
#define _tlgEventTagVal_tlgAuto(              value,                                        ndt)
#define _tlgEventTagVal_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgEventTagVal_tlgPackedData( ctype, pValue,   cbValue                                )
#define _tlgEventTagVal_tlgStruct(            fieldCount,         encoding,                 ndt)

// Extract TraceLoggingIdVersion into a constant (last one wins).
#define _tlgIdVersionVal(n, args) _tlgApplyArgs(_tlgIdVersionVal, args)
#define _tlgIdVersionVal_tlgIgnored(    ...                                                     )
#define _tlgIdVersionVal_tlgKeyword(    eventKeyword                                            )
#define _tlgIdVersionVal_tlgOpcode(     eventOpcode                                             )
#define _tlgIdVersionVal_tlgEventTag(   eventTag                                                )
#define _tlgIdVersionVal_tlgIdVersion(  eventId, eventVersion                                   ) & 0u) | (((eventId) | ((uint64_t)(eventVersion) << 32))
#define _tlgIdVersionVal_tlgLevel(      eventLevel                                              )
#define _tlgIdVersionVal_tlgAuto(              value,                                        ndt)
#define _tlgIdVersionVal_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgIdVersionVal_tlgPackedData( ctype, pValue,   cbValue                                )
#define _tlgIdVersionVal_tlgStruct(            fieldCount,         encoding,                 ndt)

// Extract TraceLoggingLevel into a constant (last one wins).
#define _tlgLevelVal(n, args) _tlgApplyArgs(_tlgLevelVal, args)
#define _tlgLevelVal_tlgIgnored(    ...                                                     )
#define _tlgLevelVal_tlgKeyword(    eventKeyword                                            )
#define _tlgLevelVal_tlgOpcode(     eventOpcode                                             )
#define _tlgLevelVal_tlgEventTag(   eventTag                                                )
#define _tlgLevelVal_tlgIdVersion(  eventId, eventVersion                                   )
#define _tlgLevelVal_tlgLevel(      eventLevel                                              ) & 0u) | ((eventLevel)
#define _tlgLevelVal_tlgAuto(              value,                                        ndt)
#define _tlgLevelVal_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgLevelVal_tlgPackedData( ctype, pValue,   cbValue                                )
#define _tlgLevelVal_tlgStruct(            fieldCount,         encoding,                 ndt)

// Declare struct fields needed for event field metadata.
#define _tlgInfoVars(n, args) _tlgApplyArgsN(_tlgInfoVars, n, args)
#define _tlgInfoVars_tlgIgnored(    n, ...                                                     )
#define _tlgInfoVars_tlgKeyword(    n, eventKeyword                                            )
#define _tlgInfoVars_tlgOpcode(     n, eventOpcode                                             )
#define _tlgInfoVars_tlgEventTag(   n, eventTag                                                )
#define _tlgInfoVars_tlgIdVersion(  n, eventId, eventVersion                                   )
#define _tlgInfoVars_tlgLevel(      n, eventLevel                                              )
#define _tlgInfoVars_tlgAuto(       n,        value,                                        ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; typename _tlgTypeMap<decltype(value)>::_tlg_PASTE2(_tlgTypeType,          _tlgNdtHasTag(ndt)) _tlgTy##n; _tlg_PASTE2(_tlgInfoVars_, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgValue(      n, ctype, value,              encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n;                    _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgStrNul(     n, ctype, pszValue,           encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgStrCch(     n, ctype, pchValue, cchValue, encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgBin(        n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgVArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n)
#define _tlgInfoVars_tlgCArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n) uint16_t _tlgValCount##n; \
    static_assert((cValues) > 0, "TraceLoggingFixedArray count must be greater than 0.");
#define _tlgInfoVars_tlgPackedField(n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n) \
    _tlg_AssertValidPackedMetadataEncoding(encoding);
#define _tlgInfoVars_tlgPackedMeta( n,                            encoding, format, hasFmt, ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, hasFmt, _tlgNdtHasTag(ndt))(n) \
    _tlg_AssertValidPackedMetadataEncoding(encoding);
#define _tlgInfoVars_tlgPackedData( n, ctype, pValue,   cbValue                                )
#define _tlgInfoVars_tlgStruct(     n,        fieldCount,         encoding,                 ndt) char _tlgName##n[sizeof(_tlgNdtName(ndt))]; uint8_t _tlgEnc##n; _tlg_PASTE3(_tlgInfoVars_, 1,      _tlgNdtHasTag(ndt))(n) \
    static_assert((fieldCount) > 0, "TraceLoggingStruct fieldCount must be greater than 0."); \
    static_assert((fieldCount) < 128, "TraceLoggingStruct fieldCount must be less than 128.");
#define _tlgInfoVars_0(n)
#define _tlgInfoVars_1(n)  uint16_t _tlgTag##n;
#define _tlgInfoVars_00(n)
#define _tlgInfoVars_10(n) uint8_t _tlgFmt##n;
#define _tlgInfoVars_01(n) uint8_t _tlgFmt##n;uint16_t _tlgTag##n;
#define _tlgInfoVars_11(n) uint8_t _tlgFmt##n;uint16_t _tlgTag##n;

// Declare struct field initializers needed for event field metadata.
#define _tlgInfoVals(n, args) _tlgApplyArgsN(_tlgInfoVals, n, args)
#define _tlgInfoVals_tlgIgnored(    n, ...                                                     )
#define _tlgInfoVals_tlgKeyword(    n, eventKeyword                                            )
#define _tlgInfoVals_tlgOpcode(     n, eventOpcode                                             )
#define _tlgInfoVals_tlgEventTag(   n, eventTag                                                )
#define _tlgInfoVals_tlgIdVersion(  n, eventId, eventVersion                                   )
#define _tlgInfoVals_tlgLevel(      n, eventLevel                                              )
#define _tlgInfoVals_tlgAuto(       n,        value,                                        ndt) , (_tlgNdtName(ndt)), _tlgTypeMap<decltype(value)>::_tlg_PASTE2(_tlgType, _tlgNdtHasTag(ndt)) _tlg_PASTE2(_tlgInfoVals_, _tlgNdtHasTag(ndt))(_tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgValue(      n, ctype, value,              encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgStrNul(     n, ctype, pszValue,           encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgStrCch(     n, ctype, pchValue, cchValue, encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgBin(        n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgVArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding|event_field_encoding_varray_flag _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgCArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding|event_field_encoding_carray_flag _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt)), (cValues)
#define _tlgInfoVals_tlgPackedField(n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgPackedMeta( n,                            encoding, format, hasFmt, ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, hasFmt, _tlgNdtHasTag(ndt))(format, _tlgNdtFtag(ndt))
#define _tlgInfoVals_tlgPackedData( n, ctype, pValue,   cbValue                                )
#define _tlgInfoVals_tlgStruct(     n,        fieldCount,         encoding,                 ndt) , (_tlgNdtName(ndt)), encoding                               _tlg_PASTE3(_tlgInfoVals_, 1,      _tlgNdtHasTag(ndt))(fieldCount, _tlgNdtFtag(ndt))
#define _tlgInfoVals_0(...)
#define _tlgInfoVals_1(ftag)          , (ftag)
#define _tlgInfoVals_00(format, ftag)
#define _tlgInfoVals_10(format, ftag) |128, format
#define _tlgInfoVals_01(format, ftag) |128, 128, (ftag)
#define _tlgInfoVals_11(format, ftag) |128, format|128, (ftag)

// Count the iovecs needed for event field data.
#define _tlgDataDescCount(n, args) _tlgApplyArgs(_tlgDataDescCount, args)
#define _tlgDataDescCount_tlgIgnored(    ...                                                     )
#define _tlgDataDescCount_tlgKeyword(    eventKeyword                                            )
#define _tlgDataDescCount_tlgOpcode(     eventOpcode                                             )
#define _tlgDataDescCount_tlgEventTag(   eventTag                                                )
#define _tlgDataDescCount_tlgIdVersion(  eventId, eventVersion                                   )
#define _tlgDataDescCount_tlgLevel(      eventLevel                                              )
#define _tlgDataDescCount_tlgAuto(              value,                                        ndt) +1
#define _tlgDataDescCount_tlgValue(      ctype, value,              encoding, format, hasFmt, ndt) +1
#define _tlgDataDescCount_tlgStrNul(     ctype, pszValue,           encoding, format, hasFmt, ndt) +1
#define _tlgDataDescCount_tlgStrCch(     ctype, pchValue, cchValue, encoding, format, hasFmt, ndt) +2
#define _tlgDataDescCount_tlgBin(        ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) +2
#define _tlgDataDescCount_tlgVArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) +2
#define _tlgDataDescCount_tlgCArray(     ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) +1
#define _tlgDataDescCount_tlgPackedField(ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) +1
#define _tlgDataDescCount_tlgPackedMeta(                            encoding, format, hasFmt, ndt)
#define _tlgDataDescCount_tlgPackedData( ctype, pValue,   cbValue                                ) +1
#define _tlgDataDescCount_tlgStruct(            fieldCount,         encoding,                 ndt)

// Populate the iovecs needed for event field data.
#ifdef __cplusplus

// For C++, we populate the iovecs in one expression.
// Temporary values are stashed in function parameters.
// This supports things like TraceLoggingString(ReturnStdString().c_str()).

#define _tlgBeginCppEval (
#define _tlgEndCppEval   )

#define _tlgDataDescCreate(n, args) _tlgApplyArgsN(_tlgDataDescCreate, n, args)
#define _tlgDataDescCreate_tlgIgnored(    n, ...                                                     )
#define _tlgDataDescCreate_tlgKeyword(    n, eventKeyword                                            )
#define _tlgDataDescCreate_tlgOpcode(     n, eventOpcode                                             )
#define _tlgDataDescCreate_tlgEventTag(   n, eventTag                                                )
#define _tlgDataDescCreate_tlgIdVersion(  n, eventId, eventVersion                                   )
#define _tlgDataDescCreate_tlgLevel(      n, eventLevel                                              )
#define _tlgDataDescCreate_tlgAuto(       n,       value,                                         ndt) \
    _tlgCppCreate1Auto(&_tlgVecs[_tlgIdx++], (value)),
#define _tlgDataDescCreate_tlgValue(      n, ctype, value,              encoding, format, hasFmt, ndt) \
    _tlgCppCreate1Val<ctype>(&_tlgVecs[_tlgIdx++], (value)),
#define _tlgDataDescCreate_tlgStrNul(     n, ctype, pszValue,           encoding, format, hasFmt, ndt) \
    _tlgCppCreate1ValsNul<ctype>(&_tlgVecs[_tlgIdx++], (pszValue)),
#define _tlgDataDescCreate_tlgStrCch(     n, ctype, pchValue, cchValue, encoding, format, hasFmt, ndt) \
    _tlgCppCreate2CountVals<ctype>(&_tlgVecs[_tlgIdx], (pchValue), (cchValue)), \
    _tlgIdx += 2,
#define _tlgDataDescCreate_tlgBin(        n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) \
    _tlgCppCreate2SizeVals<ctype>(&_tlgVecs[_tlgIdx], (pValue), (cbValue)), \
    _tlgIdx += 2,
#define _tlgDataDescCreate_tlgVArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) \
    _tlgCppCreate2CountVals<ctype>(&_tlgVecs[_tlgIdx], (pValues), (cValues)), \
    _tlgIdx += 2,
#define _tlgDataDescCreate_tlgCArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) \
    _tlgCppCreate1SizeVals<ctype>(&_tlgVecs[_tlgIdx++], (pValues), (cValues)*sizeof(ctype)),
#define _tlgDataDescCreate_tlgPackedField(n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) \
    _tlgCppCreate1SizeVals<ctype>(&_tlgVecs[_tlgIdx++], (pValue), (cbValue)*sizeof(char)),
#define _tlgDataDescCreate_tlgPackedMeta( n,                            encoding, format, hasFmt, ndt) \
    /* Nothing here. */
#define _tlgDataDescCreate_tlgPackedData( n, ctype, pValue,   cbValue                                ) \
    _tlgCppCreate1SizeVals<ctype>(&_tlgVecs[_tlgIdx++], (pValue), (cbValue)*sizeof(char)),
#define _tlgDataDescCreate_tlgStruct(     n,        fieldCount,         encoding,                 ndt) \
    /* Nothing here. */

#else // __cplusplus

// For C, we populate the iovecs in a series of statements.
// Temporary values are stashed in named variables.
// C has no destructors so this is safe.

#define _tlgBeginCppEval
#define _tlgEndCppEval

#define _tlgDataDescCreate(n, args) _tlgApplyArgsN(_tlgDataDescCreate, n, args)
#define _tlgDataDescCreate_tlgIgnored(    n, ...                                                     )
#define _tlgDataDescCreate_tlgKeyword(    n, eventKeyword                                            )
#define _tlgDataDescCreate_tlgOpcode(     n, eventOpcode                                             )
#define _tlgDataDescCreate_tlgEventTag(   n, eventTag                                                )
#define _tlgDataDescCreate_tlgIdVersion(  n, eventId, eventVersion                                   )
#define _tlgDataDescCreate_tlgLevel(      n, eventLevel                                              )
#define _tlgDataDescCreate_tlgValue(      n, ctype, value,              encoding, format, hasFmt, ndt) \
    ctype const _tlgTemp##n = (value); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx], &_tlgTemp##n, sizeof(ctype)); \
    _tlgIdx += 1;
#define _tlgDataDescCreate_tlgStrNul(     n, ctype, pszValue,           encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pszValue); \
    _tlgCreate1Sz_##ctype(&_tlgVecs[_tlgIdx], _tlgTemp##n); \
    _tlgIdx += 1;
#define _tlgDataDescCreate_tlgStrCch(     n, ctype, pchValue, cchValue, encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pchValue); \
    uint16_t const _tlgCount##n = (cchValue); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+0], &_tlgCount##n, 2); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+1], _tlgTemp##n, _tlgCount##n * sizeof(ctype)); \
    _tlgIdx += 2;
#define _tlgDataDescCreate_tlgBin(        n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pValue); \
    uint16_t const _tlgCount##n = (cbValue); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+0], &_tlgCount##n, 2); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+1], _tlgTemp##n, _tlgCount##n); \
    _tlgIdx += 2;
#define _tlgDataDescCreate_tlgVArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pValues); \
    uint16_t const _tlgCount##n = (cValues); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+0], &_tlgCount##n, 2); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx+1], _tlgTemp##n, _tlgCount##n * sizeof(ctype)); \
    _tlgIdx += 2;
#define _tlgDataDescCreate_tlgCArray(     n, ctype, pValues,  cValues,  encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pValues); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx], _tlgTemp##n, (cValues) * sizeof(ctype)); \
    _tlgIdx += 1;
#define _tlgDataDescCreate_tlgPackedField(n, ctype, pValue,   cbValue,  encoding, format, hasFmt, ndt) \
    ctype const* const _tlgTemp##n = (pValue); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx], _tlgTemp##n, (cbValue)); \
    _tlgIdx += 1;
#define _tlgDataDescCreate_tlgPackedMeta( n,                            encoding, format, hasFmt, ndt) \
    /* Nothing here. */
#define _tlgDataDescCreate_tlgPackedData( n, ctype, pValue,   cbValue                                ) \
    ctype const* const _tlgTemp##n = (pValue); \
    _tlgCreate1Vec(&_tlgVecs[_tlgIdx], _tlgTemp##n, (cbValue)); \
    _tlgIdx += 1;
#define _tlgDataDescCreate_tlgStruct(     n,        fieldCount,         encoding,                 ndt) \
    /* Nothing here. */

#endif // __cplusplus

#define _tlg_AssertValidPackedMetadataEncoding(encoding) \
    static_assert( \
        ((encoding)&event_field_encoding_value_mask) > event_field_encoding_struct && \
        ((encoding)&event_field_encoding_value_mask) < event_field_encoding_max && \
        ((encoding)|event_field_encoding_value_mask|event_field_encoding_varray_flag) == (event_field_encoding_value_mask|event_field_encoding_varray_flag), \
        "Invalid packed metadata encoding: " #encoding)

#define _tlgWriteImp(providerSymbol, eventName, pActivityId, pRelatedActivityId, ...) ({ \
    enum { \
        _tlgKeywordVal = (uint64_t)(0u _tlg_FOREACH(_tlgKeywordVal, __VA_ARGS__)), \
        _tlgOpcodeVal = (uint64_t)(0u _tlg_FOREACH(_tlgOpcodeVal, __VA_ARGS__)), \
        _tlgEventTagVal = (uint64_t)(0u _tlg_FOREACH(_tlgEventTagVal, __VA_ARGS__)), \
        _tlgIdVersionVal = (uint64_t)(0u _tlg_FOREACH(_tlgIdVersionVal, __VA_ARGS__)), \
        _tlgLevelVal = (uint64_t)(5u _tlg_FOREACH(_tlgLevelVal, __VA_ARGS__)) \
    }; \
    static tracepoint_state _tlgEvtState = TRACEPOINT_STATE_INIT; \
    static struct { \
        eventheader_extension _tlgExt; \
        struct { \
            char _tlgName[sizeof(eventName)]; \
            _tlg_FOREACH(_tlgInfoVars, __VA_ARGS__) \
        } __attribute__((packed)) _tlgDat; \
    } const _tlgMeta = { \
        { sizeof(_tlgMeta._tlgDat), eventheader_extension_kind_metadata }, \
        { (eventName) _tlg_FOREACH(_tlgInfoVals, __VA_ARGS__) } \
    }; \
    static eventheader_tracepoint const _tlgEvt = { \
        &_tlgEvtState, \
        &_tlgMeta._tlgExt, \
        { \
            eventheader_flag_default, \
            (uint64_t)(_tlgIdVersionVal) >> 32, \
            _tlgIdVersionVal & 0xFFFFFFFF, \
            _tlgEventTagVal, \
            _tlgOpcodeVal, \
            _tlgLevelVal \
        }, \
        _tlgKeywordVal \
    }; \
    static eventheader_tracepoint const* const _tlgEvtPtr \
        __attribute__((section("_tlgEventPtrs_" _tlg_STRINGIZE(providerSymbol)), used)) \
        = &_tlgEvt; \
    int _tlgWriteErr = 9 /*EBADF*/; \
    if (TRACEPOINT_ENABLED(&_tlgEvtState)) { \
        struct iovec _tlgVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT _tlg_FOREACH(_tlgDataDescCount, __VA_ARGS__)]; \
        unsigned _tlgIdx = EVENTHEADER_PREFIX_DATAVEC_COUNT; \
        _tlgBeginCppEval /* For C++, ensure no semicolons until after Write. */ \
        _tlg_FOREACH(_tlgDataDescCreate, __VA_ARGS__) \
        _tlgWriteErr = eventheader_write( \
            &_tlgEvt, \
            (pActivityId), (pRelatedActivityId), _tlgIdx, _tlgVecs) \
        _tlgEndCppEval; \
    } \
    _tlgWriteErr; })

#ifdef __EDG__
#pragma endregion
#endif

#endif // _included_TraceLoggingProvider_h
