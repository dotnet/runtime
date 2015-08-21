// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal class Suite
    {
        private readonly string _name;
        private readonly int _standardRunOperations;
        private readonly Benchmark[] _includedBenchmarks;

        public Suite(string name, int standardRunOperations, Benchmark[] includedBenchmarks)
        {
            _name = name; _standardRunOperations = standardRunOperations; _includedBenchmarks = includedBenchmarks;
        }

        public string Name { get { return _name; } }

        public int StandardRunOperations { get { return _standardRunOperations; } }

        public Benchmark[] IncludedBenchmarks { get { return _includedBenchmarks; } }
    }
}
