// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

namespace SciMark2
{
    /// <summary>
    /// Provides a stopwatch to measure elapsed time.
    /// </summary>
    /// <author> 
    /// Roldan Pozo
    /// </author>
    /// <version> 
    /// 14 October 1997, revised 1999-04-24
    /// </version>
    /// 
    public class Stopwatch
    {
        private bool _running;
        private double _last_time;
        private double _total;

        /// 
        /// <summary>R
        /// eturn system time (in seconds)
        /// </summary>
        public static double seconds()
        {
            return (System.DateTime.Now.Ticks * 1.0E-7);
        }

        public virtual void reset()
        {
            _running = false;
            _last_time = 0.0;
            _total = 0.0;
        }

        public Stopwatch()
        {
            reset();
        }

        /// 
        /// <summary>
        /// Start (and reset) timer
        /// </summary>
        public virtual void start()
        {
            if (!_running)
            {
                _running = true;
                _total = 0.0;
                _last_time = seconds();
            }
        }

        /// 
        /// <summary>
        /// Resume timing, after stopping.  (Does not wipe out accumulated times.)
        /// </summary>
        public virtual void resume()
        {
            if (!_running)
            {
                _last_time = seconds();
                _running = true;
            }
        }

        /// 
        /// <summary>
        /// Stop timer
        /// </summary>
        public virtual double stop()
        {
            if (_running)
            {
                _total += seconds() - _last_time;
                _running = false;
            }
            return _total;
        }

        /// 
        /// <summary>
        /// return the elapsed time (in seconds)
        /// </summary>
        public virtual double read()
        {
            if (_running)
            {
                _total += seconds() - _last_time;
                _last_time = seconds();
            }
            return _total;
        }
    }
}