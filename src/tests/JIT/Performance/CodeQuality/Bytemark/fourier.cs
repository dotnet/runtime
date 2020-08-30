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

public class Fourier : FourierStruct
{
    public override string Name()
    {
        return "FOURIER";
    }

    /* 
    ** Perform the transcendental/trigonometric portion of the
    ** benchmark.  This benchmark calculates the first n
    ** fourier coefficients of the function (x+1)^x defined
    ** on the interval 0,2.
    */
    public override double Run()
    {
        double[] abase;         /* Base of A[] coefficients array */
        double[] bbase;         /* Base of B[] coefficients array */
        long accumtime;         /* Accumulated time in ticks */
        double iterations;      /* # of iterations */

        /*
        ** See if we need to do self-adjustment code.
        */
        if (this.adjust == 0)
        {
            this.arraysize = 100; /* Start at 100 elements */
            while (true)
            {
                abase = new double[this.arraysize];
                bbase = new double[this.arraysize];
                /*
                ** Do an iteration of the tests.  If the elapsed time is
                ** less than or equal to the permitted minimum, re-allocate
                ** larger arrays and try again.
                */
                if (DoFPUTransIteration(abase, bbase, this.arraysize) > global.min_ticks) break;
                this.arraysize += 50;
            }
        }
        else
        {
            /*
            ** Don't need self-adjustment.  Just allocate the
            ** arrays, and go.
            */
            abase = new double[this.arraysize];
            bbase = new double[this.arraysize];
        }

        accumtime = 0L;
        iterations = 0.0;
        do
        {
            accumtime += DoFPUTransIteration(abase, bbase, this.arraysize);
            iterations += (double)this.arraysize * (double)2.0 - (double)1.0;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0)
            this.adjust = 1;

        return iterations / (double)ByteMark.TicksToFracSecs(accumtime);
    }

    /*
    ** Perform an iteration of the FPU Transcendental/trigonometric
    ** benchmark.  Here, an iteration consists of calculating the
    ** first n fourier coefficients of the function (x+1)^x on
    ** the interval 0,2.  n is given by arraysize.
    ** NOTE: The # of integration steps is fixed at
    ** 200.
    */
    private static long DoFPUTransIteration(double[] abase, double[] bbase, int arraysize)
    {
        double omega;   /* Fundamental frequency */
        int i;      /* Index */
        long elapsed;   /* Elapsed time */

        elapsed = ByteMark.StartStopwatch();

        /*
        ** Calculate the fourier series.  Begin by
        ** calculating A[0].
        */

        abase[0] = TrapezoidIntegrate(0.0, 2.0, 200, 0.0, 0) / 2.0;

        /*
        ** Calculate the fundamental frequency.
        ** ( 2 * pi ) / period...and since the period
        ** is 2, omega is simply pi.
        */
        omega = 3.1415926535897921;

        for (i = 1; i < arraysize; i++)
        {
            /*
            ** Calculate A[i] terms.  Note, once again, that we
            ** can ignore the 2/period term outside the integral
            ** since the period is 2 and the term cancels itself
            ** out.
            */
            abase[i] = TrapezoidIntegrate(0.0, 2.0, 200, omega * (double)i, 1);

            bbase[i] = TrapezoidIntegrate(0.0, 2.0, 200, omega * (double)i, 2);
        }
        /*
        ** All done, stop the stopwatch
        */
        return (ByteMark.StopStopwatch(elapsed));
    }

    /*
    ** Perform a simple trapezoid integration on the
    ** function (x+1)**x.
    ** x0,x1 set the lower and upper bounds of the
    ** integration.
    ** nsteps indicates # of trapezoidal sections
    ** omegan is the fundamental frequency times
    **  the series member #
    ** select = 0 for the A[0] term, 1 for cosine terms, and
    **   2 for sine terms.
    ** Returns the value.
    */
    private static double TrapezoidIntegrate(double x0, double x1, int nsteps, double omegan, int select)
    {
        double x;       /* Independent variable */
        double dx;      /* Stepsize */
        double rvalue;          /* Return value */

        /*
        ** Initialize independent variable
        */
        x = x0;

        /*
        ** Calculate stepsize
        */
        dx = (x1 - x0) / (double)nsteps;

        /*
        ** Initialize the return value.
        */
        rvalue = thefunction(x0, omegan, select) / (double)2.0;

        /*
        ** Compute the other terms of the integral.
        */
        if (nsteps != 1)
        {
            --nsteps;               /* Already done 1 step */
            while (--nsteps != 0)
            {
                x += dx;
                rvalue += thefunction(x, omegan, select);
            }
        }
        /*
        ** Finish computation
        */
        rvalue = (rvalue + thefunction(x1, omegan, select) / (double)2.0) * dx;

        return (rvalue);
    }

    /*
    ** This routine selects the function to be used
    ** in the Trapezoid integration.
    ** x is the independent variable
    ** omegan is omega * n
    ** select chooses which of the sine/cosine functions
    **  are used.  note the special case for select=0.
    */
    private static double thefunction(double x, double omegan, int select)
    {
        /*
        ** Use select to pick which function we call.
        */
        switch (select)
        {
            case 0: return Math.Pow(x + (double)1.0, x);
            case 1: return Math.Pow(x + (double)1.0, x) * Math.Cos(omegan * x);
            case 2: return Math.Pow(x + (double)1.0, x) * Math.Sin(omegan * x);
        }

        /*
        ** We should never reach this point, but the following
        ** keeps compilers from issuing a warning message.
        */
        return (0.0);
    }
}
