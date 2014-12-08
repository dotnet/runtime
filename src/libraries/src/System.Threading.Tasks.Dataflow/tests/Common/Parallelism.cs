// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class Parallelism
    {
        private static readonly int _DEFAULT_DEGREE_OF_PARALLELISM = Environment.ProcessorCount * 2;
        internal static int degreeOfParallelism = -1;
        internal static int randomSeed = Environment.TickCount;

        public static int RandomSeed
        {
            get
            {
                return randomSeed;
            }
        }

        public static int ActualDegreeOfParallelism
        {
            get
            {
                return degreeOfParallelism > 0 ? degreeOfParallelism : _DEFAULT_DEGREE_OF_PARALLELISM;
            }
        }
    }
}
