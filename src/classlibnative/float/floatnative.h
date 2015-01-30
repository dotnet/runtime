//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: FloatNative.h
// 

#ifndef _FLOATNATIVE_H
#define _FLOATNATIVE_H

// Removed due to compiler bug
//
// _CRTIMP double __cdecl floor(double);
// _CRTIMP double __cdecl ceil(double);

double __cdecl sqrt(double);
double __cdecl log(double);
double __cdecl log10(double);
double __cdecl exp(double);
double __cdecl pow(double, double);
double __cdecl acos(double);
double __cdecl asin(double);
double __cdecl atan(double);
double __cdecl atan2(double,double);
double __cdecl cos(double);
double __cdecl sin(double);
double __cdecl tan(double);
double __cdecl cosh(double);
double __cdecl sinh(double);
double __cdecl tanh(double);
double __cdecl fmod(double, double);

#endif  // _FLOATNATIVE_H
