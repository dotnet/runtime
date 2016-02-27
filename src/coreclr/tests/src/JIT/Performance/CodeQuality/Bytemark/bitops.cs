// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
** Copyright (c) Microsoft. All rights reserved.
** Licensed under the MIT license. 
** See LICENSE file in the project root for full license information.
** 
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

/************************
** BITFIELD OPERATIONS **
*************************/

/*************
** DoBitops **
**************
** Perform the bit operations test portion of the CPU
** benchmark.  Returns the iterations per second.
*/

using System;

public class BitOps : BitOpStruct
{
    public override string Name()
    {
        return "BITFIELD";
    }

    public override double Run()
    {
        int[] bitarraybase;             /* Base of bitmap array */
        int[] bitoparraybase;           /* Base of bitmap operations array */
        int nbitops = 0;                /* # of bitfield operations */
        long accumtime;                 /* Accumulated time in ticks */
        double iterations;              /* # of iterations */

        /*
		** See if we need to run adjustment code.
		*/
        if (this.adjust == 0)
        {
            bitarraybase = new int[this.bitfieldarraysize];

            /*
			** Initialize bitfield operations array to [2,30] elements
			*/
            this.bitoparraysize = 30;

            while (true)
            {
                /*
				** Allocate space for operations array
				*/
                bitoparraybase = new int[this.bitoparraysize * 2];

                /*
				** Do an iteration of the bitmap test.  If the
				** elapsed time is less than or equal to the permitted
				** minimum, then de-allocate the array, reallocate a
				** larger version, and try again.
				*/
                if (DoBitfieldIteration(bitarraybase,
                                       bitoparraybase,
                                       this.bitoparraysize,
                                       ref nbitops) > global.min_ticks)
                    break;          /* We're ok...exit */

                this.bitoparraysize += 100;
            }
        }
        else
        {
            /*
			** Don't need to do self adjustment, just allocate
			** the array space.
			*/
            bitarraybase = new int[this.bitfieldarraysize];
            bitoparraybase = new int[this.bitoparraysize * 2];
        }

        /*
		** All's well if we get here.  Repeatedly perform bitops until the
		** accumulated elapsed time is greater than # of seconds requested.
		*/
        accumtime = 0;
        iterations = (double)0.0;

        do
        {
            accumtime += DoBitfieldIteration(bitarraybase,
                                             bitoparraybase,
                                             this.bitoparraysize,
                                             ref nbitops);
            iterations += (double)nbitops;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        /*
		** Clean up, calculate results, and go home.
		** Also, set adjustment flag to show that we don't have
		** to do self adjusting in the future.
		*/
        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations / ByteMark.TicksToFracSecs(accumtime));
    }

    /************************
	** DoBitfieldIteration **
	*************************
	** Perform a single iteration of the bitfield benchmark.
	** Return the # of ticks accumulated by the operation.
	*/
    private static long DoBitfieldIteration(int[] bitarraybase,
                                    int[] bitoparraybase,
                                    int bitoparraysize,
                                    ref int nbitops)
    {
        int i;                         /* Index */
        int bitoffset;                 /* Offset into bitmap */
        long elapsed;                  /* Time to execute */

        /*
		** Clear # bitops counter
		*/
        nbitops = 0;

        /*
		** Construct a set of bitmap offsets and run lengths.
		** The offset can be any random number from 0 to the
		** size of the bitmap (in bits).  The run length can
		** be any random number from 1 to the number of bits
		** between the offset and the end of the bitmap.
		** Note that the bitmap has 8192 * 32 bits in it.
		** (262,144 bits)
		*/
        for (i = 0; i < bitoparraysize; i++)
        {
            /* First item is offset */
            bitoparraybase[i + i] = bitoffset = ByteMark.abs_randwc(262140);

            /* Next item is run length */
            nbitops += bitoparraybase[i + i + 1] = ByteMark.abs_randwc(262140 - bitoffset);
        }

        /*
		** Array of offset and lengths built...do an iteration of
		** the test.
		** Start the stopwatch.
		*/
        elapsed = ByteMark.StartStopwatch();

        /*
		** Loop through array off offset/run length pairs.
		** Execute operation based on modulus of index.
		*/
        for (i = 0; i < bitoparraysize; i++)
        {
            switch (i % 3)
            {
                case 0: /* Set run of bits */
                    ToggleBitRun(bitarraybase,
                                 bitoparraybase[i + i],
                                 bitoparraybase[i + i + 1],
                                 1);
                    break;

                case 1: /* Clear run of bits */
                    ToggleBitRun(bitarraybase,
                                 bitoparraybase[i + i],
                                 bitoparraybase[i + i + 1],
                                 0);
                    break;

                case 2: /* Complement run of bits */
                    FlipBitRun(bitarraybase,
                               bitoparraybase[i + i],
                               bitoparraybase[i + i + 1]);
                    break;
            }
        }

        /*
		** Return elapsed time
		*/
        return (ByteMark.StopStopwatch(elapsed));
    }


    /*****************************
	**     ToggleBitRun          *
	******************************
	** Set or clear a run of nbits starting at
	** bit_addr in bitmap.
	*/
    private static void ToggleBitRun(int[] bitmap,         /* Bitmap */
                             int bit_addr,         /* Address of bits to set */
                             int nbits,            /* # of bits to set/clr */
                             int val)              /* 1 or 0 */
    {
        int bindex;   /* Index into array */
        int bitnumb;  /* Bit number */

        while (nbits-- > 0)
        {
#if LONG64
			bindex=bit_addr>>>6;     /* Index is number /64 */
			bindex=bit_addr % 64;    /* Bit number in word */
#else
            bindex = (int)((uint)bit_addr) >> 5;     /* Index is number /32 */
            bitnumb = bit_addr % 32;   /* bit number in word */
#endif

            if (val != 0)
                bitmap[bindex] |= (1 << bitnumb);
            else
                bitmap[bindex] &= ~(1 << bitnumb);
            bit_addr++;
        }
        return;
    }

    /***************
	** FlipBitRun **
	****************
	** Complements a run of bits.
	*/
    private static void FlipBitRun(int[] bitmap,            /* Bit map */
                           int bit_addr,            /* Bit address */
                           int nbits)               /* # of bits to flip */
    {
        int bindex;   /* Index into array */
        int bitnumb;  /* Bit number */

        while (nbits-- > 0)
        {
#if LONG64
			bindex=bit_addr>>6;     /* Index is number /64 */
			bitnumb=bit_addr % 32;	 /* Bit number in longword */
#else
            bindex = bit_addr >> 5;     /* Index is number /32 */
            bitnumb = bit_addr % 32;   /* Bit number in longword */
#endif
            bitmap[bindex] ^= (1 << bitnumb);
            bit_addr++;
        }

        return;
    }
}
