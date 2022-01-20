// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FLOATDOUBLE_H_
#define _FLOATDOUBLE_H_

#include <object.h>
#include <fcall.h>

class COMDouble {
public:
    FCDECL1_V(static double, Acos, double x);
    FCDECL1_V(static double, Acosh, double x);
    FCDECL1_V(static double, Asin, double x);
    FCDECL1_V(static double, Asinh, double x);
    FCDECL1_V(static double, Atan, double x);
    FCDECL1_V(static double, Atanh, double x);
    FCDECL2_VV(static double, Atan2, double y, double x);
    FCDECL1_V(static double, Cbrt, double x);
    FCDECL1_V(static double, Ceil, double x);
    FCDECL1_V(static double, Cos, double x);
    FCDECL1_V(static double, Cosh, double x);
    FCDECL1_V(static double, Exp, double x);
    FCDECL1_V(static double, Floor, double x);
    FCDECL2_VV(static double, FMod, double x, double y);
    FCDECL3_VVV(static double, FusedMultiplyAdd, double x, double y, double z);
    FCDECL1_V(static double, Log, double x);
    FCDECL1_V(static double, Log2, double x);
    FCDECL1_V(static double, Log10, double x);
    FCDECL2_VI(static double, ModF, double x, double* intptr);
    FCDECL2_VV(static double, Pow, double x, double y);
    FCDECL1_V(static double, Sin, double x);
    FCDECL3_VII(static void, SinCos, double x, double* sin, double* cos);
    FCDECL1_V(static double, Sinh, double x);
    FCDECL1_V(static double, Sqrt, double x);
    FCDECL1_V(static double, Tan, double x);
    FCDECL1_V(static double, Tanh, double x);
};

#endif // _FLOATDOUBLE_H_
