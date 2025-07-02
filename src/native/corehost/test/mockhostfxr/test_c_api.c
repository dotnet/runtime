// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These headers provide access to the public hosting APIs. This file tests that
// they can be included in pure C code without causing compilation errors.
// Since the runtime repository primarily uses these APIs in C++ code, such issues
// might not be caught during regular development or testing.
#include <coreclr_delegates.h>
#include <hostfxr.h>
#include <nethost/nethost.h>
