// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <emscripten.h>

#ifndef __EMSCRIPTEN__
#error Cryptography Native Browser is designed to be compiled with Emscripten.
#endif // __EMSCRIPTEN__

#ifndef PALEXPORT
#ifdef TARGET_UNIX
#define PALEXPORT __attribute__ ((__visibility__ ("default")))
#else
#define PALEXPORT __declspec(dllexport)
#endif
#endif // PALEXPORT
