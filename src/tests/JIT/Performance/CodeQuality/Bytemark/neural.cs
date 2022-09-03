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

/********************************
** BACK PROPAGATION NEURAL NET **
*********************************
** This code is a modified version of the code
** that was submitted to BYTE Magazine by
** Maureen Caudill.  It accomanied an article
** that I CANNOT NOW RECALL.
** The author's original heading/comment was
** as follows:
**
**  Backpropagation Network
**  Written by Maureen Caudill
**  in Think C 4.0 on a Macintosh
**
**  (c) Maureen Caudill 1988-1991
**  This network will accept 5x7 input patterns
**  and produce 8 bit output patterns.
**  The source code may be copied or modified without restriction,
**  but no fee may be charged for its use.
**
** ++++++++++++++
** I have modified the code so that it will work
** on systems other than a Macintosh -- RG
*/

/***********
** DoNNet **
************
** Perform the neural net benchmark.
** Note that this benchmark is one of the few that
** requires an input file.  That file is "NNET.DAT" and
** should be on the local directory (from which the
** benchmark program in launched).
*/

using System;
using System.IO;

public class Neural : NNetStruct
{
    public override string Name()
    {
        return "NEURAL NET(rectangle)";
    }

    /*
    ** DEFINES
    */
    public static int T = 1;            /* TRUE */
    public static int F = 0;            /* FALSE */
    public static int ERR = -1;
    public static int MAXPATS = 10;     /* max number of patterns in data file */
    public static int IN_X_SIZE = 5;    /* number of neurodes/row of input layer */
    public static int IN_Y_SIZE = 7;    /* number of neurodes/col of input layer */
    public static int IN_SIZE = 35;     /* equals IN_X_SIZE*IN_Y_SIZE */
    public static int MID_SIZE = 8;     /* number of neurodes in middle layer */
    public static int OUT_SIZE = 8;     /* number of neurodes in output layer */
    public static double MARGIN = 0.1;  /* how near to 1,0 do we have to come to stop? */
    public static double BETA = 0.09;   /* beta learning constant */
    public static double ALPHA = 0.09;  /* momentum term constant */
    public static double STOP = 0.1;    /* when worst_error less than STOP, training is done */

    /*
    **  MAXNNETLOOPS
    **
    ** This constant sets the max number of loops through the neural
    ** net that the system will attempt before giving up.  This
    ** is not a critical constant.  You can alter it if your system
    ** has sufficient horsepower.
    */
    public static int MAXNNETLOOPS = 50000;


    /*
    ** GLOBALS
    */
    public static double[,] mid_wts = new double[MID_SIZE, IN_SIZE];     /* middle layer weights */
    public static double[,] out_wts = new double[OUT_SIZE, MID_SIZE];
    public static double[] mid_out = new double[MID_SIZE];
    public static double[] out_out = new double[OUT_SIZE];
    public static double[] mid_error = new double[MID_SIZE];
    public static double[] out_error = new double[OUT_SIZE];
    public static double[,] mid_wt_change = new double[MID_SIZE, IN_SIZE];
    public static double[,] out_wt_change = new double[OUT_SIZE, MID_SIZE];
    public static double[,] in_pats = new double[MAXPATS, IN_SIZE];
    public static double[,] out_pats = new double[MAXPATS, OUT_SIZE];
    public static double[] tot_out_error = new double[MAXPATS];
    public static double[,] out_wt_cum_change = new double[OUT_SIZE, MID_SIZE];
    public static double[,] mid_wt_cum_change = new double[MID_SIZE, IN_SIZE];
    public static double worst_error = 0.0; /* worst error each pass through the data */
    public static double average_error = 0.0; /* average error each pass through the data */
    public static double[] avg_out_error = new double[MAXPATS];

    public static int iteration_count = 0;    /* number of passes thru network so far */
    public static int numpats = 0;            /* number of patterns in data file */
    public static int numpasses = 0;          /* number of training passes through data file */
    public static int learned = 0;            /* flag--if TRUE, network has learned all patterns */

    /*
    ** The Neural Net test requires an input data file.
    ** The name is specified here.
    */
    public static string inpath = "NNET.DAT";

    public override
    double Run()
    {
        return DoNNET(this);
    }

    /*********************
    ** read_data_file() **
    **********************
    ** Read in the input data file and store the patterns in
    ** in_pats and out_pats.
    ** The format for the data file is as follows:
    **
    ** line#   data expected
    ** -----   ------------------------------
    ** 1               In-X-size,in-y-size,out-size
    ** 2               number of patterns in file
    ** 3               1st X row of 1st input pattern
    ** 4..             following rows of 1st input pattern pattern
    **                 in-x+2  y-out pattern
    **                                 1st X row of 2nd pattern
    **                 etc.
    **
    ** Each row of data is separated by commas or spaces.
    ** The data is expected to be ascii text corresponding to
    ** either a +1 or a 0.
    **
    ** Sample input for a 1-pattern file (The comments to the
    ** right may NOT be in the file unless more sophisticated
    ** parsing of the input is done.):
    **
    ** 5,7,8                      input is 5x7 grid, output is 8 bits
    ** 1                          one pattern in file
    ** 0,1,1,1,0                  beginning of pattern for "O"
    ** 1,0,0,0,1
    ** 1,0,0,0,1
    ** 1,0,0,0,1
    ** 1,0,0,0,1
    ** 1,0,0,0,0
    ** 0,1,1,1,0
    ** 0,1,0,0,1,1,1,1            ASCII code for "O" -- 0100 1111
    **
    ** Clearly, this simple scheme can be expanded or enhanced
    ** any way you like.
    **
    ** Returns -1 if any file error occurred, otherwise 0.
    **/
    private
    void read_data_file()
    {
        int xinsize = 0, yinsize = 0, youtsize = 0;
        int patt = 0, element = 0, i = 0, row = 0;
        int vals_read = 0;
        int val1 = 0, val2 = 0, val3 = 0, val4 = 0, val5 = 0, val6 = 0, val7 = 0, val8 = 0;
        Object[] results = new Object[8];

        string input = NeuralData.Input;

        StringReader infile = new StringReader(input);

        vals_read = Utility.fscanf(infile, "%d  %d  %d", results);
        xinsize = (int)results[0];
        yinsize = (int)results[1];
        youtsize = (int)results[2];

        if (vals_read != 3)
        {
            throw new Exception("NNET: error reading input");
        }
        vals_read = Utility.fscanf(infile, "%d", results);
        numpats = (int)results[0];
        if (vals_read != 1)
        {
            throw new Exception("NNET: error reading input");
        }
        if (numpats > MAXPATS)
            numpats = MAXPATS;

        for (patt = 0; patt < numpats; patt++)
        {
            element = 0;
            for (row = 0; row < yinsize; row++)
            {
                vals_read = Utility.fscanf(infile, "%d  %d  %d  %d  %d", results);
                val1 = (int)results[0];
                val2 = (int)results[1];
                val3 = (int)results[2];
                val4 = (int)results[3];
                val5 = (int)results[4];
                if (vals_read != 5)
                {
                    throw new Exception("NNET: error reading input");
                }
                element = row * xinsize;

                in_pats[patt, element] = (double)val1; element++;
                in_pats[patt, element] = (double)val2; element++;
                in_pats[patt, element] = (double)val3; element++;
                in_pats[patt, element] = (double)val4; element++;
                in_pats[patt, element] = (double)val5; element++;
            }
            for (i = 0; i < IN_SIZE; i++)
            {
                if (in_pats[patt, i] >= 0.9)
                    in_pats[patt, i] = 0.9;
                if (in_pats[patt, i] <= 0.1)
                    in_pats[patt, i] = 0.1;
            }
            element = 0;
            vals_read = Utility.fscanf(infile, "%d  %d  %d  %d  %d  %d  %d  %d", results);
            val1 = (int)results[0];
            val2 = (int)results[1];
            val3 = (int)results[2];
            val4 = (int)results[3];
            val5 = (int)results[4];
            val6 = (int)results[5];
            val7 = (int)results[6];
            val8 = (int)results[7];

            out_pats[patt, element] = (double)val1; element++;
            out_pats[patt, element] = (double)val2; element++;
            out_pats[patt, element] = (double)val3; element++;
            out_pats[patt, element] = (double)val4; element++;
            out_pats[patt, element] = (double)val5; element++;
            out_pats[patt, element] = (double)val6; element++;
            out_pats[patt, element] = (double)val7; element++;
            out_pats[patt, element] = (double)val8; element++;
        }
    }


    private
    double DoNNET(NNetStruct locnnetstruct)
    {
        //    string errorcontext = "CPU:NNET";
        //    int systemerror = 0;
        long accumtime = 0;
        double iterations = 0.0;

        /*
        ** Init random number generator.
        ** NOTE: It is important that the random number generator
        **  be re-initialized for every pass through this test.
        **  The NNET algorithm uses the random number generator
        **  to initialize the net.  Results are sensitive to
        **  the initial neural net state.
        */
        ByteMark.randnum(3);

        /*
        ** Read in the input and output patterns.  We'll do this
        ** only once here at the beginning.  These values don't
        ** change once loaded.
        */
        read_data_file();

        /*
        ** See if we need to perform self adjustment loop.
        */
        if (locnnetstruct.adjust == 0)
        {
            /*
            ** Do self-adjustment.  This involves initializing the
            ** # of loops and increasing the loop count until we
            ** get a number of loops that we can use.
            */
            for (locnnetstruct.loops = 1;
                locnnetstruct.loops < MAXNNETLOOPS;
                locnnetstruct.loops++)
            {
                ByteMark.randnum(3);
                if (DoNNetIteration(locnnetstruct.loops) > global.min_ticks)
                    break;
            }
        }

        /*
        ** All's well if we get here.  Do the test.
        */
        accumtime = 0L;
        iterations = (double)0.0;

        do
        {
            ByteMark.randnum(3);    /* Gotta do this for Neural Net */
            accumtime += DoNNetIteration(locnnetstruct.loops);
            iterations += (double)locnnetstruct.loops;
        } while (ByteMark.TicksToSecs(accumtime) < locnnetstruct.request_secs);

        /*
        ** Clean up, calculate results, and go home.  Be sure to
        ** show that we don't have to rerun adjustment code.
        */
        locnnetstruct.iterspersec = iterations / ByteMark.TicksToFracSecs(accumtime);

        if (locnnetstruct.adjust == 0)
            locnnetstruct.adjust = 1;


        return locnnetstruct.iterspersec;
    }

    /********************
    ** DoNNetIteration **
    *********************
    ** Do a single iteration of the neural net benchmark.
    ** By iteration, we mean a "learning" pass.
    */
    public static long DoNNetIteration(long nloops)
    {
        long elapsed;          /* Elapsed time */
        int patt;

        /*
        ** Run nloops learning cycles.  Notice that, counted with
        ** the learning cycle is the weight randomization and
        ** zeroing of changes.  This should reduce clock jitter,
        ** since we don't have to stop and start the clock for
        ** each iteration.
        */
        elapsed = ByteMark.StartStopwatch();
        while (nloops-- != 0)
        {
            randomize_wts();
            zero_changes();
            iteration_count = 1;
            learned = F;
            numpasses = 0;
            while (learned == F)
            {
                for (patt = 0; patt < numpats; patt++)
                {
                    worst_error = 0.0;      /* reset this every pass through data */
                    move_wt_changes();      /* move last pass's wt changes to momentum array */
                    do_forward_pass(patt);
                    do_back_pass(patt);
                    iteration_count++;
                }
                numpasses++;
                learned = check_out_error();
            }
        }
        return (ByteMark.StopStopwatch(elapsed));
    }

    /*************************
    ** do_mid_forward(patt) **
    **************************
    ** Process the middle layer's forward pass
    ** The activation of middle layer's neurode is the weighted
    ** sum of the inputs from the input pattern, with sigmoid
    ** function applied to the inputs.
    **/
    public static void do_mid_forward(int patt)
    {
        double sum;
        int neurode, i;

        for (neurode = 0; neurode < MID_SIZE; neurode++)
        {
            sum = 0.0;
            for (i = 0; i < IN_SIZE; i++)
            {       /* compute weighted sum of input signals */
                sum += mid_wts[neurode, i] * in_pats[patt, i];
            }
            /*
            ** apply sigmoid function f(x) = 1/(1+exp(-x)) to weighted sum
            */
            sum = 1.0 / (1.0 + Math.Exp(-sum));
            mid_out[neurode] = sum;
        }
        return;
    }

    /*********************
    ** do_out_forward() **
    **********************
    ** process the forward pass through the output layer
    ** The activation of the output layer is the weighted sum of
    ** the inputs (outputs from middle layer), modified by the
    ** sigmoid function.
    **/
    public static void do_out_forward()
    {
        double sum;
        int neurode, i;

        for (neurode = 0; neurode < OUT_SIZE; neurode++)
        {
            sum = 0.0;
            for (i = 0; i < MID_SIZE; i++)
            {       /*
                ** compute weighted sum of input signals
                ** from middle layer
                */
                sum += out_wts[neurode, i] * mid_out[i];
            }
            /*
            ** Apply f(x) = 1/(1+Math.Exp(-x)) to weighted input
            */
            sum = 1.0 / (1.0 + Math.Exp(-sum));
            out_out[neurode] = sum;
        }
        return;
    }

    /*************************
    ** display_output(patt) **
    **************************
    ** Display the actual output vs. the desired output of the
    ** network.
    ** Once the training is complete, and the "learned" flag set
    ** to TRUE, then display_output sends its output to both
    ** the screen and to a text output file.
    **
    ** NOTE: This routine has been disabled in the benchmark
    ** version. -- RG
    **/
    /*
    public static void  display_output(int patt)
    {
    int             i;

        fprintf(outfile,"\n Iteration # %d",iteration_count);
        fprintf(outfile,"\n Desired Output:  ");

        for (i=0; i<OUT_SIZE; i++)
        {
            fprintf(outfile,"%6.3f  ",out_pats[patt][i]);
        }
        fprintf(outfile,"\n Actual Output:   ");

        for (i=0; i<OUT_SIZE; i++)
        {
            fprintf(outfile,"%6.3f  ",out_out[i]);
        }
        fprintf(outfile,"\n");
        return;
    }
    */

    /**********************
    ** do_forward_pass() **
    ***********************
    ** control function for the forward pass through the network
    ** NOTE: I have disabled the call to display_output() in
    **  the benchmark version -- RG.
    **/
    public static void do_forward_pass(int patt)
    {
        do_mid_forward(patt);   /* process forward pass, middle layer */
        do_out_forward();       /* process forward pass, output layer */
        /* display_output(patt);        ** display results of forward pass */
        return;
    }

    /***********************
    ** do_out_error(patt) **
    ************************
    ** Compute the error for the output layer neurodes.
    ** This is simply Desired - Actual.
    **/
    public static void do_out_error(int patt)
    {
        int neurode;
        double error, tot_error, sum;

        tot_error = 0.0;
        sum = 0.0;
        for (neurode = 0; neurode < OUT_SIZE; neurode++)
        {
            out_error[neurode] = out_pats[patt, neurode] - out_out[neurode];
            /*
            ** while we're here, also compute magnitude
            ** of total error and worst error in this pass.
            ** We use these to decide if we are done yet.
            */
            error = out_error[neurode];
            if (error < 0.0)
            {
                sum += -error;
                if (-error > tot_error)
                    tot_error = -error; /* worst error this pattern */
            }
            else
            {
                sum += error;
                if (error > tot_error)
                    tot_error = error; /* worst error this pattern */
            }
        }
        avg_out_error[patt] = sum / OUT_SIZE;
        tot_out_error[patt] = tot_error;
        return;
    }

    /***********************
    ** worst_pass_error() **
    ************************
    ** Find the worst and average error in the pass and save it
    **/
    public static void worst_pass_error()
    {
        double error, sum;

        int i;

        error = 0.0;
        sum = 0.0;
        for (i = 0; i < numpats; i++)
        {
            if (tot_out_error[i] > error) error = tot_out_error[i];
            sum += avg_out_error[i];
        }
        worst_error = error;
        average_error = sum / numpats;
        return;
    }

    /*******************
    ** do_mid_error() **
    ********************
    ** Compute the error for the middle layer neurodes
    ** This is based on the output errors computed above.
    ** Note that the derivative of the sigmoid f(x) is
    **        f'(x) = f(x)(1 - f(x))
    ** Recall that f(x) is merely the output of the middle
    ** layer neurode on the forward pass.
    **/
    public static void do_mid_error()
    {
        double sum;
        int neurode, i;

        for (neurode = 0; neurode < MID_SIZE; neurode++)
        {
            sum = 0.0;
            for (i = 0; i < OUT_SIZE; i++)
                sum += out_wts[i, neurode] * out_error[i];

            /*
            ** apply the derivative of the sigmoid here
            ** Because of the choice of sigmoid f(I), the derivative
            ** of the sigmoid is f'(I) = f(I)(1 - f(I))
            */
            mid_error[neurode] = mid_out[neurode] * (1 - mid_out[neurode]) * sum;
        }
        return;
    }

    /*********************
    ** adjust_out_wts() **
    **********************
    ** Adjust the weights of the output layer.  The error for
    ** the output layer has been previously propagated back to
    ** the middle layer.
    ** Use the Delta Rule with momentum term to adjust the weights.
    **/
    public static void adjust_out_wts()
    {
        int weight, neurode;
        double learn, delta, alph;

        learn = BETA;
        alph = ALPHA;
        for (neurode = 0; neurode < OUT_SIZE; neurode++)
        {
            for (weight = 0; weight < MID_SIZE; weight++)
            {
                /* standard delta rule */
                delta = learn * out_error[neurode] * mid_out[weight];

                /* now the momentum term */
                delta += alph * out_wt_change[neurode, weight];
                out_wts[neurode, weight] += delta;

                /* keep track of this pass's cum wt changes for next pass's momentum */
                out_wt_cum_change[neurode, weight] += delta;
            }
        }
        return;
    }

    /*************************
    ** adjust_mid_wts(patt) **
    **************************
    ** Adjust the middle layer weights using the previously computed
    ** errors.
    ** We use the Generalized Delta Rule with momentum term
    **/
    public static void adjust_mid_wts(int patt)
    {
        int weight, neurode;
        double learn, alph, delta;

        learn = BETA;
        alph = ALPHA;
        for (neurode = 0; neurode < MID_SIZE; neurode++)
        {
            for (weight = 0; weight < IN_SIZE; weight++)
            {
                /* first the basic delta rule */
                delta = learn * mid_error[neurode] * in_pats[patt, weight];

                /* with the momentum term */
                delta += alph * mid_wt_change[neurode, weight];
                mid_wts[neurode, weight] += delta;

                /* keep track of this pass's cum wt changes for next pass's momentum */
                mid_wt_cum_change[neurode, weight] += delta;
            }
        }
        return;
    }

    /*******************
    ** do_back_pass() **
    ********************
    ** Process the backward propagation of error through network.
    **/
    public static void do_back_pass(int patt)
    {
        do_out_error(patt);
        do_mid_error();
        adjust_out_wts();
        adjust_mid_wts(patt);

        return;
    }


    /**********************
    ** move_wt_changes() **
    ***********************
    ** Move the weight changes accumulated last pass into the wt-change
    ** array for use by the momentum term in this pass. Also zero out
    ** the accumulating arrays after the move.
    **/
    public static void move_wt_changes()
    {
        int i, j;

        for (i = 0; i < MID_SIZE; i++)
            for (j = 0; j < IN_SIZE; j++)
            {
                mid_wt_change[i, j] = mid_wt_cum_change[i, j];
                /*
                ** Zero it out for next pass accumulation.
                */
                mid_wt_cum_change[i, j] = 0.0;
            }

        for (i = 0; i < OUT_SIZE; i++)
            for (j = 0; j < MID_SIZE; j++)
            {
                out_wt_change[i, j] = out_wt_cum_change[i, j];
                out_wt_cum_change[i, j] = 0.0;
            }

        return;
    }

    /**********************
    ** check_out_error() **
    ***********************
    ** Check to see if the error in the output layer is below
    ** MARGIN*OUT_SIZE for all output patterns.  If so, then
    ** assume the network has learned acceptably well.  This
    ** is simply an arbitrary measure of how well the network
    ** has learned -- many other standards are possible.
    **/
    public static int check_out_error()
    {
        int result, i, error;

        result = T;
        error = F;
        worst_pass_error();     /* identify the worst error in this pass */

        /*
        #if DEBUG
        Console.WriteLine("\n Iteration # {0}",iteration_count);
        #endif
        */
        for (i = 0; i < numpats; i++)
        {
            /*      printf("\n Error pattern %d:   Worst: %8.3f; Average: %8.3f",
                  i+1,tot_out_error[i], avg_out_error[i]);
                fprintf(outfile,
                 "\n Error pattern %d:   Worst: %8.3f; Average: %8.3f",
                 i+1,tot_out_error[i]);
            */

            if (worst_error >= STOP) result = F;
            if (tot_out_error[i] >= 16.0) error = T;
        }

        if (error == T) result = ERR;


#if DEBUG
        /* printf("\n Error this pass thru data:   Worst: %8.3f; Average: %8.3f",
         worst_error,average_error);
        */
        /* fprintf(outfile,
         "\n Error this pass thru data:   Worst: %8.3f; Average: %8.3f",
          worst_error, average_error); */
#endif

        return (result);
    }


    /*******************
    ** zero_changes() **
    ********************
    ** Zero out all the wt change arrays
    **/
    public static void zero_changes()
    {
        int i, j;

        for (i = 0; i < MID_SIZE; i++)
        {
            for (j = 0; j < IN_SIZE; j++)
            {
                mid_wt_change[i, j] = 0.0;
                mid_wt_cum_change[i, j] = 0.0;
            }
        }

        for (i = 0; i < OUT_SIZE; i++)
        {
            for (j = 0; j < MID_SIZE; j++)
            {
                out_wt_change[i, j] = 0.0;
                out_wt_cum_change[i, j] = 0.0;
            }
        }
        return;
    }


    /********************
    ** randomize_wts() **
    *********************
    ** Initialize the weights in the middle and output layers to
    ** random values between -0.25..+0.25
    ** Function rand() returns a value between 0 and 32767.
    **
    ** NOTE: Had to make alterations to how the random numbers were
    ** created.  -- RG.
    **/
    public static void randomize_wts()
    {
        int neurode, i;
        double value;

        /*
        ** Following not used int benchmark version -- RG
        **
        **        printf("\n Please enter a random number seed (1..32767):  ");
        **        scanf("%d", &i);
        **        srand(i);
        */

        for (neurode = 0; neurode < MID_SIZE; neurode++)
        {
            for (i = 0; i < IN_SIZE; i++)
            {
                value = (double)ByteMark.abs_randwc(100000);
                value = value / (double)100000.0 - (double)0.5;
                mid_wts[neurode, i] = value / 2;
            }
        }
        for (neurode = 0; neurode < OUT_SIZE; neurode++)
        {
            for (i = 0; i < MID_SIZE; i++)
            {
                value = (double)ByteMark.abs_randwc(100000);
                value = value / (double)10000.0 - (double)0.5;
                out_wts[neurode, i] = value / 2;
            }
        }
        return;
    }

    /**********************
    ** display_mid_wts() **
    ***********************
    ** Display the weights on the middle layer neurodes
    ** NOTE: This routine is not used in the benchmark
    **  test -- RG
    **/
    /* static void display_mid_wts()
    {
    int             neurode, weight, row, col;

    fprintf(outfile,"\n Weights of Middle Layer neurodes:");

    for (neurode=0; neurode<MID_SIZE; neurode++)
    {
        fprintf(outfile,"\n  Mid Neurode # %d",neurode);
        for (row=0; row<IN_Y_SIZE; row++)
        {
            fprintf(outfile,"\n ");
            for (col=0; col<IN_X_SIZE; col++)
            {
                weight = IN_X_SIZE * row + col;
                fprintf(outfile," %8.3f ", mid_wts[neurode,weight]);
            }
        }
    }
    return;
    }
    */
    /**********************
    ** display_out_wts() **
    ***********************
    ** Display the weights on the output layer neurodes
    ** NOTE: This code is not used in the benchmark
    **  test -- RG
    */
    /* void  display_out_wts()
    {
    int             neurode, weight;

        fprintf(outfile,"\n Weights of Output Layer neurodes:");

        for (neurode=0; neurode<OUT_SIZE; neurode++)
        {
            fprintf(outfile,"\n  Out Neurode # %d \n",neurode);
            for (weight=0; weight<MID_SIZE; weight++)
            {
                fprintf(outfile," %8.3f ", out_wts[neurode,weight]);
            }
        }
        return;
    }
    */
}

