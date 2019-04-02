// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gcenv.h"
#include "gc.h"

#define BOOL_CONFIG(name, key, default, unused_doc)            \
  bool GCConfig::Get##name() { return s_##name; }              \
  bool GCConfig::s_##name = default;

#define INT_CONFIG(name, key, default, unused_doc)             \
  int64_t GCConfig::Get##name() { return s_##name; }           \
  int64_t GCConfig::s_##name = default;

// String configs are not cached because 1) they are rare and
// not on hot paths and 2) they involve transfers of ownership
// of EE-allocated strings, which is potentially complicated.
#define STRING_CONFIG(name, key, unused_doc)                   \
  GCConfigStringHolder GCConfig::Get##name()                   \
  {                                                            \
      const char* resultStr = nullptr;                         \
      GCToEEInterface::GetStringConfigValue(key, &resultStr);  \
      return GCConfigStringHolder(resultStr);                  \
  }

GC_CONFIGURATION_KEYS

#undef BOOL_CONFIG
#undef INT_CONFIG
#undef STRING_CONFIG

void GCConfig::Initialize()
{
#define BOOL_CONFIG(name, key, default, unused_doc)          \
    GCToEEInterface::GetBooleanConfigValue(key, &s_##name);

#define INT_CONFIG(name, key, default, unused_doc)           \
    GCToEEInterface::GetIntConfigValue(key, &s_##name);

#define STRING_CONFIG(unused_name, unused_key, unused_doc)

GC_CONFIGURATION_KEYS

#undef BOOL_CONFIG
#undef INT_CONFIG
}

bool ParseGCHeapAffinitizeRanges(AffinitySet* config_affinity_set)
{
    bool success = true;

    GCConfigStringHolder cpu_index_ranges_holder(GCConfig::GetGCHeapAffinitizeRanges());
    const char* cpu_index_ranges = cpu_index_ranges_holder.Get();

    // The cpu index ranges is a comma separated list of indices or ranges of indices (e.g. 1-5).
    // Example 1,3,5,7-9,12

    if (cpu_index_ranges != NULL)
    {
        char* number_end;

        do
        {
            size_t start_index = strtoul(cpu_index_ranges, &number_end, 10);

            if (number_end == cpu_index_ranges)
            {
                // No number found, invalid format
                break;
            }

            size_t end_index = start_index;

            if (*number_end == '-')
            {
                char* range_end_start = number_end + 1;
                end_index = strtoul(range_end_start, &number_end, 10);
                if (number_end == range_end_start)
                {
                    // No number found, invalid format
                    break;
                }
            }

            if ((start_index < MAX_SUPPORTED_CPUS) && end_index < (MAX_SUPPORTED_CPUS))
            {
                for (size_t i = start_index; i <= end_index; i++)
                {
                    config_affinity_set->Add(i);
                }
            }

            cpu_index_ranges = number_end + 1;
        }
        while (*number_end == ',');

        success = (*number_end == '\0');
    }

    return success;
}
