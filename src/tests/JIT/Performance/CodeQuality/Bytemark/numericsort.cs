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

/**************
** DoNumSort **
***************
** This routine performs the CPU numeric sort test.
** NOTE: Last version incorrectly stated that the routine
**  returned result in # of longword sorted per second.
**  Not so; the routine returns # of iterations per sec.
*/

using System;

// #define DEBUG

public class NumericSortJagged : SortStruct
{
    public override string Name()
    {
        return "NUMERIC SORT(jagged)";
    }

    public override double Run()
    {
        /*
        ** Set the error context string.
        */
        int[][] arraybase;     /* Base pointers of array */
        long accumtime;         /* Accumulated time */
        double iterations;      /* Iteration counter */

        /*
        ** See if we need to do self adjustment code.
        */
        if (this.adjust == 0)
        {
            /*
            ** Self-adjustment code.  The system begins by sorting 1
            ** array.  If it does that in no time, then two arrays
            ** are built and sorted.  This process continues until
            ** enough arrays are built to handle the tolerance.
            */
            this.numarrays = 1;
            while (true)
            {
                /*
                ** Allocate space for arrays
                */
                arraybase = new int[this.numarrays][];
                for (int i = 0; i < this.numarrays; i++)
                    arraybase[i] = new int[this.arraysize];

                /*
                ** Do an iteration of the numeric sort.  If the
                ** elapsed time is less than or equal to the permitted
                ** minimum, then allocate for more arrays and
                ** try again.
                */

                if (DoNumSortIteration(arraybase,
                    this.arraysize,
                    this.numarrays) > global.min_ticks)
                    break;          /* We're ok...exit */
                if (this.numarrays++ > global.NUMNUMARRAYS)
                {
                    throw new Exception("CPU:NSORT -- NUMNUMARRAYS hit.");
                }
            }
        }
        else
        {
            /*
            ** Allocate space for arrays
            */
            arraybase = new int[this.numarrays][];
            for (int i = 0; i < this.numarrays; i++)
                arraybase[i] = new int[this.arraysize];
        }

        /*
        ** All's well if we get here.  Repeatedly perform sorts until the
        ** accumulated elapsed time is greater than # of seconds requested.
        */
        accumtime = 0L;
        iterations = (double)0.0;

        do
        {
            accumtime += DoNumSortIteration(arraybase,
                this.arraysize,
                this.numarrays);
            iterations += (double)1.0;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations * (double)this.numarrays / ByteMark.TicksToFracSecs(accumtime));
    }

    /***********************
    ** DoNumSortIteration **
    ************************
    ** This routine executes one iteration of the numeric
    ** sort benchmark.  It returns the number of ticks
    ** elapsed for the iteration.
    */

    // JTR: The last 2 parms are no longer needed as they
    // can be inferred from the arraybase. <shrug>
    private static int DoNumSortIteration(int[][] arraybase, int arraysize, int numarrays)
    {
        long elapsed;          /* Elapsed ticks */
        int i;
        /*
        ** Load up the array with random numbers
        */
        LoadNumArrayWithRand(arraybase, arraysize, numarrays);

        /*
        ** Start the stopwatch
        */
        elapsed = ByteMark.StartStopwatch();

        /*
        ** Execute a heap of heapsorts
        */
        for (i = 0; i < numarrays; i++)
        {
            //          NumHeapSort(arraybase+i*arraysize,0L,arraysize-1L);
            NumHeapSort(arraybase[i], 0, arraysize - 1);
        }

        /*
        ** Get elapsed time
        */
        elapsed = ByteMark.StopStopwatch(elapsed);
#if DEBUG
        {
            for (i = 0; i < arraysize - 1; i++)
            {   /*
                ** Compare to check for proper
                ** sort.
                */
                if (arraybase[0][i + 1] < arraybase[0][i])
                {
                    Console.Write("Sort Error\n");
                    break;
                }
            }
        }
#endif

        return ((int)elapsed);
    }

    /*************************
    ** LoadNumArrayWithRand **
    **************************
    ** Load up an array with random longs.
    */
    private static void LoadNumArrayWithRand(int[][] array,     /* Pointer to arrays */
            int arraysize,
            int numarrays)         /* # of elements in array */
    {
        int i;                 /* Used for index */

        /*
        ** Initialize the random number generator
        */
        ByteMark.randnum(13);

        /*
        ** Load up first array with randoms
        */
        for (i = 0; i < arraysize; i++)
            array[0][i] = ByteMark.randnum(0);

        /*
        ** Now, if there's more than one array to load, copy the
        ** first into each of the others.
        */
        for (i = 1; i < numarrays; i++)
        {
            // the old code didn't do a memcpy, so I'm not doing
            // an Array.Copy()
            for (int j = 0; j < arraysize; j++)
                array[i][j] = array[0][j];
        }

        return;
    }

    /****************
    ** NumHeapSort **
    *****************
    ** Pass this routine a pointer to an array of long
    ** integers.  Also pass in minimum and maximum offsets.
    ** This routine performs a heap sort on that array.
    */
    private static void NumHeapSort(int[] array,
        int bottom,           /* Lower bound */
        int top)              /* Upper bound */
    {
        int temp;                     /* Used to exchange elements */
        int i;                        /* Loop index */

        /*
        ** First, build a heap in the array
        */
        for (i = (top / 2); i > 0; --i)
            NumSift(array, i, top);

        /*
        ** Repeatedly extract maximum from heap and place it at the
        ** end of the array.  When we get done, we'll have a sorted
        ** array.
        */
        for (i = top; i > 0; --i)
        {
            NumSift(array, bottom, i);
            temp = array[0];                    /* Perform exchange */
            array[0] = array[i];
            array[i] = temp;
        }
        return;
    }

    /************
    ** NumSift **
    *************
    ** Performs the shift operation on a numeric array,
    ** constructing a heap in the array.
    */
    private static void NumSift(int[] array,     /* Array of numbers */
        int i,                /* Minimum of array */
        int j)                /* Maximum of array */
    {
        int k;
        int temp;                              /* Used for exchange */

        while ((i + i) <= j)
        {
            k = i + i;
            if (k < j)
                if (array[k] < array[k + 1])
                    ++k;
            if (array[i] < array[k])
            {
                temp = array[k];
                array[k] = array[i];
                array[i] = temp;
                i = k;
            }
            else
                i = j + 1;
        }
        return;
    }
}

///////////////////////////////////////////////////////////////////////////////////////
// New class
///////////////////////////////////////////////////////////////////////////////////////
public class NumericSortRect : SortStruct
{
    public override string Name()
    {
        return "NUMERIC SORT(rectangle)";
    }

    public override double Run()
    {
        /*
        ** Set the error context string.
        */
        int[,] arraybase;        /* Base pointers of array */
        long accumtime;         /* Accumulated time */
        double iterations;      /* Iteration counter */

        /*
        ** See if we need to do self adjustment code.
        */
        if (this.adjust == 0)
        {
            /*
            ** Self-adjustment code.  The system begins by sorting 1
            ** array.  If it does that in no time, then two arrays
            ** are built and sorted.  This process continues until
            ** enough arrays are built to handle the tolerance.
            */
            this.numarrays = 1;
            while (true)
            {
                /*
                ** Allocate space for arrays
                */
                arraybase = new int[this.numarrays, this.arraysize];

                /*
                ** Do an iteration of the numeric sort.  If the
                ** elapsed time is less than or equal to the permitted
                ** minimum, then allocate for more arrays and
                ** try again.
                */
                if (DoNumSortIteration(arraybase,
                    this.arraysize,
                    this.numarrays) > global.min_ticks)
                    break;          /* We're ok...exit */
                if (this.numarrays++ > global.NUMNUMARRAYS)
                {
                    throw new Exception("CPU:NSORT -- NUMNUMARRAYS hit.");
                }
            }
        }
        else
        {
            /*
            ** Allocate space for arrays
            */
            arraybase = new int[this.numarrays, this.arraysize];
        }

        /*
        ** All's well if we get here.  Repeatedly perform sorts until the
        ** accumulated elapsed time is greater than # of seconds requested.
        */
        accumtime = 0L;
        iterations = (double)0.0;

        do
        {
            accumtime += DoNumSortIteration(arraybase,
                this.arraysize,
                this.numarrays);
            iterations += (double)1.0;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations * (double)this.numarrays / ByteMark.TicksToFracSecs(accumtime));
    }

    /***********************
    ** DoNumSortIteration **
    ************************
    ** This routine executes one iteration of the numeric
    ** sort benchmark.  It returns the number of ticks
    ** elapsed for the iteration.
    */

    // JTR: The last 2 parms are no longer needed as they
    // can be inferred from the arraybase. <shrug>
    private static int DoNumSortIteration(int[,] arraybase, int arraysize, int numarrays)
    {
        long elapsed;          /* Elapsed ticks */
        int i;
        /*
        ** Load up the array with random numbers
        */
        LoadNumArrayWithRand(arraybase, arraysize, numarrays);

        /*
        ** Start the stopwatch
        */
        elapsed = ByteMark.StartStopwatch();

        /*
        ** Execute a heap of heapsorts
        */
        for (i = 0; i < numarrays; i++)
        {
            //          NumHeapSort(arraybase+i*arraysize,0L,arraysize-1L);
            NumHeapSort(arraybase, i, arraysize - 1);
        }

        /*
        ** Get elapsed time
        */
        elapsed = ByteMark.StopStopwatch(elapsed);
#if DEBUG
        {
            for (i = 0; i < arraysize - 1; i++)
            {   /*
                ** Compare to check for proper
                ** sort.
                */
                if (arraybase[0, i + 1] < arraybase[0, i])
                {
                    Console.Write("size: {0}, count: {1}, total: {2}\n", arraysize, numarrays, arraybase.Length);
                    Console.Write("Sort Error at index {0}\n", i);
                    break;
                }
            }
        }
#endif

        return ((int)elapsed);
    }

    /*************************
    ** LoadNumArrayWithRand **
    **************************
    ** Load up an array with random longs.
    */
    private static void LoadNumArrayWithRand(int[,] array,     /* Pointer to arrays */
            int arraysize,
            int numarrays)         /* # of elements in array */
    {
        int i;                 /* Used for index */

        /*
        ** Initialize the random number generator
        */
        ByteMark.randnum(13);

        /*
        ** Load up first array with randoms
        */
        for (i = 0; i < arraysize; i++)
            array[0, i] = ByteMark.randnum(0);

        /*
        ** Now, if there's more than one array to load, copy the
        ** first into each of the others.
        */
        while (--numarrays > 0)
        {
            for (int j = 0; j < arraysize; j++, i++)
                array[numarrays, j] = array[0, j];
        }

        return;
    }

    /****************
    ** NumHeapSort **
    *****************
    ** Pass this routine a pointer to an array of long
    ** integers.  Also pass in minimum and maximum offsets.
    ** This routine performs a heap sort on that array.
    */
    private static void NumHeapSort(int[,] array,
        int row,              /* which row */
        int top)              /* Upper bound */
    {
        int temp;                     /* Used to exchange elements */
        int i;                        /* Loop index */

        /*
        ** First, build a heap in the array
        */
        for (i = (top / 2); i > 0; --i)
            NumSift(array, row, i, top);

        /*
        ** Repeatedly extract maximum from heap and place it at the
        ** end of the array.  When we get done, we'll have a sorted
        ** array.
        */
        for (i = top; i > 0; --i)
        {
            NumSift(array, row, 0, i);
            temp = array[row, 0];                    /* Perform exchange */
            array[row, 0] = array[row, i];
            array[row, i] = temp;
        }
        return;
    }

    /************
    ** NumSift **
    *************
    ** Performs the shift operation on a numeric array,
    ** constructing a heap in the array.
    */
    private static void NumSift(int[,] array,     /* Array of numbers */
        int row,
        int i,                /* Minimum of array */
        int j)                /* Maximum of array */
    {
        int k;
        int temp;                              /* Used for exchange */

        while ((i + i) <= j)
        {
            k = i + i;
            if (k < j)
                if (array[row, k] < array[row, k + 1])
                    ++k;
            if (array[row, i] < array[row, k])
            {
                temp = array[row, k];
                array[row, k] = array[row, i];
                array[row, i] = temp;
                i = k;
            }
            else
                i = j + 1;
        }
        return;
    }
}
