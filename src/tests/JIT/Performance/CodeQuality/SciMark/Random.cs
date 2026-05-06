// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
/// 
/// For details on the original authors see http://math.nist.gov/scimark2
/// 
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>


using System;
using System.Runtime.CompilerServices;

namespace SciMark2
{
    /* Random.java based on Java Numerical Toolkit (JNT) Random.UniformSequence
	class.  We do not use Java's own java.util.Random so that we can compare
	results with equivalent C and Fortran coces.*/

    public class Random
    {
        /*------------------------------------------------------------------------------
		CLASS VARIABLES
		------------------------------------------------------------------------------ */

        internal int seed = 0;

        private int[] _m;
        private int _i = 4;
        private int _j = 16;

        private const int mdig = 32;
        private const int one = 1;
        private int _m1;
        private int _m2;

        private double _dm1;

        private bool _haveRange = false;
        private double _left = 0.0;
        private double _right = 1.0;
        private double _width = 1.0;


        /* ------------------------------------------------------------------------------
		CONSTRUCTORS
		------------------------------------------------------------------------------ */

        /// <summary>
        /// Initializes a sequence of uniformly distributed quasi random numbers with a
        /// seed based on the system clock.
        /// </summary>
        public Random()
        {
            initialize((int)System.DateTime.Now.Ticks);
        }

        /// <summary>
        /// Initializes a sequence of uniformly distributed quasi random numbers on a
        /// given half-open interval [left,right) with a seed based on the system
        /// clock.
        /// </summary>
        /// <param name="<B>left</B>">(double)<BR>
        /// The left endpoint of the half-open interval [left,right).
        /// </param>
        /// <param name="<B>right</B>">(double)<BR>
        /// The right endpoint of the half-open interval [left,right).
        /// </param>
        public Random(double left, double right)
        {
            initialize((int)System.DateTime.Now.Ticks);
            _left = left;
            _right = right;
            _width = right - left;
            _haveRange = true;
        }

        /// <summary>
        /// Initializes a sequence of uniformly distributed quasi random numbers with a
        /// given seed.
        /// </summary>
        /// <param name="<B>seed</B>">(int)<BR>
        /// The seed of the random number generator.  Two sequences with the same
        /// seed will be identical.
        /// </param>
        public Random(int seed)
        {
            initialize(seed);
        }

        /// <summary>Initializes a sequence of uniformly distributed quasi random numbers
        /// with a given seed on a given half-open interval [left,right).
        /// </summary>
        /// <param name="<B>seed</B>">(int)<BR>
        /// The seed of the random number generator.  Two sequences with the same
        /// seed will be identical.
        /// </param>
        /// <param name="<B>left</B>">(double)<BR>
        /// The left endpoint of the half-open interval [left,right).
        /// </param>
        /// <param name="<B>right</B>">(double)<BR>
        /// The right endpoint of the half-open interval [left,right).
        /// </param>
        public Random(int seed, double left, double right)
        {
            initialize(seed);
            _left = left;
            _right = right;
            _width = right - left;
            _haveRange = true;
        }

        /* ------------------------------------------------------------------------------
		PUBLIC METHODS
		------------------------------------------------------------------------------ */

        /// <summary>
        /// Returns the next random number in the sequence.
        /// </summary>
        public double nextDouble()
        {
            int k;

            k = _m[_i] - _m[_j];
            if (k < 0)
                k += _m1;
            _m[_j] = k;

            if (_i == 0)
                _i = 16;
            else
                _i--;

            if (_j == 0)
                _j = 16;
            else
                _j--;

            if (_haveRange)
                return _left + _dm1 * (double)k * _width;
            else
                return _dm1 * (double)k;
        }

        /// <summary>
        /// Returns the next N random numbers in the sequence, as
        /// a vector.
        /// </summary>
        public void nextDoubles(double[] x)
        {
            int N = x.Length;
            int remainder = N & 3;

            if (_haveRange)
            {
                for (int count = 0; count < N; count++)
                {
                    int k = _m[_i] - _m[_j];

                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;

                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;

                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;

                    x[count] = _left + _dm1 * (double)k * _width;
                }
            }
            else
            {
                for (int count = 0; count < remainder; count++)
                {
                    int k = _m[_i] - _m[_j];

                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;

                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;

                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;


                    x[count] = _dm1 * (double)k;
                }

                for (int count = remainder; count < N; count += 4)
                {
                    int k = _m[_i] - _m[_j];
                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;
                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;
                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;
                    x[count] = _dm1 * (double)k;


                    k = _m[_i] - _m[_j];
                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;
                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;
                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;
                    x[count + 1] = _dm1 * (double)k;


                    k = _m[_i] - _m[_j];
                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;
                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;
                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;
                    x[count + 2] = _dm1 * (double)k;


                    k = _m[_i] - _m[_j];
                    if (_i == 0)
                        _i = 16;
                    else
                        _i--;
                    if (k < 0)
                        k += _m1;
                    _m[_j] = k;
                    if (_j == 0)
                        _j = 16;
                    else
                        _j--;
                    x[count + 3] = _dm1 * (double)k;
                }
            }
        }

        /*----------------------------------------------------------------------------
		PRIVATE METHODS
		------------------------------------------------------------------------ */

        private void initialize(int seed)
        {
            // First the initialization of the member variables;
            _m1 = (one << mdig - 2) + ((one << mdig - 2) - one);
            _m2 = one << mdig / 2;
            _dm1 = 1.0 / (double)_m1;

            int jseed, k0, k1, j0, j1, iloop;

            this.seed = seed;

            _m = new int[17];

            jseed = System.Math.Min(System.Math.Abs(seed), _m1);
            if (jseed % 2 == 0)
                --jseed;
            k0 = 9069 % _m2;
            k1 = 9069 / _m2;
            j0 = jseed % _m2;
            j1 = jseed / _m2;
            for (iloop = 0; iloop < 17; ++iloop)
            {
                jseed = j0 * k0;
                j1 = (jseed / _m2 + j0 * k1 + j1 * k0) % (_m2 / 2);
                j0 = jseed % _m2;
                _m[iloop] = j0 + _m2 * j1;
            }
            _i = 4;
            _j = 16;
        }
    }
}
