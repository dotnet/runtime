// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_INC_INTEROPLIB_H_
#define _INTEROP_INC_INTEROPLIB_H_

namespace InteropLib
{

#ifdef _WIN32

// Get internal interop IUnknown dispatch pointers.
void GetIUnknownImpl(
    _Out_ void** fpQueryInterface,
    _Out_ void** fpAddRef,
    _Out_ void** fpRelease);

#endif // _WIN32

}

#endif // _INTEROP_INC_INTEROPLIB_H_

