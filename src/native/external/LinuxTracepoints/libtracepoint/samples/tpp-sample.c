#include <tracepoint/tracepoint-provider.h>

/*
Demonstrates basic usage of the high-level tracepoint-provider.h API.

The tracepoint-provider.h API is a developer-friendly wrapper for the
functionality provided by <tracepoint/tracepoint.h>.
*/

// A provider is a collection of tracepoints that will be registered and
// unregistered as a group.
TPP_DEFINE_PROVIDER(MyProvider);

// This defines a my_tracepoint1_func(...) function that generates a
// "my_tracepoint1" tracepoint with 3 fields. It also generates a
// my_tracepoint1_func_enabled() function that efficiently determines whether
// the tracepoint is registered and enabled.
TPP_FUNCTION(MyProvider, "my_tracepoint1", my_tracepoint1_func,
    TPP_UINT8("field1", int_param),
    TPP_STRING("field2", str_param),
    TPP_CHAR_ARRAY("field3", 5, five_chars));

// Defines my_tracepoint2_func() and my_tracepoint2_func_enabled().
TPP_FUNCTION(MyProvider, "my_tracepoint2", my_tracepoint2_func);

static uint8_t get_field1_value(void)
{
    return 1;
}

int main()
{
    // All tracepoints associated with MyProvider will be inactive until you
    // register MyProvider.
    TPP_REGISTER_PROVIDER(MyProvider);

    // If collecting the data for a tracepoint is expensive,
    // check the enabled() function before collecting the data.
    if (my_tracepoint1_func_enabled())
    {
        int field1 = get_field1_value();
        char const* field2 = "value for field 2";
        char const* field3 = "5char";
        my_tracepoint1_func(field1, field2, field3);
    }

    // While it's usually more efficient to check enabled() first, it's ok to
    // call the function without checking enabled().
    my_tracepoint2_func();

    // If you are only generating a tracepoint from one code path, use
    // TPP_WRITE instead of TPP_FUNCTION. TPP_WRITE generates the tracepoint
    // inline - no need for a separate function. The value expressions are only
    // evaluated if the specified tracepoint is enabled, e.g. this only calls
    // get_field1_value() if the tracepoint is registered and enabled.
    TPP_WRITE(MyProvider, "my_tracepoint3",
        TPP_UINT8("field1", get_field1_value()),
        TPP_STRING("field2", "value for field 2"),
        TPP_CHAR_ARRAY("field3", 5, "5char"));

    TPP_WRITE(MyProvider, "my_tracepoint4");

    // Unregister is especially important for shared objects that can unload.
    TPP_UNREGISTER_PROVIDER(MyProvider);
    return 0;
}
