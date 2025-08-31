// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"

//----------------------------------------------------------------------
// initialize: Initialize the method set by parsing the string
//
// Arguments:
//     listFromConfig - A string containing the list. The string must have come from the host's config,
//                      and this class takes ownership of the string.
//     host           - Pointer to host interface
//
void MethodSet::initialize(const char* listFromConfig)
{
    assert(m_listFromConfig == nullptr);
    assert(m_names == nullptr);

    if (listFromConfig == nullptr)
    {
        return;
    }

    size_t configSize = strlen(listFromConfig);
    if (configSize == 0)
    {
        return;
    }

    m_listFromConfig = static_cast<const char*>(malloc(configSize + 1));
    strcpy(const_cast<char*>(m_listFromConfig), listFromConfig);

    auto commitPattern = [this](const char* start, const char* end) {
        if (end <= start)
        {
            return;
        }

        MethodName* name              = static_cast<MethodName*>(calloc(1, sizeof(MethodName)));
        name->m_next                  = m_names;
        name->m_patternStart          = start;
        name->m_patternEnd            = end;
        const char* exclamation       = static_cast<const char*>(memchr(start, '!', end - start));
        const char* startOfClassName  = exclamation != nullptr ? exclamation + 1 : start;
        const char* colon             = static_cast<const char*>(memchr(startOfClassName, ':', end - startOfClassName));
        const char* startOfMethodName = colon != nullptr ? colon + 1 : startOfClassName;

        const char* parens          = static_cast<const char*>(memchr(startOfMethodName, '(', end - startOfMethodName));
        const char* endOfMethodName = parens != nullptr ? parens : end;
        name->m_methodNameContainsInstantiation =
            memchr(startOfMethodName, '[', endOfMethodName - startOfMethodName) != nullptr;

        name->m_containsAssemblyName = (exclamation != nullptr);

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

    const char* curPatternStart = m_listFromConfig;
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

//----------------------------------------------------------------------
// destroy: Destroy the method set.
//
// Arguments:
//     host - Pointer to host interface
//
void MethodSet::destroy()
{
    // Free method names, free the list string, and reset our state
    for (MethodName *name = m_names, *next = nullptr; name != nullptr; name = next)
    {
        next = name->m_next;
        free(static_cast<void*>(name));
    }
    if (m_listFromConfig != nullptr)
    {
        free((void*)m_listFromConfig);
        m_listFromConfig = nullptr;
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

bool MethodSet::contains(COMP_HANDLE comp,
                         CORINFO_METHOD_HANDLE methodHnd,
                         CORINFO_CLASS_HANDLE  classHnd,
                         CORINFO_SIG_INFO*     sigInfo) const
{
    if (isEmpty())
    {
        return false;
    }

    TArray<char> printer;
    MethodName*   prevPattern = nullptr;

    for (MethodName* name = m_names; name != nullptr; name = name->m_next)
    {
        if ((prevPattern == nullptr) ||
            (name->m_containsAssemblyName != prevPattern->m_containsAssemblyName) ||
            (name->m_containsClassName != prevPattern->m_containsClassName) ||
            (name->m_classNameContainsInstantiation != prevPattern->m_classNameContainsInstantiation) ||
            (name->m_methodNameContainsInstantiation != prevPattern->m_methodNameContainsInstantiation) ||
            (name->m_containsSignature != prevPattern->m_containsSignature))
        {
            printer = PrintMethodName(comp, classHnd, methodHnd, sigInfo,
                                    /* includeAssembly */ name->m_containsAssemblyName,
                                    /* includeClass */ name->m_containsClassName,
                                    /* includeClassInstantiation */ name->m_classNameContainsInstantiation,
                                    /* includeMethodInstantiation */ name->m_methodNameContainsInstantiation,
                                    /* includeSignature */ name->m_containsSignature,
                                    /* includeReturnType */ false,
                                    /* includeThis */ false);

            prevPattern = name;
        }

        if (matchGlob(name->m_patternStart, name->m_patternEnd, printer.GetUnderlyingArray()))
        {
            return true;
        }
    }

    return false;
}
