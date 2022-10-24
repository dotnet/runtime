// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using TLens.Analyzers;

namespace TLens
{
    sealed class Runner
    {
        readonly List<Analyzer> analyzers = new List<Analyzer>();

        public void AddAnalyzer(Analyzer analyzer)
        {
            analyzers.Add(analyzer);
        }

        public void AddAnalyzers(IEnumerable<Analyzer> analyzers)
        {
            this.analyzers.AddRange(analyzers);
        }

        public int MaxAnalyzerResults { get; set; } = 30;

        public void Process(List<AssemblyDefinition> assemblies)
        {
            if (assemblies.Count == 0)
                return;

            bool first = true;
            foreach (var a in analyzers)
            {
                foreach (var assembly in assemblies)
                {
                    a.ProcessAssembly(assembly);
                }

                if (!first)
                    Console.WriteLine();

                a.PrintResults(MaxAnalyzerResults);
                first = false;
            }
        }
    }
}
