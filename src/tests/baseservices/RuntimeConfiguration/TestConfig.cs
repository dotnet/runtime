// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime;

using Xunit;

class TestConfig
{
    const int Success = 100;
    const int Fail = 101;

    [Fact]
    [EnvVar("DOTNET_gcServer", "1")]
    static int Verify_ServerGC_Env_Enable(string[] _)
    {
        return GCSettings.IsServerGC
            ? Success
            : Fail;
    }

    [Fact]
    [EnvVar("DOTNET_gcServer", "0")]
    static int Verify_ServerGC_Env_Disable(string[] _)
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

    [Fact]
    [ConfigProperty("System.GC.Server", "true")]
    static int Verify_ServerGC_Prop_Enable(string[] _)
    {
        return GCSettings.IsServerGC
            ? Success
            : Fail;
    }

    [Fact]
    [ConfigProperty("System.GC.Server", "false")]
    static int Verify_ServerGC_Prop_Disable(string[] _)
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

    [Fact]
    [EnvVar("DOTNET_gcServer", "0")]
    [ConfigProperty("System.GC.Server", "true")]
    static int Verify_ServerGC_Env_Override_Prop(string[] _)
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return RunTests();
        }

        MethodInfo infos = typeof(TestConfig).GetMethod(args[0], BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (infos is null)
        {
            return Fail;
        }
        return (int)infos.Invoke(null, new object[] { args[1..] });
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    class EnvVarAttribute : Attribute
    {
        public EnvVarAttribute(string name, string value) { Name = name; Value = value; }
        public string Name { get; init; }
        public string Value { get; init; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    class ConfigPropertyAttribute : Attribute
    {
        public ConfigPropertyAttribute(string name, string value) { Name = name; Value = value; }
        public string Name { get; init; }
        public string Value { get; init; }
    }

    static int RunTests()
    {
        // clear some environment variables that we will set during the test run
        Environment.SetEnvironmentVariable("DOTNET_gcServer", null);
        Environment.SetEnvironmentVariable("COMPlus_gcServer", null);

        string corerunPath = GetCorerunPath();
        MethodInfo[] infos = typeof(TestConfig).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var mi in infos)
        {
            var factMaybe = mi.GetCustomAttributes(typeof(FactAttribute));
            if (!factMaybe.Any())
            {
                continue;
            }

            using Process process = new();

            StringBuilder arguments = new();
            var configProperties = mi.GetCustomAttributes(typeof(ConfigPropertyAttribute));

            foreach (Attribute cp in configProperties)
            {
                ConfigPropertyAttribute configProp = (ConfigPropertyAttribute)cp;
                arguments.Append($"-p {configProp.Name}={configProp.Value} ");
            }

            arguments.Append($"\"{System.Reflection.Assembly.GetExecutingAssembly().Location}\" {mi.Name}");

            process.StartInfo.FileName = corerunPath;
            process.StartInfo.Arguments = arguments.ToString();

            var envVariables = mi.GetCustomAttributes(typeof(EnvVarAttribute));
            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                process.StartInfo.EnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            }

            Console.WriteLine($"Running: {process.StartInfo.Arguments}");
            foreach (Attribute ev in envVariables)
            {
                EnvVarAttribute envVar = (EnvVarAttribute)ev;
                process.StartInfo.EnvironmentVariables[envVar.Name] = envVar.Value;
                Console.WriteLine($"    set {envVar.Name}={envVar.Value}");
            }

            process.Start();
            process.WaitForExit();
            if (process.ExitCode != Success)
            {
                Console.WriteLine($"Failed: {mi.Name}");
                return process.ExitCode;
            }
        }

        return Success;
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
