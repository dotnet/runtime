// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Circular file log 
//
#include "stdafx.h"

#include "utilcode.h"
#include "circularlog.h"

CircularLog::CircularLog()
{
    m_bInit = false;
}

CircularLog::~CircularLog()
{
    Shutdown();
}

bool    CircularLog::Init(const WCHAR* logname, const WCHAR* logHeader, DWORD maxSize)
{
    Shutdown();

    m_LogFilename = logname;        
    m_LogFilename.Append(W(".log"));

    m_LockFilename = logname;
    m_LockFilename.Append(W(".lock"));
    
    m_OldLogFilename = logname;
    m_OldLogFilename .Append(W(".old.log"));

    if (logHeader)
        m_LogHeader = logHeader;
            
    m_MaxSize = maxSize;
    m_uLogCount = 0;

    m_bInit = true;

    if (CheckLogHeader())
    {
        CheckForLogReset(FALSE);
    }
    return true;
}

void CircularLog::Shutdown()
{
    m_bInit = false;
}

void CircularLog::Log(const WCHAR* string)
{
    if (!m_bInit) 
    {
        return;        
    }
    
    HANDLE hLogFile = OpenFile();
    if (hLogFile == INVALID_HANDLE_VALUE)
    {
        return;
    }

    // Check for file limit only once in a while
    if ((m_uLogCount % 16) == 0)
    {
        // First do a quick check without acquiring lock, optimizing for the common case where file is not overflow.
        LARGE_INTEGER fileSize;
        if (GetFileSizeEx(hLogFile, &fileSize) && fileSize.QuadPart > m_MaxSize)
        {
            // Must close existing handle before calling CheckForOverflow, and re-open it afterwards.
            CloseHandle(hLogFile);
            CheckForLogReset(TRUE);
            hLogFile = OpenFile();
            if (hLogFile == INVALID_HANDLE_VALUE)
            {
                return;
            }
        }
    }
    m_uLogCount++;

    // Replace \n with \r\n (we're writing to a binary file)
    NewArrayHolder<WCHAR> pwszConvertedHolder = new WCHAR[wcslen(string)*2 + 1];
    WCHAR* pD = pwszConvertedHolder;
    WCHAR previous = W('\0');
    for (const WCHAR* pS = string ; *pS != W('\0') ; pS++)
    {
        // We get mixed strings ('\n' and '\r\n'). So attempt to filter
        // the ones that don't need to have a '\r' added.
        if (*pS == W('\n') && previous != W('\r'))
        {
            *pD = W('\r');
            pD ++;
        }

        *pD = *pS;
        pD++;
        
        previous = *pS;
    }   

    *pD = W('\0');

    // Convert to Utf8 to reduce typical log file size
    SString logString(pwszConvertedHolder);
    StackScratchBuffer bufUtf8;
    COUNT_T cBytesUtf8 = 0;
    const UTF8 * pszUtf8Log = logString.GetUTF8(bufUtf8, &cBytesUtf8);
    // Remove null terminator from log entry buffer
    cBytesUtf8--;

    DWORD dwWritten;
    WriteFile(hLogFile, pszUtf8Log, (DWORD)cBytesUtf8, &dwWritten, NULL);
    CloseHandle(hLogFile);
}
    

BOOL CircularLog::CheckLogHeader()
{
    BOOL fNeedsPushToBackupLog = FALSE;

    // Check to make sure the header on the log is utf8, if it is not, push the current file to the .bak file and try again.
    HANDLE hLogFile = WszCreateFile(
        m_LogFilename.GetUnicode(),
        FILE_READ_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (hLogFile != INVALID_HANDLE_VALUE)
    {
        CHAR unicodeHeader []= {(char)0xef, (char)0xbb, (char)0xbf};
        CHAR unicodeHeaderCheckBuf[_countof(unicodeHeader)];

        DWORD dwRead = sizeof(unicodeHeaderCheckBuf);
        fNeedsPushToBackupLog = !ReadFile(hLogFile, &unicodeHeaderCheckBuf, dwRead, &dwRead, NULL);

        if (!fNeedsPushToBackupLog)
        {
            // Successfully read from file. Now check to ensure we read the right amount, and that we read the right data
            if ((dwRead != sizeof(unicodeHeader)) || (0 != memcmp(unicodeHeader, unicodeHeaderCheckBuf, dwRead)))
            {
                fNeedsPushToBackupLog = TRUE;
            }
        }
        CloseHandle(hLogFile);
    }

    return fNeedsPushToBackupLog;
}

#define MOVE_FILE_RETRY_TIME 100
#define MOVE_FILE_RETRY_COUNT 10
void CircularLog::CheckForLogReset(BOOL fOverflow)
{
    if (!m_MaxSize)
    {
        return;
    }
    
    for (int i = 0; i < MOVE_FILE_RETRY_COUNT; i++)
    {
        FileLockHolder lock;
        if (FAILED(lock.AcquireNoThrow(m_LockFilename.GetUnicode())))
        {
            // FileLockHolder::Acquire already has a retry loop, so don't retry if it fails.
            return;
        }

        BOOL fLogNeedsReset = FALSE;

        if (fOverflow)
        {
            WIN32_FILE_ATTRIBUTE_DATA fileData;
            if (WszGetFileAttributesEx(
                    m_LogFilename, 
                    GetFileExInfoStandard, 
                    &fileData) == FALSE)
            {
                return;
            }

            unsigned __int64 fileSize = 
                 (((unsigned __int64) fileData.nFileSizeHigh) << 32) |
                  ((unsigned __int64) fileData.nFileSizeLow);

                
            if (fileSize > (unsigned __int64) m_MaxSize)
            {
                fLogNeedsReset = TRUE;
            }
        }
        else
        {
            fLogNeedsReset = CheckLogHeader();
        }

        if (fLogNeedsReset)
        {
            // Push current log out to .old file
            BOOL success = WszMoveFileEx(
                m_LogFilename.GetUnicode(),
                m_OldLogFilename.GetUnicode(),
                MOVEFILE_REPLACE_EXISTING | MOVEFILE_COPY_ALLOWED);

            if (success || GetLastError() != ERROR_SHARING_VIOLATION)
            {
                return;
            }
        }
        else
        {
            // Someone else moved the file before we can.
            return;
        }

        // Don't want to hold the lock while sleeping.
        lock.Release();
        ClrSleepEx(MOVE_FILE_RETRY_TIME, FALSE);
    }
}

#define OPEN_FILE_RETRY_TIME 100
#define OPEN_FILE_RETRY_COUNT 10
// Normally we open file with FILE_SHARE_WRITE, to avoid sharing violations between multiple threads or
// processes.  However, when we create a new file, the Unicode header must be written at the beginning of the
// file.  This can't be guaranteed with multiple writers, so we require exclusive access while creating a new
// file.  Our algorithm is first try to open with OPEN_EXISTING and FILE_SHARE_WRITE, and if that fails, try
// again with OPEN_ALWAYS and no write sharing.
HANDLE CircularLog::OpenFile()
{
    for (int i = 0; i < OPEN_FILE_RETRY_COUNT; i++)
    {
        // First try to open an existing file allowing shared write.
        HANDLE hLogFile = WszCreateFile(
            m_LogFilename.GetUnicode(),
            FILE_APPEND_DATA,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL);

        if (hLogFile != INVALID_HANDLE_VALUE)
        {
            return hLogFile;
        }

        if (GetLastError() == ERROR_FILE_NOT_FOUND)
        {
            // Try to create an new file with exclusive access.
            HANDLE hLogFile = WszCreateFile(
                m_LogFilename.GetUnicode(),
                FILE_APPEND_DATA,
                FILE_SHARE_READ,
                NULL,
                OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL,
                NULL);

            if (hLogFile != INVALID_HANDLE_VALUE)
            {
                LARGE_INTEGER fileSize;
                if (! GetFileSizeEx(hLogFile, &fileSize))
                {
                    CloseHandle(hLogFile);
                    return INVALID_HANDLE_VALUE;
                }

                // If the file size is 0, need to write the Unicode header (utf8 bom).
                if (fileSize.QuadPart == 0)
                {
                    CHAR unicodeHeader []= {(char)0xef, (char)0xbb, (char)0xbf};
                    DWORD dwWritten;
                    WriteFile(hLogFile, &unicodeHeader, sizeof(unicodeHeader), &dwWritten, NULL);

                    // Write out header

                    // Convert to Utf8 to reduce typical log file size
                    StackScratchBuffer bufUtf8;
                    COUNT_T cBytesUtf8 = 0;
                    const UTF8 * pszUtf8Log = m_LogHeader.GetUTF8(bufUtf8, &cBytesUtf8);
                    // Remove null terminator from log entry buffer
                    cBytesUtf8--;

                    WriteFile(hLogFile, pszUtf8Log, (DWORD)cBytesUtf8, &dwWritten, NULL);
                }

                return hLogFile;
            }
        }

        if (GetLastError() != ERROR_SHARING_VIOLATION)
        {
            break;
        }

        ClrSleepEx(OPEN_FILE_RETRY_TIME, FALSE);
    }

    return INVALID_HANDLE_VALUE;

}
