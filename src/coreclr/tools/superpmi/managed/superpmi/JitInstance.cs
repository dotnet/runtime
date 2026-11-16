// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace SuperPmi.Replay;

internal enum ReplayResult
{
    Success,
    Error,
    Miss,
}

internal struct ReplayResults
{
    public ReplayResult Result;
    public bool IsMinOpts;
    public long NumExecutedInstructions;
    public CompileResult CompileResults;
}

// An exception that escaped the JIT natively, such as an access violation.
internal sealed class NativeException(uint code) : Exception($"Unexpected exception {code:x} was thrown.")
{
    public uint Code { get; } = code;
}

// A loaded JIT, with the JIT host and the JIT-EE interface that it compiles methods with.
internal sealed unsafe class JitInstance
{
    private const int E_FAIL = unchecked((int)0x80004005);

    private IntPtr hLib;
    private delegate* unmanaged<IntPtr, void> pnjitStartup;
    private CorInfoImpl icji;
    private IntPtr configProvider;
    private readonly Stopwatch stj = new();

    private LightWeightMap<uint, uint> forceOptions;
    private LightWeightMap<uint, uint> options;
    private MethodContext.ConfigEnvironment environment;

    private bool forceClearAltJitFlag;
    private bool forceSetAltJitFlag;

    // The value of the string config option asked for, between the native JIT host's calls for its size and its data.
    private byte[] pendingStringConfigValue;

    public readonly CycleTimer lt = new();
    public MethodContext mc;
    public readonly ulong[] times = new ulong[2];
    public IntPtr pJitInstance;

    // The loaded JIT module, to detect when the baseline and diff JITs are the same module (which shares global state).
    public IntPtr Module => hLib;

    public static JitInstance InitJit(string nameOfJit, bool breakOnAssert, Stopwatch st1, MethodContext firstContext,
        LightWeightMap<uint, uint> forceOptions, LightWeightMap<uint, uint> options)
    {
        var jit = new JitInstance { forceOptions = forceOptions, options = options };

        // The flag to invoke the JIT as an altjit is in the JIT flags, not the environment. If "-jitoption force"
        // forces AltJit off or on, propagate that to the JIT flags.
        foreach (string name in (ReadOnlySpan<string>)["AltJit", "AltJitNgen"])
        {
            string altJitFlag = jit.getForceOption(name);
            if (altJitFlag == "")
                jit.forceClearAltJitFlag = true;
            else if (altJitFlag != null)
                jit.forceSetAltJitFlag = true;
        }

        st1?.Restart();
        int hr = jit.StartUp(nameOfJit, breakOnAssert, firstContext);
        st1?.Stop();
        if (hr != 0)
        {
            Logger.LogError($"Startup of JIT({nameOfJit}) failed {hr}");
            return null;
        }
        if (st1 != null)
            Logger.LogVerbose($"Jit startup took {st1.Elapsed.TotalMilliseconds:F6}ms");

        return jit;
    }

    private int StartUp(string pathToJit, bool breakOnDebugBreakorAV, MethodContext firstContext)
    {
        SpmiUtil.BreakOnDebugBreakorAV = breakOnDebugBreakorAV;

        string fullPath = Path.GetFullPath(pathToJit);
        if (OperatingSystem.IsWindows() && File.Exists(fullPath))
        {
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(fullPath);
            Logger.LogDebug($"'{fullPath}' is version {version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart}.{version.FilePrivatePart}");
        }

        // The error codes are ERROR_MOD_NOT_FOUND and ERROR_PROC_NOT_FOUND.
        if (!NativeLibrary.TryLoad(fullPath, out hLib))
        {
            Logger.LogError("LoadLibrary failed (0x0000007e)");
            return E_FAIL;
        }

        if (!NativeLibrary.TryGetExport(hLib, "getJit", out IntPtr pngetJit))
        {
            Logger.LogError("GetProcAddress 'getJit' failed (0x0000007f)");
            return -1;
        }

        // Set up the JIT host and call jitStartup, if the JIT has it.
        if (NativeLibrary.TryGetExport(hLib, "jitStartup", out IntPtr jitStartup))
        {
            pnjitStartup = (delegate* unmanaged<IntPtr, void>)jitStartup;
            mc = firstContext;
            configProvider = CreateConfigProvider();
            callJitStartup();
        }

        pJitInstance = ((delegate* unmanaged<IntPtr>)pngetJit)();
        if (pJitInstance == IntPtr.Zero)
        {
            Logger.LogError("pngetJit gave us null");
            return -1;
        }

        Guid versionId = default;
        SpmiJitInterface.JitGetVersionIdentifier(pJitInstance, &versionId);
        if (versionId != JitEEVersion.Identifier)
        {
            // A mismatched version ID fails the load.
            pJitInstance = IntPtr.Zero;
            Logger.LogError($"Jit Compiler has wrong version identifier. Expected: {JitEEVersion.Identifier:D}. Actual: {versionId:D}.");
            return -1;
        }

        icji = new CorInfoImpl(this);
        return 0;
    }

    private void callJitStartup()
    {
        pnjitStartup(SpmiJitInterface.GetJitHost(configProvider));
        environment = mc.cloneEnvironment();
    }

    // Starts the JIT up again, so that it reads the configuration of 'firstContext'.
    public bool resetConfig(MethodContext firstContext)
    {
        if (pnjitStartup == null)
            return false;

        mc = firstContext;
        callJitStartup();
        return true;
    }

    public MethodContext.ConfigEnvironment getEnvironment() => environment;

    public void updateForceOptions(LightWeightMap<uint, uint> newForceOptions) => forceOptions = newForceOptions;

    public ReplayResults CompileMethod(MethodContext methodToCompile, int mcIndex, bool collectThroughput)
    {
        var results = new ReplayResults { Result = ReplayResult.Success };
        CORINFO_METHOD_INFO info = default;
        uint flags = 0;

        // The raw values, to figure things out later.
        mc = methodToCompile;
        times[0] = 0;
        times[1] = 0;
        stj.Restart();

        ulong insCountBefore = 0;
        SpmiJitInterface.Instrumentor_GetInsCount(&insCountBefore);

        try
        {
            CORINFO_OS os = CORINFO_OS.CORINFO_WINNT;
            mc.repCompileMethod(&info, &flags, &os);

            CORJIT_FLAGS jitFlags = default;
            getJitFlags(ref jitFlags, (uint)sizeof(CORJIT_FLAGS));
            results.IsMinOpts = jitFlags.IsSet(CorJitFlag.CORJIT_FLAG_DEBUG_CODE) || jitFlags.IsSet(CorJitFlag.CORJIT_FLAG_MIN_OPT) ||
                jitFlags.IsSet(CorJitFlag.CORJIT_FLAG_TIER0);

            if (collectThroughput)
                lt.Start();
            SpmiJitInterface.JitSetOs(pJitInstance, os);
            CorJitResult jitResult = icji.CompileMethod(pJitInstance, &info, flags, out byte* nativeEntry, out uint nativeSizeOfCode);
            if (collectThroughput)
            {
                lt.Stop();
                times[0] = lt.GetCycles();
            }

            CorInfoMethodRuntimeFlags methodFlags = mc.cr.repSetMethodAttribs(info.ftn);
            if ((methodFlags & CorInfoMethodRuntimeFlags.CORINFO_FLG_SWITCHED_TO_MIN_OPT) != 0)
                results.IsMinOpts = true;
            else if ((methodFlags & CorInfoMethodRuntimeFlags.CORINFO_FLG_SWITCHED_TO_OPTIMIZED) != 0)
                results.IsMinOpts = false;

            // An altjit skips the methods of other target architectures, which isn't a failure. Neither is
            // skipping the methods of an altjit that matches the target, with RunAltJitCode=0 (which stops the
            // generated code from being used).
            if (jitResult == CorJitResult.CORJIT_SKIPPED &&
                (!MatchesTargetArchitecture((CorInfoArch)mc.repGetExpectedTargetArchitecture()) ||
                 (jitFlags.IsSet(CorJitFlag.CORJIT_FLAG_ALT_JIT) && GetIntConfigValue("RunAltJitCode"u8, 1) == 0)))
            {
                jitResult = CorJitResult.CORJIT_OK;
            }

            if (jitResult is CorJitResult.CORJIT_OK or CorJitResult.CORJIT_BADCODE or CorJitResult.CORJIT_R2R_UNSUPPORTED)
            {
                // Capture the results of the compilation.
                mc.cr.recCompileMethod(nativeEntry, nativeSizeOfCode, jitResult);
                mc.cr.recAllocMemCapture();
                mc.cr.recAllocGCInfoCapture();
                mc.cr.recMessageLog(jitResult == CorJitResult.CORJIT_OK ? "Successful Compile" : "Successful Compile (BADCODE)");
            }
            else
            {
                Logger.LogDebug($"compileMethod failed with result {(int)jitResult}");
                results.Result = ReplayResult.Error;
            }
        }
        catch (SpmiException e) when (e.Code == ExceptionCodes.MC)
        {
            Logger.LogMissing($"Method context {mcIndex} failed to replay: {e.Message}");
            results.Result = ReplayResult.Miss;
        }
        catch (SpmiException e) when (e.Code == ExceptionCodes.RECORDED_EXCEPTION)
        {
            // An exception that the EE threw during recording, such as a MissingFieldException from
            // resolveToken. Several JIT-EE APIs can throw exceptions, which the recorder rethrows under this
            // code. These aren't replay failures; capture that no code or GC info was generated.
            mc.cr.recAllocMemCapture();
            mc.cr.recAllocGCInfoCapture();
            mc.cr.recMessageLog("Successful Compile (EE API exception)");
        }
        catch (SpmiException e)
        {
            Logger.LogError($"Exception thrown: {e.Message}");
            results.Result = ReplayResult.Error;
        }
        catch (NativeException e)
        {
            Logger.LogError(e.Message);
            results.Result = ReplayResult.Error;
        }
        catch (Exception e)
        {
            Logger.LogError($"Exception thrown: {e}");
            results.Result = ReplayResult.Error;
        }

        stj.Stop();

        // Reaching here with throughput collection means the method compiles.
        if (collectThroughput)
            timeResult(&info, flags);

        mc.cr.secondsToCompile = stj.Elapsed.TotalSeconds;
        results.CompileResults = mc.cr;

        ulong insCountAfter = 0;
        SpmiJitInterface.Instrumentor_GetInsCount(&insCountAfter);
        results.NumExecutedInstructions = (long)(insCountAfter - insCountBefore);
        return results;
    }

    private static bool MatchesTargetArchitecture(CorInfoArch arch)
    {
        TargetArchitecture? expected = arch switch
        {
            CorInfoArch.CORINFO_ARCH_X64 => TargetArchitecture.X64,
            CorInfoArch.CORINFO_ARCH_X86 => TargetArchitecture.X86,
            CorInfoArch.CORINFO_ARCH_ARM => TargetArchitecture.ARM,
            CorInfoArch.CORINFO_ARCH_ARM64 => TargetArchitecture.ARM64,
            CorInfoArch.CORINFO_ARCH_LOONGARCH64 => TargetArchitecture.LoongArch64,
            CorInfoArch.CORINFO_ARCH_RISCV64 => TargetArchitecture.RiscV64,
            CorInfoArch.CORINFO_ARCH_WASM32 => TargetArchitecture.Wasm32,
            _ => null,
        };
        if (expected == null)
            Logger.LogError("Unknown target architecture");
        return expected == SpmiUtil.GetSpmiTargetArchitecture();
    }

    // Compiles the method ten more times, keeping the two smallest times (whose closest pair reduces the noise).
    private void timeResult(CORINFO_METHOD_INFO* info, uint flags)
    {
        for (int i = 0; i < 10; i++)
        {
            mc.cr = new CompileResult();
            lt.Start();
            icji.CompileMethod(pJitInstance, info, flags, out _, out _);
            lt.Stop();
            ulong time = lt.GetCycles();
            if (times[1] == 0 || time < times[1])
            {
                if (time < times[0])
                {
                    times[1] = times[0];
                    times[0] = time;
                }
                else
                {
                    times[1] = time;
                }
            }
        }
    }

    private string getForceOption(string key) => getOption(key, forceOptions);

    private string getOption(string key) => getOption(key, options);

    private static string getOption(string key, LightWeightMap<uint, uint> options)
    {
        if (options == null)
            return null;

        byte[] keyBytes = new byte[Encoding.UTF8.GetByteCount(key) + 1];
        Encoding.UTF8.GetBytes(key, keyBytes);
        int keyIndex = options.Contains(keyBytes);
        return keyIndex == -1 ? null : options.GetString(options.Get((uint)keyIndex));
    }

    // The JIT flags of the compilation, adjusted for altjit. This doesn't record the call in the compile result.
    public uint getJitFlags(ref CORJIT_FLAGS jitFlags, uint sizeInBytes)
    {
        uint ret = mc.repGetJitFlags(ref jitFlags, sizeInBytes);
        if (forceClearAltJitFlag)
            jitFlags.Clear(CorJitFlag.CORJIT_FLAG_ALT_JIT);
        else if (forceSetAltJitFlag)
            jitFlags.Set(CorJitFlag.CORJIT_FLAG_ALT_JIT);
        return ret;
    }

    // Memory handed to the EE, such as for the debug info passed to setVars() and setBoundaries().
    public void* allocateArray(nuint cBytes)
    {
        mc.cr.AddCall("allocateArray");
        return mc.cr.allocateMemory(cBytes);
    }

    // Arrays that the EE passed to the JIT, such as from getVars() and getBoundaries(). They live as long as
    // the compile result does.
    public void freeArray(void* array) => mc.cr.AddCall("freeArray");

    // The JIT host is crossgen2's (in src/coreclr/tools/aot/jitinterface/jithost.cpp), which gets the
    // configuration from this provider: a native object whose vtable has getIntConfigValue and getStringConfigValue.
    private IntPtr CreateConfigProvider()
    {
        var vtable = (IntPtr*)NativeMemory.Alloc(2, (nuint)sizeof(IntPtr));
        vtable[0] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int, int>)&getIntConfigValue;
        vtable[1] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, byte*, int, int>)&getStringConfigValue;

        // The JIT instance lives as long as the process.
        var provider = (IntPtr*)NativeMemory.Alloc(2, (nuint)sizeof(IntPtr));
        provider[0] = (IntPtr)vtable;
        provider[1] = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        return (IntPtr)provider;
    }

    private static JitInstance FromProvider(IntPtr provider) => (JitInstance)GCHandle.FromIntPtr(((IntPtr*)provider)[1]).Target;

    [UnmanagedCallersOnly]
    private static int getIntConfigValue(IntPtr thisHandle, byte* name, int defaultValue)
    {
        try
        {
            return FromProvider(thisHandle).GetIntConfigValue(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name), defaultValue);
        }
        catch (Exception e)
        {
            Logger.LogError($"Exception thrown: {e}");
            return defaultValue;
        }
    }

    // Returns the size of the value with its null terminator (or 0 for no value), and copies it to 'retBuffer'.
    [UnmanagedCallersOnly]
    private static int getStringConfigValue(IntPtr thisHandle, byte* name, byte* retBuffer, int retBufferLength)
    {
        try
        {
            JitInstance jit = FromProvider(thisHandle);
            byte[] value = retBuffer == null ? jit.GetStringConfigValue(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name)) : jit.pendingStringConfigValue;
            jit.pendingStringConfigValue = retBuffer == null ? value : null;
            if (value == null)
                return 0;
            if (retBuffer != null && retBufferLength >= value.Length)
                value.CopyTo(new Span<byte>(retBuffer, value.Length));
            return value.Length;
        }
        catch (Exception e)
        {
            Logger.LogError($"Exception thrown: {e}");
            return 0;
        }
    }

    // In order: the force options, the method context's recorded value, special keys, the JIT options, and the environment.
    private int GetIntConfigValue(ReadOnlySpan<byte> keyBytes, int defaultValue)
    {
        mc.cr.AddCall("getIntConfigValue");

        string key = Encoding.UTF8.GetString(keyBytes);
        int result = defaultValue;
        bool valueFound = convertStringValueToInt(key, getForceOption(key), ref result);

        if (!valueFound)
        {
            // A default value that was set explicitly can't be told apart from the default value of a key that
            // wasn't set; see CLRConfig::GetConfigValue.
            fixed (byte* pKey = NulTerminated(keyBytes))
                result = mc.repGetIntConfigValue(pKey, defaultValue);
            valueFound = result != defaultValue;
        }

        if (!valueFound)
        {
            if (key == "SuperPMIMethodContextNumber")
            {
                result = mc.index;
                valueFound = true;
            }
            else if (key == "JitReportMetrics")
            {
                result = 1;
                valueFound = true;
            }
        }

        if (!valueFound)
            valueFound = convertStringValueToInt(key, getOption(key), ref result);

        if (!valueFound)
            valueFound = convertStringValueToInt(key, GetConfigFromEnvironmentVariable(key), ref result);

        if (valueFound)
            Logger.LogDebug($"Environment variable {key}={result}");

        return valueFound ? result : defaultValue;
    }

    // In order: the force options, the method context's recorded value, the JIT options, and the environment.
    private byte[] GetStringConfigValue(ReadOnlySpan<byte> keyBytes)
    {
        mc.cr.AddCall("getStringConfigValue");

        string key = Encoding.UTF8.GetString(keyBytes);
        string result = getForceOption(key);
        if (result == null)
        {
            fixed (byte* pKey = NulTerminated(keyBytes))
            {
                byte* value = mc.repGetStringConfigValue(pKey);
                if (value != null)
                    result = Marshal.PtrToStringUTF8((IntPtr)value);
            }
        }
        result ??= getOption(key);
        result ??= GetConfigFromEnvironmentVariable(key);

        if (result == null)
            return null;

        Logger.LogDebug($"Environment variable {key}={result}");
        return NulTerminated(Encoding.UTF8.GetBytes(result));
    }

    private static byte[] NulTerminated(ReadOnlySpan<byte> bytes)
    {
        byte[] result = new byte[bytes.Length + 1];
        bytes.CopyTo(result);
        return result;
    }

    private static string GetConfigFromEnvironmentVariable(string key)
        => Environment.GetEnvironmentVariable("DOTNET_" + key) ?? Environment.GetEnvironmentVariable("COMPlus_" + key);

    // Config values are in hex, as parsed by strtoul (whose unsigned long is 32 bits on Windows, and 64 bits elsewhere).
    private static bool convertStringValueToInt(string key, string stringValue, ref int result)
    {
        if (stringValue == null)
            return false;

        if (!TryParseUnsignedLong16(stringValue, out ulong value) || value > uint.MaxValue)
        {
            Logger.LogWarning($"Can't convert int config value from string, key: {key}, string value: {stringValue}\n");
            return false;
        }

        result = (int)value;
        return true;
    }

    private static bool TryParseUnsignedLong16(string s, out ulong value)
    {
        ulong max = OperatingSystem.IsWindows() ? uint.MaxValue : ulong.MaxValue;
        value = 0;

        int i = 0;
        while (i < s.Length && (s[i] == ' ' || (s[i] >= '\t' && s[i] <= '\r')))
            i++;
        bool negative = i < s.Length && s[i] == '-';
        if (i < s.Length && (s[i] == '-' || s[i] == '+'))
            i++;
        if (i + 2 < s.Length && s[i] == '0' && (s[i + 1] | 0x20) == 'x' && char.IsAsciiHexDigit(s[i + 2]))
            i += 2;

        int start = i;
        bool overflow = false;
        for (; i < s.Length && char.IsAsciiHexDigit(s[i]); i++)
        {
            ulong digit = (ulong)(char.IsAsciiDigit(s[i]) ? s[i] - '0' : (s[i] | 0x20) - 'a' + 10);
            if (value > (max - digit) / 16)
                overflow = true;
            else
                value = value * 16 + digit;
        }

        if (i == start || overflow)
            return false;
        if (negative)
            value = unchecked(0 - value) & max;
        return true;
    }
}
