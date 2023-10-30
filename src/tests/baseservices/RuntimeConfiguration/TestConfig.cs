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

public class TestConfig
{
    public const int Success = 100;
    public const int Fail = 101;

    [EnvVar("DOTNET_gcServer", "1")]
    public static int Verify_ServerGC_Env_Enable()
    {
        return GCSettings.IsServerGC
            ? Success
            : Fail;
    }

    [EnvVar("DOTNET_gcServer", "0")]
    public static int Verify_ServerGC_Env_Disable()
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

    [ConfigProperty("System.GC.Server", "true")]
    public static int Verify_ServerGC_Prop_Enable()
    {
        return GCSettings.IsServerGC
            ? Success
            : Fail;
    }

    [ConfigProperty("System.GC.Server", "false")]
    public static int Verify_ServerGC_Prop_Disable()
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

    [EnvVar("DOTNET_gcServer", "0")]
    [ConfigProperty("System.GC.Server", "true")]
    public static int Verify_ServerGC_Env_Override_Prop()
    {
        return GCSettings.IsServerGC
            ? Fail
            : Success;
    }

#if !IS_TESTER_APP
    static int Main(string[] args)
    {
        MethodInfo infos = typeof(TestConfig).GetMethod(args[0], BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (infos is null)
        {
            return Fail;
        }
        return (int)infos.Invoke(null, Array.Empty<object>());
    }
#endif

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class EnvVarAttribute : Attribute
    {
        public EnvVarAttribute(string name, string value) { Name = name; Value = value; }
        public string Name { get; init; }
        public string Value { get; init; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ConfigPropertyAttribute : Attribute
    {
        public ConfigPropertyAttribute(string name, string value) { Name = name; Value = value; }
        public string Name { get; init; }
        public string Value { get; init; }
    }

}
