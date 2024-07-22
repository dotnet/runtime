#include <eventheader/EventHeaderDynamic.h>
#include <stdio.h>

static char const guid1[16] = "123456789abcdef";
static char const* const CharStrings[2] = {
    "abc", "123"
};
static wchar_t const* const WcharStrings[2] = {
    L"Labc", L"L123"
};

static uint8_t arrayOf5Bytes[5] = { 1, 2, 3, 4, 5 };

int main()
{
    int err;

    /*
    A provider is a group of events. The provider has a provider name and an
    optional group name.

    Provider name must be a valid C identifier: starts with underscore or ASCII
    letter; remaining chars may be underscores, ASCII letters, or ASCII digits.

    The provider is not used to directly write events. Instead, you use the
    provider to create an EventSet or look up an existing EventSet, and then
    you use the EventSet to write the events.

    The EventSet is thread-safe, but the provider is not thread-safe. Possible
    multi-threaded usage patterns for the provider would include the following:

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
    ehd::Provider provider1("EhdProv1");
    printf("provider1: Name=\"%.*s\" Options=\"%.*s\"\n",
        (int)provider1.Name().size(), provider1.Name().data(),
        (int)provider1.Options().size(), provider1.Options().data());

    /*
    A provider may optionally have a group name. If present, it must contain
    only ASCII lowercase letters and ASCII digits.
    */
    ehd::Provider provider2("EhdProv2", "mygroup");
    printf("provider2: Name=\"%.*s\" Options=\"%.*s\"\n",
        (int)provider2.Name().size(), provider2.Name().data(),
        (int)provider2.Options().size(), provider2.Options().data());

    /*
    An event set is required for writing events. Each event set is for a
    provider + event level + event keyword combination. Each event set
    corresponds to one unique tracepoint name.

    - Get an event set by calling provider.RegisterSet(). It will return a
      previously-created event set if one already exists, or make a new one
      if one does not already exist. It returns shared_ptr<EventSet> on
      success or nullptr if out of memory.
    - Get a previously-created event set by calling provider.FindSet(),
      which returns shared_ptr<EventSet>, or nullptr if not found.

    The shared_ptr<EventSet> will stop working if the provider is closed,
    but nothing bad will happen if you use it after the provider closes.

    Note that provider.RegisterSet() is not thread-safe, but the returned
    shared_ptr<EventSet> is thread-safe.

    If RegisterSet() hits an out-of-memory error, it returns nullptr.

    If RegisterSet() hits any other error, it returns an inactive EventSet.
    It is safe to use an inactive EventSet -- it will just always be disabled.

    If RegisterSet() succeeds, it returns an active EventSet. The EventSet
    becomes inactive when the provider is unregistered or destroyed.
    */
    auto EhdProv1_L5K1 = provider1.RegisterSet(event_level_verbose, 1);

    /*
    For debugging purposes, you can check eventSet->Errno() to see whether the
    event set was registered successfully.
    */
    if (EhdProv1_L5K1) // Protect against possible out-of-memory condition
    {
        printf("EhdProv1_L5K1: err=%u enabled=%u\n",
            EhdProv1_L5K1->Errno(), EhdProv1_L5K1->Enabled());
    }

    auto EhdProv2_L4K2Gmygroup = provider2.RegisterSet(event_level_information, 2);
    if (EhdProv2_L4K2Gmygroup)
    {
        printf("EhdProv2_L4K2Gmygroup: err=%u enabled=%u\n",
            EhdProv2_L4K2Gmygroup->Errno(), EhdProv2_L4K2Gmygroup->Enabled());
    }

    /*
    Use an EventBuilder to create the event. Call eventBuilder.Reset() to
    clear the eventBuilder and set the name and tag, call other methods to set
    attributes or add fields, and call eventBuilder.Write() to send the event
    to the kernel.

    Note that EventBuilder is reusable. If you need to write several events,
    you might see a small performance improvement by reusing the same
    EventBuilder for several events instead of creating a new one for each
    event (it stores two std::vector<char> buffers, so reusing the builder
    can reduce heap allocation/deallocation).
    */
    ehd::EventBuilder eb;
    size_t bookmark;

    /*
    Building and writing an event is a waste of CPU time if the event is not
    enabled. It's usually more efficient to check whether the event is enabled
    before building and writing the event.
    */
    if (EhdProv1_L5K1 && // If non-null (guard against out-of-memory from RegisterSet).
        EhdProv1_L5K1->Enabled()) // Only build and write if event is enabled.
    {
        eb.Reset("Name1", 0x123); // Clear the previous event (if any), then set event name and tag.
        eb.IdVersion(1, 2); // Set the event's durable id (if any).
        eb.Opcode(event_opcode_activity_start); // Set the event's opcode (if any).
        eb.AddValue("u8", (uint8_t)1, event_field_format_default); // Default format for 8-bit is unsigned.
        eb.AddValue("guid", *(ehd::Value128 const*)guid1, event_field_format_uuid); // Use Value128 struct for GUID and IPv6.
        eb.AddStruct("struct", 1, 0, &bookmark); // The next N fields are sub-fields of "struct".
            eb.AddString<char>("str", "str_val", event_field_format_default); // Default format for string is UTF.
            eb.AddNulTerminatedString("str", std::wstring_view(L"zstr_\0val"), event_field_format_default); // Chars after '\0' ignored.
            eb.SetStructFieldCount(bookmark, 2); // Update N to be 2.
        eb.AddValueRange("UintRange", &arrayOf5Bytes[0], &arrayOf5Bytes[5], event_field_format_default);
        eb.AddStringRange<char>("StringRange", &CharStrings[0], &CharStrings[2], event_field_format_default);
        eb.AddNulTerminatedStringRange<wchar_t>("NtStringRange", &WcharStrings[0], &WcharStrings[2], event_field_format_default);
        eb.AddValue("u32", (uint32_t)1, event_field_format_default);
        err = eb.Write(*EhdProv1_L5K1, guid1, guid1); // Write the event. Error code is only for debugging.
        printf("EhdProv1_L5K1: %u\n", err);
    }

    /*
    For convenience (nicer syntax), the Enabled(eventSet) function returns
    true if eventSet is non-null and enabled.
    */
    if (Enabled(EhdProv2_L4K2Gmygroup)) // If non-null and enabled.
    {
        /*
        If you prefer, you can use functional style to build and write the
        event in one statement.
        */
        err = eb.Reset("Name2")
            .IdVersion(1, 2) // Set the event's durable id (if any).
            .Opcode(event_opcode_activity_start) // Set the event's opcode (if any).
            .AddValue("u8", (uint8_t)1, event_field_format_default) // Default format for 8-bit is unsigned.
            .AddValue("guid", *(ehd::Value128 const*)guid1, event_field_format_uuid) // Use Value128 struct for GUID and IPv6.
            .AddStruct("struct", 2) // The next 2 fields are sub-fields of "struct".
                .AddString<char>("str", "str_val", event_field_format_default) // Default format for string is UTF.
                .AddNulTerminatedString("str", std::wstring_view(L"zstr_\0val"), event_field_format_default) // Chars after '\0' ignored.
            .AddValueRange("UintRange", &arrayOf5Bytes[0], &arrayOf5Bytes[5], event_field_format_default)
            .AddStringRange<char>("StringRange", &CharStrings[0], &CharStrings[2], event_field_format_default)
            .AddNulTerminatedStringRange<wchar_t>("NtStringRange", &WcharStrings[0], &WcharStrings[2], event_field_format_default)
            .AddValue("u32", (uint32_t)1, event_field_format_default)
            .Write(*EhdProv2_L4K2Gmygroup);
        printf("EhdProv2_L4K2Gmygroup: %u\n", err);
    }

    return 0;
}

#include <errno.h>
static_assert(EBADF == 9, "EBADF != 9");
