// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
C++ API for runtime-specified eventheader-encoded Linux Tracepoints via
libtracepoint

This API is intended for use when the set of events to be logged is not known
at compile-time, e.g. when implementing a middle-layer for a high-level tracing
API such as OpenTelemetry. This API should not be directly used by developers
instrumenting their own code because it is less user-friendly and less efficient
than alternatives like TraceLoggingProvider.h.

Basic usage of this API:

- Create a Provider object with a name and an optional group.
- Use the Provider to get the EventSet with the level and keyword you need.
- As an optimization, use Enabled(eventSet) to determine whether anybody
  is listening for a particular provider + event level + event keyword
  combination. If nobody is listening, you should skip the remaining steps
  because there is no need to build and write an event that nobody will
  receive.
- Use an EventBuilder to build and write the event:
  - Create an EventBuilder.
  - Call eventBuilder.Reset(...) to set the event name and to set the event
    tag (if any).
  - Call eventBuilder.AddValue(...) and similar methods to add fields to the
    event or to configure the event's other attributes (e.g. opcode, id,
    version).
  - Call eventBuilder.Write(eventSet, ...) to send the event to the system
    with the eventSet's provider, level, and keyword.

Notes:

- EventBuilder is reusable. If you need to generate several events, you may
  get a small performance benefit by reusing one EventBuilder rather than
  using a new EventBuilder for each event.
- Events are limited in size (event size = headers + metadata + data). The
  kernel will ignore any event that is larger than 64KB.
- All event sets for a provider will become disabled when the provider is
  destroyed or when you call provider.Unregister().
- The Provider object is not thread-safe, but the EventSet object is
  thread-safe. Possible strategies for multi-threaded programs might include:
  - Have a reader-writer lock for the provider. Take an exclusive lock for
    non-const operations like RegisterSet() and Unregister(). Take a shared
    lock for other provider operations like FindSet().
  - Create the provider and do all of the necessary RegisterSet() calls
    before any other threads start using it. Then you can call the const
    methods like FindSet() as needed on any thread without any lock as long
    as nobody is calling any non-const methods.
  - Use your own thread-safe data structure to keep track of all of the
    EventSets you need. Take a lock if you ever need to register a new set.
- Each event set maps to one tracepoint name, e.g. if the provider name is
  "MyCompany_MyComponent", level is verbose (5), and keyword is 0x1f, the
  event set will correspond to a tracepont named
  "user_events:MyCompany_MyComponent_L5K1f".
- Collect events to a file using a tool such as "perf", e.g.
  "perf record -k monotonic -e user_events:MyCompany_MyComponent_L5K1f".
- Decode events using a tool such as "decode-perf" (from the tools in the
  libeventheader-decode-cpp library).
*/

#pragma once
#ifndef _included_EventHeaderDynamic_h
#define _included_EventHeaderDynamic_h 1

#include "eventheader-tracepoint.h"
#include <assert.h>
#include <string.h>
#include <memory>
#include <string_view>
#include <type_traits>
#include <unordered_map>
#include <vector>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _In_opt_
#define _In_opt_
#endif
#ifndef _In_reads_bytes_opt_
#define _In_reads_bytes_opt_(size)
#endif
#ifndef _In_reads_
#define _In_reads_(count)
#endif
#ifndef _Success_
#define _Success_(condition)
#endif
#ifndef _Outptr_result_bytebuffer_
#define _Outptr_result_bytebuffer_(size)
#endif
#ifndef _Out_opt_
#define _Out_opt_
#endif

namespace ehd
{
    /*
    Represents a group of events with the same provider, level, and keyword.

    Each EventSet corresponds to one tracepoint name. For example, the EventSet
    with provider name "MyCompany_MyComponent", level verbose (5), and keyword
    0x1f would correspond to a tracepoint named
    "user_events:MyCompany_MyComponent_L5K1f".

    Get an EventSet by calling provider.RegisterSet(level, keyword) or
    provider.FindSet(level, keyword).

    Use an EventSet by calling Enabled(eventSet) or
    eventBuilder.Write(eventSet, ...);
    */
    class EventSet
    {
        friend class Provider; // Forward declaration
        friend class EventBuilder; // Forward declaration
        tracepoint_state m_tracepointState;
        uint8_t m_level;
        int m_errno;

    public:

        EventSet(EventSet const&) = delete;
        void operator=(EventSet const&) = delete;

        /*
        Creates an inactive (unregistered) EventSet. The returned event set's
        Enabled() method will always return false, and any attempt to write
        to this event set will have no effect (safe no-op).

        This method may be used to create a placeholder event set. Active
        (registered) event sets are created using provider.RegisterSet().
        */
        constexpr
        EventSet() noexcept
            : m_tracepointState(TRACEPOINT_STATE_INIT)
            , m_level()
            , m_errno(22) // EINVAL
        {
            return;
        }

        /*
        Returns true if any logging session is listening for events with the
        provider, level, and keyword associated with this event set.

        For shared_ptr<EventSet> where the eventSetPtr might be NULL, consider
        using Enabled(eventSetPtr), which is equivalent to
        (eventSetPtr != NULL && eventSetPtr->Enabled()).
        */
        bool
        Enabled() const noexcept
        {
            return TRACEPOINT_ENABLED(&m_tracepointState);
        }

        /*
        For diagnostics/debugging (usually ignored in production).
        Returns 0 if this event set was successfully registered, or a nonzero
        error code if ioctl(user_events_data, DIAG_IOCSREG, ...) returned an
        error.
        */
        int
        Errno() const noexcept
        {
            return m_errno;
        }
    };

    /*
    Represents a named event provider. Use a provider to manage the EventSets.

    - Call provider.RegisterSet(level, keyword) to get a shared_ptr<EventSet>
      that can be used to write events with the corresponding
      provider + level + keyword.
    - Call provider.FindSet(level, keyword) to get a shared_ptr<EventSet> to
      an EventSet that was previously registered.
    - Call provider.Unregister() if you want to disconnect the provider before
      it goes out of scope.

    Note that Provider is not thread-safe, but the shared_ptr<EventSet> objects
    returned by RegisterSet() and FindSet() are thread-safe. Possible ways to
    safely use a provider in a multi-threaded program:

    - Have a reader-writer lock for the provider. Take an exclusive lock for
      non-const operations like RegisterSet() and Unregister(). Take a shared
      lock for other provider operations like FindSet().
    - Create the provider and do all of the necessary RegisterSet() calls
      before any other threads start using it. Then you can call the const
      methods like FindSet() on any thread as needed without any lock as long
      as nobody is calling any non-const methods.
    - Use your own thread-safe data structure to keep track of all of the
      EventSets you need. Take a lock if you ever need to register a new set.
    */
    class Provider
    {
        static auto constexpr NamesMax = EVENTHEADER_NAME_MAX - sizeof("_LffKffffffffffffffffG");

        struct EventKey
        {
            uint64_t Keyword;
            uint8_t Level; // Comes after keyword so that there is no padding between them.
        };

        struct EventKeyOps
        {
            // hash
            size_t
            operator()(EventKey const& a) const noexcept
            {
                // FNV-1a
                constexpr auto Prime = sizeof(size_t) == 8
                    ? static_cast<size_t>(0x00000100000001B3)
                    : static_cast<size_t>(0x01000193);
                auto h = sizeof(size_t) == 8
                    ? static_cast<size_t>(0xcbf29ce484222325)
                    : static_cast<size_t>(0x811c9dc5);
                auto const p = reinterpret_cast<uint8_t const*>(&a);
                assert(&a.Level - p == 8);
                for (unsigned i = 0; i != 9; i += 1)
                {
                    h = (h ^ p[i]) * Prime;
                }
                return h;
            }

            // equals
            bool
            operator()(EventKey const& a, EventKey const& b) const noexcept
            {
                return 0 == memcmp(&a, &b, 9);
            }
        };

        using EventSetMap = std::unordered_map<EventKey, std::shared_ptr<EventSet const>, EventKeyOps, EventKeyOps>;

        EventSetMap m_eventSets;
        tracepoint_provider_state m_providerState;
        eventheader_provider m_provider;
        int m_errno;
        char m_nameOptionsBuffer[NamesMax + 3]; // 3 = provider name's NUL + 'G' + option's NUL.

    public:

        Provider(Provider const&) = delete;
        void operator=(Provider const&) = delete;

        ~Provider()
        {
            eventheader_close_provider(&m_provider);
        }

        /*
        Creates a new provider.

        - providerName is the name to use for this provider. It must be less than 234
          chars and must not contain '\0', ' ', or ':'. For best compatibility with
          trace processing components, it should contain only ASCII letters, digits,
          and '_'. It should be short, human-readable, and unique enough to not
          conflict with names of other providers. The provider name will typically
          include a company name and a component name, e.g. "MyCompany_MyComponent".

        - groupName can usually be "". If the provider needs to join a provider group,
          specify the group name, which must contain only ASCII lowercase letters and
          digits. The total length of the provider name + the group name must be less
          than 234.

        Use RegisterSet() to register event sets and add them to the per-provider list
        of event sets. Use FindSet() to find a set that is already in the list.

        Use EventBuilder to create events, then write them to an event set.

        Requires: strlen(providerName) + strlen(groupName) < 234.
        Requires: providerName does not contain '\0', ' ', or ':'.
        Requires: groupName contains only ASCII lowercase letters and digits.
        */
        explicit
        Provider(
            std::string_view providerName,
            std::string_view groupName = std::string_view()) noexcept
            : m_eventSets()
            , m_providerState(TRACEPOINT_PROVIDER_STATE_INIT)
            , m_provider()
            , m_errno()
        {
            // Precondition violation: providerName must not contain these chars.
            assert(providerName.npos == providerName.find('\0'));
            assert(providerName.npos == providerName.find(' '));
            assert(providerName.npos == providerName.find(':'));

            using namespace std::string_view_literals;
            constexpr size_t NamesMax = EVENTHEADER_NAME_MAX - "_LffKffffffffffffffffG"sv.size();

            // Precondition violation: providerName must be less than 234 chars.
            assert(providerName.size() < NamesMax);

            auto const cchProviderName = providerName.size() < NamesMax
                ? providerName.size()
                : NamesMax - 1;

            m_provider.state = &m_providerState;
            m_provider.name = m_nameOptionsBuffer;
            m_provider.options = m_nameOptionsBuffer + cchProviderName + 1;

            size_t cchGroupName;
            for (cchGroupName = 0; groupName.size() != cchGroupName; cchGroupName += 1)
            {
                if (cchGroupName == NamesMax - cchProviderName - 1)
                {
                    assert(false); // Precondition violation: providerName + groupName too long.
                    break; // In release builds, stop here.
                }

                auto const ch = groupName[cchGroupName];
                if ((ch < '0' || '9' < ch) && (ch < 'a' || 'z' < ch))
                {
                    assert(false); // Precondition violation: groupName contains invalid chars.
                    break; // In release builds, stop here.
                }
            }

            auto pStrings = m_nameOptionsBuffer;

            // Create provider name string.

            assert(pStrings == m_provider.name);
            memcpy(pStrings, providerName.data(), cchProviderName);
            pStrings += cchProviderName;
            *pStrings = '\0';
            pStrings += 1;

            // Create options string.

            assert(pStrings == m_provider.options);
            if (cchGroupName != 0)
            {
                *pStrings = 'G';
                pStrings += 1;
                memcpy(pStrings, groupName.data(), cchGroupName);
                pStrings += cchGroupName;
            }

            *pStrings = '\0';
            pStrings += 1;

            assert(pStrings <= m_nameOptionsBuffer + sizeof(m_nameOptionsBuffer));

            m_errno = eventheader_open_provider(&m_provider);
        }

        /*
        Returns this provider's name.
        */
        std::string_view
        Name() const noexcept
        {
            assert(m_provider.name < m_provider.options);
            assert(m_provider.options[-1] == 0);
            return std::string_view(m_provider.name, m_provider.options - m_provider.name - 1);
        }

        /*
        Returns the options string, e.g. "" or "Gmygroup".
        */
        std::string_view
        Options() const noexcept
        {
            return m_provider.options;
        }

        /*
        For diagnostics/debugging (usually ignored in production).
        Returns 0 if this provider was successfully registered, or a nonzero
        errno code if eventheader_open_provider() failed.
        */
        int
        Errno() const noexcept
        {
            return m_errno;
        }

        /*
        If this provider is not registered, does nothing and returns 0.
        Otherwise, unregisters all event sets that were registered by this provider
        and clears the list of already-created event sets.

        Use provider.Unregister() if you want to unregister the provider before it goes
        out of scope. The provider automatically unregisters when it is destroyed so
        most users do not need to call Unregister() directly.
        */
        void
        Unregister() noexcept
        {
            eventheader_close_provider(&m_provider);
            m_eventSets.clear();
        }

        /*
        If an event set with the specified level and keyword is in the list of
        already-created sets, returns it. Otherwise, returns nullptr.
        */
        std::shared_ptr<EventSet const>
        FindSet(event_level level, uint64_t keyword) const noexcept
        {
            EventKey const k = { keyword, static_cast<uint8_t>(level) };
            auto const it = m_eventSets.find(k);
            return it == m_eventSets.end() ? std::shared_ptr<EventSet const>() : it->second;
        }

        /*
        If an event set with the specified level and keyword is in the list of
        already-created sets, returns it. Otherwise, creates a new event set, adds it to
        the list of already-created sets, attempts to register it, and returns the new
        event set. If registration fails, the new event set will have a non-zero errno
        and will never be enabled.

        In case of out-of-memory, returns nullptr.
        */
        std::shared_ptr<EventSet const>
        RegisterSet(event_level level, uint64_t keyword) noexcept
        {
            std::shared_ptr<EventSet const> result;

            EventKey const k = { keyword, static_cast<uint8_t>(level) };
            std::pair<EventSetMap::iterator, bool> emplace_result;
            try
            {
                emplace_result = m_eventSets.emplace(k, std::shared_ptr<EventSet const>());
            }
            catch (...)
            {
                return result; // nullptr.
            }

            if (!emplace_result.second)
            {
                result = emplace_result.first->second;
            }
            else try
            {
                auto created = std::make_shared<EventSet>();
                created->m_level = static_cast<uint8_t>(level);
                if (m_errno)
                {
                    created->m_errno = m_errno; // Propagate error from eventheader_open_provider.
                }
                else
                {
                    eventheader_tracepoint tp = {
                        &created->m_tracepointState,
                        nullptr,
                        { 0, 0, 0, 0, 0, static_cast<uint8_t>(level) },
                        keyword };
                    created->m_errno = eventheader_connect(&tp, &m_provider);
                }

                emplace_result.first->second = std::move(created);
                result = emplace_result.first->second;
            }
            catch (...)
            {
                m_eventSets.erase(k);
            }

            return result;
        }

        /*
        For testing purposes: Creates an inactive (unregistered) event set.

        If an event set with the specified level and keyword is in the list of
        already-created sets, returns it. Otherwise, creates a new **unregistered**
        event set, adds it to the list of already-created sets, and returns the new
        event set.

        In case of out-of-memory, returns nullptr.
        */
        std::shared_ptr<EventSet const>
        CreateUnregistered(event_level level, uint64_t keyword, bool enabled = false) noexcept
        {
            std::shared_ptr<EventSet const> result;

            EventKey const k = { keyword, static_cast<uint8_t>(level) };
            std::pair<EventSetMap::iterator, bool> emplace_result;
            try
            {
                emplace_result = m_eventSets.emplace(k, std::shared_ptr<EventSet const>());
            }
            catch (...)
            {
                return result; // nullptr.
            }

            if (!emplace_result.second)
            {
                result = emplace_result.first->second;
            }
            else try
            {
                auto created = std::make_shared<EventSet>();
                created->m_level = static_cast<uint8_t>(level);
                created->m_errno = 0;
                created->m_tracepointState.status_word = enabled;

                emplace_result.first->second = std::move(created);
                result = emplace_result.first->second;
            }
            catch (...)
            {
                m_eventSets.erase(k);
            }

            return result;
        }
    };

    /*
    Stores event attributes: name, fields, id, version, tag, opcode.

    Usage:

    - Create a provider.
    - Use the provider to get an eventSet.
    - If the eventSet is not enabled, skip the remaining steps.
    - Create an eventBuilder.
    - Call eventBuilder.Reset(name, ...) to start building an event.
    - Call other methods on eventBuilder to set attributes or add fields.
    - Call eventBuilder.Write(eventSet, ...) to write the event.
    */
    class EventBuilder
    {
        // Optimization: Use vector for capacity management but not size management.
        // Otherwise, vector repeatedly zeroes memory that we just overwrite.
        class Buffer
        {
            uint8_t* m_next;
            std::vector<uint8_t> m_data;

        public:

            Buffer() noexcept
                : m_next()
                , m_data()
            {
                m_next = m_data.data();
            }

            size_t
            size() const noexcept
            {
                return m_next - m_data.data();
            }

            uint8_t const*
            data() const noexcept
            {
                return m_data.data();
            }

            uint8_t*
            data() noexcept
            {
                return m_data.data();
            }

            void
            clear() noexcept
            {
                m_next = m_data.data();
            }

            void
            advance(size_t cbUsed) noexcept
            {
                assert(cbUsed <= static_cast<size_t>(m_data.data() + m_data.size() - m_next));
                m_next += cbUsed;
            }

            _Success_(return) bool
            ensure_space_for(size_t cbRequired, _Outptr_result_bytebuffer_(cbRequired) uint8_t** ppBuffer) noexcept
            {
                // Keep small so it's likely to be inlined.
                *ppBuffer = m_next;
                return cbRequired <= static_cast<size_t>(m_data.data() + m_data.size() - m_next)
                    || GrowToProvideSpaceFor(cbRequired, ppBuffer);
            }

        private:

            _Success_(return) bool
            GrowToProvideSpaceFor(size_t cbRequired, _Outptr_result_bytebuffer_(cbRequired) uint8_t** ppBuffer) noexcept
            {
                size_t const oldSize = m_next - m_data.data();
                assert(oldSize <= m_data.size());

                size_t const minSize = oldSize + cbRequired;
                if (minSize < cbRequired)
                {
                    // integer overflow
                }
                else try
                {
                    m_data.reserve(minSize);
                    m_data.resize(m_data.capacity());
                    assert(minSize <= m_data.size());
                    m_next = m_data.data() + oldSize;
                    *ppBuffer = m_next;
                    return true;
                }
                catch (...)
                {
                    // length error or out-of-memory.
                }

                // integer overflow, length error, or out-of-memory.
                *ppBuffer = nullptr;
                return false;
            }
        };

        template<unsigned Size>
        struct TypeInfo
        {
            static event_field_encoding const ValueEncoding =
                Size == 1 ? event_field_encoding_value8
                : Size == 2 ? event_field_encoding_value16
                : Size == 4 ? event_field_encoding_value32
                : Size == 8 ? event_field_encoding_value64
                : Size == 16 ? event_field_encoding_value128
                : event_field_encoding_invalid;

            static event_field_encoding const StringEncoding =
                Size == 1 ? event_field_encoding_string_length16_char8
                : Size == 2 ? event_field_encoding_string_length16_char16
                : Size == 4 ? event_field_encoding_string_length16_char32
                : event_field_encoding_invalid;

            static event_field_encoding const ZStringEncoding =
                Size == 1 ? event_field_encoding_zstring_char8
                : Size == 2 ? event_field_encoding_zstring_char16
                : Size == 4 ? event_field_encoding_zstring_char32
                : event_field_encoding_invalid;
        };

        /*
        Detects whether the specified value type has a supported size
        (1, 2, 4, 8, or 16) and is trivially-copyable. If so, supplies the
        ValueEncoding (value8, value16, value32, value64, value128).
        */
        template<class ValTy>
        struct ValueEnabled
            : std::enable_if<
                std::is_trivially_copyable<ValTy>::value &&
                TypeInfo<sizeof(ValTy)>::ValueEncoding != event_field_encoding_invalid,
                EventBuilder&>
            , TypeInfo<sizeof(ValTy)>
        {};

        /*
        Detects whether the specified char type has a supported size
        (1, 2, or 4) and is trivially-copyable. If so, supplies the StringEncoding
        and ZStringEncoding (char8, char16, char32).
        */
        template<class CharTy, bool ExtraCondition = true>
        struct StringEnabled
            : std::enable_if<
                ExtraCondition &&
                std::is_trivially_copyable<CharTy>::value &&
                TypeInfo<sizeof(CharTy)>::StringEncoding != event_field_encoding_invalid,
                EventBuilder&>
            , TypeInfo<sizeof(CharTy)>
        {};

        Buffer m_meta;
        Buffer m_data;
        bool m_error;
        uint8_t m_version;
        uint16_t m_id;
        uint16_t m_tag;
        uint8_t m_opcode;

    public:

        /*
        Returns a new event builder with specified initial buffer capacities.
        Buffers will automatically grow as needed.

        Call Reset() to start building a new event.
        */
        explicit
        EventBuilder(uint16_t metaCapacity = 256, uint16_t dataCapacity = 256) noexcept
            : m_meta()
            , m_data()
            , m_error(true)
            , m_version()
            , m_id()
            , m_tag()
            , m_opcode()
        {
            uint8_t* pIgnored;
            m_meta.ensure_space_for(metaCapacity, &pIgnored);
            m_data.ensure_space_for(dataCapacity, &pIgnored);
        }

        /*
        Clears the previous event (if any) from the builder and starts building a new
        event.

        - name is the event name. It should be short and unique. It must not contain '\0'.
        - tag is a 16-bit integer that will be recorded in the event and can be
          used for any provider-defined purpose. Use 0 if you are not using event tags.
        */
        EventBuilder&
        Reset(std::string_view name, uint16_t tag = 0) noexcept
        {
            // Precondition violation: name must not contain '\0'.
            assert(name.find('\0') == name.npos);

            m_meta.clear();
            m_data.clear();
            m_error = false;
            m_version = 0;
            m_id = 0;
            m_tag = tag;
            m_opcode = event_opcode_info;

            uint8_t* pMeta;
            if (!m_meta.ensure_space_for(name.size() + 1, &pMeta))
            {
                m_error = true;
            }
            else
            {
                memcpy(pMeta, name.data(), name.size());
                pMeta[name.size()] = '\0';
                m_meta.advance(name.size() + 1);
            }

            return *this;
        }

        /*
        Sends the finished event to the kernel with the provider, event level, and event
        keyword of the specified event set.

        - eventSet should be a registered and enabled event set. Calling Write on an
          unregistered or disabled event set is a safe no-op (usually returns 0 in this
          case, though it may return ERANGE if the event is too large).

        - activityId contains a pointer to the 16-byte activity id to be assigned to the
          event, or nullptr if the event is not part of an activity. (An activity is a
          group of related events that all have the same activity id, started by an event
          with event_opcode_activity_start and ended by an event with
          event_opcode_activity_stop.)

        - relatedId contains a pointer to the 16-byte related activity id (parent activity)
          to be used for an activity-start event, or nullptr if the event is not an
          activity-start event or does not have a parent activity. If activityId is nullptr,
          this must also be nullptr.

        Returns 0 for success. Returns a nonzero errno value for failure. The return
        value is for diagnostic/debugging purposes only and should generally be ignored
        in retail builds. Returns EBADF (9) if tracepoint is unregistered or disabled.
        Returns ENOMEM (12) if out of memory. Returns ERANGE (34) if the event (headers +
        metadata + data) is greater than 64KB. Returns other errors as reported by writev.
        */
        int
        Write(
            EventSet const& eventSet,
            _In_reads_bytes_opt_(16) void const* activityId = nullptr,
            _In_reads_bytes_opt_(16) void const* relatedId = nullptr) const noexcept
        {
            assert(relatedId == nullptr || activityId != nullptr);
            if (m_error)
            {
                return 12; // ENOMEM
            }
            else if (m_meta.size() + m_data.size() > 65535 - (52 + 16))
            {
                return 34; // ERANGE
            }
            else
            {
                eventheader_extension metadataExt = {};
                metadataExt.size = static_cast<uint16_t>(m_meta.size());
                metadataExt.kind = eventheader_extension_kind_metadata;

                iovec dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA + 3];
                dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA + 0] = { &metadataExt, sizeof(metadataExt) };
                dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA + 1] = { (void*)m_meta.data(), m_meta.size() };
                dataVecs[EVENTHEADER_PREFIX_DATAVEC_COUNT_NO_METADATA + 2] = { (void*)m_data.data(), m_data.size() };

                eventheader_tracepoint tp = {};
                tp.state = const_cast<tracepoint_state*>(&eventSet.m_tracepointState);
                tp.header.flags = eventheader_flag_default_with_extension;
                tp.header.version = m_version;
                tp.header.id = m_id;
                tp.header.tag = m_tag;
                tp.header.opcode = m_opcode;
                tp.header.level = eventSet.m_level;

                return eventheader_write(
                    &tp,
                    activityId,
                    relatedId,
                    sizeof(dataVecs) / sizeof(dataVecs[0]),
                    dataVecs);
            }
        }

        /*
        Sets the manually-assigned id and version of the event to be generated
        by this builder. Most events will use id = 0, version = 0, indicating
        that the event does not have a manually-assigned id and is identified
        by event name rather than by event id.

        Call this with a nonzero value for id if the event has a
        manually-assigned (durable) unique id.

        When the id is first assigned, use version 0. Increment version each
        time the event schema changes (each time you make a change to the
        event name, field names, field types, etc.).
        */
        EventBuilder&
        IdVersion(uint16_t id, uint8_t version) noexcept
        {
            m_id = id;
            m_version = version;
            return *this;
        }

        /*
        Sets the opcode of the event, indicating special semantics to be used
        by event decoders, e.g. activity-start or activity-stop. Most events
        do not have special semantics so most events use the default opcode,
        info (0).
        */
        EventBuilder&
        Opcode(event_opcode opcode) noexcept
        {
            m_opcode = static_cast<uint8_t>(opcode);
            return *this;
        }

        /*
        Adds a field containing the specified number of sub-fields.

        A struct is a way to logically group a number of fields. To add a struct to
        an event, call builder.AddStruct("StructName", structFieldCount).
        Then add structFieldCount more fields and they will be considered to be
        members of the struct.

        - fieldName should be a short and distinct string that describes the field.

        - structFieldCount specifies the number of subsequent fields that will be
          considered to be part of this struct field. This must be in the range 1 to
          127. Empty structs (structs that contain no fields) are not permitted.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        - pFieldCountBookmark (advanced): If you don't know how many fields will be in
          the struct ahead of time, pass 1 for structFieldCount and pass the address of
          a size_t variable to receive a bookmark. After you have added all of the fields
          you can then use the bookmark to set the actual structFieldCount value. Note
          that structFieldCount still cannot be 0 or larger than 127. Use nullptr if
          you are passing the actual field count in the structFieldCount parameter.

        Structs can nest. Each nested struct and its fields count as 1 field for the
        parent struct.
        */
        EventBuilder&
        AddStruct(
            std::string_view fieldName,
            uint8_t structFieldCount,
            uint16_t fieldTag = 0,
            _Out_opt_ size_t* pFieldCountBookmark = nullptr) noexcept
        {
            uint8_t maskedFieldCount = structFieldCount & event_field_format_value_mask;
            assert(structFieldCount == maskedFieldCount); // Precondition: structFieldCount must be less than 128.

            size_t fieldCountBookmark;
            if (structFieldCount == 0)
            {
                assert(structFieldCount != 0); // Precondition: structFieldCount must not be 0.
                fieldCountBookmark = -1;
            }
            else
            {
                fieldCountBookmark = RawAddMeta(fieldName, event_field_encoding_struct, maskedFieldCount, fieldTag);
            }

            if (pFieldCountBookmark)
            {
                *pFieldCountBookmark = fieldCountBookmark;
            }

            return *this;
        }

        /*
        Advanced: Resets the number of logical fields in a structure.

        Requires:

        - fieldCountBookmark is a bookmark value returned by AddStruct, and you
          haven't called Reset since that bookmark was returned.
        - structFieldCount is a value from 1 to 127. (Must not be 0. Must be less
          than 128.)

        If the final number of fields of a structure is not known at the time
        you need to start the structure, you can follow this procedure:

        - Create a size_t bookmark variable.
        - In the AddStruct call, pass 1 as the number of fields, and pass &bookmark
          as the pFieldCountBookmark parameter.
        - After you know the actual number of fields, call
          SetStructFieldCount(bookmark, fieldCount).
        */
        EventBuilder&
        SetStructFieldCount(size_t fieldCountBookmark, uint8_t structFieldCount) noexcept
        {
            uint8_t maskedFieldCount = structFieldCount & event_field_format_value_mask;
            assert(structFieldCount == maskedFieldCount); // Precondition: structFieldCount must be less than 128.

            if (structFieldCount == 0)
            {
                assert(structFieldCount != 0); // Precondition: structFieldCount must not be 0.
            }
            else if (fieldCountBookmark < m_meta.size())
            {
                auto const pEncoding = m_meta.data() + fieldCountBookmark;
                *pEncoding = (*pEncoding & 0x80) | maskedFieldCount;
            }
            else
            {
                // Precondition: fieldCountBookmark must be a valid bookmark from AddStruct.
                assert(fieldCountBookmark == static_cast<size_t>(-1));
            }

            return *this;
        }

        /*
        Adds a field containing a simple value.

        - fieldName should be a short and distinct string that describes the field.

        - fieldValue provides the data for the field. Note that the data is treated
          as raw bytes, i.e. there will be no error, warning, or data conversion if the
          type of the fieldValue parameter conflicts with the format parameter. See below
          for the types accepted for this parameter.

        - format indicates how the decoder should interpret the field data. For example,
          if the field value is int8_t or int32_t, you would likely set format to
          event_field_format_signed_int, and if the field value is float or double, you
          would likely set format to event_field_format_float.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        Types:

        - If fieldValue is a 1-byte type (e.g. char, bool), the field will be encoded as
          value8. For 1-byte types, if format is default, the field will be formatted as
          unsigned_int. Usable formats for 1-byte types include: unsigned_int, signed_int,
          hex_int, boolean, hex_bytes, string8.
        - If fieldValue is a 2-byte type (e.g. short), the field will be encoded as
          value16. For 2-byte types, if format is default, the field will be formatted as
          unsigned_int. Usable formats for 2-byte types include: unsigned_int, signed_int,
          hex_int, boolean, hex_bytes, string_utf, port.
        - If fieldValue is a 4-byte type (e.g. int, float), the field will be encoded as
          value32. For 4-byte types, if format is default, the field will be formatted as
          unsigned_int. Usable formats for 4-byte types include: unsigned_int, signed_int,
          hex_int, errno, pid, time, boolean, float, hex_bytes, string_utf, ipv4.
        - If fieldValue is an 8-byte type (e.g. long long, double), the field will be
          encoded as value64. For 8-byte types, if format is default], the field will be
          formatted as unsigned_int. Usable formats include: unsigned_int, signed_int,
          hex_int, time, float, hex_bytes.
        - If fieldValue is a pointer-size type (e.g. void* or intptr_t), the field will be
          encoded as value_ptr (which is an alias for either value32 or value64). For
          pointer-sized types, if format is default, the field will be formatted as
          unsigned_int. Usable formats for pointer-sized types include: unsigned_int,
          signed_int, hex_int, time, float, hex_bytes.
        - If fieldValue is a 16-byte type (e.g. ehd::Value128 or your own
          trivially-copyable 16-byte struct), the field will be encoded as value128. For
          16-byte types, if format is default, the field will be formatted as hex_bytes.
          Usable formats for 16-byte types include: hex_bytes, uuid, ipv6.

        Notes:

        - Using event_field_format_default instead of another event_field_format value
          saves 1 byte per event in the trace data.
          - For small values (1-8 bytes), event_field_format_default is equivalent to
            event_field_format_unsigned_int, so if logging a small value that you want
            formatted as an unsigned decimal integer, you can save 1 byte per event with no
            change in decoding behavior by using event_field_format_default instead of
            event_field_format_unsigned_int for that field.
          - For 16-byte values, event_field_format_default is equivalent to
            event_field_format_hex_bytes, so if logging a 16-byte value that you want
            formatted as hexadecimal bytes, you can save 1 byte per event with no change in
            decoding behavior by using event_field_format_default instead of
            event_field_format_hex_bytes for that field.
        */
        template<class ValTy>
        auto // Returns EventBuilder&
        AddValue(
            std::string_view fieldName,
            ValTy const& fieldValue,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename ValueEnabled<ValTy>::type // enable_if
        {
            RawAddMeta(fieldName, ValueEnabled<ValTy>::ValueEncoding, format, fieldTag);
            RawAddDataValue(fieldValue);
            return *this;
        }

        /*
        Adds a field containing a sequence of simple values such as an array of integers.

        - fieldName should be a short and distinct string that describes the field.

        - fieldBeginIterator..fieldEndIterator provide the data for the field as a
          beginIterator-endIterator pair (can be beginPtr and endPtr if values are in a
          contiguous range). The value types accepted by this method are the same as the
          types accepted by AddValue. Note that, as with AddValue, the element data is
          treated as raw bytes, i.e. there will be no error, warning, or data conversion
          if the type of the value conflicts with the format parameter.

        - format indicates how the decoder should interpret the field data. For example,
          if the field values are int8_t or int32_t, you would likely set format to
          signed_int, and if the field values are float or double, you would likely set
          format to float.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        See AddValue for additional details about the compatible element types and how
        they are treated.

        For strings or binary blobs, use AddString instead of this method.
        If you pass a string or blob to this method, the decoder will format the field as
        an array of values (e.g. ['a', 'b', 'c'] or [0x61, 0x62, 0x63]) rather than as a
        string or blob (e.g. "abc" or "61 62 63").
        */
        template<class BeginItTy, class EndItTy>
        auto // Returns EventBuilder&
        AddValueRange(
            std::string_view fieldName,
            BeginItTy fieldBeginIterator,
            EndItTy fieldEndIterator,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename ValueEnabled<typename std::decay<decltype(*fieldBeginIterator)>::type>::type // enable_if
        {
            using ValTy = typename std::decay<decltype(*fieldBeginIterator)>::type;
            RawAddMeta(
                fieldName,
                ValueEnabled<ValTy>::ValueEncoding | event_field_encoding_varray_flag,
                format,
                fieldTag);
            RawAddDataRange(fieldBeginIterator, fieldEndIterator,
                [](EventBuilder* pThis, ValTy const& value)
                {
                    pThis->RawAddDataValue(value);
                });
            return *this;
        }

        /*
        Adds a field containing a string or a binary blob.

        - fieldName should be a short and distinct string that describes the field.

        - fieldValue provides the data for the field as a basic_string_view<CharTy>, e.g.
          std::string_view, std::wstring_view, std::u16string_view, std::u32string_view.

          Note: you can either provide an actual std::string_view for this parameter or you
          can explicitly specify the char type, e.g. AddString<char>(), and then you can
          take advantage of string_view's implicit conversions.

        - format indicates how the decoder should interpret the field data. For example,
          if the field value is a Unicode string, you would likely set format to
          default (resulting in the field decoding as string_utf, and if the field value
          is a binary blob, you would likely set format to hex_bytes.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        Types:

        - The field will be encoded as one of string_length16_char8,
          string_length16_char16, or string_length16_char32.
        - If format is default, the field will be formatted as string_utf.
        - Usable formats include: hex_bytes, string_utf_bom, string_xml, string_json.
        - For 8-bit char types, you may also use format string8, indicating a non-Unicode
          string (usually treated as Latin-1).

        Note that event_field_format_default saves 1 byte in the trace. For string/binary
        encodings, event_field_format_default is treated as event_field_format_string_utf,
        so you can save 1 byte in the trace by using event_field_format_default instead of
        event_field_format_string_utf for string fields.

        This is the same as AddNulTerminatedString except that the field will be encoded as
        a counted sequence instead of as a nul-terminated string. In most cases you
        should prefer this method and use AddNulTerminatedString only if you specifically
        need the nul-terminated encoding.
        */
        template<class CharTy>
        auto // Returns EventBuilder&
        AddString(
            std::string_view fieldName,
            std::basic_string_view<CharTy> fieldValue,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename StringEnabled<CharTy>::type // enable_if
        {
            RawAddMeta(fieldName, StringEnabled<CharTy>::StringEncoding, format, fieldTag);
            RawAddDataCounted(fieldValue);
            return *this;
        }

        /*
        Adds a field containing a sequence of strings or binary blobs.
        Note that you must provide the char type as the first template parameter, e.g.
        AddStringRange<char> or AddStringRange<wchar_t>.

        - fieldName should be a short and distinct string that describes the field.

        - fieldBeginIterator..fieldEndIterator provide the data for the field as a
          beginIterator-endIterator pair (can be beginPtr and endPtr if values are in a
          contiguous range). The iterators must return a value that is
          implicitly-convertible to std::basic_string_view<CharTy>, e.g.
          *fieldBeginIterator must return something like a char* or a std::string_view.

        - format indicates how the decoder should interpret the field data. For example,
          if the field value contains Unicode strings, you would likely set format to
          default (resulting in the field decoding as string_utf, and if the field value
          contains binary blobs, you would likely set format to hex_bytes.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        This is the same as AddNulTerminatedStringRange except that the field will be
        encoded as a counted sequence instead of as a nul-terminated string. In most cases
        you should prefer this method and use AddNulTerminatedStringRange only if you
        specifically need the nul-terminated encoding.
        */
        template<class CharTy, class BeginItTy, class EndItTy>
        auto // Returns EventBuilder&
        AddStringRange(
            std::string_view fieldName,
            BeginItTy fieldBeginIterator,
            EndItTy fieldEndIterator,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename StringEnabled<
                CharTy,
                std::is_convertible<decltype(*fieldBeginIterator), std::basic_string_view<CharTy>>::value
                >::type // enable_if
        {
            RawAddMeta(
                fieldName,
                StringEnabled<CharTy>::StringEncoding | event_field_encoding_varray_flag,
                format,
                fieldTag);
            RawAddDataRange(fieldBeginIterator, fieldEndIterator,
                [](EventBuilder* pThis, std::basic_string_view<CharTy> value)
                {
                    pThis->RawAddDataCounted(value);
                });
            return *this;
        }

        /*
        Adds a field containing a nul-terminated string.

        - fieldName should be a short and distinct string that describes the field.

        - fieldValue provides the data for the field as a basic_string_view<CharTy>, e.g.
          std::string_view, std::wstring_view, std::u16string_view, std::u32string_view.
          The event will include the provided data up to the first '\0' value (if any).

          Note: you can either provide an actual std::string_view for this parameter or you
          can explicitly specify the char type, e.g. AddString<char>(), and then you can
          take advantage of string_view's implicit conversions.

        - format indicates how the decoder should interpret the field data. For example,
          if the field value is a Unicode string, you would likely set format to
          default (resulting in the field decoding as string_utf, and if the field value
          is a binary blob, you would likely set format to hex_bytes.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        Types:

        - The field will be encoded as one of zstring_char8, zstring_char16, or
          zstring_char32.
        - If format is default, the field will be formatted as string_utf.
        - Usable formats include: hex_bytes, string_utf_bom, string_xml, string_json.
        - For 8-bit char types, you may also use format string8, indicating a non-Unicode
          string (usually treated as Latin-1).

        Note that event_field_format_default saves 1 byte in the trace. For string/binary
        encodings, event_field_format_default is treated as event_field_format_string_utf,
        so you can save 1 byte in the trace by using event_field_format_default instead of
        event_field_format_string_utf for string fields.

        This is the same as AddString except that the field will be encoded as a
        nul-terminated string instead of as a counted string. In most cases you
        should prefer AddString and use this method only if you specifically need
        the nul-terminated encoding.
        */
        template<class CharTy>
        auto // Returns EventBuilder&
        AddNulTerminatedString(
            std::string_view fieldName,
            std::basic_string_view<CharTy> fieldValue,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename StringEnabled<CharTy>::type // enable_if
        {
            RawAddMeta(fieldName, StringEnabled<CharTy>::ZStringEncoding, format, fieldTag);
            RawAddDataNulTerminated(fieldValue);
            return *this;
        }

        /*
        Adds a field containing a sequence of nul-terminated strings.
        Note that you must provide the char type as the first template parameter, e.g.
        AddNulTerminatedStringRange<char> or AddNulTerminatedStringRange<wchar_t>.

        - fieldName should be a short and distinct string that describes the field.

        - fieldBeginIterator..fieldEndIterator provide the data for the field as a
          beginIterator-endIterator pair (can be beginPtr and endPtr if values are in a
          contiguous range). The iterators must return a value that is
          implicitly-convertible to std::basic_string_view<CharTy>, e.g.
          *fieldBeginIterator must return something like a char* or a std::string_view.

        - format indicates how the decoder should interpret the field data. For example,
          if the field value contains Unicode strings, you would likely set format to
          default (resulting in the field decoding as string_utf, and if the field value
          contains binary blobs, you would likely set format to hex_bytes.

        - fieldTag is a 16-bit integer that will be recorded in the field and can be
          used for any provider-defined purpose. Use 0 if you are not using field tags.

        This is the same as AddStringRange except that the field will be
        encoded as a nul-terminated string instead of as a counted string. In most cases
        you should prefer AddStringRange and use this method only if you
        specifically need the nul-terminated encoding.
        */
        template<class CharTy, class BeginItTy, class EndItTy>
        auto // Returns EventBuilder&
        AddNulTerminatedStringRange(
            std::string_view fieldName,
            BeginItTy fieldBeginIterator,
            EndItTy fieldEndIterator,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
            -> typename StringEnabled<
                CharTy,
                std::is_convertible<decltype(*fieldBeginIterator), std::basic_string_view<CharTy>>::value
                >::type // enable_if
        {
            RawAddMeta(
                fieldName,
                StringEnabled<CharTy>::ZStringEncoding | event_field_encoding_varray_flag,
                format,
                fieldTag);
            RawAddDataRange(fieldBeginIterator, fieldEndIterator,
                [](EventBuilder* pThis, std::basic_string_view<CharTy> value)
                {
                    pThis->RawAddDataNulTerminated(value);
                });
            return *this;
        }

        /*
        *Advanced scenarios:* Directly adds unchecked metadata to the event. Using this
        method may result in events that do not decode correctly.

        There are a few things that are supported by EventHeader that cannot be expressed
        by directly calling the add methods, e.g. array-of-struct. If these edge cases are
        important, you can use the RawAddMeta and RawAddData methods to generate events
        that would otherwise be impossible. Doing this requires advanced understanding of
        the EventHeader encoding system. If done incorrectly, the resulting events will not
        decode properly.
        */
        EventBuilder&
        RawAddMetaScalar(
            std::string_view fieldName,
            event_field_encoding encoding,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
        {
            assert(encoding == (encoding & event_field_encoding_value_mask));
            RawAddMeta(fieldName, encoding, format, fieldTag);
            return *this;
        }

        /*
        *Advanced scenarios:* Directly adds unchecked metadata to the event. Using this
        method may result in events that do not decode correctly.

        There are a few things that are supported by EventHeader that cannot be expressed
        by directly calling the add methods, e.g. array-of-struct. If these edge cases are
        important, you can use the RawAddMeta and RawAddData methods to generate events
        that would otherwise be impossible. Doing this requires advanced understanding of
        the EventHeader encoding system. If done incorrectly, the resulting events will not
        decode properly.
        */
        EventBuilder&
        RawAddMetaVcount(
            std::string_view fieldName,
            event_field_encoding encoding,
            event_field_format format,
            uint16_t fieldTag = 0) noexcept
        {
            assert(encoding == (encoding & event_field_encoding_value_mask));
            RawAddMeta(fieldName, encoding | event_field_encoding_varray_flag, format, fieldTag);
            return *this;
        }

        /*
        *Advanced scenarios:* Directly adds unchecked metadata to the event. Using this
        method may result in events that do not decode correctly.

        There are a few things that are supported by EventHeader that cannot be expressed
        by directly calling the add methods, e.g. array-of-struct. If these edge cases are
        important, you can use the RawAddMeta and RawAddData methods to generate events
        that would otherwise be impossible. Doing this requires advanced understanding of
        the EventHeader encoding system. If done incorrectly, the resulting events will not
        decode properly.
        */
        template<class ValTy>
        EventBuilder&
        RawAddDataValue(ValTy const& fieldValue) noexcept
        {
            auto const cb = sizeof(fieldValue);
            uint8_t* pData;
            if (!m_data.ensure_space_for(cb, &pData))
            {
                m_error = true;
            }
            else
            {
                memcpy(pData, std::addressof(fieldValue), cb);
                m_data.advance(cb);
            }

            return *this;
        }

        /*
        *Advanced scenarios:* Directly adds unchecked metadata to the event. Using this
        method may result in events that do not decode correctly.

        There are a few things that are supported by EventHeader that cannot be expressed
        by directly calling the add methods, e.g. array-of-struct. If these edge cases are
        important, you can use the RawAddMeta and RawAddData methods to generate events
        that would otherwise be impossible. Doing this requires advanced understanding of
        the EventHeader encoding system. If done incorrectly, the resulting events will not
        decode properly.
        */
        template<class ValTy>
        EventBuilder&
        RawAddDataValues(_In_reads_(count) ValTy const* values, uint16_t count) noexcept
        {
            auto const cb = sizeof(values[0]) * count;
            uint8_t* pData;
            if (!m_data.ensure_space_for(cb, &pData))
            {
                m_error = true;
            }
            else
            {
                memcpy(pData, values, cb);
                m_data.advance(cb);
            }

            return *this;
        }

    private:

        // Returns: The offset of the format byte, or -1.
        size_t
        RawAddMeta(std::string_view fieldName, uint8_t encoding, uint8_t format, uint16_t fieldTag) noexcept
        {
            size_t formatOffset = -1;

            // Precondition violation: fieldName must not contain '\0'.
            assert(fieldName.find('\0') == fieldName.npos);

            uint8_t* pMeta;
            if (!m_meta.ensure_space_for(fieldName.size() + 7, &pMeta))
            {
                m_error = true;
            }
            else
            {
                size_t cMeta = 0;

                memcpy(pMeta, fieldName.data(), fieldName.size());
                cMeta += fieldName.size();

                pMeta[cMeta] = '\0';
                cMeta += 1;

                if (fieldTag != 0)
                {
                    pMeta[cMeta] = encoding | 0x80;
                    cMeta += 1;
                    pMeta[cMeta] = format | 0x80;
                    formatOffset = m_meta.size() + cMeta;
                    cMeta += 1;
                    memcpy(&pMeta[cMeta], &fieldTag, sizeof(fieldTag));
                    cMeta += sizeof(fieldTag);
                }
                else if (format != 0)
                {
                    pMeta[cMeta] = encoding | 0x80;
                    cMeta += 1;
                    pMeta[cMeta] = format;
                    formatOffset = m_meta.size() + cMeta;
                    cMeta += 1;
                }
                else
                {
                    pMeta[cMeta] = encoding;
                    cMeta += 1;
                }

                m_meta.advance(cMeta);
            }

            return formatOffset;
        }

        template<class CharTy>
        EventBuilder&
        RawAddDataNulTerminated(std::basic_string_view<CharTy> fieldValue) noexcept
        {
            auto cch = fieldValue.find('\0');
            if (cch > fieldValue.size())
            {
                cch = fieldValue.size();
            }

            auto const cb = cch * sizeof(CharTy);
            uint8_t* pData;
            if (!m_data.ensure_space_for(cb + sizeof(CharTy), &pData))
            {
                m_error = true;
            }
            else
            {
                memcpy(pData, fieldValue.data(), cb);
                memset(pData + cb, 0, sizeof(CharTy));
                m_data.advance(cb + sizeof(CharTy));
            }

            return *this;
        }

        template<class CharTy>
        EventBuilder&
        RawAddDataCounted(std::basic_string_view<CharTy> fieldValue) noexcept
        {
            uint16_t count = fieldValue.size() <= UINT16_MAX ? fieldValue.size() : UINT16_MAX;
            auto const cb = fieldValue.size() * sizeof(CharTy);
            uint8_t* pData;
            if (!m_data.ensure_space_for(sizeof(count) + cb, &pData))
            {
                m_error = true;
            }
            else
            {
                memcpy(pData, &count, sizeof(count));
                memcpy(pData + sizeof(count), fieldValue.data(), cb);
                m_data.advance(sizeof(count) + cb);
            }

            return *this;
        }

        template<class BeginItTy, class EndItTy, class AddDataFnTy>
        EventBuilder&
        RawAddDataRange(BeginItTy beginIt, EndItTy endIt, AddDataFnTy&& addData) noexcept
        {
            uint16_t count = 0;
            uint8_t* pCountDontUse; // We may reallocate before we update count.
            auto const countPos = m_data.size(); // Use this instead.
            if (!m_data.ensure_space_for(sizeof(count), &pCountDontUse))
            {
                m_error = true;
            }
            else
            {
                m_data.advance(sizeof(count));

                for (; beginIt != endIt; ++beginIt)
                {
                    if (count == UINT16_MAX)
                    {
                        break;
                    }

                    count += 1;
                    addData(this, *beginIt);
                }

                memcpy(m_data.data() + countPos, &count, sizeof(count));
            }

            return *this;
        }
    };

    /*
    Returns true if the eventSet is valid and enabled.
    Returns false if the eventSet is NULL or disabled.

    Since RegisterSet() can return NULL for out-of-memory, it is important to
    check for NULL before calling methods on eventSet. In cases where the
    eventSet might be NULL, you can use if(Enabled(eventSet)) instead of
    if(eventSet && eventSet->Enabled()).
    */
    inline bool
    Enabled(std::shared_ptr<EventSet const> const& eventSet) noexcept
    {
        return eventSet && eventSet->Enabled();
    }

    /*
    Returns true if the eventSet is valid and enabled.
    Returns false if the eventSet is NULL or disabled.

    Since RegisterSet() can return NULL for out-of-memory, it is important to
    check for NULL before calling methods on eventSet. In cases where the
    eventSet might be NULL, you can use if(Enabled(eventSet)) instead of
    if(eventSet && eventSet->Enabled()).
    */
    inline bool
    Enabled(EventSet const* eventSet) noexcept
    {
        return eventSet && eventSet->Enabled();
    }

    /*
    Convenience type for passing a 16-byte (128-bit) value to AddValue or
    AddValueRange:

    - If you already have a 16-byte trivially-copyable struct defined,
      AddValue and AddValueRange will accept that directly, so there is no
      need to use this.

    - If you have a 16-byte array like "char MyGuid[16]" or "char* pMyGuid",
      you will need to cast it to a 128-bit value type like Value128 to use it
      with AddValue or AddValueRange.

    Examples:

    - eb.AddValue("GUID", *(Value128 const*)MyGuid, event_field_format_uuid);

    - eb.AddValueRange("IPv6 addresses",
          (Value128 const*)pMyAddressesBegin, (Value128 const*)pMyAddressesEnd,
          event_field_format_ipv6);
    */
    struct Value128
    {
        char Data[16];
    };
}
// namespace ehd

#endif // _included_EventHeaderDynamic_h
