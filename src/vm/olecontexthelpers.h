//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// OleContextHelpers.h
//

//
// Helper APIs for interacting with Ole32 contexts & apartments.

#ifndef _H_OLECONTEXTHELPERS
#define _H_OLECONTEXTHELPERS

#ifndef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#error FEATURE_COMINTEROP_APARTMENT_SUPPORT
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#include "contxt.h"
#include "mtx.h"
#include "ctxtcall.h"

//================================================================
// OLE32 Context helpers.
LPVOID              GetCurrentCtxCookie();
HRESULT             GetCurrentObjCtx(IUnknown** ppObjCtx);
LPVOID              SetupOleContext();
HRESULT             GetCurrentThreadTypeNT5(THDTYPE* pType);
HRESULT             GetCurrentApartmentTypeNT5(IObjectContext *pObjCurrCtx, APTTYPE* pType);

#endif // _H_OLECONTEXTHELPERS
