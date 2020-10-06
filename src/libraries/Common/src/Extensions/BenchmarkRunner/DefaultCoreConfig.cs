// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreConfig : ManualConfig
    {
        public DefaultCoreConfig()
        {
            Add(ConsoleLogger.Default);
            Add(MarkdownExporter.GitHub);

            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);
            Add(DefaultColumnProviders.Instance);

            Add(JitOptimizationsValidator.FailOnError);

            Add(Job.Core
#if NETCOREAPP2_1
                .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21))
#elif NETCOREAPP3_0
                .With(CsProjCoreToolchain.From(new NetCoreAppSettings("netcoreapp3.0", null, ".NET Core 3.0")))
#elif NETCOREAPP3_1
                .With(CsProjCoreToolchain.From(new NetCoreAppSettings("netcoreapp3.1", null, ".NET Core 3.1")))
#elif NETCOREAPP5_0
                .With(CsProjCoreToolchain.From(new NetCoreAppSettings("net5.0", null, ".NET 5.0")))
#else
#error Target frameworks need to be updated.
#endif
                .With(new GcMode { Server = true })
                .With(RunStrategy.Throughput));
        }
    }
}
