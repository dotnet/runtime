// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>

//
// Wrappers for stdio with UTF-16 path.
//

int u16_fopen_s(FILE** stream, const WCHAR* path, const WCHAR* mode);
int64_t fgetsize(FILE* stream);
int64_t ftell_64(FILE* stream);
int fsetpos_64(FILE* stream, int64_t pos);
HRESULT HRESULT_FROM_LAST_STDIO();
