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


#ifndef DACCESS_COMPILE

class RhConfig
{

#define CONFIG_INI_NOT_AVAIL (void*)0x1  //signal for ini file failed to load
#define CONFIG_KEY_MAXLEN 50             //arbitrary max length of config keys increase if needed
#define CONFIG_VAL_MAXLEN 16              //64 bit uint in hex

private:
    struct ConfigPair
    {
    public:
        char Key[CONFIG_KEY_MAXLEN + 1];  //maxlen + null terminator
        char Value[CONFIG_VAL_MAXLEN + 1]; //maxlen + null terminator
    };

    // g_embeddedSettings is a buffer of ConfigPair structs embedded in the compiled binary.
    //
    //NOTE: g_embeddedSettings is only set in ReadEmbeddedSettings and must be set atomically only once
    //      using PalInterlockedCompareExchangePointer to avoid races when initializing
    void* volatile g_embeddedSettings = NULL;

public:

    bool ReadConfigValue(_In_z_ const char* wszName, uint64_t* pValue, bool decimal = false);

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
    //Parses one line of config and populates values in the passed in configPair
    //returns: true if the parsing was successful, false if the parsing failed.
    //NOTE: if the method fails configPair is left in an uninitialized state
    bool ParseConfigLine(_Out_ ConfigPair* configPair, _In_z_ const char * line);

    void ReadEmbeddedSettings();

    // Gets a pointer to the embedded configuration value. Memory is held by the callee.
    // Returns true if the variable was found, false otherwise
    bool GetEmbeddedVariable(_In_z_ const char* configName, _Out_ const char** configValue);

    uint32_t  m_uiConfigValuesRead;
    uint64_t  m_uiConfigValues[RCV_Count];
};

extern RhConfig * g_pRhConfig;

#endif //!DACCESS_COMPILE
