// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __APPHOST_WINDOWS_H__
#define __APPHOST_WINDOWS_H__

namespace apphost
{
    void buffer_errors();
    void write_buffered_errors(int error_code);
}

#endif // __APPHOST_WINDOWS_H__
