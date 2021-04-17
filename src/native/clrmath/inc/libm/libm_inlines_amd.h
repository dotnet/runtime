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

#ifndef LIBM_INLINES_AMD_H_INCLUDED
#define LIBM_INLINES_AMD_H_INCLUDED 1

 /* Scales the double x by 2.0**n.
    Assumes EMIN <= n <= EMAX, though this condition is not checked. */
static inline double scaleDouble_1(double x, int n)
{
    double t;
    /* Construct the number t = 2.0**n */
    PUT_BITS_DP64(((long long)n + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t);
    return x * t;
}


/* Scales the double x by 2.0**n.
   Assumes 2*EMIN <= n <= 2*EMAX, though this condition is not checked. */
static inline double scaleDouble_2(double x, int n)
{
    double t1, t2;
    int n1, n2;
    n1 = n / 2;
    n2 = n - n1;
    /* Construct the numbers t1 = 2.0**n1 and t2 = 2.0**n2 */
    PUT_BITS_DP64(((long long)n1 + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t1);
    PUT_BITS_DP64(((long long)n2 + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t2);
    return (x * t1) * t2;
}




/* Scales the double x by 2.0**n.
   Assumes 3*EMIN <= n <= 3*EMAX, though this condition is not checked. */
static inline double scaleDouble_3(double x, int n)
{
    double t1, t2, t3;
    int n1, n2, n3;
    n1 = n / 3;
    n2 = (n - n1) / 2;
    n3 = n - n1 - n2;
    /* Construct the numbers t1 = 2.0**n1, t2 = 2.0**n2 and t3 = 2.0**n3 */
    PUT_BITS_DP64(((long long)n1 + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t1);
    PUT_BITS_DP64(((long long)n2 + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t2);
    PUT_BITS_DP64(((long long)n3 + EXPBIAS_DP64) << EXPSHIFTBITS_DP64, t3);
    return ((x * t1) * t2) * t3;
}


/* Scales the float x by 2.0**n.
   Assumes 2*EMIN <= n <= 2*EMAX, though this condition is not checked. */
static inline float scaleFloat_2(float x, int n)
{
    float t1, t2;
    int n1, n2;
    n1 = n / 2;
    n2 = n - n1;
    /* Construct the numbers t1 = 2.0**n1 and t2 = 2.0**n2 */
    PUT_BITS_SP32((n1 + EXPBIAS_SP32) << EXPSHIFTBITS_SP32, t1);
    PUT_BITS_SP32((n2 + EXPBIAS_SP32) << EXPSHIFTBITS_SP32, t2);
    return (x * t1) * t2;
}



/* Compute the values m, z1, and z2 such that base**x = 2**m * (z1 + z2).
   Small arguments abs(x) < 1/(16*ln(base)) and extreme arguments
   abs(x) > large/(ln(base)) (where large is the largest representable
   floating point number) should be handled separately instead of calling
   this function. This function is called by exp_amd, exp2_amd, exp10_amd,
   cosh_amd and sinh_amd. */
static inline void splitexp(double x, double logbase,
    double thirtytwo_by_logbaseof2,
    double logbaseof2_by_32_lead,
    double logbaseof2_by_32_trail,
    int* m, double* z1, double* z2)
{
    double q, r, r1, r2, f1, f2;
    int n, j;

    /* Arrays two_to_jby32_lead_table and two_to_jby32_trail_table contain
       leading and trailing parts respectively of precomputed
       values of pow(2.0,j/32.0), for j = 0, 1, ..., 31.
       two_to_jby32_lead_table contains the first 25 bits of precision,
       and two_to_jby32_trail_table contains a further 53 bits precision. */

    static const double two_to_jby32_lead_table[32] = {
      1.00000000000000000000e+00,   /* 0x3ff0000000000000 */
      1.02189713716506958008e+00,   /* 0x3ff059b0d0000000 */
      1.04427373409271240234e+00,   /* 0x3ff0b55860000000 */
      1.06714040040969848633e+00,   /* 0x3ff11301d0000000 */
      1.09050768613815307617e+00,   /* 0x3ff172b830000000 */
      1.11438673734664916992e+00,   /* 0x3ff1d48730000000 */
      1.13878858089447021484e+00,   /* 0x3ff2387a60000000 */
      1.16372483968734741211e+00,   /* 0x3ff29e9df0000000 */
      1.18920707702636718750e+00,   /* 0x3ff306fe00000000 */
      1.21524733304977416992e+00,   /* 0x3ff371a730000000 */
      1.24185776710510253906e+00,   /* 0x3ff3dea640000000 */
      1.26905095577239990234e+00,   /* 0x3ff44e0860000000 */
      1.29683953523635864258e+00,   /* 0x3ff4bfdad0000000 */
      1.32523661851882934570e+00,   /* 0x3ff5342b50000000 */
      1.35425549745559692383e+00,   /* 0x3ff5ab07d0000000 */
      1.38390988111495971680e+00,   /* 0x3ff6247eb0000000 */
      1.41421353816986083984e+00,   /* 0x3ff6a09e60000000 */
      1.44518077373504638672e+00,   /* 0x3ff71f75e0000000 */
      1.47682613134384155273e+00,   /* 0x3ff7a11470000000 */
      1.50916439294815063477e+00,   /* 0x3ff8258990000000 */
      1.54221081733703613281e+00,   /* 0x3ff8ace540000000 */
      1.57598084211349487305e+00,   /* 0x3ff93737b0000000 */
      1.61049032211303710938e+00,   /* 0x3ff9c49180000000 */
      1.64575546979904174805e+00,   /* 0x3ffa5503b0000000 */
      1.68179279565811157227e+00,   /* 0x3ffae89f90000000 */
      1.71861928701400756836e+00,   /* 0x3ffb7f76f0000000 */
      1.75625211000442504883e+00,   /* 0x3ffc199bd0000000 */
      1.79470902681350708008e+00,   /* 0x3ffcb720d0000000 */
      1.83400803804397583008e+00,   /* 0x3ffd5818d0000000 */
      1.87416762113571166992e+00,   /* 0x3ffdfc9730000000 */
      1.91520655155181884766e+00,   /* 0x3ffea4afa0000000 */
      1.95714408159255981445e+00 };  /* 0x3fff507650000000 */

    static const double two_to_jby32_trail_table[32] = {
      0.00000000000000000000e+00,   /* 0x0000000000000000 */
      1.14890470981563546737e-08,   /* 0x3e48ac2ba1d73e2a */
      4.83347014379782142328e-08,   /* 0x3e69f3121ec53172 */
      2.67125131841396124714e-10,   /* 0x3df25b50a4ebbf1b */
      4.65271045830351350190e-08,   /* 0x3e68faa2f5b9bef9 */
      5.24924336638693782574e-09,   /* 0x3e368b9aa7805b80 */
      5.38622214388600821910e-08,   /* 0x3e6ceac470cd83f6 */
      1.90902301017041969782e-08,   /* 0x3e547f7b84b09745 */
      3.79763538792174980894e-08,   /* 0x3e64636e2a5bd1ab */
      2.69306947081946450986e-08,   /* 0x3e5ceaa72a9c5154 */
      4.49683815095311756138e-08,   /* 0x3e682468446b6824 */
      1.41933332021066904914e-09,   /* 0x3e18624b40c4dbd0 */
      1.94146510233556266402e-08,   /* 0x3e54d8a89c750e5e */
      2.46409119489264118569e-08,   /* 0x3e5a753e077c2a0f */
      4.94812958044698886494e-08,   /* 0x3e6a90a852b19260 */
      8.48872238075784476136e-10,   /* 0x3e0d2ac258f87d03 */
      2.42032342089579394887e-08,   /* 0x3e59fcef32422cbf */
      3.32420002333182569170e-08,   /* 0x3e61d8bee7ba46e2 */
      1.45956577586525322754e-08,   /* 0x3e4f580c36bea881 */
      3.46452721050003920866e-08,   /* 0x3e62999c25159f11 */
      8.07090469079979051284e-09,   /* 0x3e415506dadd3e2a */
      2.99439161340839520436e-09,   /* 0x3e29b8bc9e8a0388 */
      9.83621719880452147153e-09,   /* 0x3e451f8480e3e236 */
      8.35492309647188080486e-09,   /* 0x3e41f12ae45a1224 */
      3.48493175137966283582e-08,   /* 0x3e62b5a75abd0e6a */
      1.11084703472699692902e-08,   /* 0x3e47daf237553d84 */
      5.03688744342840346564e-08,   /* 0x3e6b0aa538444196 */
      4.81896001063495806249e-08,   /* 0x3e69df20d22a0798 */
      4.83653666334089557746e-08,   /* 0x3e69f7490e4bb40b */
      1.29745882314081237628e-08,   /* 0x3e4bdcdaf5cb4656 */
      9.84532844621636118964e-09,   /* 0x3e452486cc2c7b9d */
      4.25828404545651943883e-08 };  /* 0x3e66dc8a80ce9f09 */

      /*
        Step 1. Reduce the argument.

        To perform argument reduction, we find the integer n such that
        x = n * logbaseof2/32 + remainder, |remainder| <= logbaseof2/64.
        n is defined by round-to-nearest-integer( x*32/logbaseof2 ) and
        remainder by x - n*logbaseof2/32. The calculation of n is
        straightforward whereas the computation of x - n*logbaseof2/32
        must be carried out carefully.
        logbaseof2/32 is so represented in two pieces that
        (1) logbaseof2/32 is known to extra precision, (2) the product
        of n and the leading piece is a model number and is hence
        calculated without error, and (3) the subtraction of the value
        obtained in (2) from x is a model number and is hence again
        obtained without error.
      */

    r = x * thirtytwo_by_logbaseof2;
    /* Set n = nearest integer to r */
    /* This is faster on Hammer */
    if (r > 0)
        n = (int)(r + 0.5);
    else
        n = (int)(r - 0.5);

    r1 = x - n * logbaseof2_by_32_lead;
    r2 = -n * logbaseof2_by_32_trail;

    /* Set j = n mod 32:   5 mod 32 = 5,   -5 mod 32 = 27,  etc. */
    /* j = n % 32;
       if (j < 0) j += 32; */
    j = n & 0x0000001f;

    f1 = two_to_jby32_lead_table[j];
    f2 = two_to_jby32_trail_table[j];

    *m = (n - j) / 32;

    /* Step 2. The following is the core approximation. We approximate
       exp(r1+r2)-1 by a polynomial. */

    r1 *= logbase; r2 *= logbase;

    r = r1 + r2;
    q = r1 + (r2 +
        r * r * (5.00000000000000008883e-01 +
            r * (1.66666666665260878863e-01 +
                r * (4.16666666662260795726e-02 +
                    r * (8.33336798434219616221e-03 +
                        r * (1.38889490863777199667e-03))))));

    /* Step 3. Function value reconstruction.
       We now reconstruct the exponential of the input argument
       so that exp(x) = 2**m * (z1 + z2).
       The order of the computation below must be strictly observed. */

    *z1 = f1;
    *z2 = f2 + ((f1 + f2) * q);
}




/* Compute the values m, z1, and z2 such that base**x = 2**m * (z1 + z2).
   Small arguments abs(x) < 1/(16*ln(base)) and extreme arguments
   abs(x) > large/(ln(base)) (where large is the largest representable
   floating point number) should be handled separately instead of calling
   this function. This function is called by exp_amd, exp2_amd, exp10_amd,
   cosh_amd and sinh_amd. */
static inline void splitexpf(float x, float logbase,
    float thirtytwo_by_logbaseof2,
    float logbaseof2_by_32_lead,
    float logbaseof2_by_32_trail,
    int* m, float* z1, float* z2)
{
    float q, r, r1, r2, f1, f2;
    int n, j;

    /* Arrays two_to_jby32_lead_table and two_to_jby32_trail_table contain
       leading and trailing parts respectively of precomputed
       values of pow(2.0,j/32.0), for j = 0, 1, ..., 31.
       two_to_jby32_lead_table contains the first 10 bits of precision,
       and two_to_jby32_trail_table contains a further 24 bits precision. */

    static const float two_to_jby32_lead_table[32] = {
      1.0000000000E+00F,  /* 0x3F800000 */
      1.0214843750E+00F,  /* 0x3F82C000 */
      1.0429687500E+00F,  /* 0x3F858000 */
      1.0664062500E+00F,  /* 0x3F888000 */
      1.0898437500E+00F,  /* 0x3F8B8000 */
      1.1132812500E+00F,  /* 0x3F8E8000 */
      1.1386718750E+00F,  /* 0x3F91C000 */
      1.1621093750E+00F,  /* 0x3F94C000 */
      1.1875000000E+00F,  /* 0x3F980000 */
      1.2148437500E+00F,  /* 0x3F9B8000 */
      1.2402343750E+00F,  /* 0x3F9EC000 */
      1.2675781250E+00F,  /* 0x3FA24000 */
      1.2949218750E+00F,  /* 0x3FA5C000 */
      1.3242187500E+00F,  /* 0x3FA98000 */
      1.3535156250E+00F,  /* 0x3FAD4000 */
      1.3828125000E+00F,  /* 0x3FB10000 */
      1.4140625000E+00F,  /* 0x3FB50000 */
      1.4433593750E+00F,  /* 0x3FB8C000 */
      1.4765625000E+00F,  /* 0x3FBD0000 */
      1.5078125000E+00F,  /* 0x3FC10000 */
      1.5410156250E+00F,  /* 0x3FC54000 */
      1.5742187500E+00F,  /* 0x3FC98000 */
      1.6093750000E+00F,  /* 0x3FCE0000 */
      1.6445312500E+00F,  /* 0x3FD28000 */
      1.6816406250E+00F,  /* 0x3FD74000 */
      1.7167968750E+00F,  /* 0x3FDBC000 */
      1.7558593750E+00F,  /* 0x3FE0C000 */
      1.7929687500E+00F,  /* 0x3FE58000 */
      1.8339843750E+00F,  /* 0x3FEAC000 */
      1.8730468750E+00F,  /* 0x3FEFC000 */
      1.9140625000E+00F,  /* 0x3FF50000 */
      1.9570312500E+00F }; /* 0x3FFA8000 */

    static const float two_to_jby32_trail_table[32] = {
      0.0000000000E+00F,  /* 0x00000000 */
      4.1277357377E-04F,  /* 0x39D86988 */
      1.3050324051E-03F,  /* 0x3AAB0D9F */
      7.3415064253E-04F,  /* 0x3A407404 */
      6.6398258787E-04F,  /* 0x3A2E0F1E */
      1.1054925853E-03F,  /* 0x3A90E62D */
      1.1675967835E-04F,  /* 0x38F4DCE0 */
      1.6154836630E-03F,  /* 0x3AD3BEA3 */
      1.7071149778E-03F,  /* 0x3ADFC146 */
      4.0360994171E-04F,  /* 0x39D39B9C */
      1.6234370414E-03F,  /* 0x3AD4C982 */
      1.4728321694E-03F,  /* 0x3AC10C0C */
      1.9176795613E-03F,  /* 0x3AFB5AA6 */
      1.0178930825E-03F,  /* 0x3A856AD3 */
      7.3992193211E-04F,  /* 0x3A41F752 */
      1.0973819299E-03F,  /* 0x3A8FD607 */
      1.5106226783E-04F,  /* 0x391E6678 */
      1.8214319134E-03F,  /* 0x3AEEBD1D */
      2.6364589576E-04F,  /* 0x398A39F4 */
      1.3519275235E-03F,  /* 0x3AB13329 */
      1.1952003697E-03F,  /* 0x3A9CA845 */
      1.7620950239E-03F,  /* 0x3AE6F619 */
      1.1153318919E-03F,  /* 0x3A923054 */
      1.2242280645E-03F,  /* 0x3AA07647 */
      1.5220546629E-04F,  /* 0x391F9958 */
      1.8224230735E-03F,  /* 0x3AEEDE5F */
      3.9278529584E-04F,  /* 0x39CDEEC0 */
      1.7403248930E-03F,  /* 0x3AE41B9D */
      2.3711356334E-05F,  /* 0x37C6E7C0 */
      1.1207590578E-03F,  /* 0x3A92E66F */
      1.1440613307E-03F,  /* 0x3A95F454 */
      1.1287408415E-04F }; /* 0x38ECB6D0 */

      /*
        Step 1. Reduce the argument.

        To perform argument reduction, we find the integer n such that
        x = n * logbaseof2/32 + remainder, |remainder| <= logbaseof2/64.
        n is defined by round-to-nearest-integer( x*32/logbaseof2 ) and
        remainder by x - n*logbaseof2/32. The calculation of n is
        straightforward whereas the computation of x - n*logbaseof2/32
        must be carried out carefully.
        logbaseof2/32 is so represented in two pieces that
        (1) logbaseof2/32 is known to extra precision, (2) the product
        of n and the leading piece is a model number and is hence
        calculated without error, and (3) the subtraction of the value
        obtained in (2) from x is a model number and is hence again
        obtained without error.
      */

    r = x * thirtytwo_by_logbaseof2;
    /* Set n = nearest integer to r */
    /* This is faster on Hammer */
    if (r > 0)
        n = (int)(r + 0.5F);
    else
        n = (int)(r - 0.5F);

    r1 = x - n * logbaseof2_by_32_lead;
    r2 = -n * logbaseof2_by_32_trail;

    /* Set j = n mod 32:   5 mod 32 = 5,   -5 mod 32 = 27,  etc. */
    /* j = n % 32;
       if (j < 0) j += 32; */
    j = n & 0x0000001f;

    f1 = two_to_jby32_lead_table[j];
    f2 = two_to_jby32_trail_table[j];

    *m = (n - j) / 32;

    /* Step 2. The following is the core approximation. We approximate
       exp(r1+r2)-1 by a polynomial. */

    r1 *= logbase; r2 *= logbase;

    r = r1 + r2;
    q = r1 + (r2 +
        r * r * (5.00000000000000008883e-01F +
            r * (1.66666666665260878863e-01F)));

    /* Step 3. Function value reconstruction.
       We now reconstruct the exponential of the input argument
       so that exp(x) = 2**m * (z1 + z2).
       The order of the computation below must be strictly observed. */

    *z1 = f1;
    *z2 = f2 + ((f1 + f2) * q);
}



/* Scales up a double (normal or denormal) whose bit pattern is given
   as ux by 2**1024. There are no checks that the input number is
   scalable by that amount. */
static inline void scaleUpDouble1024(unsigned long long ux, unsigned long long* ur)
{
    unsigned long long uy;
    double y;

    if ((ux & EXPBITS_DP64) == 0)
    {
        /* ux is denormalised */
        PUT_BITS_DP64(ux | 0x4010000000000000, y);
        if (ux & SIGNBIT_DP64)
            y += 4.0;
        else
            y -= 4.0;
        GET_BITS_DP64(y, uy);
    }
    else
        /* ux is normal */
        uy = ux + 0x4000000000000000;

    *ur = uy;
    return;
}



/* Scales down a double whose bit pattern is given as ux by 2**k.
   There are no checks that the input number is scalable by that amount. */
static inline void scaleDownDouble(unsigned long long ux, int k,
    unsigned long long* ur)
{
    unsigned long long uy, uk, ax, xsign;
    int n, shift;
    xsign = ux & SIGNBIT_DP64;
    ax = ux & ~SIGNBIT_DP64;
    n = (int)((ax & EXPBITS_DP64) >> EXPSHIFTBITS_DP64) - k;
    if (n > 0)
    {
        uk = (unsigned long long)n << EXPSHIFTBITS_DP64;
        uy = (ax & ~EXPBITS_DP64) | uk;
    }
    else
    {
        uy = (ax & ~EXPBITS_DP64) | 0x0010000000000000;
        shift = (1 - n);
        if (shift > MANTLENGTH_DP64 + 1)
            /* Sigh. Shifting works mod 64 so be careful not to shift too much */
            uy = 0;
        else
        {
            /* Make sure we round the result */
            uy >>= shift - 1;
            uy = (uy >> 1) + (uy & 1);
        }
    }
    *ur = uy | xsign;
}


static inline void log_kernel_amd64(double x, unsigned long long ux, int* xexp, double* r1, double* r2)
{

    int expadjust;
    double r, z1, z2, correction, f, f1, f2, q, u, v, poly;
    int index;

    /*
      Computes natural log(x). Algorithm based on:
      Ping-Tak Peter Tang
      "Table-driven implementation of the logarithm function in IEEE
      floating-point arithmetic"
      ACM Transactions on Mathematical Software (TOMS)
      Volume 16, Issue 4 (December 1990)
    */

    /* Arrays ln_lead_table and ln_tail_table contain
       leading and trailing parts respectively of precomputed
       values of natural log(1+i/64), for i = 0, 1, ..., 64.
       ln_lead_table contains the first 24 bits of precision,
       and ln_tail_table contains a further 53 bits precision. */

    static const double ln_lead_table[65] = {
      0.00000000000000000000e+00,   /* 0x0000000000000000 */
      1.55041813850402832031e-02,   /* 0x3f8fc0a800000000 */
      3.07716131210327148438e-02,   /* 0x3f9f829800000000 */
      4.58095073699951171875e-02,   /* 0x3fa7745800000000 */
      6.06245994567871093750e-02,   /* 0x3faf0a3000000000 */
      7.52233862876892089844e-02,   /* 0x3fb341d700000000 */
      8.96121263504028320312e-02,   /* 0x3fb6f0d200000000 */
      1.03796780109405517578e-01,   /* 0x3fba926d00000000 */
      1.17783010005950927734e-01,   /* 0x3fbe270700000000 */
      1.31576299667358398438e-01,   /* 0x3fc0d77e00000000 */
      1.45181953907012939453e-01,   /* 0x3fc2955280000000 */
      1.58604979515075683594e-01,   /* 0x3fc44d2b00000000 */
      1.71850204467773437500e-01,   /* 0x3fc5ff3000000000 */
      1.84922337532043457031e-01,   /* 0x3fc7ab8900000000 */
      1.97825729846954345703e-01,   /* 0x3fc9525a80000000 */
      2.10564732551574707031e-01,   /* 0x3fcaf3c900000000 */
      2.23143517971038818359e-01,   /* 0x3fcc8ff780000000 */
      2.35566020011901855469e-01,   /* 0x3fce270700000000 */
      2.47836112976074218750e-01,   /* 0x3fcfb91800000000 */
      2.59957492351531982422e-01,   /* 0x3fd0a324c0000000 */
      2.71933674812316894531e-01,   /* 0x3fd1675c80000000 */
      2.83768117427825927734e-01,   /* 0x3fd22941c0000000 */
      2.95464158058166503906e-01,   /* 0x3fd2e8e280000000 */
      3.07025015354156494141e-01,   /* 0x3fd3a64c40000000 */
      3.18453729152679443359e-01,   /* 0x3fd4618bc0000000 */
      3.29753279685974121094e-01,   /* 0x3fd51aad80000000 */
      3.40926527976989746094e-01,   /* 0x3fd5d1bd80000000 */
      3.51976394653320312500e-01,   /* 0x3fd686c800000000 */
      3.62905442714691162109e-01,   /* 0x3fd739d7c0000000 */
      3.73716354370117187500e-01,   /* 0x3fd7eaf800000000 */
      3.84411692619323730469e-01,   /* 0x3fd89a3380000000 */
      3.94993782043457031250e-01,   /* 0x3fd9479400000000 */
      4.05465066432952880859e-01,   /* 0x3fd9f323c0000000 */
      4.15827870368957519531e-01,   /* 0x3fda9cec80000000 */
      4.26084339618682861328e-01,   /* 0x3fdb44f740000000 */
      4.36236739158630371094e-01,   /* 0x3fdbeb4d80000000 */
      4.46287095546722412109e-01,   /* 0x3fdc8ff7c0000000 */
      4.56237375736236572266e-01,   /* 0x3fdd32fe40000000 */
      4.66089725494384765625e-01,   /* 0x3fddd46a00000000 */
      4.75845873355865478516e-01,   /* 0x3fde744240000000 */
      4.85507786273956298828e-01,   /* 0x3fdf128f40000000 */
      4.95077252388000488281e-01,   /* 0x3fdfaf5880000000 */
      5.04556000232696533203e-01,   /* 0x3fe02552a0000000 */
      5.13945698738098144531e-01,   /* 0x3fe0723e40000000 */
      5.23248136043548583984e-01,   /* 0x3fe0be72e0000000 */
      5.32464742660522460938e-01,   /* 0x3fe109f380000000 */
      5.41597247123718261719e-01,   /* 0x3fe154c3c0000000 */
      5.50647079944610595703e-01,   /* 0x3fe19ee6a0000000 */
      5.59615731239318847656e-01,   /* 0x3fe1e85f40000000 */
      5.68504691123962402344e-01,   /* 0x3fe23130c0000000 */
      5.77315330505371093750e-01,   /* 0x3fe2795e00000000 */
      5.86049020290374755859e-01,   /* 0x3fe2c0e9e0000000 */
      5.94707071781158447266e-01,   /* 0x3fe307d720000000 */
      6.03290796279907226562e-01,   /* 0x3fe34e2880000000 */
      6.11801505088806152344e-01,   /* 0x3fe393e0c0000000 */
      6.20240390300750732422e-01,   /* 0x3fe3d90260000000 */
      6.28608644008636474609e-01,   /* 0x3fe41d8fe0000000 */
      6.36907458305358886719e-01,   /* 0x3fe4618bc0000000 */
      6.45137906074523925781e-01,   /* 0x3fe4a4f840000000 */
      6.53301239013671875000e-01,   /* 0x3fe4e7d800000000 */
      6.61398470401763916016e-01,   /* 0x3fe52a2d20000000 */
      6.69430613517761230469e-01,   /* 0x3fe56bf9c0000000 */
      6.77398800849914550781e-01,   /* 0x3fe5ad4040000000 */
      6.85303986072540283203e-01,   /* 0x3fe5ee02a0000000 */
      6.93147122859954833984e-01 };  /* 0x3fe62e42e0000000 */

    static const double ln_tail_table[65] = {
      0.00000000000000000000e+00,   /* 0x0000000000000000 */
      5.15092497094772879206e-09,   /* 0x3e361f807c79f3db */
      4.55457209735272790188e-08,   /* 0x3e6873c1980267c8 */
      2.86612990859791781788e-08,   /* 0x3e5ec65b9f88c69e */
      2.23596477332056055352e-08,   /* 0x3e58022c54cc2f99 */
      3.49498983167142274770e-08,   /* 0x3e62c37a3a125330 */
      3.23392843005887000414e-08,   /* 0x3e615cad69737c93 */
      1.35722380472479366661e-08,   /* 0x3e4d256ab1b285e9 */
      2.56504325268044191098e-08,   /* 0x3e5b8abcb97a7aa2 */
      5.81213608741512136843e-08,   /* 0x3e6f34239659a5dc */
      5.59374849578288093334e-08,   /* 0x3e6e07fd48d30177 */
      5.06615629004996189970e-08,   /* 0x3e6b32df4799f4f6 */
      5.24588857848400955725e-08,   /* 0x3e6c29e4f4f21cf8 */
      9.61968535632653505972e-10,   /* 0x3e1086c848df1b59 */
      1.34829655346594463137e-08,   /* 0x3e4cf456b4764130 */
      3.65557749306383026498e-08,   /* 0x3e63a02ffcb63398 */
      3.33431709374069198903e-08,   /* 0x3e61e6a6886b0976 */
      5.13008650536088382197e-08,   /* 0x3e6b8abcb97a7aa2 */
      5.09285070380306053751e-08,   /* 0x3e6b578f8aa35552 */
      3.20853940845502057341e-08,   /* 0x3e6139c871afb9fc */
      4.06713248643004200446e-08,   /* 0x3e65d5d30701ce64 */
      5.57028186706125221168e-08,   /* 0x3e6de7bcb2d12142 */
      5.48356693724804282546e-08,   /* 0x3e6d708e984e1664 */
      1.99407553679345001938e-08,   /* 0x3e556945e9c72f36 */
      1.96585517245087232086e-09,   /* 0x3e20e2f613e85bda */
      6.68649386072067321503e-09,   /* 0x3e3cb7e0b42724f6 */
      5.89936034642113390002e-08,   /* 0x3e6fac04e52846c7 */
      2.85038578721554472484e-08,   /* 0x3e5e9b14aec442be */
      5.09746772910284482606e-08,   /* 0x3e6b5de8034e7126 */
      5.54234668933210171467e-08,   /* 0x3e6dc157e1b259d3 */
      6.29100830926604004874e-09,   /* 0x3e3b05096ad69c62 */
      2.61974119468563937716e-08,   /* 0x3e5c2116faba4cdd */
      4.16752115011186398935e-08,   /* 0x3e665fcc25f95b47 */
      2.47747534460820790327e-08,   /* 0x3e5a9a08498d4850 */
      5.56922172017964209793e-08,   /* 0x3e6de647b1465f77 */
      2.76162876992552906035e-08,   /* 0x3e5da71b7bf7861d */
      7.08169709942321478061e-09,   /* 0x3e3e6a6886b09760 */
      5.77453510221151779025e-08,   /* 0x3e6f0075eab0ef64 */
      4.43021445893361960146e-09,   /* 0x3e33071282fb989b */
      3.15140984357495864573e-08,   /* 0x3e60eb43c3f1bed2 */
      2.95077445089736670973e-08,   /* 0x3e5faf06ecb35c84 */
      1.44098510263167149349e-08,   /* 0x3e4ef1e63db35f68 */
      1.05196987538551827693e-08,   /* 0x3e469743fb1a71a5 */
      5.23641361722697546261e-08,   /* 0x3e6c1cdf404e5796 */
      7.72099925253243069458e-09,   /* 0x3e4094aa0ada625e */
      5.62089493829364197156e-08,   /* 0x3e6e2d4c96fde3ec */
      3.53090261098577946927e-08,   /* 0x3e62f4d5e9a98f34 */
      3.80080516835568242269e-08,   /* 0x3e6467c96ecc5cbe */
      5.66961038386146408282e-08,   /* 0x3e6e7040d03dec5a */
      4.42287063097349852717e-08,   /* 0x3e67bebf4282de36 */
      3.45294525105681104660e-08,   /* 0x3e6289b11aeb783f */
      2.47132034530447431509e-08,   /* 0x3e5a891d1772f538 */
      3.59655343422487209774e-08,   /* 0x3e634f10be1fb591 */
      5.51581770357780862071e-08,   /* 0x3e6d9ce1d316eb93 */
      3.60171867511861372793e-08,   /* 0x3e63562a19a9c442 */
      1.94511067964296180547e-08,   /* 0x3e54e2adf548084c */
      1.54137376631349347838e-08,   /* 0x3e508ce55cc8c97a */
      3.93171034490174464173e-09,   /* 0x3e30e2f613e85bda */
      5.52990607758839766440e-08,   /* 0x3e6db03ebb0227bf */
      3.29990737637586136511e-08,   /* 0x3e61b75bb09cb098 */
      1.18436010922446096216e-08,   /* 0x3e496f16abb9df22 */
      4.04248680368301346709e-08,   /* 0x3e65b3f399411c62 */
      2.27418915900284316293e-08,   /* 0x3e586b3e59f65355 */
      1.70263791333409206020e-08,   /* 0x3e52482ceae1ac12 */
      5.76999904754328540596e-08 };  /* 0x3e6efa39ef35793c */

    /* Approximating polynomial coefficients for x near 1.0 */
    static const double
        ca_1 = 8.33333333333317923934e-02,  /* 0x3fb55555555554e6 */
        ca_2 = 1.25000000037717509602e-02,  /* 0x3f89999999bac6d4 */
        ca_3 = 2.23213998791944806202e-03,  /* 0x3f62492307f1519f */
        ca_4 = 4.34887777707614552256e-04;  /* 0x3f3c8034c85dfff0 */

      /* Approximating polynomial coefficients for other x */
    static const double
        cb_1 = 8.33333333333333593622e-02,  /* 0x3fb5555555555557 */
        cb_2 = 1.24999999978138668903e-02,  /* 0x3f89999999865ede */
        cb_3 = 2.23219810758559851206e-03;  /* 0x3f6249423bd94741 */

    static const unsigned long long
        log_thresh1 = 0x3fee0faa00000000,
        log_thresh2 = 0x3ff1082c00000000;

    /* log_thresh1 = 9.39412117004394531250e-1 = 0x3fee0faa00000000
       log_thresh2 = 1.06449508666992187500 = 0x3ff1082c00000000 */
    if (ux >= log_thresh1 && ux <= log_thresh2)
    {
        /* Arguments close to 1.0 are handled separately to maintain
           accuracy.

           The approximation in this region exploits the identity
               log( 1 + r ) = log( 1 + u/2 )  /  log( 1 - u/2 ), where
               u  = 2r / (2+r).
           Note that the right hand side has an odd Taylor series expansion
           which converges much faster than the Taylor series expansion of
           log( 1 + r ) in r. Thus, we approximate log( 1 + r ) by
               u + A1 * u^3 + A2 * u^5 + ... + An * u^(2n+1).

           One subtlety is that since u cannot be calculated from
           r exactly, the rounding error in the first u should be
           avoided if possible. To accomplish this, we observe that
                         u  =  r  -  r*r/(2+r).
           Since x (=1+r) is the input argument, and thus presumed exact,
           the formula above approximates u accurately because
                         u  =  r  -  correction,
           and the magnitude of "correction" (of the order of r*r)
           is small.
           With these observations, we will approximate log( 1 + r ) by
              r + (  (A1*u^3 + ... + An*u^(2n+1)) - correction ).

           We approximate log(1+r) by an odd polynomial in u, where
                    u = 2r/(2+r) = r - r*r/(2+r).
        */
        r = x - 1.0;
        u = r / (2.0 + r);
        correction = r * u;
        u = u + u;
        v = u * u;
        z1 = r;
        z2 = (u * v * (ca_1 + v * (ca_2 + v * (ca_3 + v * ca_4))) - correction);
        *r1 = z1;
        *r2 = z2;
        *xexp = 0;
    }
    else
    {
        /*
          First, we decompose the argument x to the form
          x  =  2**M  *  (F1  +  F2),
          where  1 <= F1+F2 < 2, M has the value of an integer,
          F1 = 1 + j/64, j ranges from 0 to 64, and |F2| <= 1/128.

          Second, we approximate log( 1 + F2/F1 ) by an odd polynomial
          in U, where U  =  2 F2 / (2 F2 + F1).
          Note that log( 1 + F2/F1 ) = log( 1 + U/2 ) - log( 1 - U/2 ).
          The core approximation calculates
          Poly = [log( 1 + U/2 ) - log( 1 - U/2 )]/U   -   1.
          Note that  log(1 + U/2) - log(1 - U/2) = 2 arctanh ( U/2 ),
          thus, Poly =  2 arctanh( U/2 ) / U  -  1.

          It is not hard to see that
            log(x) = M*log(2) + log(F1) + log( 1 + F2/F1 ).
          Hence, we return Z1 = log(F1), and  Z2 = log( 1 + F2/F1).
          The values of log(F1) are calculated beforehand and stored
          in the program.
        */

        f = x;
        if (ux < IMPBIT_DP64)
        {
            /* The input argument x is denormalized */
            /* Normalize f by increasing the exponent by 60
               and subtracting a correction to account for the implicit
               bit. This replaces a slow denormalized
               multiplication by a fast normal subtraction. */
            static const double corr = 2.5653355008114851558350183e-290; /* 0x03d0000000000000 */
            GET_BITS_DP64(f, ux);
            ux |= 0x03d0000000000000;
            PUT_BITS_DP64(ux, f);
            f -= corr;
            GET_BITS_DP64(f, ux);
            expadjust = 60;
        }
        else
            expadjust = 0;

        /* Store the exponent of x in xexp and put
           f into the range [0.5,1) */
        *xexp = (int)((ux & EXPBITS_DP64) >> EXPSHIFTBITS_DP64) - EXPBIAS_DP64 - expadjust;
        PUT_BITS_DP64((ux & MANTBITS_DP64) | HALFEXPBITS_DP64, f);

        /* Now  x = 2**xexp  * f,  1/2 <= f < 1. */

        /* Set index to be the nearest integer to 128*f */
        r = 128.0 * f;
        index = (int)(r + 0.5);

        z1 = ln_lead_table[index - 64];
        q = ln_tail_table[index - 64];
        f1 = index * 0.0078125; /* 0.0078125 = 1/128 */
        f2 = f - f1;
        /* At this point, x = 2**xexp * ( f1  +  f2 ) where
           f1 = j/128, j = 64, 65, ..., 128 and |f2| <= 1/256. */

           /* Calculate u = 2 f2 / ( 2 f1 + f2 ) = f2 / ( f1 + 0.5*f2 ) */
           /* u = f2 / (f1 + 0.5 * f2); */
        u = f2 / (f1 + 0.5 * f2);

        /* Here, |u| <= 2(exp(1/16)-1) / (exp(1/16)+1).
           The core approximation calculates
           poly = [log(1 + u/2) - log(1 - u/2)]/u  -  1  */
        v = u * u;
        poly = (v * (cb_1 + v * (cb_2 + v * cb_3)));
        z2 = q + (u + u * poly);
        *r1 = z1;
        *r2 = z2;
    }
    return;
}

#endif /* LIBM_INLINES_AMD_H_INCLUDED */
