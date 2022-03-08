// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <time.h>
#ifdef HOST_WINDOWS
#include <winsock.h>
#endif

//
// Format the core dump name using a subset of the standard coredump pattern
// defined here: https://man7.org/linux/man-pages/man5/core.5.html.
//
// Supported:
//
//  %%  A single % character.
//  %d  PID of dumped process (for backwards createdump compatibility).
//  %p  PID of dumped process.
//  %e  The process executable filename.
//  %h  Hostname return by gethostname().
//  %t  Time of dump, expressed as seconds since the Epoch, 1970-01-01 00:00:00 +0000 (UTC).
//
// Unsupported:
//
//  %c  Core file size soft resource limit of crashing process.
//  %E  Pathname of executable, with slashes ('/') replaced by exclamation marks ('!').
//  %g  Numeric real GID of dumped process.
//  %i  TID of thread that triggered core dump, as seen in the PID namespace in which the thread resides.
//  %I  TID of thread that triggered core dump, as seen in the initial PID namespace.
//  %P  PID of dumped process, as seen in the initial PID namespace.
//  %s  Number of signal causing dump.
//  %u  Numeric real UID of dumped process.
//
bool
FormatDumpName(std::string& name, const char* pattern, const char* exename, int pid)
{
    const char* p = pattern;
    if (*p == '|')
    {
        printf_error("Pipe syntax in dump name not supported\n");
        return false;
    }

#ifdef HOST_WINDOWS
    WSAData wsadata;
    int wsaerr = WSAStartup(1, &wsadata);
#endif

    while (*p)
    {
        if (*p != '%')
        {
            name.append(1, *p);
        }
        else
        {
            switch (*++p)
            {
                case '\0':
                    return true;

                case '%':
                    name.append(1, '%');
                    break;

                // process Id
                case 'd':
                case 'p':
                    name.append(std::to_string(pid));
                    break;

                // time of dump
                case 't':
                    time_t dumptime;
                    time(&dumptime);
                    name.append(std::to_string(dumptime));
                    break;

                // hostname
                case 'h': {
                    ArrayHolder<char> buffer = new char[MAX_LONGPATH + 1];
                    if (gethostname(buffer, MAX_LONGPATH) != 0)
                    {
                        printf_error("Could not get the host name for dump name: %d\n",
#ifdef HOST_WINDOWS
                            WSAGetLastError());
#else
                            errno);
#endif
                        return false;
                    }
                    name.append(buffer);
                    break;
                }

                // executable file name
                case 'e':
                    name.append(exename);
                    break;

                // executable file path with / replaced with !
                case 'E':
                // signal number that caused the dump
                case 's':
                // gid
                case 'g':
                // coredump size limit
                case 'c':
                // the numeric real UID of dumped process
                case 'u':
                // thread id that triggered the dump
                case 'i': 
                case 'I':
                // pid of dumped process
                case 'P':
                default:
                    printf_error("Invalid dump name format char '%c'\n", *p);
                    return false;
            }
        }
        ++p;
    }
    return true;
}
