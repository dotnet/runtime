// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Test
{
    public static class PlinqModesTests
    {
        // This test is failing on our CI machines, probably due to the VM's limited CPU.
        // To-do: Re-enable this test when we resolve the build machine issues.
        // [Fact(Skip="Issue #176")]
        public static void RunPlinqModesTests()
        {
            // I would assume that this gets the number of processors (ie. Environment.ProcessorCount)
            // but since we are trying to exclude dependencies that aren't part of the contract, we
            // can't use System.Runtime.Extensions.  So we had to remove this check.

            //            if (SchedulingProxy.GetDefaultDegreeOfParallelism() == 1)
            //            {
            //                Console.WriteLine("   - Test does not apply to the DOP=1 case.");
            //                return true;
            //            }

            Action<ParallelExecutionMode, Verifier>[] hardQueries = {
                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).Where(x => true).TakeWhile((x,i) => true).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).Where(x => true).TakeWhile((x,i) => true).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x=>true).Select(x => verifier.Verify(x)).ElementAt(5),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x=>true).Select((x,i) => verifier.Verify(x)).Iterate(),
            };

            Action<ParallelExecutionMode, Verifier>[] easyQueries = {
                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .TakeWhile(x => true).Select(x => verifier.Verify(x)).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .TakeWhile(x => true).Select(x => verifier.Verify(x)).Iterate(),

                (mode,verifier) => Enumerable.Range(0, 1000).ToArray().AsParallel()
                    .Select(x => verifier.Verify(x)).Take(100).WithExecutionMode(mode).ToArray(),

                (mode,verifier) => Enumerable.Range(0, 1000).ToArray().AsParallel().WithExecutionMode(mode)
                    .Take(100).Select(x => verifier.Verify(x)).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).ElementAt(5),

                (mode, verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).SelectMany((x,i) => Enumerable.Repeat(1, 2)).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).SelectMany((x,i) => Enumerable.Repeat(1, 2)).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode).AsUnordered()
                    .Select(x => verifier.Verify(x)).Select((x,i) => x).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode).AsUnordered().Where(x => true).Select(x => verifier.Verify(x)).First(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).OrderBy(x => x).ToArray(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).OrderBy(x => x).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().AsOrdered().WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x))
                    .Concat(Enumerable.Range(0, 1000).AsParallel().AsOrdered().Where(x => true))
                    .ToList(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x)).Take(100).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x)).Take(100).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).TakeWhile(x => true).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).TakeWhile(x => true).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000)
                    .OrderBy(x=>x).Select(x => verifier.Verify(x)).WithExecutionMode(mode).ElementAt(5),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .OrderBy(x=>x).Select((x,i) => verifier.Verify(x)).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x)).OrderBy(x=>x).Take(10000).Iterate(),
            };


            // Verify that all queries in 'easyQueries' run in parallel in default mode

            for (int i = 0; i < easyQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                easyQueries[i].Invoke(ParallelExecutionMode.Default, verifier);
                if (!verifier.Passed)
                {
                    Assert.True(false, string.Format("Easy query {0} expected to run in parallel in default mode", i));
                }
            }

            // Verify that all queries in 'easyQueries' always run in forced mode
            for (int i = 0; i < easyQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                easyQueries[i].Invoke(ParallelExecutionMode.ForceParallelism, verifier);
                if (!verifier.Passed)
                {
                    Assert.True(false, string.Format("Easy query {0} expected to run in parallel in force-parallelism mode", i));
                }
            }

            // Verify that all queries in 'easyQueries' always run in forced mode
            for (int i = 0; i < hardQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                hardQueries[i].Invoke(ParallelExecutionMode.ForceParallelism, verifier);
                if (!verifier.Passed)
                {
                    Assert.True(false, string.Format("Hard query {0} expected to run in parallel in force-parallelism mode", i));
                }
            }
        }

        #region Helper Methods / Classes

        private static void Iterate<T>(this IEnumerable<T> e)
        {
            foreach (var x in e) { }
        }

        // A class that checks whether Verify has been called from one or multiple threads.
        private abstract class Verifier
        {
            internal abstract int Verify(int x);
            internal abstract bool Passed { get; }
        }

        // A class that checks whether the Verify method got called from at least two threads.
        // The first call to Verify() blocks. If another call to Verify() occurs prior to the timeout
        // then we know that Verify() is getting called from multiple threads.
        private class ParVerifier : Verifier
        {
            private int _counter = 0;
            private bool _passed = false;
            private const int TIMEOUT_LIMIT = 5000;

            internal override int Verify(int x)
            {
                lock (this)
                {
                    _counter++;
                    if (_counter == 1)
                    {
                        if (Monitor.Wait(this, TIMEOUT_LIMIT))
                        {
                            _passed = true;
                        }
                    }
                    else if (_counter == 2)
                    {
                        Monitor.Pulse(this);
                    }
                }

                return x;
            }

            internal override bool Passed
            {
                get { return _passed; }
            }
        }
        #endregion
    }
}
