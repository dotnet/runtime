// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Provides simple configuration support through environment variables. Each variable is lazily inspected on
// first query and the resulting value cached for future use. To keep things simple we support reading only
// 32-bit hex quantities and a zero value is considered equivalent to the environment variable not being
// defined. We can get more sophisticated if needs be, but the hope is that very few configuration values are
// exposed in this manner.
//
// Values can also be embedded in the compiled binary.
//

#ifndef RHCONFIG_H
#define RHCONFIG_H

#ifndef DACCESS_COMPILE

#include <sal.h>

class RhConfig
{
#define CONFIG_VAL_MAXLEN 16              //64 bit uint in hex
public:
    struct Config
    {
        uint32_t m_count;
        char* m_first[];
    public:
        uint32_t GetCount() { return m_count; }
        char* GetKeyAt(int32_t index) { return m_first[index]; }
        char* GetValueAt(int32_t index) { return m_first[m_count + index]; }
        char** GetKeys() { return m_first; }
        char** GetValues() { return &m_first[m_count]; }
    };

    class Environment
    {
    public: // static
        static bool TryGetBooleanValue(const char* name, bool* value);
        static bool TryGetIntegerValue(const char* name, uint64_t* value, bool decimal = false);

        // Get environment variable configuration as a string. On success, the caller owns the returned string value.
        static bool TryGetStringValue(const char* name, char** value);
    };

    bool ReadConfigValue(_In_z_ const char* wszName, uint64_t* pValue, bool decimal = false);
    bool ReadKnobUInt64Value(_In_z_ const char* wszName, uint64_t* pValue);
    bool ReadKnobBooleanValue(_In_z_ const char* wszName, bool* pValue);

    char** GetKnobNames();
    char** GetKnobValues();
    uint32_t GetKnobCount();

#define DEFINE_VALUE_ACCESSOR(_name, defaultVal)        \
    uint64_t Get##_name()                                 \
    {                                                   \
        if (m_uiConfigValuesRead & (1 << RCV_##_name))  \
            return m_uiConfigValues[RCV_##_name];       \
        uint64_t uiValue;                               \
        m_uiConfigValues[RCV_##_name] = ReadConfigValue(#_name, &uiValue) ? uiValue : defaultVal; \
        m_uiConfigValuesRead |= 1 << RCV_##_name;       \
        return m_uiConfigValues[RCV_##_name];           \
    }


#ifdef _DEBUG
#define DEBUG_CONFIG_VALUE(_name) DEFINE_VALUE_ACCESSOR(_name, 0)
#define DEBUG_CONFIG_VALUE_WITH_DEFAULT(_name, defaultVal) DEFINE_VALUE_ACCESSOR(_name, defaultVal)
#else
#define DEBUG_CONFIG_VALUE(_name)
#define DEBUG_CONFIG_VALUE_WITH_DEFAULT(_name, defaultVal)
#endif
#define RETAIL_CONFIG_VALUE(_name) DEFINE_VALUE_ACCESSOR(_name, 0)
#define RETAIL_CONFIG_VALUE_WITH_DEFAULT(_name, defaultVal) DEFINE_VALUE_ACCESSOR(_name, defaultVal)
#include "RhConfigValues.h"
#undef DEBUG_CONFIG_VALUE
#undef RETAIL_CONFIG_VALUE
#undef DEBUG_CONFIG_VALUE_WITH_DEFAULT
#undef RETAIL_CONFIG_VALUE_WITH_DEFAULT

private:

    enum RhConfigValue
    {
#define DEBUG_CONFIG_VALUE(_name) RCV_##_name,
#define RETAIL_CONFIG_VALUE(_name) RCV_##_name,
#define DEBUG_CONFIG_VALUE_WITH_DEFAULT(_name, defaultVal) RCV_##_name,
#define RETAIL_CONFIG_VALUE_WITH_DEFAULT(_name, defaultVal) RCV_##_name,
#include "RhConfigValues.h"
#undef DEBUG_CONFIG_VALUE
#undef RETAIL_CONFIG_VALUE
#undef DEBUG_CONFIG_VALUE_WITH_DEFAULT
#undef RETAIL_CONFIG_VALUE_WITH_DEFAULT
        RCV_Count
    };

//accomidate for the maximum number of config values plus sizable buffer for whitespace 2K
#define CONFIG_FILE_MAXLEN RCV_Count * sizeof(ConfigPair) + 2000

private:
    // Gets a pointer to the embedded configuration value. Memory is held by the callee.
    // Returns true if the variable was found, false otherwise
    bool GetEmbeddedVariable(Config* config, _In_z_ const char* configName, bool caseSensitive, _Out_ const char** configValue);

    uint32_t  m_uiConfigValuesRead;
    uint64_t  m_uiConfigValues[RCV_Count];
};

extern RhConfig * g_pRhConfig;

#endif //!DACCESS_COMPILE

#endif // RHCONFIG_H
