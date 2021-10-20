// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: runtimeinfo.cpp
//
// The runtime info export
//
//*****************************************************************************

#include <windows.h>
#include <runtimeinfo.h>

// Runtime information public export
#ifdef HOST_UNIX
DLLEXPORT
#endif
RuntimeInfo DotNetRuntimeInfo = {
    {
        RUNTIME_INFO_SIGNATURE
    },
    1,
    {
        #include <runtimemoduleindex.h>
    },
    {
        #include <dacmoduleindex.h>
    },
    {
        #include <dbimoduleindex.h>
    },
};
