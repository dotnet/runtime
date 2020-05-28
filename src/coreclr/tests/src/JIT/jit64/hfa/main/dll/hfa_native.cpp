// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "hfa_native.h"

#ifndef _MSC_VER
#ifdef HOST_64BIT
#define __int64     long
#else // HOST_64BIT
#define __int64     long long
#endif // HOST_64BIT

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed
#endif

// ---------------------------------------------------
// Init Methods
// ---------------------------------------------------


HFADLL_API void  init_HFA01(HFA01& hfa) {
	hfa.f1 = static_cast<FLOATTYPE>(1);
}

HFADLL_API void  init_HFA02(HFA02& hfa) {
#ifdef NESTED_HFA
	init_HFA01(hfa.hfa01);
	hfa.f2 = static_cast<FLOATTYPE>(2);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
#endif
}

HFADLL_API void  init_HFA03(HFA03& hfa) {
#ifdef NESTED_HFA
	init_HFA01(hfa.hfa01);
	init_HFA02(hfa.hfa02);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
	hfa.f3 = static_cast<FLOATTYPE>(3);
#endif
}

HFADLL_API void  init_HFA05(HFA05& hfa) {
#ifdef NESTED_HFA
	init_HFA02(hfa.hfa02);
	init_HFA03(hfa.hfa03);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
	hfa.f3 = static_cast<FLOATTYPE>(3);
	hfa.f4 = static_cast<FLOATTYPE>(4);
	hfa.f5 = static_cast<FLOATTYPE>(5);
#endif
}

HFADLL_API void  init_HFA08(HFA08& hfa) {
#ifdef NESTED_HFA
	init_HFA03(hfa.hfa03);
	init_HFA05(hfa.hfa05);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
	hfa.f3 = static_cast<FLOATTYPE>(3);
	hfa.f4 = static_cast<FLOATTYPE>(4);
	hfa.f5 = static_cast<FLOATTYPE>(5);
	hfa.f6 = static_cast<FLOATTYPE>(6);
	hfa.f7 = static_cast<FLOATTYPE>(7);
	hfa.f8 = static_cast<FLOATTYPE>(8);
#endif
};

HFADLL_API void  init_HFA11(HFA11& hfa) {
#ifdef NESTED_HFA
	init_HFA03(hfa.hfa03);
	init_HFA08(hfa.hfa08);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
	hfa.f3 = static_cast<FLOATTYPE>(3);
	hfa.f4 = static_cast<FLOATTYPE>(4);
	hfa.f5 = static_cast<FLOATTYPE>(5);
	hfa.f6 = static_cast<FLOATTYPE>(6);
	hfa.f7 = static_cast<FLOATTYPE>(7);
	hfa.f8 = static_cast<FLOATTYPE>(8);
	hfa.f9 = static_cast<FLOATTYPE>(9);
	hfa.f10 = static_cast<FLOATTYPE>(10);
	hfa.f11 = static_cast<FLOATTYPE>(11);
#endif
};

HFADLL_API void  init_HFA19(HFA19& hfa) {
#ifdef NESTED_HFA
	init_HFA08(hfa.hfa08);
	init_HFA11(hfa.hfa11);
#else
	hfa.f1 = static_cast<FLOATTYPE>(1);
	hfa.f2 = static_cast<FLOATTYPE>(2);
	hfa.f3 = static_cast<FLOATTYPE>(3);
	hfa.f4 = static_cast<FLOATTYPE>(4);
	hfa.f5 = static_cast<FLOATTYPE>(5);
	hfa.f6 = static_cast<FLOATTYPE>(6);
	hfa.f7 = static_cast<FLOATTYPE>(7);
	hfa.f8 = static_cast<FLOATTYPE>(8);
	hfa.f9 = static_cast<FLOATTYPE>(9);
	hfa.f10 = static_cast<FLOATTYPE>(10);
	hfa.f11 = static_cast<FLOATTYPE>(11);
	hfa.f12 = static_cast<FLOATTYPE>(12);
	hfa.f13 = static_cast<FLOATTYPE>(13);
	hfa.f14 = static_cast<FLOATTYPE>(14);
	hfa.f15 = static_cast<FLOATTYPE>(15);
	hfa.f16 = static_cast<FLOATTYPE>(16);
	hfa.f17 = static_cast<FLOATTYPE>(17);
	hfa.f18 = static_cast<FLOATTYPE>(18);
	hfa.f19 = static_cast<FLOATTYPE>(19);
#endif
};


// --------------------------------------------------------------
// identity methods
// --------------------------------------------------------------

HFADLL_API HFA01  identity_HFA01(HFA01 hfa) {
	return hfa;
}

HFADLL_API HFA02  identity_HFA02(HFA02 hfa) {
	return hfa;
}

HFADLL_API HFA03  identity_HFA03(HFA03 hfa) {
	return hfa;
}

HFADLL_API HFA05  identity_HFA05(HFA05 hfa) {
	return hfa;
}

HFADLL_API HFA08  identity_HFA08(HFA08 hfa) {
	return hfa;
}

HFADLL_API HFA11  identity_HFA11(HFA11 hfa) {
	return hfa;
}

HFADLL_API HFA19  identity_HFA19(HFA19 hfa) {
	return hfa;
}



// --------------------------------------------------------------
// get methods
// --------------------------------------------------------------

HFADLL_API HFA01  get_HFA01() {
	HFA01 hfa;
	init_HFA01(hfa);
	return hfa;
}

HFADLL_API HFA02  get_HFA02() {
	HFA02 hfa;
	init_HFA02(hfa);
	return hfa;
}

HFADLL_API HFA03  get_HFA03() {
	HFA03 hfa;
	init_HFA03(hfa);
	return hfa;
}

HFADLL_API HFA05  get_HFA05() {
	HFA05 hfa;
	init_HFA05(hfa);
	return hfa;
}

HFADLL_API HFA08  get_HFA08() {
	HFA08 hfa;
	init_HFA08(hfa);
	return hfa;
}

HFADLL_API HFA11  get_HFA11() {
	HFA11 hfa;
	init_HFA11(hfa);
	return hfa;
}

HFADLL_API HFA19  get_HFA19() {
	HFA19 hfa;
	init_HFA19(hfa);
	return hfa;
}




// ---------------------------------------------------
// Sum Methods
// ---------------------------------------------------


#ifdef NESTED_HFA

#define EXPRESSION_SUM_HFA01(hfa)	(hfa.f1)
#define EXPRESSION_SUM_HFA02(hfa)	(sum_HFA01(hfa.hfa01) + hfa.f2)
#define EXPRESSION_SUM_HFA03(hfa)	(sum_HFA01(hfa.hfa01) + sum_HFA02(hfa.hfa02))
#define EXPRESSION_SUM_HFA05(hfa)	(sum_HFA02(hfa.hfa02) + sum_HFA03(hfa.hfa03))
#define EXPRESSION_SUM_HFA08(hfa)	(sum_HFA03(hfa.hfa03) + sum_HFA05(hfa.hfa05))
#define EXPRESSION_SUM_HFA11(hfa)	(sum_HFA03(hfa.hfa03) + sum_HFA08(hfa.hfa08))
#define EXPRESSION_SUM_HFA19(hfa)	(sum_HFA08(hfa.hfa08) + sum_HFA11(hfa.hfa11))

#else

#define EXPRESSION_SUM_HFA01(hfa)	(hfa.f1)
#define EXPRESSION_SUM_HFA02(hfa)	(hfa.f1 + hfa.f2)
#define EXPRESSION_SUM_HFA03(hfa)	(hfa.f1 + hfa.f2 + hfa.f3)
#define EXPRESSION_SUM_HFA05(hfa)	(hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5)
#define EXPRESSION_SUM_HFA08(hfa)	(hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8)
#define EXPRESSION_SUM_HFA11(hfa)	(hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11)
#define EXPRESSION_SUM_HFA19(hfa)	(hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19)

#endif

#define EXPRESSION_SUM3_HFA01(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA01(hfa1) + EXPRESSION_SUM_HFA01(hfa2) + EXPRESSION_SUM_HFA01(hfa3))
#define EXPRESSION_SUM3_HFA02(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA02(hfa1) + EXPRESSION_SUM_HFA02(hfa2) + EXPRESSION_SUM_HFA02(hfa3))
#define EXPRESSION_SUM3_HFA03(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA03(hfa1) + EXPRESSION_SUM_HFA03(hfa2) + EXPRESSION_SUM_HFA03(hfa3))
#define EXPRESSION_SUM3_HFA05(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA05(hfa1) + EXPRESSION_SUM_HFA05(hfa2) + EXPRESSION_SUM_HFA05(hfa3))
#define EXPRESSION_SUM3_HFA08(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA08(hfa1) + EXPRESSION_SUM_HFA08(hfa2) + EXPRESSION_SUM_HFA08(hfa3))
#define EXPRESSION_SUM3_HFA11(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA11(hfa1) + EXPRESSION_SUM_HFA11(hfa2) + EXPRESSION_SUM_HFA11(hfa3))
#define EXPRESSION_SUM3_HFA19(hfa1, hfa2, hfa3)	(EXPRESSION_SUM_HFA19(hfa1) + EXPRESSION_SUM_HFA19(hfa2) + EXPRESSION_SUM_HFA19(hfa3))

#define EXPRESSION_SUM5_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA01(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA01(hfa4) + EXPRESSION_SUM_HFA01(hfa5))
#define EXPRESSION_SUM5_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA02(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA02(hfa4) + EXPRESSION_SUM_HFA02(hfa5))
#define EXPRESSION_SUM5_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA03(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA03(hfa4) + EXPRESSION_SUM_HFA03(hfa5))
#define EXPRESSION_SUM5_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA05(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA05(hfa4) + EXPRESSION_SUM_HFA05(hfa5))
#define EXPRESSION_SUM5_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA08(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA08(hfa4) + EXPRESSION_SUM_HFA08(hfa5))
#define EXPRESSION_SUM5_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA11(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA11(hfa4) + EXPRESSION_SUM_HFA11(hfa5))
#define EXPRESSION_SUM5_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5)	(EXPRESSION_SUM3_HFA19(hfa1, hfa2, hfa3) + EXPRESSION_SUM_HFA19(hfa4) + EXPRESSION_SUM_HFA19(hfa5))

#define EXPRESSION_SUM8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA01(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA02(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA03(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA05(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA08(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA11(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8))
#define EXPRESSION_SUM8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	(EXPRESSION_SUM3_HFA19(hfa1, hfa2, hfa3) + (EXPRESSION_SUM5_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8))

#define EXPRESSION_SUM11_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA01(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA02(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA03(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA05(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA08(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA11(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))
#define EXPRESSION_SUM11_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	(EXPRESSION_SUM3_HFA19(hfa1, hfa2, hfa3) + (EXPRESSION_SUM8_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11))

#define EXPRESSION_SUM19_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA01(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA02(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA03(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA05(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA08(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA11(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))
#define EXPRESSION_SUM19_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_SUM8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) + EXPRESSION_SUM11_HFA19(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19))


HFADLL_API FLOATTYPE  sum_HFA01(HFA01 hfa) {
	return EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA02(HFA02 hfa) {
	return EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA03(HFA03 hfa) {
	return EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA05(HFA05 hfa) {
	return EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA08(HFA08 hfa) {
	return EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA11(HFA11 hfa) {
	return EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum_HFA19(HFA19 hfa) {
	return EXPRESSION_SUM_HFA19(hfa);
}


HFADLL_API FLOATTYPE  sum3_HFA01(float v1, __int64 v2, HFA01 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA02(float v1, __int64 v2, HFA02 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA03(float v1, __int64 v2, HFA03 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA05(float v1, __int64 v2, HFA05 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA08(float v1, __int64 v2, HFA08 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA11(float v1, __int64 v2, HFA11 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum3_HFA19(float v1, __int64 v2, HFA19 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + EXPRESSION_SUM_HFA19(hfa);
}


HFADLL_API FLOATTYPE  sum5_HFA01(__int64 v1, double v2, short v3, signed char v4, HFA01 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA02(__int64 v1, double v2, short v3, signed char v4, HFA02 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA03(__int64 v1, double v2, short v3, signed char v4, HFA03 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA05(__int64 v1, double v2, short v3, signed char v4, HFA05 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA08(__int64 v1, double v2, short v3, signed char v4, HFA08 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA11(__int64 v1, double v2, short v3, signed char v4, HFA11 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum5_HFA19(__int64 v1, double v2, short v3, signed char v4, HFA19 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + EXPRESSION_SUM_HFA19(hfa);
}


HFADLL_API FLOATTYPE  sum8_HFA01(float v1, double v2, __int64 v3, signed char v4, double v5, HFA01 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA02(float v1, double v2, __int64 v3, signed char v4, double v5, HFA02 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA03(float v1, double v2, __int64 v3, signed char v4, double v5, HFA03 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA05(float v1, double v2, __int64 v3, signed char v4, double v5, HFA05 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA08(float v1, double v2, __int64 v3, signed char v4, double v5, HFA08 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA11(float v1, double v2, __int64 v3, signed char v4, double v5, HFA11 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum8_HFA19(float v1, double v2, __int64 v3, signed char v4, double v5, HFA19 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + EXPRESSION_SUM_HFA19(hfa);
}


HFADLL_API FLOATTYPE  sum11_HFA01(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA01 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA02(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA02 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA03(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA03 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA05(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA05 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA08(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA08 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA11(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA11 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum11_HFA19(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA19 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + EXPRESSION_SUM_HFA19(hfa);
}


HFADLL_API FLOATTYPE  sum19_HFA01(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA01 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA01(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA02(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA02 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA02(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA03(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA03 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA03(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA05(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA05 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA05(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA08(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA08 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA08(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA11(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA11 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA11(hfa);
}

HFADLL_API FLOATTYPE  sum19_HFA19(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA19 hfa) {
	return static_cast<FLOATTYPE>(v1) + static_cast<FLOATTYPE>(v2) + static_cast<FLOATTYPE>(v3) + static_cast<FLOATTYPE>(v4) + static_cast<FLOATTYPE>(v5) + static_cast<FLOATTYPE>(v6) + static_cast<FLOATTYPE>(v7) + static_cast<FLOATTYPE>(v8) + static_cast<FLOATTYPE>(v9) + static_cast<FLOATTYPE>(v10) + static_cast<FLOATTYPE>(v11) + static_cast<FLOATTYPE>(v12) + static_cast<FLOATTYPE>(v13) + EXPRESSION_SUM_HFA19(hfa);
}


// ---------------------------------------------------
// average Methods
// ---------------------------------------------------

#ifdef NESTED_HFA

#define EXPRESSION_AVERAGE_HFA01(hfa)	(hfa.f1 / 1)
#define EXPRESSION_AVERAGE_HFA02(hfa)	((average_HFA01(hfa.hfa01) * 1 + hfa.f2) / 2)
#define EXPRESSION_AVERAGE_HFA03(hfa)	((average_HFA01(hfa.hfa01) * 1 + average_HFA02(hfa.hfa02) * 2) / 3)
#define EXPRESSION_AVERAGE_HFA05(hfa)	((average_HFA02(hfa.hfa02) * 2 + average_HFA03(hfa.hfa03) * 3) / 5)
#define EXPRESSION_AVERAGE_HFA08(hfa)	((average_HFA03(hfa.hfa03) * 3 + average_HFA05(hfa.hfa05) * 5) / 8)
#define EXPRESSION_AVERAGE_HFA11(hfa)	((average_HFA03(hfa.hfa03) * 3 + average_HFA08(hfa.hfa08) * 8) / 11)
#define EXPRESSION_AVERAGE_HFA19(hfa)	((average_HFA08(hfa.hfa08) * 8 + average_HFA11(hfa.hfa11) * 11) / 19)

#else

#define EXPRESSION_AVERAGE_HFA01(hfa)	(EXPRESSION_SUM_HFA01(hfa) / 1)
#define EXPRESSION_AVERAGE_HFA02(hfa)	(EXPRESSION_SUM_HFA02(hfa) / 2)
#define EXPRESSION_AVERAGE_HFA03(hfa)	(EXPRESSION_SUM_HFA03(hfa) / 3)
#define EXPRESSION_AVERAGE_HFA05(hfa)	(EXPRESSION_SUM_HFA05(hfa) / 5)
#define EXPRESSION_AVERAGE_HFA08(hfa)	(EXPRESSION_SUM_HFA08(hfa) / 8)
#define EXPRESSION_AVERAGE_HFA11(hfa)	(EXPRESSION_SUM_HFA11(hfa) / 11)
#define EXPRESSION_AVERAGE_HFA19(hfa)	(EXPRESSION_SUM_HFA19(hfa) / 19)

#endif

#define EXPRESSION_AVERAGE3_HFA01(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA01(hfa1) + EXPRESSION_AVERAGE_HFA01(hfa2) + EXPRESSION_AVERAGE_HFA01(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA02(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA02(hfa1) + EXPRESSION_AVERAGE_HFA02(hfa2) + EXPRESSION_AVERAGE_HFA02(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA03(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA03(hfa1) + EXPRESSION_AVERAGE_HFA03(hfa2) + EXPRESSION_AVERAGE_HFA03(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA05(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA05(hfa1) + EXPRESSION_AVERAGE_HFA05(hfa2) + EXPRESSION_AVERAGE_HFA05(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA08(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA08(hfa1) + EXPRESSION_AVERAGE_HFA08(hfa2) + EXPRESSION_AVERAGE_HFA08(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA11(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA11(hfa1) + EXPRESSION_AVERAGE_HFA11(hfa2) + EXPRESSION_AVERAGE_HFA11(hfa3)) / 3)
#define EXPRESSION_AVERAGE3_HFA19(hfa1, hfa2, hfa3)	((EXPRESSION_AVERAGE_HFA19(hfa1) + EXPRESSION_AVERAGE_HFA19(hfa2) + EXPRESSION_AVERAGE_HFA19(hfa3)) / 3)

#define EXPRESSION_AVERAGE5_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA01(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA01(hfa4) + EXPRESSION_AVERAGE_HFA01(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA02(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA02(hfa4) + EXPRESSION_AVERAGE_HFA02(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA03(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA03(hfa4) + EXPRESSION_AVERAGE_HFA03(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA05(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA05(hfa4) + EXPRESSION_AVERAGE_HFA05(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA08(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA08(hfa4) + EXPRESSION_AVERAGE_HFA08(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA11(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA11(hfa4) + EXPRESSION_AVERAGE_HFA11(hfa5)) / 5)
#define EXPRESSION_AVERAGE5_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5)	((EXPRESSION_AVERAGE3_HFA19(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE_HFA19(hfa4) + EXPRESSION_AVERAGE_HFA19(hfa5)) / 5)

#define EXPRESSION_AVERAGE8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA01(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA02(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA03(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA05(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA08(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA08(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA11(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)
#define EXPRESSION_AVERAGE8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8)	((EXPRESSION_AVERAGE3_HFA19(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE5_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8) * 5) / 8)

#define EXPRESSION_AVERAGE11_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA01(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA02(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA03(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA05(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA08(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA08(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA11(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)
#define EXPRESSION_AVERAGE11_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11)	((EXPRESSION_AVERAGE3_HFA19(hfa1, hfa2, hfa3) * 3 + EXPRESSION_AVERAGE8_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8) / 11)

#define EXPRESSION_AVERAGE19_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA01(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA02(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA03(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA05(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA08(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA11(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)
#define EXPRESSION_AVERAGE19_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19)	((EXPRESSION_AVERAGE8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8 + EXPRESSION_AVERAGE11_HFA19(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11) / 19)


HFADLL_API FLOATTYPE  average_HFA01(HFA01 hfa) {
	return EXPRESSION_AVERAGE_HFA01(hfa);
}

HFADLL_API FLOATTYPE  average_HFA02(HFA02 hfa) {
	return EXPRESSION_AVERAGE_HFA02(hfa);
}

HFADLL_API FLOATTYPE  average_HFA03(HFA03 hfa) {
	return EXPRESSION_AVERAGE_HFA03(hfa);
}

HFADLL_API FLOATTYPE  average_HFA05(HFA05 hfa) {
	return EXPRESSION_AVERAGE_HFA05(hfa);
}

HFADLL_API FLOATTYPE  average_HFA08(HFA08 hfa) {
	return EXPRESSION_AVERAGE_HFA08(hfa);
}

HFADLL_API FLOATTYPE  average_HFA11(HFA11 hfa) {
	return EXPRESSION_AVERAGE_HFA11(hfa);
}

HFADLL_API FLOATTYPE  average_HFA19(HFA19 hfa) {
	return EXPRESSION_AVERAGE_HFA19(hfa);
}


HFADLL_API FLOATTYPE  average3_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3) {
	return EXPRESSION_AVERAGE3_HFA01(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3) {
	return EXPRESSION_AVERAGE3_HFA02(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3) {
	return EXPRESSION_AVERAGE3_HFA03(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3) {
	return EXPRESSION_AVERAGE3_HFA05(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3) {
	return EXPRESSION_AVERAGE3_HFA08(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3) {
	return EXPRESSION_AVERAGE3_HFA11(hfa1, hfa2, hfa3);
}

HFADLL_API FLOATTYPE  average3_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3) {
	return EXPRESSION_AVERAGE3_HFA19(hfa1, hfa2, hfa3);
}


HFADLL_API FLOATTYPE  average5_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5) {
	return EXPRESSION_AVERAGE5_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5) {
	return EXPRESSION_AVERAGE5_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5) {
	return EXPRESSION_AVERAGE5_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5) {
	return EXPRESSION_AVERAGE5_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5) {
	return EXPRESSION_AVERAGE5_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5) {
	return EXPRESSION_AVERAGE5_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5);
}

HFADLL_API FLOATTYPE  average5_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5) {
	return EXPRESSION_AVERAGE5_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5);
}


HFADLL_API FLOATTYPE  average8_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8) {
	return EXPRESSION_AVERAGE8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8) {
	return EXPRESSION_AVERAGE8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8) {
	return EXPRESSION_AVERAGE8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8) {
	return EXPRESSION_AVERAGE8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8) {
	return EXPRESSION_AVERAGE8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8) {
	return EXPRESSION_AVERAGE8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}

HFADLL_API FLOATTYPE  average8_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8) {
	return EXPRESSION_AVERAGE8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8);
}


HFADLL_API FLOATTYPE  average11_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11) {
	return EXPRESSION_AVERAGE11_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11) {
	return EXPRESSION_AVERAGE11_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11) {
	return EXPRESSION_AVERAGE11_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11) {
	return EXPRESSION_AVERAGE11_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11) {
	return EXPRESSION_AVERAGE11_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11) {
	return EXPRESSION_AVERAGE11_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}

HFADLL_API FLOATTYPE  average11_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11) {
	return EXPRESSION_AVERAGE11_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11);
}


HFADLL_API FLOATTYPE  average19_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11, HFA01 hfa12, HFA01 hfa13, HFA01 hfa14, HFA01 hfa15, HFA01 hfa16, HFA01 hfa17, HFA01 hfa18, HFA01 hfa19) {
	return EXPRESSION_AVERAGE19_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11, HFA02 hfa12, HFA02 hfa13, HFA02 hfa14, HFA02 hfa15, HFA02 hfa16, HFA02 hfa17, HFA02 hfa18, HFA02 hfa19) {
	return EXPRESSION_AVERAGE19_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11, HFA03 hfa12, HFA03 hfa13, HFA03 hfa14, HFA03 hfa15, HFA03 hfa16, HFA03 hfa17, HFA03 hfa18, HFA03 hfa19) {
	return EXPRESSION_AVERAGE19_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11, HFA05 hfa12, HFA05 hfa13, HFA05 hfa14, HFA05 hfa15, HFA05 hfa16, HFA05 hfa17, HFA05 hfa18, HFA05 hfa19) {
	return EXPRESSION_AVERAGE19_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11, HFA08 hfa12, HFA08 hfa13, HFA08 hfa14, HFA08 hfa15, HFA08 hfa16, HFA08 hfa17, HFA08 hfa18, HFA08 hfa19) {
	return EXPRESSION_AVERAGE19_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11, HFA11 hfa12, HFA11 hfa13, HFA11 hfa14, HFA11 hfa15, HFA11 hfa16, HFA11 hfa17, HFA11 hfa18, HFA11 hfa19) {
	return EXPRESSION_AVERAGE19_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}

HFADLL_API FLOATTYPE  average19_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11, HFA19 hfa12, HFA19 hfa13, HFA19 hfa14, HFA19 hfa15, HFA19 hfa16, HFA19 hfa17, HFA19 hfa18, HFA19 hfa19) {
	return EXPRESSION_AVERAGE19_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19);
}



// ---------------------------------------------------
// Add Methods
// ---------------------------------------------------

HFADLL_API FLOATTYPE  add01_HFA01(HFA01 hfa1, float v1, HFA01 hfa2, int v2, HFA01 hfa3, short v3, double v4, HFA01 hfa4, HFA01 hfa5, float v5, __int64 v6, float v7, HFA01 hfa6, float v8, HFA01 hfa7) {
	return (sum_HFA01(hfa1) + sum_HFA01(hfa2) + sum_HFA01(hfa3) + sum_HFA01(hfa4) + sum_HFA01(hfa5) + sum_HFA01(hfa6) + sum_HFA01(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA02(HFA02 hfa1, float v1, HFA02 hfa2, int v2, HFA02 hfa3, short v3, double v4, HFA02 hfa4, HFA02 hfa5, float v5, __int64 v6, float v7, HFA02 hfa6, float v8, HFA02 hfa7) {
	return (sum_HFA02(hfa1) + sum_HFA02(hfa2) + sum_HFA02(hfa3) + sum_HFA02(hfa4) + sum_HFA02(hfa5) + sum_HFA02(hfa6) + sum_HFA02(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA03(HFA03 hfa1, float v1, HFA03 hfa2, int v2, HFA03 hfa3, short v3, double v4, HFA03 hfa4, HFA03 hfa5, float v5, __int64 v6, float v7, HFA03 hfa6, float v8, HFA03 hfa7) {
	return (sum_HFA03(hfa1) + sum_HFA03(hfa2) + sum_HFA03(hfa3) + sum_HFA03(hfa4) + sum_HFA03(hfa5) + sum_HFA03(hfa6) + sum_HFA03(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA05(HFA05 hfa1, float v1, HFA05 hfa2, int v2, HFA05 hfa3, short v3, double v4, HFA05 hfa4, HFA05 hfa5, float v5, __int64 v6, float v7, HFA05 hfa6, float v8, HFA05 hfa7) {
	return (sum_HFA05(hfa1) + sum_HFA05(hfa2) + sum_HFA05(hfa3) + sum_HFA05(hfa4) + sum_HFA05(hfa5) + sum_HFA05(hfa6) + sum_HFA05(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA08(HFA08 hfa1, float v1, HFA08 hfa2, int v2, HFA08 hfa3, short v3, double v4, HFA08 hfa4, HFA08 hfa5, float v5, __int64 v6, float v7, HFA08 hfa6, float v8, HFA08 hfa7) {
	return (sum_HFA08(hfa1) + sum_HFA08(hfa2) + sum_HFA08(hfa3) + sum_HFA08(hfa4) + sum_HFA08(hfa5) + sum_HFA08(hfa6) + sum_HFA08(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA11(HFA11 hfa1, float v1, HFA11 hfa2, int v2, HFA11 hfa3, short v3, double v4, HFA11 hfa4, HFA11 hfa5, float v5, __int64 v6, float v7, HFA11 hfa6, float v8, HFA11 hfa7) {
	return (sum_HFA11(hfa1) + sum_HFA11(hfa2) + sum_HFA11(hfa3) + sum_HFA11(hfa4) + sum_HFA11(hfa5) + sum_HFA11(hfa6) + sum_HFA11(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA19(HFA19 hfa1, float v1, HFA19 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA19 hfa4, HFA19 hfa5, float v5, __int64 v6, float v7, HFA19 hfa6, float v8, HFA19 hfa7) {
	return (sum_HFA19(hfa1) + sum_HFA19(hfa2) + sum_HFA19(hfa3) + sum_HFA19(hfa4) + sum_HFA19(hfa5) + sum_HFA19(hfa6) + sum_HFA19(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}

HFADLL_API FLOATTYPE  add01_HFA00(HFA03 hfa1, float v1, HFA02 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA05 hfa4, HFA08 hfa5, float v5, __int64 v6, float v7, HFA11 hfa6, float v8, HFA01 hfa7) {
	return (sum_HFA03(hfa1) + sum_HFA02(hfa2) + sum_HFA19(hfa3) + sum_HFA05(hfa4) + sum_HFA08(hfa5) + sum_HFA11(hfa6) + sum_HFA01(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8;
}



HFADLL_API FLOATTYPE  add02_HFA01(HFA01 hfa1, HFA01 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA01 hfa3, double v7, float v8, HFA01 hfa4, short v9, HFA01 hfa5, float v10, HFA01 hfa6, HFA01 hfa7) {
	return (sum_HFA01(hfa1) + sum_HFA01(hfa2) + sum_HFA01(hfa3) + sum_HFA01(hfa4) + sum_HFA01(hfa5) + sum_HFA01(hfa6) + sum_HFA01(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA02(HFA02 hfa1, HFA02 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA02 hfa3, double v7, float v8, HFA02 hfa4, short v9, HFA02 hfa5, float v10, HFA02 hfa6, HFA02 hfa7) {
	return (sum_HFA02(hfa1) + sum_HFA02(hfa2) + sum_HFA02(hfa3) + sum_HFA02(hfa4) + sum_HFA02(hfa5) + sum_HFA02(hfa6) + sum_HFA02(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA03(HFA03 hfa1, HFA03 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA03 hfa4, short v9, HFA03 hfa5, float v10, HFA03 hfa6, HFA03 hfa7) {
	return (sum_HFA03(hfa1) + sum_HFA03(hfa2) + sum_HFA03(hfa3) + sum_HFA03(hfa4) + sum_HFA03(hfa5) + sum_HFA03(hfa6) + sum_HFA03(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA05(HFA05 hfa1, HFA05 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA05 hfa3, double v7, float v8, HFA05 hfa4, short v9, HFA05 hfa5, float v10, HFA05 hfa6, HFA05 hfa7) {
	return (sum_HFA05(hfa1) + sum_HFA05(hfa2) + sum_HFA05(hfa3) + sum_HFA05(hfa4) + sum_HFA05(hfa5) + sum_HFA05(hfa6) + sum_HFA05(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA08(HFA08 hfa1, HFA08 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA08 hfa3, double v7, float v8, HFA08 hfa4, short v9, HFA08 hfa5, float v10, HFA08 hfa6, HFA08 hfa7) {
	return (sum_HFA08(hfa1) + sum_HFA08(hfa2) + sum_HFA08(hfa3) + sum_HFA08(hfa4) + sum_HFA08(hfa5) + sum_HFA08(hfa6) + sum_HFA08(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA11(HFA11 hfa1, HFA11 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA11 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA11 hfa5, float v10, HFA11 hfa6, HFA11 hfa7) {
	return (sum_HFA11(hfa1) + sum_HFA11(hfa2) + sum_HFA11(hfa3) + sum_HFA11(hfa4) + sum_HFA11(hfa5) + sum_HFA11(hfa6) + sum_HFA11(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA19(HFA19 hfa1, HFA19 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA19 hfa3, double v7, float v8, HFA19 hfa4, short v9, HFA19 hfa5, float v10, HFA19 hfa6, HFA19 hfa7) {
	return (sum_HFA19(hfa1) + sum_HFA19(hfa2) + sum_HFA19(hfa3) + sum_HFA19(hfa4) + sum_HFA19(hfa5) + sum_HFA19(hfa6) + sum_HFA19(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add02_HFA00(HFA01 hfa1, HFA05 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA19 hfa5, float v10, HFA08 hfa6, HFA02 hfa7) {
	return (sum_HFA01(hfa1) + sum_HFA05(hfa2) + sum_HFA03(hfa3) + sum_HFA11(hfa4) + sum_HFA19(hfa5) + sum_HFA08(hfa6) + sum_HFA02(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}



HFADLL_API FLOATTYPE  add03_HFA01(float v1, signed char v2, HFA01 hfa1, double v3, signed char v4, HFA01 hfa2, __int64 v5, short v6, int v7, HFA01 hfa3, HFA01 hfa4, float v8, HFA01 hfa5, float v9, HFA01 hfa6, float v10, HFA01 hfa7) {
	return (sum_HFA01(hfa1) + sum_HFA01(hfa2) + sum_HFA01(hfa3) + sum_HFA01(hfa4) + sum_HFA01(hfa5) + sum_HFA01(hfa6) + sum_HFA01(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA02(float v1, signed char v2, HFA02 hfa1, double v3, signed char v4, HFA02 hfa2, __int64 v5, short v6, int v7, HFA02 hfa3, HFA02 hfa4, float v8, HFA02 hfa5, float v9, HFA02 hfa6, float v10, HFA02 hfa7) {
	return (sum_HFA02(hfa1) + sum_HFA02(hfa2) + sum_HFA02(hfa3) + sum_HFA02(hfa4) + sum_HFA02(hfa5) + sum_HFA02(hfa6) + sum_HFA02(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA03(float v1, signed char v2, HFA03 hfa1, double v3, signed char v4, HFA03 hfa2, __int64 v5, short v6, int v7, HFA03 hfa3, HFA03 hfa4, float v8, HFA03 hfa5, float v9, HFA03 hfa6, float v10, HFA03 hfa7) {
	return (sum_HFA03(hfa1) + sum_HFA03(hfa2) + sum_HFA03(hfa3) + sum_HFA03(hfa4) + sum_HFA03(hfa5) + sum_HFA03(hfa6) + sum_HFA03(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA05(float v1, signed char v2, HFA05 hfa1, double v3, signed char v4, HFA05 hfa2, __int64 v5, short v6, int v7, HFA05 hfa3, HFA05 hfa4, float v8, HFA05 hfa5, float v9, HFA05 hfa6, float v10, HFA05 hfa7) {
	return (sum_HFA05(hfa1) + sum_HFA05(hfa2) + sum_HFA05(hfa3) + sum_HFA05(hfa4) + sum_HFA05(hfa5) + sum_HFA05(hfa6) + sum_HFA05(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA08(float v1, signed char v2, HFA08 hfa1, double v3, signed char v4, HFA08 hfa2, __int64 v5, short v6, int v7, HFA08 hfa3, HFA08 hfa4, float v8, HFA08 hfa5, float v9, HFA08 hfa6, float v10, HFA08 hfa7) {
	return (sum_HFA08(hfa1) + sum_HFA08(hfa2) + sum_HFA08(hfa3) + sum_HFA08(hfa4) + sum_HFA08(hfa5) + sum_HFA08(hfa6) + sum_HFA08(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA11(float v1, signed char v2, HFA11 hfa1, double v3, signed char v4, HFA11 hfa2, __int64 v5, short v6, int v7, HFA11 hfa3, HFA11 hfa4, float v8, HFA11 hfa5, float v9, HFA11 hfa6, float v10, HFA11 hfa7) {
	return (sum_HFA11(hfa1) + sum_HFA11(hfa2) + sum_HFA11(hfa3) + sum_HFA11(hfa4) + sum_HFA11(hfa5) + sum_HFA11(hfa6) + sum_HFA11(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA19(float v1, signed char v2, HFA19 hfa1, double v3, signed char v4, HFA19 hfa2, __int64 v5, short v6, int v7, HFA19 hfa3, HFA19 hfa4, float v8, HFA19 hfa5, float v9, HFA19 hfa6, float v10, HFA19 hfa7) {
	return (sum_HFA19(hfa1) + sum_HFA19(hfa2) + sum_HFA19(hfa3) + sum_HFA19(hfa4) + sum_HFA19(hfa5) + sum_HFA19(hfa6) + sum_HFA19(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}

HFADLL_API FLOATTYPE  add03_HFA00(float v1, signed char v2, HFA08 hfa1, double v3, signed char v4, HFA19 hfa2, __int64 v5, short v6, int v7, HFA03 hfa3, HFA01 hfa4, float v8, HFA11 hfa5, float v9, HFA02 hfa6, float v10, HFA05 hfa7) {
	return (sum_HFA08(hfa1) + sum_HFA19(hfa2) + sum_HFA03(hfa3) + sum_HFA01(hfa4) + sum_HFA11(hfa5) + sum_HFA02(hfa6) + sum_HFA05(hfa7)) +  (FLOATTYPE)v1 + (FLOATTYPE)v2 + (FLOATTYPE)v3 + (FLOATTYPE)v4 + (FLOATTYPE)v5 + (FLOATTYPE)v6 + (FLOATTYPE)v7 + (FLOATTYPE)v8 + (FLOATTYPE)v9 + (FLOATTYPE)v10;
}
