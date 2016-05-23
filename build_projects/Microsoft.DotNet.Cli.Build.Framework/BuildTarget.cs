using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildTarget
    {
        public string Name { get; }
        public string Source { get; }
        public IEnumerable<string> Dependencies { get; }
        public IEnumerable<Func<bool>> Conditions { get; }
        public Func<BuildTargetContext, BuildTargetResult> Body { get; }

        public BuildTarget(string name, string source) : this(name, source, Enumerable.Empty<string>(), Enumerable.Empty<Func<bool>>(), null) { }
        public BuildTarget(string name, string source, IEnumerable<string> dependencies) : this(name, source, dependencies, Enumerable.Empty<Func<bool>>(), null) { }
        public BuildTarget(
            string name, 
            string source, 
            IEnumerable<string> dependencies, 
            IEnumerable<Func<bool>> conditions, 
            Func<BuildTargetContext, BuildTargetResult> body)
        {
            Name = name;
            Source = source;
            Dependencies = dependencies;
            Conditions = conditions;
            Body = body;
        }
    }
}