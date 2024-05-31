// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUFEATURES_H
#define HAVE_MINIPAL_CPUFEATURES_H
#define NUM_PARTS 3
//
// Should match the constants defined in the compiler in HardwareIntrinsicHelpers.cs
//

typedef struct {
    uint32_t parts[NUM_PARTS];
} HardwareIntrinsicConstants;

inline bool areEqualHardwareIntrinsicConstants(HardwareIntrinsicConstants *constants1, HardwareIntrinsicConstants *constants2) {
    for (int i = 0; i < NUM_PARTS; i++) {
        if (constants1->parts[i] != constants2->parts[i]) {
            return false;
        }
    }
    return true;
}

inline void setFlag(HardwareIntrinsicConstants *constants, int flag) {
    constants->parts[flag / 32] |= (1 << (flag % 32));
}

inline void clearFlag(HardwareIntrinsicConstants *constants, int flag) {
    constants->parts[flag / 32] &= ~(1 << (flag % 32));
}

inline bool isFlagSet(const HardwareIntrinsicConstants *constants, int flag) {
    return (constants->parts[flag / 32] & (1 << (flag % 32))) != 0;
}

#if defined(HOST_X86) || defined(HOST_AMD64)
enum XArchIntrinsicFeatures
{
    XArchIntrinsicConstants_Aes = 0,
    XArchIntrinsicConstants_Pclmulqdq = 1,
    XArchIntrinsicConstants_Sse3 = 2,
    XArchIntrinsicConstants_Ssse3 = 3,
    XArchIntrinsicConstants_Sse41 = 4,
    XArchIntrinsicConstants_Sse42 = 5,
    XArchIntrinsicConstants_Popcnt = 6,
    XArchIntrinsicConstants_Avx = 7,
    XArchIntrinsicConstants_Fma = 8,
    XArchIntrinsicConstants_Avx2 = 9,
    XArchIntrinsicConstants_Bmi1 = 10,
    XArchIntrinsicConstants_Bmi2 = 11,
    XArchIntrinsicConstants_Lzcnt = 12,
    XArchIntrinsicConstants_AvxVnni = 13,
    XArchIntrinsicConstants_Movbe = 14,
    XArchIntrinsicConstants_Avx512f = 15,
    XArchIntrinsicConstants_Avx512f_vl = 16,
    XArchIntrinsicConstants_Avx512bw = 17,
    XArchIntrinsicConstants_Avx512bw_vl = 18,
    XArchIntrinsicConstants_Avx512cd = 19,
    XArchIntrinsicConstants_Avx512cd_vl = 20,
    XArchIntrinsicConstants_Avx512dq = 21,
    XArchIntrinsicConstants_Avx512dq_vl = 22,
    XArchIntrinsicConstants_Avx512Vbmi = 23,
    XArchIntrinsicConstants_Avx512Vbmi_vl = 24,
    XArchIntrinsicConstants_Serialize = 25,
    XArchIntrinsicConstants_VectorT128 = 26,
    XArchIntrinsicConstants_VectorT256 = 27,
    XArchIntrinsicConstants_VectorT512 = 28,
    XArchIntrinsicConstants_Avx10v1 = 29,
    XArchIntrinsicConstants_Avx10v1_V256 = 30,
    XArchIntrinsicConstants_Avx10v1_V512 = 31,
};
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
enum ARM64IntrinsicConstants
{
    ARM64IntrinsicConstants_AdvSimd = 0,
    ARM64IntrinsicConstants_Aes = 1,
    ARM64IntrinsicConstants_Crc32 = 2,
    ARM64IntrinsicConstants_Dp = 3,
    ARM64IntrinsicConstants_Rdm = 4,
    ARM64IntrinsicConstants_Sha1 = 5,
    ARM64IntrinsicConstants_Sha256 = 6,
    ARM64IntrinsicConstants_Atomics = 7,
    ARM64IntrinsicConstants_Rcpc = 8,
    ARM64IntrinsicConstants_VectorT128 = 9,
    ARM64IntrinsicConstants_Rcpc2 = 10,
    ARM64IntrinsicConstants_Sve = 11,
};

#include <assert.h>

// Bit position for the ARM64IntrinsicConstants_Atomics flags, to be used with tbz / tbnz instructions
#define ARM64_ATOMICS_FEATURE_FLAG_VALUE 7
static_assert(ARM64_ATOMICS_FEATURE_FLAG_VALUE == ARM64IntrinsicConstants_Atomics, "ARM64_ATOMICS_FEATURE_FLAG_BIT must match with ARM64IntrinsicConstants_Atomics");

#endif // HOST_ARM64

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

HardwareIntrinsicConstants minipal_getcpufeatures(void);
bool minipal_detect_rosetta(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif
