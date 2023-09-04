// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;

using Xunit;

public class TestConfigTester
{
    [Fact]
    public static void RunTests()
    {
        // clear some environment variables that we will set during the test run
        Environment.SetEnvironmentVariable("DOTNET_gcServer", null);

        string testConfigApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestConfig.dll");

        MethodInfo[] infos = typeof(TestConfig).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        string corerunPath = GetCorerunPath();
        foreach (var mi in infos)
        {
            var configProperties = mi.GetCustomAttributes(typeof(TestConfig.ConfigPropertyAttribute));
            var envVariables = mi.GetCustomAttributes(typeof(TestConfig.EnvVarAttribute));

            if (configProperties.Count() == 0 && envVariables.Count() == 0)
            {
                continue;
            }

            using Process process = new();

            StringBuilder arguments = new();

            foreach (Attribute cp in configProperties)
            {
                TestConfig.ConfigPropertyAttribute configProp = (TestConfig.ConfigPropertyAttribute)cp;
                arguments.Append($"-p {configProp.Name}={configProp.Value} ");
            }

            arguments.Append($"\"{testConfigApp}\" {mi.Name}");

            process.StartInfo.FileName = corerunPath;
            process.StartInfo.Arguments = arguments.ToString();

            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                process.StartInfo.EnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            }

            Console.WriteLine($"Running: {process.StartInfo.Arguments}");
            foreach (Attribute ev in envVariables)
            {
                TestConfig.EnvVarAttribute envVar = (TestConfig.EnvVarAttribute)ev;
                process.StartInfo.EnvironmentVariables[envVar.Name] = envVar.Value;
                Console.WriteLine($"    set {envVar.Name}={envVar.Value}");
            }

            process.Start();
            process.WaitForExit();
            if (process.ExitCode != TestConfig.Success)
            {
                throw new Exception($"Failed: {mi.Name}: exit code = {process.ExitCode}");
            }
        }
    }

    static string GetCorerunPath()
    {
        string corerunName = "corerun";
        if (TestLibrary.Utilities.IsWindows)
        {
            corerunName += ".exe";
        }
        return Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), corerunName);
    }
}
