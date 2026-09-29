// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TestDependencyRecorder
    {
        public record struct Dependency
        {
            public string Source;
            public string Target;
            public bool Marked;
            public string DependencyKind;
        }

        public List<Dependency> Dependencies = new List<Dependency>();
    }
}
