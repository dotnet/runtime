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

#ifndef __OPTIMIZED_EXP_H__
#define __OPTIMIZED_EXP_H__


#ifndef EXP_N
#define EXP_N 7
#endif

#define EXP_TABLE_SIZE (1ULL << EXP_N)

#if EXP_N == 6                              /* Table size 64 */
#define EXP_POLY_DEGREE 6

#elif EXP_N == 7                            /* Table size 128 */
#define EXP_POLY_DEGREE 5

#elif EXP_N == 8                            /* Table size 256 */
#define EXP_POLY_DEGREE 4

#elif EXP_N == 9                            /* Table size 512 */
#define EXP_POLY_DEGREE 4

#elif EXP_N == 10                           /* Table size 1024 */
#define EXP_POLY_DEGREE 4
#endif

#define EXP_MAX_POLYDEGREE 8

 /*
  * tblsz_byln2 = (-1) x (log(2) / TABLE_SIZE);
  * head = asuint64(tblsz_byln2)
  * tail = tblsz_byln2 - head
  */
struct exp_table {
    double main, head, tail;
};

struct exp_data_t {
    double Huge;
    double tblsz_byln2;
    struct {
        double head, tail;
    } ln2by_tblsz;
    double ALIGN(16) poly[EXP_MAX_POLYDEGREE];
    struct exp_table table[EXP_TABLE_SIZE];
};

static const exp_data_t exp_data = {
    /* .Huge = */ 0x1.8p+52,
#if EXP_N == 10
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+10,
    /* .ln2by_tblsz = */ {-0x1.62e42fefa0000p-11, -0x1.cf79abc9e3b3ap-50},
#elif EXP_N == 9
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+9,
    /* .ln2by_tblsz = */ {-0x1.62e42fefa0000p-10, -0x1.cf79abc9e3b39p-49}
#elif EXP_N == 8
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+8,
    /* .ln2by_tblsz = */ {-0x1.62e42fefa0000p-9, -0x1.cf79abc9e3b39p-48},
#elif EXP_N == 7
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+7,
    /* .ln2by_tblsz = */ {-0x1.62e42fefa0000p-8, -0x1.cf79abc9e3b39p-47},
#elif EXP_N == 6
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+6,
    /* .ln2by_tblsz = */ {-0x1.62e42fefa0000p-7, -0x1.cf79abc9e3b39p-46},
#else
#error "N not defined"
#endif

    /*
     * Polynomial constants, 1/x! (reciprocal of factorial(x))
     * To make better use of cache line, we dont store 0! and 1!
     */

    /* .poly = */        { /* skip for 0! and 1! */
        0x1.0000000000000p-1,   /* 1/2! = 1/2    */
        0x1.5555555555555p-3,   /* 1/3! = 1/6    */
        0x1.5555555555555p-5,   /* 1/4! = 1/24   */
        0x1.1111111111111p-7,   /* 1/5! = 1/120  */
#if EXP_POLY_DEGREE >= 5
        0x1.6c16c16c16c17p-10,  /* 1/6! = 1/720  */
#if EXP_POLY_DEGREE >= 6
        0x1.a01a01a01a01ap-13,  /* 1/7! = 1/5040 */
#if EXP_POLY_DEGREE >= 7
        0x1.a01a01a01a01ap-16,  /* 1/8! = 1/40320*/
#if EXP_POLY_DEGREE >= 8
        0x1.71de3a556c734p-19,  /* 1/9! = 1/322880*/
#endif
#endif
#endif
#endif
    },

    /* .table = */ {
#if EXP_N == 6
    #include "data/_exp_tbl_64_interleaved.data"
#elif EXP_N == 7
    #include "data/_exp_tbl_128_interleaved.data"
#endif
    },
};

/* C1 is 1 as 1! = 1 and 1/1! = 1 */
#define EXP_C2	exp_data.poly[0]
#define EXP_C3	exp_data.poly[1]
#define EXP_C4	exp_data.poly[2]
#define EXP_C5	exp_data.poly[3]
#define EXP_C6	exp_data.poly[4]
#define EXP_C7	exp_data.poly[5]
#define EXP_C8	exp_data.poly[6]
#define EXP_HUGE	exp_data.Huge

#define EXP_TBLSZ_BY_LN2	exp_data.tblsz_byln2
#define EXP_LN2_BY_TBLSZ_HEAD	exp_data.ln2by_tblsz.head
#define EXP_LN2_BY_TBLSZ_TAIL	exp_data.ln2by_tblsz.tail
#define EXP_TABLE_DATA		exp_data.table


#endif	/* OPTIMIZIED_EXP_H */
