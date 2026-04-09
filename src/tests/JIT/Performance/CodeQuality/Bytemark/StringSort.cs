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
**
*/

using System;
using System.Text;

/********************
** STRING HEAPSORT **
********************/

/*****************
** DoStringSort **
******************
** This routine performs the CPU string sort test.
** Arguments:
**      requested_secs = # of seconds to execute test
**      stringspersec = # of strings per second sorted (RETURNED)
*/

internal static class StringOrdinalComparer
{
    public static int Compare(String left, String right)
    {
        return String.CompareOrdinal(left, right);
    }
}

public class StringSort : StringSortStruct
{
    public override string Name()
    {
        return "STRING SORT";
    }
    public override double Run()
    {
        string[][] arraybase;   /* Base pointers of array */
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
                arraybase = new string[this.numarrays][];
                for (int i = 0; i < this.numarrays; i++)
                    arraybase[i] = new string[this.arraysize];

                /*
                ** Do an iteration of the string sort.  If the
                ** elapsed time is less than or equal to the permitted
                ** minimum, then allocate for more arrays and
                ** try again.
                */
                if (DoStringSortIteration(arraybase,
                                            this.numarrays,
                                            this.arraysize) > global.min_ticks)
                    break;          /* We're ok...exit */

                if (this.numarrays++ > global.NUMSTRARRAYS)
                {
                    throw new Exception("CPU:SSORT -- NUMSTRARRAYS hit.");
                }
            }
        }
        else
        {
            /*
            ** Allocate space for arrays
            */
            arraybase = new string[this.numarrays][];
            for (int i = 0; i < this.numarrays; i++)
                arraybase[i] = new string[this.arraysize];
        }

        /*
        ** All's well if we get here.  Repeatedly perform sorts until the
        ** accumulated elapsed time is greater than # of seconds requested.
        */
        accumtime = 0L;
        iterations = (double)0.0;

        do
        {
            accumtime += DoStringSortIteration(arraybase,
                this.numarrays,
                this.arraysize);
            iterations += (double)this.numarrays;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0)
            this.adjust = 1;

        /*
        ** Clean up, calculate results, and go home.
        ** Set flag to show we don't need to rerun adjustment code.
        */

        return (iterations * (double)this.numarrays / ByteMark.TicksToFracSecs(accumtime));
    }

    /**************************
    ** DoStringSortIteration **
    ***************************
    ** This routine executes one iteration of the string
    ** sort benchmark.  It returns the number of ticks
    ** Note that this routine also builds the offset pointer
    ** array.
    */

    private static int DoStringSortIteration(string[][] arraybase, int numarrays, int arraysize)
    {
        long elapsed;            /* Elapsed ticks */
        int i;

        /*
        ** Load up the array(s) with random numbers
        */
        LoadStringArray(arraybase, arraysize, numarrays);

        /*
        ** Start the stopwatch
        */
        elapsed = ByteMark.StartStopwatch();

        /*
        ** Execute heapsorts
        */
        for (i = 0; i < numarrays; i++)
        {
            // StrHeapSort(tempobase,tempsbase,nstrings,0L,nstrings-1);
            StrHeapSort(arraybase[i], 0, arraysize - 1);
        }

        /*
        ** Record elapsed time
        */
        elapsed = ByteMark.StopStopwatch(elapsed);

#if DEBUG
        for (i = 0; i < arraysize - 1; i++)
        {
            /*
            ** Compare strings to check for proper
            ** sort.
            */
            if (StringOrdinalComparer.Compare(arraybase[0][i + 1], arraybase[0][i]) < 0)
            {
                Console.Write("Error in StringSort!  arraybase[0][{0}]='{1}', arraybase[0][{2}]='{3}\n", i, arraybase[0][i], i + 1, arraybase[0][i + 1]);
                break;
            }
        }
#endif

        return ((int)elapsed);
    }


    /********************
    ** LoadStringArray **
    *********************
    ** Initialize the string array with random strings of
    ** varying sizes.
    ** Returns the pointer to the offset pointer array.
    ** Note that since we're creating a number of arrays, this
    ** routine builds one array, then copies it into the others.
    */
    private static void LoadStringArray(string[][] array,          /* String array */
                                    int arraysize,                  /* Size of array */
                                    int numarrays)                  /* # of arrays */
    {
        /*
        ** Initialize random number generator.
        */
        ByteMark.randnum(13);

        /*
        ** Load up the first array with randoms
        */

        int i;
        for (i = 0; i < arraysize; i++)
        {
            int length;

            length = 4 + ByteMark.abs_randwc(76);
            array[0][i] = "";

            /*
            ** Fill up the string with random bytes.
            */
            StringBuilder builder = new StringBuilder(length);

            int add;
            for (add = 0; add < length; add++)
            {
                char myChar = (char)(ByteMark.abs_randwc(96) + 32);
                builder.Append(myChar);
            }
            array[0][i] = builder.ToString();
        }

        /*
        ** We now have initialized a single full array.  If there
        ** is more than one array, copy the original into the
        ** others.
        */
        int k;
        for (k = 1; k < numarrays; k++)
        {
            for (i = 0; i < arraysize; i++)
            {
                array[k][i] = array[0][i];
            }
        }
    }


    /****************
    ** strheapsort **
    *****************
    ** Pass this routine a pointer to an array of unsigned char.
    ** The array is presumed to hold strings occupying at most
    ** 80 bytes (counts a byte count).
    ** This routine also needs a pointer to an array of offsets
    ** which represent string locations in the array, and
    ** an unsigned long indicating the number of strings
    ** in the array.
    */
    private static void StrHeapSort(string[] array,
                                int bottom,             /* lower bound */
                                int top)                /* upper bound */
    {
        int i;
        string temp;

        /*
        ** Build a heap in the array
        */
        for (i = (top / 2); i > 0; --i)
            strsift(array, i, top);

        /*
        ** Repeatedly extract maximum from heap and place it at the
        ** end of the array.  When we get done, we'll have a sorted
        ** array.
        */
        for (i = top; i > 0; --i)
        {
            strsift(array, bottom, i);
            temp = array[0];
            array[0] = array[i];            /* perform exchange */
            array[i] = temp;
        }
        return;
    }


    /************
    ** strsift **
    *************
    ** Pass this function:
    **      1) A pointer to an array of offset pointers
    **      2) A pointer to a string array
    **      3) The number of elements in the string array
    **      4) Offset within which to sort.
    ** Sift the array within the bounds of those offsets (thus
    ** building a heap).
    */
    private static void strsift(string[] array,
                        int i,
                        int j)
    {
        int k;
        string temp;

        while ((i + i) <= j)
        {
            k = i + i;
            if (k < j)
            {
                //array[k].CompareTo(array[k+1]);
                if (StringOrdinalComparer.Compare(array[k], array[k + 1]) < 0)
                    ++k;
            }

            //if(array[i]<array[k])
            if (StringOrdinalComparer.Compare(array[i], array[k]) < 0)
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

