// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/TracepointSpec.h>
#include <tracepoint/TracepointName.h>
#include <assert.h>

using namespace tracepoint_control;

static bool
AsciiIsSpace(char ch)
{
    return ch == ' ' || ('\t' <= ch && ch <= '\r');
}

static size_t
CountLeadingWhitespace(std::string_view str)
{
    size_t pos;
    for (pos = 0; pos != str.size(); pos += 1)
    {
        if (!AsciiIsSpace(str[pos]))
        {
            break;
        }
    }
    return pos;
}

TracepointSpec::TracepointSpec(std::string_view const specString) noexcept
{
    bool identifier;
    bool hasFields = false;

    /*
    Cases:
    1. Empty
    2. '#' ANY*
    3. ':' WS* EventName
    4. ':' WS* SystemName ':' EventName
    5. EventName (WS Fields*)?
    6. SystemName ':' EventName (':' Flags)? (WS Fields*)?
    */

    auto const trimmed = Trim(specString);
    Trimmed = trimmed;

    size_t pos = 0;
    if (pos == trimmed.size())
    {
        Kind = TracepointSpecKind::Empty;
        return; // Case 1
    }
    else if (trimmed[pos] == '#')
    {
        Kind = TracepointSpecKind::Empty;
        return; // Case 2
    }
    else if (trimmed[pos] == ':')
    {
        size_t startPos;

        // Skip ':'
        pos += 1;

        // Skip WS*
        pos += CountLeadingWhitespace(trimmed.substr(pos));

        // Skip Name
        for (startPos = pos;; pos += 1)
        {
            if (pos == trimmed.size())
            {
                SystemName = UserEventsSystemName;
                EventName = trimmed.substr(startPos, pos - startPos); // Might be empty.
                identifier = true;
                goto Done; // Case 3.
            }

            if (AsciiIsSpace(trimmed[pos]))
            {
                SystemName = UserEventsSystemName;
                EventName = trimmed.substr(startPos, pos - startPos);
                Kind = TracepointSpecKind::ErrorIdentifierCannotHaveFields;
                return;
            }

            if (trimmed[pos] == ':')
            {
                break;
            }
        }

        // End of name - ':'.

        SystemName = trimmed.substr(startPos, pos - startPos); // Might be empty.

        // Skip ':'
        pos += 1;

        // Skip Name
        for (startPos = pos; pos != trimmed.size(); pos += 1)
        {
            if (AsciiIsSpace(trimmed[pos]))
            {
                EventName = trimmed.substr(startPos, pos - startPos);
                Kind = TracepointSpecKind::ErrorIdentifierCannotHaveFields;
                return;
            }

            if (trimmed[pos] == ':')
            {
                EventName = trimmed.substr(startPos, pos - startPos);
                Kind = TracepointSpecKind::ErrorIdentifierCannotHaveFlags;
                return;
            }
        }

        EventName = trimmed.substr(startPos, pos - startPos); // Might be empty.
        identifier = true;
        goto Done; // Case 4.
    }
    else
    {
        size_t startPos;

        assert(pos != trimmed.size());
        assert(!AsciiIsSpace(trimmed[pos]));
        assert(trimmed[pos] != ':');
        startPos = pos;
        pos += 1;

        // Skip Name
        for (;; pos += 1)
        {
            if (pos == trimmed.size())
            {
                SystemName = UserEventsSystemName;
                EventName = trimmed.substr(startPos, pos - startPos);
                identifier = false;
                goto Done; // Case 5, no fields.
            }

            if (AsciiIsSpace(trimmed[pos]))
            {
                SystemName = UserEventsSystemName;
                EventName = trimmed.substr(startPos, pos - startPos);
                goto DefinitionFields; // Case 5, fields.
            }

            if (trimmed[pos] == ':')
            {
                break;
            }
        }

        // End of name - ':'.

        SystemName = trimmed.substr(startPos, pos - startPos);

        // Skip ':'
        pos += 1;

        // Skip Name
        for (startPos = pos;; pos += 1)
        {
            if (pos == trimmed.size())
            {
                EventName = trimmed.substr(startPos, pos - startPos); // Might be empty.
                identifier = false;
                goto Done; // Case 6, no fields.
            }

            if (AsciiIsSpace(trimmed[pos]))
            {
                EventName = trimmed.substr(startPos, pos - startPos); // Might be empty.
                goto DefinitionFields; // Case 6, fields.
            }

            if (trimmed[pos] == ':')
            {
                break;
            }
        }

        EventName = trimmed.substr(startPos, pos - startPos); // Might be empty.

        // Skip ':'
        pos += 1;

        // Skip Name
        for (startPos = pos;; pos += 1)
        {
            if (pos == trimmed.size())
            {
                Flags = trimmed.substr(startPos, pos - startPos); // Might be empty.
                identifier = false;
                goto Done; // Case 6, no fields.
            }

            if (AsciiIsSpace(trimmed[pos]))
            {
                Flags = trimmed.substr(startPos, pos - startPos); // Might be empty.
                goto DefinitionFields; // Case 6, fields.
            }

            if (trimmed[pos] == ':')
            {
                Flags = trimmed.substr(startPos, pos - startPos);
                Kind = TracepointSpecKind::ErrorDefinitionCannotHaveColonAfterFlags;
                return;
            }
        }

    DefinitionFields:

        // Skip WS*
        assert(AsciiIsSpace(trimmed[pos]));
        pos += CountLeadingWhitespace(trimmed.substr(pos));

        Fields = trimmed.substr(pos); // Might have trailing semicolon.
        while (!Fields.empty() &&
            (Fields.back() == ';' || AsciiIsSpace(Fields.back())))
        {
            Fields.remove_suffix(1);
        }

        hasFields = true;
        identifier = false;
        goto Done;
    }

Done:

    if (!EventNameIsValid(EventName))
    {
        if (EventName.empty())
        {
            Kind = identifier
                ? TracepointSpecKind::ErrorIdentifierEventNameEmpty
                : TracepointSpecKind::ErrorDefinitionEventNameEmpty;
        }
        else
        {
            Kind = identifier
                ? TracepointSpecKind::ErrorIdentifierEventNameInvalid
                : TracepointSpecKind::ErrorDefinitionEventNameInvalid;
        }
    }
    else if (!SystemNameIsValid(SystemName))
    {
        if (SystemName.empty())
        {
            Kind = identifier
                ? TracepointSpecKind::ErrorIdentifierSystemNameEmpty
                : TracepointSpecKind::ErrorDefinitionSystemNameEmpty;
        }
        else
        {
            Kind = identifier
                ? TracepointSpecKind::ErrorIdentifierSystemNameInvalid
                : TracepointSpecKind::ErrorDefinitionSystemNameInvalid;
        }
    }
    else if (identifier)
    {
        Kind = TracepointSpecKind::Identifier;
    }
    else if (hasFields)
    {
        Kind = TracepointSpecKind::Definition;
    }
    else if (!EventHeaderEventNameIsValid(EventName))
    {
        Kind = TracepointSpecKind::ErrorEventHeaderDefinitionEventNameInvalid;
    }
    else
    {
        Kind = TracepointSpecKind::EventHeaderDefinition;
    }
}

std::string_view
TracepointSpec::Trim(std::string_view const str) noexcept
{
    size_t startPos;
    size_t endPos;

    for (startPos = 0; startPos != str.size(); startPos += 1)
    {
        if (!AsciiIsSpace(str[startPos]))
        {
            break;
        }
    }

    for (endPos = str.size(); endPos != startPos; endPos -= 1)
    {
        if (!AsciiIsSpace(str[endPos - 1]))
        {
            break;
        }
    }

    return str.substr(startPos, endPos - startPos);
}
