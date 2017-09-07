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
