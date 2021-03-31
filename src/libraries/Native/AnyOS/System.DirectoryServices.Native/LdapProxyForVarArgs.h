// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifdef _WIN32
    #include <stdint.h>
    #include <windows.h>
    #define FUNCTIONEXPORT extern
    #define FUNCTIONCALLINGCONVENCTION __cdecl
#else
    #include "pal_types.h"
    #include "pal_compiler.h"
    #define FUNCTIONEXPORT PALEXPORT
    #define FUNCTIONCALLINGCONVENCTION
#endif

typedef struct BerElementStruct BerElement;

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy(BerElement* ber, char *fmt);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_int(BerElement *ber, char *fmt, int* value);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_bitstring(BerElement *ber, char *fmt, int** value, int* bitLength);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_ptr(BerElement *ber, char *fmt, int** value);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_emptyarg(BerElement *ber, char *fmt);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_int(BerElement *ber, char *fmt, int value);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_bytearray(BerElement *ber, char *fmt, int* value, int length);

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_berarray(BerElement *ber, char *fmt, int* value);
