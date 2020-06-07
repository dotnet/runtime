// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Method:  Simulate a bouncing ball based on the laws of physics.

using System;

class BouncingBall
{

    public static int Main(string[] args)
    {
        double coef;
        double height;
        Ball B;
        double inc;
        string output;
        bool FirstTime;

        if (args.Length >= 2)
        {
            coef = Convert.ToDouble(args[0]);
            height = Convert.ToDouble(args[1]);
        }
        else
        {
            coef = 0.8;
            height = 80.0;
        }

        Console.WriteLine("Coeficient of Restitution: {0}", coef);
        Console.WriteLine("Balls starting height    : {0} m", height);

        B = new Ball(coef, height);

        FirstTime = true;
        inc = 70 / height;
        while (FirstTime || B.Step())
        {
            output = "|";
            for (int i = 0; i < (int)Math.Floor(inc * B.Height); i++) output += " ";
            output += "*";
            Console.WriteLine("{0}\r", output);
            FirstTime = false;
        }
        Console.WriteLine("");

        double d = B.DistanceTraveled();

        Console.WriteLine("The Ball Traveld: {0} m", d);

        if ((d - 363.993284074572) > 1.0e-11)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
