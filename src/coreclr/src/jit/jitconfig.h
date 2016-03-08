// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _JITCONFIG_H_
#define _JITCONFIG_H_

struct CORINFO_SIG_INFO;
class ICorJitHost;

class JitConfigValues
{
public:
    class MethodSet
    {
    private:
        struct MethodName
        {
            MethodName* m_next;
            int m_methodNameStart;
            int m_methodNameLen;
            int m_classNameStart;
            int m_classNameLen;
            int m_numArgs;
        };

        const char* m_list;
        MethodName* m_names;

        MethodSet(const MethodSet& other) = delete;
        MethodSet& operator=(const MethodSet& other) = delete;

    public:
        MethodSet() { }
        inline const char* list() const { return m_list; }

        void initialize(const wchar_t* list, ICorJitHost* host);
        void destroy(ICorJitHost* host);

        inline bool isEmpty() const { return m_names == nullptr; }
        bool contains(const char* methodName, const char* className, CORINFO_SIG_INFO* sigInfo) const;
    };

private:
#define CONFIG_INTEGER(name, key, defaultValue) int m_##name;
#define CONFIG_STRING(name, key) const wchar_t* m_##name;
#define CONFIG_METHODSET(name, key) MethodSet m_##name;
#include "jitconfigvalues.h"

public:
#define CONFIG_INTEGER(name, key, defaultValue) inline int name() const { return m_##name; }
#define CONFIG_STRING(name, key) inline const wchar_t* name() const { return m_##name; }
#define CONFIG_METHODSET(name, key) inline const MethodSet& name() const { return m_##name; }
#include "jitconfigvalues.h"

private:
    bool m_isInitialized;

    JitConfigValues(const JitConfigValues& other) = delete;
    JitConfigValues& operator=(const JitConfigValues& other) = delete;

public:
    JitConfigValues() {}

    inline bool isInitialized() const { return m_isInitialized != 0; }
    void initialize(ICorJitHost* host);
    void destroy(ICorJitHost* host);
};

extern JitConfigValues JitConfig;

#endif
