using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class StandardGoals
    {
        public static BuildSetup UseStandardGoals(this BuildSetup self)
        {
            return self.UseTargets(new[]
            {
                new BuildTarget("Default", "Standard Goals", new [] { "Prepare", "Compile", "Test", "Package", "Publish" }),
                new BuildTarget("Prepare", "Standard Goals"),
                new BuildTarget("Compile", "Standard Goals"),
                new BuildTarget("Test", "Standard Goals"),
                new BuildTarget("Package", "Standard Goals"),
                new BuildTarget("Publish", "Standard Goals")
            });
        }
    }
}
