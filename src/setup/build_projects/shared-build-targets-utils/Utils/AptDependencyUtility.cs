using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class AptDependencyUtility
    {
        public static bool PackageIsInstalled(string packageName)
        {
            var result = Command.Create("dpkg", "-s", packageName)
                .CaptureStdOut()
                .CaptureStdErr()
                .QuietBuildReporter()
                .Execute();

            return result.ExitCode == 0;
        }
    }
}
