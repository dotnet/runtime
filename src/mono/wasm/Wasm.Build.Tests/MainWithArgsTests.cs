// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.AppHost;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class MainWithArgsTests : WasmTemplateTestsBase
    {
        public MainWithArgsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> MainWithArgsTestData(bool aot)
            => ConfigWithAOTData(aot).Multiply(
                        new object?[] { new object?[] { "abc", "foobar"} },
                        new object?[] { new object?[0] })
                .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        public static IEnumerable<object?[]> MainWithArgsTestDataForCurrentRuntime()
        {
            IEnumerable<object?[]> data = MainWithArgsTestData(aot: false);
            return IsCoreClrRuntime ? data : data.Concat(MainWithArgsTestData(aot: true));
        }

        [Theory]
        [MemberData(nameof(MainWithArgsTestDataForCurrentRuntime))]
        [TestCategory("native")]
        public async Task AsyncMainWithArgs(Configuration config, bool aot, string[] args)
            => await TestMainWithArgs(config, aot, "async_main_with_args", "AsyncMainWithArgs.cs", args);

        [Theory]
        [MemberData(nameof(MainWithArgsTestDataForCurrentRuntime))]
        [TestCategory("native")]
        public async Task NonAsyncMainWithArgs(Configuration config, bool aot, string[] args)
            => await TestMainWithArgs(config, aot, "non_async_main_args", "SyncMainWithArgs.cs", args);

        [Fact]
        [TestCategory("coreclr")]
        [TestCategory("no-workload")]
        public async Task PackagedLaunchArguments()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "packaged_launch_arguments");
            UpdateFile(Path.Combine("Common", "Program.cs"), """
                using System;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                Console.WriteLine("args:" + JsonSerializer.Serialize(args, CliJsonContext.Default.StringArray));
                Console.WriteLine("env:" + Environment.GetEnvironmentVariable("CLI_VALUE"));
                Console.WriteLine("property:" + AppContext.GetData("Cli.Property"));
                Console.WriteLine("number:" + AppContext.GetData("Cli.Number"));
                Console.WriteLine("flag:" + (AppContext.TryGetSwitch("Cli.Flag", out bool flag) && flag));
                return 42;

                [JsonSerializable(typeof(string[]))]
                internal partial class CliJsonContext : JsonSerializerContext { }
                """);
            UpdateFile("WasmBasicTestApp.csproj", new Dictionary<string, string>
            {
                ["</Project>"] = """
                    <ItemGroup>
                      <WasmEnvironmentVariable Include="CLI_VALUE" Value="packaged" />
                      <RuntimeHostConfigurationOption Include="Cli.Property" Value="packaged" />
                    </ItemGroup>
                    </Project>
                    """
            });
            string frameworkDir = GetBinFrameworkDir(config, forPublish: true);
            string loader = Path.Combine(frameworkDir, "dotnet.js");
            UpdateFile("runtimeconfig.template.json", JsonSerializer.Serialize(new
            {
                wasmHostProperties = new
                {
                    defaultConfig = "node",
                    perHostConfig = new object[]
                    {
                        new { name = "node", host = "nodejs", jsPath = loader },
                        new { name = "browser", host = "browser", htmlPath = "index.html" }
                    }
                }
            }).Replace("\"jsPath\"", "\"js-path\"").Replace("\"htmlPath\"", "\"html-path\""));
            UpdateFile(Path.Combine("wwwroot", "main.js"), """
                import { dotnet } from './_framework/dotnet.js';
                dotnet.withEnvironmentVariable("CLI_VALUE", "builder");
                dotnet.withConfig({ runtimeConfig: { runtimeOptions: { configProperties: { "Cli.Property": "builder" } } } });
                const code = await dotnet.runMain();
                console.log(`WASM EXIT ${code}`);
                """);
            UpdateFile(Path.Combine("wwwroot", "WasmBasicTestApp.lib.module.js"), """
                export function onRuntimeConfigLoaded(config) {
                    config.environmentVariables.CLI_VALUE = "initializer";
                    config.runtimeConfig.runtimeOptions.configProperties["Cli.Property"] = "initializer";
                }
                """);
            PublishProject(info, config, new PublishOptions(AssertAppBundle: false, ExtraMSBuildArgs: "-p:WasmFingerprintAssets=false"), isNativeBuild: false);

            string[] appArgs = ["", "--help", "--dotnet-env=literal", "--", "a b", "a=b", "\u017Dlu\u0165ou\u010Dk\u00FD"];
            string[] launchArgs =
            [
                "--dotnet-env=CLI_VALUE=first",
                "--dotnet-env=CLI_VALUE=last=with space",
                "--dotnet-runtime-property=Cli.Property=\"first\"",
                "--dotnet-runtime-property=Cli.Property=\"launch\"",
                "--dotnet-runtime-property=Cli.Number=42.5",
                "--dotnet-runtime-property=Cli.Flag=true",
                "--",
                .. appArgs
            ];
            string expectedArgs = "args:" + JsonSerializer.Serialize(appArgs);

            (int exitCode, string output) = await RunProcess("node", [loader, .. launchArgs]);
            AssertLaunchResult(exitCode, output);

            (exitCode, output) = await RunProcess("node", [loader]);
            Assert.Equal(42, exitCode);
            Assert.Contains("args:[]", output);
            Assert.Contains("env:initializer", output);

            string bootstrap = Path.Combine(_projectDir, "launch.mjs");
            File.WriteAllText(bootstrap, $$"""
                import { dotnet } from {{JsonSerializer.Serialize(new Uri(loader).AbsoluteUri)}};
                process.env.CLI_VALUE = "inherited";
                await dotnet.withEnvironmentVariable("CLI_VALUE", "builder")
                    .withConfig({ runtimeConfig: { runtimeOptions: { configProperties: { "Cli.Property": "builder" } } } })
                    .runMainAndExit();
                """);
            (exitCode, output) = await RunProcess("node", [bootstrap, .. launchArgs]);
            AssertLaunchResult(exitCode, output);

            (exitCode, output) = await RunProcess(s_buildEnv.DotNet,
                ["run", "--no-build", "-c", "Debug", "--", .. launchArgs]);
            AssertLaunchResult(exitCode, output);

            (exitCode, output) = await RunProcess(s_buildEnv.DotNet,
                ["run", "--no-build", "-c", "Debug", "--", "--", "--dotnet-env=literal", "--"]);
            Assert.Equal(42, exitCode);
            Assert.Contains("args:[\"--dotnet-env=literal\",\"--\"]", output);

            (exitCode, output) = await RunProcess("node",
                ["--input-type=module", "--eval", $"await import({JsonSerializer.Serialize(new Uri(loader).AbsoluteUri)}); console.log('import-only');", "not-a-script"]);
            Assert.Equal(0, exitCode);
            Assert.Equal("import-only", output.Trim());

            foreach (string invalidOption in new[]
            {
                "--dotnet-env=missing-assignment",
                "--dotnet-runtime-property=Cli.Flag=null",
                "--dotnet-runtime-property=Cli.Flag={}",
                "--dotnet-runtime-property=Cli.Flag=NaN",
                "--dotnet-unknown"
            })
            {
                (exitCode, output) = await RunProcess("node", [loader, invalidOption]);
                Assert.NotEqual(0, exitCode);
                Assert.Contains("Invalid .NET launch option", output);
            }

            (exitCode, output) = await RunProcess("node", [loader, "--dotnet-help"]);
            Assert.Equal(0, exitCode);
            Assert.Contains("--dotnet-env=NAME=VALUE", output);
            Assert.DoesNotContain("args:", output);

            RunResult browserResult = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, ExpectedExitCode: 42,
                BrowserPath: "index.html?asset=a%20b.wasm",
                BrowserQueryString: new NameValueCollection { { "marker", "additional" } },
                ExtraArgs: "-p:WasmHostArguments=--host=browser -- --dotnet-env=CLI_VALUE=browser --dotnet-runtime-property=Cli.Property=17 --dotnet-runtime-property=Cli.Number=42.5 --dotnet-runtime-property=Cli.Flag=true -- --debug --host=application --")
            {
                ExecuteAfterLoaded = async (_, page) =>
                {
                    string headers = await page.EvaluateAsync<string>("""
                        async () => {
                            const params = new URLSearchParams(location.search);
                            if (params.get("asset") !== "a b.wasm" || params.get("marker") !== "additional")
                                throw new Error("Missing browser path or additional query parameters");
                            const path = params.get("dotnet-launch");
                            if (!path) throw new Error("Missing launch configuration URL");
                            const response = await fetch(path);
                            return `${response.status}:${response.headers.get("Cache-Control")}`;
                        }
                        """);
                    Assert.Equal("200:no-store", headers);
                }
            });
            Assert.Contains(browserResult.ConsoleOutput, line => line.Contains("args:[\"--debug\",\"--host=application\",\"--\"]"));
            Assert.Contains(browserResult.ConsoleOutput, line => line.Contains("env:browser"));
            Assert.Contains(browserResult.ConsoleOutput, line => line.Contains("property:17"));
            Assert.Contains(browserResult.ConsoleOutput, line => line.Contains("flag:True"));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(_projectDir, "bin"), "runArgs.json", SearchOption.AllDirectories));

            RunResult publishedResult = await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42,
                BrowserQueryString: new NameValueCollection { { "arg", "--dotnet-env=literal" }, { "arg", "--" } })
            {
                ExecuteAfterLoaded = async (_, page) =>
                {
                    bool fetchedLaunchConfig = await page.EvaluateAsync<bool>("""
                        () => performance.getEntriesByType("resource").some(entry => entry.name.includes("/dotnet-launch/"))
                        """);
                    Assert.False(fetchedLaunchConfig);
                }
            });
            Assert.Contains(publishedResult.ConsoleOutput, line => line.Contains("args:[\"--dotnet-env=literal\",\"--\"]"));

            void AssertLaunchResult(int code, string text)
            {
                Assert.Equal(42, code);
                Assert.Contains(expectedArgs, text);
                Assert.Contains("env:last=with space", text);
                Assert.Contains("property:launch", text);
                Assert.Contains("number:42.5", text);
                Assert.Contains("flag:True", text);
            }

            async Task<(int ExitCode, string Output)> RunProcess(string executable, string[] arguments)
            {
                var startInfo = new ProcessStartInfo(executable)
                {
                    WorkingDirectory = _projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                foreach (string argument in arguments)
                    startInfo.ArgumentList.Add(argument);
                foreach ((string key, string value) in s_buildEnv.EnvVars)
                    startInfo.Environment[key] = value;
                startInfo.Environment.Remove("CLI_VALUE");
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                finally
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                string text = await stdout + await stderr;
                _testOutput.WriteLine(text);
                return (process.ExitCode, text);
            }
        }

        [Theory]
        [InlineData("http://localhost:9000/?dotnet-launch=launch", "cache-seed.html?asset=a%20b.wasm", "?test=cache", "/cache-seed.html", "?dotnet-launch=launch&asset=a%20b.wasm&test=cache", "")]
        [InlineData("http://localhost:9000/", "cache-seed.html?asset=a.wasm", "", "/cache-seed.html", "?asset=a.wasm", "")]
        [InlineData("http://localhost:9000/?dotnet-launch=launch", "", "?arg=one&arg=two", "/", "?dotnet-launch=launch&arg=one&arg=two", "")]
        [InlineData("http://localhost:9000/?dotnet-launch=launch", "index.html#section", "", "/index.html", "?dotnet-launch=launch", "#section")]
        [InlineData("http://localhost:9000/?dotnet-launch=launch", "?asset=a.wasm#section", "?arg=", "/", "?dotnet-launch=launch&asset=a.wasm&arg=", "#section")]
        [TestCategory("no-workload")]
        public void BrowserUrlPreservesQuerySources(string browserUrl, string browserPath, string queryString, string expectedPath, string expectedQuery, string expectedFragment)
        {
            var result = new Uri(BuildBrowserUrl(browserUrl, browserPath, queryString));
            Assert.Equal(expectedPath, result.AbsolutePath);
            Assert.Equal(expectedQuery, result.Query);
            Assert.Equal(expectedFragment, result.Fragment);
        }

        [Theory]
        [InlineData("--dotnet-env=missing-assignment")]
        [InlineData("--dotnet-env==value")]
        [InlineData("--dotnet-runtime-property=Flag=null")]
        [InlineData("--dotnet-runtime-property=Flag=[]")]
        [InlineData("--dotnet-runtime-property=Flag=1e400")]
        [InlineData("--dotnet-runtime-property=Flag=\"\\u0000\"")]
        [InlineData("--dotnet-runtime-property=HOST_RUNTIME_CONTRACT=1")]
        [InlineData("--dotnet-unknown")]
        [TestCategory("no-workload")]
        public void InvalidLaunchArguments(string option)
        {
            CommandLineException error = Assert.Throws<CommandLineException>(() => LaunchArguments.Parse([option]));
            Assert.Contains("Invalid .NET launch option", error.Message);
        }

        [Theory]
        [InlineData(new[] { "--", "--dotnet-env=literal", "", "--" }, new[] { "--dotnet-env=literal", "", "--" })]
        [InlineData(new[] { "--help", "--dotnet-env=literal" }, new[] { "--help", "--dotnet-env=literal" })]
        [InlineData(new[] { "--dotnet-env=VALUE=a=b", "--dotnet-env=VALUE=", "--dotnet-runtime-property=Flag=false", "--dotnet-runtime-property=Flag=true", "--", "--debug" }, new[] { "--debug" })]
        [TestCategory("no-workload")]
        public void LaunchArgumentBoundaries(string[] arguments, string[] expected)
        {
            LaunchArguments result = LaunchArguments.Parse(arguments);
            Assert.Equal(expected, result.ApplicationArguments);
            if (result.EnvironmentVariables.TryGetValue("VALUE", out string? value))
            {
                Assert.Equal("", value);
                Assert.True(result.RuntimeProperties["Flag"].GetBoolean());
            }
        }

        async Task TestMainWithArgs(Configuration config,
                                bool aot,
                                string projectNamePrefix,
                                string projectContentsName,
                                string[] args)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, projectNamePrefix);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", projectContentsName));

            var queryArgs = new NameValueCollection();
            foreach (var arg in args)
                queryArgs.Add("arg", arg);
            PublishProject(info, config, new PublishOptions(AOT: aot));

            int argsCount = args.Length;
            int expectedCode = 42 + argsCount;
            RunResult output = await RunForPublishWithWebServer(
                new BrowserRunOptions(config, TestScenario: "MainWithArgs", BrowserQueryString: queryArgs, ExpectedExitCode: expectedCode));
            Assert.Contains(output.TestOutput, m => m.Contains($"args#: {argsCount}"));
            foreach (var arg in args)
                Assert.Contains(output.TestOutput, m => m.Contains($"arg: {arg}"));
        }
    }
}
