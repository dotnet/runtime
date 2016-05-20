using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TargetAttribute : Attribute
    {
        public string Name { get; set; }
        public IEnumerable<string> Dependencies { get; }

        public TargetAttribute()
        {
            Dependencies = Enumerable.Empty<string>();
        }

        // Attributes can only use constants, so a comma-separated string is better :)
        public TargetAttribute(params string[] dependencies)
        {
            Dependencies = dependencies;
        }
    }
}