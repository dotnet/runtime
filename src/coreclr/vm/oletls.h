// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _OLETLS_H_
#define _OLETLS_H_

#ifndef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#error FEATURE_COMINTEROP_APARTMENT_SUPPORT is required for this file
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

// See https://learn.microsoft.com/previous-versions/windows/desktop/legacy/ms690269(v=vs.85)
typedef struct _SOleTlsData {
  void  *pvReserved0[2];
  DWORD dwReserved0[3];
  void  *pvReserved1[1];
  DWORD dwReserved1[3];
  void  *pvReserved2[4];
  DWORD dwReserved2[1];
  void  *pCurrentCtx;
} SOleTlsData, *PSOleTlsData;

#endif // _OLETLS_H_
