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

    const char   SEP_CHAR  = ' ';      // character used to separate each entry
    const char   WILD_CHAR = '*';      // character used as the wildcard match everything
    char         currChar  = '?';      // The current character
    int          nameStart = -1;       // Index of the start of the current class or method name
    MethodName** lastName  = &m_names; // Last entry inserted into the list
    bool         isQuoted  = false;    // true while parsing inside a quote "this-is-a-quoted-region"
    MethodName   currentName;          // Buffer used while parsing the current entry

    currentName.m_next                    = nullptr;
    currentName.m_methodNameStart         = -1;
    currentName.m_methodNameLen           = -1;
    currentName.m_methodNameWildcardAtEnd = false;
    currentName.m_classNameStart          = -1;
    currentName.m_classNameLen            = -1;
    currentName.m_classNameWildcardAtEnd  = false;
    currentName.m_numArgs                 = -1;

    enum State
    {
        NO_NAME,
        CLS_NAME,
        FUNC_NAME,
        ARG_LIST
    }; // parsing state machine

    State state = NO_NAME;
    for (int i = 0; (currChar != '\0'); i++)
    {
        currChar = m_list[i];

        switch (state)
        {
            case NO_NAME:
                // skip over zero or more blanks, then expect CLS_NAME
                if (currChar != SEP_CHAR)
                {
                    nameStart = i;
                    state     = CLS_NAME; // we have found the start of the next entry
                }
                break;

            case CLS_NAME:
                // Check for a quoted Class Name: (i.e. "MyClass")
                if (m_list[nameStart] == '"')
                {
                    // Advance until we see the second "
                    //
                    for (; (currChar != '\0'); i++)
                    {
                        currChar = m_list[i];
                        // Advance until we see the second "
                        if (currChar == '"')
                        {
                            break;
                        }
                        // or until we see the end of string
                        if (currChar == '\0')
                        {
                            break;
                        }
                    }

                    // skip the initial "
                    nameStart++;
                    isQuoted = true;
                }

                // A colon denotes the end of the Class name and the start of the Method name
                if (currChar == ':')
                {
                    // Record the class name
                    currentName.m_classNameStart = nameStart;
                    currentName.m_classNameLen   = i - nameStart;

                    // Also accept the double colon syntax as well  (i.e class::method)
                    //
                    if (m_list[i + 1] == ':')
                    {
                        i++;
                    }

                    if (isQuoted)
                    {
                        // Remove the trailing "
                        currentName.m_classNameLen--;
                        isQuoted = false;
                    }

                    // Is the first character a wildcard?
                    if (m_list[currentName.m_classNameStart] == WILD_CHAR)
                    {
                        // The class name is a full wildcard; mark it as such.
                        currentName.m_classNameStart = -1;
                        currentName.m_classNameLen   = -1;
                    }
                    // Is there a wildcard at the end of the class name?
                    //
                    else if (m_list[currentName.m_classNameStart + currentName.m_classNameLen - 1] == WILD_CHAR)
                    {
                        // i.e. bar*:method, will match any class that starts with "bar"

                        // Remove the trailing WILD_CHAR from class name
                        currentName.m_classNameWildcardAtEnd = true;
                        currentName.m_classNameLen--; // backup for WILD_CHAR
                    }

                    // The method name will start at the next character
                    nameStart = i + 1;

                    // Now expect FUNC_NAME
                    state = FUNC_NAME;
                }
                else if ((currChar == '\0') || (currChar == SEP_CHAR) || (currChar == '('))
                {
                    // Treat this as a method name without a class name.
                    currentName.m_classNameStart = -1;
                    currentName.m_classNameLen   = -1;
                    goto DONE_FUNC_NAME;
                }
                break;

            case FUNC_NAME:
                // Check for a quoted method name: i.e. className:"MyFunc"
                //
                // Note that we may have already parsed a quoted string above in CLS_NAME, i.e. "Func":
                if (!isQuoted && (m_list[nameStart] == '"'))
                {
                    // Advance until we see the second "
                    //
                    for (; (currChar != '\0'); i++)
                    {
                        currChar = m_list[i];
                        // Advance until we see the second "
                        if (currChar == '"')
                        {
                            break;
                        }
                        // or until we see the end of string
                        if (currChar == '\0')
                        {
                            break;
                        }
                    }

                    // skip the initial "
                    nameStart++;
                    isQuoted = true;
                }

                if ((currChar == '\0') || (currChar == SEP_CHAR) || (currChar == '('))
                {
                DONE_FUNC_NAME:
                    assert((currChar == '\0') || (currChar == SEP_CHAR) || (currChar == '('));

                    // Record the method name
                    currentName.m_methodNameStart = nameStart;
                    currentName.m_methodNameLen   = i - nameStart;

                    if (isQuoted)
                    {
                        // Remove the trailing "
                        currentName.m_methodNameLen--;
                        isQuoted = false;
                    }

                    // Is the first character a wildcard?
                    if (m_list[currentName.m_methodNameStart] == WILD_CHAR)
                    {
                        // The method name is a full wildcard; mark it as such.
                        currentName.m_methodNameStart = -1;
                        currentName.m_methodNameLen   = -1;
                    }
                    // Is there a wildcard at the end of the method name?
                    //
                    else if (m_list[currentName.m_methodNameStart + currentName.m_methodNameLen - 1] == WILD_CHAR)
                    {
                        // i.e. class:foo*, will match any method that starts with "foo"

                        // Remove the trailing WILD_CHAR from method name
                        currentName.m_methodNameLen--; // backup for WILD_CHAR
                        currentName.m_methodNameWildcardAtEnd = true;
                    }

                    // should we expect an ARG_LIST?
                    //
                    if (currChar == '(')
                    {
                        currentName.m_numArgs = -1;
                        // Expect an ARG_LIST
                        state = ARG_LIST;
                    }
                    else // reached the end of string or a SEP_CHAR
                    {
                        assert((currChar == '\0') || (currChar == SEP_CHAR));

                        currentName.m_numArgs = -1;

                        // There isn't an ARG_LIST
                        goto DONE_ARG_LIST;
                    }
                }
                break;

            case ARG_LIST:
                if ((currChar == '\0') || (currChar == ')'))
                {
                    if (currentName.m_numArgs == -1)
                    {
                        currentName.m_numArgs = 0;
                    }

                DONE_ARG_LIST:
                    assert((currChar == '\0') || (currChar == SEP_CHAR) || (currChar == ')'));

                    // We have parsed an entire method name; create a new entry in the list for it.
                    MethodName* name = static_cast<MethodName*>(host->allocateMemory(sizeof(MethodName)));
                    *name            = currentName;

                    assert(name->m_next == nullptr);
                    *lastName = name;
                    lastName  = &name->m_next;

                    state = NO_NAME;

                    // Skip anything after the argument list until we find the next
                    // separator character. Otherwise if we see "func(a,b):foo" we
                    // would create entries for "func(a,b)" as well as ":foo".
                    if (currChar == ')')
                    {
                        do
                        {
                            currChar = m_list[++i];
                        } while ((currChar != '\0') && (currChar != SEP_CHAR));
                    }
                }
                else // We are looking at the ARG_LIST
                {
                    if ((currChar != SEP_CHAR) && (currentName.m_numArgs == -1))
                    {
                        currentName.m_numArgs = 1;
                    }

                    // A comma means that there is an additional arg
                    if (currChar == ',')
                    {
                        currentName.m_numArgs++;
                    }
                }
                break;

            default:
                assert(!"Bad state");
                break;
        }
    }
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

static bool matchesName(const char* const name, int nameLen, bool wildcardAtEnd, const char* const s2)
{
    // 's2' must start with 'nameLen' characters of 'name'
    if (strncmp(name, s2, nameLen) != 0)
    {
        return false;
    }

    // if we don't have a wildcardAtEnd then s2 also need to be zero terminated
    if (!wildcardAtEnd && (s2[nameLen] != '\0'))
    {
        return false;
    }

    // we have a successful match
    return true;
}

bool JitConfigValues::MethodSet::contains(const char*       methodName,
                                          const char*       className,
                                          CORINFO_SIG_INFO* sigInfo) const
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
            if (!matchesName(expectedMethodName, name->m_methodNameLen, name->m_methodNameWildcardAtEnd, methodName))
            {
                // C++ embeds the class name into the method name; deal with that here.
                const char* colon = strchr(methodName, ':');
                if (colon != nullptr && colon[1] == ':' &&
                    matchesName(expectedMethodName, name->m_methodNameLen, name->m_methodNameWildcardAtEnd, methodName))
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
        if (className == nullptr || name->m_classNameStart == -1 ||
            matchesName(&m_list[name->m_classNameStart], name->m_classNameLen, name->m_classNameWildcardAtEnd,
                        className))
        {
            return true;
        }

#ifdef _DEBUG
        // Maybe className doesn't include the namespace. Try to match that
        const char* nsSep = strrchr(className, '.');
        if (nsSep != nullptr && nsSep != className)
        {
            const char* onlyClass = nsSep[-1] == '.' ? nsSep : &nsSep[1];
            if (matchesName(&m_list[name->m_classNameStart], name->m_classNameLen, name->m_classNameWildcardAtEnd,
                            onlyClass))
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
