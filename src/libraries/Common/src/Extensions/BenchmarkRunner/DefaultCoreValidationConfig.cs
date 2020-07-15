// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreValidationConfig : ManualConfig
    {
        public DefaultCoreValidationConfig()
        {
            Add(ConsoleLogger.Default);

            Add(Job.Dry.With(InProcessToolchain.Instance));
        }
    }
}
