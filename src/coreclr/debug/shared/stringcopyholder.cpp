// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// StringCopyHolder.cpp
//

//
// This is the implementation of a simple holder for a copy of a string.
//
// ======================================================================================


// Initialize to Null.
StringCopyHolder::StringCopyHolder()
{
    m_szData = NULL;
}

// Dtor to free memory.
StringCopyHolder::~StringCopyHolder()
{
    Clear();
}

// Reset the string to NULL and free memory
void StringCopyHolder::Clear()
{
    if (m_szData != NULL)
    {
        delete [] m_szData;
        m_szData = NULL;
    }
}

//---------------------------------------------------------------------------------------
//
// Allocate a copy of the incoming string and assign it to this holder.
// pStringSrc can be NULL or a pointer to a null-terminated string.
//
// Arguments:
//    pStringSrc - string to be duplicated
//
// Returns:
//    S_OK on success. That means it succeeded in allocating and copying pStringSrc.
//    If the incoming string is NULL, then the underlying string will be NULL as welll.
//    Callers may want to assert that IsSet() is true if they don't expect a NULL string.
//
//    E_OUTOFMEMORY on failure. Only happens in an OOM scenario.
//
// Notes:
//    Since this function is a callback from DAC, it must not take the process lock.
//    If it does, we may deadlock between the DD lock and the process lock.
//    If we really need to take the process lock for whatever reason, we must take it in the DBI functions
//    which call the DAC API that ends up calling this function.
//    See code:InternalDacCallbackHolder for more information.
//

HRESULT StringCopyHolder::AssignCopy(const WCHAR * pStringSrc)
{
    if (m_szData != NULL)
    {
        Clear();
    }

    if (pStringSrc == NULL)
    {
        m_szData = NULL;
    }
    else
    {
        SIZE_T cchLen = u16_strlen(pStringSrc) + 1;
        m_szData = new (nothrow) WCHAR[cchLen];
        if (m_szData == NULL)
        {
            _ASSERTE(!"Warning: Out-of-Memory in Right Side. This component is not robust in OOM sccenarios.");
            return E_OUTOFMEMORY;
        }

        wcscpy_s(m_szData, cchLen, pStringSrc);
    }
    return S_OK;
}
