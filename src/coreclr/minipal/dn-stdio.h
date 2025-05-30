// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>

//
// Wrappers for stdio with UTF-16 path.
//

int fopen_u16(FILE** stream, const WCHAR* path, const WCHAR* mode);
int64_t fgetsize(FILE* stream);
HRESULT HRESULT_FROM_LAST_STDIO();
