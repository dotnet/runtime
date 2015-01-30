//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ===========================================================================
// File: umisc.h
//

// ===========================================================================


// Abstract:
//
//	A collection of utility macros.
//

#ifndef UMISC_H
#define UMISC_H

#define COM_METHOD  HRESULT STDMETHODCALLTYPE

inline HRESULT HrFromWin32(DWORD dwWin32Error)
{
    return HRESULT_FROM_WIN32(dwWin32Error);
}

// Some helper #def's to safely Release, close & delete Objects under
// failure conditions
	
#define RELEASE(x) 			    \
	do						    \
	{						    \
		if (x)				    \
		{					    \
            IUnknown *punk = x; \
            x = NULL;           \
            punk->Release();    \
		}					    \
	} while (0)				
	

#include "debugmacros.h"	
//
// Good for verifying params withing range.
//
#define IfFalseGo(expr, HR) IfFailGo((expr) ? S_OK : (HR))

// ----------------------------------------------------------------------------
// Validation macros
// Note that the Win32 APIs like IsBadReadPtr are banned
//
#define IsValidReadPtr(ptr, type) ((ptr)!=NULL)

#define IsValidWritePtr(ptr, type) ((ptr)!=NULL)

#define IsValidReadBufferPtr(ptr, type, len) ((ptr)!=NULL)

#define IsValidWriteBufferPtr(ptr, type, len) ((ptr)!=NULL)

#define IsValidInterfacePtr(ptr, type) ((ptr)!=NULL)

#define IsValidCodePtr(ptr) ((ptr)!=NULL)

#define IsValidStringPtr(ptr) ((ptr)!=NULL)

#define IsValidIID(iid) TRUE

#define IsValidCLSID(clsid) TRUE

#endif
