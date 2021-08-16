// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gc.h"

#define BOOL_CONFIG(name, unused_private_key, unused_public_key, default, unused_doc) \
  bool GCConfig::Get##name() { return s_##name; }                                     \
  bool GCConfig::s_##name = default;

#define INT_CONFIG(name, unused_private_key, unused_public_key, default, unused_doc)  \
  int64_t GCConfig::Get##name() { return s_##name; }                                  \
  int64_t GCConfig::s_##name = default;

// String configs are not cached because 1) they are rare and
// not on hot paths and 2) they involve transfers of ownership
// of EE-allocated strings, which is potentially complicated.
#define STRING_CONFIG(name, private_key, public_key, unused_doc)                   \
  GCConfigStringHolder GCConfig::Get##name()                                       \
  {                                                                                \
      const char* resultStr = nullptr;                                             \
      GCToEEInterface::GetStringConfigValue(private_key, public_key, &resultStr);  \
      return GCConfigStringHolder(resultStr);                                      \
  }

GC_CONFIGURATION_KEYS

#undef BOOL_CONFIG
#undef INT_CONFIG
#undef STRING_CONFIG

void GCConfig::Initialize()
{
#define BOOL_CONFIG(name, private_key, public_key, default, unused_doc)          \
    GCToEEInterface::GetBooleanConfigValue(private_key, public_key, &s_##name);

#define INT_CONFIG(name, private_key, public_key, default, unused_doc)           \
    GCToEEInterface::GetIntConfigValue(private_key, public_key, &s_##name);

#define STRING_CONFIG(unused_name, unused_private_key, unused_public_key, unused_doc)

GC_CONFIGURATION_KEYS

#undef BOOL_CONFIG
#undef INT_CONFIG
#undef STRING_CONFIG
}

// Parse an integer index or range of two indices separated by '-'.
// Updates the config_string to point to the first character after the parsed part
bool ParseIndexOrRange(const char** config_string, size_t* start_index, size_t* end_index)
{
    char* number_end;
    size_t start = strtoul(*config_string, &number_end, 10);

    if (number_end == *config_string)
    {
        // No number found, invalid format
        return false;
    }

    size_t end = start;

    if (*number_end == '-')
    {
        char* range_end_start = number_end + 1;
        end = strtoul(range_end_start, &number_end, 10);
        if (number_end == range_end_start)
        {
            // No number found, invalid format
            return false;
        }
    }

    *start_index = start;
    *end_index = end;

    *config_string = number_end;

    return true;
}

bool ParseGCHeapAffinitizeRanges(const char* cpu_index_ranges, AffinitySet* config_affinity_set)
{
    bool success = true;

    // Unix:
    //  The cpu index ranges is a comma separated list of indices or ranges of indices (e.g. 1-5).
    //  Example 1,3,5,7-9,12
    // Windows:
    //  The cpu index ranges is a comma separated list of group-annotated indices or ranges of indices.
    //  The group number always prefixes index or range and is followed by colon.
    //  Example 0:1,0:3,0:5,1:7-9,1:12

    if (cpu_index_ranges != NULL)
    {
        const char* number_end = cpu_index_ranges;

        do
        {
            size_t start_index, end_index;
            if (!GCToOSInterface::ParseGCHeapAffinitizeRangesEntry(&cpu_index_ranges, &start_index, &end_index))
            {
                break;
            }

            if ((start_index >= MAX_SUPPORTED_CPUS) || (end_index >= MAX_SUPPORTED_CPUS) || (end_index < start_index))
            {
                // Invalid CPU index values or range
                break;
            }

            for (size_t i = start_index; i <= end_index; i++)
            {
                config_affinity_set->Add(i);
            }

            number_end = cpu_index_ranges;
            cpu_index_ranges++;
        }
        while (*number_end == ',');

        success = (*number_end == '\0');
    }

    return success;
}
