// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef _WIN32
    #include <stdint.h>
    #include <windows.h>
    #define FUNCTIONEXPORT
    #define FUNCTIONCALLINGCONVENTION WINAPI
#else
    #include "pal_types.h"
    #include "pal_compiler.h"
    #define FUNCTIONEXPORT PALEXPORT
    #define FUNCTIONCALLINGCONVENTION
#endif
#include <zconf.h> // voidpf

FUNCTIONEXPORT voidpf FUNCTIONCALLINGCONVENTION z_custom_calloc(voidpf opaque, unsigned items, unsigned size);

FUNCTIONEXPORT void FUNCTIONCALLINGCONVENTION z_custom_cfree(voidpf opaque, voidpf ptr);