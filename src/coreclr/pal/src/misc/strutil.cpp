// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
  psSource -- the string to copy from
--*/

PAL_ERROR
CPalString::CopyString(
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
            malloc(psSource->GetMaxLength() * sizeof(WCHAR))
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

--*/

void
CPalString::FreeBuffer()
{
    _ASSERTE(NULL != m_pwsz);
    free(const_cast<WCHAR*>(m_pwsz));
}
