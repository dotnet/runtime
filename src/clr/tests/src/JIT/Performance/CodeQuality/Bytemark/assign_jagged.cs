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

/*************
** DoAssign **
**************
** Perform an assignment algorithm.
** The algorithm was adapted from the step by step guide found
** in "Quantitative Decision Making for Business" (Gordon,
**  Pressman, and Cohn; Prentice-Hall)
**
**
** NOTES:
** 1. Even though the algorithm distinguishes between
**    ASSIGNROWS and ASSIGNCOLS, as though the two might
**    be different, it does presume a square matrix.
**    I.E., ASSIGNROWS and ASSIGNCOLS must be the same.
**    This makes for some algorithmically-correct but
**    probably non-optimal constructs.
**
*/

using System;

public class AssignJagged : AssignStruct
{
    public override string Name()
    {
        return "ASSIGNMENT(jagged)";
    }
    public override double Run()
    {
        int[][][] arraybase;
        long accumtime;
        double iterations;

        /*
		** See if we need to do self adjustment code.
		*/
        if (this.adjust == 0)
        {
            /*
			** Self-adjustment code.  The system begins by working on 1
			** array.  If it does that in no time, then two arrays
			** are built.  This process continues until
			** enough arrays are built to handle the tolerance.
			*/
            this.numarrays = 1;
            while (true)
            {
                /*
				** Allocate space for arrays
				*/
                arraybase = new int[this.numarrays][][];
                for (int i = 0; i < this.numarrays; i++)
                {
                    arraybase[i] = new int[global.ASSIGNROWS][];
                    for (int j = 0; j < global.ASSIGNROWS; j++)
                        arraybase[i][j] = new int[global.ASSIGNCOLS];
                }

                /*
				** Do an iteration of the assignment alg.  If the
				** elapsed time is less than or equal to the permitted
				** minimum, then allocate for more arrays and
				** try again.
				*/
                if (DoAssignIteration(arraybase,
                    this.numarrays) > global.min_ticks)
                    break;          /* We're ok...exit */

                this.numarrays++;
            }
        }
        else
        {       /*
				** Allocate space for arrays
				*/
            arraybase = new int[this.numarrays][][];
            for (int i = 0; i < this.numarrays; i++)
            {
                arraybase[i] = new int[global.ASSIGNROWS][];
                for (int j = 0; j < global.ASSIGNROWS; j++)
                    arraybase[i][j] = new int[global.ASSIGNCOLS];
            }
        }

        /*
		** All's well if we get here.  Do the tests.
		*/
        accumtime = 0;
        iterations = (double)0.0;

        do
        {
            accumtime += DoAssignIteration(arraybase, this.numarrays);
            iterations += (double)1.0;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations * (double)this.numarrays
            / ByteMark.TicksToFracSecs(accumtime));
    }

    /**********************
	** DoAssignIteration **
	***********************
	** This routine executes one iteration of the assignment test.
	** It returns the number of ticks elapsed in the iteration.
	*/
    private static long DoAssignIteration(int[][][] arraybase, int numarrays)
    {
        long elapsed;                   /* Elapsed ticks */
        int i;

        /*
		** Load up the arrays with a random table.
		*/
        LoadAssignArrayWithRand(arraybase, numarrays);

        /*
		** Start the stopwatch
		*/
        elapsed = ByteMark.StartStopwatch();

        /*
		** Execute assignment algorithms
		*/
        for (i = 0; i < numarrays; i++)
        {
            Assignment(arraybase[i]);
        }

        /*
		** Get elapsed time
		*/
        return (ByteMark.StopStopwatch(elapsed));
    }

    /****************************
	** LoadAssignArrayWithRand **
	*****************************
	** Load the assignment arrays with random numbers.  All positive.
	** These numbers represent costs.
	*/
    private static void LoadAssignArrayWithRand(int[][][] arraybase, int numarrays)
    {
        int i;

        /*
		** Set up the first array.  Then just copy it into the
		** others.
		*/
        LoadAssign(arraybase[0]);
        if (numarrays > 1)
            for (i = 1; i < numarrays; i++)
            {
                CopyToAssign(arraybase[0], arraybase[i]);
            }

        return;
    }

    /***************
	** LoadAssign **
	****************
	** The array given by arraybase is loaded with positive random
	** numbers.  Elements in the array are capped at 5,000,000.
	*/
    private static void LoadAssign(int[][] arraybase)
    {
        short i, j;

        /*
		** Reset random number generator so things repeat.
		*/
        ByteMark.randnum(13);

        for (i = 0; i < global.ASSIGNROWS; i++)
            for (j = 0; j < global.ASSIGNCOLS; j++)
                arraybase[i][j] = ByteMark.abs_randwc(5000000);
        return;
    }

    /*****************
	** CopyToAssign **
	******************
	** Copy the contents of one array to another.  This is called by
	** the routine that builds the initial array, and is used to copy
	** the contents of the intial array into all following arrays.
	*/
    private static void CopyToAssign(int[][] arrayfrom,
                             int[][] arrayto)
    {
        short i, j;

        for (i = 0; i < global.ASSIGNROWS; i++)
            for (j = 0; j < global.ASSIGNCOLS; j++)
                arrayto[i][j] = arrayfrom[i][j];

        return;
    }

    /***************
	** Assignment **
	***************/
    private static void Assignment(int[][] arraybase)
    {
        short[][] assignedtableau = new short[global.ASSIGNROWS][];
        for (int z = 0; z < global.ASSIGNROWS; z++)
            assignedtableau[z] = new short[global.ASSIGNCOLS];

        /*
		** First, calculate minimum costs
		*/
        calc_minimum_costs(arraybase);

        /*
		** Repeat following until the number of rows selected
		** equals the number of rows in the tableau.
		*/
        while (first_assignments(arraybase, assignedtableau) != global.ASSIGNROWS)
        {
            second_assignments(arraybase, assignedtableau);
        }

        return;
    }

    /***********************
	** calc_minimum_costs **
	************************
	** Revise the tableau by calculating the minimum costs on a
	** row and column basis.  These minima are subtracted from
	** their rows and columns, creating a new tableau.
	*/
    private static void calc_minimum_costs(int[][] tableau)
    {
        short i, j;              /* Index variables */
        int currentmin;        /* Current minimum */
                               /*
                               ** Determine minimum costs on row basis.  This is done by
                               ** subtracting -- on a row-per-row basis -- the minum value
                               ** for that row.
                               */
        for (i = 0; i < global.ASSIGNROWS; i++)
        {
            currentmin = global.MAXPOSLONG;  /* Initialize minimum */
            for (j = 0; j < global.ASSIGNCOLS; j++)
                if (tableau[i][j] < currentmin)
                    currentmin = tableau[i][j];

            for (j = 0; j < global.ASSIGNCOLS; j++)
                tableau[i][j] -= currentmin;
        }

        /*
		** Determine minimum cost on a column basis.  This works
		** just as above, only now we step through the array
		** column-wise
		*/
        for (j = 0; j < global.ASSIGNCOLS; j++)
        {
            currentmin = global.MAXPOSLONG;  /* Initialize minimum */
            for (i = 0; i < global.ASSIGNROWS; i++)
                if (tableau[i][j] < currentmin)
                    currentmin = tableau[i][j];

            /*
			** Here, we'll take the trouble to see if the current
			** minimum is zero.  This is likely worth it, since the
			** preceding loop will have created at least one zero in
			** each row.  We can save ourselves a few iterations.
			*/
            if (currentmin != 0)
                for (i = 0; i < global.ASSIGNROWS; i++)
                    tableau[i][j] -= currentmin;
        }

        return;
    }

    /**********************
	** first_assignments **
	***********************
	** Do first assignments.
	** The assignedtableau[] array holds a set of values that
	** indicate the assignment of a value, or its elimination.
	** The values are:
	**      0 = Item is neither assigned nor eliminated.
	**      1 = Item is assigned
	**      2 = Item is eliminated
	** Returns the number of selections made.  If this equals
	** the number of rows, then an optimum has been determined.
	*/
    private static int first_assignments(int[][] tableau, short[][] assignedtableau)
    {
        short i, j, k;                   /* Index variables */
        short numassigns;              /* # of assignments */
        short totnumassigns;           /* Total # of assignments */
        short numzeros;                /* # of zeros in row */
        int selected = 0;              /* Flag used to indicate selection */

        /*
		** Clear the assignedtableau, setting all members to show that
		** no one is yet assigned, eliminated, or anything.
		*/
        for (i = 0; i < global.ASSIGNROWS; i++)
            for (j = 0; j < global.ASSIGNCOLS; j++)
                assignedtableau[i][j] = 0;

        totnumassigns = 0;
        do
        {
            numassigns = 0;
            /*
			** Step through rows.  For each one that is not currently
			** assigned, see if the row has only one zero in it.  If so,
			** mark that as an assigned row/col.  Eliminate other zeros
			** in the same column.
			*/
            for (i = 0; i < global.ASSIGNROWS; i++)
            {
                numzeros = 0;
                for (j = 0; j < global.ASSIGNCOLS; j++)
                    if (tableau[i][j] == 0)
                        if (assignedtableau[i][j] == 0)
                        {
                            numzeros++;
                            selected = j;
                        }
                if (numzeros == 1)
                {
                    numassigns++;
                    totnumassigns++;
                    assignedtableau[i][selected] = 1;
                    for (k = 0; k < global.ASSIGNROWS; k++)
                        if ((k != i) &&
                           (tableau[k][selected] == 0))
                            assignedtableau[k][selected] = 2;
                }
            }
            /*
			** Step through columns, doing same as above.  Now, be careful
			** of items in the other rows of a selected column.
			*/
            for (j = 0; j < global.ASSIGNCOLS; j++)
            {
                numzeros = 0;
                for (i = 0; i < global.ASSIGNROWS; i++)
                    if (tableau[i][j] == 0)
                        if (assignedtableau[i][j] == 0)
                        {
                            numzeros++;
                            selected = i;
                        }
                if (numzeros == 1)
                {
                    numassigns++;
                    totnumassigns++;
                    assignedtableau[selected][j] = 1;
                    for (k = 0; k < global.ASSIGNCOLS; k++)
                        if ((k != j) &&
                           (tableau[selected][k] == 0))
                            assignedtableau[selected][k] = 2;
                }
            }
            /*
			** Repeat until no more assignments to be made.
			*/
        } while (numassigns != 0);

        /*
		** See if we can leave at this point.
		*/
        if (totnumassigns == global.ASSIGNROWS) return (totnumassigns);

        /*
		** Now step through the array by row.  If you find any unassigned
		** zeros, pick the first in the row.  Eliminate all zeros from
		** that same row & column.  This occurs if there are multiple optima...
		** possibly.
		*/
        for (i = 0; i < global.ASSIGNROWS; i++)
        {
            selected = -1;
            for (j = 0; j < global.ASSIGNCOLS; j++)
                if ((tableau[i][j] == 0) &&
                   (assignedtableau[i][j] == 0))
                {
                    selected = j;
                    break;
                }
            if (selected != -1)
            {
                assignedtableau[i][selected] = 1;
                totnumassigns++;
                for (k = 0; k < global.ASSIGNCOLS; k++)
                    if ((k != selected) &&
                       (tableau[i][k] == 0))
                        assignedtableau[i][k] = 2;
                for (k = 0; k < global.ASSIGNROWS; k++)
                    if ((k != i) &&
                       (tableau[k][selected] == 0))
                        assignedtableau[k][selected] = 2;
            }
        }

        return (totnumassigns);
    }

    /***********************
	** second_assignments **
	************************
	** This section of the algorithm creates the revised
	** tableau, and is difficult to explain.  I suggest you
	** refer to the algorithm's source, mentioned in comments
	** toward the beginning of the program.
	*/
    private static void second_assignments(int[][] tableau, short[][] assignedtableau)
    {
        int i, j;                                /* Indexes */
        short[] linesrow = new short[global.ASSIGNROWS];
        short[] linescol = new short[global.ASSIGNCOLS];
        int smallest;                          /* Holds smallest value */
        short numassigns;                      /* Number of assignments */
        short newrows;                         /* New rows to be considered */
                                               /*
                                               ** Clear the linesrow and linescol arrays.
                                               */
        for (i = 0; i < global.ASSIGNROWS; i++)
            linesrow[i] = 0;
        for (i = 0; i < global.ASSIGNCOLS; i++)
            linescol[i] = 0;

        /*
		** Scan rows, flag each row that has no assignment in it.
		*/
        for (i = 0; i < global.ASSIGNROWS; i++)
        {
            numassigns = 0;
            for (j = 0; j < global.ASSIGNCOLS; j++)
                if (assignedtableau[i][j] == 1)
                {
                    numassigns++;
                    break;
                }
            if (numassigns == 0) linesrow[i] = 1;
        }

        do
        {
            newrows = 0;
            /*
			** For each row checked above, scan for any zeros.  If found,
			** check the associated column.
			*/
            for (i = 0; i < global.ASSIGNROWS; i++)
            {
                if (linesrow[i] == 1)
                    for (j = 0; j < global.ASSIGNCOLS; j++)
                        if (tableau[i][j] == 0)
                            linescol[j] = 1;
            }

            /*
			** Now scan checked columns.  If any contain assigned zeros, check
			** the associated row.
			*/
            for (j = 0; j < global.ASSIGNCOLS; j++)
                if (linescol[j] == 1)
                    for (i = 0; i < global.ASSIGNROWS; i++)
                        if ((assignedtableau[i][j] == 1) &&
                            (linesrow[i] != 1))
                        {
                            linesrow[i] = 1;
                            newrows++;
                        }
        } while (newrows != 0);

        /*
		** linesrow[n]==0 indicate rows covered by imaginary line
		** linescol[n]==1 indicate cols covered by imaginary line
		** For all cells not covered by imaginary lines, determine smallest
		** value.
		*/
        smallest = global.MAXPOSLONG;
        for (i = 0; i < global.ASSIGNROWS; i++)
            if (linesrow[i] != 0)
                for (j = 0; j < global.ASSIGNCOLS; j++)
                    if (linescol[j] != 1)
                        if (tableau[i][j] < smallest)
                            smallest = tableau[i][j];

        /*
		** Subtract smallest from all cells in the above set.
		*/
        for (i = 0; i < global.ASSIGNROWS; i++)
            if (linesrow[i] != 0)
                for (j = 0; j < global.ASSIGNCOLS; j++)
                    if (linescol[j] != 1)
                        tableau[i][j] -= smallest;

        /*
		** Add smallest to all cells covered by two lines.
		*/
        for (i = 0; i < global.ASSIGNROWS; i++)
            if (linesrow[i] == 0)
                for (j = 0; j < global.ASSIGNCOLS; j++)
                    if (linescol[j] == 1)
                        tableau[i][j] += smallest;

        return;
    }
}
