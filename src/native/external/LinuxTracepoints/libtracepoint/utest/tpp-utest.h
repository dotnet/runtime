// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/tracepoint-provider.h>

#define PASTE2(a, b)        PASTE2_imp(a, b)
#define PASTE2_imp(a, b)    a##b
#define STRINGIZE(x) STRINGIZE_imp(x)
#define STRINGIZE_imp(x) #x

#define TestProvider    PASTE2(TestProvider_, C_OR_CPP)
#define FUNC1           PASTE2(func1_, C_OR_CPP)
#define FUNC1_ENABLED   PASTE2(FUNC1, _enabled)
#define FUNC2           PASTE2(func2_, C_OR_CPP)
#define FUNC2_ENABLED   PASTE2(FUNC2, _enabled)
#define SUFFIX          "_" STRINGIZE(C_OR_CPP)

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    void PrintErr(char const* operation, int err);
    int TestC(void);
    int TestCpp(void);

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus

TPP_DECLARE_PROVIDER(TestProviderC);
TPP_DECLARE_PROVIDER(TestProviderCpp);

TPP_FUNCTION(TestProvider, "func1" SUFFIX, FUNC1);

TPP_FUNCTION(TestProvider, "func2" SUFFIX, FUNC2,
    TPP_INT32("data0", data0),
    TPP_CUSTOM_REL_LOC("u32[] data1", int, data1_len, data1),
    TPP_CUSTOM_REL_LOC("unsigned[] data2", int, data2_len, data2),
    TPP_CUSTOM_REL_LOC("u32[] data3", int, data3_len, data3),
    TPP_STRING("data4", data4),
    TPP_CHAR_ARRAY("data5", 7, data5),
    TPP_STRUCT_PTR("data6", "MY_STRUCT", 8, data6));

static int TestCommon(void)
{
    int ok = 1;
    int err;

    int values[] = { 0x31323334, 0x35363738 };

    err = TPP_WRITE(TestProvider, "write1" SUFFIX);
    PrintErr("write1", err);

    err = TPP_WRITE(TestProvider, "write2" SUFFIX,
        TPP_INT32("data0", values[0]),
        TPP_CUSTOM_REL_LOC("u32[] data1", int, 0, values),
        TPP_CUSTOM_REL_LOC("unsigned[] data2", int, sizeof(values[0]), values),
        TPP_CUSTOM_REL_LOC("u32[] data3", int, sizeof(values), values),
        TPP_STRING("data4", "ABC123"),
        TPP_CHAR_ARRAY("data5", 7, "abc1234"),
        TPP_STRUCT_PTR("data6", "MY_STRUCT", 8, &values[0]));
    PrintErr("write2", err);

    if (FUNC1_ENABLED())
    {
        err = FUNC1();
        PrintErr("func1", err);
    }

    if (FUNC2_ENABLED())
    {
        err = FUNC2(values[0], 0, values, sizeof(values[0]), values, sizeof(values), values, "ABC123", "abc1234", &values[0]);
        PrintErr("func2", err);
    }

    return ok;
}

TPP_DEFINE_PROVIDER(TestProvider);
