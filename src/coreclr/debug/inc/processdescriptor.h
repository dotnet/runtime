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

static const unsigned int MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH = MAX_PATH;

// Builds a debugger transport pipe name from the process id.
void GetTransportPipeName(char *name, DWORD id, const char *applicationGroupId, const char *suffix);

#endif // HOST_UNIX

#endif // _PROCESSDESCRIPTOR_H_
