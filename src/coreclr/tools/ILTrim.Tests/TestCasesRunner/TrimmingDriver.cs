// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmingDriver
    {
        public TrimmingResults Trim(string[] args, TrimmingCustomizations? customizations, TrimmingTestLogger logger)
        {
            Driver.ProcessResponseFile(args, out var queue);
            using var driver = new Driver(queue);
            return new TrimmingResults(driver.Run(logger));
        }
    }
}
