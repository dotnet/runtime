// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Method:  Simulate a bouncing ball based on the laws of physics.
//          The general principles:
//            The velocity of a falling ball is : (½) m v^2 = m g d è v = sqrt(2 * g * d)
//            The non-ellastic collision will shoot the ball in 
//              the opposite direction at       : v2 = e v = e * sqrt(2 * g * d)
//              Where e is the coeficient of restitution
//            The height the ball will travel up: d2 = (1/2) g t^2 = v2^2 / (2 * g)
//            This process is repeated until the ball is no more that 0.1m above the ground.


using System;

class Ball
{
    protected double g = 9.80056;
    protected double Coef;
    protected double Distance;
    protected double D;
    protected double S;

    public Ball(double coef, double pos)
    {
        Distance = 0.0;
        Coef = coef;
        D = pos;
        S = 0.0;
    }

    public bool Step()
    {
        bool retVal = true;

        // about to fall
        if (D > 0.1)
        {
            Distance += D;
            S = Coef * Math.Sqrt(2 * g * D);
            D = 0.0;

            // bouncing
        }
        else if (D == 0.0 && S > 0.0)
        {
            D = (S * S) / (2 * g);
            S = 0.0;

            Distance += D;
            // stopped
        }
        else
        {
            retVal = false;
        }

        return retVal;
    }

    public double Height
    {
        get
        {
            return D;
        }
    }

    public double DistanceTraveled()
    {
        return Distance;
    }
}
