// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _FLOATSINGLE_H_
#define _FLOATSINGLE_H_

#include <object.h>
#include <fcall.h>

class COMSingle {
public:
    FCDECL1_V(static float, Abs, float x);
    FCDECL1_V(static float, Acos, float x);
    FCDECL1_V(static float, Asin, float x);
    FCDECL1_V(static float, Atan, float x);
    FCDECL2_VV(static float, Atan2, float y, float x);
    FCDECL1_V(static float, Ceil, float x);
    FCDECL1_V(static float, Cos, float x);
    FCDECL1_V(static float, Cosh, float x);
    FCDECL1_V(static float, Exp, float x);
    FCDECL1_V(static float, Floor, float x);
    FCDECL1_V(static float, Log, float x);
    FCDECL1_V(static float, Log10, float x);
    FCDECL1(static float, ModF, float* iptr);
    FCDECL2_VV(static float, Pow, float x, float y);
    FCDECL1_V(static float, Round, float x);
    FCDECL1_V(static float, Sin, float x);
    FCDECL1_V(static float, Sinh, float x);
    FCDECL1_V(static float, Sqrt, float x);
    FCDECL1_V(static float, Tan, float x);
    FCDECL1_V(static float, Tanh, float x);
};

#endif // _FLOATSINGLE_H_
