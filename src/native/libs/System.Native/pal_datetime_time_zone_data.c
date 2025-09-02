// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "pal_datetime.h"
#include <stdint.h>
#include <string.h>
#include <assert.h>
#include <stdbool.h>
#include <minipal/utils.h>

#ifdef TZ_DATA_ENABLED
#include <pal_datetime_time_zone_data-config.h>

#define STR_IMPL(...) #__VA_ARGS__
#define STR(...) STR_IMPL(__VA_ARGS__)

// Generate both the index and data in one macro loop, switching
// between their respective sections:
//
// foreach (data_id in TZ_FILES)
//   switch to the 'data' section
//     data_id_i:
//       .incbin TZ_FILES_DIR/data_id
//   switch to the 'index' section
//       g_dataIndex[i] = data_id_i
//
// Also add the end location of data to the index, so that when
// searching, the last entry doesn't need to be special-cased.
//
__asm(
#ifdef TARGET_64BIT
#define POINTER_SIZE  "8"
#define POINTER_RELOC ".int64"
#else
#define POINTER_SIZE  "4"
#define POINTER_RELOC ".int32"
#endif

    "  .macro GENERATE_SINGLE_DATA data_id\n"
    "  .section .data.tzdata,\"\",@\n"
    "data_id_\\@:\n"
    "  .size data_id_\\@, 0\n"
    "  .incbin \"" TZ_FILES_DIR "\\data_id\"\n"
    "  .section .data.tzdata.index,\"\",@\n"
    "  " POINTER_RELOC " data_id_\\@\n"
    "  .endm\n"

    "  .section .data.tzdata.index,\"\",@\n"
    "  .balign " POINTER_SIZE "\n"
    "g_dataIndex:\n"

    "  .section .data.tzdata,\"\",@\n"
    "data:\n"
    "  .irp data_id," STR(TZ_FILES) "\n"
    "  GENERATE_SINGLE_DATA \\data_id\n"
    "  .endr\n"
    "  .section .data.tzdata,\"\",@\n"
    "data_end:"
    "  .size data, data_end - data\n"
    "  .size data_end, 0\n"
    "  .section .data.tzdata.index,\"\",@\n"
    "  " POINTER_RELOC " data_end\n"
    "  .size g_dataIndex, . - g_dataIndex\n"
);

static const char *g_nameIndex[] = { TZ_FILES };
extern const char *g_dataIndex[];
#endif // TZ_DATA_ENABLED

const char* SystemNative_GetTimeZoneData(const char* name, int* length)
{
#ifdef TZ_DATA_ENABLED
    // Small size and speed optimization: skip comparing the prefix.
    static const char TZ_PREFIX[] = "/usr/share/zoneinfo/";
    static const size_t TZ_PREFIX_LENGTH = STRING_LENGTH(TZ_PREFIX);

    // TODO: use a binary search here. The index is ~500 entries long.
    assert(strncmp(TZ_PREFIX, name, TZ_PREFIX_LENGTH) == 0);
    for (size_t i = 0; i < ARRAY_SIZE(g_nameIndex); i++)
    {
        if (strcmp(name + TZ_PREFIX_LENGTH, g_nameIndex[i]) == 0)
        {
            *length = (int)(g_dataIndex[i + 1] - g_dataIndex[i]);
            return g_dataIndex[i];
        }
    }
#endif // TZ_DATA_ENABLED

    *length = 0;
    return NULL;
}
