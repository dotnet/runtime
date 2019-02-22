// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !defined(_MSC_VER)
#if __i386__
#define __stdcall __attribute__((stdcall))
#else // __i386__
#define __stdcall
#endif  // !__i386__

#ifdef BIT64
#define __int64     long
#else // BIT64
#define __int64     long long
#endif // BIT64

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#endif // !defined(_MSC_VER)

#if defined(_MSC_VER)
#define HFADLL_API extern "C" __declspec(dllexport)
#define HFADLL_CALLCONV
#else
#define HFADLL_API extern "C" __attribute__((visibility("default")))
#define HFADLL_CALLCONV __stdcall
#endif 


#ifndef FLOATTYPE
#ifdef FLOAT64
#define FLOATTYPE double
#else
#define FLOATTYPE float
#endif
#endif


struct HFA01 {
public:
	FLOATTYPE f1;
};

struct HFA02 {
public:
#ifdef NESTED_HFA
	HFA01 hfa01;
	FLOATTYPE f2;
#else
	FLOATTYPE f1, f2;
#endif
};

struct HFA03 {
public:
#ifdef NESTED_HFA
	HFA01 hfa01;
	HFA02 hfa02;
#else
	FLOATTYPE f1, f2, f3;
#endif
};

struct HFA05 {
public:
#ifdef NESTED_HFA
	HFA02 hfa02;
	HFA03 hfa03;
#else
	FLOATTYPE f1, f2, f3, f4, f5;
#endif
};

struct HFA08 {
public:
#ifdef NESTED_HFA
	HFA03 hfa03;
	HFA05 hfa05;
#else
	FLOATTYPE f1, f2, f3, f4, f5, f6, f7, f8;
#endif
};

struct HFA11 {
public:
#ifdef NESTED_HFA
	HFA03 hfa03;
	HFA08 hfa08;
#else
	FLOATTYPE f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11;
#endif
};

struct HFA19 {
public:
#ifdef NESTED_HFA
	HFA08 hfa08;
	HFA11 hfa11;
#else
	FLOATTYPE f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, f16, f17, f18, f19;
#endif
};


#ifdef NESTED_HFA
const FLOATTYPE EXPECTED_SUM_HFA01 = static_cast<FLOATTYPE>(1);
const FLOATTYPE EXPECTED_SUM_HFA02 = static_cast<FLOATTYPE>(3);
const FLOATTYPE EXPECTED_SUM_HFA03 = static_cast<FLOATTYPE>(4);
const FLOATTYPE EXPECTED_SUM_HFA05 = static_cast<FLOATTYPE>(7);
const FLOATTYPE EXPECTED_SUM_HFA08 = static_cast<FLOATTYPE>(11);
const FLOATTYPE EXPECTED_SUM_HFA11 = static_cast<FLOATTYPE>(15);
const FLOATTYPE EXPECTED_SUM_HFA19 = static_cast<FLOATTYPE>(26);
#else
const FLOATTYPE EXPECTED_SUM_HFA01 = static_cast<FLOATTYPE>(1);
const FLOATTYPE EXPECTED_SUM_HFA02 = static_cast<FLOATTYPE>(3);
const FLOATTYPE EXPECTED_SUM_HFA03 = static_cast<FLOATTYPE>(6);
const FLOATTYPE EXPECTED_SUM_HFA05 = static_cast<FLOATTYPE>(15);
const FLOATTYPE EXPECTED_SUM_HFA08 = static_cast<FLOATTYPE>(36);
const FLOATTYPE EXPECTED_SUM_HFA11 = static_cast<FLOATTYPE>(66);
const FLOATTYPE EXPECTED_SUM_HFA19 = static_cast<FLOATTYPE>(190);
#endif

HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA01() {return EXPECTED_SUM_HFA01;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA02() {return EXPECTED_SUM_HFA02;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA03() {return EXPECTED_SUM_HFA03;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA05() {return EXPECTED_SUM_HFA05;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA08() {return EXPECTED_SUM_HFA08;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA11() {return EXPECTED_SUM_HFA11;}
HFADLL_API FLOATTYPE HFADLL_CALLCONV get_EXPECTED_SUM_HFA19() {return EXPECTED_SUM_HFA19;}



// ---------------------------------------------------
// init Methods
// ---------------------------------------------------

HFADLL_API void HFADLL_CALLCONV init_HFA01(HFA01& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA02(HFA02& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA03(HFA03& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA05(HFA05& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA08(HFA08& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA11(HFA11& hfa);
HFADLL_API void HFADLL_CALLCONV init_HFA19(HFA19& hfa);



// --------------------------------------------------------------
// identity methods
// --------------------------------------------------------------

HFADLL_API HFA01 HFADLL_CALLCONV identity_HFA01(HFA01 hfa);
HFADLL_API HFA02 HFADLL_CALLCONV identity_HFA02(HFA02 hfa);
HFADLL_API HFA03 HFADLL_CALLCONV identity_HFA03(HFA03 hfa);
HFADLL_API HFA05 HFADLL_CALLCONV identity_HFA05(HFA05 hfa);
HFADLL_API HFA08 HFADLL_CALLCONV identity_HFA08(HFA08 hfa);
HFADLL_API HFA11 HFADLL_CALLCONV identity_HFA11(HFA11 hfa);
HFADLL_API HFA19 HFADLL_CALLCONV identity_HFA19(HFA19 hfa);



// --------------------------------------------------------------
// get methods
// --------------------------------------------------------------

HFADLL_API HFA01 HFADLL_CALLCONV get_HFA01();
HFADLL_API HFA02 HFADLL_CALLCONV get_HFA02();
HFADLL_API HFA03 HFADLL_CALLCONV get_HFA03();
HFADLL_API HFA05 HFADLL_CALLCONV get_HFA05();
HFADLL_API HFA08 HFADLL_CALLCONV get_HFA08();
HFADLL_API HFA11 HFADLL_CALLCONV get_HFA11();
HFADLL_API HFA19 HFADLL_CALLCONV get_HFA19();



// ---------------------------------------------------
// sum Methods
// ---------------------------------------------------

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA01(HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA02(HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA03(HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA05(HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA08(HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA11(HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum_HFA19(HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA01(float v1, __int64 v2, HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA02(float v1, __int64 v2, HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA03(float v1, __int64 v2, HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA05(float v1, __int64 v2, HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA08(float v1, __int64 v2, HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA11(float v1, __int64 v2, HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum3_HFA19(float v1, __int64 v2, HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA01(__int64 v1, double v2, short v3, signed char v4, HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA02(__int64 v1, double v2, short v3, signed char v4, HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA03(__int64 v1, double v2, short v3, signed char v4, HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA05(__int64 v1, double v2, short v3, signed char v4, HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA08(__int64 v1, double v2, short v3, signed char v4, HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA11(__int64 v1, double v2, short v3, signed char v4, HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum5_HFA19(__int64 v1, double v2, short v3, signed char v4, HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA01(float v1, double v2, __int64 v3, signed char v4, double v5, HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA02(float v1, double v2, __int64 v3, signed char v4, double v5, HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA03(float v1, double v2, __int64 v3, signed char v4, double v5, HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA05(float v1, double v2, __int64 v3, signed char v4, double v5, HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA08(float v1, double v2, __int64 v3, signed char v4, double v5, HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA11(float v1, double v2, __int64 v3, signed char v4, double v5, HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum8_HFA19(float v1, double v2, __int64 v3, signed char v4, double v5, HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA01(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA02(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA03(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA05(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA08(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA11(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum11_HFA19(double v1, float v2, float v3, int v4, float v5, __int64 v6, double v7, float v8, HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA01(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA02(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA03(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA05(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA08(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA11(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV sum19_HFA19(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA19 hfa);



// ---------------------------------------------------
// sverage Methods
// ---------------------------------------------------

HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA01(HFA01 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA02(HFA02 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA03(HFA03 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA05(HFA05 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA08(HFA08 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA11(HFA11 hfa);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average_HFA19(HFA19 hfa);

HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average3_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3);

HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average5_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5);

HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average8_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8);

HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average11_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11);

HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11, HFA01 hfa12, HFA01 hfa13, HFA01 hfa14, HFA01 hfa15, HFA01 hfa16, HFA01 hfa17, HFA01 hfa18, HFA01 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11, HFA02 hfa12, HFA02 hfa13, HFA02 hfa14, HFA02 hfa15, HFA02 hfa16, HFA02 hfa17, HFA02 hfa18, HFA02 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11, HFA03 hfa12, HFA03 hfa13, HFA03 hfa14, HFA03 hfa15, HFA03 hfa16, HFA03 hfa17, HFA03 hfa18, HFA03 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11, HFA05 hfa12, HFA05 hfa13, HFA05 hfa14, HFA05 hfa15, HFA05 hfa16, HFA05 hfa17, HFA05 hfa18, HFA05 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11, HFA08 hfa12, HFA08 hfa13, HFA08 hfa14, HFA08 hfa15, HFA08 hfa16, HFA08 hfa17, HFA08 hfa18, HFA08 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11, HFA11 hfa12, HFA11 hfa13, HFA11 hfa14, HFA11 hfa15, HFA11 hfa16, HFA11 hfa17, HFA11 hfa18, HFA11 hfa19);
HFADLL_API FLOATTYPE HFADLL_CALLCONV average19_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11, HFA19 hfa12, HFA19 hfa13, HFA19 hfa14, HFA19 hfa15, HFA19 hfa16, HFA19 hfa17, HFA19 hfa18, HFA19 hfa19);




// ---------------------------------------------------
// add Methods
// ---------------------------------------------------

HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA01(HFA01 hfa1, float v1, HFA01 hfa2, int v2, HFA01 hfa3, short v3, double v4, HFA01 hfa4, HFA01 hfa5, float v5, __int64 v6, float v7, HFA01 hfa6, float v8, HFA01 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA02(HFA02 hfa1, float v1, HFA02 hfa2, int v2, HFA02 hfa3, short v3, double v4, HFA02 hfa4, HFA02 hfa5, float v5, __int64 v6, float v7, HFA02 hfa6, float v8, HFA02 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA03(HFA03 hfa1, float v1, HFA03 hfa2, int v2, HFA03 hfa3, short v3, double v4, HFA03 hfa4, HFA03 hfa5, float v5, __int64 v6, float v7, HFA03 hfa6, float v8, HFA03 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA05(HFA05 hfa1, float v1, HFA05 hfa2, int v2, HFA05 hfa3, short v3, double v4, HFA05 hfa4, HFA05 hfa5, float v5, __int64 v6, float v7, HFA05 hfa6, float v8, HFA05 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA08(HFA08 hfa1, float v1, HFA08 hfa2, int v2, HFA08 hfa3, short v3, double v4, HFA08 hfa4, HFA08 hfa5, float v5, __int64 v6, float v7, HFA08 hfa6, float v8, HFA08 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA11(HFA11 hfa1, float v1, HFA11 hfa2, int v2, HFA11 hfa3, short v3, double v4, HFA11 hfa4, HFA11 hfa5, float v5, __int64 v6, float v7, HFA11 hfa6, float v8, HFA11 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA19(HFA19 hfa1, float v1, HFA19 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA19 hfa4, HFA19 hfa5, float v5, __int64 v6, float v7, HFA19 hfa6, float v8, HFA19 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add01_HFA00(HFA03 hfa1, float v1, HFA02 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA05 hfa4, HFA08 hfa5, float v5, __int64 v6, float v7, HFA11 hfa6, float v8, HFA01 hfa7);

HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA01(HFA01 hfa1, HFA01 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA01 hfa3, double v7, float v8, HFA01 hfa4, short v9, HFA01 hfa5, float v10, HFA01 hfa6, HFA01 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA02(HFA02 hfa1, HFA02 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA02 hfa3, double v7, float v8, HFA02 hfa4, short v9, HFA02 hfa5, float v10, HFA02 hfa6, HFA02 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA03(HFA03 hfa1, HFA03 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA03 hfa4, short v9, HFA03 hfa5, float v10, HFA03 hfa6, HFA03 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA05(HFA05 hfa1, HFA05 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA05 hfa3, double v7, float v8, HFA05 hfa4, short v9, HFA05 hfa5, float v10, HFA05 hfa6, HFA05 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA08(HFA08 hfa1, HFA08 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA08 hfa3, double v7, float v8, HFA08 hfa4, short v9, HFA08 hfa5, float v10, HFA08 hfa6, HFA08 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA11(HFA11 hfa1, HFA11 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA11 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA11 hfa5, float v10, HFA11 hfa6, HFA11 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA19(HFA19 hfa1, HFA19 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA19 hfa3, double v7, float v8, HFA19 hfa4, short v9, HFA19 hfa5, float v10, HFA19 hfa6, HFA19 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add02_HFA00(HFA01 hfa1, HFA05 hfa2, __int64 v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA19 hfa5, float v10, HFA08 hfa6, HFA02 hfa7);

HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA01(float v1, signed char v2, HFA01 hfa1, double v3, signed char v4, HFA01 hfa2, __int64 v5, short v6, int v7, HFA01 hfa3, HFA01 hfa4, float v8, HFA01 hfa5, float v9, HFA01 hfa6, float v10, HFA01 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA02(float v1, signed char v2, HFA02 hfa1, double v3, signed char v4, HFA02 hfa2, __int64 v5, short v6, int v7, HFA02 hfa3, HFA02 hfa4, float v8, HFA02 hfa5, float v9, HFA02 hfa6, float v10, HFA02 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA03(float v1, signed char v2, HFA03 hfa1, double v3, signed char v4, HFA03 hfa2, __int64 v5, short v6, int v7, HFA03 hfa3, HFA03 hfa4, float v8, HFA03 hfa5, float v9, HFA03 hfa6, float v10, HFA03 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA05(float v1, signed char v2, HFA05 hfa1, double v3, signed char v4, HFA05 hfa2, __int64 v5, short v6, int v7, HFA05 hfa3, HFA05 hfa4, float v8, HFA05 hfa5, float v9, HFA05 hfa6, float v10, HFA05 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA08(float v1, signed char v2, HFA08 hfa1, double v3, signed char v4, HFA08 hfa2, __int64 v5, short v6, int v7, HFA08 hfa3, HFA08 hfa4, float v8, HFA08 hfa5, float v9, HFA08 hfa6, float v10, HFA08 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA11(float v1, signed char v2, HFA11 hfa1, double v3, signed char v4, HFA11 hfa2, __int64 v5, short v6, int v7, HFA11 hfa3, HFA11 hfa4, float v8, HFA11 hfa5, float v9, HFA11 hfa6, float v10, HFA11 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA19(float v1, signed char v2, HFA19 hfa1, double v3, signed char v4, HFA19 hfa2, __int64 v5, short v6, int v7, HFA19 hfa3, HFA19 hfa4, float v8, HFA19 hfa5, float v9, HFA19 hfa6, float v10, HFA19 hfa7);
HFADLL_API FLOATTYPE HFADLL_CALLCONV add03_HFA00(float v1, signed char v2, HFA08 hfa1, double v3, signed char v4, HFA19 hfa2, __int64 v5, short v6, int v7, HFA03 hfa3, HFA01 hfa4, float v8, HFA11 hfa5, float v9, HFA02 hfa6, float v10, HFA05 hfa7);
