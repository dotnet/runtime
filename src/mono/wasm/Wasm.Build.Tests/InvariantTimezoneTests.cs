// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantTimezoneTests : BuildTestBase
    {
        public InvariantTimezoneTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantTimezoneTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AOT_InvariantTimezone(BuildArgs buildArgs, bool? invariantTimezone, RunHost host, string id)
            => TestInvariantTimezone(buildArgs, invariantTimezone, host, id);

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void RelinkingWithoutAOT(BuildArgs buildArgs, bool? invariantTimezone, RunHost host, string id)
            => TestInvariantTimezone(buildArgs, invariantTimezone, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private void TestInvariantTimezone(BuildArgs buildArgs, bool? invariantTimezone,
                                                        RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"invariant_{invariantTimezone?.ToString() ?? "unset"}";
            if (invariantTimezone != null)
                extraProperties = $"{extraProperties}<InvariantTimezone>{invariantTimezone}</InvariantTimezone>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            string programText = @"
                using System;

                // https://github.com/dotnet/runtime/blob/main/docs/design/features/timezone-invariant-mode.md

                var timezonesCount = TimeZoneInfo.GetSystemTimeZones().Count;
                Console.WriteLine($""Found {timezonesCount} timezones in the TZ database"");

                TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById(""UTC"");
                Console.WriteLine($""{utc.DisplayName} BaseUtcOffset is {utc.BaseUtcOffset}"");

                try
                {
                    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(""Asia/Tokyo"");
                    Console.WriteLine($""{tst.DisplayName} BaseUtcOffset is {tst.BaseUtcOffset}"");
                }
                catch (TimeZoneNotFoundException tznfe)
                {
                    Console.WriteLine($""Could not find Asia/Tokyo: {tznfe.Message}"");
                }

                return 42;
            ";

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));

            string output = RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("UTC BaseUtcOffset is 0", output);
            if (invariantGlobalization == true)
            {
                Assert.Contains("Could not find Asia/Tokyo", output);
            }
            else
            {
                Assert.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00", output);
            }
        }
    }
}
