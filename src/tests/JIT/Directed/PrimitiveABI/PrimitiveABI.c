// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <stddef.h>
#include <stdio.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif // _MSC_VER

DLLEXPORT int64_t Echo_ExtendedUint_RiscV(int a0, uint32_t a1)
{
	return (int32_t)a1;
}

DLLEXPORT int64_t Echo_ExtendedUint_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, uint32_t stack0)
{
	return (int32_t)stack0;
}

DLLEXPORT double Echo_Float_RiscV(float fa0, float fa1)
{
	return fa1 + fa0;
}

DLLEXPORT double Echo_Float_InIntegerReg_RiscV(
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	float a0)
{
	return a0 + fa7;
}

DLLEXPORT double Echo_Float_OnStack_RiscV(
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, float stack0)
{
	return stack0 + fa7;
}
