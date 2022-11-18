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

        MethodName* name              = static_cast<MethodName*>(host->allocateMemory(sizeof(MethodName)));
        name->m_next                  = m_names;
        name->m_patternStart          = start;
        name->m_patternEnd            = end;
        const char* colon             = static_cast<const char*>(memchr(start, ':', end - start));
        const char* startOfMethodName = colon != nullptr ? colon + 1 : start;

        const char* parens          = static_cast<const char*>(memchr(startOfMethodName, '(', end - startOfMethodName));
        const char* endOfMethodName = parens != nullptr ? parens : end;
        name->m_methodNameContainsInstantiation =
            memchr(startOfMethodName, '[', endOfMethodName - startOfMethodName) != nullptr;

        if (colon != nullptr)
        {
            name->m_containsClassName              = true;
            name->m_classNameContainsInstantiation = memchr(start, '[', colon - start) != nullptr;
        }
        else
        {
            name->m_containsClassName              = false;
            name->m_classNameContainsInstantiation = false;
        }

        name->m_containsSignature = parens != nullptr;
        m_names                   = name;
    };

    const char* curPatternStart = m_list;
    const char* curChar;
    for (curChar = curPatternStart; *curChar != '\0'; curChar++)
    {
        if (*curChar == ' ')
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

// Quadratic string matching algorithm that supports * and ? wildcards
static bool matchGlob(const char* pattern, const char* patternEnd, const char* str)
{
    // Invariant: [patternStart..backtrackPattern) matches [stringStart..backtrackStr)
    const char* backtrackPattern = nullptr;
    const char* backtrackStr     = nullptr;

    while (true)
    {
        if (pattern == patternEnd)
        {
            if (*str == '\0')
                return true;
        }
        else if (*pattern == '*')
        {
            backtrackPattern = ++pattern;
            backtrackStr     = str;
            continue;
        }
        else if (*str == '\0')
        {
            // No match since pattern needs at least one char in remaining cases.
        }
        else if ((*pattern == '?') || (*pattern == *str))
        {
            pattern++;
            str++;
            continue;
        }

        // In this case there was no match, see if we can backtrack to a wild
        // card and consume one more character from the string.
        if ((backtrackPattern == nullptr) || (*backtrackStr == '\0'))
            return false;

        // Consume one more character for the wildcard.
        pattern = backtrackPattern;
        str     = ++backtrackStr;
    }
}

bool JitConfigValues::MethodSet::contains(CORINFO_METHOD_HANDLE methodHnd,
                                          CORINFO_CLASS_HANDLE  classHnd,
                                          CORINFO_SIG_INFO*     sigInfo) const
{
    if (isEmpty())
    {
        return false;
    }

    Compiler*     comp = JitTls::GetCompiler();
    char          buffer[1024];
    StringPrinter printer(comp->getAllocator(CMK_DebugOnly), buffer, ArrLen(buffer));
    MethodName*   prevPattern = nullptr;

    for (MethodName* name = m_names; name != nullptr; name = name->m_next)
    {
        if ((prevPattern == nullptr) || (name->m_containsClassName != prevPattern->m_containsClassName) ||
            (name->m_classNameContainsInstantiation != prevPattern->m_classNameContainsInstantiation) ||
            (name->m_methodNameContainsInstantiation != prevPattern->m_methodNameContainsInstantiation) ||
            (name->m_containsSignature != prevPattern->m_containsSignature))
        {
            printer.Truncate(0);
            bool success = comp->eeRunFunctorWithSPMIErrorTrap([&]() {
                comp->eePrintMethod(&printer, name->m_containsClassName ? classHnd : NO_CLASS_HANDLE, methodHnd,
                                    sigInfo,
                                    /* includeNamespaces */ true,
                                    /* includeClassInstantiation */ name->m_classNameContainsInstantiation,
                                    /* includeMethodInstantiation */ name->m_methodNameContainsInstantiation,
                                    /* includeSignature */ name->m_containsSignature,
                                    /* includeReturnType */ false,
                                    /* includeThis */ false);
            });

            if (!success)
                continue;

            prevPattern = name;
        }

        if (matchGlob(name->m_patternStart, name->m_patternEnd, printer.GetBuffer()))
        {
            return true;
        }
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
