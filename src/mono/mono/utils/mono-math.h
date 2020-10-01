/**
 * \file
 */

#ifndef __MONO_MATH_H__
#define __MONO_MATH_H__

#include <math.h>
#include <mono/utils/mono-publib.h>

// Instead of isfinite, isinf, isnan, etc.,
// use mono_isfininite, mono_isinf, mono_isnan, etc.
// These functions are implemented in C in order to avoid
// a C++ runtime dependency and for more portable binding
// from C++, esp. across Android versions/architectures.
// WebAssembly, and Win32/gcc.
// See https://github.com/mono/mono/pull/10701 for what
// this systematically and more portably cleans up.

#if defined (__cplusplus) || defined (MONO_MATH_DECLARE_ALL)

// These declarations are usually hidden, in order
// to encourage using the overloaded names instead of
// the type-specific names.

G_EXTERN_C    int mono_isfinite_float (float);
G_EXTERN_C    int mono_isfinite_double (double);
G_EXTERN_C    int mono_isinf_float (float);
G_EXTERN_C    int mono_isinf_double (double);
G_EXTERN_C    int mono_isnan_float (float);
G_EXTERN_C    int mono_isnan_double (double);
G_EXTERN_C    int mono_isunordered_float (float, float);
G_EXTERN_C    int mono_isunordered_double (double, double);
G_EXTERN_C    int mono_signbit_float (float a);
G_EXTERN_C    int mono_signbit_double (double a);
G_EXTERN_C  float mono_trunc_float (float);
G_EXTERN_C double mono_trunc_double (double);

#endif

#ifdef __cplusplus

// There are three or four possible approaches here.
// 1. C++ mono_foo => foo
// 2. C++ mono_foo => std::foo
// 3. C++ mono_foo => C mono_foo_[float,double] => C foo
// 4. using std::foo -- this works mostly but not quite -- it doesn't
// work when there is already a global foo.
//
// Approach 1 works on non-wasm, non-android non-Win32/gcc.
// Approach 2 should work everywhere, but might incur a new dependency, might.
// Approach 3 should work everywhere, with identical dependencies as mono/C.
// This is approach 3.
// Approach 4 lets code keep calling foo instead of mono_foo.
// Approaches 1, 2, 4 are most efficient. 1, 2 require inlining, 4 does not.

inline    int mono_isfinite (float a)               { return mono_isfinite_float (a); }
inline    int mono_isfinite (double a)              { return mono_isfinite_double (a); }
inline    int mono_isinf (float a)                  { return mono_isinf_float (a); }
inline    int mono_isinf (double a)                 { return mono_isinf_double (a); }
inline    int mono_isnan (float a)                  { return mono_isnan_float (a); }
inline    int mono_isnan (double a)                 { return mono_isnan_double (a); }
inline    int mono_isunordered (float a, float b)   { return mono_isunordered_float (a, b); }
inline    int mono_isunordered (double a, double b) { return mono_isunordered_double (a, b); }
inline    int mono_signbit (float a)                { return mono_signbit_float (a); }
inline    int mono_signbit (double a)               { return mono_signbit_double (a); }
inline  float mono_trunc (float a)                  { return mono_trunc_float (a); }
inline double mono_trunc (double a)                 { return mono_trunc_double (a); }

#else

// Direct macros for C.
// This will also work for many C++ platforms, i.e. other than Android and WebAssembly and Win32/gcc.
#define mono_isfinite        isfinite
#define mono_isinf           isinf
#define mono_isnan           isnan
#define mono_isunordered     isunordered
#define mono_signbit         signbit
#define mono_trunc           trunc

#endif

static inline double
mono_round_to_even (double x)
{
	double floor_tmp;

	/* If the number has no fractional part do nothing This shortcut is necessary
	 * to workaround precision loss in borderline cases on some platforms */
	if (x == (double)(int64_t) x)
		return x;

	floor_tmp = floor (x + 0.5);

	if ((x == (floor (x) + 0.5)) && (fmod (floor_tmp, 2.0) != 0)) {
		floor_tmp -= 1.0;
	}

	return copysign (floor_tmp, x);
}

#endif
