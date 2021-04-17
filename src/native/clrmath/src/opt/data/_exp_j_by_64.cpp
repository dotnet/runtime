/*
 * Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software without
 *    specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 */

#include "clrmath.h"

/*
 * expm1f data
 * Generated for:
 *      (j/64.0)
 *     2      ; for j = 0, 1, 2, ... 63
 */
extern "C" const uint64_t __two_to_jby64[] = {
    0x3ff0000000000000,
    0x3fefec9a3e778061,
    0x3fefd9b0d3158574,
    0x3fefc74518759bc8,
    0x3fefb5586cf9890f,
    0x3fefa3ec32d3d1a2,
    0x3fef9301d0125b51,
    0x3fef829aaea92de0,
    0x3fef72b83c7d517b,
    0x3fef635beb6fcb75,
    0x3fef54873168b9aa,
    0x3fef463b88628cd6,
    0x3fef387a6e756238,
    0x3fef2b4565e27cdd,
    0x3fef1e9df51fdee1,
    0x3fef1285a6e4030b,
    0x3fef06fe0a31b715,
    0x3feefc08b26416ff,
    0x3feef1a7373aa9cb,
    0x3feee7db34e59ff7,
    0x3feedea64c123422,
    0x3feed60a21f72e2a,
    0x3feece086061892d,
    0x3feec6a2b5c13cd0,
    0x3feebfdad5362a27,
    0x3feeb9b2769d2ca7,
    0x3feeb42b569d4f82,
    0x3feeaf4736b527da,
    0x3feeab07dd485429,
    0x3feea76f15ad2148,
    0x3feea47eb03a5585,
    0x3feea23882552225,
    0x3feea09e667f3bcd,
    0x3fee9fb23c651a2f,
    0x3fee9f75e8ec5f74,
    0x3fee9feb564267c9,
    0x3feea11473eb0187,
    0x3feea2f336cf4e62,
    0x3feea589994cce13,
    0x3feea8d99b4492ed,
    0x3feeace5422aa0db,
    0x3feeb1ae99157736,
    0x3feeb737b0cdc5e5,
    0x3feebd829fde4e50,
    0x3feec49182a3f090,
    0x3feecc667b5de565,
    0x3feed503b23e255d,
    0x3feede6b5579fdbf,
    0x3feee89f995ad3ad,
    0x3feef3a2b84f15fb,
    0x3feeff76f2fb5e47,
    0x3fef0c1e904bc1d2,
    0x3fef199bdd85529c,
    0x3fef27f12e57d14b,
    0x3fef3720dcef9069,
    0x3fef472d4a07897c,
    0x3fef5818dcfba487,
    0x3fef69e603db3285,
    0x3fef7c97337b9b5f,
    0x3fef902ee78b3ff6,
    0x3fefa4afa2a490da,
    0x3fefba1bee615a27,
    0x3fefd0765b6e4540,
    0x3fefe7c1819e90d8,

};
