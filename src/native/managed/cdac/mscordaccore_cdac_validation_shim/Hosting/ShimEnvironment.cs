// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Reads the environment knobs that configure the validation shim and locates the two binaries it
/// brokers between: the canonical production cDAC that ships next to the shim, and the legacy DAC.
/// </summary>
internal static unsafe partial class ShimEnvironment
{
    /// <summary>Selects fallback vs strict behavior. Values: <c>fallback</c> (default) or <c>strict</c>.</summary>
    internal const string ValidationModeVariable = "DOTNET_CDAC_VALIDATION_MODE";

    /// <summary>Full path to the legacy DAC (mscordaccore) the shim compares against.</summary>
    internal const string LegacyDacPathVariable = "DOTNET_CDAC_LEGACY_DAC_PATH";

    /// <summary>
    /// Optional override for the production cDAC. When unset the shim uses the canonical cDAC that
    /// sits next to the shim binary.
    /// </summary>
    internal const string ProductionCDacPathVariable = "DOTNET_CDAC_PRODUCTION_PATH";

    private const string ProductionCDacName = "mscordaccore_universal";

    private static readonly ValidationMode s_mode = ParseMode(Environment.GetEnvironmentVariable(ValidationModeVariable));

    internal static ValidationMode Mode => s_mode;

    private static ValidationMode ParseMode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationMode.Fallback;

        if (value.Equals("strict", StringComparison.OrdinalIgnoreCase))
            return ValidationMode.Strict;

        if (value.Equals("fallback", StringComparison.OrdinalIgnoreCase))
            return ValidationMode.Fallback;

        ShimLog.Error($"Unrecognized {ValidationModeVariable} value '{value}'; using 'fallback'.");
        return ValidationMode.Fallback;
    }

    /// <summary>
    /// The production cDAC path: <see cref="ProductionCDacPathVariable"/> when set, otherwise the
    /// canonical cDAC adjacent to this shim binary.
    /// </summary>
    internal static string? GetProductionCDacPath()
    {
        string? overridePath = Environment.GetEnvironmentVariable(ProductionCDacPathVariable);
        if (!string.IsNullOrEmpty(overridePath))
            return overridePath;

        string? directory = GetShimDirectory();
        if (directory is null)
            return null;

        return Path.Combine(directory, GetNativeLibraryFileName(ProductionCDacName));
    }

    /// <summary>The legacy DAC path, or <c>null</c> when the shim should run without a legacy DAC.</summary>
    internal static string? GetLegacyDacPath()
    {
        string? path = Environment.GetEnvironmentVariable(LegacyDacPathVariable);
        return string.IsNullOrEmpty(path) ? null : path;
    }

    internal static string GetNativeLibraryFileName(string baseName)
    {
        if (OperatingSystem.IsWindows())
            return baseName + ".dll";
        if (OperatingSystem.IsMacOS())
            return "lib" + baseName + ".dylib";
        return "lib" + baseName + ".so";
    }

    /// <summary>
    /// Directory containing this shim binary. The shim is loaded as a native library by an
    /// arbitrary host (the debugger), so the process base directory is not usable; the module is
    /// located from the address of a function compiled into it.
    /// </summary>
    internal static string? GetShimDirectory()
    {
        string? modulePath = GetShimModulePath();
        return modulePath is null ? null : Path.GetDirectoryName(modulePath);
    }

    private static string? s_shimModulePath;
    private static bool s_shimModulePathResolved;

    private static string? GetShimModulePath()
    {
        if (s_shimModulePathResolved)
            return s_shimModulePath;

        s_shimModulePath = ResolveShimModulePath();
        s_shimModulePathResolved = true;
        return s_shimModulePath;
    }

    // Address anchor: a function guaranteed to live in this module.
    private static void ModuleAnchor()
    {
    }

    private static string? ResolveShimModulePath()
    {
        delegate*<void> anchor = &ModuleAnchor;
        try
        {
            if (OperatingSystem.IsWindows())
                return ResolveWindowsModulePath((IntPtr)anchor);

            return ResolveUnixModulePath((IntPtr)anchor);
        }
        catch (Exception ex)
        {
            ShimLog.Error($"Unable to locate the validation shim module: {ex.Message}");
            return null;
        }
    }

    private const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004;
    private const uint GET_MODULE_HANDLE_EX_FLAG_PIN = 0x00000001;

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
    private static partial int GetModuleHandleExW(uint flags, IntPtr address, out IntPtr module);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true)]
    private static partial uint GetModuleFileNameW(IntPtr module, char* fileName, uint size);

    private static string? ResolveWindowsModulePath(IntPtr address)
    {
        // Pin the shim itself as well: it must stay loaded for the lifetime of the process because
        // the COM objects it hands out are owned by it.
        if (GetModuleHandleExW(
                GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                address,
                out IntPtr module) == 0)
        {
            return null;
        }

        const int MaxLongPath = 1024;
        char* buffer = stackalloc char[MaxLongPath];
        uint length = GetModuleFileNameW(module, buffer, MaxLongPath);
        if (length == 0 || length >= MaxLongPath)
            return null;

        return new string(buffer, 0, (int)length);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DlInfo
    {
        public IntPtr FileName;
        public IntPtr BaseAddress;
        public IntPtr SymbolName;
        public IntPtr SymbolAddress;
    }

    [LibraryImport("libc", EntryPoint = "dladdr")]
    private static partial int dladdr_libc(IntPtr address, out DlInfo info);

    [LibraryImport("libdl.so.2", EntryPoint = "dladdr")]
    private static partial int dladdr_libdl(IntPtr address, out DlInfo info);

    private static string? ResolveUnixModulePath(IntPtr address)
    {
        DlInfo info;
        int result;
        try
        {
            result = dladdr_libc(address, out info);
        }
        catch (EntryPointNotFoundException)
        {
            result = dladdr_libdl(address, out info);
        }
        catch (DllNotFoundException)
        {
            result = dladdr_libdl(address, out info);
        }

        if (result == 0 || info.FileName == IntPtr.Zero)
            return null;

        return Marshal.PtrToStringUTF8(info.FileName);
    }
}
