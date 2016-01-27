// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _CIRCULARLOG_H__

#define _CIRCULARLOG_H__

#include "sstring.h"

class CircularLog
{
public:
    CircularLog();
    ~CircularLog();
    
    bool Init(const WCHAR* logname, const WCHAR* logHeader, DWORD maxSize = 1024*1024);
    void Shutdown();
    void Log(const WCHAR* string);
  
protected:

    void   CheckForLogReset(BOOL fOverflow);
    BOOL   CheckLogHeader();
    HANDLE OpenFile();    
    void   CloseFile();

    bool            m_bInit;
    SString         m_LogFilename;
    SString         m_LogHeader;
    SString         m_OldLogFilename;
    SString         m_LockFilename;
    DWORD           m_MaxSize;
    unsigned        m_uLogCount;
};

#endif
