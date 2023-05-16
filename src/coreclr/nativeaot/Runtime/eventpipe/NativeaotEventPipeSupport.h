// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __NativeaotEventPipeSupport_h__
#define __NativeaotEventPipeSupport_h__

// This file is included only when compiling shared EventPipe source files and contains any
// definitions which are needed by these source files but are not available in NativeAOT
// runtime source files.

// As mentioned PalRedhawk*.cpp, in general we don't want to assume that Windows and
// Redhawk global definitions can co-exist, meaning NativeAOT runtime source files
// generally do not have access to windows.h; that said, the HOST_WIN32 parts of the shared
// EventPipe code are designed to rely on windows.h, so windows.h must be included when
// compiling shared EventPipe source files, and a marker is set to indicate that windows.h
// has been added to the compilation in this manner.

#include <windows.h>

#define BUILDING_SHARED_NATIVEAOT_EVENTPIPE_CODE

#endif // __NativeaotEventPipeSupport_h__
