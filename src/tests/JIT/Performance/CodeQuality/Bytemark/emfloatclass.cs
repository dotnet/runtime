// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*
** This program was translated to C# and adapted for xunit-performance.
** New variants of several tests were added to compare class versus 
** struct and to compare jagged arrays vs multi-dimensional arrays.
*/

/*
** BYTEmark (tm)
** BYTE Magazine's Native Mode benchmarks
** Rick Grehan, BYTE Magazine
**
** Create:
** Revision: 3/95
**
** DISCLAIMER
** The source, executable, and documentation files that comprise
** the BYTEmark benchmarks are made available on an "as is" basis.
** This means that we at BYTE Magazine have made every reasonable
** effort to verify that the there are no errors in the source and
** executable code.  We cannot, however, guarantee that the programs
** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
** no claims in regard to the fitness of the source code, executable
** code, and documentation of the BYTEmark.
** 
** Furthermore, BYTE Magazine, McGraw-Hill, and all employees
** of McGraw-Hill cannot be held responsible for any damages resulting
** from the use of this code or the results obtained from using
** this code.
*/

using System;

/*
** DEFINES
*/
public class EMFloatClass : EmFloatStruct
{
    public override string Name()
    {
        return "FP EMULATION(class)";
    }

    private const int MAX_EXP = 32767;
    private const int MIN_EXP = (-32767);

    private enum IFPF : byte
    {
        IFPF_IS_ZERO = 0,
        IFPF_IS_SUBNORMAL = 1,
        IFPF_IS_NORMAL = 2,
        IFPF_IS_INFINITY = 3,
        IFPF_IS_NAN = 4,
        IFPF_TYPE_COUNT = 5,
    };

    private enum STATE
    {
        ZERO_ZERO = 0,
        ZERO_SUBNORMAL = 1,
        ZERO_NORMAL = 2,
        ZERO_INFINITY = 3,
        ZERO_NAN = 4,

        SUBNORMAL_ZERO = 5,
        SUBNORMAL_SUBNORMAL = 6,
        SUBNORMAL_NORMAL = 7,
        SUBNORMAL_INFINITY = 8,
        SUBNORMAL_NAN = 9,

        NORMAL_ZERO = 10,
        NORMAL_SUBNORMAL = 11,
        NORMAL_NORMAL = 12,
        NORMAL_INFINITY = 13,
        NORMAL_NAN = 14,

        INFINITY_ZERO = 15,
        INFINITY_SUBNORMAL = 16,
        INFINITY_NORMAL = 17,
        INFINITY_INFINITY = 18,
        INFINITY_NAN = 19,

        NAN_ZERO = 20,
        NAN_SUBNORMAL = 21,
        NAN_NORMAL = 22,
        NAN_INFINITY = 23,
        NAN_NAN = 24,
    };

    private enum OPERAND
    {
        OPERAND_ZERO = 0,
        OPERAND_SUBNORMAL = 1,
        OPERAND_NORMAL = 2,
        OPERAND_INFINITY = 3,
        OPERAND_NAN = 4,
    };

    /*
    ** Following was already defined in NMGLOBAL.H
    **
    */
    private const int INTERNAL_FPF_PRECISION = 4;

    /*
    ** TYPEDEFS
    */

    private class InternalFPF
    {
        public InternalFPF()
        {
            type = IFPF.IFPF_IS_ZERO; sign = (byte)0;
            exp = (short)0; mantissa = new char[INTERNAL_FPF_PRECISION];
        }
        public IFPF type;        /* Indicates, NORMAL, SUBNORMAL, etc. */
        public byte sign;        /* Mantissa sign */
        public short exp;      /* Signed exponent...no bias */
        public char[] mantissa; // [INTERNAL_FPF_PRECISION]
    };

    /*
    ** emfloat.c
    ** Source for emulated floating-point routines.
    ** BYTEmark (tm)
    ** BYTE's Native Mode Benchmarks
    ** Rick Grehan, BYTE Magazine.
    **
    ** Created:
    ** Last update: 3/95
    **
    ** DISCLAIMER
    ** The source, executable, and documentation files that comprise
    ** the BYTEmark benchmarks are made available on an "as is" basis.
    ** This means that we at BYTE Magazine have made every reasonable
    ** effort to verify that the there are no errors in the source and
    ** executable code.  We cannot, however, guarantee that the programs
    ** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
    ** no claims in regard to the fitness of the source code, executable
    ** code, and documentation of the BYTEmark.
    **  Furthermore, BYTE Magazine, McGraw-Hill, and all employees
    ** of McGraw-Hill cannot be held responsible for any damages resulting
    ** from the use of this code or the results obtained from using
    ** this code.
    */

    /*
    ** Floating-point emulator.
    ** These routines are only "sort of" IEEE-compliant.  All work is
    ** done using an internal representation.  Also, the routines do
    ** not check for many of the exceptions that might occur.
    ** Still, the external formats produced are IEEE-compatible,
    ** with the restriction that they presume a low-endian machine
    ** (though the endianism will not effect the performance).
    **
    ** Some code here was based on work done by Steve Snelgrove of
    ** Orem, UT.  Other code comes from routines presented in
    ** the long-ago book: "Microprocessor Programming for
    ** Computer Hobbyists" by Neill Graham.
    */

    /*****************************
    ** FLOATING-POINT EMULATION **
    *****************************/

    /**************
    ** DoEmFloat **
    ***************
    ** Perform the floating-point emulation routines portion of the
    ** CPU benchmark.  Returns the operations per second.
    */
    public override double Run()
    {
        InternalFPF[] abase;             /* Base of A array */
        InternalFPF[] bbase;             /* Base of B array */
        InternalFPF[] cbase;             /* Base of C array */
        long accumtime;                /* Accumulated time in ticks */
        double iterations;              /* # of iterations */
        long tickcount;                /* # of ticks */
        int loops;                    /* # of loops */

        /*
        ** Test the emulation routines.
        */

        abase = new InternalFPF[this.arraysize];
        bbase = new InternalFPF[this.arraysize];
        cbase = new InternalFPF[this.arraysize];

        for (int i = 0; i < this.arraysize; i++)
        {
            abase[i] = new InternalFPF();
            bbase[i] = new InternalFPF();
            cbase[i] = new InternalFPF();
        }

        /*
        for (int i = 0; i < this.arraysize; i++) 
        {
            abase[i].type = IFPF.IFPF_IS_ZERO;
            abase[i].sign = (byte)0;
            abase[i].exp = (short)0;
            abase[i].mantissa = new char[INTERNAL_FPF_PRECISION];

            bbase[i].type = IFPF.IFPF_IS_ZERO;
            bbase[i].sign = (byte)0;
            bbase[i].exp = (short)0;
            bbase[i].mantissa = new char[INTERNAL_FPF_PRECISION];

            cbase[i].type = IFPF.IFPF_IS_ZERO;
            cbase[i].sign = (byte)0;
            cbase[i].exp = (short)0;
            cbase[i].mantissa = new char[INTERNAL_FPF_PRECISION];
        }
        */

        /*
        ** Set up the arrays
        */
        SetupCPUEmFloatArrays(abase, bbase, cbase, this.arraysize);

        /*
        ** See if we need to do self-adjusting code.
        */
        if (this.adjust == 0)
        {
            this.loops = 0;

            /*
            ** Do an iteration of the tests.  If the elapsed time is
            ** less than minimum, increase the loop count and try
            ** again.
            */
            for (loops = 1; loops < global.CPUEMFLOATLOOPMAX; loops += loops)
            {
                tickcount = DoEmFloatIteration(abase, bbase, cbase,
                    this.arraysize,
                    loops);
                if (tickcount > global.min_ticks)
                {
                    this.loops = loops;
                    break;
                }
            }
        }

        /*
        ** Verify that selft adjustment code worked.
        */
        if (this.loops == 0)
        {
            throw new Exception("CPU:EMFPU -- CMPUEMFLOATLOOPMAX limit hit\n");
        }

        /*
        ** All's well if we get here.  Repeatedly perform floating
        ** tests until the accumulated time is greater than the
        ** # of seconds requested.
        ** Each iteration performs arraysize * 3 operations.
        */
        accumtime = 0L;
        iterations = (double)0.0;
        do
        {
            accumtime += DoEmFloatIteration(abase, bbase, cbase,
                this.arraysize,
                this.loops);
            iterations += (double)1.0;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        /*
        ** Clean up, calculate results, and go home.
        ** Also, indicate that adjustment is done.
        */

        if (this.adjust == 0)
            this.adjust = 1;
        double emflops = (iterations * (double)this.loops) /
            (double)ByteMark.TicksToFracSecs(accumtime);

        return (emflops);
    }






    /**************************
    ** SetupCPUEmFloatArrays **
    ***************************
    ** Set up the arrays that will be used in the emulated
    ** floating-point tests.
    ** This is done by loading abase and bbase elements with
    ** random numbers.  We use our long-to-floating point
    ** routine to set them up.
    ** NOTE: We really don't need the pointer to cbase...cbase
    ** is overwritten in the benchmark.
    */
    private static
    void SetupCPUEmFloatArrays(InternalFPF[] abase,
        InternalFPF[] bbase,
        InternalFPF[] cbase,
        int arraysize)
    {
        int i;
        InternalFPF locFPF1, locFPF2;
        locFPF1 = new InternalFPF();
        locFPF2 = new InternalFPF();

        for (i = 0; i < arraysize; i++)
        {
            LongToInternalFPF(ByteMark.randwc(50000), locFPF1);
            LongToInternalFPF(ByteMark.randwc(50000) + 1, locFPF2);
            DivideInternalFPF(locFPF1, locFPF2, abase[i]);
            LongToInternalFPF(ByteMark.randwc(50000) + 1, locFPF2);
            DivideInternalFPF(locFPF1, locFPF2, bbase[i]);
        }
        return;
    }

    /***********************
    ** DoEmFloatIteration **
    ************************
    ** Perform an iteration of the emulated floating-point
    ** benchmark.  Note that "an iteration" can involve multiple
    ** loops through the benchmark.
    */
    private static
    long DoEmFloatIteration(InternalFPF[] abase,
        InternalFPF[] bbase,
        InternalFPF[] cbase,
        int arraysize, int loops)
    {
        long elapsed;          /* For the stopwatch */
        byte[] jtable = new byte[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3 };
        int i;

        /*
        ** Begin timing
        */
        elapsed = ByteMark.StartStopwatch();

        /*
        ** Each pass through the array performs operations in
        ** the followingratios:
        **   4 adds, 4 subtracts, 5 multiplies, 3 divides
        ** (adds and subtracts being nearly the same operation)
        */
        while (loops-- > 0)
        {
            for (i = 0; i < arraysize; i++)
                switch (jtable[i % 16])
                {
                    case 0: /* Add */
                        AddSubInternalFPF(0, abase[i],
                            bbase[i],
                            cbase[i]);
                        break;
                    case 1: /* Subtract */
                        AddSubInternalFPF(1, abase[i],
                            bbase[i],
                            cbase[i]);
                        break;
                    case 2: /* Multiply */
                        MultiplyInternalFPF(abase[i],
                            bbase[i],
                            cbase[i]);
                        break;
                    case 3: /* Divide */
                        DivideInternalFPF(abase[i],
                            bbase[i],
                            cbase[i]);
                        break;
                }
        }

        return (ByteMark.StopStopwatch(elapsed));
    }

    /***********************
    ** SetInternalFPFZero **
    ************************
    ** Set an internal floating-point-format number to zero.
    ** sign determines the sign of the zero.
    */
    private static void SetInternalFPFZero(InternalFPF dest,
        byte sign)
    {
        int i;          /* Index */

        dest.type = IFPF.IFPF_IS_ZERO;
        dest.sign = sign;
        dest.exp = MIN_EXP;

        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
            dest.mantissa[i] = (char)0;
        return;
    }

    /***************************
    ** SetInternalFPFInfinity **
    ****************************
    ** Set an internal floating-point-format number to infinity.
    ** This can happen if the exponent exceeds MAX_EXP.
    ** As above, sign picks the sign of infinity.
    */
    private static void SetInternalFPFInfinity(InternalFPF dest,
        byte sign)
    {
        int i;          /* Index */

        dest.type = IFPF.IFPF_IS_INFINITY;
        dest.sign = sign;
        dest.exp = MIN_EXP;

        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
            dest.mantissa[i] = (char)0;
        return;
    }

    /**********************
    ** SetInternalFPFNaN **
    ***********************
    ** Set an internal floating-point-format number to Nan
    ** (not a number).  Note that we "emulate" an 80x87 as far
    ** as the mantissa bits go.
    */
    private static void SetInternalFPFNaN(InternalFPF dest)
    {
        int i;          /* Index */

        dest.type = IFPF.IFPF_IS_NAN;
        dest.exp = MAX_EXP;
        dest.sign = 1;

        dest.mantissa[0] = (char)0x4000;
        for (i = 1; i < INTERNAL_FPF_PRECISION; i++)
            dest.mantissa[i] = (char)0;

        return;
    }

    /*******************
    ** IsMantissaZero **
    ********************
    ** Pass this routine a pointer to an internal floating point format
    ** number's mantissa.  It checks for an all-zero mantissa.
    ** Returns 0 if it is NOT all zeros, !=0 otherwise.
    */
    private static bool IsMantissaZero(char[] mant)
    {
        int i;          /* Index */
        int n;          /* Return value */

        n = 0;
        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
            n |= mant[i];

        return (n == 0);
    }

    /**************
    ** Add16Bits **
    ***************
    ** Add b, c, and carry.  Retult in a.  New carry in carry.
    */
    private static void Add16Bits(ref char carry,
        out char a,
        char b,
        char c)
    {
        int accum;              /* Accumulator */

        /*
        ** Do the work in the 32-bit accumulator so we can return
        ** the carry.
        */
        accum = b;
        accum += c;
        accum += carry;
        carry = (char)(((accum & 0x00010000) != 0) ? 1 : 0);     /* New carry */
        a = (char)(accum & 0xFFFF);       /* Result is lo 16 bits */
        return;
    }

    /**************
    ** Sub16Bits **
    ***************
    ** Additive inverse of above.
    */
    private static void Sub16Bits(ref char borrow,
        out char a,
        char b,
        char c)
    {
        int accum;              /* Accumulator */

        accum = b;
        accum -= c;
        accum -= borrow;
        borrow = (char)(((accum & 0x00010000) != 0) ? 1 : 0);    /* New borrow */
        a = (char)(accum & 0xFFFF);
        return;
    }

    /*******************
    ** ShiftMantLeft1 **
    ********************
    ** Shift a vector of 16-bit numbers left 1 bit.  Also provides
    ** a carry bit, which is shifted in at the beginning, and
    ** shifted out at the end.
    */
    private static void ShiftMantLeft1(ref char carry,
        char[] mantissa)
    {
        int i;          /* Index */
        int new_carry;
        char accum;      /* Temporary holding placed */

        for (i = INTERNAL_FPF_PRECISION - 1; i >= 0; i--)
        {
            accum = mantissa[i];
            new_carry = accum & 0x8000;       /* Get new carry */
            accum = unchecked((char)(accum << 1)); /* Do the shift */
            if (carry != 0)
                accum |= (char)1;               /* Insert previous carry */
            carry = (char)new_carry;
            mantissa[i] = accum;              /* Return shifted value */
        }
        return;
    }

    /********************
    ** ShiftMantRight1 **
    *********************
    ** Shift a mantissa right by 1 bit.  Provides carry, as
    ** above
    */
    private static void ShiftMantRight1(ref char carry,
        char[] mantissa)
    {
        int i;          /* Index */
        int new_carry;
        char accum;

        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
        {
            accum = mantissa[i];
            new_carry = accum & 1;            /* Get new carry */
            accum = (char)(accum >> 1);
            if (carry != 0)
                accum |= (char)0x8000;
            carry = (char)new_carry;
            mantissa[i] = accum;
        }
        return;
    }


    /*****************************
    ** StickyShiftMantRight **
    ******************************
    ** This is a shift right of the mantissa with a "sticky bit".
    ** I.E., if a carry of 1 is shifted out of the least significant
    ** bit, the least significant bit is set to 1.
    */
    private static void StickyShiftRightMant(InternalFPF ptr,
        int amount)
    {
        int i;          /* Index */
        char carry;      /* Self-explanatory */

        if (ptr.type != IFPF.IFPF_IS_ZERO)     /* Don't bother shifting a zero */
        {
            /*
            ** If the amount of shifting will shift everyting
            ** out of existence, then just clear the whole mantissa
            ** and set the lowmost bit to 1.
            */
            if (amount >= INTERNAL_FPF_PRECISION * 16)
            {
                for (i = 0; i < INTERNAL_FPF_PRECISION - 1; i++)
                    ptr.mantissa[i] = (char)0;
                ptr.mantissa[INTERNAL_FPF_PRECISION - 1] = (char)1;
            }
            else
                for (i = 0; i < amount; i++)
                {
                    carry = (char)0;
                    ShiftMantRight1(ref carry, ptr.mantissa);
                    if (carry != 0)
                        ptr.mantissa[INTERNAL_FPF_PRECISION - 1] |= (char)1;
                }
        }
        return;
    }


    /**************************************************
    **         POST ARITHMETIC PROCESSING            **
    **  (NORMALIZE, ROUND, OVERFLOW, AND UNDERFLOW)  **
    **************************************************/

    /**************
    ** normalize **
    ***************
    ** Normalize an internal-representation number.  Normalization
    ** discards empty most-significant bits.
    */
    private static void normalize(InternalFPF ptr)
    {
        char carry;

        /*
        ** As long as there's a highmost 0 bit, shift the significand
        ** left 1 bit.  Each time you do this, though, you've
        ** gotta decrement the exponent.
        */
        while ((ptr.mantissa[0] & 0x8000) == 0)
        {
            carry = (char)0;
            ShiftMantLeft1(ref carry, ptr.mantissa);
            ptr.exp--;
        }
        return;
    }

    /****************
    ** denormalize **
    *****************
    ** Denormalize an internal-representation number.  This means
    ** shifting it right until its exponent is equivalent to
    ** minimum_exponent. (You have to do this often in order
    ** to perform additions and subtractions).
    */
    private static void denormalize(InternalFPF ptr,
        int minimum_exponent)
    {
        int exponent_difference;

        if (IsMantissaZero(ptr.mantissa))
        {
            throw new Exception("Error:  zero significand in denormalize");
        }

        exponent_difference = ptr.exp - minimum_exponent;
        if (exponent_difference < 0)
        {
            /*
            ** The number is subnormal
            */
            exponent_difference = -exponent_difference;
            if (exponent_difference >= (INTERNAL_FPF_PRECISION * 16))
            {
                /* Underflow */
                SetInternalFPFZero(ptr, ptr.sign);
            }
            else
            {
                ptr.exp += (short)exponent_difference;
                StickyShiftRightMant(ptr, exponent_difference);
            }
        }
        return;
    }


    /*********************
    ** RoundInternalFPF **
    **********************
    ** Round an internal-representation number.
    ** The kind of rounding we do here is simplest...referred to as
    ** "chop".  "Extraneous" rightmost bits are simply hacked off.
    */
    private static
    void RoundInternalFPF(InternalFPF ptr)
    {
        /* int i; */

        if (ptr.type == IFPF.IFPF_IS_NORMAL ||
            ptr.type == IFPF.IFPF_IS_SUBNORMAL)
        {
            denormalize(ptr, MIN_EXP);
            if (ptr.type != IFPF.IFPF_IS_ZERO)
            {
                /* clear the extraneous bits */
                ptr.mantissa[3] &= (char)0xfff8;
                /*              for (i=4; i<INTERNAL_FPF_PRECISION; i++)
                {
                ptr->mantissa[i] = 0;
                }
                */
                /*
                ** Check for overflow
                */
                if (ptr.exp > MAX_EXP)
                {
                    SetInternalFPFInfinity(ptr, ptr.sign);
                }
            }
        }
        return;
    }

    /*******************************************************
    **  ARITHMETIC OPERATIONS ON INTERNAL REPRESENTATION  **
    *******************************************************/

    private static void memmove(InternalFPF dest, InternalFPF src)
    {
        dest.type = src.type;
        dest.sign = src.sign;
        dest.exp = src.exp;
        for (int i = 0; i < INTERNAL_FPF_PRECISION; i++)
        {
            dest.mantissa[i] = src.mantissa[i];
        }
    }

    /***************
    ** choose_nan **
    ****************
    ** Called by routines that are forced to perform math on
    ** a pair of NaN's.  This routine "selects" which NaN is
    ** to be returned.
    */
    private static void choose_nan(InternalFPF x,
        InternalFPF y,
        InternalFPF z,
        int intel_flag)
    {
        int i;

        /*
        ** Compare the two mantissas,
        ** return the larger.  Note that we will be emulating
        ** an 80387 in this operation.
        */
        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
        {
            if (x.mantissa[i] > y.mantissa[i])
            {
                memmove(z, x);
                return;
            }
            if (x.mantissa[i] < y.mantissa[i])
            {
                memmove(z, y);
                return;
            }
        }

        /*
        ** They are equal
        */
        if (intel_flag == 0)
            /* if the operation is addition */
            memmove(z, x);
        else
            /* if the operation is multiplication */
            memmove(z, y);
        return;
    }


    /**********************
    ** AddSubInternalFPF **
    ***********************
    ** Adding or subtracting internal-representation numbers.
    ** Internal-representation numbers pointed to by x and y are
    ** added/subtracted and the result returned in z.
    */
    private static void AddSubInternalFPF(byte operation,
        InternalFPF x,
        InternalFPF y,
        InternalFPF z)
    {
        int exponent_difference;
        char borrow;
        char carry;
        int i;
        InternalFPF locx, locy;  /* Needed since we alter them */
        /*
        ** Following big switch statement handles the
        ** various combinations of operand types.
        */
        int count = (int)IFPF.IFPF_TYPE_COUNT;
        switch ((STATE)(((int)x.type * count) + (int)y.type))
        {
            case STATE.ZERO_ZERO:
                memmove(z, x);
                if ((x.sign ^ y.sign ^ operation) != 0)
                {
                    z.sign = 0; /* positive */
                }
                break;

            case STATE.NAN_ZERO:
            case STATE.NAN_SUBNORMAL:
            case STATE.NAN_NORMAL:
            case STATE.NAN_INFINITY:
            case STATE.SUBNORMAL_ZERO:
            case STATE.NORMAL_ZERO:
            case STATE.INFINITY_ZERO:
            case STATE.INFINITY_SUBNORMAL:
            case STATE.INFINITY_NORMAL:
                memmove(z, x);
                break;


            case STATE.ZERO_NAN:
            case STATE.SUBNORMAL_NAN:
            case STATE.NORMAL_NAN:
            case STATE.INFINITY_NAN:
                memmove(z, y);
                break;

            case STATE.ZERO_SUBNORMAL:
            case STATE.ZERO_NORMAL:
            case STATE.ZERO_INFINITY:
            case STATE.SUBNORMAL_INFINITY:
            case STATE.NORMAL_INFINITY:
                memmove(z, x);
                z.sign ^= operation;
                break;

            case STATE.SUBNORMAL_SUBNORMAL:
            case STATE.SUBNORMAL_NORMAL:
            case STATE.NORMAL_SUBNORMAL:
            case STATE.NORMAL_NORMAL:
                /*
                ** Copy x and y to locals, since we may have
                ** to alter them.
                */
                locx = new InternalFPF();
                locy = new InternalFPF();
                memmove(locx, x);
                memmove(locy, y);

                /* compute sum/difference */
                exponent_difference = locx.exp - locy.exp;
                if (exponent_difference == 0)
                {
                    /*
                    ** locx.exp == locy.exp
                    ** so, no shifting required
                    */
                    if (locx.type == IFPF.IFPF_IS_SUBNORMAL ||
                        locy.type == IFPF.IFPF_IS_SUBNORMAL)
                        z.type = IFPF.IFPF_IS_SUBNORMAL;
                    else
                        z.type = IFPF.IFPF_IS_NORMAL;

                    /*
                    ** Assume that locx.mantissa > locy.mantissa
                    */
                    z.sign = locx.sign;
                    z.exp = locx.exp;
                }
                else
                    if (exponent_difference > 0)
                {
                    /*
                    ** locx.exp > locy.exp
                    */
                    StickyShiftRightMant(locy,
                        exponent_difference);
                    z.type = locx.type;
                    z.sign = locx.sign;
                    z.exp = locx.exp;
                }
                else    /* if (exponent_difference < 0) */
                {
                    /*
                    ** locx.exp < locy.exp
                    */
                    StickyShiftRightMant(locx,
                        -exponent_difference);
                    z.type = locy.type;
                    z.sign = (byte)(locy.sign ^ operation);
                    z.exp = locy.exp;
                }

                if ((locx.sign ^ locy.sign ^ operation) != 0)
                {
                    /*
                    ** Signs are different, subtract mantissas
                    */
                    borrow = (char)0;
                    for (i = (INTERNAL_FPF_PRECISION - 1); i >= 0; i--)
                        Sub16Bits(ref borrow,
                        out z.mantissa[i],
                        locx.mantissa[i],
                        locy.mantissa[i]);

                    if (borrow != 0)
                    {
                        /* The y->mantissa was larger than the
                        ** x->mantissa leaving a negative
                        ** result.  Change the result back to
                        ** an unsigned number and flip the
                        ** sign flag.
                        */
                        z.sign = (byte)(locy.sign ^ operation);
                        borrow = (char)0;
                        for (i = (INTERNAL_FPF_PRECISION - 1); i >= 0; i--)
                        {
                            Sub16Bits(ref borrow,
                                out z.mantissa[i],
                                (char)0,
                                z.mantissa[i]);
                        }
                    }
                    else
                    {
                        /* The assumption made above
                        ** (i.e. x->mantissa >= y->mantissa)
                        ** was correct.  Therefore, do nothing.
                        ** z->sign = x->sign;
                        */
                    }

                    if (IsMantissaZero(z.mantissa))
                    {
                        z.type = IFPF.IFPF_IS_ZERO;
                        z.sign = 0; /* positive */
                    }
                    else
                        if (locx.type == IFPF.IFPF_IS_NORMAL ||
                            locy.type == IFPF.IFPF_IS_NORMAL)
                    {
                        normalize(z);
                    }
                }
                else
                {
                    /* signs are the same, add mantissas */
                    carry = (char)0;
                    for (i = (INTERNAL_FPF_PRECISION - 1); i >= 0; i--)
                    {
                        Add16Bits(ref carry,
                            out z.mantissa[i],
                            locx.mantissa[i],
                            locy.mantissa[i]);
                    }

                    if (carry != 0)
                    {
                        z.exp++;
                        carry = (char)0;
                        ShiftMantRight1(ref carry, z.mantissa);
                        z.mantissa[0] |= (char)0x8000;
                        z.type = IFPF.IFPF_IS_NORMAL;
                    }
                    else
                        if ((z.mantissa[0] & 0x8000) != 0)
                        z.type = IFPF.IFPF_IS_NORMAL;
                }
                break;

            case STATE.INFINITY_INFINITY:
                SetInternalFPFNaN(z);
                break;

            case STATE.NAN_NAN:
                choose_nan(x, y, z, 1);
                break;
        }

        /*
        ** All the math is done; time to round.
        */
        RoundInternalFPF(z);
        return;
    }


    /************************
    ** MultiplyInternalFPF **
    *************************
    ** Two internal-representation numbers x and y are multiplied; the
    ** result is returned in z.
    */
    private static void MultiplyInternalFPF(
        InternalFPF x,
        InternalFPF y,
        InternalFPF z)
    {
        int i;
        int j;
        char carry;
        char[] extra_bits = new char[INTERNAL_FPF_PRECISION];
        InternalFPF locy;       /* Needed since this will be altered */
        /*
        ** As in the preceding function, this large switch
        ** statement selects among the many combinations
        ** of operands.
        */
        int count = (int)IFPF.IFPF_TYPE_COUNT;
        switch ((STATE)(((int)x.type * count) + (int)y.type))
        {
            case STATE.INFINITY_SUBNORMAL:
            case STATE.INFINITY_NORMAL:
            case STATE.INFINITY_INFINITY:
            case STATE.ZERO_ZERO:
            case STATE.ZERO_SUBNORMAL:
            case STATE.ZERO_NORMAL:
                memmove(z, x);
                z.sign ^= y.sign;
                break;

            case STATE.SUBNORMAL_INFINITY:
            case STATE.NORMAL_INFINITY:
            case STATE.SUBNORMAL_ZERO:
            case STATE.NORMAL_ZERO:
                memmove(z, y);
                z.sign ^= x.sign;
                break;

            case STATE.ZERO_INFINITY:
            case STATE.INFINITY_ZERO:
                SetInternalFPFNaN(z);
                break;

            case STATE.NAN_ZERO:
            case STATE.NAN_SUBNORMAL:
            case STATE.NAN_NORMAL:
            case STATE.NAN_INFINITY:
                memmove(z, x);
                break;

            case STATE.ZERO_NAN:
            case STATE.SUBNORMAL_NAN:
            case STATE.NORMAL_NAN:
            case STATE.INFINITY_NAN:
                memmove(z, y);
                break;


            case STATE.SUBNORMAL_SUBNORMAL:
            case STATE.SUBNORMAL_NORMAL:
            case STATE.NORMAL_SUBNORMAL:
            case STATE.NORMAL_NORMAL:
                /*
                ** Make a local copy of the y number, since we will be
                ** altering it in the process of multiplying.
                */
                locy = new InternalFPF();
                memmove(locy, y);

                /*
                ** Check for unnormal zero arguments
                */
                if (IsMantissaZero(x.mantissa) || IsMantissaZero(y.mantissa))
                {
                    SetInternalFPFInfinity(z, 0);
                }

                /*
                ** Initialize the result
                */
                if (x.type == IFPF.IFPF_IS_SUBNORMAL ||
                    y.type == IFPF.IFPF_IS_SUBNORMAL)
                    z.type = IFPF.IFPF_IS_SUBNORMAL;
                else
                    z.type = IFPF.IFPF_IS_NORMAL;

                z.sign = (byte)(x.sign ^ y.sign);
                z.exp = (short)(x.exp + y.exp);
                for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
                {
                    z.mantissa[i] = (char)0;
                    extra_bits[i] = (char)0;
                }

                for (i = 0; i < (INTERNAL_FPF_PRECISION * 16); i++)
                {
                    /*
                    ** Get rightmost bit of the multiplier
                    */
                    carry = (char)0;
                    ShiftMantRight1(ref carry, locy.mantissa);
                    if (carry != 0)
                    {
                        /*
                        ** Add the multiplicand to the product
                        */
                        carry = (char)0;
                        for (j = (INTERNAL_FPF_PRECISION - 1); j >= 0; j--)
                            Add16Bits(ref carry,
                            out z.mantissa[j],
                            z.mantissa[j],
                            x.mantissa[j]);
                    }
                    else
                    {
                        carry = (char)0;
                    }

                    /*
                    ** Shift the product right.  Overflow bits get
                    ** shifted into extra_bits.  We'll use it later
                    ** to help with the "sticky" bit.
                    */
                    ShiftMantRight1(ref carry, z.mantissa);
                    ShiftMantRight1(ref carry, extra_bits);
                }

                /*
                ** Normalize
                ** Note that we use a "special" normalization routine
                ** because we need to use the extra bits. (These are
                ** bits that may have been shifted off the bottom that
                ** we want to reclaim...if we can.
                */
                while ((z.mantissa[0] & 0x8000) == 0)
                {
                    carry = (char)0;
                    ShiftMantLeft1(ref carry, extra_bits);
                    ShiftMantLeft1(ref carry, z.mantissa);
                    z.exp--;
                }

                /*
                ** Set the sticky bit if any bits set in extra bits.
                */
                if (IsMantissaZero(extra_bits))
                {
                    z.mantissa[INTERNAL_FPF_PRECISION - 1] |= (char)1;
                }
                break;

            case STATE.NAN_NAN:
                choose_nan(x, y, z, 0);
                break;
        }

        /*
        ** All math done...do rounding.
        */
        RoundInternalFPF(z);
        return;
    }


    /**********************
    ** DivideInternalFPF **
    ***********************
    ** Divide internal FPF number x by y.  Return result in z.
    */
    private static void DivideInternalFPF(
        InternalFPF x,
        InternalFPF y,
        InternalFPF z)
    {
        int i;
        int j;
        char carry;
        char[] extra_bits = new char[INTERNAL_FPF_PRECISION];
        InternalFPF locx;       /* Local for x number */

        /*
        ** As with preceding function, the following switch
        ** statement selects among the various possible
        ** operands.
        */
        int count = (int)IFPF.IFPF_TYPE_COUNT;
        switch ((STATE)(((int)x.type * count) + (int)y.type))
        {
            case STATE.ZERO_ZERO:
            case STATE.INFINITY_INFINITY:
                SetInternalFPFNaN(z);
                break;

            case STATE.ZERO_SUBNORMAL:
            case STATE.ZERO_NORMAL:
                if (IsMantissaZero(y.mantissa))
                {
                    SetInternalFPFNaN(z);
                    break;
                }
                goto case STATE.ZERO_INFINITY;

            case STATE.ZERO_INFINITY:
            case STATE.SUBNORMAL_INFINITY:
            case STATE.NORMAL_INFINITY:
                SetInternalFPFZero(z, (byte)(x.sign ^ y.sign));
                break;

            case STATE.SUBNORMAL_ZERO:
            case STATE.NORMAL_ZERO:
                if (IsMantissaZero(x.mantissa))
                {
                    SetInternalFPFNaN(z);
                    break;
                }
                goto case STATE.INFINITY_ZERO;

            case STATE.INFINITY_ZERO:
            case STATE.INFINITY_SUBNORMAL:
            case STATE.INFINITY_NORMAL:
                SetInternalFPFInfinity(z, 0);
                z.sign = (byte)(x.sign ^ y.sign);
                break;

            case STATE.NAN_ZERO:
            case STATE.NAN_SUBNORMAL:
            case STATE.NAN_NORMAL:
            case STATE.NAN_INFINITY:
                memmove(z, x);
                break;

            case STATE.ZERO_NAN:
            case STATE.SUBNORMAL_NAN:
            case STATE.NORMAL_NAN:
            case STATE.INFINITY_NAN:
                memmove(z, y);
                break;

            case STATE.SUBNORMAL_SUBNORMAL:
            case STATE.NORMAL_SUBNORMAL:
            case STATE.SUBNORMAL_NORMAL:
            case STATE.NORMAL_NORMAL:
                /*
                ** Make local copy of x number, since we'll be
                ** altering it in the process of dividing.
                */

                locx = new InternalFPF();
                memmove(locx, x);

                /*
                ** Check for unnormal zero arguments
                */
                if (IsMantissaZero(locx.mantissa))
                {
                    if (IsMantissaZero(y.mantissa))
                        SetInternalFPFNaN(z);
                    else
                        SetInternalFPFZero(z, 0);
                    break;
                }
                if (IsMantissaZero(y.mantissa))
                {
                    SetInternalFPFInfinity(z, 0);
                    break;
                }

                /*
                ** Initialize the result
                */
                z.type = x.type;
                z.sign = (byte)(x.sign ^ y.sign);
                z.exp = (short)(x.exp - y.exp +
                    ((INTERNAL_FPF_PRECISION * 16 * 2)));
                for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
                {
                    z.mantissa[i] = (char)0;
                    extra_bits[i] = (char)0;
                }

                while ((z.mantissa[0] & 0x8000) == 0)
                {
                    carry = (char)0;
                    ShiftMantLeft1(ref carry, locx.mantissa);
                    ShiftMantLeft1(ref carry, extra_bits);

                    /*
                    ** Time to subtract yet?
                    */
                    if (carry == 0)
                        for (j = 0; j < INTERNAL_FPF_PRECISION; j++)
                        {
                            if (y.mantissa[j] > extra_bits[j])
                            {
                                carry = (char)0;
                                goto no_subtract;
                            }
                            if (y.mantissa[j] < extra_bits[j])
                                break;
                        }
                    /*
                    ** Divisor (y) <= dividend (x), subtract
                    */
                    carry = (char)0;
                    for (j = (INTERNAL_FPF_PRECISION - 1); j >= 0; j--)
                        Sub16Bits(ref carry,
                        out extra_bits[j],
                        extra_bits[j],
                        y.mantissa[j]);
                    carry = (char)1;      /* 1 shifted into quotient */
                no_subtract:
                    ShiftMantLeft1(ref carry, z.mantissa);
                    z.exp--;
                }
                break;

            case STATE.NAN_NAN:
                choose_nan(x, y, z, 0);
                break;
        }

        /*
        ** Math complete...do rounding
        */
        RoundInternalFPF(z);
    }

    /**********************
    ** LongToInternalFPF **
    ***********************
    ** Convert a signed long integer into an internal FPF number.
    */
    private static void LongToInternalFPF(
        int mylong,
        InternalFPF dest)
    {
        int i;          /* Index */
        char myword;     /* Used to hold converted stuff */
        /*
        ** Save the sign and get the absolute value.  This will help us
        ** with 64-bit machines, since we use only the lower 32
        ** bits just in case.
        */
        if (mylong < 0)
        {
            dest.sign = 1;
            mylong = 0 - mylong;
        }
        else
            dest.sign = 0;
        /*
        ** Prepare the destination floating point number
        */
        dest.type = IFPF.IFPF_IS_NORMAL;
        for (i = 0; i < INTERNAL_FPF_PRECISION; i++)
            dest.mantissa[i] = (char)0;

        /*
        ** See if we've got a zero.  If so, make the resultant FP
        ** number a true zero and go home.
        */
        if (mylong == 0)
        {
            dest.type = IFPF.IFPF_IS_ZERO;
            dest.exp = 0;
            return;
        }

        /*
        ** Not a true zero.  Set the exponent to 32 (internal FPFs have
        ** no bias) and load the low and high words into their proper
        ** locations in the mantissa.  Then normalize.  The action of
        ** normalizing slides the mantissa bits into place and sets
        ** up the exponent properly.
        */
        dest.exp = 32;
        myword = (char)((mylong >> 16) & 0xFFFFL);
        dest.mantissa[0] = myword;
        myword = (char)(mylong & 0xFFFFL);
        dest.mantissa[1] = myword;
        normalize(dest);
        return;
    }

    /************************
    ** InternalFPFToString **
    *************************
    ** FOR DEBUG PURPOSES
    ** This routine converts an internal floating point representation
    ** number to a string.  Used in debugging the package.
    ** Returns length of converted number.
    ** NOTE: dest must point to a buffer big enough to hold the
    **  result.  Also, this routine does append a null (an effect
    **  of using the sprintf() function).  It also returns
    **  a length count.
    ** NOTE: This routine returns 5 significant digits.  Thats
    **  about all I feel safe with, given the method of
    **  conversion.  It should be more than enough for programmers
    **  to determine whether the package is properly ported.
    */
    private static int InternalFPFToString(
        out string dest,
        InternalFPF src)
    {
        InternalFPF locFPFNum;          /* Local for src (will be altered) */
        InternalFPF IFPF10;             /* Floating-point 10 */
        InternalFPF IFPFComp;           /* For doing comparisons */
        int msign;                      /* Holding for mantissa sign */
        int expcount;                   /* Exponent counter */
        int ccount;                     /* Character counter */
        int i, j, k;                      /* Index */
        char carryaccum;                 /* Carry accumulator */
        char mycarry;                    /* Local for carry */

        locFPFNum = new InternalFPF();
        IFPF10 = new InternalFPF();
        IFPFComp = new InternalFPF();
        dest = "";
        /*
        ** Check first for the simple things...Nan, Infinity, Zero.
        ** If found, copy the proper string in and go home.
        */
        switch (src.type)
        {
            case IFPF.IFPF_IS_NAN:
                dest = "NaN";
                return (3);

            case IFPF.IFPF_IS_INFINITY:
                if (src.sign == 0)
                    dest = "+Inf";
                else
                    dest = "-Inf";
                return (4);

            case IFPF.IFPF_IS_ZERO:
                if (src.sign == 0)
                    dest = "+0";
                else
                    dest = "-0";
                return (2);
        }

        /*
        ** Move the internal number into our local holding area, since
        ** we'll be altering it to print it out.
        */
        memmove(locFPFNum, src);

        /*
        ** Set up a floating-point 10...which we'll use a lot in a minute.
        */
        LongToInternalFPF(10, IFPF10);

        /*
        ** Save the mantissa sign and make it positive.
        */
        msign = src.sign;
        src.sign = 0;

        expcount = 0;             /* Init exponent counter */

        /*
        ** See if the number is less than 10.  If so, multiply
        ** the number repeatedly by 10 until it's not.   For each
        ** multiplication, decrement a counter so we can keep track
        ** of the exponent.
        */
        while (true)
        {
            AddSubInternalFPF(1, locFPFNum, IFPF10, IFPFComp);
            if (IFPFComp.sign == 0)
                break;
            MultiplyInternalFPF(locFPFNum, IFPF10, IFPFComp);
            expcount--;
            memmove(locFPFNum, IFPFComp);
        }

        /*
        ** Do the reverse of the above.  As long as the number is
        ** greater than or equal to 10, divide it by 10.  Increment the
        ** exponent counter for each multiplication.
        */
        while (true)
        {
            AddSubInternalFPF(1, locFPFNum, IFPF10, IFPFComp);
            if (IFPFComp.sign != 0)
                break;
            DivideInternalFPF(locFPFNum, IFPF10, IFPFComp);
            expcount++;
            memmove(locFPFNum, IFPFComp);
        }

        /*
        ** About time to start storing things.  First, store the
        ** mantissa sign.
        */
        ccount = 1;               /* Init character counter */
        if (msign == 0)
            dest += "+";
        else
            dest += "-";

        /*
        ** At this point we know that the number is in the range
        ** 10 > n >=1.  We need to "strip digits" out of the
        ** mantissa.  We do this by treating the mantissa as
        ** an integer and multiplying by 10. (Not a floating-point
        ** 10, but an integer 10.  Since this is debug code and we
        ** could care less about speed, we'll do it the stupid
        ** way and simply add the number to itself 10 times.
        ** Anything that makes it to the left of the implied binary point
        ** gets stripped off and emitted.  We'll do this for
        ** 5 significant digits (which should be enough to
        ** verify things).
        */
        /*
        ** Re-position radix point
        */
        carryaccum = (char)0;
        while (locFPFNum.exp > 0)
        {
            mycarry = (char)0;
            ShiftMantLeft1(ref mycarry, locFPFNum.mantissa);
            carryaccum = (char)(carryaccum << 1);
            if (mycarry != 0)
                carryaccum++;
            locFPFNum.exp--;
        }

        while (locFPFNum.exp < 0)
        {
            mycarry = (char)0;
            ShiftMantRight1(ref mycarry, locFPFNum.mantissa);
            locFPFNum.exp++;
        }

        for (i = 0; i < 6; i++)
            if (i == 1)
            {       /* Emit decimal point */
                dest += ".";
                ccount++;
            }
            else
            {       /* Emit a digit */
                string s = "0"; // ((char)('0'+carryaccum)); // never gets called.
                dest += s;
                ccount++;

                carryaccum = (char)0;
                memmove(IFPF10, locFPFNum);

                /* Do multiply via repeated adds */
                for (j = 0; j < 9; j++)
                {
                    mycarry = (char)0;
                    for (k = (INTERNAL_FPF_PRECISION - 1); k >= 0; k--)
                        Add16Bits(ref mycarry, out IFPFComp.mantissa[k],
                            locFPFNum.mantissa[k],
                            IFPF10.mantissa[k]);
                    carryaccum += (char)(mycarry != 0 ? 1 : 0);
                    memmove(locFPFNum, IFPFComp);
                }
            }

        /*
        ** Now move the 'E', the exponent sign, and the exponent
        ** into the string.
        */
        dest += "E";
        dest += expcount.ToString();

        /*
        ** All done, go home.
        */
        return (dest.Length);
    }
}
