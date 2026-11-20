// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MOUNTINFO_H
#define HAVE_MINIPAL_MOUNTINFO_H

#include <stdbool.h>
#include <stddef.h>
#include <string.h>

static inline void minipal_unescape_mountinfo_path(char* path)
{
    char* destination = strchr(path, '\\');
    if (destination == NULL)
        return;

    // Mount table paths escape space, tab, LF, and backslash as four-character octal sequences.
    // https://man7.org/linux/man-pages/man3/getmntent.3.html
    const size_t escapeLength = 4;
    const char* source = destination;
    while (*source != '\0')
    {
        char value = *source;
        if (value == '\\')
        {
            if (strncmp(source, "\\040", escapeLength) == 0)
                value = ' ';
            else if (strncmp(source, "\\011", escapeLength) == 0)
                value = '\t';
            else if (strncmp(source, "\\012", escapeLength) == 0)
                value = '\n';
            else if (strncmp(source, "\\134", escapeLength) == 0)
                value = '\\';
            else
            {
                *destination++ = *source++;
                continue;
            }

            source += escapeLength;
        }
        else
        {
            source++;
        }

        *destination++ = value;
    }

    *destination = '\0';
}

static inline bool minipal_get_mountinfo_paths(const char* line, char* root, char* mountPoint, size_t bufferSize)
{
    if (bufferSize == 0)
        return false;

    root[0] = '\0';
    mountPoint[0] = '\0';

    // Root and mount point are fields 4 and 5 in proc_pid_mountinfo(5).
    // https://man7.org/linux/man-pages/man5/proc_pid_mountinfo.5.html
    const char* rootStart = line;
    for (int field = 0; field < 3; field++)
    {
        rootStart = strchr(rootStart, ' ');
        if (rootStart == NULL)
            return false;
        rootStart++;
    }

    const char* rootEnd = strchr(rootStart, ' ');
    if (*rootStart != '/' || rootEnd == NULL)
        return false;

    const char* mountPointStart = rootEnd + 1;
    const char* mountPointEnd = strchr(mountPointStart, ' ');
    if (*mountPointStart != '/' || mountPointEnd == NULL)
        return false;

    size_t rootLength = (size_t)(rootEnd - rootStart);
    size_t mountPointLength = (size_t)(mountPointEnd - mountPointStart);
    if (rootLength >= bufferSize || mountPointLength >= bufferSize)
        return false;

    memcpy(root, rootStart, rootLength);
    root[rootLength] = '\0';
    memcpy(mountPoint, mountPointStart, mountPointLength);
    mountPoint[mountPointLength] = '\0';

    minipal_unescape_mountinfo_path(root);
    minipal_unescape_mountinfo_path(mountPoint);
    return true;
}

#endif // HAVE_MINIPAL_MOUNTINFO_H
