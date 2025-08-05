// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/TraceLoggingProvider.h>

#include <inttypes.h>
#include <limits.h>
#include <stdio.h>
#include <string.h>

TRACELOGGING_DEFINE_PROVIDER(
    MyProvider,
    "MyProviderName",
    // {b7aa4d18-240c-5f41-5852-817dbf477472}
    (0xb7aa4d18, 0x240c, 0x5f41, 0x58, 0x52, 0x81, 0x7d, 0xbf, 0x47, 0x74, 0x72));

TRACELOGGING_DEFINE_PROVIDER(
    OtherProvider,
    "OtherProviderName",
    // {8ec53ac6-09b4-535e-5d19-e499de8832b4}
    (0x8ec53ac6, 0x09b4, 0x535e, 0x5d, 0x19, 0xe4, 0x99, 0xde, 0x88, 0x32, 0xb4),
    TraceLoggingOptionGroupName("mygroup"));

int
main(int argc, char** argv)
{
    int err = 0;

    printf("\n");

    TraceLoggingRegister(MyProvider);
    TraceLoggingRegister(OtherProvider);

    for (unsigned iteration = 1;; iteration += 1)
    {
        event_level const event1_level = event_level_information;
        uint64_t const event1_keyword = 0x1;

        // For sample purposes, show whether Event1 is currently enabled.
        // TraceLoggingProviderEnabled is usually unnecessary because every
        // TraceLoggingWrite automatically checks its own enable state.
        printf("MyProviderName_L4K1 Event1 status=%x\n",
            TraceLoggingProviderEnabled(MyProvider, event1_level, event1_keyword));

        // If Event1 is enabled then evaluate args, pack fields, write the event.
        TraceLoggingWrite(
            MyProvider,                               // Provider to use for the event.
            "Event1",                                 // Event name.
            TraceLoggingLevel(event1_level),          // Event severity level.
            TraceLoggingKeyword(event1_keyword),      // Event category bits.
            TraceLoggingInt32(argc, "ArgC"),          // Int32 field named "ArgC".
            TraceLoggingStruct(2, "Structure"),       // The following 2 fields are part of "Structure".
                TraceLoggingValue(argc, "ArgCount"),  // int field named "ArgCount".
                TraceLoggingString(argv[0], "Arg0"),  // char string field named "Arg0".
            TraceLoggingUInt32(iteration));           // uint32 field named "iteration".

        event_level const event2_level = event_level_verbose;
        uint64_t const event2_keyword = 0x23;

        // For sample purposes, show whether Event2 is currently enabled.
        printf("OtherProviderName_L5K23Gmygroup Event2 status=%x\n",
            TraceLoggingProviderEnabled(OtherProvider, event2_level, event2_keyword));

        // If Event2 is enabled then evaluate args, pack fields, write the event.
        TraceLoggingWrite(
            OtherProvider,
            "Event2",
            TraceLoggingLevel(event2_level),
            TraceLoggingKeyword(event2_keyword),
            TraceLoggingUInt32(iteration),
            TraceLoggingString(NULL),
            TraceLoggingString(argv[0], "argv0"),
            TraceLoggingStruct(1, "struct"),
                TraceLoggingCountedString(argv[0], (uint16_t)strlen(argv[0]), "cargv0"),
            TraceLoggingBinary(argv[0], 2, "bin", "desc"),
            TraceLoggingCharArray(argv[0], 2, "vchar", "desc", 123),
            TraceLoggingCharFixedArray(argv[0], 2, "cchar"));

        printf("Press enter to refresh, x + enter to exit...\n");
        char ch = (char)getchar();
        if (ch == 'x' || ch == 'X')
        {
            break;
        }
        while (ch != '\n')
        {
            ch = (char)getchar();
        }
    }

    TraceLoggingUnregister(MyProvider);
    TraceLoggingUnregister(OtherProvider);
    return err;
}
