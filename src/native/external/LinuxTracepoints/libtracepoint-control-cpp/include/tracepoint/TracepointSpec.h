// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
A TracepointSpec stores a view of the information needed to add a tracepoint to
a trace collection session. It may or may not have the information needed to
pre-register the tracepoint.
*/

#pragma once
#ifndef _included_TracepointSpec_h
#define _included_TracepointSpec_h 1

#include <string_view>

namespace tracepoint_control
{
    /*
    Value indicating whether the TracepointSpec is empty, an identifier, a
    definition, an EventHeader definition, or an error.
    */
    enum class TracepointSpecKind : unsigned char
    {
        Empty,          // Empty spec, all whitespace, or started with "#" (comment).
        Identifier,     // Name only, event cannot be pre-registered.
        Definition,     // Name plus field information, event can be pre-registered.
        EventHeaderDefinition, // EventHeader name, event can be pre-registered.

        ErrorIdentifierCannotHaveFields,
        ErrorIdentifierCannotHaveFlags,
        ErrorDefinitionCannotHaveColonAfterFlags,
        ErrorIdentifierEventNameEmpty,
        ErrorDefinitionEventNameEmpty,
        ErrorIdentifierEventNameInvalid,
        ErrorDefinitionEventNameInvalid,
        ErrorEventHeaderDefinitionEventNameInvalid,
        ErrorIdentifierSystemNameEmpty,
        ErrorDefinitionSystemNameEmpty, // Unreachable via specString.
        ErrorIdentifierSystemNameInvalid,
        ErrorDefinitionSystemNameInvalid,
    };

    /*
    A TracepointSpec stores the information needed to add a tracepoint to a
    trace collection session.

    The TracepointSpec is either a tracepoint identifier (name only, not enough
    information to pre-register the tracepoint) or a tracepoint definition
    (enough information to pre-register the tracepoint if not already
    registered). A tracepoint definition can be either a normal tracepoint
    definition (explicitly-specified fields) or an EventHeader tracepoint
    definition (implicit well-known fields).
    */
    struct TracepointSpec
    {
        std::string_view Trimmed = {};    // Input with leading/trailing whitespace removed = Trim(specString).
        std::string_view SystemName = {}; // e.g. "user_events".
        std::string_view EventName = {};  // e.g. "MyEvent" or "MyProvider_L2K1Gmygroup".
        std::string_view Flags = {};      // e.g. "" or "flag1,flag2".
        std::string_view Fields = {};     // e.g. "" or "u32 Field1; u16 Field2".
        TracepointSpecKind Kind = {};     // Empty, Identifier, Definition, EventHeaderDefinition, or Error.

        /*
        Initializes an empty TracepointSpec.
        */
        constexpr
        TracepointSpec() noexcept = default;

        /*
        Initializes a TracepointSpec from the specified string.
        Leading and trailing whitespace is always ignored.
        If SystemName is not specified, "user_events" is assumed.

        Accepted inputs:

        * Empty string, or string starting with "#" --> Kind = Empty.

        * Leading colon --> Kind = Identifier.

          Examples: ":EventName", ":SystemName:EventName".

        * No leading colon, fields present --> Kind = Definition.

          Use a trailing space + semicolon to indicate no fields. The space is
          required because semicolons are valid in event names.

          Examples: "EventName ;", "SystemName:EventName:Flags Field1; Field2".

        * No leading colon, no fields present --> Kind = EventHeaderDefinition.

          Examples: "ProviderName_L1K1", or "SystemName:ProviderName_L1KffGgroup:Flags".
        */
        explicit
        TracepointSpec(std::string_view const specString) noexcept;

        /*
        Returns specString with all leading and trailing whitespace removed.
        Uses the same definition of whitespace as the TracepointSpec constructor.
        */
        static std::string_view
        Trim(std::string_view const specString) noexcept;
    };
}
// namespace tracepoint_control

#endif // _included_TracepointSpec_h
