// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Streaming JSON writer implementation for crash reports.

#include "crashjsonwriter.h"

#include <string.h>

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
    CrashJsonWriter* w,
    CrashJsonOutputCallback outputCallback,
    void* outputContext)
{
    w->pos = 0;
    w->commaNeeded = false;
    w->writeFailed = false;
    w->outputCallback = outputCallback;
    w->outputContext = outputContext;
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

void
CrashJsonFinish(
    CrashJsonWriter* w)
{
    (void)CrashJsonFlush(w);
}

int
CrashJsonHasFailed(
    CrashJsonWriter* w)
{
    return w->writeFailed ? 1 : 0;
}

int
CrashJsonFlush(
    CrashJsonWriter* w)
{
    if (w->writeFailed)
    {
        return 0;
    }

    if (w->pos == 0)
    {
        return 1;
    }

    if (w->outputCallback != NULL && !w->outputCallback(w->buffer, w->pos, w->outputContext))
    {
        w->writeFailed = true;
        return 0;
    }

    w->pos = 0;
    w->buffer[0] = '\0';
    return 1;
}

int
CrashJsonAppend(
    CrashJsonWriter* w,
    const char* str,
    int len)
{
    if (w->writeFailed || str == NULL || len < 0)
    {
        return 0;
    }

    if (len == 0)
    {
        return 1;
    }

    int offset = 0;
    while (offset < len)
    {
        int remaining = (CRASH_JSON_BUFFER_SIZE - 1) - w->pos;
        if (remaining == 0 && !CrashJsonFlush(w))
        {
            return 0;
        }

        remaining = (CRASH_JSON_BUFFER_SIZE - 1) - w->pos;
        int chunk = len - offset;
        if (chunk > remaining)
        {
            chunk = remaining;
        }

        memcpy(w->buffer + w->pos, str + offset, static_cast<size_t>(chunk));
        w->pos += chunk;
        offset += chunk;
    }

    return 1;
}

int
CrashJsonAppendStr(
    CrashJsonWriter* w,
    const char* str)
{
    if (str == NULL)
    {
        return 0;
    }

    return CrashJsonAppend(w, str, static_cast<int>(strlen(str)));
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
