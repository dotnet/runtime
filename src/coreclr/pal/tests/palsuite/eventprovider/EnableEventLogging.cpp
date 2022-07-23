
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  EnableEnventLogging.cpp
**
** Purpose: Fix linker issue on platforms where the PAL is built against
**          version 2.4 of liblttng-ust-dev
**
**
**===================================================================*/

#if defined(HOST_UNIX)
// This is a wrapper method for LTTng. See https://github.com/dotnet/coreclr/pull/27273 for details.
extern "C" bool XplatEventLoggerIsEnabled()
{
    // As we are testing the lttng events here, enable them unconditionally.
    return true;
}
#endif // HOST_UNIX
