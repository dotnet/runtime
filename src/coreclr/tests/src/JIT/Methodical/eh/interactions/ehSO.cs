// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
Spilling volatile objects opposed to spilling only
  the exception objects. The equivalence of GTF_OTHER_SIDEEFF and
  GT_CATCH_ARG is altered by impGetByRefResultType.
  
Actual Results:
System.StackOverflowException during JIT of function

Expected Results:
no exception
*/

public class Form1
{
    public static volatile bool RunsInWebServer = false;


    public Form1()
    {
        try
        {
        }
        catch
        {
            string lT = string.Format("{0}", RunsInWebServer);
        }
    }

    public static int Main()
    {
        Form1 f = new Form1();
        return 100;
    }
}

