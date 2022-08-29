// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gc.h"

#define BOOL_CONFIG(name, unused_private_key, unused_public_key, default, unused_doc) \
  bool GCConfig::Get##name() { return s_##name; }                                     \
  void GCConfig::Set##name(bool value) { s_Updated##name = value; }                   \
  bool GCConfig::s_##name = default;                                                  \
  bool GCConfig::s_Updated##name = default;

#define INT_CONFIG(name, unused_private_key, unused_public_key, default, unused_doc)  \
  int64_t GCConfig::Get##name() { return s_##name; }                                  \
  void GCConfig::Set##name(int64_t value) { s_Updated##name = value; }              \
  int64_t GCConfig::s_##name = default;                                               \
  int64_t GCConfig::s_Updated##name = default;

// String configs are not cached because 1) they are rare and
// not on hot paths and 2) they involve transfers of ownership
// of EE-allocated strings, which is potentially complicated.
#define STRING_CONFIG(name, private_key, public_key, unused_doc)  \
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

void GCConfig::EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc)
{
#define INT_CONFIG(name, unused_private_key, public_key, default, unused_doc) \
    configurationValueFunc(context, (void*)(#name), (void*)(public_key), GCConfigurationType::Int64, static_cast<int64_t>(s_Updated##name));
    
#define STRING_CONFIG(name, private_key, public_key, unused_doc)                     \
    {                                                                                \
        const char* resultStr = nullptr;                                             \
        GCToEEInterface::GetStringConfigValue(private_key, public_key, &resultStr);  \
        GCConfigStringHolder holder(resultStr);                                      \
        configurationValueFunc(context, (void*)(#name), (void*)(public_key), GCConfigurationType::StringUtf8, reinterpret_cast<int64_t>(resultStr)); \
    }

#define BOOL_CONFIG(name, unused_private_key, public_key, default, unused_doc) \
    configurationValueFunc(context, (void*)(#name), (void*)(public_key), GCConfigurationType::Boolean, static_cast<int64_t>(s_Updated##name));

GC_CONFIGURATION_KEYS

#undef BOOL_CONFIG
#undef INT_CONFIG
#undef STRING_CONFIG
}

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

bool ParseGCHeapAffinitizeRanges(const char* cpu_index_ranges, AffinitySet* config_affinity_set, uintptr_t& config_affinity_mask)
{
    bool success = true;

    // Case 1: config_affinity_mask and config_affinity_set are both null. No affinitization. 
    // Case 2: config_affinity_mask is not null but config_affinity_set is null. Affinitization is based on config_affinity_mask.
    if (cpu_index_ranges == nullptr)
    {
        // Case 2.5: If CPU Groups are enabled, however, if the user passes in the config_affinity_mask, it can't apply. 
        // Therefore, we return a CLR_E_GC_BAD_AFFINITY_CONFIG_FORMAT error.
        if (config_affinity_mask != 0 && GCToOSInterface::CanEnableGCCPUGroups())
        {
            success = false;
        }

        return success;
    }

    // Case 3: config_affinity_mask is null but cpu_index_ranges isn't.
    // To facilitate the case where there are less than 65 cores but the user passes in an affinitized range associated
    // with the 0th CPU Group, we override the config_affinity_mask with the same contents as the cpu_index_ranges. 
    else if (config_affinity_mask == 0 && cpu_index_ranges != nullptr)
    {
        // Unix:
        //  The cpu index ranges is a comma separated list of indices or ranges of indices (e.g. 1-5).
        //  Example 1,3,5,7-9,12
        // Windows:
        //  The cpu index ranges is a comma separated list of group-annotated indices or ranges of indices.
        //  The group number always prefixes index or range and is followed by colon.
        //  Example 0:1,0:3,0:5,1:7-9,1:12

        if (cpu_index_ranges != nullptr)
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

                static const size_t BitsPerBitsetEntry = 8 * sizeof(uintptr_t);

                for (size_t i = start_index; i <= end_index; i++)
                {
                    config_affinity_set->Add(i);
                    config_affinity_mask |= (uintptr_t)1 << (i & (BitsPerBitsetEntry - 1));
                }

                number_end = cpu_index_ranges;
                cpu_index_ranges++;
            }
            while (*number_end == ',');

            success = (*number_end == '\0');
        }
    }

    return success;
}
