// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/TraceLoggingProvider.h>
#include <stdio.h>
#include <errno.h>
#include <inttypes.h>

TRACELOGGING_DEFINE_PROVIDER(
    LongProvider,
    "Long_Provider_Name_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX0123456789",
    // {7a442600-4333-5126-6401-08ff132396f0}
    (0x7a442600, 0x4333, 0x5126, 0x64, 0x01, 0x08, 0xff, 0x13, 0x23, 0x96, 0xf0),
    TraceLoggingOptionGroupName("asdf"));

extern "C" {

    bool TestC(void);
    bool TestCpp(void);

} // extern "C"

extern char const* g_interceptorFileName;

int main(int argc, char* argv[])
{
    if (argc > 1)
    {
        g_interceptorFileName = argv[1];
    }
    else
    {
        g_interceptorFileName = "EventHeaderInterceptor"
#if __BYTE_ORDER == __LITTLE_ENDIAN
            "LE"
#elif __BYTE_ORDER == __BIG_ENDIAN
            "BE"
#endif
#if __SIZEOF_POINTER__ == 8
            "64"
#elif __SIZEOF_POINTER__ == 4
            "32"
#endif
            ".dat"
            ;
    }

    bool allOk = true;
    bool oneOk;

    int result;

    if (remove(g_interceptorFileName))
    {
        printf("Error %u clearing output file %s\n", errno, g_interceptorFileName);
        allOk = false;
    }

    char str[EVENTHEADER_COMMAND_MAX];
    result = EVENTHEADER_FORMAT_COMMAND(str, EVENTHEADER_COMMAND_MAX,
        TraceLoggingProviderName(LongProvider), -1, -1, TraceLoggingProviderOptions(LongProvider));
    printf("%d %s\n", result, str);
    result = EVENTHEADER_FORMAT_TRACEPOINT_NAME(str, EVENTHEADER_NAME_MAX,
        TraceLoggingProviderName(LongProvider), -1, -1, TraceLoggingProviderOptions(LongProvider));
    printf("%d %s\n", result, str);

    TraceLoggingRegister(LongProvider);
    TraceLoggingWrite(LongProvider, "LongProviderEvent");
    TraceLoggingUnregister(LongProvider);

    oneOk = TestC();
    printf("TestProvider: %s\n", oneOk ? "ok" : "ERROR");
    allOk &= oneOk;

    oneOk = TestCpp();
    printf("TestProvider: %s\n", oneOk ? "ok" : "ERROR");
    allOk &= oneOk;

    printf("Events saved to \"%s\".\n", g_interceptorFileName);

    return !allOk;
}
