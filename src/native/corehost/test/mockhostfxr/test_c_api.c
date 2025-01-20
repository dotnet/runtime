// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// hostfxr.h is a public API. When included in .c files, it may fail to compile
// if C++-specific syntax is used within the extern "C" block. Since all usage of
// this API in runtime repo are within C++ code, such breakages are not encountered
// during normal development or testing.
#include "hostfxr.h"
