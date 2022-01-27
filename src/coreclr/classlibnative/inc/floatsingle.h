// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FLOATSINGLE_H_
#define _FLOATSINGLE_H_

#include <object.h>
#include <fcall.h>

class COMSingle {
public:
    FCDECL1_V(static float, Acos, float x);
    FCDECL1_V(static float, Acosh, float x);
    FCDECL1_V(static float, Asin, float x);
    FCDECL1_V(static float, Asinh, float x);
    FCDECL1_V(static float, Atan, float x);
    FCDECL1_V(static float, Atanh, float x);
    FCDECL2_VV(static float, Atan2, float y, float x);
    FCDECL1_V(static float, Cbrt, float x);
    FCDECL1_V(static float, Ceil, float x);
    FCDECL1_V(static float, Cos, float x);
    FCDECL1_V(static float, Cosh, float x);
    FCDECL1_V(static float, Exp, float x);
    FCDECL1_V(static float, Floor, float x);
    FCDECL2_VV(static float, FMod, float x, float y);
    FCDECL3_VVV(static float, FusedMultiplyAdd, float x, float y, float z);
    FCDECL1_V(static float, Log, float x);
    FCDECL1_V(static float, Log2, float x);
    FCDECL1_V(static float, Log10, float x);
    FCDECL2_VI(static float, ModF, float x, float* intptr);
    FCDECL2_VV(static float, Pow, float x, float y);
    FCDECL1_V(static float, Sin, float x);
    FCDECL3_VII(static void, SinCos, float x, float* sin, float* cos);
    FCDECL1_V(static float, Sinh, float x);
    FCDECL1_V(static float, Sqrt, float x);
    FCDECL1_V(static float, Tan, float x);
    FCDECL1_V(static float, Tanh, float x);
};

#endif // _FLOATSINGLE_H_
