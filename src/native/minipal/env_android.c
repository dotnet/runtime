// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipalconfig.h"
#include <errno.h>
#include <assert.h>
#include <stdint.h>
#include <string.h>
#include <ctype.h>
#include <stdio.h>
#include "minipal/env.h"
#include "minipal/volatile.h"
#include "minipal/utils.h"
#include "minipal/strings.h"

#ifdef HOST_ANDROID
#include <sys/system_properties.h>

// Android system properties are checked once and cached in a global variable.
// UINT32_MAX means not initialized, 0 means disabled, 1 means enabled.
static volatile uint32_t g_android_system_properties_enabled = UINT32_MAX;

// Passed to __system_property_foreach callback.
typedef struct _SystemPropertyValues
{
    int index;
    int max;
    char** values;
} SystemPropertyValues;

// Prefixes and names used by dotnet Android system properties.
#define ENV_DOTNET_PREFIX "DOTNET_"
#define ENV_DOTNET_PREFIX_LEN STRING_LENGTH(ENV_DOTNET_PREFIX)
#define SYS_PROPS_DEBUG_PREFIX "debug."
#define SYS_PROPS_DEBUG_PREFIX_LEN STRING_LENGTH(SYS_PROPS_DEBUG_PREFIX)
#define SYS_PROPS_DOTNET_PREFIX "dotnet."
#define SYS_PROPS_DOTNET_PREFIX_LEN STRING_LENGTH(SYS_PROPS_DOTNET_PREFIX)
#define ENABLED_SYS_PROPS "enable_sys_props"
#define ENABLED_SYS_PROPS_LEN STRING_LENGTH(ENABLED_SYS_PROPS)

/**
 * @brief Retrieves the value of an Android system property with the specified name.
 *
 * @param name          The name of the Android system property to retrieve.
 * @param value         Buffer to receive the value of the Android system property.
 * @param value_len     Length of value in bytes, at least PROP_VALUE_MAX.
 *
 * @return Returns length if the property was found and read successfully, -1 otherwise.
 */
static int get_android_system_property(const char* name, char* value, size_t value_len)
{
    assert(value_len >= PROP_VALUE_MAX);

    int ret = -1;
    const prop_info* info = __system_property_find(name);
    if (info != NULL)
    {
        char tmp_name[PROP_NAME_MAX];
        ret = __system_property_read(info, tmp_name, value);
    }

    return ret;
}

/**
 * @brief Determines whether Android system property should be enabled.
 *
 * @return true if Android system properties are enabled, false otherwise.
 */
static bool is_android_system_properties_enabled(void)
{
    char value[PROP_VALUE_MAX];

    uint32_t enabled = minipal_volatile_load_uint32_t(&g_android_system_properties_enabled);
    if (enabled == UINT32_MAX)
    {
        int ret = get_android_system_property(SYS_PROPS_DEBUG_PREFIX SYS_PROPS_DOTNET_PREFIX ENABLED_SYS_PROPS, value, ARRAY_SIZE(value));
        if (ret == -1)
        {
            ret = get_android_system_property(SYS_PROPS_DOTNET_PREFIX ENABLED_SYS_PROPS, value, ARRAY_SIZE(value));
        }

        if (ret == 1 && *value == '1')
        {
            enabled = 1;
        }
        else
        {
            enabled = 0;
        }

        minipal_volatile_store_uint32_t(&g_android_system_properties_enabled, enabled);
    }

    assert(enabled == 0 || enabled == 1);
    return enabled == 1 ? true : false;
}

/**
 * @brief Callback function used to enumerate Android system properties.
 *
 * @param info      Pointer to the property info structure.
 * @param cookie    Pointer to user data (SystemPropertyValues).
 */
static void android_system_property_callback(const prop_info* info, void* cookie)
{
    SystemPropertyValues* sys_props = (SystemPropertyValues*)cookie;
    if (sys_props == NULL)
    {
        return;
    }

    char name[PROP_NAME_MAX];
    char value[PROP_VALUE_MAX];

    // API 26 use different read.
    int ret = __system_property_read(info, name, value);
    if (ret <= 0)
    {
        return;
    }

    const char* property_name_prefix = "";
    const char* property_delimiter = "=";
    const char* property_name = name;
    const char* property_value = value;

    if (strncmp(property_name, SYS_PROPS_DEBUG_PREFIX, SYS_PROPS_DEBUG_PREFIX_LEN) == 0)
    {
        property_name += SYS_PROPS_DEBUG_PREFIX_LEN;
    }

    if (strncmp(property_name, SYS_PROPS_DOTNET_PREFIX, SYS_PROPS_DOTNET_PREFIX_LEN) == 0)
    {
        property_name += SYS_PROPS_DOTNET_PREFIX_LEN;
        if (strncmp(property_name, ENABLED_SYS_PROPS, ENABLED_SYS_PROPS_LEN) == 0)
        {
            return;
        }

        // Ending property name with a "." and a number is a way to handle large property name and value.
        // Property ending with index 0 include property name, up to PROP_VALUE_MAX in length.
        // Property ending with index 1 - n include values. All concatenated into final value.
        // debug.dotnet.long_prop.0 = ThisIsReallyALongPropertyNameAbove32Chars
        // debug.dotnet.long_prop.1 = SomePropertyNameValue
        char* index_start = strrchr(name, '.');
        if (index_start != NULL && isdigit(index_start[1]) == 0)
        {
            index_start = NULL;
        }

        if (index_start != NULL)
        {
            index_start++;

            char* index_end = NULL;
            long index = strtoul(index_start, &index_end, 10);
            if (index_start == index_end || errno == ERANGE)
            {
                index_start = NULL;
            }
            else
            {
                if (index != 0)
                {
                    return;
                }

                *(index_start - 1) = '\0';
            }
        }

        if (sys_props->values == NULL && *property_value != '\0')
        {
            sys_props->max++;
            return;
        }

        if (sys_props->values != NULL && *property_value != '\0')
        {
            assert(sys_props->index < sys_props->max);

            if (index_start != NULL)
            {
                property_name = property_value;
                property_value = "";
            }
            else if (strchr(property_value, '=') != NULL)
            {
                property_name = property_value;
                property_value = "";
                property_delimiter = "";
            }

            if (strncmp(property_name, ENV_DOTNET_PREFIX, ENV_DOTNET_PREFIX_LEN) != 0)
            {
                property_name_prefix = ENV_DOTNET_PREFIX;
            }

            // property_name_prefix + property_name + property_delimiter + property_value + \0
            size_t alloc_size = strlen(property_name_prefix) + strlen(property_name) + strlen(property_delimiter) + 1;
            if (*property_value != '\0')
            {
                alloc_size += strlen(property_value);
            }
            else
            {
                alloc_size += PROP_VALUE_MAX;
            }

            sys_props->values[sys_props->index] = (char*)malloc(alloc_size * sizeof(char));
            if (sys_props->values[sys_props->index] != NULL)
            {
                minipal_strcpy_s(sys_props->values[sys_props->index], alloc_size, property_name_prefix);
                minipal_strcat_s(sys_props->values[sys_props->index], alloc_size, property_name);
                minipal_strcat_s(sys_props->values[sys_props->index], alloc_size, property_delimiter);
                minipal_strcat_s(sys_props->values[sys_props->index], alloc_size, property_value);

                // If the property name is indexed, we need to append the values in index 1 - n.
                if (index_start)
                {
                    int index = 1;
                    char name2[PROP_NAME_MAX];
                    char value2[PROP_VALUE_MAX];

                    size_t used_size = strlen(sys_props->values[sys_props->index]) + 1;

                    do
                    {
                        snprintf(name2, ARRAY_SIZE(name2), "%s.%d", name, index);
                        ret = get_android_system_property(name2, value2, ARRAY_SIZE(value2));
                        if (ret > 0)
                        {
                            if (used_size + ret <= alloc_size)
                            {
                                minipal_strcat_s(sys_props->values[sys_props->index], alloc_size, value2);
                            }
                            else
                            {
                                // Reallocate the buffer to fit the new value.
                                alloc_size += ret;
                                char* new_value = (char*)malloc(alloc_size * sizeof(char));
                                if (new_value != NULL)
                                {
                                    minipal_strcpy_s(new_value, alloc_size, sys_props->values[sys_props->index]);
                                    minipal_strcat_s(new_value, alloc_size, value2);
                                    free(sys_props->values[sys_props->index]);
                                    sys_props->values[sys_props->index] = new_value;
                                }
                            }

                            used_size += ret;
                            index++;
                        }
                    }
                    while (ret > 0);
                }

                sys_props->index++;
            }
        }
    }
}

/**
 * @brief Retrieves a Android system properties starting with "debug.dotnet" or "dotnet." prefix.
 *
 * @return Newly allocated string containing the value. Returns NULL if not found or if
*  Android system properties are not enabled.
 *
 * @remarks
 * - Caller is responsible freeing the returned string.
 */
static char* get_system_property(const char* name)
{
    assert(name != NULL);

    if (!is_android_system_properties_enabled())
    {
        return NULL;
    }

    char* result = NULL;

    const prop_info* info = __system_property_find(name);
    if (info != NULL)
    {
        SystemPropertyValues value = { 0 };
        char* data[1] = { 0 };

        value.max = 1;
        value.values = data;

        android_system_property_callback(info, &value);
        result = data[0];
    }

    return result;
}

/**
 * @brief Frees the memory allocated for a Android system properties.
 *
 * @param system_props Pointer to the Android system property strings to free.
 */
static void free_system_property(char* system_prop)
{
    if (system_prop != NULL)
    {
        free(system_prop);
    }
}

/**
 * @brief Retrieves all Android system properties using "debug.dotnet" or "dotnet." prefixes as an array of strings.
 *
 * @return Returns a pointer to an array of strings containing filtered Android system properties,
 * or NULL if none are found or Android system properties are not enabled.
 *
 * @remarks
 * - The returned array should be freed by calling free_system_properties.
 */
static char** get_system_properties(void)
{
    if (!is_android_system_properties_enabled())
    {
        return NULL;
    }

    SystemPropertyValues sys_props = { 0 };
    __system_property_foreach(android_system_property_callback, &sys_props);
    if (sys_props.max == 0)
    {
        return NULL;
    }

    sys_props.values = (char**)malloc((sys_props.max + 1) * sizeof(char*));
    if (sys_props.values != NULL)
    {
        memset(sys_props.values, 0, (sys_props.max + 1) * sizeof(char*));
        sys_props.index = 0;
        __system_property_foreach(android_system_property_callback, &sys_props);
    }

    return sys_props.values;
}

/**
 * @brief Frees the memory allocated for the array of Android system properties.
 *
 * @param system_props Pointer to the array of Android system property strings to free.
 */
static void free_system_properties(char** system_props)
{
    if (system_props != NULL)
    {
        for (int i = 0; system_props[i] != NULL; i++)
        {
            free(system_props[i]);
        }
        free(system_props);
    }
}

EnvironmentProvider g_minipal_env_android_system_properties_provider =
{
    ENV_ANDROID_SYSTEM_PROPERTIES_PROVIDER_TYPE,
    NULL,
    get_system_property,
    free_system_property,
    NULL,
    NULL,
    get_system_properties,
    free_system_properties
};
#endif // HOST_ANDROID
