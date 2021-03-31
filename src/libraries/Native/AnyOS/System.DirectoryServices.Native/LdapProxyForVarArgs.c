// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "LdapProxyForVarArgs.h"
#include <stdio.h>

#ifdef _WIN32
FUNCTIONEXPORT ULONG FUNCTIONCALLINGCONVENCTION ber_scanf(BerElement* ber, char* fmt, ...);
FUNCTIONEXPORT INT FUNCTIONCALLINGCONVENCTION ber_printf(BerElement* ber, char* fmt, ...);
#else
FUNCTIONEXPORT long FUNCTIONCALLINGCONVENCTION ber_scanf(BerElement* ber, const char* fmt, ...);
FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf(BerElement* ber, const char* fmt, ...);
#endif

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy(BerElement *ber, char *fmt)
{
    return (int)ber_scanf(ber, fmt);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_int(BerElement *ber, char *fmt, int* value)
{
    return (int)ber_scanf(ber, fmt, value);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_bitstring(BerElement *ber, char *fmt, int** value, int* bitLength)
{
    return (int)ber_scanf(ber, fmt, value, bitLength);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_scanf_proxy_ptr(BerElement *ber, char *fmt, int** value)
{
    return (int)ber_scanf(ber, fmt, value);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_emptyarg(BerElement *ber, char *fmt)
{
    return ber_printf(ber, fmt);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_int(BerElement *ber, char *fmt, int value)
{
    return ber_printf(ber, fmt, value);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_bytearray(BerElement *ber, char *fmt, int* value, int length)
{
    return ber_printf(ber, fmt, value, length);
}

FUNCTIONEXPORT int FUNCTIONCALLINGCONVENCTION ber_printf_proxy_berarray(BerElement *ber, char *fmt, int* value)
{
    return ber_printf(ber, fmt, value);
}
