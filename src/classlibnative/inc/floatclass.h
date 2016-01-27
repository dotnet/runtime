// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _FLOATCLASS_H_
#define _FLOATCLASS_H_

#include <object.h>
#include <fcall.h>

class COMDouble {
public:
    FCDECL1_V(static double, Floor, double d);
    FCDECL1_V(static double, Sqrt, double d);
    FCDECL1_V(static double, Log, double d);
    FCDECL1_V(static double, Log10, double d);
    FCDECL1_V(static double, Exp, double d);
    FCDECL2_VV(static double, Pow, double x, double y);
    FCDECL1_V(static double, Acos, double d);
    FCDECL1_V(static double, Asin, double d);
    FCDECL1_V(static double, Atan, double d);
    FCDECL2_VV(static double, Atan2, double x, double y);
    FCDECL1_V(static double, Cos, double d);
    FCDECL1_V(static double, Sin, double d);
    FCDECL1_V(static double, Tan, double d);
    FCDECL1_V(static double, Cosh, double d);
    FCDECL1_V(static double, Sinh, double d);
    FCDECL1_V(static double, Tanh, double d);
    FCDECL1_V(static double, Round, double d);
    FCDECL1_V(static double, Ceil, double d);
    FCDECL1_V(static float, AbsFlt, float f);
    FCDECL1_V(static double, AbsDbl, double d);
    FCDECL1(static double, ModFDouble, double* d);

#if defined(_TARGET_X86_)
//private:
    FCDECL2_VV(static double, PowHelper, double x, double y);
    FCDECL2_VV(static double, PowHelperSimple, double x, double y);
#endif

};
    

#endif // _FLOATCLASS_H_
