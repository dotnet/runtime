// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
TracepointName is a view of a SystemName and an EventName.
*/

#pragma once
#ifndef _included_TracepointName_h
#define _included_TracepointName_h 1

#include <string_view>

namespace tracepoint_control
{
    /*
    The name of the "user_events" system.
    */
    static constexpr std::string_view UserEventsSystemName = std::string_view("user_events", 11);

    /*
    Maximum length of a SystemName = 255. (Does not count nul-termination.)
    */
    static constexpr unsigned SystemNameMaxSize = 255;

    /*
    Maximum length of an EventName = 255. (Does not count nul-termination.)
    */
    static constexpr unsigned EventNameMaxSize = 255;

    /*
    Returns true if the specified string is a valid tracepoint system name.

    At present, this returns true if:
    - systemName is not empty.
    - systemName.size() <= SystemNameMaxSize.
    - systemName does not contain nul, space, slash, or colon.
    */
    constexpr bool
    SystemNameIsValid(std::string_view systemName) noexcept
    {
        return systemName.size() > 0
            && systemName.size() <= SystemNameMaxSize
            && systemName.find('\0') == std::string_view::npos
            && systemName.find(' ') == std::string_view::npos
            && systemName.find('/') == std::string_view::npos
            && systemName.find(':') == std::string_view::npos;
    }

    /*
    Returns true if the specified string is a valid tracepoint event name.

    At present, this returns true if:
    - eventName is not empty.
    - eventName.size() <= EventNameMaxSize.
    - eventName does not contain nul, space, slash, or colon.
    */
    constexpr bool
    EventNameIsValid(std::string_view eventName) noexcept
    {
        return eventName.size() > 0
            && eventName.size() <= EventNameMaxSize
            && eventName.find('\0') == std::string_view::npos
            && eventName.find(' ') == std::string_view::npos
            && eventName.find('/') == std::string_view::npos
            && eventName.find(':') == std::string_view::npos;
    }

    /*
    Returns true if the specified string is a valid EventHeader tracepoint name,
    e.g. "MyComponent_MyProvider_L1K2e" or "MyComponent_MyProv_L5K1Gmyprovider".

    A valid EventHeader tracepoint name is a valid tracepoint event name that ends
    with a "_LxKx..." suffix, where "x" is 1 or more lowercase hex digits and "..."
    is 0 or more ASCII letters or digits.
    */
    static constexpr bool
    EventHeaderEventNameIsValid(std::string_view eventName) noexcept
    {
        auto const eventNameSize = eventName.size();

        if (eventNameSize < 5 || // 5 = "_L1K1".size()
            !EventNameIsValid(eventName))
        {
            return false;
        }

        auto i = eventName.rfind('_');
        if (i > eventNameSize - 5 || // 5 = "_L1K1".size()
            eventName[i + 1] != 'L')
        {
            // Does not end with "_L...".
            return false;
        }

        i += 2; // Skip "_L".

        // Skip level value (lowercase hex digits).
        auto const levelStart = i;
        for (; i != eventNameSize; i += 1)
        {
            auto const ch = eventName[i];
            if ((ch < '0' || '9' < ch) && (ch < 'a' || 'f' < ch))
            {
                break;
            }
        }

        if (levelStart == i)
        {
            // Does not end with "_Lx...".
            return false;
        }

        if (i == eventNameSize || eventName[i] != 'K')
        {
            // Does not end with "_LxK...".
            return false;
        }

        i += 1; // Skip "K"

        // Skip keyword value (lowercase hex digits).
        auto const keywordStart = i;
        for (; i != eventNameSize; i += 1)
        {
            auto const ch = eventName[i];
            if ((ch < '0' || '9' < ch) && (ch < 'a' || 'f' < ch))
            {
                break;
            }
        }

        if (keywordStart == i)
        {
            // Does not end with "_LxKx...".
            return false;
        }

        // If there are attributes, validate them.
        if (i != eventNameSize)
        {
            if (eventName[i] < 'A' || 'Z' < eventName[i])
            {
                // Invalid attribute lead char.
                return false;
            }

            // Skip attributes and their values.
            for (; i != eventNameSize; i += 1)
            {
                auto const ch = eventName[i];
                if ((ch < '0' || '9' < ch) &&
                    (ch < 'A' || 'Z' < ch) &&
                    (ch < 'a' || 'z' < ch))
                {
                    // Invalid attribute character.
                    return false;
                }
            }
        }

        return true;
    }

    /*
    A TracepointName is a string identifier for a tracepoint on a system.
    It contains two parts: SystemName and EventName.

    Construct a TracepointName by one of the following:
    - TracepointName("SystemName", "EventName")
    - TracepointName("SystemName:EventName")
    - TracepointName("SystemName/EventName")
    - TracepointName("EventName") // Uses SystemName = "user_events"
    */
    struct TracepointName
    {
        /*
        SystemName is the name of a subdirectory of
        "/sys/kernel/tracing/events" such as "user_events" or "ftrace".
        */
        std::string_view SystemName;

        /*
        EventName is the name of a subdirectory of
        "/sys/kernel/tracing/events/SystemName" such as "MyEvent" or "function".
        */
        std::string_view EventName;

        /*
        Create a TracepointName from systemName and eventName, e.g.
        TracepointName("user_events", "MyEvent_L1K1").

        - systemName is the name of a subdirectory of
          "/sys/kernel/tracing/events" such as "user_events" or "ftrace".
        - eventName is the name of a subdirectory of
          "/sys/kernel/tracing/events/systemName", e.g. "MyEvent" or
          "function".
        */
        constexpr
        TracepointName(std::string_view systemName, std::string_view eventName) noexcept
            : SystemName(systemName)
            , EventName(eventName)
        {
            return;
        }

        /*
        Create a TracepointName from a combined "systemName:eventName" or
        "systemName/eventName" string. If the string does not contain ':' or '/',
        the SystemName is assumed to be "user_events".
        */
        explicit constexpr
        TracepointName(std::string_view systemAndEventName) noexcept
            : SystemName()
            , EventName()
        {
            auto const splitPos = systemAndEventName.find_first_of(":/", 0, 2);
            if (splitPos == systemAndEventName.npos)
            {
                SystemName = UserEventsSystemName;
                EventName = systemAndEventName;
            }
            else
            {
                SystemName = systemAndEventName.substr(0, splitPos);
                EventName = systemAndEventName.substr(splitPos + 1);
            }
        }

        /*
        Require SystemName and EventName to always be specified.
        */
        TracepointName() = delete;

        /*
        Returns true if SystemName is a valid tracepoint system name and EventName
        is a valid tracepoint event name.
        */
        constexpr bool
        IsValid() const noexcept
        {
            return SystemNameIsValid(SystemName) && EventNameIsValid(EventName);
        }

        /*
        Returns true if SystemName is a valid tracepoint system name and EventName
        is a valid EventHeader tracepoint event name.
        */
        constexpr bool
        IsValidEventHeader() const noexcept
        {
            return SystemNameIsValid(SystemName) && EventHeaderEventNameIsValid(EventName);
        }
    };
}
// namespace tracepoint_control

#endif // _included_TracepointName_h
