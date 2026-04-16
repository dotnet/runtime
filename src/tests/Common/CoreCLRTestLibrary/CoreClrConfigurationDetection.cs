// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TestLibrary;

public static class CoreClrConfigurationDetection
{
    public static bool IsJitStress => !string.Equals(GetEnvironmentVariableValue("JitStress"), "0", StringComparison.InvariantCulture);
    public static bool IsJitStressRegs => !string.Equals(GetEnvironmentVariableValue("JitStressRegs"), "0", StringComparison.InvariantCulture);
    public static bool IsJitMinOpts => string.Equals(GetEnvironmentVariableValue("JITMinOpts"), "1", StringComparison.InvariantCulture);
    public static bool IsTailCallStress => string.Equals(GetEnvironmentVariableValue("TailcallStress"), "1", StringComparison.InvariantCulture);
    public static bool IsDisableR2R => string.Equals(GetEnvironmentVariableValue("ReadyToRun"), "0", StringComparison.InvariantCulture);
    public static bool IsGCStress3 => CompareGCStressModeAsLower(GetEnvironmentVariableValue("GCStress"), "0x3", "3");
    public static bool IsGCStressC => CompareGCStressModeAsLower(GetEnvironmentVariableValue("GCStress"), "0xC", "C");
    public static bool IsTieredCompilation => string.Equals(GetEnvironmentVariableValue("TieredCompilation", "1"), "1", StringComparison.InvariantCulture);
    public static bool IsHeapVerify => string.Equals(GetEnvironmentVariableValue("HeapVerify"), "1", StringComparison.InvariantCulture);

    public static bool IsCoreClrInterpreter
    {
        get
        {
            // WASM-TODO: update when codegen is in place
            if (PlatformDetection.IsWasm)
                return true;
            if (!string.IsNullOrWhiteSpace(GetEnvironmentVariableValue("Interpreter", "")))
                return true;
            if (int.TryParse(GetEnvironmentVariableValue("InterpMode", "0"), out int mode) && (mode > 0))
                return true;
            return false;
        }
    }

    public static bool IsGCStress => !string.Equals(GetEnvironmentVariableValue("GCStress"), "0", StringComparison.InvariantCulture);

    public static bool IsAnyJitStress => IsJitStress || IsJitStressRegs || IsJitMinOpts || IsTailCallStress;

    public static bool IsAnyJitOptimizationStress => IsAnyJitStress || IsTieredCompilation;

    /// <summary>
    /// Returns true if the given assembly file on disk contains a ReadyToRun header
    /// (i.e. was precompiled to native code by crossgen2). Both single-file R2R and
    /// composite R2R are detected: for composite images, crossgen2 rewrites each
    /// component MSIL assembly with a real R2R header whose <c>Flags</c> has
    /// <c>READYTORUN_FLAG_COMPONENT</c> set and whose only section is an
    /// <c>OwnerCompositeExecutable</c> string referencing the composite file name
    /// (see <c>docs/design/coreclr/botr/readytorun-format.md</c>). In both cases,
    /// <c>CorHeader.ManagedNativeHeaderDirectory</c> has a non-zero size.
    ///
    /// Note: this reports whether the assembly file is R2R-compiled, not whether the
    /// runtime is actually executing the R2R code (e.g. <c>DOTNET_ReadyToRun=0</c> leaves
    /// the header intact but disables R2R use at runtime). That is the desired semantic
    /// for skip attributes: if the test assembly was R2R'd, skip it even if the current
    /// run happens to have disabled R2R execution.
    /// </summary>
    public static bool IsAssemblyReadyToRunCompiled(Assembly assembly)
    {
        string? location = assembly.Location;
        if (string.IsNullOrEmpty(location))
            return false;

        try
        {
            using FileStream fs = File.OpenRead(location);
            using PEReader pe = new PEReader(fs);
            return pe.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory.Size > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsCheckedRuntime => AssemblyConfigurationEquals("Checked");
    public static bool IsReleaseRuntime => AssemblyConfigurationEquals("Release");
    public static bool IsDebugRuntime => AssemblyConfigurationEquals("Debug");

    public static bool IsStressTest =>
        IsGCStress ||
        IsDisableR2R ||
        IsAnyJitStress ||
        IsHeapVerify;

    private static string GetEnvironmentVariableValue(string name, string defaultValue = "0") =>
        Environment.GetEnvironmentVariable("DOTNET_" + name) ?? Environment.GetEnvironmentVariable("COMPlus_" + name) ?? defaultValue;

    private static bool AssemblyConfigurationEquals(string configuration)
    {
        AssemblyConfigurationAttribute assemblyConfigurationAttribute = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

        return assemblyConfigurationAttribute != null &&
            string.Equals(assemblyConfigurationAttribute.Configuration, configuration, StringComparison.InvariantCulture);
    }

    private static bool CompareGCStressModeAsLower(string value, string first, string second)
    {
        value = value.ToLowerInvariant();
        return string.Equals(value, first.ToLowerInvariant(), StringComparison.InvariantCulture) ||
            string.Equals(value, second.ToLowerInvariant(), StringComparison.InvariantCulture) ||
            string.Equals(value, "0xf", StringComparison.InvariantCulture) ||
            string.Equals(value, "f", StringComparison.InvariantCulture);
    }
}
