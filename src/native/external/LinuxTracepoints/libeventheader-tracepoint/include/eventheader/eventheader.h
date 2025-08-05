// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_eventheader_h
#define _included_eventheader_h 1

#include <stddef.h>
#include <stdint.h>

#ifdef _WIN32
#define EVENTHEADER_LITTLE_ENDIAN 1
#else // _WIN32
#include <endian.h>
#define EVENTHEADER_LITTLE_ENDIAN (__BYTE_ORDER == __LITTLE_ENDIAN)
#endif // _WIN32

/*--EventHeader Events--------------------------------------------------------

EventHeader is a tracing convention layered on top of Linux Tracepoints.

To reduce the number of unique Tracepoint names tracked by the kernel, we
use a small number of Tracepoints to manage a larger number of events. All
events with the same attributes (provider name, severity level, category
keyword, etc.) will share one Tracepoint.

- This means we cannot enable/disable events individually. Instead, all events
  with similar attributes will be enabled/disabled as a group.
- This means we cannot rely on the kernel's Tracepoint metadata for event
  identity or event field names/types. Instead, all events contain a common
  header that provides event identity, core event attributes, and support for
  optional event attributes. The kernel's Tracepoint metadata is used only for
  the Tracepoint's name and to determine whether the event follows the
  EventHeader conventions.

We define a naming scheme to be used for the shared Tracepoints:

  TracepointName = ProviderName + '_' + 'L' + EventLevel + 'K' + EventKeyword +
                   [Options]

We define a common event layout to be used by all EventHeader events. The
event has a header, optional header extensions, and then the event data:

  Event = eventheader + [HeaderExtensions] + Data

We define a format to be used for header extensions:

  HeaderExtension = eventheader_extension + ExtensionData

We define a header extension to be used for activity IDs.

We define a header extension to be used for event metadata (event name, field
names, field types).

For use in the event metadata extension, we define a field type system that
supports scalar, string, binary, array, and struct.

Note that we assume that the Tracepoint name corresponding to the event is
available during event decoding. The event decoder obtains the provider name
and keyword for an event by parsing the event's Tracepoint name.

--Provider Names--------------------------------------------------------------

A provider is a component that generates events. Each event from a provider is
associated with a Provider Name that uniquely identifies the provider.

The provider name should be short, yet descriptive enough to minimize the
chance of collision and to help developers track down the component generating
the events. Hierarchical namespaces may be useful for provider names, e.g.
"MyCompany_MyOrg_MyComponent".

Restrictions:

- ProviderName may not contain ' ' or ':' characters.
- strlen(ProviderName + '_' + Attributes) must be less than
  EVENTHEADER_NAME_MAX (256) characters.
- Some event APIs (e.g. tracefs) might impose additional restrictions on
  tracepoint names. For best compatibility, use only ASCII identifier
  characters [A-Za-z0-9_] in provider names.

Event attribute semantics should be consistent within a given provider. While
some event attributes have generally-accepted semantics (e.g. level value 3
is defined below as "warning"), the precise semantics of the attribute values
are defined at the scope of a provider (e.g. different providers will use
different criteria for what constitutes a warning). In addition, some
attributes (tag, keyword) are completely provider-defined. All events with a
particular provider name should use consistent semantics for all attributes
(e.g. keyword bit 0x1 should have a consistent meaning for all events from a
particular provider but will mean something different for other providers).

--Tracepoint Names------------------------------------------------------------

A Tracepoint is registered with the kernel for each unique combination of
ProviderName + Attributes. This allows a larger number of distinct events to
be controlled by a smaller number of kernel Tracepoints while still allowing
events to be enabled/disabled at a reasonable granularity.

The Tracepoint name for an EventHeader event is defined as:

  ProviderName + '_' + 'L' + eventLevel + 'K' + eventKeyword + [Options]
  or printf("%s_L%xK%lx%s", providerName, eventLevel, eventKeyword, options),
  e.g. "MyProvider_L3K2a" or "OtherProvider_L5K0Gperf".

Event level is a uint8 value 1..255 indicating event severity, formatted as
lowercase hexadecimal, e.g. printf("L%x", eventLevel). The defined level values
are: 1 = critical error, 2 = error, 3 = warning, 4 = information, 5 = verbose.

Event keyword is a uint64 bitmask indicating event category membership,
formatted as lowercase hexadecimal, e.g. printf("K%lx", eventKeyword). Each
bit in the keyword corresponds to a provider-defined category, e.g. a provider
might define 0x2 = networking and 0x4 = I/O so that keyword value of 0x2|0x4 =
0x6 would indicate that an event is in both the networking and I/O categories.

Options (optional attributes) can be specified after the keyword attribute.
Each option consists of an uppercase ASCII letter (option type) followed by 0
or more ASCII digits or lowercase ASCII letters (option value). To support
consistent event names, the options must be sorted in alphabetical order, e.g.
"Aoption" should come before "Boption".

The currently defined options are:

- 'G' = provider Group name. Defines a group of providers. This can be used by
  event analysis tools to find all providers that generate a certain kind of
  information.

Restrictions:

- ProviderName may not contain ' ' or ':' characters.
- Tracepoint name must be less than EVENTHEADER_NAME_MAX (256)
  characters in length.
- Some event APIs (e.g. tracefs) might impose additional restrictions on
  tracepoint names. For best compatibility, use only ASCII identifier
  characters [A-Za-z0-9_] in provider names.

--Header-----------------------------------------------------------------------

Because multiple events may share a single Tracepoint, each event must contain
information to distinguish it from other events. To enable this, each event
starts with an EventHeader structure which contains information about the
event:

- flags: Bits indicating pointer size (32 or 64 bits), byte order
  (big-endian or little), and whether any header extensions are present.
- opcode: Indicates special event semantics e.g. "normal event",
  "activity start event", "activity end event".
- tag: Provider-defined 16-bit value. Can be used for anything.
- id: 16-bit stable event identifier, or 0 if no identifier is assigned.
- version: 8-bit event version, incremented for e.g. field type changes.
- level: 8-bit event severity level, 1 = critical .. 5 = verbose.
  (level value in event header must match the level in the Tracepoint name.)

If the extension flag is not set, the header is immediately followed by the
event payload.

If the extension flag is set, the header is immediately followed by one or more
header extensions. Each header extension has a 16-bit size, a 15-bit type code,
and a 1-bit flag indicating whether another header extension follows the
current extension. The final header extension is immediately followed by the
event payload.

The following header extensions are defined:

- Activity ID: Contains a 128-bit ID that can be used to correlate events. May
  also contain the 128-bit ID of the parent activity (typically used only for
  the first event of an activity).
- Metadata: Contains the event's metadata: event name, event attributes, field
  names, field attributes, and field types. Both simple (e.g. Int32, HexInt16,
  Float64, Char32, Uuid) and complex (e.g. NulTerminatedString8,
  CountedString16, Binary, Struct, Array) types are supported.
*/

/*
eventheader struct: Core metadata for an EventHeader event.

Each EventHeader event starts with an instance of the eventheader structure.
It contains core information recorded for every event to help with event
identification, filtering, and decoding.

If eventheader.flags has the extension bit set then the eventheader is followed
by one or more eventheader_extension blocks. Otherwise the eventheader is
followed by the event payload data.

If eventheader_extension.kind has the chain flag set then the
eventheader_extension block is followed immediately (no alignment/padding) by
another extension block. Otherwise it is followed immediately (no
alignment/padding) by the event payload data.

If there is a Metadata extension then it contains the event name, field names,
and field types needed to decode the payload data. Otherwise, the payload
decoding system is defined externally, i.e. you will use the provider name to
find the appropriate decoding manifest, then use the event's id+version to
find the decoding information within the manifest, then use that decoding
information to decode the event payload data.

For a particular event definition (i.e. for a particular event name, or for a
particular nonzero event id+version), the information in the eventheader (and
in the Metadata extension, if present) should be constant. For example, instead
of having a single event with a runtime-variable level, you should have a
distinct event definition (with distinct event name and/or distinct event id)
for each level.
*/
typedef struct eventheader {
    uint8_t  flags;   // eventheader_flags: pointer64, little_endian, extension.
    uint8_t  version; // If id != 0 then increment version when event layout changes.
    uint16_t id;      // Stable id for this event, or 0 if none.
    uint16_t tag;     // Provider-defined event tag, or 0 if none.
    uint8_t  opcode;  // event_opcode: info, start activity, stop activity, etc.
    uint8_t  level;   // event_level: critical, error, warning, info, verbose.
    // Followed by: eventheader_extension block(s), then event payload.
} eventheader;

/*
Type string for use in the DIAG_IOCSREG command string.
Use EVENTHEADER_FORMAT_COMMAND to generate the full command string.
*/
#define EVENTHEADER_COMMAND_TYPES "u8 eventheader_flags; u8 version; u16 id; u16 tag; u8 opcode; u8 level"

/*
eventheader_flags enum: Values for eventheader.flags.
*/
typedef enum eventheader_flags {
    eventheader_flag_none = 0,             // Pointer-32, big-endian, no extensions.
    eventheader_flag_pointer64 = 0x01,     // Pointer is 64 bits, not 32 bits.
    eventheader_flag_little_endian = 0x02, // Event uses little-endian, not big-endian.
    eventheader_flag_extension = 0x04,     // There is at least one eventheader_extension block.

    // Pointer-size and byte-order as appropriate for the target, no
    // eventheader_extension blocks present.
    eventheader_flag_default = 0
        | (EVENTHEADER_LITTLE_ENDIAN ? eventheader_flag_little_endian : 0)
        | (sizeof(void*) == 8 ? eventheader_flag_pointer64 : 0),

    // Pointer-size and byte-order as appropriate for the target, one or more
    // eventheader_extension blocks present.
    eventheader_flag_default_with_extension = eventheader_flag_default | eventheader_flag_extension,
} eventheader_flags;

/*
event_opcode enum: Values for eventheader.opcode. Special semantics for events.

Most events set opcode = info (0). Other opcode values add special semantics to
an event that help the event analysis tool with grouping related events. The
most frequently-used special semantics are activity-start and activity-stop.

To record an activity:

- Generate a new activity id. An activity id is a 128-bit value that must be
  unique within the trace. This can be a UUID or it can be generated by any
  other id-generation system that is unlikely to create the same value for any
  other activity id in the same trace.
- Write an event with opcode = activity_start and with an ActivityId header
  extension. The ActivityId extension should have the newly-generated activity
  id, followed by the id of a parent activity (if any). If there is a parent
  activity, the extension length will be 32; otherwise it will be 16.
- As appropriate, write any number of normal events (events with opcode set to
  something other than activity_start or activity_stop, e.g. opcode = info). To
  indicate that the events are part of the activity, each of these events
  should have an ActivityId header extension with the new activity id
  (extension length will be 16).
- When the activity ends, write an event with opcode = activity_stop and with
  an ActivityId header extension containing the activity id of the activity
  that is ending (extension length will be 16).
*/
typedef enum event_opcode {
    event_opcode_info = 0,        // Normal informational event.
    event_opcode_activity_start,  // Begins an activity (the first event to use a particular activity id).
    event_opcode_activity_stop,   // Ends an activity (the last event to use the particular activity id).
    event_opcode_collection_start,
    event_opcode_collection_stop,
    event_opcode_extension,
    event_opcode_reply,
    event_opcode_resume,
    event_opcode_suspend,
    event_opcode_send,
    event_opcode_receive = 0xf0,
} event_opcode;

/*
event_level enum: Values for eventheader.level.

0 is not a valid level. Values greater than 5 are permitted but are not
well-defined.
*/
typedef enum event_level {
    event_level_invalid = 0,
    event_level_critical_error,
    event_level_error,
    event_level_warning,
    event_level_information,
    event_level_verbose,
} event_level;

/*
eventheader_extension struct: Optional information about an EventHeader event.

If eventheader.flags has the extension bit set then the eventheader is
followed by one or more eventheader_extension blocks. Otherwise the eventheader
is followed by the event payload data.

If eventheader_extension.kind has the chain flag set then the
eventheader_extension block is followed immediately (no alignment/padding) by
another extension block. Otherwise it is followed immediately (no
alignment/padding) by the event payload data.
*/
typedef struct eventheader_extension {
    uint16_t size;
    uint16_t kind; // eventheader_extension_kind
    // Followed by size bytes of data. No padding/alignment.
} eventheader_extension;

/*
eventheader_extension_kind enum: Values for eventheader_extension.kind.
*/
typedef enum eventheader_extension_kind {
    eventheader_extension_kind_value_mask = 0x7FFF,

    /*
    If not set, this is the last extension block (event payload data follows).
    If set, this is not the last extension block (another extension block
    follows).
    */
    eventheader_extension_kind_chain_flag = 0x8000,

    /*
    Invalid extension kind.
    */
    eventheader_extension_kind_invalid = 0,

    /*
    Extension contains an event definition (i.e. event metadata).

    Event definition format:

    - char event_name[]; // Nul-terminated utf-8 string: "eventName{;attribName=attribValue}"
    - 0 or more field definition blocks, tightly-packed (no padding).

    Field definition block:

    - char field_name[]; // Nul-terminated utf-8 string: "fieldName{;attribName=attribValue}"
    - uint8_t encoding; // encoding is 0..31, with 3 flag bits.
    - uint8_t format; // Present if 0 != (encoding & 128). format is 0..127, with 1 flag bit.
    - uint16_t tag; // Present if 0 != (format & 128). Contains provider-defined value.
    - uint16_t array_length; // Present if 0 != (encoding & 32). Contains element count of constant-length array.

    Notes:

    - event_name and field_name may not contain any ';' characters.
    - event_name and field_name may be followed by attribute strings.
    - attribute string is: ';' + attribName + '=' + attribValue.
    - attribName may not contain any ';' or '=' characters.
    - Semicolons in attribValue must be escaped by doubling, e.g.
      "my;value" is escaped as "my;;value".
    - array_length may not be 0, i.e. constant-length arrays may not be empty.
    */
    eventheader_extension_kind_metadata,

    /*
    Extension contains activity id information.

    Any event that is part of an activity has an ActivityId extension.

    - Activity is started by an event with opcode = activity_start. The
      ActivityId extension for the start event must contain a newly-generated
      activity id and may optionally contain the parent activity id.
    - Activity may contain any number of normal events (opcode something other
      than activity_start or activity_stop). The ActivityId extension for each
      normal event must contain the id of the associated activity (otherwise
      it is not considered to be part of the activity).
    - Activity is ended by an event with opcode = activity_stop. The ActivityId
      extension for the stop event must contain the id of the activity that is
      ending.

    An activity id is a 128-bit value that is unique within this trace
    session. It may be a UUID. Since UUID generation can be non-trivial, this
    may also be a 128-bit LUID (locally-unique id) generated using any method
    that is unlikely to conflict with any other activity ids in the same trace.

    If extension.size == 16 then value is a 128-bit activity id.

    If extension.size == 32 then value is a 128-bit activity id followed by a
    128-bit related (parent) activity id.
    */
    eventheader_extension_kind_activity_id,
} eventheader_extension_kind;

/*
event_field_encoding enum: Values for the encoding byte of a field definition.

The low 5 bits of the encoding byte contain the field's encoding. The encoding
indicates how a decoder should determine the size of the field. It also
indicates a default format behavior that should be used if the field has no
format specified or if the specified format is 0, unrecognized, or unsupported.

The top 3 bits of the field encoding byte are flags:

- carray_flag indicates that this field is a constant-length array, with the
  element count specified as a 16-bit value in the event metadata. (The
  element count must not be 0, i.e. constant-length arrays may not be empty.)
- varray_flag indicates that this field is a variable-length array, with the
  element count specified as a 16-bit value in the event payload (immediately
  before the array elements, may be 0).
- chain_flag indicates that a format byte is present after the encoding byte.
  If chain_flag is not set, the format byte is omitted and is assumed to be 0.

Setting both carray_flag and varray_flag is invalid (reserved).
*/
typedef enum event_field_encoding {
    // Mask for the encoding types.
    event_field_encoding_value_mask = 0x1F,

    // Mask for the encoding flags.
    event_field_encoding_flag_mask = 0xE0,

    // Flag indicating that the field is a constant-length array, with a 16-bit
    // element count in the event metadata (must not be 0).
    event_field_encoding_carray_flag = 0x20,

    // Flag indicating that the field is a variable-length array, with a 16-bit
    // element count in the payload immediately before the elements (may be 0).
    event_field_encoding_varray_flag = 0x40,

    // Flag indicating that an event_field_format byte follows the
    // event_field_encoding byte.
    event_field_encoding_chain_flag = 0x80,

    // Invalid encoding value.
    event_field_encoding_invalid = 0,

    // 0-byte value, logically groups subsequent N fields, N = (format & 0x7F),
    // N must not be 0 (empty structs are not allowed).
    event_field_encoding_struct,

    // 1-byte value, default format unsigned_int.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, boolean, hex_bytes,
    // string8.
    event_field_encoding_value8,

    // 2-byte value, default format unsigned_int.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, boolean, hex_bytes,
    // string_utf, port.
    event_field_encoding_value16,

    // 4-byte value, default format unsigned_int.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, errno, pid, time,
    // boolean, float, hex_bytes, string_utf, IPv4.
    event_field_encoding_value32,

    // 8-byte value, default format unsigned_int.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, time, float,
    // hex_bytes.
    event_field_encoding_value64,

    // 16-byte value, default format hex_bytes.
    //
    // Usable formats: hex_bytes, uuid, ipv6.
    event_field_encoding_value128,

    // zero-terminated uint8[], default format string_utf.
    //
    // Usable formats: hex_bytes, string8, string_utf, string_utf_bom,
    // string_xml, string_json.
    event_field_encoding_zstring_char8,

    // zero-terminated uint16[], default format string_utf.
    //
    // Usable formats: hex_bytes, string_utf, string_utf_bom, string_xml,
    // string_json.
    event_field_encoding_zstring_char16,

    // zero-terminated uint32[], default format string_utf.
    //
    // Usable formats: hex_bytes, string_utf, string_utf_bom, string_xml,
    // string_json.
    event_field_encoding_zstring_char32,

    // uint16 Length followed by uint8 Data[Length], default format string_utf.
    // Also used for binary data (format hex_bytes).
    //
    // Usable formats: hex_bytes, String8, string_utf, string_utf_bom,
    // string_xml, string_json.
    event_field_encoding_string_length16_char8,

    // uint16 Length followed by uint16 Data[Length], default format
    // string_utf.
    //
    // Usable formats: hex_bytes, string_utf, string_utf_bom, string_xml,
    // string_json.
    event_field_encoding_string_length16_char16,

    // uint16 Length followed by uint32 Data[Length], default format
    // string_utf.
    //
    // Usable formats: hex_bytes, string_utf, string_utf_bom, string_xml,
    // string_json.
    event_field_encoding_string_length16_char32,

    // Invalid encoding value. Value will change in future versions of this
    // header.
    event_field_encoding_max,

    // long-sized value, default format unsigned_int.
    // This is an alias for either value32 or value64.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, Time, Float,
    // hex_bytes.
    event_field_encoding_value_long =
        sizeof(long) == 4 ? event_field_encoding_value32 :
        sizeof(long) == 8 ? event_field_encoding_value64 :
        event_field_encoding_invalid,

    // pointer-sized value, default format unsigned_int.
    // This is an alias for either value32 or value64.
    //
    // Usable formats: unsigned_int, signed_int, hex_int, Time, Float,
    // hex_bytes.
    event_field_encoding_value_ptr =
        sizeof(void*) == 4 ? event_field_encoding_value32 :
        sizeof(void*) == 8 ? event_field_encoding_value64 :
        event_field_encoding_invalid,

    // float-sized value, default format unsigned_int. (To treat as float,
    // use event_field_format_float.)
    // 
    // This is an alias for either value32 or value64.
    event_field_encoding_value_float =
        sizeof(float) == 4 ? event_field_encoding_value32 :
        sizeof(float) == 8 ? event_field_encoding_value64 :
        event_field_encoding_invalid,

    // double-sized value, default format unsigned_int. (To treat as float,
    // use event_field_format_float.)
    // 
    // This is an alias for either value32 or value64.
    event_field_encoding_value_double =
        sizeof(double) == 4 ? event_field_encoding_value32 :
        sizeof(double) == 8 ? event_field_encoding_value64 :
        event_field_encoding_invalid,

    // wchar-sized value, default format unsigned_int. (To treat as char, use
    // event_field_format_string_utf.)
    // 
    // This is an alias for either value16 or value32.
    event_field_encoding_value_wchar =
        sizeof(wchar_t) == 2 ? event_field_encoding_value16 :
        sizeof(wchar_t) == 4 ? event_field_encoding_value32 :
        event_field_encoding_invalid,

    // zero-terminated wchar_t[], default format string_utf.
    // 
    // This is an alias for either zstring_char16 or zstring_char32.
    event_field_encoding_zstring_wchar =
        sizeof(wchar_t) == 2 ? event_field_encoding_zstring_char16 :
        sizeof(wchar_t) == 4 ? event_field_encoding_zstring_char32 :
        event_field_encoding_invalid,

    // uint16 Length followed by uint16 Data[Length], default format
    // string_utf.
    // 
    // This is an alias for either string_length16_char16 or
    // string_length16_char32.
    event_field_encoding_string_length16_wchar =
        sizeof(wchar_t) == 2 ? event_field_encoding_string_length16_char16 :
        sizeof(wchar_t) == 4 ? event_field_encoding_string_length16_char32 :
        event_field_encoding_invalid,

} event_field_encoding;

#if defined(__cplusplus) || defined(static_assert)
static_assert(event_field_encoding_max <= event_field_encoding_carray_flag, "Too many encodings.");
static_assert(event_field_encoding_invalid != event_field_encoding_value_long, "Unsupported sizeof(long).");
static_assert(event_field_encoding_invalid != event_field_encoding_value_ptr, "Unsupported sizeof(void*).");
static_assert(event_field_encoding_invalid != event_field_encoding_value_float, "Unsupported sizeof(float).");
static_assert(event_field_encoding_invalid != event_field_encoding_value_double, "Unsupported sizeof(double).");
static_assert(event_field_encoding_invalid != event_field_encoding_value_wchar, "Unsupported sizeof(wchar_t).");
#endif

/*
event_field_format enum: Values for the format byte of a field definition.

The low 7 bits of the format byte contain the field's format.
In the case of the Struct encoding, the low 7 bits of the format byte contain
the number of logical fields in the struct (which must not be 0).

The top bit of the field format byte is the FlagChain. If set, it indicates
that a field tag (uint16) is present after the format byte. If not set, the
field tag is not present and is assumed to be 0.
*/
typedef enum event_field_format {
    event_field_format_value_mask = 0x7F,
    event_field_format_chain_flag = 0x80, // A field tag (uint16) follows the format byte.

    event_field_format_default = 0, // Use the default format of the encoding.
    event_field_format_unsigned_int,    // unsigned integer, event byte order. Use with Value8..Value64 encodings.
    event_field_format_signed_int,      // signed integer, event byte order. Use with Value8..Value64 encodings.
    event_field_format_hex_int,         // hex integer, event byte order. Use with Value8..Value64 encodings.
    event_field_format_errno,       // errno, event byte order. Use with Value32 encoding.
    event_field_format_pid,         // process id, event byte order. Use with Value32 encoding.
    event_field_format_time,        // signed integer, event byte order, seconds since 1970. Use with Value32 or Value64 encodings.
    event_field_format_boolean,     // 0 = false, 1 = true, event byte order. Use with Value8..Value32 encodings.
    event_field_format_float,       // floating point, event byte order. Use with Value32..Value64 encodings.
    event_field_format_hex_bytes,   // binary, decoded as hex dump of bytes. Use with any encoding.
    event_field_format_string8,     // 8-bit char string, unspecified character set (usually treated as ISO-8859-1 or CP-1252). Use with Value8 and Char8 encodings.
    event_field_format_string_utf,   // UTF string, event byte order, code unit size based on encoding. Use with Value16..Value32 and Char8..Char32 encodings.
    event_field_format_string_utf_bom,// UTF string, BOM used if present, otherwise behaves like string_utf. Use with Char8..Char32 encodings.
    event_field_format_string_xml,   // XML string, otherwise behaves like string_utf_bom. Use with Char8..Char32 encodings.
    event_field_format_string_json,  // JSON string, otherwise behaves like string_utf_bom. Use with Char8..Char32 encodings.
    event_field_format_uuid,        // UUID, network byte order (RFC 4122 format). Use with Value128 encoding.
    event_field_format_port,        // IP port, network byte order (in_port_t layout). Use with Value16 encoding.
    event_field_format_ipv4,        // IPv4 address, network byte order (in_addr layout). Use with Value32 encoding.
    event_field_format_ipv6,        // IPv6 address, in6_addr layout. Use with Value128 encoding.
} event_field_format;

enum {
    // Maximum length of a Tracepoint name "ProviderName_Attributes", including nul termination.
    EVENTHEADER_NAME_MAX = 256,

    // Maximum length needed for a DIAG_IOCSREG command "ProviderName_Attributes CommandTypes".
    EVENTHEADER_COMMAND_MAX = EVENTHEADER_NAME_MAX + sizeof(EVENTHEADER_COMMAND_TYPES)
};

/*
Macro EVENTHEADER_FORMAT_TRACEPOINT_NAME generates the Tracepoint name for an
event, tracepointName = "ProviderName_LnKnnnOptions":

    char tracepointName[EVENTHEADER_NAME_MAX];
    EVENTHEADER_FORMAT_TRACEPOINT_NAME(
        tracepointName, sizeof(tracepointName),
        providerName, eventLevel, eventKeyword, options);

Returns the value returned by snprintf:
- return >= 0 && return < tracepointNameMax indicates success.
- return < 0 || return >= tracepointNameMax indicates error.

Requires: #include <stdio.h>, #include <inttypes.h>
*/
#define EVENTHEADER_FORMAT_TRACEPOINT_NAME( \
    tracepointName, tracepointNameMax, providerName, eventLevel, eventKeyword, options) \
({ \
    /* Put arguments into temp vars for type-checking: */ \
    char const* const _providerName = (providerName); \
    uint8_t const _eventLevel = (eventLevel); \
    uint64_t const _eventKeyword = (eventKeyword); \
    char const* const _options = (options); \
    snprintf(tracepointName, tracepointNameMax, "%s_L%xK%" PRIx64 "%s", \
        _providerName, _eventLevel, _eventKeyword, _options ? _options : ""); \
}) \

/*
Macro EVENTHEADER_FORMAT_COMMAND generates the DIAG_IOCSREG command string for
an event, command = "ProviderName_LnKnnnOptions CommandTypes":

    char command[EVENTHEADER_COMMAND_MAX];
    EVENTHEADER_FORMAT_COMMAND(
        command, sizeof(command),
        providerName, eventLevel, eventKeyword, options);

Returns the value returned by snprintf:
- return >= 0 && return < commandMax indicates success.
- return < 0 || return >= commandMax indicates error.

Requires: #include <stdio.h>, #include <inttypes.h>
*/
#define EVENTHEADER_FORMAT_COMMAND( \
    command, commandMax, providerName, eventLevel, eventKeyword, options) \
({ \
    /* Put arguments into temp vars for type-checking: */ \
    char const* const _providerName = (providerName); \
    uint8_t const _eventLevel = (eventLevel); \
    uint64_t const _eventKeyword = (eventKeyword); \
    char const* const _options = (options); \
    snprintf(command, commandMax, "%s_L%xK%" PRIx64 "%s %s", \
        _providerName, _eventLevel, _eventKeyword, _options ? _options : "", \
        EVENTHEADER_COMMAND_TYPES); \
}) \

#endif // _included_eventheader_h
