// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

#ifndef _PROCESSDESCRIPTOR_H_
#define _PROCESSDESCRIPTOR_H_

struct ProcessDescriptor
{
    const static DWORD UNINITIALIZED_PID = 0;

    static ProcessDescriptor Create(DWORD pid, LPCSTR applicationGroupId)
    {
        ProcessDescriptor pd;
        pd.m_Pid = pid;
        pd.m_ApplicationGroupId = applicationGroupId;

        return pd;
    }

    static ProcessDescriptor FromCurrentProcess();
    static ProcessDescriptor FromPid(DWORD pid)
    {
        return Create(pid, nullptr);
    }
    static ProcessDescriptor CreateUninitialized()
    {
        return Create(UNINITIALIZED_PID, nullptr);
    }

    bool IsInitialized() const { return m_Pid != UNINITIALIZED_PID; }

    DWORD m_Pid;
    LPCSTR m_ApplicationGroupId;
};

#ifdef HOST_UNIX

#ifdef __APPLE__
// Max semaphore length on Mac is 31 characters. In a sandbox, IPC names are prefixed with an
// application group ID, e.g. ABCDEFGHIJ.MS (10 characters of developer ID and 2 of group name).
#define MAX_APPLICATION_GROUP_ID_LENGTH 13
#endif // __APPLE__

static const unsigned int MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH = MAX_PATH;

// Gets a value that distinguishes processes that had the same pid at different times (based on
// the process start time). Sets the key to 0 on failure, as the debugger side does.
bool GetProcessIdDisambiguationKey(DWORD processId, UINT64 *disambiguationKey);

// Builds an IPC transport name from the process id. These names are a contract with dbgshim and
// the diagnostics tools. On failure, name is set to an empty string.
void GetTransportName(unsigned int maxTransportNameLength, char *name, const char *prefix, DWORD id, const char *applicationGroupId, const char *suffix);

// Builds a debugger transport pipe name from the process id.
void GetTransportPipeName(char *name, DWORD id, const char *applicationGroupId, const char *suffix);

#endif // HOST_UNIX

#endif // _PROCESSDESCRIPTOR_H_
