// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define JSON_INDENT_VALUE 1

class CrashReportWriter
{
private:
    int m_fd;
    int m_indent;
    bool m_comma;
    CrashInfo& m_crashInfo;

public:
    CrashReportWriter(CrashInfo& crashInfo);
    virtual ~CrashReportWriter();
    void WriteCrashReport(const std::string& dumpFileName);

private:
    void WriteCrashReport();
#ifdef __APPLE__
    void WriteStackFrame(const StackFrame& frame);
    void WriteSysctl(const char* sysctlname, const char* valueName);
#endif
    void Write(const std::string& text);
    void Write(const char* buffer);
    void Indent(std::string& text);
    void WriteSeperator(std::string& text);
    void OpenValue(const char* key, char marker);
    void CloseValue(char marker);
    bool OpenWriter(const char* fileName);
    void CloseWriter();
    void WriteValue(const char* key, const char* value);
    void WriteValueBool(const char* key, bool value);
    void WriteValue32(const char* key, uint32_t value);
    void WriteValue64(const char* key, uint64_t value);
    void OpenObject(const char* key = nullptr);
    void CloseObject();
    void OpenArray(const char* key);
    void CloseArray();
};
