//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ---------------------------------------------------------------------------
// SString_COM.cpp

// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "sstring.h"
#include "ex.h"
#include "holder.h"

#define DEFAULT_RESOURCE_STRING_SIZE 255

//----------------------------------------------------------------------------
// Load the string resource into this string.
//----------------------------------------------------------------------------
BOOL SString::LoadResource(CCompRC::ResourceCategory eCategory, int resourceID)
{
    return SUCCEEDED(LoadResourceAndReturnHR(eCategory, resourceID));
}

HRESULT SString::LoadResourceAndReturnHR(CCompRC::ResourceCategory eCategory, int resourceID)
{
    WRAPPER_NO_CONTRACT;
    return LoadResourceAndReturnHR(NULL, eCategory,resourceID);
}

HRESULT SString::LoadResourceAndReturnHR(CCompRC* pResourceDLL, CCompRC::ResourceCategory eCategory, int resourceID)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
    }
    CONTRACT_END;

    HRESULT hr = E_FAIL;

#ifndef FEATURE_UTILCODE_NO_DEPENDENCIES
    if (pResourceDLL == NULL) 
    {
        pResourceDLL = CCompRC::GetDefaultResourceDll();
    }
    
    if (pResourceDLL != NULL)
    {
 
        int size = 0;

        EX_TRY
        {
            if (GetRawCount() == 0)
                Resize(DEFAULT_RESOURCE_STRING_SIZE, REPRESENTATION_UNICODE);

            while (TRUE)
            {
                // First try and load the string in the amount of space that we have.
                // In fatal error reporting scenarios, we may not have enough memory to 
                // allocate a larger buffer.
            
                hr = pResourceDLL->LoadString(eCategory, resourceID, GetRawUnicode(), GetRawCount()+1,&size);
                if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                {
                    if (FAILED(hr))
                    {
                        Clear();
                        break;
                    }

                    // Although we cannot generally detect truncation, we can tell if we
                    // used up all the space (in which case we will assume truncation.)
                    if (size < (int)GetRawCount())
                    {
                        break;
                    }
                }

                // Double the size and try again.
                Resize(size*2, REPRESENTATION_UNICODE);

            }

            if (SUCCEEDED(hr))
            {
                Truncate(Begin() + (COUNT_T) wcslen(GetRawUnicode()));
            }

            Normalize();
            
        }
        EX_CATCH
        {
            hr = E_FAIL;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
#endif //!FEATURE_UTILCODE_NO_DEPENDENCIES
    
    RETURN hr;
} // SString::LoadResourceAndReturnHR
