// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmTemplateTestsBase : BlazorWasmTestBase
    {
        public WasmTemplateTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        private string StringReplaceWithAssert(string oldContent, string oldValue, string newValue)
        {
            string newContent = oldContent.Replace(oldValue, newValue);
            if (oldValue != newValue && oldContent == newContent)
                throw new XunitException($"Replacing '{oldValue}' with '{newValue}' did not change the content '{oldContent}'");

            return newContent;
        }

        protected void UpdateProjectFile(string pathRelativeToProjectDir, Dictionary<string, string> replacements)
        {
            var path = Path.Combine(_projectDir!, pathRelativeToProjectDir);
            string text = File.ReadAllText(path);
            foreach (var replacement in replacements)
            {
                text = StringReplaceWithAssert(text, replacement.Key, replacement.Value);
            }
            File.WriteAllText(path, text);
        }

        protected void RemoveContentsFromProjectFile(string pathRelativeToProjectDir, string afterMarker, string beforeMarker)
        {
            var path = Path.Combine(_projectDir!, pathRelativeToProjectDir);
            string text = File.ReadAllText(path);
            int start = text.IndexOf(afterMarker);
            int end = text.IndexOf(beforeMarker, start);
            if (start == -1 || end == -1)
                throw new XunitException($"Start or end marker not found in '{path}'");
            start += afterMarker.Length;
            text = text.Remove(start, end - start);
            // separate the markers with a new line
            text = text.Insert(start, "\n");
            File.WriteAllText(path, text);
        }

        protected void UpdateBrowserMainJs(string targetFramework, string runtimeAssetsRelativePath = DefaultRuntimeAssetsRelativePath)
        {
            base.UpdateBrowserMainJs(
                (mainJsContent) =>
                {
                    // .withExitOnUnhandledError() is available only only >net7.0
                    mainJsContent = StringReplaceWithAssert(
                        mainJsContent,
                        ".create()",
                        (targetFramework == "net8.0" || targetFramework == "net9.0")
                            ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                            : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()"
                    );

                    // dotnet.run() is already used in <= net8.0
                    if (targetFramework != "net8.0")
                        mainJsContent = StringReplaceWithAssert(mainJsContent, "runMain()", "dotnet.run()");

                    mainJsContent = StringReplaceWithAssert(mainJsContent, "from './_framework/dotnet.js'", $"from '{runtimeAssetsRelativePath}dotnet.js'");

                    return mainJsContent;
                },
                targetFramework,
                runtimeAssetsRelativePath
            );
        }

        protected void UpdateMainJsEnvironmentVariables(params (string key, string value)[] variables)
        {
            string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
            string mainJsContent = File.ReadAllText(mainJsPath);

            StringBuilder js = new();
            foreach (var variable in variables)
            {
                js.Append($".withEnvironmentVariable(\"{variable.key}\", \"{variable.value}\")");
            }

            mainJsContent = StringReplaceWithAssert(mainJsContent, ".create()", js.ToString() + ".create()");

            File.WriteAllText(mainJsPath, mainJsContent);
        }

        protected string RunConsole(BuildArgs buildArgs, int expectedExitCode = 42, string language = "en-US")
        {
            CommandResult res = new RunCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("LANG", language)
                .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {buildArgs.Config}")
                .EnsureExitCode(expectedExitCode);
            return res.Output;
        }

        protected async Task<string> RunBrowser(string config, string projectFile, string language = "en-US")
        {
            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(_projectDir!);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --no-build --project \"{projectFile}\" --forward-console", language: language);
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("WASM EXIT 42", string.Join(Environment.NewLine, runner.OutputLines));
            return string.Join("\n", runner.OutputLines);
        }
    }
}
