// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _PAL_UNICODEDATA_H_
#define _PAL_UNICODEDATA_H_

#include "pal/palinternal.h"

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#define UPPER_CASE 1
#define LOWER_CASE 2

typedef struct
{
  WCHAR nUnicodeValue;
  WORD  nFlag;
  WCHAR nOpposingCase;
} UnicodeDataRec;

extern CONST UnicodeDataRec UnicodeData[];
extern CONST UINT UNICODE_DATA_SIZE;

#ifdef __cplusplus
}
#endif // __cplusplus

#endif  /* _UNICODE_DATA_H_ */
