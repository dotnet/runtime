// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _FLOATDOUBLE_H_
#define _FLOATDOUBLE_H_

#include <object.h>
#include <fcall.h>

class COMDouble {
public:
    FCDECL1_V(static double, Abs, double x);
    FCDECL1_V(static double, Acos, double x);
    FCDECL1_V(static double, Asin, double x);
    FCDECL1_V(static double, Atan, double x);
    FCDECL2_VV(static double, Atan2, double y, double x);
    FCDECL1_V(static double, Ceil, double x);
    FCDECL1_V(static double, Cos, double x);
    FCDECL1_V(static double, Cosh, double x);
    FCDECL1_V(static double, Exp, double x);
    FCDECL1_V(static double, Floor, double x);
    FCDECL1_V(static double, Log, double x);
    FCDECL1_V(static double, Log10, double x);
    FCDECL1(static double, ModF, double* iptr);
    FCDECL2_VV(static double, Pow, double x, double y);
    FCDECL1_V(static double, Round, double x);
    FCDECL1_V(static double, Sin, double x);
    FCDECL1_V(static double, Sinh, double x);
    FCDECL1_V(static double, Sqrt, double x);
    FCDECL1_V(static double, Tan, double x);
    FCDECL1_V(static double, Tanh, double x);
};

#endif // _FLOATDOUBLE_H_
