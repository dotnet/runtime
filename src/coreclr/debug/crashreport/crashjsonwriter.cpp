// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Async-signal-safe JSON writer implementation.
// Every function here uses only stack variables and the pre-allocated buffer.
// No malloc, no stdio, no locks — safe to call from a signal handler.

#include "crashjsonwriter.h"

static
int
CrashJsonAppend(
    CrashJsonWriter* w,
    const char* str,
    int len);

static
int
CrashJsonAppendStr(
    CrashJsonWriter* w,
    const char* str);

static
char
ToHexChar(
    unsigned value);

static
void
CrashJsonWriteSeparator(
    CrashJsonWriter* w);

static
void
CrashJsonWriteEscapedString(
    CrashJsonWriter* w,
    const char* str);

void
CrashJsonInit(
    CrashJsonWriter* w)
{
    w->pos = 0;
    w->commaNeeded = false;
    w->buffer[0] = '\0';
}

void
CrashJsonOpenObject(
    CrashJsonWriter* w,
    const char* key)
{
    CrashJsonWriteSeparator(w);
    if (key != NULL)
    {
        CrashJsonWriteEscapedString(w, key);
        CrashJsonAppendStr(w, ": ");
    }
    CrashJsonAppendStr(w, "{");
    w->commaNeeded = false;
}

void
CrashJsonCloseObject(
    CrashJsonWriter* w)
{
    CrashJsonAppendStr(w, "}");
    w->commaNeeded = true;
}

void
CrashJsonOpenArray(
    CrashJsonWriter* w,
    const char* key)
{
    CrashJsonWriteSeparator(w);
    if (key != NULL)
    {
        CrashJsonWriteEscapedString(w, key);
        CrashJsonAppendStr(w, ": ");
    }
    CrashJsonAppendStr(w, "[");
    w->commaNeeded = false;
}

void
CrashJsonCloseArray(
    CrashJsonWriter* w)
{
    CrashJsonAppendStr(w, "]");
    w->commaNeeded = true;
}

void
CrashJsonWriteString(
    CrashJsonWriter* w,
    const char* key,
    const char* value)
{
    CrashJsonWriteSeparator(w);
    CrashJsonWriteEscapedString(w, key);
    CrashJsonAppendStr(w, ": ");
    CrashJsonWriteEscapedString(w, value);
}

int
CrashJsonGetLength(
    CrashJsonWriter* w)
{
    return w->pos;
}

const char*
CrashJsonGetBuffer(
    CrashJsonWriter* w)
{
    w->buffer[w->pos] = '\0';
    return w->buffer;
}

// Append raw bytes to buffer. Returns 0 if out of space.
int
CrashJsonAppend(
    CrashJsonWriter* w,
    const char* str,
    int len)
{
    if (w->pos + len >= CRASH_JSON_BUFFER_SIZE - 16)
        return 0;

    for (int i = 0; i < len; i++)
    {
        w->buffer[w->pos + i] = str[i];
    }

    w->pos += len;
    return 1;
}

int
CrashJsonAppendStr(
    CrashJsonWriter* w,
    const char* str)
{
    int len = 0;
    while (str[len])
        len++;

    return CrashJsonAppend(w, str, len);
}

char
ToHexChar(
    unsigned value)
{
    return (value < 10) ? (char)('0' + value) : (char)('a' + (value - 10));
}

void
CrashJsonWriteSeparator(
    CrashJsonWriter* w)
{
    if (w->commaNeeded)
        CrashJsonAppendStr(w, ",");

    w->commaNeeded = true;
}

// Escape a string value for JSON. Handles \, ", and control characters.
void
CrashJsonWriteEscapedString(
    CrashJsonWriter* w,
    const char* str)
{
    CrashJsonAppendStr(w, "\"");
    if (str != NULL)
    {
        for (int i = 0; str[i]; i++)
        {
            char c = str[i];
            if (c == '"')
                CrashJsonAppendStr(w, "\\\"");
            else if (c == '\\')
                CrashJsonAppendStr(w, "\\\\");
            else if (c == '\n')
                CrashJsonAppendStr(w, "\\n");
            else if (c == '\r')
                CrashJsonAppendStr(w, "\\r");
            else if (c == '\t')
                CrashJsonAppendStr(w, "\\t");
            else if ((unsigned char)c < 0x20)
            {
                char esc[7];
                esc[0] = '\\';
                esc[1] = 'u';
                esc[2] = '0';
                esc[3] = '0';
                esc[4] = ToHexChar(((unsigned char)c >> 4) & 0xF);
                esc[5] = ToHexChar((unsigned char)c & 0xF);
                esc[6] = '\0';
                CrashJsonAppendStr(w, esc);
            }
            else
            {
                CrashJsonAppend(w, &c, 1);
            }
        }
    }

    CrashJsonAppendStr(w, "\"");
}
