// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class BuildReporter
    {
        private const string TimeSpanFormat = @"hh\:mm\:ss\.fff";
        private static DateTime _initialTime = DateTime.Now;

        public static void BeginSection(string type, string name)
        {
            Reporter.Output.WriteLine($"[{type.PadRight(10)} >]".Green() + $" [....] [{(DateTime.Now - _initialTime).ToString(TimeSpanFormat)}]".Blue() + $" {name}");
        }

        public static void SectionComment(string type, string comment)
        {
            Reporter.Output.WriteLine($"[{type.PadRight(10)} -]".Green() + $" [....] [{(DateTime.Now - _initialTime).ToString(TimeSpanFormat)}]".Blue() + $" {comment}");
        }

        public static void EndSection(string type, string name, bool success)
        {
            var header = $"[{type.PadRight(10)} <]";
            if (success)
            {
                header = header.Green();
            }
            else
            {
                header = header.Red();
            }
            var successString = success ? " OK " : "FAIL";
            Reporter.Output.WriteLine(header + $" [{successString}] [{(DateTime.Now - _initialTime).ToString(TimeSpanFormat)}]".Blue() + $" {name}");
        }
    }
}
