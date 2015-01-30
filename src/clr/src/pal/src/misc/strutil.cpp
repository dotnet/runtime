//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    strutil.cpp

Abstract:
    Various string-related utility functions



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(PAL);

using namespace CorUnix;

/*++
Function:
  CPalString::CopyString

  Copies a CPalString into a new (empty) instance, allocating buffer space
  as necessary

Parameters:
  pthr -- thread data for calling thread
  psSource -- the string to copy from
--*/

PAL_ERROR
CPalString::CopyString(
    CPalThread *pthr,
    CPalString *psSource
    )
{
    PAL_ERROR palError = NO_ERROR;
        
    _ASSERTE(NULL != psSource);
    _ASSERTE(NULL == m_pwsz);
    _ASSERTE(0 == m_dwStringLength);
    _ASSERTE(0 == m_dwMaxLength);    

    if (0 != psSource->GetStringLength())
    {
        _ASSERTE(psSource->GetMaxLength() > psSource->GetStringLength());
        
        WCHAR *pwsz = reinterpret_cast<WCHAR*>(
            InternalMalloc(pthr, psSource->GetMaxLength() * sizeof(WCHAR))
            );

        if (NULL != pwsz)
        {
            _ASSERTE(NULL != psSource->GetString());
            
            CopyMemory(
                pwsz,
                psSource->GetString(),
                psSource->GetMaxLength() * sizeof(WCHAR)
                );

            m_pwsz = pwsz;
            m_dwStringLength = psSource->GetStringLength();
            m_dwMaxLength = psSource->GetMaxLength();
        }
        else
        {
            palError = ERROR_OUTOFMEMORY;
        }
    }

    return palError;
}

/*++
Function:
  CPalString::FreeBuffer

  Frees the contained string buffer

Parameters:
  pthr -- thread data for calling thread
--*/

void
CPalString::FreeBuffer(
    CPalThread *pthr
    )
{
    _ASSERTE(NULL != m_pwsz);
    
    InternalFree(pthr, const_cast<WCHAR*>(m_pwsz));
}

/*++
Function:
  InternalWszNameFromSzName

  Helper function to convert an ANSI string object name parameter to a
  unicode string

Parameters:
  pthr -- thread data for calling thread
  pszName -- the ANSI string name
  pwszName -- on success, receives the converted unicode string
  cch -- the size of pwszName, in characters
--*/

PAL_ERROR
CorUnix::InternalWszNameFromSzName(
    CPalThread *pthr,
    LPCSTR pszName,
    LPWSTR pwszName,
    DWORD cch
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pszName);
    _ASSERTE(NULL != pwszName);
    _ASSERTE(0 < cch);
    
    if (MultiByteToWideChar(CP_ACP, 0, pszName, -1, pwszName, cch) == 0)
    {
        palError = pthr->GetLastError();
        if (ERROR_INSUFFICIENT_BUFFER == palError)
        {
            ERROR("pszName is larger than cch (%d)!\n", palError);
            palError = ERROR_FILENAME_EXCED_RANGE;
        }
        else
        {
            ERROR("MultiByteToWideChar failure! (error=%d)\n", palError);
            palError = ERROR_INVALID_PARAMETER;
        }            
    }

    return palError;
}

