// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Compute distance light travels using long variables.
using System;
using Xunit;
public class Light
{

    [Fact]
    public static int TestEntryPoint()
    {
        int lightspeed;
        long days;
        long seconds1, seconds2, seconds3, seconds4, seconds5;
        long distance;

        // approximate speed of light in miles per second
        lightspeed = 86000;

        days = 1000;
        seconds1 = days * 24 * 60 * 60; //this is one of the problems, more than 2 multiplication produce the error
        //seconds = days * 24;  // if we calculate "seconds" like follows it works
        //seconds *=60;	    
        // seconds *=60;
        seconds2 = (days * 24) * (60 * 60);
        seconds3 = days * (24 * 60) * 60;
        seconds4 = days * 24 * (60 * 60);
        seconds5 = (days * 24) * 60 * 60;
        if (seconds1 != seconds2 ||
             seconds1 != seconds3 ||
             seconds1 != seconds4 ||
             seconds1 != seconds5)
        {
            Console.WriteLine("Test failed.");
            return 1;
        }

        distance = lightspeed * seconds1;
        Console.WriteLine("in  {0} days light will travel about  {1} miles.", days, distance);//crash here but if we replace 
        //the variable days by "seconds" it works (same type) !!!!!!!!!!
        //by removing this line or casting to integer it works.
        // if we change the type of days to int it works !!

        seconds1 /= 60 * 60;
        seconds1 /= days;
        if (seconds1 / 24 != 1)
        {
            Console.WriteLine("Test failed.");
            return 1;
        }
        return 100;
    }
}
