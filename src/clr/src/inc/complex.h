//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// complex.h
// 

// 
// Defines a basic complex number data type.  We cannot use the standard C++ library's
// complex implementation, because the CLR links to the wrong CRT.
//

#ifndef _COMPLEX_H_
#define _COMPLEX_H_

#include <math.h>

//
// Default compilation mode is /fp:precise, which disables fp intrinsics. This causes us to pull in FP stuff (sqrt,etc.) from
// The CRT, and increases our download size.  We don't need the extra precision this gets us, so let's switch to 
// the intrinsic versions.
//
#ifdef _MSC_VER
#pragma float_control(precise, off, push)
#endif


class Complex
{
public:
    double r;
    double i;

    Complex() : r(0), i(i) {}
    Complex(double real) : r(real), i(0) {}
    Complex(double real, double imag) : r(real), i(imag) {}
    Complex(const Complex& other) : r(other.r), i(other.i) {}
};

inline Complex operator+(Complex left, Complex right)
{
    LIMITED_METHOD_CONTRACT;
    return Complex(left.r + right.r, left.i + right.i);
}

inline Complex operator-(Complex left, Complex right)
{
    LIMITED_METHOD_CONTRACT;
    return Complex(left.r - right.r, left.i - right.i);
}

inline Complex operator*(Complex left, Complex right)
{
    LIMITED_METHOD_CONTRACT;
    return Complex(
        left.r * right.r - left.i * right.i,
        left.r * right.i + left.i * right.r);
}

inline Complex operator/(Complex left, Complex right)
{
    LIMITED_METHOD_CONTRACT;
    double denom = right.r * right.r + right.i * right.i;
    return Complex(
        (left.r * right.r + left.i * right.i) / denom,
        (-left.r * right.i + left.i * right.r) / denom);
}

inline double abs(Complex c)
{
    LIMITED_METHOD_CONTRACT;
    return sqrt(c.r * c.r + c.i * c.i);
}

#ifdef _MSC_VER
#pragma float_control(pop)
#endif


#endif //_COMPLEX_H_
