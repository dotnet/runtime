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

/********************
** IDEA Encryption **
*********************
** IDEA - International Data Encryption Algorithm.
** Based on code presented in Applied Cryptography by Bruce Schneier.
** Which was based on code developed by Xuejia Lai and James L. Massey.
** Other modifications made by Colin Plumb.
**
*/

/***********
** DoIDEA **
************
** Perform IDEA encryption.  Note that we time encryption & decryption
** time as being a single loop.
*/

using System;

public class IDEAEncryption : IDEAStruct
{
    public override string Name()
    {
        return "IDEA";
    }

    public override double Run()
    {
        int i;
        char[] Z = new char[global.KEYLEN];
        char[] DK = new char[global.KEYLEN];
        char[] userkey = new char[8];
        long accumtime;
        double iterations;
        byte[] plain1;               /* First plaintext buffer */
        byte[] crypt1;               /* Encryption buffer */
        byte[] plain2;               /* Second plaintext buffer */

        /*
		** Re-init random-number generator.
		*/
        ByteMark.randnum(3);

        /*
		** Build an encryption/decryption key
		*/
        for (i = 0; i < 8; i++)
            userkey[i] = (char)(ByteMark.abs_randwc(60000) & 0xFFFF);
        for (i = 0; i < global.KEYLEN; i++)
            Z[i] = (char)0;

        /*
		** Compute encryption/decryption subkeys
		*/
        en_key_idea(userkey, Z);
        de_key_idea(Z, DK);

        /*
		** Allocate memory for buffers.  We'll make 3, called plain1,
		** crypt1, and plain2.  It works like this:
		**   plain1 >>encrypt>> crypt1 >>decrypt>> plain2.
		** So, plain1 and plain2 should match.
		** Also, fill up plain1 with sample text.
		*/
        plain1 = new byte[this.arraysize];
        crypt1 = new byte[this.arraysize];
        plain2 = new byte[this.arraysize];

        /*
		** Note that we build the "plaintext" by simply loading
		** the array up with random numbers.
		*/
        for (i = 0; i < this.arraysize; i++)
            plain1[i] = (byte)(ByteMark.abs_randwc(255) & 0xFF);

        /*
		** See if we need to perform self adjustment loop.
		*/
        if (this.adjust == 0)
        {
            /*
			** Do self-adjustment.  This involves initializing the
			** # of loops and increasing the loop count until we
			** get a number of loops that we can use.
			*/
            for (this.loops = 100;
                 this.loops < global.MAXIDEALOOPS;
                 this.loops += 10)
                if (DoIDEAIteration(plain1, crypt1, plain2,
                                    this.arraysize,
                                    this.loops,
                                    Z, DK) > global.min_ticks)
                    break;
        }

        /*
		** All's well if we get here.  Do the test.
		*/
        accumtime = 0;
        iterations = (double)0.0;

        do
        {
            accumtime += DoIDEAIteration(plain1, crypt1, plain2,
                                         this.arraysize,
                                         this.loops, Z, DK);
            iterations += (double)this.loops;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        /*
		** Clean up, calculate results, and go home.  Be sure to
		** show that we don't have to rerun adjustment code.
		*/

        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations / ByteMark.TicksToFracSecs(accumtime));
    }

    /********************
	** DoIDEAIteration **
	*********************
	** Execute a single iteration of the IDEA encryption algorithm.
	** Actually, a single iteration is one encryption and one
	** decryption.
	*/
    private static long DoIDEAIteration(byte[] plain1,
                                 byte[] crypt1,
                               byte[] plain2,
                               int arraysize,
                               int nloops,
                               char[] Z,
                               char[] DK)
    {
        int i;
        int j;
        long elapsed;

        /*
		** Start the stopwatch.
		*/
        elapsed = ByteMark.StartStopwatch();

        /*
		** Do everything for nloops.
		*/

        for (i = 0; i < nloops; i++)
        {
            for (j = 0; j < arraysize; j += 8)
                cipher_idea(plain1, crypt1, j, Z);  /* Encrypt */

            for (j = 0; j < arraysize; j += 8)
                cipher_idea(crypt1, plain2, j, DK);  /* Decrypt */
        }

        // Validate output
        for (j = 0; j < arraysize; j++)
            if (plain1[j] != plain2[j])
            {
                string error = String.Format("IDEA: error at index {0} ({1} <> {2})!", j, (int)plain1[j], (int)plain2[j]);
                throw new Exception(error);
            }

        /*
		** Get elapsed time.
		*/
        return (ByteMark.StopStopwatch(elapsed));
    }

    /********
	** mul **
	*********
	** Performs multiplication, modulo (2**16)+1.  This code is structured
	** on the assumption that untaken branches are cheaper than taken
	** branches, and that the compiler doesn't schedule branches.
	*/
    private static char mul(char a, char b)
    {
        int p;
        if (a != 0)
        {
            if (b != 0)
            {
                p = unchecked((int)(a * b));
                b = low16(p);
                a = unchecked((char)(p >> 16));
                return unchecked((char)(b - a + (b < a ? 1 : 0)));
            }
            else
                return unchecked((char)(1 - a));
        }
        else
            return unchecked((char)(1 - b));
    }

    /********
	** inv **
	*********
	** Compute multiplicative inverse of x, modulo (2**16)+1
	** using Euclid's GCD algorithm.  It is unrolled twice
	** to avoid swapping the meaning of the registers.  And
	** some subtracts are changed to adds.
	*/
    private static char inv(char x)
    {
        char t0, t1;
        char q, y;

        if (x <= 1)
            return (x);                 /* 0 and 1 are self-inverse */

        t1 = (char)(0x10001 / x);
        y = (char)(0x10001 % x);

        if (y == 1)
            return (low16(1 - t1));

        t0 = (char)1;

        do
        {
            q = (char)(x / y);
            x = (char)(x % y);
            t0 += (char)(q * t1);
            if (x == 1)
                return (t0);
            q = (char)(y / x);
            y = (char)(y % x);
            t1 += (char)(q * t0);
        } while (y != 1);
        return (low16(1 - t1));
    }

    /****************
	** en_key_idea **
	*****************
	** Compute IDEA encryption subkeys Z
	*/
    private static void en_key_idea(char[] userkey, char[] Z)
    {
        int i, j;

        // NOTE: The temp variables (tmp,idx) were not in original C code.
        //	     It may affect numbers a bit.
        int tmp = 0;
        int idx = 0;

        /*
		** shifts
		*/
        for (j = 0; j < 8; j++)
            Z[j + idx] = userkey[tmp++];
        for (i = 0; j < global.KEYLEN; j++)
        {
            i++;
            Z[i + 7 + idx] = unchecked((char)((Z[(i & 7) + idx] << 9) | (Z[((i + 1) & 7) + idx] >> 7)));
            idx += (i & 8);
            i &= 7;
        }
        return;
    }

    /****************
	** de_key_idea **
	*****************
	** Compute IDEA decryption subkeys DK from encryption
	** subkeys Z.
	*/
    private static void de_key_idea(char[] Z, char[] DK)
    {
        char[] TT = new char[global.KEYLEN];
        int j;
        char t1, t2, t3;

        short p = (short)global.KEYLEN;

        // NOTE:  Another local variable was needed here but was not in original C.
        //		  May affect benchmark numbers.
        int tmpZ = 0;

        t1 = inv(Z[tmpZ++]);
        t2 = unchecked((char)(-Z[tmpZ++]));
        t3 = unchecked((char)(-Z[tmpZ++]));
        TT[--p] = inv(Z[tmpZ++]);
        TT[--p] = t3;
        TT[--p] = t2;
        TT[--p] = t1;

        for (j = 1; j < global.ROUNDS; j++)
        {
            t1 = Z[tmpZ++];
            TT[--p] = Z[tmpZ++];
            TT[--p] = t1;
            t1 = inv(Z[tmpZ++]);
            t2 = unchecked((char)(-Z[tmpZ++]));
            t3 = unchecked((char)(-Z[tmpZ++]));
            TT[--p] = inv(Z[tmpZ++]);
            TT[--p] = t2;
            TT[--p] = t3;
            TT[--p] = t1;
        }

        t1 = Z[tmpZ++];
        TT[--p] = Z[tmpZ++];
        TT[--p] = t1;
        t1 = inv(Z[tmpZ++]);
        t2 = unchecked((char)(-Z[tmpZ++]));
        t3 = unchecked((char)(-Z[tmpZ++]));
        TT[--p] = inv(Z[tmpZ++]);
        TT[--p] = t3;
        TT[--p] = t2;
        TT[--p] = t1;

        /*
		** Copy and destroy temp copy
		*/
        for (j = 0, p = 0; j < global.KEYLEN; j++)
        {
            DK[j] = TT[p];
            TT[p++] = (char)0;
        }

        return;
    }

    /*
	** MUL(x,y)
	** This #define creates a macro that computes x=x*y modulo 0x10001.
	** Requires temps t16 and t32.  Also requires y to be strictly 16
	** bits.  Here, I am using the simplest form.  May not be the
	** fastest. -- RG
	*/
    /* #define MUL(x,y) (x=mul(low16(x),y)) */

    /****************
	** cipher_idea **
	*****************
	** IDEA encryption/decryption algorithm.
	*/

    // NOTE: args in and out were renamed because in/out are reserved words
    //		 in cool.

    private static void cipher_idea(byte[] xin, byte[] xout, int offset, char[] Z)
    {
        char x1, x2, x3, x4, t1, t2;
        int r = global.ROUNDS;

        // NOTE:  More local variables (AND AN ARG) were required by this
        //		  function.  The original C code did not need/have these.
        int offset2 = offset;
        int idx = 0;

        // NOTE:  Because of big endian (and lack of pointers) I had to
        //		  force two bytes into the chars instead of how original
        //		  c code did it.
        unchecked
        {
            x1 = (char)((xin[offset]) | (xin[offset + 1] << 8));
            x2 = (char)((xin[offset + 2]) | (xin[offset + 3] << 8));
            x3 = (char)((xin[offset + 4]) | (xin[offset + 5] << 8));
            x4 = (char)((xin[offset + 6]) | (xin[offset + 7] << 8));

            do
            {
                MUL(ref x1, Z[idx++]);
                x2 += Z[idx++];
                x3 += Z[idx++];
                MUL(ref x4, Z[idx++]);

                t2 = (char)(x1 ^ x3);
                MUL(ref t2, Z[idx++]);
                t1 = (char)(t2 + (x2 ^ x4));
                MUL(ref t1, Z[idx++]);
                t2 = (char)(t1 + t2);

                x1 ^= t1;
                x4 ^= t2;

                t2 ^= x2;
                x2 = (char)(x3 ^ t1);
                x3 = t2;
            } while ((--r) != 0);

            MUL(ref x1, Z[idx++]);
            xout[offset2] = (byte)(x1 & 0x00ff);
            xout[offset2 + 1] = (byte)((x1 >> 8) & 0x00ff);
            xout[offset2 + 2] = (byte)((x3 + Z[idx]) & 0x00ff);
            xout[offset2 + 3] = (byte)(((x3 + Z[idx++]) >> 8) & 0x00ff);
            xout[offset2 + 4] = (byte)((x2 + Z[idx]) & 0x00ff);
            xout[offset2 + 5] = (byte)(((x2 + Z[idx++]) >> 8) & 0x00ff);
            MUL(ref x4, Z[idx]);
            xout[offset2 + 6] = (byte)(x4 & 0x00ff);
            xout[offset2 + 7] = (byte)((x4 >> 8) & 0x00ff);
        }
        return;
    }

    // These were macros in the original C code

    /* #define low16(x) ((x) & 0x0FFFF) */
    private static char low16(int x)
    {
        return (char)((x) & 0x0FFFF);
    }

    /* #define MUL(x,y) (x=mul(low16(x),y)) */
    private static void MUL(ref char x, char y)
    {
        x = mul(low16(x), y);
    }
}
