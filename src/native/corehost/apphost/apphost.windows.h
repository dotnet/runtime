// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __APPHOST_WINDOWS_H__
#define __APPHOST_WINDOWS_H__

#ifdef __cplusplus
extern "C" {
#endif

void apphost_buffer_errors(void);
void apphost_write_buffered_errors(int error_code);

#ifdef __cplusplus
}
#endif

#endif // __APPHOST_WINDOWS_H__
