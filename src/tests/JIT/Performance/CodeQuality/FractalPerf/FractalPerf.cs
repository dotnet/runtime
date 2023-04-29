// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace FractalPerf
{
    struct complex
    {
        public complex(double a, double b) { r = a; i = b; }
        public double r;
        public double i;

        public complex square() {
            return new complex(r * r - i * i, 2.0 * r * i);
        }

        public double sqabs() {
            return r * r + i * i;
        }

        public override string ToString() {
            return String.Format("[{0} + {1}i]", r, i);
        }

        public static complex operator +(complex a, complex b) {
            return new complex(a.r + b.r, a.i + b.i);
        }
    }

    public abstract class Fractal
    {
        protected double XB, YB, XE, YE, XS, YS;
        const double resolution = 375.0;

        public Fractal(double xbeg, double ybeg, double xend, double yend) {
            XB = Math.Min(xbeg, xend);
            YB = Math.Min(ybeg, yend);
            XE = Math.Max(xbeg, xend);
            YE = Math.Max(ybeg, yend);
            XS = (xend - xbeg) / resolution;
            YS = (yend - ybeg) / resolution;
        }

        public abstract double Render();

        public static double Clamp(double val, double lo, double hi) {
            return Math.Min(Math.Max(val, lo), hi);
        }
    }

    public class Mandelbrot : Fractal
    {
        public Mandelbrot() : base(-2.0, -1.5, 1.0, 1.5) { }

        public override double Render() {
            double limit = 4.0;
            double result = 0.0;

            for (double y = YB; y < YE; y += YS) {
                for (double x = YB; x < YE; x += XS) {
                    complex num = new complex(x, y);
                    complex accum = num;
                    int iters;
                    for (iters = 0; iters < 1000; iters++) {
                        accum = accum.square();
                        accum += num;
                        if (accum.sqabs() > limit)
                            break;
                    }
                    result += iters;
                }
            }
            return result;
        }
    }

    public class Julia : Fractal
    {
        private double Real;
        double Imaginary;
        public Julia(double real, double imaginary)
            : base(-2.0, -1.5, 1.0, 1.5) {
            Real = real;
            Imaginary = imaginary;
        }

        public override double Render() {
            double limit = 4.0;
            double result = 0.0;

            // set the Julia Set constant
            complex seed = new complex(Real, Imaginary);
            // run through every point on the screen, setting
            // m and n to the coordinates
            for (double m = XB; m < XE; m += XS) {
                for (double n = YB; n < YE; n += YS) {
                    // the initial z value is the current pixel,
                    // so x and y have to be set to m and n
                    complex accum = new complex(m, n);
                    // perform the iteration
                    int num;
                    for (num = 0; num < 1000; num++) {
                        // exit the loop if the number  becomes too big
                        if (accum.sqabs() > limit)
                            break;
                        // use the formula
                        accum = accum.square() + seed;
                    }
                    // determine the color using the number of
                    // iterations it took for the number to become too big
                    // char color = num % number_of_colors;
                    // plot the point
                    result += num;
                }
            }
            return result;
        }
    }

    public class Launch
    {

#if DEBUG
        public const int Iterations = 1;
#else
        public const int Iterations = 5;
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Bench()
        {
            Mandelbrot m = new Mandelbrot();
            Julia j = new Julia(-0.62, 0.41);
            double mResult = m.Render();
            double jResult = j.Render();

            return true;
        }

        static bool TestBase() {
            bool result = true;
            for (int i = 0; i < Iterations; i++) {
                result &= Bench();
            }
            return result;
        }

        [Fact]
        public static int TestEntryPoint() {
            bool result = TestBase();
            return (result ? 100 : -1);
        }
    }
}
