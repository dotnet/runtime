// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreDebugConfig : ManualConfig
    {
        public DefaultCoreDebugConfig()
        {
            Add(ConsoleLogger.Default);
            Add(JitOptimizationsValidator.DontFailOnError);

            Add(Job.InProcess
                .With(RunStrategy.Throughput));
        }
    }
}
