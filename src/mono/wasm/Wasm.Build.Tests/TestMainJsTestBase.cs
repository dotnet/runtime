// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class TestMainJsTestBase : BuildTestBase
{
    protected TestMainJsProjectProvider _provider;
    protected TestMainJsTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(new TestMainJsProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<TestMainJsProjectProvider>();
    }

    public (string projectDir, string buildOutput) BuildProject(BuildArgs buildArgs,
                              string id,
                              BuildProjectOptions options)
    {
        string msgPrefix = options.Label != null ? $"[{options.Label}] " : string.Empty;
        if (options.UseCache && _buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
        {
            _testOutput.WriteLine($"Using existing build found at {product.ProjectDir}, with build log at {product.LogFile}");

            if (!product.Result)
                throw new XunitException($"Found existing build at {product.ProjectDir}, but it had failed. Check build log at {product.LogFile}");
            _projectDir = product.ProjectDir;

            // use this test's id for the run logs
            _logPath = Path.Combine(s_buildEnv.LogRootPath, id);
            return (_projectDir, product.BuildOutput);
        }

        if (options.CreateProject)
        {
            InitPaths(id);
            InitProjectDir(_projectDir);
            options.InitProject?.Invoke();

            File.WriteAllText(Path.Combine(_projectDir, $"{buildArgs.ProjectName}.csproj"), buildArgs.ProjectFileContents);
            File.Copy(
                Path.Combine(
                    AppContext.BaseDirectory,
                    options.TargetFramework == "net7.0"
                        ? "data/test-main-7.0.js"
                        : "test-main.js"
                ),
                Path.Combine(_projectDir, "test-main.js")
            );

            File.WriteAllText(Path.Combine(_projectDir!, "index.html"), @"<html><body><script type=""module"" src=""test-main.js""></script></body></html>");
        }
        else if (_projectDir is null)
        {
            throw new Exception("_projectDir should be set, to use options.createProject=false");
        }

        if (options.ExtraBuildEnvironmentVariables is null)
            options = options with { ExtraBuildEnvironmentVariables = new Dictionary<string, string>() };
        options.ExtraBuildEnvironmentVariables["ForceNet8Current"] = "false";

        try
        {
            (CommandResult res, string logFilePath) = BuildProjectWithoutAssert(id,
                                                                                buildArgs.Config,
                                                                                options,
                                                                                string.Join(" ", buildArgs.ExtraBuildArgs));

            if (options.ExpectSuccess && options.AssertAppBundle)
            {
                ProjectProviderBase.AssertRuntimePackPath(res.Output, options.TargetFramework ?? DefaultTargetFramework);
                _provider.AssertBundle(buildArgs, options);
            }

            if (options.UseCache)
                _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, logFilePath, true, res.Output));

            return (_projectDir, res.Output);
        }
        catch (Exception ex)
        {
            if (options.UseCache)
                _buildContext.CacheBuild(buildArgs, new BuildProduct(_projectDir, /*logFilePath*/"unset-log-path", false, $"The build attempt resulted in exception: {ex}."));
            throw;
        }
    }
}
