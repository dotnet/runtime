// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************

#ifndef _PROCESSCONTEXT_H
#define _PROCESSCONTEXT_H

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

#endif // _PROCESSCONTEXT_H