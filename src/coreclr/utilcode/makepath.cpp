// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/***
*makepath.c - create path name from components
*

*
*Purpose:
*       To provide support for creation of full path names from components
*
*******************************************************************************/
#include "stdafx.h"
#include "utilcode.h"
#include "ex.h"

// Returns the directory for clr module. So, if path was for "C:\Dir1\Dir2\Filename.DLL",
// then this would return "C:\Dir1\Dir2\" (note the trailing backslash).HRESULT GetClrModuleDirectory(SString& wszPath)
HRESULT GetClrModuleDirectory(SString& wszPath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD dwRet = GetClrModulePathName(wszPath);

    if (dwRet == 0)
    {   // Some other error.
        return HRESULT_FROM_GetLastError();
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        SString::Iterator iter = wszPath.End();
        if (wszPath.FindBack(iter,DIRECTORY_SEPARATOR_CHAR_W))
        {
            iter++;
            wszPath.Truncate(iter);
        }
        else
        {
            hr = E_UNEXPECTED;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
