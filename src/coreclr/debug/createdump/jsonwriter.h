// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define JSON_INDENT_VALUE 1

class JsonWriter
{
private:
    int m_fd;
    int m_indent;
    bool m_comma;

    void Write(std::string& text);
    void Write(const char* buffer);
    void Indent(std::string& text);
    void WriteSeperator(std::string& text);
    void OpenBlock(const char* name, char marker);
    void CloseBlock(char marker);

public:
    JsonWriter();
    bool OpenWriter(const char* fileName);
    void CloseWriter();
    void WriteValue(const char* key, const char* value);
    void WriteValueBool(const char* key, bool value);
    void WriteValue32(const char* key, uint32_t value);
    void WriteValue64(const char* key, uint64_t value);
    void OpenSection(const char* sectionName);
    void CloseSection();
    void OpenArray(const char* arrayName);
    void CloseArray();
    void OpenArrayEntry();
    void CloseArrayEntry();
};
