// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

JsonWriter::JsonWriter()
{
    m_fd = -1;
    m_indent = JSON_INDENT_VALUE;
    m_comma = false;
}

JsonWriter::~JsonWriter()
{
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
}

void JsonWriter::Write(const std::string& text)
{
    if (!DumpWriter::WriteData(m_fd, (void*)text.c_str(), text.length()))
    {
        throw std::exception();
    }
}

void JsonWriter::Write(const char* buffer)
{
    std::string text(buffer);
    Write(text);
}

void JsonWriter::Indent(std::string& text)
{
    assert(m_indent >= 0);
    text.append(m_indent, ' ');
}

void JsonWriter::WriteSeperator(std::string& text)
{
    if (m_comma)
    {
        text.append(1, ',');
        text.append(1, '\n');
    }
    Indent(text);
}

void JsonWriter::OpenBlock(const char* name, char marker)
{
    std::string text;
    WriteSeperator(text);
    if (name != nullptr)
    {
        text.append("\"");
        text.append(name);
        text.append("\" : ");
    }
    text.append(1, marker);
    text.append(1, '\n');
    m_comma = false;
    m_indent += JSON_INDENT_VALUE;
    Write(text);
}

void JsonWriter::CloseBlock(char marker)
{
    std::string text;
    text.append(1, '\n');
    assert(m_indent >= JSON_INDENT_VALUE);
    m_indent -= JSON_INDENT_VALUE;
    Indent(text);
    text.append(1, marker);
    m_comma = true;
    Write(text);
}

bool JsonWriter::OpenWriter(const char* fileName)
{
    m_fd = open(fileName, O_WRONLY|O_CREAT|O_TRUNC, 0664);
    if (m_fd == -1)
    {
        fprintf(stderr, "Could not create json file %s: %d %s\n", fileName, errno, strerror(errno));
        return false;
    }
    Write("{\n");
    return true;
}

void JsonWriter::CloseWriter()
{
    assert(m_indent == JSON_INDENT_VALUE);
    Write("\n}\n");
}

void JsonWriter::WriteValue(const char* key, const char* value)
{
    std::string text;
    WriteSeperator(text);
    text.append("\"");
    text.append(key);
    text.append("\" : \"");
    text.append(value);
    text.append("\"");
    m_comma = true;
    Write(text);
}

void JsonWriter::WriteValueBool(const char* key, bool value)
{
    WriteValue(key, value ? "true" : "false");
}

void JsonWriter::WriteValue32(const char* key, uint32_t value)
{
    char buffer[16];
    snprintf(buffer, sizeof(buffer), "0x%x", value);
    WriteValue(key, buffer);
}

void JsonWriter::WriteValue64(const char* key, uint64_t value)
{
    char buffer[32];
    snprintf(buffer, sizeof(buffer), "0x%" PRIx64, value);
    WriteValue(key, buffer);
}

void JsonWriter::OpenSection(const char* sectionName)
{
    OpenBlock(sectionName, '{');
}

void JsonWriter::CloseSection()
{
    CloseBlock('}');
}

void JsonWriter::OpenArray(const char* arrayName)
{
    OpenBlock(arrayName, '[');
}

void JsonWriter::CloseArray()
{
    CloseBlock(']');
}

void JsonWriter::OpenArrayEntry()
{
    OpenBlock(nullptr, '{');
}

void JsonWriter::CloseArrayEntry()
{
    CloseBlock('}');
}
