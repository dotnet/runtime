// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "jitconfig.h"

JitConfigValues JitConfig;

void JitConfigValues::MethodSet::initialize(const wchar_t* list, ICorJitHost* host)
{
    _ASSERTE(m_list == nullptr);

    enum State { NO_NAME, CLS_NAME, FUNC_NAME, ARG_LIST }; // parsing state machine

    const char SEP_CHAR = ' ';     // current character use to separate each entry

    wchar_t lastChar = '?'; // dummy
    int nameStart = -1; // Index of the start of the current class or method name
    MethodName currentName; // Buffer used while parsing the current entry
    MethodName** lastName = &m_names; // Last entry inserted into the list
    bool isQuoted = false;

    currentName.m_next = nullptr;
    currentName.m_methodNameStart = -1;
    currentName.m_methodNameLen = -1;
    currentName.m_classNameStart = -1;
    currentName.m_classNameLen = -1;
    currentName.m_numArgs = -1;

    // Convert the input list to UTF-8
    int utf8ListLen = WszWideCharToMultiByte(CP_UTF8, 0, list, -1, NULL, 0, NULL, NULL);
    m_list = (char*)host->allocateMemory(utf8ListLen);
    if (WszWideCharToMultiByte(CP_UTF8, 0, list, -1, const_cast<LPSTR>(m_list), utf8ListLen, NULL, NULL) == 0)
    {
        // Failed to convert the list. Free the memory and ignore the list.
        host->freeMemory(reinterpret_cast<void*>(const_cast<char*>(m_list)));
        m_list = "";
        return;
    }

    State state = NO_NAME;
    for (int i = 0; lastChar != '\0'; i++)
    {
        lastChar = m_list[i];

        switch(state)
        {
        case NO_NAME:
            if (m_list[i] != SEP_CHAR)
            {
                nameStart = i;
                state = CLS_NAME; // we have found the start of the next entry
            }
            break;

        case CLS_NAME:
            if (m_list[nameStart] == '"')
            {
                for (; m_list[i] != '\0' && m_list[i] != '"'; i++)
                    ;

                nameStart++;
                isQuoted = true;
            }

            if (m_list[i] == ':')
            {
                if (m_list[nameStart] == '*' && !isQuoted)
                {
                    // The class name is a wildcard; mark it invalid.
                    currentName.m_classNameStart = -1;
                    currentName.m_classNameLen = -1;
                }
                else
                {
                    currentName.m_classNameStart = nameStart;
                    currentName.m_classNameLen = i - nameStart;

                    // Remove the trailing quote, if any
                    if (isQuoted)
                    {
                        currentName.m_classNameLen--;
                        isQuoted = false;
                    }
                }

                // Accept class::name syntax as well
                if (m_list[i + 1] == ':')
                {
                    i++;
                }

                nameStart = i + 1;
                state = FUNC_NAME;
            }
            else if (m_list[i] == '\0' || m_list[i] == SEP_CHAR || m_list[i] == '(')
            {
                // Treat this as a method name without a class name.
                currentName.m_classNameStart = -1;
                currentName.m_classNameLen = -1;
                goto DONE_FUNC_NAME;
            }
            break;

        case FUNC_NAME:
            if (m_list[nameStart] == '"')
            {
                // The first half of the outer contdition handles the case where the
                // class name is valid.
                for (; nameStart == i || (m_list[i] != '\0' && m_list[i] != '"'); i++)
                    ;
                       
                nameStart++;
                isQuoted = true;
            }

            if (m_list[i] == '\0' || m_list[i] == SEP_CHAR || m_list[i] == '(')
            {
            DONE_FUNC_NAME:
                _ASSERTE(m_list[i] == '\0' || m_list[i] == SEP_CHAR || m_list[i] == '(');

                if (m_list[nameStart] == '*' && !isQuoted)
                {
                    // The method name is a wildcard; mark it invalid.
                    currentName.m_methodNameStart = -1;
                    currentName.m_methodNameLen = -1;
                }
                else
                {
                    currentName.m_methodNameStart = nameStart;
                    currentName.m_methodNameLen = i - nameStart;

                    // Remove the trailing quote, if any
                    if (isQuoted)
                    {
                        currentName.m_classNameLen--;
                        isQuoted = false;
                    }
                }

                if (m_list[i] == '\0' || m_list[i] == SEP_CHAR)
                {
                    currentName.m_numArgs = -1;
                    goto DONE_ARG_LIST;
                }
                else
                {
                    _ASSERTE(m_list[i] == '(');
                    currentName.m_numArgs = -1;
                    state = ARG_LIST;
                }
            }
            break;

        case ARG_LIST:
            if (m_list[i] == '\0' || m_list[i] == ')')
            {
                if (currentName.m_numArgs == -1)
                {
                    currentName.m_numArgs = 0;
                }

            DONE_ARG_LIST:
                _ASSERTE(m_list[i] == '\0' || m_list[i] == SEP_CHAR || m_list[i] == ')');

                // We have parsed an entire method name; create a new entry in the list for it.
                MethodName* name = (MethodName*)host->allocateMemory(sizeof(MethodName));
                *name = currentName;

                _ASSERTE(name->m_next == nullptr);
                *lastName = name;
                lastName = &name->m_next;

                state = NO_NAME;

                // Skip anything after the argument list until we find the next
                // separator character. Otherwise if we see "func(a,b):foo" we
                // create entries for "func(a,b)" as well as ":foo".
                if (m_list[i] == ')')
                {
                    for (; m_list[i] && m_list[i] != SEP_CHAR; i++)
                        ;

                    lastChar = m_list[i];
                }
            }
            else
            {
                if (m_list[i] != SEP_CHAR && currentName.m_numArgs == -1)
                {
                    currentName.m_numArgs = 1;
                }

                if (m_list[i] == ',')
                {
                    currentName.m_numArgs++;
                }
            }
            break;

        default:
            _ASSERTE(!"Bad state");
            break;
        }
    }
}

void JitConfigValues::MethodSet::destroy(ICorJitHost* host)
{
    // Free method names, free the list string, and reset our state
    for (MethodName* name = m_names, *next = nullptr; name != nullptr; name = next)
    {
        next = name->m_next;
        host->freeMemory(reinterpret_cast<void*>(const_cast<MethodName*>(name)));
    }

    host->freeMemory(reinterpret_cast<void*>(const_cast<char*>(m_list)));

    m_names = nullptr;
    m_list = nullptr;
}

static bool matchesName(const char* const name, int nameLen, const char* const s2)
{
    return strncmp(name, s2, nameLen) == 0 && s2[nameLen] == '\0';
}

bool JitConfigValues::MethodSet::contains(const char* methodName, const char* className, CORINFO_SIG_INFO* sigInfo) const
{
    int numArgs = sigInfo != nullptr ? sigInfo->numArgs : -1;

    // Try to match any the entries in the list.
    for (MethodName* name = m_names; name != nullptr; name = name->m_next)
    {
        // If m_numArgs is valid, check for a mismatch
        if (name->m_numArgs != -1 && name->m_numArgs != numArgs)
        {
            continue;
        }

        // If m_methodNameStart is valid, check for a mismatch
        if (name->m_methodNameStart != -1)
        {
            const char* expectedMethodName = &m_list[name->m_methodNameStart];
            if (!matchesName(expectedMethodName, name->m_methodNameLen, methodName))
            {
                // C++ embeds the class name into the method name; deal with that here.
                const char* colon = strchr(methodName, ':');
                if (colon != nullptr && colon[1] == ':' && matchesName(expectedMethodName, name->m_methodNameLen, methodName))
                {
                    int classLen = (int)(colon - methodName);
                    if (name->m_classNameStart == -1 ||
                        (classLen == name->m_classNameLen &&
                            strncmp(&m_list[name->m_classNameStart], methodName, classLen) == 0))
                    {
                        return true;
                    }
                }
                continue;
            }
        }

        // If m_classNameStart is valid, check for a mismatch
        if (className == nullptr || name->m_classNameStart == -1 || matchesName(&m_list[name->m_classNameStart], name->m_classNameLen, className))
        {
            return true;
        }

        // Check for suffix wildcard like System.*
        if (name->m_classNameLen > 0 &&
            m_list[name->m_classNameStart + name->m_classNameLen - 1] == '*' &&
            strncmp(&m_list[name->m_classNameStart], className, name->m_classNameLen - 1) == 0)
        {
            return true;
        }

#ifdef _DEBUG
        // Maybe className doesn't include the namespace. Try to match that
        const char* nsSep = strrchr(className, '.');
        if (nsSep != nullptr && nsSep != className)
        {
            const char* onlyClass = nsSep[-1] == '.' ? nsSep : &nsSep[1];
            if (matchesName(&m_list[name->m_classNameStart], name->m_classNameLen, onlyClass))
            {
                return true;
            }
        }
#endif
    }

    return false;
}

void JitConfigValues::initialize(ICorJitHost* host)
{
    _ASSERTE(!m_isInitialized);

#define CONFIG_INTEGER(name, key, defaultValue) m_##name = host->getIntConfigValue(key, defaultValue);
#define CONFIG_STRING(name, key) m_##name = host->getStringConfigValue(key);
#define CONFIG_METHODSET(name, key) \
    const wchar_t* name##value = host->getStringConfigValue(key); \
    m_##name.initialize(name##value, host); \
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
