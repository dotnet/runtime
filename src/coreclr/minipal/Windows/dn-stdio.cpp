// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-stdio.h>

int u16_fopen_s(FILE** stream, const WCHAR* path, const WCHAR* mode)
{
    return _wfopen_s(stream, path, mode);
}

int64_t fgetsize(FILE* stream)
{
    fpos_t current;
    fgetpos(stream, &current);
    fseek(stream, 0, SEEK_END);
    int64_t length = _ftelli64(stream);
    fsetpos(stream, &current);
    return length;
}

int64_t ftell_64(FILE* stream)
{
    fpos_t pos;
    fgetpos(stream, &pos);
    return pos;
}

int fsetpos_64(FILE* stream, int64_t pos)
{
    fpos_t fpos = pos;
    return fsetpos(stream, &fpos);
}

HRESULT HRESULTFromErrno()
{
    // stdio functions preserve last error in simple cases.
    // It's sufficient for logging only.
    HRESULT hr = HRESULT_FROM_WIN32(::GetLastError());

    if (SUCCEEDED(hr) && errno != 0)
    {
        // Fallback when last error was cleared unexpectedly.
        return E_FAIL;
    }

    return hr;
}
