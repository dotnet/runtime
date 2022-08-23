// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "jitconfig.h"

JitConfigValues JitConfig;

void JitConfigValues::MethodSet::initialize(const WCHAR* list, ICorJitHost* host)
{
    assert(m_list == nullptr);
    assert(m_names == nullptr);

    // Convert the input list to UTF-8
    int utf8ListLen = WszWideCharToMultiByte(CP_UTF8, 0, list, -1, nullptr, 0, nullptr, nullptr);
    if (utf8ListLen == 0)
    {
        return;
    }
    else
    {
        // char* m_list;
        //
        m_list = static_cast<char*>(host->allocateMemory(utf8ListLen));
        if (WszWideCharToMultiByte(CP_UTF8, 0, list, -1, static_cast<LPSTR>(m_list), utf8ListLen, nullptr, nullptr) ==
            0)
        {
            // Failed to convert the list. Free the memory and ignore the list.
            host->freeMemory(static_cast<void*>(m_list));
            m_list = nullptr;
            return;
        }
    }

    auto commitPattern = [this, host](const char* start, const char* end) {
        if (end <= start)
        {
            return;
        }

        MethodName* name          = static_cast<MethodName*>(host->allocateMemory(sizeof(MethodName)));
        name->m_next              = m_names;
        name->m_patternStart      = start;
        name->m_patternEnd        = end;
        name->m_containsClassName = memchr(start, ':', end - start) != nullptr;
        name->m_containsSignature = memchr(start, '(', end - start) != nullptr;
        m_names                   = name;
    };

    const char* curPatternStart = m_list;
    const char* curChar;
    for (curChar = curPatternStart; *curChar != '\0'; curChar++)
    {
        if ((curChar == curPatternStart) && (*curChar == '"'))
        {
            do
            {
                curChar++;
            } while (*curChar != '"' && *curChar != '\0');

            // Remove initial quotation mark
            commitPattern(curPatternStart + 1, curChar);
            curPatternStart = curChar + 1;
        }
        else if (*curChar == ' ')
        {
            commitPattern(curPatternStart, curChar);
            curPatternStart = curChar + 1;
        }
    }

    commitPattern(curPatternStart, curChar);
}

void JitConfigValues::MethodSet::destroy(ICorJitHost* host)
{
    // Free method names, free the list string, and reset our state
    for (MethodName *name = m_names, *next = nullptr; name != nullptr; name = next)
    {
        next = name->m_next;
        host->freeMemory(static_cast<void*>(name));
    }
    if (m_list != nullptr)
    {
        host->freeMemory(static_cast<void*>(m_list));
        m_list = nullptr;
    }
    m_names = nullptr;
}

static bool matchGlob(const char* pattern, const char* patternEnd, const char* str)
{
    while (true)
    {
        if (pattern == patternEnd)
        {
            return *str == '\0';
        }

        if (*pattern == '*')
        {
            while (true)
            {
                if (matchGlob(pattern + 1, patternEnd, str))
                    return true;

                if (*str == '\0')
                    return false;

                str++;
            }
        }

        if (*str == '\0')
        {
            return false;
        }

        if ((*pattern != '?') && (*pattern != *str))
        {
            return false;
        }

        pattern++;
        str++;
    }
}

bool JitConfigValues::MethodSet::contains(const char*       methodName,
                                          const char*       className,
                                          CORINFO_SIG_INFO* sigInfo) const
{
    // names[hasClassName][hasSignature]
    char*     names[2][2] = {};
    Compiler* comp        = JitTls::GetCompiler();

    // Try to match any the entries in the list.
    for (MethodName* name = m_names; name != nullptr; name = name->m_next)
    {
        char*& nameStr = names[name->m_containsClassName ? 1 : 0][name->m_containsSignature ? 1 : 0];
        if (nameStr == nullptr)
        {
            const int NAME_STR_SIZE = 1024;
            nameStr                 = static_cast<char*>(_alloca(NAME_STR_SIZE));
            comp->eeFormatMethodName(&nameStr, NAME_STR_SIZE, name->m_containsClassName ? className : nullptr,
                                     methodName, name->m_containsSignature ? sigInfo : nullptr,
                                     /* includeReturnType */ false,
                                     /* includeThis */ false);
        }

        if (matchGlob(name->m_patternStart, name->m_patternEnd, nameStr))
        {
            return true;
        }

#ifdef DEBUG
        if (name->m_containsClassName)
        {
            // Maybe className doesn't include the namespace. Try to match that.
            const char* methName = strchr(nameStr, ':');
            if (methName != nullptr)
            {
                const char* nsSep = methName;
                while ((nsSep > nameStr) && (*nsSep != '.'))
                    nsSep--;

                if ((nsSep != nameStr) && matchGlob(name->m_patternStart, name->m_patternEnd, nsSep + 1))
                {
                    return true;
                }
            }
        }
#endif
    }

    return false;
}

void JitConfigValues::initialize(ICorJitHost* host)
{
    assert(!m_isInitialized);

#define CONFIG_INTEGER(name, key, defaultValue) m_##name = host->getIntConfigValue(key, defaultValue);
#define CONFIG_STRING(name, key) m_##name = host->getStringConfigValue(key);
#define CONFIG_METHODSET(name, key)                                                                                    \
    const WCHAR* name##value = host->getStringConfigValue(key);                                                        \
    m_##name.initialize(name##value, host);                                                                            \
    host->freeStringConfigValue(name##value);

#include "jitconfigvalues.h"

    m_isInitialized = true;
}

void JitConfigValues::destroy(ICorJitHost* host)
{
    if (!m_isInitialized)
    {
        return;
    }

#define CONFIG_INTEGER(name, key, defaultValue)
#define CONFIG_STRING(name, key) host->freeStringConfigValue(m_##name);
#define CONFIG_METHODSET(name, key) m_##name.destroy(host);

#include "jitconfigvalues.h"

    m_isInitialized = false;
}
